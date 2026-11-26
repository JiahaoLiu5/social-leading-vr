using System;
using UnityEngine;

/// <summary>Writes a <see cref="ModulationProfile"/> into every modulator under this avatar. Edits apply live.</summary>
[DefaultExecutionOrder(-70)]
public class ModulationProfileApplier : MonoBehaviour
{
    public ModulationProfile profile;

    // Found once: an avatar never changes its modulators, and names are only read here because reading them allocates.
    private HandMixedFollowSystem[] mixedHands;
    private HandAddedFollowSystem[] addedHands;
    private bool[] mixedIsLeft, addedIsLeft;
    private HeadAddedFollowSystem[] addedHeads;
    private HeadMixedFollowSystem[] mixedHeads;
    private SlowFollowSystem[] partnerFilters;

    private void OnEnable()
    {
        mixedHands = GetComponentsInChildren<HandMixedFollowSystem>(true);
        addedHands = GetComponentsInChildren<HandAddedFollowSystem>(true);
        mixedIsLeft = Array.ConvertAll(mixedHands, IsLeft);
        addedIsLeft = Array.ConvertAll(addedHands, IsLeft);
        addedHeads = GetComponentsInChildren<HeadAddedFollowSystem>(true);
        mixedHeads = GetComponentsInChildren<HeadMixedFollowSystem>(true);
        partnerFilters = Array.FindAll(GetComponentsInChildren<SlowFollowSystem>(true), slow => slow.name.Contains("Partner Slow"));
    }

    private void Update() => Apply();

    public void Apply()
    {
        if (profile == null || mixedHands == null) return;

        for (int i = 0; i < mixedHands.Length; i++)
        {
            bool on = mixedIsLeft[i] ? profile.leftHand : profile.rightHand;
            mixedHands[i].enabled = profile.hands != ModulationProfile.HandMode.Superimpose;
            mixedHands[i].followRatio = on && profile.hands == ModulationProfile.HandMode.Blend ? profile.handRatio : 0f;
            mixedHands[i].axisRatio = profile.handAxisRatio;
        }
        for (int i = 0; i < addedHands.Length; i++)
        {
            bool on = addedIsLeft[i] ? profile.leftHand : profile.rightHand;
            addedHands[i].enabled = profile.hands == ModulationProfile.HandMode.Superimpose;
            addedHands[i].addRatio = on ? profile.handRatio : 0f;
        }
        foreach (var head in addedHeads)
        {
            head.enabled = profile.position != ModulationProfile.HeadMode.Blend;
            head.addRatio = profile.position == ModulationProfile.HeadMode.Superimpose ? profile.positionRatio : 0f;
            head.maxOffset = profile.maxPositionOffset;
        }
        foreach (var head in mixedHeads)
        {
            head.enabled = profile.position == ModulationProfile.HeadMode.Blend;
            head.followRatio = profile.positionRatio;
        }
        foreach (var filter in partnerFilters) filter.lerpTime = profile.partnerSmoothing;
    }

    private static bool IsLeft(Component hand) => hand.name.StartsWith("Left");
}
