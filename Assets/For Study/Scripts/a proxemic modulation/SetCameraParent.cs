using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;

public class SetCameraParent : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject rootPrefab;

    void Start()
    {
        if (rootPrefab.GetComponent<RealtimeView>().isOwnedLocallySelf)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                mainCamera.transform.SetParent(transform, false);
                mainCamera.transform.localPosition = Vector3.zero;
                mainCamera.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogError("No main camera found in the scene");
            }
        }    
    }
}
