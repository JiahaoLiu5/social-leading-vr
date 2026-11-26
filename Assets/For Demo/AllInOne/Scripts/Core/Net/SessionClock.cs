using Normal.Realtime;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// The one time source of a session. Online it is the room time. Offline it counts frames, so that it respects
    /// Time.captureFramerate and can run faster in tests.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class SessionClock : MonoBehaviour
    {
        [SerializeField] private Realtime realtime;

        private double offlineNow;
        private int connectFrame;

        /// <summary>True while connected to a room on the server.</summary>
        public bool Online => Connected && !realtime.room.offlineMode;

        public bool Connected => realtime != null && realtime.connected && realtime.room != null;

        /// <summary>Seconds. Only differences are meaningful.</summary>
        public double Now => Online ? realtime.roomTime : offlineNow;

        public float DeltaTime => Online ? Time.deltaTime : Time.deltaTime * TestSpeed;

        /// <summary>
        /// False until the time can be trusted. Right after a connect the room still reports the time of the previous room.
        /// </summary>
        public bool Valid => Connected && Time.frameCount >= connectFrame + 2 && (Online ? realtime.roomTime > 1.0 : offlineNow > 0.0);

        /// <summary>Speed of the offline clock, for tests. Ignored online.</summary>
        public float TestSpeed { get; set; } = 1f;

        private void OnEnable() => realtime.didConnectToRoom += OnConnected;

        private void OnDisable() => realtime.didConnectToRoom -= OnConnected;

        private void OnConnected(Realtime _) => connectFrame = Time.frameCount;

        private void Update()
        {
            if (!Connected) offlineNow = 0.0;
            else if (!Online) offlineNow = (offlineNow > 0.0 ? offlineNow : 1000.0) + Time.deltaTime * TestSpeed;
        }
    }
}
