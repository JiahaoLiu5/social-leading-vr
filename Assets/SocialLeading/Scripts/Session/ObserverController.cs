using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard control for the monitor client: Space advances to the next stage once the current narration has finished.
/// </summary>
public class ObserverController : MonoBehaviour
{
    private const float LookupInterval = 0.5f;

    [Tooltip("Found under the rig object when empty.")]
    public MovementTriggers movementTriggers;

    [Tooltip("Name of the rig object that holds MovementTriggers.")]
    [SerializeField] private string rigName = "XR Origin (XR Rig) 1";

    private float nextLookupTime;

    private void Update()
    {
        if (movementTriggers == null && Time.unscaledTime >= nextLookupTime)
        {
            FindMovementTriggers();
        }

        if (movementTriggers != null && !movementTriggers.IsWaitingForAudio() && SpacePressedThisFrame())
        {
            movementTriggers.MoveToNextPosition();
        }
    }

    private void FindMovementTriggers()
    {
        nextLookupTime = Time.unscaledTime + LookupInterval;

        GameObject rig = GameObject.Find(rigName);
        if (rig != null)
        {
            movementTriggers = rig.GetComponentInChildren<MovementTriggers>();
        }
    }

    private static bool SpacePressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            return keyboard.spaceKey.wasPressedThisFrame;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }
}
