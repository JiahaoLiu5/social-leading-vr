using UnityEngine;

/// <summary>Pairs one tracked transform with the IK target that should follow it.</summary>
[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform ikTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;

    public bool IsComplete => vrTarget != null && ikTarget != null;

    public void Map()
    {
        if (!IsComplete) return;
        ikTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
        ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }
}

/// <summary>
/// Places the avatar body under the head, turns it smoothly toward the head's heading,
/// and writes the head and hand poses into the IK targets.
/// </summary>
public class IKTargetFollowVRRig : MonoBehaviour
{
    private const float ReferenceFrameRate = 90f;

    [Tooltip("Share of the remaining turn made per frame at 90 Hz. Scaled by frame time, so the turn speed does not depend on the frame rate.")]
    [Range(0,1)]
    public float turnSmoothness = 0.1f;
    public VRMap head;
    public VRMap leftHand;
    public VRMap rightHand;

    public Vector3 headBodyPositionOffset;

    [Tooltip("Not used by the current mapping. Kept so that existing prefab data stays valid.")]
    public float headBodyYawOffset;

    private void LateUpdate()
    {
        if (head != null && head.IsComplete)
        {
            transform.position = head.ikTarget.position + headBodyPositionOffset;
            float yaw = head.vrTarget.eulerAngles.y;
            Quaternion target = Quaternion.Euler(transform.eulerAngles.x, yaw, transform.eulerAngles.z);
            transform.rotation = Quaternion.Lerp(transform.rotation, target, TurnFraction(Time.deltaTime));
        }

        head?.Map();
        leftHand?.Map();
        rightHand?.Map();
    }

    // 1 - exp(-rate * dt), with the rate chosen so that one 90 Hz frame turns the body by turnSmoothness.
    private float TurnFraction(float deltaTime)
    {
        if (turnSmoothness >= 1f) return 1f;
        float rate = -Mathf.Log(1f - Mathf.Max(0f, turnSmoothness)) * ReferenceFrameRate;
        return 1f - Mathf.Exp(-rate * deltaTime);
    }
}
