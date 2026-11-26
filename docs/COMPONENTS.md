# Components

Every first-party script in `Assets/For Demo`, what it does, its inspector fields, and who uses it. *Default* is the value in code. *Set to* is the value in the prefabs or scenes when it differs. The scripts of `Assets/For Study` are the original ones and are not described here.

Contents: [Modulation](#modulation), [Avatar](#avatar), [Session](#session), [Task UI](#task-ui), [Logging](#logging), [Desktop demo](#desktop-demo), [All-in-One](#all-in-one), [DemoKit](#demokit).

## Modulation

### GetPartnerTransform

[Source](../Assets/For%20Demo/Scripts/Modulation/GetPartnerTransform.cs). Execution order -60. Finds the avatar that moves this one and copies its head and hands into children 3 to 5.

| Field | Default | Meaning |
|---|---|---|
| `sourceTag` | empty | Tag of the avatar that moves this one. Empty pairs with the only other registered avatar. |
| `linkEnabled` | true | Off lets the avatar keep its own movement |
| `useExplicitSource` | false | Ignore the tags and use `explicitSource`. On in All-in-One |
| `explicitSource` | none | The avatar that moves this one, set by code (`LinkWiring`) |
| `myTransform` | set in `OnEnable` | Own root |
| `sharedEntitiesEditor` | filled in the editor | Read-only view of the registered avatars |

Code access: `Source` (current partner or null), `HasSource`, static `sharedEntitiesStatic`.

Used by: all leader and follower avatar prefabs and `Demo AllInOne Player`. Not on the monitor avatar.

### MovementModulator

[Source](../Assets/For%20Demo/Scripts/Modulation/MovementModulator.cs). Abstract base class. `LinkActive` tells whether the avatar is linked. `HeadingAlignment(partnerHead, ownHead)` returns the rotation around the vertical axis between the two headings. No fields.

### HandMixedFollowSystem

[Source](../Assets/For%20Demo/Scripts/Modulation/HandMixedFollowSystem.cs). Order -45. Blends the user's own hand with the partner's hand.

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

[Source](../Assets/For%20Demo/Scripts/Modulation/HandAddedFollowSystem.cs). Order -45. Adds a share of the partner's hand displacement.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `myHead`, `myHand`, `partnerHead`, `modulatedHead` | | as above | |
| `partnerHand` | | `... Partner` (unfiltered) | |
| `addRatio` | 0.5 | 0.35 | Share that is added |
| `maxOffset` | 0 | 0.5 in All-in-One | Limit in metres, 0 = none |

The offset returns to zero while the link is off, the ratio is 0, or the component is disabled. Used by: same objects as `HandMixedFollowSystem`, disabled.

### HeadAddedFollowSystem

[Source](../Assets/For%20Demo/Scripts/Modulation/HeadAddedFollowSystem.cs). Order -50. Adds a share of the partner's horizontal head displacement.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `myHead`, `partnerHead` | | `Head`, `Head Partner` | |
| `addRatio` | 0.5 | 0.3 | Share that is added |
| `maxOffset` | 0 | | Horizontal limit in metres, 0 = none |
| `releaseTime` | 0 | 0.4 in All-in-One | Time constant in seconds of the fade after the link ends or the ratio becomes 0; 0 removes the offset at once |
| `ignoreAboveSpeed` | 0 | 6 in All-in-One | Partner displacement faster than this (metres per second) is not added; 0 adds everything |

Used by: `Head Modulated` of the follower prefabs and of `Demo AllInOne Player`.

### HeadMixedFollowSystem

[Source](../Assets/For%20Demo/Scripts/Modulation/HeadMixedFollowSystem.cs). Order -50. Blends the horizontal head position with the partner's. Fields: `myHead`, `partnerHead`, `followRatio` (0.5). Used by: `Head Modulated`, disabled.

### LocalFollowSystem

[Source](../Assets/For%20Demo/Scripts/Modulation/LocalFollowSystem.cs). Order -50. Copies the local pose of `UpStreamTarget`. Used by: modulator objects, disabled. Enable it alone to show the raw pose.

### SlowFollowSystem

[Source](../Assets/For%20Demo/Scripts/Modulation/SlowFollowSystem.cs). Order -55. Low-pass filter on a local pose.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `UpStreamTarget` | | `Left` or `Right Hand Partner` | Pose to follow |
| `lerpTime` | 0.5 | 0.2 | Time constant in seconds, 0 copies directly |

Used by: `Left Hand Partner Slow`, `Right Hand Partner Slow`. Disabled copies sit on the modulator objects.

### ModulationProfile

[Source](../Assets/For%20Demo/Scripts/Modulation/ModulationProfile.cs). Asset. Create it with *Assets > Create > Social Leading > Modulation Profile*.

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

Used by: [`DesktopDemo/DemoProfile.asset`](../Assets/For%20Demo/DesktopDemo/DemoProfile.asset), and in All-in-One by `Free Play Profile`, `Study Profile`, and one live profile per body made at run time.

### ModulationProfileApplier

[Source](../Assets/For%20Demo/Scripts/Modulation/ModulationProfileApplier.cs). Order -70. Writes `profile` into every modulator below it on each frame. Field: `profile`. Used by: the follower in the desktop demo and `Demo AllInOne Player`. The networked prefabs of the single-role scenes do not have it.

## Avatar

### IKTargetFollowVRRig

[Source](../Assets/For%20Demo/Scripts/Avatar/IKTargetFollowVRRig.cs). LateUpdate. Places the body under the head and writes the head and hand poses into the IK targets.

| Field | Default | Set to | Meaning |
|---|---|---|---|
| `turnSmoothness` | 0.1 | | Share of the remaining turn per frame at 90 Hz |
| `head`, `leftHand`, `rightHand` | | per prefab | `vrTarget`, `ikTarget`, `trackingPositionOffset`, `trackingRotationOffset` |
| `headBodyPositionOffset` | (0, 0, 0) | (0, -0.54, 0) | Body root relative to the head IK target |
| `headBodyYawOffset` | 0 | | Not used by the current mapping |

Used by: the five `VR Character IK` prefabs. Follower P1 carries its own copy and disables the one of its body prefab.

### IKFootSolver

[Source](../Assets/For%20Demo/Scripts/Avatar/IKFootSolver.cs). Update. Procedural stepping for one foot.

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

`Snap()` puts the foot on the floor below the body at once; it runs at start and after a body was placed. Used by: the leg IK targets of all five bodies. In `VR Character IK 3` the layer mask is empty; the P3 avatar prefabs set it.

### SetCameraParent

[Source](../Assets/For%20Demo/Scripts/Avatar/SetCameraParent.cs). Attaches the scene camera to the avatar's head when the avatar belongs to the local user.

| Field | Default | Meaning |
|---|---|---|
| `mainCamera` | none | Camera to attach. Empty uses `Camera.main`. |
| `rootPrefab` | | Avatar root with the `RealtimeView` |

Used by: `Camera Object` in all leader and follower avatar prefabs.

## Session

### ObserverController

[Source](../Assets/For%20Demo/Scripts/Session/ObserverController.cs). Space goes to the next step when no narration is playing.

| Field | Default | Meaning |
|---|---|---|
| `movementTriggers` | none | Found under the rig when empty |
| `rigName` | `XR Origin (XR Rig) 1` | Object that holds `MovementTriggers` |

Used by: `control` in the monitor scene.

### MovementTriggers

[Source](../Assets/For%20Demo/Scripts/Session/MovementTriggers.cs). Moves the monitor rig through a list of positions and starts the narration on the monitor.

| Field | Default | Set to |
|---|---|---|
| `targetPositions` | 13 zero vectors | (2, 0, 1), (4, 0, 1) ... (26, 0, 1) |

Used by: `XR Origin (XR Rig) 1` in the monitor scene.

### MovementStateHandler

[Source](../Assets/For%20Demo/Scripts/Session/MovementStateHandler.cs). Watches the synced monitor position on every client and plays the matching clip.

| Field | Default | Set to |
|---|---|---|
| `targetPositions` | 13 zero vectors | same list as `MovementTriggers` |
| `movementAudioClips` | 13 empty | 13 empty slots, add your own |
| `audioSourceName` | `Audio Source` | |

Used by: `Demo VR Player Complete Prefab 2` (monitor avatar).

## Task UI

### UIPlayerInput

[Source](../Assets/For%20Demo/Scripts/TaskUI/UIPlayerInput.cs). Opens panels on signals from the monitor, reads the controllers, and writes the answers.

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
| Signal | `monitorObjectName` | `Demo VR Player Complete Prefab 2(Clone)` | |
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

[Source](../Assets/For%20Demo/Scripts/TaskUI/RatingUI.cs). Rating panel: one slider (1 to 7, whole numbers, start 4) and a label with its value.

Fields: `slider`, `_playerVal` (label), `playerValue` (current value), `ratingCanvas1/2` and `UIPlayerInput` (not read). Method: `ChangeSliderValue(int amount)`. Used by: `RatingPanel1`, `RatingPanel2`, and the task panels of All-in-One.

### SplitUI

[Source](../Assets/For%20Demo/Scripts/TaskUI/SplitUI.cs). Split panel: one slider (0 to 100) divides a total between two labels.

Fields: `slider`, `_playerVal`, `_leftVal`, `total` (100), `playerValue` (current value), four canvas references and `UIPlayerInput` (not read). Method: `ChangeSliderValue(int amount)`. Used by: `SplitPanel1`, `SplitPanel2`, and the task panels of All-in-One. Disabled on the rating panels.

### LoadingEffect

[Source](../Assets/For%20Demo/Scripts/TaskUI/LoadingEffect.cs). Spins an object. `rotationSpeed`: default 100, set to 200 degrees per second. Used by: `WaitingScreen`.

### UISpriteAnimation

[Source](../Assets/For%20Demo/Scripts/TaskUI/UISpriteAnimation.cs). Loops an image through sprites. Fields: `m_Image`, `m_SpriteArray`, `m_Speed` (default 0.02, set to 0.15 seconds per sprite). Used by: the thumbstick hint on the rating and split panels.

## Logging

### DataLoggerSystem

[Source](../Assets/For%20Demo/Scripts/Logging/DataLoggerSystem.cs). Writes one CSV row per physics step with the poses of both avatars. Runs on the monitor.

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

## Desktop demo

| Script | Purpose | Fields (default) |
|---|---|---|
| [DesktopDemoController](../Assets/For%20Demo/DesktopDemo/Scripts/DesktopDemoController.cs) | On-screen panel. Space toggles the link, Tab hides the panel. | `leaderRig`, `followerRig`, `followerLink`, `profile`, `ghost`, `showPanel` (true) |
| [SimulatedRig](../Assets/For%20Demo/DesktopDemo/Scripts/SimulatedRig.cs) | Stands in for headset and controllers. Order -100. | `head`, `leftHand`, `rightHand`, `motion` (Idle; Wave, RaiseHands, Talk, Reach, Step; Nod, RaiseLeft, RaiseRight, RaiseBoth, HighFive, Handshake, Clap for simulated partners), `speed` (1, range 0.25 to 2), `phase` (0), `eyeHeight` (1.6, set to 1.8), `headOffset` (0, -0.08, -0.02), `leftWristOffset`, `rightWristOffset` (identity, set to the wrist offsets of the XR rig) |
| [RawPoseGhost](../Assets/For%20Demo/DesktopDemo/Scripts/RawPoseGhost.cs) | Markers at the tracked hands, relative to the displayed head, and a line to the displayed hands | raw and rendered head and hands, `ghostMaterial`, `visible` (true) |
| [OrbitCamera](../Assets/For%20Demo/DesktopDemo/Scripts/OrbitCamera.cs) | Right-drag orbits, scroll zooms | `target` (0, 1.15, 0), `distance` (3.4, set to 2.75), `yaw` (-90), `pitch` (8), `maxDistance` (3.6, set to 2.75) |
| [DesktopDemoBuilder](../Assets/For%20Demo/DesktopDemo/Editor/DesktopDemoBuilder.cs) | Editor menu that rebuilds the demo scene | none |

Used by: `Scenes/DesktopDemo.unity`. `SimulatedRig` also moves the simulated partners of All-in-One; `RestartMotion()` starts a motion from its beginning.

## All-in-One

In [`Assets/For Demo/AllInOne/Scripts`](../Assets/For%20Demo/AllInOne/Scripts), namespace `SocialLeading.AllInOne`. How the parts work together: [ALL_IN_ONE.md](ALL_IN_ONE.md). The scene builder creates and wires every component; most sit on the objects `App`, `Networking`, `Session`, and `Rig` of `AllInOne.unity`. Fields that only hold references are not listed. *Order* is the execution order.

### App and networking

| Class | Order | Purpose |
|---|---|---|
| [AllInOneApp](../Assets/For%20Demo/AllInOne/Scripts/Core/App/AllInOneApp.cs) | -120 | The way through the app: states `Menu`, `Connecting`, `Lobby`, `InRoom`, `Leaving`; joining, entering, leaving, seats, fades, choices of the panels |
| [AppConfig](../Assets/For%20Demo/AllInOne/Scripts/Core/App/AppConfig.cs) | asset | Texts, `roomPrefix` (`sl-dyad-aio1-r`), `soloRoom`, `roomCount` (9), `eyeHeight` (1.6), places of the lobby, the operator, and the panels, walking area, catalogs, `studyProfile`, `playProfile`, `playMaxPositionOffset` (0.6 m) |
| [LocalChoices](../Assets/For%20Demo/AllInOne/Scripts/Core/App/LocalChoices.cs) | | The user's choices, which survive leaving a room; the enums `Experience`, `Director`, `Pace`, `ParticipantKind` |
| [AnswerStore](../Assets/For%20Demo/AllInOne/Scripts/Core/App/AnswerStore.cs) | static | `Add`, `DeleteAll`, `HasAnswers`, `FilePath`; `Folder` for tests |
| [RoomConnector](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/RoomConnector.cs) | | `JoinOnline(room)`, `JoinSolo()`, `Leave()`; events `Connected`, `Failed`, `Lost`; `Offline`; `HasKey`, which never shows the key. Gives up after 20 s |
| [NetworkPlayer](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/NetworkPlayer.cs), [PlayerModel](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/PlayerModel.cs) | | One person. The owner writes; every client dresses, tags, shows, and mutes the body from the model |
| [SessionState](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/SessionState.cs), [SessionModel](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/SessionModel.cs) | | The session of the room, its commands, and all rights (`MayChangeAvatar`, `MayChangeRole`, `MayChangeLink`, `MayOperate`, and others); event `Changed` |
| [Roster](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/Roster.cs) | -180 | Everybody in the room, networked or simulated: `People`, `Local`, `RoleOf`, `ByRole`, `IsTaken`, `AllDone`, `ElectedId`, `OperatorPresent` |
| [RoleResolver](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/RoleResolver.cs), [AuthorityElection](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/AuthorityElection.cs) | static | Roles and leftovers from the players' claims; the client that advances an automatic session, after 1 s |
| [SessionClock](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/SessionClock.cs) | -200 | `Now`: room time online, a frame clock offline; `Valid`; `TestSpeed` for tests |
| [IParticipant](../Assets/For%20Demo/AllInOne/Scripts/Core/Net/IParticipant.cs) | | What the roster needs to know of a networked player or a simulated partner |

### Session

| Class | Order | Purpose |
|---|---|---|
| [StudyScript](../Assets/For%20Demo/AllInOne/Scripts/Core/Study/StudyScript.cs) | asset | Steps (`StudyStep`, `BotCue`, `AdvanceRule`), countdowns, gaps; `Length`, `Window`, `LongestDuration`, `UsualDuration` |
| [StepTimeline](../Assets/For%20Demo/AllInOne/Scripts/Core/Study/StepTimeline.cs) | static | `Evaluate` turns step, start, and now into a `StepView`; `ShouldAdvance` |
| [SessionDirector](../Assets/For%20Demo/AllInOne/Scripts/Core/Study/SessionDirector.cs) | -150 | `View` and `Step` for every client; advances an automatic session on the elected client. `EarlyPause` 0.6 s |
| [Narrator](../Assets/For%20Demo/AllInOne/Scripts/Core/Study/Narrator.cs) | -140 | Plays the clip of the step from the right second and shows its captions; `Muted` |
| [StudyStage](../Assets/For%20Demo/AllInOne/Scripts/Core/Study/StudyStage.cs) | abstract | What a project shows per step: `Apply(view)` every frame, `Clear`, `TaskOpen`, `UsesThumbstick` |
| [RaiseHandDetector](../Assets/For%20Demo/AllInOne/Scripts/Core/Study/RaiseHandDetector.cs) | | The left hand 10 cm above the head for 0.7 s |
| [DyadStage](../Assets/For%20Demo/AllInOne/Scripts/Dyad/DyadStage.cs) | | The stage of this project: two wall boards and the private task panels, by cue |
| [DyadTaskFlow](../Assets/For%20Demo/AllInOne/Scripts/Dyad/DyadTaskFlow.cs) | | The pages of the task panels; buttons, rays, and keys at the same time; events `Answered`, `Completed` |
| [DyadTaskConfig](../Assets/For%20Demo/AllInOne/Scripts/Dyad/DyadTaskConfig.cs) | asset | Times of the pages (3, 3, 5 s), the scripted reply, stick dead zone 0.1, slider intervals 0.05 and 0.25 s |

### Bodies and link

| Class | Order | Purpose |
|---|---|---|
| [AvatarBody](../Assets/For%20Demo/AllInOne/Scripts/Core/Avatars/AvatarBody.cs) | 50 | The eleven transforms of a body, its live profile, `SetAvatar`, `SetVisible`, `Snap`, `ViewOffset` |
| [AvatarCatalog](../Assets/For%20Demo/AllInOne/Scripts/Core/Avatars/AvatarCatalog.cs) | asset | Id, label, `Resources` path, and picture per avatar |
| [AvatarDresser](../Assets/For%20Demo/AllInOne/Scripts/Core/Avatars/AvatarDresser.cs), [HumanoidDresser](../Assets/For%20Demo/AllInOne/Scripts/Dyad/HumanoidDresser.cs) | | Load a character on demand and wire its IK to the displayed poses; the newest request wins |
| [HandPoseDriver](../Assets/For%20Demo/AllInOne/Scripts/Core/Avatars/HandPoseDriver.cs) | | Trigger and grip move the fingers (`Left Pinch`, `Left Grab`, `Right Pinch`, `Right Grab`), smoothed with 0.06 s |
| [PoseFeed](../Assets/For%20Demo/AllInOne/Scripts/Core/Avatars/PoseFeed.cs) | -90 | Copies the rig into the preview body of the menu |
| [LinkWiring](../Assets/For%20Demo/AllInOne/Scripts/Core/Link/LinkWiring.cs) | -300 | Source and live profile of every body, with ramps of 0.7 s (`RampSeconds`) |
| [RoleCatalog](../Assets/For%20Demo/AllInOne/Scripts/Core/Link/RoleCatalog.cs), [TopologyCatalog](../Assets/For%20Demo/AllInOne/Scripts/Core/Link/TopologyCatalog.cs) | assets | Roles with label, sentence, tag, and seat; who moves whom |
| [SoloPartners](../Assets/For%20Demo/AllInOne/Scripts/Core/Solo/SoloPartners.cs), [BotPartner](../Assets/For%20Demo/AllInOne/Scripts/Core/Solo/BotPartner.cs), [BotMover](../Assets/For%20Demo/AllInOne/Scripts/Core/Solo/BotMover.cs) | | Simulated partners: one per free role, id 1000 + role, cues per step, walking at 1 m/s |

### Panels, view, and tests

| Class | Order | Purpose |
|---|---|---|
| [StartPanel](../Assets/For%20Demo/AllInOne/Scripts/Core/UI/StartPanel.cs), [LobbyPanel](../Assets/For%20Demo/AllInOne/Scripts/Core/UI/LobbyPanel.cs) | | The four start pages; the lobby of online rooms |
| [SessionHud](../Assets/For%20Demo/AllInOne/Scripts/Core/UI/SessionHud.cs) | | The session bar at the lower edge of the view |
| [RoomMenu](../Assets/For%20Demo/AllInOne/Scripts/Core/UI/RoomMenu.cs), [RoomPanel](../Assets/For%20Demo/AllInOne/Scripts/Core/UI/RoomPanel.cs), [LinkPanel](../Assets/For%20Demo/AllInOne/Scripts/Core/UI/LinkPanel.cs) | | The menu and its pages, on the left hand or beside the desktop view |
| [OperatorPanel](../Assets/For%20Demo/AllInOne/Scripts/Core/UI/OperatorPanel.cs) | | The operator's controls |
| [PanelParts](../Assets/For%20Demo/AllInOne/Scripts/Core/UI/PanelParts.cs) | | `ChoiceGroup` and shared building blocks of the panels |
| [RigMode](../Assets/For%20Demo/AllInOne/Scripts/Core/View/RigMode.cs) | -250 | Headset or desktop, decided once; `ForceDesktop`, and `SimulateHeadset` for tests |
| [DesktopRig](../Assets/For%20Demo/AllInOne/Scripts/Core/View/DesktopRig.cs) | -95 | Feeds the desktop body of the kit into the XR rig; the line with the keys |
| [Seating](../Assets/For%20Demo/AllInOne/Scripts/Core/View/Seating.cs), [StickMove](../Assets/For%20Demo/AllInOne/Scripts/Core/View/StickMove.cs) | | Seat and recentre; walking at 1.2 m/s and turns of 30 degrees with the sticks |
| [ViewRig](../Assets/For%20Demo/AllInOne/Scripts/Core/View/ViewRig.cs) | 100 | Camera and pointers at the tracked pose plus the offset of the displayed head |
| [AppInput](../Assets/For%20Demo/AllInOne/Scripts/Core/View/AppInput.cs) | -260 | Buttons and keys of the app, whatever the device |
| [MirrorGate](../Assets/For%20Demo/AllInOne/Scripts/Core/View/MirrorGate.cs) | | Renders the mirror only when it is seen, at half rate on a standalone headset |
| [QualityTuner](../Assets/For%20Demo/AllInOne/Scripts/Core/View/QualityTuner.cs) | -400 | Quality per device, restored when the scene ends |
| [DemoCommandLine](../Assets/For%20Demo/AllInOne/Scripts/Core/Testing/DemoCommandLine.cs) | -500 | Command-line control for networked tests |
| [DemoProbe](../Assets/For%20Demo/AllInOne/Scripts/Core/Testing/DemoProbe.cs), [ViewCapture](../Assets/For%20Demo/AllInOne/Scripts/Core/Testing/ViewCapture.cs) | | The state of the app as data, counted errors, frame cost; pictures of a camera |

Editor, in [`AllInOne/Editor`](../Assets/For%20Demo/AllInOne/Editor): `AllInOneBuilder` (content, prefabs, and scene), `BuildAllInOne` (players), `AllInOneSmokeTest` and `SmokeRunner` (tests), `ThumbnailRenderer` (avatar pictures).

## DemoKit

Interface and input kit in [`Assets/For Demo/DemoKit`](../Assets/For%20Demo/DemoKit), namespace `DemoKit`, see [ARCHITECTURE.md](ARCHITECTURE.md#demokit).

| Group | Classes |
|---|---|
| Entry and panels | `DemoUi` (build, find, click, toasts), `DemoBuilder`, `DemoPanel`, `DemoPlacement`, `DemoPages`, `DemoTheme` |
| Widgets | `DemoButton`, `DemoCard`, `DemoScrollGrid`, `DemoStepper`, `DemoSlider`, `DemoSwitch`, `DemoToggleRow`, `DemoLabel`, `DemoStatus`, `DemoProgressBar`, `DemoProgressDots`, `DemoPicture`, `DemoHotspot` (a pressable area on an existing canvas), `DemoTarget` |
| Messages | `DemoCaptionBar`, `DemoToast`, `DemoSounds` |
| Input | `DemoInput`, `DemoPointer`, `DemoPointerRay`, `DemoPointerPoke`, `DemoPointerMouse`, `DemoPointerVisual`, `DemoPress`, `DemoPoseDriver` |
| Rig | `DemoDesktopRig`, `DemoFade`, `DemoXr` |
| Editor | Menu **Demo Kit**: create a sample scene or a theme asset, rebuild the fonts, render sample pictures |
