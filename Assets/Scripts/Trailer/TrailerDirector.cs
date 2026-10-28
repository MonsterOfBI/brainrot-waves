#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BrainrotWaves.Trailer
{
    /// <summary>
    /// Описание одного дубля для промо-ролика. Пишется редакторным TrailerCapture в Logs/Trailer/current_shot.json.
    /// </summary>
    [Serializable]
    public class TrailerShot
    {
        public string id = "shot";
        public string scene = "Game";        // Game | MainMenu | Portraits
        public int width = 1920;             // размер окна Game (ставит редактор)
        public int height = 1080;
        public int portraitSize = 1024;
        public string lang = "EN";           // EN | RU
        public int fps = 60;
        public float warmup = 1.5f;          // секунд до начала записи (кадры не пишутся)
        public float duration = 6f;          // секунд записи
        public int seed = 1;

        public string hero = "Tralalero";
        public string mode = "Story";        // Story | Survival | Defense
        public string arenaOwner = "Tralalero";
        public string weapon = "Rifle";      // Pistol | Rifle | Shotgun
        public float damage = 1.4f;          // множитель урона героя
        public float magazine = 2f;          // множитель магазина
        public bool hud = true;

        public string bannerTitle = "";
        public string bannerSubtitle = "";
        public float bannerAt = -1f;         // время от начала записи; <0 — без баннера
        public string status = "";           // большая строка вверху ("Wave 3 / 5")
        public string statusSub = "";        // {n} — врагов осталось, {arena} — строка арены
        public int waveNumber = 1;

        public string[] enemies = new string[0];
        public int initialCount = 8;
        public float spawnEvery = 0.6f;
        public int spawnPack = 2;
        public int maxAlive = 24;
        public float distMin = 10f;
        public float distMax = 22f;
        public float arc = 70f;              // разброс спавна вокруг взгляда, градусы
        public float enemyHp = 1f;           // множитель здоровья врагов
        public string[] abilities = new string[0]; // "Q@1.5", "E@3.2" — время от начала записи

        public Vector3 spawnCenter = new Vector3(999f, 0f, 0f); // 999 — вокруг игрока
        public string boss = "";             // id босса — запускается родная заставка и бой
        public bool bossDirect = false;      // босс без заставки (для пролётов камеры)
        public Vector3 bossPos = new Vector3(0f, 0f, 14f);
        public float bossHealth = 4000f;
        public float rageAt = -1f;           // время от начала записи, когда босс впадает в ярость
        public bool bossNoShoot = false;     // не стрелять по боссу (только по помощникам)
        public bool preferBoss = false;      // целиться в босса в первую очередь

        public float playerYaw = 0f;
        public Vector3 playerPos = new Vector3(999f, 0f, 0f); // 999 — не трогать
        public float strafe = 0f;            // амплитуда стрейфа, м/с
        public float forward = 0f;           // движение вперёд, м/с
        public float aimSmooth = 0.1f;
        public float keepAway = 6f;          // ближе этого бот пятится
        public float clearRadius = 4f;       // спрятать мелкий декор вокруг игрока
        public float flashlight = 7f;        // яркость фонарика на ночной арене (в игре 25)
        public bool shoot = true;

        public string camera = "fps";        // fps | orbit | dolly
        public Vector3 camFrom;
        public Vector3 camTo;
        public Vector3 lookAt;
        public bool lookAtBoss = false;
        public float orbitRadius = 9f;
        public float orbitHeight = 3f;
        public float orbitDegrees = 90f;     // на сколько градусов облететь за дубль
        public float orbitStart = 0f;
        public float camFov = 55f;
        public float killEvery = -1f;        // для пролётов: подрывать случайного врага каждые N секунд
        public float timeScale = 1f;

        public float menuEvery = 0.3f;       // меню: смена героя
        public string menuMode = "Survival";

        public string outDir = "Logs/Trailer/out";

        public static string ConfigPath => Path.Combine(Application.dataPath, "..", "Logs", "Trailer", "current_shot.json");
    }

    /// <summary>
    /// Режиссёр дубля: настраивает забег, ведёт бота-оператора и пишет кадры (JPG) с постоянной частотой
    /// через Time.captureFramerate, а звуковые события (выстрелы, смерти, навыки) — в events.csv,
    /// из которого звук собирается в монтаже. Только для редактора, в сборку не попадает.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class TrailerDirector : MonoBehaviour
    {
        static TrailerShot pending;
        TrailerShot shot;
        Lang originalLang;

        bool recording;
        int frameIndex;
        string framesDir;

        // бот
        float yaw, pitch, yawVel, pitchVel;
        Camera freeCam;
        float killTimer;

        static readonly BindingFlags NP = BindingFlags.NonPublic | BindingFlags.Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            string path = TrailerShot.ConfigPath;
            if (!File.Exists(path)) return;
            try
            {
                pending = JsonUtility.FromJson<TrailerShot>(File.ReadAllText(path));
            }
            finally
            {
                File.Delete(path); // одноразовый: обычный запуск игры не должен попасть в режим записи
            }
            if (pending == null) return;

            UnityEngine.Random.InitState(pending.seed);
            if (pending.scene == "Game")
            {
                var mode = (GameMode)Enum.Parse(typeof(GameMode), pending.mode);
                var hero = (BrainrotId)Enum.Parse(typeof(BrainrotId), pending.hero);
                var owner = (BrainrotId)Enum.Parse(typeof(BrainrotId), pending.arenaOwner);
                RunState.StartNew(hero, mode, 0);
                RunState.hero = hero; // сюжет подменяет несюжетного героя — возвращаем выбранного
                RunState.arenaOrder.Clear();
                RunState.arenaOrder.Add(owner);
                if (mode == GameMode.Story)
                    foreach (var info in Brainrots.Story)
                        if (info.id != owner && info.id != hero && RunState.arenaOrder.Count < 5) RunState.arenaOrder.Add(info.id);
                RunState.arenaIndex = 0;
                RunState.modeArena = Brainrots.Get(owner).arena;
            }
            var go = new GameObject("TrailerDirector");
            DontDestroyOnLoad(go);
            go.AddComponent<TrailerDirector>();
        }

        void Awake()
        {
            shot = pending;
            pending = null;
            originalLang = Loc.Current;
            Loc.Set(shot.lang == "RU" ? Lang.RU : Lang.EN);
            Time.captureFramerate = shot.fps;
        }

        void Start() => StartCoroutine(shot.scene == "MainMenu" ? MenuShot() : shot.scene == "Portraits" ? PortraitShot() : GameShot());

        // ================= Портреты для карточек =================

        /// <summary>
        /// Рендерит каждого брейнрота на прозрачном фоне (два прохода — на чёрном и белом — дают альфу).
        /// Идёт в Play Mode поверх открытой сцены, поэтому сцена пользователя не меняется.
        /// </summary>
        IEnumerator PortraitShot()
        {
            yield return null;
            yield return null;
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (root != gameObject) root.SetActive(false);
            foreach (var m in FindObjectsByType<Music>(FindObjectsSortMode.None)) m.gameObject.SetActive(false);

            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = GameAssets.Hex("#D9CFF2");
            RenderSettings.ambientEquatorColor = GameAssets.Hex("#9C8AC9");
            RenderSettings.ambientGroundColor = GameAssets.Hex("#4A3A7A");

            var sun = new GameObject("PortraitSun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
            var rim = new GameObject("PortraitRim").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.intensity = 0.55f;
            rim.color = GameAssets.Hex("#FFD6F5");
            rim.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

            var cam = new GameObject("PortraitCam").AddComponent<Camera>();
            cam.fieldOfView = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.enabled = false;
            int size = shot.portraitSize;
            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 8 };
            cam.targetTexture = rt;

            string dir = Path.Combine(Application.dataPath, "..", shot.outDir, "portraits");
            Directory.CreateDirectory(dir);
            var roster = new System.Text.StringBuilder("[");
            bool first = true;
            foreach (var info in Brainrots.All)
            {
                var holder = new GameObject("Holder").transform;
                holder.rotation = Quaternion.Euler(0f, 200f, 0f);
                var rig = BrainrotModels.Build(info.id, holder);
                rig.moveAmount = 0f;
                yield return null; // даём модели встать в позу
                var b = new Bounds(holder.position, Vector3.zero);
                foreach (var r in holder.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                float radius = Mathf.Max(b.extents.y, Mathf.Max(b.extents.x, b.extents.z));
                float dist = radius / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.08f + b.extents.z;
                cam.transform.position = b.center + new Vector3(0f, radius * 0.12f, -dist);
                cam.transform.LookAt(b.center);

                var black = RenderOn(cam, rt, Color.black);
                var white = RenderOn(cam, rt, Color.white);
                var outTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pb = black.GetPixels();
                var pw = white.GetPixels();
                var po = new Color[pb.Length];
                for (int i = 0; i < pb.Length; i++)
                {
                    float a = 1f - ((pw[i].r - pb[i].r) + (pw[i].g - pb[i].g) + (pw[i].b - pb[i].b)) / 3f;
                    a = Mathf.Clamp01(a);
                    po[i] = a > 0.004f ? new Color(pb[i].r / a, pb[i].g / a, pb[i].b / a, a) : new Color(0f, 0f, 0f, 0f);
                }
                outTex.SetPixels(po);
                outTex.Apply();
                File.WriteAllBytes(Path.Combine(dir, info.id + ".png"), outTex.EncodeToPNG());
                Destroy(black);
                Destroy(white);
                Destroy(outTex);
                Destroy(holder.gameObject);

                roster.Append(first ? "" : ",").Append("\n  {\"id\":\"").Append(info.id)
                    .Append("\",\"en\":\"").Append(info.name.en).Append("\",\"ru\":\"").Append(info.name.ru)
                    .Append("\",\"color\":\"#").Append(ColorUtility.ToHtmlStringRGB(info.themeColor))
                    .Append("\",\"story\":").Append(info.story ? "true" : "false").Append("}");
                first = false;
            }
            roster.Append("\n]\n");
            File.WriteAllText(Path.Combine(dir, "roster.json"), roster.ToString());
            File.WriteAllText(Path.Combine(dir, "done.json"), "{\"count\":" + Brainrots.All.Length + "}");
            Time.captureFramerate = 0;
            Loc.Set(originalLang);
            UnityEditor.EditorApplication.isPlaying = false;
        }

        static Texture2D RenderOn(Camera cam, RenderTexture rt, Color bg)
        {
            cam.backgroundColor = bg;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            return tex;
        }

        // ================= Игра =================

        IEnumerator GameShot()
        {
            GameManager gm = null;
            while (gm == null || gm.WaveNumber < 1 || PlayerController.Instance == null)
            {
                gm = GameManager.Instance;
                yield return null;
            }
            // Останавливаем родной ход забега: дальше волнами управляет режиссёр
            gm.StopAllCoroutines();
            var player = PlayerController.Instance;
            player.InputEnabled = false;
            SetProp(gm, "WaveNumber", shot.waveNumber);
            MuteMusic();

            var stats = RunState.stats;
            stats.damageMultiplier = shot.damage;
            stats.magazineMultiplier = shot.magazine;
            stats.coinMagnetRadius = 9f;
            var weapon = (WeaponId)Enum.Parse(typeof(WeaponId), shot.weapon);
            if (weapon != WeaponId.Pistol) gm.Weapons.Give(weapon, true);

            if (shot.playerPos.x < 900f) player.Teleport(shot.playerPos, shot.playerYaw);
            else player.Teleport(player.transform.position, shot.playerYaw);
            yaw = shot.playerYaw;
            pitch = 0f;
            ClearDecorNear(gm, player.transform.position, shot.clearRadius);
            // ночью фонарик пересвечивает оружие и врагов вблизи — для записи делаем мягче
            var flash = typeof(GameManager).GetField("flashlight", NP)?.GetValue(gm) as Light;
            if (flash != null) flash.intensity = shot.flashlight;

            if (!shot.hud) SetHud(false);
            if (shot.camera != "fps") SetupFreeCamera(player);

            // первая партия врагов
            for (int i = 0; i < shot.initialCount; i++) SpawnEnemy(player, i);

            Enemy boss = null;
            if (!string.IsNullOrEmpty(shot.boss))
            {
                var info = Brainrots.Get((BrainrotId)Enum.Parse(typeof(BrainrotId), shot.boss));
                if (shot.bossDirect)
                {
                    boss = Enemy.Create(info.id, shot.bossPos, true, shot.bossHealth, 1f);
                }
                else
                {
                    var run = typeof(GameManager).GetMethod("RunBoss", NP);
                    gm.StartCoroutine((IEnumerator)run.Invoke(gm, new object[] { info, shot.bossHealth, 1f, 3f, 1f }));
                }
            }

            float spawnTimer = 0f;
            float total = shot.warmup + shot.duration;
            float t = 0f;
            var abilityTimes = ParseAbilities();
            bool rageDone = false, bannerDone = false, wasCutscene = false;
            int lastPhase = 0;
            Time.timeScale = shot.timeScale;

            while (t < total)
            {
                float dt = Time.deltaTime;
                t += dt;
                float rt = t - shot.warmup; // время записи
                if (!recording && rt >= 0f) BeginRecording();

                player.Health.damageTakenMultiplier = 0f;
                player.InputEnabled = false;
                if (boss == null && gm.Boss != null) boss = gm.Boss;

                // баннер
                if (!bannerDone && shot.bannerAt >= 0f && rt >= shot.bannerAt)
                {
                    bannerDone = true;
                    LogEvent("banner");
                    GameUI.Instance?.ShowBanner(shot.bannerTitle, shot.bannerSubtitle, 2.2f, Brainrots.Get(RunState.CurrentArenaOwner).themeColor);
                }

                // строки статуса как в игре
                if (!string.IsNullOrEmpty(shot.status) && (boss == null || boss.IsDead))
                {
                    SetProp(gm, "StatusTitle", shot.status);
                    string arenaLine = (string)typeof(GameManager).GetMethod("ArenaLine", NP).Invoke(gm, null);
                    SetProp(gm, "StatusSubtitle", shot.statusSub.Replace("{n}", (Enemy.Active.Count + 6).ToString()).Replace("{arena}", arenaLine));
                }

                // подкрепления
                spawnTimer -= dt;
                if (spawnTimer <= 0f && shot.enemies.Length > 0 && Enemy.Active.Count < shot.maxAlive)
                {
                    spawnTimer = shot.spawnEvery;
                    for (int i = 0; i < shot.spawnPack; i++) SpawnEnemy(player, UnityEngine.Random.Range(0, 1000));
                }

                // ярость босса
                if (!rageDone && boss != null && shot.rageAt >= 0f && rt >= shot.rageAt && !gm.CutsceneActive)
                {
                    rageDone = true;
                    float target = boss.Health.maxHealth * 0.6f;
                    if (boss.Health.Current > target) boss.TakeHit(boss.Health.Current - target, true, boss.transform.position + Vector3.up * 2f);
                    LogEvent("rage");
                }
                if (boss != null && boss.BossPhase > lastPhase) { lastPhase = boss.BossPhase; if (!rageDone) LogEvent("rage"); }
                if (gm.CutsceneActive && !wasCutscene) LogEvent("boss_intro");
                wasCutscene = gm.CutsceneActive;

                // навыки
                foreach (var a in abilityTimes)
                {
                    if (!a.done && rt >= a.time && !gm.CutsceneActive)
                    {
                        a.done = true;
                        gm.Ability.ActivateSlot(player, a.slot);
                        LogEvent("ability_" + gm.Ability.Def(a.slot).type);
                    }
                }

                if (!gm.CutsceneActive)
                {
                    if (shot.camera == "fps") BotFps(player, gm, boss, dt, t);
                    else UpdateFreeCamera(player, boss, rt, dt);
                }

                if (shot.killEvery > 0f)
                {
                    killTimer -= dt;
                    if (killTimer <= 0f && Enemy.Active.Count > 0)
                    {
                        killTimer = shot.killEvery;
                        var e = Enemy.Active[UnityEngine.Random.Range(0, Enemy.Active.Count)];
                        if (e != null && !e.IsBoss && !e.IsDead) e.TakeHit(99999f, true, e.transform.position + Vector3.up);
                    }
                }

                MuteMusic();
                yield return null;
            }
            yield return Finish();
        }

        void SpawnEnemy(PlayerController player, int salt)
        {
            if (shot.enemies.Length == 0) return;
            var id = (BrainrotId)Enum.Parse(typeof(BrainrotId), shot.enemies[UnityEngine.Random.Range(0, shot.enemies.Length)]);
            var info = Brainrots.Get(id);
            float ang = shot.playerYaw + UnityEngine.Random.Range(-shot.arc * 0.5f, shot.arc * 0.5f);
            float dist = UnityEngine.Random.Range(shot.distMin, shot.distMax);
            Vector3 c = shot.spawnCenter.x < 900f ? shot.spawnCenter : player.transform.position;
            Vector3 p = c + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * dist;
            float lim = GameManager.ArenaHalfSize - 3f;
            p.x = Mathf.Clamp(p.x, -lim, lim);
            p.z = Mathf.Clamp(p.z, -lim, lim);
            p.y = 0f;
            Enemy.Create(id, p, false, info.enemyHealth * shot.enemyHp, 1f, UnityEngine.Random.value < 0.08f);
        }

        // ---------- бот от первого лица ----------

        void BotFps(PlayerController player, GameManager gm, Enemy boss, float dt, float t)
        {
            var cam = player.cam;
            Enemy target = PickTarget(cam, boss);
            if (target != null)
            {
                Vector3 aim = target.transform.position + Vector3.up * 0.9f * target.Size;
                Vector3 d = aim - cam.transform.position;
                float wantYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                float wantPitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
                // лёгкое «дыхание», чтобы прицел не был роботом
                wantYaw += Mathf.Sin(t * 1.7f) * 0.8f;
                wantPitch += Mathf.Sin(t * 2.3f) * 0.5f;
                yaw = Mathf.SmoothDampAngle(yaw, wantYaw, ref yawVel, shot.aimSmooth);
                pitch = Mathf.SmoothDampAngle(pitch, Mathf.Clamp(wantPitch, -60f, 60f), ref pitchVel, shot.aimSmooth);
                float err = Vector3.Angle(cam.transform.forward, d);
                float tol = gm.Weapons.CurrentDef.id == WeaponId.Shotgun ? 9f : 5f;
                if (shot.shoot && err < tol && !(shot.bossNoShoot && target.IsBoss)) Shoot(gm.Weapons);
            }
            player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            typeof(PlayerController).GetField("pitch", NP).SetValue(player, pitch);

            Vector3 move = player.transform.right * Mathf.Sin(t * 0.9f) * shot.strafe + player.transform.forward * shot.forward;
            // пятимся, если враги подошли вплотную — чтобы кадр не забивался мордами
            float nearest = float.MaxValue;
            foreach (var e in Enemy.Active)
                if (e != null && !e.IsDead) nearest = Mathf.Min(nearest, Vector3.Distance(e.transform.position, player.transform.position));
            if (nearest < shot.keepAway) move -= player.transform.forward * 4.5f * (1f - nearest / shot.keepAway + 0.3f);
            if (move.sqrMagnitude > 0.01f)
            {
                player.GetComponent<CharacterController>().Move(move * dt);
                SetProp(player, "MoveFraction", Mathf.Clamp01(move.magnitude / 6f) * 0.8f);
            }
        }

        Enemy PickTarget(Camera cam, Enemy boss)
        {
            Enemy best = null;
            float bestScore = float.MaxValue;
            foreach (var e in Enemy.Active)
            {
                if (e == null || e.IsDead) continue;
                if (shot.bossNoShoot && e.IsBoss) continue;
                Vector3 d = e.transform.position - cam.transform.position;
                float dist = d.magnitude;
                if (dist > 45f) continue;
                float ang = Vector3.Angle(cam.transform.forward, d);
                float score = dist + ang * 0.35f - (shot.preferBoss && e.IsBoss ? 40f : 0f);
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        void Shoot(WeaponSystem ws)
        {
            if (ws.IsSwitching) return;
            var t = typeof(WeaponSystem);
            if ((bool)t.GetField("reloading", NP).GetValue(ws)) return;
            if (ws.Ammo <= 0)
            {
                t.GetMethod("StartReload", NP).Invoke(ws, null);
                LogEvent("reload");
                return;
            }
            if (Time.time >= (float)t.GetField("nextFireTime", NP).GetValue(ws))
            {
                t.GetMethod("Fire", NP).Invoke(ws, null);
                LogEvent("shot_" + ws.CurrentDef.id);
            }
        }

        // ---------- журнал звуковых событий (для сборки звука в монтаже) ----------

        readonly System.Text.StringBuilder events = new System.Text.StringBuilder("frame,event\n");

        void LogEvent(string name)
        {
            if (recording) events.Append(frameIndex).Append(',').Append(name).Append('\n');
        }

        void OnEnable() => Enemy.AnyDied += OnAnyDied;
        void OnDisable() => Enemy.AnyDied -= OnAnyDied;
        void OnAnyDied(Enemy e) => LogEvent(e != null && e.IsBoss ? "boss_death" : "death");

        // ---------- свободная камера ----------

        void SetupFreeCamera(PlayerController player)
        {
            var go = new GameObject("TrailerCamera");
            freeCam = go.AddComponent<Camera>();
            freeCam.fieldOfView = shot.camFov;
            freeCam.nearClipPlane = 0.05f;
            freeCam.farClipPlane = 400f;
            freeCam.clearFlags = player.cam.clearFlags;
            freeCam.backgroundColor = player.cam.backgroundColor;
            GameSettings.SetupCamera(freeCam);
            player.cam.enabled = false;
            killTimer = shot.killEvery;
        }

        void UpdateFreeCamera(PlayerController player, Enemy boss, float rt, float dt)
        {
            if (freeCam == null) return;
            if (!freeCam.enabled) freeCam.enabled = true;
            if (player.cam.enabled) player.cam.enabled = false;
            float k = Mathf.Clamp01(rt / Mathf.Max(0.01f, shot.duration));
            float s = k * k * (3f - 2f * k);
            Vector3 focus = shot.lookAt;
            if (shot.lookAtBoss && boss != null) focus = boss.transform.position + Vector3.up * 1.3f * boss.Size;
            if (shot.camera == "orbit")
            {
                float a = (shot.orbitStart + shot.orbitDegrees * Mathf.Max(0f, rt) / shot.duration) * Mathf.Deg2Rad;
                Vector3 c = shot.lookAtBoss && boss != null ? boss.transform.position : shot.lookAt;
                freeCam.transform.position = c + new Vector3(Mathf.Sin(a) * shot.orbitRadius, shot.orbitHeight, Mathf.Cos(a) * shot.orbitRadius);
            }
            else
            {
                freeCam.transform.position = Vector3.Lerp(shot.camFrom, shot.camTo, Mathf.Max(0f, s));
            }
            freeCam.transform.rotation = Quaternion.LookRotation(focus - freeCam.transform.position);
        }

        // ================= Меню =================

        IEnumerator MenuShot()
        {
            MainMenu menu = null;
            while (menu == null)
            {
                menu = FindFirstObjectByType<MainMenu>();
                yield return null;
            }
            yield return null;
            var mode = (GameMode)Enum.Parse(typeof(GameMode), shot.menuMode);
            typeof(MainMenu).GetMethod("SetMode", NP).Invoke(menu, new object[] { mode });
            var select = typeof(MainMenu).GetMethod("SelectHero", NP);
            var list = (BrainrotInfo[])typeof(MainMenu).GetField("list", NP).GetValue(menu);
            int index = 0;
            float t = 0f, next = 0f;
            float total = shot.warmup + shot.duration;
            while (t < total)
            {
                t += Time.deltaTime;
                if (!recording && t >= shot.warmup) BeginRecording();
                if (t >= next)
                {
                    next = t + shot.menuEvery;
                    select.Invoke(menu, new object[] { index % list.Length, true });
                    LogEvent("click");
                    index++;
                }
                MuteMusic();
                yield return null;
            }
            yield return Finish();
        }

        // ================= Запись =================

        void BeginRecording()
        {
            recording = true;
            frameIndex = 0;
            framesDir = Path.Combine(Application.dataPath, "..", shot.outDir, shot.id);
            if (Directory.Exists(framesDir)) Directory.Delete(framesDir, true);
            Directory.CreateDirectory(framesDir);
            StartCoroutine(CaptureLoop());
        }

        IEnumerator CaptureLoop()
        {
            var eof = new WaitForEndOfFrame();
            while (recording)
            {
                yield return eof;
                if (!recording) break;
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(framesDir, "f" + frameIndex.ToString("00000") + ".jpg"), tex.EncodeToJPG(92));
                if (frameIndex == 0) File.WriteAllText(Path.Combine(framesDir, "size.txt"), tex.width + "x" + tex.height);
                Destroy(tex);
                frameIndex++;
            }
        }

        IEnumerator Finish()
        {
            yield return new WaitForEndOfFrame();
            yield return null; // даём последнему кадру записаться
            recording = false;
            if (framesDir != null)
            {
                File.WriteAllText(Path.Combine(framesDir, "events.csv"), events.ToString());
                File.WriteAllText(Path.Combine(framesDir, "done.json"), "{\"frames\":" + frameIndex + ",\"fps\":" + shot.fps + "}");
            }
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            Loc.Set(originalLang);
            UnityEditor.EditorApplication.isPlaying = false;
        }

        // ================= Вспомогательное =================

        class AbilityAt { public int slot; public float time; public bool done; }

        List<AbilityAt> ParseAbilities()
        {
            var list = new List<AbilityAt>();
            foreach (var s in shot.abilities)
            {
                var parts = s.Split('@');
                if (parts.Length != 2) continue;
                list.Add(new AbilityAt { slot = parts[0].Trim().ToUpperInvariant() == "E" ? 1 : 0, time = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) });
            }
            return list;
        }

        static void SetProp(object o, string name, object value)
        {
            var p = o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            p?.GetSetMethod(true)?.Invoke(o, new[] { value });
        }

        /// <summary>Прячет мелкий декор рядом с камерой (доски, зонтики), чтобы он не лез в кадр.</summary>
        static void ClearDecorNear(GameManager gm, Vector3 center, float radius)
        {
            if (radius <= 0f) return;
            var arena = typeof(GameManager).GetField("arena", NP)?.GetValue(gm);
            if (arena == null) return;
            var rootObj = arena.GetType().GetProperty("Root")?.GetValue(arena);
            Transform root = rootObj is Component c ? c.transform : (rootObj as GameObject)?.transform;
            if (root == null) return;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                var b = r.bounds;
                if (b.size.magnitude > 15f) continue; // земля, стены
                if (b.SqrDistance(center) < radius * radius)
                {
                    r.enabled = false;
                    foreach (var col in r.GetComponents<Collider>()) col.enabled = false;
                }
            }
        }

        static void SetHud(bool on)
        {
            if (GameUI.Instance == null) return;
            foreach (var c in GameUI.Instance.GetComponentsInChildren<Canvas>(true)) c.enabled = on;
        }

        static void MuteMusic()
        {
            foreach (var m in FindObjectsByType<Music>(FindObjectsSortMode.None))
                foreach (var s in m.GetComponents<AudioSource>()) s.mute = true;
        }
    }
}
#endif
