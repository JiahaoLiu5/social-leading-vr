using UnityEngine;

/// <summary>
/// Low-pass filter: follows another transform's local pose with an exponential time constant.
/// Used on the partner's hands so that blended movement stays smooth.
/// </summary>
[DefaultExecutionOrder(-55)]
public class SlowFollowSystem : MonoBehaviour
{
    public Transform UpStreamTarget;

    [Tooltip("Time constant in seconds. 0 copies the pose directly.")]
    [Min(0f)]
    public float lerpTime = 0.5f;

    private bool initialized;

    private void OnEnable() => initialized = false;

    private void Update()
    {
        if (UpStreamTarget == null) return;
        float t = !initialized || lerpTime <= 0f ? 1f : 1f - Mathf.Exp(-Time.deltaTime / lerpTime);
        initialized = true;
        transform.localPosition = Vector3.Lerp(transform.localPosition, UpStreamTarget.localPosition, t);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, UpStreamTarget.localRotation, t);
    }
}
