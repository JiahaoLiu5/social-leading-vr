using System.Text;
using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// The control panel of an operator: who is here, where the session is, and the buttons that move it on. "Next" is
    /// refused while a clip plays, as in the study.
    /// </summary>
    public sealed class OperatorPanel : MonoBehaviour
    {
        private const float Width = 1180f;

        [SerializeField] private AppConfig config;
        [SerializeField] private AllInOneApp app;
        [SerializeField] private SessionState session;
        [SerializeField] private SessionDirector director;
        [SerializeField] private SessionClock clock;
        [SerializeField] private RoomConnector connector;
        [SerializeField] private Roster roster;
        [SerializeField] private Narrator narrator;
        [SerializeField] private ViewRig view;
        [SerializeField] private RigMode mode;
        [SerializeField] private AppInput input;

        private readonly ChoiceGroup paces = new ChoiceGroup();
        private readonly StringBuilder text = new StringBuilder();

        private DemoPanel panel;
        private DemoLabel steps, people, now;
        private DemoProgressBar progress;
        private DemoButton start, next, repeat, pause, end, take, auto, hand;
        private DemoSwitch talk, mute;
        private bool active, dirty, onHand;
        private int shownSecond = -1;
        private StepPhase shownPhase;

        public bool OnLeftHand => panel.IsHeld();

        public void Begin()
        {
            if (panel == null) Build();
            active = true;
            dirty = true;
            onHand = false;
            Place();
            roster.Changed += MarkDirty;
            session.Changed += OnSession;
        }

        public void End()
        {
            if (!active) return;
            active = false;
            roster.Changed -= MarkDirty;
            session.Changed -= OnSession;
            // The next time this user may take part, and needs to hear the narration.
            narrator.Muted = false;
            mute.SetSilently(false);
            panel.Hide();
        }

        private void OnDestroy()
        {
            roster.Changed -= MarkDirty;
            session.Changed -= OnSession;
        }

        private void MarkDirty() => dirty = true;

        private void OnSession(SessionChange change, bool local) => dirty = true;

        // Building --------------------------------------------------------------------------------------------

        private void Build()
        {
            panel = DemoUi.Panel("op", Width, 0f, 44f)
                .Heading("Operator")
                .Row(r => r
                    .Column(Left, 0.8f, 14f)
                    .Space(36f)
                    .Column(Right, 1f, 14f), 0f, TextAnchor.UpperLeft);
            paces.Chosen += i => session.SetPace((Pace)i);
        }

        private void Left(DemoBuilder b)
        {
            b.Text("People", true)
                .Label("people", " ").Out(out people)
                .Divider()
                .Text("Steps", true)
                .Label("steps", StepList(-1, true)).Out(out steps);
        }

        private void Right(DemoBuilder b)
        {
            b.Label("now", " ", DemoTextStyle.Strong).Out(out now)
                .Progress("time", "Time in this step", 0f, "0:00").Out(out progress)
                .Primary("next", "Next step", Next, DemoIcon.Right).Out(out next)
                .Row(r => r
                    .Button("start", "Start", StartSession, DemoStyle.Secondary, DemoIcon.Play).Out(out start)
                    .Flex()
                    .Button("end", "End", session.Finish).Out(out end))
                .Row(r => r
                    .Button("repeat", "Repeat", Repeat).Out(out repeat)
                    .Flex()
                    .Button("pause", "Pause", TogglePause, DemoStyle.Secondary, DemoIcon.Pause).Out(out pause))
                .Options(paces, null, 0f, ("pace-study", "Study timing"), ("pace-quick", "Quick preview"))
                .Row(r => r
                    .Button("take", "Take control", () => session.SetDirector(Director.Operator)).Out(out take)
                    .Flex()
                    .Button("auto", "Automatic", () => session.SetDirector(Director.Automatic)).Out(out auto))
                .Switch("talk", "Talk to the room", false, on => { if (roster.Local != null) roster.Local.SetMuted(!on); }).Out(out talk)
                .Switch("mute-narration", "Mute narration", false, on => narrator.Muted = on).Out(out mute)
                .Row(r => r
                    .Button("leave", "Leave", app.Leave, DemoStyle.Quiet, DemoIcon.Left)
                    .Flex()
                    .Button("hand", "Move to hand", ToggleHand).Out(out hand));
        }

        private void Place()
        {
            hand.gameObject.SetActive(!mode.Desktop);
            hand.Label = onHand ? "Put back" : "Move to hand";
            if (onHand)
            {
                panel.Scale(0.45f);
                panel.ShowAttached(view.LeftPointer, new Vector3(0.06f, 0.18f, 0.30f), Quaternion.Euler(22f, -8f, 0f), DemoHand.Left);
                return;
            }
            if (mode.Desktop)
            {
                // Parallel to the picture, so that it looks straight although it is at the side.
                panel.Scale(0.53f);
                panel.ShowAt(config.operatorDesktopPanelPosition, Quaternion.Euler(0f, config.operatorDesktopYaw, 0f));
                return;
            }
            panel.Scale(0.6f);
            panel.ShowFacing(config.operatorPanelPosition, DemoUi.Head.position);
        }

        private void ToggleHand()
        {
            onHand = !onHand;
            Place();
        }

        // Commands --------------------------------------------------------------------------------------------

        private bool Speaking => director.View.phase == StepPhase.Narration && !session.Paused;

        /// <summary>The Space key of the original monitor.</summary>
        public void Next()
        {
            if (!session.MayOperate(roster.Local) || session.Finished) return;
            if (Speaking)
            {
                DemoUi.Toast("Wait until the narration has ended.", DemoTone.Warning);
                return;
            }
            session.AdvanceFrom(session.Step, clock.Now + SessionDirector.EarlyPause);
        }

        private void StartSession()
        {
            if (session.Waiting && session.Director == Director.Operator)
            {
                Next();
                return;
            }
            session.Begin(Experience.FullSession, Director.Operator, (Pace)Mathf.Max(0, paces.Index));
            session.AdvanceFrom(-1, clock.Now + SessionDirector.EarlyPause);
        }

        private void Repeat()
        {
            if (session.MayOperate(roster.Local)) session.Repeat(clock.Now + SessionDirector.EarlyPause);
        }

        private void TogglePause()
        {
            if (session.MayOperate(roster.Local)) session.SetPaused(!session.Paused);
        }

        private void Update()
        {
            if (!active || roster.Local == null) return;
            if (input.OperatorNextPressed) Next();
            if (input.OperatorRepeatPressed) Repeat();
            if (input.OperatorPausePressed) TogglePause();
            if (shownPhase != director.View.phase) dirty = true;
            if (dirty) Refresh();
            Tick();
        }

        // State -----------------------------------------------------------------------------------------------

        private void Refresh()
        {
            dirty = false;
            shownPhase = director.View.phase;
            bool full = session.Experience == Experience.FullSession;
            bool operating = session.MayOperate(roster.Local);
            people.Text = People();
            steps.Text = StepList(session.Step, full);
            now.Text = NowText();
            next.Interactable = operating && !session.Finished && !Speaking;
            next.Label = session.Step < 0 ? "Start step 1" : session.Step == session.StepCount - 1 ? "Finish" : "Next step";
            start.Interactable = !session.Running;
            end.Interactable = operating && session.Running;
            repeat.Interactable = operating && session.Running;
            pause.Interactable = operating && session.Running;
            pause.Label = session.Paused ? "Continue" : "Pause";
            pause.Icon = session.Paused ? DemoIcon.Play : DemoIcon.Pause;
            paces.Index = (int)session.Pace;
            paces.SetEnabled(session.MayChangePace(roster.Local));
            take.Interactable = full && session.Director == Director.Automatic;
            auto.Interactable = full && session.Director == Director.Operator;
            talk.SetSilently(!roster.Local.Muted);
            talk.Interactable = !connector.Offline;
            panel.Layout();
        }

        private void Tick()
        {
            StepView v = director.View;
            int second = Mathf.FloorToInt((float)v.time);
            if (second == shownSecond) return;
            shownSecond = second;
            float length = config.script.Length(v.step) + v.window;
            progress.Set(length > 0f ? Mathf.Clamp01((float)(v.time / length)) : 0f, PanelParts.Minutes(second));
            progress.Color = length > 0f && v.time > length ? DemoUi.Theme.danger : DemoUi.Theme.accent;
        }

        private string NowText()
        {
            switch (session.Experience)
            {
                case Experience.None: return "Nothing runs yet.";
                case Experience.FreePlay: return "Free play";
            }
            string suffix = session.Director == Director.Automatic ? " (automatic)" : string.Empty;
            if (session.Waiting) return "Waiting to start" + suffix;
            if (session.Finished) return "Finished" + suffix;
            string phase = session.Paused ? "paused" : Speaking ? "narration" : "users act";
            return "Step " + (session.Step + 1) + ", " + phase + suffix;
        }

        private string People()
        {
            text.Clear();
            for (int role = 0; role < config.roles.Count; role++)
            {
                IParticipant p = roster.ByRole(role);
                if (role > 0) text.Append('\n');
                text.Append(config.roles.Label(role)).Append(": ");
                if (p == null)
                {
                    text.Append("nobody");
                    continue;
                }
                text.Append(p.IsBot ? "simulated, " : "person " + (p.Id + 1) + ", ").Append(config.avatars.Label(p.AvatarId));
                if (session.Running && p.DoneMark == session.Mark(session.Step)) text.Append(", done");
            }
            foreach (IParticipant p in roster.People)
                if (!p.InRoom) text.Append("\nPerson ").Append(p.Id + 1).Append(": in the lobby");
            return text.ToString();
        }

        private string StepList(int current, bool full)
        {
            text.Clear();
            string accent = ColorUtility.ToHtmlStringRGB(DemoUi.Theme.accent);
            for (int i = 0; i < config.script.Count; i++)
            {
                if (i > 0) text.Append('\n');
                string line = (i + 1) + ". " + config.script.Get(i).title;
                if (full && i == current) text.Append("<b><color=#").Append(accent).Append('>').Append(line).Append("</color></b>");
                else text.Append(line);
            }
            return text.ToString();
        }
    }
}
