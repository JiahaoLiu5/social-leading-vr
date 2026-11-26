using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// Makes a rectangle of an existing canvas a pointer target, for content that was not built with the kit, for example
    /// the answer boxes of a board. The optional graphic is faded in while a pointer is on the target.
    /// Put the canvas on a DemoPanel with DemoPanel.Adopt, so that fingertips and rays find it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DemoTarget : DemoElement
    {
        [SerializeField] private Graphic highlight;
        [SerializeField, Range(0f, 1f)] private float hoverAlpha = 0.45f;
        [SerializeField, Range(0f, 1f)] private float pressAlpha = 0.85f;

        /// <summary>Adds a target to a rectangle. The rectangle's size is the size of the target.</summary>
        public static DemoTarget Add(RectTransform rect, string id, Graphic highlight = null)
        {
            var t = rect.gameObject.AddComponent<DemoTarget>();
            t.highlight = highlight;
            t.Bind(id, null);
            t.Finish();
            return t;
        }

        protected override void Draw(float hover, float press, float select, float disable)
        {
            if (highlight == null) return;
            Color c = Theme.accent;
            c.a = Mathf.Max(hover * hoverAlpha, press * pressAlpha) * (1f - disable);
            highlight.color = c;
        }
    }
}
