using UnityEngine;

/// <summary>
/// Shows where the tracked hands really are next to the rendered avatar,
/// so the difference between physical and displayed movement is visible.
/// </summary>
public class RawPoseGhost : MonoBehaviour
{
    public Transform rawLeft;
    public Transform rawRight;
    public Transform renderedLeft;
    public Transform renderedRight;
    public Transform rawHead;
    public Transform renderedHead;
    public Material ghostMaterial;
    public bool visible = true;

    private Transform[] markers;
    private LineRenderer[] lines;

    private void Start()
    {
        markers = new Transform[2];
        lines = new LineRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = i == 0 ? "Ghost Left" : "Ghost Right";
            Destroy(marker.GetComponent<Collider>());
            marker.transform.SetParent(transform, false);
            marker.transform.localScale = Vector3.one * 0.09f;
            marker.GetComponent<Renderer>().sharedMaterial = ghostMaterial;
            markers[i] = marker.transform;

            LineRenderer line = marker.AddComponent<LineRenderer>();
            line.sharedMaterial = ghostMaterial;
            line.widthMultiplier = 0.02f;
            line.positionCount = 2;
            line.useWorldSpace = true;
            lines[i] = line;
        }
    }

    private void LateUpdate()
    {
        if (markers == null) return;
        Place(0, rawLeft, renderedLeft);
        Place(1, rawRight, renderedRight);
    }

    private void Place(int index, Transform raw, Transform rendered)
    {
        bool show = visible && raw != null && rendered != null;
        markers[index].gameObject.SetActive(show);
        if (!show) return;
        // Express the raw hand relative to the rendered head, so only the hand offset is shown.
        Vector3 offset = rawHead != null && renderedHead != null ? renderedHead.localPosition - rawHead.localPosition : Vector3.zero;
        Vector3 world = raw.parent.TransformPoint(raw.localPosition + offset);
        markers[index].position = world;
        lines[index].SetPosition(0, world);
        lines[index].SetPosition(1, rendered.position);
    }
}
