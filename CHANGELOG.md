# Changelog

This project uses [Semantic Versioning](https://semver.org/).

## [1.1.0] - 2026-11-25

### Added

- **All-in-One**: one app for every person and role. Avatar, role, free play or a full session, automatic or operator control, pace, room, solo with a simulated partner or together, and the movement link are chosen inside VR or on a desktop. Avatar, role, and ready state are properties of the networked player, so everybody sees the same, late joiners included. With builds for Quest and Windows, a command line for networked tests, and two automated smoke tests. See [docs/ALL_IN_ONE.md](docs/ALL_IN_ONE.md).
- **DemoKit**: panels, pointers, captions, messages, a view fade, and a desktop body, made in code, with the Inter font (SIL Open Font License 1.1).
- **For Study**: the original scenes of research made with the platform, kept for the record.

### Changed

- Folders: `Assets/For Demo`, `Assets/For Study`, `Assets/Shared`. The demo has its own assemblies (`SocialLeading.Demo`, `SocialLeading.Demo.Editor`, `SocialLeading.AllInOne.Editor`, `DemoKit`), which are not referenced automatically. The networked prefabs of the demo are named `Demo ...`.
- Scenes: `Assets/For Demo/Scenes/AllInOne`, `DesktopDemo`, `Leader/`, `Follower/`, `Monitor/`. `AllInOne` is the only scene enabled in the build list; the others stay listed.
- `GetPartnerTransform` can take its source from code (`useExplicitSource`, `explicitSource`).
- `HeadAddedFollowSystem` and `HandAddedFollowSystem` now also clear their offset when the ratio is 0 or the component was disabled; before, only switching the link off did. `HeadAddedFollowSystem` can fade a released offset (`releaseTime`) and ignore jumps of the partner (`ignoreAboveSpeed`); both are 0 by default.
- `IKFootSolver.Snap()` puts the feet on the floor after a body was placed. `SimulatedRig` has more motions. `ModulationProfileApplier` looks up its modulators once.

### Verified

The automated tests of All-in-One pass in the Unity Editor, with mouse and keyboard and with a simulated headset. Online rooms were tested with two or three processes on one PC and the Normcore service. The Windows player was built and run, and the APK was built. Nothing was run on a headset, and voice between people was not heard.

## [1.0.0] - 2026-11-24

First public release.

- Movement coupling between two networked avatars: hands (blend, superimpose) and position (superimpose, blend), with ratio, axes, smoothing, mirroring, and offset limit.
- `ModulationProfile` assets and `ModulationProfileApplier`, so a coupling design can be saved, shared, and changed while the scene runs.
- `GetPartnerTransform` with a source tag and a run-time switch for the link.
- Desktop demo with two simulated users, markers for the tracked hands, and on-screen controls. It needs no headset, account, or network.
- Leader, follower, and monitor scenes for Normcore, with five full-body IK avatars.
- Session shell: 13 steps from the monitor, in-VR boards, rating and split panels, response log, and a 50 Hz motion log.

The editor project, the references in scenes and prefabs, and the desktop demo were checked. The headset scenes were not re-run on devices for this release.
