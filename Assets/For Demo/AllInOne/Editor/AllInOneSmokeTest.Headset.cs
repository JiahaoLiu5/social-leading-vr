using System.Collections;
using DemoKit;
using SocialLeading.AllInOne;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

/// <summary>
/// The same app with a headset and two controllers, whose poses and buttons are simulated. Everything is done with rays,
/// triggers, buttons, and sticks. Batch mode: -executeMethod AllInOneSmokeTest.RunHeadset, result in Logs/AllInOne-Headset.
/// </summary>
public static partial class AllInOneSmokeTest
{
    private const string HeadsetKey = "AllInOne.SmokeHeadset";
    private const string HeadsetFolder = "Logs/AllInOne-Headset";
    private const int Tracked = 3;
    private const int AimFrames = 12;

    private static XRSimulatedHMD hmd;
    private static XRSimulatedController leftDevice, rightDevice;
    private static XRSimulatedHMDState hmdState;
    private static XRSimulatedControllerState leftState, rightState;

    private static bool WithHeadset => UnityEditor.SessionState.GetBool(HeadsetKey, false);

    [MenuItem("Social Leading/All-in-One/Run Smoke Test (simulated headset)")]
    public static void RunHeadset() => Begin(HeadsetFolder, true);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PrepareHeadset()
    {
        if (!UnityEditor.SessionState.GetBool(RunningKey, false) || !WithHeadset) return;
        RigMode.SimulateHeadset = true;
        hmd = InputSystem.AddDevice<XRSimulatedHMD>("Test Headset");
        leftDevice = InputSystem.AddDevice<XRSimulatedController>("Test Left Controller");
        rightDevice = InputSystem.AddDevice<XRSimulatedController>("Test Right Controller");
        InputSystem.SetDeviceUsage(leftDevice, CommonUsages.LeftHand);
        InputSystem.SetDeviceUsage(rightDevice, CommonUsages.RightHand);

        // A user who put the headset on somewhere in the room, lower than the seat and turned away from it.
        hmdState.Reset();
        hmdState.centerEyePosition = new Vector3(0.4f, -0.25f, 0.3f);
        hmdState.centerEyeRotation = Quaternion.Euler(0f, 130f, 0f);
        hmdState.trackingState = Tracked;
        hmdState.isTracked = true;
        leftState.Reset();
        rightState.Reset();
        leftState.trackingState = rightState.trackingState = Tracked;
        leftState.isTracked = rightState.isTracked = true;
        PoseHands();
        SendDevices();
    }

    private static void RemoveHeadset()
    {
        if (hmd == null) return;
        InputSystem.RemoveDevice(hmd);
        InputSystem.RemoveDevice(leftDevice);
        InputSystem.RemoveDevice(rightDevice);
        hmd = null;
    }

    /// <summary>Sends the simulated devices to the Input System, every frame.</summary>
    private static void SendDevices()
    {
        if (hmd == null) return;
        InputSystem.QueueStateEvent(hmd, hmdState);
        InputSystem.QueueStateEvent(leftDevice, leftState);
        InputSystem.QueueStateEvent(rightDevice, rightState);
    }

    /// <summary>Both hands in front of the body, relative to the head.</summary>
    private static void PoseHands()
    {
        Quaternion heading = Quaternion.Euler(0f, hmdState.centerEyeRotation.eulerAngles.y, 0f);
        leftState.devicePosition = hmdState.centerEyePosition + heading * new Vector3(-0.20f, -0.32f, 0.34f);
        rightState.devicePosition = hmdState.centerEyePosition + heading * new Vector3(0.20f, -0.32f, 0.26f);
        leftState.deviceRotation = rightState.deviceRotation = heading;
    }

    // Script --------------------------------------------------------------------------------------------------

    private static IEnumerator HeadsetChecks()
    {
        yield return HeadsetReach();
        yield return HeadsetMenu();
        yield return HeadsetRoom();
        yield return HeadsetSession();
        yield return HeadsetOperator();
    }

    /// <summary>Every control of the start panel can be reached with a ray from a hand at rest.</summary>
    private static IEnumerator HeadsetReach()
    {
        runner.Group("Headset: reach", 240f);
        hmdState.centerEyeRotation = Quaternion.Euler(0f, 130f - 22f, 0f);
        yield return runner.Wait(0.5f);
        for (int page = 0; page < 4; page++)
        {
            if (page == 2) yield return Click("start/exp-full");
            yield return AimAtAll("start");
            if (page < 3) yield return Click("start/next");
        }
        yield return Click("start/join-operator");
        yield return runner.Wait(0.4f);
        yield return AimAtAll("start");
        yield return Click("start/operator-back");
        yield return GoToFirstPage();
        for (int i = 0; i < 2; i++) yield return Click("start/next");
        yield return Click("start/exp-free");
        yield return GoToFirstPage();
        hmdState.centerEyeRotation = Quaternion.Euler(0f, 130f, 0f);
        yield return runner.Wait(0.3f);
    }

    private static IEnumerator AimAtAll(string panel)
    {
        yield return runner.Wait(0.5f);
        foreach (DemoElement element in DemoUi.GetPanel(panel).GetComponentsInChildren<DemoElement>())
        {
            if (!element.CanClick) continue;
            yield return AimAt(element, AimFrames);
        }
    }

    private static IEnumerator HeadsetMenu()
    {
        runner.Group("Headset: menu", 90f);
        var mode = Object.FindObjectOfType<RigMode>();
        runner.Check(mode.IsDecided && !mode.Desktop, "the headset drives the rig");
        CheckPlace(config.lobbyPosition, config.lobbyYaw, "the user starts at the mirror");
        yield return runner.Wait(0.5f);
        hmdState.centerEyeRotation = Quaternion.Euler(0f, 130f - 22f, 0f);
        yield return Aim("start/avatar-4");
        yield return runner.Wait(0.3f);
        Shot("headset_01_menu");
        yield return Press("start/avatar-4");
        yield return runner.Until(() => CharacterOf(PreviewBody) == "avatar-4", 3f, "the ray chooses an avatar");
        hmdState.centerEyeRotation = Quaternion.Euler(0f, 130f, 0f);
        foreach (string id in new[] { "next", "role-follower", "next", "exp-free", "next", "join-solo" }) yield return Press("start/" + id);
        yield return runner.Until(() => app.State == AppState.InRoom, 5f, "in the room");
        yield return runner.Wait(1f);
    }

    private static IEnumerator HeadsetRoom()
    {
        runner.Group("Headset: room", 120f);
        RoleCatalog.Entry seat = config.roles.Get(1);
        CheckPlace(seat.seatPosition, seat.seatYaw, "the user stands at the seat of the Follower");
        runner.Near(seating.transform.position, seat.seatPosition, 0.001f, "the rig root has the exact seat");
        Transform target = roster.Local.Body.head;
        runner.Near(target.position, view.Camera.transform.TransformPoint(new Vector3(0f, -0.08f, -0.02f)), 0.03f, "the networked head follows the headset");

        yield return Tap(() => leftState = leftState.WithButton(ControllerButton.SecondaryButton), () => leftState = leftState.WithButton(ControllerButton.SecondaryButton, false));
        runner.Check(IsShown("menu") && DemoUi.GetPanel("menu").HeldBy == DemoHand.Left, "Y opens the menu on the left hand");
        yield return runner.Wait(0.2f);
        runner.Check(Object.FindObjectOfType<RoomMenu>().OnLeftHand, "the left ray is off while the menu hangs on the left hand");
        LiftLeftHand();
        yield return Aim("menu/link");
        CheckLayout("menu");
        Shot("headset_02_menu_on_hand");
        yield return Press("menu/link");
        yield return runner.Wait(0.5f);
        if (!runner.Check(IsShown("link") && !IsShown("menu"), "the link page replaces the menu")) yield break;
        yield return Aim("link/hand-super");
        CheckLayout("link");
        Shot("headset_03_link_on_hand");
        yield return Press("link/pin");
        Vector3 place = DemoUi.GetPanel("link").transform.position;
        leftState.devicePosition += new Vector3(0.1f, 0.1f, 0f);
        yield return runner.Wait(0.4f);
        runner.Near(DemoUi.GetPanel("link").transform.position, place, 0.005f, "a page that was put down stays");
        yield return Press("link/close");
        yield return HandPages();
        PoseHands();

        yield return Sticks();
        yield return Fingers();
        yield return RecentreFromTheMenu(seat);
        yield return Tap(() => leftState = leftState.WithButton(ControllerButton.MenuButton), () => leftState = leftState.WithButton(ControllerButton.MenuButton, false));
        LiftLeftHand();
        yield return Press("menu/leave");
        PoseHands();
        yield return runner.Until(() => app.State == AppState.Menu && IsShown("start"), 5f, "back in the menu");
        yield return runner.Wait(0.6f);
        CheckPlace(config.lobbyPosition, config.lobbyYaw, "the user is back at the mirror");
    }

    /// <summary>Every control of the pages on the left hand.</summary>
    private static IEnumerator HandPages()
    {
        LiftLeftHand();
        foreach (string page in new[] { "menu", "room", "link" })
        {
            var menu = Object.FindObjectOfType<RoomMenu>();
            if (page == "menu") menu.OpenMenu();
            else if (page == "room") menu.OpenRoom();
            else menu.OpenLink();
            yield return runner.Wait(0.4f);
            runner.Check(DemoUi.GetPanel(page).HeldBy == DemoHand.Left, page + " hangs on the left hand");
            CheckLayout(page);
            yield return AimAtAll(page);
            menu.CloseAll();
        }
    }

    private static IEnumerator Sticks()
    {
        Vector3 before = view.Camera.transform.position, root = seating.transform.position;
        leftState.primary2DAxis = new Vector2(0f, 1f);
        yield return runner.Wait(0.5f);
        leftState.primary2DAxis = Vector2.zero;
        yield return runner.Wait(0.2f);
        Vector3 moved = view.Camera.transform.position - before;
        Vector3 forward = Vector3.ProjectOnPlane(view.Camera.transform.forward, Vector3.up).normalized;
        runner.Check(Vector3.Dot(moved, forward) > 0.4f, "the left stick walks", moved.ToString("F2"));
        runner.Near(seating.transform.position, root, 0.001f, "walking leaves the rig root where it is");

        float yaw = view.Camera.transform.eulerAngles.y;
        rightState.primary2DAxis = new Vector2(1f, 0f);
        yield return runner.Wait(0.4f);
        rightState.primary2DAxis = Vector2.zero;
        yield return runner.Wait(0.2f);
        runner.Near(Mathf.DeltaAngle(yaw, view.Camera.transform.eulerAngles.y), 30f, 1f, "the right stick turns once");
    }

    private static IEnumerator Fingers()
    {
        rightState.grip = 1f;
        yield return runner.Wait(0.6f);
        Animator animator = roster.Local.Body.dresser.CurrentAnimator;
        runner.Check(animator.GetFloat("Right Grab") > 0.9f && animator.GetFloat("Left Grab") < 0.1f, "the grip closes the fingers of the own character",
            animator.GetFloat("Right Grab").ToString("0.00"));
        rightState.grip = 0f;
        yield return runner.Wait(0.3f);
    }

    private static IEnumerator RecentreFromTheMenu(RoleCatalog.Entry seat)
    {
        hmdState.centerEyePosition += new Vector3(0.2f, 0.12f, -0.3f);
        PoseHands();
        yield return Tap(() => leftState = leftState.WithButton(ControllerButton.SecondaryButton), () => leftState = leftState.WithButton(ControllerButton.SecondaryButton, false));
        LiftLeftHand();
        yield return Press("menu/recentre");
        yield return runner.Wait(0.3f);
        CheckPlace(seat.seatPosition, seat.seatYaw, "recentre brings the user back to the seat");
        yield return Press("menu/close");
        PoseHands();
        runner.Check(!IsShown("menu"), "the menu closes");
    }

    private static IEnumerator HeadsetSession()
    {
        runner.Group("Headset: session", 120f);
        foreach (string id in new[] { "back", "back", "back", "next", "role-leader", "next", "exp-full", "pace-quick", "next", "join-solo" })
            if (DemoUi.Find("start/" + id).CanClick) yield return Press("start/" + id);
        yield return runner.Until(() => app.State == AppState.InRoom, 5f, "in the room");
        yield return runner.Until(() => session.Step == 0 && director.View.phase == StepPhase.Response, 20f, "response time of step 1");
        yield return runner.Wait(0.3f);
        Shot("headset_04_step");
        yield return AimAtAll("hud");
        yield return Tap(() => rightState = rightState.WithButton(ControllerButton.SecondaryButton), () => rightState = rightState.WithButton(ControllerButton.SecondaryButton, false));
        runner.Check(roster.Local.DoneMark == session.Mark(0) || session.Step == 1, "B is I am done");
        yield return runner.Until(() => session.Step == 1, 5f, "the step ends early");

        yield return JumpTo(10);
        yield return runner.Until(() => stage.Flow.Shown == DyadTaskFlow.Page.Rating1, 5f, "the first rating");
        rightState.primary2DAxis = new Vector2(-1f, 0f);
        yield return runner.Wait(0.4f);
        rightState.primary2DAxis = Vector2.zero;
        yield return runner.Wait(0.1f);
        runner.Check(stage.Flow.ActivePanel.Value < 4, "the right stick moves the slider", stage.Flow.ActivePanel.Value.ToString());
        runner.Near(view.Camera.transform.eulerAngles.y, 0f, 1f, "the stick does not turn the user while a slider is open");
        Shot("headset_05_rating");
        yield return ButtonA();
        runner.Check(stage.Flow.Shown == DyadTaskFlow.Page.Question2, "A confirms");
        yield return JumpTo(12);
        yield return ButtonA();
        yield return runner.Until(() => stage.Flow.Shown == DyadTaskFlow.Page.Offer1, 5f, "the first offer");
        yield return Tap(() => leftState = leftState.WithButton(ControllerButton.PrimaryButton), () => leftState = leftState.WithButton(ControllerButton.PrimaryButton, false));
        runner.Check(stage.Flow.Shown == DyadTaskFlow.Page.WaitReply, "X rejects");
        yield return LeaveToMenu();
        AnswerStore.DeleteAll();
    }

    private static IEnumerator HeadsetOperator()
    {
        runner.Group("Headset: operator", 60f);
        yield return GoToFirstPage();
        for (int i = 0; i < 3; i++) yield return Click("start/next");
        yield return Click("start/join-operator");
        yield return Click("start/operator-solo");
        yield return runner.Until(() => app.State == AppState.InRoom && IsShown("op"), 5f, "the operator panel");
        yield return runner.Wait(1f);
        runner.Near(Flat(view.Camera.transform.position), Flat(config.watchPosition), 0.03f, "the operator stands at the wall");
        CheckLayout("op");
        hmdState.centerEyeRotation = Quaternion.Euler(0f, hmdState.centerEyeRotation.eulerAngles.y - 35f, 0f);
        yield return Aim("op/start");
        Shot("headset_06_operator");
        yield return AimAtAll("op");
        yield return Press("op/start");
        yield return runner.Until(() => session.Step == 0, 4f, "the operator starts the session with the ray");
        yield return Press("op/hand");
        LiftLeftHand();
        yield return Aim("op/hand");
        Shot("headset_07_operator_on_hand");
        yield return Press("op/hand");
        yield return Click("op/leave");
        yield return runner.Until(() => app.State == AppState.Menu, 5f, "back in the menu");
    }

    // Helpers -------------------------------------------------------------------------------------------------

    private static void CheckPlace(Vector3 floor, float yaw, string check)
    {
        Transform eye = view.Camera.transform;
        runner.Near(Flat(eye.position), Flat(floor), 0.03f, check);
        runner.Near(eye.position.y, config.eyeHeight, 0.03f, check + ", at eye height");
        runner.Near(Mathf.DeltaAngle(eye.eulerAngles.y, yaw), 0f, 2f, check + ", with the right heading");
    }

    /// <summary>Brings the left controller, and with it a panel, in front of the eyes.</summary>
    private static void LiftLeftHand()
    {
        Quaternion heading = Quaternion.Euler(0f, hmdState.centerEyeRotation.eulerAngles.y, 0f);
        leftState.devicePosition = hmdState.centerEyePosition + heading * new Vector3(-0.16f, -0.26f, 0.36f);
        leftState.deviceRotation = heading;
    }

    /// <summary>Points the right controller at an element and waits until it is hovered.</summary>
    private static IEnumerator Aim(string path)
    {
        DemoElement element = DemoUi.Find(path);
        if (!runner.Check(element != null, "find " + path)) yield break;
        yield return runner.Wait(0.5f);
        yield return AimAt(element, AimFrames);
    }

    private static IEnumerator AimAt(DemoElement element, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            Transform space = view.Camera.transform.parent;
            var rect = (RectTransform)element.transform;
            Vector3 hand = space.TransformPoint(rightState.devicePosition);
            rightState.deviceRotation = Quaternion.Inverse(space.rotation) * Quaternion.LookRotation(rect.TransformPoint(rect.rect.center) - hand);
            yield return null;
        }
        runner.Check(element.Hovered, "the ray reaches " + element.Path);
    }

    private static IEnumerator Press(string path)
    {
        yield return Aim(path);
        yield return Tap(() => { rightState.trigger = 1f; rightState = rightState.WithButton(ControllerButton.TriggerButton); },
                         () => { rightState.trigger = 0f; rightState = rightState.WithButton(ControllerButton.TriggerButton, false); });
    }

    private static IEnumerator ButtonA() =>
        Tap(() => rightState = rightState.WithButton(ControllerButton.PrimaryButton), () => rightState = rightState.WithButton(ControllerButton.PrimaryButton, false));

    private static IEnumerator Tap(System.Action down, System.Action up)
    {
        down();
        yield return runner.Wait(0.15f);
        up();
        yield return runner.Wait(0.3f);
    }
}
