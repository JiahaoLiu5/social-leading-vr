using UnityEngine;

/// <summary>Writes a <see cref="ModulationProfile"/> into every modulator under this avatar. Edits apply live.</summary>
[DefaultExecutionOrder(-70)]
public class ModulationProfileApplier : MonoBehaviour
{
    public ModulationProfile profile;

    private void Update() => Apply();

    public void Apply()
    {
        if (profile == null) return;

        foreach (var hand in GetComponentsInChildren<HandMixedFollowSystem>(true))
        {
            bool on = IsLeft(hand.name) ? profile.leftHand : profile.rightHand;
            hand.enabled = profile.hands != ModulationProfile.HandMode.Superimpose;
            hand.followRatio = on && profile.hands == ModulationProfile.HandMode.Blend ? profile.handRatio : 0f;
            hand.axisRatio = profile.handAxisRatio;
        }
        foreach (var hand in GetComponentsInChildren<HandAddedFollowSystem>(true))
        {
            bool on = IsLeft(hand.name) ? profile.leftHand : profile.rightHand;
            hand.enabled = profile.hands == ModulationProfile.HandMode.Superimpose;
            hand.addRatio = on ? profile.handRatio : 0f;
        }
        foreach (var head in GetComponentsInChildren<HeadAddedFollowSystem>(true))
        {
            head.enabled = profile.position != ModulationProfile.HeadMode.Blend;
            head.addRatio = profile.position == ModulationProfile.HeadMode.Superimpose ? profile.positionRatio : 0f;
            head.maxOffset = profile.maxPositionOffset;
        }
        foreach (var head in GetComponentsInChildren<HeadMixedFollowSystem>(true))
        {
            head.enabled = profile.position == ModulationProfile.HeadMode.Blend;
            head.followRatio = profile.positionRatio;
        }
        foreach (var slow in GetComponentsInChildren<SlowFollowSystem>(true))
            if (slow.name.Contains("Partner Slow")) slow.lerpTime = profile.partnerSmoothing;
    }

    private static bool IsLeft(string objectName) => objectName.StartsWith("Left");
}
