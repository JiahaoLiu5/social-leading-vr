using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace SocialLeading.AllInOne
{
    /// <summary>First view on a desktop, where it may differ from the seat (degrees, metres).</summary>
    public struct DesktopView
    {
        public float yaw, pitch, eyeHeight;
    }

    /// <summary>
    /// Puts the user on a seat: the rig root takes the exact seat pose, and the node below it takes back where the user
    /// really stands and looks. The exact root matters, because the movement link adds displacements in root axes.
    /// </summary>
    public sealed class Seating : MonoBehaviour
    {
        [SerializeField] private AppConfig config;
        [SerializeField] private RigMode mode;
        [SerializeField] private DesktopRig desktop;
        [SerializeField] private Transform recentre;
        [SerializeField] private Transform trackedHead;

        private readonly List<XRInputSubsystem> inputs = new List<XRInputSubsystem>();
        private Pose seat = Pose.identity;
        private DesktopView? seatView;
        private bool pending;

        /// <summary>The body that is moved with the seat, or null.</summary>
        public AvatarBody Body { get; set; }

        public Pose Seat => seat;

        /// <summary>Moves the user to a seat. On a desktop the first view may differ from the seat.</summary>
        public void Apply(Pose newSeat, DesktopView? desktopView = null)
        {
            seat = newSeat;
            seatView = desktopView;
            transform.SetPositionAndRotation(seat.position, seat.rotation);
            if (mode.Desktop)
            {
                recentre.localPosition = Vector3.zero;
                recentre.localRotation = Quaternion.identity;
                desktop.Place(seat.position, seatView ?? new DesktopView { yaw = seat.rotation.eulerAngles.y, pitch = config.desktopPitch, eyeHeight = config.eyeHeight });
            }
            else if (!TryAlign()) pending = true;
            if (Body != null) Body.Snap();
        }

        /// <summary>Aligns the user with the current seat again.</summary>
        public void Recentre() => Apply(seat, seatView);

        private void OnEnable()
        {
            SubsystemManager.GetSubsystems(inputs);
            foreach (XRInputSubsystem input in inputs) input.trackingOriginUpdated += OnOriginUpdated;
        }

        private void OnDisable()
        {
            foreach (XRInputSubsystem input in inputs) input.trackingOriginUpdated -= OnOriginUpdated;
            inputs.Clear();
        }

        private void OnOriginUpdated(XRInputSubsystem _) => pending = true;

        private void Update()
        {
            if (!pending || mode.Desktop || !mode.IsDecided) return;
            if (!TryAlign()) return;
            pending = false;
            if (Body != null) Body.Snap();
        }

        /// <summary>False while the head has no tracked pose yet.</summary>
        private bool TryAlign()
        {
            Vector3 position = recentre.InverseTransformPoint(trackedHead.position);
            Vector3 forward = recentre.InverseTransformDirection(trackedHead.forward);
            forward.y = 0f;
            bool tracked = RigMode.HeadsetRuns && trackedHead.localPosition.sqrMagnitude > 1e-6f;
            if (!tracked || forward.sqrMagnitude < 1e-4f) return false;

            float yaw = Quaternion.LookRotation(forward).eulerAngles.y;
            recentre.localRotation = Quaternion.Euler(0f, -yaw, 0f);
            Vector3 back = -(recentre.localRotation * new Vector3(position.x, 0f, position.z));
            back.y = config.eyeHeight - position.y;
            recentre.localPosition = back;
            return true;
        }
    }
}
