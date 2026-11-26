using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DemoKit
{
    /// <summary>
    /// Moves a transform with a tracked head, controller, or hand, in the local space of its parent (the tracking space).
    /// With aim on, hands use the Meta aim pose and controllers their pointer pose, which is what a ray needs.
    /// While nothing is tracked the transform keeps its last pose and IsTracked is false.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class DemoPoseDriver : MonoBehaviour
    {
        // Bits of the tracking state: the position and the rotation are valid.
        private const int PositionAndRotation = 3;
        // Bit 1 of the aim flags says that the aim pose is valid.
        private const int AimValid = 1 << 1;

        [Tooltip("None = the head.")]
        [SerializeField] private DemoHand hand = DemoHand.Right;
        [SerializeField] private bool aim = true;

        private InputAction position, rotation, gripPosition, gripRotation, handPosition, handRotation, handFlags;

        public DemoHand Hand { get { return hand; } set { hand = value; if (isActiveAndEnabled) { Stop(); Begin(); } } }

        public bool Aim { get { return aim; } set { aim = value; if (isActiveAndEnabled) { Stop(); Begin(); } } }

        /// <summary>True while a device delivers a valid pose.</summary>
        public bool IsTracked { get; private set; }

        /// <summary>True when the pose comes from hand tracking.</summary>
        public bool IsHand { get; private set; }

        /// <summary>Creates a driven child of a tracking space.</summary>
        public static DemoPoseDriver Create(Transform trackingSpace, DemoHand hand, bool aim = true)
        {
            var go = new GameObject(hand == DemoHand.None ? "Head Pose" : hand + (aim ? " Aim Pose" : " Grip Pose"));
            go.SetActive(false);
            go.transform.SetParent(trackingSpace, false);
            var d = go.AddComponent<DemoPoseDriver>();
            d.hand = hand;
            d.aim = aim;
            go.SetActive(true);
            return d;
        }

        private void OnEnable()
        {
            Begin();
            Step();
            Application.onBeforeRender += Step;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= Step;
            Stop();
        }

        private void Update() { Step(); }

        private void Begin()
        {
            if (hand == DemoHand.None)
            {
                position = Make("<XRHMD>/centerEyePosition", "Vector3");
                rotation = Make("<XRHMD>/centerEyeRotation", "Quaternion");
                return;
            }
            string device = "<XRController>{" + DemoPress.Usage(hand) + "}";
            gripPosition = Make(device + "/devicePosition", "Vector3");
            gripRotation = Make(device + "/deviceRotation", "Quaternion");
            if (!aim) return;
            position = Make(device + "/pointerPosition", "Vector3");
            rotation = Make(device + "/pointerRotation", "Quaternion");
            string aimHand = "<MetaAimHand>{" + DemoPress.Usage(hand) + "}";
            handPosition = Make(aimHand + "/devicePosition", "Vector3");
            handRotation = Make(aimHand + "/deviceRotation", "Quaternion");
            handFlags = Make(aimHand + "/aimFlags", null);
        }

        // The actions only find the controls. Their values are read from the controls themselves, because an action reports
        // the default value until something changes, and cannot say which of several devices is tracked.
        private static InputAction Make(string path, string type)
        {
            var a = new InputAction("DemoKit pose", InputActionType.PassThrough, path, null, null, type);
            a.Enable();
            return a;
        }

        private void Stop()
        {
            Free(ref position);
            Free(ref rotation);
            Free(ref gripPosition);
            Free(ref gripRotation);
            Free(ref handPosition);
            Free(ref handRotation);
            Free(ref handFlags);
            IsTracked = IsHand = false;
        }

        private static void Free(ref InputAction action)
        {
            action?.Dispose();
            action = null;
        }

        private void Step()
        {
            if (position == null && gripPosition == null) return;
            IsHand = Read(handPosition, handRotation, true);
            IsTracked = IsHand || Read(position, rotation, false);
            // A controller without a pointer pose: the XR device simulator, or a plug-in other than OpenXR.
            if (!IsTracked && (position == null || position.controls.Count == 0)) IsTracked = Read(gripPosition, gripRotation, false);
        }

        /// <summary>Applies the pose of the first tracked device.</summary>
        private bool Read(InputAction positions, InputAction rotations, bool needsAim)
        {
            if (positions == null) return false;
            var found = positions.controls;
            for (int i = 0; i < found.Count; i++)
            {
                InputDevice device = found[i].device;
                if (!(found[i] is Vector3Control p) || !Tracked(device) || needsAim && !HasAim(device)) continue;
                Vector3 at = p.ReadValue();
                Quaternion q = Quaternion.identity;
                var turns = rotations.controls;
                for (int k = 0; k < turns.Count; k++)
                    if (turns[k].device == device && turns[k] is QuaternionControl r) { q = r.ReadValue(); break; }
                if (q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w < 0.5f) continue;
                transform.localPosition = at;
                transform.localRotation = q;
                return true;
            }
            return false;
        }

        private static bool Tracked(InputDevice device)
        {
            if (!(device is TrackedDevice t) || t.trackingState == null) return true;
            return (t.trackingState.ReadValue() & PositionAndRotation) == PositionAndRotation;
        }

        private bool HasAim(InputDevice device)
        {
            var flags = handFlags.controls;
            for (int i = 0; i < flags.Count; i++)
                if (flags[i].device == device && flags[i] is IntegerControl f) return (f.ReadValue() & AimValid) != 0;
            return false;
        }
    }
}
