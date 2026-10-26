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
        Text heroName, heroDescription, passiveText, abilityText, statsText, crystalsText, threatText, threatInfo;
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
            selected = Mathf.Clamp(Meta.P.lastHero, 0, Brainrots.All.Length - 1);
            BuildStage();
            BuildUI();
            SelectHero(selected);
            SoundFX.PlayMusic(SoundFX.MenuMusic);
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

        void BuildUI()
        {
            UIKit.EnsureEventSystem();
            canvas = UIKit.Canvas("MenuCanvas", 10);
            var root = canvas;

            var title = UIKit.Text(root, "Title", "BRAINROT WAVES", 88, TextAnchor.UpperLeft, UIKit.Accent, true);
            UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -50f), new Vector2(820f, 120f));
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6f, -6f);
            title.gameObject.AddComponent<Wobble>();
            var subtitle = UIKit.Text(root, "Subtitle", new LS("Шутер с волнами итальянских брейнротов", "A wave shooter with Italian brainrots"), 34, TextAnchor.UpperLeft, Color.white);
            UIKit.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(76f, -170f), new Vector2(1200f, 50f));
            SlideIn.Add(subtitle, new Vector2(-300f, 0f), 0.1f);

            // Кристаллы и кнопки сверху справа
            var crystalPanel = UIKit.Image(root, "Crystals", UIKit.Panel);
            UIKit.Place(crystalPanel.rectTransform, new Vector2(1f, 1f), new Vector2(-60f, -40f), new Vector2(240f, 70f), new Vector2(1f, 1f));
            UIKit.Icon(crystalPanel.rectTransform, "Icon", IconKind.Crystal, UIKit.Crystal, new Vector2(0f, 0.5f), new Vector2(14f, 0f), 48f, new Vector2(0f, 0.5f)).gameObject.AddComponent<Pulse>();
            crystalsText = UIKit.Text(crystalPanel.rectTransform, "Value", "", 38, TextAnchor.MiddleRight, UIKit.Crystal, true);
            UIKit.Place(crystalsText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(160f, 60f), new Vector2(1f, 0.5f));
            SlideIn.Add(crystalPanel, new Vector2(0f, 120f), 0.1f);

            var lab = UIKit.IconButton(root, "Lab", IconKind.Flask, new LS("Лаборатория", "Lab"), GameAssets.Hex("#00897B"), ShowLab, 28);
            UIKit.Place(lab.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-320f, -40f), new Vector2(290f, 70f), new Vector2(1f, 1f));
            SlideIn.Add(lab, new Vector2(0f, 120f), 0.15f);
            var settings = UIKit.IconButton(root, "Settings", IconKind.Gear, new LS("Настройки", "Settings"), new Color(0.25f, 0.4f, 0.75f, 1f), () => SettingsScreen.Show(overlay, null), 28);
            UIKit.Place(settings.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-630f, -40f), new Vector2(270f, 70f), new Vector2(1f, 1f));
            SlideIn.Add(settings, new Vector2(0f, 120f), 0.2f);
            var lang = UIKit.IconButton(root, "Lang", IconKind.Globe, new LS("RU", "EN"), UIKit.Purple, Loc.Toggle, 28);
            UIKit.Place(lang.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-920f, -40f), new Vector2(150f, 70f), new Vector2(1f, 1f));
            SlideIn.Add(lang, new Vector2(0f, 120f), 0.25f);

            // Список героев слева
            var listLabel = UIKit.Text(root, "Choose", new LS("ВЫБЕРИ ГЕРОЯ", "CHOOSE A HERO"), 30, TextAnchor.UpperLeft, UIKit.Pink, true);
            UIKit.Place(listLabel.rectTransform, new Vector2(0f, 1f), new Vector2(76f, -250f), new Vector2(500f, 40f));
            for (int i = 0; i < Brainrots.All.Length; i++)
            {
                int index = i;
                var info = Brainrots.All[i];
                var b = UIKit.Button(root, "Hero" + i, info.name, UIKit.Panel, () => SelectHero(index), 28);
                var rect = b.GetComponent<RectTransform>();
                UIKit.Place(rect, new Vector2(0f, 1f), new Vector2(70f, -300f - i * 78f), new Vector2(460f, 66f));
                var label = rect.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(34f, 0f);
                var swatch = UIKit.Image(rect, "Color", info.themeColor);
                UIKit.Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(14f, 46f), new Vector2(0f, 0.5f));
                var lockIcon = UIKit.Icon(rect, "Lock", IconKind.Lock, new Color(1f, 1f, 1f, 0.8f), new Vector2(1f, 0.5f), new Vector2(-14f, 0f), 38f, new Vector2(1f, 0.5f));
                heroLocks.Add(lockIcon);
                heroButtons.Add(b.GetComponent<Image>());
                SlideIn.Add(b, new Vector2(-500f, 0f), 0.1f + i * 0.05f, 0.4f);
            }

            // Описание справа
            var panel = UIKit.Image(root, "InfoPanel", UIKit.Panel);
            UIKit.Place(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-60f, -10f), new Vector2(640f, 720f), new Vector2(1f, 0.5f));
            SlideIn.Add(panel, new Vector2(700f, 0f), 0.15f, 0.5f);
            var pr = panel.rectTransform;
            heroColorBar = UIKit.Image(pr, "Bar", Color.white);
            UIKit.Place(heroColorBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(608f, 14f), new Vector2(0.5f, 1f));
            heroName = UIKit.Text(pr, "Name", "", 46, TextAnchor.UpperLeft, Color.white, true);
            UIKit.Place(heroName.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -44f), new Vector2(590f, 60f));
            heroDescription = UIKit.Text(pr, "Desc", "", 26, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.85f));
            UIKit.Place(heroDescription.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -110f), new Vector2(590f, 80f));
            passiveText = UIKit.Text(pr, "Passive", "", 26, TextAnchor.UpperLeft, Color.white);
            UIKit.Place(passiveText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -200f), new Vector2(590f, 100f));
            abilityText = UIKit.Text(pr, "Ability", "", 26, TextAnchor.UpperLeft, Color.white);
            UIKit.Place(abilityText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -300f), new Vector2(590f, 110f));
            statsText = UIKit.Text(pr, "Stats", "", 24, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.75f));
            UIKit.Place(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -410f), new Vector2(590f, 90f));

            // Уровень угрозы
            var threatRow = UIKit.Rect(pr, "Threat");
            UIKit.Place(threatRow, new Vector2(0.5f, 0f), new Vector2(0f, 136f), new Vector2(590f, 90f), new Vector2(0.5f, 0f));
            var minus = UIKit.Button(threatRow, "Minus", "<", new Color(0.22f, 0.17f, 0.34f, 1f), () => ChangeThreat(-1), 26);
            UIKit.Place(minus.GetComponent<RectTransform>(), new Vector2(0f, 1f), Vector2.zero, new Vector2(60f, 50f), new Vector2(0f, 1f));
            var plus = UIKit.Button(threatRow, "Plus", ">", new Color(0.22f, 0.17f, 0.34f, 1f), () => ChangeThreat(1), 26);
            UIKit.Place(plus.GetComponent<RectTransform>(), new Vector2(1f, 1f), Vector2.zero, new Vector2(60f, 50f), new Vector2(1f, 1f));
            threatText = UIKit.Text(threatRow, "Value", "", 30, TextAnchor.MiddleCenter, UIKit.Pink, true);
            UIKit.Place(threatText.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(440f, 50f), new Vector2(0.5f, 1f));
            threatInfo = UIKit.Text(threatRow, "Info", "", 20, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.65f));
            UIKit.Place(threatInfo.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(590f, 36f), new Vector2(0.5f, 0f));
            UpdateThreat();

            playButton = UIKit.Button(pr, "Play", "", UIKit.PlayColor, OnPlayPressed, 46);
            UIKit.Place(playButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(580f, 96f), new Vector2(0.5f, 0f));
            playLabel = playButton.GetComponentInChildren<Text>();
            playButton.gameObject.AddComponent<Pulse>().amount = 0.025f;

            var recordText = UIKit.Text(root, "Record", "", 24, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.7f));
            UIKit.Place(recordText.rectTransform, new Vector2(0f, 0f), new Vector2(76f, 40f), new Vector2(1000f, 90f));
            var p = Meta.P;
            recordText.text = Loc.T("Лучший результат: ", "Best result: ") + p.bestArena + Loc.T(" из 5 арен · Побед: ", " of 5 arenas · Wins: ") + p.wins +
                              Loc.T(" · Лучшее комбо: ", " · Best combo: ") + p.bestCombo +
                              "\n" + Loc.T("Стрелки ←/→ — выбор героя, Enter — играть", "Arrows ←/→ — choose hero, Enter — play");

            var quit = UIKit.IconButton(root, "Quit", IconKind.Exit, new LS("Выход", "Quit"), new Color(0.3f, 0.2f, 0.4f, 0.9f), Quit, 26);
            UIKit.Place(quit.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-60f, 30f), new Vector2(210f, 60f), new Vector2(1f, 0f));

            var credits = UIKit.IconButton(root, "Credits", IconKind.Star, new LS("Авторы", "Credits"), new Color(0.3f, 0.2f, 0.4f, 0.9f), ShowCredits, 26);
            UIKit.Place(credits.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-290f, 30f), new Vector2(210f, 60f), new Vector2(1f, 0f));

            overlay = UIKit.Rect(root, "Overlay");
            UIKit.Stretch(overlay);
            UpdateCrystals();
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
            var p = Meta.P;
            int t = Mathf.Clamp(p.selectedThreat, 0, p.threatUnlocked);
            threatText.text = Loc.T("Угроза: ", "Threat: ") + t + " / " + Meta.MaxThreat;
            if (p.threatUnlocked == 0)
                threatInfo.text = Loc.T("Победи, чтобы открыть уровни угрозы", "Win a run to unlock threat levels");
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
            selected = (index + Brainrots.All.Length) % Brainrots.All.Length;
            var info = Brainrots.All[selected];
            bool unlocked = Meta.P.HeroUnlocked(info.id);
            if (unlocked)
            {
                Meta.P.lastHero = selected;
                Meta.Save();
            }

            if (rebuildModel || currentModel == null)
            {
                if (currentModel != null) Destroy(currentModel.gameObject);
                currentModel = BrainrotModels.Build(info.id, turntable);
                currentModel.moveAmount = 0f;
                float scale = info.id == BrainrotId.Bombardiro ? 0.75f : 1f;
                currentModel.transform.localScale = Vector3.one * scale;
                currentModel.Init();
                currentModel.Roar(0.8f);
                modelPop = 0f;
                FX.Burst(new Vector3(0f, 1f, 0f), info.themeColor, 8, 5f, 0.18f, 0.7f);
                Particles.Sparkle(new Vector3(0f, 1.2f, 0f), info.themeColor, 25, 4f, 0.2f);
            }

            heroName.text = info.name;
            heroName.color = info.themeColor;
            heroColorBar.color = info.themeColor;
            heroDescription.text = info.description;
            passiveText.text = "<color=#FFC93C><b>" + Loc.T("Пассивка: ", "Passive: ") + info.passiveName + "</b></color>\n" + info.passiveDescription;
            abilityText.text = "<color=#3DE0FF><b>" + Controls.KeyName(GameAction.Ability) + ": " + info.abilityName + "</b></color>\n" + info.abilityDescription +
                               Loc.T(" (перезарядка ", " (cooldown ") + info.abilityCooldown + Loc.T(" с)", " s)");
            statsText.text = Loc.T("Здоровье: ", "Health: ") + info.heroMaxHealth + Loc.T(" · Скорость: ×", " · Speed: ×") + info.heroSpeedMultiplier.ToString("0.00") +
                             "\n" + Loc.T("Его арена (", "His arena (") + info.arenaName + Loc.T(") в забеге пропускается", ") is skipped in the run");

            int price = Meta.HeroPrice(info.id);
            if (unlocked)
            {
                playLabel.text = Loc.T("ИГРАТЬ", "PLAY");
                playButton.GetComponent<Image>().color = UIKit.PlayColor;
            }
            else
            {
                bool can = Meta.P.crystals >= price;
                playLabel.text = Loc.T("ОТКРЫТЬ: ", "UNLOCK: ") + Loc.Plural(price, "кристалл", "кристалла", "кристаллов", "crystal", "crystals");
                playButton.GetComponent<Image>().color = can ? GameAssets.Hex("#00897B") : new Color(0.3f, 0.25f, 0.35f, 1f);
            }

            for (int i = 0; i < heroButtons.Count; i++)
            {
                var hi = Brainrots.All[i];
                heroButtons[i].color = i == selected ? new Color(hi.themeColor.r * 0.6f, hi.themeColor.g * 0.6f, hi.themeColor.b * 0.6f, 0.95f) : UIKit.Panel;
                heroLocks[i].enabled = !Meta.P.HeroUnlocked(hi.id);
            }
        }

        void OnPlayPressed()
        {
            var info = Brainrots.All[selected];
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
            var info = Brainrots.All[selected];
            if (!Meta.P.HeroUnlocked(info.id)) return;
            RunState.StartNew(info.id);
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
            string text = Loc.T("<b>Brainrot Waves</b>\n\n<color=#FFC93C>3D-модели, звуки, частицы</color>\nKenney (kenney.nl) — CC0\n\n<color=#FFC93C>Шрифты</color>\nRubik, Russo One — SIL Open Font License\n\n<color=#FFC93C>Музыка и звуки окружения</color>\nPixabay — Pixabay Content License\n\n<color=#FFC93C>Спасибо</color>\nвсем авторам бесплатных ассетов!",
                "<b>Brainrot Waves</b>\n\n<color=#FFC93C>3D models, sounds, particles</color>\nKenney (kenney.nl) — CC0\n\n<color=#FFC93C>Fonts</color>\nRubik, Russo One — SIL Open Font License\n\n<color=#FFC93C>Music and ambience</color>\nPixabay — Pixabay Content License\n\n<color=#FFC93C>Thanks</color>\nto all authors of free assets!");
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
