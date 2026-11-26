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

    [Tooltip("Seconds in which the offset fades after the link ends or the ratio becomes 0. 0 removes it at once.")]
    [Min(0f)]
    public float releaseTime;

    [Tooltip("Partner displacement faster than this (metres per second) is a recentre or a teleport and is not added. 0 adds everything.")]
    [Min(0f)]
    public float ignoreAboveSpeed;

    private Vector3 previousPartner;
    private Vector3 accumulated;
    private Vector3 residual;   // what is left of an offset whose link has ended
    private bool initialized;

    private void OnDisable()
    {
        accumulated = Vector3.zero;
        residual = Vector3.zero;
        initialized = false;
    }

    private void Update()
    {
        if (myHead == null) return;

        if (LinkActive && partnerHead != null && addRatio > 0f)
        {
            if (initialized)
            {
                Vector3 moved = partnerHead.localPosition - previousPartner;
                moved.y = 0f;
                if (ignoreAboveSpeed <= 0f || moved.magnitude <= ignoreAboveSpeed * Time.deltaTime)
                    accumulated += moved * addRatio;
                if (maxOffset > 0f) accumulated = Vector3.ClampMagnitude(accumulated, maxOffset);
            }
            previousPartner = partnerHead.localPosition;
            initialized = true;
        }
        else
        {
            residual = releaseTime > 0f ? residual + accumulated : Vector3.zero;
            accumulated = Vector3.zero;
            initialized = false;
        }

        if (releaseTime > 0f)
            residual = Vector3.MoveTowards(residual, Vector3.zero, (residual.magnitude / releaseTime + 0.02f) * Time.deltaTime);

        transform.localPosition = myHead.localPosition + accumulated + residual;
        transform.localRotation = myHead.localRotation;
    }
}
