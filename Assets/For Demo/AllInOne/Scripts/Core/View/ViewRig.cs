using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Keeps the user's camera out of the networked avatar, so that the view survives leaving a room. The camera stands at
    /// the tracked head plus the offset that the movement link gives the displayed head, which is the same place as a
    /// camera inside the avatar's head.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ViewRig : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform trackingSpace;
        [SerializeField] private Transform trackedHead;
        [SerializeField] private RigMode mode;
        [SerializeField] private Transform leftController;
        [SerializeField] private Transform rightController;
        [SerializeField] private Transform leftPointer;
        [SerializeField] private Transform rightPointer;

        public Camera Camera => viewCamera;
        public Transform LeftPointer => leftPointer;
        public Transform RightPointer => rightPointer;

        /// <summary>The body whose displayed head this view follows, or null.</summary>
        public AvatarBody Body { get; set; }

        /// <summary>The share of the partner's steps in this view (metres, horizontal).</summary>
        public Vector3 Offset { get; private set; }

        private void OnEnable() => Application.onBeforeRender += Step;

        private void OnDisable() => Application.onBeforeRender -= Step;

        private void LateUpdate() => Step();

        [BeforeRenderOrder(100)]
        private void Step()
        {
            Offset = Body != null ? Body.ViewOffset : Vector3.zero;
            if (mode.Desktop)
            {
                viewCamera.transform.SetPositionAndRotation(trackedHead.position + Offset, trackedHead.rotation);
            }
            else
            {
                transform.SetPositionAndRotation(trackingSpace.position + Offset, trackingSpace.rotation);
                viewCamera.transform.localPosition = trackedHead.localPosition;
                viewCamera.transform.localRotation = trackedHead.localRotation;
            }
            leftPointer.SetPositionAndRotation(leftController.position + Offset, leftController.rotation);
            rightPointer.SetPositionAndRotation(rightController.position + Offset, rightController.rotation);
        }
    }
}
