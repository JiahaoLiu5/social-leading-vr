using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadMixedFollowSystem : MonoBehaviour
{
    public Transform myHead;
    public Transform partnerHead;
    public float followRatio = 0.5f;
    void Update()
    {            
        Vector3 targetPosition = myHead.localPosition;
        Quaternion targetRotation = myHead.localRotation;

        if (myHead != null && partnerHead != null && GetPartnerTransform.sharedEntitiesStatic.Count == 2)
        {
            Vector3 lerpPosition = Vector3.Lerp(myHead.localPosition, partnerHead.localPosition, followRatio);
            targetPosition.x = lerpPosition.x;
            targetPosition.z = lerpPosition.z;
        }

        transform.localPosition = targetPosition;
        transform.localRotation = targetRotation;
    }
}




