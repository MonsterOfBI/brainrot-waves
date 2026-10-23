using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrainrotWaves.EditorTools
{
    /// <summary>
    /// Собирает сцену арены автоматически при первом открытии проекта.
    /// Пересобрать вручную: меню Brainrot Waves → Пересобрать арену.
    /// </summary>
    [InitializeOnLoad]
    public static class ArenaBuilder
    {
        const string ScenePath = "Assets/Scenes/Arena.unity";
        const string MaterialsFolder = "Assets/Materials";

        static ArenaBuilder()
        {
            EditorApplication.delayCall += AutoBuildOnce;
        }

        static void AutoBuildOnce()
        {
            if (SessionState.GetBool("BrainrotWaves_ArenaChecked", false)) return;
            SessionState.SetBool("BrainrotWaves_ArenaChecked", true);
            if (!File.Exists(ScenePath)) Build();
        }

        [MenuItem("Brainrot Waves/Пересобрать арену")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets", "Materials");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Камеру по умолчанию убираем: у игрока будет своя
            var defaultCamera = GameObject.Find("Main Camera");
            if (defaultCamera != null) Object.DestroyImmediate(defaultCamera);

            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                sun.intensity = 1.3f;
                sun.color = new Color(1f, 0.96f, 0.88f);
                sun.shadows = LightShadows.Soft;
            }

            // Яркие мультяшные материалы
            Material grass = GetMaterial("Grass", new Color(0.45f, 0.8f, 0.35f));
            Material wall = GetMaterial("Wall", new Color(1f, 0.72f, 0.4f));
            Material pink = GetMaterial("CoverPink", new Color(1f, 0.45f, 0.7f));
            Material cyan = GetMaterial("CoverCyan", new Color(0.3f, 0.85f, 0.95f));
            Material yellow = GetMaterial("CoverYellow", new Color(1f, 0.9f, 0.3f));
            Material gate = GetMaterial("Gate", new Color(0.6f, 0.3f, 0.9f));
            Material gunDark = GetMaterial("GunDark", new Color(0.18f, 0.18f, 0.22f));
            Material gunAccent = GetMaterial("GunAccent", new Color(1f, 0.5f, 0.15f));
            Material enemyNormal = GetMaterial("EnemyNormal", new Color(0.35f, 0.9f, 0.4f));
            Material enemyFast = GetMaterial("EnemyFast", new Color(1f, 0.85f, 0.2f));
            Material enemyTank = GetMaterial("EnemyTank", new Color(0.6f, 0.35f, 0.95f));
            Material eye = GetMaterial("EnemyEye", Color.white);
            Material pupil = GetMaterial("EnemyPupil", new Color(0.05f, 0.05f, 0.05f));
            Material tracer = GetMaterial("Tracer", new Color(1f, 0.95f, 0.4f), unlit: true);

            // ---------- Арена ----------
            var arena = new GameObject("Arena");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(arena.transform);
            ground.transform.localScale = new Vector3(6f, 1f, 6f); // 60 x 60 метров
            ground.GetComponent<Renderer>().sharedMaterial = grass;

            const float half = 30f;
            CreateBox(arena.transform, "Wall_North", new Vector3(0f, 2f, half), new Vector3(61f, 4f, 1f), wall);
            CreateBox(arena.transform, "Wall_South", new Vector3(0f, 2f, -half), new Vector3(61f, 4f, 1f), wall);
            CreateBox(arena.transform, "Wall_East", new Vector3(half, 2f, 0f), new Vector3(1f, 4f, 61f), wall);
            CreateBox(arena.transform, "Wall_West", new Vector3(-half, 2f, 0f), new Vector3(1f, 4f, 61f), wall);

            // Укрытия
            CreateBox(arena.transform, "Cover", new Vector3(8f, 1f, 8f), new Vector3(3f, 2f, 3f), pink);
            CreateBox(arena.transform, "Cover", new Vector3(-9f, 0.75f, 6f), new Vector3(4f, 1.5f, 1.5f), cyan);
            CreateBox(arena.transform, "Cover", new Vector3(-7f, 1.25f, -10f), new Vector3(2.5f, 2.5f, 2.5f), yellow);
            CreateBox(arena.transform, "Cover", new Vector3(10f, 0.75f, -7f), new Vector3(1.5f, 1.5f, 5f), pink);
            CreateBox(arena.transform, "Cover", new Vector3(0f, 1f, 15f), new Vector3(6f, 2f, 1.5f), cyan);
            CreateBox(arena.transform, "Cover", new Vector3(0f, 1f, -16f), new Vector3(6f, 2f, 1.5f), yellow);
            CreateBox(arena.transform, "Cover", new Vector3(17f, 1.5f, 2f), new Vector3(2f, 3f, 2f), yellow);
            CreateBox(arena.transform, "Cover", new Vector3(-17f, 1.5f, -2f), new Vector3(2f, 3f, 2f), pink);

            // Точки появления врагов («ворота» в углах)
            var spawnRoot = new GameObject("SpawnPoints");
            var spawnPoints = new List<Transform>();
            Vector3[] corners =
            {
                new Vector3(24f, 0f, 24f), new Vector3(-24f, 0f, 24f),
                new Vector3(24f, 0f, -24f), new Vector3(-24f, 0f, -24f)
            };
            for (int i = 0; i < corners.Length; i++)
            {
                var point = new GameObject("Spawn_" + (i + 1));
                point.transform.SetParent(spawnRoot.transform);
                point.transform.position = corners[i];
                spawnPoints.Add(point.transform);

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "GateMarker";
                Object.DestroyImmediate(marker.GetComponent<Collider>());
                marker.transform.SetParent(point.transform, false);
                marker.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                marker.transform.localScale = new Vector3(4f, 0.02f, 4f);
                marker.GetComponent<Renderer>().sharedMaterial = gate;
            }

            // ---------- Игрок ----------
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0.1f, 0f);
            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.4f;
            var playerHealth = player.AddComponent<Health>();
            playerHealth.maxHealth = 100f;

            var cameraObject = new GameObject("PlayerCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.05f;
            cameraObject.AddComponent<AudioListener>();

            var playerController = player.AddComponent<PlayerController>();
            playerController.playerCamera = camera;

            // Пистолет из простых фигур (позже заменим на модель)
            var gunRoot = new GameObject("Pistol");
            gunRoot.transform.SetParent(cameraObject.transform, false);
            gunRoot.transform.localPosition = new Vector3(0.25f, -0.22f, 0.45f);
            var gunModel = new GameObject("Model");
            gunModel.transform.SetParent(gunRoot.transform, false);
            CreateVisual(gunModel.transform, "Body", new Vector3(0f, 0f, 0f), new Vector3(0.08f, 0.1f, 0.3f), gunDark);
            CreateVisual(gunModel.transform, "Barrel", new Vector3(0f, 0.02f, 0.2f), new Vector3(0.05f, 0.05f, 0.15f), gunAccent);
            CreateVisual(gunModel.transform, "Grip", new Vector3(0f, -0.1f, -0.08f), new Vector3(0.07f, 0.16f, 0.08f), gunAccent);
            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(gunModel.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0.02f, 0.3f);

            var weapon = gunRoot.AddComponent<Weapon>();
            weapon.playerCamera = camera;
            weapon.weaponName = "Пистолет";
            weapon.damage = 25f;
            weapon.fireRate = 4f;
            weapon.automatic = false;
            weapon.model = gunModel.transform;
            weapon.muzzle = muzzle.transform;
            weapon.tracerMaterial = tracer;

            // ---------- Волны и интерфейс ----------
            var waveObject = new GameObject("WaveManager");
            var waves = waveObject.AddComponent<WaveManager>();
            waves.player = player.transform;
            waves.spawnPoints = spawnPoints.ToArray();
            waves.normalMaterial = enemyNormal;
            waves.fastMaterial = enemyFast;
            waves.tankMaterial = enemyTank;
            waves.eyeMaterial = eye;
            waves.pupilMaterial = pupil;

            var uiObject = new GameObject("GameUI");
            var ui = uiObject.AddComponent<GameUI>();
            ui.playerHealth = playerHealth;
            ui.waves = waves;
            ui.weapon = weapon;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();

            Debug.Log("Brainrot Waves: арена собрана. Нажмите Play (▶) сверху, чтобы играть!");
        }

        static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
            box.isStatic = true;
            return box;
        }

        static void CreateVisual(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = size;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Material GetMaterial(string name, Color color, bool unlit = false)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            string shaderName = unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit";
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            if (!unlit && material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.2f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
