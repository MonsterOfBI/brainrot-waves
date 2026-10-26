using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace BrainrotWaves
{
    /// <summary>
    /// Управляет забегом: арены → 5 волн с выбором улучшений → заставка и бой с боссом → магазин → следующая арена.
    /// В конце начисляет кристаллы мозга для Лаборатории.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public const float ArenaHalfSize = 32f;
        const float ComboWindow = 3.5f;

        public PlayerController Player { get; private set; }
        public WeaponSystem Weapons { get; private set; }
        public HeroAbility Ability { get; private set; }
        public Enemy Boss { get; private set; }
        public int WaveNumber { get; private set; }
        public string StatusTitle { get; private set; } = "";
        public string StatusSubtitle { get; private set; } = "";
        public int Combo { get; private set; }
        public float ComboFraction => Combo > 0 ? Mathf.Clamp01(comboTimer / ComboWindow) : 0f;
        public bool CutsceneActive { get; private set; }

        GameUI ui;
        Light sun;
        Camera cam;
        ArenaGenerator arena;
        bool paused;
        bool gameOver;
        bool choosing;
        bool runFinished;
        int toSpawn;
        float breakTimeLeft;
        float comboTimer;

        void Awake()
        {
            Instance = this;
            GameSettings.EnsureLoaded();
            if (!RunState.started) RunState.StartNew(BrainrotId.Tralalero);
            PlayerController.MouseSensitivity = GameSettings.Sensitivity;
            Time.timeScale = 1f;
            Enemy.TimeScale = 1f;
            Enemy.Active.Clear();
            Pickup.Active.Clear();
            Enemy.AnyDied += OnEnemyDied;
            PostFX.Create();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Enemy.AnyDied -= OnEnemyDied;
            SoundFX.PlayAmbient(null);
            Time.timeScale = 1f;
            Enemy.TimeScale = 1f;
        }

        void Start()
        {
            var sunObject = new GameObject("Sun");
            sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = GameSettings.SunShadows;

            BuildPlayer();
            GameSettings.SetupCamera(cam);

            var uiObject = new GameObject("GameUI");
            ui = uiObject.AddComponent<GameUI>();
            ui.Bind(Player, Weapons);

            Player.Health.Died += _ => OnPlayerDied();
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
            cam.fieldOfView = GameSettings.Fov;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 400f;
            cameraObject.AddComponent<AudioListener>();

            // Отдельная камера для оружия в руках: своё постоянное поле зрения,
            // чтобы пистолет не растягивался при широком угле обзора и не проходил сквозь стены
            cam.cullingMask &= ~(1 << WeaponSystem.ViewmodelLayer);
            var vmObject = new GameObject("ViewmodelCamera");
            vmObject.transform.SetParent(cameraObject.transform, false);
            var vm = vmObject.AddComponent<Camera>();
            vm.fieldOfView = 62f;
            vm.nearClipPlane = 0.01f;
            vm.farClipPlane = 5f;
            vm.cullingMask = 1 << WeaponSystem.ViewmodelLayer;
            var vmData = vm.GetUniversalAdditionalCameraData();
            vmData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
            vmData.renderShadows = false;
            cam.GetUniversalAdditionalCameraData().cameraStack.Add(vm);

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

        // ---------------- Комбо ----------------

        void OnEnemyDied(Enemy e)
        {
            if (gameOver || e == null || e.IsBoss) return;
            Combo++;
            comboTimer = ComboWindow;
            if (Combo > RunState.bestCombo) RunState.bestCombo = Combo;
            if (Combo % 10 == 0)
            {
                int bonus = Combo / 2;
                RunState.AddCoins(bonus);
                SoundFX.Play(SoundFX.Sfx.Upgrade, 0.5f);
                if (ui != null) ui.ShowBanner("", Loc.T("КОМБО ×", "COMBO ×") + Combo + "! +" + Loc.Plural(bonus, "монета", "монеты", "монет", "coin", "coins"), 1.6f);
                if (Player != null) Particles.Sparkle(Player.transform.position + Vector3.up * 1.2f, UIKit.Accent, 20, 4f, 0.2f);
            }
        }

        // ---------------- Ход забега ----------------

        IEnumerator RunLoop()
        {
            while (RunState.arenaIndex < RunState.ArenaCount)
            {
                var owner = Brainrots.Get(RunState.CurrentArenaOwner);
                BuildArena(owner);
                SoundFX.PlayMusic((int)owner.arena);

                ui.ShowBanner(Loc.T("АРЕНА ", "ARENA ") + (RunState.arenaIndex + 1) + " / " + RunState.ArenaCount,
                    owner.arenaName + Loc.T(" · хозяин: ", " · owner: ") + owner.name, 3.5f, owner.themeColor);

                for (int wave = 1; wave <= RunState.WavesPerArena; wave++)
                {
                    WaveNumber = wave;
                    yield return Break(wave == 1 ? 6f : 4f, Loc.T("Волна ", "Wave ") + wave + Loc.T(" через", " in"));
                    yield return RunWave(owner, wave);
                    if (gameOver) yield break;

                    RunState.wavesCleared++;
                    ui.ShowBanner(Loc.T("ВОЛНА ", "WAVE ") + wave + Loc.T(" ПРОЙДЕНА!", " CLEARED!"),
                        wave < RunState.WavesPerArena ? "" : Loc.T("Готовься к боссу...", "Get ready for the boss..."), 2f, UIKit.Green);
                    MagnetizeCoins();
                    SoundFX.Play(SoundFX.Sfx.WaveStart, 0.6f);
                    yield return new WaitForSeconds(1.2f);
                    yield return ChooseUpgrade();
                }

                // Босс
                WaveNumber = RunState.WavesPerArena + 1;
                yield return Break(4f, Loc.T("Босс через", "Boss in"));
                yield return RunBoss(owner);
                if (gameOver) yield break;

                RunState.bossesKilled++;
                MagnetizeCoins();
                Player.Health.Heal(Player.Health.maxHealth * 0.5f);
                SoundFX.Play(SoundFX.Sfx.Victory, 0.8f);
                ui.ShowBanner(Loc.T("БОСС ПОВЕРЖЕН!", "BOSS DEFEATED!"), owner.name + Loc.T(" больше не хозяин арены", " no longer owns this arena"), 3f, UIKit.Accent);
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
            if (sun != null) sun.shadows = GameSettings.SunShadows;
            Player.Teleport(arena.PlayerSpawn, 0f);
            Boss = null;
            Particles.Ambient(owner.arena, Player.transform);
            SoundFX.PlayAmbient(owner.arena);
            if (PostFX.Instance != null) PostFX.Instance.ApplyTheme(owner.arena);
        }

        void ClearLeftovers()
        {
            foreach (var e in new List<Enemy>(Enemy.Active)) if (e != null) Destroy(e.gameObject);
            Enemy.Active.Clear();
            foreach (var p in new List<Pickup>(Pickup.Active)) if (p != null) Destroy(p.gameObject);
            ClearProjectiles();
        }

        static void ClearProjectiles()
        {
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

        string ArenaLine() => Brainrots.Get(RunState.CurrentArenaOwner).arenaName + Loc.T(" · арена ", " · arena ") + (RunState.arenaIndex + 1) + "/" + RunState.ArenaCount
                              + (RunState.threat > 0 ? Loc.T(" · угроза ", " · threat ") + RunState.threat : "");

        IEnumerator RunWave(BrainrotInfo owner, int wave)
        {
            int a = RunState.arenaIndex;
            toSpawn = 6 + (wave - 1) * 2 + a * 3;
            float hpMult = (1f + 0.12f * (wave - 1) + 0.4f * a) * Meta.ThreatHealth(RunState.threat);
            float dmgMult = (1f + 0.05f * (wave - 1) + 0.12f * a) * Meta.ThreatDamage(RunState.threat);
            int maxAlive = 10 + a * 2 + RunState.threat;

            SoundFX.Play(SoundFX.Sfx.WaveStart, 0.8f);
            ui.ShowBanner(Loc.T("ВОЛНА ", "WAVE ") + wave, owner.shortName + Loc.T(" и компания идут!", " and friends are coming!"), 2f, owner.themeColor);

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
                StatusTitle = Loc.T("Волна ", "Wave ") + wave + " / " + RunState.WavesPerArena;
                StatusSubtitle = Loc.T("Осталось врагов: ", "Enemies left: ") + (toSpawn + Enemy.Active.Count) + " · " + Brainrots.Get(RunState.CurrentArenaOwner).arenaName;
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
            ui.ShowBanner("", Loc.T("На арене появился ящик с оружием!", "A weapon crate appeared in the arena!"), 2.5f);
        }

        IEnumerator RunBoss(BrainrotInfo owner)
        {
            int a = RunState.arenaIndex;
            float bossHealth = 1200f * (1f + 0.55f * a) * Meta.ThreatHealth(RunState.threat);
            float dmgMult = (1f + 0.2f + 0.12f * a) * Meta.ThreatDamage(RunState.threat);

            SoundFX.PlayMusic(100 + (int)owner.arena);

            Vector3 spawn = PickSpawnPoint();
            Boss = Enemy.Create(owner.id, spawn, true, bossHealth, dmgMult);
            yield return BossIntro(Boss, owner);
            if (gameOver) yield break;

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
                        Enemy.Create(id, PickSpawnPoint(), false, info.enemyHealth * (1f + 0.4f * a) * Meta.ThreatHealth(RunState.threat), (1f + 0.12f * a) * Meta.ThreatDamage(RunState.threat));
                    }
                }
                StatusTitle = Loc.T("БИТВА С БОССОМ", "BOSS FIGHT");
                StatusSubtitle = ArenaLine();
                yield return null;
            }

            // Замедление в момент победы
            Combo = 0;
            float slow = 1.4f;
            while (slow > 0f && !gameOver)
            {
                if (!paused && !choosing) Time.timeScale = 0.35f;
                slow -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (!paused && !choosing) Time.timeScale = 1f;

            // Остальные враги разбегаются
            foreach (var e in new List<Enemy>(Enemy.Active))
                if (e != null && !e.IsDead) e.TakeHit(99999f, false, e.transform.position + Vector3.up);
            SoundFX.PlayMusic((int)owner.arena);
        }

        /// <summary>
        /// Заставка босса: камера облетает босса, он рычит, внизу появляется его имя.
        /// Можно пропустить кликом или пробелом.
        /// </summary>
        IEnumerator BossIntro(Enemy boss, BrainrotInfo owner)
        {
            CutsceneActive = true;
            ui.SetLetterbox(true);
            ClearProjectiles();
            Enemy.TimeScale = 0f;
            Player.InputEnabled = false;
            float savedArmor = Player.Health.damageTakenMultiplier;
            Player.Health.damageTakenMultiplier = 0f;

            var camObject = new GameObject("CutsceneCamera");
            var cutCam = camObject.AddComponent<Camera>();
            cutCam.fieldOfView = 55f;
            cutCam.nearClipPlane = 0.05f;
            cutCam.farClipPlane = 400f;
            cutCam.clearFlags = cam.clearFlags;
            cutCam.backgroundColor = cam.backgroundColor;
            GameSettings.SetupCamera(cutCam);
            cam.enabled = false;

            Vector3 startPos = cam.transform.position;
            Quaternion startRot = cam.transform.rotation;
            Vector3 b = boss.transform.position;
            Vector3 toPlayer = Player.transform.position - b;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.01f) toPlayer = Vector3.back;
            toPlayer.Normalize();
            boss.transform.rotation = Quaternion.LookRotation(toPlayer);
            float s = boss.Size;
            bool flyer = owner.behavior == EnemyBehavior.Flyer;
            Vector3 focus = boss.transform.position + Vector3.up * (flyer ? 1.2f : 1.3f) * s;
            Vector3 side = Vector3.Cross(Vector3.up, toPlayer);
            Vector3 shotA = focus + toPlayer * 3.4f * s + side * 1.6f * s + Vector3.up * 0.2f * s;
            Vector3 shotB = focus + toPlayer * 2.4f * s - side * 1.2f * s - Vector3.up * 0.35f * s;

            const float flyIn = 0.9f, hold = 2.2f, flyOut = 0.6f;
            float t = 0f;
            bool roared = false, skipped = false;
            while (t < flyIn + hold + flyOut)
            {
                t += Time.deltaTime;
                if (t > 0.6f && !skipped)
                {
                    var kb = Keyboard.current;
                    var mouse = Mouse.current;
                    if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (mouse != null && mouse.leftButton.wasPressedThisFrame))
                    {
                        skipped = true;
                        t = Mathf.Max(t, flyIn + hold);
                        if (!roared) { roared = true; ui.ShowBossIntro(owner); }
                    }
                }

                Vector3 pos;
                Quaternion rot;
                if (t < flyIn)
                {
                    float k = Ease.InOutCubic(t / flyIn);
                    pos = Vector3.Lerp(startPos, shotA, k);
                    rot = Quaternion.Slerp(startRot, Quaternion.LookRotation(focus - shotA), k);
                }
                else if (t < flyIn + hold)
                {
                    if (!roared)
                    {
                        roared = true;
                        boss.Roar(1.6f);
                        ui.ShowBossIntro(owner);
                        SoundFX.Play(SoundFX.Sfx.BossRoar, 1f, 0f);
                        CameraShake.Add(0.6f);
                        PostFX.Impact(1f);
                        Particles.Dust(new Vector3(b.x, 0.05f, b.z), 2.5f * s, 30, GameAssets.Hex("#A1887F"));
                        FX.Ring(new Vector3(b.x, 0.05f, b.z), 4f * s, owner.themeColor, 0.6f);
                    }
                    float k = Ease.InOutCubic((t - flyIn) / hold);
                    pos = Vector3.Lerp(shotA, shotB, k);
                    rot = Quaternion.LookRotation(focus - pos);
                }
                else
                {
                    float k = Ease.InOutCubic((t - flyIn - hold) / flyOut);
                    if (k > 0.05f) ui.HideBossIntro();
                    Vector3 back = cam.transform.position;
                    pos = Vector3.Lerp(shotB, back, k);
                    rot = Quaternion.Slerp(Quaternion.LookRotation(focus - shotB), cam.transform.rotation, k);
                }

                float shake = CameraShake.trauma * CameraShake.trauma;
                CameraShake.trauma = Mathf.Max(0f, CameraShake.trauma - Time.deltaTime * 1.2f);
                cutCam.transform.SetPositionAndRotation(pos + Random.insideUnitSphere * shake * 0.3f, rot);
                yield return null;
            }

            ui.HideBossIntro();
            cam.enabled = true;
            Destroy(camObject);
            Enemy.TimeScale = 1f;
            Player.InputEnabled = true;
            Player.Health.damageTakenMultiplier = savedArmor;
            CutsceneActive = false;
            ui.ShowBanner(Loc.T("БОСС: ", "BOSS: ") + owner.name.ToUpper(), owner.bossTitle, 2.5f, UIKit.Pink);
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
                Enemy.Create(bossId, pos, false, info.enemyHealth * (0.8f + 0.4f * a) * Meta.ThreatHealth(RunState.threat), (1f + 0.1f * a) * Meta.ThreatDamage(RunState.threat));
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
            Player.InputEnabled = !CutsceneActive;
            PlayerController.LockCursor(true);
        }

        IEnumerator ChooseUpgrade()
        {
            if (gameOver) yield break;
            choosing = true;
            bool picked = false;
            OpenScreen();
            SoundFX.Play(SoundFX.Sfx.Upgrade, 0.6f);

            void Pick(UpgradeDef upgrade)
            {
                if (picked) return;
                picked = true;
                upgrade.apply?.Invoke(RunState.stats);
                Player.ApplyStats(false);
                if (upgrade.healAmount > 0f) Player.Health.Heal(upgrade.healAmount);
                if (upgrade.healsToFull) Player.Health.HealFull();
                SoundFX.Play(SoundFX.Sfx.Heal, 0.6f);
            }

            void Show()
            {
                ui.ShowUpgrades(Upgrades.PickThree(), Pick, RunState.stats.rerolls > 0 ? () =>
                {
                    if (RunState.rerollsLeft <= 0) return;
                    RunState.rerollsLeft--;
                    Show();
                } : (System.Action)null);
            }

            Show();
            while (!picked) yield return null;
            CloseScreen();
            choosing = false;
        }

        IEnumerator Shop()
        {
            choosing = true;
            bool done = false;
            OpenScreen();
            SoundFX.PlayMusic(SoundFX.MenuMusic);
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
                    name = WeaponDef.Rifle.name, description = Loc.T("Скорострельный, 32 патрона.\nЗажми ЛКМ и поливай!", "Rapid fire, 32 rounds.\nHold LMB and spray!"), price = WeaponDef.Rifle.price,
                    color = UIKit.Cyan, icon = IconKind.Bullet, soldOut = Weapons.Has(WeaponId.Rifle),
                    buy = () => { Weapons.Give(WeaponId.Rifle); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = WeaponDef.Shotgun.name, description = Loc.T("9 дробин за выстрел.\nУничтожает вблизи.", "9 pellets per shot.\nDevastating up close."), price = WeaponDef.Shotgun.price,
                    color = UIKit.Cyan, icon = IconKind.Ammo, soldOut = Weapons.Has(WeaponId.Shotgun),
                    buy = () => { Weapons.Give(WeaponId.Shotgun); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = Loc.T("Пицца-аптечка", "Pizza medkit"), description = Loc.T("Полностью восстановить\nздоровье", "Fully restore\nhealth"), price = Price("heal", 20),
                    color = UIKit.Green, icon = IconKind.Plus, soldOut = Player.Health.Current >= Player.Health.maxHealth,
                    buy = () => { Player.Health.HealFull(); Count("heal"); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = Loc.T("Здоровье +25", "Health +25"), description = Loc.T("Увеличить максимум\nздоровья на 25", "Increase max\nhealth by 25"), price = Price("hp", 40),
                    color = UIKit.Green, icon = IconKind.Heart,
                    buy = () => { RunState.stats.bonusMaxHealth += 25f; Player.ApplyStats(false); Player.Health.Heal(25f); Count("hp"); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = Loc.T("Урон +15%", "Damage +15%"), description = Loc.T("Все пули и взрывы\nбьют сильнее", "All bullets and\nexplosions hit harder"), price = Price("dmg", 50),
                    color = UIKit.Pink, icon = IconKind.Sword,
                    buy = () => { RunState.stats.damageMultiplier += 0.15f; Count("dmg"); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = Loc.T("Способность −20%", "Ability −20%"), description = Loc.T("Способность\nперезаряжается быстрее", "Your ability\nrecharges faster"), price = Price("cd", 45),
                    color = UIKit.Accent, icon = IconKind.Bolt,
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

        // ---------------- Смерть и второй шанс ----------------

        void OnPlayerDied()
        {
            if (gameOver) return;
            if (RunState.revivesLeft > 0)
            {
                RunState.revivesLeft--;
                StartCoroutine(Revive());
                return;
            }
            StartCoroutine(Defeat());
        }

        IEnumerator Revive()
        {
            Player.Health.Revive(0.5f, 3f);
            Vector3 p = Player.transform.position;
            SoundFX.Play(SoundFX.Sfx.Heal, 1f);
            SoundFX.Play(SoundFX.Sfx.Explosion, 0.8f);
            FX.Ring(p, 10f, UIKit.Accent, 0.6f);
            Particles.Sparkle(p + Vector3.up, UIKit.Accent, 50, 8f, 0.3f);
            Combat.DamageEnemiesInRadius(p, 10f, 150f * RunState.stats.damageMultiplier, 22f);
            ClearProjectiles();
            PostFX.Impact(1f);
            ui.ShowBanner(Loc.T("ВТОРОЙ ШАНС!", "SECOND CHANCE!"), Loc.T("Мама-миа, ты снова в строю!", "Mamma mia, you're back!"), 2.5f, UIKit.Accent);
            float t = 0.8f;
            while (t > 0f)
            {
                if (!paused && !choosing) Time.timeScale = 0.4f;
                t -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (!paused && !choosing) Time.timeScale = 1f;
        }

        Meta.RunReward FinishRun(bool victory)
        {
            if (runFinished) return null;
            runFinished = true;
            return Meta.FinishRun(victory);
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
            var reward = FinishRun(false);
            OpenScreen();
            ui.ShowEnd(false, reward, Retry, ToMenu);
        }

        IEnumerator Victory()
        {
            gameOver = true;
            SoundFX.Play(SoundFX.Sfx.Victory, 1f, 0f);
            ui.ShowBanner(Loc.T("ПОБЕДА!", "VICTORY!"), Loc.T("Все брейнроты повержены!", "All brainrots defeated!"), 3f, UIKit.Accent);
            yield return new WaitForSeconds(2.5f);
            var reward = FinishRun(true);
            OpenScreen();
            ui.ShowEnd(true, reward, Retry, ToMenu);
        }

        void Retry()
        {
            if (SceneLoader.Busy) return;
            RunState.StartNew(RunState.hero, RunState.threat);
            SceneLoader.Load("Game");
        }

        void ToMenu()
        {
            if (SceneLoader.Busy) return;
            if (!gameOver) FinishRun(false);
            RunState.started = false;
            SceneLoader.Load("MainMenu");
        }

        void Update()
        {
            if (!gameOver && Time.timeScale > 0f) RunState.time += Time.deltaTime;

            if (Combo > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f) Combo = 0;
            }

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame && !gameOver && !choosing && !CutsceneActive && !SceneLoader.Busy)
            {
                if (SettingsScreen.IsOpen || SettingsScreen.ClosedByEscFrame == Time.frameCount) { }
                else if (paused) Resume();
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
