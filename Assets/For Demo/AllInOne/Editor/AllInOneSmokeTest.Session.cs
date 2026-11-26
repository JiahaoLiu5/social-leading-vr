using System.Collections;
using System.Collections.Generic;
using DemoKit;
using Normal.Realtime;
using SocialLeading.AllInOne;
using UnityEngine;

/// <summary>A full session alone as Follower, from the countdown to the end, with all boards and tasks.</summary>
public static partial class AllInOneSmokeTest
{
    private const float Fast = 20f;

    private static readonly List<string> visible = new List<string>();

    private static IEnumerator SessionChecks()
    {
        yield return SoloEntry();
        yield return LinkInStepOne();
        yield return WholeSession();
        yield return AfterTheEnd();
        yield return LimitAndSecondReply();
        yield return PauseAndRepeat();
        yield return LeaveToMenu();
        yield return AnswersInTheMenu();
    }

    // Entry ---------------------------------------------------------------------------------------------------

    private static IEnumerator SoloEntry()
    {
        runner.Group("Solo entry", 60f);
        yield return JoinSolo(3, "follower", true, true);
        yield return runner.Until(() => CharacterOf(roster.Local.Body) == "avatar-3", 3f, "the own body is dressed");
        ProbeSnapshot s = probe.Snapshot();
        runner.Check(s.offline && s.clientId == 0, "offline room with client id 0");
        runner.Check(Object.FindObjectOfType<RealtimeAvatarManager>().avatars.Count == 1, "one networked avatar");
        runner.Check(solo.Bots.Count == 1 && solo.Bots[0].GetComponentInChildren<RealtimeAvatar>(true) == null, "one simulated partner without network parts");
        runner.Check(roster.RoleOf(solo.Bots[0]) == 0 && roster.RoleOf(roster.Local) == 1, "the partner is the Leader");
        runner.Check(roster.Local.AvatarId == 2 && solo.Bots[0].AvatarId != 2, "own avatar and another one for the partner");
        runner.Check(roster.Local.CompareTag("FollowerTag") && solo.Bots[0].CompareTag("LeaderTag"), "the roles set the tags");
        runner.Near(seating.transform.position, new Vector3(0f, 0f, 1.5f), 0.02f, "rig at the seat of the Follower");
        runner.Check(view.Camera.isActiveAndEnabled, "the view is alive");
        runner.Check(!roster.Local.GetComponentInChildren<RealtimeAvatarVoice>(true).enabled, "no microphone in solo");
        runner.Check(session.Experience == Experience.FullSession && session.Director == Director.Automatic && session.Pace == Pace.Quick, "the session runs as chosen");
        yield return runner.Until(() => director.View.phase == StepPhase.Countdown && IsShown("hud"), 3f, "countdown on the HUD");
        yield return runner.Wait(0.6f);
        CheckLayout("hud");
        Shot("session_00_countdown");
    }

    private static IEnumerator LinkInStepOne()
    {
        runner.Group("Link in step 1", 60f);
        yield return runner.Until(() => session.Step == 0 && director.View.time > 5.6, 15f, "step 1 and the partner waves");
        app.ChooseAvatar(0);
        app.ChangeRole(0);
        runner.Check(roster.Local.AvatarId == 2 && roster.Local.WantedRole == 1, "avatar and role are fixed while a session runs");
        AvatarBody me = roster.Local.Body;
        float worst = 0f, largest = 0f;
        for (int i = 0; i < 60; i++)
        {
            worst = Mathf.Max(worst, (me.rightOut.localPosition - ExpectedHand(me, me.rightHand, me.rightPartnerSlow, 0.3f)).magnitude);
            largest = Mathf.Max(largest, (me.rightOut.position - me.rightHand.position).magnitude);
            if (i == 30)
            {
                CheckLayout("hud");
                Shot("session_01_wave");
            }
            yield return null;
        }
        runner.Check(worst < 0.005f, "the displayed hand follows the formula", worst.ToString("0.0000"));
        runner.Check(largest > 0.05f, "the partner's wave moves the displayed hand", largest.ToString("0.000"));

        // The partner leans forward, in its own root axes. The own view moves by the share, in the own root axes.
        clock.TestSpeed = 0.01f;
        Vector3 before = OffsetInRoot(me);
        Vector3 viewBefore = view.Camera.transform.position - view.Offset;
        for (int i = 0; i < 30; i++)
        {
            solo.Bots[0].Rig.headOffset += new Vector3(0f, 0f, 0.4f / 30f);
            yield return null;
        }
        yield return runner.Wait(0.4f);
        runner.Near(OffsetInRoot(me) - before, new Vector3(0f, 0f, 0.12f), 0.015f, "a step of the partner moves the own view");
        runner.Near(view.Camera.transform.position - view.Offset, viewBefore, 0.02f, "the view moves by the offset only");

        // A jump of the partner is a recentre, not a step.
        before = OffsetInRoot(me);
        solo.Bots[0].Rig.headOffset += new Vector3(0f, 0f, 0.8f);
        yield return runner.Wait(0.3f);
        runner.Near(OffsetInRoot(me), before, 0.012f, "a jump of the partner is ignored");
        solo.Bots[0].Rig.headOffset = new Vector3(0f, -0.08f, -0.02f);
        yield return runner.Wait(0.3f);
        clock.TestSpeed = 1f;
    }

    /// <summary>The blend of HandMixedFollowSystem, computed again.</summary>
    private static Vector3 ExpectedHand(AvatarBody body, Transform ownHand, Transform partnerHand, float ratio)
    {
        Vector3 own = ownHand.localPosition - body.head.localPosition;
        Vector3 from = body.headPartner.localRotation * Vector3.forward, to = body.headOut.localRotation * Vector3.forward;
        from.y = to.y = 0f;
        Quaternion turn = Quaternion.AngleAxis(Vector3.SignedAngle(from, to, Vector3.up), Vector3.up);
        Vector3 partner = turn * (partnerHand.localPosition - body.headPartner.localPosition);
        return body.headOut.localPosition + Vector3.Lerp(own, partner, ratio);
    }

    private static Vector3 OffsetInRoot(AvatarBody body) => body.transform.InverseTransformDirection(view.Offset);

    // Steps ---------------------------------------------------------------------------------------------------

    private static IEnumerator WholeSession()
    {
        StudyScript script = config.script;
        for (int i = 0; i < script.Count; i++)
        {
            runner.Group("Step " + (i + 1), 90f);
            int step = i;
            yield return runner.Until(() => session.Step == step, 60f, "the step starts");
            double start = session.StepStartTime;
            clock.TestSpeed = 1f;
            yield return runner.Until(() => narrator.Captions.IsPlaying && narrator.Captions.Text.Length > 0, 2f, "captions are shown");
            CheckVisible(i);
            yield return StepPicture(i);

            if (i == 10) yield return RatingsTask();
            else if (i == 12) yield return SplitTask(85, "rejects");
            else
            {
                // The last steps of the list are waited for at high speed; the length is checked on the session clock.
                clock.TestSpeed = Fast;
                yield return runner.Until(() => session.Step != step, 60f, "the step ends by itself");
                float length = (float)(session.StepStartTime - start);
                float expected = script.Length(i) + script.Window(i, Pace.Quick) + script.Gap(i, Pace.Quick);
                runner.Near(length, expected, 1.5f, "length of the step");
            }
            clock.TestSpeed = 1f;
        }
    }

    private static void CheckVisible(int step)
    {
        visible.Clear();
        stage.CollectVisible(visible);
        bool boards = visible.Contains("Board 1") && visible.Contains("Board 2");
        bool standpoints = visible.Contains("Board 1 Text 4") && visible.Contains("Board 2 Text 4");
        runner.Check(boards == step >= 7, "boards from step 8 on", string.Join(", ", visible));
        runner.Check(standpoints == step >= 8, "positions from step 9 on", string.Join(", ", visible));
        runner.Check(stage.Flow.IsOpen == (step == 10 || step == 12), "task panel only in steps 11 and 13", stage.Flow.Shown.ToString());
    }

    /// <summary>Lets the interesting steps run for a while at normal speed and takes pictures.</summary>
    private static IEnumerator StepPicture(int step)
    {
        switch (step)
        {
            case 1:
                yield return runner.Until(() => director.View.phase == StepPhase.Response && director.View.time > 6.4, 12f, "the partner came closer");
                runner.Check(Vector3.Distance(Flat(solo.Bots[0].Body.headOut.position), Flat(roster.Local.Body.headOut.position)) < 1.2f, "the partner is close enough for a handshake");
                Shot("session_02_handshake");
                Overview("session_02_handshake_overview");
                break;
            case 3:
                desktop.Body.Yaw = 90f;
                desktop.Body.CurrentMotion = DemoDesktopRig.Motion.RaiseLeft;
                yield return runner.Until(() => director.View.time > 19.0, 25f, "left hand up at the mirror");
                runner.Check(mirrorGate.InView && mirror.enabled, "the mirror is alive in front of the user");
                Shot("session_04_mirror");
                desktop.Body.CurrentMotion = DemoDesktopRig.Motion.Idle;
                desktop.Body.Yaw = 180f;
                break;
            case 7:
                yield return runner.Until(() => director.View.time > 10.0, 15f, "the partner went to a board");
                desktop.Body.Teleport(new Vector3(-1.0f, 0f, 1.75f), 270f);
                yield return runner.Wait(1.2f);
                Shot("session_08_board");
                Overview("session_08_boards_overview");
                runner.Check(solo.Bots[0].transform.position.x < -1.5f && solo.Bots[0].transform.position.z < 0f, "the partner took the other board",
                    solo.Bots[0].transform.position.ToString("F2"));
                break;
            case 8:
                yield return runner.Until(() => director.View.phase == StepPhase.Response, 20f, "thinking time");
                yield return runner.Wait(0.5f);
                runner.Check(DemoUi.Get<DemoProgressBar>("hud/timer") != null, "the HUD shows the time that is left");
                CheckLayout("hud");
                Shot("session_09_position");
                desktop.Body.Teleport(new Vector3(0f, 0f, 1.5f), 180f);
                break;
            case 9:
                yield return runner.Until(() => director.View.phase == StepPhase.Response && director.View.time > 11.0, 20f, "debate");
                Shot("session_10_debate");
                break;
        }
    }

    private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // Tasks ---------------------------------------------------------------------------------------------------

    private static IEnumerator RatingsTask()
    {
        DyadTaskFlow flow = stage.Flow;
        runner.Check(flow.Shown == DyadTaskFlow.Page.Question1, "page of question 1");
        yield return runner.Wait(1.8f);
        runner.Check(!IsShown("hud"), "the HUD is gone while a task panel is open");
        Shot("session_11_question");
        yield return runner.Until(() => flow.Shown == DyadTaskFlow.Page.Rating1, 4f, "question 1 after three seconds");
        yield return runner.Wait(0.3f);
        Transform panels = GameObject.Find("Task Panels").transform;
        runner.Near(Mathf.DeltaAngle(panels.eulerAngles.y, 180f), 0f, 0.1f, "the panel faces the Follower");
        runner.Near(flow.ActivePanel.Value, 4f, 0.01f, "the rating starts in the middle");
        flow.ActivePanel.rating.ChangeSliderValue(2);
        yield return null;
        runner.Near(flow.ActivePanel.Value, 6f, 0.01f, "the rating moves");
        Shot("session_11_rating");
        yield return Click("task-confirm");
        runner.Check(flow.Shown == DyadTaskFlow.Page.Question2, "page of question 2");
        yield return runner.Until(() => flow.Shown == DyadTaskFlow.Page.Rating2, 4f, "question 2 after three seconds");
        yield return RayOnRating(flow);
        int mark = session.Mark(10);
        runner.Check(roster.Local.DoneMark == mark, "the own answer is marked as done");
        runner.Check(!flow.IsOpen, "the panel closes");
        yield return RaiseLeftHand();
        clock.TestSpeed = Fast;
        yield return runner.Until(() => session.Step > 10, 30f, "the step ends early when both are done");
        clock.TestSpeed = 1f;
    }

    /// <summary>The clip asks for a raised left hand, and the app notices it.</summary>
    private static IEnumerator RaiseLeftHand()
    {
        desktop.Body.CurrentMotion = DemoDesktopRig.Motion.RaiseLeft;
        yield return runner.Until(() => stage.HandWasRaised, 3f, "the raised left hand is noticed");
        Shot("session_11_done");
        desktop.Body.CurrentMotion = DemoDesktopRig.Motion.Idle;
    }

    /// <summary>The same with a ray: drag the slider to the right end, then press the confirm picture.</summary>
    private static IEnumerator RayOnRating(DyadTaskFlow flow)
    {
        Transform origin = new GameObject("Test Ray").transform;
        origin.position = view.Camera.transform.position;
        DemoManualPress press = DemoPress.Manual();
        DemoPointerRay ray = DemoInput.AddRay(origin, press, DemoHand.Right);
        ray.Smoothing = 0f;
        var slider = (RectTransform)flow.ActivePanel.slider.transform;
        origin.LookAt(slider.TransformPoint(Vector3.zero));
        yield return runner.Wait(0.5f);
        runner.Check(ray.Hovered != null && ray.Hovered.Id == DyadTaskFlow.SliderId, "the ray finds the slider");
        press.IsPressed = true;
        yield return runner.Wait(0.2f);
        for (int i = 0; i <= 20; i++)
        {
            origin.LookAt(slider.TransformPoint(new Vector3(slider.rect.width * 0.49f * i / 20f, 0f, 0f)));
            yield return null;
        }
        yield return runner.Wait(0.2f);
        runner.Near(flow.ActivePanel.Value, 7f, 0.01f, "the ray drags the slider to the end");
        press.IsPressed = false;
        yield return runner.Wait(0.3f);

        DemoElement confirm = DemoUi.Find(DyadTaskFlow.ConfirmId);
        origin.LookAt(confirm.transform.position);
        yield return runner.Wait(0.4f);
        runner.Check(ray.Hovered == confirm, "the ray finds the confirm picture");
        Shot("session_11_rating_ray");
        press.IsPressed = true;
        yield return runner.Wait(0.2f);
        press.IsPressed = false;
        yield return runner.Wait(0.2f);
        Object.Destroy(ray.gameObject);
        Object.Destroy(origin.gameObject);
        yield return null;
    }

    private static IEnumerator SplitTask(int firstSplit, string reply)
    {
        DyadTaskFlow flow = stage.Flow;
        runner.Check(flow.Shown == DyadTaskFlow.Page.Split1, "first split");
        flow.ActivePanel.split.ChangeSliderValue(firstSplit);
        yield return runner.Wait(0.3f);
        Shot("session_13_split");
        yield return Click("task-confirm");
        runner.Check(flow.Shown == DyadTaskFlow.Page.WaitOffer1, "waiting page");
        yield return runner.Wait(0.5f);
        Shot("session_13_waiting");
        yield return runner.Until(() => flow.Shown == DyadTaskFlow.Page.Offer1, 4f, "the first offer after three seconds");
        yield return runner.Wait(0.3f);
        Shot("session_13_offer");
        yield return Click("task-reject");
        yield return runner.Until(() => flow.Shown == DyadTaskFlow.Page.Reply, 4f, "the reply after three seconds");
        yield return runner.Wait(0.3f);
        Shot("session_13_reply");
        runner.Check(GameObject.Find("RejOrAcc").GetComponent<TMPro.TMP_Text>().text == reply, "the reply " + reply);
        yield return runner.Until(() => flow.Shown == DyadTaskFlow.Page.Split2, 6f, "the second split after five seconds");
        flow.ActivePanel.split.ChangeSliderValue(40);
        yield return null;
        yield return Click("task-confirm");
        yield return runner.Until(() => flow.Shown == DyadTaskFlow.Page.Offer2, 4f, "the second offer after three seconds");
        yield return Click("task-accept");
        runner.Check(flow.Shown == DyadTaskFlow.Page.Final && flow.Done, "the last page");
        runner.Check(roster.Local.DoneMark == session.Mark(12), "the task is marked as done");
    }

    // End -----------------------------------------------------------------------------------------------------

    private static IEnumerator AfterTheEnd()
    {
        runner.Group("After the end", 90f);
        clock.TestSpeed = Fast;
        yield return runner.Until(() => session.Finished, 30f, "the session ends");
        clock.TestSpeed = 1f;
        yield return runner.Wait(1f);
        runner.Check(stage.Flow.Shown == DyadTaskFlow.Page.Final, "the final page stays");
        foreach (string id in new[] { "return", "again", "stay", "delete-answers" })
            runner.Check(DemoUi.Find("hud/" + id) != null && DemoUi.Find("hud/" + id).CanClick, "the HUD offers " + id);
        CheckLayout("hud");
        Shot("session_14_finish");
        Overview("session_14_finish_overview");
        runner.Check(AnswerStore.HasAnswers, "the answers are on the device");
        yield return Click("hud/delete-answers");
        runner.Check(!AnswerStore.HasAnswers, "the answers can be deleted");

        int before = session.SessionId;
        yield return Click("hud/again");
        runner.Check(session.SessionId == before + 1 && session.Step < 0, "run it again starts from the beginning");
        yield return runner.Until(() => session.Step == 0, 8f, "the second run starts");
        double start = session.StepStartTime;
        clock.TestSpeed = Fast;
        yield return runner.Until(() => session.Step == 1, 30f, "step 1 ends");
        runner.Check(session.StepStartTime - start >= config.script.Length(0) + 2f, "nobody is done from the first run", (session.StepStartTime - start).ToString("0.0"));
        clock.TestSpeed = 1f;
    }

    private static IEnumerator LimitAndSecondReply()
    {
        runner.Group("Limits", 120f);
        yield return JumpTo(10);
        runner.Check(stage.Flow.IsOpen, "the ratings open");
        clock.TestSpeed = Fast;
        double start = session.StepStartTime;
        yield return runner.Until(() => session.Step > 10, 30f, "the step ends at its limit");
        float limit = config.script.Length(10) + config.script.Window(10, Pace.Quick) + config.script.Gap(10, Pace.Quick);
        runner.Near((float)(session.StepStartTime - start), limit, 1.5f, "the limit of the ratings");
        clock.TestSpeed = 1f;
        yield return null;
        runner.Check(!stage.Flow.IsOpen, "an open panel closes at the limit");
        yield return JumpTo(12);
        yield return SplitTask(79, "accepts");
    }

    private static IEnumerator PauseAndRepeat()
    {
        runner.Group("Pause and repeat", 60f);
        session.Begin(Experience.FullSession, Director.Automatic, Pace.Quick);
        yield return runner.Until(() => session.Step == 0 && director.View.time > 1.0, 10f, "a new run");
        yield return Click("hud/pause");
        float time = (float)director.View.time;
        yield return runner.Wait(0.6f);
        Shot("session_paused");
        yield return runner.Wait(4.4f);
        runner.Near((float)director.View.time, time, 0.15f, "a pause stops the step");
        yield return Click("hud/pause");
        yield return runner.Wait(1f);
        runner.Near((float)director.View.time, time + 1f, 0.3f, "the step goes on after the pause");
        int repeats = session.RepeatCount;
        session.Repeat(clock.Now + SessionDirector.EarlyPause);
        yield return runner.Wait(1f);
        runner.Check(session.RepeatCount == repeats + 1 && director.View.time < 1.0, "a repeat starts the step again");

        yield return runner.Until(() => director.View.phase == StepPhase.Response, 15f, "response time");
        yield return runner.Wait(0.3f);
        CheckLayout("hud");
        Shot("session_01_response");
        double start = session.StepStartTime;
        yield return Click("hud/done");
        yield return runner.Until(() => session.Step == 1, 5f, "the step ends early when everybody is done");
        runner.Check(session.StepStartTime - start < config.script.Length(0) + 3.6f, "the early end is early", (session.StepStartTime - start).ToString("0.0"));
    }

    /// <summary>Answers of earlier sessions can also be deleted without entering a room.</summary>
    private static IEnumerator AnswersInTheMenu()
    {
        runner.Group("Answers in the menu", 60f);
        runner.Check(AnswerStore.HasAnswers, "the answers of the last session are on the device");
        yield return GoToFirstPage();
        for (int i = 0; i < 2; i++) yield return Click("start/next");
        yield return runner.Wait(0.4f);
        CheckLayout("start");
        Shot("menu_3_experience_answers");
        yield return Click("start/delete-answers");
        runner.Check(!AnswerStore.HasAnswers && !DemoUi.Find("start/delete-answers").CanClick, "the answers are gone and so is the button");
        yield return GoToFirstPage();
    }

    /// <summary>Goes to a step as if the steps before had ended.</summary>
    private static IEnumerator JumpTo(int step)
    {
        for (int k = session.Step; k < step; k++) session.AdvanceFrom(k, clock.Now);
        yield return null;
        yield return null;
        runner.Check(session.Step == step, "at step " + (step + 1));
    }
}
