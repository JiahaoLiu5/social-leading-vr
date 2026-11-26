using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>A short message in front of the user that disappears on its own. Messages queue up.</summary>
    public sealed class DemoToast : DemoWidget
    {
        private struct Message { public string text; public DemoTone tone; public float seconds; }

        private const int MaxWaiting = 4;

        private static DemoToast instance;

        [SerializeField] private DemoPanel panel;
        [SerializeField] private DemoShape icon;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private RectTransform row;

        private readonly Queue<Message> queue = new Queue<Message>();
        private float left;
        private bool showing;

        /// <summary>Distance and height (relative to the eyes) of toasts, in metres.</summary>
        public static float Distance = 1.2f;
        public static float Height = 0.36f;

        /// <summary>The message on screen, or null.</summary>
        public static string Current { get { return instance != null && instance.showing ? instance.label.text : null; } }

        /// <summary>The panel of the toast, or null before the first message.</summary>
        public static DemoPanel CurrentPanel { get { return instance != null ? instance.panel : null; } }

        /// <summary>Shows a message as soon as the earlier ones are gone.</summary>
        public static void Show(string message, DemoTone tone = DemoTone.Neutral, float seconds = 2.6f)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (instance == null) instance = Create();
            if (instance.showing && instance.label.text == message)
            {
                // The same message again keeps the one on screen.
                instance.left = Mathf.Max(instance.left, seconds);
                return;
            }
            foreach (Message m in instance.queue)
                if (m.text == message) return;
            while (instance.queue.Count >= MaxWaiting) instance.queue.Dequeue();
            instance.queue.Enqueue(new Message { text = message, tone = tone, seconds = seconds });
            if (!instance.showing) instance.Next();
        }

        /// <summary>Removes the current and all waiting messages.</summary>
        public static void Clear()
        {
            if (instance == null) return;
            instance.queue.Clear();
            instance.showing = false;
            instance.panel.Hide();
        }

        private static DemoToast Create()
        {
            DemoTheme theme = DemoTheme.Current;
            DemoPanel p = DemoPanel.Create("toast.panel", theme, 700f, 0f, 0f, false);
            DemoShape shadow = DemoMake.Backdrop("Shadow", p.Content, -44f);
            shadow.SetShadow(34f, 44f);
            shadow.color = new Color(theme.shadow.r, theme.shadow.g, theme.shadow.b, theme.shadow.a * 0.8f);
            DemoMake.Fill(p.Content, 34f, theme.paper);
            DemoMake.Outline(p.Content, 34f, 2f, theme.line);
            var t = p.gameObject.AddComponent<DemoToast>();
            t.panel = p;
            t.row = DemoMake.Rect("Row", p.Content);
            DemoMake.Horizontal(t.row, 18f, DemoMake.Pad(26f, 34f, 20f, 20f));
            t.icon = DemoMake.Icon(t.row, DemoIcon.Info, 40f, theme.inkSoft);
            t.label = DemoMake.Text(t.row, "", theme, theme.bodySize - 1f, theme.ink, false, TextAlignmentOptions.Left);
            DemoMake.Size(t.label, -1f, -1f, 1f);
            p.BlocksPointers = false;
            p.OnTop(30);
            p.OverScene();
            p.FadeSeconds = 0.18f;
            t.Bind("toast", theme);
            if (Application.isPlaying) DontDestroyOnLoad(p.gameObject);
            p.Hide(true);
            return t;
        }

        private void Next()
        {
            if (queue.Count == 0)
            {
                showing = false;
                panel.Hide();
                return;
            }
            Message m = queue.Dequeue();
            showing = true;
            left = m.seconds;
            DemoTheme t = Theme;
            icon.SetIcon(m.tone == DemoTone.Good ? DemoIcon.Check : m.tone == DemoTone.Warning || m.tone == DemoTone.Danger ? DemoIcon.Warning : DemoIcon.Info);
            icon.color = t.Tone(m.tone);
            label.text = m.text;
            float width = Mathf.Clamp(label.GetPreferredValues(m.text, 2000f, 200f).x + 26f + 34f + 18f + 40f + 8f, 180f, 920f);
            panel.RectTransform.sizeDelta = new Vector2(width, panel.RectTransform.sizeDelta.y);
            if (DemoUi.Head != null) panel.ShowInFrontOf(null, Distance, Height, DemoFollow.Lazy, DemoBillboard.Full);
            else panel.Show();
            panel.Layout();
        }

        private void Update()
        {
            if (!showing) return;
            left -= DemoClock.DeltaTime;
            if (left <= 0f) Next();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (instance == this) instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            Distance = 1.2f;
            Height = 0.36f;
        }
    }
}
