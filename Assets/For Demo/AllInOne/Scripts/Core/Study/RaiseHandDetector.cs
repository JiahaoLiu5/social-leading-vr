using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Notices that the user holds the tracked left hand above the head. Only listens while it is asked to.</summary>
    public sealed class RaiseHandDetector
    {
        private const float Height = 0.10f;
        private const float HoldSeconds = 0.7f;

        private float held;

        /// <summary>True once the hand was held up long enough since the last reset.</summary>
        public bool Raised { get; private set; }

        public void Reset()
        {
            held = 0f;
            Raised = false;
        }

        public void Step(AvatarBody body, float deltaTime)
        {
            if (Raised || body == null) return;
            bool up = body.leftHand.position.y > body.head.position.y + Height;
            held = up ? held + deltaTime : 0f;
            if (held >= HoldSeconds) Raised = true;
        }
    }
}
