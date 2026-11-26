using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Evaluates the running step on every client and, on the one client that has the authority, advances an automatic
    /// session.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class SessionDirector : MonoBehaviour
    {
        /// <summary>Short breath before the next step when everybody finished early.</summary>
        public const double EarlyPause = 0.6;

        [SerializeField] private AppConfig config;
        [SerializeField] private SessionState session;
        [SerializeField] private SessionClock clock;
        [SerializeField] private Roster roster;
        [SerializeField] private RoomConnector connector;

        private readonly AuthorityElection authority = new AuthorityElection();

        /// <summary>The running step, the same on every client.</summary>
        public StepView View { get; private set; } = new StepView { step = -1 };

        /// <summary>The running step, or null.</summary>
        public StudyStep Step => config.script.Get(View.step);

        /// <summary>True when this client advances the steps.</summary>
        public bool HasAuthority { get; private set; }

        private StudyScript Script => config.script;

        private bool Runs => session.Ready && clock.Valid && session.Experience == Experience.FullSession;

        private void OnEnable() => session.Changed += OnSession;

        private void OnDisable() => session.Changed -= OnSession;

        /// <summary>The view follows the session at once, so that nobody sees the new step number with the old step.</summary>
        private void OnSession(SessionChange change, bool local) => Evaluate();

        private void Evaluate() => View = Runs
            ? StepTimeline.Evaluate(Script, session.Pace, session.Step, session.StepStartTime, clock.Now, session.Paused, session.PauseTime)
            : new StepView { step = -1 };

        private void Update()
        {
            Evaluate();
            if (!Runs)
            {
                HasAuthority = false;
                authority.Reset();
                return;
            }
            bool mayAct = authority.MayAct(roster.ElectedId, connector.ClientId, clock.Now);
            // In an offline room there is nobody else, so the local client always acts.
            HasAuthority = connector.Offline || mayAct;
            if (HasAuthority && session.Director == Director.Automatic && !session.Paused) Direct();
        }

        private void Direct()
        {
            bool complete = roster.SeatedCount >= config.roles.Count;
            switch (View.phase)
            {
                case StepPhase.Waiting:
                    if (complete) session.ScheduleFirstStep(clock.Now + Script.Countdown(session.Pace));
                    return;
                case StepPhase.Countdown when !complete:
                    session.ScheduleFirstStep(0.0);
                    return;
            }
            bool allDone = View.step >= 0 && roster.AllDone(session.Mark(View.step));
            if (!StepTimeline.ShouldAdvance(View, Script, session.Pace, allDone)) return;
            session.AdvanceFrom(View.step, NextStart());
        }

        private double NextStart()
        {
            if (View.phase == StepPhase.Countdown) return session.StepStartTime;
            return View.phase == StepPhase.Response ? clock.Now + EarlyPause : clock.Now;
        }
    }
}
