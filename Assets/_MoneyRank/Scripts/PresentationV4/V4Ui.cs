using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MoneyRank.V4.Presentation
{
    public enum UiAnchor { TL, TR, BL, BR, TC, BC, C, Stretch }

    /// <summary>Tiny code-built uGUI toolkit (flat design, brand palette). Reference resolution 1920x1080.</summary>
    public static class V4Ui
    {
        public static readonly Color Ink = new Color(0.03f, 0.03f, 0.035f, 1f);
        public static readonly Color Panel = new Color(0.07f, 0.065f, 0.06f, 0.90f);
        public static readonly Color PanelLight = new Color(0.17f, 0.145f, 0.10f, 0.96f);
        public static readonly Color Gold = new Color(0.91f, 0.73f, 0.24f, 1f);
        public static readonly Color Teal = new Color(0.46f, 0.34f, 0.09f, 1f);   // deep gold (brand), name kept for existing call sites
        public static readonly Color Paper = new Color(0.97f, 0.96f, 0.91f, 1f);
        public static readonly Color TextMain = new Color(0.95f, 0.96f, 0.98f, 1f);
        public static readonly Color TextMuted = new Color(0.72f, 0.68f, 0.58f, 1f);
        public static readonly Color Good = new Color(0.30f, 0.80f, 0.48f, 1f);
        public static readonly Color Bad = new Color(1f, 0.38f, 0.38f, 1f);
        public static readonly Color Dim = new Color(0f, 0f, 0f, 0.62f);

        private static Font _font;
        public static Font Font => _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform Rect(Transform parent, string name, UiAnchor a, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            Place(rt, a, x, y, w, h);
            return rt;
        }

        public static void Place(RectTransform rt, UiAnchor a, float x, float y, float w, float h)
        {
            Vector2 min, max, pivot;
            switch (a)
            {
                case UiAnchor.TL: min = max = pivot = new Vector2(0f, 1f); break;
                case UiAnchor.TR: min = max = pivot = new Vector2(1f, 1f); break;
                case UiAnchor.BL: min = max = pivot = new Vector2(0f, 0f); break;
                case UiAnchor.BR: min = max = pivot = new Vector2(1f, 0f); break;
                case UiAnchor.TC: min = max = pivot = new Vector2(0.5f, 1f); break;
                case UiAnchor.BC: min = max = pivot = new Vector2(0.5f, 0f); break;
                case UiAnchor.C: min = max = pivot = new Vector2(0.5f, 0.5f); break;
                default:
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(x, y); rt.offsetMax = new Vector2(-w, -h);
                    return;
            }
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
            var signY = (a == UiAnchor.TL || a == UiAnchor.TR || a == UiAnchor.TC) ? -1f : 1f;
            var signX = (a == UiAnchor.TR || a == UiAnchor.BR) ? -1f : 1f;
            rt.anchoredPosition = new Vector2(x * signX, y * signY);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static Image Box(Transform parent, string name, Color color, UiAnchor a, float x, float y, float w, float h)
        {
            var rt = Rect(parent, name, a, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align, UiAnchor a, float x, float y, float w, float h, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, "Text", a, x, y, w, h);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        public static Button Btn(Transform parent, string text, int size, Color bg, Color fg, UiAnchor a, float x, float y, float w, float h, UnityAction onClick)
        {
            var img = Box(parent, "Btn " + text, bg, a, x, y, w, h);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f); colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            btn.colors = colors;
            var t = Label(img.transform, text, size, fg, TextAnchor.MiddleCenter, UiAnchor.Stretch, 4, 2, 4, 2, FontStyle.Bold);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        public static InputField Input(Transform parent, string initial, int size, UiAnchor a, float x, float y, float w, float h, UnityAction<string> onEnd)
        {
            var img = Box(parent, "Input", new Color(0.04f, 0.08f, 0.12f, 1f), a, x, y, w, h);
            var field = img.gameObject.AddComponent<InputField>();
            var text = Label(img.transform, initial, size, TextMain, TextAnchor.MiddleLeft, UiAnchor.Stretch, 10, 2, 10, 2);
            text.raycastTarget = true;
            field.textComponent = text; field.text = initial; field.targetGraphic = img;
            if (onEnd != null) field.onEndEdit.AddListener(onEnd);
            return field;
        }

        public static void Clear(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }

        public static string Money(int v) => v.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + " CRIA Coin";
        public static string Dec(decimal v) => v.ToString("0.00", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
    }
}
