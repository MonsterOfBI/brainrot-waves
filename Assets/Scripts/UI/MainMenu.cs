using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BrainrotWaves
{
    /// <summary>
    /// Главное меню: выбор героя с 3D-превью, открытие героев за кристаллы,
    /// уровень угрозы, Лаборатория (постоянные улучшения), настройки, язык.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        int selected;
        Transform stage;
        Transform turntable;
        BrainrotRig currentModel;
        float modelPop;
        RectTransform canvas;
        RectTransform overlay;
        Text crystalsText, threatText, threatInfo;
        Image heroColorBar;
        Button playButton;
        Text playLabel;
        readonly List<Image> heroButtons = new List<Image>();
        readonly List<Image> heroLocks = new List<Image>();
        float spin;
        Camera cam;

        void Start()
        {
            Time.timeScale = 1f;
            GameSettings.EnsureLoaded();
            PlayerController.LockCursor(false);
            mode = (GameMode)Mathf.Clamp(Meta.P.lastMode, 0, 2);
            list = Brainrots.ForMode(mode);
            selected = 0;
            for (int i = 0; i < list.Length; i++) if ((int)list[i].id == Meta.P.lastHero) selected = i;
            BuildStage();
            BuildUI();
            SelectHero(selected);
            Music.Menu();
            SoundFX.PlayAmbient(null);
            Loc.Changed += RebuildUI;
        }

        void OnDestroy() => Loc.Changed -= RebuildUI;

        void BuildStage()
        {
            stage = new GameObject("Stage").transform;

            var camObject = new GameObject("MenuCamera");
            camObject.tag = "MainCamera";
            cam = camObject.AddComponent<Camera>();
            cam.transform.position = new Vector3(-1.4f, 2.1f, -5.6f);
            cam.transform.rotation = Quaternion.Euler(10f, 8f, 0f);
            cam.fieldOfView = 45f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameAssets.Hex("#2A1745");
            camObject.AddComponent<AudioListener>();
            GameSettings.SetupCamera(cam);
            PostFX.Create().ApplyMenu();

            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.color = GameAssets.Hex("#FFF2E0");
            sun.shadows = GameSettings.SunShadows;
            sunObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = GameAssets.Hex("#B39DDB");
            RenderSettings.ambientEquatorColor = GameAssets.Hex("#7E57C2");
            RenderSettings.ambientGroundColor = GameAssets.Hex("#311B92");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = GameAssets.Hex("#2A1745");
            RenderSettings.fogDensity = 0.04f;

            // Подиум
            BrainrotModels.Part(stage, PrimitiveType.Cylinder, new Vector3(0f, -0.15f, 0f), new Vector3(3.6f, 0.15f, 3.6f), GameAssets.Hex("#FFC93C"), default, 0.6f);
            BrainrotModels.Part(stage, PrimitiveType.Cylinder, new Vector3(0f, -0.05f, 0f), new Vector3(3.2f, 0.06f, 3.2f), GameAssets.Hex("#FF5FA2"), default, 0.6f);
            BrainrotModels.Part(stage, PrimitiveType.Cylinder, new Vector3(0f, -0.4f, 0f), new Vector3(40f, 0.1f, 40f), GameAssets.Hex("#3B2063"), default, 0.2f);

            // Светящиеся огоньки по краю подиума
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f;
                var bulb = BrainrotModels.Part(stage, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 1.75f, -0.02f, Mathf.Sin(a) * 1.75f), Vector3.one * 0.12f, Color.white, default, 0.5f, false);
                bulb.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(i % 2 == 0 ? GameAssets.Hex("#FFC93C") : GameAssets.Hex("#3DE0FF"), 3f);
            }

            // Конфетти-кубики на фоне
            var rng = new System.Random(7);
            Color[] colors = { GameAssets.Hex("#FFC93C"), GameAssets.Hex("#FF5FA2"), GameAssets.Hex("#3DE0FF"), GameAssets.Hex("#5CFF8A") };
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector3((float)rng.NextDouble() * 24f - 12f, (float)rng.NextDouble() * 8f - 1f, 4f + (float)rng.NextDouble() * 10f);
                var c = BrainrotModels.Part(stage, PrimitiveType.Cube, p, Vector3.one * (0.2f + (float)rng.NextDouble() * 0.5f), colors[i % colors.Length], new Vector3(i * 37f, i * 53f, 0f), 0.5f, false);
                c.gameObject.AddComponent<Floater>();
            }

            var spot = new GameObject("Spot");
            spot.transform.position = new Vector3(0f, 5f, -2f);
            spot.transform.rotation = Quaternion.Euler(70f, 0f, 0f);
            var l = spot.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 12f;
            l.spotAngle = 50f;
            l.intensity = 20f;
            l.color = Color.white;

            turntable = new GameObject("Turntable").transform;
            turntable.SetParent(stage, false);

            Particles.Ambient(ArenaTheme.NightVillage, stage);
        }

        // ---------------- Интерфейс ----------------

        GameMode mode;
        BrainrotInfo[] list;
        readonly Button[] modeButtons = new Button[3];
        RectTransform infoPanel;
        RectTransform heroDetails;

        void RebuildUI()
        {
            bool settingsOpen = SettingsScreen.IsOpen;
            if (canvas != null) Destroy(canvas.gameObject);
            heroButtons.Clear();
            heroLocks.Clear();
            BuildUI();
            SelectHero(selected, false);
            if (settingsOpen) SettingsScreen.Show(overlay, null);
        }

        static LS ModeTitle(GameMode m)
        {
            switch (m)
            {
                case GameMode.Survival: return new LS("Выживание", "Survival");
                case GameMode.Defense: return new LS("Защита", "Defense");
                default: return new LS("Сюжет", "Story");
            }
        }

        static IconKind ModeIcon(GameMode m) => m == GameMode.Survival ? IconKind.Infinity : m == GameMode.Defense ? IconKind.Home : IconKind.Book;

        static string ModeDescription(GameMode m)
        {
            switch (m)
            {
                case GameMode.Survival: return Loc.T("Бесконечные волны всех 26 брейнротов. Босс каждые 10 волн. Работает Лаборатория.", "Endless waves of all 26 brainrots. A boss every 10 waves. The Lab works here.");
                case GameMode.Defense: return Loc.T("Защити пиццерию Нонны 15 волн. Брейнроты идут к печи. Работает Лаборатория.", "Defend Nonna's pizzeria for 15 waves. Brainrots go for the oven. The Lab works here.");
                default: return Loc.T("5 арен, 5 боссов-хозяев. Только сила героя и удача — Лаборатория не помогает.", "5 arenas, 5 boss owners. Just your hero and luck — the Lab doesn't help here.");
            }
        }

        void BuildUI()
        {
            UIKit.EnsureEventSystem();
            canvas = UIKit.Canvas("MenuCanvas", 10);
            var root = canvas;
            list = Brainrots.ForMode(mode);

            var title = UIKit.Text(root, "Title", "BRAINROT WAVES", 84, TextAnchor.UpperLeft, UIKit.Accent, true);
            UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -44f), new Vector2(900f, 110f));
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(5f, -5f);
            title.gameObject.AddComponent<Wobble>().angle = 1f;
            var subtitle = UIKit.Text(root, "Subtitle", new LS("Шутер с волнами итальянских брейнротов", "A wave shooter with Italian brainrots"), 26, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.8f));
            UIKit.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(76f, -138f), new Vector2(1000f, 40f));
            SlideIn.Add(subtitle, new Vector2(-300f, 0f), 0.1f);

            // Кристаллы и кнопки сверху справа
            var crystalPanel = UIKit.Image(root, "Crystals", UIKit.Panel);
            UIKit.Place(crystalPanel.rectTransform, new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(220f, 64f), new Vector2(1f, 1f));
            UIKit.Icon(crystalPanel.rectTransform, "Icon", IconKind.Crystal, UIKit.Crystal, new Vector2(0f, 0.5f), new Vector2(14f, 0f), 42f, new Vector2(0f, 0.5f)).gameObject.AddComponent<Pulse>();
            crystalsText = UIKit.Text(crystalPanel.rectTransform, "Value", "", 32, TextAnchor.MiddleRight, UIKit.Crystal, true);
            UIKit.Place(crystalsText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(150f, 56f), new Vector2(1f, 0.5f));
            SlideIn.Add(crystalPanel, new Vector2(0f, 120f), 0.1f);

            var lab = UIKit.IconButton(root, "Lab", IconKind.Flask, new LS("Лаборатория", "Lab"), GameAssets.Hex("#00796B"), ShowLab, 24);
            UIKit.Place(lab.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-296f, -40f), new Vector2(260f, 64f), new Vector2(1f, 1f));
            SlideIn.Add(lab, new Vector2(0f, 120f), 0.15f);
            var settings = UIKit.IconButton(root, "Settings", IconKind.Gear, new LS("Настройки", "Settings"), new Color(0.2f, 0.35f, 0.7f, 1f), () => SettingsScreen.Show(overlay, null), 24);
            UIKit.Place(settings.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-572f, -40f), new Vector2(250f, 64f), new Vector2(1f, 1f));
            SlideIn.Add(settings, new Vector2(0f, 120f), 0.2f);
            var lang = UIKit.IconButton(root, "Lang", IconKind.Globe, new LS("RU", "EN"), UIKit.Purple, Loc.Toggle, 24);
            UIKit.Place(lang.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-838f, -40f), new Vector2(140f, 64f), new Vector2(1f, 1f));
            SlideIn.Add(lang, new Vector2(0f, 120f), 0.25f);

            // Выбор режима
            GameMode[] modes = { GameMode.Story, GameMode.Survival, GameMode.Defense };
            for (int i = 0; i < 3; i++)
            {
                var m = modes[i];
                var b = UIKit.IconButton(root, "Mode" + i, ModeIcon(m), ModeTitle(m), m == mode ? UIKit.PlayColor : UIKit.PanelLight, () => SetMode(m), 19);
                UIKit.Place(b.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(70f + i * 176f, -196f), new Vector2(168f, 60f), new Vector2(0f, 1f));
                modeButtons[i] = b;
                SlideIn.Add(b, new Vector2(-400f, 0f), 0.05f + i * 0.05f, 0.35f);
            }
            var modeDesc = UIKit.Text(root, "ModeDesc", ModeDescription(mode), 19, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.7f));
            UIKit.Place(modeDesc.rectTransform, new Vector2(0f, 1f), new Vector2(74f, -266f), new Vector2(520f, 60f));

            // Герои режима
            bool many = list.Length > 8;
            int columns = many ? 3 : 1;
            float w = many ? 170f : 520f, h = many ? 56f : 72f, gap = 6f;
            for (int i = 0; i < list.Length; i++)
            {
                int index = i;
                var info = list[i];
                int col = i % columns, row = i / columns;
                var b = UIKit.Button(root, "Hero" + i, many ? (string)info.shortName : (string)info.name, UIKit.Panel, () => SelectHero(index), many ? 17 : 26);
                var rect = b.GetComponent<RectTransform>();
                UIKit.Place(rect, new Vector2(0f, 1f), new Vector2(70f + col * (w + gap), -336f - row * (h + gap)), new Vector2(w, h));
                var label = rect.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(many ? 20f : 34f, 0f);
                label.rectTransform.offsetMax = new Vector2(many ? -26f : -50f, 0f);
                var swatch = UIKit.Image(rect, "Color", info.themeColor);
                UIKit.Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(many ? 7f : 12f, h - 16f), new Vector2(0f, 0.5f));
                var lockIcon = UIKit.Icon(rect, "Lock", IconKind.Lock, new Color(1f, 1f, 1f, 0.75f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), many ? 22f : 34f, new Vector2(1f, 0.5f));
                heroLocks.Add(lockIcon);
                heroButtons.Add(b.GetComponent<Image>());
                SlideIn.Add(b, new Vector2(-500f, 0f), 0.08f + i * 0.015f, 0.35f);
            }

            // Описание справа
            var panel = UIKit.Image(root, "InfoPanel", UIKit.Panel);
            UIKit.Place(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-60f, 0f), new Vector2(660f, 800f), new Vector2(1f, 0.5f));
            SlideIn.Add(panel, new Vector2(700f, 0f), 0.15f, 0.5f);
            infoPanel = panel.rectTransform;
            heroColorBar = UIKit.Image(infoPanel, "Bar", Color.white);
            UIKit.Place(heroColorBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(630f, 8f), new Vector2(0.5f, 1f));

            playButton = UIKit.Button(infoPanel, "Play", "", UIKit.PlayColor, OnPlayPressed, 40);
            UIKit.Place(playButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(610f, 88f), new Vector2(0.5f, 0f));
            playLabel = playButton.GetComponentInChildren<Text>();
            playButton.gameObject.AddComponent<Pulse>().amount = 0.02f;

            // Уровень угрозы (только сюжет)
            if (mode == GameMode.Story)
            {
                var threatRow = UIKit.Rect(infoPanel, "Threat");
                UIKit.Place(threatRow, new Vector2(0.5f, 0f), new Vector2(0f, 126f), new Vector2(610f, 84f), new Vector2(0.5f, 0f));
                var minus = UIKit.Button(threatRow, "Minus", "<", UIKit.PanelLight, () => ChangeThreat(-1), 26);
                UIKit.Place(minus.GetComponent<RectTransform>(), new Vector2(0f, 1f), Vector2.zero, new Vector2(56f, 46f), new Vector2(0f, 1f));
                var plus = UIKit.Button(threatRow, "Plus", ">", UIKit.PanelLight, () => ChangeThreat(1), 26);
                UIKit.Place(plus.GetComponent<RectTransform>(), new Vector2(1f, 1f), Vector2.zero, new Vector2(56f, 46f), new Vector2(1f, 1f));
                threatText = UIKit.Text(threatRow, "Value", "", 26, TextAnchor.MiddleCenter, UIKit.Pink, true);
                UIKit.Place(threatText.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(460f, 46f), new Vector2(0.5f, 1f));
                threatInfo = UIKit.Text(threatRow, "Info", "", 17, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.65f));
                UIKit.Place(threatInfo.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(610f, 34f), new Vector2(0.5f, 0f));
                UpdateThreat();
            }
            else threatText = threatInfo = null;

            var recordText = UIKit.Text(root, "Record", "", 20, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.6f));
            UIKit.Place(recordText.rectTransform, new Vector2(0f, 0f), new Vector2(76f, 26f), new Vector2(1100f, 60f));
            var p = Meta.P;
            string rec;
            if (mode == GameMode.Survival) rec = Loc.T("Рекорд выживания: волна ", "Survival record: wave ") + p.bestSurvivalWave;
            else if (mode == GameMode.Defense) rec = Loc.T("Рекорд защиты: волна ", "Defense record: wave ") + p.bestDefenseWave + Loc.T(" · побед: ", " · wins: ") + p.defenseWins;
            else rec = Loc.T("Лучший результат: ", "Best result: ") + p.bestArena + Loc.T(" из 5 арен · Побед: ", " of 5 arenas · Wins: ") + p.wins;
            recordText.text = rec + Loc.T(" · Боссов побеждено: ", " · Bosses defeated: ") + p.bossesTotal + "\n" + Loc.T("Стрелки — выбор героя, Enter — играть", "Arrows — choose hero, Enter — play");

            var quit = UIKit.IconButton(root, "Quit", IconKind.Exit, new LS("Выход", "Quit"), UIKit.PanelLight, Quit, 22);
            UIKit.Place(quit.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-60f, 24f), new Vector2(180f, 54f), new Vector2(1f, 0f));
            var credits = UIKit.IconButton(root, "Credits", IconKind.Star, new LS("Авторы", "Credits"), UIKit.PanelLight, ShowCredits, 22);
            UIKit.Place(credits.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-250f, 24f), new Vector2(190f, 54f), new Vector2(1f, 0f));

            overlay = UIKit.Rect(root, "Overlay");
            UIKit.Stretch(overlay);
            UpdateCrystals();
        }

        void SetMode(GameMode m)
        {
            if (m == mode) return;
            var prev = list[Mathf.Clamp(selected, 0, list.Length - 1)].id;
            mode = m;
            Meta.P.lastMode = (int)m;
            Meta.Save();
            list = Brainrots.ForMode(mode);
            selected = 0;
            for (int i = 0; i < list.Length; i++) if (list[i].id == prev) selected = i;
            RebuildUI();
            SelectHero(selected);
        }

        void UpdateCrystals()
        {
            if (crystalsText != null) crystalsText.text = Meta.P.crystals.ToString();
        }

        // ---------------- Угроза ----------------

        void ChangeThreat(int delta)
        {
            var p = Meta.P;
            int v = Mathf.Clamp(p.selectedThreat + delta, 0, p.threatUnlocked);
            if (v == p.selectedThreat)
            {
                SoundFX.Play(SoundFX.Sfx.Empty, 0.5f);
                return;
            }
            p.selectedThreat = v;
            Meta.Save();
            UpdateThreat();
        }

        void UpdateThreat()
        {
            if (threatText == null) return;
            var p = Meta.P;
            int t = Mathf.Clamp(p.selectedThreat, 0, p.threatUnlocked);
            threatText.text = Loc.T("Угроза: ", "Threat: ") + t + " / " + Meta.MaxThreat;
            if (p.threatUnlocked == 0)
                threatInfo.text = Loc.T("Пройди сюжет, чтобы открыть уровни угрозы", "Beat the story to unlock threat levels");
            else if (t == 0)
                threatInfo.text = Loc.T("Обычная сложность", "Normal difficulty");
            else
                threatInfo.text = Loc.T("Враги: здоровье +", "Enemies: health +") + Mathf.RoundToInt((Meta.ThreatHealth(t) - 1f) * 100f) + Loc.T("%, урон +", "%, damage +") +
                                  Mathf.RoundToInt((Meta.ThreatDamage(t) - 1f) * 100f) + Loc.T("% · Кристаллы +", "% · Crystals +") + Mathf.RoundToInt((Meta.ThreatReward(t) - 1f) * 100f) + "%";
            threatText.color = t == 0 ? Color.white : Color.Lerp(UIKit.Accent, UIKit.Pink, t / (float)Meta.MaxThreat);
        }

        // ---------------- Герои ----------------

        void SelectHero(int index, bool rebuildModel = true)
        {
            selected = (index + list.Length) % list.Length;
            var info = list[selected];
            bool unlocked = Meta.P.HeroUnlocked(info.id);
            if (unlocked)
            {
                Meta.P.lastHero = (int)info.id;
                Meta.Save();
            }

            if (rebuildModel || currentModel == null)
            {
                if (currentModel != null) Destroy(currentModel.gameObject);
                currentModel = BrainrotModels.Build(info.id, turntable);
                currentModel.moveAmount = 0f;
                float scale = info.behavior == EnemyBehavior.Flyer ? 0.75f : 1f;
                currentModel.transform.localScale = Vector3.one * scale;
                currentModel.Init();
                currentModel.Roar(0.8f);
                modelPop = 0f;
                FX.Burst(new Vector3(0f, 1f, 0f), info.themeColor, 8, 5f, 0.18f, 0.7f);
                Particles.Sparkle(new Vector3(0f, 1.2f, 0f), info.themeColor, 25, 4f, 0.2f);
            }

            // Карточка героя (перестраивается целиком)
            if (heroDetails != null) Destroy(heroDetails.gameObject);
            heroDetails = UIKit.Rect(infoPanel, "Details");
            UIKit.Stretch(heroDetails);
            var d = heroDetails;
            heroColorBar.color = info.themeColor;

            var name = UIKit.Text(d, "Name", info.name, 42, TextAnchor.MiddleLeft, info.themeColor, true);
            UIKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(26f, -30f), new Vector2(610f, 60f), new Vector2(0f, 1f));
            var desc = UIKit.Text(d, "Desc", info.description, 20, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.8f));
            UIKit.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -94f), new Vector2(604f, 56f), new Vector2(0f, 1f));

            // Пассивка
            var pas = UIKit.Image(d, "Passive", UIKit.PanelLight);
            UIKit.Place(pas.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -156f), new Vector2(612f, 62f), new Vector2(0f, 1f));
            UIKit.Icon(pas.rectTransform, "Icon", IconKind.Star, UIKit.Accent, new Vector2(0f, 0.5f), new Vector2(14f, 0f), 36f, new Vector2(0f, 0.5f));
            var pt = UIKit.Text(pas.rectTransform, "T", "<color=#FFC93C>" + Loc.T("Пассивка · ", "Passive · ") + info.passiveName + "</color>\n" + info.passiveDescription, 18, TextAnchor.MiddleLeft, Color.white, false, false);
            UIKit.Place(pt.rectTransform, new Vector2(0f, 0.5f), new Vector2(62f, 0f), new Vector2(540f, 58f), new Vector2(0f, 0.5f));

            // Навыки
            GameUI.SkillCard(d, new Vector2(24f, -228f), 612f, info.q, Controls.KeyName(GameAction.Ability), info.themeColor);
            GameUI.SkillCard(d, new Vector2(24f, -334f), 612f, info.e, Controls.KeyName(GameAction.Ability2), info.themeColor);

            // Характеристики
            Chip(d, new Vector2(24f, -442f), IconKind.Heart, GameAssets.Hex("#FF5F7E"), Loc.T("Здоровье", "Health"), Mathf.RoundToInt(info.heroMaxHealth).ToString());
            Chip(d, new Vector2(334f, -442f), IconKind.Boot, UIKit.Cyan, Loc.T("Скорость", "Speed"), "×" + info.heroSpeedMultiplier.ToString("0.00"));

            // Открытие / заметка
            string note;
            if (!unlocked)
            {
                string goal = Meta.GoalText(info);
                note = "<color=#FF8FB1>" + Loc.T("Закрыт. ", "Locked. ") + "</color>" +
                       (goal.Length > 0 ? Loc.T("Задание: ", "Goal: ") + goal + Loc.T("\nили купи за кристаллы", "\nor buy with crystals") : Loc.T("Открой за кристаллы", "Unlock with crystals"));
            }
            else if (mode == GameMode.Story) note = Loc.T("Его арена (", "His arena (") + info.arenaName + Loc.T(") в забеге пропускается", ") is skipped in the run");
            else note = info.story ? Loc.T("Сюжетный герой", "Story hero") : Loc.T("Встречается врагом в выживании и защите", "Appears as an enemy in survival and defense");
            var nt = UIKit.Text(d, "Note", note, 18, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.7f));
            UIKit.Place(nt.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -512f), new Vector2(604f, 64f), new Vector2(0f, 1f));

            int price = Meta.HeroPrice(info.id);
            if (unlocked)
            {
                playLabel.text = Loc.T("ИГРАТЬ", "PLAY") + " · " + ModeTitle(mode).ToUpper();
                playButton.GetComponent<Image>().color = UIKit.PlayColor;
            }
            else
            {
                bool can = Meta.P.crystals >= price;
                playLabel.text = Loc.T("ОТКРЫТЬ: ", "UNLOCK: ") + Loc.Plural(price, "кристалл", "кристалла", "кристаллов", "crystal", "crystals");
                playButton.GetComponent<Image>().color = can ? GameAssets.Hex("#00796B") : new Color(0.25f, 0.22f, 0.3f, 1f);
            }

            for (int i = 0; i < heroButtons.Count; i++)
            {
                var hi = list[i];
                heroButtons[i].color = i == selected ? new Color(hi.themeColor.r * 0.55f, hi.themeColor.g * 0.55f, hi.themeColor.b * 0.55f, 0.95f) : UIKit.Panel;
                heroLocks[i].enabled = !Meta.P.HeroUnlocked(hi.id);
            }
        }

        static void Chip(RectTransform parent, Vector2 pos, IconKind icon, Color color, string label, string value)
        {
            var chip = UIKit.Image(parent, "Chip", UIKit.PanelLight);
            UIKit.Place(chip.rectTransform, new Vector2(0f, 1f), pos, new Vector2(302f, 54f), new Vector2(0f, 1f));
            UIKit.Icon(chip.rectTransform, "Icon", icon, color, new Vector2(0f, 0.5f), new Vector2(12f, 0f), 32f, new Vector2(0f, 0.5f));
            var l = UIKit.Text(chip.rectTransform, "L", label, 18, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.65f), false, false);
            UIKit.Place(l.rectTransform, new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(150f, 48f), new Vector2(0f, 0.5f));
            var v = UIKit.Text(chip.rectTransform, "V", value, 22, TextAnchor.MiddleRight, Color.white, true, false);
            UIKit.Place(v.rectTransform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(120f, 48f), new Vector2(1f, 0.5f));
        }

        void OnPlayPressed()
        {
            var info = list[selected];
            if (Meta.P.HeroUnlocked(info.id))
            {
                Play();
                return;
            }
            if (Meta.TryUnlockHero(info.id))
            {
                SoundFX.Play(SoundFX.Sfx.Victory, 0.7f);
                Particles.Sparkle(new Vector3(0f, 1.2f, 0f), UIKit.Crystal, 60, 6f, 0.3f);
                UpdateCrystals();
                SelectHero(selected, false);
                if (currentModel != null) currentModel.Roar(1.2f);
            }
            else SoundFX.Play(SoundFX.Sfx.Empty);
        }

        void Play()
        {
            if (SceneLoader.Busy) return;
            var info = list[selected];
            if (!Meta.P.HeroUnlocked(info.id)) return;
            RunState.StartNew(info.id, mode);
            SceneLoader.Load("Game");
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------- Лаборатория ----------------

        void ShowLab()
        {
            for (int i = overlay.childCount - 1; i >= 0; i--) Destroy(overlay.GetChild(i).gameObject);
            var screen = UIKit.Image(overlay, "Lab", new Color(0.02f, 0.03f, 0.06f, 0.94f), false);
            screen.raycastTarget = true;
            UIKit.Stretch(screen.rectTransform);
            screen.gameObject.AddComponent<ScreenIntro>();
            var sr = screen.rectTransform;

            UIKit.Icon(sr, "Icon", IconKind.Flask, UIKit.Crystal, new Vector2(0f, 1f), new Vector2(70f, -30f), 80f, new Vector2(0f, 1f)).gameObject.AddComponent<Wobble>();
            var title = UIKit.Text(sr, "Title", new LS("ЛАБОРАТОРИЯ", "LABORATORY"), 70, TextAnchor.MiddleLeft, UIKit.Crystal, true);
            UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(170f, -30f), new Vector2(900f, 90f), new Vector2(0f, 1f));
            var sub = UIKit.Text(sr, "Sub", new LS("Постоянные улучшения за кристаллы мозга. Действуют во всех забегах.", "Permanent upgrades for brain crystals. They work in every run."), 26, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.75f));
            UIKit.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(172f, -115f), new Vector2(1300f, 40f), new Vector2(0f, 1f));

            var crystalPanel = UIKit.Image(sr, "Crystals", UIKit.Panel);
            UIKit.Place(crystalPanel.rectTransform, new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(260f, 76f), new Vector2(1f, 1f));
            UIKit.Icon(crystalPanel.rectTransform, "Icon", IconKind.Crystal, UIKit.Crystal, new Vector2(0f, 0.5f), new Vector2(14f, 0f), 52f, new Vector2(0f, 0.5f));
            var have = UIKit.Text(crystalPanel.rectTransform, "Value", "", 42, TextAnchor.MiddleRight, UIKit.Crystal, true);
            UIKit.Place(have.rectTransform, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(170f, 60f), new Vector2(1f, 0.5f));

            var grid = UIKit.Rect(sr, "Grid");
            UIKit.Place(grid, new Vector2(0.5f, 1f), new Vector2(0f, -175f), new Vector2(1780f, 740f), new Vector2(0.5f, 1f));
            bool first = true;

            void Rebuild()
            {
                for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
                have.text = Meta.P.crystals.ToString();
                UpdateCrystals();
                const int columns = 5;
                for (int i = 0; i < Meta.Lab.Length; i++)
                {
                    var u = Meta.Lab[i];
                    int rank = Meta.Rank(u);
                    bool maxed = rank >= u.maxRank;
                    int cost = maxed ? 0 : u.CostForRank(rank);
                    bool can = Meta.CanBuy(u);
                    int col = i % columns, row = i / columns;

                    var card = UIKit.Button(grid, "Card", "", can ? UIKit.Panel : new Color(0.1f, 0.08f, 0.15f, 0.9f), () =>
                    {
                        if (Meta.Buy(u))
                        {
                            SoundFX.Play(SoundFX.Sfx.Upgrade, 0.7f);
                            Rebuild();
                        }
                        else SoundFX.Play(SoundFX.Sfx.Empty);
                    });
                    var cr = card.GetComponent<RectTransform>();
                    UIKit.Place(cr, new Vector2(0f, 1f), new Vector2(col * 356f, -row * 246f), new Vector2(340f, 232f), new Vector2(0f, 1f));
                    if (first) PopIn.Add(card, 0.05f + i * 0.03f, 0.3f);

                    var iconBack = UIKit.Image(cr, "IconBack", new Color(u.color.r * 0.35f, u.color.g * 0.35f, u.color.b * 0.35f, 1f));
                    iconBack.sprite = UIKit.Circle;
                    UIKit.Place(iconBack.rectTransform, new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(76f, 76f), new Vector2(0f, 1f));
                    UIKit.Icon(iconBack.rectTransform, "Icon", u.icon, u.color, new Vector2(0.5f, 0.5f), Vector2.zero, 52f, new Vector2(0.5f, 0.5f));
                    var n = UIKit.Text(cr, "Name", u.name, 27, TextAnchor.UpperLeft, u.color, true);
                    UIKit.Place(n.rectTransform, new Vector2(0f, 1f), new Vector2(102f, -16f), new Vector2(230f, 64f), new Vector2(0f, 1f));

                    // Ранги точками
                    for (int r = 0; r < u.maxRank; r++)
                    {
                        var pip = UIKit.Image(cr, "Pip", r < rank ? u.color : new Color(1f, 1f, 1f, 0.15f));
                        UIKit.Place(pip.rectTransform, new Vector2(0f, 1f), new Vector2(102f + r * 30f, -70f), new Vector2(24f, 10f), new Vector2(0f, 1f));
                    }

                    string effect = rank > 0 ? string.Format(u.description, u.format(u.Total(rank))) : Loc.T("Не изучено", "Not researched");
                    string next = maxed ? Loc.T("Максимум!", "Maxed!") : Loc.T("Далее: ", "Next: ") + string.Format(u.description, u.format(u.Total(rank + 1)));
                    var d = UIKit.Text(cr, "Desc", "<color=#FFFFFFDD>" + effect + "</color>\n<color=#FFFFFF88>" + next + "</color>", 20, TextAnchor.UpperLeft, Color.white);
                    UIKit.Place(d.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -96f), new Vector2(312f, 80f), new Vector2(0f, 1f));

                    var price = UIKit.Text(cr, "Price", maxed ? Loc.T("МАКС", "MAX") : cost.ToString(), 28, TextAnchor.LowerRight, maxed ? UIKit.Green : (can ? UIKit.Crystal : UIKit.Pink), true);
                    UIKit.Place(price.rectTransform, new Vector2(1f, 0f), new Vector2(-16f, 10f), new Vector2(200f, 40f), new Vector2(1f, 0f));
                    card.interactable = !maxed;
                }
                first = false;
            }

            Rebuild();

            var back = UIKit.IconButton(sr, "Back", IconKind.Back, new LS("Назад", "Back"), UIKit.Purple, () =>
            {
                Destroy(screen.gameObject);
                SelectHero(selected, false);
            }, 32);
            UIKit.Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(-200f, 40f), new Vector2(340f, 80f), new Vector2(0.5f, 0f));

            var refund = UIKit.IconButton(sr, "Refund", IconKind.Reload, new LS("Сбросить (вернуть всё)", "Reset (full refund)"), new Color(0.35f, 0.25f, 0.45f, 1f), () =>
            {
                if (Meta.SpentInLab() <= 0) { SoundFX.Play(SoundFX.Sfx.Empty); return; }
                Meta.RefundLab();
                SoundFX.Play(SoundFX.Sfx.Coin, 0.8f);
                Rebuild();
            }, 24);
            UIKit.Place(refund.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(200f, 40f), new Vector2(380f, 80f), new Vector2(0.5f, 0f));
        }

        // ---------------- Авторы ----------------

        void ShowCredits()
        {
            for (int i = overlay.childCount - 1; i >= 0; i--) Destroy(overlay.GetChild(i).gameObject);
            var screen = UIKit.Image(overlay, "Credits", new Color(0.02f, 0.01f, 0.06f, 0.94f), false);
            screen.raycastTarget = true;
            UIKit.Stretch(screen.rectTransform);
            screen.gameObject.AddComponent<ScreenIntro>();
            var sr = screen.rectTransform;
            var title = UIKit.Text(sr, "Title", new LS("АВТОРЫ", "CREDITS"), 70, TextAnchor.MiddleCenter, UIKit.Accent, true);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1400f, 90f), new Vector2(0.5f, 1f));
            string text = Loc.T("<b>Brainrot Waves</b>\n\n<color=#FFC93C>3D-модели, звуки, частицы</color>\nKenney (kenney.nl) — CC0\nCartoon FX Remaster — Jean Moreno (JMO)\n\n<color=#FFC93C>Шрифты</color>\nUnbounded, Exo 2 — SIL Open Font License\n\n<color=#FFC93C>Музыка и звуки окружения</color>\nKevin MacLeod (incompetech.com), CC BY 4.0\nPixabay: alanajordan, Art_Roseman, DesiFreeMusic, SlimeyFox, AllWorldMusic,\nWhite_Records, Denis-Pavlov-Music и др. — Pixabay Content License\n\n<color=#FFC93C>Спасибо</color>\nвсем авторам бесплатных ассетов!",
                "<b>Brainrot Waves</b>\n\n<color=#FFC93C>3D models, sounds, particles</color>\nKenney (kenney.nl) — CC0\nCartoon FX Remaster — Jean Moreno (JMO)\n\n<color=#FFC93C>Fonts</color>\nUnbounded, Exo 2 — SIL Open Font License\n\n<color=#FFC93C>Music and ambience</color>\nKevin MacLeod (incompetech.com), Licensed under Creative Commons: By Attribution 4.0\nPixabay: alanajordan, Art_Roseman, DesiFreeMusic, SlimeyFox, AllWorldMusic,\nWhite_Records, Denis-Pavlov-Music and others — Pixabay Content License\n\n<color=#FFC93C>Thanks</color>\nto all authors of free assets!");
            var body = UIKit.Text(sr, "Body", text, 30, TextAnchor.UpperCenter, Color.white);
            UIKit.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(1500f, 700f), new Vector2(0.5f, 1f));
            var back = UIKit.IconButton(sr, "Back", IconKind.Back, new LS("Назад", "Back"), UIKit.Purple, () => Destroy(screen.gameObject), 32);
            UIKit.Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(340f, 80f), new Vector2(0.5f, 0f));
        }

        void Update()
        {
            spin += Time.deltaTime * 40f;
            turntable.localRotation = Quaternion.Euler(0f, 180f + 25f + Mathf.Sin(spin * 0.02f) * 40f, 0f);
            modelPop = Mathf.MoveTowards(modelPop, 1f, Time.deltaTime * 3f);
            turntable.localScale = Vector3.one * Ease.OutBack(modelPop);

            var kb = Keyboard.current;
            if (kb == null || overlay == null || overlay.childCount > 0 || SceneLoader.Busy) return;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) { SoundFX.Play(SoundFX.Sfx.Click, 0.5f); SelectHero(selected + 1); }
            if (kb.leftArrowKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) { SoundFX.Play(SoundFX.Sfx.Click, 0.5f); SelectHero(selected - 1); }
            if (kb.enterKey.wasPressedThisFrame) OnPlayPressed();
        }
    }

    /// <summary>Медленно плавает и вращается (украшения меню).</summary>
    public class Floater : MonoBehaviour
    {
        Vector3 start;
        float offset;

        void Start()
        {
            start = transform.position;
            offset = Random.value * 10f;
        }

        void Update()
        {
            transform.position = start + Vector3.up * Mathf.Sin(Time.time * 0.8f + offset) * 0.3f;
            transform.Rotate(20f * Time.deltaTime, 30f * Time.deltaTime, 0f);
        }
    }
}
