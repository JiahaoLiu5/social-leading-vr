using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Records the head and hand poses of the leader and follower avatars to a CSV file, one row per physics step.
/// Runs on the monitor client. Avatar roots are found by tag, their tracked parts by child name.
/// </summary>
public class DataLoggerSystem : MonoBehaviour
{
    private const float LookupInterval = 0.5f;
    private const float FlushInterval = 1f;
    private const int BufferSize = 64 * 1024;

    private const string ColumnNames =
        "Frame, Time," +
        "LeaderHeadX, LeaderHeadY, LeaderHeadZ, LeaderHeadPitch, LeaderHeadYaw, LeaderHeadRoll," +
        "LeaderLHandX, LeaderLHandY, LeaderLHandZ, LeaderLHandPitch, LeaderLHandYaw, LeaderLHandRoll," +
        "LeaderRHandX, LeaderRHandY, LeaderRHandZ, LeaderRHandPitch, LeaderRHandYaw, LeaderRHandRoll," +
        "FollowerHeadX, FollowerHeadY, FollowerHeadZ, FollowerHeadPitch, FollowerHeadYaw, FollowerHeadRoll," +
        "FollowerLHandX, FollowerLHandY, FollowerLHandZ, FollowerLHandPitch, FollowerLHandYaw, FollowerLHandRoll," +
        "FollowerRHandX, FollowerRHandY, FollowerRHandZ, FollowerRHandPitch, FollowerRHandYaw, FollowerRHandRoll," +
        "ModulatedHeadX, ModulatedHeadY, ModulatedHeadZ," +
        "ModulatedLHandX, ModulatedLHandY, ModulatedLHandZ," +
        "ModulatedRHandX, ModulatedRHandY, ModulatedRHandZ\n";

    [Header("Avatar Roots (found by tag when empty)")]
    public Transform leader;
    public Transform follower;

    [Header("Tracked Parts (found by name when empty)")]
    [SerializeField] private Transform leaderHead;
    [SerializeField] private Transform leaderLHand;
    [SerializeField] private Transform leaderRHand;

    [SerializeField] private Transform followerHead;
    [SerializeField] private Transform followerLHand;
    [SerializeField] private Transform followerRHand;

    [SerializeField] private Transform modulatedHead;
    [SerializeField] private Transform modulatedLHand;
    [SerializeField] private Transform modulatedRHand;

    [Header("Lookup")]
    [SerializeField] private string leaderTag = "LeaderTag";
    [SerializeField] private string followerTag = "FollowerTag";
    [SerializeField] private string headName = "Head";
    [SerializeField] private string leftHandName = "Left Hand";
    [SerializeField] private string rightHandName = "Right Hand";
    [SerializeField] private string modulatedHeadName = "Head Modulated";
    [SerializeField] private string modulatedLeftHandName = "Left Hand Modulated";
    [SerializeField] private string modulatedRightHandName = "Right Hand Modulated";

    [Header("Output")]
    [Tooltip("Folder inside the application's persistent data path.")]
    [SerializeField] private string folderName = "System_Movement_Tracking_Data";

    [Tooltip("Start of the file name. A UTC timestamp and .csv are added.")]
    [SerializeField] private string filePrefix = "SystemTrackingData_";

    private readonly StringBuilder row = new StringBuilder(1024);
    private StreamWriter writer;
    private float nextLookupTime;
    private float nextFlushTime;
    private bool reportedMissing;

    private void Start()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);
        string filePath = Path.Combine(folderPath, $"{filePrefix}{timestamp}.csv");

        try
        {
            Directory.CreateDirectory(folderPath);

            // Shared for reading, so the file can be opened while it is still being written.
            var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(stream, new UTF8Encoding(false), BufferSize);
            writer.Write(ColumnNames);
            writer.Flush();
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogError($"DataLoggerSystem: cannot write {filePath}: {e.Message}");
            return;
        }

        Debug.Log($"File path set to: {filePath}");
    }

    private void Update()
    {
        if (writer == null || Time.unscaledTime < nextFlushTime) return;
        nextFlushTime = Time.unscaledTime + FlushInterval;
        writer.Flush();
    }

    private void FixedUpdate()
    {
        if (writer == null) return;

        if (!HasAllParts())
        {
            LookUpAvatars();
            return;
        }

        row.Clear();
        row.Append(Time.frameCount.ToString(CultureInfo.InvariantCulture)).Append(", ");
        row.Append(Time.time.ToString("F2", CultureInfo.InvariantCulture)).Append(',');

        AppendPose(leaderHead);
        AppendPose(leaderLHand);
        AppendPose(leaderRHand);

        AppendPose(followerHead);
        AppendPose(followerLHand);
        AppendPose(followerRHand);

        AppendVector(modulatedHead.position, ',');
        AppendVector(modulatedLHand.position, ',');
        AppendVector(modulatedRHand.position, '\n');

        writer.Write(row.ToString());
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) writer?.Flush();
    }

    private void OnApplicationQuit() => writer?.Flush();

    private void OnDestroy()
    {
        writer?.Dispose();
        writer = null;
    }

    private void AppendPose(Transform part)
    {
        AppendVector(part.position, ',');
        AppendVector(part.rotation.eulerAngles, ',');
    }

    // Invariant culture: a decimal comma would break the column layout.
    private void AppendVector(Vector3 value, char terminator)
    {
        row.Append(value.x.ToString(CultureInfo.InvariantCulture)).Append(", ");
        row.Append(value.y.ToString(CultureInfo.InvariantCulture)).Append(", ");
        row.Append(value.z.ToString(CultureInfo.InvariantCulture)).Append(terminator);
    }

    private bool HasAllParts()
    {
        return leader != null && follower != null &&
               leaderHead != null && leaderLHand != null && leaderRHand != null &&
               followerHead != null && followerLHand != null && followerRHand != null &&
               modulatedHead != null && modulatedLHand != null && modulatedRHand != null;
    }

    private void LookUpAvatars()
    {
        if (Time.unscaledTime < nextLookupTime) return;
        nextLookupTime = Time.unscaledTime + LookupInterval;

        if (leader == null) leader = FindRoot(leaderTag);
        if (follower == null) follower = FindRoot(followerTag);

        if (leader != null)
        {
            leaderHead = FindPart(leaderHead, leader, headName);
            leaderLHand = FindPart(leaderLHand, leader, leftHandName);
            leaderRHand = FindPart(leaderRHand, leader, rightHandName);
        }

        if (follower != null)
        {
            followerHead = FindPart(followerHead, follower, headName);
            followerLHand = FindPart(followerLHand, follower, leftHandName);
            followerRHand = FindPart(followerRHand, follower, rightHandName);
            modulatedHead = FindPart(modulatedHead, follower, modulatedHeadName);
            modulatedLHand = FindPart(modulatedLHand, follower, modulatedLeftHandName);
            modulatedRHand = FindPart(modulatedRHand, follower, modulatedRightHandName);
        }

        if (HasAllParts())
        {
            reportedMissing = false;
            Debug.Log("DataLoggerSystem: leader and follower found, recording.");
        }
        else if (!reportedMissing)
        {
            reportedMissing = true;
            Debug.LogWarning("DataLoggerSystem: waiting for the leader and follower avatars.");
        }
    }

    private static Transform FindRoot(string rootTag)
    {
        GameObject root = GameObject.FindWithTag(rootTag);
        return root != null ? root.transform : null;
    }

    private static Transform FindPart(Transform current, Transform root, string partName) =>
        current != null ? current : root.Find(partName);
}
