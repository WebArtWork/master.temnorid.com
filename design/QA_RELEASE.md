# Temnorid: Master — Testing, Documentation and Release

## 1. Test fixtures — deterministic logic (EditMode)

1. Catalog: exactly 7 profession IDs, **12 unique workshop job IDs**, 2 unique training IDs, no missing task config or incorrect data reference; each profession has two jobs except Stable.
2. All professions accessible on New Game; each second workshop job unlocks only after first successful completion of its corresponding first job.
3. Global level thresholds: 0→1, 75→2, 180→3, 330→4, 500→5; no negative XP.
4. Each profession skill rank follows 0/15/35/70; no duplicate first-time reward.
5. Scribe: `SCR-01 ACCEPT` passes; `FLAG` rejects. `SCR-02 FLAG` passes; `ACCEPT` rejects. Correct/incorrect seal changes status. Exact trace path produces ~100; distant path low.
6. Scribe: correct SCR-01 with trace quality **90** pays **18 coins +10 global XP+15 skill XP** first time; replay at 90 pays **6 coins +2 global XP+5 skill XP**. No pay on rejected attempt.
7. Forge: finish choice mismatch rejects; heat at target score 100; hammer taps at exact centers score 100; correct + ideal = score 100; score below 60 rejects.
8. Workshop: correct part order accepted; any swapped/distractor component rejected. Test bar exactly at target gives score 100.
9. Cooking: same three ingredients in different selection order succeeds; wrong set rejects. Ideal heat + 3 hits = quality 100.
10. Tailor: wrong material rejects; exact cut path + 3 hits = 100; under-threshold cut fails until retry; score below 60 rejects.
11. Apothecary: exactly two herb IDs + correct label pass; wrong herb/label rejected; target grind gives 100.
12. Upgrades: `UP_GUIDE` increases only Scribe/Tailor precision (cap 100); `UP_BENCH` increases eligible final scores **after validity checks**; `UP_BRUSH` changes groom +25→+35; all purchases charge once, no negative wallet.
13. Horse starting needs 70/70/80/40; care clamps [0,100]; free ration always available; pet/groom trust cooldown prevents farming; zero coins never soft-locks.
14. Training requires correct satiety/energy, drains energy **once** at start, completes 8 obstacles/5 rounds deterministically. `STA-OBS` 8 clears: **26 coins +34 global XP +34 horse XP**; `STA-CMD` 5 hits: **23 coins +28 global XP +28 horse XP**; stable skill XP first training = 15 (5+10), repeated = 5.
15. Offline horse changes applied once for elapsed up to 8h, never double applied by repeated scene load; backwards clock clamps to zero.
16. Idempotency: double-click submit/complete, replay old attempt ID, save/reload and retry wrong job cannot duplicate rewards or charges; unique run IDs survive reload.
17. Save migration: missing fields initialized safely, unknown job IDs ignored, corrupted save triggers recovery menu, wallet/needs never become negative/out of bounds.
18. All 12 workshop jobs are finishable without purchasing an upgrade; no need to spend currency for materials.

## 2. Manual PlayMode / Windows acceptance walkthrough

1. Launch first time; Main Menu works, `Continue` disabled until save exists; UI fits 1280×720 and 1920×1080.
2. Open Hub; all 7 professions accessible; choose Stable, feed basic ration, groom/pet/rest, view needs feedback.
3. Play Command Timing, see score and award; exit/reopen, verify persisted state and no duplicated rewards.
4. Play obstacle mode at fixed-side camera, miss a hurdle without crashing, finish and see result.
5. Visit Scribe SCR-01; choose wrong answer and submit (rejection); retry correctly with trace and stamp, see reward.
6. Complete first job of Forge, Workshop, Cooking, Tailor and Apothecary; verify each uses distinct interaction, no fake buttons.
7. Return after finishing each first job; corresponding second job unlocks. Finish all 12 authored jobs.
8. Switch professions during active job: confirm cancel or remain; no state corruption. Esc exits a close-up and returns correct camera.
9. Buy one affordable upgrade, quit/reopen and confirm permanent effect; try buying twice and when coins low.
10. `Journal` shows correct 12 job flags + two training activities; repeated job pays smaller amount.
11. Finish all first-time commissions; app stays playable, no forced ending.
12. Disable audio, alt-tab, change resolution, restart during subtask; app remains controllable.
13. Inspect the entire application offline. There must be **no Lords, Academy contribution, sync/login or other-game integration screens**.

## 3. Documentation website (`docs/index.html`) — mandatory

The repository must contain a standalone responsive HTML page with:

- Title and concept `Temnorid: Master`.
- Overview of **seven professions** and screenshot placeholders until actual screenshots exist.
- Clear gameplay loop, controls and minimum requirements once measured.
- Explicit `Offline MVP` notice (no online integration).
- Changelog/version `0.1.0`.
- `Download for Windows` button linking to a **verified, real GitHub Release asset/page** when published. Before publication, disabled button labeled `Coming soon`/`Очікується`; never make up a URL or claim installer exists.
- Repository and contact links only if known/confirmed.

A player should be able to view this site without the game running. Keep game documentation under its own repository; don't link to obsolete Pets/Scribe installers.

## 4. Windows package

1. Read actual Unity `ProjectVersion.txt`; use installed version rather than hardcoding speculative runtime requirements.
2. Confirm all three scenes or equivalent are included in build settings.
3. Produce Windows x64 standalone Unity player. Distribute `.exe` **with** its required `*_Data` folder, Unity dependencies and associated files, zipped together as `Temnorid-Master-v0.1.0-win-x64.zip`.
4. Unzip and launch on a fresh Windows test setup where feasible; complete at least SCR-01 and horse training, check Cyrillic fonts.
5. Publish release/tag only after explicit user authorization and verified artifact; then update `docs/index.html` to the verified release URL.
6. Ignore `Library/`, `Temp/`, build caches and cloned local package sources in Git, respecting existing project rules.

## 5. Definition of Done

The MVP is complete when all **seven** activities are not just menu placeholders; all **12** authored commissions and both horse minigames are playable, failure/retry work, wallet & progression are shared, no player can double-claim rewards, save/reopen works, fixed camera navigation is reliable, and the offline Windows build/website can be validated. If builds are not possible in agent environment, report that as an unresolved acceptance gate rather than treating it as passed.

## 6. Agent handoff report

At delivery, provide: repo path; exact Unity/URP version; dependencies actually used; list of scenes; completed/minimal UI; number of authored jobs verified; test execution and counts; playable Windows file path; `/docs/index.html` path; GitHub release URL **only if actually published**; known issues and next smallest action. Do not create or delete any other games' repositories.
