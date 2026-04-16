# Synthesis — Project Context (as of 2026-04-16)

## 1) Goal and Delivery Target
- Diploma FPS shooter prototype on Unity/C#.
- Near-term target: stable **80% playable build** by **April 20–22, 2026** (2nd checkpoint).
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
