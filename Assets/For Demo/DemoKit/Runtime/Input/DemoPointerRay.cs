using System;
using UnityEngine;

namespace DemoKit
{
    /// <summary>
    /// A ray from a transform (controller aim pose, hand aim pose, head) with a press provider, a smooth line, and a reticle.
    /// </summary>
    public sealed class DemoPointerRay : DemoPointer
    {
        public enum LineMode
        {
            /// <summary>Only while the ray is on a panel.</summary>
            OnPanel,
            /// <summary>Also a short stub while the ray is close to a panel.</summary>
            NearPanel,
            /// <summary>Always, with a stub when it is not on a panel.</summary>
            Always,
            Never
        }

        [SerializeField] private Transform origin;
        [SerializeField] private float maxDistance = 6f;
        [Tooltip("Time constant of the smoothing (seconds).")]
        [SerializeField] private float smoothing = 0.035f;
        [Tooltip("Smoothing while the pose comes from a tracked hand (seconds).")]
        [SerializeField] private float handSmoothing = 0.07f;
        [SerializeField] private LineMode lineMode = LineMode.NearPanel;
        [SerializeField] private float lineWidth = 0.004f;
        [SerializeField] private float startGap = 0.05f;
        [Tooltip("NearPanel: the stub is shown when the ray is within this angle of a panel (degrees).")]
        [SerializeField] private float nearAngle = 25f;
        [SerializeField] private DemoPointerPoke poke;

        private DemoPointerVisual visual;
        private Vector3 position, direction = Vector3.forward, end;
        private bool hasPose, endValid;

        public Transform Origin { get { return origin; } set { origin = value; hasPose = false; } }

        public IDemoPress Press { get; set; }

        /// <summary>Optional extra condition, for example "the hand is tracked".</summary>
        public Func<bool> ActiveWhen { get; set; }

        /// <summary>The ray hides while this fingertip of the same hand is close to a panel.</summary>
        public DemoPointerPoke Poke { get { return poke; } set { poke = value; } }

        public float MaxDistance { get { return maxDistance; } set { maxDistance = value; } }
        public float Smoothing { get { return smoothing; } set { smoothing = value; } }
        public float HandSmoothing { get { return handSmoothing; } set { handSmoothing = value; } }

        /// <summary>Optional: the driver of the origin, which tells hands from controllers.</summary>
        public DemoPoseDriver Driver { get; set; }

        public LineMode Line { get { return lineMode; } set { lineMode = value; } }
        public float LineWidth { get { return lineWidth; } set { lineWidth = value; } }
        public float StartGap { get { return startGap; } set { startGap = value; } }

        /// <summary>Colour of the line and the reticle. Clear = the accent of the theme.</summary>
        public Color Color { get; set; }

        /// <summary>The ray after smoothing.</summary>
        public Ray Ray { get { return new Ray(position, direction); } }

        protected override void OnDisable()
        {
            base.OnDisable();
            visual?.Hide();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            visual?.Destroy();
            (Press as IDisposable)?.Dispose();
        }

        protected override bool TryPlane(Transform plane, out Vector3 point) => RayOnPlane(new Ray(position, direction), plane, out point);

        private void LateUpdate() { Step(DemoClock.DeltaTime); }

        private void Step(float dt)
        {
            bool active = origin != null && origin.gameObject.activeInHierarchy && (ActiveWhen == null || ActiveWhen())
                && (poke == null || !poke.IsNear) && !DemoInput.Blocked;
            if (!active)
            {
                hasPose = false;
                Drive(false, false, default, Press != null && Press.IsPressed);
                visual?.Hide();
                endValid = false;
                return;
            }
            Vector3 p = origin.position, d = origin.forward;
            float time = Driver != null && Driver.IsHand ? Mathf.Max(smoothing, handSmoothing) : smoothing;
            if (!hasPose || time <= 0f || (p - position).sqrMagnitude > 1f) { position = p; direction = d; hasPose = true; }
            else
            {
                float k = 1f - Mathf.Exp(-dt / time);
                position = Vector3.Lerp(position, p, k);
                direction = Vector3.Slerp(direction, d, k).normalized;
            }
            bool on = DemoInput.Raycast(new Ray(position, direction), maxDistance, Layers, out DemoHit hit, Hand);
            Drive(true, on, hit, Press != null && Press.IsPressed);
            Draw(on, hit, dt);
        }

        private void Draw(bool on, DemoHit hit, float dt)
        {
            if (lineMode == LineMode.Never) { visual?.Hide(); return; }
            if (visual == null) visual = new DemoPointerVisual(transform, DemoTheme.Current.layer);
            Color color = Color.a > 0f ? Color : DemoTheme.Current.accent;
            if (on)
            {
                // The line follows the hit point with a little delay, which hides the tremor of hands.
                end = endValid ? DemoEase.Damp(end, hit.point, 0.012f, dt) : hit.point;
                endValid = true;
                bool target = Hovered != null;
                visual.DrawLine(position, direction, end, lineWidth * (IsPressed ? 0.8f : 1f), color, target ? 1f : 0.7f, startGap, dt);
                float size = Mathf.Clamp(hit.distance * 0.022f, 0.014f, 0.055f);
                visual.DrawReticle(end, hit.normal, size, color, target ? 1f : 0f, Captured != null ? 1f : 0f, dt);
                return;
            }
            endValid = false;
            visual.HideReticle();
            bool stub = lineMode == LineMode.Always || lineMode == LineMode.NearPanel && DemoInput.AngleToNearestPanel(new Ray(position, direction), Hand) < nearAngle;
            visual.DrawStub(position, direction, 0.5f, lineWidth, color, stub ? 0.8f : 0f, startGap, dt);
        }
    }
}
