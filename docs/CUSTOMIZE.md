# Recipes

Short recipes for common changes. Work on a copy of a prefab or scene, and try coupling changes in the desktop demo or in All-in-One solo first. Code in this page is an example and is not part of the project.

Put your scripts in `Assets/For Demo`, or in an assembly definition that references `SocialLeading.Demo`. The demo assembly is not referenced automatically, and `Assets/For Study` has its own classes with the same names, so a script elsewhere would bind to those. Recipes 1 to 12 work in the single-role scenes and the desktop demo; recipes 13 to 17 are for All-in-One.

1. [Change the coupling without code](#1-change-the-coupling-without-code)
2. [One hand, one axis, or a mirror image](#2-one-hand-one-axis-or-a-mirror-image)
3. [Fade the link in and out](#3-fade-the-link-in-and-out)
4. [Let both users lead each other](#4-let-both-users-lead-each-other)
5. [More than two avatars](#5-more-than-two-avatars)
6. [Write a new coupling rule](#6-write-a-new-coupling-rule)
7. [Add an avatar](#7-add-an-avatar)
8. [Change the session steps and narration](#8-change-the-session-steps-and-narration)
9. [Change or add panels](#9-change-or-add-panels)
10. [Use another network library](#10-use-another-network-library)
11. [Log extra data](#11-log-extra-data)
12. [Use the coupling in your own project](#12-use-the-coupling-in-your-own-project)
13. [Change the link of All-in-One](#13-change-the-link-of-all-in-one)
14. [Add a way of who moves whom](#14-add-a-way-of-who-moves-whom)
15. [Add an avatar to All-in-One](#15-add-an-avatar-to-all-in-one)
16. [Change the session of All-in-One](#16-change-the-session-of-all-in-one)
17. [Use your own rooms](#17-use-your-own-rooms)

## 1. Change the coupling without code

1. *Assets > Create > Social Leading > Modulation Profile*.
2. Set the hand mode, position mode, ratios, and smoothing.
3. Open a follower prefab in `Assets/For Demo/Prefabs/Network/Avatars/Pk/Resources/`, add `ModulationProfileApplier` to its root, and assign the profile. In the desktop demo, assign it to *Desktop Demo > Profile* instead.

A profile replaces the prefab values. The default profile has position *Off*, while the networked prefabs ship with position superimpose 0.3 and no applier.

To switch at run time:

```csharp
applier.profile = otherProfile;      // takes effect on the next frame
link.linkEnabled = false;            // GetPartnerTransform on the same root
```

Each client computes the coupling, so make the change on every client.

## 2. One hand, one axis, or a mirror image

- One hand: untick `leftHand` or `rightHand` in the profile.
- One axis: set `handAxisRatio`. (0, 1, 0) couples height only. (1, 0, 1) keeps the user's own height.
- Mirror: on `Left Hand Modulated` and `Right Hand Modulated`, tick `mirror` on `HandMixedFollowSystem` and set `mirrorPartnerHand` to the opposite `Partner Slow` object. Profiles do not change these two fields.

## 3. Fade the link in and out

`linkEnabled` switches in one frame. To fade, move the ratio:

```csharp
using UnityEngine;

/// <summary>Moves the ratio of one hand toward a target. Use it without a ModulationProfileApplier.</summary>
[DefaultExecutionOrder(-46)]
public class HandRatioFade : MonoBehaviour
{
    public HandMixedFollowSystem hand;
    [Range(0f, 1f)] public float targetRatio = 0.3f;
    [Min(0.01f)] public float seconds = 1f;

    private void Update()
    {
        if (hand != null)
            hand.followRatio = Mathf.MoveTowards(hand.followRatio, targetRatio, Time.deltaTime / seconds);
    }
}
```

Set `targetRatio` to 0, wait for the fade, then turn `linkEnabled` off. This fades the hand only. `HeadAddedFollowSystem` accumulates an offset, so a lower `addRatio` does not bring it back. The offset ends when the link goes off or the ratio reaches 0: in one frame with `releaseTime` 0, as in the single-role prefabs, or smoothly with a `releaseTime` above 0 (All-in-One uses 0.4 s).

## 4. Let both users lead each other

Coupling always reads the raw children, so two linked avatars do not feed back into each other.

1. Duplicate a follower prefab inside the same `Resources` folder and give it a new, unique name.
2. Set the tag of the copy's root to `LeaderTag`, so that the motion log finds it as the leader.
3. In a leader scene, set *Local Avatar Prefab* of `RealtimeAvatarManager` to the copy.

Both avatars now have modulators, and each takes the other as its source. The motion log has displayed-pose columns for the follower only; see [recipe 11](#11-log-extra-data). In All-in-One choose **Both ways** on the link page instead.

## 5. More than two avatars

The default pairing needs exactly two avatars with `GetPartnerTransform`. With more, set `sourceTag` on every avatar that should follow.

- Add tags in *Project Settings > Tags and Layers* and set them on the avatar roots.
- Two followers with `sourceTag` = `LeaderTag` follow the same leader.
- If several avatars share the tag, the first registered one is used.
- A chain works: A follows B and B follows C, each by tag. Every avatar reads raw poses, so C's movement does not reach A through B.

Place one rig per user in the scenes, so that users do not start in the same spot.

## 6. Write a new coupling rule

Derive from `MovementModulator`, read the raw and partner children, and write the local pose of a `Modulated` object. This example pushes the hand away from the partner's hand:

```csharp
using UnityEngine;

[DefaultExecutionOrder(-44)]          // after the built-in hand modulators, so this result wins
public class HandContrastSystem : MovementModulator
{
    public Transform myHead, myHand, partnerHead, partnerHand, modulatedHead;
    [Range(0f, 1f)] public float ratio = 0.3f;

    private void Update()
    {
        if (myHead == null || myHand == null || modulatedHead == null) return;

        Vector3 own = myHand.localPosition - myHead.localPosition;
        Vector3 offset = own;

        if (LinkActive && partnerHead != null && partnerHand != null)
        {
            Vector3 partner = HeadingAlignment(partnerHead, modulatedHead)
                              * (partnerHand.localPosition - partnerHead.localPosition);
            offset = Vector3.LerpUnclamped(own, partner, -ratio);
        }

        transform.localPosition = offset + modulatedHead.localPosition;
        transform.localRotation = myHand.localRotation;
    }
}
```

Add it to `Left Hand Modulated` and `Right Hand Modulated` and assign the same references as on `HandMixedFollowSystem`. Pass the own pose through when `LinkActive` is false.

## 7. Add an avatar

1. Import a humanoid model and set *Rig > Animation Type* to *Humanoid*.
2. Build the IK rig like `Assets/For Demo/Characters/IK Prefabs/VR Character IK 4.prefab`: `RigBuilder` on the root, a child with `Rig`, four `TwoBoneIKConstraint` (arms, legs), one `MultiParentConstraint` (head), `IKTargetFollowVRRig` on the root, and `IKFootSolver` on both leg targets. Set the solver's `terrainLayer`, `body`, and `otherFoot`.
3. Duplicate a leader and a follower prefab from `Assets/For Demo/Prefabs/Network/Avatars/P4/Resources/` into a new folder, for example `P6/Resources/`. Give both unique names, also different from the prefabs in `Assets/For Study`. The folder must be called `Resources`.
4. Replace the `VR Character IK 4` child with your body. Set `vrTarget` of `IKTargetFollowVRRig`: `Head`, `Left Hand`, `Right Hand` on the leader; `Head Modulated`, `Left Hand Modulated`, `Right Hand Modulated` on the follower. Keep the body after the first six children.
5. Duplicate a scene and set *Local Avatar Prefab* to the new prefab.
6. Tune `trackingRotationOffset` per hand, and `headBodyPositionOffset` for the body height.

The finger poses in `Assets/Shared/Characters/Animations` are controlled by the animator parameters `Left Grab`, `Left Pinch`, `Right Grab`, and `Right Pinch`. In the single-role scenes no script sets them; in All-in-One `HandPoseDriver` does, from trigger and grip.

## 8. Change the session steps and narration

Three lists must agree:

| Where | Field |
|---|---|
| Monitor scene, `XR Origin (XR Rig) 1` > `MovementTriggers` | `targetPositions` |
| `Assets/For Demo/Prefabs/Network/Resources/Demo VR Player Complete Prefab 2` > `MovementStateHandler` | `targetPositions`, `movementAudioClips` |
| `Assets/For Demo/UI/Prefabs/PlayerInputManager` > `UIPlayerInput` | four signal positions |

- Narration: drop your clips into `movementAudioClips`. The release ships with empty slots. An empty slot is a silent step.
- Fewer or more steps: change the array sizes. Keep every position different from the others and from the start position (0, 0, 1).
- Key: Space is read in `ObserverController.SpacePressedThisFrame`; change it there.

Every client needs the same prefab, so rebuild the headset apps after a change.

## 9. Change or add panels

- Texts: edit the TextMesh Pro objects in `Assets/For Demo/UI/Prefabs`. The boards and rating panels ship with placeholder texts. Some scenes override a text, so check the prefab instances too.
- Scale: set *Min Value*, *Max Value*, and *Whole Numbers* on the slider of `RatingPanel1` and `RatingPanel2`, and edit the number labels.
- Split total: `total` on `SplitUI`, and *Max Value* on the slider.
- Scripted reply: `rejectThreshold`, `acceptText`, `rejectText` on `UIPlayerInput`.
- Output file names: `outputFolder`, `summaryFilePrefix`, `responseFilePrefix` on `UIPlayerInput`.

To add a panel: make a world-space canvas prefab, place it in the scenes, add a field and a signal position to `UIPlayerInput`, and follow the pattern of `MonitorReachSplit` (signal) and `HandleConfirm` (button). Call `SaveResponse` to write the answer.

## 10. Use another network library

The coupling reads plain transforms. A replacement for Normcore has to provide four things:

1. Spawn the avatar prefab of each client on all clients.
2. Sync the root, `Head`, `Left Hand`, and `Right Hand`.
3. On the owner, write the XR rig poses into those objects before execution order -60. `SimulatedRig` shows the pattern.
4. Tell whether an avatar is locally owned.

Then change the two run-time scripts that use Normcore: `SetCameraParent` (ownership, disconnect event) and `MovementTriggers` (ownership request). Remove `RealtimeView`, `RealtimeTransform`, `RealtimeAvatar`, and `RealtimeAvatarVoice` from the prefabs. `DesktopDemoBuilder` and the prefabs refer to Normcore types, so remove the package last. Bring your own voice solution.

`UIPlayerInput` finds the monitor avatar by object name, so set `monitorObjectName` to the name that your library gives it.

All-in-One is built on Normcore models. There, a replacement also needs `RoomConnector`, `NetworkPlayer` with `PlayerModel`, `SessionState` with `SessionModel`, and the room time of `SessionClock`.

## 11. Log extra data

In `DataLoggerSystem`:

1. Add your column names at the end of `ColumnNames`, before the line break.
2. Change the terminator of the last `AppendVector` call from a line break to a comma.
3. Append your values and end the row with a line break.

For example, log whether the link is on:

```csharp
// ColumnNames: "...ModulatedRHandX, ModulatedRHandY, ModulatedRHandZ, LinkOn\n"
AppendVector(modulatedRHand.position, ',');
var link = follower.GetComponent<GetPartnerTransform>();
row.Append(link != null && link.HasSource ? "1" : "0").Append('\n');
```

Keep `CultureInfo.InvariantCulture` for numbers. The rate follows *Project Settings > Time > Fixed Timestep*.

To read a log in Python:

```python
import pandas as pd
df = pd.read_csv("SystemTrackingData_20260101_120000000.csv", skipinitialspace=True)
```

## 12. Use the coupling in your own project

`Assets/For Demo/Scripts/Modulation` depends on Unity only.

1. Copy the folder with its `.meta` files.
2. Build the [avatar hierarchy](ARCHITECTURE.md#avatar-hierarchy): own head and hands as children 0 to 2, partner copies as children 3 to 5, then the filtered hands and the `Modulated` objects.
3. Write tracked poses into children 0 to 2 before execution order -60.
4. Drive your body or IK from the `Modulated` objects.

The easiest start is to copy the *Follower* object from `Assets/For Demo/Scenes/DesktopDemo.unity`.

## 13. Change the link of All-in-One

`Content` is `Assets/For Demo/AllInOne/Content`.

- Start values of free play, and the values of **Reset**: `Content/Free Play Profile.asset`.
- The fixed link of the full session: `Content/Study Profile.asset`.
- Limit of the position offset in free play: `playMaxPositionOffset` in `Content/App Config.asset` (0.6 m).

**Rebuild Scene** writes both profiles again from `AllInOneBuilder.Content.cs` (`BuildProfile`), so change them there too if you rebuild.

From code, a client changes the link for everybody through the session, in free play only. Any client may write; the last write wins.

```csharp
using SocialLeading.AllInOne;
using UnityEngine;

/// <summary>Example: a larger share of hands while K is held. Put it into Assets/For Demo.</summary>
public class ShareWhileKeyHeld : MonoBehaviour
{
    public SessionState session;   // on the object Session of AllInOne.unity

    private void Update()
    {
        if (session == null || session.Experience != Experience.FreePlay) return;
        float wanted = Input.GetKey(KeyCode.K) ? 0.8f : 0.3f;
        if (!Mathf.Approximately(session.HandRatio, wanted)) session.SetHandRatio(wanted);
    }
}
```

`LinkWiring` ramps every client to the new value in 0.7 s. The other setters are `SetLinkOn`, `SetTopology`, `SetHandMode`, `SetSmoothing`, `SetPositionMode`, `SetPositionRatio`, and `ResetLink`.

## 14. Add a way of who moves whom

1. Select `Content/Topology Catalog.asset` and add an entry: `id`, `label`, and `sourceOfRole`, which holds for every role the index of the role that moves it, or -1. The roles are the entries of `Content/Role Catalog.asset`: 0 Leader, 1 Follower.
2. The link page lists the new entry by its label. Nothing else is needed.
3. Add the entry to `BuildTopologies` in `AllInOneBuilder.Content.cs` too, or **Rebuild Scene** removes it.

A body is always moved by the raw pose of its source, so a ring or "both ways" does not feed back.

## 15. Add an avatar to All-in-One

1. Make an IK character as in [recipe 7](#7-add-an-avatar), steps 1 and 2, and save it as `Assets/For Demo/Characters/IK Prefabs/VR Character IK 6.prefab`.
2. Raise `AvatarCount` in [`AllInOneBuilder.cs`](../Assets/For%20Demo/AllInOne/Editor/AllInOneBuilder.cs) to 6.
3. Run **Social Leading > All-in-One > Rebuild Scene**. It makes `Resources/AllInOne/Characters/Demo Character 6` and a catalog entry.
4. Run **Render Thumbnails**, which renders the pictures from live bodies, and **Rebuild Scene** once more so that the catalog takes the new picture.

Without the builder: put a prefab with `IKTargetFollowVRRig` below a `Resources` folder and add an entry to `Content/Avatar Catalog.asset` with its path and a picture. Every client must have the same catalog, because only the index travels over the network. The cards on the panels become narrower with every avatar.

## 16. Change the session of All-in-One

The full session is one asset, `Content/Dyad Study Script.asset`. Clips, captions, hints, times, rules, timers, and the cues of simulated partners are fields of its steps, and steps can be added or removed without code. See [ALL_IN_ONE.md](ALL_IN_ONE.md#add-or-change-a-step), also for new cues and for the test that checks the shipped steps.

## 17. Use your own rooms

In `Content/App Config.asset`:

| Field | Default | Meaning |
|---|---|---|
| `roomPrefix` | `sl-dyad-aio1-r` | Room names are the prefix and the room number |
| `roomCount` | 9 | Number of rooms on the Join page, 1 to 9 |
| `soloRoom` | `solo` | Name of the offline room |

The number in the prefix is a protocol version. When you add a property to `PlayerModel` or `SessionModel` (with a new id in `[RealtimeProperty]`), raise it, so that builds with different models never meet in one room. Everybody who has your app key can join a room whose name they know.
