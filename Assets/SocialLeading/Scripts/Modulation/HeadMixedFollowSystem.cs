using UnityEngine;

/// <summary>
/// Blends this avatar's horizontal head position with the partner's, each in its own root space.
/// Alternative to <see cref="HeadAddedFollowSystem"/>.
/// </summary>
[DefaultExecutionOrder(-50)]
public class HeadMixedFollowSystem : MovementModulator
{
    public Transform myHead;
    public Transform partnerHead;

    [Range(0f, 1f)]
    public float followRatio = 0.5f;

    private void Update()
    {
        if (myHead == null) return;

        Vector3 position = myHead.localPosition;
        if (LinkActive && partnerHead != null)
        {
            Vector3 mixed = Vector3.Lerp(position, partnerHead.localPosition, followRatio);
            position.x = mixed.x;
            position.z = mixed.z;
        }

        transform.localPosition = position;
        transform.localRotation = myHead.localRotation;
    }
}
