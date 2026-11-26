using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// A simulated partner of an offline room. It moves and follows the steps, but it does not talk. It is not networked and
    /// never advances a session.
    /// </summary>
    [RequireComponent(typeof(AvatarBody), typeof(SimulatedRig), typeof(BotMover))]
    public sealed class BotPartner : MonoBehaviour, IParticipant
    {
        private const int FirstId = 1000;
        private const float DoneAfterUser = 1.5f;
        private const float ShowSeconds = 5f;
        private const float RaiseSeconds = 2.5f;

        private static readonly SimulatedRig.MotionKind[] Show =
        {
            SimulatedRig.MotionKind.Wave, SimulatedRig.MotionKind.RaiseHands, SimulatedRig.MotionKind.Talk, SimulatedRig.MotionKind.Reach
        };

        private AppConfig config;
        private SessionState session;
        private SessionDirector director;
        private Roster roster;
        private StudyStage stage;
        private AvatarBody body;
        private SimulatedRig rig;
        private BotMover mover;
        private int role, doneMark, watchedMark;
        private float userDoneSince, playClock, raiseUntil;

        public int Id => FirstId + role;
        public bool IsLocal => false;
        public bool IsBot => true;
        public bool InRoom => true;
        public ParticipantKind Kind => ParticipantKind.Participant;
        public int AvatarId => body.AvatarId;
        public int WantedRole => role;
        public int DoneMark => doneMark;
        public string UserKey => string.Empty;
        public int Connection => 0;
        public bool Muted => true;
        public AvatarBody Body => body;

        /// <summary>The simulated tracking of this partner, for tests.</summary>
        public SimulatedRig Rig => rig;

        public void Begin(AppConfig appConfig, SessionState sessionState, SessionDirector sessionDirector, Roster everybody, StudyStage studyStage,
            int wantedRole, int avatar)
        {
            config = appConfig;
            session = sessionState;
            director = sessionDirector;
            roster = everybody;
            stage = studyStage;
            role = wantedRole;
            body = GetComponent<AvatarBody>();
            rig = GetComponent<SimulatedRig>();
            mover = GetComponent<BotMover>();
            RoleCatalog.Entry entry = config.roles.Get(role);
            name = "Simulated " + entry.label;
            if (!string.IsNullOrEmpty(entry.tag)) tag = entry.tag;
            rig.phase = role * 1.7f;
            mover.Teleport(entry.seatPosition, entry.seatYaw);
            body.SetAvatar(avatar);
            body.Snap();
            roster.Add(this);
        }

        private void OnDestroy()
        {
            if (roster != null) roster.Remove(this);
        }

        private void Update()
        {
            if (session == null) return;
            StepView view = director.View;
            StudyStep step = config.script.Get(view.step);
            if (step != null)
            {
                FollowStep(step, view);
                UpdateDone(step, view);
            }
            else if (session.Experience == Experience.FreePlay) Play();
            else Rest();
        }

        // Free play -------------------------------------------------------------------------------------------

        /// <summary>The first role shows movements that the others can follow. The others stand still and show the link.</summary>
        private void Play()
        {
            GoToSeat();
            playClock += Time.deltaTime;
            Move(role == 0 ? Show[(int)(playClock / ShowSeconds) % Show.Length] : SimulatedRig.MotionKind.Idle);
        }

        private void Rest()
        {
            GoToSeat();
            Move(SimulatedRig.MotionKind.Idle);
        }

        private void Move(SimulatedRig.MotionKind motion)
        {
            if (rig.motion == motion) return;
            rig.motion = motion;
            rig.RestartMotion();
        }

        // Full session ----------------------------------------------------------------------------------------

        private void FollowStep(StudyStep step, StepView view)
        {
            BotCue cue = CurrentCue(step, view);
            if (cue == null)
            {
                Move(SimulatedRig.MotionKind.Idle);
                return;
            }
            MoveTo(cue);
            bool second = cue.interval > 0f && (int)(view.time / cue.interval) % 2 == 1;
            bool raising = Time.time < raiseUntil;
            Move(raising ? SimulatedRig.MotionKind.RaiseLeft : mover.IsWalking ? SimulatedRig.MotionKind.Idle : second ? cue.alternate : cue.motion);
        }

        /// <summary>The last cue whose time has come.</summary>
        private BotCue CurrentCue(StudyStep step, StepView view)
        {
            BotCue current = null;
            float length = config.script.Length(view.step);
            foreach (BotCue cue in step.bots)
            {
                double time = cue.clock == BotCue.Clock.Step ? view.time : view.time - length;
                if (time >= cue.at) current = cue;
            }
            return current;
        }

        private void MoveTo(BotCue cue)
        {
            switch (cue.place)
            {
                case BotCue.Place.Seat:
                    GoToSeat();
                    break;
                case BotCue.Place.NearUser:
                    GoNear(cue.distance);
                    break;
                case BotCue.Place.MirrorSide:
                    mover.Go(transform.position, config.lobbyYaw);
                    break;
                case BotCue.Place.OwnBoard:
                    if (!TryFindOther(out Vector3 head)) break;
                    Vector3 stand = stage.BoardFor(head, out float yaw);
                    mover.Go(stand, yaw);
                    break;
            }
        }

        private void GoToSeat()
        {
            RoleCatalog.Entry entry = config.roles.Get(role);
            mover.Go(entry.seatPosition, entry.seatYaw);
        }

        private void GoNear(float distance)
        {
            if (!TryFindOther(out Vector3 head)) return;
            Vector3 toMe = transform.position - new Vector3(head.x, 0f, head.z);
            toMe.y = 0f;
            if (toMe.sqrMagnitude < 1e-4f) toMe = Vector3.forward;
            Vector3 place = config.ClampToRoom(new Vector3(head.x, 0f, head.z) + toMe.normalized * distance);
            mover.Go(place, Quaternion.LookRotation(-toMe).eulerAngles.y);
        }

        /// <summary>Displayed head of the user, or of another simulated partner in a rehearsal.</summary>
        private bool TryFindOther(out Vector3 head)
        {
            IParticipant other = null;
            for (int i = 0; i < roster.People.Count; i++)
            {
                IParticipant p = roster.People[i];
                if (!ReferenceEquals(p, this) && roster.RoleOf(p) >= 0 && (other == null || !p.IsBot)) other = p;
            }
            head = other != null ? other.Body.headOut.position : Vector3.zero;
            return other != null;
        }

        // Done ------------------------------------------------------------------------------------------------

        private void UpdateDone(StudyStep step, StepView view)
        {
            int mark = session.Mark(view.step);
            if (watchedMark != mark)
            {
                watchedMark = mark;
                userDoneSince = 0f;
            }
            if (doneMark == mark || !MayBeDone(step, view, mark)) return;
            doneMark = mark;
            roster.MarkDirty();
            // The clip asks for a raised left hand when the questions are answered.
            if (step.HasCue("raise-hand")) raiseUntil = Time.time + RaiseSeconds;
        }

        private bool MayBeDone(StudyStep step, StepView view, int mark)
        {
            NetworkPlayer user = roster.Local;
            bool userTakesPart = user != null && roster.RoleOf(user) >= 0;
            if (userTakesPart)
            {
                if (user.DoneMark != mark) return false;
                userDoneSince += Time.deltaTime;
                if (userDoneSince < DoneAfterUser) return false;
            }
            else if (step.rule != AdvanceRule.AllDone) return false;
            float needed = step.botDoneSeconds * (session.Pace == Pace.Quick ? 0.5f : 1f);
            return step.rule != AdvanceRule.AllDone || view.time >= needed + (userTakesPart ? 0f : config.script.Length(view.step));
        }
    }
}
