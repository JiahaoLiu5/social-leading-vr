using UnityEngine;

/// <summary>
/// Procedural stepping for one foot: casts a ray below the body and moves the foot's IK target in an arc
/// to the hit point when it is too far away and the other foot is on the ground.
/// </summary>
public class IKFootSolver : MonoBehaviour
{
    public bool isMovingForward;

    [SerializeField] LayerMask terrainLayer = default;
    [SerializeField] Transform body = default;
    [SerializeField] IKFootSolver otherFoot = default;
    [SerializeField] float speed = 4;
    [SerializeField] float stepDistance = .2f;
    [SerializeField] float stepLength = .2f;
    [SerializeField] float sideStepLength = .1f;

    [SerializeField] float stepHeight = .3f;
    [SerializeField] Vector3 footOffset = default;

    public Vector3 footRotOffset;
    public float footYPosOffset = 0.1f;

    public float rayStartYOffset = 0;
    public float rayLength = 1.5f;

    float footSpacing;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    float lerp;

    private void Start()
    {
        footSpacing = transform.localPosition.x;
        currentPosition = newPosition = oldPosition = transform.position;
        currentNormal = newNormal = oldNormal = transform.up;
        lerp = 1;
    }

    private void Update()
    {
        transform.position = currentPosition + Vector3.up * footYPosOffset;
        transform.localRotation = Quaternion.Euler(footRotOffset);

        if (body != null)
        {
            LookForStep();
        }

        if (lerp < 1)
        {
            Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
            tempPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

            currentPosition = tempPosition;
            currentNormal = Vector3.Lerp(oldNormal, newNormal, lerp);
            lerp += Time.deltaTime * speed;
        }
        else
        {
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
    }

    private void LookForStep()
    {
        Vector3 rayStart = body.position + (body.right * footSpacing) + Vector3.up * rayStartYOffset;
        Debug.DrawRay(rayStart, Vector3.down);

        if (!Physics.Raycast(new Ray(rayStart, Vector3.down), out RaycastHit info, rayLength, terrainLayer.value)) return;

        bool otherFootMoving = otherFoot != null && otherFoot.IsMoving();
        if (Vector3.Distance(newPosition, info.point) <= stepDistance || otherFootMoving || lerp < 1) return;

        lerp = 0;
        Vector3 direction = Vector3.ProjectOnPlane(info.point - currentPosition, Vector3.up).normalized;

        // Both vectors are in world space.
        float angle = Vector3.Angle(body.forward, direction);
        isMovingForward = angle < 50 || angle > 130;

        newPosition = info.point + direction * (isMovingForward ? stepLength : sideStepLength) + footOffset;
        newNormal = info.normal;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.1f);
    }

    public bool IsMoving()
    {
        return lerp < 1;
    }
}
