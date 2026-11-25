using UnityEngine;

/// <summary>Right-drag to orbit, scroll to zoom. The camera stays inside the room.</summary>
public class OrbitCamera : MonoBehaviour
{
    public Vector3 target = new Vector3(0f, 1.15f, 0f);
    public float distance = 3.4f;
    public float yaw = -90f;
    public float pitch = 8f;
    [Tooltip("Farthest zoom, in metres, so that the camera stays inside the room.")]
    public float maxDistance = 3.6f;

    private void LateUpdate()
    {
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * 4f;
            pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3f, -10f, 50f);
        }
        distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 0.2f, 1.2f, maxDistance);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);
    }
}
