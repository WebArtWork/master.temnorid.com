# Temnorid: Master — Canonical Activity Mechanics

All activity calculations here are **game abstractions**, not real instructions for medicine, metallurgy or weapon construction. Each interactive task must be achievable using mouse/pointer plus an optional Space-key action. Values are normalized floats in `[0,1]` where not otherwise stated. All task scores clamp to integer `[0,100]`. Pure-scoring functions should have deterministic unit tests.

## A. Stable (`stable`)

### Horse

One ordinary horse, starting as a foal named `Коник` (player can rename). Render with primitives when necessary. Persist needs (integers 0–100): satiety **70**, cleanliness **70**, energy **80**, trust **40**. Starts local horse level 1, XP 0. Never die or disappear.

| Action | Cost | Immediate effect | Interaction / duration |
|---|---:|---|---|
| Hay | 10 coins | satiety +30; trust +2 | click trough, ~2s reaction |
| Basic ration | Free | satiety +15 | click Free Feed, ~2s; always available |
| Groom | Free | cleanliness +25; trust +3 only if 30s cooldown elapsed | drag brush over horse or 3s click-and-hold |
| Pet | Free | trust +5 only if 30s cooldown elapsed | click horse / 2s reaction |
| Rest | Free | energy +40 | click bedding, 8s local in-game (no wait outside game) |

Clamp needs to [0,100]. If insufficient money for hay, free ration remains available. Horse training cannot be opened while a care action is in progress. Care costs never produce negative coins. Cooldowns are optional visual affordances; re-click within cooldown does not grant trust again.

**Offline change:** on loading and while open, apply up to **8 elapsed hours** since last applied timestamp, using UTC (never on every redraw): satiety −2/h, cleanliness −1/h, energy +5/h, trust unchanged. Store fractional remainders or deterministic rounding. Cap elapsed interval to 8h to avoid punishing absence. Clamp device clock rollback to zero elapsed.

**Horse growth:** horse XP thresholds 0 / 100 / 250 / 450 / 700 for level 1–5. Foal levels 1–2, young horse levels 3–4, adult level 5. Show visible scale/proportion change or cosmetic indicator. Horse-level XP earned only from training; global Master XP also earned (same values). No breeding, injuries or hunger deaths.

### STA-OBS — Obstacle Run

Fixed side-view track, eight spaced obstacles, Space/tap to jump. Horse auto-moves at constant speed for a ~45–60 second run; cheap primitive obstacle and arc jump are enough. **Eligibility:** energy ≥20, satiety ≥20. Deduct 20 energy once at start. Collision counts as a missed obstacle, no health damage or restart. Correct clear +10 points; maximum 80 display points. `cleared ∈ [0,8]`.

- Coins `10 + 2 * cleared` (10–26).
- Global XP and horse XP `10 + 3 * cleared` (10–34).
- Quality score `round(100 * cleared / 8)`.
- If cleanliness <20 at starting time, reduce coin reward by **20%** using `floor` (XP unchanged).
- Training completion awards once per run ID. Free replay after energy recovery.

### STA-CMD — Command Timing

Five rounds, horizontal moving marker; press Space/tap when marker is within central highlighted zone. Start speeds deterministic and varied; 1 hit per round, no multiple input. **Eligibility:** energy ≥10, satiety ≥20. Deduct 10 energy at start. `hits ∈ [0,5]`.

- Coins `8 + 3 * hits` (8–23).
- Global XP and horse XP `8 + 4 * hits` (8–28).
- Quality score `20 * hits`.
- Same cleanliness modifier as above.
- Missed press is not a blocker; display horse reactions and results.

## B. Scribe (`scribe`)

Each commission: inspect a source vs reference note (`ACCEPT` or `FLAG`), trace a decorative fictional path, choose one of three fictional seals and place it on the target.

1. Inspection answer exactly matches `CONTENT.md` developer key (player sees both documents and the question, not answer).
2. Trace: pointer down→move→release; require ≥10 captured points, normalize to panel `[0,1]²`, resample both player and reference polylines to **64 equal arc-length points**. Let `err` = mean Euclidean point distance. `trace = clamp(round(100 * (1 - err/0.15)),0,100)`. A path can be retried; require trace ≥50 to submit. Provide visible path and avoid physically simulating a pen.
3. Seal: allowed IDs `ACADEMY` (book), `VILLAGE` (tower), `MERCHANT` (scales). Correct seal ID and position distance ≤0.12 normalized paper distance. Use an obvious target circle and stamp preview. No false correctness indication before final filing.
4. If wrong inspection/seal, **reject without rewards**, explain the discrepancy and allow retry.
5. If valid: `quality = 40 + round(30 * trace / 100) + 30`, capped 100 (85–100). One commission is one submission transaction.

Two jobs: `SCR-01`, `SCR-02` in CONTENT.md. UI close-ups for comparison, trace, seal. Older Scribe prototype is a reference only; don't copy a Lords Academy board or Knowledge stat into Master.

## C. Forge (`forge`)

A stylized smithy minigame with no realistic metalworking instructions or 3D physics. Every forge order follows **Heat → Hammer (3 strikes) → Select Finish → Submit**. Commission supplies materials at no player cost.

- **Heat bar:** moving indicator on `[0,1]`; target `targetHeat` authored per job. On one press lock value. `heatScore = clamp(round(100*(1-abs(locked-targetHeat)/0.40)),0,100)`.
- **Hammer:** three round targets visually marked on a flat silhouette; click each target. For each `distance = clamp(distance(pointer,targetCenter),0,1)` normalized to anvil panel; `strikeScore_i = clamp(round(100*(1-distance/0.20)),0,100)`; `hammerScore=round(mean(three))`. Show three separate successive targets. No camera physics.
- **Finish:** choose required fictional treatment `GENTLE` or `QUICK` stated in order. Wrong finish rejects job (feedback after submit).
- **Quality:** `round(0.40*heatScore + 0.60*hammerScore)` when finish is correct; **success requires quality ≥60**. Failure yields zero rewards, free retry.
- Visual: anvil, glowing stylized material block, hammer block, simple output silhouette (horseshoe or short practice sword). Do not require realistic forging or working weapon instructions.

## D. Workshop (`workshop`)

A small 3-slot assembly puzzle. Each order has one correct ordered combination of three component IDs and a test target value. Four parts are offered, so there is one distractor. Drag or click to assign a part to each numbered slot; allow removal and replacement before testing.

- Parts must match **all three** expected IDs in the correct slot order, otherwise on submit reject with a mechanism-specific hint, no rewards.
- When parts are correctly placed, player runs a short test bar (Space/tap), indicator stops at selected position. `testScore = clamp(round(100*(1 - abs(locked-targetTest)/0.45)),0,100)`.
- `quality = round(70 + 0.30*testScore)` (70–100) for correct assembly. Always successful when parts correct; test quality affects money but does not require a perfect timing press.
- Simulate moving gate latch or wooden gear via a simple 10-degree rotation or translation on testing. No full physical machine or visual scripting engine.

## E. Cooking (`cooking`)

Each order uses a supplied recipe with three correct ingredient IDs drawn from four visible icons. Game ingredients are generic items; no detailed historical cooking simulation.

- Select **exactly three** ingredients. Set comparison must equal order's correct ingredient set; wrong set rejects on submit and explains one mismatch. Ingredients do not cost local coins.
- Heat control: player locks marker on bar near authored target `targetHeat`. `heat = clamp(round(100*(1-abs(value-targetHeat)/0.45)),0,100)`.
- Stir: press the button when the marker crosses target, **three rounds**; `stir = round(100*hits/3)`.
- `quality = round(60 + 0.20*heat + 0.20*stir)` for correct set, success without strict skill gate, 60–100.
- Show cauldron, spoon, bowl, steam. No complete recipe/cooking physics simulation.

## F. Tailor (`tailor`)

Use one fabric selection + guided pattern cut + three stitch beats.

- Fabric: `WOOL` or `LINEN` (the job tells requested material); wrong choice rejects on submit.
- Cutting: trace a visible simple pattern; same 64-point arc-length algorithm as Scribe. `cut = clamp(round(100*(1 - err/0.15)),0,100)`; require ≥40 to submit (allows accessibility).
- Stitching: three timing presses; `stitches = round(100 * hits / 3)`.
- `quality = round(0.60*cut + 0.40*stitches)`; requires ≥60 for success. Show result on cloth silhouette. Wrong fabric always fails.
- No cloth physics, realistic garment simulation or character clothing mesh changes required.

## G. Apothecary (`apothecary`)

In-game **fictional herbal commission simulator**, not a medical guide. Use everyday-looking herbs but do not imply real health treatments or doses. Each order is a scented village sachet or decorative herb mixture, **not a therapeutic product**.

- Choose exactly two herbs out of four. Compare set against the correct IDs in CONTENT.md; wrong combination rejects at submission.
- Grind: hold-and-release or press moving marker on a simple bar to target `targetGrind`; `grind = clamp(round(100*(1-abs(value-targetGrind)/0.45)),0,100)`.
- Label: choose fictional label icon `SUN`, `MOON` or `LEAF`, required icon specified in order; wrong label rejects.
- `quality = round(60 + 0.40*grind)` if correct herbs and label; range 60–100; no strict grind threshold.
- Visual: wooden counter, mortar, herb bundles, small jars. Absolutely no real dosage, medical claims or instructions for mixing real remedies.

## Shared activity rules

- `Attempt` state: `AVAILABLE -> ACCEPTED -> WORKING -> READY_TO_SUBMIT -> SUCCESS | REJECTED`. Users may Back/Retry. No negative consequences for wrong answers.
- **Exactly one active commission**. Switching professions while a commission is active asks for confirmation; preserve completed subtasks (or cancel explicitly). Do not allow overlapping reward commits.
- Close-up may use a 2D overlay panel anchored to a 3D prop. Keep click/drag hitboxes large.
- Only `Economy.CommitCompletion(uniqueAttemptId, result)` grants coins/XP; animations, view panels and buttons never grant currency.
- Stable training uses its own unique run ID and does not use first-time commission payouts. Both horse and workshop results enter the same result/history UI.
