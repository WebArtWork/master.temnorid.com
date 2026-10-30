# Temnorid: Master — Canonical Authored Content

**All IDs and expected answers in this file are developer-only.** The player sees the source/order and rules, but not invisible correctness flags or answer keys until the result. Ukrainian UI copy is authoritative for MVP. Commission base coins and XP are consumed by the shared reward rules in `MVP.md`.

## Catalog overview

Each of the six workstation professions has exactly **two authored commissions**: the first available on New Game; second unlocks on first successful completion of the first. Stable has two repeatable minigames instead of commissions. **All seven professions are accessible immediately.**

| ID | Profession | Ukrainian title | Base coins | Global XP | Skill XP |
|---|---|---|---:|---:|---:|
| `SCR-01` | scribe | Перший запис | 20 | 10 | 15 |
| `SCR-02` | scribe | Розбіжність у коморі | 28 | 14 | 20 |
| `FOR-01` | forge | Підкова для стайні | 24 | 12 | 15 |
| `FOR-02` | forge | Навчальний меч | 34 | 16 | 20 |
| `WRK-01` | workshop | Засув воріт | 24 | 12 | 15 |
| `WRK-02` | workshop | Передача млина | 34 | 16 | 20 |
| `CK-01` | cooking | Селянський суп | 20 | 10 | 15 |
| `CK-02` | cooking | Ячмінна каша | 28 | 14 | 20 |
| `TLR-01` | tailor | Вовняна торбина | 22 | 11 | 15 |
| `TLR-02` | tailor | Лляний плащ | 32 | 15 | 20 |
| `APT-01` | apothecary | Пахучий мішечок | 20 | 10 | 15 |
| `APT-02` | apothecary | Трав'яний набір для комори | 30 | 14 | 20 |

**Base reward**: quality scales coins, while first successful completion awards listed XP and skill XP. Repeats use reduced values. Wrong/rejected attempts pay nothing. Content is preauthored, with no required randomness.

## Scribe jobs

### `SCR-01` — Перший запис
- Story: “Старша писарка Марта просить підготувати запис для Академії.”
- Source: `Академія. Учнів: 8. День: 3.`
- Reference: `Журнал занять: день 3, присутні 8 учнів.`
- Question: `Чи збігаються день та кількість учнів?`
- **Developer answer**: `ACCEPT`.
- Stroke: `stroke_arc` (gentle left-to-right upward arc, ≥5 authored normalized points).
- Seal: `ACADEMY`; stamp center fixed and visibly marked on parchment.
- Success explanation: `Дані збігаються. Запис внесено до архіву.`

### `SCR-02` — Розбіжність у коморі
- Story: “Комірник просить перевірити облік мішків зерна.”
- Source: `Відвантажено: 14 мішків зерна.`
- Reference: `Розписка візника: 12 мішків зерна.`
- Question: `Чи підтверджена кількість у відомості?`
- **Developer answer**: `FLAG` (14 ≠ 12).
- Stroke: `stroke_hook` (downstroke with curved hook, ≥5 authored normalized points).
- Seal: `VILLAGE`.
- Success explanation: `Розбіжність знайдена: у розписці 12, у відомості 14.`

## Forge jobs

### `FOR-01` — Підкова для стайні
- Order: `Підготуй підкову. Обери м’яке завершення роботи.`
- `targetHeat=0.45`; hammer target centers (normalised panel): `(0.30,0.45),(0.50,0.35),(0.70,0.45)`.
- Required finish `GENTLE`; result prop `horseshoe`.
- Failure hints: `Перевір спосіб завершення` or `Спробуй точніше нагріти та обробити заготовку`.

### `FOR-02` — Навчальний меч
- Order: `Підготуй декоративний навчальний меч для вправ. Обери швидке завершення.`
- `targetHeat=0.62`; hammer centers `(0.25,0.50),(0.50,0.50),(0.75,0.50)`.
- Required finish `QUICK`; result prop `practice_blade`. No realistic weapon instructions or usable weapon production specifications.

## Workshop jobs

### `WRK-01` — Засув воріт
- Order: `Збери простий дерев'яний засув і перевір хід важеля.`
- Parts available: `LEVER`, `PIN`, `SPRING`, `GEAR` (distractor).
- Expected slot order: `PIN → LEVER → SPRING`.
- `targetTest=0.50`.
- Demonstration on completion: gate bar slides / latch rotates.

### `WRK-02` — Передача млина
- Order: `З'єднай деталі приводу млина і перевір обертання.`
- Parts available: `AXLE`, `GEAR`, `CRANK`, `BOLT` (distractor).
- Expected slots: `AXLE → GEAR → CRANK`.
- `targetTest=0.65`.
- Completion: visible low-poly gears rotate for 2 seconds.

## Cooking jobs

### `CK-01` — Селянський суп
- Order: `Звари суп із трьох потрібних інгредієнтів.`
- Available: `WATER`, `CARROT`, `ONION`, `APPLE` (distractor).
- Correct set: `{WATER, CARROT, ONION}`.
- `targetHeat=0.50`; three stir beats.
- Output: steaming bowl model. This is fictional recipe gameplay, not a real cooking guide.

### `CK-02` — Ячмінна каша
- Order: `Звари просту кашу, не переплутавши складники.`
- Available: `WATER`, `BARLEY`, `MILK`, `SALT` (distractor).
- Correct set: `{WATER, BARLEY, MILK}`.
- `targetHeat=0.60`; three stir beats.

## Tailor jobs

### `TLR-01` — Вовняна торбина
- Order: `Підготуй вовняну торбину. Виріж тканину за контуром і проший край.`
- Required fabric `WOOL` (alternative `LINEN` visible).
- Pattern `cut_rectangle` (rounded rectangular polyline path, ≥5 points).
- Stitch beats: 3, each with one highlighted zone.
- Output: folded cloth pouch.

### `TLR-02` — Лляний плащ
- Order: `Зший простий лляний плащ за шаблоном.`
- Required fabric `LINEN` (alternative `WOOL`).
- Pattern `cut_cape` (symmetric angled path with curved lower edge, ≥5 points).
- Stitch beats: 3. Output flat cape silhouette; no cloth physics.

## Apothecary jobs

These are **fictional, non-therapeutic herbal goods**. Do not imply that these combinations cure real illnesses; never provide medical dosages.

### `APT-01` — Пахучий мішечок
- Order: `Збери запашний мішечок для кімнати з м'яти та ромашки, прикрась символом листка.`
- Available herbs: `MINT`, `CHAMOMILE`, `THYME`, `ROSEMARY`.
- Correct set `{MINT,CHAMOMILE}`.
- `targetGrind=0.50`; required label `LEAF`.
- Output: fragrant sachet (decorative item).

### `APT-02` — Трав'яний набір для комори
- Order: `Підготуй декоративний трав'яний набір із чебрецю та розмарину; потрібна сонячна позначка.`
- Available same four herbs.
- Correct set `{THYME,ROSEMARY}`.
- `targetGrind=0.65`; required label `SUN`.
- Output: labeled herb bundle for storage (no medical effect).

## Horse training content

- `STA-OBS`: `Перешкоди` — eight simple hurdles; jump, scoring and rewards per `ACTIVITIES.md`.
- `STA-CMD`: `Команди` — five timing prompts; scoring and rewards per `ACTIVITIES.md`.
- Start horse name `Коник`, with four meters, two care locations and two training activity buttons.

## Asset/animation placeholders

Use shared low-poly icons and primitive shapes: horse body/legs/head with simple keyframe nod; parchment with printed text; 3 seal silhouettes; anvil/hammer/horse shoe; basic wooden gears; kitchen pot/spoon/vegetable cubes; scissors/fabric piece; mortar/jars/herb bundles. Use clear labels for each part so temporary meshes are understandable. No asset generation prerequisite for a playable MVP.
