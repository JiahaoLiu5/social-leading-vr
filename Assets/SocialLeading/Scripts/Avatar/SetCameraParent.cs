using Normal.Realtime;
using UnityEngine;

/// <summary>
/// Attaches the scene camera to this transform when the avatar belongs to the local user, so that they see
/// from the avatar's head. Releases the camera when the room disconnects or this object is destroyed,
/// so that the view stays at its last pose.
/// </summary>
public class SetCameraParent : MonoBehaviour
{
    [Tooltip("Camera to attach. The main camera is used when empty.")]
    public Camera mainCamera;

    [Tooltip("Avatar root that carries the RealtimeView.")]
    public GameObject rootPrefab;

    private Realtime realtime;
    private Transform attachedCamera;
    private Transform previousParent;

    private void Start()
    {
        RealtimeView view = rootPrefab != null ? rootPrefab.GetComponent<RealtimeView>() : null;
        if (view == null)
        {
            Debug.LogWarning("SetCameraParent: no RealtimeView found on the avatar root.");
            return;
        }

        if (!view.isOwnedLocallySelf) return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("No main camera found in the scene");
            return;
        }

        attachedCamera = mainCamera.transform;
        previousParent = attachedCamera.parent;
        attachedCamera.SetParent(transform, false);
        attachedCamera.localPosition = Vector3.zero;
        attachedCamera.localRotation = Quaternion.identity;

        realtime = view.realtime;
        if (realtime != null)
        {
            realtime.didDisconnectFromRoom += OnDisconnectedFromRoom;
        }
    }

    private void OnDestroy()
    {
        if (realtime != null)
        {
            realtime.didDisconnectFromRoom -= OnDisconnectedFromRoom;
        }

        ReleaseCamera();
    }

    private void OnDisconnectedFromRoom(Realtime disconnected) => ReleaseCamera();

    private void ReleaseCamera()
    {
        // When the whole scene unloads, the camera goes with it and must not be moved.
        if (attachedCamera != null && attachedCamera.parent == transform && gameObject.scene.isLoaded)
        {
            attachedCamera.SetParent(previousParent != null ? previousParent : null, true);
        }

        attachedCamera = null;
    }
}
