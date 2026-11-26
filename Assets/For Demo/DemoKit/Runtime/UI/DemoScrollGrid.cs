using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// A grid of cards in pages. Pages are turned with two buttons, so no dragging is needed. One card can be selected.
    /// Cards have the ids "id.cardId"; the page buttons are "id.prev" and "id.next".
    /// </summary>
    public sealed class DemoScrollGrid : DemoWidget
    {
        [SerializeField] private string[] cardIds;
        [SerializeField] private DemoCard[] cards;
        [SerializeField] private RectTransform[] pages;
        [SerializeField] private DemoProgressDots dots;
        [SerializeField] private DemoButton prev;
        [SerializeField] private DemoButton next;
        [SerializeField] private int perPage = 1;
        [SerializeField] private int page;
        [SerializeField] private int selected = -1;
        private bool wired;

        /// <summary>Raised with the card id when the user selects a card.</summary>
        public event Action<string> Changed;

        public int Page { get { return page; } set { ShowPage(value); } }

        public int PageCount { get { return pages.Length; } }

        public int Count { get { return cards.Length; } }

        /// <summary>The id of the selected card, or null.</summary>
        public string SelectedId { get { return selected >= 0 ? cardIds[selected] : null; } }

        public int SelectedIndex { get { return selected; } }

        public IReadOnlyList<string> CardIds { get { return cardIds; } }

        internal static DemoScrollGrid Create(Transform parent, string id, IList<DemoCardData> data, int columns, int rows, float pictureHeight, int selected, DemoTheme theme)
        {
            RectTransform root = DemoMake.Rect("Grid " + id, parent);
            DemoMake.Vertical(root, 20f);
            var g = root.gameObject.AddComponent<DemoScrollGrid>();
            columns = Mathf.Max(1, columns);
            int rowsNeeded = Mathf.Max(1, Mathf.CeilToInt(data.Count / (float)columns));
            rows = rows <= 0 ? rowsNeeded : rows;
            g.perPage = columns * rows;
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(data.Count / (float)g.perPage));
            g.cards = new DemoCard[data.Count];
            g.cardIds = new string[data.Count];
            g.pages = new RectTransform[pageCount];
            const float gap = 20f;
            for (int p = 0; p < pageCount; p++)
            {
                RectTransform pageRoot = DemoMake.Rect("Page " + (p + 1), root);
                DemoMake.Vertical(pageRoot, gap);
                g.pages[p] = pageRoot;
                for (int r = 0; r < rows; r++)
                {
                    int first = p * g.perPage + r * columns;
                    // Empty rows are kept so that every page has the same height.
                    if (first >= data.Count && pageCount == 1) break;
                    RectTransform row = DemoMake.Rect("Row", pageRoot);
                    HorizontalLayoutGroup h = DemoMake.Horizontal(row, gap, null, TextAnchor.UpperLeft);
                    h.childForceExpandHeight = true;
                    // At least this high, more when a caption needs a second line.
                    LayoutElement size = DemoMake.Size(row, -1f, -1f, -1f, 0f);
                    if (pictureHeight > 0f) size.minHeight = RowHeight(data, pictureHeight, theme);
                    for (int c = 0; c < columns; c++)
                    {
                        int i = first + c;
                        if (i >= data.Count)
                        {
                            DemoMake.Size(DemoMake.Rect("Empty", row), 0f, 0f, 1f);
                            continue;
                        }
                        g.cardIds[i] = data[i].id;
                        g.cards[i] = DemoCard.Create(row, id + "." + data[i].id, data[i], theme, pictureHeight);
                    }
                }
            }
            if (pageCount > 1)
            {
                RectTransform pager = DemoMake.Rect("Pager", root);
                DemoMake.Horizontal(pager, 26f, null, TextAnchor.MiddleCenter);
                DemoMake.Size(pager, -1f, 64f);
                g.prev = DemoButton.Create(pager, id + ".prev", null, theme, DemoStyle.Secondary, DemoIcon.Left, 64f, 32f, 28f);
                g.dots = DemoProgressDots.CreateDots(pager, id + ".dots", pageCount, 0, theme);
                g.next = DemoButton.Create(pager, id + ".next", null, theme, DemoStyle.Secondary, DemoIcon.Right, 64f, 32f, 28f);
            }
            g.Bind(id, theme);
            g.Wire();
            g.SelectIndex(selected, false, true);
            g.ShowPage(selected >= 0 ? selected / g.perPage : 0);
            foreach (DemoCard card in g.cards) card.Snap();
            return g;
        }

        private static float RowHeight(IList<DemoCardData> data, float pictureHeight, DemoTheme theme)
        {
            bool captions = false;
            foreach (DemoCardData d in data) captions |= !string.IsNullOrEmpty(d.caption);
            float h = 12f + pictureHeight + 10f + (theme.captionSize + 2f) * 1.3f + 16f;
            return Mathf.Ceil(captions ? h + 10f + (theme.captionSize - 2f) * 1.25f : h);
        }

        public DemoCard Card(string cardId)
        {
            int i = Array.IndexOf(cardIds, cardId);
            return i >= 0 ? cards[i] : null;
        }

        public DemoCard Card(int index) => cards[index];

        /// <summary>Selects a card from code and turns to its page. Changed is raised when notify is true.</summary>
        public void Select(string cardId, bool notify = false)
        {
            int i = Array.IndexOf(cardIds, cardId);
            SelectIndex(i, notify, true);
            if (i >= 0) ShowPage(i / perPage);
        }

        /// <summary>Enables or disables a card and sets its tag, for example "Taken". A null tag is left alone.</summary>
        public void SetEnabled(string cardId, bool on, string tag = null)
        {
            DemoCard card = Card(cardId);
            if (card == null) return;
            card.Interactable = on;
            if (tag != null) card.Tag = tag;
        }

        /// <summary>Enables or disables all cards, for example while nothing may be changed.</summary>
        public void SetEnabled(bool on) { foreach (DemoCard card in cards) card.Interactable = on; }

        /// <summary>Turns to the page of a card.</summary>
        public void Reveal(string cardId)
        {
            int i = Array.IndexOf(cardIds, cardId);
            if (i >= 0) ShowPage(i / perPage);
        }

        private void Awake() { if (cards != null && cards.Length > 0) Wire(); }

        private void Wire()
        {
            if (wired || cards == null) return;
            wired = true;
            for (int i = 0; i < cards.Length; i++)
            {
                int k = i;
                if (cards[i] != null) cards[i].Clicked += e => SelectIndex(k, true, false);
            }
            if (prev != null) prev.Clicked += e => ShowPage(page - 1);
            if (next != null) next.Clicked += e => ShowPage(page + 1);
        }

        private void SelectIndex(int index, bool notify, bool force)
        {
            if (index >= cards.Length) index = -1;
            bool changed = index != selected;
            selected = index;
            for (int i = 0; i < cards.Length; i++) cards[i].Selected = i == index;
            if (notify && (changed || force) && index >= 0) Changed?.Invoke(cardIds[index]);
        }

        private void ShowPage(int target)
        {
            page = Mathf.Clamp(target, 0, pages.Length - 1);
            for (int i = 0; i < pages.Length; i++) pages[i].gameObject.SetActive(i == page);
            if (dots != null) dots.Index = page;
            if (prev != null) prev.Interactable = page > 0;
            if (next != null) next.Interactable = page < pages.Length - 1;
        }
    }
}
