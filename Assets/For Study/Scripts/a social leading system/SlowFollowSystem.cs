using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowFollowSystem : MonoBehaviour
{
    public Transform UpStreamTarget;
    public float lerpTime = 0.5f;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    void Update()
    {
        if (UpStreamTarget != null)
        {
            targetPosition = UpStreamTarget.localPosition;
            targetRotation = UpStreamTarget.localRotation;
        }
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime / lerpTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime / lerpTime);
    }
}

