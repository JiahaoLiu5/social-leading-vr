using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalFollowSystem : MonoBehaviour
{
    public Transform UpStreamTarget;

    void Update()
    {
        if (UpStreamTarget != null)
        {
            Vector3 targetPositionLocal = UpStreamTarget.localPosition;
            Quaternion targetRotationLocal = UpStreamTarget.localRotation;
            transform.localPosition = targetPositionLocal;
            transform.localRotation = targetRotationLocal;
        }
    }
}
