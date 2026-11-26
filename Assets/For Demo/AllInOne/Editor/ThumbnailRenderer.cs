using SocialLeading.AllInOne;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Renders the avatar pictures of the start panel from live bodies, because a character only looks right while its rig runs.
/// </summary>
[InitializeOnLoad]
public static class ThumbnailRenderer
{
    private const string RunningKey = "AllInOne.Thumbnails";
    private const int Size = 512;
    private const int SettleFrames = 90;
    private static readonly Color Paper = new Color32(0xF1, 0xEC, 0xE4, 0xFF);

    private static AvatarBody body;
    private static Camera camera;
    private static int avatar = -1, frames, lastFrame = -1;

    static ThumbnailRenderer()
    {
        if (UnityEditor.SessionState.GetBool(RunningKey, false)) EditorApplication.update += Tick;
    }

    [MenuItem("Social Leading/All-in-One/Render Thumbnails")]
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        UnityEditor.SessionState.SetBool(RunningKey, true);
        EditorApplication.update += Tick;
        EditorApplication.isPlaying = true;
    }

    private static string PathOf(int index) => AllInOneBuilder.Root + "Content/Thumbnails/avatar-" + (index + 1) + ".png";

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        if (body == null) Prepare();
        if (frames-- > 0) return;

        var config = AssetDatabase.LoadAssetAtPath<AppConfig>(AllInOneBuilder.Root + "Content/App Config.asset");
        if (avatar >= 0) ViewCapture.Save(camera, Size, Size, PathOf(avatar));
        avatar++;
        if (avatar < config.avatars.Count)
        {
            body.SetAvatar(avatar);
            frames = SettleFrames;
            return;
        }
        Finish(config.avatars.Count);
    }

    private static void Prepare()
    {
        Time.captureFramerate = 30;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.78f, 0.77f, 0.76f);
        var light = new GameObject("Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.transform.rotation = Quaternion.Euler(28f, 200f, 0f);
        var floor = new GameObject("Floor").AddComponent<BoxCollider>();
        floor.center = new Vector3(0f, -0.05f, 0f);
        floor.size = new Vector3(6f, 0.1f, 6f);

        var bot = AssetDatabase.LoadAssetAtPath<GameObject>(AllInOneBuilder.Root + "Prefabs/Simulated Partner.prefab");
        GameObject person = Object.Instantiate(bot);
        Object.Destroy(person.GetComponent<BotPartner>());
        person.GetComponent<SimulatedRig>().motion = SimulatedRig.MotionKind.Idle;
        body = person.GetComponent<AvatarBody>();

        camera = new GameObject("Camera").AddComponent<Camera>();
        camera.enabled = false;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Paper;
        camera.fieldOfView = 25f;
        camera.transform.position = new Vector3(0.36f, 1.55f, 1.80f);
        camera.transform.LookAt(new Vector3(0f, 1.42f, 0f));
    }

    private static void Finish(int count)
    {
        UnityEditor.SessionState.SetBool(RunningKey, false);
        EditorApplication.update -= Tick;
        Time.captureFramerate = 0;
        EditorApplication.isPlaying = false;
        AssetDatabase.Refresh();
        for (int i = 0; i < count; i++) Import(PathOf(i));
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    private static void Import(string path)
    {
        AssetDatabase.ImportAsset(path);
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
        importer.maxTextureSize = Size;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }
}
