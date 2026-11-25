# Components

Every first-party script, what it does, its inspector fields, and who uses it. *Default* is the value in code. *Set to* is the value in the prefabs or scenes when it differs.

Contents: [Modulation](#modulation), [Avatar](#avatar), [Session](#session), [Task UI](#task-ui), [Logging](#logging), [Demo](#demo).

## Modulation

### GetPartnerTransform

[Source](../Assets/SocialLeading/Scripts/Modulation/GetPartnerTransform.cs). Execution order -60. Finds the avatar that moves this one and copies its head and hands into children 3 to 5.

| Field | Default | Meaning |
|---|---|---|
| `sourceTag` | empty | Tag of the avatar that moves this one. Empty pairs with the only other registered avatar. |
| `linkEnabled` | true | Off lets the avatar keep its own movement |
| `myTransform` | set in `OnEnable` | Own root |
| `sharedEntitiesEditor` | filled in the editor | Read-only view of the registered avatars |

Code access: `Source` (current partner or null), `HasSource`, static `sharedEntitiesStatic`.

Used by: all leader and follower avatar prefabs. Not on the monitor avatar.

### MovementModulator

[Source](../Assets/SocialLeading/Scripts/Modulation/MovementModulator.cs). Abstract base class. `LinkActive` tells whether the avatar is linked. `HeadingAlignment(partnerHead, ownHead)` returns the rotation around the vertical axis between the two headings. No fields.

### HandMixedFollowSystem

[Source](../Assets/SocialLeading/Scripts/Modulation/HandMixedFollowSystem.cs). Order -45. Blends the own hand with the partner's hand.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `myHead`, `myHand` | | raw `Head` and hand | Own raw pose |
| `partnerHead`, `partnerHand` | | `Head Partner`, `... Partner Slow` | Partner pose |
| `modulatedHead` | | `Head Modulated` | Displayed head |
| `followRatio` | 0.5 | 0.3 | 0 own hand, 1 partner's hand |
| `axisRatio` | (1, 1, 1) | | Scales the ratio per axis, in own root space |
| `mirror` | false | | Reflects the partner left-right |
| `mirrorPartnerHand` | none | | Partner hand to read while mirroring. Empty uses `partnerHand`. |

Used by: `Left Hand Modulated` and `Right Hand Modulated` of the follower prefabs.

### HandAddedFollowSystem

[Source](../Assets/SocialLeading/Scripts/Modulation/HandAddedFollowSystem.cs). Order -45. Adds a share of the partner's hand displacement.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `myHead`, `myHand`, `partnerHead`, `modulatedHead` | | as above | |
| `partnerHand` | | `... Partner` (unfiltered) | |
| `addRatio` | 0.5 | 0.35 | Share that is added |
| `maxOffset` | 0 | | Limit in metres, 0 = none |

Used by: same objects as `HandMixedFollowSystem`, disabled.

### HeadAddedFollowSystem

[Source](../Assets/SocialLeading/Scripts/Modulation/HeadAddedFollowSystem.cs). Order -50. Adds a share of the partner's horizontal head displacement.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `myHead`, `partnerHead` | | `Head`, `Head Partner` | |
| `addRatio` | 0.5 | 0.3 | Share that is added |
| `maxOffset` | 0 | | Horizontal limit in metres, 0 = none |

Used by: `Head Modulated` of the follower prefabs.

### HeadMixedFollowSystem

[Source](../Assets/SocialLeading/Scripts/Modulation/HeadMixedFollowSystem.cs). Order -50. Blends the horizontal head position with the partner's. Fields: `myHead`, `partnerHead`, `followRatio` (0.5). Used by: `Head Modulated`, disabled.

### LocalFollowSystem

[Source](../Assets/SocialLeading/Scripts/Modulation/LocalFollowSystem.cs). Order -50. Copies the local pose of `UpStreamTarget`. Used by: modulator objects, disabled. Enable it alone to show the raw pose.

### SlowFollowSystem

[Source](../Assets/SocialLeading/Scripts/Modulation/SlowFollowSystem.cs). Order -55. Low-pass filter on a local pose.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `UpStreamTarget` | | `Left` or `Right Hand Partner` | Pose to follow |
| `lerpTime` | 0.5 | 0.2 | Time constant in seconds, 0 copies directly |

Used by: `Left Hand Partner Slow`, `Right Hand Partner Slow`. Disabled copies sit on the modulator objects.

### ModulationProfile

[Source](../Assets/SocialLeading/Scripts/Modulation/ModulationProfile.cs). Asset. Create it with *Assets > Create > Social Leading > Modulation Profile*.

| Field | Default | Options |
|---|---|---|
| `hands` | Blend | Off, Blend, Superimpose |
| `handRatio` | 0.3 | 0 to 1 |
| `handAxisRatio` | (1, 1, 1) | Blend only |
| `leftHand`, `rightHand` | true | |
| `position` | Off | Off, Superimpose, Blend |
| `positionRatio` | 0.3 | 0 to 1 |
| `maxPositionOffset` | 0 | Metres, 0 = none. Superimpose only |
| `partnerSmoothing` | 0.2 | Seconds |

Used by: [`Demo/DemoProfile.asset`](../Assets/SocialLeading/Demo/DemoProfile.asset).

### ModulationProfileApplier

[Source](../Assets/SocialLeading/Scripts/Modulation/ModulationProfileApplier.cs). Order -70. Writes `profile` into every modulator below it on each frame. Field: `profile`. Used by: the follower in the desktop demo. The networked prefabs do not have it.

## Avatar

### IKTargetFollowVRRig

[Source](../Assets/SocialLeading/Scripts/Avatar/IKTargetFollowVRRig.cs). LateUpdate. Places the body under the head and writes the head and hand poses into the IK targets.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `turnSmoothness` | 0.1 | | Share of the remaining turn per frame at 90 Hz |
| `head`, `leftHand`, `rightHand` | | per prefab | `vrTarget`, `ikTarget`, `trackingPositionOffset`, `trackingRotationOffset` |
| `headBodyPositionOffset` | (0, 0, 0) | (0, -0.54, 0) | Body root relative to the head IK target |
| `headBodyYawOffset` | 0 | | Not used by the current mapping |

Used by: the five `VR Character IK` prefabs. Follower P1 carries its own copy and disables the one of its body prefab.

### IKFootSolver

[Source](../Assets/SocialLeading/Scripts/Avatar/IKFootSolver.cs). Update. Procedural stepping for one foot.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `terrainLayer` | none | Default layer | Layers the ray can hit |
| `body`, `otherFoot` | | per prefab | Body root and the other foot |
| `speed` | 4 | | Step speed, in steps per second |
| `stepDistance` | 0.2 | | Distance that triggers a step, metres |
| `stepLength`, `sideStepLength` | 0.2, 0.1 | | Overshoot forward or sideways, metres |
| `stepHeight` | 0.3 | | Height of the step arc, metres |
| `footOffset`, `footRotOffset` | (0, 0, 0) | `footRotOffset` (-120, 180, 0) | Foot placement |
| `footYPosOffset` | 0.1 | | Height above the hit point |
| `rayStartYOffset`, `rayLength` | 0, 1.5 | | Ray from the body root downward |
| `isMovingForward` | false | | State of the last step, not meant for editing |

Used by: the leg IK targets of all five bodies. In `VR Character IK 3` the layer mask is empty; the P3 avatar prefabs set it.

### SetCameraParent

[Source](../Assets/SocialLeading/Scripts/Avatar/SetCameraParent.cs). Attaches the scene camera to the avatar's head when the avatar belongs to the local user.

| Field | Default | Meaning |
|---|---|---|
| `mainCamera` | none | Camera to attach. Empty uses `Camera.main`. |
| `rootPrefab` | | Avatar root with the `RealtimeView` |

Used by: `Camera Object` in all leader and follower avatar prefabs.

## Session

### ObserverController

[Source](../Assets/SocialLeading/Scripts/Session/ObserverController.cs). Space goes to the next step when no narration is playing.

| Field | Default | Meaning |
|---|---|---|
| `movementTriggers` | none | Found under the rig when empty |
| `rigName` | `XR Origin (XR Rig) 1` | Object that holds `MovementTriggers` |

Used by: `control` in the monitor scene.

### MovementTriggers

[Source](../Assets/SocialLeading/Scripts/Session/MovementTriggers.cs). Moves the monitor rig through a list of positions and starts the narration on the monitor.

| Field | Default | Set to |
|---|---|---|
| `targetPositions` | 13 zero vectors | (2, 0, 1), (4, 0, 1) ... (26, 0, 1) |

Used by: `XR Origin (XR Rig) 1` in the monitor scene.

### MovementStateHandler

[Source](../Assets/SocialLeading/Scripts/Session/MovementStateHandler.cs). Watches the synced monitor position on every client and plays the matching clip.

| Field | Default | Set to |
|---|---|---|
| `targetPositions` | 13 zero vectors | same list as `MovementTriggers` |
| `movementAudioClips` | 13 empty | 13 empty slots, add your own |
| `audioSourceName` | `Audio Source` | |

Used by: `VR Player Complete Prefab 2` (monitor avatar).

## Task UI

### UIPlayerInput

[Source](../Assets/SocialLeading/Scripts/TaskUI/UIPlayerInput.cs). Opens panels on signals from the monitor, reads the controllers, and writes the answers.

| Group | Fields | Default | Set to |
|---|---|---|---|
| Buttons | `pressA_Action`, `pressX_Action`, `customThumbstick` | none | `PressA` (right primary button), `PressX` (left primary button), `CustomThumbStick` (right thumbstick) |
| Panels | `sliderCanvas1/2`, `partnerCanvas1/2`, `finalCanvas`, `ratingCanvas1/2`, `question1Transition`, `question2Transition`, `infoCanvas1/2`, `roleObject1/2`, `waitingCanvas`, `responseCanvas`, `partnerResponse` | none | set per scene |
| Slider speed | `singleTimeCheck` | 0.08 s | 0.05 s |
| | `maxIncrement` | 10 | 100 |
| Screen times | `waitScreenTime` | 0 s | 3 s |
| | `responseScreenTime` | 0 s | 5 s |
| | `questionTransitionTime` | 0 s | 3 s |
| Scripted reply | `rejectThreshold` | 80 | |
| | `acceptText`, `rejectText` | `accepts`, `rejects` | |
| Signal | `monitorObjectName` | `VR Player Complete Prefab 2(Clone)` | |
| | `taskInfoPosition` | (16, 0, 1) | |
| | `rolePosition` | (18, 0, 1) | |
| | `ratingPosition` | (22, 0, 1) | |
| | `splitPosition` | (26, 0, 1) | |
| Output | `outputFolder` | empty (persistent data path) | |
| | `summaryFilePrefix` | `SessionSummary_` | |
| | `responseFilePrefix` | `ResponseLog_` | |
| State, not meant for editing | `ratingVal1/2`, `selectedVal1/2`, `firstDecision`, `secondDecision`, `SplitOn`, `rawStickVector`, `monitor` | | |

Each file name is the prefix, a local timestamp, and `.csv`. `singleTimeCheck` is the shortest time between two slider steps. A step grows while the stick is moved, up to `maxIncrement`.

Used by: `PlayerInputManager` in all leader and follower scenes.

### RatingUI

[Source](../Assets/SocialLeading/Scripts/TaskUI/RatingUI.cs). Rating panel: one slider (1 to 7, whole numbers, start 4) and a label with its value.

Fields: `slider`, `_playerVal` (label), `playerValue` (current value), `ratingCanvas1/2` and `UIPlayerInput` (not read). Method: `ChangeSliderValue(int amount)`. Used by: `RatingPanel1`, `RatingPanel2`.

### SplitUI

[Source](../Assets/SocialLeading/Scripts/TaskUI/SplitUI.cs). Split panel: one slider (0 to 100) divides a total between two labels.

Fields: `slider`, `_playerVal`, `_leftVal`, `total` (100), `playerValue` (current value), four canvas references and `UIPlayerInput` (not read). Method: `ChangeSliderValue(int amount)`. Used by: `SplitPanel1`, `SplitPanel2`. Disabled on the rating panels.

### LoadingEffect

[Source](../Assets/SocialLeading/Scripts/TaskUI/LoadingEffect.cs). Spins an object. `rotationSpeed`: default 100, set to 200 degrees per second. Used by: `WaitingScreen`.

### UISpriteAnimation

[Source](../Assets/SocialLeading/Scripts/TaskUI/UISpriteAnimation.cs). Loops an image through sprites. Fields: `m_Image`, `m_SpriteArray`, `m_Speed` (default 0.02, set to 0.15 seconds per sprite). Used by: the thumbstick hint on the rating and split panels.

## Logging

### DataLoggerSystem

[Source](../Assets/SocialLeading/Scripts/Logging/DataLoggerSystem.cs). Writes one CSV row per physics step with the poses of both avatars. Runs on the monitor.

| Field | Default | Meaning |
|---|---|---|
| `leader`, `follower` | none | Avatar roots, found by tag when empty |
| `leaderHead` ... `modulatedRHand` | none | Nine tracked parts, found by name when empty |
| `leaderTag`, `followerTag` | `LeaderTag`, `FollowerTag` | |
| `headName`, `leftHandName`, `rightHandName` | `Head`, `Left Hand`, `Right Hand` | Raw parts |
| `modulatedHeadName`, `modulatedLeftHandName`, `modulatedRightHandName` | `Head Modulated`, `Left Hand Modulated`, `Right Hand Modulated` | Displayed parts of the follower |
| `folderName` | `System_Movement_Tracking_Data` | Folder in the persistent data path |
| `filePrefix` | `SystemTrackingData_` | A UTC timestamp and `.csv` are added |

Used by: `DataLoggerObject` in the monitor scene.

## Demo

| Script | Purpose | Fields (default) |
|---|---|---|
| [DesktopDemoController](../Assets/SocialLeading/Demo/Scripts/DesktopDemoController.cs) | On-screen panel. Space toggles the link, Tab hides the panel. | `leaderRig`, `followerRig`, `followerLink`, `profile`, `ghost`, `showPanel` (true) |
| [SimulatedRig](../Assets/SocialLeading/Demo/Scripts/SimulatedRig.cs) | Stands in for headset and controllers. Order -100. | `head`, `leftHand`, `rightHand`, `motion` (Idle; Wave, RaiseHands, Talk, Reach, Step), `speed` (1, range 0.25 to 2), `phase` (0), `eyeHeight` (1.6, set to 1.8), `headOffset` (0, -0.08, -0.02), `leftWristOffset`, `rightWristOffset` (identity, set to the wrist offsets of the XR rig) |
| [RawPoseGhost](../Assets/SocialLeading/Demo/Scripts/RawPoseGhost.cs) | Markers at the tracked hands, relative to the displayed head, and a line to the displayed hands | raw and rendered head and hands, `ghostMaterial`, `visible` (true) |
| [OrbitCamera](../Assets/SocialLeading/Demo/Scripts/OrbitCamera.cs) | Right-drag orbits, scroll zooms | `target` (0, 1.15, 0), `distance` (3.4, set to 2.75), `yaw` (-90), `pitch` (8), `maxDistance` (3.6, set to 2.75) |
| [DesktopDemoBuilder](../Assets/SocialLeading/Demo/Editor/DesktopDemoBuilder.cs) | Editor menu that rebuilds the demo scene | none |

Used by: `Demo/DesktopDemo.unity`.
