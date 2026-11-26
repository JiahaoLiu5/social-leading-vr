using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObserverController : MonoBehaviour
{ 
    public MovementTriggers movementTriggers;
    private bool hasFoundMovementTriggers = false;

    private void Start() {
        FindMovementTriggers();
    }

    private void Update() {
        if (!hasFoundMovementTriggers) {
            FindMovementTriggers();
        }

        if (Input.GetKeyDown(KeyCode.Space) && movementTriggers != null && !movementTriggers.IsWaitingForAudio()) {
            movementTriggers.MoveToNextPosition();
        }
    }

    private void FindMovementTriggers() {
        GameObject playerObject = GameObject.Find("XR Origin (XR Rig) 1");

        if (playerObject != null) {
            movementTriggers = playerObject.GetComponentInChildren<MovementTriggers>();
        }

        if (movementTriggers != null) {
            hasFoundMovementTriggers = true;
            Debug.Log("SpacebarInputHandler: Successfully found MovementTriggers!");
        }
    }
}