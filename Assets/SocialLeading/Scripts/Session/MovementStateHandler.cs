using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Sits on the monitor avatar. Each client watches the avatar's synced root position and plays narration
/// clip k when the avatar arrives at target position k. On the monitor itself, MovementTriggers starts the clip.
/// </summary>
public class MovementStateHandler : MonoBehaviour
{
    [Header("Target Positions & Audio Clips")]
    public Vector3[] targetPositions = new Vector3[13];  // Set manually in Inspector
    public AudioClip[] movementAudioClips = new AudioClip[13]; // Set manually in Inspector

    [Header("Scene Lookup")]
    [Tooltip("Name of the scene object whose AudioSource plays the narration.")]
    [SerializeField] private string audioSourceName = "Audio Source";

    private MovementTriggers movementTriggers;
    private AudioSource audioSource;
    private int currentCycle = 0; // Tracks the current target position
    private bool isPlayingAudio = false; // Prevents overlapping audio

    private void Start()
    {
        FindAudioSource();
        if (audioSource == null)
        {
            Debug.LogWarning($"MovementStateHandler: no AudioSource found on an object named '{audioSourceName}'.");
        }
    }

    private void Update()
    {
        if (!isPlayingAudio)
        {
            ComparePositionAndPlayAudio();
        }
    }

    private void OnDisable()
    {
        // The wait ends with this object, so let the monitor continue.
        StopAllCoroutines();
        isPlayingAudio = false;
        if (movementTriggers != null)
        {
            movementTriggers.SetWaitingForAudio(false);
        }
    }

    /// <summary>
    /// Plays the clip of one target position and tells MovementTriggers when it has finished.
    /// Called by MovementTriggers on the monitor. Returns false when nothing could be started.
    /// </summary>
    public bool HandlePositionReached(int cycleIndex)
    {
        if (movementTriggers == null)
        {
            movementTriggers = FindObjectOfType<MovementTriggers>();
        }

        if (movementTriggers == null)
        {
            Debug.LogWarning("MovementStateHandler: Cannot handle position, MovementTriggers is missing!");
            return false;
        }

        if (movementAudioClips == null || cycleIndex < 0 || cycleIndex >= movementAudioClips.Length)
        {
            return false;
        }

        if (!PlayAudioClip(movementAudioClips[cycleIndex], () => movementTriggers.SetWaitingForAudio(false)))
        {
            return false;
        }

        // This stage is handled, so Update does not start the same clip a second time.
        currentCycle = Mathf.Max(currentCycle, cycleIndex + 1);
        return true;
    }

    private void ComparePositionAndPlayAudio()
    {
        if (targetPositions == null || currentCycle >= targetPositions.Length)
        {
            return;
        }

        // Check if the GameObject has reached the current target position
        if (transform.position == targetPositions[currentCycle])
        {
            bool hasClip = movementAudioClips != null && currentCycle < movementAudioClips.Length;
            PlayAudioClip(hasClip ? movementAudioClips[currentCycle] : null, null);
            currentCycle++; // Move to next position in the sequence
        }
    }

    private bool PlayAudioClip(AudioClip clip, Action onComplete)
    {
        if (clip == null)
        {
            onComplete?.Invoke();
            return true;
        }

        if (audioSource == null)
        {
            FindAudioSource();
        }

        if (audioSource == null)
        {
            Debug.LogError("MovementStateHandler: Cannot play audio, AudioSource not found!");
            return false;
        }

        audioSource.clip = clip;
        audioSource.Play();
        StartCoroutine(WaitForAudioToFinish(clip.length, onComplete));
        return true;
    }

    private IEnumerator WaitForAudioToFinish(float duration, Action onComplete)
    {
        isPlayingAudio = true;
        yield return new WaitForSeconds(duration);
        isPlayingAudio = false; // Allow next audio to play when the next position is reached
        onComplete?.Invoke();
    }

    private void FindAudioSource()
    {
        GameObject audioObject = GameObject.Find(audioSourceName);
        audioSource = audioObject != null ? audioObject.GetComponent<AudioSource>() : null;
    }
}
