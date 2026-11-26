using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>A labelled bar from 0 to 1 with a value text, for example the time that is left.</summary>
    public sealed class DemoProgressBar : DemoWidget
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI value;
        [SerializeField] private RectTransform track;
        [SerializeField] private RectTransform bar;
        [SerializeField] private DemoShape fill;
        [SerializeField] private float amount;

        public float Amount { get { return amount; } set { amount = Mathf.Clamp01(value); Apply(); } }

        public string Label { get { return label.text; } set { label.text = value ?? string.Empty; } }

        public string ValueText { get { return value.text; } set { this.value.text = value ?? string.Empty; } }

        public Color Color { get { return fill.color; } set { fill.color = value; } }

        public void Set(float newAmount, string text) { Amount = newAmount; ValueText = text; }

        internal static DemoProgressBar Create(Transform parent, string id, string text, float amount, string valueText, DemoTheme theme)
        {
            RectTransform root = DemoMake.Rect("Progress " + id, parent);
            DemoMake.Vertical(root, 12f);
            var p = root.gameObject.AddComponent<DemoProgressBar>();
            RectTransform head = DemoMake.Rect("Head", root);
            DemoMake.Horizontal(head, 16f);
            p.label = DemoMake.Line(head, text, theme, theme.captionSize + 1f, theme.inkSoft);
            DemoMake.Size(p.label, -1f, -1f, 1f);
            p.value = DemoMake.Text(head, valueText, theme, theme.captionSize + 1f, theme.ink, true, TextAlignmentOptions.Right, false);
            p.value.overflowMode = TextOverflowModes.Overflow;
            p.track = DemoMake.Rect("Track", root);
            DemoMake.Size(p.track, -1f, 14f);
            DemoMake.Fill(p.track, 7f, theme.surfaceDeep);
            p.bar = DemoMake.Rect("Bar", p.track);
            p.bar.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            p.bar.anchorMin = Vector2.zero;
            p.bar.pivot = new Vector2(0f, 0.5f);
            p.fill = p.bar.gameObject.AddComponent<DemoShape>();
            p.fill.SetRounded(7f);
            p.fill.color = theme.accent;
            p.Bind(id, theme);
            p.Amount = amount;
            return p;
        }

        private void Apply()
        {
            if (bar == null) return;
            bar.anchorMax = new Vector2(Mathf.Max(0.02f, amount), 1f);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            fill.enabled = amount > 0.0001f;
        }
    }
}
