using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
 
public class MovementTriggers : MonoBehaviour { 

    private bool _movementStarted = false;
    private int _currentCycle = 0;
    private bool _waitingForAudio = false;

    [Header("Target Positions & Durations")]
    public Vector3[] targetPositions = new Vector3[13];  // Custom positions for each cycle

    private MovementStateHandler stateHandler;

    private void Start() {
        FindMovementStateHandler();
    }

    private void Update() {
        // Keep searching for MovementStateHandler until it's found
        if (stateHandler == null) {
            FindMovementStateHandler();
        }

        // Detect two players and start movement
        if (!_movementStarted) {
            GameObject followerObject = GameObject.Find("VR Player Complete Prefab 1 (system V3)(Clone)");

            if (followerObject != null) {
                _movementStarted = true;
                Debug.Log("Two players detected. Ready to move.");
            }
        }
    }

    private void FindMovementStateHandler() {
        stateHandler = FindObjectOfType<MovementStateHandler>();

        if (stateHandler != null) {
            Debug.Log("MovementTriggers: Successfully found MovementStateHandler in the scene!");
        }
    }

    public void MoveToNextPosition() {
        if (_currentCycle < targetPositions.Length) {
            RealtimeTransform realtimeTransform = GetComponent<RealtimeTransform>();
            if (realtimeTransform != null) {
                realtimeTransform.RequestOwnership();
            }

            transform.position = targetPositions[_currentCycle];
            Debug.Log($"Moved instantly to: {targetPositions[_currentCycle]}!");

            if (stateHandler != null) {
                stateHandler.HandlePositionReached(_currentCycle);
            } else {
                Debug.LogWarning("MovementTriggers: StateHandler not found, skipping position check!");
            }

            _waitingForAudio = true;
            _currentCycle++;
        }
    }

    public void SetWaitingForAudio(bool waiting) {
        _waitingForAudio = waiting;
    }

    public bool IsWaitingForAudio() {
        return _waitingForAudio;
    }
}
