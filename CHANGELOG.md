# Changelog

This project uses [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-11-24

First public release.

- Movement coupling between two networked avatars: hands (blend, superimpose) and position (superimpose, blend), with ratio, axes, smoothing, mirroring, and offset limit.
- `ModulationProfile` assets and `ModulationProfileApplier`, so a coupling design can be saved, shared, and changed while the scene runs.
- `GetPartnerTransform` with a source tag and a run-time switch for the link.
- Desktop demo with two simulated users, markers for the tracked hands, and on-screen controls. It needs no headset, account, or network.
- Leader, follower, and monitor scenes for Normcore, with five full-body IK avatars.
- Session shell: 13 steps from the monitor, in-VR boards, rating and split panels, response log, and a 50 Hz motion log.

The editor project, the references in scenes and prefabs, and the desktop demo were checked. The headset scenes were not re-run on devices for this release.
