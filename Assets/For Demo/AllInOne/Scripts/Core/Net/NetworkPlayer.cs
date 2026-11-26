using Normal.Realtime;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// A networked player. The avatar of the own body, of the mirror image, and of every other client comes from the same
    /// property, so what a user sees as their avatar is what everybody else sees.
    /// </summary>
    [RequireComponent(typeof(AvatarBody))]
    public sealed class NetworkPlayer : RealtimeComponent<PlayerModel>, IParticipant
    {
        private const float HandPoseInterval = 0.05f;

        [SerializeField] private AppConfig config;
        [SerializeField] private RealtimeAvatarVoice voice;

        private AvatarBody body;
        private Roster roster;
        private RoomConnector connector;
        private bool announced;
        private float nextHandPose;

        public int Id => realtimeView.ownerIDSelf;
        public bool IsLocal => realtimeView.isOwnedLocallySelf;
        public bool IsBot => false;
        public bool InRoom => model != null && model.phase == 1;
        public ParticipantKind Kind => model != null ? (ParticipantKind)model.kind : ParticipantKind.Participant;
        public int AvatarId => model != null ? model.avatarId : -1;
        public int WantedRole => model != null ? model.role : -1;
        public int DoneMark => model != null ? model.doneMark : 0;
        public string UserKey => model != null ? model.userKey : string.Empty;
        public int Connection => model != null ? model.connection : 0;
        public bool Muted => model != null && model.muted;
        public AvatarBody Body => body;

        // Commands of the owner -------------------------------------------------------------------------------

        public void SetAvatar(int avatar) { if (IsLocal) model.avatarId = avatar; }

        public void SetRole(int role) { if (IsLocal) model.role = role; }

        public void SetKind(ParticipantKind kind) { if (IsLocal) model.kind = (int)kind; }

        public void SetInRoom(bool inRoom) { if (IsLocal) model.phase = inRoom ? 1 : 0; }

        public void SetMuted(bool muted) { if (IsLocal) model.muted = muted; }

        public void SetDone(int mark) { if (IsLocal) model.doneMark = mark; }

        /// <summary>Finger poses: left pinch, left grab, right pinch, right grab. Sent at most 20 times per second.</summary>
        public void SetHandPose(Vector4 pose)
        {
            if (!IsLocal || Time.unscaledTime < nextHandPose || (model.handPose - pose).sqrMagnitude < 1e-4f) return;
            nextHandPose = Time.unscaledTime + HandPoseInterval;
            model.handPose = pose;
        }

        // Life ------------------------------------------------------------------------------------------------

        private void Awake() => body = GetComponent<AvatarBody>();

        private void Start()
        {
            roster = realtime.GetComponent<Roster>();
            connector = realtime.GetComponent<RoomConnector>();
            if (IsLocal) Announce(connector.Choices);
            roster.Changed += ApplyRole;
            roster.Add(this);
            ApplyAll();
        }

        private void OnDestroy()
        {
            if (roster == null) return;
            roster.Changed -= ApplyRole;
            roster.Remove(this);
        }

        /// <summary>The owner tells the room who they are.</summary>
        private void Announce(LocalChoices choices)
        {
            if (announced) return;
            announced = true;
            bool participant = choices.kind == ParticipantKind.Participant;
            model.userKey = choices.userKey;
            model.connection = choices.connection;
            model.kind = (int)choices.kind;
            model.role = participant ? choices.role : -1;
            model.avatarId = participant ? choices.avatar : -1;
            model.muted = !participant || choices.muted;
            model.phase = 0;
        }

        private void Update()
        {
            if (model != null) body.HandPose = model.handPose;
        }

        protected override void OnRealtimeModelReplaced(PlayerModel previousModel, PlayerModel currentModel)
        {
            if (previousModel != null) Subscribe(previousModel, false);
            if (currentModel == null) return;
            Subscribe(currentModel, true);
            if (roster != null) ApplyAll();
        }

        private void Subscribe(PlayerModel m, bool on)
        {
            m.avatarIdDidChange -= OnAvatar;
            m.roleDidChange -= OnRoster;
            m.kindDidChange -= OnRoster;
            m.phaseDidChange -= OnRoster;
            m.doneMarkDidChange -= OnRoster;
            m.connectionDidChange -= OnRoster;
            m.userKeyDidChange -= OnUserKey;
            m.mutedDidChange -= OnMuted;
            if (!on) return;
            m.avatarIdDidChange += OnAvatar;
            m.roleDidChange += OnRoster;
            m.kindDidChange += OnRoster;
            m.phaseDidChange += OnRoster;
            m.doneMarkDidChange += OnRoster;
            m.connectionDidChange += OnRoster;
            m.userKeyDidChange += OnUserKey;
            m.mutedDidChange += OnMuted;
        }

        private void OnAvatar(PlayerModel m, int value)
        {
            body.SetAvatar(value);
            if (roster != null) roster.MarkDirty();
        }

        private void OnRoster(PlayerModel m, int value)
        {
            if (roster != null) roster.MarkDirty();
            ApplyVoice();
        }

        private void OnUserKey(PlayerModel m, string value)
        {
            if (roster != null) roster.MarkDirty();
        }

        private void OnMuted(PlayerModel m, bool value)
        {
            if (roster != null) roster.MarkDirty();
            ApplyVoice();
        }

        private void ApplyAll()
        {
            body.SetAvatar(AvatarId);
            ApplyRole();
            ApplyVoice();
            roster.MarkDirty();
        }

        /// <summary>Tag and visibility follow the role that counts, not the wanted one.</summary>
        private void ApplyRole()
        {
            int role = roster.RoleOf(this);
            RoleCatalog.Entry entry = config.roles.Get(role);
            string wanted = entry != null && !string.IsNullOrEmpty(entry.tag) ? entry.tag : "Untagged";
            if (!CompareTag(wanted)) tag = wanted;
            bool inLobby = IsLocal && !InRoom;
            body.SetVisible(Kind == ParticipantKind.Participant && !roster.IsGhost(this) && (inLobby || role >= 0));
        }

        private void ApplyVoice()
        {
            if (voice == null || model == null || connector == null) return;
            // Offline there is no voice stream, and the microphone would be opened for nothing.
            bool use = !connector.Offline && (!IsLocal || connector.Choices.microphone);
            if (voice.enabled != use) voice.enabled = use;
            voice.mute = model.muted || model.phase == 0;
        }
    }
}
