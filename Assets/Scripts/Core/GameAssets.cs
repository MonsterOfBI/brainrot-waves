using System.Collections.Generic;
using UnityEngine;

namespace BrainrotWaves
{
    /// <summary>
    /// Базовые материалы игры. Лежит в Resources/GameAssets.asset (создаётся автоматически).
    /// Все цветные материалы делаются копиями этих базовых, чтобы они попали в сборку игры.
    /// </summary>
    [CreateAssetMenu(menuName = "Brainrot Waves/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        public Material lit;
        public Material litEmissive;
        public Material litTransparent;
        public Material unlit;
        public Material skybox;

        static GameAssets instance;
        static readonly Dictionary<long, Material> cache = new Dictionary<long, Material>();

        public static GameAssets I
        {
            get
            {
                if (instance == null) instance = Resources.Load<GameAssets>("GameAssets");
                return instance;
            }
        }

        static long Key(Color c, int kind, float extra)
        {
            Color32 c32 = c;
            long key = (long)c32.r | ((long)c32.g << 8) | ((long)c32.b << 16) | ((long)c32.a << 24);
            key |= (long)kind << 32;
            key |= (long)Mathf.RoundToInt(extra * 100f) << 36;
            return key;
        }

        /// <summary>Обычный матовый или глянцевый материал заданного цвета.</summary>
        public static Material Lit(Color color, float smoothness = 0.3f)
        {
            long key = Key(color, 1, smoothness);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(I.lit);
            SetColor(m, color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            cache[key] = m;
            return m;
        }

        /// <summary>Светящийся материал (фонари, глаза, вспышки).</summary>
        public static Material Glow(Color color, float intensity = 2f)
        {
            long key = Key(color, 2, intensity);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(I.litEmissive);
            SetColor(m, color);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * intensity);
            cache[key] = m;
            return m;
        }

        /// <summary>Полупрозрачный материал (вода, эффекты). Каждый вызов — новый материал, чтобы менять прозрачность.</summary>
        public static Material Transparent(Color color)
        {
            var m = new Material(I.litTransparent);
            SetColor(m, color);
            return m;
        }

        /// <summary>Материал без освещения (трассеры пуль).</summary>
        public static Material Unlit(Color color)
        {
            long key = Key(color, 3, 0f);
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(I.unlit);
            SetColor(m, color);
            cache[key] = m;
            return m;
        }

        public static void SetColor(Material m, Color color)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            m.color = color;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c);
            return c;
        }
    }
}
