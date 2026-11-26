using UnityEngine;

/// <summary>
/// Adds a share of the partner's hand displacement on top of this avatar's own hand movement.
/// Alternative to <see cref="HandMixedFollowSystem"/>: it reacts to changes instead of absolute position,
/// so the offset accumulates over time.
/// </summary>
[DefaultExecutionOrder(-45)]
public class HandAddedFollowSystem : MovementModulator
{
    public Transform myHead;
    public Transform myHand;
    public Transform partnerHead;
    public Transform partnerHand;
    public Transform modulatedHead;

    [Range(0f, 1f)]
    public float addRatio = 0.5f;

    [Tooltip("Largest accumulated offset in metres. 0 means unlimited.")]
    [Min(0f)]
    public float maxOffset;

    private Vector3 previousPartner;
    private Vector3 accumulated;
    private bool initialized;

    private void OnDisable()
    {
        accumulated = Vector3.zero;
        initialized = false;
    }

    private void Update()
    {
        if (myHead == null || myHand == null || modulatedHead == null) return;

        Vector3 own = myHand.localPosition - myHead.localPosition;

        if (LinkActive && partnerHead != null && partnerHand != null && addRatio > 0f)
        {
            Vector3 partner = partnerHand.localPosition - partnerHead.localPosition;
            if (initialized)
                accumulated += HeadingAlignment(partnerHead, modulatedHead) * (partner - previousPartner) * addRatio;
            if (maxOffset > 0f) accumulated = Vector3.ClampMagnitude(accumulated, maxOffset);
            previousPartner = partner;
            initialized = true;
        }
        else
        {
            accumulated = Vector3.zero;
            initialized = false;
        }

        transform.localPosition = own + accumulated + modulatedHead.localPosition;
        transform.localRotation = myHand.localRotation;
    }
}
