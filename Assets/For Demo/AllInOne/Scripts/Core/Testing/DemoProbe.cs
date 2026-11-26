using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>One person as this client sees them.</summary>
    [Serializable]
    public sealed class ProbePerson
    {
        public int id, role, avatar, done, connection;
        public string kind, character;
        public bool local, bot, inRoom, visible, muted;
        public Vector3 root, head, rightHand, rightHandShown;
    }

    /// <summary>The state of the app on this client, for automated tests.</summary>
    [Serializable]
    public sealed class ProbeSnapshot
    {
        public float seconds;
        public string app, connection, phase;
        public bool offline, hasAuthority, paused;
        public int clientId, experience, director, pace, step, sessionId, repeatCount, topology, handMode, positionMode, errors;
        public double now, stepStart;
        public float stepTime, remaining, handRatio, positionRatio;
        public bool linkOn, narration;
        public string caption, lastError;
        public Vector3 view, viewOffset;
        public string[] visible;
        public ProbePerson[] people;

        // Cost of a frame. The counts are 0 where the player does not deliver them.
        public float frameMilliseconds;
        public long drawCalls, setPassCalls, triangles;
    }

    /// <summary>Reads the state of the app and counts errors. Nothing here changes the app.</summary>
    public sealed class DemoProbe : MonoBehaviour
    {
        [SerializeField] private AllInOneApp app;
        [SerializeField] private RoomConnector connector;
        [SerializeField] private SessionState session;
        [SerializeField] private SessionDirector director;
        [SerializeField] private SessionClock clock;
        [SerializeField] private Roster roster;
        [SerializeField] private StudyStage stage;
        [SerializeField] private Narrator narrator;
        [SerializeField] private ViewRig view;

        private readonly List<string> names = new List<string>();
        private ProfilerRecorder drawCalls, setPassCalls, triangles;
        private float frameSeconds;

        public int Errors { get; private set; }

        public string LastError { get; private set; } = string.Empty;

        private void OnEnable()
        {
            Application.logMessageReceived += OnLog;
            drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLog;
            drawCalls.Dispose();
            setPassCalls.Dispose();
            triangles.Dispose();
        }

        private void Update() => frameSeconds = Mathf.Lerp(frameSeconds, Time.unscaledDeltaTime, 0.05f);

        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            Errors++;
            LastError = message;
        }

        public ProbeSnapshot Snapshot()
        {
            StepView v = director.View;
            names.Clear();
            stage.CollectVisible(names);
            var people = new List<ProbePerson>();
            foreach (IParticipant p in roster.People) people.Add(Person(p));
            return new ProbeSnapshot
            {
                seconds = Time.realtimeSinceStartup,
                app = app.State.ToString(),
                connection = connector.State.ToString(),
                offline = connector.Offline,
                clientId = connector.ClientId,
                hasAuthority = director.HasAuthority,
                experience = (int)session.Experience,
                director = (int)session.Director,
                pace = (int)session.Pace,
                step = session.Step,
                stepStart = session.StepStartTime,
                sessionId = session.SessionId,
                repeatCount = session.RepeatCount,
                paused = session.Paused,
                now = clock.Now,
                phase = v.phase.ToString(),
                stepTime = (float)v.time,
                remaining = v.remaining,
                linkOn = session.LinkOn,
                handMode = session.HandMode,
                handRatio = session.HandRatio,
                positionMode = session.PositionMode,
                positionRatio = session.PositionRatio,
                topology = session.Topology,
                narration = narrator.IsPlaying,
                caption = narrator.Captions != null ? narrator.Captions.Text : string.Empty,
                view = view.Camera.transform.position,
                viewOffset = view.Offset,
                visible = names.ToArray(),
                people = people.ToArray(),
                errors = Errors,
                lastError = LastError,
                frameMilliseconds = frameSeconds * 1000f,
                drawCalls = drawCalls.Valid ? drawCalls.LastValue : 0L,
                setPassCalls = setPassCalls.Valid ? setPassCalls.LastValue : 0L,
                triangles = triangles.Valid ? triangles.LastValue : 0L
            };
        }

        private ProbePerson Person(IParticipant p)
        {
            AvatarBody body = p.Body;
            return new ProbePerson
            {
                id = p.Id,
                role = roster.RoleOf(p),
                avatar = p.AvatarId,
                done = p.DoneMark,
                connection = p.Connection,
                kind = p.Kind.ToString(),
                character = body.dresser.Current != null ? body.dresser.Current.name : string.Empty,
                local = p.IsLocal,
                bot = p.IsBot,
                inRoom = p.InRoom,
                visible = body.Visible,
                muted = p.Muted,
                root = body.transform.position,
                head = body.headOut.position,
                rightHand = body.rightHand.position,
                rightHandShown = body.rightOut.position
            };
        }
    }
}
