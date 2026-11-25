using UnityEngine;

/// <summary>
/// One movement-coupling design as an asset. Create with Assets > Create > Social Leading > Modulation Profile,
/// then assign it to a <see cref="ModulationProfileApplier"/> on an avatar root.
/// </summary>
[CreateAssetMenu(menuName = "Social Leading/Modulation Profile", fileName = "ModulationProfile")]
public class ModulationProfile : ScriptableObject
{
    public enum HandMode { Off, Blend, Superimpose }
    public enum HeadMode { Off, Superimpose, Blend }

    [Header("Hands")]
    public HandMode hands = HandMode.Blend;
    [Range(0f, 1f)] public float handRatio = 0.3f;
    public Vector3 handAxisRatio = Vector3.one;
    public bool leftHand = true;
    public bool rightHand = true;

    [Header("Position")]
    public HeadMode position = HeadMode.Off;
    [Range(0f, 1f)] public float positionRatio = 0.3f;
    [Tooltip("Comfort limit in metres for the superimposed offset. 0 means unlimited.")]
    [Min(0f)] public float maxPositionOffset = 0f;

    [Header("Smoothing")]
    [Tooltip("Time constant of the low-pass filter on the partner's hands, in seconds.")]
    [Min(0f)] public float partnerSmoothing = 0.2f;
}
