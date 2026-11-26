using System.Collections;
using System.Reflection;
using DemoKit;
using Normal.Realtime;
using SocialLeading.AllInOne;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Things that go wrong: a room that cannot be reached, a broken connection, an operator who disappears, hurried clicks, and
/// slow frames.
/// </summary>
public static partial class AllInOneSmokeTest
{
    private static IEnumerator ProblemChecks()
    {
        yield return UnreachableRoom();
        yield return BrokenConnection();
        yield return HurriedUser();
        yield return OperatorDisappears();
        yield return SlowFrames();
        yield return LobbyLayout();
        yield return SceneReload();
    }

    /// <summary>A server that does not exist. The host name is reserved and never resolves.</summary>
    private static IEnumerator UnreachableRoom()
    {
        runner.Group("Room that cannot be reached", 240f);
        runner.IgnoreErrors = true;
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            typeof(AllInOneApp).GetProperty("State").SetValue(app, AppState.Connecting);
            MethodInfo connect = typeof(RoomConnector).GetMethod("Connect", BindingFlags.Instance | BindingFlags.NonPublic);
            connect.Invoke(connector, new object[] { "nowhere", new Room.ConnectOptions { appKey = "none", matcherURL = "wss://offline.invalid" } });
            yield return runner.Until(() => app.State == AppState.Menu && connector.State == RoomConnector.ConnectorState.Disconnected, 200f,
                "back in the menu after attempt " + attempt);
            runner.Check(DemoUi.Get<DemoLabel>("start/status").Text.Contains("could not be reached"), "the panel says what happened");
            yield return runner.Wait(0.5f);
            runner.Check(DemoUi.Find("start/join-solo").CanClick, "the user can try again");
        }
        runner.IgnoreErrors = false;
        yield return JoinSolo(1, "leader", false, false);
        runner.Check(connector.Offline && roster.Local != null, "solo works after a failed connection");
        yield return LeaveToMenu();
    }

    private static IEnumerator BrokenConnection()
    {
        runner.Group("Broken connection", 90f);
        yield return JoinSolo(4, "follower", true, true);
        yield return runner.Until(() => session.Step == 0, 10f, "a session runs");
        yield return Click("hud/pause");
        Object.FindObjectOfType<RoomMenu>().OpenMenu();
        yield return runner.Wait(0.3f);
        // Somebody looks at the people of the room before the scripts of each frame, when its players are just destroyed.
        InputSystem.onBeforeUpdate += LookAtEverything;
        Object.FindObjectOfType<Realtime>().Disconnect();
        yield return runner.Wait(0.3f);
        InputSystem.onBeforeUpdate -= LookAtEverything;
        runner.Check(roster.Local == null, "no player is left");
        yield return runner.Until(() => app.State == AppState.Menu && IsShown("start"), 5f, "back in the menu");
        yield return runner.Wait(0.6f);
        runner.Check(DemoUi.Get<DemoLabel>("start/status").Text.Contains("connection was lost"), "the panel says what happened");
        runner.Check(!IsShown("hud") && !IsShown("menu") && solo.Bots.Count == 0, "nothing of the room is left");
        runner.Check(Object.FindObjectsOfType<AvatarBody>(true).Length == 1, "only the preview body is left");
        yield return JoinSolo(4, "follower", true, true);
        runner.Check(roster.Local.Connection > 1 && !session.Paused, "the user can join again");
        yield return LeaveToMenu();
    }

    private static void LookAtEverything() => probe.Snapshot();

    private static IEnumerator HurriedUser()
    {
        runner.Group("Hurried user", 90f);
        app.Join(true, ParticipantKind.Participant);
        app.Join(true, ParticipantKind.Operator);
        yield return runner.Until(() => app.State == AppState.Lobby, 3f, "the room opens once");
        app.Leave();
        app.Leave();
        yield return runner.Until(() => app.State == AppState.Menu && IsShown("start"), 5f, "leaving while the view fades");
        yield return runner.Wait(1f);
        runner.Check(roster.Local == null && solo.Bots.Count == 0 && !IsShown("hud"), "the menu is clean");
        runner.Check(CharacterOf(PreviewBody).Length > 0 && PreviewBody.Visible, "the preview is back");

        yield return JoinSolo(1, "leader", false, false);
        for (int i = 0; i < 12; i++)
        {
            app.ChooseAvatar(i % config.avatars.Count);
            if (i % 3 == 0) yield return null;
        }
        app.ChooseAvatar(2);
        yield return runner.Wait(2f);
        runner.Check(CharacterOf(roster.Local.Body) == "avatar-3" && roster.Local.AvatarId == 2, "the last avatar wins", CharacterOf(roster.Local.Body));
        runner.Check(roster.Local.Body.GetComponentsInChildren<IKTargetFollowVRRig>(true).Length == 1, "one character is left");
        for (int i = 0; i < 4; i++) app.ChangeRole(i % 2 == 0 ? 1 : 0);
        yield return runner.Wait(1.5f);
        runner.Check(roster.RoleOf(roster.Local) == 0 && solo.Bots.Count == 1 && roster.RoleOf(solo.Bots[0]) == 1, "quick role changes end in order");
        runner.Near(seating.transform.position, config.roles.Get(0).seatPosition, 0.02f, "at the seat of the last role");
        yield return LeaveToMenu();
    }

    /// <summary>As a participant of a room sees it: the operator paused the session and lost the connection.</summary>
    private static IEnumerator OperatorDisappears()
    {
        runner.Group("Operator disappears", 90f);
        yield return JoinSolo(2, "follower", true, true);
        yield return runner.Until(() => session.Step == 0 && director.View.time > 1.0, 10f, "a session runs");
        session.SetDirector(Director.Operator);
        session.SetPaused(true);
        yield return runner.Wait(0.4f);
        runner.Check(DemoUi.Find("hud/to-auto") != null && DemoUi.Find("hud/to-auto").CanClick, "the HUD offers to go on without an operator");
        CheckLayout("hud");
        Shot("session_operator_gone");
        yield return Click("hud/to-auto");
        runner.Check(session.Director == Director.Automatic && !session.Paused, "the session goes on");

        // What the session was keeps, whatever this user chose on the start panel.
        yield return JumpTo(config.script.Count);
        connector.Choices.pace = Pace.Study;
        connector.Choices.director = Director.Operator;
        yield return runner.Wait(0.4f);
        yield return Click("hud/again");
        runner.Check(session.Pace == Pace.Quick && session.Director == Director.Automatic, "run it again keeps pace and director");
        connector.Choices.director = Director.Automatic;

        // A new step number never shows the hint of the old step.
        yield return runner.Until(() => session.Step == 0 && director.View.time > 1.0, 10f, "a second run");
        session.SetPaused(true);
        yield return runner.Wait(0.2f);
        session.AdvanceFrom(0, clock.Now);
        runner.Check(director.View.step == 1, "the view of the step follows at once");
        yield return runner.Wait(0.3f);
        runner.Check(HudText().Contains(config.script.Get(1).hint), "the HUD shows the hint of the new step", HudText());
        yield return LeaveToMenu();
    }

    private static string HudText()
    {
        var text = new System.Text.StringBuilder();
        foreach (TMPro.TMP_Text t in DemoUi.GetPanel("hud").GetComponentsInChildren<TMPro.TMP_Text>()) text.Append(t.text).Append(" | ");
        return text.ToString();
    }

    private static IEnumerator SlowFrames()
    {
        runner.Group("Slow frames", 120f);
        yield return JoinSolo(1, "follower", false, false);
        yield return runner.Until(() => CharacterOf(BotBody).Length > 0, 4f, "both bodies are dressed");
        yield return runner.Wait(1.5f);
        Time.captureFramerate = 10;
        AvatarBody me = roster.Local.Body;
        float worst = 0f, largest = 0f, jump = 0f;
        Vector3 last = me.rightOut.position;
        for (int i = 0; i < 100; i++)
        {
            worst = Mathf.Max(worst, (me.rightOut.localPosition - ExpectedHand(me, me.rightHand, me.rightPartnerSlow, 0.3f)).magnitude);
            largest = Mathf.Max(largest, HandGap(me));
            jump = Mathf.Max(jump, (me.rightOut.position - last).magnitude);
            last = me.rightOut.position;
            yield return null;
        }
        runner.Check(worst < 0.005f, "the displayed hand follows the formula at 10 frames per second", worst.ToString("0.0000"));
        runner.Check(largest > 0.05f, "the partner moves the hand", largest.ToString("0.000"));
        runner.Check(jump < 0.25f, "the hand does not jump", jump.ToString("0.000"));
        foreach (IKFootSolver foot in me.GetComponentsInChildren<IKFootSolver>())
            runner.Check(foot.transform.position.y > -0.05f && foot.transform.position.y < 0.5f, "the feet stay near the floor", foot.transform.position.y.ToString("0.00"));
        Time.captureFramerate = Mathf.RoundToInt(1f / SmokeRunner.FrameSeconds);
        yield return LeaveToMenu();
    }

    /// <summary>The scene is loaded again in the middle of a session. Nothing of the old one may be left.</summary>
    private static IEnumerator SceneReload()
    {
        runner.Group("Scene reload", 90f);
        yield return JoinSolo(3, "follower", true, true);
        yield return runner.Until(() => session.Step == 0, 10f, "a session runs");
        Object.FindObjectOfType<RoomMenu>().OpenMenu();
        yield return runner.Wait(0.3f);
        SceneManager.LoadScene(0);
        yield return null;
        yield return null;
        FindObjects();
        yield return runner.Until(() => app.State == AppState.Menu && IsShown("start"), 8f, "the menu is back");
        yield return runner.Wait(1f);
        runner.Check(Object.FindObjectsOfType<AvatarBody>(true).Length == 1 && Object.FindObjectsOfType<Realtime>().Length == 1, "one preview body and one network object");
        runner.Check(!IsShown("hud") && !IsShown("menu"), "the panels of the room are gone");
        yield return JoinSolo(2, "leader", false, false);
        yield return runner.Until(() => CharacterOf(roster.Local.Body) == "avatar-2", 4f, "the new scene works");
        yield return LeaveToMenu();
    }

    /// <summary>The lobby belongs to online rooms. Its layout is checked in a room without a server.</summary>
    private static IEnumerator LobbyLayout()
    {
        runner.Group("Lobby layout", 60f);
        yield return JoinSolo(1, "leader", false, false);
        yield return runner.Wait(0.5f);
        var lobby = Object.FindObjectOfType<LobbyPanel>();
        Transform eye = view.Camera.transform;
        lobby.Show(new Pose(eye.position + eye.forward * 1.5f, Quaternion.LookRotation(eye.forward)));
        yield return runner.Wait(0.6f);
        CheckLayout("lobby");
        Shot("lobby");
        foreach (string id in new[] { "avatar-1", "avatar-5", "role-leader", "role-follower", "role-random", "enter", "leave" })
            runner.Check(DemoUi.Find("lobby/" + id) != null && DemoUi.Find("lobby/" + id).CanClick, "the lobby has " + id);
        runner.Check(DemoUi.Get<DemoLabel>("lobby/note").Text == config.roleNote, "the note about the study is in the lobby");
        lobby.Hide();
        yield return LeaveToMenu();
    }
}
