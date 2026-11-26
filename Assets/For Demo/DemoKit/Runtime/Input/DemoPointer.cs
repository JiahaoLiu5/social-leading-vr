using System;
using UnityEngine;

namespace DemoKit
{
    /// <summary>
    /// Base of all pointers. Hover, press, debounce, and capture are kept per pointer, so any number of pointers can work
    /// at the same time.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public abstract class DemoPointer : MonoBehaviour
    {
        [SerializeField] private DemoHand hand;
        [Tooltip("Layers of the panels this pointer can use.")]
        [SerializeField] private LayerMask layers = ~0;
        [Tooltip("Shortest time between two clicks of this pointer (seconds).")]
        [SerializeField] private float debounce = 0.18f;
        [Tooltip("The hovered element is kept for this long after the pointer left it (seconds). Pinching moves the hand, so the press often lands just beside the target.")]
        [SerializeField] private float hoverGrace = 0.09f;
        [Tooltip("While an element is held, the pointer may drift this far past its edge (metres) and still count as on it.")]
        [SerializeField] private float captureMargin = 0.045f;
        [Tooltip("Click when pressed instead of when released.")]
        [SerializeField] private bool clickOnPress;

        private bool wasPressed, armed;
        private float lastClick = -10f, lastSeen = -10f;
        private bool wasActive;

        public DemoHand Hand { get { return hand; } set { hand = value; } }
        public LayerMask Layers { get { return layers; } set { layers = value; } }
        public float Debounce { get { return debounce; } set { debounce = value; } }
        public float HoverGrace { get { return hoverGrace; } set { hoverGrace = value; } }
        public float CaptureMargin { get { return captureMargin; } set { captureMargin = value; } }
        public bool ClickOnPress { get { return clickOnPress; } set { clickOnPress = value; } }

        /// <summary>The element under the pointer, or null.</summary>
        public DemoElement Hovered { get; private set; }

        /// <summary>The element that was pressed and not yet released, or null.</summary>
        public DemoElement Captured { get; private set; }

        /// <summary>True when the pointer is on a panel or a hotspot.</summary>
        public bool HasHit { get; private set; }

        public DemoHit Hit { get; private set; }

        public bool IsPressed { get { return wasPressed; } }

        /// <summary>Raised when the hovered element changes. Null when nothing is hovered.</summary>
        public event Action<DemoPointer, DemoElement> HoverChanged;

        /// <summary>Raised after this pointer clicked an element.</summary>
        public event Action<DemoPointer, DemoElement> Clicked;

        /// <summary>
        /// Called with an amplitude (0 to 1) and a duration in seconds. By default it vibrates the controller of the
        /// pointer's hand. Set to null to switch haptics off.
        /// </summary>
        public Action<float, float> Haptics { get; set; }

        protected virtual void Awake() { Haptics = DefaultHaptics; }

        protected virtual void OnEnable() { DemoInput.Register(this); }

        protected virtual void OnDisable()
        {
            Drop();
            wasActive = false;
            DemoInput.Unregister(this, false);
        }

        protected virtual void OnDestroy() { DemoInput.Unregister(this, true); }

        private void DefaultHaptics(float amplitude, float seconds) { if (hand != DemoHand.None) DemoXr.Vibrate(hand, amplitude, seconds); }

        /// <summary>Where the pointer meets the plane of a transform (its local XY plane).</summary>
        protected abstract bool TryPlane(Transform plane, out Vector3 point);

        protected static bool RayOnPlane(Ray ray, Transform plane, out Vector3 point)
        {
            Vector3 n = plane.forward;
            float along = Vector3.Dot(n, ray.direction);
            float distance = Mathf.Abs(along) < 1e-5f ? -1f : Vector3.Dot(n, plane.position - ray.origin) / along;
            point = distance > 0f ? ray.GetPoint(distance) : plane.position;
            return distance > 0f;
        }

        /// <summary>One step of the pointer. Subclasses call it once per frame.</summary>
        protected void Drive(bool active, bool hasHit, DemoHit hit, bool pressed)
        {
            active &= !DemoInput.Blocked;
            float now = DemoClock.Now;
            if (!active)
            {
                if (wasActive) Drop();
                wasActive = false;
                wasPressed = pressed;
                return;
            }
            if (!wasActive)
            {
                // A pointer that appears with the trigger held down must let go first.
                wasActive = true;
                armed = !pressed;
                wasPressed = pressed;
            }
            if (!pressed) armed = true;

            HasHit = hasHit;
            Hit = hit;
            ForgetDestroyed();
            DemoElement under = hasHit && hit.element != null && hit.element.CanClick ? hit.element : null;
            if (Captured != null && !Captured.CanClick) Release();
            // An element that was switched off and on again in one frame, as a new layout does, has forgotten its pointers.
            if (Captured != null && !Captured.IsPressedBy(this)) Captured.PointerDown(this);
            if (Captured != null && under != Captured && (Captured.WantsDrag || IsNear(Captured, hasHit, hit))) under = Captured;

            if (under != null) { lastSeen = now; SetHovered(under); }
            else if (Hovered != null && (!Hovered.CanClick || now - lastSeen > hoverGrace || hasHit && hit.element != null)) SetHovered(null);

            bool down = pressed && !wasPressed && armed;
            bool up = !pressed && wasPressed;
            wasPressed = pressed;
            if (down)
            {
                armed = false;
                if (Hovered != null)
                {
                    Captured = Hovered;
                    Captured.PointerDown(this);
                    Haptics?.Invoke(0.35f, 0.02f);
                    if (clickOnPress) Fire(Captured, now);
                }
            }
            if (pressed) Drag();
            if (up && Captured != null)
            {
                DemoElement e = Captured;
                bool over = Hovered == e;
                Release();
                if (over && !clickOnPress) Fire(e, now);
            }
        }

        /// <summary>Moves the held element if it can be dragged.</summary>
        protected void Drag()
        {
            if (Captured != null && Captured.WantsDrag && TryPlane(Captured.transform, out Vector3 point)) Captured.PointerDrag(point);
        }

        private bool IsNear(DemoElement element, bool hasHit, DemoHit hit)
        {
            // Something in front of the element, or another panel.
            if (hasHit && hit.panel != element.Panel) return false;
            if (!TryPlane(element.transform, out Vector3 point)) return false;
            Vector3 p = element.transform.InverseTransformPoint(point), scale = element.transform.lossyScale;
            float x = captureMargin / Mathf.Max(1e-6f, Mathf.Abs(scale.x)), y = captureMargin / Mathf.Max(1e-6f, Mathf.Abs(scale.y));
            Rect r = element.LocalRect;
            return p.x > r.xMin - x && p.x < r.xMax + x && p.y > r.yMin - y && p.y < r.yMax + y;
        }

        /// <summary>Clicks an element at once, with the debounce of this pointer.</summary>
        protected bool Fire(DemoElement element, float now)
        {
            if (now - lastClick < debounce) return false;
            lastClick = now;
            if (!element.Click(this)) return false;
            Haptics?.Invoke(0.6f, 0.03f);
            Clicked?.Invoke(this, element);
            return true;
        }

        protected void SetCaptured(DemoElement element)
        {
            Release();
            Captured = element;
            if (element != null) element.PointerDown(this);
        }

        protected void SetHovered(DemoElement element)
        {
            if (Hovered == element)
            {
                if (element != null && !element.IsHoveredBy(this)) element.PointerEnter(this);
                return;
            }
            if (Hovered != null) Hovered.PointerExit(this);
            Hovered = element;
            if (element != null)
            {
                element.PointerEnter(this);
                Haptics?.Invoke(0.12f, 0.01f);
            }
            HoverChanged?.Invoke(this, element);
        }

        protected void SetHit(bool has, DemoHit hit) { HasHit = has; Hit = hit; }

        /// <summary>Lets go of elements that were destroyed.</summary>
        protected void ForgetDestroyed()
        {
            if (!ReferenceEquals(Captured, null) && Captured == null) Captured = null;
            if (ReferenceEquals(Hovered, null) || Hovered != null) return;
            Hovered = null;
            HoverChanged?.Invoke(this, null);
        }

        /// <summary>Lets go of everything without a click.</summary>
        protected void Drop()
        {
            ForgetDestroyed();
            Release();
            SetHovered(null);
            HasHit = false;
            Hit = default;
            armed = false;
        }

        protected void Release()
        {
            if (Captured != null) Captured.PointerUp(this);
            Captured = null;
        }
    }
}
