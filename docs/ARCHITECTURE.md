# Architecture

This page describes how Social Leading VR is put together: folders and assemblies, the scenes, the avatar hierarchy, the update order, the coupling maths, networking, the session flow of the single-role scenes, and the files it writes. The integrated demo has a page of its own, [ALL_IN_ONE.md](ALL_IN_ONE.md). Fields and defaults: [COMPONENTS.md](COMPONENTS.md).

Contents: [Folders and assemblies](#folders-and-assemblies), [Scenes and roles](#scenes-and-roles), [Avatar hierarchy](#avatar-hierarchy), [Update order](#update-order), [Coupling](#coupling), [Body and camera](#body-and-camera), [Networking](#networking), [Session flow of the monitor](#session-flow-of-the-monitor), [Panels](#panels), [Files written at run time](#files-written-at-run-time), [Desktop demo](#desktop-demo), [DemoKit](#demokit), [Extension points](#extension-points).

## Folders and assemblies

| Folder | Assembly | Contents |
|---|---|---|
| `Assets/For Demo` | `SocialLeading.Demo` | Scenes, coupling scripts, avatars, session shell, the runtime of All-in-One |
| `Assets/For Demo/DesktopDemo/Editor` | `SocialLeading.Demo.Editor` | Builder of the desktop demo |
| `Assets/For Demo/AllInOne/Editor` | `SocialLeading.AllInOne.Editor` | Scene builder, builds, and tests of All-in-One |
| `Assets/For Demo/DemoKit` | `DemoKit`, `DemoKit.Editor` | Interface and input kit, see [DemoKit](#demokit) |
| `Assets/For Study` | `Assembly-CSharp` | The original scenes of research made with the platform, kept for the record with their own scripts and prefabs |
| `Assets/Shared` | none | Character models, hand poses, room, mirror, camera, panel parts: no code |
| `Assets/Samples/XR Interaction Toolkit` | the sample's own | Starter assets with four added input actions |

`For Study` has its own copies of several classes, for example `GetPartnerTransform` and `HandMixedFollowSystem`. The two sets do not meet: the demo assemblies are not referenced automatically, and `For Study` cannot see them. A script that should use the demo classes belongs in `Assets/For Demo`, or in an assembly definition that references `SocialLeading.Demo`. The networked prefabs of the demo are named `Demo ...`, because Normcore finds avatar prefabs by name in any `Resources` folder.

All-in-One plays the narration clips of `For Study`. Otherwise the two folders are independent.

## Scenes and roles

All in `Assets/For Demo/Scenes`.

| Scene | Runs on | What it does |
|---|---|---|
| `AllInOne` | Headset or PC | Every role, chosen inside; see [ALL_IN_ONE.md](ALL_IN_ONE.md) |
| `Leader/SLSL_P1` to `SLSL_P5` | Headset | Sends raw poses and voice. The avatar shows the user's own movement |
| `Follower/SLSF_P1` to `SLSF_P5` | Headset | Sends raw poses and voice. The avatar shows the coupled movement |
| `Monitor/Monitor` | PC | Invisible avatar, fixed side camera, Space key, motion log |
| `DesktopDemo` | PC | Both avatars in one scene, no network |

In the single-role scenes the scene fixes the role, and P1 to P5 are avatar choices. Any leader scene works with any follower scene, because every avatar prefab is part of every build. The leader rig stands at (0, 0, -1.5) facing +z, the follower rig at (0, 0, 1.5) facing -z. Both rigs use the device tracking origin with a camera offset of 1.6 m. All-in-One uses the same two seats.

```mermaid
flowchart TB
  subgraph H1[Leader headset]
    L1[XR Origin 2] --> L2[Leader avatar, owned]
  end
  subgraph H2[Follower headset]
    F1[XR Origin 1] --> F2[Follower avatar, owned]
  end
  subgraph PC[Monitor PC]
    M1[Space key] --> M2[MovementTriggers<br/>moves the rig root]
    M2 --> M3[Monitor avatar, owned, invisible]
    M4[DataLoggerSystem, 50 Hz] --> M5[(CSV)]
  end
  L2 <--> N((Normcore room))
  F2 <--> N
  M3 <--> N
  N --> M4
```

## Avatar hierarchy

Follower avatar prefab. The leader prefab has only the first six children and the body. The networked player of All-in-One, `Demo AllInOne Player`, is built from the Follower P4 prefab with the same first eleven children and loads its body at run time.

```text
Demo VR Player Complete Prefab Follower Pk   RealtimeView, RealtimeTransform, RealtimeAvatar, GetPartnerTransform
  0  Head                                 raw, synced; RealtimeAvatarVoice
  1  Left Hand                            raw, synced
  2  Right Hand                           raw, synced
  3  Head Partner                         copy of the partner's child 0
  4  Left Hand Partner                    copy of the partner's child 1
  5  Right Hand Partner                   copy of the partner's child 2
  6  Left Hand Partner Slow               SlowFollowSystem on child 4
  7  Right Hand Partner Slow              SlowFollowSystem on child 5
  8  Head Modulated                       head modulators
       Camera Object                      SetCameraParent
  9  Left Hand Modulated                  hand modulators
  10 Right Hand Modulated                 hand modulators
  11 VR Character IK k                    body, IKTargetFollowVRRig
```

**Contract:** `GetPartnerTransform` reads children 0 to 2 and writes children 3 to 5 by index. Keep that order.

The body follows `Head Modulated` and the two `Hand Modulated` objects on the follower, and the raw `Head` and hands on the leader. On the leader, `Camera Object` is a child of `Head`. Without a source the modulators pass the user's own pose through, so in All-in-One every body follows the `Modulated` objects, whatever its role.

Each modulator object carries several modulator components. Only one per object is enabled:

| Object | Enabled in the follower prefabs | Present but disabled |
|---|---|---|
| Head Modulated | `HeadAddedFollowSystem`, ratio 0.3 | `HeadMixedFollowSystem` 0.5, `LocalFollowSystem`, `SlowFollowSystem` 0.5 s |
| Left and Right Hand Modulated | `HandMixedFollowSystem`, ratio 0.3 | `HandAddedFollowSystem` 0.35, `LocalFollowSystem`, `SlowFollowSystem` 0.5 s |
| Left and Right Hand Partner Slow | `SlowFollowSystem`, 0.2 s | |

## Update order

The coupling runs in one chain inside `Update`. Execution orders are set in code with `[DefaultExecutionOrder]`.

| Order | Component | Phase | Step |
|---|---|---|---|
| -300 | `LinkWiring` (All-in-One) | LateUpdate | Source and live profile of every body, for the next frame |
| -250 to -120 | `RigMode`, `SessionClock`, `Roster`, `SessionDirector`, `Narrator`, `AllInOneApp` | Update | All-in-One: rig, clock, roles, step |
| -100 | `SimulatedRig`, `DemoDesktopRig` | Update | Desktop demo, simulated partners, desktop body: write raw poses |
| -95 | `DesktopRig` (All-in-One) | Update | Copies the desktop body into the XR rig |
| -90 | `RealtimeAvatar` (Normcore), `PoseFeed` | FixedUpdate, Update, LateUpdate | Copy the local XR rig into the raw children |
| -70 | `ModulationProfileApplier` | Update | Writes profile values into the modulators |
| -60 | `GetPartnerTransform` | Update | Finds the partner and copies its raw poses |
| -55 | `SlowFollowSystem` | Update | Low-pass on the partner's hands |
| -50 | `HeadAddedFollowSystem`, `HeadMixedFollowSystem`, `LocalFollowSystem` | Update | Displayed head |
| -45 | `HandMixedFollowSystem`, `HandAddedFollowSystem` | Update | Displayed hands, which need the displayed head |
| 0 | `IKFootSolver`, `MovementStateHandler`, `UIPlayerInput`, others | Update | Feet, session, panels |
| 0 | `IKTargetFollowVRRig` | LateUpdate | Places the body and writes the IK targets |
| 100 | `ViewRig` (All-in-One) | LateUpdate, before render | Puts the camera at the displayed head |

Animation Rigging evaluates between `Update` and `LateUpdate`. The limbs therefore use the IK targets written in the previous frame.

## Coupling

All modulators work on `localPosition` and `localRotation` under the avatar root. The partner's pose is copied with the same local values, so a movement "forward" in the leader's root space becomes a movement "forward" in the follower's root space, whatever direction the two roots face in the room.

### Link

`GetPartnerTransform` keeps a static list of every avatar that has the component. The list is reset when play starts.

- `sourceTag` empty: the link is active when exactly two avatars are registered.
- `sourceTag` set: the link uses the first registered avatar with that tag. While none is found, it searches twice per second.
- `useExplicitSource` on: the link uses `explicitSource`, set by code. All-in-One works this way, so that a source follows the roles and the chosen topology.
- `linkEnabled` off: `HasSource` is false. The modulators then pass the user's own pose through.

### Heading alignment

`MovementModulator.HeadingAlignment` projects both head directions onto the horizontal plane and returns the rotation around the vertical axis that turns the partner's heading into this avatar's heading. If either head looks straight up or down, the last valid rotation is kept.

### Hands, blend (`HandMixedFollowSystem`)

```text
own     = myHand - myHead
partner = Yaw * (partnerHandSlow - partnerHead)
w       = followRatio * axisRatio                    (per axis, clamped to 0..1)
hand*   = modulatedHead + Lerp(own, partner, w)
```

Ratio 0 leaves the hand alone. Ratio 1 copies the partner's hand. `axisRatio` acts in the follower's root space. With `mirror` on, the partner vector is first reflected left-right in the partner's own heading, and `mirrorPartnerHand` selects which partner hand to read.

### Hands, superimpose (`HandAddedFollowSystem`)

```text
offset += Yaw * (change of (partnerHand - partnerHead)) * addRatio
hand*   = modulatedHead + own + offset               (offset clamped to maxOffset when it is above 0)
```

It reacts to changes and not to absolute position, so the offset can drift. In the prefabs it reads the unfiltered partner hand. With the link off, with ratio 0, or with the component disabled, the offset returns to zero.

### Position, superimpose (`HeadAddedFollowSystem`)

```text
offset.xz += (change of partnerHead).xz * addRatio   (skipped when faster than ignoreAboveSpeed)
head*      = myHead + offset + residual              (offset clamped to maxOffset when it is above 0)
```

With a constant ratio the sum equals `addRatio * (partnerHead(t) - partnerHead(t0))`, where `t0` is the first frame of the link. The vertical axis is untouched. When the link ends or the ratio becomes 0, the offset moves into `residual`, which fades out with the time constant `releaseTime` (plus 2 cm/s, so that it reaches zero); with `releaseTime` 0 it is removed in one frame. All-in-One uses 0.4 s and ignores displacements above 6 m/s (a recentre of the partner); the single-role prefabs keep 0 for both.

### Position, blend (`HeadMixedFollowSystem`)

```text
head*.xz = Lerp(myHead.xz, partnerHead.xz, followRatio)
head*.y  = myHead.y
```

### Smoothing (`SlowFollowSystem`)

```text
x += (target - x) * (1 - exp(-dt / lerpTime))
```

The filter does not depend on the frame rate. `lerpTime` 0 copies the pose. Only the partner's hands are filtered; the partner's head is not.

### Rotations

Head and hand rotations are always the user's own.

### Profiles

A `ModulationProfile` asset holds one coupling design. `ModulationProfileApplier` writes it into every modulator below it on each frame, so edits apply live. Without an applier, the values in the prefab count. The networked prefabs of the single-role scenes have no applier; the player of All-in-One has one, with a profile of its own that `LinkWiring` fills.

Some details follow from how the applier writes values:

- Hands off, or one hand off, means ratio 0 on that hand. The component stays enabled, so the displayed hand follows the user's own hand.
- `handAxisRatio` and `partnerSmoothing` affect hands blend only, because hands superimpose reads the unfiltered partner hand.
- `maxPositionOffset` affects position superimpose only. `HandAddedFollowSystem.maxOffset` is set on the component (0.5 m in All-in-One).
- The smoothing value is written to objects whose name contains `Partner Slow`. The left hand is recognised by a name that starts with `Left`.

## Body and camera

`IKTargetFollowVRRig` places the body root under the head target (offset (0, -0.54, 0) in the prefabs), turns it toward the head's yaw, and copies the three tracked poses into the IK targets. `turnSmoothness` is the share of the remaining turn per frame at 90 Hz; the code converts it so that the turn speed is the same at other frame rates. Each body has four two-bone IK constraints (arms and legs) and one multi-parent constraint (head). `IKFootSolver` steps each foot toward a ray-cast point below the body, one foot at a time, and `Snap()` puts a foot on the floor at once after a body was placed.

In the single-role scenes the view of each headset comes from `Camera Local Object`, the only enabled camera that is tagged `MainCamera`. The camera of the XR rig is disabled and only provides tracking. When an avatar is locally owned, `SetCameraParent` attaches the scene camera to `Camera Object`. The follower therefore sees from the coupled head. The script releases the camera when the room disconnects and when the avatar is destroyed.

All-in-One keeps the camera out of the avatar instead, so that the view survives leaving a room; see [ALL_IN_ONE.md](ALL_IN_ONE.md#view-mirror-and-quality).

## Networking

| Item | Single-role scenes | All-in-One |
|---|---|---|
| Library | Normcore 2.12.0 | Normcore 2.12.0 |
| App key | `Assets/Normal/Resources/NormcoreAppSettings.asset`, empty in the repository | same; solo uses none |
| Room | `Test Room`, joined on start (`Realtime` component in the two `Realtime + VR Player Complete Prefab` prefabs) | `sl-dyad-aio1-r1` to `-r9`, joined from the panel; `solo` offline |
| Avatar spawn | `RealtimeAvatarManager` instantiates *Local Avatar Prefab* by name from a `Resources` folder | the same, always `Demo AllInOne Player` |
| Synced poses | Root (position, rotation, scale), head and hands (position, rotation), all interpolated | the same, and finger poses |
| Voice | From the `Head` object of every avatar, including the monitor | Only online and only in the room; operators and spectators muted |
| Other synced data | none | `PlayerModel` per player, one `SessionModel` per room |
| Not synced | Coupled poses, panel state, slider values, step index | Coupled poses, panels, answers |

The local rig is mapped in each scene: root to the XR Origin, head to `Head VR Target`, hands to `Left Hand VR Target` and `Right Hand VR Target`. The hand targets carry the fixed offset between the controller and the wrist. The head target sits 8 cm below and 2 cm behind the tracked camera.

In the single-role scenes no first-party script defines a Normcore model. Two first-party scripts use the Normcore API at run time: `SetCameraParent` (ownership check, disconnect event) and `MovementTriggers` (ownership request, only if the rig has a `RealtimeTransform`). The editor script `DesktopDemoBuilder` removes Normcore components from a copy of the prefabs. The two models of All-in-One, roles, authority, and joining are described in [ALL_IN_ONE.md](ALL_IN_ONE.md#avatars-and-roles-for-everybody).

## Session flow of the monitor

The monitor has no message channel of its own. It uses the synced position of its avatar root as the signal.

1. `ObserverController` reads Space and calls `MovementTriggers.MoveToNextPosition`.
2. `MovementTriggers` moves the monitor rig to the next target position. Normcore syncs the root of the monitor avatar.
3. On every client, `MovementStateHandler` on the monitor avatar compares the root with the next target position. On a match it plays the clip with the same index on the scene object named `Audio Source` and advances its own counter. An empty slot is a silent step. On the monitor, `MovementTriggers` starts the clip directly.
4. On the headsets, `UIPlayerInput` compares the same root with four signal positions and opens panels.
5. On the monitor, Space is ignored until the clip has finished. The clip slots are empty in the demo scenes.

| Step | Monitor root | Clip slot | Panel on the headsets |
|---|---|---|---|
| 1 to 7 | x = 2, 4, 6, 8, 10, 12, 14 | 1 to 7 | |
| 8 | x = 16 | 8 | Two information boards |
| 9 | x = 18 | 9 | Extra text on the boards |
| 10 | x = 20 | 10 | |
| 11 | x = 22 | 11 | Two rating panels, one after the other |
| 12 | x = 24 | 12 | |
| 13 | x = 26 | 13 | Split panels, two rounds with scripted proposals and a scripted reply after the first |

All positions have y = 0 and z = 1. The monitor rig starts at (0, 0, 1). The three lists (`MovementTriggers`, `MovementStateHandler`, `UIPlayerInput`) must agree. Narration follows the order of the list, so a client that joins after step 1 plays no clips. Panels check each position on its own and do not depend on that order. All-in-One replaces this signal with a shared session model; see [ALL_IN_ONE.md](ALL_IN_ONE.md#session-as-data).

## Panels

`UIPlayerInput` sits on `PlayerInputManager` in every leader and follower scene. The panels are world-space canvases with placeholder texts. Each client has its own copy and its own answers. The right thumbstick moves the slider, A confirms or accepts, X rejects. The slider reacts to changes of the stick value, so a stick that is held perfectly still does not repeat.

```mermaid
stateDiagram-v2
  direction LR
  [*] --> Boards: x = 16
  Boards --> BoardsText: x = 18
  [*] --> Transition1: x = 22
  Transition1 --> Rating1: 3 s
  Rating1 --> Transition2: A
  Transition2 --> Rating2: 3 s
  Rating2 --> [*]: A
  [*] --> Split1: x = 26
  Split1 --> Wait1: A
  Wait1 --> Proposal1: 3 s
  Proposal1 --> Wait2: A or X
  Wait2 --> Reply: 3 s
  Reply --> Split2: 5 s
  Split2 --> Wait3: A
  Wait3 --> Proposal2: 3 s
  Proposal2 --> Final: A or X
```

The proposals are fixed texts in the panel prefabs. The reply text depends on the local slider value only: at or above `rejectThreshold` (80) it shows `rejectText`, otherwise `acceptText`. Nothing is exchanged with the other client.

## Files written at run time

All files go to `Application.persistentDataPath` on the client that writes them. No file is uploaded.

| File | Client | Content |
|---|---|---|
| `System_Movement_Tracking_Data/SystemTrackingData_<UTC time>.csv` | Monitor | One row per physics step (50 Hz at the default fixed time step): frame, time, world position and Euler angles of the leader's and follower's raw head and hands, and world position of the follower's displayed head and hands. 47 columns |
| `SessionSummary_<local time>.csv` | Leader and follower | Header at start, one row after the last decision. Six columns: two split values, two decisions (`True` or `False`), two ratings |
| `ResponseLog_<local time>.csv` | Leader and follower | Header at start, one row per response, written at once: UTC time, scene name, response name, value |
| `all-in-one-answers.csv` | All-in-One | One row per answer: local time, session number, answer, value. Deleted with **Delete my answers** |

The motion log starts when both avatars are found by tag (`LeaderTag`, `FollowerTag`). It is flushed once per second, on pause, and on quit. Numbers always use a decimal point. A space follows some of the commas, in the header and in the rows, so read the file with, for example, `pandas.read_csv(path, skipinitialspace=True)`. The file prefixes and folders are fields of `DataLoggerSystem` and `UIPlayerInput`. The displayed poses in the log are the ones computed on the monitor, from the poses it received.

## Desktop demo

`DesktopDemoBuilder` builds `Scenes/DesktopDemo.unity` from the room and two avatars that keep the hierarchy, modulators, and IK of the networked prefabs. `SimulatedRig` takes the place of the headset and controllers and writes the raw children. `DesktopDemoController` draws the panel and works on a copy of the profile. `RawPoseGhost` draws the markers that show the tracked hands. The leader is avatar P3, the follower is avatar P4. The leader prefab has no modulators, so it shows its own movement.

The menu *Social Leading > Rebuild Desktop Demo* runs the builder again, for example after a change to the avatar prefabs. It opens the monitor scene to take over the room and discards unsaved changes in the open scene, so save first. All-in-One takes its room from this scene, so rebuild All-in-One afterwards.

## DemoKit

[`Assets/For Demo/DemoKit`](../Assets/For%20Demo/DemoKit) is a small kit for interfaces in VR, used by the All-in-One scene. Nothing of it has to be in a scene; panels and pointers are made in code.

- **Panels** in world space (uGUI and TextMesh Pro) with a fluent builder: text, buttons, picture cards, steppers, sliders, switches, pages, status and progress. Units are millimetres.
- **Pointers** for controller rays, fingertips, and the mouse, with hover, capture, and debouncing per pointer. A panel can hang on a hand and let the other hand's ray through.
- **Captions, messages, and a view fade.**
- **`DemoDesktopRig`**, a body with a head and two hands that is moved with mouse and keyboard, for work without a headset.
- Elements can be found and clicked from code by id, and texts that do not fit are reported; the tests use both.

It needs the Input System, uGUI, and TextMesh Pro, and does not depend on the XR Interaction Toolkit, OpenXR, or an EventSystem. Its font is Inter (SIL Open Font License, text in `DemoKit/Fonts`). Menu **Demo Kit > Create Sample Scene** shows all widgets; the code of the sample is in `Runtime/Sample`.

```csharp
using DemoKit;

DemoUi.Panel("hello", 800f)
    .Title("Hello", "A panel in front of you.")
    .Buttons(id => DemoUi.Toast("You pressed " + id), ("yes", "Yes"), ("no", "No"))
    .Build()
    .ShowInFrontOf(distance: 1.4f);
```

## Extension points

| To add | Do this |
|---|---|
| A coupling rule | Derive from `MovementModulator`, use `LinkActive` and `HeadingAlignment`, write the local pose of a `Modulated` object |
| A pose source | Write the three raw children before order -60, as `SimulatedRig` does |
| A transport | Sync the root and the three raw children; replace the run-time uses of the Normcore API |
| A step of the monitor | Extend the position and clip lists, and add a check in `UIPlayerInput` |
| A step of All-in-One | An element of the session asset, see [ALL_IN_ONE.md](ALL_IN_ONE.md#add-or-change-a-step) |

Recipes: [CUSTOMIZE.md](CUSTOMIZE.md).
