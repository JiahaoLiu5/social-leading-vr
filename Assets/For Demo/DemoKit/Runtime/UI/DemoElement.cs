using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DemoKit
{
    public enum DemoState { Normal, Hover, Pressed, Selected, Disabled }

    /// <summary>
    /// Something that can be pointed at and clicked. Has a box collider on the theme layer, per-pointer hover and
    /// press state, and can be clicked from code with Click().
    /// </summary>
    public abstract class DemoElement : DemoWidget
    {
        /// <summary>Two pointers that click the same element within this time count as one click (seconds).</summary>
        private const float SharedDebounce = 0.1f;

        [SerializeField] private bool interactable = true;
        [SerializeField] private bool selected;
        [SerializeField] private BoxCollider box;
        [SerializeField] private bool ownsBox;
        [SerializeField] protected RectTransform body;
        [SerializeField] private UnityEvent onClick = new UnityEvent();

        private readonly HashSet<DemoPointer> hovering = new HashSet<DemoPointer>();
        private readonly HashSet<DemoPointer> pressing = new HashSet<DemoPointer>();
        private float hover, press, select, disable, flash;
        private bool visualsReady;
        private DemoPointer lastPointer;
        private float lastPointerClick = -10f;

        /// <summary>Raised after a click by a pointer or from code.</summary>
        public event Action<DemoElement> Clicked;

        /// <summary>Raised when the first pointer enters and when the last one leaves.</summary>
        public event Action<DemoElement, bool> HoverChanged;

        /// <summary>
        /// Raised in every frame in which a pointer holds the element, with its place in the rectangle: (0, 0) is the lower
        /// left corner, (1, 1) the upper right one. An element with a listener keeps the pointer until it is released.
        /// </summary>
        public event Action<DemoElement, Vector2> Dragged;

        /// <summary>Click event that can be set in the Inspector.</summary>
        public UnityEvent OnClick { get { return onClick; } }

        /// <summary>The collider of the element. A collider that was there before the element is never changed.</summary>
        public BoxCollider Box { get { return box; } }

        public bool Interactable
        {
            get { return interactable; }
            set { interactable = value; if (!value) ReleaseAll(); if (!Application.isPlaying) Snap(); }
        }

        public bool Selected
        {
            get { return selected; }
            set { selected = value; if (!Application.isPlaying) Snap(); }
        }

        public bool Hovered { get { return hovering.Count > 0; } }

        public bool Pressed { get { return pressing.Count > 0; } }

        public DemoState State
        {
            get
            {
                if (!interactable) return DemoState.Disabled;
                if (Pressed) return DemoState.Pressed;
                if (Hovered) return DemoState.Hover;
                return selected ? DemoState.Selected : DemoState.Normal;
            }
        }

        /// <summary>True when a click would be accepted right now.</summary>
        public bool CanClick
        {
            get { return this != null && interactable && gameObject.activeInHierarchy && Available && (Panel == null || Panel.IsShown); }
        }

        /// <summary>Extra condition of a subclass for CanClick.</summary>
        protected virtual bool Available { get { return true; } }

        /// <summary>The box that rays are tested against, in local units.</summary>
        internal Vector3 HitCenter { get; private set; }

        internal Vector3 HitSize { get; private set; }

        /// <summary>The sensitive area in local units.</summary>
        internal virtual Rect LocalRect { get { return RectTransform.rect; } }

        /// <summary>Local z of the surface a fingertip touches.</summary>
        internal virtual float LocalFront { get { return 0f; } }

        /// <summary>True when fingertips may press the element.</summary>
        internal virtual bool AcceptsPoke { get { return true; } }

        /// <summary>True when the element may be pointed at from behind.</summary>
        internal virtual bool TwoSided { get { return false; } }

        /// <summary>True when the element follows a pointer that holds it.</summary>
        internal virtual bool WantsDrag { get { return Dragged != null; } }

        /// <summary>Clicks the element from code. Returns false when it is disabled or hidden.</summary>
        public bool Click() => Click(null);

        internal bool Click(DemoPointer pointer)
        {
            if (!CanClick) return false;
            if (pointer != null)
            {
                float now = DemoClock.Now;
                // Both hands on one button are one click, or a switch would go on and off.
                if (lastPointer != null && lastPointer != pointer && now - lastPointerClick < SharedDebounce) return false;
                lastPointer = pointer;
                lastPointerClick = now;
            }
            flash = 1f;
            OnClicked();
            onClick.Invoke();
            Clicked?.Invoke(this);
            DemoUi.RaiseClicked(this, pointer);
            return true;
        }

        /// <summary>Marks the element as hovered or not for a screenshot or a tutorial, without a pointer.</summary>
        public void ShowHover(bool on) { hover = on ? 1f : 0f; Draw(hover, press, select, disable); }

        internal void PointerEnter(DemoPointer pointer)
        {
            bool first = hovering.Count == 0;
            hovering.Add(pointer);
            if (first) HoverChanged?.Invoke(this, true);
        }

        internal void PointerExit(DemoPointer pointer)
        {
            if (hovering.Remove(pointer) && hovering.Count == 0) HoverChanged?.Invoke(this, false);
        }

        internal void PointerDown(DemoPointer pointer) { pressing.Add(pointer); }

        internal void PointerUp(DemoPointer pointer) { pressing.Remove(pointer); }

        internal bool IsHoveredBy(DemoPointer pointer) => hovering.Contains(pointer);

        internal bool IsPressedBy(DemoPointer pointer) => pressing.Contains(pointer);

        internal void PointerDrag(Vector3 worldPoint)
        {
            Rect r = LocalRect;
            Vector3 p = transform.InverseTransformPoint(worldPoint);
            var at = new Vector2(Mathf.InverseLerp(r.xMin, r.xMax, p.x), Mathf.InverseLerp(r.yMin, r.yMax, p.y));
            OnDragged(at);
            Dragged?.Invoke(this, at);
        }

        /// <summary>Called on every accepted click, before the events.</summary>
        protected virtual void OnClicked() { }

        /// <summary>Called while a pointer holds the element, before the event.</summary>
        protected virtual void OnDragged(Vector2 at) { }

        /// <summary>Draws the element. All values go from 0 to 1.</summary>
        protected abstract void Draw(float hover, float press, float select, float disable);

        /// <summary>Shows the current state at once, without a transition.</summary>
        public void Snap()
        {
            hover = Hovered ? 1f : 0f;
            press = Pressed ? 1f : 0f;
            select = selected ? 1f : 0f;
            disable = interactable ? 0f : 1f;
            flash = 0f;
            visualsReady = true;
            Draw(hover, press, select, disable);
        }

        /// <summary>Adds the collider. Called by the builder once the element has its parts.</summary>
        internal void Finish()
        {
            if (box == null)
            {
                box = gameObject.GetComponent<BoxCollider>();
                ownsBox = box == null;
                if (ownsBox)
                {
                    box = gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.enabled = Theme.physicsColliders;
                }
            }
            FitCollider();
            Snap();
        }

        /// <summary>True when the collider was made by the kit.</summary>
        protected bool OwnsBox { get { return ownsBox; } }

        protected override void OnEnable()
        {
            base.OnEnable();
            FitCollider();
            if (body != null) Snap();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ReleaseAll();
        }

        private void ReleaseAll()
        {
            bool was = hovering.Count > 0;
            hovering.Clear();
            pressing.Clear();
            if (was) HoverChanged?.Invoke(this, false);
        }

        protected virtual void OnRectTransformDimensionsChange() { FitCollider(); }

        internal virtual void FitCollider()
        {
            Rect r = LocalRect;
            // The front face stands 8 mm proud of the surface, so that elements win over the panel behind them.
            float mm = Millimetre(transform);
            SetHitBox(new Vector3(r.center.x, r.center.y, -2f * mm), new Vector3(Mathf.Max(1e-4f, r.width), Mathf.Max(1e-4f, r.height), 12f * mm));
        }

        protected void SetHitBox(Vector3 center, Vector3 size)
        {
            HitCenter = center;
            HitSize = size;
            if (!ownsBox || box == null) return;
            box.center = center;
            box.size = size;
        }

        /// <summary>One millimetre along the local z axis. Canvases come in any scale, often with another one along z.</summary>
        internal static float Millimetre(Transform t) => 0.001f / Mathf.Max(1e-7f, Mathf.Abs(t.lossyScale.z));

        protected virtual void Update()
        {
            float dt = DemoClock.DeltaTime;
            float h = interactable && Hovered ? 1f : 0f;
            float p = interactable && (Pressed || flash > 0.5f) ? 1f : 0f;
            float s = selected ? 1f : 0f;
            float d = interactable ? 0f : 1f;
            flash = Mathf.Max(0f, flash - dt / 0.24f);
            if (visualsReady && Near(hover, h) && Near(press, p) && Near(select, s) && Near(disable, d)) return;
            hover = Step(hover, h, 0.030f, dt);
            press = Step(press, p, 0.020f, dt);
            select = Step(select, s, 0.045f, dt);
            disable = Step(disable, d, 0.045f, dt);
            visualsReady = true;
            Draw(hover, press, select, disable);
        }

        private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.0005f;

        private static float Step(float value, float target, float halfLife, float dt)
        {
            value = DemoEase.Damp(value, target, halfLife, dt);
            return Mathf.Abs(value - target) < 0.004f ? target : value;
        }
    }
}
