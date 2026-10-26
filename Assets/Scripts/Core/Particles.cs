using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Системы частиц (настоящие ParticleSystem): искры, дым, пыль, вспышки, атмосфера арен.
    /// Одна система на вид эффекта, частицы выпускаются вызовом Emit — это очень дёшево.
    /// </summary>
    public class Particles : MonoBehaviour
    {
        static Particles instance;
        static Texture2D dotTexture, leafTexture;

        ParticleSystem sparks, smoke, dust, glow, leaves;
        ParticleSystem ambient, ambientMist;
        Transform follow;

        static Particles I
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("Particles");
                    instance = go.AddComponent<Particles>();
                    instance.Init();
                }
                return instance;
            }
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // ---------- Текстуры и материалы ----------

        static Texture2D DotTexture
        {
            get
            {
                if (dotTexture != null) return dotTexture;
                const int size = 64;
                dotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "SoftDot" };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a * (3f - 2f * a);
                        px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                dotTexture.SetPixels32(px);
                dotTexture.Apply();
                return dotTexture;
            }
        }

        static Texture2D LeafTexture
        {
            get
            {
                if (leafTexture != null) return leafTexture;
                const int size = 64;
                leafTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Leaf" };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx / 0.25f + dy * dy);
                        float a = Mathf.Clamp01((1f - d) * 8f);
                        float vein = Mathf.Abs(dx) < 0.04f ? 0.8f : 1f;
                        byte v = (byte)(255 * vein);
                        px[y * size + x] = new Color32(v, v, v, (byte)(a * 255));
                    }
                leafTexture.SetPixels32(px);
                leafTexture.Apply();
                return leafTexture;
            }
        }

        static Material MakeMaterial(bool additive, Texture texture)
        {
            var source = additive ? GameAssets.I.particleAdditive : GameAssets.I.particleAlpha;
            Material m;
            if (source != null) m = new Material(source);
            else
            {
                m = new Material(GameAssets.I.unlit);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
            }
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
            m.mainTexture = texture;
            return m;
        }

        // ---------- Создание систем ----------

        void Init()
        {
            sparks = NewSystem("Sparks", true, DotTexture, 1.2f, 1500);
            var trail = sparks.GetComponent<ParticleSystemRenderer>();
            trail.renderMode = ParticleSystemRenderMode.Stretch;
            trail.velocityScale = 0.04f;
            trail.lengthScale = 1.5f;

            smoke = NewSystem("Smoke", false, DotTexture, -0.05f, 600);
            var sol = smoke.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.8f));

            dust = NewSystem("Dust", false, DotTexture, 0.3f, 600);
            var dsol = dust.sizeOverLifetime;
            dsol.enabled = true;
            dsol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 1.5f));

            glow = NewSystem("Glow", true, DotTexture, 0f, 800);
            var gsol = glow.sizeOverLifetime;
            gsol.enabled = true;
            gsol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            leaves = NewSystem("Leaves", false, LeafTexture, 0.4f, 400);
            var rot = leaves.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);
        }

        ParticleSystem NewSystem(string name, bool additive, Texture texture, float gravity, int max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = max;
            main.startSpeed = 0f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MakeMaterial(additive, texture);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = additive ? -10f : 0f;
            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 position, Vector3 velocity, Color color, float size, float life, float rotation = 0f)
        {
            var p = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startColor = color,
                startSize = size,
                startLifetime = life,
                rotation = rotation,
                applyShapeToPosition = false
            };
            ps.Emit(p, 1);
        }

        static int Scaled(int count) => Mathf.Max(1, Mathf.RoundToInt(count * GameSettings.ParticleScale));

        // ---------- Эффекты ----------

        /// <summary>Искры от попадания (по направлению нормали).</summary>
        public static void Sparks(Vector3 position, Vector3 normal, Color color, int count = 8, float speed = 7f)
        {
            var s = I;
            for (int i = 0; i < Scaled(count); i++)
            {
                Vector3 dir = (normal + Random.insideUnitSphere * 0.9f).normalized;
                Emit(s.sparks, position, dir * speed * Random.Range(0.4f, 1.2f), color, Random.Range(0.05f, 0.12f), Random.Range(0.15f, 0.4f));
            }
            Emit(s.glow, position, Vector3.zero, color, 0.6f, 0.08f);
        }

        /// <summary>Клуб дыма (взрывы, выстрелы, смерть).</summary>
        public static void Smoke(Vector3 position, float radius, int count, Color? tint = null)
        {
            var s = I;
            Color c = tint ?? new Color(0.35f, 0.33f, 0.32f, 0.55f);
            for (int i = 0; i < Scaled(count); i++)
            {
                Vector3 offset = Random.insideUnitSphere * radius * 0.5f;
                Vector3 vel = offset.normalized * Random.Range(0.5f, 2f) * radius + Vector3.up * Random.Range(0.5f, 1.5f);
                Emit(s.smoke, position + offset, vel, c, radius * Random.Range(0.8f, 1.4f), Random.Range(0.8f, 1.6f), Random.Range(0f, 360f));
            }
        }

        /// <summary>Пыль из-под ног (приземление, рывок, появление врага).</summary>
        public static void Dust(Vector3 position, float radius, int count, Color color)
        {
            var s = I;
            for (int i = 0; i < Scaled(count); i++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Emit(s.dust, position + dir * radius * 0.3f + Vector3.up * 0.15f, dir * Random.Range(1.5f, 4f) * radius + Vector3.up * Random.Range(0.5f, 2f),
                    new Color(color.r, color.g, color.b, 0.6f), Random.Range(0.4f, 0.9f) * radius, Random.Range(0.5f, 1f));
            }
        }

        /// <summary>Яркая вспышка (выстрел, взрыв, подбор).</summary>
        public static void Flash(Vector3 position, Color color, float size, float life = 0.12f)
        {
            Emit(I.glow, position, Vector3.zero, color, size, life);
        }

        /// <summary>Сверкающие точки во все стороны (подбор монеты, улучшение, смерть).</summary>
        public static void Sparkle(Vector3 position, Color color, int count, float speed = 3f, float size = 0.15f)
        {
            var s = I;
            for (int i = 0; i < Scaled(count); i++)
                Emit(s.glow, position + Random.insideUnitSphere * 0.2f, Random.insideUnitSphere * speed, color, size * Random.Range(0.6f, 1.3f), Random.Range(0.3f, 0.8f));
        }

        /// <summary>Огненный шар взрыва из частиц.</summary>
        public static void Fireball(Vector3 position, float radius, Color color)
        {
            var s = I;
            for (int i = 0; i < Scaled(18); i++)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
                Color c = Color.Lerp(color, new Color(1f, 0.95f, 0.6f), Random.value * 0.6f);
                Emit(s.glow, position + dir * radius * 0.2f, dir * radius * Random.Range(1.5f, 3.5f), c, radius * Random.Range(0.5f, 0.9f), Random.Range(0.25f, 0.5f));
            }
            Sparks(position, Vector3.up, new Color(1f, 0.8f, 0.3f), 14, radius * 5f);
            Smoke(position + Vector3.up * 0.5f, radius * 0.9f, 10);
        }

        // ---------- Атмосфера арены ----------

        /// <summary>Постоянные частицы вокруг игрока: листья, светлячки, песок, искорки, туман у земли.</summary>
        public static void Ambient(ArenaTheme theme, Transform target)
        {
            var s = I;
            s.follow = target;
            if (s.ambient != null) Destroy(s.ambient.gameObject);
            if (s.ambientMist != null) Destroy(s.ambientMist.gameObject);

            bool additive = theme == ArenaTheme.Jungle || theme == ArenaTheme.NightVillage || theme == ArenaTheme.Beach;
            bool leafy = theme == ArenaTheme.Forest || theme == ArenaTheme.City;
            s.ambient = s.AmbientSystem("Ambient", additive, leafy ? LeafTexture : DotTexture);
            var main = s.ambient.main;
            var emission = s.ambient.emission;
            var vel = s.ambient.velocityOverLifetime;
            vel.enabled = true;
            var noise = s.ambient.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.4f;
            var shape = s.ambient.shape;

            float rate;
            switch (theme)
            {
                case ArenaTheme.Beach:
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.7f), new Color(0.7f, 0.9f, 1f, 0.5f));
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                    vel.y = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
                    vel.x = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
                    vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                    rate = 25f;
                    break;
                case ArenaTheme.Forest:
                    main.startColor = new ParticleSystem.MinMaxGradient(GameAssets.Hex("#8BC34A"), GameAssets.Hex("#E6A23C"));
                    main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
                    vel.y = new ParticleSystem.MinMaxCurve(-1.2f, -0.6f);
                    vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.6f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                    noise.strength = 1.2f;
                    rate = 14f;
                    break;
                case ArenaTheme.Jungle:
                    main.startColor = new ParticleSystem.MinMaxGradient(GameAssets.Hex("#E6FF7A"), GameAssets.Hex("#7CFFB2"));
                    main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.25f);
                    vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
                    rate = 30f;
                    break;
                case ArenaTheme.Desert:
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.8f, 0.55f, 0.5f), new Color(0.85f, 0.65f, 0.4f, 0.35f));
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.5f);
                    vel.x = new ParticleSystem.MinMaxCurve(4f, 7f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.3f);
                    vel.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
                    rate = 60f;
                    break;
                case ArenaTheme.City:
                    main.startColor = new ParticleSystem.MinMaxGradient(GameAssets.Hex("#FFB7D5"), GameAssets.Hex("#FFFFFF"));
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.2f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.4f);
                    vel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.4f);
                    rate = 18f;
                    break;
                default:
                    main.startColor = new ParticleSystem.MinMaxGradient(GameAssets.Hex("#FFF59D"), GameAssets.Hex("#B2FF59"));
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f);
                    vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                    vel.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                    noise.strength = 1f;
                    rate = 22f;
                    break;
            }
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            emission.rateOverTime = rate * GameSettings.ParticleScale;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44f, theme == ArenaTheme.Desert ? 3f : 10f, 44f);
            shape.position = new Vector3(0f, theme == ArenaTheme.Desert ? 1f : 4f, 0f);
            if (leafy)
            {
                var rot = s.ambient.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-2f, 2f);
            }
            s.ambient.Simulate(6f, true, true);
            s.ambient.Play();

            // Туман у земли: большие прозрачные пятна
            if (theme == ArenaTheme.Forest || theme == ArenaTheme.Jungle || theme == ArenaTheme.NightVillage)
            {
                s.ambientMist = s.AmbientSystem("Mist", false, DotTexture);
                var m = s.ambientMist.main;
                Color mist = theme == ArenaTheme.NightVillage ? new Color(0.45f, 0.55f, 0.85f, 0.12f) : new Color(0.9f, 1f, 0.9f, 0.1f);
                m.startColor = mist;
                m.startSize = new ParticleSystem.MinMaxCurve(5f, 9f);
                m.startLifetime = new ParticleSystem.MinMaxCurve(10f, 16f);
                m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                var e = s.ambientMist.emission;
                e.rateOverTime = 5f * GameSettings.ParticleScale;
                var sh = s.ambientMist.shape;
                sh.shapeType = ParticleSystemShapeType.Box;
                sh.scale = new Vector3(60f, 0.6f, 60f);
                sh.position = new Vector3(0f, 0.8f, 0f);
                var v = s.ambientMist.velocityOverLifetime;
                v.enabled = true;
                v.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                v.y = new ParticleSystem.MinMaxCurve(0f, 0f);
                v.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                var col = s.ambientMist.colorOverLifetime;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
                s.ambientMist.Simulate(12f, true, true);
                s.ambientMist.Play();
            }
        }

        ParticleSystem AmbientSystem(string name, bool additive, Texture texture)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 800;
            main.startSpeed = 0f;
            var shape = ps.shape;
            shape.enabled = true;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MakeMaterial(additive, texture);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        void LateUpdate()
        {
            if (follow == null) return;
            Vector3 p = follow.position;
            p.y = 0f;
            if (ambient != null) ambient.transform.position = p;
            if (ambientMist != null) ambientMist.transform.position = p;
        }
    }
}
