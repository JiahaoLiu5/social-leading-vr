using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DemoKit;
using SocialLeading.AllInOne;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using SessionState = SocialLeading.AllInOne.SessionState;

/// <summary>
/// Plays the integrated demo without a person: every panel, solo sessions, the operator, leaving and coming back.
/// Batch mode: Unity -batchmode -projectPath P -executeMethod AllInOneSmokeTest.Run -logFile L (no -quit, no -nographics).
/// Exit code 0 = passed. Result and pictures: Logs/AllInOne (Unity empties Temp when it closes).
/// </summary>
[InitializeOnLoad]
public static partial class AllInOneSmokeTest
{
    private const string RunningKey = "AllInOne.Smoke";
    private const string PartsKey = "AllInOne.SmokeParts";
    private const string OutputFolder = "Logs/AllInOne";

    private static SmokeRunner runner;
    private static int lastFrame = -1;

    // The objects of the running scene.
    private static AllInOneApp app;
    private static AppConfig config;
    private static RoomConnector connector;
    private static SessionState session;
    private static SessionDirector director;
    private static SessionClock clock;
    private static Roster roster;
    private static DyadStage stage;
    private static SoloPartners solo;
    private static Narrator narrator;
    private static ViewRig view;
    private static Seating seating;
    private static DesktopRig desktop;
    private static DemoProbe probe;
    private static MirrorGate mirrorGate;
    private static Camera overview, mirror;

    static AllInOneSmokeTest()
    {
        if (UnityEditor.SessionState.GetBool(RunningKey, false)) EditorApplication.update += Tick;
    }

    [MenuItem("Social Leading/All-in-One/Run Smoke Test")]
    public static void Run() => Begin(OutputFolder, false);

    private static void Begin(string folder, bool headset)
    {
        // Some parts only, for work on the test itself: -aio-parts menu,session
        string[] arguments = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(arguments, "-aio-parts");
        UnityEditor.SessionState.SetString(PartsKey, at >= 0 && at + 1 < arguments.Length ? arguments[at + 1] : string.Empty);
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
        EditorSceneManager.OpenScene(AllInOneBuilder.ScenePath);
        UnityEditor.SessionState.SetBool(RunningKey, true);
        UnityEditor.SessionState.SetBool(HeadsetKey, headset);
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        if (runner == null)
        {
            Time.captureFramerate = Mathf.RoundToInt(1f / SmokeRunner.FrameSeconds);
            Application.runInBackground = true;
            ShaderUtil.allowAsyncCompilation = false;
            string folder = WithHeadset ? HeadsetFolder : OutputFolder;
            // The answers of a person on this computer stay untouched.
            AnswerStore.Folder = Directory.CreateDirectory(folder).FullName;
            runner = new SmokeRunner(folder, WithHeadset ? HeadsetScript() : Script());
        }
        SendDevices();
        runner.Step();
        if (runner.Done) Stop();
    }

    private static void Stop()
    {
        UnityEditor.SessionState.SetBool(RunningKey, false);
        EditorApplication.update -= Tick;
        RemoveHeadset();
        Time.captureFramerate = 0;
        AnswerStore.Folder = null;
        bool passed = runner.Passed;
        runner = null;
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.Exit(passed ? 0 : 1);
    }

    private static bool Wanted(string part)
    {
        string parts = UnityEditor.SessionState.GetString(PartsKey, string.Empty);
        return parts.Length == 0 || Array.IndexOf(parts.Split(','), part) >= 0;
    }

    private static IEnumerator Script()
    {
        FindObjects();
        if (Wanted("static")) StaticChecks();
        yield return WaitForMenu();
        if (Wanted("menu")) yield return MenuChecks();
        if (Wanted("session")) yield return SessionChecks();
        if (Wanted("play")) yield return FreePlayChecks();
        if (Wanted("operator")) yield return OperatorChecks();
        if (Wanted("late")) yield return LateJoinChecks();
        if (Wanted("problems")) yield return ProblemChecks();
    }

    private static IEnumerator HeadsetScript()
    {
        FindObjects();
        yield return WaitForMenu();
        yield return HeadsetChecks();
    }

    private static void FindObjects()
    {
        app = Object.FindObjectOfType<AllInOneApp>();
        connector = Object.FindObjectOfType<RoomConnector>();
        session = Object.FindObjectOfType<SessionState>();
        director = Object.FindObjectOfType<SessionDirector>();
        clock = Object.FindObjectOfType<SessionClock>();
        roster = Object.FindObjectOfType<Roster>();
        stage = Object.FindObjectOfType<DyadStage>();
        solo = Object.FindObjectOfType<SoloPartners>();
        narrator = Object.FindObjectOfType<Narrator>();
        view = Object.FindObjectOfType<ViewRig>();
        seating = Object.FindObjectOfType<Seating>();
        desktop = Object.FindObjectOfType<DesktopRig>();
        probe = Object.FindObjectOfType<DemoProbe>();
        config = AssetDatabase.LoadAssetAtPath<AppConfig>(AllInOneBuilder.Root + "Content/App Config.asset");
        mirrorGate = Object.FindObjectOfType<MirrorGate>();
        mirror = mirrorGate.GetComponentInChildren<Camera>(true);
        overview = new GameObject("Overview Camera").AddComponent<Camera>();
        overview.enabled = false;
        overview.fieldOfView = 62f;
        overview.nearClipPlane = 0.05f;
        PlaceOverview(new Vector3(2.75f, 2.35f, -3.1f), new Vector3(-0.4f, 0.95f, 0.2f));
    }

    // Helpers -------------------------------------------------------------------------------------------------

    private static void PlaceOverview(Vector3 from, Vector3 at)
    {
        overview.transform.position = from;
        overview.transform.LookAt(at);
    }

    private static IEnumerator WaitForMenu()
    {
        runner.Group("Start", 30f);
        yield return runner.Until(() => app.State == AppState.Menu && IsShown("start"), 6f, "the start panel appears");
        yield return runner.Wait(1f);
    }

    private static bool IsShown(string panel)
    {
        DemoPanel p = DemoUi.GetPanel(panel);
        return p != null && p.IsShown;
    }

    /// <summary>Clicks an element and records a failure when that is not possible.</summary>
    private static IEnumerator Click(string path)
    {
        runner.Check(DemoUi.Click(path), "click " + path);
        yield return null;
        yield return null;
    }

    private static void Shot(string name) => runner.Shot(view.Camera, name, mirror);

    /// <summary>Picture from the side, without the bars that belong to the view of one user.</summary>
    private static void Overview(string name)
    {
        foreach (string id in new[] { "hud", "captions.panel" }) ShowCanvas(id, false);
        runner.Shot(overview, name, mirror);
        foreach (string id in new[] { "hud", "captions.panel" }) ShowCanvas(id, true);
    }

    private static void ShowCanvas(string panel, bool on)
    {
        DemoPanel p = DemoUi.GetPanel(panel);
        if (p != null) p.Canvas.enabled = on;
    }

    private static void CheckLayout(string panel)
    {
        DemoPanel p = DemoUi.GetPanel(panel);
        if (!runner.Check(p != null, "panel " + panel + " exists")) return;
        List<string> bad = DemoUi.FindOverflow(p);
        runner.Check(bad.Count == 0, "layout of " + panel, string.Join("; ", bad));
    }

    private static string CharacterOf(AvatarBody body) => body.dresser.Current != null ? body.dresser.Current.name : string.Empty;

    private static IEnumerator LeaveToMenu()
    {
        app.Leave();
        yield return runner.Until(() => app.State == AppState.Menu && IsShown("start"), 5f, "back in the menu");
        yield return runner.Wait(0.5f);
    }

    /// <summary>Goes through the pages of the start panel with clicks only.</summary>
    private static IEnumerator JoinSolo(int avatar, string role, bool full, bool quick)
    {
        // The same simulated partner in every run.
        UnityEngine.Random.InitState(avatar);
        yield return GoToFirstPage();
        yield return Click("start/avatar-" + avatar);
        yield return Click("start/next");
        yield return Click("start/role-" + role);
        yield return Click("start/next");
        yield return Click(full ? "start/exp-full" : "start/exp-free");
        if (full)
        {
            yield return Click("start/director-auto");
            yield return Click(quick ? "start/pace-quick" : "start/pace-study");
        }
        yield return Click("start/next");
        yield return Click("start/join-solo");
        yield return runner.Until(() => app.State == AppState.InRoom, 5f, "in the room");
    }

    private static IEnumerator GoToFirstPage()
    {
        while (DemoUi.Get<DemoPages>("start/pages").Index > 0) yield return Click("start/back");
    }
}
