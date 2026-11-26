using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// A slim bar at the lower edge of the view that says what to do now. It stays below boards, faces, and captions, and it
    /// is gone while a task panel is open. Its content is built again when the state changes; while the state stays, only
    /// the timer changes.
    /// </summary>
    public sealed class SessionHud : MonoBehaviour
    {
        private enum State { Hidden, Hint, WaitingForPartner, WaitingForOperator, Countdown, Narration, Response, Paused, Finished }

        private const float Width = 1000f;
        private const float ButtonHeight = 76f;
        // Close to the user and small, so that a partner at arm's length does not stand in front of it.
        private const float Distance = 0.75f;
        private const float Scale = 0.54f;
        // Metres below the eyes. The upper edge stays at one place, whatever the height of the content.
        private const float UpperEdge = -0.352f;
        private const float MenuHintSeconds = 10f;
        private const string PartnerHint = "Simulated partner. It moves but does not talk.";

        [SerializeField] private AppConfig config;
        [SerializeField] private AllInOneApp app;
        [SerializeField] private SessionState session;
        [SerializeField] private SessionDirector director;
        [SerializeField] private Roster roster;
        [SerializeField] private RoomConnector connector;
        [SerializeField] private StudyStage stage;
        [SerializeField] private RigMode mode;
        [SerializeField] private AppInput input;

        private DemoPanel panel;
        private DemoProgressBar timer;
        private DemoLabel countdown;
        private State shown = State.Hidden;
        private int shownMark = -1, shownSecond = -1, shownSeated = -1, shownAnswers = -1;
        private bool shownOperator, shownDone, active;
        private float hintUntil;

        /// <summary>Switches the panel on when the user is in a room.</summary>
        public void Begin()
        {
            active = true;
            shown = State.Hidden;
            shownMark = shownSecond = shownSeated = -1;
            hintUntil = DemoClock.Now + MenuHintSeconds;
            // Whoever comes late into a running session does not see the bar with the hint.
            if (session.Running) DemoUi.Toast(MenuHint());
        }

        public void End()
        {
            active = false;
            shown = State.Hidden;
            if (panel != null) panel.Hide();
        }

        private bool IsParticipant => roster.Local.Kind == ParticipantKind.Participant;

        /// <summary>True when somebody else's movement reaches this user in a full session.</summary>
        private bool IsMoved => config.topologies.SourceOf(0, roster.RoleOf(roster.Local)) >= 0;

        private bool IsDone => session.Running && IsParticipant && roster.Local.DoneMark == session.Mark(session.Step);

        private bool MayFinish => session.Running && IsParticipant && !IsDone && director.Step != null && director.Step.rule == AdvanceRule.TimeOrAllDone;

        private void Update()
        {
            if (!active || roster.Local == null) return;
            State wanted = Wanted();
            int mark = session.Running ? session.Mark(session.Step) : session.SessionId;
            bool changed = wanted != shown || mark != shownMark
                           || wanted == State.Response && shownDone != IsDone
                           || wanted != State.Hidden && shownSeated != roster.SeatedCount
                           || wanted == State.WaitingForOperator && shownOperator != roster.OperatorPresent
                           || wanted == State.Finished && shownAnswers != AnswerStore.Version;
            if (changed) Build(wanted, mark);
            Tick();
            if (wanted == State.Response && input.DonePressed) Done();
        }

        private State Wanted()
        {
            if (roster.Local.Kind == ParticipantKind.Operator) return State.Hidden;
            switch (session.Experience)
            {
                case Experience.FreePlay:
                    return DemoClock.Now < hintUntil ? State.Hint : State.Hidden;
                case Experience.FullSession:
                    if (session.Finished) return State.Finished;
                    // Also when the operator paused the session and left: then somebody must be able to go on.
                    if (session.Director == Director.Operator && (session.Waiting || !roster.OperatorPresent)) return State.WaitingForOperator;
                    if (session.Paused) return State.Paused;
                    return WantedInSession();
                default:
                    return State.Hidden;
            }
        }

        private State WantedInSession()
        {
            if (stage.TaskOpen) return State.Hidden;
            switch (director.View.phase)
            {
                case StepPhase.Waiting: return State.WaitingForPartner;
                case StepPhase.Countdown: return State.Countdown;
                case StepPhase.Narration: return State.Narration;
                default: return session.Running ? State.Response : State.Hidden;
            }
        }

        // Content ---------------------------------------------------------------------------------------------

        private void Build(State wanted, int mark)
        {
            shown = wanted;
            shownMark = mark;
            shownSecond = -1;
            shownSeated = roster.SeatedCount;
            shownOperator = roster.OperatorPresent;
            shownDone = IsDone;
            shownAnswers = AnswerStore.Version;
            timer = null;
            countdown = null;
            if (wanted == State.Hidden)
            {
                if (panel != null) panel.Hide();
                return;
            }
            bool appears = panel == null || !panel.IsShown;
            DemoBuilder b = panel == null ? DemoUi.Panel("hud", Width, 0f, 30f) : panel.Rebuild();
            switch (wanted)
            {
                case State.Hint: BuildHint(b); break;
                case State.WaitingForPartner: BuildWaiting(b); break;
                case State.WaitingForOperator: BuildWaitingForOperator(b); break;
                case State.Countdown: BuildCountdown(b); break;
                case State.Narration: BuildStep(b, false); break;
                case State.Response: BuildStep(b, true); break;
                case State.Paused: BuildPaused(b); break;
                case State.Finished: BuildFinished(b); break;
            }
            panel = b.Build();
            panel.Scale(Scale).OnTop(5).OverScene();
            Place(appears);
        }

        private void Place(bool appears)
        {
            float height = UpperEdge - panel.SizeMetres.y * 0.5f;
            if (appears)
            {
                panel.FollowView();
                panel.ShowInFrontOf(DemoUi.Head, Distance, height, DemoFollow.Lazy, mode.Desktop ? DemoBillboard.Yaw : DemoBillboard.Full);
                return;
            }
            panel.Placement.height = height;
            panel.Placement.Recentre();
        }

        private void BuildHint(DemoBuilder b)
        {
            b.Text("Free play. " + MenuHint(), true);
            if (connector.Offline) b.Caption(PartnerHint);
        }

        private string MenuHint() => mode.Desktop ? "Menu: Tab. All keys: H." : "Menu: left Y button.";

        private void BuildWaiting(DemoBuilder b)
        {
            b.Row(r =>
            {
                string here = roster.SeatedCount + " of " + config.roles.Count + " people are here.";
                r.Column(c => c.Text("Waiting for your partner", true).Caption(here).Caption(MenuHint()), 1f, 6f);
                if (!connector.Offline && IsParticipant) r.Button("solo", "Start in solo instead", app.SwitchToSolo, DemoStyle.Secondary, DemoIcon.Person, 0f, ButtonHeight);
            });
        }

        private void BuildWaitingForOperator(DemoBuilder b)
        {
            string text = roster.OperatorPresent ? "The operator starts the session." : "No operator is in the room.";
            b.Row(r =>
            {
                r.Column(c =>
                {
                    c.Text("Waiting for the operator", true).Caption(text).Caption(MenuHint());
                    if (session.Waiting && IsMoved) c.Caption(config.comfortNote);
                }, 1f, 6f);
                if (!roster.OperatorPresent && session.MayChangeDirector(roster.Local))
                    r.Button("to-auto", "Switch to automatic", () => session.SetDirector(Director.Automatic), DemoStyle.Secondary, DemoIcon.None, 0f, ButtonHeight);
            });
        }

        private void BuildCountdown(DemoBuilder b)
        {
            b.Row(r => r
                .Column(c => c.Text("Get ready", true).Label("countdown", " ", DemoTextStyle.Body).Out(out countdown), 0.5f, 6f)
                .Column(c =>
                {
                    if (IsMoved) c.Caption(config.comfortNote);
                    if (connector.Offline) c.Caption(PartnerHint);
                    c.Caption(MenuHint());
                }, 1f, 6f), 30f);
        }

        private void BuildStep(DemoBuilder b, bool response)
        {
            StudyStep step = director.Step;
            if (step == null) return;
            b.Row(r =>
            {
                r.Column(c => c.Caption(StepText()).Text(IsDone ? DoneText(step) : step.hint, true), 1f, 6f);
                if (response && MayFinish) r.Button("done", "I am done", Done, DemoStyle.Primary, DemoIcon.Check, 0f, ButtonHeight);
                if (connector.Offline && IsParticipant)
                    r.Button("pause", "Pause", () => session.SetPaused(true), DemoStyle.Quiet, DemoIcon.Pause, 0f, ButtonHeight);
            }, 16f);
            if (response && step.showTimer && IsParticipant) b.Progress("timer", "Time left", 1f, " ").Out(out timer);
        }

        private string StepText()
        {
            string text = "Step " + (session.Step + 1) + " of " + session.StepCount;
            return IsParticipant && roster.SeatedCount < config.roles.Count ? text + ". Your partner left the room." : text;
        }

        private string DoneText(StudyStep step)
        {
            string raise = step.HasCue("raise-hand") ? "Raise your left hand. " : string.Empty;
            return roster.SeatedCount > 1 ? raise + "Waiting for your partner." : raise + "Done.";
        }

        private void BuildPaused(DemoBuilder b)
        {
            b.Row(r =>
            {
                r.Column(c => c.Text("Paused", true).Caption(connector.Offline ? "The session waits for you." : "The operator paused the session."), 1f, 6f);
                if (connector.Offline) r.Button("pause", "Continue", () => session.SetPaused(false), DemoStyle.Primary, DemoIcon.Play, 0f, ButtonHeight);
            });
        }

        private void BuildFinished(DemoBuilder b)
        {
            b.Caption("Session finished. " + config.finishNote);
            if (!IsParticipant) return;
            b.Row(r =>
            {
                r.Button("return", "Return to menu", app.Leave, DemoStyle.Primary, DemoIcon.None, 0f, ButtonHeight)
                    .Button("again", "Run it again", app.RunAgain, DemoStyle.Secondary, DemoIcon.None, 0f, ButtonHeight)
                    .Button("stay", "Stay and play", app.StayAndPlay, DemoStyle.Secondary, DemoIcon.None, 0f, ButtonHeight);
                if (AnswerStore.HasAnswers) r.Button("delete-answers", "Delete my answers", app.DeleteAnswers, DemoStyle.Quiet, DemoIcon.None, 0f, ButtonHeight);
            }, 14f);
        }

        // Changing values -------------------------------------------------------------------------------------

        private void Tick()
        {
            StepView view = director.View;
            int second = Mathf.CeilToInt(view.remaining);
            if (second == shownSecond) return;
            shownSecond = second;
            if (countdown != null) countdown.Text = "Starts in " + Mathf.Max(1, second) + ".";
            if (timer == null) return;
            timer.Set(view.window > 0f ? view.remaining / view.window : 0f, PanelParts.Minutes(view.remaining));
            timer.Color = view.remaining <= 0f ? DemoUi.Theme.danger : DemoUi.Theme.accent;
        }

        private void Done()
        {
            if (MayFinish) roster.Local.SetDone(session.Mark(session.Step));
        }
    }
}
