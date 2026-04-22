# Synthesis — Полный контекст проекта и истории работ

Версия: 2026-04-22  
Формат: единый контекст для переноса в ChatGPT (браузер)  
Источник: фактическое состояние проекта `E:\University\Diploma\diploma` + накопленная история работ в этой сессии и рабочих `.md`

---

## 1. Что это за проект

`Synthesis` — дипломный Unity-проект в жанре FPS (шутер от первого лица) с упором на:
- боевой геймплей (стрельба, оружие, ИИ врагов),
- системный респавн и чекпоинты,
- UI/HUD и экранные эффекты,
- USP-механику «циклическая реальность» (смена варианта локации после смерти + энтропия),
- короткий нарративный цикл между двумя локациями.

Технический стек проекта:
- Unity (URP),
- C#,
- Input System,
- Cinemachine,
- NavMesh / AI Navigation,
- Animation Rigging,
- TextMeshPro,
- стандартные UI-системы Unity.

---

## 2. Целевой результат (проект + записка)

Основная цель: работоспособный вертикальный срез FPS-игры с демонстрацией всех ключевых механик и подтверждённой стабильностью для защиты диплома.

Цели записки:
- зафиксировать архитектуру и логику системы,
- описать UI/UX,
- привести таблицы тестирования,
- синхронизировать текст с реальной реализацией в коде.

---

## 3. Текущий статус записки (по структуре docx)

В рабочем документе есть главы:
- Введение
- Глава 1: обзор жанра/аналогов
- Глава 2: системное проектирование
- Глава 3: функциональное проектирование
- Глава 4: UI/UX
- Глава 5: тестирование и анализ
- Глава 6: технико-экономическое обоснование
- Заключение

Фактический акцент последних итераций:
- расширение главы 3.2 (техническая часть с классами/методами),
- усиление главы 4 (подробный UI/UX-текст + список рисунков),
- подготовка главы 5 с табличным ручным тестированием в стиле примера.

---

## 4. Каркас проекта и ключевые папки скриптов

Главные директории:
- `Assets/Scripts/Player` — движение, здоровье, выносливость, прицеливание, persistent-логика.
- `Assets/Scripts/Weapon` — оружие, стрельба, recoil/spread, VFX, подбор оружия.
- `Assets/Scripts/Enemy` — ИИ наземных врагов и дронов, health/damage interface, state machine.
- `Assets/Scripts/UI` — HUD, пауза, экран смерти, переходные экраны, квестовый UI.
- `Assets/Scripts/Interactions` — аптечки, патроны, ключ-карта, чекпоинт, лифт/двери.
- `Assets/Scripts/Triggers` — смена сцен, DDOL-маркеры, триггеры переходов.
- `Assets/Scripts/USP` — энтропия, циклы смерти, переключение вариантов реальности.
- `Assets/Scripts/Core` — утилиты поиска компонентов.

---

## 5. Основные игровые подсистемы (что реализовано)

### 5.1 Игрок и управление

Ключевые классы:
- `PlayerMovement.cs`
- `PlayerLook.cs`
- `PlayerCrouch.cs`
- `PlayerStamina.cs` (и legacy UI-ветка `PlayerStaminaUI.cs`)
- `GameInput.cs`

Сделано:
- Source-lite движение (ground/air acceleration + friction),
- coyote/jump buffer,
- спринт с расходом выносливости и восстановлением,
- приседание,
- reset состояния после респавна.

### 5.2 Боевая система и оружие

Ключевые классы:
- `BaseWeapon.cs`
- `WeaponManager.cs`
- `WeaponStats.cs`
- `AssaultRifle.cs`, `Pistol.cs`
- `SwayNBobScript.cs`
- `TracerVFX.cs`, `MuzzleFlashOneShot.cs`

Сделано:
- стрельба Raycast-логикой,
- ограниченный магазин и резерв патронов,
- reload/swap,
- spread/bloom/recoil,
- muzzle flash, tracer, impact/decal,
- HUD-обновление боезапаса,
- ADS-поза и FOV-логика через `AimController`.

### 5.3 Здоровье, смерть, респавн

Ключевые классы:
- `PlayerHealth.cs`
- `DeathScreen.cs`
- `RespawnCheckpointState.cs`
- `SceneEntrySpawnState.cs`
- `PlayerPersistent.cs`

Сделано:
- отказ от «псевдо-смерти» через кривой `timeScale=0`-подход в пользу управляемого death flow,
- экран смерти с рабочими кнопками,
- респавн от чекпоинта,
- восстановление нужных состояний игрока/оружия/HUD,
- hard respawn hooks + cross-scene сохранение ключевых систем.

### 5.4 UI/HUD

Ключевые классы:
- `PlayerVitalsHud.cs`
- `NeuroresistUI.cs`
- `AmmoHudPulse.cs`
- `BloodSplatUI.cs`
- `DamageVignetteUI.cs`
- `PauseManager.cs`
- `MenuButtonHover.cs`
- `CycleTransitionScreen.cs`
- `QuestUI.cs`, `QuestSystem.cs`, `QuestItem.cs`

Сделано:
- рабочий боевой HUD (HP/Stamina/Neuro/Ammo),
- blood + vignette feedback,
- меню паузы,
- экран смерти,
- сюжетные чёрные экраны с печатающимся текстом,
- hover-анимации кнопок меню,
- частичная «helmet/visor» стилизация.

### 5.5 Враги и ИИ

Ключевые классы:
- `EnemyStateMachine.cs` (`IAIState`, `ChangeState`, `Tick`)
- `GroundShooterAI.cs`
- `DroneShooterAI.cs`
- `IDamageable.cs`

Сделано:
- state-based поведение врагов (idle/patrol/attack/search),
- FOV/LOS проверки,
- патрулирование наземных врагов через NavMesh,
- дроны с отдельной логикой полёта и атаки,
- реакция на попадание,
- смерть врага и корректный переход в пост-смертное состояние,
- вражеские tracer/muzzle эффекты.

### 5.6 USP: циклическая реальность

Ключевые классы:
- `DeathCycleManager.cs`
- `FractureLoopConfig.cs`
- `RealityVariantController.cs`

Сделано:
- счётчик циклов смерти,
- энтропия и tier-уровни,
- переключение варианта локации после смерти,
- анти-стак логика recovery (в рамках настроек конфига),
- привязка варианта к checkpoint/nav-refresh.

### 5.7 Переходы между сценами

Ключевые классы:
- `SceneLoader.cs`
- `StartGameDoorTrigger.cs`
- `PlayerPersistent.cs`
- `DontDestroyObj.cs`, `DontDestroyMarker.cs`

Сделано:
- переходы между уровнями с установкой точки входа,
- сохранение состояния ключевых объектов,
- синхронизация респавна/чекпоинтов при переходе,
- финал первой локации через лифт + переходный нарративный экран.

### 5.8 Интеракции и предметы

Ключевые классы:
- `AmmoPickup.cs`
- `MedkitPickup.cs`
- `KeycardPickup.cs`
- `CheckpointTrigger.cs`
- `WeaponPickup.cs`
- `ElevatorDoorController.cs`
- `ElevatorEndSequence.cs`

Сделано:
- автоподбор аптечек/патронов,
- подбор оружия,
- квестовый pickup (ключ-карта),
- чекпоинты,
- логика двери лифта + конечная последовательность этапа.

---

## 6. История крупных этапов (хронология, сжатая)

1. Сборка базовой FPS-механики: движение, стрельба, смена оружия, базовый UI.  
2. Переработка боевой части: spread/bloom/recoil, ограниченные патроны, pickups.  
3. Доработка эффектов: decals/tracers/muzzle, экранный урон (blood/vignette).  
4. Критическая переработка death flow: стабильный экран смерти, restart, блокировка конфликтных состояний.  
5. Введение чекпоинтов и устойчивого респавна.  
6. Интеграция и стабилизация DDOL-пайплайна между сценами.  
7. Реализация USP: entropy/cycles + switch вариантов локаций.  
8. Переработка ИИ на state-machine модель для ground/drone.  
9. Финализация переходных экранов (typewriter narrative).  
10. Добавление лифта и завершения этапа первой локации.  
11. Анимационный pass: кодовый swap-аним, bob/sway, частичный player animation pipeline.  
12. Полировка UI и подготовка материалов для записки.

---

## 7. Важные проектные решения

1. Приоритет «stability-first»: перед защитой избегать рискованных переписей ядра.  
2. Модульность и раздельные зоны ответственности по папкам/системам.  
3. Единая входная система (`GameInput`), отказ от случайных дублирующих путей ввода.  
4. Устойчивый lifecycle после scene load: повторная привязка ссылок в UI/health/ammo там, где нужно.  
5. В смерти/респавне важнее предсказуемость, чем визуальные «хитрости».  
6. USP-смена мира после смерти — системная механика, а не разовый scripted-ивент.

---

## 8. Текущие задачи и остаточный backlog

1. Финальная полировка ИИ (поведение + анимационные связки в combat/death).  
2. Дофикс некоторых recoil-сценариев (особенно у автомата без ADS, ось Z/roll).  
3. Полировка прицеливания/визора и единый стиль HUD.  
4. Балансировка врагов и ресурсов под целевой difficulty профиль.  
5. Проверка второй локации на завершённость маршрута/квестовой логики.  
6. Финальный регрессионный тест по сценарию демонстрации 4–10 минут.  
7. Завершение главы 5 в записке (таблицы в нужном формате и стилистике).

---

## 9. Рекомендованный «режим работы» для следующего этапа

1. Вносить изменения только по 1 блоку за итерацию (UI, AI, оружие, переходы — отдельно).  
2. После каждого блока делать короткий smoke-test:
   - запуск сцены,
   - стрельба,
   - смерть,
   - респавн,
   - переход сцены.
3. Не смешивать одновременно архитектурный рефакторинг и контентные правки.
4. Для записки использовать только утверждённые формулировки, синхронизированные с реальным кодом.

---

## 10. Критичные ограничения и запреты (для будущих сессий)

1. Не делать «всё сразу» большими пакетами изменений без промежуточной проверки.  
2. Не ломать цепочку persistence (`Player + Cameras + Canvas + GameSystems`) при смене сцен.  
3. Не возвращаться к старой нестабильной смерти через непрозрачные костыли с `timeScale` как единственным механизмом.  
4. Не удалять рабочие системы, пока нет проверенного replacement-пути.  
5. Не использовать агрессивные массовые правки перед финальной демонстрацией без backup.

---

## 11. Что уже подготовлено для записки

1. Сформированы главы 3/4/5 на уровне черновика с техническим наполнением.  
2. Подготовлены наборы таблиц тестирования (по блокам: интерфейс, движение, оружие, ИИ, переходы/USP).  
3. Подготовлены рекомендации по рисункам и схемам (в т.ч. схема UI-flow для draw.io).  
4. Есть отдельные файлы контекста/плана/истории/баланса (см. ниже).

---

## 12. Список ключевых файлов для быстрого старта в новой сессии

### Проектные документы:
- `context.md`
- `plan.md`
- `plan_1604.md`
- `project_worklog.md`
- `balance_table.md`
- `Assets/Configs/USP/FractureLoop_DesignFreeze.md`

### Критичные скрипты:
- `Assets/Scripts/Player/Systems/PlayerMovement.cs`
- `Assets/Scripts/Player/Systems/AimController.cs`
- `Assets/Scripts/Player/Systems/PlayerHealth.cs`
- `Assets/Scripts/Player/Systems/PlayerPersistent.cs`
- `Assets/Scripts/Weapon/Weapons/BaseWeapon.cs`
- `Assets/Scripts/Weapon/Weapons/WeaponManager.cs`
- `Assets/Scripts/Enemy/AI/EnemyStateMachine.cs`
- `Assets/Scripts/Enemy/AI/GroundShooterAI.cs`
- `Assets/Scripts/Enemy/AI/DroneShooterAI.cs`
- `Assets/Scripts/UI/PlayerVitalsHud.cs`
- `Assets/Scripts/UI/DeathScreen.cs`
- `Assets/Scripts/USP/DeathCycleManager.cs`
- `Assets/Scripts/USP/RealityVariantController.cs`
- `Assets/Scripts/Triggers/SceneLoader.cs`

---

## 13. Шаблон промпта для переноса в ChatGPT (браузер)

Ниже текст, который можно вставить в новый чат:

> Я веду дипломный Unity-проект FPS `Synthesis`.  
> Прошу работать по принципу stability-first: менять только то, что я прошу, небольшими шагами.  
> Контекст: есть движение Source-lite, оружие (spread/recoil/reload/swap), HUD, death/respawn/checkpoints, переходы между сценами, USP (entropy + смена варианта локации после смерти), AI ground/drone на state machine.  
> Нужна помощь по двум трекам:  
> 1) Полировка прототипа и точечные фиксы.  
> 2) Написание пояснительной записки (главы 3/4/5, таблицы тестирования, рисунки).  
> В ответах используй русский, подробный технический стиль, без «воды».  
> Перед предложением изменений сверяйся с существующими классами и методами проекта.

---

## 14. Приложение: включённые рабочие .md файлы

Ниже в этом файле добавлены/дублируются ключевые markdown-файлы проекта для полного контекста:
- `context.md`
- `plan.md`
- `plan_1604.md`
- `project_worklog.md`
- `balance_table.md`
- `Assets/Configs/USP/FractureLoop_DesignFreeze.md`

> Если нужен максимально «сырой» перенос, можно копировать этот файл целиком в новый чат.



---

## 15. Сырые вложения markdown (полные копии)


### RAW: context.md

```md
# Synthesis вЂ” Project Context (as of 2026-04-16)

## 1) Goal and Delivery Target
- Diploma FPS shooter prototype on Unity/C#.
- Near-term target: stable **80% playable build** by **April 20вЂ“22, 2026** (2nd checkpoint).
- Priority mode: **stability-first** (no risky deep rewrites before deadline).

## 2) Core Identity / USP
- USP: **Cyclic Reality**.
- Player death does not just restart the same state:
  - entropy increases,
  - layout/enemy variant changes,
  - run pressure escalates,
  - anti-stuck recovery can temporarily reduce pressure after repeated failures in same zone.

## 3) Implemented Systems (current)
- Player movement: Source-lite ground/air accel, friction, coyote/jump buffer.
- Combat: spread/bloom/recoil, reload, weapon swap, limited reserve ammo, medkits.
- VFX/UI combat feedback: blood splats, vignette pulse, muzzle flash, tracers, decals.
- ADS: per-weapon aim pose, FOV lerp, optional ADS overlay.
- Respawn: death screen + checkpoints + hard respawn integration.
- USP runtime:
  - death cycles + entropy tiers,
  - variant switching (`RealityVariantController`),
  - enemy entropy scaling hooks.
- Enemy AI:
  - ground shooter (state machine, cover/peek behavior),
  - drone shooter (state machine, patrol/chase/attack/search).

## 4) Scene and Runtime Architecture Notes
- Active production scene: `Assets/Scenes/level0.unity`.
- Persistence uses `DontDestroyObj` + `PlayerPersistent`.
- Camera stack is split for world/viewmodel/UI.
- UI stack includes `VitalsRoot`, `BloodRoot`, `DamageVignette`, `ADSOverlay`, death panel.

## 5) Known Risks / Technical Debt
- Legacy scripts coexist with newer systems (risk of accidental double logic):
  - old enemy stack (`EnemyAI`, `EnemyCombat`, etc.),
  - `PlayerStaminaUI` legacy path vs `PlayerVitalsHud`.
- Some systems still use fallback object lookup patterns.
- Scene wiring can be fragile when references are missing after scene transitions.
- CLI build check is unavailable in this shell environment (`dotnet` missing); verification is Unity PlayMode.

## 6) Runtime Rules to Keep Stable
- Keep one authoritative gameplay state per domain:
  - one death flow,
  - one stamina/HUD flow,
  - one active enemy AI implementation per prefab family.
- Preserve cross-scene state for required objects:
  - player, core cameras, canvas, game systems.
- Avoid changing data model shape close to deadline unless needed for blocker fixes.

## 7) Diploma Writing Mapping (Section 3/4/5)
- Section 3 (functional design):
  - input pipeline, movement model, weapon pipeline, AI state flow, death cycle flow.
- Section 4 (UI/UX):
  - HUD readability, ADS overlay behavior, damage feedback, death/pause menus.
- Section 5 (testing):
  - scenario-driven checks: combat loop, death cycle loop, transitions, regressions, perf smoke.

```


### RAW: plan.md

```md
# Synthesis вЂ” Master Plan (Updated 2026-04-17)

## Summary
- Strategy: stability-first + demo-first.
- Goal: by April 20вЂ“22 have a playable, stable vertical slice with clear USP and coherent short narrative loop.
- Focus: blocker fixes, UX polish, measurable performance gains, and 5+ minute gameplay flow across first 2 locations.

## Delivery Priorities
1. Stable core loop: combat -> death -> respawn/checkpoint -> continue.
2. USP visibility: death cycles change layout/enemies and affect difficulty.
3. Clear short story context: intro text, mission objective flow, level transition with narrative closure.
4. No critical regressions in demo run (10вЂ“12 minutes).

## Day-by-Day Execution

## 2026-04-17 (Stabilization B + UI safety)
- Finalize input/UI/runtime binding stability.
- Confirm death/pause/restart/weapon HUD works after repeated scene reloads.
- Validate neuroresist + xray + post-process behavior on all enemy prefabs.
- DoD:
  - no null-ref in 10-minute run,
  - no stuck ADS/death menu,
  - no persistent xray material leak.

## 2026-04-18 (USP Reliability + AI Balance)
- Ensure entropy tiers affect modern AIs (ground + drone) in observable way.
- Validate anti-stuck recovery window after repeated same-zone deaths.
- Verify variant + enemy set switching + navmesh switching remains consistent.
- DoD:
  - 3+ deaths produce clear variant/tier progression,
  - no AI navigation break after variant change.

## 2026-04-19 (Narrative + Flow Lock)
- Add short narrative wrapper for first 2 levels (see "Narrative Gameplay Plan").
- Implement black-screen typing intro before gameplay.
- Implement black-screen typing outro when entering level-2 elevator.
- Connect mission objectives with existing quest/trigger systems.
- DoD:
  - full 5+ minute guided flow exists,
  - player always knows next objective.

## 2026-04-20 (Bug Bash + Performance + Freeze)
- Run full regression pass:
  - combat, AI, death, checkpoints, scene transitions, quest flow, USP cycles.
- Performance pass:
  - light/shadow budget, occlusion bake, camera masks, enemy tick sanity.
- Fix only blocker/high.
- Content freeze.
- DoD:
  - 10вЂ“12 minute uninterrupted demo,
  - no blocker in default route.

## 2026-04-21..22 (Buffer + Diploma Text)
- Capture screenshots and test evidence.
- Fill test tables and implementation notes for sections 3/4/5.
- Ensure thesis wording matches actual implemented behavior.

## 2026-04-19..20 (Added Backlog вЂ” Final Gameplay/Balancing Pass)
- Add world models for pickups:
  - medkit world model prefab,
  - ammo world model prefabs (pistol/rifle),
  - visual readability pass (silhouette + emissive + interaction distance).
- Place pickups with balance intent (not random scatter):
  - low-risk areas: light sustain,
  - high-risk branches: reward pickups,
  - no infinite sustain routes.
- Deep enemy balance pass with measurable table:
  - GroundShooter and Drone TTK/DPS tuning,
  - strafe/approach pressure normalization for drone,
  - define acceptable kill windows for average player.
- Add 3 selectable difficulty configs at game start:
  - Easy / Normal / Hard,
  - each profile controls enemy damage/fire interval/accuracy + pickup density multipliers.
- Keep Level 1 (generator + elevator objective chain) as the final implementation block after balance lock.
- Prepare final 4вЂ“5+ minute gameplay capture route:
  - intro -> level 0 objective -> transition -> level 1 objective step -> outro.

## Narrative Gameplay Plan (First 2 Locations, 5+ Minutes)

## Level 0 вЂ” Lab Entry (Objective: obtain keycard and reach transition door)
- Purpose: onboarding + world setup + first combat pressure.
- Target duration: 2вЂ“3 minutes.

### Flow
1. Intro black screen with typing text (10вЂ“15 seconds):
   - "ARK Lab breach detected."
   - "Neuroresist protocol unstable."
   - "Primary objective: retrieve access keycard."
2. Player regains control in start room.
3. Soft tutorial prompts:
   - move/look,
   - fire/reload,
   - medkit/ammo pickup hints.
4. First encounter wave (light pressure).
5. Objective marker guides to keycard room.
6. Player picks keycard -> quest updates:
   - "Access granted. Reach transfer corridor."
7. Small second encounter near route out.
8. Door trigger loads Level 1.

### Required implementation notes
- Reuse existing `QuestSystem/QuestUI` hints.
- Intro text can be a dedicated Canvas panel with typewriter effect.
- Keep objective chain linear and explicit.

## Level 1 вЂ” Power Restore (Objective: restore generator and escape via elevator)
- Purpose: stronger tension + short objective chain + narrative cliffhanger.
- Target duration: 3вЂ“4 minutes.

### Flow
1. Arrival text prompt:
   - "Sector power offline."
   - "Restore generator to unlock elevator."
2. Player reaches locked elevator area (cannot use yet).
3. Objective updates to generator route:
   - find generator room,
   - activate switch/terminal.
4. Combat in generator wing (medium pressure, cover usage).
5. Generator interaction (hold/use action) + feedback:
   - lights partially return,
   - elevator power online.
6. Return path has final short encounter.
7. Player enters elevator trigger.
8. Outro black screen with typing text:
   - "Power restored."
   - "Signal source detected below."
   - "Cycle integrity: unstable."

### Anti-abuse/balance notes
- Do not reduce enemy count per death directly.
- Difficulty adaptation comes from entropy/recovery rules, not from trivial enemy removal.
- Keep resource pickups finite per run-state where possible.

## Performance Plan (Targeted)
1. Bake occlusion for level geometry (`m_OcclusionCullingData` must be non-zero).
2. Keep only one active listener at runtime.
3. Restrict additional light shadows:
   - lower per-object shadow load,
   - disable shadows on secondary lights.
4. Confirm camera layer split:
   - main world,
   - weapon-only,
   - UI-only.
5. Keep only active variant/enemy set enabled.

## Manual Acceptance Checklist
1. 5 death cycles: panel/restart/checkpoint/variant works.
2. 2 scene transitions: no lost camera/UI/player state.
3. 10-min combat soak: no soft-lock, no HUD desync.
4. Neuroresist: distant enemies visible through walls while active, correctly restored afterward.
5. Narrative flow:
   - intro text appears before level-0 control,
   - level-1 generator objective gates elevator,
   - elevator outro text appears at finish.
6. Balance/progression:
   - player can clear default route on Normal without perfect aim,
   - drone no longer deletes player in ~3вЂ“4 seconds unless player makes repeated high-risk errors,
   - medkit/ammo placement supports intended pacing, not abuse.
7. Difficulty presets:
   - Easy/Normal/Hard selectable in menu,
   - each preset produces clearly different pressure level.

## Bug Priority Policy
- Blocker: breaks progression/demo.
- High: severe repeated malfunction in combat/UI/respawn/quest flow.
- Medium: polish-only or low-frequency issue.
- Freeze window rule: fix Blocker/High only.

## 2026-04-20..21 (Added Backlog вЂ” HL-inspired Feel Pass)
- Player movement + camera feel pass (priority high):
  - tighten acceleration/deceleration feel,
  - reduce unwanted sliding feeling,
  - improve look responsiveness and consistency under combat stress.
- Weapon handling baseline animations (must-have):
  - weapon swap animation (hide down -> pull from below),
  - reload animation baseline (at least for primary weapon),
  - keep implementation lightweight and stable for demo.
- Enemy encounter layout pass (pragmatic design over complex AI):
  - use small patrol zones for enemies,
  - place enemies so player naturally collides with patrol routes,
  - rely on patrol + attack states as core behavior for release version.
- Enemy animation integration pass:
  - ensure patrol/combat/death clips are wired and visually coherent,
  - avoid high-risk AI rewrites before freeze.

## Immediate Finish Window (target: tomorrow / max day-after)
1. Lock movement/camera feel.
2. Lock swap/reload baseline animations.
3. Lock enemy placement and patrol zones in level 0 + variants.
4. Run full 5+ minute route smoke test for capture readiness.

## Status Update вЂ” 2026-04-21
- [DONE] Level 1 elevator flow finalized:
  - elevator door logic with safe close/anti-crush behavior,
  - end-of-level trigger sequence after door is fully closed,
  - player control/audio lock,
  - black narrative screen with typewriter text (prototype finale).

## Status Update вЂ” 2026-04-21 (Animation)
- [IN PROGRESS ~50%] Player animation pass:
  - implemented: code-based weapon swap animation, movement feel improvements, weapon vertical bob.
  - pending: full reload/sprint/advanced hands animation set and final polish pass.

```


### RAW: plan_1604.md

```md
# Synthesis вЂ” Execution Plan (2026-04-16 в†’ 2026-04-22)

## Summary
- Strategy: **stability-first**.
- Objective: demo-safe vertical slice with working combat loop, death-cycle USP, and clean UX.
- Acceptance focus: no blocker regressions, consistent scene transitions, reproducible behavior.

## Day-by-Day Plan

## 2026-04-16 (Stabilization A)
- Remove runtime instability sources:
  - single active gameplay input flow,
  - single active audio listener behavior at runtime,
  - disable legacy stamina UI when modern vitals HUD is present.
- Lock critical references and reduce per-frame lookups in HUD/weapon systems.
- DoD:
  - no multiple-listener warnings,
  - death/pause/HUD stable through 3 restart cycles.

## 2026-04-17 (Stabilization B)
- Input pipeline cleanup:
  - ADS uses InputActions (no direct mouse polling path in controller).
- Move UI binding to deterministic points (`OnEnable`, `sceneLoaded`) where applicable.
- Reduce fallback scans to throttled/one-time recovery in:
  - vitals HUD,
  - player health dependencies,
  - ammo UI binding.
- DoD:
  - 10-minute session without null refs or HUD desync after death/restart/weapon swap.

## 2026-04-18 (USP Reliability + AI Balance)
- Finalize entropy impact:
  - both modern AI types react to tier scaling.
- Add anti-stuck recovery behavior from config:
  - repeated deaths in same zone temporarily soften pressure.
- Ensure variant/navmesh switching remains functional even if explicit nav root is not bound.
- DoD:
  - 3+ consecutive deaths show variant progression + observable difficulty behavior changes.

## 2026-04-19 (UX Polish)
- HUD polish pass:
  - readability, alpha, glow balance, visual stability under camera motion.
- ADS polish:
  - overlay timing alignment with weapon pose/fov movement.
- Damage feedback polish:
  - blood/vignette rhythm, no abrupt transitions.
- DoD:
  - HUD perceived as in-world visor-like overlay, not static вЂњstickerвЂќ.

## 2026-04-20 (Bug Bash + Freeze)
- Full regression pass:
  - combat, death/respawn, checkpoints, scene transitions, quests, neuroresist, reload/swap.
- Fix only blocker/high issues.
- Enter feature/content freeze.
- DoD:
  - 10вЂ“12 min uninterrupted demo run.

## 2026-04-21 to 2026-04-22 (Buffer + Diploma Text)
- Collect final screenshots for sections 3/4/5.
- Produce test tables (cases/results/known limits).
- Sync wording in explanatory note with actual implemented behavior.

## Manual Test Checklist (required)
1. 5 death cycles in a row:
   - death panel appears,
   - restart works,
   - respawn uses checkpoint,
   - variant switch applies.
2. 2 scene transitions:
   - no loss of player/camera/ui state,
   - no duplicate persistent gameplay flow.
3. Combat soak test (10 min):
   - ammo/hp/stamina values remain consistent,
   - no soft-lock in reload/swap/aim/fire.
4. AI behavior:
   - no stuck loops near player,
   - sensible search grace after LOS loss,
   - entropy/recovery effects observable.
5. UX:
   - ADS overlay timing correct,
   - HUD remains above world,
   - blood/vignette transitions smooth.

## Bug Priority Policy
- **Blocker**: prevents playing core loop or blocks demo progression.
- **High**: severe gameplay inconsistency or repeatable user-facing malfunction.
- **Medium**: polish issue or occasional non-blocking inconsistency.
- During freeze window: fix Blocker/High only.

```


### RAW: project_worklog.md

```md
# Synthesis вЂ” Project Worklog (for diploma write-up)

## Purpose
- Consolidated history of implemented systems and major decisions.
- Intended to speed up section 3/4/5 writing in explanatory note.

## High-Level Timeline

## Phase A вЂ” Core FPS foundation
- Player movement and look pipeline.
- Weapon handling baseline (shoot/reload/swap).
- Enemy damage interface and combat loop foundation.

## Phase B вЂ” Combat depth
- Dynamic spread/bloom per movement state and ADS.
- Recoil and camera/weapon handling improvements.
- Bullet impact stack: decals, tracer, muzzle flash.
- Limited ammo economy introduced:
  - reserve ammo,
  - ammo pickups by weapon type,
  - HUD sync with current/reserve values.

## Phase C вЂ” Survival/feedback systems
- Health system reworked to non-regenerative flow.
- Medkit auto-pickup treatment integrated.
- Damage feedback upgraded:
  - blood splats on UI,
  - damage vignette with smoother in/out behavior.
- Death panel/restart flow stabilized (no timeScale death lock dependency).

## Phase D вЂ” Respawn and persistence stability
- Checkpoint-based respawn integrated.
- Hard respawn reset hooks added for health, stamina, weapon state.
- Cross-scene persistence stabilized (`DontDestroy` chain).
- Duplicate runtime systems reduced:
  - listener cleanup,
  - legacy stamina UI suppression when modern HUD present.

## Phase E вЂ” USP implementation (Cyclic Reality)
- Death cycle manager with:
  - entropy,
  - tier computation,
  - cycle count snapshot persistence.
- Anti-stuck recovery window implemented for repeated same-zone deaths.
- Variant progression connected to death cycle.
- Transition changed from in-scene variant toggling toward variant-scene routing.
- Black transition screen with narrative text + typewriter loading flow added for cycle transitions.

## Phase F вЂ” Enemy AI architecture upgrade
- Introduced state machine driven AI for:
  - GroundShooter (idle/patrol/attack/search/cover-peek),
  - DroneShooter (idle/patrol/attack/search).
- Entropy tier scaling connected to AI attack cadence and damage.
- NavMesh safety guards added (no invalid SetDestination/Resume spam).
- Additional fixes:
  - death lock behavior,
  - combat script interference prevention,
  - movement-facing improvements to reduce sideways sliding.

## Phase G вЂ” UI/HUD modernization
- New vitals HUD architecture with:
  - health/stamina/neuro bars,
  - ammo split display and pulse feedback,
  - visor-style motion layer.
- Start menu style direction defined (minimal sci-fi).
- Custom hover button script (`MenuButtonHover`) added for polished menu interactions.

## Phase H вЂ” Performance and rendering
- Camera stack separation validated (world / weapon / UI).
- Additional light shadow budget tuning started.
- Occlusion culling limitations identified for dynamic variant layout.
- Migration strategy selected:
  - split `level0` into per-variant scenes for stable per-layout occlusion bake.

## Current Implemented Key Systems (snapshot)
- Stable death -> restart -> checkpoint loop.
- Persistent entropy/cycle state across scene switches.
- Variant-scene routing foundations in place.
- Neuroresist visual + xray material restore fixes.
- Modern enemy AI active and integrated with cycle difficulty.

## Current Known Open Work (as of 2026-04-19)
- Final pickup visuals and deliberate placement pass.
- Full combat balance table (DPS/TTK by enemy and difficulty).
- Difficulty preset selection (Easy/Normal/Hard) in menu and runtime profile application.
- Level 1 objective chain finalization:
  - generator restore,
  - elevator unlock,
  - outro text.
- Final 4вЂ“5+ minute gameplay capture route.

## Suggested Tables for Diploma Section 5
- Test matrix:
  - death/respawn cycles,
  - scene transition persistence,
  - AI behavior states,
  - neuroresist visibility correctness,
  - HUD synchronization.
- Balance matrix:
  - enemy damage,
  - fire interval,
  - expected TTK on player (Easy/Normal/Hard),
  - pickup density per zone.
- Performance matrix:
  - FPS/Batches in stress viewpoints,
  - lighting/shadow config deltas,
  - effect of occlusion per variant scene.


## Daily Update вЂ” 2026-04-20

### Completed Today
- Pickup visuals pass:
  - Medkit and Ammo world-model implementation completed in scene workflow.
- Pickup scripts cleanup:
  - Removed highlight dependency from `AmmoPickup` and `MedkitPickup`.
  - Left only core pickup logic (ammo add / healing) with safe one-time consume guards.
- Intro narrative polish:
  - Rewrote StartGame intro text with longer story context.
  - Fixed transition persistence bug where text stayed on screen after level load.
  - Root cause: transition coroutine ran on scene-bound object and was interrupted on load.
  - Fix: transition execution moved to persistent `CycleTransitionScreen` owner flow.
- Enemy balance foundation (Normal/Medium baseline):
  - Analyzed current enemy prefabs and actual level distribution (`~5-6 robots + 2 drones`).
  - Created dedicated config: `Assets/Configs/Difficulty/EnemyDifficulty_Medium.json`.
  - Applied Medium defaults to scripts:
    - `GroundShooterAI`
    - `DroneShooterAI`
  - Applied same Medium values to base enemy prefabs:
    - `EnemyRobot.prefab`
    - `EnemyPatrol.prefab`

### Design Decision Logged
- Near-release enemy design will prioritize encounter design over high-complexity AI:
  - small patrol zones,
  - intentional enemy placement,
  - reliable patrol + attack behavior,
  - no risky AI rewrites before freeze.

### Next Focus (tomorrow / max day-after)
1. Movement + camera feel pass (HL-inspired responsiveness).
2. Baseline weapon animations:
   - swap (down/out + draw from below),
   - reload (at least primary weapon complete).
3. Enemy placement + patrol-zone pass for level flow quality.
4. Full 5+ minute route validation for capture and diploma demo.

### 2026-04-21 вЂ” Player Animation Midpoint
- Player animation pipeline reached ~50% completion:
  - code swap animation is active,
  - movement/camera/weapon bob feel improved,
  - advanced reload animation and full final hands polish deferred for later pass.

```


### RAW: balance_table.md

```md
# Synthesis вЂ” Balance Table Template

## How to use
- Fill values after each playtest pass (do not leave "memory estimates").
- Keep one row per enemy/weapon/pickup profile.
- Recalculate TTK after any change to damage, fire rate, HP, armor, or spread.

## Global Assumptions
| Parameter | Value | Notes |
|---|---:|---|
| Player Max HP | 150 | Current no-regeneration model |
| Medkit Heal | 50 | Auto pickup |
| Default Difficulty | Normal | Baseline for comparisons |
| Test Build |  | Commit/build tag |
| Test Date |  | YYYY-MM-DD |

---

## Enemy Combat Balance (Per Difficulty)
| Enemy Type | Difficulty | HP | Damage/Hit | Fire Interval (s) | RPM (calc) | Hit Chance % (practical) | DPS (practical) | Avg Time To Kill Player (s) | Notes |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| GroundShooter | Easy |  |  |  |  |  |  |  |  |
| GroundShooter | Normal |  |  |  |  |  |  |  |  |
| GroundShooter | Hard |  |  |  |  |  |  |  |  |
| DroneShooter | Easy |  |  |  |  |  |  |  |  |
| DroneShooter | Normal |  |  |  |  |  |  |  |  |
| DroneShooter | Hard |  |  |  |  |  |  |  |  |

### Formulas
- `RPM = 60 / FireInterval`
- `Practical DPS = DamagePerHit * (1/FireInterval) * HitChance`
- `Player TTK = PlayerHP / PracticalDPS`

---

## Player Weapon Balance
| Weapon | Damage/Shot | Fire Interval (s) | RPM | Magazine | Reserve Max | Reload (s) | Hip Spread | ADS Spread | Practical DPS | Enemy TTK vs Ground (s) | Enemy TTK vs Drone (s) | Notes |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| Pistol |  |  |  |  |  |  |  |  |  |  |  |  |
| Rifle |  |  |  |  |  |  |  |  |  |  |  |  |

---

## Pickup Economy (Per Level Route)
| Scene | Route Segment | Medkits Count | Pistol Ammo Pickups | Rifle Ammo Pickups | Expected Damage Taken | Sustain Margin (HP+Ammo) | Notes |
|---|---|---:|---:|---:|---:|---:|---|
| level0_var0 | Start -> Keycard |  |  |  |  |  |  |
| level0_var0 | Keycard -> Exit |  |  |  |  |  |  |
| level0_var1 | Start -> Keycard |  |  |  |  |  |  |
| level0_var1 | Keycard -> Exit |  |  |  |  |  |  |
| level0_var2 | Start -> Keycard |  |  |  |  |  |  |
| level0_var2 | Keycard -> Exit |  |  |  |  |  |  |
| level1 | Entry -> Generator |  |  |  |  |  |  |
| level1 | Generator -> Elevator |  |  |  |  |  |  |

---

## Entropy Tier Impact Validation
| Tier | Ground Damage Mult | Ground FireRate Mult | Drone Damage Mult | Drone FireRate Mult | Observed Difficulty Delta | Pass/Fail | Notes |
|---|---:|---:|---:|---:|---|---|---|
| T1 | 1.00 | 1.00 | 1.00 | 1.00 |  |  |  |
| T2 |  |  |  |  |  |  |  |
| T3 |  |  |  |  |  |  |  |
| T4 |  |  |  |  |  |  |  |

---

## Difficulty Preset Matrix
| Setting | Easy | Normal | Hard | Notes |
|---|---:|---:|---:|---|
| Ground Damage Mult |  | 1.00 |  |  |
| Ground Fire Interval Mult |  | 1.00 |  | lower = faster fire |
| Drone Damage Mult |  | 1.00 |  |  |
| Drone Fire Interval Mult |  | 1.00 |  |  |
| Enemy Accuracy Mult |  | 1.00 |  |  |
| Medkit Spawn Mult |  | 1.00 |  |  |
| Ammo Spawn Mult |  | 1.00 |  |  |
| Intended Player Death Rate |  |  |  | per 10 min |

---

## Test Session Log
| Session ID | Date | Difficulty | Scene Path | Deaths | Avg Encounter Time | Avg Player HP End | Ammo Left End | Main Pain Point | Action Item |
|---|---|---|---|---:|---:|---:|---:|---|---|
|  |  |  |  |  |  |  |  |  |  |

---

## Acceptance Targets (Suggested)
| Metric | Easy | Normal | Hard |
|---|---:|---:|---:|
| Avg deaths per 10 min (new player) | <=2 | 2вЂ“4 | 4вЂ“7 |
| Avg player HP after medium encounter | 60%+ | 35вЂ“60% | 15вЂ“40% |
| Drone unfair burst complaints | Low | Medium | Medium |
| Soft-lock / no-resource runs | 0 | 0 | 0 |

---

## Change History
| Date | Changed By | What Changed | Why | Result |
|---|---|---|---|---|
|  |  |  |  |  |


```


### RAW: FractureLoop_DesignFreeze.md

```md
# Fracture Loop - Design Freeze (Step 1)

Date: 2026-04-09
Status: Locked for implementation steps 2-6

## USP Statement
Death does not only restart combat. It shifts the player into the next unstable reality variant of the same level, changing routes, cover, object layout, enemy encounters, and pressure profile.

## Core Rules
1. Reality variants rotate in strict order: A -> B -> C -> A.
2. Every death increases Entropy.
3. Entropy controls pressure tier (not just enemy count).
4. Recovery activates only for real stuck scenarios.
5. Death must never be the optimal farming strategy.

## Entropy Rules (Default)
- Entropy cap: 100
- Death penalty: +20
- Repeat death in same zone: +25
- Suicide / self-harm death: +30
- Major objective progress reward: -10

## Difficulty Tiers (By Entropy)
- Tier 1: 0-24
- Tier 2: 25-49
- Tier 3: 50-74
- Tier 4: 75-100

## Recovery Rules (Anti-Stuck)
Recovery may trigger if player dies 2+ times in same encounter zone inside 240 seconds.
When active, it grants a short stabilization window (12s) with mild damage reduction (0.85 multiplier).
Recovery does not reduce Entropy and does not reset cycle index.

## Anti-Abuse Rules
1. Suicide grants larger entropy increase than normal death.
2. Entropy always increases on death (except special scripted exemptions).
3. Respawn carries only part of resources (default ammo carry 75%).
4. One-time emergency caches cannot be farmed infinitely within one run.

## Run Collapse (Optional)
If enabled and Entropy reaches 95+, run enters collapse state (full run reset behavior configured later).

## Implementation Boundary For Step 1
- Added config schema only.
- No gameplay logic activated yet.
- No scene content switching yet.
- No enemy rebalance yet.

```
