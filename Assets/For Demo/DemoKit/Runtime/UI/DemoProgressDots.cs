using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// Progress through a few steps: plain dots, or numbered steps with labels and a line between them.
    /// </summary>
    public sealed class DemoProgressDots : DemoWidget
    {
        [SerializeField] private int index;
        [SerializeField] private RectTransform[] marks;
        [SerializeField] private DemoShape[] fills;
        [SerializeField] private DemoShape[] lines;
        [SerializeField] private TextMeshProUGUI[] numbers;
        [SerializeField] private DemoShape[] checks;
        [SerializeField] private TextMeshProUGUI[] labels;
        private float shown = -1f;

        /// <summary>The current step, counted from 0.</summary>
        public int Index
        {
            get { return index; }
            set { index = Mathf.Clamp(value, 0, Mathf.Max(0, marks.Length - 1)); if (!Application.isPlaying || shown < 0f) { shown = index; Draw(); } }
        }

        public int Count { get { return marks.Length; } }

        internal static DemoProgressDots CreateDots(Transform parent, string id, int count, int index, DemoTheme theme)
        {
            RectTransform root = DemoMake.Rect("Dots " + id, parent);
            DemoMake.Horizontal(root, 12f, null, TextAnchor.MiddleCenter);
            DemoMake.Size(root, -1f, 16f);
            var d = root.gameObject.AddComponent<DemoProgressDots>();
            d.marks = new RectTransform[count];
            d.fills = new DemoShape[count];
            for (int i = 0; i < count; i++)
            {
                d.marks[i] = DemoMake.Rect("Dot", root);
                DemoMake.Size(d.marks[i], 16f, 16f);
                d.fills[i] = d.marks[i].gameObject.AddComponent<DemoShape>();
                d.fills[i].SetRounded(8f);
            }
            d.Bind(id, theme);
            d.Index = index;
            return d;
        }

        internal static DemoProgressDots CreateSteps(Transform parent, string id, string[] names, int index, DemoTheme theme)
        {
            RectTransform root = DemoMake.Rect("Steps " + id, parent);
            // Some air below, so that the title belongs to its page and not to the steps.
            DemoMake.Horizontal(root, 16f, DemoMake.Pad(0f, 0f, 0f, 10f), TextAnchor.MiddleLeft);
            DemoMake.Size(root, -1f, 56f);
            var d = root.gameObject.AddComponent<DemoProgressDots>();
            int count = names.Length;
            d.marks = new RectTransform[count];
            d.fills = new DemoShape[count];
            d.lines = new DemoShape[Mathf.Max(0, count - 1)];
            d.numbers = new TextMeshProUGUI[count];
            d.checks = new DemoShape[count];
            d.labels = new TextMeshProUGUI[count];
            for (int i = 0; i < count; i++)
            {
                d.marks[i] = DemoMake.Rect("Step", root);
                DemoMake.Size(d.marks[i], 46f, 46f);
                d.fills[i] = d.marks[i].gameObject.AddComponent<DemoShape>();
                d.fills[i].SetRounded(23f);
                d.numbers[i] = DemoMake.Text(d.marks[i], (i + 1).ToString(), theme, theme.captionSize, theme.ink, true, TextAlignmentOptions.Center, false);
                DemoMake.Stretch(d.numbers[i].rectTransform);
                d.numbers[i].overflowMode = TextOverflowModes.Overflow;
                d.checks[i] = DemoMake.Icon(d.marks[i], DemoIcon.Check, 26f, theme.onAccent);
                DemoMake.Anchor(d.checks[i].rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, Vector2.one * 26f);
                d.labels[i] = DemoMake.Text(root, names[i], theme, theme.captionSize + 1f, theme.inkSoft, true, TextAlignmentOptions.Left, false);
                d.labels[i].overflowMode = TextOverflowModes.Overflow;
                if (i >= count - 1) continue;
                RectTransform line = DemoMake.Rect("Line", root);
                DemoMake.Size(line, 24f, 3f, 1f);
                d.lines[i] = line.gameObject.AddComponent<DemoShape>();
                d.lines[i].SetRounded(1f);
            }
            d.Bind(id, theme);
            d.Index = index;
            return d;
        }

        private void Update()
        {
            if (marks == null || Mathf.Approximately(shown, index)) return;
            shown = shown < 0f ? index : DemoEase.Damp(shown, index, 0.06f, DemoClock.DeltaTime);
            if (Mathf.Abs(shown - index) < 0.01f) shown = index;
            Draw();
        }

        private void Draw()
        {
            DemoTheme t = Theme;
            for (int i = 0; i < marks.Length; i++)
            {
                float here = Mathf.Clamp01(1f - Mathf.Abs(shown - i));
                float done = Mathf.Clamp01(shown - i);
                if (labels == null || labels.Length == 0)
                {
                    DemoMake.Size(marks[i], Mathf.Lerp(16f, 44f, here));
                    fills[i].color = Color.Lerp(t.line, t.accent, here);
                    continue;
                }
                fills[i].color = Color.Lerp(Color.Lerp(t.surfaceDeep, t.ink, done), t.accent, here);
                numbers[i].color = new Color(Mathf.Lerp(t.inkSoft.r, 1f, here), Mathf.Lerp(t.inkSoft.g, 1f, here), Mathf.Lerp(t.inkSoft.b, 1f, here), 1f - done);
                checks[i].color = new Color(1f, 1f, 1f, done);
                labels[i].color = Color.Lerp(Color.Lerp(t.inkFaint, t.inkSoft, done), t.ink, here);
                if (i < lines.Length) lines[i].color = Color.Lerp(t.line, t.ink, done);
            }
        }
    }
}
