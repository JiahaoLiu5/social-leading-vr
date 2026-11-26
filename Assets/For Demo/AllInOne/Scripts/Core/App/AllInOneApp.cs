using System.Collections;
using System.Collections.Generic;
using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    public enum AppState { Starting, Menu, Connecting, Lobby, InRoom, Leaving }

    /// <summary>
    /// The way through the app: menu, connecting, lobby, room, and back to the menu without loading a scene.
    /// </summary>
    [DefaultExecutionOrder(-120)]
    public sealed class AllInOneApp : MonoBehaviour
    {
        private const float UnloadDelay = 4f;
        private const float DesktopMenuView = 50f;
        private const float DesktopRoomView = 68f;

        [SerializeField] private AppConfig config;

        [Header("Room")]
        [SerializeField] private RoomConnector connector;
        [SerializeField] private SessionState session;
        [SerializeField] private SessionDirector director;
        [SerializeField] private Roster roster;
        [SerializeField] private SoloPartners solo;
        [SerializeField] private StudyStage stage;

        [Header("User")]
        [SerializeField] private RigMode mode;
        [SerializeField] private DesktopRig desktop;
        [SerializeField] private Seating seating;
        [SerializeField] private StickMove stick;
        [SerializeField] private ViewRig view;
        [SerializeField] private AppInput input;
        [SerializeField] private AvatarBody preview;

        [Header("Panels")]
        [SerializeField] private StartPanel startPanel;
        [SerializeField] private LobbyPanel lobby;
        [SerializeField] private SessionHud hud;
        [SerializeField] private RoomMenu menu;
        [SerializeField] private OperatorPanel operatorPanel;

        private readonly List<DemoPointerRay> rays = new List<DemoPointerRay>();
        private DemoFade fade;
        private int seatedRole = -1, partners;
        private bool restart;
        private float unloadAt = -1f;

        public AppState State { get; private set; } = AppState.Starting;

        public LocalChoices Choices => connector.Choices;

        // Choices of the panels -------------------------------------------------------------------------------

        /// <summary>Changes the own avatar, in the menu and, when the rights allow it, in a room.</summary>
        public void ChooseAvatar(int avatar)
        {
            NetworkPlayer me = roster.Local;
            if (me != null && !session.MayChangeAvatar(me)) return;
            Choices.avatar = avatar;
            if (me != null) me.SetAvatar(avatar);
            else preview.SetAvatar(avatar);
            if (State == AppState.Menu) unloadAt = Time.unscaledTime + UnloadDelay;
        }

        /// <summary>Changes the own role inside the room, when the rights allow it.</summary>
        public void ChangeRole(int role)
        {
            NetworkPlayer me = roster.Local;
            if (me == null || !session.MayChangeRole(me) || roster.IsTaken(role)) return;
            Choices.role = role;
            me.SetRole(role);
            if (connector.Offline) solo.Fill(role, me.AvatarId);
        }

        /// <summary>Starts an experience from the beginning for everybody in the room.</summary>
        public void StartForEverybody(Experience experience, Director who, Pace pace)
        {
            if (!session.MayChangeExperience(roster.Local)) return;
            Choices.experience = experience;
            session.Begin(experience, Possible(who), pace);
        }

        /// <summary>The same session once more, for everybody in the room.</summary>
        public void RunAgain() => StartForEverybody(Experience.FullSession, session.Director, session.Pace);

        /// <summary>Free play after a session, for everybody in the room.</summary>
        public void StayAndPlay() => StartForEverybody(Experience.FreePlay, session.Director, session.Pace);

        /// <summary>In the lobby of a finished room: what starts when the user enters.</summary>
        public void ChooseRestart(Experience experience)
        {
            Choices.experience = experience;
            restart = true;
            DemoUi.Toast("It starts for everybody when you enter.");
        }

        public void DeleteAnswers()
        {
            bool deleted = AnswerStore.DeleteAll();
            DemoUi.Toast(deleted ? "Your answers were deleted from this device." : "The answers could not be deleted.", deleted ? DemoTone.Good : DemoTone.Warning);
        }

        // Way through the app ---------------------------------------------------------------------------------

        public void Join(bool alone, ParticipantKind kind)
        {
            if (State != AppState.Menu) return;
            State = AppState.Connecting;
            restart = false;
            Choices.kind = kind;
            Choices.solo = alone;
            startPanel.SetBusy(true, alone ? "Opening the room ..." : "Connecting to room " + Choices.room + " ...");
            if (alone) connector.JoinSolo();
            else connector.JoinOnline(Choices.room);
        }

        /// <summary>Goes from the lobby into the room.</summary>
        public void Enter()
        {
            if (State != AppState.Lobby) return;
            StartCoroutine(EnterRoutine());
        }

        public void Leave()
        {
            if (State == AppState.Lobby || State == AppState.InRoom) StartCoroutine(LeaveRoutine(null));
        }

        /// <summary>Leaves an online room in which nobody came and starts the same choices alone.</summary>
        public void SwitchToSolo()
        {
            if (State == AppState.InRoom) StartCoroutine(SwitchToSoloRoutine());
        }

        private void OnEnable()
        {
            mode.Decided += OnModeDecided;
            connector.Connected += OnConnected;
            connector.Failed += OnFailed;
            connector.Lost += OnLost;
            roster.Changed += OnRoster;
            session.Changed += OnSession;
        }

        private void OnDisable()
        {
            mode.Decided -= OnModeDecided;
            connector.Connected -= OnConnected;
            connector.Failed -= OnFailed;
            connector.Lost -= OnLost;
            roster.Changed -= OnRoster;
            session.Changed -= OnSession;
        }

        private void OnModeDecided()
        {
            DemoUi.Head = view.Camera.transform;
            fade = DemoFade.For(view.Camera);
            if (mode.Desktop) DemoInput.AddMouse(view.Camera);
            else
            {
                AddRay(view.LeftPointer, DemoHand.Left).ActiveWhen = () => !menu.OnLeftHand && !operatorPanel.OnLeftHand;
                AddRay(view.RightPointer, DemoHand.Right);
            }
            StartCoroutine(OpenMenuRoutine());
        }

        private DemoPointerRay AddRay(Transform origin, DemoHand hand)
        {
            DemoPointerRay ray = DemoInput.AddRay(origin, DemoPress.Controller(hand), hand);
            rays.Add(ray);
            return ray;
        }

        private IEnumerator OpenMenuRoutine()
        {
            GoToLobbyPlace();
            // The view follows the seat in the same frame, but the panel needs the final head position.
            yield return null;
            ShowMenu();
        }

        private void ShowMenu()
        {
            State = AppState.Menu;
            UsePreview();
            startPanel.Show(MenuPanelPose());
            startPanel.SetBusy(false);
            unloadAt = Time.unscaledTime + UnloadDelay;
        }

        /// <summary>Turned to the user with a headset, parallel to the picture on a desktop, so that it looks straight.</summary>
        private Pose MenuPanelPose()
        {
            Vector3 place = config.startPanelPosition;
            if (mode.Desktop) return new Pose(place, Quaternion.Euler(config.desktopPitch, config.lobbyDesktopYaw, 0f));
            Vector3 away = Vector3.ProjectOnPlane(place - DemoUi.Head.position, Vector3.up);
            return new Pose(place, away.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(away) : Quaternion.identity);
        }

        private void SetDesktopView(float fieldOfView)
        {
            if (mode.Desktop) view.Camera.fieldOfView = fieldOfView;
        }

        private void GoToLobbyPlace()
        {
            SetDesktopView(DesktopMenuView);
            desktop.LimitToRoom = true;
            seating.Body = view.Body = preview;
            seating.Apply(new Pose(config.lobbyPosition, Quaternion.Euler(0f, config.lobbyYaw, 0f)),
                new DesktopView { yaw = config.lobbyDesktopYaw, pitch = config.desktopPitch, eyeHeight = config.eyeHeight });
            desktop.ShowKeys(false, false);
        }

        private void UsePreview()
        {
            preview.gameObject.SetActive(true);
            preview.SetAvatar(Choices.avatar);
            preview.Snap();
        }

        private void OnConnected()
        {
            if (State != AppState.Connecting) return;
            roster.Refresh();
            State = AppState.Lobby;
            UseOwnBody();
            if (Choices.solo || Choices.kind == ParticipantKind.Operator)
            {
                StartCoroutine(EnterRoutine());
                return;
            }
            startPanel.Hide();
            lobby.Show(MenuPanelPose());
        }

        private void OnFailed()
        {
            ShowMenu();
            startPanel.GoToJoin("The room could not be reached.");
            DemoUi.Toast("The room could not be reached.", DemoTone.Warning);
            Uncover();
        }

        private void OnLost()
        {
            if (State == AppState.Leaving) return;
            StartCoroutine(LeaveRoutine("The connection was lost. You can join again."));
        }

        private IEnumerator EnterRoutine()
        {
            yield return Cover();
            NetworkPlayer me = roster.Local;
            // When the connection broke meanwhile, the way back to the menu is already running.
            if (me == null || State != AppState.Lobby) yield break;
            startPanel.Hide();
            lobby.Hide();
            bool participant = Choices.kind == ParticipantKind.Participant;
            if (participant)
            {
                int role = Choices.role == LocalChoices.RandomRole ? RandomFreeRole() : Choices.role;
                me.SetRole(role);
                me.SetAvatar(Choices.avatar);
            }
            bool idle = session.Idle;
            me.SetKind(Choices.kind);
            me.SetInRoom(true);
            me.SetMuted(Choices.kind == ParticipantKind.Operator || Choices.muted);
            roster.MarkDirty();
            roster.Refresh();
            BeginSession(idle);
            if (connector.Offline) solo.Fill(participant ? roster.RoleOf(me) : -1, Choices.avatar);
            roster.Refresh();
            State = AppState.InRoom;
            SetDesktopView(DesktopRoomView);
            partners = PartnerCount();
            TakeSeat();
            desktop.ShowKeys(true, !participant);
            hud.Begin();
            menu.Begin();
            // The view reaches the seat at the end of the frame, and the operator panel turns toward the view.
            yield return null;
            if (State != AppState.InRoom) yield break;
            if (Choices.kind == ParticipantKind.Operator) operatorPanel.Begin();
            Uncover();
        }

        /// <summary>The first person in a room decides what it runs. Everybody else joins what is there.</summary>
        private void BeginSession(bool idle)
        {
            if (Choices.kind == ParticipantKind.Operator)
            {
                if (idle) session.Begin(Experience.FullSession, Director.Operator, Choices.pace);
                return;
            }
            if (idle || restart && session.Finished)
            {
                if (connector.Offline && Choices.experience == Experience.FullSession && Choices.director == Director.Operator)
                    DemoUi.Toast("Solo runs automatically. To try the operator side, join as operator.");
                session.Begin(Choices.experience, Possible(Choices.director), Choices.pace);
                return;
            }
            if (session.Experience != Choices.experience)
                DemoUi.Toast(session.Experience == Experience.FreePlay ? "This room already runs free play." : "This room already runs a full session.");
        }

        /// <summary>A participant who is alone has no operator to wait for.</summary>
        private Director Possible(Director who) => connector.Offline && Choices.kind == ParticipantKind.Participant ? Director.Automatic : who;

        private int RandomFreeRole()
        {
            var free = new List<int>();
            for (int role = 0; role < config.roles.Count; role++)
                if (!roster.IsTaken(role)) free.Add(role);
            return free.Count > 0 ? free[Random.Range(0, free.Count)] : 0;
        }

        private IEnumerator LeaveRoutine(string message)
        {
            State = AppState.Leaving;
            yield return Cover();
            yield return CloseRoom();
            GoToLobbyPlace();
            UsePreview();
            yield return null;
            ShowMenu();
            if (message != null)
            {
                startPanel.GoToJoin(message);
                DemoUi.Toast(message, DemoTone.Warning);
            }
            else startPanel.GoToStart();
            Uncover();
        }

        private IEnumerator SwitchToSoloRoutine()
        {
            State = AppState.Leaving;
            yield return Cover();
            yield return CloseRoom();
            GoToLobbyPlace();
            UsePreview();
            State = AppState.Connecting;
            Choices.solo = true;
            Choices.kind = ParticipantKind.Participant;
            connector.JoinSolo();
        }

        private IEnumerator CloseRoom()
        {
            hud.End();
            menu.End();
            lobby.Hide();
            operatorPanel.End();
            startPanel.Hide();
            view.Body = seating.Body = null;
            seatedRole = -1;
            solo.Clear();
            yield return connector.Leave();
            stage.Clear();
        }

        // Inside the room -------------------------------------------------------------------------------------

        private void UseOwnBody()
        {
            NetworkPlayer me = roster.Local;
            bool hasBody = me != null && Choices.kind == ParticipantKind.Participant;
            view.Body = seating.Body = hasBody ? me.Body : null;
            if (!hasBody) preview.gameObject.SetActive(false);
        }

        /// <summary>Seats the user where the role that counts belongs.</summary>
        private void TakeSeat()
        {
            NetworkPlayer me = roster.Local;
            int role = roster.RoleOf(me);
            seatedRole = role;
            if (me.Kind == ParticipantKind.Participant && role < 0)
            {
                me.SetKind(ParticipantKind.Spectator);
                me.SetMuted(true);
                DemoUi.Toast("All roles are taken. You can watch, or choose another room.", DemoTone.Warning, 4f);
            }
            RoleCatalog.Entry seat = config.roles.Get(role);
            if (seat != null)
            {
                if (me.WantedRole != role)
                {
                    DemoUi.Toast(config.roles.Label(me.WantedRole) + " was already taken. You are the " + seat.label + ".", DemoTone.Warning, 4f);
                    me.SetRole(role);
                }
                view.Body = seating.Body = me.Body;
                seating.Apply(new Pose(seat.seatPosition, Quaternion.Euler(0f, seat.seatYaw, 0f)));
                return;
            }
            view.Body = seating.Body = null;
            desktop.LimitToRoom = false;
            bool atWall = mode.Desktop && Choices.kind == ParticipantKind.Operator;
            DesktopView? monitor = null;
            if (atWall) monitor = new DesktopView { yaw = config.operatorDesktopYaw, eyeHeight = config.operatorDesktopEyeHeight };
            seating.Apply(new Pose(atWall ? config.operatorDesktopPosition : config.watchPosition, Quaternion.Euler(0f, config.watchYaw, 0f)), monitor);
        }

        private void OnRoster()
        {
            if (State != AppState.InRoom || roster.Local == null) return;
            TellWhoCameAndLeft();
            if (roster.Local.Kind == ParticipantKind.Participant && roster.RoleOf(roster.Local) != seatedRole) StartCoroutine(MoveToSeat());
        }

        private void TellWhoCameAndLeft()
        {
            int before = partners;
            partners = PartnerCount();
            if (partners == before) return;
            string who = roster.Local.Kind == ParticipantKind.Participant ? "Your partner" : "A participant";
            DemoUi.Toast(who + (partners > before ? " entered the room." : " left the room."));
        }

        /// <summary>Other people with a role. Simulated partners do not come and go.</summary>
        private int PartnerCount()
        {
            int count = 0;
            foreach (IParticipant p in roster.People)
                if (!p.IsLocal && !p.IsBot && roster.RoleOf(p) >= 0) count++;
            return count;
        }

        private IEnumerator MoveToSeat()
        {
            seatedRole = roster.RoleOf(roster.Local);
            yield return Cover();
            if (State == AppState.InRoom && roster.Local != null) TakeSeat();
            yield return null;
            if (State == AppState.InRoom) Uncover();
        }

        private void OnSession(SessionChange change, bool local)
        {
            if (local || State != AppState.InRoom) return;
            switch (change)
            {
                case SessionChange.Started when session.Step < 0 && session.StepStartTime <= 0.0:
                    DemoUi.Toast(session.Experience == Experience.FreePlay ? "Free play was started for everybody." : "A full session was started for everybody.");
                    break;
                case SessionChange.Director when session.Experience == Experience.FullSession:
                    DemoUi.Toast(session.Director == Director.Operator ? "An operator controls the session now." : "The session runs automatically now.");
                    break;
            }
        }

        private void Update()
        {
            if (!mode.IsDecided) return;
            NetworkPlayer me = roster.Local;
            if (State == AppState.InRoom)
            {
                stage.Apply(director.View);
                if (!mode.Desktop && me != null) me.SetHandPose(input.HandPose);
            }
            // The preview stays until the networked body wears its character, so that the mirror is never empty.
            if (preview.gameObject.activeSelf && State != AppState.Menu && me != null && me.Body.dresser.Current != null)
                preview.gameObject.SetActive(false);
            stick.TurnAllowed = !stage.UsesThumbstick;
            foreach (DemoPointerRay ray in rays)
                ray.Line = stage.TaskOpen ? DemoPointerRay.LineMode.Always : DemoPointerRay.LineMode.NearPanel;
            ForgetOldCharacters();
        }

        /// <summary>Characters that were only tried on leave the memory a little later, never while in a room.</summary>
        private void ForgetOldCharacters()
        {
            if (unloadAt < 0f || Time.unscaledTime < unloadAt) return;
            unloadAt = -1f;
            if (State == AppState.Menu) Resources.UnloadUnusedAssets();
        }

        private IEnumerator Cover()
        {
            fade.Out(0.2f);
            while (fade.IsRunning) yield return null;
        }

        private void Uncover() => fade.In(0.35f);
    }
}
