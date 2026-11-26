using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// A label and a row of options of which one is selected. Each option is a button with the id "id.option",
    /// where option is the label in lower case with hyphens ("Study timing" gives "pace.study-timing").
    /// </summary>
    public sealed class DemoToggleRow : DemoWidget
    {
        [SerializeField] private string[] options;
        [SerializeField] private int index;
        [SerializeField] private DemoButton[] buttons;
        [SerializeField] private TextMeshProUGUI label;
        private bool wired;

        /// <summary>Raised with the new index when the selection changes.</summary>
        public event Action<int> Changed;

        /// <summary>The selected option, or -1.</summary>
        public int Index { get { return index; } set { Set(value, true); } }

        public string Value { get { return index >= 0 && index < options.Length ? options[index] : null; } }

        public int Count { get { return buttons.Length; } }

        public DemoButton Option(int i) => buttons[i];

        internal static DemoToggleRow Create(Transform parent, string id, string text, string[] options, int index, DemoTheme theme, Color groove, bool stacked, float labelWidth)
        {
            RectTransform root = DemoMake.Rect("Toggle " + id, parent);
            bool hasLabel = !string.IsNullOrEmpty(text);
            if (stacked) DemoMake.Vertical(root, 12f); else DemoMake.Horizontal(root, 20f);
            var t = root.gameObject.AddComponent<DemoToggleRow>();
            t.options = options;
            t.index = index;
            if (hasLabel)
            {
                // Above the options there is room for a long question, beside them there is not.
                t.label = stacked ? DemoMake.Text(root, text, theme, theme.bodySize, theme.ink) : DemoMake.Line(root, text, theme, theme.bodySize, theme.ink);
                if (!stacked) DemoMake.Size(t.label, labelWidth > 0f ? labelWidth : -1f, -1f, labelWidth > 0f ? -1f : 1f);
            }

            RectTransform bar = DemoMake.Rect("Options", root);
            HorizontalLayoutGroup row = DemoMake.Horizontal(bar, 6f, DemoMake.Pad(7f), TextAnchor.MiddleCenter);
            row.childForceExpandWidth = true;
            DemoMake.Fill(bar, 24f, groove);
            DemoMake.Size(bar, -1f, 80f, stacked || !hasLabel || labelWidth > 0f ? 1f : -1f);
            t.buttons = new DemoButton[options.Length];
            float widest = 0f;
            var slugs = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < options.Length; i++)
            {
                // "+1" and "-1" have the same slug. The second one becomes "1-2".
                string slug = DemoMake.Slug(options[i]), unique = slug;
                for (int n = 2; !slugs.Add(unique); n++) unique = slug + "-" + n;
                t.buttons[i] = DemoButton.Create(bar, id + "." + unique, options[i], theme, DemoStyle.Segment, DemoIcon.None, 66f, 18f, theme.bodySize - 2f);
                widest = Mathf.Max(widest, t.buttons[i].NaturalWidth(theme.bodySize - 2f, 66f));
            }
            foreach (DemoButton b in t.buttons) DemoMake.Equal(b, widest);
            t.Bind(id, theme);
            t.Wire();
            t.Set(index, false);
            foreach (DemoButton b in t.buttons) b.Snap();
            return t;
        }

        /// <summary>Selects an option without raising Changed.</summary>
        public void SetSilently(int newIndex) => Set(newIndex, false);

        /// <summary>False if no option can be chosen.</summary>
        public bool Interactable
        {
            get { foreach (DemoButton b in buttons) if (b.Interactable) return true; return false; }
            set
            {
                foreach (DemoButton b in buttons) b.Interactable = value;
                if (label != null) label.color = value ? Theme.ink : Theme.inkFaint;
            }
        }

        public void SetInteractable(bool on) { Interactable = on; }

        private void Awake() { if (buttons != null) Wire(); }

        private void Wire()
        {
            if (wired || buttons == null) return;
            wired = true;
            for (int i = 0; i < buttons.Length; i++)
            {
                int k = i;
                buttons[i].Clicked += e => Set(k, true);
            }
        }

        private void Set(int newIndex, bool notify)
        {
            newIndex = Mathf.Clamp(newIndex, -1, buttons.Length - 1);
            bool changed = newIndex != index;
            index = newIndex;
            for (int i = 0; i < buttons.Length; i++) buttons[i].Selected = i == index;
            if (changed && notify) Changed?.Invoke(index);
        }
    }
}
