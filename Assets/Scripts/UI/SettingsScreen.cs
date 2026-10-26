using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BrainrotWaves
{
    /// <summary>
    /// Окно настроек (из главного меню и из паузы): звук, графика, управление, игра/язык.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        enum Tab { Sound, Graphics, Controls, Game }

        public static bool IsOpen => current != null;
        /// <summary>Кадр, в котором окно закрылось по Esc (чтобы пауза не снялась тем же нажатием).</summary>
        public static int ClosedByEscFrame = -1;

        static SettingsScreen current;
        static Tab lastTab = Tab.Sound;

        RectTransform screen;
        RectTransform content;
        Action onClose;
        Tab tab;
        GameAction? rebinding;
        Text rebindingLabel;
        readonly Button[] tabButtons = new Button[4];

        public static SettingsScreen Show(RectTransform parent, Action onClose)
        {
            GameSettings.EnsureLoaded();
            var dim = UIKit.Image(parent, "Settings", new Color(0.02f, 0.01f, 0.06f, 0.9f), false);
            dim.raycastTarget = true;
            UIKit.Stretch(dim.rectTransform);
            dim.gameObject.AddComponent<ScreenIntro>();
            var s = dim.gameObject.AddComponent<SettingsScreen>();
            s.screen = dim.rectTransform;
            s.onClose = onClose;
            s.Build();
            current = s;
            return s;
        }

        void OnDestroy()
        {
            if (current == this) current = null;
            Loc.Changed -= Rebuild;
        }

        void Build()
        {
            Loc.Changed += Rebuild;
            var panel = UIKit.Image(screen, "Panel", UIKit.Panel);
            UIKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 860f), new Vector2(0.5f, 0.5f));
            PopIn.Add(panel, 0f, 0.3f);
            var pr = panel.rectTransform;

            UIKit.Icon(pr, "GearIcon", IconKind.Gear, UIKit.Accent, new Vector2(0f, 1f), new Vector2(40f, -28f), 64f, new Vector2(0f, 1f)).gameObject.AddComponent<Spin>().speed = -25f;
            var title = UIKit.Text(pr, "Title", new LS("НАСТРОЙКИ", "SETTINGS"), 56, TextAnchor.MiddleLeft, UIKit.Accent, true);
            UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(120f, -24f), new Vector2(700f, 72f), new Vector2(0f, 1f));

            LS[] names = { new LS("Звук", "Sound"), new LS("Графика", "Graphics"), new LS("Управление", "Controls"), new LS("Игра", "Game") };
            IconKind[] icons = { IconKind.Speaker, IconKind.Monitor, IconKind.Keyboard, IconKind.Globe };
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var b = UIKit.IconButton(pr, "Tab" + i, icons[i], names[i], UIKit.Panel, () => SelectTab((Tab)index), 28);
                UIKit.Place(b.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(40f + i * 292f, -120f), new Vector2(276f, 64f), new Vector2(0f, 1f));
                tabButtons[i] = b;
            }

            content = UIKit.Rect(pr, "Content");
            UIKit.Place(content, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(1160f, 520f), new Vector2(0.5f, 1f));

            var done = UIKit.IconButton(pr, "Done", IconKind.Check, new LS("Готово", "Done"), UIKit.OkColor, Close, 34);
            UIKit.Place(done.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(360f, 80f), new Vector2(0.5f, 0f));

            SelectTab(lastTab);
        }

        void Rebuild() => SelectTab(tab);

        void SelectTab(Tab t)
        {
            tab = t;
            lastTab = t;
            rebinding = null;
            for (int i = 0; i < tabButtons.Length; i++)
                tabButtons[i].GetComponent<Image>().color = i == (int)t ? UIKit.PlayColor : new Color(0.2f, 0.15f, 0.3f, 0.95f);
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

            switch (t)
            {
                case Tab.Sound: BuildSound(); break;
                case Tab.Graphics: BuildGraphics(); break;
                case Tab.Controls: BuildControls(); break;
                default: BuildGame(); break;
            }
        }

        // ---------- Вкладки ----------

        void BuildSound()
        {
            float y = 0f;
            SliderRow(ref y, new LS("Общая громкость", "Master volume"), 0f, 1f, GameSettings.MasterVolume, v => { GameSettings.MasterVolume = v; GameSettings.Apply(); }, Percent);
            SliderRow(ref y, new LS("Музыка", "Music"), 0f, 1f, GameSettings.MusicVolume, v => { GameSettings.MusicVolume = v; GameSettings.Apply(); }, Percent);
            SliderRow(ref y, new LS("Звуки", "Sound effects"), 0f, 1f, GameSettings.SfxVolume, v =>
            {
                GameSettings.SfxVolume = v;
                GameSettings.Apply();
            }, Percent, true);
        }

        void BuildGraphics()
        {
            float y = 0f;
            ChoiceRow(ref y, new LS("Качество графики", "Graphics quality"), new[] { GameSettings.QualityName(0), GameSettings.QualityName(1), GameSettings.QualityName(2) },
                GameSettings.Quality, i => { GameSettings.Quality = i; GameSettings.Apply(); });
            ToggleRow(ref y, new LS("Пост-обработка (свечение, цвета)", "Post-processing (bloom, colors)"), () => GameSettings.PostProcessing, v => { GameSettings.PostProcessing = v; GameSettings.Apply(); });
            ToggleRow(ref y, new LS("Полный экран", "Fullscreen"), () => GameSettings.Fullscreen, v => { GameSettings.Fullscreen = v; GameSettings.Apply(); });
            ToggleRow(ref y, new LS("Вертикальная синхронизация", "V-Sync"), () => GameSettings.VSync, v => { GameSettings.VSync = v; GameSettings.Apply(); });
            SliderRow(ref y, new LS("Угол обзора", "Field of view"), 60f, 100f, GameSettings.Fov, v => { GameSettings.Fov = Mathf.Round(v); GameSettings.Apply(); }, v => Mathf.RoundToInt(v) + "°");
            SliderRow(ref y, new LS("Тряска экрана", "Screen shake"), 0f, 1f, GameSettings.ScreenShake, v => { GameSettings.ScreenShake = v; GameSettings.Apply(); }, Percent);
        }

        void BuildControls()
        {
            float y = 0f;
            SliderRow(ref y, new LS("Чувствительность мыши", "Mouse sensitivity"), 0.02f, 0.5f, GameSettings.Sensitivity, v => { GameSettings.Sensitivity = v; GameSettings.Apply(); }, v => v.ToString("0.00"));
            ToggleRow(ref y, new LS("Инвертировать мышь по вертикали", "Invert mouse Y"), () => GameSettings.InvertY, v => { GameSettings.InvertY = v; GameSettings.Apply(); });

            int count = Controls.Count;
            int perColumn = (count + 1) / 2;
            for (int i = 0; i < count; i++)
            {
                var action = (GameAction)i;
                int col = i / perColumn, row = i % perColumn;
                var label = UIKit.Text(content, "Key" + i, Controls.Name(action), 26, TextAnchor.MiddleLeft, Color.white);
                UIKit.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(20f + col * 580f, y - row * 54f), new Vector2(330f, 48f), new Vector2(0f, 1f));
                Text keyText = null;
                var b = UIKit.Button(content, "Bind" + i, Controls.KeyName(action), new Color(0.22f, 0.17f, 0.34f, 1f), () => StartRebind(action, keyText), 26);
                UIKit.Place(b.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(350f + col * 580f, y - row * 54f), new Vector2(200f, 46f), new Vector2(0f, 1f));
                keyText = b.GetComponentInChildren<Text>();
            }
            y -= perColumn * 54f + 16f;
            var hint = UIKit.Text(content, "Hint", new LS("ЛКМ — выстрел, колесо — смена оружия, Esc — пауза", "LMB — shoot, wheel — switch weapon, Esc — pause"), 22, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.6f));
            UIKit.Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(20f, y), new Vector2(760f, 40f), new Vector2(0f, 1f));
            var reset = UIKit.Button(content, "Reset", new LS("Сбросить клавиши", "Reset keys"), UIKit.Purple, () => { Controls.ResetDefaults(); SelectTab(Tab.Controls); }, 24);
            UIKit.Place(reset.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-20f, y), new Vector2(300f, 46f), new Vector2(1f, 1f));
        }

        void BuildGame()
        {
            float y = 0f;
            ChoiceRow(ref y, new LS("Язык", "Language"), new[] { new LS("Русский", "Русский"), new LS("English", "English") }, (int)Loc.Current, i => Loc.Set((Lang)i));
            ToggleRow(ref y, new LS("Цифры урона над врагами", "Damage numbers"), () => GameSettings.DamageNumbers, v => { GameSettings.DamageNumbers = v; GameSettings.Apply(); });
        }

        static string Percent(float v) => Mathf.RoundToInt(v * 100f) + "%";

        // ---------- Строки ----------

        Text RowLabel(float y, LS label)
        {
            var t = UIKit.Text(content, "Label", label, 30, TextAnchor.MiddleLeft, Color.white);
            UIKit.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(20f, y), new Vector2(560f, 64f), new Vector2(0f, 1f));
            SlideIn.Add(t, new Vector2(-60f, 0f), content.childCount * 0.02f, 0.3f);
            return t;
        }

        void SliderRow(ref float y, LS label, float min, float max, float value, Action<float> onChange, Func<float, string> format, bool clickSound = false)
        {
            RowLabel(y, label);
            var valueText = UIKit.Text(content, "Value", format(value), 28, TextAnchor.MiddleRight, UIKit.Accent, true);
            UIKit.Place(valueText.rectTransform, new Vector2(1f, 1f), new Vector2(-20f, y), new Vector2(120f, 64f), new Vector2(1f, 1f));
            float lastSound = 0f;
            var slider = UIKit.Slider(content, "Slider", min, max, value, v =>
            {
                onChange(v);
                valueText.text = format(v);
                if (clickSound && Time.unscaledTime - lastSound > 0.08f)
                {
                    lastSound = Time.unscaledTime;
                    SoundFX.Play(SoundFX.Sfx.Coin, 0.5f);
                }
            });
            UIKit.Place(slider.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-160f, y - 10f), new Vector2(420f, 44f), new Vector2(1f, 1f));
            y -= 76f;
        }

        void ToggleRow(ref float y, LS label, Func<bool> get, Action<bool> set)
        {
            RowLabel(y, label);
            Button b = null;
            b = UIKit.Button(content, "Toggle", "", Color.white, () =>
            {
                set(!get());
                Refresh(b, get());
            }, 26);
            UIKit.Place(b.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-20f, y - 6f), new Vector2(220f, 52f), new Vector2(1f, 1f));
            Refresh(b, get());
            y -= 76f;

            void Refresh(Button button, bool on)
            {
                button.GetComponent<Image>().color = on ? UIKit.OkColor : new Color(0.45f, 0.2f, 0.3f, 1f);
                button.GetComponentInChildren<Text>().text = on ? Loc.T("ВКЛ", "ON") : Loc.T("ВЫКЛ", "OFF");
            }
        }

        void ChoiceRow(ref float y, LS label, LS[] options, int index, Action<int> onChange)
        {
            RowLabel(y, label);
            int current = Mathf.Clamp(index, 0, options.Length - 1);
            var valueText = UIKit.Text(content, "Choice", options[current], 28, TextAnchor.MiddleCenter, UIKit.Accent, true);
            UIKit.Place(valueText.rectTransform, new Vector2(1f, 1f), new Vector2(-90f, y), new Vector2(260f, 64f), new Vector2(1f, 1f));

            void Change(int delta)
            {
                current = (current + delta + options.Length) % options.Length;
                valueText.text = options[current];
                onChange(current);
            }

            var left = UIKit.Button(content, "Prev", "<", new Color(0.22f, 0.17f, 0.34f, 1f), () => Change(-1), 26);
            UIKit.Place(left.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-360f, y - 6f), new Vector2(64f, 52f), new Vector2(1f, 1f));
            var right = UIKit.Button(content, "Next", ">", new Color(0.22f, 0.17f, 0.34f, 1f), () => Change(1), 26);
            UIKit.Place(right.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-20f, y - 6f), new Vector2(64f, 52f), new Vector2(1f, 1f));
            y -= 76f;
        }

        // ---------- Переназначение клавиш ----------

        void StartRebind(GameAction action, Text label)
        {
            rebinding = action;
            rebindingLabel = label;
            label.text = Loc.T("Нажми…", "Press…");
            label.color = UIKit.Accent;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (rebinding.HasValue)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    ClosedByEscFrame = Time.frameCount;
                    rebinding = null;
                    SelectTab(Tab.Controls);
                    return;
                }
                var key = Controls.PressedKey();
                if (key != Key.None)
                {
                    Controls.Set(rebinding.Value, key);
                    rebinding = null;
                    SoundFX.Play(SoundFX.Sfx.Click, 0.6f);
                    SelectTab(Tab.Controls);
                }
                return;
            }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                ClosedByEscFrame = Time.frameCount;
                Close();
            }
        }

        void Close()
        {
            GameSettings.Save();
            var cb = onClose;
            Destroy(gameObject);
            current = null;
            cb?.Invoke();
        }
    }
}
