# Contributing

Issues and pull requests are welcome.

## Before you start

- Use Unity 2022.3.15f1. Opening the project in another version rewrites many files.
- Open `Assets/For Demo/Scenes/AllInOne.unity`, press Play, and join solo. It needs no headset, account, or network. `Assets/For Demo/Scenes/DesktopDemo.unity` should play without errors too.
- For a larger change, open an issue first and describe what you want to change and why.

## Rules that keep scenes working

- **Work in `Assets/For Demo`.** `Assets/For Study` is kept for the record and stays as it is. Changes in `Assets/Shared` reach both.
- Do not rename or move a `.cs` file that a scene or prefab uses, and do not rename its class. Move files inside Unity so that the `.meta` file moves with them.
- Do not rename a serialized field. If a rename is needed, keep the old name with `[FormerlySerializedAs("oldName")]`.
- Keep the order of the first six children of each networked avatar. `GetPartnerTransform` reads them by index. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#avatar-hierarchy).
- Keep execution orders as listed in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md#update-order). Say so in the pull request if you change one.
- Change `AllInOne.unity` and the content of All-in-One through [`AllInOneBuilder`](Assets/For%20Demo/AllInOne/Editor/AllInOneBuilder.cs) and **Social Leading > All-in-One > Rebuild Scene**. The builder writes scene, prefabs, and content again, so edits made only by hand are lost.
- When you change `PlayerModel` or `SessionModel`, raise the number in `roomPrefix`, so that old and new builds never meet in one room.
- Commit every new asset together with its `.meta` file.

## Code style

- One class per file, named like the file. Four spaces, braces on their own line in longer blocks.
- Put a short `<summary>` on every class and a `[Tooltip]` on every field that is not obvious.
- Check for missing references. Avatars appear and disappear at run time.
- Do not search the scene on every frame. Look objects up at an interval and keep the result.
- Comments, tooltips, and log messages in plain English.

## What not to commit

- Keys, tokens, e-mail addresses, or machine paths. `Assets/Normal/Resources/NormcoreAppSettings.asset` stays in the repository with an empty key. After you paste your own key, run:

  ```sh
  git update-index --skip-worktree Assets/Normal/Resources/NormcoreAppSettings.asset
  ```

- Builds. A build made with a key contains that key; `Builds` is ignored by git.
- Recorded movement, voice, or answers.
- Third-party assets without a licence that allows redistribution. Add an entry to [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for every asset you add.

## Before you open a pull request

1. The project compiles without errors for Windows and Android.
2. The scenes you touched open without missing scripts or references.
3. The automated tests of All-in-One pass, see [docs/ALL_IN_ONE.md](docs/ALL_IN_ONE.md#automated-tests). They use neither your key nor your microphone.
4. If you changed networking, run two players in one room, see [docs/ALL_IN_ONE.md](docs/ALL_IN_ONE.md#networked-tests).
5. If you changed VR behaviour, say which headset you used. If you could not test on a headset, say so.
6. [CHANGELOG.md](CHANGELOG.md) has an entry.

Contributions are accepted under the MIT licence in [LICENSE](LICENSE).
