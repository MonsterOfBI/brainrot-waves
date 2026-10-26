using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrainrotWaves.EditorTools
{
    /// <summary>
    /// Автоматическая проверка: запускает игру, создаёт всех врагов и боссов, все арены,
    /// жмёт способности и открывает экраны. Ошибки пишутся в отчёт. Для разработчика.
    /// </summary>
    [InitializeOnLoad]
    public static class SmokeTest
    {
        const string Key = "BW_SmokeTest";
        const string ReportPath = "Logs/smoke-report.txt";
        static readonly List<string> errors = new List<string>();
        static readonly StringBuilder log = new StringBuilder();
        static double startTime = -1;
        static int step;

        static SmokeTest()
        {
            if (!SessionState.GetBool(Key, false)) return;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
        }

        [MenuItem("Brainrot Waves/Автопроверка (для разработчика)")]
        public static void Run()
        {
            SessionState.SetBool(Key, true);
            Directory.CreateDirectory("Logs");
            File.WriteAllText(ReportPath, "running\n");
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            EditorApplication.isPlaying = true;
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                if (condition.Contains("Licensing") || condition.Contains("Access token")) return;
                errors.Add(type + ": " + condition + "\n" + stackTrace);
            }
        }

        static void Note(string s)
        {
            log.AppendLine("[" + (EditorApplication.timeSinceStartup - startTime).ToString("0.0") + "] " + s);
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (startTime < 0) { startTime = EditorApplication.timeSinceStartup; step = 0; }
            double t = EditorApplication.timeSinceStartup - startTime;

            try
            {
                if (step == 0 && t > 3)
                {
                    step++;
                    var gm = GameManager.Instance;
                    Note("GameManager: " + (gm != null) + ", player: " + (PlayerController.Instance != null));
                    if (PlayerController.Instance != null) PlayerController.Instance.Health.damageTakenMultiplier = 0f;
                    Vector3 p = PlayerController.Instance.transform.position;
                    int i = 0;
                    foreach (var info in Brainrots.All)
                    {
                        var e = Enemy.Create(info.id, p + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 9f, false, 100f, 1f);
                        Note("Spawned " + info.id + " at " + e.transform.position);
                        i++;
                    }
                    var bossCheck = Enemy.Create(BrainrotId.TungTung, p + Vector3.forward * 15f, true, 500f, 1f); Note("Boss hp " + bossCheck.Health.Current + " / " + bossCheck.Health.maxHealth);
                    Enemy.Create(BrainrotId.Bombardiro, p + Vector3.back * 15f, true, 500f, 1f);
                    Enemy.Create(BrainrotId.Chimpanzini, p + Vector3.left * 15f, true, 500f, 1f);
                    Note("Enemies active: " + Enemy.Active.Count);
                }
                else if (step == 1 && t > 9)
                {
                    step++;
                    Note("Enemies before kill: " + Enemy.Active.Count + ", player hp " + PlayerController.Instance.Health.Current + ", player pos " + PlayerController.Instance.transform.position);
                    foreach (var e in new List<Enemy>(Enemy.Active))
                    {
                        var ag = e.GetComponent<UnityEngine.AI.NavMeshAgent>(); Note(" - " + e.name + " pos " + e.transform.position + " dist " + Vector3.Distance(e.transform.position, PlayerController.Instance.transform.position).ToString("0.0") + (ag != null ? " onNav " + ag.isOnNavMesh + " dest " + ag.destination + " path " + ag.pathStatus : ""));
                        e.TakeHit(99999f, true, e.transform.position);
                    }
                    Note("Coins on ground: " + Pickup.Active.Count);
                }
                else if (step == 2 && t > 16)
                {
                    step++;
                    foreach (ArenaTheme theme in System.Enum.GetValues(typeof(ArenaTheme)))
                    {
                        var gen = new ArenaGenerator(GameManager.ArenaHalfSize);
                        gen.Build(theme, null, Camera.main);
                        Particles.Ambient(theme, PlayerController.Instance.transform);
                        if (PostFX.Instance != null) PostFX.Instance.ApplyTheme(theme);
                        int renderers = gen.Root.GetComponentsInChildren<Renderer>().Length;
                        var tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
                        Note("Arena " + theme + ": renderers " + renderers + ", navmesh verts " + tri.vertices.Length);
                        gen.Destroy();
                    }
                }
                else if (step == 3 && t > 18)
                {
                    step++;
                    var gm = GameManager.Instance;
                    var activate = typeof(HeroAbility).GetMethod("Activate", BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var info in Brainrots.All)
                    {
                        gm.Ability.hero = info;
                        activate.Invoke(gm.Ability, new object[] { PlayerController.Instance });
                        Note("Ability " + info.abilityName + " ok");
                    }
                    Enemy.TimeScale = 1f;
                    gm.Weapons.Give(WeaponId.Rifle);
                    gm.Weapons.Give(WeaponId.Shotgun);
                    var fire = typeof(WeaponSystem).GetMethod("Fire", BindingFlags.NonPublic | BindingFlags.Instance);
                    fire.Invoke(gm.Weapons, null);
                    Note("Weapons: " + gm.Weapons.SlotCount + ", ammo " + gm.Weapons.Ammo);
                }
                else if (step == 4 && t > 20)
                {
                    step++;
                    var ui = GameUI.Instance;
                    bool picked = false;
                    ui.ShowUpgrades(Upgrades.PickThree(), u => { u.apply(RunState.stats); picked = true; }, () => { });
                    ui.CloseScreen();
                    foreach (var u in Upgrades.All) u.apply(new PlayerStats());
                    ui.ShowShop(() => new List<GameUI.ShopItem> { new GameUI.ShopItem { name = "x", description = "y", price = 1, color = Color.red, buy = () => true } }, () => { });
                    ui.CloseScreen();
                    ui.ShowPause(() => { }, () => { });
                    ui.CloseScreen();
                    ui.ShowEnd(true, Meta.ComputeReward(true), () => { }, () => { });
                    ui.CloseScreen();
                    ui.ShowEnd(false, null, () => { }, () => { });
                    ui.CloseScreen();
                    ui.ShowBanner("TEST", "test", 1f);
                    foreach (IconKind k in System.Enum.GetValues(typeof(IconKind))) Icons.Get(k);
                    var settings = SettingsScreen.Show(ui.GetComponentInChildren<Canvas>().GetComponent<RectTransform>(), null);
                    var selectTab = typeof(SettingsScreen).GetMethod("SelectTab", BindingFlags.NonPublic | BindingFlags.Instance);
                    var tabType = typeof(SettingsScreen).GetNestedType("Tab", BindingFlags.NonPublic);
                    foreach (var tab in System.Enum.GetValues(tabType)) selectTab.Invoke(settings, new object[] { tab });
                    Object.Destroy(settings.gameObject);
                    Loc.Set(Loc.Current == Lang.RU ? Lang.EN : Lang.RU);
                    Loc.Set(Loc.Current == Lang.RU ? Lang.EN : Lang.RU);
                    ui.ShowBossIntro(Brainrots.All[0]);
                    ui.HideBossIntro();
                    Note("UI screens ok (picked flag " + picked + ")");
                }
                else if (step == 5 && t > 22)
                {
                    step++;
                    Time.timeScale = 1f;
                    SceneManager.LoadScene("MainMenu");
                    Note("Loading MainMenu");
                }
                else if (step == 6 && t > 26)
                {
                    step++;
                    var menu = Object.FindFirstObjectByType<MainMenu>();
                    var select = typeof(MainMenu).GetMethod("SelectHero", BindingFlags.NonPublic | BindingFlags.Instance);
                    for (int i = 0; i < Brainrots.All.Length; i++) select.Invoke(menu, new object[] { i, true });
                    var showLab = typeof(MainMenu).GetMethod("ShowLab", BindingFlags.NonPublic | BindingFlags.Instance);
                    showLab.Invoke(menu, null);
                    Note("Menu heroes + lab ok");
                    select.Invoke(menu, new object[] { 0, true });
                    var play = typeof(MainMenu).GetMethod("Play", BindingFlags.NonPublic | BindingFlags.Instance);
                    play.Invoke(menu, null);
                    Note("Play pressed (hero " + RunState.hero + ", arenas " + RunState.ArenaCount + ")");
                }
                else if (step == 7 && t > 36)
                {
                    step++;
                    var gm = GameManager.Instance;
                    Note("Game loaded via loader: " + (gm != null));
                    if (gm != null)
                    {
                        PlayerController.Instance.Health.damageTakenMultiplier = 0f;
                        var bossRoutine = typeof(GameManager).GetMethod("BossIntro", BindingFlags.NonPublic | BindingFlags.Instance);
                        var b = Enemy.Create(BrainrotId.Patapim, PlayerController.Instance.transform.position + Vector3.forward * 12f, true, 500f, 1f);
                        gm.StartCoroutine((System.Collections.IEnumerator)bossRoutine.Invoke(gm, new object[] { b, Brainrots.Get(BrainrotId.Patapim) }));
                        Note("Boss intro started");
                    }
                }
                else if (step == 8 && t > 44)
                {
                    step++;
                    Note("After 14s of real wave: enemies " + Enemy.Active.Count + ", kills " + RunState.kills + ", status '" + (GameManager.Instance != null ? GameManager.Instance.StatusTitle : "-") + "'");
                    Finish();
                }
            }
            catch (System.Exception ex)
            {
                errors.Add("Smoke exception: " + ex);
                Finish();
            }
        }

        static void Finish()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ERRORS: " + errors.Count);
            foreach (var e in errors) sb.AppendLine(e);
            sb.AppendLine("---- LOG ----");
            sb.Append(log);
            File.WriteAllText(ReportPath, sb.ToString());
            SessionState.SetBool(Key, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(errors.Count > 0 ? 1 : 0);
        }
    }
}
