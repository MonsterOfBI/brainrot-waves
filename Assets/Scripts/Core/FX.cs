using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>Визуальные эффекты: взрывы, осколки, цифры урона, вспышки, тряска камеры.</summary>
    public static class FX
    {
        static Font font, boldFont, titleFont;

        static Font LoadFont(string name)
        {
            var f = Resources.Load<Font>("Fonts/" + name);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>Основной шрифт интерфейса (Exo 2, есть кириллица).</summary>
        public static Font Font
        {
            get
            {
                if (font == null) font = LoadFont("Body");
                return font;
            }
        }

        /// <summary>Жирный основной шрифт (Exo 2 ExtraBold).</summary>
        public static Font BoldFont
        {
            get
            {
                if (boldFont == null) boldFont = LoadFont("BodyBold");
                return boldFont;
            }
        }

        /// <summary>Шрифт заголовков и цифр урона (Unbounded Bold).</summary>
        public static Font TitleFont
        {
            get
            {
                if (titleFont == null) titleFont = LoadFont("Title");
                return titleFont;
            }
        }

        static readonly System.Collections.Generic.Dictionary<string, GameObject> cfxCache = new System.Collections.Generic.Dictionary<string, GameObject>();

        /// <summary>Готовый мультяшный эффект из Cartoon FX Remaster Free (Resources/CFX). Возвращает false, если пака нет.</summary>
        public static bool Cfx(string name, Vector3 position, float scale = 1f, Quaternion? rotation = null)
        {
            if (!cfxCache.TryGetValue(name, out var prefab))
            {
                prefab = Resources.Load<GameObject>("CFX/" + name);
                cfxCache[name] = prefab;
            }
            if (prefab == null) return false;
            var go = Object.Instantiate(prefab, position, rotation ?? Quaternion.identity);
            go.transform.localScale = Vector3.one * scale;
            Object.Destroy(go, 5f);
            return true;
        }

        /// <summary>Разлёт цветных кусочков (смерть врага, попадание).</summary>
        public static void Burst(Vector3 position, Color color, int count, float speed, float size, float life = 0.8f)
        {
            for (int i = 0; i < count; i++)
            {
                var piece = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Sphere);
                Object.DestroyImmediate(piece.GetComponent<Collider>());
                piece.transform.position = position + Random.insideUnitSphere * size;
                piece.transform.localScale = Vector3.one * size * Random.Range(0.6f, 1.2f);
                piece.transform.rotation = Random.rotation;
                var r = piece.GetComponent<Renderer>();
                r.sharedMaterial = GameAssets.Lit(color);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) + 0.3f;
                var debris = piece.AddComponent<Debris>();
                debris.velocity = dir.normalized * speed * Random.Range(0.5f, 1.2f);
                debris.life = life * Random.Range(0.7f, 1.2f);
            }
        }

        /// <summary>Огненный шар взрыва с кольцом и дымом.</summary>
        public static void Explosion(Vector3 position, float radius, Color color)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(ball.GetComponent<Collider>());
            ball.transform.position = position;
            var r = ball.GetComponent<Renderer>();
            r.sharedMaterial = GameAssets.Glow(color, 3f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var grow = ball.AddComponent<GrowFade>();
            grow.from = radius * 0.3f;
            grow.to = radius * 2f;
            grow.life = 0.35f;

            var lightObject = new GameObject("ExplosionLight");
            lightObject.transform.position = position + Vector3.up;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = radius * 4f;
            light.intensity = 6f;
            lightObject.AddComponent<LightFade>().life = 0.35f;

            Burst(position, new Color(0.25f, 0.22f, 0.2f), 5, radius * 3f, 0.35f, 1f);
            Burst(position, color, 4, radius * 4f, 0.22f, 0.6f);
            Particles.Fireball(position, radius, color);
            Cfx("CFXR Explosion 1", position, Mathf.Clamp(radius / 2.5f, 0.6f, 2.5f));
            if (radius >= 3f && Random.value < 0.35f) Cfx("CFXR _BOOM_", position + Vector3.up * 1.5f, 1.2f);
            Particles.Dust(new Vector3(position.x, 0.05f, position.z), radius * 0.6f, 10, new Color(0.55f, 0.5f, 0.45f));
            CameraShake.AddAt(position, 0.5f, radius * 6f);
            SoundFX.Play(SoundFX.Sfx.Explosion, 0.9f, 0.15f);
        }

        /// <summary>Кольцо по земле (удары, способности).</summary>
        public static void Ring(Vector3 position, float radius, Color color, float life = 0.4f)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.position = new Vector3(position.x, position.y + 0.05f, position.z);
            var r = ring.GetComponent<Renderer>();
            r.sharedMaterial = GameAssets.Glow(color, 2f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var grow = ring.AddComponent<GrowFade>();
            grow.from = 0.5f;
            grow.to = radius * 2f;
            grow.flat = true;
            grow.life = life;
        }

        /// <summary>Летящая вверх цифра урона.</summary>
        public static void DamageNumber(Vector3 position, float amount, bool crit)
        {
            if (!GameSettings.DamageNumbers) return;
            var go = new GameObject("DamageNumber");
            go.transform.position = position + Random.insideUnitSphere * 0.3f;
            var text = go.AddComponent<TextMesh>();
            text.font = TitleFont;
            go.GetComponent<MeshRenderer>().sharedMaterial = TitleFont.material;
            text.text = Mathf.CeilToInt(amount).ToString() + (crit ? "!" : "");
            text.fontSize = 64;
            text.characterSize = crit ? 0.07f : 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = crit ? new Color(1f, 0.85f, 0.1f) : Color.white;
            var f = go.AddComponent<FloatingText>();
            f.pop = crit ? 1.8f : 1.3f;
        }

        public static void Text(Vector3 position, string message, Color color, float size = 0.06f)
        {
            var go = new GameObject("FloatingText");
            go.transform.position = position;
            var text = go.AddComponent<TextMesh>();
            text.font = Font;
            go.GetComponent<MeshRenderer>().sharedMaterial = Font.material;
            text.text = message;
            text.fontSize = 64;
            text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter;
            text.color = color;
            var f = go.AddComponent<FloatingText>();
            f.life = 1.2f;
        }
    }

    /// <summary>Осколок: летит, падает, уменьшается и исчезает.</summary>
    public class Debris : MonoBehaviour
    {
        public Vector3 velocity;
        public float life = 0.8f;
        float age;
        Vector3 startScale;
        Vector3 spin;

        void Start()
        {
            startScale = transform.localScale;
            spin = Random.insideUnitSphere * 720f;
        }

        void Update()
        {
            age += Time.deltaTime;
            velocity.y -= 18f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            if (transform.position.y < 0.05f)
            {
                transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
                velocity *= 0.5f;
                velocity.y = Mathf.Abs(velocity.y) * 0.3f;
            }
            transform.Rotate(spin * Time.deltaTime);
            float t = age / life;
            transform.localScale = startScale * Mathf.Clamp01(1f - t * t);
            if (age >= life) Destroy(gameObject);
        }
    }

    /// <summary>Объект растёт и исчезает (взрывы, кольца).</summary>
    public class GrowFade : MonoBehaviour
    {
        public float from = 0.5f;
        public float to = 3f;
        public float life = 0.4f;
        public bool flat;
        float age;

        void Update()
        {
            age += Time.deltaTime;
            float t = Mathf.Clamp01(age / life);
            float s = Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t));
            float fade = 1f - t;
            transform.localScale = flat ? new Vector3(s, 0.03f, s) : Vector3.one * s * (0.3f + 0.7f * fade);
            if (age >= life) Destroy(gameObject);
        }
    }

    public class LightFade : MonoBehaviour
    {
        public float life = 0.3f;
        float age;
        Light lightComponent;
        float startIntensity;

        void Start()
        {
            lightComponent = GetComponent<Light>();
            startIntensity = lightComponent.intensity;
        }

        void Update()
        {
            age += Time.deltaTime;
            lightComponent.intensity = startIntensity * Mathf.Clamp01(1f - age / life);
            if (age >= life) Destroy(gameObject);
        }
    }

    /// <summary>Текст, который всплывает, смотрит в камеру и исчезает.</summary>
    public class FloatingText : MonoBehaviour
    {
        public float life = 0.8f;
        public float pop = 1f;
        float age;
        TextMesh text;
        Vector3 baseScale = Vector3.one;
        Color startColor;
        Vector3 velocity;

        void Start()
        {
            text = GetComponent<TextMesh>();
            startColor = text.color;
            baseScale = transform.localScale;
            velocity = new Vector3(Random.Range(-0.6f, 0.6f), 2.5f, Random.Range(-0.6f, 0.6f));
        }

        void Update()
        {
            age += Time.deltaTime;
            velocity.y -= 3f * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
            var cam = Camera.main;
            if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            float t = age / life;
            text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t * t);
            float popT = Mathf.Clamp01(age / 0.15f);
            transform.localScale = baseScale * Mathf.Lerp(pop, 1f, Ease.OutCubic(popT));
            if (age >= life) Destroy(gameObject);
        }
    }

    /// <summary>Тряска камеры игрока.</summary>
    public static class CameraShake
    {
        public static float trauma;

        public static void Add(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount * GameSettings.ScreenShake);
        }

        /// <summary>Тряска, ослабевающая с расстоянием до игрока.</summary>
        public static void AddAt(Vector3 position, float amount, float range)
        {
            var cam = Camera.main;
            if (cam == null) return;
            float d = Vector3.Distance(cam.transform.position, position);
            Add(amount * Mathf.Clamp01(1f - d / Mathf.Max(range, 0.1f)));
        }
    }
}
