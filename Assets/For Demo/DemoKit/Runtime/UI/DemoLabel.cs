using TMPro;
using UnityEngine;

namespace DemoKit
{
    public enum DemoTextStyle { Title, Heading, Body, Strong, Caption }

    /// <summary>A text with an id, for content that changes while the panel is shown.</summary>
    public sealed class DemoLabel : DemoWidget
    {
        [SerializeField] private TextMeshProUGUI text;

        /// <summary>The text. Rich text tags work. An empty text takes no space.</summary>
        public string Text
        {
            get { return text.text; }
            set
            {
                text.text = value ?? string.Empty;
                text.gameObject.SetActive(!string.IsNullOrEmpty(value));
            }
        }

        public Color Color { get { return text.color; } set { text.color = value; } }

        /// <summary>The TextMeshPro component, for anything else.</summary>
        public TextMeshProUGUI Mesh { get { return text; } }

        internal static DemoLabel Create(Transform parent, string id, string content, DemoTextStyle style, TextAlignmentOptions align, DemoTheme theme)
        {
            RectTransform root = DemoMake.Rect("Label " + id, parent);
            DemoMake.Vertical(root, 0f);
            var l = root.gameObject.AddComponent<DemoLabel>();
            float size = style == DemoTextStyle.Title ? theme.titleSize : style == DemoTextStyle.Heading ? theme.headingSize : style == DemoTextStyle.Caption ? theme.captionSize : theme.bodySize;
            bool bold = style == DemoTextStyle.Title || style == DemoTextStyle.Heading || style == DemoTextStyle.Strong;
            l.text = DemoMake.Text(root, content, theme, size, style == DemoTextStyle.Caption ? theme.inkSoft : theme.ink, bold, align);
            if (style == DemoTextStyle.Title) l.text.lineSpacing = -6f;
            l.Bind(id, theme);
            l.Text = content;
            return l;
        }
    }
}
