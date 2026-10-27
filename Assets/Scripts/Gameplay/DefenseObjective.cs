using System;
using UnityEngine;
using UnityEngine.AI;

namespace BrainrotWaves
{
    /// <summary>
    /// Режим «Защита»: пиццерия Нонны в центре арены. Брейнроты хотят её разрушить.
    /// Если здоровье печи кончится — забег проигран.
    /// </summary>
    public class DefenseObjective : MonoBehaviour
    {
        public static DefenseObjective Instance { get; private set; }

        public float MaxHealth { get; private set; } = 3000f;
        public float Current { get; private set; }
        public float Armor = 0.5f; // печь крепкая: получает половину урона
        public bool Destroyed => Current <= 0f;
        public float Fraction => Mathf.Clamp01(Current / MaxHealth);
        public Vector3 Center => transform.position + Vector3.up * 1.2f;
        public float Radius => 2.4f;

        public event Action Damaged;
        public event Action DestroyedEvent;

        Transform model;
        Renderer[] renderers;
        Material[] materials;
        float flash;
        float shake;
        Light fireLight;
        float smokeTimer;

        public static DefenseObjective Create(Vector3 position)
        {
            var go = new GameObject("PizzaOven");
            go.transform.position = position;
            var o = go.AddComponent<DefenseObjective>();
            o.Build();
            return o;
        }

        void Awake()
        {
            Instance = this;
            Current = MaxHealth;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Build()
        {
            model = new GameObject("Model").transform;
            model.SetParent(transform, false);
            Color brick = GameAssets.Hex("#C0583A");
            Color brickDark = GameAssets.Hex("#8E3B26");
            Color stone = GameAssets.Hex("#BDB2A6");

            // Каменное основание
            BrainrotModels.Part(model, PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(4.6f, 0.45f, 4.6f), stone, default, 0.2f);
            // Купол печи
            BrainrotModels.Part(model, PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), new Vector3(4f, 3.2f, 4f), brick, default, 0.25f);
            for (int i = 0; i < 10; i++)
            {
                var q = Quaternion.Euler(0f, i * 36f, 0f);
                BrainrotModels.Part(model, PrimitiveType.Cube, q * new Vector3(0f, 1.3f, 1.85f), new Vector3(0.5f, 0.12f, 0.2f), brickDark, new Vector3(0f, i * 36f, 0f), 0.2f, false);
            }
            // Устье с огнём
            BrainrotModels.Part(model, PrimitiveType.Cube, new Vector3(0f, 1.05f, 1.75f), new Vector3(1.5f, 1.1f, 0.6f), brickDark, default, 0.2f);
            var fire = BrainrotModels.Part(model, PrimitiveType.Sphere, new Vector3(0f, 1f, 1.95f), new Vector3(1.1f, 0.8f, 0.3f), Color.white, default, 0.5f, false);
            fire.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(GameAssets.Hex("#FF8F00"), 4f);
            // Труба
            BrainrotModels.Part(model, PrimitiveType.Cylinder, new Vector3(0.6f, 3.1f, -0.4f), new Vector3(0.6f, 0.8f, 0.6f), brickDark, default, 0.2f);
            // Вывеска
            var sign = BrainrotModels.Part(model, PrimitiveType.Cube, new Vector3(0f, 3.3f, 1.2f), new Vector3(2.6f, 0.7f, 0.12f), GameAssets.Hex("#2E7D32"), new Vector3(-10f, 0f, 0f), 0.4f);
            BrainrotModels.Part(sign, PrimitiveType.Cube, new Vector3(0f, 0f, 0.6f), new Vector3(0.95f, 0.6f, 0.3f), Color.white, default, 0.4f, false);
            var pizza = BrainrotModels.Part(model, PrimitiveType.Cylinder, new Vector3(0f, 3.3f, 1.35f), new Vector3(0.55f, 0.03f, 0.55f), GameAssets.Hex("#FFC107"), new Vector3(80f, 0f, 0f), 0.4f, false);
            pizza.GetComponent<Renderer>().sharedMaterial = GameAssets.Glow(GameAssets.Hex("#FFC107"), 1.5f);

            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.2f, 2.6f);
            fireLight = lightGo.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.6f, 0.2f);
            fireLight.range = 9f;
            fireLight.intensity = 3f;

            // Препятствие: враги обходят печь, пули в неё упираются
            var col = gameObject.AddComponent<CapsuleCollider>();
            col.radius = Radius;
            col.height = 4f;
            col.center = new Vector3(0f, 2f, 0f);
            var obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.radius = Radius;
            obstacle.height = 4f;
            obstacle.center = new Vector3(0f, 2f, 0f);
            obstacle.carving = true;

            renderers = model.GetComponentsInChildren<Renderer>();
            materials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) materials[i] = renderers[i].sharedMaterial;
        }

        public void SetMaxHealth(float value, bool refill)
        {
            MaxHealth = value;
            if (refill) Current = value;
            else Current = Mathf.Min(Current, value);
        }

        public void Repair(float fraction)
        {
            if (Destroyed) return;
            Current = Mathf.Min(MaxHealth, Current + MaxHealth * fraction);
            Particles.Sparkle(Center, UIKit.Green, 40, 4f, 0.3f);
        }

        public void Damage(float amount)
        {
            if (Destroyed || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount * Armor);
            flash = 0.08f;
            shake = 0.25f;
            Damaged?.Invoke();
            if (Destroyed)
            {
                FX.Explosion(Center, 5f, new Color(1f, 0.5f, 0.2f));
                FX.Cfx("CFXR2 WW Enemy Explosion", Center, 3f);
                model.gameObject.SetActive(false);
                DestroyedEvent?.Invoke();
            }
        }

        void Update()
        {
            if (Destroyed) return;
            flash -= Time.deltaTime;
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime);
            model.localPosition = UnityEngine.Random.insideUnitSphere * shake * 0.15f;
            bool on = flash > 0f;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].sharedMaterial = on ? GameAssets.Glow(new Color(1f, 0.4f, 0.4f), 1.2f) : materials[i];
            fireLight.intensity = 2.6f + Mathf.PerlinNoise(Time.time * 4f, 0f) * 1.2f;

            // Дым из трубы и огонь, если печь сильно повреждена
            smokeTimer -= Time.deltaTime;
            if (smokeTimer <= 0f)
            {
                smokeTimer = Fraction < 0.35f ? 0.12f : 0.4f;
                Particles.Smoke(transform.position + new Vector3(0.6f, 4f, -0.4f), 0.5f, 1, Fraction < 0.35f ? new Color(0.15f, 0.15f, 0.15f, 0.7f) : new Color(0.9f, 0.9f, 0.9f, 0.4f));
                if (Fraction < 0.35f) Particles.Flash(Center + UnityEngine.Random.insideUnitSphere * 1.5f, new Color(1f, 0.5f, 0.1f), 0.8f, 0.3f);
            }
        }
    }
}
