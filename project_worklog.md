# Synthesis — Project Worklog (for diploma write-up)

## Purpose
- Consolidated history of implemented systems and major decisions.
- Intended to speed up section 3/4/5 writing in explanatory note.

## High-Level Timeline

## Phase A — Core FPS foundation
- Player movement and look pipeline.
- Weapon handling baseline (shoot/reload/swap).
- Enemy damage interface and combat loop foundation.

## Phase B — Combat depth
- Dynamic spread/bloom per movement state and ADS.
- Recoil and camera/weapon handling improvements.
- Bullet impact stack: decals, tracer, muzzle flash.
- Limited ammo economy introduced:
  - reserve ammo,
  - ammo pickups by weapon type,
  - HUD sync with current/reserve values.

## Phase C — Survival/feedback systems
- Health system reworked to non-regenerative flow.
- Medkit auto-pickup treatment integrated.
- Damage feedback upgraded:
  - blood splats on UI,
  - damage vignette with smoother in/out behavior.
- Death panel/restart flow stabilized (no timeScale death lock dependency).

## Phase D — Respawn and persistence stability
- Checkpoint-based respawn integrated.
- Hard respawn reset hooks added for health, stamina, weapon state.
- Cross-scene persistence stabilized (`DontDestroy` chain).
- Duplicate runtime systems reduced:
  - listener cleanup,
  - legacy stamina UI suppression when modern HUD present.

## Phase E — USP implementation (Cyclic Reality)
- Death cycle manager with:
  - entropy,
  - tier computation,
  - cycle count snapshot persistence.
- Anti-stuck recovery window implemented for repeated same-zone deaths.
- Variant progression connected to death cycle.
- Transition changed from in-scene variant toggling toward variant-scene routing.
- Black transition screen with narrative text + typewriter loading flow added for cycle transitions.

## Phase F — Enemy AI architecture upgrade
- Introduced state machine driven AI for:
  - GroundShooter (idle/patrol/attack/search/cover-peek),
  - DroneShooter (idle/patrol/attack/search).
- Entropy tier scaling connected to AI attack cadence and damage.
- NavMesh safety guards added (no invalid SetDestination/Resume spam).
- Additional fixes:
  - death lock behavior,
  - combat script interference prevention,
  - movement-facing improvements to reduce sideways sliding.

## Phase G — UI/HUD modernization
- New vitals HUD architecture with:
  - health/stamina/neuro bars,
  - ammo split display and pulse feedback,
  - visor-style motion layer.
- Start menu style direction defined (minimal sci-fi).
- Custom hover button script (`MenuButtonHover`) added for polished menu interactions.

## Phase H — Performance and rendering
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
- Final 4–5+ minute gameplay capture route.

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


## Daily Update — 2026-04-20

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

### 2026-04-21 — Player Animation Midpoint
- Player animation pipeline reached ~50% completion:
  - code swap animation is active,
  - movement/camera/weapon bob feel improved,
  - advanced reload animation and full final hands polish deferred for later pass.
