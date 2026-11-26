using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>Creates the uGUI objects the widgets are made of.</summary>
    internal static class DemoMake
    {
        public static RectTransform Rect(string name, Transform parent, int layer)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = layer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        public static RectTransform Rect(string name, Transform parent) => Rect(name, parent, parent != null ? parent.gameObject.layer : 0);

        /// <summary>Fills the parent. A positive inset makes the rectangle smaller.</summary>
        public static RectTransform Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>A shape that fills its parent and takes no part in the layout.</summary>
        public static DemoShape Backdrop(string name, Transform parent, float inset = 0f)
        {
            RectTransform rt = Stretch(Rect(name, parent), inset);
            rt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return rt.gameObject.AddComponent<DemoShape>();
        }

        public static DemoShape Fill(Transform parent, float radius, Color color)
        {
            DemoShape s = Backdrop("Fill", parent);
            s.SetRounded(radius);
            s.color = color;
            return s;
        }

        public static DemoShape Outline(Transform parent, float radius, float thickness, Color color)
        {
            DemoShape s = Backdrop("Outline", parent);
            s.SetOutline(radius, thickness);
            s.color = color;
            return s;
        }

        public static DemoShape Icon(Transform parent, DemoIcon icon, float size, Color color)
        {
            RectTransform rt = Rect("Icon", parent);
            rt.sizeDelta = new Vector2(size, size);
            Size(rt, size, size);
            var s = rt.gameObject.AddComponent<DemoShape>();
            s.SetIcon(icon);
            s.color = color;
            return s;
        }

        public static TextMeshProUGUI Text(Transform parent, string text, DemoTheme theme, float size, Color color, bool bold = false,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, bool wrap = true)
        {
            RectTransform rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset face = bold ? theme.Bold : theme.Regular;
            if (face != null) t.font = face;
            else if (theme.Regular != null) t.font = theme.Regular;
            t.fontStyle = bold && face == null ? FontStyles.Bold : FontStyles.Normal;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            t.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
            t.lineSpacing = 4f;
            // Inter is spaced for small sizes. Panel text is large, so it is set a little tighter.
            t.characterSpacing = Mathf.Lerp(-0.8f, -2.2f, Mathf.InverseLerp(24f, 50f, size));
            Wrap(t, wrap);
            t.text = text ?? string.Empty;
            return t;
        }

        /// <summary>
        /// Text in one line that gets up to a quarter smaller before it is cut off with an ellipsis, because labels change
        /// at run time.
        /// </summary>
        public static TextMeshProUGUI Line(Transform parent, string text, DemoTheme theme, float size, Color color, bool bold = false,
            TextAlignmentOptions align = TextAlignmentOptions.Left, bool twoLines = false)
        {
            TextMeshProUGUI t = Text(parent, text, theme, size, color, bold, align, twoLines);
            t.overflowMode = TextOverflowModes.Ellipsis;
            if (twoLines) t.lineSpacing = -8f;
            t.fontSizeMax = size;
            t.fontSizeMin = size * 0.75f;
            t.enableAutoSizing = true;
            return t;
        }

        public static void Wrap(TMP_Text t, bool wrap)
        {
#if UNITY_2023_2_OR_NEWER
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
#else
            t.enableWordWrapping = wrap;
#endif
        }

        public static VerticalLayoutGroup Vertical(RectTransform rt, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.UpperLeft)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            Configure(g, spacing, padding, align);
            g.childForceExpandWidth = true;
            return g;
        }

        public static HorizontalLayoutGroup Horizontal(RectTransform rt, float spacing, RectOffset padding = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            Configure(g, spacing, padding, align);
            return g;
        }

        private static void Configure(HorizontalOrVerticalLayoutGroup g, float spacing, RectOffset padding, TextAnchor align)
        {
            g.spacing = spacing;
            if (padding != null) g.padding = padding;
            g.childAlignment = align;
            g.childControlWidth = g.childControlHeight = true;
            g.childForceExpandWidth = g.childForceExpandHeight = false;
        }

        /// <summary>Preferred size of an element inside a layout. Negative values are left alone.</summary>
        public static LayoutElement Size(Component c, float width = -1f, float height = -1f, float flexWidth = -1f, float flexHeight = -1f)
        {
            var e = c.GetComponent<LayoutElement>();
            if (e == null) e = c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0f) { e.minWidth = width; e.preferredWidth = width; }
            if (height >= 0f) { e.minHeight = height; e.preferredHeight = height; }
            if (flexWidth >= 0f) e.flexibleWidth = flexWidth;
            if (flexHeight >= 0f) e.flexibleHeight = flexHeight;
            return e;
        }

        /// <summary>Same width as its neighbours: prefers a width, shares the rest, and may shrink.</summary>
        public static void Equal(Component c, float preferred)
        {
            LayoutElement e = Size(c, -1f, -1f, 1f);
            e.minWidth = 0f;
            e.preferredWidth = preferred;
        }

        public static RectOffset Pad(float all) => Pad(all, all, all, all);

        public static RectOffset Pad(float left, float right, float top, float bottom) =>
            new RectOffset(Mathf.RoundToInt(left), Mathf.RoundToInt(right), Mathf.RoundToInt(top), Mathf.RoundToInt(bottom));

        /// <summary>"Study timing" becomes "study-timing".</summary>
        public static string Slug(string label)
        {
            if (string.IsNullOrEmpty(label)) return "item";
            var b = new System.Text.StringBuilder(label.Length);
            foreach (char c in label)
            {
                if (char.IsLetterOrDigit(c)) b.Append(char.ToLowerInvariant(c));
                else if (b.Length > 0 && b[b.Length - 1] != '-') b.Append('-');
            }
            return b.ToString().Trim('-');
        }
    }
}
