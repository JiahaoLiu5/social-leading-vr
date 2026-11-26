using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>An on and off switch with a label. The whole row can be clicked.</summary>
    public sealed class DemoSwitch : DemoElement
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private DemoShape track;
        [SerializeField] private DemoShape ring;
        [SerializeField] private DemoShape back;
        [SerializeField] private RectTransform knob;
        [SerializeField] private DemoShape knobFill;

        public event Action<bool> Changed;

        public bool On
        {
            get { return Selected; }
            set { if (value == Selected) return; Selected = value; Changed?.Invoke(value); }
        }

        public string Label { get { return label.text; } set { label.text = value ?? string.Empty; } }

        public void SetSilently(bool on) { Selected = on; }

        internal static DemoSwitch Create(Transform parent, string id, string text, bool on, DemoTheme theme)
        {
            RectTransform root = DemoMake.Rect("Switch " + id, parent);
            DemoMake.Vertical(root, 0f);
            DemoMake.Size(root, -1f, 76f);
            var s = root.gameObject.AddComponent<DemoSwitch>();
            s.body = DemoMake.Rect("Body", root);
            s.back = DemoMake.Backdrop("Back", s.body);
            s.back.rectTransform.offsetMin = new Vector2(-16f, 0f);
            s.back.rectTransform.offsetMax = new Vector2(16f, 0f);
            s.back.SetRounded(20f);
            DemoMake.Horizontal(s.body, 20f);
            DemoMake.Size(s.body, -1f, 76f);
            s.label = DemoMake.Line(s.body, text, theme, theme.bodySize, theme.ink, false, TextAlignmentOptions.Left, true);
            DemoMake.Size(s.label, -1f, -1f, 1f);
            RectTransform t = DemoMake.Rect("Track", s.body);
            DemoMake.Size(t, 100f, 56f);
            s.track = t.gameObject.AddComponent<DemoShape>();
            s.track.SetRounded(28f);
            s.ring = DemoMake.Outline(t, 28f, 4f, theme.accent);
            s.knob = DemoMake.Rect("Knob", t);
            DemoMake.Anchor(s.knob, new Vector2(0f, 0.5f), Vector2.one * 0.5f, new Vector2(28f, 0f), Vector2.one * 44f);
            s.knobFill = s.knob.gameObject.AddComponent<DemoShape>();
            s.knobFill.SetRounded(22f);
            s.Selected = on;
            s.Bind(id, theme);
            s.Finish();
            return s;
        }

        protected override void OnClicked() { On = !On; }

        protected override void Draw(float hover, float press, float select, float disable)
        {
            if (track == null || label == null) return;
            DemoTheme t = Theme;
            float on = 1f - disable;
            Color c = Color.Lerp(Color.Lerp(t.line, t.inkFaint, hover * 0.35f), t.accent, select);
            track.color = Color.Lerp(c, t.surfaceDeep, disable);
            ring.color = new Color(t.accentPressed.r, t.accentPressed.g, t.accentPressed.b, 0.65f * hover * on);
            back.color = new Color(t.surface.r, t.surface.g, t.surface.b, hover * on * 0.9f);
            knobFill.color = Color.white;
            knob.anchoredPosition = new Vector2(Mathf.Lerp(28f, 72f, DemoEase.InOutCubic(select)), 0f);
            knob.localScale = Vector3.one * (1f - 0.08f * press);
            label.color = Color.Lerp(t.ink, t.inkFaint, disable);
        }
    }
}
