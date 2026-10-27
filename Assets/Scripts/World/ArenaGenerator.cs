using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrainrotWaves
{
    /// <summary>
    /// Строит тематическую арену из простых фигур: пляж, лес, джунгли, пустыня, город, ночная деревня.
    /// </summary>
    public partial class ArenaGenerator
    {
        public Transform Root { get; private set; }
        public readonly List<Vector3> spawnPoints = new List<Vector3>();
        public Vector3 PlayerSpawn => new Vector3(0f, 0.1f, -6f);

        readonly float half;
        System.Random rng;
        readonly List<Vector2> occupied = new List<Vector2>();

        public ArenaGenerator(float halfSize)
        {
            half = halfSize;
        }

        // ---------- Вспомогательное ----------

        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        int RI(int a, int b) => rng.Next(a, b);

        Transform Group(string name, Vector3 position, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        /// <summary>Деталь с коллайдером (мешает пройти и стрелять сквозь).</summary>
        Transform Solid(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Vector3 euler = default, float smooth = 0.25f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = GameAssets.Lit(color, smooth);
            if (type == PrimitiveType.Capsule || type == PrimitiveType.Cylinder || type == PrimitiveType.Sphere)
            {
                // Сферы и цилиндры оставляем как есть: их коллайдеры нормально работают для препятствий
            }
            return go.transform;
        }

        Transform Deco(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Vector3 euler = default, float smooth = 0.25f)
        {
            return BrainrotModels.Part(parent, type, pos, scale, color, euler, smooth);
        }

        Transform Glow(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, float intensity = 2f)
        {
            var t = BrainrotModels.Part(parent, type, pos, scale, color, default, 0.5f, false);
            t.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(color, intensity);
            return t;
        }

        void PointLight(Transform parent, Vector3 pos, Color color, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
        }

        bool IsFree(Vector2 p, float radius)
        {
            if (p.magnitude < 7f + radius) return false; // центр для игрока
            foreach (var s in spawnPoints)
                if (Vector2.Distance(p, new Vector2(s.x, s.z)) < 4.5f + radius) return false;
            foreach (var o in occupied)
                if (Vector2.Distance(p, o) < radius + 1.8f) return false;
            return true;
        }

        /// <summary>Подбирает свободное место для декорации внутри арены.</summary>
        bool FindSpot(float radius, out Vector3 position, float margin = 3f)
        {
            for (int i = 0; i < 40; i++)
            {
                var p = new Vector2(R(-half + margin, half - margin), R(-half + margin, half - margin));
                if (!IsFree(p, radius)) continue;
                occupied.Add(p);
                position = new Vector3(p.x, 0f, p.y);
                return true;
            }
            position = Vector3.zero;
            return false;
        }

        // ---------- Сборка ----------

        public void Build(ArenaTheme theme, Light sun, Camera cam)
        {
            rng = new System.Random(1000 + (int)theme * 31);
            Root = new GameObject("Arena_" + theme).transform;
            occupied.Clear();
            spawnPoints.Clear();

            float g = half - 5f;
            spawnPoints.Add(new Vector3(g, 0f, g));
            spawnPoints.Add(new Vector3(-g, 0f, g));
            spawnPoints.Add(new Vector3(g, 0f, -g));
            spawnPoints.Add(new Vector3(-g, 0f, -g));
            spawnPoints.Add(new Vector3(0f, 0f, half - 4f));
            spawnPoints.Add(new Vector3(0f, 0f, -half + 4f));
            spawnPoints.Add(new Vector3(half - 4f, 0f, 0f));
            spawnPoints.Add(new Vector3(-half + 4f, 0f, 0f));

            switch (theme)
            {
                case ArenaTheme.Beach: BuildBeach(); break;
                case ArenaTheme.Forest: BuildForest(); break;
                case ArenaTheme.Jungle: BuildJungle(); break;
                case ArenaTheme.Desert: BuildDesert(); break;
                case ArenaTheme.City: BuildCity(); break;
                default: BuildNightVillage(); break;
            }

            AddKitDecor(theme);
            BuildPortals(theme);
            ApplyLighting(theme, sun, cam);

            var surface = Root.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = Combat.EnvironmentMask;
            surface.BuildNavMesh();
        }

        public void Destroy()
        {
            if (Root != null) Object.Destroy(Root.gameObject);
        }

        void Ground(Color color, Color borderColor)
        {
            var ground = Solid(Root, PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), new Vector3(half * 2f + 2f, 1f, half * 2f + 2f), color);
            ground.name = "Ground";
            // Внешняя земля до горизонта (без коллайдера)
            Deco(Root, PrimitiveType.Cube, new Vector3(0f, -0.52f, 0f), new Vector3(400f, 1f, 400f), borderColor);
        }

        void Walls(Color color, float height = 3f)
        {
            var walls = Group("Walls", Vector3.zero);
            Solid(walls, PrimitiveType.Cube, new Vector3(0f, height / 2f, half + 0.5f), new Vector3(half * 2f + 2f, height, 1f), color);
            Solid(walls, PrimitiveType.Cube, new Vector3(0f, height / 2f, -half - 0.5f), new Vector3(half * 2f + 2f, height, 1f), color);
            Solid(walls, PrimitiveType.Cube, new Vector3(half + 0.5f, height / 2f, 0f), new Vector3(1f, height, half * 2f + 2f), color);
            Solid(walls, PrimitiveType.Cube, new Vector3(-half - 0.5f, height / 2f, 0f), new Vector3(1f, height, half * 2f + 2f), color);
        }

        /// <summary>Ворота, из которых выходят враги.</summary>
        void BuildPortals(ArenaTheme theme)
        {
            Color portal = GameAssets.Hex("#9C4DFF");
            foreach (var s in spawnPoints)
            {
                var p = Group("Portal", s, Mathf.Atan2(-s.x, -s.z) * Mathf.Rad2Deg);
                Glow(p, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(3.2f, 0.02f, 3.2f), portal, 1.2f);
                Deco(p, PrimitiveType.Cylinder, new Vector3(0f, 0.01f, 0f), new Vector3(3.6f, 0.02f, 3.6f), GameAssets.Hex("#2A1745"));
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f;
                    var q = Quaternion.Euler(0f, a, 0f);
                    Glow(p, PrimitiveType.Cube, q * new Vector3(0f, 0.3f, 1.75f), new Vector3(0.2f, 0.6f, 0.2f), portal, 2f);
                }
            }
        }

        void ApplyLighting(ArenaTheme theme, Light sun, Camera cam)
        {
            Color sunColor, sky, equator, groundAmbient, fog, skyTint;
            float sunIntensity, exposure, fogDensity, atmosphere;
            Vector3 sunEuler;

            switch (theme)
            {
                case ArenaTheme.Beach:
                    sunColor = GameAssets.Hex("#FFF4DC"); sunIntensity = 1.1f; sunEuler = new Vector3(55f, -40f, 0f);
                    sky = GameAssets.Hex("#8CC8F0"); equator = GameAssets.Hex("#A9CFE0"); groundAmbient = GameAssets.Hex("#9C8A66");
                    fog = GameAssets.Hex("#BDE6FF"); fogDensity = 0.006f; skyTint = GameAssets.Hex("#7FC4FF"); exposure = 1.4f; atmosphere = 0.8f;
                    break;
                case ArenaTheme.Forest:
                    sunColor = GameAssets.Hex("#FFF1C9"); sunIntensity = 1.2f; sunEuler = new Vector3(45f, 30f, 0f);
                    sky = GameAssets.Hex("#A9D7B0"); equator = GameAssets.Hex("#8DB88A"); groundAmbient = GameAssets.Hex("#4E6B3A");
                    fog = GameAssets.Hex("#B9D9B5"); fogDensity = 0.014f; skyTint = GameAssets.Hex("#9CCFA0"); exposure = 1.2f; atmosphere = 1f;
                    break;
                case ArenaTheme.Jungle:
                    sunColor = GameAssets.Hex("#FFF6B8"); sunIntensity = 1.3f; sunEuler = new Vector3(65f, 10f, 0f);
                    sky = GameAssets.Hex("#B6E3A1"); equator = GameAssets.Hex("#7FB069"); groundAmbient = GameAssets.Hex("#3D5A2A");
                    fog = GameAssets.Hex("#C7E5B0"); fogDensity = 0.018f; skyTint = GameAssets.Hex("#A8DCA0"); exposure = 1.25f; atmosphere = 1.3f;
                    break;
                case ArenaTheme.Desert:
                    sunColor = GameAssets.Hex("#FFE2B0"); sunIntensity = 1.7f; sunEuler = new Vector3(40f, -60f, 0f);
                    sky = GameAssets.Hex("#FFD9A0"); equator = GameAssets.Hex("#F2B880"); groundAmbient = GameAssets.Hex("#B07A45");
                    fog = GameAssets.Hex("#F5CFA0"); fogDensity = 0.008f; skyTint = GameAssets.Hex("#6FB7FF"); exposure = 1.3f; atmosphere = 1.3f;
                    break;
                case ArenaTheme.City:
                    sunColor = GameAssets.Hex("#FFB59E"); sunIntensity = 1.1f; sunEuler = new Vector3(18f, 70f, 0f);
                    sky = GameAssets.Hex("#B38BD9"); equator = GameAssets.Hex("#E39BA6"); groundAmbient = GameAssets.Hex("#4A4060");
                    fog = GameAssets.Hex("#C89BC9"); fogDensity = 0.01f; skyTint = GameAssets.Hex("#C77DFF"); exposure = 1.0f; atmosphere = 2f;
                    break;
                default:
                    // Ночь, но персонажей хорошо видно: яркая луна, светлее окружение, туман реже
                    sunColor = GameAssets.Hex("#B8C8FF"); sunIntensity = 1.05f; sunEuler = new Vector3(42f, 150f, 0f);
                    sky = GameAssets.Hex("#6878B8"); equator = GameAssets.Hex("#4A5A90"); groundAmbient = GameAssets.Hex("#2E3A5C");
                    fog = GameAssets.Hex("#2A3560"); fogDensity = 0.011f; skyTint = GameAssets.Hex("#1A2350"); exposure = 0.25f; atmosphere = 0.6f;
                    break;
            }

            if (sun != null)
            {
                sun.color = sunColor;
                sun.intensity = sunIntensity;
                sun.transform.rotation = Quaternion.Euler(sunEuler);
                sun.shadows = LightShadows.Soft;
                RenderSettings.sun = sun;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = groundAmbient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = fogDensity;

            if (GameAssets.I.skybox != null)
            {
                var skybox = new Material(GameAssets.I.skybox);
                if (skybox.HasProperty("_SkyTint")) skybox.SetColor("_SkyTint", skyTint);
                if (skybox.HasProperty("_GroundColor")) skybox.SetColor("_GroundColor", fog);
                if (skybox.HasProperty("_Exposure")) skybox.SetFloat("_Exposure", exposure);
                if (skybox.HasProperty("_AtmosphereThickness")) skybox.SetFloat("_AtmosphereThickness", atmosphere);
                if (skybox.HasProperty("_SunSize")) skybox.SetFloat("_SunSize", theme == ArenaTheme.NightVillage ? 0.02f : 0.05f);
                RenderSettings.skybox = skybox;
                if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
            }
            else if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = fog;
            }
        }

        // ---------- Пляж ----------

        void BuildBeach()
        {
            Color sand = GameAssets.Hex("#E0BE7A");
            Ground(sand, GameAssets.Hex("#E9CB8A"));

            // Море за северной стороной
            var sea = BrainrotModels.Part(Root, PrimitiveType.Cube, new Vector3(0f, -0.05f, half + 60f), new Vector3(400f, 0.2f, 110f), GameAssets.Hex("#2E9CDB"), default, 0.95f);
            sea.GetComponent<Renderer>().sharedMaterial = GameAssets.Lit(GameAssets.Hex("#2E9CDB"), 0.95f);
            Deco(Root, PrimitiveType.Cube, new Vector3(0f, -0.03f, half + 5f), new Vector3(400f, 0.2f, 3f), GameAssets.Hex("#E9F6FF"));

            // Ограда из камней и деревянного забора
            var walls = Group("Walls", Vector3.zero);
            Color rock = GameAssets.Hex("#9E9E9E");
            for (float x = -half; x <= half; x += 3f)
            {
                Solid(walls, PrimitiveType.Sphere, new Vector3(x + R(-0.5f, 0.5f), 0.6f, half + 0.8f), new Vector3(R(2.6f, 3.4f), R(1.6f, 2.4f), 2f), rock);
                Solid(walls, PrimitiveType.Cube, new Vector3(x, 0.9f, -half - 0.5f), new Vector3(0.2f, 1.8f, 0.2f), GameAssets.Hex("#A47148"));
            }
            for (float z = -half; z <= half; z += 3f)
            {
                Solid(walls, PrimitiveType.Sphere, new Vector3(half + 0.8f, 0.6f, z + R(-0.5f, 0.5f)), new Vector3(2f, R(1.6f, 2.4f), R(2.6f, 3.4f)), rock);
                Solid(walls, PrimitiveType.Sphere, new Vector3(-half - 0.8f, 0.6f, z + R(-0.5f, 0.5f)), new Vector3(2f, R(1.6f, 2.4f), R(2.6f, 3.4f)), rock);
            }
            Solid(walls, PrimitiveType.Cube, new Vector3(0f, 0.9f, -half - 0.5f), new Vector3(half * 2f, 0.25f, 0.15f), GameAssets.Hex("#C28F5C"));
            Solid(walls, PrimitiveType.Cube, new Vector3(0f, 1.5f, -half - 0.5f), new Vector3(half * 2f, 0.25f, 0.15f), GameAssets.Hex("#C28F5C"));
            InvisibleBounds();

            for (int i = 0; i < 9; i++) if (FindSpot(1.5f, out var p)) PalmTree(p);
            for (int i = 0; i < 5; i++) if (FindSpot(1.8f, out var p)) Umbrella(p);
            for (int i = 0; i < 4; i++) if (FindSpot(1.5f, out var p)) SandCastle(p);
            for (int i = 0; i < 4; i++) if (FindSpot(1.2f, out var p)) Lounger(p);
            for (int i = 0; i < 4; i++) if (FindSpot(1f, out var p)) Surfboard(p);
            for (int i = 0; i < 4; i++) if (FindSpot(0.8f, out var p)) BeachBall(p);
            // Ракушки и морские звёзды
            for (int i = 0; i < 25; i++)
                Deco(Root, PrimitiveType.Sphere, new Vector3(R(-half, half), 0.02f, R(-half, half)), new Vector3(0.3f, 0.05f, 0.3f), i % 2 == 0 ? GameAssets.Hex("#FF8A65") : GameAssets.Hex("#FFF3E0"));
        }

        /// <summary>Невидимые стены по краю арены.</summary>
        void InvisibleBounds()
        {
            var bounds = Group("Bounds", Vector3.zero);
            const float h = 6f;
            var parts = new[]
            {
                Solid(bounds, PrimitiveType.Cube, new Vector3(0f, h / 2f, half + 0.5f), new Vector3(half * 2f + 2f, h, 1f), Color.white),
                Solid(bounds, PrimitiveType.Cube, new Vector3(0f, h / 2f, -half - 0.5f), new Vector3(half * 2f + 2f, h, 1f), Color.white),
                Solid(bounds, PrimitiveType.Cube, new Vector3(half + 0.5f, h / 2f, 0f), new Vector3(1f, h, half * 2f + 2f), Color.white),
                Solid(bounds, PrimitiveType.Cube, new Vector3(-half - 0.5f, h / 2f, 0f), new Vector3(1f, h, half * 2f + 2f), Color.white)
            };
            foreach (var p in parts) p.GetComponent<Renderer>().enabled = false;
        }

        void PalmTree(Vector3 p)
        {
            var t = Group("Palm", p, R(0f, 360f));
            Color trunk = GameAssets.Hex("#A47148");
            float lean = R(5f, 15f);
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 0f), new Vector3(0.45f, 1.5f, 0.45f), trunk);
            Transform top = Group("Top", Vector3.zero);
            top.SetParent(t, false);
            top.localPosition = new Vector3(0f, 3f, 0f);
            top.localRotation = Quaternion.Euler(lean, 0f, 0f);
            for (int i = 0; i < 4; i++)
                Deco(top, PrimitiveType.Cylinder, new Vector3(0f, i * 0.75f + 0.35f, 0f), new Vector3(0.4f - i * 0.03f, 0.38f, 0.4f - i * 0.03f), i % 2 == 0 ? trunk : GameAssets.Hex("#8D5F3A"));
            var crown = Group("Crown", Vector3.zero);
            crown.SetParent(top, false);
            crown.localPosition = new Vector3(0f, 3.1f, 0f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * 360f / 7f;
                var leaf = Group("Leaf", Vector3.zero);
                leaf.SetParent(crown, false);
                leaf.localRotation = Quaternion.Euler(0f, a, 0f);
                Deco(leaf, PrimitiveType.Cube, new Vector3(0f, 0f, 1.2f), new Vector3(0.8f, 0.06f, 2.4f), GameAssets.Hex(i % 2 == 0 ? "#43A047" : "#66BB6A"), new Vector3(25f, 0f, 0f));
                Deco(leaf, PrimitiveType.Cube, new Vector3(0f, -0.75f, 2.6f), new Vector3(0.6f, 0.05f, 1f), GameAssets.Hex("#388E3C"), new Vector3(50f, 0f, 0f));
            }
            for (int i = 0; i < 3; i++)
                Deco(crown, PrimitiveType.Sphere, new Vector3(Mathf.Cos(i * 2.1f) * 0.3f, -0.25f, Mathf.Sin(i * 2.1f) * 0.3f), Vector3.one * 0.35f, GameAssets.Hex("#6D4C41"));
        }

        void Umbrella(Vector3 p)
        {
            var t = Group("Umbrella", p, R(0f, 360f));
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 1.3f, 0f), new Vector3(0.1f, 1.3f, 0.1f), Color.white);
            Color[] colors = { GameAssets.Hex("#FF5252"), GameAssets.Hex("#FFEB3B"), GameAssets.Hex("#40C4FF"), GameAssets.Hex("#69F0AE") };
            Color c = colors[RI(0, colors.Length)];
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 2.6f, 0f), new Vector3(3.2f, 0.7f, 3.2f), c);
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 2.62f, 0f), new Vector3(3.25f, 0.5f, 1f), Color.white);
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 2.62f, 0f), new Vector3(1f, 0.5f, 3.25f), Color.white);
            Deco(t, PrimitiveType.Cube, new Vector3(0.9f, 0.05f, 0.5f), new Vector3(1f, 0.04f, 2f), colors[RI(0, colors.Length)]);
        }

        void SandCastle(Vector3 p)
        {
            var t = Group("SandCastle", p, R(0f, 360f));
            Color s = GameAssets.Hex("#E6C27A");
            Solid(t, PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), new Vector3(2.2f, 1.2f, 2.2f), s);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * 1.1f, z = (i < 2 ? -1f : 1f) * 1.1f;
                Solid(t, PrimitiveType.Cylinder, new Vector3(x, 0.9f, z), new Vector3(0.7f, 0.9f, 0.7f), s);
                Deco(t, PrimitiveType.Cylinder, new Vector3(x, 1.85f, z), new Vector3(0.75f, 0.08f, 0.75f), GameAssets.Hex("#D9B064"));
            }
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0f), new Vector3(0.05f, 0.5f, 0.05f), Color.white);
            Deco(t, PrimitiveType.Cube, new Vector3(0.2f, 1.95f, 0f), new Vector3(0.4f, 0.25f, 0.02f), GameAssets.Hex("#FF5252"));
        }

        void Lounger(Vector3 p)
        {
            var t = Group("Lounger", p, R(0f, 360f));
            Solid(t, PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.12f, 1.8f), Color.white);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 0.65f, -0.75f), new Vector3(0.8f, 0.12f, 0.8f), Color.white, new Vector3(-50f, 0f, 0f));
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 0.43f, 0.1f), new Vector3(0.7f, 0.05f, 1.4f), GameAssets.Hex("#40C4FF"));
        }

        void Surfboard(Vector3 p)
        {
            var t = Group("Surfboard", p, R(0f, 360f));
            Color[] colors = { GameAssets.Hex("#FF7043"), GameAssets.Hex("#26C6DA"), GameAssets.Hex("#FFCA28") };
            Solid(t, PrimitiveType.Capsule, new Vector3(0f, 1.1f, 0f), new Vector3(0.7f, 1.2f, 0.15f), colors[RI(0, 3)], new Vector3(R(-10f, 10f), 0f, R(-10f, 10f)), 0.8f);
        }

        void BeachBall(Vector3 p)
        {
            var t = Group("BeachBall", p);
            Solid(t, PrimitiveType.Sphere, new Vector3(0f, 0.5f, 0f), Vector3.one, Color.white, default, 0.7f);
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 0.5f, 0f), new Vector3(1.02f, 1.02f, 0.4f), GameAssets.Hex("#FF5252"), default, 0.7f);
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 0.5f, 0f), new Vector3(0.4f, 1.02f, 1.02f), GameAssets.Hex("#448AFF"), default, 0.7f);
        }

        // ---------- Лес ----------

        void BuildForest()
        {
            Ground(GameAssets.Hex("#5E9E45"), GameAssets.Hex("#4F8A3A"));
            InvisibleBounds();

            // Стена из деревьев по краю
            for (float a = -half; a <= half; a += 3.2f)
            {
                RoundTree(new Vector3(a + R(-0.8f, 0.8f), 0f, half + R(1f, 3f)), R(1.1f, 1.5f));
                RoundTree(new Vector3(a + R(-0.8f, 0.8f), 0f, -half - R(1f, 3f)), R(1.1f, 1.5f));
                RoundTree(new Vector3(half + R(1f, 3f), 0f, a + R(-0.8f, 0.8f)), R(1.1f, 1.5f));
                RoundTree(new Vector3(-half - R(1f, 3f), 0f, a + R(-0.8f, 0.8f)), R(1.1f, 1.5f));
            }

            for (int i = 0; i < 12; i++) if (FindSpot(1.6f, out var p)) { if (i % 3 == 0) PineTree(p); else RoundTree(p, R(0.8f, 1.1f)); }
            for (int i = 0; i < 6; i++) if (FindSpot(1.4f, out var p)) Rock(p, GameAssets.Hex("#8A8F93"), R(1f, 1.8f));
            for (int i = 0; i < 4; i++) if (FindSpot(2.5f, out var p)) FallenLog(p);
            for (int i = 0; i < 10; i++) if (FindSpot(0.6f, out var p)) Mushroom(p, R(0.6f, 1.4f));
            for (int i = 0; i < 12; i++) if (FindSpot(0.8f, out var p)) Bush(p, GameAssets.Hex("#43A047"));
            Flowers(40);
        }

        void RoundTree(Vector3 p, float scale)
        {
            var t = Group("Tree", p, R(0f, 360f));
            t.localScale = Vector3.one * scale;
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 0f), new Vector3(0.6f, 1.5f, 0.6f), GameAssets.Hex("#6D4C41"));
            Color leaf = GameAssets.Hex(RI(0, 2) == 0 ? "#43A047" : "#558B2F");
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 3.8f, 0f), new Vector3(3.2f, 2.8f, 3.2f), leaf, default, 0.15f);
            Deco(t, PrimitiveType.Sphere, new Vector3(0.9f, 3.3f, 0.3f), new Vector3(2f, 1.8f, 2f), GameAssets.Hex("#66BB6A"), default, 0.15f);
            Deco(t, PrimitiveType.Sphere, new Vector3(-0.8f, 3.5f, -0.4f), new Vector3(2f, 1.8f, 2f), leaf, default, 0.15f);
        }

        void PineTree(Vector3 p)
        {
            var t = Group("Pine", p, R(0f, 360f));
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 0f), new Vector3(0.45f, 1.2f, 0.45f), GameAssets.Hex("#5D4037"));
            for (int i = 0; i < 4; i++)
            {
                float s = 3f - i * 0.65f;
                Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 2.2f + i * 1.1f, 0f), new Vector3(s, 0.5f, s), GameAssets.Hex("#2E7D32"), default, 0.15f);
                Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 2.7f + i * 1.1f, 0f), new Vector3(s * 0.7f, 0.3f, s * 0.7f), GameAssets.Hex("#388E3C"), default, 0.15f);
            }
        }

        void Rock(Vector3 p, Color color, float scale)
        {
            var t = Group("Rock", p, R(0f, 360f));
            Solid(t, PrimitiveType.Sphere, new Vector3(0f, 0.4f * scale, 0f), new Vector3(1.6f, 1.1f, 1.3f) * scale, color, new Vector3(R(-10f, 10f), 0f, R(-10f, 10f)), 0.1f);
            Deco(t, PrimitiveType.Sphere, new Vector3(0.6f * scale, 0.25f * scale, 0.3f * scale), new Vector3(0.8f, 0.6f, 0.7f) * scale, color * 0.9f, default, 0.1f);
        }

        void FallenLog(Vector3 p)
        {
            var t = Group("Log", p, R(0f, 360f));
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 2f, 0.9f), GameAssets.Hex("#795548"), new Vector3(0f, 0f, 90f));
            Deco(t, PrimitiveType.Cylinder, new Vector3(2.01f, 0.45f, 0f), new Vector3(0.75f, 0.01f, 0.75f), GameAssets.Hex("#D7B48A"), new Vector3(0f, 0f, 90f));
            Deco(t, PrimitiveType.Cylinder, new Vector3(-2.01f, 0.45f, 0f), new Vector3(0.75f, 0.01f, 0.75f), GameAssets.Hex("#D7B48A"), new Vector3(0f, 0f, 90f));
            Deco(t, PrimitiveType.Sphere, new Vector3(0.5f, 0.9f, 0.2f), new Vector3(0.5f, 0.2f, 0.5f), GameAssets.Hex("#7CB342"));
        }

        void Mushroom(Vector3 p, float scale)
        {
            var t = Group("Mushroom", p);
            t.localScale = Vector3.one * scale;
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f), new Vector3(0.3f, 0.4f, 0.3f), GameAssets.Hex("#FFF8E1"));
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 0.85f, 0f), new Vector3(1f, 0.55f, 1f), GameAssets.Hex("#E53935"), default, 0.5f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                Deco(t, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.3f, 1.02f, Mathf.Sin(a) * 0.3f), Vector3.one * 0.13f, Color.white);
            }
        }

        void Bush(Vector3 p, Color color)
        {
            var t = Group("Bush", p);
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 0.4f, 0f), new Vector3(1.4f, 0.9f, 1.4f), color, default, 0.1f);
            Deco(t, PrimitiveType.Sphere, new Vector3(0.5f, 0.3f, 0.3f), new Vector3(0.9f, 0.7f, 0.9f), color * 1.1f, default, 0.1f);
        }

        void Flowers(int count)
        {
            Color[] colors = { GameAssets.Hex("#FFEB3B"), GameAssets.Hex("#FF80AB"), GameAssets.Hex("#FFFFFF"), GameAssets.Hex("#B388FF") };
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector3(R(-half, half), 0f, R(-half, half));
                Deco(Root, PrimitiveType.Cylinder, pos + Vector3.up * 0.15f, new Vector3(0.03f, 0.15f, 0.03f), GameAssets.Hex("#388E3C"));
                Deco(Root, PrimitiveType.Sphere, pos + Vector3.up * 0.32f, new Vector3(0.2f, 0.1f, 0.2f), colors[RI(0, colors.Length)]);
            }
        }

        // ---------- Джунгли ----------

        void BuildJungle()
        {
            Ground(GameAssets.Hex("#3E8E41"), GameAssets.Hex("#2E7D32"));
            InvisibleBounds();

            for (float a = -half; a <= half; a += 3.5f)
            {
                JungleTree(new Vector3(a + R(-1f, 1f), 0f, half + R(1f, 3f)));
                JungleTree(new Vector3(a + R(-1f, 1f), 0f, -half - R(1f, 3f)));
                JungleTree(new Vector3(half + R(1f, 3f), 0f, a + R(-1f, 1f)));
                JungleTree(new Vector3(-half - R(1f, 3f), 0f, a + R(-1f, 1f)));
            }

            for (int i = 0; i < 10; i++) if (FindSpot(1.5f, out var p)) BananaTree(p);
            for (int i = 0; i < 6; i++) if (FindSpot(2f, out var p)) Ruin(p);
            for (int i = 0; i < 14; i++) if (FindSpot(1f, out var p)) BigLeafPlant(p);
            for (int i = 0; i < 6; i++) if (FindSpot(1.2f, out var p)) Rock(p, GameAssets.Hex("#78909C"), R(0.8f, 1.4f));
            Flowers(30);
        }

        void JungleTree(Vector3 p)
        {
            var t = Group("JungleTree", p, R(0f, 360f));
            float h = R(5f, 7f);
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, h / 2f, 0f), new Vector3(0.9f, h / 2f, 0.9f), GameAssets.Hex("#5D4037"));
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, h + 0.5f, 0f), new Vector3(5f, 2.5f, 5f), GameAssets.Hex("#2E7D32"), default, 0.15f);
            Deco(t, PrimitiveType.Sphere, new Vector3(1.2f, h - 0.3f, 0.5f), new Vector3(3f, 2f, 3f), GameAssets.Hex("#388E3C"), default, 0.15f);
            for (int i = 0; i < 3; i++)
                Deco(t, PrimitiveType.Cylinder, new Vector3(R(-1.5f, 1.5f), h - 1.5f, R(-1.5f, 1.5f)), new Vector3(0.06f, 1.5f, 0.06f), GameAssets.Hex("#558B2F"));
        }

        void BananaTree(Vector3 p)
        {
            var t = Group("BananaTree", p, R(0f, 360f));
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0f), new Vector3(0.4f, 1.6f, 0.4f), GameAssets.Hex("#8D9440"));
            for (int i = 0; i < 6; i++)
            {
                var leaf = Group("Leaf", Vector3.zero);
                leaf.SetParent(t, false);
                leaf.localPosition = new Vector3(0f, 3.1f, 0f);
                leaf.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
                Deco(leaf, PrimitiveType.Cube, new Vector3(0f, 0.2f, 1.3f), new Vector3(1.1f, 0.05f, 2.8f), GameAssets.Hex(i % 2 == 0 ? "#43A047" : "#7CB342"), new Vector3(-20f + (i % 2) * 30f, 0f, 0f));
            }
            for (int i = 0; i < 5; i++)
                Deco(t, PrimitiveType.Capsule, new Vector3(Mathf.Cos(i * 1.25f) * 0.35f, 2.6f, Mathf.Sin(i * 1.25f) * 0.35f), new Vector3(0.15f, 0.25f, 0.15f), GameAssets.Hex("#FFD93B"), new Vector3(20f, i * 72f, 0f), 0.6f);
        }

        void Ruin(Vector3 p)
        {
            var t = Group("Ruin", p, R(0f, 360f));
            Color stone = GameAssets.Hex("#A1A887");
            Color moss = GameAssets.Hex("#7CB342");
            Solid(t, PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f), new Vector3(1f, 2.4f, 1f), stone);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 2.5f, 0f), new Vector3(1.3f, 0.3f, 1.3f), stone * 0.9f);
            Solid(t, PrimitiveType.Cube, new Vector3(2.4f, 0.8f, 0f), new Vector3(1f, 1.6f, 1f), stone);
            Solid(t, PrimitiveType.Cube, new Vector3(1.2f, 0.3f, 1.4f), new Vector3(2.4f, 0.6f, 1f), stone, new Vector3(0f, 15f, 0f));
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 2.67f, 0f), new Vector3(1.32f, 0.05f, 1.32f), moss);
            Deco(t, PrimitiveType.Cube, new Vector3(0.51f, 1.2f, 0f), new Vector3(0.02f, 0.5f, 0.5f), GameAssets.Hex("#5D6B4A"));
        }

        void BigLeafPlant(Vector3 p)
        {
            var t = Group("Plant", p, R(0f, 360f));
            for (int i = 0; i < 4; i++)
            {
                var leaf = Group("Leaf", Vector3.zero);
                leaf.SetParent(t, false);
                leaf.localRotation = Quaternion.Euler(0f, i * 90f + 45f, 0f);
                Deco(leaf, PrimitiveType.Sphere, new Vector3(0f, 0.5f, 0.6f), new Vector3(0.9f, 0.08f, 1.4f), GameAssets.Hex(i % 2 == 0 ? "#2E7D32" : "#43A047"), new Vector3(-30f, 0f, 0f));
            }
        }

        // ---------- Пустыня ----------

        void BuildDesert()
        {
            Ground(GameAssets.Hex("#E3A857"), GameAssets.Hex("#D89A4A"));
            InvisibleBounds();

            // Скалы-столовые горы по краю
            Color[] mesa = { GameAssets.Hex("#C1693C"), GameAssets.Hex("#B5582F"), GameAssets.Hex("#D27D4A") };
            for (float a = -half - 4f; a <= half + 4f; a += 6f)
            {
                Mesa(new Vector3(a + R(-1f, 1f), 0f, half + R(3f, 6f)), mesa);
                Mesa(new Vector3(a + R(-1f, 1f), 0f, -half - R(3f, 6f)), mesa);
                Mesa(new Vector3(half + R(3f, 6f), 0f, a + R(-1f, 1f)), mesa);
                Mesa(new Vector3(-half - R(3f, 6f), 0f, a + R(-1f, 1f)), mesa);
            }

            for (int i = 0; i < 12; i++) if (FindSpot(1.2f, out var p)) Cactus(p);
            for (int i = 0; i < 6; i++) if (FindSpot(1.5f, out var p)) Rock(p, GameAssets.Hex("#B5764A"), R(1f, 2f));
            if (FindSpot(4f, out var wreck)) PlaneWreck(wreck);
            for (int i = 0; i < 5; i++) if (FindSpot(0.8f, out var p)) Barrels(p);
            for (int i = 0; i < 6; i++) if (FindSpot(1f, out var p)) Bones(p);
            // Барханы
            for (int i = 0; i < 12; i++)
                Deco(Root, PrimitiveType.Sphere, new Vector3(R(-half, half), -0.4f, R(-half, half)), new Vector3(R(4f, 8f), 1f, R(3f, 6f)), GameAssets.Hex("#E8B465"), default, 0.1f);
        }

        void Mesa(Vector3 p, Color[] colors)
        {
            var t = Group("Mesa", p, R(0f, 360f));
            float h = R(4f, 9f);
            float w = R(5f, 8f);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, w * 0.8f), colors[RI(0, colors.Length)], default, 0.1f);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, h * 0.35f, 0f), new Vector3(w + 0.1f, 0.4f, w * 0.8f + 0.1f), GameAssets.Hex("#E0A070"), default, 0.1f);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, h * 0.7f, 0f), new Vector3(w + 0.1f, 0.3f, w * 0.8f + 0.1f), GameAssets.Hex("#9E4A28"), default, 0.1f);
        }

        void Cactus(Vector3 p)
        {
            var t = Group("Cactus", p, R(0f, 360f));
            Color green = GameAssets.Hex("#4E9A3E");
            float h = R(2.2f, 3.5f);
            Solid(t, PrimitiveType.Capsule, new Vector3(0f, h / 2f, 0f), new Vector3(0.6f, h / 2f, 0.6f), green, default, 0.3f);
            Deco(t, PrimitiveType.Capsule, new Vector3(0.6f, h * 0.55f, 0f), new Vector3(0.35f, 0.5f, 0.35f), green, default, 0.3f);
            Deco(t, PrimitiveType.Capsule, new Vector3(0.38f, h * 0.42f, 0f), new Vector3(0.3f, 0.25f, 0.3f), green, new Vector3(0f, 0f, 90f), 0.3f);
            Deco(t, PrimitiveType.Capsule, new Vector3(-0.55f, h * 0.7f, 0f), new Vector3(0.3f, 0.4f, 0.3f), green, default, 0.3f);
            Deco(t, PrimitiveType.Capsule, new Vector3(-0.35f, h * 0.6f, 0f), new Vector3(0.25f, 0.2f, 0.25f), green, new Vector3(0f, 0f, 90f), 0.3f);
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, h, 0f), Vector3.one * 0.25f, GameAssets.Hex("#FF80AB"));
        }

        void PlaneWreck(Vector3 p)
        {
            var t = Group("Wreck", p, R(0f, 360f));
            Color metal = GameAssets.Hex("#7D8463");
            Solid(t, PrimitiveType.Capsule, new Vector3(0f, 0.8f, 0f), new Vector3(1.6f, 3f, 1.6f), metal, new Vector3(80f, 0f, 10f), 0.4f);
            Solid(t, PrimitiveType.Cube, new Vector3(2.5f, 0.4f, 0.5f), new Vector3(4f, 0.15f, 1.4f), metal, new Vector3(0f, 10f, -12f), 0.4f);
            Deco(t, PrimitiveType.Cube, new Vector3(-2.2f, 0.2f, -1f), new Vector3(2.5f, 0.12f, 1.2f), metal * 0.8f, new Vector3(0f, -30f, 8f), 0.4f);
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 1.2f, 2.8f), new Vector3(0.8f, 0.3f, 0.8f), GameAssets.Hex("#3B3B3B"), new Vector3(80f, 0f, 0f), 0.6f);
            FX_Smoke(t, new Vector3(0f, 1.5f, 1f));
        }

        void FX_Smoke(Transform parent, Vector3 pos)
        {
            for (int i = 0; i < 4; i++)
                Deco(parent, PrimitiveType.Sphere, pos + new Vector3(R(-0.3f, 0.3f), i * 0.7f, R(-0.3f, 0.3f)), Vector3.one * (0.8f + i * 0.35f), new Color(0.35f, 0.33f, 0.32f), default, 0f);
        }

        void Barrels(Vector3 p)
        {
            var t = Group("Barrels", p, R(0f, 360f));
            Color red = GameAssets.Hex("#C62828");
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.6f, 0.8f), red, default, 0.5f);
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0f), new Vector3(0.82f, 0.04f, 0.82f), GameAssets.Hex("#424242"));
            Solid(t, PrimitiveType.Cylinder, new Vector3(0.9f, 0.6f, 0.2f), new Vector3(0.8f, 0.6f, 0.8f), GameAssets.Hex("#F9A825"), default, 0.5f);
        }

        void Bones(Vector3 p)
        {
            var t = Group("Bones", p, R(0f, 360f));
            Color bone = GameAssets.Hex("#F5F0E1");
            Deco(t, PrimitiveType.Capsule, new Vector3(0f, 0.12f, 0f), new Vector3(0.18f, 0.6f, 0.18f), bone, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 4; i++)
                Deco(t, PrimitiveType.Capsule, new Vector3(0.25f, 0.15f, -0.3f + i * 0.2f), new Vector3(0.08f, 0.3f, 0.08f), bone, new Vector3(0f, 0f, 70f));
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 0.25f, 0.75f), new Vector3(0.45f, 0.4f, 0.55f), bone);
        }

        // ---------- Город ----------

        void BuildCity()
        {
            Ground(GameAssets.Hex("#5B6068"), GameAssets.Hex("#4A4E55"));
            InvisibleBounds();

            // Разметка
            for (float z = -half + 2f; z < half; z += 4f)
                Deco(Root, PrimitiveType.Cube, new Vector3(0f, 0.01f, z), new Vector3(0.3f, 0.02f, 2f), GameAssets.Hex("#FFEB3B"));
            for (int i = -3; i <= 3; i++)
                Deco(Root, PrimitiveType.Cube, new Vector3(i * 1.2f, 0.01f, 12f), new Vector3(0.7f, 0.02f, 3f), Color.white);
            // Тротуар по краю
            Deco(Root, PrimitiveType.Cube, new Vector3(0f, 0.1f, half - 1f), new Vector3(half * 2f, 0.2f, 2f), GameAssets.Hex("#9E9E9E"));
            Deco(Root, PrimitiveType.Cube, new Vector3(0f, 0.1f, -half + 1f), new Vector3(half * 2f, 0.2f, 2f), GameAssets.Hex("#9E9E9E"));

            Color[] buildingColors = { GameAssets.Hex("#E57373"), GameAssets.Hex("#FFB74D"), GameAssets.Hex("#F0E6D2"), GameAssets.Hex("#81C784"), GameAssets.Hex("#64B5F6"), GameAssets.Hex("#CE93D8") };
            for (float a = -half - 2f; a <= half + 2f; a += 7f)
            {
                Building(new Vector3(a, 0f, half + 5f), buildingColors[RI(0, buildingColors.Length)], 0f);
                Building(new Vector3(a, 0f, -half - 5f), buildingColors[RI(0, buildingColors.Length)], 180f);
                Building(new Vector3(half + 5f, 0f, a), buildingColors[RI(0, buildingColors.Length)], 90f);
                Building(new Vector3(-half - 5f, 0f, a), buildingColors[RI(0, buildingColors.Length)], -90f);
            }

            for (int i = 0; i < 8; i++) if (FindSpot(2.2f, out var p)) Car(p);
            for (int i = 0; i < 8; i++) if (FindSpot(0.6f, out var p)) StreetLamp(p);
            for (int i = 0; i < 5; i++) if (FindSpot(1.4f, out var p)) CafeTable(p);
            if (FindSpot(2.5f, out var cup)) GiantCup(cup);
            for (int i = 0; i < 4; i++) if (FindSpot(0.8f, out var p)) Hydrant(p);
        }

        void Building(Vector3 p, Color color, float yaw)
        {
            var t = Group("Building", p, yaw);
            float h = R(8f, 18f);
            float w = R(6f, 7.5f);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, 6f), color, default, 0.2f);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, h + 0.2f, 0f), new Vector3(w + 0.3f, 0.4f, 6.3f), color * 0.8f, default, 0.2f);
            int floors = Mathf.FloorToInt((h - 2f) / 2.5f);
            for (int f = 0; f < floors; f++)
            {
                for (int x = 0; x < 3; x++)
                {
                    float px = -w / 2f + w * (x + 0.5f) / 3f;
                    bool lit = RI(0, 3) > 0;
                    var win = BrainrotModels.Part(t, PrimitiveType.Cube, new Vector3(px, 2.5f + f * 2.5f, -3.01f), new Vector3(1.2f, 1.3f, 0.05f), Color.white, default, 0.9f, false);
                    win.GetComponent<Renderer>().sharedMaterial = lit ? GameAssets.Glow(GameAssets.Hex("#FFE9A8"), 1.2f) : GameAssets.Lit(GameAssets.Hex("#37474F"), 0.9f);
                }
            }
            // Козырёк кафе
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 2.6f, -3.4f), new Vector3(w * 0.8f, 0.1f, 1f), GameAssets.Hex("#D32F2F"), new Vector3(-12f, 0f, 0f));
        }

        void Car(Vector3 p)
        {
            var t = Group("Car", p, RI(0, 4) * 90f + R(-15f, 15f));
            Color[] colors = { GameAssets.Hex("#E53935"), GameAssets.Hex("#1E88E5"), GameAssets.Hex("#FDD835"), GameAssets.Hex("#43A047"), GameAssets.Hex("#FFFFFF") };
            Color c = colors[RI(0, colors.Length)];
            Solid(t, PrimitiveType.Cube, new Vector3(0f, 0.65f, 0f), new Vector3(1.9f, 0.7f, 4f), c, default, 0.85f);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 1.25f, -0.2f), new Vector3(1.7f, 0.6f, 2.1f), c, default, 0.85f);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 1.25f, -0.2f), new Vector3(1.72f, 0.45f, 1.9f), GameAssets.Hex("#90CAF9"), default, 0.95f);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * 0.95f, z = (i < 2 ? -1.3f : 1.3f);
                Deco(t, PrimitiveType.Cylinder, new Vector3(x, 0.35f, z), new Vector3(0.7f, 0.12f, 0.7f), GameAssets.Hex("#212121"), new Vector3(0f, 0f, 90f));
            }
            var head1 = Glow(t, PrimitiveType.Cube, new Vector3(-0.6f, 0.75f, 2.01f), new Vector3(0.4f, 0.2f, 0.05f), GameAssets.Hex("#FFF9C4"), 2f);
            var head2 = Glow(t, PrimitiveType.Cube, new Vector3(0.6f, 0.75f, 2.01f), new Vector3(0.4f, 0.2f, 0.05f), GameAssets.Hex("#FFF9C4"), 2f);
        }

        void StreetLamp(Vector3 p)
        {
            var t = Group("Lamp", p);
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 2f, 0f), new Vector3(0.18f, 2f, 0.18f), GameAssets.Hex("#37474F"), default, 0.6f);
            Deco(t, PrimitiveType.Cube, new Vector3(0.4f, 4f, 0f), new Vector3(0.9f, 0.1f, 0.15f), GameAssets.Hex("#37474F"));
            Glow(t, PrimitiveType.Sphere, new Vector3(0.8f, 3.85f, 0f), Vector3.one * 0.45f, GameAssets.Hex("#FFE082"), 3f);
            PointLight(t, new Vector3(0.8f, 3.5f, 0f), GameAssets.Hex("#FFD180"), 9f, 2f);
        }

        void CafeTable(Vector3 p)
        {
            var t = Group("Cafe", p, R(0f, 360f));
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.1f, 0.5f, 0.1f), GameAssets.Hex("#424242"));
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 1f, 0f), new Vector3(1.1f, 0.04f, 1.1f), Color.white, default, 0.7f);
            Deco(t, PrimitiveType.Cylinder, new Vector3(0.2f, 1.1f, 0.1f), new Vector3(0.18f, 0.08f, 0.18f), Color.white, default, 0.8f);
            Deco(t, PrimitiveType.Cylinder, new Vector3(0.2f, 1.17f, 0.1f), new Vector3(0.15f, 0.01f, 0.15f), GameAssets.Hex("#8B5A2B"));
            for (int i = 0; i < 2; i++)
                Deco(t, PrimitiveType.Cube, new Vector3(i == 0 ? -0.9f : 0.9f, 0.45f, 0f), new Vector3(0.5f, 0.08f, 0.5f), GameAssets.Hex("#A1887F"));
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.05f, 0.8f, 0.05f), Color.white);
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 2.6f, 0f), new Vector3(2.4f, 0.5f, 2.4f), GameAssets.Hex("#2E7D32"));
        }

        void GiantCup(Vector3 p)
        {
            var t = Group("CoffeeMonument", p, R(0f, 360f));
            Solid(t, PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(3f, 0.8f, 3f), GameAssets.Hex("#BDBDBD"));
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 2.1f, 0f), new Vector3(2.2f, 1.3f, 2.2f), GameAssets.Hex("#F5F1E8"), default, 0.8f);
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 3.41f, 0f), new Vector3(2f, 0.02f, 2f), GameAssets.Hex("#8B5A2B"));
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 3.43f, 0f), new Vector3(1.4f, 0.02f, 1.4f), GameAssets.Hex("#E8D2B0"));
            Deco(t, PrimitiveType.Cube, new Vector3(1.3f, 2.1f, 0f), new Vector3(0.3f, 1.4f, 0.25f), GameAssets.Hex("#F5F1E8"), default, 0.8f);
            for (int i = 0; i < 3; i++)
                Deco(t, PrimitiveType.Sphere, new Vector3(R(-0.3f, 0.3f), 4f + i * 0.6f, R(-0.3f, 0.3f)), Vector3.one * (0.6f - i * 0.12f), new Color(1f, 1f, 1f), default, 0f);
        }

        void Hydrant(Vector3 p)
        {
            var t = Group("Hydrant", p);
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.4f, 0.45f, 0.4f), GameAssets.Hex("#E53935"), default, 0.6f);
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 0.95f, 0f), Vector3.one * 0.42f, GameAssets.Hex("#E53935"), default, 0.6f);
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 0.6f, 0f), new Vector3(0.7f, 0.06f, 0.15f), GameAssets.Hex("#BDBDBD"), new Vector3(0f, 0f, 90f));
        }

        // ---------- Ночная деревня ----------

        void BuildNightVillage()
        {
            Ground(GameAssets.Hex("#2F4A3A"), GameAssets.Hex("#25392E"));
            InvisibleBounds();

            // Деревянный забор
            Color fence = GameAssets.Hex("#6D5440");
            for (float a = -half; a <= half; a += 1.2f)
            {
                Deco(Root, PrimitiveType.Cube, new Vector3(a, 0.8f, half + 0.3f), new Vector3(0.18f, 1.6f, 0.1f), fence);
                Deco(Root, PrimitiveType.Cube, new Vector3(a, 0.8f, -half - 0.3f), new Vector3(0.18f, 1.6f, 0.1f), fence);
                Deco(Root, PrimitiveType.Cube, new Vector3(half + 0.3f, 0.8f, a), new Vector3(0.1f, 1.6f, 0.18f), fence);
                Deco(Root, PrimitiveType.Cube, new Vector3(-half - 0.3f, 0.8f, a), new Vector3(0.1f, 1.6f, 0.18f), fence);
            }

            Color[] walls = { GameAssets.Hex("#D7C4A3"), GameAssets.Hex("#C9B18A"), GameAssets.Hex("#E3D5B8") };
            for (float a = -half; a <= half; a += 9f)
            {
                House(new Vector3(a + R(-1f, 1f), 0f, half + 6f), walls[RI(0, 3)], 180f);
                House(new Vector3(a + R(-1f, 1f), 0f, -half - 6f), walls[RI(0, 3)], 0f);
                House(new Vector3(half + 6f, 0f, a + R(-1f, 1f)), walls[RI(0, 3)], -90f);
                House(new Vector3(-half - 6f, 0f, a + R(-1f, 1f)), walls[RI(0, 3)], 90f);
            }

            for (int i = 0; i < 10; i++) if (FindSpot(0.6f, out var p)) Lantern(p);
            for (int i = 0; i < 5; i++) if (FindSpot(1.4f, out var p)) Haystack(p);
            for (int i = 0; i < 6; i++) if (FindSpot(0.9f, out var p)) Drum(p);
            if (FindSpot(1.6f, out var well)) Well(well);
            for (int i = 0; i < 3; i++) if (FindSpot(2f, out var p)) Cart(p);
            for (int i = 0; i < 6; i++) if (FindSpot(1f, out var p)) Barrels(p);

            // Луна и звёзды
            var moon = Glow(Root, PrimitiveType.Sphere, new Vector3(-40f, 45f, 70f), Vector3.one * 9f, GameAssets.Hex("#FFF8E1"), 2.5f);
            for (int i = 0; i < 60; i++)
            {
                Vector3 dir = Quaternion.Euler(R(-70f, -15f), R(0f, 360f), 0f) * Vector3.forward;
                Glow(Root, PrimitiveType.Sphere, dir * 180f, Vector3.one * R(0.4f, 1f), Color.white, 3f);
            }
        }

        void House(Vector3 p, Color wall, float yaw)
        {
            var t = Group("House", p, yaw);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 2f, 0f), new Vector3(7f, 4f, 6f), wall, default, 0.15f);
            var roof1 = Deco(t, PrimitiveType.Cube, new Vector3(0f, 4.9f, -1.6f), new Vector3(7.6f, 0.3f, 3.8f), GameAssets.Hex("#8D3B2F"), new Vector3(-35f, 0f, 0f));
            var roof2 = Deco(t, PrimitiveType.Cube, new Vector3(0f, 4.9f, 1.6f), new Vector3(7.6f, 0.3f, 3.8f), GameAssets.Hex("#8D3B2F"), new Vector3(35f, 0f, 0f));
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 1.1f, 3.01f), new Vector3(1.2f, 2.2f, 0.05f), GameAssets.Hex("#5D4037"));
            Glow(t, PrimitiveType.Cube, new Vector3(-2.2f, 2.3f, 3.01f), new Vector3(1.1f, 1f, 0.05f), GameAssets.Hex("#FFB74D"), 2f);
            Glow(t, PrimitiveType.Cube, new Vector3(2.2f, 2.3f, 3.01f), new Vector3(1.1f, 1f, 0.05f), GameAssets.Hex("#FFB74D"), 2f);
        }

        void Lantern(Vector3 p)
        {
            var t = Group("Lantern", p);
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 1.3f, 0f), new Vector3(0.15f, 1.3f, 0.15f), GameAssets.Hex("#4E342E"));
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 2.7f, 0f), new Vector3(0.6f, 0.08f, 0.6f), GameAssets.Hex("#3E2723"));
            Glow(t, PrimitiveType.Cube, new Vector3(0f, 2.95f, 0f), new Vector3(0.4f, 0.45f, 0.4f), GameAssets.Hex("#FFB74D"), 3f);
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 3.22f, 0f), new Vector3(0.55f, 0.1f, 0.55f), GameAssets.Hex("#3E2723"));
            PointLight(t, new Vector3(0f, 2.9f, 0f), GameAssets.Hex("#FFA040"), 11f, 3f);
        }

        void Haystack(Vector3 p)
        {
            var t = Group("Haystack", p, R(0f, 360f));
            Solid(t, PrimitiveType.Sphere, new Vector3(0f, 0.8f, 0f), new Vector3(2.4f, 1.8f, 2.4f), GameAssets.Hex("#D4B04C"), default, 0.05f);
            Deco(t, PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0f), new Vector3(1.4f, 0.9f, 1.4f), GameAssets.Hex("#E0C062"), default, 0.05f);
        }

        void Drum(Vector3 p)
        {
            var t = Group("Drum", p, R(0f, 360f));
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 0.5f, 0.9f), GameAssets.Hex("#B71C1C"), default, 0.5f);
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 1.01f, 0f), new Vector3(0.85f, 0.02f, 0.85f), GameAssets.Hex("#F5E6C8"));
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 0.85f, 0f), new Vector3(0.92f, 0.04f, 0.92f), GameAssets.Hex("#FFC107"), default, 0.8f);
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.92f, 0.04f, 0.92f), GameAssets.Hex("#FFC107"), default, 0.8f);
        }

        void Well(Vector3 p)
        {
            var t = Group("Well", p);
            Solid(t, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(2f, 0.5f, 2f), GameAssets.Hex("#8E8E8E"), default, 0.1f);
            Deco(t, PrimitiveType.Cylinder, new Vector3(0f, 1.01f, 0f), new Vector3(1.6f, 0.02f, 1.6f), GameAssets.Hex("#1A237E"), default, 0.9f);
            Deco(t, PrimitiveType.Cube, new Vector3(-0.9f, 1.6f, 0f), new Vector3(0.15f, 1.6f, 0.15f), GameAssets.Hex("#5D4037"));
            Deco(t, PrimitiveType.Cube, new Vector3(0.9f, 1.6f, 0f), new Vector3(0.15f, 1.6f, 0.15f), GameAssets.Hex("#5D4037"));
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 2.5f, 0f), new Vector3(2.4f, 0.2f, 1.4f), GameAssets.Hex("#8D3B2F"));
        }

        void Cart(Vector3 p)
        {
            var t = Group("Cart", p, R(0f, 360f));
            Solid(t, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(1.6f, 0.6f, 2.4f), GameAssets.Hex("#8D6E63"));
            Deco(t, PrimitiveType.Cylinder, new Vector3(-0.9f, 0.5f, 0f), new Vector3(1f, 0.08f, 1f), GameAssets.Hex("#5D4037"), new Vector3(0f, 0f, 90f));
            Deco(t, PrimitiveType.Cylinder, new Vector3(0.9f, 0.5f, 0f), new Vector3(1f, 0.08f, 1f), GameAssets.Hex("#5D4037"), new Vector3(0f, 0f, 90f));
            Deco(t, PrimitiveType.Cube, new Vector3(0f, 0.8f, 1.9f), new Vector3(0.1f, 0.1f, 1.6f), GameAssets.Hex("#5D4037"), new Vector3(-10f, 0f, 0f));
            for (int i = 0; i < 4; i++)
                Deco(t, PrimitiveType.Sphere, new Vector3(R(-0.5f, 0.5f), 1.3f, R(-0.8f, 0.8f)), Vector3.one * 0.5f, GameAssets.Hex("#FF7043"));
        }
    }
}
