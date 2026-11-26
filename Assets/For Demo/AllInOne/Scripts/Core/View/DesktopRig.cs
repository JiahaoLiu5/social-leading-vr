using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Feeds the simulated body of the kit into the XR rig, so that avatars use the same transforms as with a headset.
    /// Runs after the kit's rig (-100) and before the networked avatar (-90).
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public sealed class DesktopRig : MonoBehaviour
    {
        private const string MenuKeys = "Left mouse: press    Right mouse: look    WASD: walk    Q E: turn    H: hide";
        private const string RoomKeys = "Right mouse: look    WASD: walk    Q E: turn    1 to 7: gestures    Tab: menu    L: link    Esc: leave    H: hide\n"
                                        + "Enter: confirm, done    Backspace: reject    - =: slider";
        private const string OperatorKeys = "Space: next step    R: repeat    P: pause    Right mouse: look    WASD: walk    Tab: menu    Esc: leave    H: hide";

        [SerializeField] private AppConfig config;
        [SerializeField] private Camera trackingCamera;
        [SerializeField] private Transform leftController;
        [SerializeField] private Transform rightController;

        private DemoDesktopRig body;

        /// <summary>The simulated body, or null with a headset.</summary>
        public DemoDesktopRig Body => body;

        /// <summary>Off for the operator, whose place is at the wall.</summary>
        public bool LimitToRoom { get; set; } = true;

        /// <summary>Makes the simulated body the first time it is needed.</summary>
        private void Build()
        {
            if (body != null) return;
            // The kit takes the tracking camera under its own head. The camera component itself stays off.
            body = DemoDesktopRig.Create(transform.position, transform.eulerAngles.y, trackingCamera);
            body.name = "Desktop Body";
            body.EyeHeight = config.eyeHeight;
        }

        /// <summary>Moves the simulated body at once. Only for a desktop.</summary>
        public void Place(Vector3 position, DesktopView first)
        {
            Build();
            body.EyeHeight = first.eyeHeight;
            body.Pitch = first.pitch;
            body.Teleport(position, first.yaw);
            Copy();
        }

        /// <summary>The line with the keys. Hidden at first in a room, where the session bar needs the place; H shows it.</summary>
        public void ShowKeys(bool inRoom, bool isOperator)
        {
            if (body == null) return;
            body.HelpText = !inRoom ? MenuKeys : isOperator ? OperatorKeys : RoomKeys;
            body.ShowHelp = !inRoom || isOperator;
        }

        private void Update()
        {
            if (body == null) return;
            if (LimitToRoom)
            {
                Vector3 inside = config.ClampToRoom(body.Origin.position);
                if (inside != body.Origin.position) body.Origin.position = inside;
            }
            Copy();
        }

        private void Copy()
        {
            leftController.SetPositionAndRotation(body.LeftHand.position, body.LeftHand.rotation);
            rightController.SetPositionAndRotation(body.RightHand.position, body.RightHand.rotation);
        }
    }
}
