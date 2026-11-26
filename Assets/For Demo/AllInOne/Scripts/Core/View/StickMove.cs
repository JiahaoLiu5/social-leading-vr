using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Thumbstick movement for people without much room: smooth walking on the left stick, snap turns on the right. It moves
    /// the node below the rig root, like walking does. Not part of the study.
    /// </summary>
    public sealed class StickMove : MonoBehaviour
    {
        private const float DeadZone = 0.2f;

        [SerializeField] private AppConfig config;
        [SerializeField] private RigMode mode;
        [SerializeField] private AppInput input;
        [SerializeField] private Transform recentre;
        [SerializeField] private Transform trackedHead;
        [SerializeField] private float speed = 1.2f;
        [SerializeField] private float turnDegrees = 30f;

        private bool turned;

        /// <summary>False while the right stick moves a slider.</summary>
        public bool TurnAllowed { get; set; } = true;

        private void Update()
        {
            if (!mode.IsDecided || mode.Desktop) return;
            Walk(input.LeftStick);
            Turn(input.RightStick.x);
        }

        private void Walk(Vector2 stick)
        {
            if (stick.sqrMagnitude < DeadZone * DeadZone) return;
            Vector3 direction = Quaternion.Euler(0f, trackedHead.eulerAngles.y, 0f) * new Vector3(stick.x, 0f, stick.y);
            Vector3 head = trackedHead.position;
            Vector3 goal = config.ClampToRoom(head + direction * (speed * Time.deltaTime));
            recentre.position += new Vector3(goal.x - head.x, 0f, goal.z - head.z);
        }

        private void Turn(float x)
        {
            if (Mathf.Abs(x) < 0.4f) turned = false;
            if (turned || !TurnAllowed || Mathf.Abs(x) < 0.7f) return;
            turned = true;
            recentre.RotateAround(trackedHead.position, Vector3.up, Mathf.Sign(x) * turnDegrees);
        }
    }
}
