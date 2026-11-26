using UnityEngine;

/// <summary>Spins this object around its Z axis, for the waiting screen.</summary>
public class LoadingEffect : MonoBehaviour
{
    [Tooltip("Degrees per second.")]
    public float rotationSpeed = 100f;

    private void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }
}
