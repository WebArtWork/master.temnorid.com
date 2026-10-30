# Temnorid: Master — Game Design Document

## 1. Fantasy and mood

A medieval workshop career simulator. You are a young villager seeking mastery of several trades. Each profession offers satisfying, direct manipulation of objects with mouse and keyboard; there is no long tutorial, survival punishment, or mandatory daily login. Some commissions are peaceful; others require careful timing, concentration and solving a mechanism. The world's population consists of humans and ordinary animals. Magic exists in lore but is not required to play this MVP.

The game is **not** a city builder, an RTS or an open-world life simulator. The player does the work personally in short, self-contained activities, similar in approachability to a virtual-pet game.

## 2. Core loop

**Choose profession → Read order or check horse → Perform 1–3 hands-on interactions → Review results → Earn coins and XP → Improve workstation → Choose next activity.**

Expected session 3–8 minutes. A complete run through all 12 commissions plus two training modes is an extended goal, not a timed speedrun.

## 3. Places

1. **Workshop Hub:** stationary camera looks across warm medieval common workspace; profession-selection sign/board provides seven cards. Top bar shows coins, Master level and profession. Player may switch professions between jobs.
2. **Shared Workroom:** one 3D scene for Scribe, Forge, Mechanic/Workshop, Cooking, Tailor, and Apothecary. Replace a handful of visible props/bench dressing by profession preset; do not load an entire separate open world for each. Each has fixed overview and one or two close-up camera poses.
3. **Stable & Training Yard:** horse in a small stall (foal at game start), feed trough, brush, resting mat and interaction markers. A short 2D/3D side-view course and a timing bar are training modes. No player avatar, no horse physics rig needed.

A simple wooden button or nav panel returns to the Hub. Add a `Back` action and Esc. Never require a 3D camera transition to reach a button.

## 4. Professions

| ID | Profession | Workspace | What player does | MVP task types |
|---|---|---|---|---|
| `stable` | Stable / Догляд за конем | Stable | Raise foal, care for horse and train | Feed, groom, rest, pet, obstacle run, command timing |
| `scribe` | Scribe / Писар | Desk | Compare records, write glyph, press fictional seal | inspect, trace, seal |
| `forge` | Forge / Коваль | Anvil | Shape tools with timed heat and hammer taps | heat, strike, quench |
| `workshop` | Workshop / Механік | Mechanical bench | Assemble gates/gears and test movements | select and place parts, test |
| `cooking` | Cooking / Кухар | Kitchen | Pick ingredients, control heat and stir | ingredient choice, timing |
| `tailor` | Tailor / Кравець | Sewing desk | Choose fabric, cut pattern, stitch | selection, trace, timing |
| `apothecary` | Apothecary / Травник | Herb bench | Prepare fictional village herb items | ingredient choice, grind, label |

All 7 unlocked from the beginning. Workshops each have two authored jobs, with job #2 unlocking on first successful completion of #1. Horse training modes are both available immediately when its needs permit. Cosmetic room changes reflect selected profession; no need for NPCs to physically walk into room.

## 5. Suggested first-session walkthrough

1. New Game displays a friendly Ukrainian prompt and the seven professions. The **Stable** card is highlighted but not forced.
2. Enter Stable, click foal, see four needs. Groom, feed a free basic ration, pet, practice **Command Timing** (5 input beats), receive coins and XP.
3. Return to the hub. Open Scribe, select `SCR-01`, compare the simple document, trace a symbol, stamp it, file and see reward.
4. Open Forge, complete a simple horseshoe commission using a heat bar and three hammer taps.
5. Coins support buying a small improvement. On leaving/reloading, all progress is present.
6. Explore the other workshop jobs in any order. Show optional contextual tips on first visit to each profession, not long modal tutorials.

## 6. Camera language

- **Overview:** fixed 3/4 view showing the environment and the most important interactables.
- **Task Focus:** fixed close-up at desk/anvil/bench; no need for free camera rotation. UI minigame may be a flat overlay over the relevant prop.
- **Stable:** fixed 3/4 horse view, fixed side view for obstacle run, UI overlay for command timing.
- **Result:** keep current overview, show a panel; restore a valid camera on Esc and successful submission.

Aim for quick, predictable blends around 0.25–0.45 seconds if supported; instant transitions are acceptable when blends cause input issues. When camera switches, do **not** invert movement because the user is not controlling an avatar. Block pointer interactions while transitioning.

## 7. Feedback and tone

- Friendly rather than punishing. Wrong materials, broken mechanisms or incorrect paperwork receive an explanation and unlimited retry at no cost.
- Reward feedback: coin icon, XP, profession skill XP and current grade; first-time completion gets a distinct badge.
- The horse reacts to actions via simple head movement, tail animation, body bob or sound; it never dies or gets permanently sick.
- Low-poly wood, parchment, wool, iron, herb colors, warm ambient lighting. No fancy creatures, guns, combat or horror.
- No pop-up ads or real money.

## 8. Long-term extensibility (code organization only, NOT MVP features)

A profession is represented by stable ID + room preset + commission catalog + registered task modes; new professions can reuse screens and save conventions. Avoid prematurely building integration or dynamic mod support. All player progression belongs to Temnorid: Master, not other games.
