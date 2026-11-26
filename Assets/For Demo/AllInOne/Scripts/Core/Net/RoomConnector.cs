using System;
using System.Collections;
using Normal.Realtime;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>Connects to a room on the server or to a local offline room, and leaves it again.</summary>
    public sealed class RoomConnector : MonoBehaviour
    {
        public enum ConnectorState { Disconnected, Connecting, Connected, Leaving }

        private const float TimeoutSeconds = 20f;

        [SerializeField] private AppConfig config;
        [SerializeField] private Realtime realtime;
        [SerializeField] private RealtimeAvatarManager avatars;

        private float connectingSince;

        /// <summary>The room is ready: connected, and this user's player exists.</summary>
        public event Action Connected;

        /// <summary>The room could not be reached.</summary>
        public event Action Failed;

        /// <summary>The connection broke.</summary>
        public event Action Lost;

        public LocalChoices Choices { get; } = new LocalChoices();

        public ConnectorState State { get; private set; } = ConnectorState.Disconnected;

        /// <summary>True in a room without a server.</summary>
        public bool Offline => realtime.connected && realtime.room != null && realtime.room.offlineMode;

        public int ClientId => realtime.connected ? realtime.clientID : -1;

        /// <summary>True when an app key is set. The value is never shown or logged.</summary>
        public bool HasKey => realtime.normcoreAppSettings != null
                              && !string.IsNullOrWhiteSpace(realtime.normcoreAppSettings.normcoreAppKey);

        public void JoinOnline(int room) => Connect(config.RoomName(room), new Room.ConnectOptions());

        /// <summary>
        /// Opens a local room without a server. Normcore checks key and address before it looks at the offline flag, so
        /// placeholders are passed; the user's settings are not read.
        /// </summary>
        public void JoinSolo() => Connect(config.soloRoom, new Room.ConnectOptions
        {
            offlineMode = true,
            appKey = "offline",
            matcherURL = "wss://offline.invalid"
        });

        /// <summary>Leaves the room. The routine ends when the next connect is safe.</summary>
        public IEnumerator Leave()
        {
            if (State == ConnectorState.Disconnected) yield break;
            State = ConnectorState.Leaving;
            if (realtime.connected || realtime.connecting) realtime.Disconnect();
            yield return Settle();
            State = ConnectorState.Disconnected;
        }

        private void Connect(string roomName, Room.ConnectOptions options)
        {
            if (State != ConnectorState.Disconnected) return;
            // The offline connect ends inside the call, so everything is set before it.
            State = ConnectorState.Connecting;
            connectingSince = Time.unscaledTime;
            Choices.connection++;
            realtime.Connect(roomName, options);
        }

        private void OnEnable()
        {
            realtime.didConnectToRoom += OnConnected;
            realtime.didDisconnectFromRoom += OnDisconnected;
        }

        private void OnDisable()
        {
            realtime.didConnectToRoom -= OnConnected;
            realtime.didDisconnectFromRoom -= OnDisconnected;
        }

        private void Update()
        {
            if (State != ConnectorState.Connecting) return;
            bool broken = realtime.room != null && realtime.room.connectionState == Room.ConnectionState.Error;
            if (broken || Time.unscaledTime - connectingSince > TimeoutSeconds) StartCoroutine(GiveUp());
        }

        private void OnConnected(Realtime _)
        {
            if (State == ConnectorState.Connecting) StartCoroutine(FinishConnect());
        }

        private IEnumerator FinishConnect()
        {
            // The player prefab and the first room time arrive within the next frames.
            for (int frame = 0; frame < 2 || avatars.localAvatar == null; frame++)
            {
                yield return null;
                if (State != ConnectorState.Connecting) yield break;
            }
            State = ConnectorState.Connected;
            Connected?.Invoke();
        }

        private void OnDisconnected(Realtime _)
        {
            switch (State)
            {
                case ConnectorState.Connecting:
                    StartCoroutine(GiveUp());
                    break;
                case ConnectorState.Connected:
                    StartCoroutine(LoseConnection());
                    break;
            }
        }

        private IEnumerator GiveUp()
        {
            yield return Leave();
            Failed?.Invoke();
        }

        private IEnumerator LoseConnection()
        {
            yield return Leave();
            Lost?.Invoke();
        }

        /// <summary>Normcore destroys the player prefabs at the end of the frame. A connect before that makes no new player.</summary>
        private IEnumerator Settle()
        {
            while (!realtime.disconnected) yield return null;
            yield return null;
            yield return null;
        }
    }
}
