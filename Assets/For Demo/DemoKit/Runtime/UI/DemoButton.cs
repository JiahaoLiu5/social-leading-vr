using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    public enum DemoStyle
    {
        /// <summary>Filled with the accent colour. One per panel.</summary>
        Primary,
        /// <summary>White with a thin outline.</summary>
        Secondary,
        /// <summary>No background until hovered.</summary>
        Quiet,
        /// <summary>One option of a DemoToggleRow.</summary>
        Segment
    }

    /// <summary>A button with a label and an optional icon.</summary>
    public sealed class DemoButton : DemoElement
    {
        private const float IconScale = 1.26f;

        [SerializeField] private DemoStyle style;
        [SerializeField] private DemoShape fill;
        [SerializeField] private DemoShape line;
        [SerializeField] private DemoShape ring;
        [SerializeField] private DemoShape icon;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float minWidth;

        public DemoStyle Style { get { return style; } set { style = value; Snap(); } }

        public string Label
        {
            get { return label != null ? label.text : string.Empty; }
            set
            {
                if (label == null) return;
                label.text = value ?? string.Empty;
                label.gameObject.SetActive(!string.IsNullOrEmpty(value));
                FitWidth();
            }
        }

        public DemoIcon Icon
        {
            get { return icon != null ? icon.IconName : DemoIcon.None; }
            set
            {
                if (icon == null) return;
                icon.gameObject.SetActive(value != DemoIcon.None);
                if (value != DemoIcon.None) icon.SetIcon(value);
            }
        }

        /// <summary>At least this wide. A longer label makes the button wider where there is room.</summary>
        internal void SetWidth(float width)
        {
            minWidth = width;
            DemoMake.Size(this, width);
            FitWidth();
        }

        private void FitWidth()
        {
            if (minWidth > 0f) DemoMake.Size(this).preferredWidth = Mathf.Max(minWidth, NaturalWidth(label.fontSizeMax, RectTransform.rect.height > 1f ? RectTransform.rect.height : 88f));
        }

        /// <summary>Width the button takes when nothing limits it.</summary>
        internal float NaturalWidth(float fontSize, float height) =>
            label.GetPreferredValues(label.text, 4000f, 200f).x + 2f * Mathf.Round(height * 0.34f) + (Icon != DemoIcon.None ? fontSize * IconScale + 14f : 0f) + 4f;

        internal static DemoButton Create(Transform parent, string id, string text, DemoTheme theme, DemoStyle style, DemoIcon iconName, float height, float radius, float fontSize, bool iconAfter = false)
        {
            RectTransform root = DemoMake.Rect("Button " + id, parent);
            DemoMake.Vertical(root, 0f);
            var b = root.gameObject.AddComponent<DemoButton>();
            b.style = style;
            bool iconOnly = string.IsNullOrEmpty(text);
            DemoMake.Size(root, iconOnly ? height : -1f, height, 0f);

            b.body = DemoMake.Rect("Body", root);
            b.fill = DemoMake.Fill(b.body, radius, Color.white);
            b.line = DemoMake.Outline(b.body, radius, 2.5f, theme.line);
            b.ring = DemoMake.Outline(b.body, radius, 4f, theme.accent);
            float side = iconOnly ? 0f : Mathf.Round(height * 0.34f);
            HorizontalLayoutGroup row = DemoMake.Horizontal(b.body, 14f, DemoMake.Pad(side, side, 0f, 0f), TextAnchor.MiddleCenter);
            row.childForceExpandHeight = false;
            DemoMake.Size(b.body, -1f, height);

            if (!iconAfter) b.icon = DemoMake.Icon(b.body, iconName, Mathf.Round(fontSize * IconScale), theme.ink);
            b.label = DemoMake.Line(b.body, text, theme, fontSize, theme.ink, true, TextAlignmentOptions.Center);
            b.label.gameObject.SetActive(!iconOnly);
            if (iconAfter) b.icon = DemoMake.Icon(b.body, iconName, Mathf.Round(fontSize * IconScale), theme.ink);
            b.icon.gameObject.SetActive(iconName != DemoIcon.None);
            b.Bind(id, theme);
            b.Finish();
            return b;
        }

        protected override void Draw(float hover, float press, float select, float disable)
        {
            if (fill == null || label == null) return;
            DemoTheme t = Theme;
            Color back, text, edge = t.line;
            float edgeAlpha = 0f, ringAlpha = 0f;
            switch (style)
            {
                case DemoStyle.Primary:
                    back = Color.Lerp(Color.Lerp(t.accent, t.accentHover, hover), t.accentPressed, press);
                    back = Color.Lerp(back, t.surfaceDeep, disable);
                    text = Color.Lerp(t.onAccent, t.inkFaint, disable);
                    break;
                case DemoStyle.Quiet:
                    back = t.surface;
                    back.a = Mathf.Max(hover, press);
                    back = Color.Lerp(back, t.surfaceDeep, press);
                    text = Color.Lerp(Color.Lerp(t.inkSoft, t.ink, hover), t.inkFaint, disable);
                    ringAlpha = hover * 0.45f;
                    break;
                case DemoStyle.Segment:
                    // The unselected segment is transparent: the row behind it shows through.
                    back = new Color(1f, 1f, 1f, Mathf.Max(select, hover * 0.7f));
                    back = Color.Lerp(back, t.accentSoft, select * 0.8f);
                    back = Color.Lerp(back, t.surfaceDeep, press);
                    text = Color.Lerp(Color.Lerp(t.inkSoft, t.ink, hover), t.accentPressed, select);
                    text = Color.Lerp(text, t.inkFaint, disable);
                    ringAlpha = Mathf.Max(select, hover * 0.4f);
                    break;
                default:
                    back = Color.Lerp(Color.Lerp(Color.white, t.surface, hover * 0.8f), t.surfaceDeep, press);
                    back = Color.Lerp(back, t.accentSoft, select);
                    back = Color.Lerp(back, t.surface, disable);
                    text = Color.Lerp(Color.Lerp(t.ink, t.accentPressed, select), t.inkFaint, disable);
                    edge = Color.Lerp(t.line, t.inkFaint, hover * 0.6f);
                    edgeAlpha = 1f;
                    ringAlpha = Mathf.Max(select, hover * 0.6f);
                    break;
            }
            ringAlpha *= 1f - disable;
            edge.a = edgeAlpha * (1f - disable * 0.4f);
            fill.color = back;
            line.color = edge;
            ring.color = new Color(t.accent.r, t.accent.g, t.accent.b, ringAlpha);
            label.color = text;
            if (icon != null) icon.color = text;
            if (body != null)
            {
                float lift = (hover - press) * (1f - disable);
                body.localScale = Vector3.one * (1f + 0.02f * hover * (1f - disable) - 0.045f * press);
                Vector3 p = body.localPosition;
                p.z = -6f * lift;
                body.localPosition = p;
            }
        }
    }
}
