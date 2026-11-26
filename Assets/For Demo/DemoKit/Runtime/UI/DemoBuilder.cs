using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// Fills a panel (or a row, column, or page of it) from top to bottom. Every method returns the builder, so a whole
    /// panel is one expression. Use Out to keep a reference to the widget that was added last.
    /// </summary>
    public sealed class DemoBuilder
    {
        private readonly RectTransform parent;
        private readonly Color groove;
        private object last;

        public DemoPanel Panel { get; }
        public DemoTheme Theme { get; }

        internal DemoBuilder(DemoPanel panel, RectTransform parent, DemoTheme theme, Color? groove = null)
        {
            this.groove = groove ?? theme.surface;
            Panel = panel;
            this.parent = parent;
            Theme = theme;
        }

        /// <summary>The rectangle that is being filled, for custom content.</summary>
        public RectTransform Root { get { return parent; } }

        public static implicit operator DemoPanel(DemoBuilder b) => b.Build();

        /// <summary>Finishes the panel and computes its layout.</summary>
        public DemoPanel Build()
        {
            Panel.Layout();
            return Panel;
        }

        /// <summary>Returns the widget that was added last.</summary>
        public DemoBuilder Out<T>(out T widget) where T : class
        {
            widget = last as T;
            if (widget == null) Debug.LogError("DemoKit: Out cannot give a " + typeof(T).Name + " here, the last thing added is a " + (last != null ? last.GetType().Name : "nothing"));
            return this;
        }

        /// <summary>Runs code in the middle of a chain.</summary>
        public DemoBuilder Do(Action action)
        {
            action();
            return this;
        }

        // Text ------------------------------------------------------------------------------------------------

        /// <summary>Large title, with an optional line below it.</summary>
        public DemoBuilder Title(string text, string subtitle = null)
        {
            RectTransform box = DemoMake.Rect("Title", parent);
            DemoMake.Vertical(box, 8f);
            TextMeshProUGUI t = DemoMake.Text(box, text, Theme, Theme.titleSize, Theme.ink, true);
            t.lineSpacing = -6f;
            if (!string.IsNullOrEmpty(subtitle)) DemoMake.Text(box, subtitle, Theme, Theme.bodySize - 1f, Theme.inkSoft);
            return Keep(t);
        }

        /// <summary>A heading with an icon button at the right, usually to close the panel.</summary>
        public DemoBuilder Header(string text, string buttonId, Action onClick, DemoIcon icon = DemoIcon.Close)
        {
            RectTransform rt = DemoMake.Rect("Header", parent);
            DemoMake.Horizontal(rt, 16f);
            TextMeshProUGUI t = DemoMake.Text(rt, text, Theme, Theme.headingSize, Theme.ink, true, TextAlignmentOptions.Left);
            DemoMake.Size(t, -1f, -1f, 1f);
            DemoButton b = DemoButton.Create(rt, buttonId, null, Theme, DemoStyle.Quiet, icon, 68f, 34f, 28f);
            if (onClick != null) b.Clicked += e => onClick();
            return Keep(b);
        }

        public DemoBuilder Heading(string text) => Keep(DemoMake.Text(parent, text, Theme, Theme.headingSize, Theme.ink, true));

        public DemoBuilder Text(string text, bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.TopLeft) =>
            Keep(DemoMake.Text(parent, text, Theme, Theme.bodySize, Theme.ink, bold, align));

        public DemoBuilder Text(string text, Color color, float size = 0f, TextAlignmentOptions align = TextAlignmentOptions.TopLeft) =>
            Keep(DemoMake.Text(parent, text, Theme, size > 0f ? size : Theme.bodySize, color, false, align));

        /// <summary>Small secondary text.</summary>
        public DemoBuilder Caption(string text, TextAlignmentOptions align = TextAlignmentOptions.TopLeft) =>
            Keep(DemoMake.Text(parent, text, Theme, Theme.captionSize, Theme.inkSoft, false, align));

        /// <summary>A text with an id, for content that changes: DemoUi.Get&lt;DemoLabel&gt;(id).Text = "...".</summary>
        public DemoBuilder Label(string id, string text, DemoTextStyle style = DemoTextStyle.Body, TextAlignmentOptions align = TextAlignmentOptions.TopLeft) =>
            Keep(DemoLabel.Create(parent, id, text, style, align, Theme));

        /// <summary>A tinted box with an icon, for remarks that must not be missed.</summary>
        public DemoBuilder Note(string text, DemoIcon icon = DemoIcon.Info, DemoTone tone = DemoTone.Accent)
        {
            RectTransform box = DemoMake.Rect("Note", parent);
            Color c = Theme.Tone(tone);
            DemoMake.Horizontal(box, 18f, DemoMake.Pad(24f, 28f, 18f, 20f), TextAnchor.UpperLeft);
            DemoMake.Fill(box, Theme.boxRadius, tone == DemoTone.Neutral ? Theme.surface : Color.Lerp(Theme.paper, c, 0.11f));
            DemoMake.Icon(box, icon, Theme.captionSize + 9f, c);
            TextMeshProUGUI t = DemoMake.Text(box, text, Theme, Theme.captionSize, Color.Lerp(Theme.ink, c, 0.10f));
            DemoMake.Size(t, -1f, -1f, 1f);
            return Keep(t);
        }

        // Layout ----------------------------------------------------------------------------------------------

        /// <summary>Widgets side by side.</summary>
        public DemoBuilder Row(Action<DemoBuilder> fill, float spacing = 20f, TextAnchor align = TextAnchor.MiddleLeft)
        {
            RectTransform rt = DemoMake.Rect("Row", parent);
            DemoMake.Horizontal(rt, spacing, null, align);
            fill(new DemoBuilder(Panel, rt, Theme, groove));
            return Keep(rt);
        }

        /// <summary>Widgets below each other, usually inside a Row. Share is the part of the free width it takes.</summary>
        public DemoBuilder Column(Action<DemoBuilder> fill, float share = 1f, float spacing = -1f)
        {
            RectTransform rt = DemoMake.Rect("Column", parent);
            DemoMake.Vertical(rt, spacing < 0f ? Theme.spacing : spacing);
            DemoMake.Size(rt, 0f, -1f, share);
            fill(new DemoBuilder(Panel, rt, Theme, groove));
            return Keep(rt);
        }

        /// <summary>Columns of equal width.</summary>
        public DemoBuilder Columns(params Action<DemoBuilder>[] columns)
        {
            return Row(row => { foreach (Action<DemoBuilder> c in columns) row.Column(c); }, Theme.spacing + 12f, TextAnchor.UpperLeft);
        }

        /// <summary>A tinted box around widgets.</summary>
        public DemoBuilder Box(Action<DemoBuilder> fill, float padding = 28f, float spacing = 18f)
        {
            RectTransform rt = DemoMake.Rect("Box", parent);
            DemoMake.Vertical(rt, spacing, DemoMake.Pad(padding, padding, padding - 4f, padding - 2f));
            DemoMake.Fill(rt, Theme.boxRadius, Theme.surface);
            DemoMake.Size(rt, -1f, -1f, 1f);
            fill(new DemoBuilder(Panel, rt, Theme, Color.white));
            return Keep(rt);
        }

        /// <summary>Equal cells from left to right and top to bottom.</summary>
        public DemoBuilder Grid(int columns, float spacing, params Action<DemoBuilder>[] cells)
        {
            for (int i = 0; i < cells.Length; i += columns)
            {
                int first = i;
                Row(row =>
                {
                    for (int c = 0; c < columns; c++)
                    {
                        int k = first + c;
                        if (k < cells.Length) row.Column(cells[k]);
                        else row.Column(_ => { });
                    }
                }, spacing, TextAnchor.UpperLeft);
            }
            return this;
        }

        /// <summary>Empty space of a fixed size.</summary>
        public DemoBuilder Space(float size = 12f)
        {
            RectTransform rt = DemoMake.Rect("Space", parent);
            bool horizontal = parent.GetComponent<HorizontalLayoutGroup>() != null;
            DemoMake.Size(rt, horizontal ? size : -1f, horizontal ? -1f : size);
            return Keep(rt);
        }

        /// <summary>Takes all free space. In a row it pushes the rest to the right, in a fixed-height panel down.</summary>
        public DemoBuilder Flex()
        {
            RectTransform rt = DemoMake.Rect("Flex", parent);
            bool horizontal = parent.GetComponent<HorizontalLayoutGroup>() != null;
            DemoMake.Size(rt, -1f, -1f, horizontal ? 1f : -1f, horizontal ? -1f : 1f);
            return Keep(rt);
        }

        /// <summary>A thin line.</summary>
        public DemoBuilder Divider()
        {
            RectTransform rt = DemoMake.Rect("Divider", parent);
            DemoMake.Size(rt, -1f, 2f);
            var s = rt.gameObject.AddComponent<DemoShape>();
            s.SetRounded(1f);
            s.color = Theme.line;
            return Keep(rt);
        }

        // Widgets ---------------------------------------------------------------------------------------------

        public DemoBuilder Button(string id, string label, Action onClick = null, DemoStyle style = DemoStyle.Secondary,
            DemoIcon icon = DemoIcon.None, float width = 0f, float height = 88f, bool iconAfter = false)
        {
            DemoButton b = DemoButton.Create(parent, id, label, Theme, style, icon, height, Theme.buttonRadius, Theme.buttonSize, iconAfter);
            if (width > 0f) b.SetWidth(width);
            if (onClick != null) b.Clicked += e => onClick();
            return Keep(b);
        }

        /// <summary>The one main action of a panel.</summary>
        public DemoBuilder Primary(string id, string label, Action onClick = null, DemoIcon icon = DemoIcon.None, float width = 0f, float height = 88f) =>
            Button(id, label, onClick, DemoStyle.Primary, icon, width, height, true);

        /// <summary>A row of buttons of equal width. Give id and label pairs: Buttons(("a", "One"), ("b", "Two")).</summary>
        public DemoBuilder Buttons(params (string id, string label)[] buttons) => Buttons(null, buttons);

        /// <summary>A row of buttons of equal width with one handler that receives the id.</summary>
        public DemoBuilder Buttons(Action<string> onClick, params (string id, string label)[] buttons)
        {
            RectTransform rt = DemoMake.Rect("Buttons", parent);
            DemoMake.Horizontal(rt, 18f, null, TextAnchor.MiddleCenter);
            var made = new DemoButton[buttons.Length];
            float widest = 0f;
            for (int i = 0; i < buttons.Length; i++)
            {
                string id = buttons[i].id;
                made[i] = DemoButton.Create(rt, id, buttons[i].label, Theme, DemoStyle.Secondary, DemoIcon.None, 88f, Theme.buttonRadius, Theme.buttonSize);
                widest = Mathf.Max(widest, made[i].NaturalWidth(Theme.buttonSize, 88f));
                if (onClick != null) made[i].Clicked += e => onClick(id);
            }
            foreach (DemoButton b in made) DemoMake.Equal(b, widest);
            return Keep(made);
        }

        /// <summary>A single card in the current row or column.</summary>
        public DemoBuilder Card(string id, DemoCardData data, Action onClick = null, float pictureHeight = 0f)
        {
            DemoCard c = DemoCard.Create(parent, id, data, Theme, pictureHeight);
            if (onClick != null) c.Clicked += e => onClick();
            return Keep(c);
        }

        /// <summary>
        /// A grid of cards of which one can be selected. With rows set, the grid has pages. Picture height 0 gives text cards.
        /// </summary>
        public DemoBuilder Cards(string id, IList<DemoCardData> cards, Action<string> onSelect = null, int columns = 4, int rows = 0,
            float pictureHeight = 150f, int selected = -1)
        {
            DemoScrollGrid g = DemoScrollGrid.Create(parent, id, cards, columns, rows, pictureHeight, selected, Theme);
            if (onSelect != null) g.Changed += onSelect;
            return Keep(g);
        }

        /// <summary>A number with minus and plus.</summary>
        public DemoBuilder Stepper(string id, string label, float min, float max, float value, Action<float> onChange = null, float step = 1f, string format = "0")
        {
            DemoStepper s = DemoStepper.Create(parent, id, label, min, max, step, value, format, null, Theme, groove);
            if (onChange != null) s.Changed += onChange;
            return Keep(s);
        }

        /// <summary>A list of options that are stepped through with minus and plus.</summary>
        public DemoBuilder Stepper(string id, string label, string[] options, int index, Action<int> onChange = null)
        {
            DemoStepper s = DemoStepper.Create(parent, id, label, 0f, options.Length - 1, 1f, index, "0", options, Theme, groove);
            if (onChange != null) s.Changed += v => onChange(Mathf.RoundToInt(v));
            return Keep(s);
        }

        /// <summary>
        /// A value that is dragged along a track. Step 0 is continuous. Width is the length of the track (0 = 220, or all
        /// the space when there is no label).
        /// </summary>
        public DemoBuilder Slider(string id, string label, float min, float max, float value, Action<float> onChange = null, float step = 0f, string format = "0.00", float width = 0f)
        {
            DemoSlider s = DemoSlider.Create(parent, id, label, min, max, value, step, format, width, Theme);
            if (onChange != null) s.Changed += onChange;
            return Keep(s);
        }

        /// <summary>One choice out of a few. Stacked puts the label above. With a label width, several rows line up.</summary>
        public DemoBuilder Toggle(string id, string label, string[] options, int index, Action<int> onChange = null, bool stacked = false, float labelWidth = 0f)
        {
            DemoToggleRow t = DemoToggleRow.Create(parent, id, label, options, index, Theme, groove, stacked, labelWidth);
            if (onChange != null) t.Changed += onChange;
            return Keep(t);
        }

        public DemoBuilder Switch(string id, string label, bool on, Action<bool> onChange = null)
        {
            DemoSwitch s = DemoSwitch.Create(parent, id, label, on, Theme);
            if (onChange != null) s.Changed += onChange;
            return Keep(s);
        }

        public DemoBuilder Dots(string id, int count, int index = 0) => Keep(DemoProgressDots.CreateDots(parent, id, count, index, Theme));

        /// <summary>Numbered steps with labels.</summary>
        public DemoBuilder Steps(string id, int index, params string[] names) => Keep(DemoProgressDots.CreateSteps(parent, id, names, index, Theme));

        public DemoBuilder Progress(string id, string label, float amount, string value = "") =>
            Keep(DemoProgressBar.Create(parent, id, label, amount, value, Theme));

        public DemoBuilder Status(string id, string label, string value, DemoTone tone = DemoTone.Neutral) =>
            Keep(DemoStatus.Create(parent, id, label, value, tone, Theme));

        /// <summary>A picture or a RenderTexture, for example the preview of an avatar.</summary>
        public DemoBuilder Picture(Texture texture, float height, float width = 0f, bool cover = true)
        {
            RectTransform rt = DemoMake.Rect("Picture", parent);
            DemoMake.Size(rt, width > 0f ? width : -1f, height, width > 0f ? -1f : 1f);
            DemoMake.Fill(rt, Theme.boxRadius, Theme.surface);
            RectTransform image = DemoMake.Stretch(DemoMake.Rect("Image", rt));
            var p = image.gameObject.AddComponent<DemoPicture>();
            p.raycastTarget = false;
            p.Radius = Theme.boxRadius;
            p.Texture = texture;
            p.Cover = cover;
            return Keep(p);
        }

        /// <summary>Several pages in the same place. The panel keeps the height of the tallest page.</summary>
        public DemoBuilder Pages(string id, Action<DemoPages> fill) => Pages(id, 0f, fill);

        /// <summary>Several pages in the same place, at least this high.</summary>
        public DemoBuilder Pages(string id, float height, Action<DemoPages> fill)
        {
            DemoPages p = DemoPages.Create(parent, id, height, Theme);
            fill(p);
            return Keep(p);
        }

        private DemoBuilder Keep(object widget)
        {
            last = widget;
            return this;
        }
    }
}
