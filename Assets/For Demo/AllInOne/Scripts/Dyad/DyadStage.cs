using System.Collections.Generic;
using DemoKit;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>The two wall boards and the private task panels of the dyad session.</summary>
    public sealed class DyadStage : StudyStage
    {
        private const float BoardStandDistance = 1.1f;

        [SerializeField] private AppConfig config;
        [SerializeField] private SessionState session;
        [SerializeField] private Roster roster;
        [SerializeField] private DyadTaskFlow flow;
        [SerializeField] private GameObject[] boards;
        [SerializeField] private GameObject[] standpoints;

        private readonly RaiseHandDetector raisedHand = new RaiseHandDetector();
        private int boardsFrom, standpointsFrom, splitFrom, openMark = -1;

        public DyadTaskFlow Flow => flow;

        /// <summary>True once the user raised the left hand after the questions.</summary>
        public bool HandWasRaised { get; private set; }

        public override bool UsesThumbstick => flow.ActivePanel != null;

        public override bool TaskOpen => flow.IsOpen && flow.Shown != DyadTaskFlow.Page.Final;

        private void Awake()
        {
            boardsFrom = FirstWith("boards");
            standpointsFrom = FirstWith("standpoint");
            splitFrom = FirstWith("split");
            Clear();
        }

        private void OnEnable()
        {
            flow.Completed += OnCompleted;
            flow.Answered += OnAnswered;
        }

        private void OnDisable()
        {
            flow.Completed -= OnCompleted;
            flow.Answered -= OnAnswered;
        }

        public override void Apply(StepView view)
        {
            flow.Halted = session.Paused;
            Show(boards, view.step >= boardsFrom);
            Show(standpoints, view.step >= standpointsFrom);
            int role = roster.Local != null ? roster.RoleOf(roster.Local) : -1;
            if (role < 0 || view.step < 0) CloseTask();
            else RunTask(view, config.roles.Get(role).seatYaw);
            ListenForHand(view);
        }

        public override void Clear()
        {
            Show(boards, false);
            Show(standpoints, false);
            CloseTask();
        }

        public override Vector3 BoardFor(Vector3 userHead, out float yaw)
        {
            Transform best = boards[0].transform;
            foreach (GameObject board in boards)
                if (Flat(board.transform.position, userHead) > Flat(best.position, userHead)) best = board.transform;
            // The readable side of a board looks against its forward axis.
            Vector3 stand = best.position - best.forward * BoardStandDistance;
            yaw = best.eulerAngles.y;
            return new Vector3(stand.x, 0f, stand.z);
        }

        public override void CollectVisible(List<string> names)
        {
            foreach (GameObject board in boards)
                if (board.activeInHierarchy) names.Add(board.name);
            foreach (GameObject standpoint in standpoints)
                if (standpoint.activeInHierarchy) names.Add(standpoint.transform.parent.name + " " + standpoint.name);
            if (flow.IsOpen) names.Add(flow.Shown.ToString());
        }

        private void RunTask(StepView view, float yaw)
        {
            StudyStep step = config.script.Get(view.step);
            if (view.phase == StepPhase.Finished)
            {
                if (flow.Shown != DyadTaskFlow.Page.Final) flow.ShowFinal(yaw);
                return;
            }
            bool ratings = step.HasCue("ratings");
            bool split = view.step >= splitFrom;
            if (!ratings && !split)
            {
                CloseTask();
                return;
            }
            int mark = session.Mark(view.step);
            if (openMark == mark) return;
            openMark = mark;
            HandWasRaised = false;
            raisedHand.Reset();
            flow.Open(ratings ? DyadTaskFlow.Task.Ratings : DyadTaskFlow.Task.Split, yaw);
        }

        private void CloseTask()
        {
            openMark = -1;
            if (flow.IsOpen) flow.Close();
        }

        /// <summary>The clip asks for a raised hand. It is answered, but the answers decide when the step ends.</summary>
        private void ListenForHand(StepView view)
        {
            StudyStep step = config.script.Get(view.step);
            if (HandWasRaised || step == null || !step.HasCue("raise-hand") || !flow.Done || roster.Local == null) return;
            raisedHand.Step(roster.Local.Body, Time.deltaTime);
            if (!raisedHand.Raised) return;
            HandWasRaised = true;
            DemoUi.Toast("Thank you.", DemoTone.Good);
        }

        private void OnCompleted(DyadTaskFlow.Task task)
        {
            if (roster.Local != null) roster.Local.SetDone(openMark);
        }

        private void OnAnswered(string answer, string value) => AnswerStore.Add(session.SessionId.ToString(), answer, value);

        private int FirstWith(string cue)
        {
            for (int i = 0; i < config.script.Count; i++)
                if (config.script.Get(i).HasCue(cue)) return i;
            return int.MaxValue;
        }

        private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).sqrMagnitude;

        private static void Show(GameObject[] objects, bool on)
        {
            foreach (GameObject o in objects)
                if (o.activeSelf != on) o.SetActive(on);
        }
    }
}
