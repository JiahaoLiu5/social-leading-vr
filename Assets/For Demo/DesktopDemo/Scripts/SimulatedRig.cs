using UnityEngine;

/// <summary>
/// Stands in for a headset and two controllers so the demo runs on a desktop.
/// Writes plausible local poses into the avatar's raw Head, Left Hand and Right Hand.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SimulatedRig : MonoBehaviour
{
    public enum MotionKind { Idle, Wave, RaiseHands, Talk, Reach, Step, Nod, RaiseLeft, RaiseRight, RaiseBoth, HighFive, Handshake, Clap }

    [Header("Raw tracked transforms")]
    public Transform head;
    public Transform leftHand;
    public Transform rightHand;

    [Header("Motion")]
    public MotionKind motion = MotionKind.Idle;
    [Range(0.25f, 2f)] public float speed = 1f;
    public float phase;
    public float eyeHeight = 1.6f;

    [Header("Rig calibration")]
    [Tooltip("Offset of the tracked head target from the eyes, copied from the XR rig prefab.")]
    public Vector3 headOffset = new Vector3(0f, -0.08f, -0.02f);
    public Quaternion leftWristOffset = Quaternion.identity;
    public Quaternion rightWristOffset = Quaternion.identity;

    private float clock;
    private Vector3 left, right, lean;
    private float leftUp, rightUp, nodding;
    private bool initialized;

    public void ResetClock() { clock = 0f; initialized = false; }

    /// <summary>Starts the repeating part of the motion from its beginning, without a jump of the hands.</summary>
    public void RestartMotion() => clock = -phase;

    private void Update()
    {
        if (head == null || leftHand == null || rightHand == null) return;
        clock += Time.deltaTime * speed;
        float t = clock + phase;

        Vector3 restL = new Vector3(-0.30f + 0.01f * Mathf.Sin(t * 0.9f), 0.90f + 0.012f * Mathf.Sin(t * 1.3f), 0.10f);
        Vector3 restR = new Vector3(0.30f + 0.01f * Mathf.Sin(t * 1.1f + 1f), 0.90f + 0.012f * Mathf.Sin(t * 1.2f + 2f), 0.10f);
        Vector3 targetL = restL, targetR = restR, targetLean = Vector3.zero;
        float upL = 0f, upR = 0f, nod = 0f;

        switch (motion)
        {
            case MotionKind.Wave:
            {
                float on = Pulse(t, 5f, 3.4f);
                targetR = Vector3.Lerp(restR, new Vector3(0.36f + 0.13f * Mathf.Sin(t * 7.5f), 1.66f, 0.26f), on);
                upR = on;
                break;
            }
            case MotionKind.RaiseHands:
            {
                float l = Pulse(t, 6f, 2.4f), r = Pulse(t + 3f, 6f, 2.4f);
                targetL = Vector3.Lerp(restL, new Vector3(-0.34f, 1.82f, 0.16f), l);
                targetR = Vector3.Lerp(restR, new Vector3(0.34f, 1.82f, 0.16f), r);
                upL = l; upR = r;
                break;
            }
            case MotionKind.Talk:
            {
                float a = 0.5f + 0.5f * Mathf.Sin(t * 1.7f), b = 0.5f + 0.5f * Mathf.Sin(t * 1.3f + 1.4f);
                targetL = new Vector3(-0.24f - 0.10f * a, 1.10f + 0.16f * a, 0.34f + 0.06f * b);
                targetR = new Vector3(0.24f + 0.12f * b, 1.10f + 0.20f * b, 0.34f + 0.06f * a);
                upL = 0.35f * a; upR = 0.4f * b;
                break;
            }
            case MotionKind.Reach:
            {
                float on = Pulse(t, 4.5f, 2.2f);
                targetR = Vector3.Lerp(restR, new Vector3(0.18f, 1.34f, 0.60f), on);
                targetL = Vector3.Lerp(restL, new Vector3(-0.26f, 1.05f, 0.28f), on * 0.6f);
                upR = on * 0.3f;
                break;
            }
            case MotionKind.Step:
                targetLean = new Vector3(0f, 0f, 0.35f * Pulse(t, 5f, 2.6f));
                break;
            case MotionKind.Nod:
                nod = 1f;
                break;
            case MotionKind.RaiseLeft:
                targetL = new Vector3(-0.32f, 1.82f, 0.16f);
                upL = 1f;
                break;
            case MotionKind.RaiseRight:
                targetR = new Vector3(0.32f, 1.82f, 0.16f);
                upR = 1f;
                break;
            case MotionKind.RaiseBoth:
                targetL = new Vector3(-0.32f, 1.82f, 0.16f);
                targetR = new Vector3(0.32f, 1.82f, 0.16f);
                upL = upR = 1f;
                break;
            case MotionKind.HighFive:
                targetR = new Vector3(0.16f, 1.54f, 0.42f);
                upR = 1f;
                break;
            case MotionKind.Handshake:
                targetR = new Vector3(0.10f, 1.08f + 0.03f * Mathf.Sin(t * 9f), 0.46f);
                upR = 0.42f;
                break;
            case MotionKind.Clap:
            {
                float gap = 0.05f + 0.10f * Mathf.Abs(Mathf.Sin(t * 6.5f));
                targetL = new Vector3(-gap, 1.18f, 0.34f);
                targetR = new Vector3(gap, 1.18f, 0.34f);
                upL = upR = 0.6f;
                break;
            }
        }

        float k = initialized ? 1f - Mathf.Exp(-Time.deltaTime * 9f) : 1f;
        initialized = true;
        left = Vector3.Lerp(left, targetL, k);
        right = Vector3.Lerp(right, targetR, k);
        lean = Vector3.Lerp(lean, targetLean, k);
        leftUp = Mathf.Lerp(leftUp, upL, k);
        rightUp = Mathf.Lerp(rightUp, upR, k);
        nodding = Mathf.Lerp(nodding, nod, k);

        Vector3 eye = new Vector3(0.008f * Mathf.Sin(t * 0.8f), eyeHeight + 0.004f * Mathf.Sin(t * 1.6f), 0f) + lean;
        head.localPosition = eye + headOffset;
        head.localRotation = Quaternion.Euler(2f * Mathf.Sin(t * 0.7f) + nodding * 13f * Mathf.Sin(t * 5f), 3f * Mathf.Sin(t * 0.5f), nodding * 7f * Mathf.Sin(t * 1.6f));
        leftHand.localPosition = left + lean;
        rightHand.localPosition = right + lean;
        leftHand.localRotation = Controller(leftUp, -1f) * leftWristOffset;
        rightHand.localRotation = Controller(rightUp, 1f) * rightWristOffset;
    }

    /// <summary>Controller orientation: pointing down along the body at rest, up when the hand is raised.</summary>
    private static Quaternion Controller(float up, float side) =>
        Quaternion.Euler(Mathf.Lerp(62f, -78f, up), side * Mathf.Lerp(-6f, 8f, up), 0f);

    /// <summary>Smooth 0-1-0 pulse of the given length that repeats every period.</summary>
    private static float Pulse(float t, float period, float length)
    {
        float x = Mathf.Repeat(t, period);
        if (x > length) return 0f;
        float edge = Mathf.Min(0.6f, length * 0.35f);
        return Mathf.SmoothStep(0f, 1f, x / edge) * Mathf.SmoothStep(0f, 1f, (length - x) / edge);
    }
}
