using System;
using System.Collections.Generic;
using UnityEngine;

namespace DemoKit
{
    /// <summary>
    /// A fingertip that presses elements by touching them. The finger has to come from the front; it must be pulled back
    /// before the next press.
    /// </summary>
    public sealed class DemoPointerPoke : DemoPointer
    {
        [SerializeField] private Transform fingertip;
        [Tooltip("Radius of the fingertip (metres).")]
        [SerializeField] private float radius = 0.008f;
        [Tooltip("Distance from which an element is highlighted (metres).")]
        [SerializeField] private float hoverDistance = 0.07f;
        [Tooltip("The finger has to be pulled back this far before it can press again (metres).")]
        [SerializeField] private float releaseDistance = 0.015f;
        [Tooltip("How far the finger may go through the panel (metres).")]
        [SerializeField] private float maxDepth = 0.10f;
        [SerializeField] private bool showReticle = true;

        private DemoPointerVisual visual;
        private bool ready;
        private DemoWidget lastSurface;

        public Transform Fingertip { get { return fingertip; } set { fingertip = value; } }
        public float Radius { get { return radius; } set { radius = value; } }
        public float HoverDistance { get { return hoverDistance; } set { hoverDistance = value; } }
        public bool ShowReticle { get { return showReticle; } set { showReticle = value; } }

        /// <summary>Optional extra condition, for example "the hand is tracked".</summary>
        public Func<bool> ActiveWhen { get; set; }

        /// <summary>True when the fingertip is close to a panel or a hotspot.</summary>
        public bool IsNear { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ClickOnPress = true;
            Debounce = 0.25f;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            IsNear = false;
            ready = false;
            lastSurface = null;
            visual?.Hide();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            visual?.Destroy();
        }

        protected override bool TryPlane(Transform plane, out Vector3 point)
        {
            point = plane.position;
            if (fingertip == null) return false;
            Vector3 n = plane.forward;
            point = fingertip.position - n * Vector3.Dot(n, fingertip.position - plane.position);
            return true;
        }

        private void LateUpdate()
        {
            float dt = DemoClock.DeltaTime;
            ForgetDestroyed();
            bool active = fingertip != null && fingertip.gameObject.activeInHierarchy && (ActiveWhen == null || ActiveWhen()) && !DemoInput.Blocked;
            DemoHit hit = default;
            DemoWidget surface = null;
            float height = 0f;
            IsNear = active && Find(fingertip.position, out hit, out surface, out height);
            if (!IsNear)
            {
                ready = false;
                lastSurface = null;
                Drop();
                visual?.Hide();
                return;
            }
            if (surface != lastSurface) { ready = false; lastSurface = surface; }

            SetHit(true, hit);
            if (Captured != null && !Captured.CanClick) Release();
            if (Captured != null && !Captured.IsPressedBy(this)) Captured.PointerDown(this);
            SetHovered(Captured != null ? Captured : hit.element != null && hit.element.CanClick ? hit.element : null);

            float gap = height - radius;
            if (gap > releaseDistance * 0.5f && Captured == null) ready = true;
            if (Captured != null && gap > releaseDistance) { Release(); ready = true; }
            if (ready && gap <= 0f)
            {
                ready = false;
                if (Hovered != null)
                {
                    DemoElement e = Hovered;
                    SetCaptured(e);
                    Drag();
                    Fire(e, DemoClock.Now);
                }
            }
            else Drag();

            if (!showReticle) { visual?.Hide(); return; }
            if (visual == null) visual = new DemoPointerVisual(transform, DemoTheme.Current.layer);
            float near = 1f - Mathf.Clamp01(gap / hoverDistance);
            visual.DrawReticle(hit.point, hit.normal, Mathf.Lerp(0.034f, 0.018f, near), DemoTheme.Current.accent, Hovered != null ? 1f : 0f, Captured != null ? 1f : 0f, dt);
        }

        /// <summary>
        /// The nearest panel or hotspot in front of the fingertip and the element below it. Height is the distance to the
        /// surface in metres, negative behind it.
        /// </summary>
        private bool Find(Vector3 tip, out DemoHit hit, out DemoWidget surface, out float height)
        {
            hit = default;
            surface = null;
            height = float.MaxValue;
            float best = float.MaxValue;
            List<DemoWidget> widgets = DemoUi.Live;
            for (int i = 0; i < widgets.Count; i++)
            {
                DemoWidget w = widgets[i];
                if (w == null || (Layers.value & (1 << w.gameObject.layer)) == 0) continue;
                Rect r;
                float front = 0f;
                if (w is DemoPanel p)
                {
                    if (!p.AcceptsFrom(Hand)) continue;
                    r = p.RectTransform.rect;
                }
                else if (w is DemoElement free && free.Panel == null && free.AcceptsPoke && free.CanClick)
                {
                    r = free.LocalRect;
                    front = free.LocalFront;
                }
                else continue;
                Transform t = w.transform;
                Vector3 local = t.InverseTransformPoint(tip);
                if (local.x < r.xMin || local.x > r.xMax || local.y < r.yMin || local.y > r.yMax) continue;
                Vector3 on = t.TransformPoint(new Vector3(local.x, local.y, front));
                float h = Vector3.Dot(on - tip, t.forward);
                // A finger that came through the surface may be behind it. One that was never in front may not.
                if (h > hoverDistance + radius || h < (w == lastSurface ? -maxDepth : -radius)) continue;
                if (Mathf.Abs(h) >= best) continue;
                best = Mathf.Abs(h);
                surface = w;
                height = h;
                hit.panel = w as DemoPanel;
                hit.element = w as DemoElement;
                hit.point = on;
                hit.normal = -t.forward;
                hit.distance = Mathf.Max(0f, h);
            }
            if (surface == null) return false;
            if (hit.panel == null) return true;
            for (int i = 0; i < widgets.Count; i++)
            {
                if (!(widgets[i] is DemoElement e) || e == null || e.Panel != hit.panel || !e.AcceptsPoke || !e.gameObject.activeInHierarchy) continue;
                Vector3 local = e.transform.InverseTransformPoint(hit.point);
                if (!e.LocalRect.Contains(new Vector2(local.x, local.y))) continue;
                hit.element = e;
                break;
            }
            return true;
        }
    }
}
