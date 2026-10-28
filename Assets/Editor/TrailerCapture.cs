using System;
using System.IO;
using System.Reflection;
using BrainrotWaves.Trailer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BrainrotWaves.EditorTools
{
    /// <summary>
    /// Запись дублей для промо-ролика. Каждый дубль описан JSON-файлом (TrailerShot):
    /// RunShot открывает нужную сцену, ставит размер окна Game, кладёт описание
    /// в Logs/Trailer/current_shot.json и входит в Play Mode. Дальше работает TrailerDirector,
    /// он сам выходит из Play Mode и пишет done.json рядом с кадрами.
    /// Вызывается из консоли через unity CLI. Для разработчика, в сборку не попадает.
    /// </summary>
    public static class TrailerCapture
    {
        const string BackupPath = "Logs/Trailer/prefs_backup.json";

        [Serializable]
        class PrefsBackup
        {
            public string metaKey;
            public string metaValue;
            public bool metaExists;
            public string langKey;
            public int langValue;
            public bool langExists;
        }

        /// <summary>Запустить дубль. Возвращает "started" или текст ошибки.</summary>
        public static string RunShot(string shotJsonPath)
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) return "busy";
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
                if (EditorSceneManager.GetSceneAt(i).isDirty) return "unsaved scene: " + EditorSceneManager.GetSceneAt(i).path;

            string json = File.ReadAllText(shotJsonPath);
            var shot = JsonUtility.FromJson<TrailerShot>(json);
            BackupPrefs();

            string scene = shot.scene == "Game" ? "Assets/Scenes/Game.unity" : "Assets/Scenes/MainMenu.unity";
            if (EditorSceneManager.GetActiveScene().path != scene)
                EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);

            if (shot.scene != "Portraits") SetGameViewSize(shot.width, shot.height);
            Directory.CreateDirectory(Path.GetDirectoryName(TrailerShot.ConfigPath));
            File.WriteAllText(TrailerShot.ConfigPath, json);
            EditorApplication.isPlaying = true;
            return "started";
        }

        /// <summary>Сохранить прогресс и язык игрока до записи (меню и игра их трогают).</summary>
        static void BackupPrefs()
        {
            if (File.Exists(BackupPath)) return;
            var b = new PrefsBackup
            {
                metaKey = ConstOf(typeof(Meta), "Key"),
                langKey = ConstOf(typeof(Loc), "Key"),
            };
            b.metaExists = PlayerPrefs.HasKey(b.metaKey);
            b.metaValue = PlayerPrefs.GetString(b.metaKey, "");
            b.langExists = PlayerPrefs.HasKey(b.langKey);
            b.langValue = PlayerPrefs.GetInt(b.langKey, 0);
            Directory.CreateDirectory(Path.GetDirectoryName(BackupPath));
            File.WriteAllText(BackupPath, JsonUtility.ToJson(b, true));
        }

        /// <summary>Вернуть прогресс и язык игрока после записи всех дублей.</summary>
        public static string RestorePrefs()
        {
            if (!File.Exists(BackupPath)) return "no backup";
            var b = JsonUtility.FromJson<PrefsBackup>(File.ReadAllText(BackupPath));
            if (b.metaExists) PlayerPrefs.SetString(b.metaKey, b.metaValue); else PlayerPrefs.DeleteKey(b.metaKey);
            if (b.langExists) PlayerPrefs.SetInt(b.langKey, b.langValue); else PlayerPrefs.DeleteKey(b.langKey);
            PlayerPrefs.Save();
            File.Delete(BackupPath);
            return "restored";
        }

        static string ConstOf(Type t, string name)
        {
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return f == null ? null : (string)(f.IsLiteral ? f.GetRawConstantValue() : f.GetValue(null));
        }

        // ---------- Размер окна Game ----------

        public static string SetGameViewSize(int width, int height)
        {
            var asm = typeof(Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleton.GetProperty("instance").GetValue(null);
            var groupType = (GameViewSizeGroupType)sizesType.GetProperty("currentGroupType").GetValue(instance);
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { (int)groupType });
            var groupT = group.GetType();
            int total = (int)groupT.GetMethod("GetTotalCount").Invoke(group, null);
            int index = -1;
            for (int i = 0; i < total; i++)
            {
                var size = groupT.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                var st = size.GetType();
                if ((int)st.GetProperty("width").GetValue(size) == width && (int)st.GetProperty("height").GetValue(size) == height &&
                    st.GetProperty("sizeType").GetValue(size).ToString() == "FixedResolution")
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                var sizeT = asm.GetType("UnityEditor.GameViewSize");
                var sizeTypeT = asm.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeT.GetConstructor(new[] { sizeTypeT, typeof(int), typeof(int), typeof(string) });
                var newSize = ctor.Invoke(new object[] { Enum.Parse(sizeTypeT, "FixedResolution"), width, height, "Trailer " + width + "x" + height });
                groupT.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                index = total;
            }
            var gameViewT = asm.GetType("UnityEditor.GameView");
            var gv = EditorWindow.GetWindow(gameViewT, false, null, false);
            var prop = gameViewT.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            prop.SetValue(gv, index);
            var lowRes = gameViewT.GetProperty("lowResolutionForAspectRatios", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (lowRes != null && lowRes.CanWrite) lowRes.SetValue(gv, false);
            gv.Repaint();
            return "game view " + width + "x" + height + " (index " + index + ")";
        }
    }
}
