using UnityEngine;

/// <summary>
/// Adds a share of the partner's horizontal head displacement to this avatar's position.
///
///   position.xz = own.xz + addRatio * (partner displacement since the link started).xz
///
/// Both displacements are measured in each user's own root space, so "partner steps forward"
/// becomes "this avatar steps forward" whatever the seating arrangement is.
/// The vertical axis and all rotations stay the user's own.
/// </summary>
[DefaultExecutionOrder(-50)]
public class HeadAddedFollowSystem : MovementModulator
{
    public Transform myHead;
    public Transform partnerHead;

    [Range(0f, 1f)]
    public float addRatio = 0.5f;

    [Tooltip("Largest horizontal offset in metres. 0 means unlimited.")]
    [Min(0f)]
    public float maxOffset;

    private Vector3 previousPartner;
    private Vector3 accumulated;
    private bool initialized;

    private void Update()
    {
        if (myHead == null) return;

        if (LinkActive && partnerHead != null)
        {
            if (initialized)
            {
                Vector3 delta = (partnerHead.localPosition - previousPartner) * addRatio;
                delta.y = 0f;
                accumulated += delta;
                if (maxOffset > 0f) accumulated = Vector3.ClampMagnitude(accumulated, maxOffset);
            }
            previousPartner = partnerHead.localPosition;
            initialized = true;
        }
        else
        {
            accumulated = Vector3.zero;
            initialized = false;
        }

        transform.localPosition = myHead.localPosition + accumulated;
        transform.localRotation = myHead.localRotation;
    }
}
