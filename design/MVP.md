# Temnorid: Master — Canonical MVP Systems, Economy and Persistence

## 1. Scope

One offline Windows Unity game; **7 playable professions**, **12 workshop commissions**, **2 stable training activities**, 3 upgrades and one local save. No connections to Lords, Academy progression, multiplayer, backend, browser login, blockchain or real-world commerce. The player is stationary in each workplace.

## 2. Canonical state schema (v1)

Suggested serializable model (field names illustrative; semantics required):

| Field | Initial / rule |
|---|---|
| `saveVersion` | `1` |
| `coins` | `100`, integer ≥0 |
| `globalXp` | `0`, integer ≥0 |
| `masterLevel` | derived from XP, cap 5 |
| `selectedProfessionId` | `stable` initially |
| `skillXpByProfessionId` | all 7 IDs start at 0 |
| `completedCommissionIds` | initially empty unique set |
| `bestCommissionScores` | empty ID → best integer [0,100] |
| `activeCommission` | nullable commission ID + current steps; at most one |
| `processedAttemptIds` | unique result IDs to prevent double award |
| `firstCompletedTrainingIds` | empty unique set; stable skill first-completion bonus only once per training type |
| `purchasedUpgradeIds` | empty unique set |
| `horse` | name `Коник`, satiety 70, cleanliness 70, energy 80, trust 40, horseXp 0, lastUpdatedUtc, care cooldown timestamps |
| `history` | last 25 results (profession ID, commission/training ID, quality, coins, XP, timestamp) |
| `settings` | music/SFX volume, resolution/windowed preferences, tutorials shown |

Save `skillXpByProfessionId` and completed IDs by stable strings rather than serialized enum ordinals. Keep no cross-game foreign IDs. New Game resets only local Master progress after confirmation. Save includes a session-level counter or GUID to generate unique attempt IDs that cannot collide after relaunch.

## 3. Global progression

| Master level | Cumulative global XP |
|---:|---:|
| 1 | 0 |
| 2 | 75 |
| 3 | 180 |
| 4 | 330 |
| 5 | 500 |

Level capped at 5; XP can continue accumulating for display. Each profession has separate cumulative `skillXp` and a display rank:

| Skill rank | Profession XP |
|---|---:|
| Novice / Новачок | 0 |
| Apprentice / Учень | 15 |
| Craftsman / Ремісник | 35 |
| Master / Майстер | 70 |

These are **cosmetic progress titles**, not feature gates. Repeated activities can eventually increase skill rank without unlocking unimplemented mechanics. All professions available immediately.

## 4. Workshop commission reward contract

For all **six non-stable professions**, one accepted commission produces a numeric score (0–100) and successful/rejected status according to `ACTIVITIES.md`. No reward on rejected/canceled attempts.

**First successful completion** of job `J`:

- `coins = max(1, floor(baseCoins[J] * qualityScore / 100))`
- `globalXp = baseGlobalXp[J]` (from `CONTENT.md`)
- `skillXp[profession] = baseSkillXp[J]`
- Unlock the profession's second commission upon completing the first, idempotently.

**Successful repeat** (completed ID already present **before** transaction):

- `coins = max(1, floor(baseCoins[J] * 0.35 * qualityScore / 100))`
- `globalXp = max(1, floor(baseGlobalXp[J] * 0.25))`
- `skillXp = 5`
- No extra first-time bonus and no double-unlock.

A completion transaction is atomic: check unique attempt ID, compute reward, apply wallet/XP, update completion/best score/history, mark attempt committed, save; **double clicking submit does not pay twice**. Award screen reads the committed transaction receipt. Workshop order provides materials free; there is no spendable ingredient inventory in MVP, so low coins never block content.

**Example:** `SCR-01` successful quality 90 → first payout 18 coins, 10 global XP, 15 scribe skill XP; replay at quality 90 → `floor(20*0.35*0.9)=6` coins, `floor(10*0.25)=2` global XP, 5 skill XP.

## 5. Horse/training economy

Care action effects, hourly decay and the two training minigames are canonically specified in `ACTIVITIES.md`. Global XP and horseXP from each training are the **same numeric reward**; they are not two additive global rewards. Each completed training also increases `skillXp[stable]` by **5**; the first completed run of each training ID increases stable skill XP by an additional **10** one-time. Track this bonus with `firstCompletedTrainingIds` separately. Training is replayable, must debit energy at most once per start, and must pay coins/XP at most once per run ID.

When training eligibility fails, disable Launch with a reason: energy or satiety too low. Free ration and free rest ensure recovery. The stable is never gated by insufficient coins. Care itself does not print XP/coins.

## 6. Upgrades

| ID | Price | Effect |
|---|---:|---|
| `UP_GUIDE` | 50 coins | +10 (capped 100) to Scribe tracing and Tailor cutting scores before quality; does not change validation choice |
| `UP_BENCH` | 80 coins | +5 to Forge, Workshop, Cooking and Apothecary final quality score (cap 100), **only after all validity checks** |
| `UP_BRUSH` | 60 coins | Stable Groom restores +35 cleanliness (instead of +25), trust cooldown unchanged |

Each upgrade can be purchased once. Disable if insufficient coins; never allow negative balance or double deduction. Upgrades are **not mandatory** to beat any commission. Show benefits in the upgrade UI.

## 7. Task-state and switching

- Workshop: `AVAILABLE -> ACCEPTED -> WORKING -> READY_TO_SUBMIT -> (SUCCESS | REJECTED)`.
- On `SUCCESS`, atomic commit then return to workspace or hub; archive shows the result.
- On `REJECTED`, show explanation, allow free retry of current commission or return to profession; no data lost except incomplete task steps.
- On Back/Esc while active: `Continue task`, `Abandon task`, `Cancel`. Abandon discards only unsubmitted task UI inputs, never completed history, coins or upgrades.
- Switching profession during active commission requires confirmation. No overlapping active tasks or training.
- No countdown or daily login penalty for workshop jobs.
- Play can continue with replay jobs even after all 12 first completions. Show `All professions explored` badge but **not** a game-over.

## 8. Local saving

- Store versioned JSON (or current repo's verified robust format) under Unity `Application.persistentDataPath`.
- Use temporary file + atomic replacement when available; keep previous-good backup if practical. Never write directly over valid data during a partially serialized operation.
- Save on: initial New Game, profession selection, horse care, training start/finish, job acceptance, successful subtask checkpoint, job submit/rejection/abandon, upgrade purchase, settings changes, manual return to menu.
- Resume task from last *completed step* where implementable; partial drag/timing input need not be resumed, restart that individual interaction. Preserve one active job ID, previous successful trace and selection if persisted.
- On load apply capped horse elapsed-time change exactly once (do not double-apply from repeated scene loads); save new `lastUpdatedUtc` after applying.
- Corrupted/newer-version save → do not crash. Offer `Try backup`, `Start new`, `Quit`. Explicit confirmation for reset.
- Save/reopen must preserve horse needs/stage, wallet, skills, completed IDs, best scores, purchases and no duplicate rewards.

## 9. Main screens

- **Main Menu:** New Game, Continue (only if save), Settings, Quit.
- **Hub:** seven profession cards and summary wallet/level/skills; completed jobs progress `x/12`.
- **Profession Overview:** clear workstation with clickable hotspot and order list / Stable care panel; return hub.
- **Task Close-Up:** instructions, interactive component, step indicator, quality feedback only when appropriate, Back/Retry/Done.
- **Result:** success/rejected, quality, individual mistakes, coins/global XP/skill XP and `Next job` or `Retry`.
- **Upgrades:** 3 entries, costs, equipped state; no store monetization.
- **Journal:** 12 jobs and 2 training modes, completion indicators, best score, skill ranks; do not expose hidden answers.
- **Settings:** volumes, fullscreen/resolution, controls help, reset with confirmation.

No mock integration/Academy progress board. Local Academy is simply the fictional Scribe workplace name, not an extra resource currency.

## 10. Accessibility and UX

- Ukrainian player-facing text and font supporting Cyrillic. Use stable localization IDs for possible future translation.
- Mouse LMB interact, click/drag/hold, Space for timing/jump, Esc Back/Pause, Enter for focused UI confirmation; active control instructions visible.
- Tasks designed for 1280×720 and 1920×1080. At least 40 px UI tap targets at 1080p; visible hover/focus and large normalized drag regions.
- Avoid relying only on color for status. Provide textual icons, optional reduced camera motion/instant transitions if easy.
- No internet required, no forced real-time waiting, no microtransactions. Pause/quit does not erase earned rewards.

## 11. Out of scope

No Lords, Academy contributions, region resources, building upgrades, import/export from other Temnorid games, cloud accounts, mobile releases, advanced NPCs, 3D clothing or horse physics, simulated real-life disease treatment, procedural 3D buildings, external AI services or weapon crafting tutorials.

## 12. MVP finished when

Fresh user can play all **7 professions**; complete both jobs in each of 6 workshops; care for horse and play both training modes; successfully retry a deliberately failed job; earn/spend local coins; progress Master and profession ranks; quit/relaunch without losing state; open `/docs/index.html`; and play tested Windows standalone when build environment is available. Every live system is local and stable.
