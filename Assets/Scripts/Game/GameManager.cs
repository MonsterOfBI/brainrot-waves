using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace BrainrotWaves
{
    /// <summary>
    /// Управляет забегом в одном из трёх режимов:
    /// • Сюжет — 5 арен: 5 волн с выбором улучшений → заставка и бой с боссом → магазин → следующая арена.
    /// • Выживание — бесконечные волны всех 26 брейнротов, босс каждые 10 волн, после босса — новая арена.
    /// • Защита — 15 волн, брейнроты рвутся к пиццерии Нонны, боссы на 5, 10 и 15 волне.
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
        public DefenseObjective Objective { get; private set; }
        public int WaveNumber { get; private set; }
        public string StatusTitle { get; private set; } = "";
        public string StatusSubtitle { get; private set; } = "";
        public int Combo { get; private set; }
        public float ComboFraction => Combo > 0 ? Mathf.Clamp01(comboTimer / ComboWindow) : 0f;
        public bool CutsceneActive { get; private set; }

        GameUI ui;
        Light sun;
        Camera cam;
        Light flashlight;
        ArenaGenerator arena;
        BrainrotInfo arenaOwner;
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
            if (!RunState.started) RunState.StartNew(BrainrotId.Tralalero, GameMode.Story);
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
            vmData.renderType = CameraRenderType.Overlay;
            vmData.renderShadows = false;
            cam.GetUniversalAdditionalCameraData().cameraStack.Add(vm);

            // Фонарик (включается на ночной арене)
            var flash = new GameObject("Flashlight");
            flash.transform.SetParent(cameraObject.transform, false);
            flash.transform.localPosition = new Vector3(0.3f, -0.2f, 0f);
            flashlight = flash.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.range = 30f;
            flashlight.spotAngle = 70f;
            flashlight.innerSpotAngle = 35f;
            flashlight.intensity = 25f;
            flashlight.color = new Color(1f, 0.95f, 0.85f);
            flashlight.shadows = LightShadows.None;
            flashlight.enabled = false;

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
            switch (RunState.mode)
            {
                case GameMode.Survival: yield return SurvivalLoop(); break;
                case GameMode.Defense: yield return DefenseLoop(); break;
                default: yield return StoryLoop(); break;
            }
        }

        // ===== Сюжет =====

        IEnumerator StoryLoop()
        {
            while (RunState.arenaIndex < RunState.ArenaCount)
            {
                var owner = Brainrots.Get(RunState.CurrentArenaOwner);
                BuildArena(owner);

                ui.ShowBanner(Loc.T("АРЕНА ", "ARENA ") + (RunState.arenaIndex + 1) + " / " + RunState.ArenaCount,
                    owner.arenaName + Loc.T(" · хозяин: ", " · owner: ") + owner.name, 3.5f, owner.themeColor);

                int a = RunState.arenaIndex;
                for (int wave = 1; wave <= RunState.WavesPerArena; wave++)
                {
                    WaveNumber = wave;
                    yield return Break(wave == 1 ? 6f : 4f, Loc.T("Волна ", "Wave ") + wave + Loc.T(" через", " in"));
                    int count = 6 + (wave - 1) * 2 + a * 3;
                    float hpMult = (1f + 0.12f * (wave - 1) + 0.4f * a) * Meta.ThreatHealth(RunState.threat);
                    float dmgMult = (1f + 0.05f * (wave - 1) + 0.12f * a) * Meta.ThreatDamage(RunState.threat);
                    float elite = wave >= 4 ? 0.06f + 0.02f * a : 0f;
                    int w = wave;
                    yield return RunWave(count, () => PickStoryEnemy(owner.id, w), hpMult, dmgMult, elite, 10 + a * 2 + RunState.threat,
                        Loc.T("Волна ", "Wave ") + wave + " / " + RunState.WavesPerArena, owner.shortName + Loc.T(" и компания идут!", " and friends are coming!"), owner.themeColor);
                    if (gameOver) yield break;
                    if (wave == 2 || wave == 4) SpawnWeaponCrate();

                    yield return WaveCleared(wave, wave < RunState.WavesPerArena ? "" : Loc.T("Готовься к боссу...", "Get ready for the boss..."));
                    yield return ChooseUpgrade();
                }

                // Босс: заметно живучее, с фазами ярости и щитом
                WaveNumber = RunState.WavesPerArena + 1;
                yield return Break(4f, Loc.T("Босс через", "Boss in"));
                float bossHealth = 5000f * (1f + 0.6f * a) * Meta.ThreatHealth(RunState.threat);
                float bossDmg = (1.2f + 0.12f * a) * Meta.ThreatDamage(RunState.threat);
                yield return RunBoss(owner, bossHealth, bossDmg, Mathf.Max(5f, 9f - a), 1f + 0.4f * a);
                if (gameOver) yield break;
                yield return BossDefeated(owner, owner.name + Loc.T(" больше не хозяин арены", " no longer owns this arena"));

                if (RunState.arenaIndex >= RunState.ArenaCount - 1)
                {
                    yield return Victory();
                    yield break;
                }

                yield return Shop();
                RunState.arenaIndex++;
            }
        }

        BrainrotId PickStoryEnemy(BrainrotId owner, int wave)
        {
            if (RunState.arenaIndex == 0 && wave <= 2) return owner;
            var pool = new List<BrainrotId>();
            foreach (var info in Brainrots.Story)
            {
                if (info.id == RunState.hero) continue;
                int weight = info.id == owner ? 4 : 1;
                for (int i = 0; i < weight; i++) pool.Add(info.id);
            }
            return pool[Random.Range(0, pool.Count)];
        }

        // ===== Выживание =====

        IEnumerator SurvivalLoop()
        {
            var owner = Brainrots.Get(RunState.CurrentArenaOwner);
            BuildArena(owner);
            ui.ShowBanner(Loc.T("ВЫЖИВАНИЕ", "SURVIVAL"), Loc.T("Сколько волн ты продержишься?", "How many waves can you last?"), 3.5f, UIKit.Pink);

            for (int wave = 1; ; wave++)
            {
                WaveNumber = wave;
                yield return Break(wave == 1 ? 6f : 4f, Loc.T("Волна ", "Wave ") + wave + Loc.T(" через", " in"));

                if (wave % 10 == 0)
                {
                    // Каждая десятая волна — случайный босс из всех брейнротов
                    var bossInfo = RandomBoss();
                    int n = wave / 10;
                    float hp = 4000f * (1f + 0.5f * (n - 1)) * (1f + 0.06f * wave);
                    float dmg = 1.2f + 0.06f * wave;
                    yield return RunBoss(bossInfo, hp, dmg, Mathf.Max(4f, 8f - n), 1f + 0.08f * wave);
                    if (gameOver) yield break;
                    RunState.wavesCleared++;
                    yield return BossDefeated(bossInfo, Loc.T("Новая арена впереди!", "A new arena awaits!"));
                    yield return Shop();
                    // После босса — новая арена
                    var next = (ArenaTheme)(((int)arenaOwner.arena + 1 + Random.Range(0, 5)) % 6);
                    foreach (var s in Brainrots.Story) if (s.arena == next) owner = s;
                    BuildArena(owner);
                    ui.ShowBanner(owner.arenaName, Loc.T("Волна ", "Wave ") + (wave + 1), 3f, owner.themeColor);
                    continue;
                }

                int count = 6 + Mathf.RoundToInt(wave * 1.7f);
                float hpMult = 1f + 0.12f * (wave - 1) + 0.004f * wave * wave;
                float dmgMult = 1f + 0.05f * (wave - 1);
                float elite = wave >= 3 ? Mathf.Min(0.25f, 0.02f * wave) : 0f;
                int maxAlive = Mathf.Min(26, 10 + wave / 2);
                int w = wave;
                yield return RunWave(count, () => PickModeEnemy(w), hpMult, dmgMult, elite, maxAlive,
                    Loc.T("Волна ", "Wave ") + wave, Loc.T("Рекорд: ", "Best: ") + Meta.P.bestSurvivalWave, UIKit.Pink);
                if (gameOver) yield break;
                if (wave % 4 == 2) SpawnWeaponCrate();
                yield return WaveCleared(wave, (wave + 1) % 10 == 0 ? Loc.T("Готовься к боссу...", "Get ready for the boss...") : "");
                yield return ChooseUpgrade();
                if (wave % 5 == 0) yield return Shop();
            }
        }

        // ===== Защита =====

        IEnumerator DefenseLoop()
        {
            var owner = Brainrots.Get(RunState.CurrentArenaOwner);
            BuildArena(owner);
            Objective = DefenseObjective.Create(Vector3.zero);
            Objective.DestroyedEvent += () => StartCoroutine(Defeat(Loc.T("ПИЦЦЕРИЮ РАЗРУШИЛИ!", "THE PIZZERIA IS DESTROYED!")));
            Player.Teleport(new Vector3(0f, 0.1f, -6f), 0f);
            ui.ShowBanner(Loc.T("ЗАЩИТА ПИЦЦЕРИИ", "DEFEND THE PIZZERIA"), Loc.T("Не дай брейнротам разрушить печь Нонны!", "Don't let the brainrots wreck Nonna's oven!"), 4f, UIKit.Accent);

            for (int wave = 1; wave <= RunState.DefenseWaves; wave++)
            {
                WaveNumber = wave;
                yield return Break(wave == 1 ? 8f : 5f, Loc.T("Волна ", "Wave ") + wave + Loc.T(" через", " in"));

                int count = 8 + Mathf.RoundToInt(wave * 1.8f);
                float hpMult = 1f + 0.13f * (wave - 1);
                float dmgMult = 1f + 0.05f * (wave - 1);
                float elite = wave >= 3 ? Mathf.Min(0.2f, 0.015f * wave) : 0f;
                int w = wave;

                if (wave % 5 == 0)
                {
                    // Босс вместе с обычной волной
                    var bossInfo = RandomBoss();
                    float hp = 3500f * (1f + 0.35f * (wave / 5 - 1)) * (1f + 0.04f * wave);
                    StartCoroutine(RunWave(count / 2, () => PickModeEnemy(w), hpMult, dmgMult, elite, 12, "", "", UIKit.Pink, true));
                    yield return RunBoss(bossInfo, hp, 1.2f + 0.05f * wave, 7f, hpMult);
                    if (gameOver) yield break;
                    RunState.wavesCleared++;
                    if (wave == RunState.DefenseWaves)
                    {
                        yield return BossDefeated(bossInfo, Loc.T("Пиццерия спасена!", "The pizzeria is saved!"));
                        yield return Victory();
                        yield break;
                    }
                    yield return BossDefeated(bossInfo, Loc.T("Печь Нонны выстояла!", "Nonna's oven stands!"));
                    Objective.Repair(0.25f);
                    yield return ChooseUpgrade();
                    yield return Shop();
                    continue;
                }

                yield return RunWave(count, () => PickModeEnemy(w), hpMult, dmgMult, elite, Mathf.Min(22, 10 + wave),
                    Loc.T("Волна ", "Wave ") + wave + " / " + RunState.DefenseWaves, Loc.T("Защити пиццерию!", "Defend the pizzeria!"), UIKit.Accent);
                if (gameOver) yield break;
                Objective.Repair(0.05f);
                if (wave % 4 == 2) SpawnWeaponCrate();
                yield return WaveCleared(wave, (wave + 1) % 5 == 0 ? Loc.T("Готовься к боссу...", "Get ready for the boss...") : "");
                yield return ChooseUpgrade();
            }
        }

        /// <summary>
        /// Враги выживания и защиты: все 26 брейнротов, новые архетипы появляются постепенно
        /// (как в Brotato: сначала простые, потом снайперы, танки и призыватели).
        /// </summary>
        BrainrotId PickModeEnemy(int wave)
        {
            var pool = new List<BrainrotId>();
            foreach (var info in Brainrots.All)
            {
                if (info.id == RunState.hero) continue;
                int minWave;
                switch (info.behavior)
                {
                    case EnemyBehavior.Runner: case EnemyBehavior.Chaser: case EnemyBehavior.Dasher: minWave = 1; break;
                    case EnemyBehavior.Thrower: case EnemyBehavior.Leaper: case EnemyBehavior.Flyer: minWave = 3; break;
                    case EnemyBehavior.Exploder: case EnemyBehavior.Teleporter: minWave = 5; break;
                    default: minWave = 7; break; // танки, снайперы, призыватели
                }
                if (wave < minWave) continue;
                pool.Add(info.id);
                if (info.arena == arenaOwner.arena) pool.Add(info.id); // местные чаще
            }
            if (pool.Count == 0) pool.Add(BrainrotId.TrippiTroppi);
            return pool[Random.Range(0, pool.Count)];
        }

        BrainrotInfo RandomBoss()
        {
            var list = new List<BrainrotInfo>();
            foreach (var info in Brainrots.All) if (info.id != RunState.hero && info.behavior != EnemyBehavior.Exploder) list.Add(info);
            return list[Random.Range(0, list.Count)];
        }

        // ===== Общее =====

        void BuildArena(BrainrotInfo owner)
        {
            arena?.Destroy();
            ClearLeftovers();
            arenaOwner = owner;
            arena = new ArenaGenerator(ArenaHalfSize);
            arena.Build(owner.arena, sun, cam);
            if (sun != null) sun.shadows = GameSettings.SunShadows;
            Player.Teleport(arena.PlayerSpawn, 0f);
            Boss = null;
            Particles.Ambient(owner.arena, Player.transform);
            SoundFX.PlayAmbient(owner.arena);
            Music.NewArena(owner.arena);
            if (PostFX.Instance != null) PostFX.Instance.ApplyTheme(owner.arena);
            flashlight.enabled = owner.arena == ArenaTheme.NightVillage;
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
            Music.Break();
            while (breakTimeLeft > 0f)
            {
                breakTimeLeft -= Time.deltaTime;
                StatusTitle = label + " " + Mathf.CeilToInt(breakTimeLeft);
                StatusSubtitle = ArenaLine();
                yield return null;
            }
        }

        string ArenaLine()
        {
            string arenaName = arenaOwner != null ? (string)arenaOwner.arenaName : "";
            switch (RunState.mode)
            {
                case GameMode.Survival: return Loc.T("Выживание · ", "Survival · ") + arenaName + Loc.T(" · рекорд: волна ", " · best: wave ") + Meta.P.bestSurvivalWave;
                case GameMode.Defense: return Loc.T("Защита · ", "Defense · ") + arenaName;
                default:
                    return arenaName + Loc.T(" · арена ", " · arena ") + (RunState.arenaIndex + 1) + "/" + RunState.ArenaCount
                           + (RunState.threat > 0 ? Loc.T(" · угроза ", " · threat ") + RunState.threat : "");
            }
        }

        /// <summary>Волна: спавнит врагов из порталов, пока не кончатся.</summary>
        IEnumerator RunWave(int count, System.Func<BrainrotId> pick, float hpMult, float dmgMult, float eliteChance, int maxAlive,
            string title, string subtitle, Color color, bool silent = false)
        {
            toSpawn = count;
            if (!silent)
            {
                SoundFX.Play(SoundFX.Sfx.WaveStart, 0.8f);
                ui.ShowBanner(title.ToUpperInvariant(), subtitle, 2f, color);
                Music.WaveStart();
            }

            float spawnTimer = 0f;
            int a = RunState.mode == GameMode.Story ? RunState.arenaIndex : Mathf.Min(4, WaveNumber / 6);
            while (toSpawn > 0 || (!silent && Enemy.Active.Count > 0))
            {
                if (gameOver) yield break;
                spawnTimer -= Time.deltaTime;
                if (toSpawn > 0 && spawnTimer <= 0f && Enemy.Active.Count < maxAlive)
                {
                    var id = pick();
                    var info = Brainrots.Get(id);
                    Vector3 point = PickSpawnPoint();
                    bool elite = Random.value < eliteChance;
                    // Бегуны-«стайники» приходят по трое
                    int pack = info.behavior == EnemyBehavior.Runner && WaveNumber >= 3 && !elite ? Mathf.Min(3, toSpawn) : 1;
                    for (int i = 0; i < pack; i++)
                        Enemy.Create(id, point + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)), false, info.enemyHealth * hpMult, dmgMult, elite);
                    toSpawn -= pack;
                    spawnTimer = Mathf.Max(0.3f, 0.9f - a * 0.1f);
                }
                if (!silent)
                {
                    StatusTitle = title;
                    StatusSubtitle = Loc.T("Осталось врагов: ", "Enemies left: ") + (toSpawn + Enemy.Active.Count) + " · " + ArenaLine();
                }
                yield return null;
            }
        }

        IEnumerator WaveCleared(int wave, string subtitle)
        {
            RunState.wavesCleared++;
            ui.ShowBanner(Loc.T("ВОЛНА ", "WAVE ") + wave + Loc.T(" ПРОЙДЕНА!", " CLEARED!"), subtitle, 2f, UIKit.Green);
            MagnetizeCoins();
            SoundFX.Play(SoundFX.Sfx.WaveStart, 0.6f);
            yield return new WaitForSeconds(1.2f);
        }

        IEnumerator BossDefeated(BrainrotInfo info, string subtitle)
        {
            RunState.bossesKilled++;
            MagnetizeCoins();
            Player.Health.Heal(Player.Health.maxHealth * 0.5f);
            SoundFX.Play(SoundFX.Sfx.Victory, 0.8f);
            Music.Calm();
            ui.ShowBanner(Loc.T("БОСС ПОВЕРЖЕН!", "BOSS DEFEATED!"), subtitle, 3f, UIKit.Accent);
            yield return new WaitForSeconds(3f);
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
            if (Objective != null && pos.magnitude < 5f) pos = pos.normalized * 7f + Vector3.up * 0.5f;
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) pos = hit.position + Vector3.up * 0.5f;
            Pickup.Spawn(Pickup.Kind.WeaponCrate, pos, 1, missing[Random.Range(0, missing.Count)]);
            ui.ShowBanner("", Loc.T("На арене появился ящик с оружием!", "A weapon crate appeared in the arena!"), 2.5f);
        }

        /// <summary>Бой с боссом: заставка, потом босс с фазами ярости и подкреплением.</summary>
        IEnumerator RunBoss(BrainrotInfo bossInfo, float bossHealth, float dmgMult, float minionEvery, float minionHp)
        {
            Music.Boss();
            Vector3 spawn = PickSpawnPoint();
            Boss = Enemy.Create(bossInfo.id, spawn, true, bossHealth, dmgMult);
            yield return BossIntro(Boss, bossInfo);
            if (gameOver) yield break;

            float minionTimer = minionEvery;
            while (Boss != null && !Boss.IsDead)
            {
                if (gameOver) yield break;
                minionTimer -= Time.deltaTime;
                if (minionTimer <= 0f && Enemy.Active.Count < 8)
                {
                    minionTimer = minionEvery;
                    for (int i = 0; i < 2; i++)
                    {
                        var id = RunState.mode == GameMode.Story ? PickStoryEnemy(bossInfo.id, 5) : PickModeEnemy(WaveNumber);
                        var info = Brainrots.Get(id);
                        Enemy.Create(id, PickSpawnPoint(), false, info.enemyHealth * minionHp, 1f + 0.1f * RunState.arenaIndex);
                    }
                }
                StatusTitle = Loc.T("БИТВА С БОССОМ", "BOSS FIGHT") + (Boss.BossPhase > 0 ? Loc.T(" · ЯРОСТЬ ", " · RAGE ") + Boss.BossPhase : "");
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
        }

        /// <summary>Босс впал в ярость (фаза 2 или 3).</summary>
        public void OnBossRage(Enemy boss)
        {
            if (ui == null) return;
            ui.ShowBanner(Loc.T("БОСС В ЯРОСТИ!", "THE BOSS IS ENRAGED!"),
                Loc.T("Щит! Убей помощников, пока он не спадёт", "Shield up! Kill the minions while it lasts"), 2.5f, UIKit.Pink);
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

        /// <summary>Призвать помощников рядом с точкой (боссы и призыватели).</summary>
        public void SummonMinions(BrainrotId id, Vector3 near, int count)
        {
            int a = RunState.mode == GameMode.Story ? RunState.arenaIndex : Mathf.Min(4, WaveNumber / 6);
            for (int i = 0; i < count; i++)
            {
                if (Enemy.Active.Count >= 26) return;
                Vector3 offset = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(3f, 6f);
                Vector3 pos = near + offset;
                pos.y = 0f;
                pos.x = Mathf.Clamp(pos.x, -ArenaHalfSize + 3f, ArenaHalfSize - 3f);
                pos.z = Mathf.Clamp(pos.z, -ArenaHalfSize + 3f, ArenaHalfSize - 3f);
                var info = Brainrots.Get(id);
                Enemy.Create(id, pos, false, info.enemyHealth * (0.8f + 0.4f * a) * Meta.ThreatHealth(RunState.threat), (1f + 0.1f * a) * Meta.ThreatDamage(RunState.threat));
            }
        }

        // ---------------- Экраны ----------------

        void OpenScreen()
        {
            Time.timeScale = 0f;
            AudioListener.pause = false;
            Player.InputEnabled = false;
            PlayerController.LockCursor(false);
            Music.Duck(true);
        }

        void CloseScreen()
        {
            ui.CloseScreen();
            Time.timeScale = 1f;
            Player.InputEnabled = !CutsceneActive;
            PlayerController.LockCursor(true);
            Music.Duck(false);
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
            var counts = new Dictionary<string, int>();
            int a = RunState.mode == GameMode.Story ? RunState.arenaIndex : WaveNumber / 5;

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
                if (Objective != null)
                {
                    list.Add(new GameUI.ShopItem
                    {
                        name = Loc.T("Ремонт печи", "Repair oven"), description = Loc.T("Восстановить пиццерии\n40% прочности", "Restore 40% of\nthe pizzeria's health"), price = Price("repair", 30),
                        color = UIKit.Accent, icon = IconKind.Home, soldOut = Objective.Fraction >= 0.999f,
                        buy = () => { Objective.Repair(0.4f); Count("repair"); return true; }
                    });
                    list.Add(new GameUI.ShopItem
                    {
                        name = Loc.T("Укрепить печь", "Fortify oven"), description = Loc.T("Пиццерия получает\nна 20% меньше урона", "The pizzeria takes\n20% less damage"), price = Price("armor", 60),
                        color = UIKit.Accent, icon = IconKind.Shield,
                        buy = () => { Objective.Armor *= 0.8f; Count("armor"); return true; }
                    });
                }
                else
                {
                    list.Add(new GameUI.ShopItem
                    {
                        name = Loc.T("Здоровье +25", "Health +25"), description = Loc.T("Увеличить максимум\nздоровья на 25", "Increase max\nhealth by 25"), price = Price("hp", 40),
                        color = UIKit.Green, icon = IconKind.Heart,
                        buy = () => { RunState.stats.bonusMaxHealth += 25f; Player.ApplyStats(false); Player.Health.Heal(25f); Count("hp"); return true; }
                    });
                }
                list.Add(new GameUI.ShopItem
                {
                    name = Loc.T("Урон +15%", "Damage +15%"), description = Loc.T("Все пули и взрывы\nбьют сильнее", "All bullets and\nexplosions hit harder"), price = Price("dmg", 50),
                    color = UIKit.Pink, icon = IconKind.Sword,
                    buy = () => { RunState.stats.damageMultiplier += 0.15f; Count("dmg"); return true; }
                });
                list.Add(new GameUI.ShopItem
                {
                    name = Loc.T("Навыки −20%", "Skills −20%"), description = Loc.T("Навыки Q и E\nперезаряжаются быстрее", "Q and E skills\nrecharge faster"), price = Price("cd", 45),
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
            StartCoroutine(Defeat(null));
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

        IEnumerator Defeat(string reason)
        {
            if (gameOver) yield break;
            gameOver = true;
            Boss = null;
            Music.Stop(0.5f);
            SoundFX.Play(SoundFX.Sfx.Defeat, 1f, 0f);
            if (!string.IsNullOrEmpty(reason)) ui.ShowBanner(reason, "", 2f, UIKit.Pink);
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
            Music.Calm();
            ui.ShowBanner(Loc.T("ПОБЕДА!", "VICTORY!"), RunState.mode == GameMode.Defense ? Loc.T("Пиццерия Нонны спасена!", "Nonna's pizzeria is saved!") : Loc.T("Все брейнроты повержены!", "All brainrots defeated!"), 3f, UIKit.Accent);
            yield return new WaitForSeconds(2.5f);
            var reward = FinishRun(true);
            OpenScreen();
            ui.ShowEnd(true, reward, Retry, ToMenu);
        }

        void Retry()
        {
            if (SceneLoader.Busy) return;
            RunState.StartNew(RunState.hero, RunState.mode, RunState.threat);
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
