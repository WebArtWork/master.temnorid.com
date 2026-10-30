# Temnorid: Master — Screens, Camera and Presentation

## Visual identity

Warm, clean, stylized low-poly medieval environment: wooden beams, parchment cards, iron anvil, clay jars, wool, herbs, stable hay. Bright enough to see interactables, not photorealistic. Original simple materials and shapes are acceptable. Avoid hard dependencies on purchased textures and remote generators. All working objects get a text label/hover affordance.

## Primary navigation

```text
Start / Continue
    |
    v
Profession Hub — [Stable] [Scribe] [Forge] [Workshop] [Cooking] [Tailor] [Apothecary]
    |                                                  |
    v                                                  v
Stable (care -> training -> result)           Workroom preset (commission list)
    |                                                  |
    +-------------------------< Back >------------------+
                                                        |
                                               Task Focus (1–3 steps)
                                                        |
                                               Result -> Replay / Next / Hub
```

Screen titles in Ukrainian: `Майстер`, `Професії`, `Стайня`, `Писар`, `Коваль`, `Майстерня`, `Кухня`, `Кравець`, `Травник`, `Замовлення`, `Результат`, `Покращення`, `Журнал`, `Налаштування`.

## Camera presets

| Camera mode | Placement | Context |
|---|---|---|
| `HUB` | Fixed 3/4 across workshop / board | Choose profession |
| `STATION_OVERVIEW` | Fixed 3/4 on current desk / workstation | Current profession |
| `STATION_FOCUS` | Near-top or angled crop over task surface | Stamping, tracing, hammer, parts, recipe |
| `STABLE` | Fixed 3/4 with entire horse visible | Care / needs |
| `TRAIN_SIDE` | Fixed side of obstacle course | Obstacle run |
| `RESULT` | Reuse prior camera behind legible modal | Reward summary |

Fixed-camera starter's current APIs must be checked. Do not require Unity Cinemachine unless present and appropriate. A simple set of enabled Camera objects or virtual camera presets is fine. Lock input during blends, restore valid view on Esc, don't trap cursor behind modal. No avatar tracking.

## Hub layout

- Top left: logo `Temnorid: Master` and short one-line objective.
- Top right: `Coins`, `Master Level`, current XP; persistent throughout.
- Center: seven clickable profession cards with distinct icon, short tagline, current skill rank and first-completion count; do not require player walking to an entrance.
- Bottom: `Журнал`, `Покращення`, `Налаштування`, `Вийти`.

## Workbench layout

- Physical 3D desk receives contextual props; large on-screen `Orders` panel lists 2 authored jobs, locked/unlocked/completed state.
- In a job, use left-to-right numbered step indicator and one central interaction panel. Only essential controls on screen at a time. Avoid multi-panel UI on 720p.
- Task feedback shows numeric result where appropriate without leaking hidden validation before submit (Scribe ACCEPT/FLAG, chosen forge finish, exact part recipe answers).
- Disabled next action always displays a reason. Always provide `Спробувати ще`, `Назад`, `Підтвердити`.

## Stable layout

Horse centered and responsive; four bars at bottom `Ситість`, `Чистота`, `Енергія`, `Довіра`; buttons `Сіно`, `Безкоштовний корм`, `Почистити`, `Погладити`, `Відпочити`. Right panel: `Перешкоди`, `Команди`. Show coin price and eligibility/energy cost in button tooltip. Rest can be sped through a short locally animated sequence, not a real-world wait.

## Result panel

Show icon and title `Успіх` or `Спробуй ще`, score `NN/100`, concise breakdown (e.g. accuracy, timing or recipe choice), earned coins/XP/skill XP, first-time badge. Rejection reveals **why**, then free `Повторити` button. Successful result reads an already committed receipt, never triggers a fresh reward itself.

## Assets plan (low cost)

| Workspace | Primitive props |
|---|---|
| Stable | box stall, low-poly foal, feed trough, hay, brush, fence, hurdle blocks |
| Scribe | wood desk, parchment quad, quill, three stamp shapes, shelf |
| Forge | anvil blocks, non-realistic glowing metal shape, hammer, horseshoe icon |
| Workshop | simple timber table, three numbered slots, gears/cylinder parts |
| Cooking | pot, spoon, bowl, stylized ingredient blocks |
| Tailor | fabric plane, scissors prop, animated thread line |
| Apothecary | mortar, pestle, herb icon bundles, labeled jars |

No large 3D asset generation required. Agents should first make mechanics complete using primitives; optional visual polishing only after playtesting.

## Help and hints

- First visit to each profession: short overlay, one example interaction, dismiss or reopen with `?`.
- Distinct audio: paper scratch, hammer thud, bubbling, cloth swish, gentle horse snort—optional synthesized/clearly licensed assets. Sound disabled must not break minigame timing.
- Documents and interactions must be readable for Ukrainian Cyrillic; avoid tiny overlays.
