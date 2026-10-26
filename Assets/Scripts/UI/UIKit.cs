using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BrainrotWaves
{
    /// <summary>Набор помощников для создания интерфейса кодом.</summary>
    public static class UIKit
    {
        static Sprite rounded;
        static Sprite circle;

        public static readonly Color Panel = new Color(0.08f, 0.06f, 0.14f, 0.88f);
        public static readonly Color Accent = GameAssets.Hex("#FFC93C");
        public static readonly Color Pink = GameAssets.Hex("#FF5FA2");
        public static readonly Color Cyan = GameAssets.Hex("#3DE0FF");
        public static readonly Color Green = GameAssets.Hex("#5CFF8A");
        public static readonly Color Purple = GameAssets.Hex("#8E44AD");
        public static readonly Color Crystal = GameAssets.Hex("#64FFDA");
        public static readonly Color PlayColor = GameAssets.Hex("#E53977");
        public static readonly Color OkColor = GameAssets.Hex("#2BB673");
        public static readonly Color Dim = new Color(0.03f, 0.02f, 0.08f, 0.82f);

        /// <summary>Скруглённый прямоугольник, нарисованный кодом (растягивается без искажений).</summary>
        public static Sprite Rounded
        {
            get
            {
                if (rounded == null) rounded = MakeRounded(64, 18);
                return rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null) circle = MakeRounded(64, 32);
                return circle;
            }
        }

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            float border = radius;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static RectTransform Canvas(string name, int order)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform Rect(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        public static Image Image(RectTransform parent, string name, Color color, bool rounded = true)
        {
            var rect = Rect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = Rounded;
                image.type = UnityEngine.UI.Image.Type.Sliced;
            }
            image.raycastTarget = false;
            return image;
        }

        public static Text Text(RectTransform parent, string name, string value, int size, TextAnchor anchor, Color? color = null, bool bold = false, bool outline = true)
        {
            var rect = Rect(parent, name);
            var text = rect.gameObject.AddComponent<Text>();
            bool title = bold && size >= 44;
            text.font = title ? FX.TitleFont : FX.Font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = bold && !title ? FontStyle.Bold : FontStyle.Normal;
            text.alignment = anchor;
            text.color = color ?? Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            if (outline)
            {
                var o = rect.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0f, 0.75f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return text;
        }

        /// <summary>Текст, который сам переводится при смене языка.</summary>
        public static Text Text(RectTransform parent, string name, LS value, int size, TextAnchor anchor, Color? color = null, bool bold = false, bool outline = true)
        {
            var text = Text(parent, name, value.Value, size, anchor, color, bold, outline);
            var loc = text.gameObject.AddComponent<LocText>();
            loc.value = value;
            return text;
        }

        public static Image Icon(RectTransform parent, string name, IconKind kind, Color color, Vector2 anchor, Vector2 position, float size, Vector2? pivot = null)
        {
            var rect = Rect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Icons.Get(kind);
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Place(rect, anchor, position, new Vector2(size, size), pivot);
            return image;
        }

        /// <summary>Кнопка с иконкой слева от текста.</summary>
        public static Button IconButton(RectTransform parent, string name, IconKind icon, LS label, Color color, Action onClick, int fontSize = 34)
        {
            var b = Button(parent, name, label, color, onClick, fontSize);
            var rect = b.GetComponent<RectTransform>();
            var text = rect.GetComponentInChildren<Text>();
            text.rectTransform.offsetMin = new Vector2(fontSize * 1.9f, text.rectTransform.offsetMin.y);
            text.alignment = TextAnchor.MiddleLeft;
            Icon(rect, "Icon", icon, Color.white, new Vector2(0f, 0.5f), new Vector2(fontSize * 0.5f, 0f), fontSize * 1.2f, new Vector2(0f, 0.5f));
            return b;
        }

        public static Button Button(RectTransform parent, string name, LS label, Color color, Action onClick, int fontSize = 34)
        {
            var b = Button(parent, name, label.Value, color, onClick, fontSize);
            var text = b.GetComponentInChildren<Text>();
            text.gameObject.AddComponent<LocText>().value = label;
            return b;
        }

        /// <summary>Ползунок (громкость, чувствительность...).</summary>
        public static Slider Slider(RectTransform parent, string name, float min, float max, float value, Action<float> onChange, Color? fillColor = null)
        {
            var root = Rect(parent, name);
            var slider = root.gameObject.AddComponent<Slider>();

            var back = Image(root, "Background", new Color(0f, 0f, 0f, 0.55f));
            back.rectTransform.anchorMin = new Vector2(0f, 0.3f);
            back.rectTransform.anchorMax = new Vector2(1f, 0.7f);
            back.rectTransform.offsetMin = Vector2.zero;
            back.rectTransform.offsetMax = Vector2.zero;

            var fillArea = Rect(root, "FillArea");
            fillArea.anchorMin = new Vector2(0f, 0.3f);
            fillArea.anchorMax = new Vector2(1f, 0.7f);
            fillArea.offsetMin = new Vector2(4f, 3f);
            fillArea.offsetMax = new Vector2(-4f, -3f);
            var fill = Image(fillArea, "Fill", fillColor ?? Accent);
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = Rect(root, "HandleArea");
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(14f, 0f);
            handleArea.offsetMax = new Vector2(-14f, 0f);
            var handle = Image(handleArea, "Handle", Color.white);
            handle.sprite = Circle;
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(30f, 0f);
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);

            back.raycastTarget = true;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return slider;
        }

        public static Button Button(RectTransform parent, string name, string label, Color color, Action onClick, int fontSize = 34)
        {
            var image = Image(parent, name, color);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colors.colorMultiplier = 1.2f;
            button.colors = colors;
            button.onClick.AddListener(() =>
            {
                SoundFX.Play(SoundFX.Sfx.Click, 0.6f);
                onClick?.Invoke();
            });
            var text = Text(image.rectTransform, "Label", label, fontSize, TextAnchor.MiddleCenter, Color.white, true);
            Stretch(text.rectTransform, 10f);
            image.gameObject.AddComponent<ButtonHover>();
            return button;
        }

        public static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }

    /// <summary>Кнопка слегка увеличивается при наведении.</summary>
    public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        bool hover;

        public void OnPointerEnter(PointerEventData eventData) => hover = true;
        public void OnPointerExit(PointerEventData eventData) => hover = false;

        void OnDisable()
        {
            hover = false;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            if (GetComponent<PopIn>() != null) return;
            float target = hover ? 1.06f : 1f;
            float s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 1.5f);
            transform.localScale = Vector3.one * s;
        }
    }
}
