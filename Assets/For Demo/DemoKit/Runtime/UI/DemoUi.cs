using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace DemoKit
{
    /// <summary>Entry point of the kit: builds panels, finds widgets by id, and clicks them from code.</summary>
    public static class DemoUi
    {
        private static readonly List<DemoWidget> Widgets = new List<DemoWidget>();
        private static readonly HashSet<DemoWidget> Known = new HashSet<DemoWidget>();
        private static readonly List<DemoWidget> Enabled = new List<DemoWidget>();
        private static Transform head;

        /// <summary>Raised after every accepted click. The pointer is null for clicks from code.</summary>
        public static event Action<DemoElement, DemoPointer> AnyClicked;

        /// <summary>The theme of new panels.</summary>
        public static DemoTheme Theme { get { return DemoTheme.Current; } set { DemoTheme.Current = value; } }

        /// <summary>The user's head. Falls back to the main camera.</summary>
        public static Transform Head
        {
            get
            {
                if (head != null) return head;
                Camera main = Camera.main;
                return main != null ? main.transform : null;
            }
            set { head = value; }
        }

        // Building --------------------------------------------------------------------------------------------

        /// <summary>
        /// Starts a panel. Width and height are in panel units (millimetres by default). Height 0 fits the content.
        /// </summary>
        public static DemoBuilder Panel(string id, float width = 1000f, float height = 0f, float padding = -1f)
        {
            DemoTheme theme = DemoTheme.Current;
            DemoPanel panel = DemoPanel.Create(id, theme, width, height, padding < 0f ? theme.panelPadding : padding, true);
            return new DemoBuilder(panel, panel.Content, theme);
        }

        /// <summary>A panel without paper, for free-standing buttons or labels.</summary>
        public static DemoBuilder Bare(string id, float width = 600f, float height = 0f)
        {
            DemoTheme theme = DemoTheme.Current;
            DemoPanel panel = DemoPanel.Create(id, theme, width, height, 0f, false);
            return new DemoBuilder(panel, panel.Content, theme);
        }

        /// <summary>Shows a short message. See DemoToast.</summary>
        public static void Toast(string message, DemoTone tone = DemoTone.Neutral, float seconds = 2.6f) => DemoToast.Show(message, tone, seconds);

        // Finding ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Finds an interactive element by id ("next") or path ("start/next"). When several share an id, a usable one
        /// wins over a hidden one. Returns null when there is none.
        /// </summary>
        public static DemoElement Find(string id) => Get<DemoElement>(id);

        /// <summary>Finds any widget by id or path.</summary>
        public static T Get<T>(string id) where T : DemoWidget
        {
            if (string.IsNullOrEmpty(id)) return null;
            int slash = id.IndexOf('/');
            return Search<T>(slash > 0 ? id.Substring(slash + 1) : id, slash > 0 ? id.Substring(0, slash) : null, null);
        }

        internal static T Search<T>(string widgetId, string panelId, DemoPanel panel) where T : DemoWidget
        {
            T best = null;
            for (int i = Widgets.Count - 1; i >= 0; i--)
            {
                DemoWidget w = Widgets[i];
                if (w == null) { Known.Remove(w); Widgets.RemoveAt(i); continue; }
                if (!(w is T match) || w.Id != widgetId) continue;
                if (panelId != null && (w.Panel == null || w.Panel.Id != panelId)) continue;
                if (panel != null && w.Panel != panel) continue;
                if (w is DemoElement e ? e.CanClick : w.gameObject.activeInHierarchy) return match;
                if (best == null) best = match;
            }
            return best;
        }

        /// <summary>The panel with this id, or null.</summary>
        public static DemoPanel GetPanel(string id) => Get<DemoPanel>(id);

        /// <summary>Clicks an element by id or path. Returns false when it is missing, hidden, or disabled.</summary>
        public static bool Click(string id)
        {
            DemoElement e = Find(id);
            return e != null && e.Click();
        }

        /// <summary>
        /// Sets the value of a stepper, slider, switch (0 or 1), toggle row, page stack, or card grid (index) by id or
        /// path, as if the user had done it. Returns false when there is no such widget.
        /// </summary>
        public static bool SetValue(string id, float value)
        {
            DemoWidget w = Get<DemoStepper>(id);
            if (w == null) w = Get<DemoSlider>(id);
            if (w == null) w = Get<DemoSwitch>(id);
            if (w == null) w = Get<DemoToggleRow>(id);
            if (w == null) w = Get<DemoScrollGrid>(id);
            if (w == null) w = Get<DemoPages>(id);
            switch (w)
            {
                case DemoStepper s: s.Value = value; return true;
                case DemoSlider s: s.Value = value; return true;
                case DemoSwitch s: s.On = value > 0.5f; return true;
                case DemoToggleRow t: t.Index = Mathf.RoundToInt(value); return true;
                case DemoPages p: p.Index = Mathf.RoundToInt(value); return true;
                case DemoScrollGrid g:
                    int i = Mathf.RoundToInt(value);
                    if (i < 0 || i >= g.Count) return false;
                    g.Select(g.CardIds[i], true);
                    return true;
                default: return false;
            }
        }

        /// <summary>True when a widget with this id or path exists, is active, and is on a shown panel.</summary>
        public static bool IsVisible(string id)
        {
            DemoWidget w = Get<DemoWidget>(id);
            return w != null && w.IsVisible;
        }

        /// <summary>All panels that are shown right now.</summary>
        public static IEnumerable<DemoPanel> ShownPanels
        {
            get
            {
                for (int i = 0; i < Widgets.Count; i++)
                    if (Widgets[i] is DemoPanel p && p != null && p.IsShown) yield return p;
            }
        }

        /// <summary>All interactive elements, hidden ones included.</summary>
        public static IEnumerable<DemoElement> Elements
        {
            get
            {
                for (int i = 0; i < Widgets.Count; i++)
                    if (Widgets[i] is DemoElement e && e != null) yield return e;
            }
        }

        /// <summary>The widgets whose objects are active, which is what pointers have to look at.</summary>
        internal static List<DemoWidget> Live { get { return Enabled; } }

        // Tests -----------------------------------------------------------------------------------------------

        /// <summary>One line per interactive element: path, state, and whether it can be clicked.</summary>
        public static string Dump()
        {
            var b = new StringBuilder();
            foreach (DemoElement e in Elements)
                b.Append(e.Path).Append('\t').Append(e.State).Append('\t').Append(e.CanClick ? "ready" : "hidden").Append('\n');
            return b.ToString();
        }

        /// <summary>Texts of a panel that do not fit their rectangle. Empty when the layout is sound.</summary>
        public static List<string> FindOverflow(DemoPanel panel)
        {
            var result = new List<string>();
            Rect frame = panel.RectTransform.rect;
            var corners = new Vector3[4];
            foreach (TMP_Text t in panel.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrEmpty(t.text)) continue;
                t.ForceMeshUpdate();
                Rect r = t.rectTransform.rect;
                // The bounds of the lines as laid out, which is what the reader sees.
                Bounds used = t.textBounds;
                string name = "\"" + (t.text.Length > 40 ? t.text.Substring(0, 40) : t.text) + "\"";
                if (t.isTextTruncated) result.Add(name + " is cut off");
                else if (used.size.x > r.width + 2f || used.size.y > r.height + 6f)
                    result.Add(name + " needs " + used.size.x.ToString("0") + " x " + used.size.y.ToString("0")
                        + " and has " + r.width.ToString("0") + " x " + r.height.ToString("0"));
                t.rectTransform.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = panel.transform.InverseTransformPoint(corners[i]);
                    if (p.x < frame.xMin - 1f || p.x > frame.xMax + 1f || p.y < frame.yMin - 1f || p.y > frame.yMax + 1f)
                    {
                        result.Add(name + " leaves the panel");
                        break;
                    }
                }
            }
            return result;
        }

        // Registry --------------------------------------------------------------------------------------------

        internal static void Register(DemoWidget widget) { if (Known.Add(widget)) Widgets.Add(widget); }

        internal static void Unregister(DemoWidget widget) { if (Known.Remove(widget)) Widgets.Remove(widget); }

        internal static void Enter(DemoWidget widget)
        {
            if (IsLive(widget)) return;
            // Objects that are destroyed in edit mode do not say so.
            if (!Application.isPlaying)
                for (int i = Enabled.Count - 1; i >= 0; i--)
                    if (Enabled[i] == null) RemoveLive(i);
            widget.liveIndex = Enabled.Count;
            Enabled.Add(widget);
        }

        internal static void Leave(DemoWidget widget)
        {
            if (!IsLive(widget)) { widget.liveIndex = -1; return; }
            RemoveLive(widget.liveIndex);
            widget.liveIndex = -1;
        }

        /// <summary>The last entry takes the free place.</summary>
        private static void RemoveLive(int i)
        {
            int last = Enabled.Count - 1;
            Enabled[i] = Enabled[last];
            if (!ReferenceEquals(Enabled[i], null)) Enabled[i].liveIndex = i;
            Enabled.RemoveAt(last);
        }

        private static bool IsLive(DemoWidget widget)
        {
            int i = widget.liveIndex;
            return i >= 0 && i < Enabled.Count && ReferenceEquals(Enabled[i], widget);
        }

        internal static void RaiseClicked(DemoElement element, DemoPointer pointer)
        {
            DemoSounds.Click(element);
            AnyClicked?.Invoke(element, pointer);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Widgets.Clear();
            Known.Clear();
            Enabled.Clear();
            head = null;
            AnyClicked = null;
        }
    }
}
