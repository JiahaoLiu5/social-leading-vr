using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DemoKit
{
    /// <summary>One subtitle with its own times, in seconds from the start of the clip.</summary>
    [Serializable]
    public struct DemoCaption
    {
        public float start;
        [Tooltip("0 = until the next caption starts, or until the clip ends.")]
        public float end;
        [TextArea] public string text;

        public DemoCaption(float start, string text, float end = 0f)
        {
            this.start = start;
            this.text = text;
            this.end = end;
        }
    }

    /// <summary>
    /// Subtitles for narration and speech. A dark bar below the view that follows the head lazily and is drawn over the
    /// scene, so that a person who stands close does not cover it.
    /// A long transcript is split into short captions whose times are proportional to their length; captions with known
    /// times can be given as well.
    /// </summary>
    public sealed class DemoCaptionBar : DemoWidget
    {
        private const float SidePadding = 44f;
        private const int LongestPiece = 96;
        private const int Unknown = -2;

        [SerializeField] private TextMeshProUGUI line;
        [SerializeField] private TextMeshProUGUI speaker;
        [SerializeField] private DemoPanel bar;
        [SerializeField] private float holdSeconds = 1.2f;
        [SerializeField] private float maxWidth = 1000f;
        [SerializeField] private float distance = 1.2f;
        [SerializeField] private float height = -0.52f;

        private readonly List<DemoCaption> captions = new List<DemoCaption>();
        private Transform fixedHead;
        private float elapsed, total;
        private int current = -1;
        private bool playing;

        /// <summary>
        /// Optional clock that returns the seconds since the narration started, for example the room time minus the
        /// start of the step. Nothing is shown while it is negative. Without it the bar counts real time.
        /// </summary>
        public Func<float> Clock { get; set; }

        public bool IsPlaying { get { return playing; } }

        public float Elapsed { get { return elapsed; } }

        /// <summary>The caption on screen, or an empty string.</summary>
        public string Text { get { return playing && current >= 0 ? line.text : string.Empty; } }

        public DemoPanel Bar { get { return bar; } }

        /// <summary>Metres in front of the head and above the eyes (negative = below). Used from the next caption on.</summary>
        public void Place(float newDistance, float newHeight)
        {
            distance = newDistance;
            height = newHeight;
            current = Unknown;
        }

        /// <summary>Without a head the bar follows DemoUi.Head, also when that changes.</summary>
        public static DemoCaptionBar Create(Transform head = null, string id = "captions", float width = 1000f, float distance = 1.2f, float height = -0.52f)
        {
            DemoTheme theme = DemoTheme.Current;
            DemoPanel panel = DemoPanel.Create(id + ".panel", theme, width, 0f, 0f, false);
            DemoMake.Fill(panel.Content, 34f, theme.ink);
            RectTransform column = DemoMake.Rect("Column", panel.Content);
            DemoMake.Vertical(column, 4f, DemoMake.Pad(SidePadding, SidePadding, 22f, 26f));
            var c = panel.gameObject.AddComponent<DemoCaptionBar>();
            c.bar = panel;
            c.fixedHead = head;
            c.maxWidth = width;
            c.distance = distance;
            c.height = height;
            c.speaker = DemoMake.Text(column, "", theme, theme.captionSize - 2f, theme.accentHover, true, TextAlignmentOptions.Center, false);
            c.line = DemoMake.Text(column, "", theme, theme.bodySize + 2f, Color.white, false, TextAlignmentOptions.Center);
            c.speaker.gameObject.SetActive(false);
            panel.BlocksPointers = false;
            panel.OnTop(20);
            panel.OverScene();
            c.Bind(id, theme);
            panel.Placement.lazyAngle = 22f;
            panel.Placement.lazyDistance = 0.35f;
            panel.Placement.smoothTime = 0.22f;
            c.Follow();
            panel.Hide(true);
            return c;
        }

        /// <summary>Shows one line. With seconds 0 it stays until the next call.</summary>
        public void Show(string text, float seconds = 0f, string speakerName = null)
        {
            captions.Clear();
            if (!string.IsNullOrEmpty(text)) captions.Add(new DemoCaption(float.MinValue, text, float.MaxValue));
            Begin(seconds > 0f ? seconds : float.MaxValue, speakerName, 0f);
        }

        /// <summary>
        /// Shows a transcript in step with a clip of the given length. Use startAt for people who join late.
        /// </summary>
        public void Play(string transcript, float seconds, string speakerName = null, float startAt = 0f)
        {
            Split(transcript, 0f, Mathf.Max(0.5f, seconds), captions, true);
            Begin(seconds, speakerName, startAt);
        }

        /// <summary>
        /// Shows captions at their own times. One that is too long for two lines is split, in proportion to the length of
        /// its parts.
        /// </summary>
        public void Play(IReadOnlyList<DemoCaption> timed, float seconds, string speakerName = null, float startAt = 0f)
        {
            captions.Clear();
            for (int i = 0; timed != null && i < timed.Count; i++)
            {
                DemoCaption c = timed[i];
                float end = c.end > c.start ? c.end : i + 1 < timed.Count ? timed[i + 1].start : Mathf.Max(seconds, c.start + 1f);
                Split(c.text, c.start, Mathf.Max(0.1f, end - c.start), captions, false);
            }
            Begin(seconds, speakerName, startAt);
        }

        public void Stop()
        {
            playing = false;
            current = -1;
            captions.Clear();
            if (bar != null) bar.Hide();
        }

        private void Begin(float seconds, string speakerName, float startAt)
        {
            total = seconds;
            elapsed = startAt;
            current = Unknown;
            playing = captions.Count > 0;
            speaker.text = speakerName ?? string.Empty;
            speaker.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
            if (!playing) { Stop(); return; }
            // The bar object must be active for its clock to run, also before the first caption.
            Follow();
            bar.Show();
            if (Clock != null) elapsed = Clock();
            Step();
        }

        private void Follow()
        {
            Transform head = fixedHead != null ? fixedHead : DemoUi.Head;
            DemoPlacement p = bar.Placement;
            if (head == null || p.head == head && p.distance == distance && p.height == height) return;
            p.InFrontOf(head, distance, height, DemoFollow.Lazy, DemoBillboard.Full);
        }

        private void Update()
        {
            if (!playing) return;
            elapsed = Clock != null ? Clock() : elapsed + DemoClock.FullDeltaTime;
            Step();
        }

        private void Step()
        {
            if (total < float.MaxValue && elapsed > total + holdSeconds) { Stop(); return; }
            int at = -1;
            for (int i = 0; i < captions.Count && captions[i].start <= elapsed; i++) at = i;
            // A caption stays a moment after its end, so that a short pause does not make the bar flicker.
            if (at >= 0 && elapsed > captions[at].end + holdSeconds) at = -1;
            if (at == current) return;
            current = at;
            Follow();
            line.text = at >= 0 ? captions[at].text : string.Empty;
            bar.Canvas.enabled = at >= 0;
            if (at >= 0) Fit();
        }

        /// <summary>The bar is as wide as its text needs.</summary>
        private void Fit()
        {
            float want = line.GetPreferredValues(line.text, 4000f, 200f).x;
            if (speaker.gameObject.activeSelf) want = Mathf.Max(want, speaker.GetPreferredValues(speaker.text, 4000f, 200f).x);
            float width = Mathf.Clamp(Mathf.Ceil(want) + 2f * SidePadding + 6f, Mathf.Min(300f, maxWidth), maxWidth);
            RectTransform rt = bar.RectTransform;
            if (Mathf.Abs(rt.sizeDelta.x - width) > 0.5f) rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
            bar.Layout();
        }

        /// <summary>Splits at sentence ends, then at commas and spaces, into pieces of at most 96 characters.</summary>
        private static void Split(string transcript, float from, float seconds, List<DemoCaption> result, bool clear)
        {
            if (clear) result.Clear();
            var pieces = new List<string>();
            string rest = (transcript ?? string.Empty).Trim();
            while (rest.Length > 0)
            {
                int cut = rest.Length;
                if (rest.Length > LongestPiece)
                {
                    cut = LastBreak(rest, LongestPiece, ".!?");
                    if (cut < LongestPiece / 3) cut = Mathf.Max(cut, LastBreak(rest, LongestPiece, ",;:"));
                    if (cut < LongestPiece / 3) cut = rest.LastIndexOf(' ', LongestPiece);
                    if (cut <= 0) cut = LongestPiece;
                }
                pieces.Add(rest.Substring(0, cut).Trim());
                rest = rest.Substring(cut).Trim();
            }
            float sum = 0f;
            foreach (string p in pieces) sum += p.Length + 12f;
            float t = from;
            foreach (string p in pieces)
            {
                float d = seconds * (p.Length + 12f) / Mathf.Max(1f, sum);
                result.Add(new DemoCaption(t, p, t + d));
                t += d;
            }
        }

        private static int LastBreak(string s, int max, string marks)
        {
            for (int i = Mathf.Min(max, s.Length - 1); i > 0; i--)
                if (marks.IndexOf(s[i - 1]) >= 0 && s[i] == ' ') return i;
            return 0;
        }
    }
}
