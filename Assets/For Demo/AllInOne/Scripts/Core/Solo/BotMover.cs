using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Walks a simulated partner to a place. It moves the body root, which the movement link does not see, so the user's view
    /// stays calm while the partner comes closer.
    /// </summary>
    public sealed class BotMover : MonoBehaviour
    {
        private const float StartDistance = 0.25f;
        private const float StopDistance = 0.04f;

        [SerializeField] private float speed = 1.0f;
        [SerializeField] private float turnSpeed = 180f;
        [SerializeField] private float acceleration = 2.5f;

        private Vector3 goal;
        private float goalYaw, currentSpeed;
        private bool walking;

        public bool IsWalking => walking;

        public void Teleport(Vector3 position, float yaw)
        {
            goal = position;
            goalYaw = yaw;
            currentSpeed = 0f;
            walking = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        }

        /// <summary>Sets the place to walk to and the direction to face there.</summary>
        public void Go(Vector3 position, float yaw)
        {
            goal = position;
            goalYaw = yaw;
            if (!walking && Distance() > StartDistance) walking = true;
        }

        private void Update()
        {
            float distance = Distance();
            if (walking && distance < StopDistance) walking = false;
            float wanted = walking ? Mathf.Min(speed, distance * 3f) : 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, wanted, acceleration * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, walking ? goal : transform.position, currentSpeed * Time.deltaTime);

            float yaw = goalYaw;
            if (walking && distance > 0.5f) yaw = Quaternion.LookRotation(goal - transform.position).eulerAngles.y;
            Quaternion turn = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, turn, turnSpeed * Time.deltaTime);
        }

        private float Distance()
        {
            Vector3 rest = goal - transform.position;
            rest.y = 0f;
            return rest.magnitude;
        }
    }
}
