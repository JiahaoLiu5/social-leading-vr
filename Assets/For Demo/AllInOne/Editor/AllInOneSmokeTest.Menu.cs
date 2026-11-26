using System.Collections;
using DemoKit;
using SocialLeading.AllInOne;
using UnityEngine;

/// <summary>Checks of the start panel and the preview.</summary>
public static partial class AllInOneSmokeTest
{
    private static AvatarBody PreviewBody => Object.FindObjectOfType<PoseFeed>(true).GetComponent<AvatarBody>();

    private static IEnumerator MenuChecks()
    {
        runner.Group("Menu", 120f);
        runner.Check(mirrorGate.InView && mirror.enabled, "the mirror is alive beside the start panel");
        string[] pages = { "avatar", "role", "experience", "join" };
        yield return GoToFirstPage();
        for (int page = 0; page < pages.Length; page++)
        {
            yield return runner.Wait(0.6f);
            CheckLayout("start");
            Shot("menu_" + (page + 1) + "_" + pages[page]);
            if (page == 1) runner.Check(DemoUi.Get<DemoLabel>("start/note").Text == config.roleNote, "the note about the study is on the role page");
            if (page == 2) yield return ExperiencePageCheck();
            if (page < pages.Length - 1) yield return Click("start/next");
        }
        runner.Check(DemoUi.Find("start/join-online").Interactable == connector.HasKey, "joining online needs a key");
        yield return Click("start/join-operator");
        yield return runner.Wait(0.4f);
        CheckLayout("start");
        Shot("menu_5_operator");
        yield return Click("start/operator-back");

        yield return GoToFirstPage();
        for (int k = 1; k <= config.avatars.Count; k++) yield return PreviewCheck(k);
        yield return Click("start/avatar-1");
        yield return runner.Wait(0.5f);
    }

    private static IEnumerator ExperiencePageCheck()
    {
        runner.Check(DemoUi.Find("start/exp-free").Selected && DemoUi.Find("start/director-auto").Selected && DemoUi.Find("start/pace-study").Selected,
            "free play, automatic, and study timing are preselected");
        runner.Check(!DemoUi.Find("start/director-auto").Interactable, "the session options belong to a full session");
        yield return Click("start/exp-full");
        runner.Check(DemoUi.Find("start/director-auto").Interactable, "a full session has options");
        yield return runner.Wait(0.4f);
        Shot("menu_3_experience_full");
        yield return Click("start/exp-free");
    }

    private static IEnumerator PreviewCheck(int k)
    {
        AvatarBody body = PreviewBody;
        yield return Click("start/avatar-" + k);
        yield return runner.Until(() => CharacterOf(body) == "avatar-" + k, 2f, "the preview wears avatar " + k);
        yield return runner.Wait(1.2f);
        runner.Check(body.GetComponentsInChildren<IKTargetFollowVRRig>(true).Length == 1, "one character on the preview");
        int shown = 0;
        foreach (SkinnedMeshRenderer r in body.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (r.enabled) shown++;
        runner.Check(shown == 6 || shown == 7, "all parts of avatar " + k + " are visible", shown.ToString());
        foreach (IKFootSolver foot in body.GetComponentsInChildren<IKFootSolver>(true))
            runner.Check(foot.transform.position.y < 0.25f, "feet of avatar " + k + " are on the floor", foot.transform.position.y.ToString("0.00"));

        Transform head = body.dresser.CurrentAnimator.GetBoneTransform(HumanBodyBones.Head);
        // The animated root moves the character a little after the rig was solved, as in the study.
        runner.Near(head.position, body.headOut.position, 0.08f, "head of avatar " + k + " is at the displayed head");
        Quaternion level = head.rotation;
        desktop.Body.Pitch += 20f;
        yield return runner.Wait(0.3f);
        runner.Near(Quaternion.Angle(level, head.rotation), 20f, 4f, "head of avatar " + k + " nods with the user");
        desktop.Body.Pitch -= 20f;
        yield return runner.Wait(0.3f);
        Shot("preview_" + k);
    }
}
