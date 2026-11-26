using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadAddedFollowSystem : MonoBehaviour
{
    public Transform myHead;
    public Transform partnerHead;
    public float addRatio = 0.5f;
    private Vector3 parPreLocalPosition;
    private Vector3 myPreLocalPosition;
    private bool firstTime = true;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        targetPosition = myHead.localPosition;
        targetRotation = myHead.localRotation;
    }

    void Update()
    {
        targetRotation = myHead.localRotation;
        Vector3 getYPosition = myHead.localPosition;

        if (myHead != null && partnerHead != null && GetPartnerTransform.sharedEntitiesStatic.Count == 2)
        {
            if (!firstTime)
            {
                Vector3 parChangedPosition = partnerHead.localPosition - parPreLocalPosition;
                Vector3 myChangedPosition = myHead.localPosition - myPreLocalPosition;
                targetPosition.x += myChangedPosition.x + parChangedPosition.x * addRatio;
                targetPosition.y = getYPosition.y;
                targetPosition.z += myChangedPosition.z + parChangedPosition.z * addRatio;
                parPreLocalPosition = partnerHead.localPosition;               
                myPreLocalPosition = myHead.localPosition;
            }
            else
            {
                firstTime = false;
                parPreLocalPosition = partnerHead.localPosition;
                myPreLocalPosition = myHead.localPosition;
            }
        }
        else
        {
            targetPosition = myHead.localPosition;
            targetRotation = myHead.localRotation;
            firstTime = true;
        }

        transform.localPosition = targetPosition;
        transform.localRotation = targetRotation;
    }
}



