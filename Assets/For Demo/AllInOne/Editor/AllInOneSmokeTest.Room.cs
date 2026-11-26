using System;
using System.Collections;
using DemoKit;
using Normal.Realtime;
using SocialLeading.AllInOne;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Free play with the link panel, changes inside the room, leaving and coming back, the operator, and late joining.</summary>
public static partial class AllInOneSmokeTest
{
    private static AvatarBody BotBody => solo.Bots[0].Body;

    // Free play -----------------------------------------------------------------------------------------------

    private static IEnumerator FreePlayChecks()
    {
        runner.Group("Free play as Leader", 60f);
        yield return JoinSolo(5, "leader", false, false);
        yield return runner.Until(() => CharacterOf(roster.Local.Body) == "avatar-5" && CharacterOf(BotBody).Length > 0, 4f, "both bodies are dressed");
        yield return runner.Wait(1.5f);
        CheckLayout("hud");
        runner.Near(roster.Local.Body.rightOut.position, roster.Local.Body.rightHand.position, 0.001f, "a Leader is not moved");
        desktop.Body.CurrentMotion = DemoDesktopRig.Motion.Wave;
        yield return runner.Wait(1.5f);
        runner.Check(HandGap(BotBody) > 0.05f, "the partner follows a share of the own wave", HandGap(BotBody).ToString("0.000"));
        Shot("play_01_wave");
        Overview("play_01_overview");
        yield return LinkPanelChecks();
        yield return PositionChecks();
        yield return ChangesInTheRoom();
        yield return LeaveWithTheMenu();
        yield return JoinAndLeaveFiveTimes();
    }

    private static float HandGap(AvatarBody body) => (body.rightOut.position - body.rightHand.position).magnitude;

    private static IEnumerator LinkPanelChecks()
    {
        runner.Group("Link panel", 90f);
        OpenMenu();
        yield return runner.Wait(0.5f);
        CheckLayout("menu");
        Shot("play_02_menu");
        yield return Click("menu/link");
        yield return runner.Wait(0.6f);
        CheckLayout("link");
        Shot("play_03_link");
        foreach (string id in new[] { "on", "hand-off", "hand-blend", "hand-super", "pos-off", "pos-super", "pos-blend", "reset", "pin", "close" })
            runner.Check(DemoUi.Find("link/" + id) != null, "the link panel has " + id);
        foreach (string id in new[] { "ratio", "smooth", "pos-ratio", "topology" })
            runner.Check(DemoUi.Get<DemoStepper>("link/" + id) != null, "the link panel has " + id);

        // Off: no displayed hand moves faster than a ramp allows.
        float worst = 0f;
        Vector3 last = BotBody.rightOut.position - BotBody.rightHand.position;
        yield return Click("link/on");
        for (int i = 0; i < 40; i++)
        {
            yield return null;
            Vector3 now = BotBody.rightOut.position - BotBody.rightHand.position;
            worst = Mathf.Max(worst, (now - last).magnitude);
            last = now;
        }
        runner.Check(worst < 0.05f, "the link fades out", worst.ToString("0.000"));
        runner.Check(HandGap(BotBody) < 0.001f, "link off: the partner moves alone", HandGap(BotBody).ToString("0.0000"));
        runner.Check(!session.LinkOn, "link off in the session");

        // Ratio 1, no smoothing: a copy of the own hand relative to the head.
        yield return Click("link/on");
        for (int i = 0; i < 7; i++) yield return Click("link/ratio.plus");
        for (int i = 0; i < 2; i++) yield return Click("link/smooth.minus");
        yield return runner.Wait(1.6f);
        runner.Near(session.HandRatio, 1f, 0.01f, "ratio 1 in the session");
        AvatarBody me = roster.Local.Body;
        Vector3 own = me.rightHand.localPosition - me.head.localPosition, copied = BotBody.rightOut.localPosition - BotBody.headOut.localPosition;
        runner.Near(copied, own, 0.04f, "at ratio 1 the partner copies the hand");
        Shot("play_04_ratio_one");

        yield return Click("link/hand-super");
        yield return runner.Wait(2.5f);
        runner.Check(BotBody.LiveProfile.hands == ModulationProfile.HandMode.Superimpose, "superimpose reaches the body");
        yield return Click("link/topology.plus");
        yield return runner.Wait(2f);
        runner.Check(session.Topology == 1 && me.link.explicitSource == BotBody.transform && BotBody.link.explicitSource == null, "the Follower moves the Leader now");
        yield return Click("link/hand-blend");
        yield return runner.Wait(2f);
        runner.Check(HandGap(me) > 0.02f, "the own displayed hand differs now", HandGap(me).ToString("0.000"));
        yield return Click("link/reset");
        yield return runner.Wait(2f);
        runner.Check(session.Topology == 0 && Mathf.Abs(session.HandRatio - 0.3f) < 0.01f && session.HandMode == 1, "reset brings the start values back");
    }

    private static IEnumerator PositionChecks()
    {
        runner.Group("Position channel", 60f);
        yield return Click("link/topology.plus");
        yield return Click("link/pos-super");
        yield return runner.Wait(2f);
        for (int i = 0; i < 40; i++)
        {
            solo.Bots[0].Rig.headOffset += new Vector3(0f, 0f, 0.6f / 40f);
            yield return null;
        }
        yield return runner.Wait(0.3f);
        runner.Check(view.Offset.magnitude > 0.1f, "the partner's steps move the own view", view.Offset.magnitude.ToString("0.000"));
        float worst = 0f;
        Vector3 last = view.Offset;
        yield return Click("link/pos-off");
        for (int i = 0; i < 60; i++)
        {
            yield return null;
            worst = Mathf.Max(worst, (view.Offset - last).magnitude);
            last = view.Offset;
        }
        runner.Check(worst < 0.05f, "the offset fades without a jump", worst.ToString("0.000"));
        runner.Check(view.Offset.magnitude < 0.005f, "no offset is left after the channel is off", view.Offset.magnitude.ToString("0.0000"));
        yield return Click("link/pos-super");
        worst = 0f;
        last = view.Offset;
        for (int i = 0; i < 40; i++)
        {
            yield return null;
            worst = Mathf.Max(worst, (view.Offset - last).magnitude);
            last = view.Offset;
        }
        runner.Check(worst < 0.02f, "switching it on again does not jump", worst.ToString("0.000"));
        solo.Bots[0].Rig.headOffset = new Vector3(0f, -0.08f, -0.02f);
        yield return Click("link/reset");
        yield return Click("link/close");
    }

    private static IEnumerator ChangesInTheRoom()
    {
        runner.Group("Changes in the room", 120f);
        OpenMenu();
        yield return runner.Wait(0.4f);
        yield return Click("menu/room");
        yield return runner.Wait(0.5f);
        CheckLayout("room");
        Shot("play_05_room");
        yield return Click("room/avatar-2");
        yield return runner.Until(() => CharacterOf(roster.Local.Body) == "avatar-2" && roster.Local.AvatarId == 1, 3f, "a new avatar inside the room");
        yield return Click("room/role-follower");
        yield return runner.Wait(1.5f);
        runner.Check(roster.RoleOf(roster.Local) == 1 && roster.RoleOf(solo.Bots[0]) == 0, "a new role inside the room; the partner takes the other one");
        runner.Near(seating.transform.position, config.roles.Get(1).seatPosition, 0.02f, "a new seat with the new role");
        runner.Check(CharacterOf(roster.Local.Body) == "avatar-2", "the avatar stays with a new role");

        int before = session.SessionId;
        yield return Click("room/exp-full");
        yield return Click("room/pace-quick");
        yield return Click("room/start");
        runner.Check(session.SessionId == before + 1 && session.Experience == Experience.FullSession && session.Pace == Pace.Quick, "a full session starts from free play");
        yield return runner.Until(() => director.View.phase == StepPhase.Countdown, 3f, "its countdown runs");
        runner.Check(!session.MayChangeLink(roster.Local), "the link of a full session is fixed");
        yield return JumpTo(13);
        yield return runner.Wait(0.6f);
        runner.Check(session.Finished, "the session is over");
        yield return Click("hud/stay");
        yield return runner.Wait(0.5f);
        runner.Check(session.Experience == Experience.FreePlay && !stage.Flow.IsOpen, "stay and play ends the session");
        visible.Clear();
        stage.CollectVisible(visible);
        runner.Check(visible.Count == 0, "boards and panels are gone", string.Join(", ", visible));
    }

    private static IEnumerator LeaveWithTheMenu()
    {
        runner.Group("Leave and come back", 60f);
        OpenMenu();
        yield return runner.Wait(0.4f);
        yield return Click("menu/leave");
        yield return runner.Until(() => app.State == AppState.Menu && IsShown("start"), 4f, "back in the menu");
        yield return runner.Wait(0.8f);
        runner.Check(Object.FindObjectOfType<RealtimeAvatar>() == null && solo.Bots.Count == 0, "no bodies are left");
        runner.Check(view.Camera.isActiveAndEnabled && PreviewBody.gameObject.activeInHierarchy, "view and preview are alive");
        runner.Check(CharacterOf(PreviewBody) == "avatar-2" && DemoUi.Find("start/avatar-2").Selected, "the last choices are still there", CharacterOf(PreviewBody));
        runner.Check(!IsShown("hud") && !IsShown("menu") && !IsShown("room") && !IsShown("link"), "the panels of the room are closed");
        runner.Near(seating.transform.position, config.lobbyPosition, 0.01f, "the rig is back at the mirror");
    }

    private static IEnumerator JoinAndLeaveFiveTimes()
    {
        runner.Group("Five times", 120f);
        yield return RunOnce(1);
        GC.Collect();
        long before = GC.GetTotalMemory(true);
        for (int i = 2; i <= 5; i++) yield return RunOnce(i);
        long growth = GC.GetTotalMemory(true) - before;
        runner.Check(growth < 20 * 1024 * 1024, "memory does not grow", (growth / 1024) + " KB");
        runner.Check(Object.FindObjectsOfType<AvatarBody>(true).Length == 1, "only the preview body is left");
    }

    private static IEnumerator RunOnce(int avatar)
    {
        yield return JoinSolo(avatar, avatar % 2 == 0 ? "leader" : "random", false, false);
        yield return runner.Until(() => CharacterOf(roster.Local.Body) == "avatar-" + avatar, 3f, "dressed in run " + avatar);
        yield return LeaveToMenu();
    }

    private static void OpenMenu() => Object.FindObjectOfType<RoomMenu>().OpenMenu();

    // Operator ------------------------------------------------------------------------------------------------

    private static IEnumerator OperatorChecks()
    {
        runner.Group("Operator", 120f);
        yield return GoToFirstPage();
        for (int i = 0; i < 3; i++) yield return Click("start/next");
        yield return Click("start/join-operator");
        yield return Click("start/operator-solo");
        yield return runner.Until(() => app.State == AppState.InRoom, 5f, "in the room");
        yield return runner.Wait(1.5f);
        var op = Object.FindObjectOfType<OperatorPanel>();
        runner.Check(!roster.Local.Body.Visible && roster.Local.AvatarId < 0 && roster.Local.Kind == ParticipantKind.Operator, "an operator has no body");
        runner.Check(solo.Bots.Count == 2 && roster.SeatedCount == 2, "simulated partners take both roles");
        runner.Check(session.Experience == Experience.FullSession && session.Director == Director.Operator && session.Waiting, "the session waits for the operator");
        runner.Check(roster.Local.Muted, "the operator is muted at first");
        runner.Check(mirrorGate.InView && mirror.enabled, "the mirror is alive at the place of the operator");
        CheckLayout("op");
        Shot("operator_1_waiting");
        foreach (string id in new[] { "start", "next", "repeat", "pause", "end", "take", "auto", "pace-study", "pace-quick", "talk", "mute-narration", "hand", "leave" })
            runner.Check(DemoUi.Find("op/" + id) != null, "the operator panel has " + id);
        runner.Check(DemoUi.Find("op/back") == null, "there is no way back, as in the study");

        yield return Click("op/pace-quick");
        yield return Click("op/start");
        yield return runner.Until(() => session.Step == 0 && director.View.time > 1.0, 4f, "start begins step 1");
        op.Next();
        yield return null;
        runner.Check(session.Step == 0, "next is refused while the clip plays");
        yield return runner.Until(() => director.View.phase == StepPhase.Response, 12f, "the clip ends");
        yield return runner.Wait(0.5f);
        CheckLayout("op");
        Shot("operator_2_step");
        Overview("operator_3_overview");
        clock.TestSpeed = Fast;
        yield return runner.Wait(2f);
        runner.Check(session.Step == 0, "a step never ends by itself");
        clock.TestSpeed = 1f;
        op.Next();
        yield return null;
        runner.Check(session.Step == 1, "next advances after the clip");
        yield return Click("op/repeat");
        runner.Check(session.RepeatCount == 1, "repeat");
        yield return Click("op/pause");
        runner.Check(session.Paused, "pause");
        yield return Click("op/pause");
        runner.Check(!session.Paused, "go on");
        yield return Click("op/pause");
        yield return Click("op/auto");
        runner.Check(session.Director == Director.Automatic && !session.Paused, "back to automatic ends a pause");
        yield return Click("op/take");
        runner.Check(session.Director == Director.Operator, "take control");
        yield return Click("op/end");
        yield return runner.Wait(0.3f);
        runner.Check(session.Finished, "end finishes the session");
        Shot("operator_4_end");
        desktop.Body.Teleport(Vector3.zero, 200f);
        seating.Recentre();
        yield return null;
        yield return null;
        runner.Near(view.Camera.transform.position, config.operatorDesktopPosition + Vector3.up * config.operatorDesktopEyeHeight, 0.03f, "recentre brings the operator back to the wall");
        runner.Near(Mathf.DeltaAngle(view.Camera.transform.eulerAngles.y, config.operatorDesktopYaw), 0f, 1f, "with the first view");
        yield return Click("op/mute-narration");
        runner.Check(narrator.Muted, "the operator can mute the narration");
        yield return Click("op/leave");
        yield return runner.Until(() => app.State == AppState.Menu, 5f, "back in the menu");
        runner.Check(!narrator.Muted, "the narration is on again for the next role");
    }

    // Late join -----------------------------------------------------------------------------------------------

    /// <summary>A model in the middle of a step, as a client that joins late sees it.</summary>
    private static IEnumerator LateJoinChecks()
    {
        runner.Group("Late join", 90f);
        yield return JoinSolo(4, "follower", true, false);
        yield return runner.Wait(0.5f);
        for (int k = session.Step; k < 7; k++) session.AdvanceFrom(k, clock.Now);
        session.AdvanceFrom(7, clock.Now - 5.0);
        yield return null;
        yield return null;
        yield return null;
        runner.Check(session.Step == 8 && director.View.phase == StepPhase.Narration, "in the middle of the clip of step 9");
        var source = narrator.GetComponent<AudioSource>();
        runner.Check(source.clip == config.script.Get(8).clip && Mathf.Abs(source.time - (float)director.View.time) < 0.5f, "the narration starts in the middle",
            source.time.ToString("0.00") + " of " + director.View.time.ToString("0.00"));
        runner.Near(narrator.Captions.Elapsed, (float)director.View.time, 0.5f, "the captions too");
        CheckVisible(8);

        session.Repeat(clock.Now - 20.0);
        yield return runner.Wait(0.5f);
        runner.Check(director.View.phase == StepPhase.Response, "in the response time");
        DemoProgressBar timer = DemoUi.Get<DemoProgressBar>("hud/timer");
        float left = config.script.Length(8) + 60f - 20.5f;
        runner.Check(timer != null && timer.ValueText == PanelParts.Minutes(director.View.remaining), "the HUD timer is right", timer != null ? timer.ValueText : "none");
        runner.Near(director.View.remaining, left, 0.6f, "the time that is left");
        yield return LeaveToMenu();
    }
}
