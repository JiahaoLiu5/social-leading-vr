using UnityEngine;

/// <summary>Copies another transform's local pose without changing it.</summary>
[DefaultExecutionOrder(-50)]
public class LocalFollowSystem : MonoBehaviour
{
    public Transform UpStreamTarget;

    private void Update()
    {
        if (UpStreamTarget == null) return;
        transform.localPosition = UpStreamTarget.localPosition;
        transform.localRotation = UpStreamTarget.localRotation;
    }
}
