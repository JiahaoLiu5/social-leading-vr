using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;

public class MovementStateHandler : MonoBehaviour
{
    private MovementTriggers movementTriggers;    
    [Header("Target Positions & Audio Clips")]
    public Vector3[] targetPositions = new Vector3[13];  // Set manually in Inspector
    public AudioClip[] movementAudioClips = new AudioClip[13]; // Set manually in Inspector

    private AudioSource audioSource;
    private int currentCycle = 0; // Tracks the current target position
    private bool isPlayingAudio = false; // Prevents overlapping audio

    private void Start() {
        FindAudioSource();
    }

    private void Update() {
        FindComponents();
        Debug.Log(transform.position);
        Debug.Log("Next Target" + targetPositions[0]);

        if (!isPlayingAudio && currentCycle < targetPositions.Length) {
            ComparePositionAndPlayAudio();
        }
    }

    private void FindComponents() {
        GameObject audioObject = GameObject.Find("Audio Source");
        if (audioObject != null) {
            audioSource = audioObject.GetComponent<AudioSource>();
        }

        movementTriggers = FindObjectOfType<MovementTriggers>();

        if (movementTriggers != null && audioSource != null) {
         
            Debug.Log("MovementStateHandler: Successfully found required components!");
        }
    }

    private void FindAudioSource() {
        GameObject audioObject = GameObject.Find("Audio Source");

        if (audioObject != null) {
            audioSource = audioObject.GetComponent<AudioSource>();
        }

        if (audioSource == null) {
            Debug.LogError("MovementStateHandler: Audio Source GameObject found, but missing AudioSource component!");
        }
    }

    private void ComparePositionAndPlayAudio() {
        // Check if the GameObject has reached the current target position
        if ( transform.position == targetPositions[currentCycle]) {
            Debug.Log("Reached!");
            PlayAudioClip(movementAudioClips[currentCycle]);
            currentCycle++; // Move to next position in the sequence
        }
    }

    private void PlayAudioClip(AudioClip clip) {
        if (audioSource == null) {
            Debug.LogError("MovementStateHandler: Cannot play audio, AudioSource not found!");
            return;
        }

        if (clip != null) {
            audioSource.clip = clip;
            audioSource.Play();
            StartCoroutine(WaitForAudioToFinish(clip.length));
        }
    }

    private IEnumerator WaitForAudioToFinish(float duration) {
        isPlayingAudio = true;
        yield return new WaitForSeconds(duration);
        isPlayingAudio = false; // Allow next audio to play when the next position is reached
    }

    public void HandlePositionReached(int cycleIndex) {
        if (movementTriggers == null) {
            Debug.LogWarning("MovementStateHandler: Cannot handle position, MovementTriggers is missing!");
            return;
        }

        if (audioSource == null) {
            Debug.LogError("MovementStateHandler: Cannot play audio, AudioSource not found!");
            return;
        }

        if (cycleIndex < movementAudioClips.Length) {
            PlayAudioClip(movementAudioClips[cycleIndex], () => {
                movementTriggers.SetWaitingForAudio(false);
            });
        }
    }

    private void PlayAudioClip(AudioClip clip, System.Action onComplete) {
        if (clip != null) {
            audioSource.clip = clip;
            audioSource.Play();
            StartCoroutine(WaitForAudioToFinish(clip.length, onComplete));
        } else {
            onComplete?.Invoke();
        }
    }

    private IEnumerator WaitForAudioToFinish(float duration, System.Action onComplete) {
        yield return new WaitForSeconds(duration);
        onComplete?.Invoke();
    }
}
