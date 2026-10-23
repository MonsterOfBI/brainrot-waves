using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrainrotWaves.EditorTools
{
    /// <summary>
    /// Настраивает проект: базовые материалы, слои, сцены «MainMenu» и «Game».
    /// Запускается сам при первом открытии проекта. Повторить: меню Brainrot Waves → Настроить проект.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectBuilder
    {
        const string AssetsPath = "Assets/Resources/GameAssets.asset";
        const string MenuScene = "Assets/Scenes/MainMenu.unity";
        const string GameScene = "Assets/Scenes/Game.unity";
        const string MaterialsFolder = "Assets/Materials";

        static ProjectBuilder()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (Application.isBatchMode) return;
            if (SessionState.GetBool("BW_SetupChecked", false)) return;
            SessionState.SetBool("BW_SetupChecked", true);
            if (!File.Exists(AssetsPath) || !File.Exists(MenuScene) || !File.Exists(GameScene))
            {
                Setup();
                EditorSceneManager.OpenScene(MenuScene);
            }
        }

        [MenuItem("Brainrot Waves/Настроить проект")]
        public static void Setup()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Materials");

            SetupLayers();
            CreateGameAssets();
            CreateScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("Brainrot Waves: проект настроен. Откройте сцену MainMenu и нажмите Play.");
        }

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        static void SetupLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(8).stringValue = "Enemy";
            layers.GetArrayElementAtIndex(9).stringValue = "Player";
            tagManager.ApplyModifiedProperties();
        }

        static Material SaveMaterial(string name, Material material)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = material.shader;
                existing.CopyPropertiesFromMaterial(material);
                existing.shaderKeywords = material.shaderKeywords;
                existing.renderQueue = material.renderQueue;
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void CreateGameAssets()
        {
            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            var unlitShader = Shader.Find("Universal Render Pipeline/Unlit");

            var lit = new Material(litShader);
            lit.SetFloat("_Smoothness", 0.3f);
            lit = SaveMaterial("BaseLit", lit);

            var emissive = new Material(litShader);
            emissive.EnableKeyword("_EMISSION");
            emissive.SetColor("_EmissionColor", Color.white);
            emissive.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            emissive = SaveMaterial("BaseLitEmissive", emissive);

            var transparent = new Material(litShader);
            transparent.SetFloat("_Surface", 1f);
            transparent.SetFloat("_Blend", 0f);
            transparent.SetOverrideTag("RenderType", "Transparent");
            transparent.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            transparent.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            transparent.SetFloat("_ZWrite", 0f);
            transparent.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            transparent.renderQueue = (int)RenderQueue.Transparent;
            transparent = SaveMaterial("BaseLitTransparent", transparent);

            var unlit = SaveMaterial("BaseUnlit", new Material(unlitShader));

            Material skybox = null;
            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_SunDisk", 2f);
                sky.SetFloat("_SunSize", 0.05f);
                sky.SetFloat("_AtmosphereThickness", 1f);
                sky.SetFloat("_Exposure", 1.2f);
                skybox = SaveMaterial("Sky", sky);
            }

            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);
            if (assets == null)
            {
                assets = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(assets, AssetsPath);
            }
            assets.lit = lit;
            assets.litEmissive = emissive;
            assets.litTransparent = transparent;
            assets.unlit = unlit;
            assets.skybox = skybox;
            EditorUtility.SetDirty(assets);
        }

        static void CreateScenes()
        {
            var menu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("MainMenu").AddComponent<MainMenu>();
            EditorSceneManager.SaveScene(menu, MenuScene);

            var game = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameManager").AddComponent<GameManager>();
            var assets = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);
            if (assets != null && assets.skybox != null) RenderSettings.skybox = assets.skybox;
            EditorSceneManager.SaveScene(game, GameScene);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(GameScene, true)
            };
        }
    }
}
