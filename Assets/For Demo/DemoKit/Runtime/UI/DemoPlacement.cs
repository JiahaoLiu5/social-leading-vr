using UnityEngine;

namespace DemoKit
{
    public enum DemoFollow
    {
        /// <summary>Placed once, then stays.</summary>
        None,
        /// <summary>Stays until the user turns or walks away from it, then glides back in front of the user.</summary>
        Lazy,
        /// <summary>Always moves with the head.</summary>
        Always
    }

    public enum DemoBillboard
    {
        /// <summary>Keeps its rotation.</summary>
        None,
        /// <summary>Upright, turned toward the head.</summary>
        Yaw,
        /// <summary>Turned and tilted toward the head.</summary>
        Full
    }

    /// <summary>
    /// Places a panel in front of a head, or keeps it attached to another transform, with smooth motion.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public sealed class DemoPlacement : MonoBehaviour
    {
        [Header("In front of a head")]
        public Transform head;
        public float distance = 1.4f;
        [Tooltip("Metres above the head (negative = below), or above the floor when absoluteHeight is on.")]
        public float height = -0.08f;
        public bool absoluteHeight;
        [Tooltip("Degrees to the right of the view direction.")]
        public float angle;
        public DemoFollow follow = DemoFollow.None;
        public DemoBillboard billboard = DemoBillboard.Yaw;
        [Tooltip("Lazy follow: how far the user may turn before the panel moves (degrees).")]
        public float lazyAngle = 38f;
        [Tooltip("Lazy follow: how far the distance may change before the panel moves (metres).")]
        public float lazyDistance = 0.6f;
        public float smoothTime = 0.16f;

        [Header("Attached to a transform")]
        public Transform anchor;
        public Vector3 anchorPosition;
        public Quaternion anchorRotation = Quaternion.identity;

        private bool placed, gliding;
        private Vector3 goal;

        /// <summary>Shows the panel in front of a head.</summary>
        public void InFrontOf(Transform newHead, float newDistance, float newHeight, DemoFollow newFollow, DemoBillboard newBillboard)
        {
            anchor = null;
            head = newHead;
            distance = newDistance;
            height = newHeight;
            follow = newFollow;
            billboard = newBillboard;
            placed = false;
            Step(true, 0f);
        }

        /// <summary>Keeps the panel at an offset from a transform, for example a hand.</summary>
        public void AttachTo(Transform target, Vector3 position, Quaternion rotation)
        {
            anchor = target;
            anchorPosition = position;
            anchorRotation = rotation;
            Step(true, 0f);
        }

        /// <summary>Stops all following and leaves the panel where it is.</summary>
        public void Release()
        {
            anchor = null;
            head = null;
            follow = DemoFollow.None;
        }

        /// <summary>Puts the panel in front of the head again.</summary>
        public void Recentre(bool instant = false)
        {
            if (instant) { placed = false; Step(true, 0f); }
            else { goal = Goal(); gliding = true; }
        }

        private void OnEnable()
        {
            if (anchor != null || (head != null && follow != DemoFollow.None)) { placed = false; Step(true, 0f); }
        }

        private void LateUpdate() { Step(false, DemoClock.DeltaTime); }

        private void Step(bool instant, float dt)
        {
            if (anchor != null)
            {
                Vector3 p = anchor.position + anchor.rotation * anchorPosition;
                Quaternion q = anchor.rotation * anchorRotation;
                transform.SetPositionAndRotation(
                    instant ? p : DemoEase.Damp(transform.position, p, smoothTime * 0.3f, dt),
                    instant ? q : DemoEase.Damp(transform.rotation, q, smoothTime * 0.3f, dt));
                return;
            }
            if (head == null) return;

            if (!placed || instant)
            {
                goal = Goal();
                transform.position = goal;
                placed = true;
                gliding = false;
                Turn(true, dt);
                return;
            }
            if (follow == DemoFollow.Always) { goal = Goal(); gliding = true; }
            else if (follow == DemoFollow.Lazy && TooFar()) { goal = Goal(); gliding = true; }
            if (gliding)
            {
                transform.position = DemoEase.Damp(transform.position, goal, smoothTime, dt);
                if ((transform.position - goal).sqrMagnitude < 1e-6f) gliding = false;
            }
            if (gliding || follow != DemoFollow.None) Turn(false, dt);
        }

        private bool TooFar()
        {
            Vector3 to = transform.position - head.position;
            to.y = 0f;
            float off = Vector3.Angle(to, Quaternion.AngleAxis(angle, Vector3.up) * Forward());
            if (off > lazyAngle) return true;
            if (Mathf.Abs(to.magnitude - distance) > lazyDistance) return true;
            float y = absoluteHeight ? transform.position.y : head.position.y + height;
            return Mathf.Abs(transform.position.y - y) > 0.35f;
        }

        private Vector3 Goal()
        {
            Vector3 p = head.position + Quaternion.AngleAxis(angle, Vector3.up) * Forward() * distance;
            p.y = absoluteHeight ? FloorY() + height : head.position.y + height;
            return p;
        }

        private float FloorY() => head.root != head ? head.root.position.y : 0f;

        private Vector3 Forward()
        {
            Vector3 f = head.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.04f)
            {
                // Looking straight up or down: the top of the head points backward or forward.
                f = head.forward.y < 0f ? head.up : -head.up;
                f.y = 0f;
            }
            return f.sqrMagnitude < 1e-6f ? Vector3.forward : f.normalized;
        }

        private void Turn(bool instant, float dt)
        {
            if (billboard == DemoBillboard.None) return;
            Vector3 away = transform.position - head.position;
            if (billboard == DemoBillboard.Yaw) away.y = 0f;
            if (away.sqrMagnitude < 1e-5f) return;
            Quaternion q = Quaternion.LookRotation(away, Vector3.up);
            transform.rotation = instant ? q : DemoEase.Damp(transform.rotation, q, smoothTime * 0.6f, dt);
        }
    }
}
