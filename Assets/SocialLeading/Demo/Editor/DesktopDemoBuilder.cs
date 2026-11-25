using Normal.Realtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Rebuilds the desktop demo scene. The avatars keep the hierarchy, modulators, and IK of the networked prefabs;
/// the network components are replaced by a simulated rig.
/// </summary>
public static class DesktopDemoBuilder
{
    private const string Root = "Assets/SocialLeading/";
    private const string ScenePath = Root + "Demo/DesktopDemo.unity";
    private const string LeaderPrefab = Root + "Prefabs/Network/Avatars/P3/Resources/VR Player Complete Prefab Leader P3.prefab";
    private const string FollowerPrefab = Root + "Prefabs/Network/Avatars/P4/Resources/VR Player Complete Prefab Follower P4.prefab";

    [MenuItem("Social Leading/Rebuild Desktop Demo")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(Root + "Scenes/Monitor/Monitor.unity", OpenSceneMode.Single);

        // Keep only the room.
        foreach (GameObject go in scene.GetRootGameObjects())
            if (!go.GetComponentInChildren<MeshRenderer>(true) || go.GetComponentInChildren<Canvas>(true) || go.GetComponentInChildren<Realtime>(true) || go.name.StartsWith("XR ") || go.GetComponent<Camera>())
                Object.DestroyImmediate(go);

        var fill = new GameObject("Fill Light").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.75f;
        fill.color = new Color(1f, 0.95f, 0.88f);
        fill.transform.rotation = Quaternion.Euler(28f, -78f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.46f, 0.44f, 0.42f);

        GameObject leader = OfflineAvatar(LeaderPrefab);
        GameObject follower = OfflineAvatar(FollowerPrefab);
        Place(leader, "Leader", new Vector3(0f, 0f, -0.85f), 0f);
        Place(follower, "Follower", new Vector3(0f, 0f, 0.85f), 180f);
        SimulatedRig leaderRig = AddRig(leader, SimulatedRig.MotionKind.RaiseHands, 0f);
        SimulatedRig followerRig = AddRig(follower, SimulatedRig.MotionKind.Idle, 1.7f);
        leader.GetComponent<GetPartnerTransform>().linkEnabled = false;

        var profile = AssetDatabase.LoadAssetAtPath<ModulationProfile>(Root + "Demo/DemoProfile.asset");
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<ModulationProfile>();
            AssetDatabase.CreateAsset(profile, Root + "Demo/DemoProfile.asset");
        }
        follower.AddComponent<ModulationProfileApplier>().profile = profile;

        var ghostMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Demo/Ghost.mat");
        if (ghostMaterial == null)
        {
            ghostMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.95f, 0.42f, 0.28f) };
            AssetDatabase.CreateAsset(ghostMaterial, Root + "Demo/Ghost.mat");
        }
        RawPoseGhost ghost = follower.AddComponent<RawPoseGhost>();
        ghost.rawHead = follower.transform.Find("Head");
        ghost.rawLeft = follower.transform.Find("Left Hand");
        ghost.rawRight = follower.transform.Find("Right Hand");
        ghost.renderedHead = follower.transform.Find("Head Modulated");
        ghost.renderedLeft = follower.transform.Find("Left Hand Modulated");
        ghost.renderedRight = follower.transform.Find("Right Hand Modulated");
        ghost.ghostMaterial = ghostMaterial;

        var camera = new GameObject("Demo Camera", typeof(Camera), typeof(AudioListener), typeof(OrbitCamera)) { tag = "MainCamera" };
        camera.GetComponent<Camera>().fieldOfView = 45f;
        camera.GetComponent<Camera>().nearClipPlane = 0.05f;
        camera.GetComponent<OrbitCamera>().distance = 2.75f;
        camera.GetComponent<OrbitCamera>().maxDistance = 2.75f;

        var controller = new GameObject("Desktop Demo").AddComponent<DesktopDemoController>();
        controller.leaderRig = leaderRig;
        controller.followerRig = followerRig;
        controller.followerLink = follower.GetComponent<GetPartnerTransform>();
        controller.profile = profile;
        controller.ghost = ghost;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Desktop demo rebuilt: " + ScenePath);
    }

    /// <summary>The networked avatar prefab without its network components.</summary>
    private static GameObject OfflineAvatar(string path)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        foreach (var c in go.GetComponentsInChildren<SetCameraParent>(true)) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<RealtimeAvatarVoice>(true)) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<RealtimeAvatar>(true)) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<RealtimeTransform>(true)) Object.DestroyImmediate(c);
        foreach (var c in go.GetComponentsInChildren<RealtimeView>(true)) Object.DestroyImmediate(c);
        return go;
    }

    private static void Place(GameObject go, string name, Vector3 position, float yaw)
    {
        go.name = name;
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
    }

    private static SimulatedRig AddRig(GameObject avatar, SimulatedRig.MotionKind motion, float phase)
    {
        var rig = avatar.AddComponent<SimulatedRig>();
        rig.head = avatar.transform.Find("Head");
        rig.leftHand = avatar.transform.Find("Left Hand");
        rig.rightHand = avatar.transform.Find("Right Hand");
        rig.motion = motion;
        rig.phase = phase;
        rig.eyeHeight = 1.8f;
        // Wrist calibration of the controller rig.
        rig.leftWristOffset = new Quaternion(0.53567934f, 0.46157086f, 0.53567934f, 0.46157086f);
        rig.rightWristOffset = new Quaternion(-0.53567934f, 0.4615709f, 0.53567934f, -0.4615709f);
        return rig;
    }
}
