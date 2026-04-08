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
