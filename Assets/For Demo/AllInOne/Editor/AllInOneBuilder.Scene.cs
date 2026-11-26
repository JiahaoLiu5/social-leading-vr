using Normal.Realtime;
using SocialLeading.AllInOne;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using SessionState = SocialLeading.AllInOne.SessionState;

/// <summary>The scene of the integrated demo.</summary>
public static partial class AllInOneBuilder
{
    /// <summary>Constant id of the scene view of the session, so that every build finds the same model in a room.</summary>
    public const string SessionViewId = "5d1c6a52-7f0e-4e0b-9a46-2b6f0c3a1d01";

    /// <summary>Everything in the scene that other objects refer to.</summary>
    private sealed class Parts
    {
        public AppConfig config;
        public Transform room;
        public Realtime realtime;
        public RealtimeAvatarManager avatars;
        public RoomConnector connector;
        public Roster roster;
        public SessionClock clock;
        public SessionState session;
        public SessionDirector director;
        public Narrator narrator;
        public RigMode mode;
        public Seating seating;
        public StickMove stick;
        public DesktopRig desktop;
        public AppInput input;
        public Transform rig, recentre, trackingSpace, trackedHead, headTarget, left, right, leftTarget, rightTarget;
        public Camera trackingCamera;
        public ViewRig view;
        public MirrorGate mirror;
        public Camera viewCamera;
        public DyadStage stage;
        public AvatarBody preview;
        public SoloPartners solo;
    }

    private static void BuildScene(AppConfig config)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var parts = new Parts { config = config };
        AddRoom(parts, scene);
        SetLighting();
        AddApp(parts, out GameObject app);
        AddRig(parts);
        AddNetworking(parts);
        AddSession(parts);
        AddView(parts);
        AddStage(parts);
        AddPeople(parts);
        Wire(parts, app);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    // Room ----------------------------------------------------------------------------------------------------

    /// <summary>Takes the room with its lights and the mirror from the desktop demo.</summary>
    private static void AddRoom(Parts parts, Scene scene)
    {
        Scene source = EditorSceneManager.OpenScene(DemoRoot + "Scenes/DesktopDemo.unity", OpenSceneMode.Additive);
        GameObject room = null;
        foreach (GameObject candidate in source.GetRootGameObjects())
            if (candidate.name == "WhiteRoomNew") room = candidate;
        Assert(room != null, "the desktop demo has no WhiteRoomNew");
        SceneManager.MoveGameObjectToScene(room, scene);
        EditorSceneManager.CloseScene(source, true);
        SceneManager.SetActiveScene(scene);
        room.name = "Room";
        parts.room = room.transform;
    }

    private static void SetLighting()
    {
        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        // The study scenes have no ambient light without baked data. Fixed colours and a fill light keep faces readable.
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.50f, 0.49f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.46f, 0.45f, 0.44f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.28f, 0.26f);
        // No sky reflections, so that the room looks the same in the Editor and in players.
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = null;
        RenderSettings.fog = false;

        // Only for the sky behind the windows; it lights nothing.
        var sun = new GameObject("Sky Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.34f;
        sun.cullingMask = 0;
        sun.shadows = LightShadows.None;
        sun.renderMode = LightRenderMode.ForcePixel;
        sun.transform.rotation = Quaternion.Euler(24f, 15f, 0f);
        RenderSettings.sun = sun;

        // One for faces at each seat, one for faces at the mirror.
        foreach (float yaw in new[] { 15f, 165f, -80f })
        {
            var fill = new GameObject("Fill Light").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.62f;
            fill.color = new Color(1f, 0.97f, 0.93f);
            fill.shadows = LightShadows.None;
            // Diffuse light only: a fill light must not put white sparkles on hair, and it costs no extra pass.
            fill.renderMode = LightRenderMode.ForceVertex;
            fill.transform.rotation = Quaternion.Euler(24f, yaw, 0f);
        }
    }

    // User ----------------------------------------------------------------------------------------------------

    private static void AddRig(Parts parts)
    {
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(DemoRoot + "Prefabs/Rig/XR Origin (XR Rig) 1.prefab"));
        PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        rig.name = "Rig";
        rig.transform.SetPositionAndRotation(parts.config.lobbyPosition, Quaternion.Euler(0f, parts.config.lobbyYaw, 0f));
        parts.rig = rig.transform;
        parts.trackingSpace = Child(rig.transform, "Camera Offset");
        parts.trackingSpace.localPosition = new Vector3(0f, parts.config.eyeHeight, 0f);
        rig.GetComponent<XROrigin>().CameraYOffset = parts.config.eyeHeight;

        // Takes back where the user stands and looks, so that the rig root keeps the exact seat pose.
        parts.recentre = new GameObject("Recentre").transform;
        parts.recentre.SetParent(rig.transform, false);
        parts.trackingSpace.SetParent(parts.recentre, false);

        parts.trackedHead = Child(parts.trackingSpace, "Main Camera");
        parts.trackingCamera = parts.trackedHead.GetComponent<Camera>();
        parts.trackingCamera.enabled = false;
        parts.trackedHead.GetComponent<AudioListener>().enabled = false;
        parts.headTarget = Child(parts.trackedHead, "Head VR Target");
        parts.left = Child(parts.trackingSpace, "Left Controller");
        parts.right = Child(parts.trackingSpace, "Right Controller");
        parts.leftTarget = Child(parts.left, "Left Hand VR Target");
        parts.rightTarget = Child(parts.right, "Right Hand VR Target");
        parts.mode = rig.AddComponent<RigMode>();
        parts.seating = rig.AddComponent<Seating>();
        parts.stick = rig.AddComponent<StickMove>();
        parts.desktop = rig.AddComponent<DesktopRig>();
    }

    private static void AddView(Parts parts)
    {
        var root = new GameObject("View Rig");
        var camera = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(SharedRoot + "Prefabs/Rig/Camera Local Object.prefab"), root.transform);
        camera.name = "View Camera";
        camera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        camera.AddComponent<AudioListener>();
        parts.viewCamera = camera.GetComponent<Camera>();
        parts.viewCamera.nearClipPlane = 0.1f;
        parts.viewCamera.farClipPlane = 100f;
        parts.viewCamera.allowMSAA = true;
        parts.viewCamera.clearFlags = CameraClearFlags.Skybox;
        parts.view = root.AddComponent<ViewRig>();
        Set(parts.view,
            ("viewCamera", parts.viewCamera), ("trackingSpace", parts.trackingSpace), ("trackedHead", parts.trackedHead), ("mode", parts.mode),
            ("leftController", parts.left), ("rightController", parts.right),
            ("leftPointer", new GameObject("Pointer Left").transform), ("rightPointer", new GameObject("Pointer Right").transform));

        Transform mirror = Child(parts.room, "VR Mirror New");
        parts.mirror = mirror.gameObject.AddComponent<MirrorGate>();
        Set(parts.mirror, ("viewCamera", parts.viewCamera), ("mirrorCamera", mirror.GetComponentInChildren<Camera>(true)),
            ("surface", Child(mirror, "Quad").GetComponent<Renderer>()));
    }

    // Network -------------------------------------------------------------------------------------------------

    private static void AddNetworking(Parts parts)
    {
        var networking = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(DemoRoot + "Prefabs/Network/Realtime + VR Player Complete Prefab 2.prefab"));
        networking.name = "Networking";
        parts.realtime = networking.GetComponent<Realtime>();
        parts.avatars = networking.GetComponent<RealtimeAvatarManager>();
        Set(parts.realtime, ("_joinRoomOnStart", false));
        Set(parts.avatars,
            ("_localAvatarPrefab", Load<GameObject>(PlayerPath)), ("_localPlayer.root", parts.rig), ("_localPlayer.head", parts.headTarget),
            ("_localPlayer.leftHand", parts.leftTarget), ("_localPlayer.rightHand", parts.rightTarget));
        parts.connector = networking.AddComponent<RoomConnector>();
        parts.roster = networking.AddComponent<Roster>();
        parts.clock = networking.AddComponent<SessionClock>();
        Set(parts.connector, ("config", parts.config), ("realtime", parts.realtime), ("avatars", parts.avatars));
        Set(parts.roster, ("config", parts.config));
        Set(parts.clock, ("realtime", parts.realtime));
    }

    private static void AddSession(Parts parts)
    {
        var session = new GameObject("Session");
        parts.session = session.AddComponent<SessionState>();
        RealtimeView view = session.GetComponent<RealtimeView>();
        if (view == null) view = session.AddComponent<RealtimeView>();
        parts.director = session.AddComponent<SessionDirector>();
        var wiring = session.AddComponent<LinkWiring>();
        Set(view,
            ("_realtime", parts.realtime), ("_viewUUID", SessionViewId), ("_sceneViewPreventOwnershipTakeover", false),
            ("_sceneViewDestroyWhenOwnerOrLastClientLeaves", true), ("_sceneViewDestroyWhenOwnerOrLastClientLeavesMigrated", true),
            ("_sceneViewDestroyWhenLastClientLeaves", true));
        RebuildRealtimeGraph(view);
        Set(parts.session, ("config", parts.config), ("roster", parts.roster), ("clock", parts.clock));
        Set(parts.director, ("config", parts.config), ("session", parts.session), ("clock", parts.clock), ("roster", parts.roster), ("connector", parts.connector));
        Set(wiring, ("config", parts.config), ("roster", parts.roster), ("session", parts.session));

        var narrator = new GameObject("Narrator");
        narrator.transform.position = new Vector3(0f, 2f, 0f);
        var source = narrator.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        parts.narrator = narrator.AddComponent<Narrator>();
        Set(parts.narrator, ("config", parts.config), ("session", parts.session), ("director", parts.director), ("clock", parts.clock), ("connector", parts.connector));
    }

    // Study ---------------------------------------------------------------------------------------------------

    private static void AddStage(Parts parts)
    {
        var boards = new GameObject("Boards");
        GameObject board1 = AddBoard(boards, "Board 1", -2.013f, "Your Standpoint: Social Media Drives People Apart");
        GameObject board2 = AddBoard(boards, "Board 2", 1.866f, "Your Standpoint: Social Media Brings People Together");

        var panels = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(TaskPanelsPath));
        panels.name = "Task Panels";
        Transform root = panels.transform;

        var stage = new GameObject("Stage");
        var flow = stage.AddComponent<DyadTaskFlow>();
        Set(flow,
            ("config", Load<DyadTaskConfig>(Content + "Dyad Task Config.asset")), ("clock", parts.clock), ("input", parts.input), ("root", root),
            ("question1", Child(root, "Question 1").gameObject), ("question2", Child(root, "Question 2").gameObject),
            ("offer1", Child(root, "Offer 1").gameObject), ("offer2", Child(root, "Offer 2").gameObject),
            ("waiting", Child(root, "Waiting").gameObject), ("reply", Child(root, "Reply").gameObject),
            ("replyText", Child(Child(root, "Reply"), "RejOrAcc").GetComponent<TMP_Text>()), ("final", Child(root, "Final").gameObject));
        SetPanel(flow, "rating1", Child(root, "Rating 1"), true);
        SetPanel(flow, "rating2", Child(root, "Rating 2"), true);
        SetPanel(flow, "split1", Child(root, "Split 1"), false);
        SetPanel(flow, "split2", Child(root, "Split 2"), false);

        parts.stage = stage.AddComponent<DyadStage>();
        Set(parts.stage,
            ("config", parts.config), ("session", parts.session), ("roster", parts.roster), ("flow", flow),
            ("boards", new Object[] { board1, board2 }),
            ("standpoints", new Object[] { Child(board1.transform, "Text 4").gameObject, Child(board2.transform, "Text 4").gameObject }));
    }

    private static GameObject AddBoard(GameObject parent, string name, float z, string standpoint)
    {
        var board = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(DemoRoot + "UI/Prefabs/InstructionBoard.prefab"), parent.transform);
        board.name = name;
        board.transform.SetPositionAndRotation(new Vector3(-3.13f, 1.608f, z), Quaternion.Euler(0f, -90f, 0f));
        board.transform.localScale = Vector3.one * 0.0014982f;
        SetText(board, "Text 0", BoardHeading);
        // The empty line at the end moves the text up, as in the study.
        SetText(board, "Text 1", "The Impact of Social Media on Social Interaction\n");
        SetText(board, "Text 2", "\" Does social media bring people together\nor drive people further apart? \"");
        SetText(board, "Text 3", "\tSocial media is part of our daily lives, profoundly shaping how we communicate and interact with one another. Supporters argue that platforms like Facebook, Tiktok, and Instagram have brought people closer together, fostering global connections, facilitating instant communication, and enabling the sharing of diverse ideas and cultures. Critics, however, argue that social media has led to increased feelings of isolation, superficial relationships, and echo chambers where individuals are exposed primarily to viewpoints similar to their own.");
        SetText(board, "Text 4", standpoint);
        SetText(board, "Tips", "*Tips: When you are debating with your partner, combining verbal communication with\nnon-verbal movements and gestures can make your argument more compelling.");
        board.SetActive(false);
        return board;
    }

    private static void SetPanel(DyadTaskFlow flow, string field, Transform page, bool rating)
    {
        Set(flow, (field + ".root", page.gameObject), (field + ".slider", page.GetComponentInChildren<UnityEngine.UI.Slider>(true)));
        if (rating) Set(flow, (field + ".rating", page.GetComponentInChildren<RatingUI>(true)));
        else Set(flow, (field + ".split", page.GetComponentInChildren<SplitUI>(true)));
    }

    // People --------------------------------------------------------------------------------------------------

    private static void AddPeople(Parts parts)
    {
        var preview = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(BodyPath));
        preview.name = "Preview";
        var feed = preview.AddComponent<PoseFeed>();
        feed.root = parts.rig;
        feed.head = parts.headTarget;
        feed.leftHand = parts.leftTarget;
        feed.rightHand = parts.rightTarget;
        parts.preview = preview.GetComponent<AvatarBody>();

        parts.solo = new GameObject("Solo").AddComponent<SoloPartners>();
        Set(parts.solo,
            ("config", parts.config), ("session", parts.session), ("director", parts.director), ("roster", parts.roster), ("stage", parts.stage),
            ("prefab", Load<GameObject>(BotPath).GetComponent<BotPartner>()));
    }

    // App -----------------------------------------------------------------------------------------------------

    private static void AddApp(Parts parts, out GameObject app)
    {
        app = new GameObject("App");
        parts.input = app.AddComponent<AppInput>();
    }

    private static void Wire(Parts p, GameObject app)
    {
        Set(p.mode, ("origin", p.rig.GetComponent<XROrigin>()), ("trackingDriver", p.trackedHead.GetComponent<TrackedPoseDriver>()),
            ("leftController", p.left.GetComponent<ActionBasedController>()), ("rightController", p.right.GetComponent<ActionBasedController>()));
        Set(p.seating, ("config", p.config), ("mode", p.mode), ("desktop", p.desktop), ("recentre", p.recentre), ("trackedHead", p.trackedHead));
        Set(p.stick, ("config", p.config), ("mode", p.mode), ("input", p.input), ("recentre", p.recentre), ("trackedHead", p.trackedHead));
        Set(p.desktop, ("config", p.config), ("trackingCamera", p.trackingCamera), ("leftController", p.left), ("rightController", p.right));

        var main = app.AddComponent<AllInOneApp>();
        var start = app.AddComponent<StartPanel>();
        var lobby = app.AddComponent<LobbyPanel>();
        var hud = app.AddComponent<SessionHud>();
        var menu = app.AddComponent<RoomMenu>();
        var roomPage = app.AddComponent<RoomPanel>();
        var linkPage = app.AddComponent<LinkPanel>();
        var op = app.AddComponent<OperatorPanel>();
        var quality = app.AddComponent<QualityTuner>();
        var probe = app.AddComponent<DemoProbe>();
        var commandLine = app.AddComponent<DemoCommandLine>();

        Set(main, ("config", p.config), ("connector", p.connector), ("session", p.session), ("director", p.director), ("roster", p.roster),
            ("solo", p.solo), ("stage", p.stage), ("mode", p.mode), ("desktop", p.desktop), ("seating", p.seating), ("stick", p.stick),
            ("view", p.view), ("input", p.input), ("preview", p.preview), ("startPanel", start), ("lobby", lobby), ("hud", hud),
            ("menu", menu), ("operatorPanel", op));
        Set(start, ("config", p.config), ("app", main), ("connector", p.connector));
        Set(lobby, ("config", p.config), ("app", main), ("connector", p.connector), ("session", p.session), ("roster", p.roster));
        Set(hud, ("config", p.config), ("app", main), ("session", p.session), ("director", p.director), ("roster", p.roster),
            ("connector", p.connector), ("stage", p.stage), ("mode", p.mode), ("input", p.input));
        Set(p.narrator, ("stage", p.stage), ("menu", menu), ("operatorPanel", op));
        Set(menu, ("app", main), ("session", p.session), ("connector", p.connector), ("roster", p.roster), ("mode", p.mode), ("view", p.view),
            ("seating", p.seating), ("input", p.input), ("roomPage", roomPage), ("linkPage", linkPage));
        Set(roomPage, ("config", p.config), ("app", main), ("session", p.session), ("connector", p.connector), ("roster", p.roster));
        Set(linkPage, ("config", p.config), ("session", p.session));
        Set(op, ("config", p.config), ("app", main), ("session", p.session), ("director", p.director), ("clock", p.clock),
            ("connector", p.connector), ("roster", p.roster), ("narrator", p.narrator), ("view", p.view), ("mode", p.mode), ("input", p.input));
        Set(quality, ("roomLights", System.Array.ConvertAll(p.room.GetComponentsInChildren<Light>(true), l => (Object)l)));
        Set(probe, ("app", main), ("connector", p.connector), ("session", p.session), ("director", p.director), ("clock", p.clock),
            ("roster", p.roster), ("stage", p.stage), ("narrator", p.narrator), ("view", p.view));
        Set(commandLine, ("config", p.config), ("app", main), ("connector", p.connector), ("session", p.session), ("clock", p.clock),
            ("mode", p.mode), ("view", p.view), ("desktop", p.desktop), ("mirror", p.mirror), ("menu", menu), ("probe", probe));
    }
}
