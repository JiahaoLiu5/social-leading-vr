using System;
using System.Collections.Generic;
using DemoKit;
using UnityEngine;
using UnityEngine.UI;

namespace SocialLeading.AllInOne
{
    /// <summary>Cards or buttons of which one is selected, each with its own id.</summary>
    public sealed class ChoiceGroup
    {
        private readonly List<DemoElement> items = new List<DemoElement>();
        private int index = -1;

        /// <summary>Raised with the index when the user chooses an item. Not raised by code.</summary>
        public event Action<int> Chosen;

        public int Count => items.Count;

        /// <summary>The selected item, or -1.</summary>
        public int Index
        {
            get => index;
            set
            {
                index = value;
                for (int i = 0; i < items.Count; i++)
                    if (items[i].Selected != (i == index)) items[i].Selected = i == index;
            }
        }

        public DemoElement this[int i] => items[i];

        public void Add(DemoElement item)
        {
            int mine = items.Count;
            items.Add(item);
            item.Clicked += _ =>
            {
                Index = mine;
                Chosen?.Invoke(mine);
            };
        }

        public void SetEnabled(int i, bool on)
        {
            if (items[i].Interactable != on) items[i].Interactable = on;
        }

        public void SetEnabled(bool on)
        {
            for (int i = 0; i < items.Count; i++) SetEnabled(i, on);
        }

        /// <summary>Marks a card, for example as taken. An empty text removes the mark.</summary>
        public void SetTag(int i, string text)
        {
            if (items[i] is DemoCard card && card.Tag != text) card.Tag = text;
        }
    }

    /// <summary>Building blocks that several panels use.</summary>
    public static class PanelParts
    {
        private const float SegmentHeight = 66f;

        /// <summary>A row of options in one groove, with an optional label at the left. Ids are given, not made from labels.</summary>
        public static DemoBuilder Options(this DemoBuilder b, ChoiceGroup group, string label, float labelWidth, params (string id, string label)[] options)
        {
            return b.Row(row =>
            {
                if (!string.IsNullOrEmpty(label))
                    row.Column(c => c.Text(label), 0f).Out(out RectTransform column).Do(() => FixWidth(column, labelWidth));
                row.Row(bar =>
                {
                    foreach ((string id, string text) in options)
                    {
                        bar.Button(id, text, null, DemoStyle.Segment, DemoIcon.None, 0f, SegmentHeight).Out(out DemoButton button);
                        Share(button);
                        group.Add(button);
                    }
                }, 6f).Out(out RectTransform groove).Do(() => MakeGroove(groove, b.Theme));
            });
        }

        /// <summary>Cards side by side, of equal width and height.</summary>
        public static DemoBuilder CardRow(this DemoBuilder b, ChoiceGroup group, float pictureHeight, IEnumerable<DemoCardData> cards, float spacing = 20f)
        {
            return b.Row(row =>
            {
                foreach (DemoCardData data in cards)
                {
                    row.Card(data.id, data, null, pictureHeight).Out(out DemoCard card);
                    group.Add(card);
                }
            }, spacing, TextAnchor.UpperLeft).Out(out RectTransform rect).Do(() => rect.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true);
        }

        /// <summary>Picture cards of all avatars.</summary>
        public static List<DemoCardData> AvatarCards(AvatarCatalog catalog)
        {
            var cards = new List<DemoCardData>();
            foreach (AvatarCatalog.Entry entry in catalog.avatars) cards.Add(new DemoCardData(entry.id, entry.label, null, entry.thumbnail));
            return cards;
        }

        /// <summary>Text cards of all roles, with the colours of the theme.</summary>
        public static List<DemoCardData> RoleCards(RoleCatalog catalog, DemoTheme theme, bool withSentence)
        {
            var cards = new List<DemoCardData>();
            foreach (RoleCatalog.Entry entry in catalog.roles)
            {
                cards.Add(new DemoCardData("role-" + entry.id, entry.label, withSentence ? entry.sentence : null)
                {
                    color = theme.Role(entry.id),
                    icon = cards.Count == 0 ? DemoIcon.Person : DemoIcon.Link
                });
            }
            return cards;
        }

        /// <summary>The same for a row of plain option buttons.</summary>
        public static (string id, string label)[] RoleOptions(RoleCatalog catalog) =>
            Array.ConvertAll(catalog.roles, r => ("role-" + r.id, r.label));

        /// <summary>How the bars at the lower edge follow the view. The same for all of them, so that they stay in line.</summary>
        public static void FollowView(this DemoPanel panel)
        {
            panel.Placement.lazyAngle = 8f;
            panel.Placement.lazyDistance = 0.08f;
            panel.Placement.smoothTime = 0.2f;
        }

        /// <summary>True while the panel is shown on the left hand.</summary>
        public static bool IsHeld(this DemoPanel panel) => panel != null && panel.IsShown && panel.HeldBy == DemoHand.Left;

        /// <summary>"4:05" from seconds.</summary>
        public static string Minutes(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return s / 60 + ":" + (s % 60).ToString("00");
        }

        private static void MakeGroove(RectTransform rect, DemoTheme theme)
        {
            var layout = rect.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 7, 7);
            layout.childForceExpandWidth = true;
            Share(rect);

            var back = new GameObject("Groove", typeof(RectTransform)) { layer = rect.gameObject.layer };
            var backRect = (RectTransform)back.transform;
            backRect.SetParent(rect, false);
            backRect.SetAsFirstSibling();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = backRect.offsetMax = Vector2.zero;
            back.AddComponent<LayoutElement>().ignoreLayout = true;
            var shape = back.AddComponent<DemoShape>();
            shape.raycastTarget = false;
            shape.SetRounded(24f);
            shape.color = theme.surfaceDeep;
        }

        /// <summary>Takes an equal part of the free width.</summary>
        private static void Share(Component widget)
        {
            LayoutElement element = widget.GetComponent<LayoutElement>();
            if (element == null) element = widget.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.minWidth = 0f;
        }

        private static void FixWidth(RectTransform rect, float width)
        {
            LayoutElement element = rect.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }
    }
}
