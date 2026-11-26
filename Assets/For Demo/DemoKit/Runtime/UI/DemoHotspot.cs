using System;
using UnityEngine;
using UnityEngine.UI;

namespace DemoKit
{
    /// <summary>
    /// Makes an existing object clickable with the kit's pointers, without graphics of its own: the picture of a button on
    /// a canvas that is not part of the kit, an answer box, a thing in the room. Works at any scale.
    /// </summary>
    /// <remarks>
    /// The sensitive area is the object's BoxCollider when it already has one (the collider is only read), else its
    /// rectangle, which can be pointed at from the side a canvas is read from.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class DemoHotspot : DemoElement
    {
        [Tooltip("Tinted on hover and press. May be empty.")]
        [SerializeField] private Graphic target;
        [Tooltip("Colour of the tint. Clear = the accent of the theme.")]
        [SerializeField] private Color tint = Color.clear;
        [Tooltip("The area is made larger by this much on every side (metres).")]
        [SerializeField] private float margin;
        [Tooltip("Can be pointed at from behind as well.")]
        [SerializeField] private bool bothSides;
        [Tooltip("Off: only rays and the mouse, for objects that have their own touch logic.")]
        [SerializeField] private bool poke = true;

        private Color normal;
        private bool tinted;

        /// <summary>Optional extra condition, for example "the board is open".</summary>
        public Func<bool> ActiveWhen { get; set; }

        /// <summary>The graphic that is tinted. Its colour may change at any time while nothing points at the hotspot.</summary>
        public Graphic Target { get { return target; } set { Restore(); target = value; } }

        public Color Tint { get { return tint; } set { tint = value; } }

        public float Margin { get { return margin; } set { margin = value; FitCollider(); } }

        public bool BothSides { get { return bothSides; } set { bothSides = value; } }

        /// <summary>False leaves the hotspot to rays and the mouse.</summary>
        public bool Poke { get { return poke; } set { poke = value; } }

        protected override bool Available { get { return ActiveWhen == null || ActiveWhen(); } }

        internal override bool AcceptsPoke { get { return poke; } }

        internal override bool TwoSided { get { return bothSides; } }

        private bool UsesCollider { get { return Box != null && !OwnsBox; } }

        internal override Rect LocalRect
        {
            get
            {
                Rect r;
                if (UsesCollider)
                {
                    Vector3 c = Box.center, size = Box.size;
                    r = new Rect(c.x - size.x * 0.5f, c.y - size.y * 0.5f, size.x, size.y);
                }
                else
                {
                    var rt = transform as RectTransform;
                    r = rt != null ? rt.rect : new Rect(-0.5f, -0.5f, 1f, 1f);
                }
                if (margin == 0f) return r;
                Vector3 s = transform.lossyScale;
                float x = margin / Mathf.Max(1e-7f, Mathf.Abs(s.x)), y = margin / Mathf.Max(1e-7f, Mathf.Abs(s.y));
                return new Rect(r.x - x, r.y - y, r.width + 2f * x, r.height + 2f * y);
            }
        }

        internal override float LocalFront { get { return UsesCollider ? Box.center.z - Box.size.z * 0.5f : 0f; } }

        /// <summary>Adds a hotspot to an object, or returns the one it has. DemoUi.Click(id) works as for other elements.</summary>
        /// <param name="place">Any component of the object, for example its RectTransform or its BoxCollider.</param>
        /// <param name="id">Outside of a DemoPanel the id is also the path.</param>
        /// <param name="target">Optional graphic that is tinted on hover and press.</param>
        /// <param name="onClick">Called on every click.</param>
        public static DemoHotspot Add(Component place, string id, Graphic target = null, Action onClick = null)
        {
            var spot = place.GetComponent<DemoHotspot>();
            if (spot == null)
            {
                spot = place.gameObject.AddComponent<DemoHotspot>();
                spot.bothSides = !(place.transform is RectTransform);
            }
            spot.Bind(id, null);
            spot.Target = target;
            spot.Finish();
            if (onClick != null) spot.Clicked += e => onClick();
            return spot;
        }

        internal override void FitCollider()
        {
            if (!UsesCollider) { base.FitCollider(); return; }
            Rect r = LocalRect;
            SetHitBox(new Vector3(r.center.x, r.center.y, Box.center.z), new Vector3(r.width, r.height, Box.size.z));
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Box == null) Finish(); else Snap();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Restore();
        }

        protected override void Draw(float hover, float press, float select, float disable)
        {
            if (target == null) return;
            if (hover + press < 0.001f) { Restore(); return; }
            if (!tinted) { normal = target.color; tinted = true; }
            Color c = tint.a > 0f ? tint : Theme.accent;
            c.a = normal.a;
            target.color = Color.Lerp(Color.Lerp(normal, c, hover * 0.30f), c, press * 0.55f);
        }

        private void Restore()
        {
            if (tinted && target != null) target.color = normal;
            tinted = false;
        }
    }
}
