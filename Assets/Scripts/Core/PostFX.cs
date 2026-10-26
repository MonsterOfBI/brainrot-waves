using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BrainrotWaves
{
    /// <summary>
    /// Пост-обработка: свечение (bloom), цветокоррекция, виньетка, эффекты при ранении.
    /// Профиль лежит в Resources/PostFXProfile.asset; для каждой арены свои цвета.
    /// </summary>
    public class PostFX : MonoBehaviour
    {
        public static PostFX Instance { get; private set; }

        Volume volume;
        VolumeProfile profile;
        Bloom bloom;
        Tonemapping tonemapping;
        ColorAdjustments color;
        Vignette vignette;
        ChromaticAberration chroma;
        LiftGammaGain lgg;

        float hurt;
        float baseVignette = 0.22f;
        Color baseVignetteColor = Color.black;
        float baseSaturation = 12f;
        float bossPulse;

        public static PostFX Create()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("PostFX");
            return go.AddComponent<PostFX>();
        }

        void Awake()
        {
            Instance = this;
            var source = Resources.Load<VolumeProfile>("PostFXProfile");
            // Глубокая копия, чтобы изменения во время игры не портили файл профиля
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            if (source != null)
                foreach (var c in source.components)
                    if (c != null) profile.components.Add(Instantiate(c));
            bloom = Get<Bloom>();
            tonemapping = Get<Tonemapping>();
            color = Get<ColorAdjustments>();
            vignette = Get<Vignette>();
            chroma = Get<ChromaticAberration>();
            lgg = Get<LiftGammaGain>();

            tonemapping.mode.Override(TonemappingMode.Neutral);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.65f);
            bloom.highQualityFiltering.Override(GameSettings.Quality >= 2);
            color.postExposure.Override(0.15f);
            color.contrast.Override(12f);
            color.saturation.Override(baseSaturation);
            vignette.intensity.Override(baseVignette);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(Color.black);
            chroma.intensity.Override(0f);

            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.profile = profile;

            GameSettings.Changed += OnSettingsChanged;
        }

        void Start() => OnSettingsChanged();

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            GameSettings.Changed -= OnSettingsChanged;
            if (profile != null)
            {
                foreach (var c in profile.components) if (c != null) Destroy(c);
                Destroy(profile);
            }
        }

        T Get<T>() where T : VolumeComponent
        {
            if (!profile.TryGet(out T component)) component = profile.Add<T>(true);
            component.active = true;
            return component;
        }

        void OnSettingsChanged()
        {
            foreach (var cam in Camera.allCameras) GameSettings.SetupCamera(cam);
            if (bloom != null) bloom.highQualityFiltering.Override(GameSettings.Quality >= 2);
        }

        /// <summary>Цвета под конкретную арену.</summary>
        public void ApplyTheme(ArenaTheme theme)
        {
            Color filter = Color.white;
            float exposure = 0.15f, contrast = 12f, bloomIntensity = 0.8f;
            baseSaturation = 12f;
            baseVignette = 0.22f;
            Vector4 lift = new Vector4(1f, 1f, 1f, 0f), gain = new Vector4(1f, 1f, 1f, 0f);
            switch (theme)
            {
                case ArenaTheme.Beach:
                    filter = new Color(1f, 0.98f, 0.95f); exposure = 0.2f; baseSaturation = 18f;
                    gain = new Vector4(1f, 0.98f, 0.92f, 0.02f);
                    break;
                case ArenaTheme.Forest:
                    filter = new Color(0.97f, 1f, 0.94f); baseSaturation = 10f; bloomIntensity = 0.7f;
                    lift = new Vector4(0.95f, 1f, 0.95f, 0f);
                    break;
                case ArenaTheme.Jungle:
                    filter = new Color(0.98f, 1f, 0.9f); baseSaturation = 20f; bloomIntensity = 0.9f;
                    break;
                case ArenaTheme.Desert:
                    filter = new Color(1f, 0.95f, 0.86f); exposure = 0.1f; contrast = 16f; baseSaturation = 8f;
                    gain = new Vector4(1f, 0.96f, 0.88f, 0f);
                    break;
                case ArenaTheme.City:
                    filter = new Color(1f, 0.92f, 0.98f); baseSaturation = 16f; bloomIntensity = 1.1f;
                    lift = new Vector4(1f, 0.95f, 1f, 0f);
                    break;
                case ArenaTheme.NightVillage:
                    filter = new Color(0.85f, 0.9f, 1.1f); exposure = 0.35f; contrast = 18f; baseSaturation = 6f; bloomIntensity = 1.5f;
                    baseVignette = 0.32f;
                    lift = new Vector4(0.9f, 0.95f, 1.1f, 0f);
                    break;
            }
            color.colorFilter.Override(filter);
            color.postExposure.Override(exposure);
            color.contrast.Override(contrast);
            color.saturation.Override(baseSaturation);
            bloom.intensity.Override(bloomIntensity);
            lgg.lift.Override(lift);
            lgg.gain.Override(gain);
        }

        /// <summary>Мягкие цвета главного меню.</summary>
        public void ApplyMenu()
        {
            color.colorFilter.Override(Color.white);
            color.postExposure.Override(0.25f);
            color.contrast.Override(10f);
            baseSaturation = 15f;
            color.saturation.Override(baseSaturation);
            bloom.intensity.Override(1.2f);
            baseVignette = 0.3f;
        }

        /// <summary>Вспышка при получении урона: красная виньетка и цветные края.</summary>
        public static void Hurt(float amount)
        {
            if (Instance == null) return;
            Instance.hurt = Mathf.Clamp01(Instance.hurt + amount);
        }

        /// <summary>Тяжёлый момент (появление босса) — короткий импульс.</summary>
        public static void Impact(float amount)
        {
            if (Instance == null) return;
            Instance.bossPulse = Mathf.Max(Instance.bossPulse, amount);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            hurt = Mathf.MoveTowards(hurt, 0f, dt * 2.2f);
            bossPulse = Mathf.MoveTowards(bossPulse, 0f, dt * 1.2f);

            float low = 0f;
            var player = PlayerController.Instance;
            if (player != null && !player.Health.IsDead)
            {
                float f = player.Health.Fraction;
                if (f < 0.35f) low = (0.35f - f) / 0.35f * (0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 6f));
            }

            float red = Mathf.Max(hurt, low * 0.8f);
            vignette.intensity.Override(baseVignette + red * 0.25f + bossPulse * 0.15f);
            vignette.color.Override(Color.Lerp(baseVignetteColor, new Color(0.6f, 0f, 0.05f), red));
            chroma.intensity.Override(Mathf.Clamp01(hurt * 0.8f + bossPulse * 0.6f));
            color.saturation.Override(baseSaturation - low * 35f + (HeroAbility.SlowTimeActive ? -25f : 0f));
        }
    }
}
