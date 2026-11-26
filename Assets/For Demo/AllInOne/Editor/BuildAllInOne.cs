using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds players that contain only the integrated demo. Batch mode: -executeMethod BuildAllInOne.Android or
/// BuildAllInOne.Windows, optionally with -aio-out PATH. Without it the output goes to Builds, which git ignores.
/// </summary>
public static class BuildAllInOne
{
    private const string ProductName = "Social Leading VR All-in-One";
    private const string AndroidId = "com.jiahaoliu.socialleadingvr.allinone";
    private const string AndroidPath = "Builds/AllInOne-Android/SocialLeadingVR-AllInOne.apk";
    private const string WindowsPath = "Builds/AllInOne-Windows/SocialLeadingVR-AllInOne.exe";

    // Two characters, the room, and the mirror fit well into the memory of a standalone headset at this size.
    private const int AndroidTextureSize = 1024;

    [MenuItem("Social Leading/All-in-One/Build Android")]
    public static void Android() => Run(BuildTarget.Android, BuildTargetGroup.Android, AndroidPath);

    [MenuItem("Social Leading/All-in-One/Build Windows")]
    public static void Windows() => Run(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, WindowsPath);

    private static void Run(BuildTarget target, BuildTargetGroup group, string defaultPath)
    {
        string path = OutputPath(defaultPath);
        var settings = new SettingsBackup(group);
        bool succeeded = false;
        try
        {
            PlayerSettings.productName = ProductName;
            if (target == BuildTarget.Android) UseAndroidSettings();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { AllInOneBuilder.ScenePath },
                locationPathName = path,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            });
            succeeded = report.summary.result == BuildResult.Succeeded;
            Debug.Log("All-in-One build " + report.summary.result + ": " + path + ", " + report.summary.totalErrors + " errors, "
                      + report.summary.totalSize / (1024 * 1024) + " MB");
            Debug.LogWarning("This build may contain your Normcore key. Do not publish it.");
        }
        finally
        {
            settings.Restore();
            AssetDatabase.SaveAssets();
        }
        if (Application.isBatchMode) EditorApplication.Exit(succeeded ? 0 : 1);
    }

    private static void UseAndroidSettings()
    {
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidId);
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // Normcore declares no permissions of its own.
        PlayerSettings.Android.forceInternetPermission = true;
        EditorUserBuildSettings.buildAppBundle = false;
        // An import override of the build: it lives in the Library and changes no asset.
        EditorUserBuildSettings.overrideMaxTextureSize = AndroidTextureSize;
    }

    private static string OutputPath(string fallback)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(arguments, "-aio-out");
        return at >= 0 && at + 1 < arguments.Length ? arguments[at + 1] : fallback;
    }

    /// <summary>The project settings a build changes, and how to bring them back.</summary>
    private sealed class SettingsBackup
    {
        private readonly BuildTargetGroup group;
        private readonly string product, identifier;
        private readonly ScriptingImplementation backend;
        private readonly AndroidArchitecture architectures;
        private readonly bool internet, bundle;
        private readonly int textureSize;

        public SettingsBackup(BuildTargetGroup group)
        {
            this.group = group;
            product = PlayerSettings.productName;
            identifier = PlayerSettings.GetApplicationIdentifier(group);
            backend = PlayerSettings.GetScriptingBackend(group);
            architectures = PlayerSettings.Android.targetArchitectures;
            internet = PlayerSettings.Android.forceInternetPermission;
            bundle = EditorUserBuildSettings.buildAppBundle;
            textureSize = EditorUserBuildSettings.overrideMaxTextureSize;
        }

        public void Restore()
        {
            PlayerSettings.productName = product;
            EditorUserBuildSettings.buildAppBundle = bundle;
            EditorUserBuildSettings.overrideMaxTextureSize = textureSize;
            if (group != BuildTargetGroup.Android) return;
            PlayerSettings.SetApplicationIdentifier(group, identifier);
            PlayerSettings.SetScriptingBackend(group, backend);
            PlayerSettings.Android.targetArchitectures = architectures;
            PlayerSettings.Android.forceInternetPermission = internet;
        }
    }
}
