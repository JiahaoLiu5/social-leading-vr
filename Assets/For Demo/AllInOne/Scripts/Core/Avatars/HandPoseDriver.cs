using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Moves the fingers of the character with the trigger and grip values of its person. Not part of the study.</summary>
    [RequireComponent(typeof(AvatarBody))]
    public sealed class HandPoseDriver : MonoBehaviour
    {
        private static readonly int LeftPinch = Animator.StringToHash("Left Pinch");
        private static readonly int LeftGrab = Animator.StringToHash("Left Grab");
        private static readonly int RightPinch = Animator.StringToHash("Right Pinch");
        private static readonly int RightGrab = Animator.StringToHash("Right Grab");

        [SerializeField] private float timeConstant = 0.06f;

        private AvatarBody body;
        private Vector4 shown;

        private void Awake() => body = GetComponent<AvatarBody>();

        private void Update()
        {
            Animator animator = body.dresser.CurrentAnimator;
            if (animator == null || !animator.isActiveAndEnabled) return;
            shown = Vector4.Lerp(shown, body.HandPose, 1f - Mathf.Exp(-Time.deltaTime / timeConstant));
            animator.SetFloat(LeftPinch, shown.x);
            animator.SetFloat(LeftGrab, shown.y);
            animator.SetFloat(RightPinch, shown.z);
            animator.SetFloat(RightGrab, shown.w);
        }
    }
}
