using UnityEngine;

/// <summary>On-screen controls for the desktop demo. No headset, network, or account needed.</summary>
public class DesktopDemoController : MonoBehaviour
{
    public SimulatedRig leaderRig;
    public SimulatedRig followerRig;
    public GetPartnerTransform followerLink;
    public ModulationProfile profile;
    public RawPoseGhost ghost;
    public bool showPanel = true;

    private static readonly string[] HandModes = { "Off", "Blend", "Superimpose" };
    private static readonly string[] HeadModes = { "Off", "Superimpose", "Blend" };
    private static readonly string[] Motions = { "Idle", "Wave", "Raise", "Talk", "Reach", "Step" };
    private ModulationProfile live;

    private void Start()
    {
        // Work on a copy so that play-mode edits never change the asset.
        live = Instantiate(profile);
        foreach (var applier in FindObjectsOfType<ModulationProfileApplier>()) applier.profile = live;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) showPanel = !showPanel;
        if (Input.GetKeyDown(KeyCode.Space) && followerLink != null) followerLink.linkEnabled = !followerLink.linkEnabled;
    }

    private void OnGUI()
    {
        if (!showPanel || live == null) return;
        GUILayout.BeginArea(new Rect(16, 16, 330, 560), GUI.skin.box);
        GUILayout.Label("Social Leading: desktop demo");
        followerLink.linkEnabled = GUILayout.Toggle(followerLink.linkEnabled, " Link on (Space)");
        if (ghost != null) ghost.visible = GUILayout.Toggle(ghost.visible, " Show tracked hands");

        GUILayout.Space(6);
        GUILayout.Label("Hands");
        live.hands = (ModulationProfile.HandMode)GUILayout.Toolbar((int)live.hands, HandModes);
        live.handRatio = Slider("Ratio", live.handRatio, 0f, 1f);
        live.partnerSmoothing = Slider("Smoothing (s)", live.partnerSmoothing, 0f, 1f);
        GUILayout.BeginHorizontal();
        live.leftHand = GUILayout.Toggle(live.leftHand, " Left");
        live.rightHand = GUILayout.Toggle(live.rightHand, " Right");
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.Label("Position");
        live.position = (ModulationProfile.HeadMode)GUILayout.Toolbar((int)live.position, HeadModes);
        live.positionRatio = Slider("Ratio", live.positionRatio, 0f, 1f);

        GUILayout.Space(6);
        GUILayout.Label("Leader motion");
        leaderRig.motion = (SimulatedRig.MotionKind)GUILayout.SelectionGrid((int)leaderRig.motion, Motions, 3);
        GUILayout.Label("Follower motion");
        followerRig.motion = (SimulatedRig.MotionKind)GUILayout.SelectionGrid((int)followerRig.motion, Motions, 3);
        GUILayout.Space(6);
        GUILayout.Label("Right-drag: orbit   Scroll: zoom   Tab: hide panel");
        GUILayout.EndArea();
    }

    private static float Slider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:0.00}", GUILayout.Width(130));
        value = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.EndHorizontal();
        return value;
    }
}
