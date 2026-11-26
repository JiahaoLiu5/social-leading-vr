using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

public class DataLoggerSystem : MonoBehaviour
{
    public Transform leader;
    public Transform follower;
    
    [SerializeField] private Transform leaderHead;
    [SerializeField] private Transform leaderLHand;
    [SerializeField] private Transform leaderRHand;

    [SerializeField] private Transform followerHead;
    [SerializeField] private Transform followerLHand;
    [SerializeField] private Transform followerRHand;

    [SerializeField] private Transform modulatedHead;
    [SerializeField] private Transform modulatedLHand;
    [SerializeField] private Transform modulatedRHand;

    private string filePath;
    private string folderName = "System_Movement_Tracking_Data";

    void Start()
    {
        // Create the folder if it doesn't exist  
        string folderPath = Path.Combine(Application.persistentDataPath, folderName);  
        if (!Directory.Exists(folderPath))  
        {  
            Directory.CreateDirectory(folderPath);  
        }  

        // Generate a unique file name based on timestamp  
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");  
        string fileName = $"SystemTrackingData_{timestamp}.csv";  

        // Set the file path to the folder and unique filename  
        filePath = Path.Combine(folderPath, fileName);  

        Debug.Log($"File path set to: {filePath}"); 

        // Create the CSV file with headers
        File.WriteAllText(filePath, "Frame, Time," +
                        "LeaderHeadX, LeaderHeadY, LeaderHeadZ, LeaderHeadPitch, LeaderHeadYaw, LeaderHeadRoll," +
                        "LeaderLHandX, LeaderLHandY, LeaderLHandZ, LeaderLHandPitch, LeaderLHandYaw, LeaderLHandRoll," +
                        "LeaderRHandX, LeaderRHandY, LeaderRHandZ, LeaderRHandPitch, LeaderRHandYaw, LeaderRHandRoll," +
                        "FollowerHeadX, FollowerHeadY, FollowerHeadZ, FollowerHeadPitch, FollowerHeadYaw, FollowerHeadRoll," +
                        "FollowerLHandX, FollowerLHandY, FollowerLHandZ, FollowerLHandPitch, FollowerLHandYaw, FollowerLHandRoll," +
                        "FollowerRHandX, FollowerRHandY, FollowerRHandZ, FollowerRHandPitch, FollowerRHandYaw, FollowerRHandRoll," +
                        "ModulatedHeadX, ModulatedHeadY, ModulatedHeadZ," +
                        "ModulatedLHandX, ModulatedLHandY, ModulatedLHandZ," +
                        "ModulatedRHandX, ModulatedRHandY, ModulatedRHandZ\n");    
    }

    void FixedUpdate()
    {
        if (leader != null && follower != null)
        {
            // Get position for leader
            Vector3 leaderHeadPosition = leaderHead.position;
            Vector3 leaderLHandPosition = leaderLHand.position;
            Vector3 leaderRHandPosition = leaderRHand.position;

            // Get rotation angles for leader
            Vector3 leaderHeadRotation = leaderHead.rotation.eulerAngles;
            Vector3 leaderLHandRotation = leaderLHand.rotation.eulerAngles;
            Vector3 leaderRHandRotation = leaderRHand.rotation.eulerAngles;

            // Get position for follower
            Vector3 followerHeadPosition = followerHead.position;
            Vector3 followerLHandPosition = followerLHand.position;
            Vector3 followerRHandPosition = followerRHand.position;

            // Get rotation angles for follower
            Vector3 followerHeadRotation = followerHead.rotation.eulerAngles;
            Vector3 followerLHandRotation = followerLHand.rotation.eulerAngles;
            Vector3 followerRHandRotation = followerRHand.rotation.eulerAngles;

            // Get position for folower modulated
            Vector3 modulatedHeadPosition = modulatedHead.position;
            Vector3 modulatedLHandPosition = modulatedLHand.position;
            Vector3 modulatedRHandPosition = modulatedRHand.position;

            // Record the data directly to the CSV
            string dataRow = $"{Time.frameCount}, {Time.time:F2}," +
                             $"{leaderHeadPosition.x}, {leaderHeadPosition.y}, {leaderHeadPosition.z}," +
                             $"{leaderHeadRotation.x}, {leaderHeadRotation.y}, {leaderHeadRotation.z}," +
                             $"{leaderLHandPosition.x}, {leaderLHandPosition.y}, {leaderLHandPosition.z}," +
                             $"{leaderLHandRotation.x}, {leaderLHandRotation.y}, {leaderLHandRotation.z}," +
                             $"{leaderRHandPosition.x}, {leaderRHandPosition.y}, {leaderRHandPosition.z}," +
                             $"{leaderRHandRotation.x}, {leaderRHandRotation.y}, {leaderRHandRotation.z}," +
                             $"{followerHeadPosition.x}, {followerHeadPosition.y}, {followerHeadPosition.z}," +
                             $"{followerHeadRotation.x}, {followerHeadRotation.y}, {followerHeadRotation.z}," +
                             $"{followerLHandPosition.x}, {followerLHandPosition.y}, {followerLHandPosition.z}," +
                             $"{followerLHandRotation.x}, {followerLHandRotation.y}, {followerLHandRotation.z}," +
                             $"{followerRHandPosition.x}, {followerRHandPosition.y}, {followerRHandPosition.z}," +
                             $"{followerRHandRotation.x}, {followerRHandRotation.y}, {followerRHandRotation.z}," +
                             $"{modulatedHeadPosition.x}, {modulatedHeadPosition.y}, {modulatedHeadPosition.z}," +
                             $"{modulatedLHandPosition.x}, {modulatedLHandPosition.y}, {modulatedLHandPosition.z}," +
                             $"{modulatedRHandPosition.x}, {modulatedRHandPosition.y}, {modulatedRHandPosition.z}\n";
            
            File.AppendAllText(filePath, dataRow);
        }
        else
        {
            // Find corresponding leader follower Game Object
            GameObject leaderObject = GameObject.FindWithTag("LeaderTag");
            GameObject followerObject = GameObject.FindWithTag("FollowerTag");

            if (leaderObject != null)
            {
                leader = leaderObject.transform;
            }
            else
            {
                Debug.LogError("leader object not found!");
            }

            if (followerObject != null)
            {
                follower = followerObject.transform;
            }
            else
            {
                Debug.LogError("follower object not found!");
            }

            // Assign corresponding transforms
            if (leader != null)
            {
                leaderHead = leader.Find("Head");
                leaderLHand = leader.Find("Left Hand");
                leaderRHand = leader.Find("Right Hand");
            }

            if (follower != null)
            {
                followerHead = follower.Find("Head");
                followerLHand = follower.Find("Left Hand");
                followerRHand = follower.Find("Right Hand");
                modulatedHead = follower.Find("Head Modulated");
                modulatedLHand = follower.Find("Right Hand Modulated");
                modulatedRHand = follower.Find("Left Hand Modulated");
            }      
        }
    }
}
