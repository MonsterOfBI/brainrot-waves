using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrainrotWaves
{
    /// <summary>Главное меню: выбор героя с 3D-превью, описание, кнопка «Играть».</summary>
    public class MainMenu : MonoBehaviour
    {
        int selected;
        Transform stage;
        Transform turntable;
        BrainrotRig currentModel;
        Text heroName, heroDescription, passiveText, abilityText, statsText, recordText;
        Image heroColorBar;
        readonly List<Image> heroButtons = new List<Image>();
        float spin;
        Camera cam;

        void Start()
        {
            Time.timeScale = 1f;
            PlayerController.LockCursor(false);
            selected = Mathf.Clamp(PlayerPrefs.GetInt("BW_LastHero", 0), 0, Brainrots.All.Length - 1);
            BuildStage();
            BuildUI();
            SelectHero(selected);
            SoundFX.PlayMusic(0);
        }

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

            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.color = GameAssets.Hex("#FFF2E0");
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = GameAssets.Hex("#B39DDB");
            RenderSettings.ambientEquatorColor = GameAssets.Hex("#7E57C2");
            RenderSettings.ambientGroundColor = GameAssets.Hex("#311B92");
            RenderSettings.fog = false;

            // Подиум
            BrainrotModels.Part(stage, PrimitiveType.Cylinder, new Vector3(0f, -0.15f, 0f), new Vector3(3.6f, 0.15f, 3.6f), GameAssets.Hex("#FFC93C"), default, 0.6f);
            BrainrotModels.Part(stage, PrimitiveType.Cylinder, new Vector3(0f, -0.05f, 0f), new Vector3(3.2f, 0.06f, 3.2f), GameAssets.Hex("#FF5FA2"), default, 0.6f);
            var floor = BrainrotModels.Part(stage, PrimitiveType.Cylinder, new Vector3(0f, -0.4f, 0f), new Vector3(40f, 0.1f, 40f), GameAssets.Hex("#3B2063"), default, 0.2f);

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
        }

        void BuildUI()
        {
            UIKit.EnsureEventSystem();
            var root = UIKit.Canvas("MenuCanvas", 10);

            var title = UIKit.Text(root, "Title", "BRAINROT WAVES", 110, TextAnchor.UpperLeft, UIKit.Accent, true);
            UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(70f, -40f), new Vector2(1200f, 140f));
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(6f, -6f);
            var subtitle = UIKit.Text(root, "Subtitle", "Шутер с волнами итальянских брейнротов", 34, TextAnchor.UpperLeft, Color.white);
            UIKit.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(76f, -170f), new Vector2(1200f, 50f));

            // Список героев слева
            var listLabel = UIKit.Text(root, "Choose", "ВЫБЕРИ ГЕРОЯ", 30, TextAnchor.UpperLeft, UIKit.Pink, true);
            UIKit.Place(listLabel.rectTransform, new Vector2(0f, 1f), new Vector2(76f, -250f), new Vector2(500f, 40f));
            for (int i = 0; i < Brainrots.All.Length; i++)
            {
                int index = i;
                var info = Brainrots.All[i];
                var b = UIKit.Button(root, "Hero" + i, info.name, UIKit.Panel, () => SelectHero(index), 28);
                var rect = b.GetComponent<RectTransform>();
                UIKit.Place(rect, new Vector2(0f, 1f), new Vector2(70f, -300f - i * 78f), new Vector2(440f, 66f));
                var label = rect.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(34f, 0f);
                var swatch = UIKit.Image(rect, "Color", info.themeColor);
                UIKit.Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(14f, 46f), new Vector2(0f, 0.5f));
                heroButtons.Add(b.GetComponent<Image>());
            }

            // Описание справа
            var panel = UIKit.Image(root, "InfoPanel", UIKit.Panel);
            UIKit.Place(panel.rectTransform, new Vector2(1f, 0.5f), new Vector2(-60f, 40f), new Vector2(620f, 640f), new Vector2(1f, 0.5f));
            var pr = panel.rectTransform;
            heroColorBar = UIKit.Image(pr, "Bar", Color.white);
            UIKit.Place(heroColorBar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(588f, 14f), new Vector2(0.5f, 1f));
            heroName = UIKit.Text(pr, "Name", "", 46, TextAnchor.UpperLeft, Color.white, true);
            UIKit.Place(heroName.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -44f), new Vector2(570f, 60f));
            heroDescription = UIKit.Text(pr, "Desc", "", 26, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.85f));
            UIKit.Place(heroDescription.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -110f), new Vector2(570f, 80f));
            passiveText = UIKit.Text(pr, "Passive", "", 26, TextAnchor.UpperLeft, Color.white);
            UIKit.Place(passiveText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -210f), new Vector2(570f, 100f));
            abilityText = UIKit.Text(pr, "Ability", "", 26, TextAnchor.UpperLeft, Color.white);
            UIKit.Place(abilityText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -320f), new Vector2(570f, 110f));
            statsText = UIKit.Text(pr, "Stats", "", 24, TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 0.75f));
            UIKit.Place(statsText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -440f), new Vector2(570f, 110f));

            var play = UIKit.Button(pr, "Play", "ИГРАТЬ ▶", GameAssets.Hex("#E53977"), Play, 48);
            UIKit.Place(play.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(560f, 96f), new Vector2(0.5f, 0f));

            recordText = UIKit.Text(root, "Record", "", 26, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.7f));
            UIKit.Place(recordText.rectTransform, new Vector2(0f, 0f), new Vector2(76f, 40f), new Vector2(900f, 80f));
            recordText.text = "Лучший результат: " + RunState.BestArena + " из 5 арен · Побед: " + RunState.Wins +
                              "\nСтрелки ←/→ — выбор героя, Enter — играть";

            var quit = UIKit.Button(root, "Quit", "Выход", new Color(0.3f, 0.2f, 0.4f, 0.9f), Quit, 26);
            UIKit.Place(quit.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(-60f, 30f), new Vector2(200f, 60f), new Vector2(1f, 0f));
        }

        void SelectHero(int index)
        {
            selected = (index + Brainrots.All.Length) % Brainrots.All.Length;
            var info = Brainrots.All[selected];
            PlayerPrefs.SetInt("BW_LastHero", selected);

            if (currentModel != null) Destroy(currentModel.gameObject);
            currentModel = BrainrotModels.Build(info.id, turntable);
            currentModel.moveAmount = 0f;
            float scale = info.id == BrainrotId.Bombardiro ? 0.75f : 1f;
            currentModel.transform.localScale = Vector3.one * scale;
            currentModel.Init();
            FX.Burst(new Vector3(0f, 1f, 0f), info.themeColor, 12, 5f, 0.18f, 0.7f);

            heroName.text = info.name;
            heroName.color = info.themeColor;
            heroColorBar.color = info.themeColor;
            heroDescription.text = info.description;
            passiveText.text = "<color=#FFC93C><b>Пассивка: " + info.passiveName + "</b></color>\n" + info.passiveDescription;
            abilityText.text = "<color=#3DE0FF><b>Q: " + info.abilityName + "</b></color>\n" + info.abilityDescription + " (перезарядка " + info.abilityCooldown + " с)";
            statsText.text = "Здоровье: " + info.heroMaxHealth + " · Скорость: ×" + info.heroSpeedMultiplier.ToString("0.00") +
                             "\nЕго арена (" + info.arenaName + ") в забеге пропускается";

            for (int i = 0; i < heroButtons.Count; i++)
                heroButtons[i].color = i == selected ? new Color(info.themeColor.r * 0.6f, info.themeColor.g * 0.6f, info.themeColor.b * 0.6f, 0.95f) : UIKit.Panel;
        }

        void Play()
        {
            RunState.StartNew(Brainrots.All[selected].id);
            SceneManager.LoadScene("Game");
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Update()
        {
            spin += Time.deltaTime * 40f;
            turntable.localRotation = Quaternion.Euler(0f, 180f + 25f + Mathf.Sin(spin * 0.02f) * 40f, 0f);

            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) { SoundFX.Play(SoundFX.Sfx.Click, 0.5f); SelectHero(selected + 1); }
            if (kb.leftArrowKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) { SoundFX.Play(SoundFX.Sfx.Click, 0.5f); SelectHero(selected - 1); }
            if (kb.enterKey.wasPressedThisFrame) Play();
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
