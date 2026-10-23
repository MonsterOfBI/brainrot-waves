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
            text.font = FX.Font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
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
            float target = hover ? 1.06f : 1f;
            float s = Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 1.5f);
            transform.localScale = Vector3.one * s;
        }
    }
}
