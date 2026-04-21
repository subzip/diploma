# Synthesis — Master Plan (Updated 2026-04-17)

## Summary
- Strategy: stability-first + demo-first.
- Goal: by April 20–22 have a playable, stable vertical slice with clear USP and coherent short narrative loop.
- Focus: blocker fixes, UX polish, measurable performance gains, and 5+ minute gameplay flow across first 2 locations.

## Delivery Priorities
1. Stable core loop: combat -> death -> respawn/checkpoint -> continue.
2. USP visibility: death cycles change layout/enemies and affect difficulty.
3. Clear short story context: intro text, mission objective flow, level transition with narrative closure.
4. No critical regressions in demo run (10–12 minutes).

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
  - 10–12 minute uninterrupted demo,
  - no blocker in default route.

## 2026-04-21..22 (Buffer + Diploma Text)
- Capture screenshots and test evidence.
- Fill test tables and implementation notes for sections 3/4/5.
- Ensure thesis wording matches actual implemented behavior.

## 2026-04-19..20 (Added Backlog — Final Gameplay/Balancing Pass)
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
- Prepare final 4–5+ minute gameplay capture route:
  - intro -> level 0 objective -> transition -> level 1 objective step -> outro.

## Narrative Gameplay Plan (First 2 Locations, 5+ Minutes)

## Level 0 — Lab Entry (Objective: obtain keycard and reach transition door)
- Purpose: onboarding + world setup + first combat pressure.
- Target duration: 2–3 minutes.

### Flow
1. Intro black screen with typing text (10–15 seconds):
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

## Level 1 — Power Restore (Objective: restore generator and escape via elevator)
- Purpose: stronger tension + short objective chain + narrative cliffhanger.
- Target duration: 3–4 minutes.

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
   - drone no longer deletes player in ~3–4 seconds unless player makes repeated high-risk errors,
   - medkit/ammo placement supports intended pacing, not abuse.
7. Difficulty presets:
   - Easy/Normal/Hard selectable in menu,
   - each preset produces clearly different pressure level.

## Bug Priority Policy
- Blocker: breaks progression/demo.
- High: severe repeated malfunction in combat/UI/respawn/quest flow.
- Medium: polish-only or low-frequency issue.
- Freeze window rule: fix Blocker/High only.

## 2026-04-20..21 (Added Backlog — HL-inspired Feel Pass)
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

## Status Update — 2026-04-21
- [DONE] Level 1 elevator flow finalized:
  - elevator door logic with safe close/anti-crush behavior,
  - end-of-level trigger sequence after door is fully closed,
  - player control/audio lock,
  - black narrative screen with typewriter text (prototype finale).

## Status Update — 2026-04-21 (Animation)
- [IN PROGRESS ~50%] Player animation pass:
  - implemented: code-based weapon swap animation, movement feel improvements, weapon vertical bob.
  - pending: full reload/sprint/advanced hands animation set and final polish pass.
