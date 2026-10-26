using UnityEngine;
using UnityEngine.UI;

namespace BrainrotWaves
{
    /// <summary>Плавные функции для анимаций интерфейса.</summary>
    public static class Ease
    {
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
        public static float InOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }
        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t == 0f || t == 1f) return t;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }

    /// <summary>Появление экрана: затемнение и лёгкое увеличение.</summary>
    public class ScreenIntro : MonoBehaviour
    {
        public float duration = 0.25f;
        CanvasGroup group;
        float t;

        void Awake()
        {
            group = gameObject.GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime / duration;
            group.alpha = Ease.OutCubic(t);
            if (t >= 1f) enabled = false;
        }
    }

    /// <summary>Элемент «выпрыгивает» с задержкой (карточки, кнопки).</summary>
    public class PopIn : MonoBehaviour
    {
        public float delay;
        public float duration = 0.35f;
        float t;
        Vector3 target = Vector3.one;

        public static PopIn Add(Component c, float delay, float duration = 0.35f)
        {
            var p = c.gameObject.AddComponent<PopIn>();
            p.delay = delay;
            p.duration = duration;
            return p;
        }

        void Start()
        {
            target = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        void Update()
        {
            if (delay > 0f)
            {
                delay -= Time.unscaledDeltaTime;
                return;
            }
            t += Time.unscaledDeltaTime / duration;
            transform.localScale = target * Ease.OutBack(t);
            if (t >= 1f)
            {
                transform.localScale = target;
                Destroy(this);
            }
        }
    }

    /// <summary>Элемент въезжает со смещением.</summary>
    public class SlideIn : MonoBehaviour
    {
        public Vector2 offset = new Vector2(-400f, 0f);
        public float delay;
        public float duration = 0.45f;
        RectTransform rect;
        Vector2 target;
        float t;
        CanvasGroup group;

        public static SlideIn Add(Component c, Vector2 offset, float delay = 0f, float duration = 0.45f)
        {
            var s = c.gameObject.AddComponent<SlideIn>();
            s.offset = offset;
            s.delay = delay;
            s.duration = duration;
            return s;
        }

        void Start()
        {
            rect = (RectTransform)transform;
            target = rect.anchoredPosition;
            rect.anchoredPosition = target + offset;
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        void Update()
        {
            if (delay > 0f)
            {
                delay -= Time.unscaledDeltaTime;
                return;
            }
            t += Time.unscaledDeltaTime / duration;
            float e = Ease.OutCubic(t);
            rect.anchoredPosition = target + offset * (1f - e);
            group.alpha = e;
            if (t >= 1f)
            {
                rect.anchoredPosition = target;
                group.alpha = 1f;
                Destroy(this);
            }
        }
    }

    /// <summary>Мягкая пульсация размера.</summary>
    public class Pulse : MonoBehaviour
    {
        public float amount = 0.05f;
        public float speed = 3f;
        Vector3 baseScale;

        void Start() => baseScale = transform.localScale;

        void Update()
        {
            transform.localScale = baseScale * (1f + Mathf.Sin(Time.unscaledTime * speed) * amount);
        }
    }

    /// <summary>Покачивание (заголовок меню).</summary>
    public class Wobble : MonoBehaviour
    {
        public float angle = 2f;
        public float lift = 6f;
        public float speed = 1.6f;
        RectTransform rect;
        Vector2 basePos;

        void Start()
        {
            rect = (RectTransform)transform;
            basePos = rect.anchoredPosition;
        }

        void Update()
        {
            float t = Time.unscaledTime * speed;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t) * angle);
            rect.anchoredPosition = basePos + new Vector2(0f, Mathf.Sin(t * 1.3f) * lift);
        }
    }

    /// <summary>Вращение (иконки загрузки).</summary>
    public class Spin : MonoBehaviour
    {
        public float speed = -180f;
        void Update() => transform.Rotate(0f, 0f, speed * Time.unscaledDeltaTime);
    }

    /// <summary>Текст, который сам переводится при смене языка.</summary>
    public class LocText : MonoBehaviour
    {
        public LS value;
        Text text;

        void Awake()
        {
            text = GetComponent<Text>();
            Loc.Changed += Refresh;
        }

        void OnDestroy() => Loc.Changed -= Refresh;

        public void Refresh()
        {
            if (text != null) text.text = value.Value;
        }
    }
}
