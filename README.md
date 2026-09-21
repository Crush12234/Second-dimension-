# Second Dimension: Guild of Worlds

A fantasy card-campaign RPG with independently resumable Campaign, Endless Tower, Titan Trials, and Town progression.

This project contains the source and artwork corresponding to **Alpha Test 2.0, update165**. Gameplay and presentation are written in **C#**, using **Unity 6000.3.22f1**. Authored content and saves use JSON. The current playable targets Windows x64 Mono.

## Get started

Install Git and Unity 6000.3.22f1 with Windows Build Support. Clone this repository, then add the project folder through Unity Hub. Open `Assets/Scenes/Boot.unity`.

Source and artwork are stored directly in Git; Git LFS is not required. For a smaller history download, use `git clone --depth 1 https://github.com/Crush12234/Second-dimension-.git`. Allow several gigabytes for the checkout and additional space for Unity's generated Library.

## Build and test

The portable build menu is **Second Dimension > Build > Windows portable alpha**. It creates a new build directory and includes a portable SaveData folder. The command-line equivalent is:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe' -batchmode -quit -projectPath $PWD.Path -buildTarget StandaloneWindows64 -executeMethod SecondDimension.Editor.Studio163.PortableAlphaBuild163.Build -logFile 'Build165.log'
```

The default output is a new timestamped directory under `Builds`. Avoid the historical release build helpers when you want a portable output location. Apply the SD executable icon using the instructions in [Tools/WindowsBranding](Tools/WindowsBranding/README.md), or set the PNG as the default icon in Unity Player Settings before building.

Focused tests are `Assets/Tests/EditMode/OpeningQuestFate165Tests.cs` and `Merchant165Tests.cs`; run them in Unity's Test Runner. The accepted update165 verification comprises 24 native gameplay tests, focused rendered UI and Unity EventSystem checks, Windows startup, and packaged file verification. No new clean Unity Editor build or physical phone test is claimed; see [verification scope](Documentation/Release165/FINAL_UI165.md).

## Project map

| Folder | Purpose |
|---|---|
| `Assets/SecondDimension/Gameplay` | Rules, scaling, purchases and progression |
| `Assets/SecondDimension/Presentation` | UI, navigation, animation and read models |
| `Assets/SecondDimension/Save` | Persistence and migration |
| `Assets/StreamingAssets`, `Assets/Resources` | Content, artwork and runtime registries |
| `Assets/Tests` | Edit Mode and Play Mode tests |
| `Assets/Editor/Studio163` | Portable Windows build entry |
| `Documentation/Release165` | Release notes, verification and source baseline |
| `Documentation/AssetProvenance` | Asset provenance and notices |

Update165 adds D20 blessings/curses and Fortune Wheel rooms to the opening campaign, expands Union planning, replaces Quest 3's old route graph, adds the illustrated Waystation, and gives merchants distinct equipment, hero, accessory and luck-tonic offers. The campaign remains the main adventure.

## Publishing and releases

This repository is public at the owner's request. Anyone can view and download these files. This initial source import excludes save profiles, local credentials, old builds, Unity caches and private QA snapshots. No open-source license is granted by this import. Existing third-party notices remain in [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt).

Distribute tested Windows players through GitHub Releases or the complete Alpha 2.0 USB folder. A release on this public repository is also public. For a direct handoff, copy the entire `alph test 2.0` folder to a USB drive; testers copy it to their Windows PC and run `PLAY SECOND DIMENSION.cmd`. Do not add a personal `SaveData` folder or compiled game copies to Git history.

Repository: [Crush12234/Second-dimension-](https://github.com/Crush12234/Second-dimension-). This initial import preserves update165's verified source and adds repository documentation. It uses ordinary Git and does not enable Git LFS, Actions, Codespaces, paid hosting or a paid plan. Later updates should use focused branches and review the relevant tests before merging.
