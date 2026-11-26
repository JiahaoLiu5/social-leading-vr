using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// A value with a minus and a plus button, for example a room number or a ratio.
    /// The buttons have the ids "id.minus" and "id.plus".
    /// </summary>
    public sealed class DemoStepper : DemoWidget
    {
        [SerializeField] private float min;
        [SerializeField] private float max = 10f;
        [SerializeField] private float step = 1f;
        [SerializeField] private float value;
        [SerializeField] private string format = "0";
        [SerializeField] private string[] options;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private DemoButton minus;
        [SerializeField] private DemoButton plus;
        private bool wired;

        public event Action<float> Changed;

        /// <summary>The value. With options it is the index of the option.</summary>
        public float Value { get { return value; } set { Set(value, true); } }

        public int IntValue { get { return Mathf.RoundToInt(value); } }

        public string Label { get { return label.text; } set { label.text = value ?? string.Empty; } }

        public bool Interactable
        {
            get { return enabledByUser; }
            set { enabledByUser = value; Refresh(); }
        }

        [SerializeField] private bool enabledByUser = true;

        internal static DemoStepper Create(Transform parent, string id, string text, float min, float max, float step, float value, string format, string[] options, DemoTheme theme, Color groove)
        {
            RectTransform root = DemoMake.Rect("Stepper " + id, parent);
            DemoMake.Horizontal(root, 16f);
            DemoMake.Size(root, -1f, 76f);
            var s = root.gameObject.AddComponent<DemoStepper>();
            s.min = min; s.max = max; s.step = step; s.format = format; s.options = options;
            s.value = value;
            s.label = DemoMake.Line(root, text, theme, theme.bodySize, theme.ink, false, TextAlignmentOptions.Left, true);
            DemoMake.Size(s.label, -1f, -1f, 1f);
            s.label.gameObject.SetActive(!string.IsNullOrEmpty(text));

            RectTransform box = DemoMake.Rect("Value", root);
            DemoMake.Horizontal(box, 6f, DemoMake.Pad(7f), TextAnchor.MiddleCenter);
            DemoMake.Fill(box, 22f, groove);
            DemoMake.Size(box, -1f, 76f);
            s.minus = DemoButton.Create(box, id + ".minus", null, theme, DemoStyle.Secondary, DemoIcon.Minus, 62f, 17f, 26f);
            s.valueText = DemoMake.Text(box, "", theme, theme.bodySize, theme.ink, true, TextAlignmentOptions.Center, false);
            s.valueText.overflowMode = TextOverflowModes.Overflow;
            float width = 110f;
            if (options != null)
                foreach (string o in options) width = Mathf.Max(width, s.valueText.GetPreferredValues(o).x + 24f);
            else
                width = Mathf.Max(width, Mathf.Max(s.valueText.GetPreferredValues(s.Show(min)).x, s.valueText.GetPreferredValues(s.Show(max)).x) + 36f);
            DemoMake.Size(s.valueText, Mathf.Ceil(width));
            s.plus = DemoButton.Create(box, id + ".plus", null, theme, DemoStyle.Secondary, DemoIcon.Plus, 62f, 17f, 26f);
            s.Bind(id, theme);
            s.Wire();
            s.Set(value, false);
            return s;
        }

        /// <summary>Sets the value without raising Changed.</summary>
        public void SetSilently(float newValue) => Set(newValue, false);

        private void Awake() { if (minus != null) { Wire(); Refresh(); } }

        private void Wire()
        {
            if (wired || minus == null) return;
            wired = true;
            minus.Clicked += e => Set(value - step, true);
            plus.Clicked += e => Set(value + step, true);
        }

        private void Set(float newValue, bool notify)
        {
            float snapped = min + Mathf.Round((newValue - min) / Mathf.Max(1e-5f, step)) * step;
            snapped = Mathf.Clamp(snapped, min, max);
            bool changed = Mathf.Abs(snapped - value) > 1e-5f;
            value = snapped;
            Refresh();
            if (changed && notify) Changed?.Invoke(value);
        }

        private void Refresh()
        {
            valueText.text = Show(value);
            valueText.color = enabledByUser ? Theme.ink : Theme.inkFaint;
            label.color = enabledByUser ? Theme.ink : Theme.inkFaint;
            minus.Interactable = enabledByUser && value > min + 1e-5f;
            plus.Interactable = enabledByUser && value < max - 1e-5f;
        }

        private string Show(float v)
        {
            if (options != null && options.Length > 0) return options[Mathf.Clamp(Mathf.RoundToInt(v), 0, options.Length - 1)];
            return v.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
