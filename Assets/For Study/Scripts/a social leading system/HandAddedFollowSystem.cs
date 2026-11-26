using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAddedFollowSystem : MonoBehaviour
{
    public Transform myHead;
    public Transform myHand;
    public Transform partnerHead;
    public Transform partnerHand;
    public Transform modulatedHead;
    public float addRatio = 0.5f;
    private Vector3 preParHeadHand;
    private Vector3 preMyHeadHand;
    private Vector3 oriHeadHand;
    private bool firstTime = true;
    private Vector3 totalChangedVector = new Vector3(0f, 0f, 0f);
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        oriHeadHand = myHand.localPosition - myHead.localPosition;
        targetPosition = oriHeadHand + modulatedHead.localPosition;
        targetRotation = myHand.localRotation;
    }

    void Update()
    {
        // Keep updating targetRotation every frame 
        targetRotation = myHand.localRotation;

        if (myHead != null && myHand != null && partnerHead != null && partnerHand != null && modulatedHead != null && GetPartnerTransform.sharedEntitiesStatic.Count == 2)
        {
            // Difference in facing directions
            Vector3 modHeadForward = modulatedHead.transform.forward;
            Vector3 modHeadForwardPlane = new Vector3(modHeadForward.x, 0, modHeadForward.z);
            Vector3 modUnitVec = modHeadForwardPlane.normalized;
            Vector3 parHeadForward = partnerHead.transform.forward;
            Vector3 parHeadForwardPlane = new Vector3(parHeadForward.x, 0, parHeadForward.z);
            Vector3 parUnitVec = parHeadForwardPlane.normalized;

            // Calculate the partner's and my head-hand vector
            Vector3 parHeadHand = partnerHand.localPosition - partnerHead.localPosition;
            Vector3 myHeadHand = myHand.localPosition - myHead.localPosition;

            if(!firstTime)
            {
                Vector3 parChangedVector = parHeadHand - preParHeadHand;
                Vector3 myChangedVector = myHeadHand - preMyHeadHand;

                // Calculate the transformed head-hand vector's change
                Quaternion transformRotation = Quaternion.FromToRotation(parUnitVec, modUnitVec);
                Vector3 tranChangedVector = transformRotation * parChangedVector;

                // Calculate the modulated head-hand vector via superimpose
                totalChangedVector += myChangedVector + tranChangedVector * addRatio;
                Vector3 modHeadHand = oriHeadHand + totalChangedVector;
                targetPosition = modHeadHand + modulatedHead.localPosition;

                // Assign the current partner's and my head-hand vector to the preParHeadHand and preMyHeadHand
                preParHeadHand = parHeadHand;
                preMyHeadHand = myHeadHand;
            }
            else
            {
                firstTime = false;
                preParHeadHand = parHeadHand;
                preMyHeadHand = myHeadHand;
            }
        }
        else
        {
            oriHeadHand = myHand.localPosition - myHead.localPosition;
            targetPosition = oriHeadHand + modulatedHead.localPosition;
            targetRotation = myHand.localRotation;
            firstTime = true;
        }

        transform.localPosition = targetPosition;
        transform.localRotation = targetRotation;
    }
}