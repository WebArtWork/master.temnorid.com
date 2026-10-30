# Temnorid: Master — AI Agent Implementation Guides

**Release:** `0.1.0-mvp`  
**Genre:** Cozy medieval professions / workbench minigames / virtual pet care  
**Platform:** Unity Windows desktop, offline  
**Suggested repository:** `temnorid-master`  
**Constructor:** https://github.com/IT-Kamianets/3d-constructor-fixed-view  
**Game text:** Ukrainian (`uk-UA`) first, stable localization keys  
**Setting:** Temnorid; human society, ordinary real-world animals, optional magic (magic not needed for MVP)

## Vision

The player is a novice who gradually becomes a **Master** by practicing several everyday professions in one game. Instead of installing separate games for horse care, documents, forge work, engineering, cooking, tailoring and herbal preparations, the player chooses a workplace, performs short tactile tasks, collects local coins/XP, purchases modest upgrades and returns later. Each activity stands on its own but shares **one save, one installation and one UI language**.

**No Lords or other Temnorid integrations in this MVP.** There is no server, simulated village contribution, synchronization, cross-game reward, or Academy integration. Names such as Academy or Smithy are flavor for the standalone world only. The user will remove/retire separate prototype projects themselves; do not delete or migrate any existing repositories.

## MVP scope at a glance

| Category | Decision |
|---|---|
| Professions | **7**: Stable/Pets, Scribe, Forge, Workshop, Cooking, Tailor, Apothecary |
| Game layout | One selection hub + shared workroom using different station presets + a small stable/training yard |
| Character | No movable avatar; interact by click, drag, press, and focus camera |
| Camera | Fixed cinematic overview and task-specific close-ups; stable obstacle game can use a fixed side camera |
| Content | **12 authored workshop commissions** (2 per six non-pet professions), horse care and **2 training modes** |
| Progress | One wallet, global XP/level, local skill XP for each profession, three upgrades |
| Time | ~3–8 min per visit; ~35–60 min to see all first-time workshop jobs and training modes |
| Persistence | Versioned local autosave, safe recovery, replay without duplicate first-completion bonus |
| Assets | Simple low-poly primitives, authored icons, simple procedural motions; no purchased/AI assets required |
| Delivery | Windows build when tooling permits; `docs/index.html` with verified release link or clearly disabled TODO |

## Read in order

1. [AGENT.md](AGENT.md) — agent rules, starter verification, architecture, phases.
2. [GAME.md](GAME.md) — vision, scenes, player journey, camera and activities.
3. [ACTIVITIES.md](ACTIVITIES.md) — implementable minigame mechanics for all seven professions.
4. [CONTENT.md](CONTENT.md) — canonical IDs, 12 commissions, instructional text and answers.
5. [MVP.md](MVP.md) — state contract, economy, progress, saving, UI requirements.
6. [UI.md](UI.md) — screens, camera locations, controls, accessibility, art targets.
7. [QA_RELEASE.md](QA_RELEASE.md) — test vectors, release website, Windows build, definition of done.

## Non-goals

No walking around a full village, multiplayer, combat, NPC pathfinding, simulation of real smithing/metallurgy or medicine, realistic horse anatomy, OCR, handwritten text recognition, custom 3D tool physics, procedural job generation, cloud saves, mobile distribution, real-world document verification, real-money shop, blockchain, or live services. Do not design game-specific shared engine packages: keep content inside the game.

## Final handoff from the agent

Report the exact Unity version; installed real package names; implemented content and missing items; tests actually run and results; Windows build path (if produced); `/docs/index.html` location; real GitHub Release URL only when verified; known limitations. Do not claim an installer or successful tests unless checked.
