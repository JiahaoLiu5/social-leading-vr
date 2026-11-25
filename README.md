# Social Leading VR

A Unity platform for prototyping social VR with real-time movement coupling: one user's hands, and optionally position, partly drive another user's avatar.

![Unity 2022.3.15f1](docs/media/badge-unity.svg)
![Platform: Quest and Windows Editor](docs/media/badge-platform.svg)
![Code licence: MIT](docs/media/badge-licence.svg)

![Desktop demo: the leader on the left moves, the follower's avatar on the right follows part of the way](docs/media/demo.gif)

*Desktop demo. Left: the leader. Right: the follower. The orange markers show where the follower's tracked hands are; the avatar's hands show the coupled result.*

## What it does

- **Couples movement live.** The follower's avatar shows a mix of the follower's own hands and the leader's hands. Two hand modes (blend, superimpose) and two position modes (superimpose, blend), each with a ratio from 0 to 1. Each hand can be switched off, and hands blend takes a weight per axis.
- **Keeps it spatially correct.** Hands are compared relative to each head and rotated around the vertical axis, so "forward" for the leader becomes "forward" for the follower. In hands blend, a low-pass filter smooths the leader's hands.
- **Shows it on full bodies.** Five humanoid avatars with Animation Rigging IK. The follower's camera looks from the coupled head.
- **Runs over the network.** Normcore carries raw poses and voice. Each client computes the coupling locally.
- **Comes with a session shell.** A monitor scene steps through a 13-step session with the Space key. A step can play a narration clip and open task panels in the headsets. The monitor writes a 50 Hz motion log. Clip slots are empty, and the board and rating texts are placeholders, so you can build your own activity.

More projects by the author: [jiahaoliu5.github.io/projects](https://jiahaoliu5.github.io/projects/).

## Try it in five minutes

No headset, account, or app key is needed. The first import runs unattended and can take longer than the rest.

1. Install **Unity 2022.3.15f1** (Unity Hub > Installs > Install Editor > Archive).
2. Clone the repository:

   ```sh
   git clone https://github.com/JiahaoLiu5/social-leading-vr.git
   ```

3. In Unity Hub choose *Add project from disk*, select the folder, and open it. Unity downloads the packages on the first open, so stay online. If a Normcore window appears, close it.
4. Open `Assets/SocialLeading/Demo/DesktopDemo.unity`.
5. Press **Play**.

You should see two avatars facing each other. The leader (left) raises one hand after the other. The follower (right) stands idle, yet its hands rise about 30 percent of the way, with a short delay.

Then try this in the panel on the left:

| Do this | You should see |
|---|---|
| *Hands > Ratio* to 1.00 | The follower's hands copy the leader's hands |
| *Hands > Superimpose* | The leader's hand displacement is added on top of the follower's own hands |
| *Position > Superimpose*, *Leader motion > Step* | The follower's body moves part of the leader's step |
| *Space* or *Link on* | The coupling stops and the follower's hands return to the markers |

Right-drag orbits, scroll zooms, Tab hides the panel. The panel edits a copy of the profile, so play-mode changes are not saved. The menu *Social Leading > Rebuild Desktop Demo* regenerates the scene.

## Run it in VR

You need two headsets, a PC for the monitor, and a Normcore account with an app key.

1. Create an app at [normcore.io](https://normcore.io), copy its key, and paste it into `Assets/Normal/Resources/NormcoreAppSettings.asset`. Keep the key out of git:

   ```sh
   git update-index --skip-worktree Assets/Normal/Resources/NormcoreAppSettings.asset
   ```

2. Build one scene from `Assets/SocialLeading/Scenes/Leader/` for the first headset and one from `Scenes/Follower/` for the second. `SLSL_P1` to `SLSL_P5` and `SLSF_P1` to `SLSF_P5` differ in the avatar (P1 to P5).
3. Open `Assets/SocialLeading/Scenes/Monitor/Monitor.unity` in the editor and press Play.
4. Start both headsets and allow the microphone. Leader and follower start 3 m apart, facing each other.
5. Click the Game view on the PC and press **Space** to go to the next step. Space waits while a narration clip plays.

Build settings, permissions, and troubleshooting: [docs/VR_SETUP.md](docs/VR_SETUP.md).

## How it works

```mermaid
flowchart LR
  LX[Leader XR rig] --> LR
  FX[Follower XR rig] --> FR
  subgraph EV["On every client, from poses synced by Normcore"]
    LR[Leader avatar<br/>raw head and hands] --> GP[GetPartnerTransform<br/>copies into follower root space]
    FR[Follower avatar<br/>raw head and hands]
    GP --> SL[SlowFollowSystem<br/>low-pass on partner hands]
    GP --> HD[Head modulator]
    SL --> HA[Hand modulators]
    FR --> HD
    FR --> HA
    HD --> HA
    HD --> IK[Full-body IK]
    HA --> IK
  end
  HD --> CAM[First-person camera<br/>on the follower's client]
```

All vectors are local to each user's own avatar root. `F` is the follower, `L` the leader, `*` marks the displayed pose, and `r` is the ratio of that mode. `axisRatio` scales `r` per axis in hands blend.

```text
own      = hand_F - head_F
partner  = Yaw(heading_L -> heading_F) * (LowPass(hand_L) - head_L)

hands, blend         hand* = head* + Lerp(own, partner, r)
hands, superimpose   hand* = head* + own + sum over frames of r * Yaw * change of (hand_L - head_L)

position, superimpose   head*.xz = head_F.xz + r * (head_L(t) - head_L(t0)).xz
position, blend         head*.xz = Lerp(head_F.xz, head_L.xz, r)

LowPass: x += (target - x) * (1 - exp(-dt / tau))
```

`t0` is the moment the link starts. Head height and all rotations stay the user's own. Hand height is coupled like the other two axes. The follower prefabs use hands blend 0.3, position superimpose 0.3, and `tau` 0.2 s. Update order, networking, session flow, and logging: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Build on it

The coupling, the avatars, and the session shell are separate parts, so the project can serve as a base for other social VR work.

- **Guided movement.** Raise the ratio and mirror the hands: one person's movement becomes a guide for the other, as in coaching, dance, or rehabilitation prototypes.
- **Transformed nonverbal behaviour.** Scale single axes, superimpose instead of blend, or link only one hand, and compare the results side by side.
- **Adaptive coupling.** Change `linkEnabled` or a `ModulationProfile` from your own script, for example from speech, gaze, or distance.
- **Your own two-person activity.** Replace boards, panels, narration, and steps. Networking, avatars, operator view, and logging stay.

## Make it yours

| Change | Where | Options and defaults |
|---|---|---|
| Hand coupling | `ModulationProfile` asset, *Hands* | Off, **Blend**, Superimpose; ratio 0 to 1 (**0.3**); ratio per axis (**1, 1, 1**); left and right hand on or off (**on**) |
| Position coupling | `ModulationProfile` asset, *Position* | **Off**, Superimpose, Blend; ratio 0 to 1 (**0.3**); offset limit in metres (**0** = none) |
| Smoothing | `ModulationProfile`, *Partner Smoothing* | Time constant in seconds (**0.2**), used by hands blend |
| Apply a profile | `ModulationProfileApplier` on the avatar root | Any profile; edits apply live |
| Who leads | `GetPartnerTransform` > *Source Tag* | **Empty** = the only other avatar; or a tag such as `LeaderTag` |
| Link on or off | `GetPartnerTransform` > *Link Enabled* | **On**; can be toggled at run time |
| Mirror the movement | `HandMixedFollowSystem` > *Mirror* | **Off** |
| Avatar of a scene | `RealtimeAvatarManager` > *Local Avatar Prefab* | P1 to P5, or your own |
| Session steps | `MovementTriggers`, `MovementStateHandler` | 13 positions; 13 clip slots (**empty**) |
| Panels | `UIPlayerInput` and `Assets/SocialLeading/UI/Prefabs` | Boards, sliders from 1 to 7, a split slider from 0 to 100 with scripted proposals and a scripted reply; **placeholder texts** on boards and rating panels |
| Log rate | *Project Settings > Time > Fixed Timestep* | **0.02 s** (50 Hz) |

Step-by-step recipes: [docs/CUSTOMIZE.md](docs/CUSTOMIZE.md). Every script and field: [docs/COMPONENTS.md](docs/COMPONENTS.md).

## Project layout

```text
Assets/
  SocialLeading/
    Characters/            five character models, IK prefabs, hand poses
    Demo/                  DesktopDemo scene, sample profile, scripts, scene builder
    Environment/           room, lights, mirror
    Prefabs/Network/       networked avatars (P1..P5, leader and follower) and Realtime objects
    Prefabs/Rig/           XR rigs and the first-person camera
    Scenes/Leader|Follower|Monitor/
    Scripts/               Avatar, Logging, Modulation, Session, TaskUI
    UI/                    in-VR panels and sprites
  Normal/                  Normcore settings (app key is empty)
  Samples/                 XR Interaction Toolkit starter assets (four added input actions)
  ThirdParty/              TextMesh Pro essentials
  XR/, XRI/, Plugins/      OpenXR, XRI, and Android settings
docs/                      architecture, components, VR setup, recipes
```

## Requirements

| Item | Version |
|---|---|
| Unity | 2022.3.15f1, built-in render pipeline, Android Build Support for Quest builds |
| Normcore (`com.normalvr.normcore`) | 2.12.0, from the Normal scoped registry in `Packages/manifest.json` |
| XR Interaction Toolkit | 2.5.4 |
| OpenXR Plugin | 1.9.1 |
| XR Plug-in Management | 4.5.0 |
| Animation Rigging | 1.2.1 |
| Input System | 1.7.0 |
| TextMesh Pro, Unity UI | 3.0.6, 1.0.0 |
| Profiling Core API, Test Framework, Visual Studio Editor | 1.0.3, 1.1.33, 2.0.22 |
| Headset | Quest with Touch controllers; hand tracking is not used |
| Disk | About 520 MB of assets, of which 490 MB are the five character models, plus the Unity `Library` folder |

## Known limits

- For this release the project was opened and compiled in the Unity editor, scene and prefab references were checked, and the desktop demo was played. The VR scenes were not re-run on headsets or in a networked session after the clean-up.
- Coupled poses are computed on each client from the synced raw poses, so two clients can show slightly different results.
- Position coupling moves the follower's viewpoint without the follower moving. Start with a low ratio and an offset limit, and stop if a user feels unwell.
- Switching the link off removes the offset in one frame. There is no fade.
- The monitor signals a step through its avatar's position. A client that joins after the first step plays no narration.
- All scenes join the room `Test Room` and share one application id. Change the room name to run several sessions with one key.
- Eye height starts at 1.6 m for every user (device tracking origin). Finger poses are not driven.
- Proposals and replies in the split panels are scripted on each client. They do not come from the other user.
- OpenXR 1.9.1 does not list Quest 3 as a target device. See [docs/VR_SETUP.md](docs/VR_SETUP.md#quest-3).

## Credits

System design and lead development: Jiahao Liu. Code contributions: Zewuzi Meng (session flow and task UI), Lydia Lin (avatar rigging), Stella Dong. Built in the Virtual Embodiment Lab, Cornell University, directed by Andrea Stevenson Won.

## Licence and third-party assets

The source code in this repository is under the [MIT licence](LICENSE). The licence does not cover third-party assets: avatars, room model, fonts, Unity samples, and packages keep their own terms. Read [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) before you redistribute anything.

## Cite

Use [CITATION.cff](CITATION.cff), or *Cite this repository* on GitHub:

> Liu, J. (2026). *Social Leading VR* (version 1.0.0) [Computer software]. https://github.com/JiahaoLiu5/social-leading-vr
