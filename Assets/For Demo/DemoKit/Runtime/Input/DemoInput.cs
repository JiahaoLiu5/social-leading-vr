using System.Collections.Generic;
using UnityEngine;

namespace DemoKit
{
    /// <summary>What a ray or a fingertip is on.</summary>
    public struct DemoHit
    {
        /// <summary>The interactive element, or null when only the panel was hit.</summary>
        public DemoElement element;
        /// <summary>The panel, or null for a hotspot outside a panel.</summary>
        public DemoPanel panel;
        public Vector3 point;
        public Vector3 normal;
        public float distance;
    }

    /// <summary>The pointers made by DemoInput.AddHands. Parts that were not asked for are null.</summary>
    public struct DemoHands
    {
        public DemoPointerRay leftRay, rightRay;
        public DemoPointerPoke leftPoke, rightPoke;

        public DemoPointerRay Ray(DemoHand hand) => hand == DemoHand.Left ? leftRay : hand == DemoHand.Right ? rightRay : null;

        public DemoPointerPoke Poke(DemoHand hand) => hand == DemoHand.Left ? leftPoke : hand == DemoHand.Right ? rightPoke : null;
    }

    /// <summary>Keeps the list of pointers, creates them, and tests rays against panels.</summary>
    public static class DemoInput
    {
        private static readonly List<DemoPointer> AllPointers = new List<DemoPointer>();
        private static readonly List<DemoPointer> ActivePointers = new List<DemoPointer>();
        private static readonly List<Object> Blockers = new List<Object>();
        private static bool blockedByApplication;

        /// <summary>All pointers that are enabled.</summary>
        public static IReadOnlyList<DemoPointer> Pointers { get { return ActivePointers; } }

        /// <summary>
        /// Turns all pointers off, for example while a scene loads. Reads true as well while a fade covers the view;
        /// setting it has no influence on fades.
        /// </summary>
        public static bool Blocked
        {
            get
            {
                for (int i = Blockers.Count - 1; i >= 0; i--)
                    if (Blockers[i] == null) Blockers.RemoveAt(i);
                return blockedByApplication || Blockers.Count > 0;
            }
            set { blockedByApplication = value; }
        }

        /// <summary>A ray from a transform, for example the aim pose of a controller.</summary>
        public static DemoPointerRay AddRay(Transform origin, IDemoPress press, DemoHand hand = DemoHand.None)
        {
            DemoPointerRay p = New<DemoPointerRay>("Ray " + hand, hand);
            p.Origin = origin;
            p.Press = press;
            return p;
        }

        /// <summary>A fingertip that presses elements by touching them.</summary>
        public static DemoPointerPoke AddPoke(Transform fingertip, DemoHand hand = DemoHand.None)
        {
            DemoPointerPoke p = New<DemoPointerPoke>("Poke " + hand, hand);
            p.Fingertip = fingertip;
            return p;
        }

        /// <summary>The mouse, through a camera (default: the main camera).</summary>
        public static DemoPointerMouse AddMouse(Camera camera = null)
        {
            DemoPointerMouse p = New<DemoPointerMouse>("Mouse", DemoHand.None);
            p.Camera = camera;
            return p;
        }

        /// <summary>
        /// Rays for both hands (trigger or pinch), and pokes when fingertips are given. A poke near a panel hides the ray
        /// of the same hand. An aim with a DemoPoseDriver hides its ray while nothing is tracked.
        /// </summary>
        public static DemoHands AddHands(Transform leftAim, Transform rightAim, Transform leftFingertip = null, Transform rightFingertip = null)
        {
            var hands = new DemoHands();
            AddHand(DemoHand.Left, leftAim, leftFingertip, out hands.leftRay, out hands.leftPoke);
            AddHand(DemoHand.Right, rightAim, rightFingertip, out hands.rightRay, out hands.rightPoke);
            return hands;
        }

        private static void AddHand(DemoHand hand, Transform aim, Transform fingertip, out DemoPointerRay ray, out DemoPointerPoke poke)
        {
            poke = fingertip != null ? AddPoke(fingertip, hand) : null;
            ray = null;
            if (aim == null) return;
            ray = AddRay(aim, DemoPress.ControllerOrPinch(hand), hand);
            ray.Poke = poke;
            // Hide the ray while the controller or the hand is not tracked.
            var driver = aim.GetComponent<DemoPoseDriver>();
            ray.Driver = driver;
            if (driver != null) ray.ActiveWhen = () => driver != null && driver.IsTracked;
        }

        /// <summary>Removes all pointers, disabled ones included.</summary>
        public static void Clear()
        {
            for (int i = AllPointers.Count - 1; i >= 0; i--)
                if (AllPointers[i] != null) Object.Destroy(AllPointers[i].gameObject);
        }

        /// <summary>
        /// Nearest element or panel on a ray. Panels are only hit from their readable side. A panel that is held by the
        /// given hand is passed over.
        /// </summary>
        public static bool Raycast(Ray ray, float maxDistance, LayerMask layers, out DemoHit hit, DemoHand hand = DemoHand.None)
        {
            hit = default;
            float best = maxDistance;
            bool found = false;
            List<DemoWidget> widgets = DemoUi.Live;
            for (int i = 0; i < widgets.Count; i++)
            {
                DemoWidget w = widgets[i];
                if (w == null) continue;
                if (w is DemoElement e)
                {
                    if (e.HitSize.x <= 0f || !e.gameObject.activeInHierarchy || !InLayers(e.gameObject, layers)) continue;
                    DemoPanel owner = e.Panel;
                    if (owner != null ? !owner.AcceptsFrom(hand) : !e.CanClick) continue;
                    if (!Cast(e.transform, e.HitCenter, e.HitSize, ray, best, e.TwoSided, out float d)) continue;
                    best = d;
                    found = true;
                    hit.element = e;
                    hit.panel = owner;
                    Fill(ref hit, e.transform, ray, d);
                }
                else if (w is DemoPanel p)
                {
                    if (p.HitSize.x <= 0f || !p.AcceptsFrom(hand) || !InLayers(p.gameObject, layers)) continue;
                    if (!Cast(p.transform, p.HitCenter, p.HitSize, ray, best, false, out float d)) continue;
                    best = d;
                    found = true;
                    hit.element = null;
                    hit.panel = p;
                    Fill(ref hit, p.transform, ray, d);
                }
            }
            return found;
        }

        /// <summary>
        /// Distance from a ray to the nearest panel or hotspot that can be read from the ray's origin, as an angle in
        /// degrees. 180 when there is none. Pointers use it to hide their line when nobody is aiming at a panel.
        /// </summary>
        public static float AngleToNearestPanel(Ray ray, DemoHand hand = DemoHand.None)
        {
            float best = 180f;
            List<DemoWidget> widgets = DemoUi.Live;
            for (int i = 0; i < widgets.Count; i++)
            {
                DemoWidget w = widgets[i];
                if (w == null) continue;
                Rect r;
                bool twoSided = false;
                if (w is DemoPanel p)
                {
                    if (!p.AcceptsFrom(hand)) continue;
                    r = p.RectTransform.rect;
                }
                else if (w is DemoElement e && e.Panel == null && e.CanClick)
                {
                    r = e.LocalRect;
                    twoSided = e.TwoSided;
                }
                else continue;
                Transform t = w.transform;
                Vector3 o = t.InverseTransformPoint(ray.origin), d = t.InverseTransformVector(ray.direction);
                if (o.z > 0f && !twoSided) continue;
                // The point of the rectangle that is nearest to where the ray meets its plane.
                Vector3 target;
                if (Mathf.Abs(d.z) > 1e-9f && -o.z / d.z > 0f)
                {
                    Vector3 at = o + d * (-o.z / d.z);
                    target = new Vector3(Mathf.Clamp(at.x, r.xMin, r.xMax), Mathf.Clamp(at.y, r.yMin, r.yMax), 0f);
                }
                else target = new Vector3(r.center.x, r.center.y, 0f);
                best = Mathf.Min(best, Vector3.Angle(ray.direction, t.TransformPoint(target) - ray.origin));
            }
            return best;
        }

        private static void Fill(ref DemoHit hit, Transform t, Ray ray, float distance)
        {
            hit.distance = distance;
            hit.point = ray.GetPoint(distance);
            hit.normal = Vector3.Dot(ray.direction, t.forward) < 0f ? t.forward : -t.forward;
        }

        private static bool InLayers(GameObject go, LayerMask layers) => (layers.value & (1 << go.layer)) != 0;

        /// <summary>
        /// Ray against a box in local space. Done here rather than through the physics scene: it needs no simulation step, so it
        /// is right on the frame a panel moves, with paused time, and whatever the project's physics settings are.
        /// </summary>
        private static bool Cast(Transform t, Vector3 center, Vector3 size, Ray ray, float max, bool twoSided, out float distance)
        {
            Vector3 o = t.InverseTransformPoint(ray.origin) - center;
            Vector3 d = t.InverseTransformVector(ray.direction);
            distance = 0f;
            if (d.z <= 0f && !twoSided) return false;
            Vector3 half = size * 0.5f;
            float near = 0f, far = max;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(d[axis]) < 1e-9f)
                {
                    if (Mathf.Abs(o[axis]) > half[axis]) return false;
                    continue;
                }
                float a = (-half[axis] - o[axis]) / d[axis], b = (half[axis] - o[axis]) / d[axis];
                if (a > b) { float swap = a; a = b; b = swap; }
                if (a > near) near = a;
                if (b < far) far = b;
                if (near > far) return false;
            }
            distance = near;
            return true;
        }

        internal static void Block(Object owner, bool on)
        {
            Blockers.Remove(owner);
            if (on) Blockers.Add(owner);
        }

        internal static void Register(DemoPointer p)
        {
            if (!AllPointers.Contains(p)) AllPointers.Add(p);
            if (!ActivePointers.Contains(p)) ActivePointers.Add(p);
        }

        internal static void Unregister(DemoPointer p, bool destroyed)
        {
            ActivePointers.Remove(p);
            if (destroyed) AllPointers.Remove(p);
        }

        private static T New<T>(string name, DemoHand hand) where T : DemoPointer
        {
            var go = new GameObject("DemoPointer " + name);
            var p = go.AddComponent<T>();
            p.Hand = hand;
            return p;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AllPointers.Clear();
            ActivePointers.Clear();
            Blockers.Clear();
            blockedByApplication = false;
        }
    }
}
