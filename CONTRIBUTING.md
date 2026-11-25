# Contributing

Issues and pull requests are welcome.

## Before you start

- Use Unity 2022.3.15f1. Opening the project in another version rewrites many files.
- Check that `Assets/SocialLeading/Demo/DesktopDemo.unity` plays without errors. It needs no headset and no account.
- For a larger change, open an issue first and describe what you want to change and why.

## Rules that keep scenes working

- Do not rename or move a `.cs` file that a scene or prefab uses, and do not rename its class. Move files inside Unity so that the `.meta` file moves with them.
- Do not rename a serialized field. If a rename is needed, keep the old name with `[FormerlySerializedAs("oldName")]`.
- Keep the order of the first six children of each networked avatar. `GetPartnerTransform` reads them by index. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#avatar-hierarchy).
- Keep execution orders as listed in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#update-order). Say so in the pull request if you change one.
- Commit every new asset together with its `.meta` file.

## Code style

- One class per file, named like the file. Four spaces, braces on their own line in longer blocks.
- Put a short `<summary>` on every class and a `[Tooltip]` on every field that is not obvious.
- Check for missing references. Avatars appear and disappear at run time.
- Do not search the scene on every frame. Look objects up at an interval and keep the result.

## What not to commit

- Keys, tokens, e-mail addresses, or machine paths. `Assets/Normal/Resources/NormcoreAppSettings.asset` stays in the repository with an empty key. After you paste your own key, run:

  ```sh
  git update-index --skip-worktree Assets/Normal/Resources/NormcoreAppSettings.asset
  ```

- Recorded movement, voice, or response data.
- Third-party assets without a licence that allows redistribution. Add an entry to [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for every asset you add.

## Pull requests

State which Unity version you used, what you tested (desktop demo, editor with a headset, headset build), and what you did not test. Contributions are accepted under the MIT licence in [LICENSE](LICENSE).
