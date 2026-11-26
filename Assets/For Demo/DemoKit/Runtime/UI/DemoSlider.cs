using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// A value that is set by dragging a handle along a track, with a ray, a fingertip, or the mouse. Pressing the track
    /// moves the handle there.
    /// </summary>
    public sealed class DemoSlider : DemoElement
    {
        private const float HandleSize = 46f, TrackHeight = 14f;

        [SerializeField] private float min;
        [SerializeField] private float max = 1f;
        [SerializeField] private float step;
        [SerializeField] private float value;
        [SerializeField] private string format = "0.00";
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private DemoShape track;
        [SerializeField] private RectTransform bar;
        [SerializeField] private DemoShape barFill;
        [SerializeField] private RectTransform handle;
        [SerializeField] private DemoShape handleFill;
        [SerializeField] private DemoShape handleRing;

        /// <summary>Raised when the value changes, many times while the handle is dragged.</summary>
        public event Action<float> Changed;

        public float Value { get { return value; } set { Set(value, true); } }

        public float Min { get { return min; } }

        public float Max { get { return max; } }

        public string Label { get { return label.text; } set { label.text = value ?? string.Empty; } }

        internal override bool WantsDrag { get { return true; } }

        internal static DemoSlider Create(Transform parent, string id, string text, float min, float max, float value, float step, string format, float width, DemoTheme theme)
        {
            RectTransform row = DemoMake.Rect("Slider " + id, parent);
            DemoMake.Horizontal(row, 22f);
            DemoMake.Size(row, -1f, 76f);
            TextMeshProUGUI label = DemoMake.Line(row, text, theme, theme.bodySize, theme.ink, false, TextAlignmentOptions.Left, true);
            DemoMake.Size(label, -1f, -1f, 1f);
            label.gameObject.SetActive(!string.IsNullOrEmpty(text));

            RectTransform root = DemoMake.Rect("Track", row);
            DemoMake.Size(root, width > 0f ? width : 220f, 76f, width > 0f ? 0f : string.IsNullOrEmpty(text) ? 1f : 0f);
            var s = root.gameObject.AddComponent<DemoSlider>();
            s.min = min;
            s.max = Mathf.Max(min + 1e-4f, max);
            s.step = step;
            s.format = format;
            s.label = label;

            s.body = DemoMake.Stretch(DemoMake.Rect("Body", root));
            RectTransform line = DemoMake.Rect("Line", s.body);
            line.anchorMin = new Vector2(0f, 0.5f);
            line.anchorMax = new Vector2(1f, 0.5f);
            line.sizeDelta = new Vector2(0f, TrackHeight);
            s.track = line.gameObject.AddComponent<DemoShape>();
            s.track.SetRounded(TrackHeight * 0.5f);
            s.bar = DemoMake.Rect("Bar", s.body);
            s.bar.anchorMin = new Vector2(0f, 0.5f);
            s.bar.pivot = new Vector2(0f, 0.5f);
            s.barFill = s.bar.gameObject.AddComponent<DemoShape>();
            s.barFill.SetRounded(TrackHeight * 0.5f);
            s.handle = DemoMake.Rect("Handle", s.body);
            s.handle.sizeDelta = Vector2.one * HandleSize;
            s.handleFill = s.handle.gameObject.AddComponent<DemoShape>();
            s.handleFill.SetRounded(HandleSize * 0.5f);
            s.handleRing = DemoMake.Outline(s.handle, HandleSize * 0.5f, 5f, theme.accent);

            s.valueText = DemoMake.Text(row, "", theme, theme.bodySize, theme.ink, true, TextAlignmentOptions.Right, false);
            s.valueText.overflowMode = TextOverflowModes.Overflow;
            float wide = Mathf.Max(s.valueText.GetPreferredValues(s.Show(min)).x, s.valueText.GetPreferredValues(s.Show(max)).x);
            DemoMake.Size(s.valueText, Mathf.Ceil(Mathf.Max(70f, wide + 8f)));
            s.Bind(id, theme);
            s.Set(value, false);
            s.Finish();
            return s;
        }

        /// <summary>Sets the value without raising Changed.</summary>
        public void SetSilently(float newValue) => Set(newValue, false);

        protected override void OnDragged(Vector2 at)
        {
            float width = Mathf.Max(1f, RectTransform.rect.width);
            float inset = HandleSize * 0.5f / width;
            Set(Mathf.Lerp(min, max, Mathf.InverseLerp(inset, 1f - inset, at.x)), true);
        }

        private void Set(float newValue, bool notify)
        {
            if (step > 1e-6f) newValue = min + Mathf.Round((newValue - min) / step) * step;
            newValue = Mathf.Clamp(newValue, min, max);
            bool changed = Mathf.Abs(newValue - value) > 1e-6f;
            value = newValue;
            Place();
            if (changed && notify) Changed?.Invoke(value);
        }

        private string Show(float v) => v.ToString(format, CultureInfo.InvariantCulture);

        private void Place()
        {
            if (handle == null) return;
            float t = Mathf.InverseLerp(min, max, value), half = HandleSize * 0.5f;
            valueText.text = Show(value);
            handle.anchorMin = handle.anchorMax = new Vector2(t, 0.5f);
            handle.anchoredPosition = new Vector2(Mathf.Lerp(half, -half, t), 0f);
            bar.anchorMax = new Vector2(t, 0.5f);
            bar.offsetMin = new Vector2(0f, -TrackHeight * 0.5f);
            bar.offsetMax = new Vector2(Mathf.Lerp(half, -half, t), TrackHeight * 0.5f);
        }

        protected override void Draw(float hover, float press, float select, float disable)
        {
            if (track == null || label == null) return;
            DemoTheme t = Theme;
            float on = 1f - disable;
            track.color = t.surfaceDeep;
            barFill.color = Color.Lerp(t.accent, t.inkFaint, disable);
            handleFill.color = Color.Lerp(Color.white, t.surface, disable);
            handleRing.color = Color.Lerp(Color.Lerp(t.accent, t.accentPressed, press), t.inkFaint, disable);
            handle.localScale = Vector3.one * (1f + 0.10f * hover * on - 0.06f * press);
            label.color = Color.Lerp(t.ink, t.inkFaint, disable);
            valueText.color = Color.Lerp(t.ink, t.inkFaint, disable);
        }
    }
}
