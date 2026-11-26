using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DemoKit;
using Normal.Realtime;
using UnityEngine;

namespace SocialLeading.AllInOne
{
    /// <summary>
    /// Drives a player from the command line, for tests with several processes. Without arguments it does nothing.
    /// Example: -desktop -aio-auto online -aio-room 9 -aio-role leader -aio-avatar 2 -aio-exp full -aio-pace quick -aio-mute
    /// -aio-exit-after 60 -aio-report C:\a.json -aio-do "20:avatar 4;40:leave;45:join"
    /// With -aio-headset-quality a PC uses the quality and the mirror of a standalone headset.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class DemoCommandLine : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public List<ProbeSnapshot> samples = new List<ProbeSnapshot>();
        }

        private sealed class Command
        {
            public float at;
            public string[] words;
        }

        private const float SampleInterval = 0.5f;

        [SerializeField] private AppConfig config;
        [SerializeField] private AllInOneApp app;
        [SerializeField] private RoomConnector connector;
        [SerializeField] private SessionState session;
        [SerializeField] private SessionClock clock;
        [SerializeField] private RigMode mode;
        [SerializeField] private ViewRig view;
        [SerializeField] private DesktopRig desktop;
        [SerializeField] private MirrorGate mirror;
        [SerializeField] private RoomMenu menu;
        [SerializeField] private DemoProbe probe;

        private readonly Report report = new Report();
        private readonly List<Command> commands = new List<Command>();
        private string[] arguments;
        private string reportPath, auto;
        private float exitAfter = -1f, nextSample;

        private void Awake()
        {
            arguments = Environment.GetCommandLineArgs();
            if (Has("-desktop")) mode.ForceDesktop = true;
            if (Has("-aio-headset-quality")) QualityTuner.Standalone = true;
            auto = Value("-aio-auto");
            if (string.IsNullOrEmpty(auto))
            {
                enabled = false;
                return;
            }
            mode.ForceDesktop = true;
            Application.runInBackground = true;
            ReadChoices();
            reportPath = Value("-aio-report");
            exitAfter = Number("-aio-exit-after", -1f);
            clock.TestSpeed = Number("-aio-speed", 1f);
            foreach (string part in (Value("-aio-do") ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int colon = part.IndexOf(':');
                if (colon <= 0) continue;
                commands.Add(new Command { at = Parse(part.Substring(0, colon), 0f), words = part.Substring(colon + 1).Trim().Split(' ') });
            }
        }

        private void ReadChoices()
        {
            LocalChoices choices = connector.Choices;
            string role = Value("-aio-role");
            choices.kind = role == "operator" ? ParticipantKind.Operator : ParticipantKind.Participant;
            choices.role = RoleIndex(role);
            choices.avatar = Mathf.RoundToInt(Number("-aio-avatar", 1f)) - 1;
            choices.room = Mathf.RoundToInt(Number("-aio-room", 1f));
            choices.experience = Value("-aio-exp") == "full" ? Experience.FullSession : Experience.FreePlay;
            choices.director = Value("-aio-director") == "operator" ? Director.Operator : Director.Automatic;
            choices.pace = Value("-aio-pace") == "quick" ? Pace.Quick : Pace.Study;
            choices.microphone = !Has("-aio-mute");
            choices.muted = !choices.microphone;
        }

        private IEnumerator Start()
        {
            if (!enabled) yield break;
            while (app.State != AppState.Menu) yield return null;
            yield return new WaitForSecondsRealtime(0.5f);
            Join();
            StartCoroutine(RunCommands());
        }

        private void Join()
        {
            app.Join(auto == "solo", connector.Choices.kind);
            StartCoroutine(EnterWhenPossible());
        }

        private IEnumerator EnterWhenPossible()
        {
            float until = Time.realtimeSinceStartup + 25f;
            while (Time.realtimeSinceStartup < until && app.State != AppState.Lobby && app.State != AppState.InRoom) yield return null;
            // A moment in the lobby, so that the other players are known before the role is taken.
            yield return new WaitForSecondsRealtime(Number("-aio-lobby", 1f));
            app.Enter();
        }

        private IEnumerator RunCommands()
        {
            commands.Sort((a, b) => a.at.CompareTo(b.at));
            foreach (Command command in commands)
            {
                while (Time.realtimeSinceStartup < command.at) yield return null;
                Run(command.words);
            }
        }

        private void Run(string[] words)
        {
            string argument = words.Length > 1 ? words[1] : string.Empty;
            switch (words[0])
            {
                case "avatar": app.ChooseAvatar(Mathf.RoundToInt(Parse(argument, 1f)) - 1); break;
                case "role": app.ChangeRole(RoleIndex(argument)); break;
                case "leave": app.Leave(); break;
                case "join": Join(); break;
                case "menu": menu.OpenMenu(); break;
                case "click": DemoUi.Click(argument); break;
                case "turn": if (desktop.Body != null) desktop.Body.Yaw = Parse(argument, 0f); break;
                case "motion": if (desktop.Body != null) desktop.Body.CurrentMotion = (DemoDesktopRig.Motion)Mathf.RoundToInt(Parse(argument, 0f)); break;
                case "shot":
                    mirror.RenderNow();
                    ViewCapture.Save(view.Camera, 1920, 1080, argument);
                    break;
                case "drop": connector.GetComponent<Realtime>().Disconnect(); break;
                case "window": ScreenCapture.CaptureScreenshot(argument); break;
                case "quit": Quit(); break;
            }
        }

        private void Update()
        {
            float now = Time.realtimeSinceStartup;
            if (now >= nextSample)
            {
                nextSample = now + SampleInterval;
                report.samples.Add(probe.Snapshot());
                // Written again and again, so that a process that is killed leaves its report.
                if (report.samples.Count % 10 == 0) WriteReport();
            }
            if (exitAfter > 0f && now >= exitAfter) Quit();
        }

        private void Quit()
        {
            WriteReport();
            Application.Quit();
        }

        private void WriteReport()
        {
            if (string.IsNullOrEmpty(reportPath)) return;
            try
            {
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            }
            catch (IOException e)
            {
                Debug.LogWarning("All-in-One: the report could not be written: " + e.Message);
            }
        }

        private int RoleIndex(string id)
        {
            int index = Array.FindIndex(config.roles.roles, r => r.id == id);
            return index >= 0 ? index : LocalChoices.RandomRole;
        }

        private bool Has(string name) => Array.IndexOf(arguments, name) >= 0;

        private string Value(string name)
        {
            int at = Array.IndexOf(arguments, name);
            return at >= 0 && at + 1 < arguments.Length ? arguments[at + 1] : null;
        }

        private float Number(string name, float fallback) => Parse(Value(name), fallback);

        private static float Parse(string text, float fallback) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) ? number : fallback;
    }
}
