using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>A line with a coloured dot, a label, and a value, for example "Connection: connected".</summary>
    public sealed class DemoStatus : DemoWidget
    {
        [SerializeField] private DemoShape dot;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI value;

        public string Label
        {
            get { return label.text; }
            set
            {
                label.text = value ?? string.Empty;
                label.GetComponent<LayoutElement>().minWidth = label.GetPreferredValues(label.text, 4000f, 200f).x;
            }
        }

        public string Value { get { return value.text; } }

        public void Set(string text, DemoTone tone = DemoTone.Neutral)
        {
            value.text = text ?? string.Empty;
            dot.color = tone == DemoTone.Neutral ? Theme.inkFaint : Theme.Tone(tone);
        }

        internal static DemoStatus Create(Transform parent, string id, string text, string valueText, DemoTone tone, DemoTheme theme)
        {
            RectTransform root = DemoMake.Rect("Status " + id, parent);
            DemoMake.Horizontal(root, 14f);
            // A long value takes more lines.
            DemoMake.Size(root, -1f, -1f).minHeight = 44f;
            var s = root.gameObject.AddComponent<DemoStatus>();
            RectTransform d = DemoMake.Rect("Dot", root);
            DemoMake.Size(d, 18f, 18f);
            s.dot = d.gameObject.AddComponent<DemoShape>();
            s.dot.SetRounded(9f);
            s.label = DemoMake.Text(root, text, theme, theme.captionSize + 1f, theme.inkSoft, false, TextAlignmentOptions.Left, false);
            s.label.overflowMode = TextOverflowModes.Overflow;
            s.label.gameObject.AddComponent<LayoutElement>().minWidth = s.label.GetPreferredValues(text ?? string.Empty, 4000f, 200f).x;
            s.value = DemoMake.Text(root, valueText, theme, theme.captionSize + 1f, theme.ink, true, TextAlignmentOptions.Right);
            DemoMake.Size(s.value, -1f, -1f, 1f);
            s.Bind(id, theme);
            s.Set(valueText, tone);
            return s;
        }
    }
}
