# Synthesis — Balance Table Template

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
| Avg deaths per 10 min (new player) | <=2 | 2–4 | 4–7 |
| Avg player HP after medium encounter | 60%+ | 35–60% | 15–40% |
| Drone unfair burst complaints | Low | Medium | Medium |
| Soft-lock / no-resource runs | 0 | 0 | 0 |

---

## Change History
| Date | Changed By | What Changed | Why | Result |
|---|---|---|---|---|
|  |  |  |  |  |

