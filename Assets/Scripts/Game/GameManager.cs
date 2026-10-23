using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BrainrotWaves
{
    /// <summary>
    /// Управляет забегом: арены → 5 волн с выбором улучшений → босс → магазин → следующая арена.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public const float ArenaHalfSize = 32f;

        public PlayerController Player { get; private set; }
        public WeaponSystem Weapons { get; private set; }
        public HeroAbility Ability { get; private set; }
        public Enemy Boss { get; private set; }
        public int WaveNumber { get; private set; }
        public string StatusTitle { get; private set; } = "";
        public string StatusSubtitle { get; private set; } = "";

        GameUI ui;
        Light sun;
        Camera cam;
        ArenaGenerator arena;
        bool paused;
        bool gameOver;
        bool choosing;
        int toSpawn;
        float breakTimeLeft;

        void Awake()
        {
            Instance = this;
            if (!RunState.started) RunState.StartNew(BrainrotId.Tralalero);
            PlayerController.MouseSensitivity = PlayerPrefs.GetFloat("BW_Sens", 0.1f);
            Time.timeScale = 1f;
            Enemy.TimeScale = 1f;
            Enemy.Active.Clear();
            Pickup.Active.Clear();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
            Enemy.TimeScale = 1f;
        }

        void Start()
        {
            var sunObject = new GameObject("Sun");
            sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;

            BuildPlayer();

            var uiObject = new GameObject("GameUI");
            ui = uiObject.AddComponent<GameUI>();
            ui.Bind(Player, Weapons);

            Player.Health.Died += _ => StartCoroutine(Defeat());
            StartCoroutine(RunLoop());
        }

        void BuildPlayer()
        {
            var hero = RunState.HeroInfo;
            var playerObject = new GameObject("Player");
            playerObject.layer = Combat.PlayerLayer;
            playerObject.tag = "Player";
            var controller = playerObject.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.4f;
            controller.stepOffset = 0.45f;
            controller.slopeLimit = 50f;
            playerObject.AddComponent<Health>();

            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(playerObject.transform, false);
            pivot.localPosition = new Vector3(0f, 1.6f, 0f);

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(pivot, false);
            cam = cameraObject.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 400f;
            cameraObject.AddComponent<AudioListener>();

            Player = playerObject.AddComponent<PlayerController>();
            Player.cameraPivot = pivot;
            Player.cam = cam;

            var weaponObject = new GameObject("Weapons");
            weaponObject.transform.SetParent(cameraObject.transform, false);
            Weapons = weaponObject.AddComponent<WeaponSystem>();
            Weapons.cam = cam;
            Weapons.accentColor = hero.themeColor;
            foreach (var id in new List<WeaponId>(RunState.ownedWeapons)) Weapons.Give(id, false);
            if (Weapons.SlotCount == 0) Weapons.Give(WeaponId.Pistol);

            Ability = playerObject.AddComponent<HeroAbility>();
            Ability.hero = hero;
            Ability.cam = cam;

            Player.Setup(hero);
        }

        // ---------------- Ход забега ----------------

        IEnumerator RunLoop()
        {
            while (RunState.arenaIndex < RunState.ArenaCount)
            {
                var owner = Brainrots.Get(RunState.CurrentArenaOwner);
                BuildArena(owner);
                SoundFX.PlayMusic((int)owner.arena);

                ui.ShowBanner("АРЕНА " + (RunState.arenaIndex + 1) + " / " + RunState.ArenaCount, owner.arenaName + " · хозяин: " + owner.name, 3.5f, owner.themeColor);

                for (int wave = 1; wave <= RunState.WavesPerArena; wave++)
                {
                    WaveNumber = wave;
                    yield return Break(wave == 1 ? 6f : 4f, "Волна " + wave + " через");
                    yield return RunWave(owner, wave);
                    if (gameOver) yield break;

                    ui.ShowBanner("ВОЛНА " + wave + " ПРОЙДЕНА!", wave < RunState.WavesPerArena ? "" : "Готовься к боссу...", 2f, UIKit.Green);
                    MagnetizeCoins();
                    SoundFX.Play(SoundFX.Sfx.WaveStart, 0.6f);
                    yield return new WaitForSeconds(1.2f);
                    yield return ChooseUpgrade();
                }

                // Босс
                WaveNumber = RunState.WavesPerArena + 1;
                yield return Break(4f, "Босс через");
                yield return RunBoss(owner);
                if (gameOver) yield break;

                MagnetizeCoins();
                Player.Health.Heal(Player.Health.maxHealth * 0.5f);
                SoundFX.Play(SoundFX.Sfx.Victory, 0.8f);
                ui.ShowBanner("БОСС ПОВЕРЖЕН!", owner.name + " больше не хозяин арены", 3f, UIKit.Accent);
                yield return new WaitForSeconds(3f);

                if (RunState.arenaIndex >= RunState.ArenaCount - 1)
                {
                    yield return Victory();
                    yield break;
                }

                yield return Shop();
                RunState.arenaIndex++;
            }
        }

        void BuildArena(BrainrotInfo owner)
        {
            arena?.Destroy();
            ClearLeftovers();
            arena = new ArenaGenerator(ArenaHalfSize);
            arena.Build(owner.arena, sun, cam);
            Player.Teleport(arena.PlayerSpawn, 0f);
            Boss = null;
        }

        void ClearLeftovers()
        {
            foreach (var e in new List<Enemy>(Enemy.Active)) if (e != null) Destroy(e.gameObject);
            Enemy.Active.Clear();
            foreach (var p in new List<Pickup>(Pickup.Active)) if (p != null) Destroy(p.gameObject);
            foreach (var p in FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Destroy(p.gameObject);
        }

        IEnumerator Break(float seconds, string label)
        {
            breakTimeLeft = seconds;
            while (breakTimeLeft > 0f)
            {
                breakTimeLeft -= Time.deltaTime;
                StatusTitle = label + " " + Mathf.CeilToInt(breakTimeLeft);
                StatusSubtitle = ArenaLine();
                yield return null;
            }
        }

        string ArenaLine() => Brainrots.Get(RunState.CurrentArenaOwner).arenaName + " · арена " + (RunState.arenaIndex + 1) + "/" + RunState.ArenaCount;

        IEnumerator RunWave(BrainrotInfo owner, int wave)
        {
            int a = RunState.arenaIndex;
            toSpawn = 6 + (wave - 1) * 2 + a * 3;
            float hpMult = 1f + 0.12f * (wave - 1) + 0.4f * a;
            float dmgMult = 1f + 0.05f * (wave - 1) + 0.12f * a;
            int maxAlive = 10 + a * 2;

            SoundFX.Play(SoundFX.Sfx.WaveStart, 0.8f);
            ui.ShowBanner("ВОЛНА " + wave, owner.shortName + " и компания идут!", 2f, owner.themeColor);

            // Ящик с оружием на 2-й и 4-й волне
            if (wave == 2 || wave == 4) SpawnWeaponCrate();

            float spawnTimer = 0f;
            while (toSpawn > 0 || Enemy.Active.Count > 0)
            {
                if (gameOver) yield break;
                spawnTimer -= Time.deltaTime;
                if (toSpawn > 0 && spawnTimer <= 0f && Enemy.Active.Count < maxAlive)
                {
                    var id = PickEnemyType(owner.id, wave);
                    var info = Brainrots.Get(id);
                    Enemy.Create(id, PickSpawnPoint(), false, info.enemyHealth * hpMult, dmgMult);
                    toSpawn--;
                    spawnTimer = Mathf.Max(0.35f, 0.9f - a * 0.1f);
                }
                StatusTitle = "Волна " + wave + " / " + RunState.WavesPerArena;
                StatusSubtitle = "Осталось врагов: " + (toSpawn + Enemy.Active.Count) + " · " + Brainrots.Get(RunState.CurrentArenaOwner).arenaName;
                yield return null;
            }
        }

        BrainrotId PickEnemyType(BrainrotId owner, int wave)
        {
            if (RunState.arenaIndex == 0 && wave <= 2) return owner;
            var pool = new List<BrainrotId>();
            foreach (var info in Brainrots.All)
            {
                if (info.id == RunState.hero) continue;
                int weight = info.id == owner ? 4 : 1;
                for (int i = 0; i < weight; i++) pool.Add(info.id);
            }
            return pool[Random.Range(0, pool.Count)];
        }

        Vector3 PickSpawnPoint()
        {
            var candidates = new List<Vector3>();
            Vector3 p = Player.transform.position;
            foreach (var s in arena.spawnPoints)
                if (Vector3.Distance(new Vector3(s.x, 0f, s.z), new Vector3(p.x, 0f, p.z)) > 16f) candidates.Add(s);
            if (candidates.Count == 0) candidates.AddRange(arena.spawnPoints);
            return candidates[Random.Range(0, candidates.Count)];
        }

        void SpawnWeaponCrate()
        {
            var missing = new List<WeaponId>();
            if (!Weapons.Has(WeaponId.Rifle)) missing.Add(WeaponId.Rifle);
            if (!Weapons.Has(WeaponId.Shotgun)) missing.Add(WeaponId.Shotgun);
            if (missing.Count == 0 || Random.value > 0.6f) return;
            Vector3 pos = new Vector3(Random.Range(-12f, 12f), 0.5f, Random.Range(-12f, 12f));
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) pos = hit.position + Vector3.up * 0.5f;
            Pickup.Spawn(Pickup.Kind.WeaponCrate, pos, 1, missing[Random.Range(0, missing.Count)]);
            ui.ShowBanner("", "На арене появился ящик с оружием!", 2.5f);
        }

        IEnumerator RunBoss(BrainrotInfo owner)
        {
            int a = RunState.arenaIndex;
            float bossHealth = 1200f * (1f + 0.55f * a);
            float dmgMult = 1f + 0.2f + 0.12f * a;

            SoundFX.PlayMusic(100 + (int)owner.arena);
            SoundFX.Play(SoundFX.Sfx.BossRoar, 1f, 0f);
            CameraShake.Add(0.5f);

            Vector3 spawn = PickSpawnPoint();
            Boss = Enemy.Create(owner.id, spawn, true, bossHealth, dmgMult);
            ui.ShowBanner("БОСС: " + owner.name.ToUpper(), owner.bossTitle, 3.5f, UIKit.Pink);

            float minionTimer = 8f;
            while (Boss != null && !Boss.IsDead)
            {
                if (gameOver) yield break;
                minionTimer -= Time.deltaTime;
                if (minionTimer <= 0f && Enemy.Active.Count < 8)
                {
                    minionTimer = Mathf.Max(5f, 9f - a);
                    for (int i = 0; i < 2; i++)
                    {
                        var id = PickEnemyType(owner.id, 5);
                        var info = Brainrots.Get(id);
                        Enemy.Create(id, PickSpawnPoint(), false, info.enemyHealth * (1f + 0.4f * a), 1f + 0.12f * a);
                    }
                }
                StatusTitle = "БИТВА С БОССОМ";
                StatusSubtitle = owner.arenaName + " · арена " + (a + 1) + "/" + RunState.ArenaCount;
                yield return null;
            }

            // Остальные враги разбегаются
            foreach (var e in new List<Enemy>(Enemy.Active))
                if (e != null && !e.IsDead) e.TakeHit(99999f, false, e.transform.position + Vector3.up);
            SoundFX.PlayMusic((int)owner.arena);
        }

        void MagnetizeCoins()
        {
            foreach (var p in Pickup.Active)
                if (p != null && p.kind == Pickup.Kind.Coin) p.Magnetize();
        }

        /// <summary>Вызывается боссом: призвать помощников рядом с ним.</summary>
        public void SummonMinions(BrainrotId bossId, Vector3 near, int count)
        {
            int a = RunState.arenaIndex;
            for (int i = 0; i < count; i++)
            {
                if (Enemy.Active.Count >= 12) return;
                Vector3 offset = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(3f, 6f);
                Vector3 pos = near + offset;
                pos.y = 0f;
                pos.x = Mathf.Clamp(pos.x, -ArenaHalfSize + 3f, ArenaHalfSize - 3f);
                pos.z = Mathf.Clamp(pos.z, -ArenaHalfSize + 3f, ArenaHalfSize - 3f);
                var info = Brainrots.Get(bossId);
                Enemy.Create(bossId, pos, false, info.enemyHealth * (0.8f + 0.4f * a), 1f + 0.1f * a);
            }
        }

        // ---------------- Экраны ----------------

        void OpenScreen()
        {
            Time.timeScale = 0f;
            AudioListener.pause = false;
            Player.InputEnabled = false;
            PlayerController.LockCursor(false);
        }

        void CloseScreen()
        {
            ui.CloseScreen();
            Time.timeScale = 1f;
            Player.InputEnabled = true;
            PlayerController.LockCursor(true);
        }

        IEnumerator ChooseUpgrade()
        {
            if (gameOver) yield break;
            choosing = true;
            bool picked = false;
            OpenScreen();
            SoundFX.Play(SoundFX.Sfx.Upgrade, 0.6f);
            ui.ShowUpgrades(Upgrades.PickThree(), upgrade =>
            {
                if (picked) return;
                picked = true;
                upgrade.apply?.Invoke(RunState.stats);
                Player.ApplyStats(false);
                if (upgrade.healAmount > 0f) Player.Health.Heal(upgrade.healAmount);
                if (upgrade.healsToFull) Player.Health.HealFull();
                SoundFX.Play(SoundFX.Sfx.Heal, 0.6f);
            });
            while (!picked) yield return null;
            CloseScreen();
            choosing = false;
        }

        IEnumerator Shop()
        {
            choosing = true;
            bool done = false;
            OpenScreen();
            SoundFX.PlayMusic(0);
            var counts = new Dictionary<string, int>();
            int a = RunState.arenaIndex;

            int Price(string key, int basePrice)
            {
                counts.TryGetValue(key, out int c);
                return Mathf.RoundToInt(basePrice * (1f + 0.15f * a) * Mathf.Pow(1.5f, c));
            }

            void Count(string key)
            {
                counts.TryGetValue(key, out int c);
                counts[key] = c + 1;
            }

            List<GameUI.ShopItem> Items()
            {
                var list = new List<GameUI.ShopItem>();
                list.Add(new GameUI.ShopItem
                {
                    name = "Автомат", description = "Скорострельный, 32 патрона.\nЗажми ЛКМ и поливай!", price = WeaponDef.Rifle.price,
                    color = UIKit.Cyan, soldOut = Weapons.Has(WeaponId.Rifle),
                    buy = () => { Weapons.Give(WeaponId.Rifle); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = "Дробовик", description = "9 дробин за выстрел.\nУничтожает вблизи.", price = WeaponDef.Shotgun.price,
                    color = UIKit.Cyan, soldOut = Weapons.Has(WeaponId.Shotgun),
                    buy = () => { Weapons.Give(WeaponId.Shotgun); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = "Пицца-аптечка", description = "Полностью восстановить\nздоровье", price = Price("heal", 20),
                    color = UIKit.Green, soldOut = Player.Health.Current >= Player.Health.maxHealth,
                    buy = () => { Player.Health.HealFull(); Count("heal"); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = "Здоровье +25", description = "Увеличить максимум\nздоровья на 25", price = Price("hp", 40),
                    color = UIKit.Green,
                    buy = () => { RunState.stats.bonusMaxHealth += 25f; Player.ApplyStats(false); Player.Health.Heal(25f); Count("hp"); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = "Урон +15%", description = "Все пули и взрывы\nбьют сильнее", price = Price("dmg", 50),
                    color = UIKit.Pink,
                    buy = () => { RunState.stats.damageMultiplier += 0.15f; Count("dmg"); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = "Способность −20%", description = "Способность Q\nперезаряжается быстрее", price = Price("cd", 45),
                    color = UIKit.Accent,
                    buy = () => { RunState.stats.abilityCooldownMultiplier *= 0.8f; Count("cd"); return true; }
                });
                return list;
            }

            ui.ShowShop(Items, () => done = true);
            while (!done) yield return null;
            Weapons.RefillAll();
            CloseScreen();
            choosing = false;
        }

        IEnumerator Defeat()
        {
            if (gameOver) yield break;
            gameOver = true;
            Boss = null;
            SoundFX.StopMusic();
            SoundFX.Play(SoundFX.Sfx.Defeat, 1f, 0f);
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(1.5f);
            bool record = RunState.SaveResult(false);
            OpenScreen();
            ui.ShowEnd(false, record, Retry, ToMenu);
        }

        IEnumerator Victory()
        {
            gameOver = true;
            SoundFX.Play(SoundFX.Sfx.Victory, 1f, 0f);
            ui.ShowBanner("ПОБЕДА!", "Все брейнроты повержены!", 3f, UIKit.Accent);
            yield return new WaitForSeconds(2.5f);
            bool record = RunState.SaveResult(true);
            OpenScreen();
            ui.ShowEnd(true, record, Retry, ToMenu);
        }

        void Retry()
        {
            RunState.StartNew(RunState.hero);
            Time.timeScale = 1f;
            SceneManager.LoadScene("Game");
        }

        void ToMenu()
        {
            Time.timeScale = 1f;
            RunState.started = false;
            SceneManager.LoadScene("MainMenu");
        }

        void Update()
        {
            if (!gameOver && Time.timeScale > 0f) RunState.time += Time.deltaTime;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame && !gameOver && !choosing)
            {
                if (paused) Resume();
                else Pause();
            }

            // Клик по окну игры снова захватывает курсор
            if (!paused && !choosing && !gameOver && mouse != null && mouse.leftButton.wasPressedThisFrame && !PlayerController.IsCursorLocked)
                PlayerController.LockCursor(true);
        }

        void Pause()
        {
            paused = true;
            OpenScreen();
            ui.ShowPause(Resume, ToMenu);
        }

        void Resume()
        {
            paused = false;
            CloseScreen();
        }
    }
}
