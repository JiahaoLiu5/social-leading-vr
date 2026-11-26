# VR setup

This page covers the Normcore key, the Quest build of All-in-One and of the single-role scenes, the OpenXR settings, permissions, and common problems. **None of these steps was run on a headset for this release.** The All-in-One builds were made and the Windows player was run on a PC; the APK was not installed.

Contents: [What you need](#what-you-need), [1. Normcore key](#1-normcore-key), [2. Platform](#2-platform), [3. OpenXR](#3-openxr), [4. Build All-in-One](#4-build-all-in-one), [5. Build the single-role scenes](#5-build-the-single-role-scenes), [6. Permissions](#6-permissions), [7. Run the single-role scenes](#7-run-the-single-role-scenes), [Eye height](#eye-height), [Troubleshooting](#troubleshooting).

## What you need

- Unity 2022.3.15f1 with **Android Build Support**, including the Android SDK and NDK tools and OpenJDK
- A Quest headset with Touch controllers and developer mode on, one per person
- For rooms with other people: a Normcore account and an internet connection on every device
- For the single-role scenes: a PC for the monitor scene

Solo in All-in-One needs neither an account nor a network. You can replace a headset with a PC headset or with Quest Link: open the scene in the editor and press Play.

## 1. Normcore key

Only rooms with other people need it.

1. Create an account at [normcore.io](https://normcore.io) and create an app.
2. Copy the app key.
3. In Unity select `Assets/Normal/Resources/NormcoreAppSettings.asset` and paste the key into the app key field.
4. Keep the key out of git:

   ```sh
   git update-index --skip-worktree Assets/Normal/Resources/NormcoreAppSettings.asset
   ```

   To commit that file again later, run the same command with `--no-skip-worktree`.

All clients that should meet need the same app key and the same room name. The settings file lies in a `Resources` folder, so **every build made with a key contains it**. Anyone who has the key can join your rooms and hear the voice chat. Do not publish such a build, and do not use a key that you use elsewhere.

Room names:

- All-in-One: `sl-dyad-aio1-r1` to `sl-dyad-aio1-r9`, chosen on the Join page. The prefix is `roomPrefix` in `Assets/For Demo/AllInOne/Content/App Config.asset`.
- Single-role scenes: `Test Room`, set on the `Realtime` component of two prefabs in `Assets/For Demo/Prefabs/Network/`: `Realtime + VR Player Complete Prefab 1` (follower and monitor scenes) and `Realtime + VR Player Complete Prefab 2` (leader scenes).

## 2. Platform

1. *File > Build Settings > Android > Switch Platform*. The first switch re-imports the textures.
2. Connect the headset with USB and allow debugging inside the headset.

The project already sets these values in *Project Settings > Player > Android*:

| Setting | Value |
|---|---|
| Scripting backend | IL2CPP |
| Architecture | ARM64 |
| Minimum API level | 29 |
| Graphics API | Vulkan |
| Colour space | Linear |
| Input handling | Both |
| Application id | `com.jiahaoliu.socialleadingvr` (All-in-One builds use their own, see below) |
| Render pipeline | Built-in |

## 3. OpenXR

*Project Settings > XR Plug-in Management* has OpenXR enabled on Android and on Windows. *Initialize XR on Startup* is on for both. Under *OpenXR*:

| Platform | Setting | Value |
|---|---|---|
| Android | Interaction profile | Oculus Touch Controller Profile |
| Android | Feature | Meta Quest Support |
| Android | Render mode | Single Pass Instanced |
| Windows | Interaction profile | Oculus Touch Controller Profile |
| Windows | Render mode | Single Pass Instanced |

Open *XR Plug-in Management > Project Validation* and fix what it reports before you build.

### Quest 3

The target device list of OpenXR 1.9.1 holds Quest, Quest 2, and Quest Pro. Quest 3 is not in it, so the build does not name Quest 3 in `com.oculus.supportedDevices`. There are two ways to add it: move to an OpenXR version that lists Quest 3, or add the device with an editor script. The second way uses a public method of the package. It was not tested in this release.

```csharp
// Assets/Editor/Quest3Target.cs
using UnityEditor;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

[InitializeOnLoad]
public static class Quest3Target
{
    static Quest3Target() => EditorApplication.delayCall += Add;

    private static void Add()
    {
        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        MetaQuestFeature quest = settings != null ? settings.GetFeature<MetaQuestFeature>() : null;
        if (quest != null) quest.AddTargetDevice("eureka", "Quest 3", true);
    }
}
```

Then check the list under *OpenXR > Android > Meta Quest Support > settings (gear)*.

## 4. Build All-in-One

One build serves every person and every role.

1. Run **Social Leading > All-in-One > Build Android**. The APK is written to `Builds/AllInOne-Android/SocialLeadingVR-AllInOne.apk` (about 47 MB).
2. Install it on every headset:

   ```sh
   adb install -r Builds/AllInOne-Android/SocialLeadingVR-AllInOne.apk
   ```

3. Start it from *Unknown sources* in the headset's library. Choose the same room number on every headset.

The build contains only `AllInOne.unity`, under the product name "Social Leading VR All-in-One" and the application id `com.jiahaoliu.socialleadingvr.allinone`, so it installs next to builds of the single-role scenes. **Build Windows** makes a desktop program in `Builds/AllInOne-Windows` for a PC headset or for mouse and keyboard; it was run with mouse and keyboard only. Command-line builds and the other settings of the build: [ALL_IN_ONE.md](ALL_IN_ONE.md#builds).

## 5. Build the single-role scenes

Each headset runs one scene.

1. Open a leader scene, for example `Assets/For Demo/Scenes/Leader/SLSL_P3.unity`.
2. In *Build Settings* tick this scene and untick the others. All scenes are in the list; `AllInOne` is ticked by default.
3. *Build And Run* to the first headset.
4. Repeat with a follower scene, for example `Assets/For Demo/Scenes/Follower/SLSF_P4.unity`, for the second headset.

All single-role scenes share one application id. Installing a second build on the same headset replaces the first. Change *Package Name* if you want both on one device.

Every build contains all avatar prefabs, because Normcore loads avatars by name from `Resources` folders and Unity packs every such folder. To make a build smaller, rename the `Resources` folders of avatars that no client uses, for example `Avatars/P5/Resources` to `Avatars/P5/Unused`. Do it in the Unity Project window and on every client. Renaming keeps the scene references, and a prefab outside a `Resources` folder is left out of builds whose scene does not use it. *Rebuild Desktop Demo* and the All-in-One builder expect the P3 and P4 folders under their original names.

The monitor needs no build. Run `Assets/For Demo/Scenes/Monitor/Monitor.unity` in the editor.

## 6. Permissions

| Permission | Why | How it is granted |
|---|---|---|
| Microphone (`android.permission.RECORD_AUDIO`) | Normcore voice | Normcore asks when the voice starts: in All-in-One when you join an online room, in the single-role scenes when the avatar connects |
| Internet (`android.permission.INTERNET`) | Normcore | No prompt. The All-in-One build forces it |

The merged manifest of the All-in-One APK lists both. If the microphone prompt never appears or a release build of another scene cannot connect, look at the merged manifest in the APK. Add the missing line to `Assets/Plugins/Android/AndroidManifest.xml`, or set *Player > Other Settings > Internet Access* to *Require*.

```xml
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.INTERNET" />
```

In the single-role scenes voice runs on every avatar, including the monitor; mute the PC microphone if the monitor should be silent. In All-in-One an operator is muted until **Talk to the room** is switched on.

## 7. Run the single-role scenes

1. Start the monitor scene on the PC. Wait until the console shows no connection error.
2. Start the leader and the follower. Their avatars appear in the Game view of the monitor.
3. The console shows `DataLoggerSystem: leader and follower found, recording.` From then on the motion log is written.
4. Click the Game view and press Space for each step. Start only when both headsets are in the room, because a client that joins later plays no narration.

In the headset: the right thumbstick moves a slider, A confirms or accepts, X rejects.

To copy files from a headset:

```sh
adb pull /sdcard/Android/data/com.jiahaoliu.socialleadingvr/files ./quest_files
```

On Windows the editor writes to `%USERPROFILE%\AppData\LocalLow\Jiahao Liu\Social Leading VR`.

## Eye height

All-in-One seats every user with the eyes at 1.6 m, whatever their own height, and does so again with **Recentre**. The single-role scenes use the device tracking origin with *Camera Y Offset* 1.6, so all users start with the same eye height too. For the real height in those scenes, set *Tracking Origin Mode* of the XR Origin to *Floor*.

## Troubleshooting

| Problem | Likely cause | What to do |
|---|---|---|
| **Join room ... online** cannot be pressed | No app key in the build | Paste the key, see [step 1](#1-normcore-key), and build again. Solo works without one |
| "The room could not be reached" | No internet, a wrong key, or no answer within 20 s | Check the network and the key, or use solo |
| Two devices do not meet | Different key, room number, or builds with a different room prefix | Use the same build and the same room number |
| "Waiting for your partner" | An automatic session starts when both roles are filled | Wait, or press **Start in solo instead** |
| "Waiting for the operator" | The session waits for an operator who is not in the room | Press **Switch to automatic** |
| You only watch | Both roles of the room are taken | Choose another room |
| All-in-One starts with mouse and keyboard although a headset is linked | The headset did not render within 2 s of the start | Put the headset on and start the scene again |
| Console shows a Normcore error about the app key | Key is empty | Paste the key, see [step 1](#1-normcore-key) |
| Clients of the single-role scenes do not see each other | Different key or room name, or no internet | Compare `NormcoreAppSettings` and the `Realtime` components |
| An avatar does not spawn | Prefab is not in a `Resources` folder, or two prefabs have the same name | Keep names unique and the folder name `Resources` |
| Follower does not react to the leader | Not exactly two avatars with `GetPartnerTransform` in the room, or `Link Enabled` is off | Check `Source Tag` and `Link Enabled`. With three or more avatars set `Source Tag` to `LeaderTag` |
| Follower snaps when the leader joins or leaves | The single-role scenes switch the link on and off without a fade | Expected there. All-in-One fades |
| App starts as a flat window | OpenXR or Meta Quest Support is off for Android | Check *XR Plug-in Management* |
| Controllers do not move | Interaction profile is missing | Add Oculus Touch Controller Profile |
| A slider of the single-role scenes stops while the stick is held still | The slider reacts to changes of the stick value | Move the stick slightly, or release it and push again |
| A, X, or thumbstick do nothing in the panels of the single-role scenes | The XR Interaction Toolkit sample was re-imported, which removes the added actions `PressA`, `PressB`, `PressX`, `CustomThumbStick` | Restore `XRI Default Input Actions.inputactions` from git |
| No voice | Microphone permission denied, or a solo room | Grant it in the headset settings and restart. Solo has no voice |
| Echo or room noise in voice | The PC microphone is live on the monitor | Mute it |
| Space does nothing on the monitor | Game view has no focus, a clip is playing, or all 13 steps are done | Click the Game view. Restart the scene to start over |
| No narration in the single-role scenes | No clip is assigned, the client joined after step 1, or the scene has no object named `Audio Source` | Assign clips on `MovementStateHandler`, restart the client before the first step |
| Panels of the single-role scenes do not open | Monitor avatar name or signal positions do not match | Compare `monitorObjectName` and the four positions in `UIPlayerInput` |
| View height is wrong | Device tracking origin with a fixed offset | See [Eye height](#eye-height) |
| OpenXR message in the console when you press Play without a headset | XR starts with the player and finds no runtime | Safe to ignore for the desktop scenes |
| The desktop demo shows up in the headset | A headset is connected over Link | Disconnect Link |
