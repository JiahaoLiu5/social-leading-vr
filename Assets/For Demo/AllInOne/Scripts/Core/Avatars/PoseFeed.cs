using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Copies the user's rig into a body that is not networked, for the preview in front of the mirror.</summary>
    [DefaultExecutionOrder(-90)]
    [RequireComponent(typeof(AvatarBody))]
    public sealed class PoseFeed : MonoBehaviour
    {
        public Transform root;
        public Transform head;
        public Transform leftHand;
        public Transform rightHand;

        private AvatarBody body;

        private void Awake() => body = GetComponent<AvatarBody>();

        private void Update() => Copy();

        private void LateUpdate() => Copy();

        private void Copy()
        {
            transform.SetPositionAndRotation(root.position, root.rotation);
            body.head.SetPositionAndRotation(head.position, head.rotation);
            body.leftHand.SetPositionAndRotation(leftHand.position, leftHand.rotation);
            body.rightHand.SetPositionAndRotation(rightHand.position, rightHand.rotation);
        }
    }
}
