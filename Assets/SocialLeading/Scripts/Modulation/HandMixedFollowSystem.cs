using UnityEngine;

/// <summary>
/// Blends this avatar's hand with the partner's corresponding hand.
///
///   own     = myHand - myHead
///   partner = Yaw(partner heading -> own heading) * (partnerHand - partnerHead)
///   hand*   = modulatedHead + Lerp(own, partner, followRatio)
///
/// With followRatio 0 the hand is untouched; with 1 it copies the partner completely.
/// Rotation always stays the user's own.
/// </summary>
[DefaultExecutionOrder(-45)]
public class HandMixedFollowSystem : MovementModulator
{
    public Transform myHead;
    public Transform myHand;
    public Transform partnerHead;
    public Transform partnerHand;
    public Transform modulatedHead;

    [Range(0f, 1f)]
    public float followRatio = 0.5f;

    [Header("Design options")]
    [Tooltip("Scales the ratio per axis, in this avatar's root space. (1,1,1) blends all axes equally.")]
    public Vector3 axisRatio = Vector3.one;

    [Tooltip("Reflect the partner's movement left-right, like facing a mirror.")]
    public bool mirror;

    [Tooltip("Partner hand to read while mirroring, usually the opposite hand. Falls back to Partner Hand.")]
    public Transform mirrorPartnerHand;

    private void Update()
    {
        if (myHead == null || myHand == null || modulatedHead == null) return;

        Vector3 own = myHand.localPosition - myHead.localPosition;
        Vector3 offset = own;

        if (LinkActive && partnerHead != null && partnerHand != null)
        {
            Transform sourceHand = mirror && mirrorPartnerHand != null ? mirrorPartnerHand : partnerHand;
            Vector3 partner = sourceHand.localPosition - partnerHead.localPosition;
            if (mirror)
            {
                Vector3 right = Quaternion.Euler(0f, partnerHead.localEulerAngles.y, 0f) * Vector3.right;
                partner -= 2f * Vector3.Dot(partner, right) * right;
            }
            partner = HeadingAlignment(partnerHead, modulatedHead) * partner;

            Vector3 weight = axisRatio * followRatio;
            offset = new Vector3(
                Mathf.Lerp(own.x, partner.x, weight.x),
                Mathf.Lerp(own.y, partner.y, weight.y),
                Mathf.Lerp(own.z, partner.z, weight.z));
        }

        transform.localPosition = offset + modulatedHead.localPosition;
        transform.localRotation = myHand.localRotation;
    }
}
