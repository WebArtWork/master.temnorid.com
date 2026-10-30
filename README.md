# Temnorid: Master (Темнорід: Майстер)

**A medieval workshop career game: learn seven trades in one village workshop.** A short, offline game for **Windows**, in the world of [Temnorid](https://temnorid.com), built on the [IT Kamianets 3D Engine](https://3d.itkamianets.com) with Unity 6000.6. The game's text is in Ukrainian.

It replaces the separate Pets and Scribe games: the foal, its care and its two trainings are the **Stable**, the Academy desk is the **Scribe**, and five more trades (forge, workshop, kitchen, tailor, herbalist) share one wallet, one level and one save. You choose a trade from the hub, take an order, do a few short hands-on steps with the mouse and are paid for the quality of your work. A wrong answer is never punished: you are told why and try again for free. No avatar, no walking, no connection of any kind.

**Status:** version 0.1.0, first playable MVP. Website: https://master.temnorid.com · Download: not published yet (no repository or release exists, so the page shows "Coming soon" instead of a made-up link).

## How it plays

| Area | What there is |
|---|---|
| Hub | The common workshop with six workstations in a row and a fixed camera; seven cards (Stable, Scribe, Forge, Workshop, Kitchen, Tailor, Herbalist), each with its icon, a short line, your rank and how much you have done. A card or the station itself can be clicked. All seven are open from the first minute. Top bar: coins, master level, experience. |
| Orders | Every trade has two authored orders (12 in all, `SCR-01` … `APT-02`); the second opens when the first is completed; completed ones can be repeated for about a third of the pay. Only one order is open at a time. |
| Steps | A numbered step bar (1–3) and one central panel: inspect two notes, trace a path, drag a wax seal, stop a sweeping heat bar, strike three hammer rings, choose a finish, pick ingredients or herbs, place parts in slots in order, stir or stitch in three green zones. Nothing says whether you are right until you press confirm. |
| Result | `Успіх` or `Спробуй ще`, the score `NN/100`, a short breakdown, coins, experience and skill experience, a first-time note. A rejection says why and has a free retry. The result only reads a payout that is already committed. |
| Stable | The horse (a foal at the start, a young and then an adult horse as it levels) with four needs (satiety, cleanliness, energy, trust), hay for 10 coins, a free ration, grooming, petting, rest, and two trainings: obstacle course (8 jumps) and command timing (5 rounds). Time away changes the needs a little, once, up to 8 hours. |
| Progress | One wallet; master level 1–5 (0/75/180/330/500 experience); a rank per trade (0/15/35/70 skill experience: Novice, Apprentice, Craftsman, Master); three upgrades (guide template 50, better bench 80, good brush 60); a journal with every order and the last 25 results. |
| Saving | One local slot, written to a temporary file and swapped in, with the last good save kept as a backup. Saved after every committed step (taking an order, every answer, a good trace, a placed seal, a result, a purchase, care, a training). If the file cannot be read there is a recovery menu: try the backup, start a new game or quit. Settings are in a separate small file. |

### Controls

| Action | Input |
|---|---|
| Choose, press, drag | Mouse (left button) |
| Stop the bar, stir, stitch, jump, give a command | Space, or the big on-screen button |
| Back, close, pause | Esc |
| Menus | Mouse, or arrows and Enter |

## Run it

1. Open the folder in Unity Hub with Unity **6000.6** (the project references the shared Temnorid packages by local path: `file:../../3d-unity-*`, so keep it next to `D:\work\temnorid\` and those folders).
2. Menu **Temnorid > Master > Build Scene** rebuilds `Assets/Scenes/Master.unity` from primitives (it is generated, do not edit it by hand), then press Play.

From the command line:

```bash
Unity.exe -batchmode -nographics -projectPath . -executeMethod Temnorid.Master.EditorTools.MasterSceneBuilder.Build -quit
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode
Unity.exe -batchmode -nographics -projectPath . -executeMethod Temnorid.Master.EditorTools.MasterReleaseBuilder.BuildWindows -quit
"ISCC.exe" installer\Master.iss
```

(Do not pass `-quit` together with `-runTests`.) The build is `Builds/Windows/TemnoridMaster/TemnoridMaster.exe` with its `_Data` folder, zipped as `Builds/Windows/Temnorid-Master-v0.1.0-win-x64.zip`; the installer is `Builds/Installer/Temnorid-Master-Setup.exe`. `TEST_REPORT.md` says what the tests expect and what was observed.

## Layout

```text
Assets/Scenes/Master.unity      the one scene: title screen, hub, workstations, stable and screens
Assets/Scripts/                 MasterContent (the authored game), TaskScoring (the scoring of every trade), Horse,
                                MasterSession (the one authority: wallet, orders, stable, save data), MiniGames
                                (obstacle run and command timing), Loc (all text), MasterGame (the game as played),
                                MasterHud (+ .Task: every screen), ViewCamera, Station, HorseView, YardTrack,
                                MasterAudio, MasterSaveStore
Assets/Scripts/Editor/          MasterSceneBuilder, MasterReleaseBuilder
Assets/Tests/                   EditMode and PlayMode
design/                         the design documents this game was built from (README, GAME, CONTENT, ACTIVITIES, UI, MVP, AGENT, QA_RELEASE)
docs/index.html, docs/CNAME     the website (master.temnorid.com)
installer/Master.iss            Inno Setup script
```

Packages (all local, see `Packages/manifest.json`): `3d-unity-core`, `3d-unity-input` (the only input entry point, including the pointer for tracing, sealing and the anvil), `3d-unity-save` (atomic save, with the previous good save kept as a backup), `3d-unity-ui` (screens), `3d-unity-synth` (the generated sounds), `3d-unity-graybox` (asset ids, scene builder helpers), `3d-unity-feedback` and `3d-unity-combat` (only because feedback depends on it). No `3d-master-*` package exists; all game logic is in this game.

## Gap report and differences from the guides

- The fixed-view constructor repository and the live package catalog could not be read (no network access in the build environment), so nothing was assumed from them. The camera is a tiny game-local `ViewCamera` with a handful of named fixed poses (the hub, each workstation's overview and close view, the stable, the side of the course), because there is no avatar and no subject to follow. It glides between poses and the pointer never steers it.
- The guides ask for three scenes or equivalent. There is **one scene, `Master.unity`**, with the title screen as an overlay on it, as in the other Temnorid games. The hub, the workroom (six workstations) and the stable with its course are all in it, reached by camera poses.
- The obstacle course is a treadmill: the horse stays where the fixed side camera looks and the jumps come to it. Command timing is a bar over the stable view. Neither needs physics.
- The close-ups of the tasks are 2D panels over the matching 3D station (the guide allows this), with the camera moving to the close view behind them.
- Pets and Scribe are gone as separate products: this game has their Stable and Scribe content, with their own tuning from `design/CONTENT.md`. No Lords, Academy contribution, login or sync screen exists, and no data leaves the machine.
- Texts are Ukrainian only; every line is in `Loc.cs`. Order, upgrade, trade and training ids are English and never change with the language. The answer keys live in `MasterContent.cs` as developer data and are never shown before you confirm an order.
- Everything is made of primitives with stable asset ids (`AssetIdTag`); the site's `/models` page lists them so they can be replaced with generated models later.

## Not verified / limitations

- Played by tests (the game's own commands, plus a virtual mouse drawing the mark, dragging the seal and clicking the anvil through the real screens) and looked at in the editor's 1280×720 game view (hub, orders, every kind of step, the stable, both trainings, results, upgrades, journal); not played by a human from start to finish with a real mouse, and the Windows installer was built but not installed.
- The screens were not checked one by one at 1920×1080 and 1280×720 on a real monitor.
- No gamepad or touch build, no English text, no music; the sounds are simple generated tones.
- No cloud, no accounts and no multiplayer, by design: a local game can be edited, so nothing in it may ever grant anything elsewhere.

## Release

Windows installer or zip attached to a GitHub Release, and the "Download" button on `docs/index.html` pointing at that asset. TODO: create the repository and the release (not done: nothing is committed or pushed); until then the button is disabled.
