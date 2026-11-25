using UnityEngine;

/// <summary>Shared helpers for every component that rewrites a tracked pose.</summary>
public abstract class MovementModulator : MonoBehaviour
{
    private GetPartnerTransform link;

    /// <summary>True while this avatar is linked to another one.</summary>
    protected bool LinkActive
    {
        get
        {
            if (link == null) link = GetComponentInParent<GetPartnerTransform>();
            return link != null && link.HasSource;
        }
    }

    private Quaternion lastYaw = Quaternion.identity;

    /// <summary>
    /// Rotation around the vertical axis that turns the partner's heading into this avatar's heading.
    /// Keeps the last valid value while either head looks straight up or down.
    /// </summary>
    protected Quaternion HeadingAlignment(Transform partnerHead, Transform ownHead)
    {
        Vector3 from = partnerHead.localRotation * Vector3.forward;
        Vector3 to = ownHead.localRotation * Vector3.forward;
        from.y = 0f;
        to.y = 0f;
        if (from.sqrMagnitude > 1e-4f && to.sqrMagnitude > 1e-4f)
            lastYaw = Quaternion.AngleAxis(Vector3.SignedAngle(from, to, Vector3.up), Vector3.up);
        return lastYaw;
    }
}
