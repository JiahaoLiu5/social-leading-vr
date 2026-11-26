# Third-party notices

The MIT licence in [LICENSE](LICENSE) covers the source code in this repository only. It does not cover the assets and packages listed here. Each item keeps its own terms. This page states what is known about each item; it is not legal advice. Where the origin or the terms are not certain, the entry says so.

If you hold rights in an item and see a problem, please open an issue.

## Summary

| Content | Path | Owner | Covered by the MIT licence of this project |
|---|---|---|---|
| Characters | `Assets/Shared/Characters/Models` and everything made from them | Adobe (Mixamo) | No |
| Room model | `Assets/Shared/Environment/RoomModel` | natcrot, CC BY 4.0 | No |
| Inter font | `Assets/For Demo/DemoKit/Fonts`, `Assets/For Demo/DemoKit/Runtime/Resources/DemoKit` | The Inter Project Authors, SIL OFL 1.1 | No |
| XR Interaction Toolkit starter assets | `Assets/Samples/XR Interaction Toolkit/2.5.4/Starter Assets` | Unity | No |
| TextMesh Pro essentials | `Assets/ThirdParty/TextMesh Pro` | Unity and others | No |
| Scripts based on tutorials | `Assets/For Demo/Scripts/Avatar`, `Assets/For Study/VR Body` | see below | Only the changes |
| Narration | `Assets/For Study/Sound/Instruction Recording` | not confirmed | No |
| Hand models of the original rigs | `Assets/For Study/Lets Make a VR Game/Oculus Hands` | not confirmed | No |
| Normcore example script | `Assets/For Study/Normal/Examples` | Normal VR, LLC | No |
| Unity packages and Normcore | `Packages/manifest.json` | Unity, Normal VR | No |

## Assets in this repository

### Mixamo characters

| | |
|---|---|
| Path | `Assets/Shared/Characters/Models/` (Ch07, Ch27, Ch28, Ch31, Ch42: FBX files, extracted textures, materials) |
| Made from them | The IK prefabs in `Assets/For Demo/Characters/IK Prefabs/` and `Assets/For Study/Prefab/VR Character IK prefab/`; the characters, material copies, and avatar pictures of All-in-One in `Assets/For Demo/AllInOne/Resources/AllInOne/Characters/`, `Content/Materials/`, and `Content/Thumbnails/` |
| Used by | All scenes |
| Origin | [mixamo.com](https://www.mixamo.com), rigged there |
| Owner | Adobe |
| Terms | Mixamo terms of Adobe. Not covered by the MIT licence of this repository |
| Note | The files are here so that the project opens complete. For your own projects, get the characters from Mixamo under your own Adobe account |

The IK set-up around the characters (constraints, targets, prefabs) was made for this project.

### Room model

| | |
|---|---|
| Path | `Assets/Shared/Environment/RoomModel/` |
| Used by | `Assets/Shared/Environment/Prefabs/WhiteRoomNew.prefab`, all scenes |
| Origin | [White-room1](https://sketchfab.com/3d-models/white-room1-26a61fba0d5d41dcbe01478de9831218) on Sketchfab |
| Author | natcrot |
| Terms | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) |
| Changes | Geometry edited and regrouped (`WhiteRoomRegrouped2.fbx`), materials rebuilt for Unity, mirrors and lights added |
| What you must do | Keep the credit, the licence link, and the note on changes |

### Inter font

| | |
|---|---|
| Path | `Assets/For Demo/DemoKit/Fonts/` (`Inter-Medium.otf`, `Inter-SemiBold.otf`, version 3.015) and the distance field assets made from them in `Assets/For Demo/DemoKit/Runtime/Resources/DemoKit/` |
| Used by | The panels of All-in-One and the DemoKit sample |
| Owner | Copyright 2020 The Inter Project Authors, <https://github.com/rsms/inter> |
| Terms | SIL Open Font License 1.1, text in [`Inter - OFL.txt`](Assets/For%20Demo/DemoKit/Fonts/Inter%20-%20OFL.txt) |
| What you must do | Keep the copyright notice and the licence text with the fonts, and do not sell the fonts by themselves |

### XR Interaction Toolkit starter assets

| | |
|---|---|
| Path | `Assets/Samples/XR Interaction Toolkit/2.5.4/Starter Assets/` |
| Origin | Sample of the Unity package `com.unity.xr.interaction.toolkit` 2.5.4 |
| Owner | Unity Technologies |
| Terms | [Unity Companion License](https://unity3d.com/legal/licenses/unity_companion_license) |
| Changes | Four actions added to `XRI Default Input Actions.inputactions`: `PressA`, `PressB`, `PressX`, `CustomThumbStick`. One generated id in `Presets/XRI Default Gaze Controller.preset` differs. Demo scene not included. |
| What you must do | Use it only with Unity. Keep the notice. Importing the sample again removes the added actions. |

### TextMesh Pro essentials

| | |
|---|---|
| Path | `Assets/ThirdParty/TextMesh Pro/` |
| Origin | Essential resources of `com.unity.textmeshpro` |
| Owner | Unity Technologies |
| Terms | [Unity Companion License](https://unity3d.com/legal/licenses/unity_companion_license), except the two items below |
| Font | Liberation Sans, copyright 2012 Red Hat, Inc., with Reserved Font Name Liberation, [SIL Open Font License 1.1](Assets/ThirdParty/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt) |
| Sprites | EmojiOne sample sprite sheet (`Sprites/EmojiOne.png`, `Resources/Sprite Assets/EmojiOne.asset`). Unity ships this sample with the note that the emojis are provided by EmojiOne (https://www.emojione.com/) and that the licence terms are on that site. EmojiOne is now JoyPixels. The scenes do not show these sprites; they are the default sprite asset in the TextMesh Pro settings. |
| What you must do | Keep the font licence file with the font. Replace the sprite sheet if you do not want to check its terms. |

### Scripts based on tutorials

| | |
|---|---|
| Path | `IKTargetFollowVRRig.cs` and `IKFootSolver.cs` in `Assets/For Demo/Scripts/Avatar/` (changed) and in `Assets/For Study/VR Body/` (original) |
| Origin | Not recorded in the files. They follow public tutorials on full-body VR IK (most likely Valem Tutorials) and on procedural foot placement (most likely the Unity Prototype Series). |
| Terms | No licence statement was found for the originals |
| Changes in the demo copies | Turn speed made independent of the frame rate, direction test corrected, guards, tooltips, and `IKFootSolver.Snap` added |
| What you must do | Treat the parts that come from the originals as outside the MIT licence. Rewrite them if you need clear terms. |

### Hand poses and animator

| | |
|---|---|
| Path | `Assets/Shared/Characters/Animations/` |
| Origin | Made in this project along the lines of the same VR body tutorial (not recorded in the files) |
| Terms | No separate terms known |

### Networked avatar prefabs

| | |
|---|---|
| Path | `Assets/For Demo/Prefabs/Network/`, `Assets/For Demo/AllInOne/Resources/`, and the prefabs in `Assets/For Study/Resources/` and `Assets/For Study/Prefab/` |
| Origin | Built in this project from the structure of the Normcore VR player example (head and two hands with `RealtimeView` and `RealtimeTransform`). No Normcore code or model is included. |
| Owner of the example | Normal VR, LLC |
| Terms | Normcore Terms of Service, see below |

### Panel sprites

| | |
|---|---|
| Path | `Assets/Shared/UI/Sprites/` |
| Origin | Drawings of buttons, a controller, and a loading ring, made for the panels of this project. No source file is recorded. |
| Terms | No separate terms known. The controller drawing shows the Meta mark on one button; Meta's trademark rules apply. |

### Content of For Study

`Assets/For Study` holds the original scenes of research made with the platform, unchanged. Its scenes, scripts, boards, and panels were made for them, except the items below.

| Item | Path | What is known |
|---|---|---|
| Narration | `Sound/Instruction Recording/` (13 MP3 files) | Spoken instructions made for the original scenes. The files name only the encoding software and contain no author, tool, or licence note. The source of the voice was not confirmed for this release. All-in-One plays them in its full session. Do not reuse them outside this project without asking |
| Hand models | `Lets Make a VR Game/Oculus Hands/` (models, animations, two prefabs, `AnimateHandOnInput.cs`) | The folder name follows the "Let's Make a VR Game" tutorial series. The FBX files were exported from Maya and name no author or licence; the models appear to be the sample hands of Meta's Oculus Integration. The source was not confirmed. Used, inactive, by the rigs of the original scenes |
| `VoiceMouthMove.cs` | `Normal/Examples/VR Player/Scripts/` | Example script of Normcore (namespace `Normal.Realtime.Examples`), Normal VR, LLC; Normcore terms. Used, disabled, by the original avatar prefabs |
| Recorded animation | `Recordings/Leading Origin Recordings/` (`VR IK Rig.anim`, 36 s, and its controller) | Made while the original scenes were developed; the file does not say whose movement was recorded. Referenced by a disabled Animator of one original character prefab |

### Settings and manifests

`Assets/XR`, `Assets/XRI`, `Assets/Normal`, and `Assets/Plugins/Android/AndroidManifest.xml` were generated by Unity, the XR packages, and Normcore. The Normcore settings file has an empty app key.

## Packages that Unity downloads

These packages are not part of the repository. Unity fetches them when you open the project. The exact versions are in `Packages/manifest.json` and `Packages/packages-lock.json`.

| Package | Version | Owner | Terms |
|---|---|---|---|
| `com.normalvr.normcore` | 2.12.0 | Normal VR, LLC | Normcore Terms of Service (`LICENSE.md` in the package) |
| `com.unity.animation.rigging` | 1.2.1 | Unity Technologies | Unity Companion License |
| `com.unity.inputsystem` | 1.7.0 | Unity Technologies | Unity Companion License |
| `com.unity.profiling.core` | 1.0.3 | Unity Technologies | Unity Companion License |
| `com.unity.test-framework` | 1.1.33 | Unity Technologies | Unity Companion License |
| `com.unity.textmeshpro` | 3.0.6 | Unity Technologies | Unity Companion License |
| `com.unity.ugui` | 1.0.0 | Unity Technologies | Unity Companion License |
| `com.unity.xr.interaction.toolkit` | 2.5.4 | Unity Technologies | Unity Companion License |
| `com.unity.xr.management` | 4.5.0 | Unity Technologies | Unity Companion License |
| `com.unity.xr.openxr` | 1.9.1 | Unity Technologies | Unity Companion License and Unity Package Distribution License |
| `com.unity.ide.visualstudio` | 2.0.22 | Unity Technologies, Microsoft | MIT |

Packages that these depend on (for example Burst, Mathematics, XR Core Utilities) come with the terms in their own folders.

### Normcore

Normcore is a hosted service with an account, an app key, and usage-based terms. You need your own account. The terms do not allow distributing Normcore or parts of it, so do not copy the package into a fork. Normcore carries voice and movement data of your users through the servers of Normal; read its terms and privacy policy before you use it with other people.

## Content made for this project

The scripts in `Assets/For Demo` (except the scripts based on tutorials above), including All-in-One and the DemoKit apart from its font, the scenes, prefabs, boards, and panels in `Assets/For Demo`, `Assets/For Study`, and `Assets/Shared` (except the items above), and the documentation were made for this project.

## Trademarks

Unity, Meta, Quest, Oculus, OpenXR, Mixamo, Adobe, Normcore, and other names belong to their owners. This project is not affiliated with or endorsed by them.
