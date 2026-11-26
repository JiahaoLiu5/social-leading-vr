using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Times and scripted replies of the dyad tasks, with the values of the study.</summary>
    [CreateAssetMenu(menuName = "Social Leading/All-in-One/Dyad Task Config", fileName = "Dyad Task Config")]
    public sealed class DyadTaskConfig : ScriptableObject
    {
        [Tooltip("The scripted partner rejects when the own first split is at or above this value.")]
        public int rejectThreshold = 80;
        public string acceptText = "accepts";
        public string rejectText = "rejects";
        public float questionSeconds = 3f;
        public float waitSeconds = 3f;
        public float replySeconds = 5f;

        [Header("Thumbstick")]
        public float stickDeadZone = 0.1f;
        [Tooltip("Seconds between two slider steps on the split panels.")]
        public float splitInterval = 0.05f;
        [Tooltip("Seconds between two slider steps on the rating panels.")]
        public float ratingInterval = 0.25f;
        public int maxIncrement = 100;
    }
}
