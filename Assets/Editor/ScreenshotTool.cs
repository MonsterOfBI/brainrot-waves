using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BrainrotWaves.EditorTools
{
    /// <summary>Делает снимки моделей и арен в папку Logs/Shots (для проверки внешнего вида).</summary>
    public static class ScreenshotTool
    {
        const string Folder = "Logs/Shots";

        [MenuItem("Brainrot Waves/Снимки моделей и арен (для разработчика)")]
        public static void Run()
        {
            Directory.CreateDirectory(Folder);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var sunObject = new GameObject("Sun");
            var sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sunObject.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            var camObject = new GameObject("Cam");
            var cam = camObject.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameAssets.Hex("#3B2063");
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = GameAssets.Hex("#B39DDB");
            RenderSettings.ambientEquatorColor = GameAssets.Hex("#7E57C2");
            RenderSettings.ambientGroundColor = GameAssets.Hex("#311B92");
            RenderSettings.fog = false;

            // Модели: каждая отдельно, спереди-сбоку
            foreach (var info in Brainrots.All)
            {
                var holder = new GameObject("Holder").transform;
                holder.rotation = Quaternion.Euler(0f, 205f, 0f);
                var rig = BrainrotModels.Build(info.id, holder);
                float scale = info.id == BrainrotId.Bombardiro ? 0.7f : 1f;
                rig.transform.localScale = Vector3.one * scale;
                cam.transform.position = new Vector3(0f, 1.4f, -5.2f);
                cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
                Shoot(cam, "model_" + info.id, 900, 900);
                holder.rotation = Quaternion.Euler(0f, 120f, 0f);
                Shoot(cam, "model_" + info.id + "_side", 900, 900);
                Object.DestroyImmediate(holder.gameObject);
            }

            // Арены
            foreach (ArenaTheme theme in System.Enum.GetValues(typeof(ArenaTheme)))
            {
                var gen = new ArenaGenerator(GameManager.ArenaHalfSize);
                gen.Build(theme, sun, cam);
                // Несколько врагов для масштаба
                var demo = new GameObject("Demo").transform;
                int i = 0;
                foreach (var info in Brainrots.All)
                {
                    var h = new GameObject("E").transform;
                    h.SetParent(demo, false);
                    h.position = new Vector3(-6f + i * 2.4f, info.behavior == EnemyBehavior.Flyer ? 3f : 0f, 6f);
                    h.rotation = Quaternion.Euler(0f, 180f, 0f);
                    BrainrotModels.Build(info.id, h);
                    i++;
                }
                cam.fieldOfView = 75f;
                cam.transform.position = new Vector3(0f, 1.7f, -6f);
                cam.transform.rotation = Quaternion.Euler(3f, 0f, 0f);
                Shoot(cam, "arena_" + theme + "_eye", 1280, 720);
                cam.transform.position = new Vector3(0f, 38f, -46f);
                cam.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
                Shoot(cam, "arena_" + theme + "_top", 1280, 720);
                Object.DestroyImmediate(demo.gameObject);
                Object.DestroyImmediate(gen.Root.gameObject);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.fieldOfView = 40f;
            }
            Debug.Log("Screenshots saved to " + Path.GetFullPath(Folder));
        }

        static void Shoot(Camera cam, string name, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }
}
