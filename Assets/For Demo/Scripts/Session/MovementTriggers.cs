using Normal.Realtime;
using UnityEngine;

/// <summary>
/// Steps the monitor rig through a list of signal positions. The monitor avatar's networked root follows the rig,
/// so its position tells every client which stage is active.
/// </summary>
public class MovementTriggers : MonoBehaviour
{
    private const float LookupInterval = 0.5f;

    [Header("Target Positions & Durations")]
    public Vector3[] targetPositions = new Vector3[13];  // Custom positions for each cycle

    private int _currentCycle = 0;
    private bool _waitingForAudio = false;
    private MovementStateHandler stateHandler;
    private float nextLookupTime;

    private void Update()
    {
        // The handler arrives with the monitor avatar once the room is joined.
        if (stateHandler == null && Time.unscaledTime >= nextLookupTime)
        {
            FindMovementStateHandler();
        }
    }

    /// <summary>Moves to the next signal position and starts its narration.</summary>
    public void MoveToNextPosition()
    {
        if (targetPositions == null || _currentCycle >= targetPositions.Length)
        {
            return;
        }

        if (TryGetComponent(out RealtimeTransform realtimeTransform))
        {
            realtimeTransform.RequestOwnership();
        }

        transform.position = targetPositions[_currentCycle];
        Debug.Log($"Moved instantly to: {targetPositions[_currentCycle]}!");

        if (stateHandler == null)
        {
            FindMovementStateHandler();
        }

        // Set before notifying: the handler clears the flag right away when a stage has no clip.
        _waitingForAudio = true;
        if (stateHandler == null || !stateHandler.HandlePositionReached(_currentCycle))
        {
            Debug.LogWarning("MovementTriggers: No narration was started for this position.");
            _waitingForAudio = false;
        }

        _currentCycle++;
    }

    public void SetWaitingForAudio(bool waiting)
    {
        _waitingForAudio = waiting;
    }

    public bool IsWaitingForAudio()
    {
        return _waitingForAudio;
    }

    private void FindMovementStateHandler()
    {
        nextLookupTime = Time.unscaledTime + LookupInterval;
        stateHandler = FindObjectOfType<MovementStateHandler>();
    }
}
