# Test report: Temnorid: Master 0.1.0

Run on Unity 6000.6.5f1, headless (`-batchmode -nographics`), against the generated `Master.unity`. Result: **45 edit-mode tests passed, 18 play-mode tests passed, 0 failed.** The numbers in brackets are the fixture numbers in `design/QA_RELEASE.md` section 1.

## Edit-mode (pure rules, `Assets/Tests/EditMode/MasterRulesTests.cs`)

| Expected | Observed |
|---|---|
| Seven professions, twelve unique orders, every order and trade and upgrade and shown item has its text; every authored path exists (1) | Passed. |
| Every trade is open from the start; the second order of a trade is locked until the first is completed (2) | Passed (`SCR-02` is locked, then available). |
| Levels resolve at 0, 75, 180, 330, 500 and stop at 5; ranks at 0, 15, 35, 70 (3, 4) | Passed (74 against 75, 5000 stays at 5, 14 against 15, 70 is the top rank). |
| Scribe: `SCR-01` `ACCEPT` passes and `FLAG` rejects; a wrong seal or a seal off the mark rejects; a poor trace rejects; an exact trace scores 100, a reversed one too, a distant one 0 (5) | Passed. |
| `SCR-01` at quality 90 pays 18 coins, 10 experience and 15 skill experience; the replay pays 6, 2 and 5; a perfect replay pays exactly 7 coins; a rejection pays nothing (6) | Passed. The 7 is a test of its own: the first version of the rule computed 6 (see below). |
| Forge: a wrong finish rejects; heat at the mark and rings at their centres score 100; a score under 60 rejects (7) | Passed. |
| Workshop: the right order passes, any swapped part rejects and names the part out of place (8) | Passed. |
| Cooking: the same three ingredients in another order pass, a wrong set rejects (9) | Passed. |
| Tailor: the wrong fabric rejects, a cut under 40 rejects, an exact cut and three hits give 100 (10) | Passed. |
| Apothecary: exactly the two right herbs and the right label pass, a wrong herb or label rejects (11) | Passed. |
| Upgrades: the guide adds 10 to tracing and cutting only and is capped at 100 and never fixes a wrong answer; the bench adds 5 only after the checks passed; the brush gives +35 instead of +25; each is charged once and the wallet never goes negative (12) | Passed. |
| The horse starts at 70/70/80/40; care clamps to 0–100; the free ration is always there; hay needs coins; petting and grooming trust has a cooldown (13) | Passed (a poor player is never stuck). |
| Training needs energy and food and no open order, drains energy once, and pays by the table: 8 jumps give 26 coins, 34 experience, 34 horse experience, 5 hits give 23, 28, 28; a dirty horse earns 80% (floored); skill experience is 15 the first time and 5 after (14) | Passed. |
| Time away is applied once, at most 8 hours, and a clock set back changes nothing (15) | Passed. |
| Submitting twice, an old attempt id after a reload, and a repeated training run id cannot pay twice (16) | Passed. |
| Missing fields get start values, unknown orders and upgrades are ignored, negative or out-of-range numbers are brought back into range, a damaged file is `Corrupt`, a newer version is `NewerVersion`, a backup is kept and read (17) | Passed through the real save store and file. |

## Play-mode (the real scene, `Assets/Tests/PlayMode/MasterRunTests.cs`)

| Expected | Observed |
|---|---|
| The game opens on the title with all six workstations and every camera pose (hub, stable, course, overview and close view of each trade) | Passed. |
| Every workstation can be clicked from the hub with nothing in front (a 16:9 view): each is in view above the cards and a ray through its centre hits it | Passed. |
| A player finishes all twelve orders in sequence with no upgrade bought, each at 95 or more, paid by the table (18); then buys all three upgrades once each; a repeat pays the smaller reward | Passed. 416 coins and level 2 afterwards, 35 skill experience in every trade. |
| Wrong answers (a wrong verdict, parts in the wrong order, a wrong ingredient, a wrong fabric) are judged only when confirmed, cost nothing, explain themselves and can be retried clean | Passed. |
| Walking away keeps the task; only one order is open; the stable's training and other trades' orders are blocked with a reason; giving up keeps the wallet | Passed. |
| Care: hay costs 10, a rest animation plays, trust has a cooldown, a tired horse cannot train and the toast says why | Passed. |
| A bot runs the obstacle course (jumping before each one) and the command timing (pressing in the zone) at six times speed, is paid once, and the course is put away afterwards | Passed (the run was at least 75% cleared; all five command hits). |
| Leaving a training pays nothing and the energy stays spent | Passed. |
| A virtual mouse draws the mark (90 or more; a drag far off the paper is clamped and does not replace a good trace), drags a seal (placed at the ring; letting go outside leaves it as it was; a press with no seal chosen does nothing) and clicks the three anvil rings in turn | Passed. |
| The sweeping bar runs, one press locks it, a second changes nothing, a later step stays closed until the earlier one is done, reset clears the step | Passed. |
| Quit in the middle of an order and continue: wallet, upgrade, completed list, the open order, its attempt id and its answers are back; it can be finished and cannot be filed twice (16) | Passed. |
| Thirty hours away change the horse as eight hours, once (15) | Passed. |
| A damaged save opens the recovery menu; the backup restores an earlier state; with no backup the menu says so and a new game works; a save from a newer version is not opened (17) | Passed. |
| Esc and the back buttons always lead somewhere valid (settings, journal, upgrades, help, pause, orders, tasks, confirmations); a new game over progress and a reset ask first | Passed. |
| Every screen shows without errors through a full first order of every trade | Passed (no unexpected log messages). |

## Found while testing

- The repeat payout of a perfect result was 6 coins instead of 7: the product of a `float` constant and integers can come out a hair under the exact value and the `floor` took it down. The factor is now a `double` with a tiny allowance, and both the edit-mode and the play-mode test check the 7.
- A save written moments before the scene was reloaded was overwritten by the autosave of the old scene (the autosave fired on the first frame). The autosave timer now starts a minute after a game is entered.
- Visual: the first hub was hidden behind tall cards, the stable roof and a fence were in front of the course camera, and the jumps were stacked on the horse before the first run. All fixed after looking at the game view.

## Not covered by automated tests

Walkthrough items 1–13 of `design/QA_RELEASE.md` section 2 by a human with a real mouse: the full path from `New game` to the last order on one run, switching trades with an order open (the game blocks it with a reason and has an explicit "give up" button instead of a confirmation), alt-tab, changing resolution, muting audio, restarting in the middle of a subtask on a real machine; installing the installer; the screens at 1280 × 720 and 1920 × 1080 on a real monitor (they were looked at in the editor's 1280 × 720 game view only); a clean Windows machine. The standalone player was built and started (it opened on the title with no exception in its log), and a zip and an installer were produced; neither was installed or played by hand.
