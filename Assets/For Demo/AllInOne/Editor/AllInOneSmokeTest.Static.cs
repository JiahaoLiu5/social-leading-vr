using System.Collections.Generic;
using DemoKit;
using Normal.Realtime;
using SocialLeading.AllInOne;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using NetworkPlayer = SocialLeading.AllInOne.NetworkPlayer;

/// <summary>Checks that need no play time: rules, timing, scene, and prefabs.</summary>
public static partial class AllInOneSmokeTest
{
    private static readonly float[] ClipLengths = { 7.632f, 5.232f, 10.176f, 32.976f, 12.144f, 9.408f, 4.080f, 26.544f, 11.208f, 8.088f, 17.304f, 48.384f, 14.064f };
    private static readonly float[] StudyWindows = { 10f, 15f, 10f, 15f, 15f, 45f, 60f, 40f, 60f, 180f, 120f, 5f, 180f };
    private static readonly float[] QuickWindows = { 3f, 3f, 3f, 3f, 3f, 9f, 10f, 8f, 10f, 10f, 45f, 3f, 75f };

    private static void StaticChecks()
    {
        runner.Group("Rules");
        RoleRules();
        AuthorityRules();
        runner.Group("Timing");
        TimelineChecks();
        runner.Group("Scene and prefabs");
        SceneChecks();
        PrefabChecks();
    }

    private static Claim Person(int id, int wanted, string key = "", int connection = 1, bool participant = true, bool bot = false) =>
        new Claim { id = id, wanted = wanted, userKey = key, connection = connection, participant = participant, bot = bot };

    private static void RoleRules()
    {
        Dictionary<int, int> roles = RoleResolver.Resolve(new[] { Person(3, 0), Person(7, 1) }, 2);
        runner.Check(roles[3] == 0 && roles[7] == 1, "no conflict");

        roles = RoleResolver.Resolve(new[] { Person(7, 0), Person(3, 0) }, 2);
        runner.Check(roles[3] == 0 && roles[7] == 1, "the lower id keeps the role, whatever the order");

        roles = RoleResolver.Resolve(new[] { Person(2, 0, "a", 1), Person(9, 0, "a", 2) }, 2);
        runner.Check(!roles.ContainsKey(2) && roles[9] == 0, "a leftover of the same user gives up its role");

        roles = RoleResolver.Resolve(new[] { Person(5, 0, "a", 1), Person(1, 0, "a", 2) }, 2);
        runner.Check(!roles.ContainsKey(5) && roles[1] == 0, "the connection number counts, not the client id");

        roles = RoleResolver.Resolve(new[] { Person(1, 0), Person(2, 1), Person(3, 0) }, 2);
        runner.Check(roles[3] == -1, "a third person has no role");

        roles = RoleResolver.Resolve(new[] { Person(1, 0, "", 1, false), Person(2, 0) }, 2);
        runner.Check(!roles.ContainsKey(1) && roles[2] == 0, "operators take no role");
    }

    private static void AuthorityRules()
    {
        runner.Check(AuthorityElection.Elect(new[] { Person(4, 0), Person(0, 1) }) == 0, "the lowest id has the authority");
        runner.Check(AuthorityElection.Elect(new[] { Person(4, 0) }) == 4, "the next one takes over");
        runner.Check(AuthorityElection.Elect(new[] { Person(0, 0, "", 1, false), Person(1000, 0, "", 0, true, true), Person(6, 1) }) == 6,
            "operators, spectators, and simulated partners never have it");
        runner.Check(AuthorityElection.Elect(new[] { Person(1, 0, "a", 1), Person(8, 1, "a", 2) }) == 8, "a leftover never has it");

        var election = new AuthorityElection();
        bool first = election.MayAct(0, 4, 100.0);
        bool early = election.MayAct(4, 4, 100.5);
        bool stillEarly = election.MayAct(4, 4, 101.4);
        bool late = election.MayAct(4, 4, 101.6);
        runner.Check(!first && !early && !stillEarly && late, "a new authority waits one second");
    }

    private static void TimelineChecks()
    {
        StudyScript script = config.script;
        runner.Check(script.Count == 13, "13 steps");
        for (int i = 0; i < script.Count; i++)
        {
            StudyStep step = script.Get(i);
            runner.Check(step.clip != null && !string.IsNullOrEmpty(step.hint), "step " + (i + 1) + " is complete");
            CaptionChecks(step, i);
            runner.Near(script.Length(i), ClipLengths[i], 0.1f, "clip length of step " + (i + 1));
            runner.Near(script.Window(i, Pace.Study), StudyWindows[i], 0.01f, "study window of step " + (i + 1));
            runner.Near(script.Window(i, Pace.Quick), QuickWindows[i], 0.01f, "quick window of step " + (i + 1));
        }
        runner.Near(script.LongestDuration(Pace.Study), 979.24f, 1f, "longest study session");
        runner.Near(script.LongestDuration(Pace.Quick), 401.24f, 1f, "longest quick session");

        float length = script.Length(0), window = script.Window(0, Pace.Study);
        CheckPhase(-1, 0.0, 0.0, StepPhase.Waiting, "waiting");
        CheckPhase(-1, 100.0, 99.0, StepPhase.Countdown, "countdown");
        CheckPhase(0, 100.0, 99.0, StepPhase.Narration, "a start in the future");
        CheckPhase(0, 100.0, 100.0, StepPhase.Narration, "start of the clip");
        CheckPhase(0, 100.0, 100.0 + length - 0.01, StepPhase.Narration, "end of the clip");
        CheckPhase(0, 100.0, 100.0 + length + 0.01, StepPhase.Response, "start of the response time");
        CheckPhase(0, 100.0, 100.0 + length + window - 0.01, StepPhase.Response, "end of the response time");
        CheckPhase(0, 100.0, 100.0 + length + window + 0.01, StepPhase.Gap, "gap");
        CheckPhase(13, 100.0, 200.0, StepPhase.Finished, "finished");

        StepView paused = StepTimeline.Evaluate(script, Pace.Study, 0, 100.0, 500.0, true, 101.0);
        runner.Check(paused.phase == StepPhase.Narration && Mathf.Abs((float)paused.time - 1f) < 0.001f, "a pause stops the time");
        runner.Check(script.Gap(12, Pace.Study) == 0f, "no gap after the last step");
    }

    /// <summary>Captions are in order and end before the voice does.</summary>
    private static void CaptionChecks(StudyStep step, int index)
    {
        float last = -0.01f;
        bool good = step.captions.Length > 0;
        foreach (DemoCaption caption in step.captions)
        {
            good &= caption.start > last && !string.IsNullOrEmpty(caption.text);
            last = caption.start;
        }
        runner.Check(good && last < config.script.Length(index) - 0.8f, "captions of step " + (index + 1));
    }

    private static void CheckPhase(int step, double start, double now, StepPhase expected, string check)
    {
        StepView v = StepTimeline.Evaluate(config.script, Pace.Study, step, start, now, false, 0.0);
        runner.Check(v.phase == expected, "phase: " + check, v.phase.ToString());
    }

    private static void SceneChecks()
    {
        Realtime[] realtimes = Object.FindObjectsOfType<Realtime>(true);
        runner.Check(realtimes.Length == 1 && !realtimes[0].joinRoomOnStart, "one Realtime that does not join by itself");

        var view = session.GetComponent<RealtimeView>();
        var serialized = new SerializedObject(view);
        runner.Check(serialized.FindProperty("_viewUUID").stringValue == AllInOneBuilder.SessionViewId, "the session view has its constant id");
        runner.Check(serialized.FindProperty("_sceneViewDestroyWhenLastClientLeaves").intValue == 1
                     && serialized.FindProperty("_sceneViewDestroyWhenOwnerOrLastClientLeavesMigrated").intValue == 1, "the session ends with the last client");
        SerializedProperty components = serialized.FindProperty("_components");
        runner.Check(components.arraySize == 1 && components.GetArrayElementAtIndex(0).FindPropertyRelative("component").objectReferenceValue == session,
            "the session view lists the session state");

        int cameras = 0, listeners = 0, missing = 0, studyScripts = 0;
        foreach (Camera c in Object.FindObjectsOfType<Camera>())
            if (c.enabled && c.CompareTag("MainCamera")) cameras++;
        foreach (AudioListener l in Object.FindObjectsOfType<AudioListener>())
            if (l.enabled) listeners++;
        foreach (GameObject root in app.gameObject.scene.GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            foreach (MonoBehaviour b in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (b != null && b.GetType().Assembly.GetName().Name == "Assembly-CSharp") studyScripts++;
        }
        runner.Check(cameras == 1, "one main camera", cameras.ToString());
        runner.Check(listeners == 1, "one audio listener", listeners.ToString());
        runner.Check(missing == 0, "no missing scripts", missing.ToString());
        runner.Check(studyScripts == 0, "no study scripts in the scene", studyScripts.ToString());
        runner.Check(EditorBuildSettings.scenes[0].path == AllInOneBuilder.ScenePath && EditorBuildSettings.scenes[0].enabled, "the scene is the first in the build");
        int enabled = 0;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled) enabled++;
        runner.Check(enabled == 1, "all other scenes are listed and disabled", enabled.ToString());
    }

    private static void PrefabChecks()
    {
        var player = Resources.Load<GameObject>(AllInOneBuilder.PlayerName);
        runner.Check(player != null && player.GetComponent<NetworkPlayer>() != null, "the player prefab loads");
        runner.Check(!player.GetComponentInChildren<RealtimeAvatarVoice>(true).enabled, "its voice starts disabled");
        runner.Check(player.GetComponentInChildren<SetCameraParent>(true) == null, "it does not take the camera");

        var body = AssetDatabase.LoadAssetAtPath<GameObject>(AllInOneBuilder.Root + "Prefabs/Avatar Body.prefab");
        runner.Check(body.GetComponentInChildren<RealtimeView>(true) == null && body.GetComponentInChildren<RealtimeAvatar>(true) == null, "the offline body has no network parts");

        for (int k = 0; k < config.avatars.Count; k++)
        {
            AvatarCatalog.Entry entry = config.avatars.Get(k);
            var character = Resources.Load<GameObject>(entry.resourcePath);
            if (!runner.Check(character != null, entry.label + " loads")) continue;
            runner.Check(entry.thumbnail != null, entry.label + " has a picture");
            runner.Check(character.GetComponentsInChildren<IKTargetFollowVRRig>(true).Length == 1 && character.GetComponentsInChildren<RigBuilder>(true).Length == 1,
                entry.label + " has one rig");
            var head = character.GetComponentInChildren<MultiParentConstraint>(true);
            runner.Check(head.data.sourceObjects[0].transform != head.data.constrainedObject, entry.label + ": the head follows its target");
            foreach (IKFootSolver foot in character.GetComponentsInChildren<IKFootSolver>(true))
                runner.Check(new SerializedObject(foot).FindProperty("terrainLayer.m_Bits").intValue != 0, entry.label + ": the feet see the floor");
        }
    }
}
