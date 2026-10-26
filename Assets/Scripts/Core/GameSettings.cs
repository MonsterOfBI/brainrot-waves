using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BrainrotWaves
{
    /// <summary>Настройки игрока: звук, графика, управление. Хранятся в PlayerPrefs.</summary>
    public static class GameSettings
    {
        public static float MasterVolume = 0.8f;
        public static float MusicVolume = 0.6f;
        public static float SfxVolume = 1f;
        /// <summary>0 — низкое, 1 — среднее, 2 — высокое.</summary>
        public static int Quality = 2;
        public static bool PostProcessing = true;
        public static bool Fullscreen = true;
        public static bool VSync = true;
        public static float Fov = 75f;
        public static float ScreenShake = 1f;
        public static bool DamageNumbers = true;
        public static float Sensitivity = 0.1f;
        public static bool InvertY;

        public static event Action Changed;

        static bool loaded;
        static UniversalRenderPipelineAsset runtimeAsset;

        public static float ParticleScale => Quality == 0 ? 0.4f : Quality == 1 ? 0.7f : 1f;
        public static LightShadows SunShadows => Quality == 0 ? LightShadows.Hard : LightShadows.Soft;

        public static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            MasterVolume = PlayerPrefs.GetFloat("BW_VolMaster", 0.8f);
            MusicVolume = PlayerPrefs.GetFloat("BW_VolMusic", 0.6f);
            SfxVolume = PlayerPrefs.GetFloat("BW_VolSfx", 1f);
            Quality = PlayerPrefs.GetInt("BW_Quality", Application.isMobilePlatform ? 1 : 2);
            PostProcessing = PlayerPrefs.GetInt("BW_Post", 1) == 1;
            Fullscreen = PlayerPrefs.GetInt("BW_Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            VSync = PlayerPrefs.GetInt("BW_VSync", 1) == 1;
            Fov = PlayerPrefs.GetFloat("BW_Fov", 75f);
            ScreenShake = PlayerPrefs.GetFloat("BW_Shake", 1f);
            DamageNumbers = PlayerPrefs.GetInt("BW_DmgNumbers", 1) == 1;
            Sensitivity = PlayerPrefs.GetFloat("BW_Sens", 0.1f);
            InvertY = PlayerPrefs.GetInt("BW_InvertY", 0) == 1;
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("BW_VolMaster", MasterVolume);
            PlayerPrefs.SetFloat("BW_VolMusic", MusicVolume);
            PlayerPrefs.SetFloat("BW_VolSfx", SfxVolume);
            PlayerPrefs.SetInt("BW_Quality", Quality);
            PlayerPrefs.SetInt("BW_Post", PostProcessing ? 1 : 0);
            PlayerPrefs.SetInt("BW_Fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("BW_VSync", VSync ? 1 : 0);
            PlayerPrefs.SetFloat("BW_Fov", Fov);
            PlayerPrefs.SetFloat("BW_Shake", ScreenShake);
            PlayerPrefs.SetInt("BW_DmgNumbers", DamageNumbers ? 1 : 0);
            PlayerPrefs.SetFloat("BW_Sens", Sensitivity);
            PlayerPrefs.SetInt("BW_InvertY", InvertY ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        /// <summary>Применяет всё сразу: громкость, графику, экран.</summary>
        public static void Apply()
        {
            SoundFX.ApplyVolumes();
            PlayerController.MouseSensitivity = Sensitivity;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            if (!VSync) Application.targetFrameRate = 144;
            if (!Application.isEditor && Screen.fullScreen != Fullscreen) Screen.fullScreen = Fullscreen;
            ApplyRenderPipeline();
            Changed?.Invoke();
        }

        static void ApplyRenderPipeline()
        {
            // В редакторе файл настроек не трогаем (иначе изменения сохранятся в проект).
            // В собранной игре меняем настройки на лету.
            if (Application.isEditor) return;
            runtimeAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (runtimeAsset == null) runtimeAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (runtimeAsset == null) return;

            switch (Quality)
            {
                case 0:
                    runtimeAsset.shadowDistance = 25f;
                    runtimeAsset.shadowCascadeCount = 1;
                    runtimeAsset.renderScale = 0.8f;
                    runtimeAsset.msaaSampleCount = 1;
                    break;
                case 1:
                    runtimeAsset.shadowDistance = 45f;
                    runtimeAsset.shadowCascadeCount = 2;
                    runtimeAsset.renderScale = 1f;
                    runtimeAsset.msaaSampleCount = 1;
                    break;
                default:
                    runtimeAsset.shadowDistance = 70f;
                    runtimeAsset.shadowCascadeCount = 4;
                    runtimeAsset.renderScale = 1f;
                    runtimeAsset.msaaSampleCount = 2;
                    break;
            }
        }

        /// <summary>Настраивает камеру: пост-обработка и сглаживание.</summary>
        public static void SetupCamera(Camera cam)
        {
            if (cam == null) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null) return;
            data.renderPostProcessing = PostProcessing;
            data.antialiasing = Quality == 0 ? AntialiasingMode.FastApproximateAntialiasing : AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = Quality == 2 ? AntialiasingQuality.High : AntialiasingQuality.Medium;
            data.renderShadows = true;
        }

        public static LS QualityName(int q)
        {
            switch (q)
            {
                case 0: return new LS("Низкое", "Low");
                case 1: return new LS("Среднее", "Medium");
                default: return new LS("Высокое", "High");
            }
        }
    }

    public enum GameAction { Forward, Back, Left, Right, Jump, Sprint, Reload, Ability, Weapon1, Weapon2, Weapon3 }

    /// <summary>Переназначаемые клавиши. ЛКМ — выстрел, Esc — пауза (не меняются).</summary>
    public static class Controls
    {
        static readonly Key[] Defaults =
        {
            Key.W, Key.S, Key.A, Key.D, Key.Space, Key.LeftShift, Key.R, Key.Q, Key.Digit1, Key.Digit2, Key.Digit3
        };

        static Key[] keys;

        public static int Count => Defaults.Length;

        static void EnsureLoaded()
        {
            if (keys != null) return;
            keys = new Key[Defaults.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                int v = PlayerPrefs.GetInt("BW_Key_" + i, (int)Defaults[i]);
                keys[i] = Enum.IsDefined(typeof(Key), v) && v != (int)Key.None ? (Key)v : Defaults[i];
            }
        }

        public static Key Get(GameAction a)
        {
            EnsureLoaded();
            return keys[(int)a];
        }

        /// <summary>Назначает клавишу. Если она занята другим действием — меняет их местами.</summary>
        public static void Set(GameAction a, Key key)
        {
            EnsureLoaded();
            if (key == Key.None || key == Key.Escape) return;
            for (int i = 0; i < keys.Length; i++)
                if (keys[i] == key && i != (int)a) keys[i] = keys[(int)a];
            keys[(int)a] = key;
            Save();
        }

        public static void ResetDefaults()
        {
            keys = (Key[])Defaults.Clone();
            Save();
        }

        static void Save()
        {
            for (int i = 0; i < keys.Length; i++) PlayerPrefs.SetInt("BW_Key_" + i, (int)keys[i]);
            PlayerPrefs.Save();
        }

        static UnityEngine.InputSystem.Controls.KeyControl Control(GameAction a)
        {
            var kb = Keyboard.current;
            if (kb == null) return null;
            var key = Get(a);
            if (key == Key.None) return null;
            try { return kb[key]; }
            catch { return null; }
        }

        public static bool Held(GameAction a)
        {
            var c = Control(a);
            return c != null && c.isPressed;
        }

        public static bool Down(GameAction a)
        {
            var c = Control(a);
            return c != null && c.wasPressedThisFrame;
        }

        public static LS Name(GameAction a)
        {
            switch (a)
            {
                case GameAction.Forward: return new LS("Вперёд", "Forward");
                case GameAction.Back: return new LS("Назад", "Back");
                case GameAction.Left: return new LS("Влево", "Left");
                case GameAction.Right: return new LS("Вправо", "Right");
                case GameAction.Jump: return new LS("Прыжок", "Jump");
                case GameAction.Sprint: return new LS("Бег", "Sprint");
                case GameAction.Reload: return new LS("Перезарядка", "Reload");
                case GameAction.Ability: return new LS("Способность", "Ability");
                case GameAction.Weapon1: return new LS("Оружие 1", "Weapon 1");
                case GameAction.Weapon2: return new LS("Оружие 2", "Weapon 2");
                default: return new LS("Оружие 3", "Weapon 3");
            }
        }

        public static string KeyName(Key k)
        {
            switch (k)
            {
                case Key.Space: return Loc.T("Пробел", "Space");
                case Key.LeftShift: return "L-Shift";
                case Key.RightShift: return "R-Shift";
                case Key.LeftCtrl: return "L-Ctrl";
                case Key.RightCtrl: return "R-Ctrl";
                case Key.LeftAlt: return "L-Alt";
                case Key.RightAlt: return "R-Alt";
                case Key.Tab: return "Tab";
                case Key.Enter: return "Enter";
                case Key.Backspace: return "Backspace";
                case Key.UpArrow: return "↑";
                case Key.DownArrow: return "↓";
                case Key.LeftArrow: return "←";
                case Key.RightArrow: return "→";
            }
            string s = k.ToString();
            if (s.StartsWith("Digit")) return s.Substring(5);
            if (s.StartsWith("Numpad")) return "Num " + s.Substring(6);
            return s;
        }

        public static string KeyName(GameAction a) => KeyName(Get(a));

        /// <summary>Какая клавиша нажата в этом кадре (для переназначения).</summary>
        public static Key PressedKey()
        {
            var kb = Keyboard.current;
            if (kb == null) return Key.None;
            foreach (var key in kb.allKeys)
                if (key != null && key.wasPressedThisFrame) return key.keyCode;
            return Key.None;
        }
    }
}
