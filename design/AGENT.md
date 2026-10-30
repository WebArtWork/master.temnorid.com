# Temnorid: Master — Instructions for Local AI Agent

## 0. Rules before editing

1. Read all eight docs in `README.md` order. `CONTENT.md` is canonical for authored jobs; `ACTIVITIES.md` for task math; `MVP.md` for rewards, state, caps and persistence. If contradictions appear, prefer their explicit canonical ownership, document and resolve before implementing.
2. Audit the actual game repository. Read its `AGENTS.md`, `CLAUDE.md` if present, `README.md`, `Packages/manifest.json`, and `ProjectSettings/ProjectVersion.txt`. Existing project instructions take precedence over speculative class names here.
3. **Do not delete old Pets or Scribe repos or modify them.** Create the game independently; legacy concepts are design references, not required runtime dependencies.
4. Never assume a shared `3d-*` package API exists. Verify each dependency, version and class before using. If a general camera/input capability is missing, prefer the smallest game-local implementation unless the constructor's documented contribution process clearly requires a shared-package change.
5. Keep Windows build runnable offline. No login, web requests, external AI, user accounts or integration placeholders that masquerade as functioning services.
6. Implement playable paths before decoration. Put all job data under stable IDs, no magic numbers scattered in MonoBehaviours, and separate data/state/rules from scene view code.

## 1. Base project and packages

- Suggested starter: https://github.com/IT-Kamianets/3d-constructor-fixed-view . At documentation time its public README described an *empty Unity 6000.6 Universal 3D* starter, with fixed-camera logic not yet built. Verify the actual current state locally first.
- Starter may mention `3d-unity-core`, `3d-unity-scene`, `3d-unity-input`, `3d-unity-character`, `3d-unity-assets`, `3d-unity-audio`. Only install what this game truly uses; a character-movement package is probably unnecessary.
- Review https://3d.itkamianets.com/packages.md if reachable; this is a reference, **not** a claim that APIs exist.
- Do not upgrade Unity automatically or invent a working package integration.

## 2. Keep architecture lean

**Game-owned modules** (names illustrative, adapt to actual architecture):

| System | Responsibility |
|---|---|
| `MasterSession` | Single mutable authority for coins, XP, profession progress, transactions, job completion, horse and upgrades |
| `ProfessionCatalog` | Stable profession definitions, room presets, unlock flags, display labels |
| `CommissionCatalog` | Immutable 12 authored jobs; correct answers and required task configs |
| `ActivityController` | Launch/cancel/finish one active interaction; delegate to profession-specific rules |
| `TaskScoring` | Pure functions for normalized trace, timing, drag tolerance, recipe and parts validation |
| `CameraViewController` | Fixed overview / close-up / yard camera presets and safe returns; no reward logic |
| `RoomPresenter` | Reusable workroom populated with station props/icons for selected profession |
| `HorseController` | Stable care actions, needs update, training eligibility, simple feedback animation |
| `Economy` | Exact once reward commit, upgrades, no negative wallet, global/local XP |
| `SaveService` | Versioned JSON save, atomic file swap + optional previous-save fallback |

Do not build a generalized node editor, task scripting language, multiplayer-compatible inventory, or complex RPG framework for a 0.1 MVP. Use ScriptableObjects, serializable C# content or local JSON depending on current repository conventions. Unit-test core calculations without a scene.

## 3. Proposed layout

```text
<temnorid-master>/
├── AGENTS.md                       # Keep original; add links to docs where useful
├── README.md / GAME.md / ...       # These guides at root or in docs/agent/
├── Assets/
│   └── TemnoridMaster/
│       ├── Scenes/Boot.unity
│       ├── Scenes/Workshop.unity    # Selection hub + interchangeable workstations
│       ├── Scenes/Stable.unity      # Horse and fixed-side training
│       ├── Content/                # Professions, commissions, stroke paths, presets
│       ├── Scripts/Core/
│       ├── Scripts/Professions/
│       ├── Scripts/UI/
│       ├── Prefabs/                # Station presets / primitives
│       ├── Materials/
│       ├── Audio/
│       └── Tests/EditMode/
├── Packages/
├── ProjectSettings/
└── docs/index.html                 # Public game page, separate from agent docs
```

`Workshop.unity` may have a menu overlay showing a cozy room and a selection board; selecting a profession replaces room dressing and opens its station. Stable is separate for the horse's presence and mini-games. Prefer shared UI panels and one generic camera preset abstraction rather than 7 full scenes. If scene conventions differ, match the starter and document the decision.

## 4. Milestone order — finish each before the next

- **Phase 0 / Audit:** inspect starter, input, UI and camera APIs. Open empty game successfully in Unity. Produce simple visible startup menu.
- **Phase 1 / Shared vertical slice:** Workshop selection, one fixed camera and one close-up, mouse interaction, shared wallet/save, **Scribe SCR-01** full inspect → trace → stamp → submit loop with failure/retry and exact-once reward.
- **Phase 2 / Second loop:** stable with horse, feed/groom/rest/pet, fixed camera, command-timing minigame. Finish and re-open game to verify save.
- **Phase 3 / Professions:** Forge → Workshop → Cooking → Tailor → Apothecary, first authored commission for each. Use small placeholder meshes and direct UI minigames; playable first, decorate later.
- **Phase 4 / Content and progression:** remaining six commissions, obstacle run, first-time vs repeat awards, upgrades and skill XP, tutorial help.
- **Phase 5 / Reliability:** save migration/error handling, exact-once commits, restart during task, UI scaling, keyboard escape/back and automated tests.
- **Phase 6 / Release:** add `docs/index.html`, capture real screenshots, Windows build, test, report. GitHub release creation/push only with permission.

## 5. Quality decisions

- Main cursor: point-and-click only; no controllable player avatar.
- Tasks are short, deterministic and fair; every failure gives feedback and a free retry. Submitting a wrong task must not charge materials or permanently lock content.
- Author jobs as data. Do not use random correct answers. No unavailable external asset dependency; primitives, colored panels, text and simple particles are enough.
- Set English-stable ID strings for data. Display Ukrainian in-game text, including labels and help.
- Support 1280×720 and 1920×1080. No gameplay-critical details hidden behind tiny hit targets or lighting.
- No real-life medicine or legal-document instructions presented as factual advice: recipes and seals are explicitly fictional game mechanics.

## 6. Scope and source control

Only commit changes in the game's repository. Do not automatically migrate, overwrite or remove the previous Pets/Scribe projects. Do not publish unapproved packages or create a remote release without explicit permission. Report missing build tooling rather than asserting completion. Never create shared packages for profession-specific UI/state.
