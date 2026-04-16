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

## Bug Priority Policy
- Blocker: breaks progression/demo.
- High: severe repeated malfunction in combat/UI/respawn/quest flow.
- Medium: polish-only or low-frequency issue.
- Freeze window rule: fix Blocker/High only.

