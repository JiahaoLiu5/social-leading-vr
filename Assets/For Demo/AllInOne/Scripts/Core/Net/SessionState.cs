using System;
using Normal.Realtime;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>What changed in the session.</summary>
    public enum SessionChange { Started, Step, Paused, Director, Pace, Link }

    /// <summary>
    /// The session of the room: what is run, who directs it, which step runs since when, and the movement link of free play.
    /// Every client reads the same model. All rules about who may change what are in this class.
    /// </summary>
    public sealed class SessionState : RealtimeComponent<SessionModel>
    {
        [SerializeField] private AppConfig config;
        [SerializeField] private Roster roster;
        [SerializeField] private SessionClock clock;

        private bool writing;

        /// <summary>Raised after a change. The flag is true when this client made it.</summary>
        public event Action<SessionChange, bool> Changed;

        /// <summary>
        /// False outside a room. Nothing may be written then: a fresh model would be carried into the next empty room.
        /// </summary>
        public bool Ready => model != null && realtime != null && realtime.connected;

        public Experience Experience => Ready ? (Experience)model.experience : Experience.None;
        public Director Director => Ready ? (Director)model.director : Director.Automatic;
        public Pace Pace => Ready ? (Pace)model.pace : Pace.Study;

        /// <summary>-1 before the first step, the number of steps after the last one.</summary>
        public int Step => Ready ? model.step : -1;

        /// <summary>Session time of the step. Before the first step: its scheduled time, or 0.</summary>
        public double StepStartTime => Ready ? model.stepStartTime : 0.0;

        public bool Paused => Ready && model.paused;
        public double PauseTime => Ready ? model.pauseTime : 0.0;
        public int Seed => Ready ? model.seed : 0;
        public int SessionId => Ready ? model.sessionId : 0;
        public int RepeatCount => Ready ? model.repeatCount : 0;
        public bool LinkOn => !Ready || model.linkOn;
        public int HandMode => Ready ? model.handMode : 0;
        public float HandRatio => Ready ? model.handRatio : 0f;
        public float Smoothing => Ready ? model.smoothing : 0f;
        public int PositionMode => Ready ? model.positionMode : 0;
        public float PositionRatio => Ready ? model.positionRatio : 0f;
        public int Topology => Ready ? model.topology : 0;

        public int StepCount => config.script.Count;
        public bool Waiting => Experience == Experience.FullSession && Step < 0;
        public bool Running => Experience == Experience.FullSession && Step >= 0 && Step < StepCount;
        public bool Finished => Experience == Experience.FullSession && Step >= StepCount;

        /// <summary>True when the next person who enters decides what the room runs.</summary>
        public bool Idle => Ready && (Experience == Experience.None || !roster.SomebodyElseInRoom);

        /// <summary>
        /// The value that marks a step as finished. It changes with every start and repeat, so that an old mark never
        /// counts for a new run.
        /// </summary>
        public int Mark(int step) => SessionId * 100000 + RepeatCount * 100 + step + 1;

        // Rights ----------------------------------------------------------------------------------------------

        private static bool IsActive(IParticipant who) => who != null && who.InRoom && who.Kind != ParticipantKind.Spectator;

        private static bool IsOperator(IParticipant who) => IsActive(who) && who.Kind == ParticipantKind.Operator;

        /// <summary>Participants in free play, while waiting, and after the end. Operators always.</summary>
        public bool MayChangeExperience(IParticipant who) => Ready && IsActive(who) && (!Running || IsOperator(who));

        /// <summary>Anybody before the first step. Later an operator, or a participant while no operator is in the room.</summary>
        public bool MayChangeDirector(IParticipant who) =>
            Ready && IsActive(who) && (!Running || IsOperator(who) || !roster.OperatorPresent);

        public bool MayChangePace(IParticipant who) => Ready && IsActive(who) && !Running;

        public bool MayChangeLink(IParticipant who) => Ready && IsActive(who) && Experience == Experience.FreePlay;

        public bool MayChangeRole(IParticipant who) => Ready && who != null && (!who.InRoom || Experience == Experience.FreePlay);

        public bool MayChangeAvatar(IParticipant who) => Ready && who != null && (!who.InRoom || Experience == Experience.FreePlay || Waiting);

        public bool MayOperate(IParticipant who) => Ready && IsOperator(who) && Experience == Experience.FullSession && Director == Director.Operator;

        // Commands --------------------------------------------------------------------------------------------

        /// <summary>Starts an experience from the beginning for everybody in the room.</summary>
        public void Begin(Experience experience, Director director, Pace pace)
        {
            if (!Ready) return;
            writing = true;
            model.experience = (int)experience;
            model.director = (int)director;
            model.pace = (int)pace;
            model.seed = UnityEngine.Random.Range(0, 1 << 20);
            model.step = -1;
            model.stepStartTime = 0.0;
            model.paused = false;
            model.repeatCount = 0;
            if (experience == Experience.FreePlay) WriteLink(config.playProfile, 0);
            model.sessionId++;
            writing = false;
        }

        /// <summary>Sets the time of the first step. 0 takes it back.</summary>
        public void ScheduleFirstStep(double time)
        {
            if (!Ready || model.step >= 0) return;
            writing = true;
            model.stepStartTime = time;
            writing = false;
        }

        /// <summary>
        /// Leaves a step. Does nothing when somebody else already did. The start may lie in the future.
        /// </summary>
        public void AdvanceFrom(int step, double startOfNext)
        {
            if (!Ready || model.step != step || step >= StepCount) return;
            writing = true;
            model.repeatCount = 0;
            model.paused = false;
            model.stepStartTime = startOfNext;
            model.step = step + 1;
            writing = false;
        }

        public void Repeat(double startAgain)
        {
            if (!Running) return;
            writing = true;
            model.repeatCount++;
            model.paused = false;
            model.stepStartTime = startAgain;
            writing = false;
        }

        public void SetPaused(bool paused)
        {
            if (!Running || model.paused == paused) return;
            writing = true;
            if (paused) model.pauseTime = clock.Now;
            else model.stepStartTime += clock.Now - model.pauseTime;
            model.paused = paused;
            writing = false;
        }

        public void Finish()
        {
            if (!Ready || Experience != Experience.FullSession) return;
            writing = true;
            model.paused = false;
            model.repeatCount = 0;
            model.step = StepCount;
            writing = false;
        }

        /// <summary>Hands the session over. A pause of the operator ends with it, because nobody else could end it.</summary>
        public void SetDirector(Director director)
        {
            if (!Ready) return;
            if (director == Director.Automatic) SetPaused(false);
            writing = true;
            model.director = (int)director;
            writing = false;
        }

        public void SetPace(Pace pace)
        {
            if (!Ready) return;
            writing = true;
            model.pace = (int)pace;
            writing = false;
        }

        public void SetLinkOn(bool on) { if (!Ready) return; writing = true; model.linkOn = on; writing = false; }

        public void SetHandMode(int mode) { if (!Ready) return; writing = true; model.handMode = mode; writing = false; }

        public void SetHandRatio(float ratio) { if (!Ready) return; writing = true; model.handRatio = ratio; writing = false; }

        public void SetSmoothing(float seconds) { if (!Ready) return; writing = true; model.smoothing = seconds; writing = false; }

        public void SetPositionMode(int mode) { if (!Ready) return; writing = true; model.positionMode = mode; writing = false; }

        public void SetPositionRatio(float ratio) { if (!Ready) return; writing = true; model.positionRatio = ratio; writing = false; }

        public void SetTopology(int topology) { if (!Ready) return; writing = true; model.topology = topology; writing = false; }

        public void ResetLink()
        {
            if (!Ready) return;
            writing = true;
            WriteLink(config.playProfile, 0);
            writing = false;
        }

        private void WriteLink(ModulationProfile profile, int topology)
        {
            model.linkOn = true;
            model.handMode = (int)profile.hands;
            model.handRatio = profile.handRatio;
            model.smoothing = profile.partnerSmoothing;
            model.positionMode = (int)profile.position;
            model.positionRatio = profile.positionRatio;
            model.topology = topology;
        }

        // Model -----------------------------------------------------------------------------------------------

        protected override void OnRealtimeModelReplaced(SessionModel previousModel, SessionModel currentModel)
        {
            if (previousModel != null) Subscribe(previousModel, false);
            if (currentModel != null) Subscribe(currentModel, true);
        }

        private void Subscribe(SessionModel m, bool on)
        {
            m.sessionIdDidChange -= OnStarted;
            m.stepDidChange -= OnStep;
            m.repeatCountDidChange -= OnStep;
            m.stepStartTimeDidChange -= OnStepTime;
            m.pausedDidChange -= OnPaused;
            m.directorDidChange -= OnDirector;
            m.paceDidChange -= OnPace;
            m.linkOnDidChange -= OnLinkBool;
            m.handModeDidChange -= OnLinkInt;
            m.positionModeDidChange -= OnLinkInt;
            m.topologyDidChange -= OnLinkInt;
            m.handRatioDidChange -= OnLinkFloat;
            m.smoothingDidChange -= OnLinkFloat;
            m.positionRatioDidChange -= OnLinkFloat;
            if (!on) return;
            m.sessionIdDidChange += OnStarted;
            m.stepDidChange += OnStep;
            m.repeatCountDidChange += OnStep;
            m.stepStartTimeDidChange += OnStepTime;
            m.pausedDidChange += OnPaused;
            m.directorDidChange += OnDirector;
            m.paceDidChange += OnPace;
            m.linkOnDidChange += OnLinkBool;
            m.handModeDidChange += OnLinkInt;
            m.positionModeDidChange += OnLinkInt;
            m.topologyDidChange += OnLinkInt;
            m.handRatioDidChange += OnLinkFloat;
            m.smoothingDidChange += OnLinkFloat;
            m.positionRatioDidChange += OnLinkFloat;
        }

        private void OnStarted(SessionModel m, int value) => Raise(SessionChange.Started);
        private void OnStep(SessionModel m, int value) => Raise(SessionChange.Step);
        private void OnStepTime(SessionModel m, double value) => Raise(SessionChange.Step);
        private void OnPaused(SessionModel m, bool value) => Raise(SessionChange.Paused);
        private void OnDirector(SessionModel m, int value) => Raise(SessionChange.Director);
        private void OnPace(SessionModel m, int value) => Raise(SessionChange.Pace);
        private void OnLinkBool(SessionModel m, bool value) => Raise(SessionChange.Link);
        private void OnLinkInt(SessionModel m, int value) => Raise(SessionChange.Link);
        private void OnLinkFloat(SessionModel m, float value) => Raise(SessionChange.Link);

        private void Raise(SessionChange change) => Changed?.Invoke(change, writing);
    }
}
