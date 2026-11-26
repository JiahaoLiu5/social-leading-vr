using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>Several pages in the same place of a panel, of which one is shown. Used for step-by-step panels.</summary>
    public sealed class DemoPages : DemoWidget
    {
        [SerializeField] private List<string> names = new List<string>();
        [SerializeField] private List<RectTransform> pages = new List<RectTransform>();
        [SerializeField] private int index;
        [SerializeField] private bool fitTallest;
        private float fade = 1f;

        /// <summary>Raised with the new index when another page is shown.</summary>
        public event Action<int> Changed;

        public int Index { get { return index; } set { Go(value); } }

        public int Count { get { return pages.Count; } }

        public string Current { get { return index >= 0 && index < names.Count ? names[index] : null; } }

        public bool IsFirst { get { return index <= 0; } }

        public bool IsLast { get { return index >= pages.Count - 1; } }

        internal static DemoPages Create(Transform parent, string id, float height, DemoTheme theme)
        {
            RectTransform root = DemoMake.Rect("Pages " + id, parent);
            VerticalLayoutGroup v = DemoMake.Vertical(root, 0f);
            v.childForceExpandHeight = true;
            DemoMake.Size(root, -1f, -1f, -1f, 1f).minHeight = Mathf.Max(0f, height);
            var p = root.gameObject.AddComponent<DemoPages>();
            p.fitTallest = height <= 0f;
            p.Bind(id, theme);
            return p;
        }

        /// <summary>Adds a page and fills it.</summary>
        public DemoPages Page(string pageName, Action<DemoBuilder> fill)
        {
            DemoTheme t = Theme;
            RectTransform rt = DemoMake.Rect("Page " + pageName, transform);
            DemoMake.Vertical(rt, t.spacing);
            rt.gameObject.AddComponent<CanvasGroup>();
            names.Add(pageName);
            pages.Add(rt);
            fill?.Invoke(new DemoBuilder(Panel, rt, t));
            rt.gameObject.SetActive(pages.Count - 1 == index);
            return this;
        }

        public void Next() => Go(index + 1);

        public void Back() => Go(index - 1);

        /// <summary>Shows a page by name. An unknown name is ignored with a warning.</summary>
        public void Go(string pageName)
        {
            int i = names.IndexOf(pageName);
            if (i >= 0) Go(i);
            else Debug.LogWarning("DemoPages " + Path + " has no page named " + pageName, this);
        }

        public bool Has(string pageName) => names.Contains(pageName);

        public void Go(int target)
        {
            if (pages.Count == 0) return;
            target = Mathf.Clamp(target, 0, pages.Count - 1);
            if (target == index) return;
            pages[index].gameObject.SetActive(false);
            index = target;
            pages[index].gameObject.SetActive(true);
            fade = Application.isPlaying ? 0f : 1f;
            pages[index].GetComponent<CanvasGroup>().alpha = fade;
            Changed?.Invoke(index);
        }

        /// <summary>Reserves the height of the tallest page, so that the panel keeps its size when the page changes.</summary>
        internal void Fit()
        {
            if (!fitTallest || pages.Count == 0) return;
            float tallest = 0f;
            for (int i = 0; i < pages.Count; i++)
            {
                for (int k = 0; k < pages.Count; k++) pages[k].gameObject.SetActive(k == i);
                // Twice: text needs its final width before its height is known.
                LayoutRebuilder.ForceRebuildLayoutImmediate(Panel.Content);
                LayoutRebuilder.ForceRebuildLayoutImmediate(Panel.Content);
                tallest = Mathf.Max(tallest, LayoutUtility.GetPreferredHeight(pages[i]));
            }
            for (int k = 0; k < pages.Count; k++) pages[k].gameObject.SetActive(k == index);
            DemoMake.Size(this).minHeight = Mathf.Ceil(tallest);
        }

        private void Update()
        {
            if (fade >= 1f || pages.Count == 0) return;
            fade = Mathf.Min(1f, fade + DemoClock.DeltaTime / 0.22f);
            pages[index].GetComponent<CanvasGroup>().alpha = DemoEase.OutCubic(fade);
        }
    }
}
