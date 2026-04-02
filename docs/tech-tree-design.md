# Cordite Wars: Six Fronts — Tech Tree Design

> **Document status:** First draft — v0.1  
> **Last updated:** 2026-04-01  
> **Purpose:** Complete tech tree specification for all 6 factions. Defines building prerequisites, unit unlock gates, research upgrades, and asymmetric tech paths. Drives data in `data/upgrades/` and influences `data/buildings/`.

---

## Table of Contents

1. [Tech Tree Architecture](#1-tech-tree-architecture)
2. [Universal Tier Rules](#2-universal-tier-rules)
3. [Upgrade Design Philosophy](#3-upgrade-design-philosophy)
4. [Valkyr Tech Tree](#4-valkyr-tech-tree)
5. [Kragmore Tech Tree](#5-kragmore-tech-tree)
6. [Bastion Tech Tree](#6-bastion-tech-tree)
7. [Arcloft Tech Tree](#7-arcloft-tech-tree)
8. [Ironmarch Tech Tree](#8-ironmarch-tech-tree)
9. [Stormrend Tech Tree](#9-stormrend-tech-tree)
10. [Cross-Faction Tier Comparison](#10-cross-faction-tier-comparison)
11. [Design Notes & Balance Rationale](#11-design-notes--balance-rationale)

---

## 1. Tech Tree Architecture

The tech tree in Cordite Wars uses a **building-prerequisite gate** system rather than a linear research queue. Players advance tiers by constructing specific buildings, which then unlock production of new units and the ability to research upgrades. VC (Voltaic Charge) acts as the secondary gate — some buildings and units require VC on top of their Cordite cost.

### Core Tier Structure (All Factions)

```
Tier 0 ──── HQ (free)
             │
             ├─ Refinery
             └─ Supply Depot
                  │
Tier 1 ──────────┤
                  ├─ Barracks
                  ├─ Vehicle Factory
                  └─ Reactor (VC starts accumulating)
                        │
Tier 2 ───────────────┤
                        ├─ Tech Lab (req. Barracks + Vehicle Factory + 200 VC)
                        └─ Airfield (req. Vehicle Factory)
                              │
Tier 3 ─────────────────────┤
                              └─ Tech Lab Lv.2 (upgrade, req. Tech Lab + 300 VC)
                                    │
Tier 4 ─────────────────────────────┤
                                     └─ Faction Superweapon Building
                                          (req. Tech Lab Lv.2 + 600 VC)
```

### Tier Cost Summary

| Tier | Unlock Requirement                          | Min Cordite Investment | VC Required |
|------|---------------------------------------------|------------------------|-------------|
| 0    | Game start                                  | 0                      | 0           |
| 1    | Barracks + Vehicle Factory                  | ~2,300 C               | 0           |
| 2    | Tech Lab                                    | +1,500 C               | 200 VC      |
| 3    | Tech Lab Lv.2 (upgrade)                     | +1,500 C               | 300 VC      |
| 4    | Faction Superweapon Building                | +3,500 C               | 600 VC      |

---

## 2. Universal Tier Rules

### Tier 0 — Starting State
- **Auto-unlocked:** HQ (Command Center). Pre-placed at game start. Free.
- **Buildable immediately:** Refinery (1,500 C), Supply Depot (500 C)
- **HQ produces:** Harvester unit (auto-queue), faction Scout unit (buildable)
- **No VC requirement** for any Tier 0 content
- **Defensive options:** Basic wall segment and proximity mine available from HQ without prerequisites

### Tier 1 — Barracks + Vehicle Factory
- **Gate buildings:** Barracks (800 C), Vehicle Factory (1,500 C + 100 VC)
- **Reactor** becomes buildable at Tier 1 (cost varies by faction; begins VC accumulation)
- **Unlocks:** Full infantry roster (from Barracks), light vehicles and APCs (from Vehicle Factory)
- **Basic helicopter** (where available by faction) unlocks at Tier 1 from Vehicle Factory or Barracks
- **VC requirement:** None for buildings themselves; a few T1 units carry small VC costs (0–50 VC)
- **Upgrades:** 2 Tier 1 upgrades available per faction once Barracks is up

### Tier 2 — Tech Lab
- **Gate building:** Tech Lab (1,500 C + 200 VC) — requires Barracks AND Vehicle Factory active
- **Airfield** becomes buildable at Tier 2 (2,000 C + 200 VC) — requires Vehicle Factory
- **Faction-unique building** may unlock at Tier 2 (varies by faction)
- **Unlocks:** Tanks, medium artillery, advanced helicopters, jets (from Airfield), faction-specific upgrades
- **VC requirement:** Most T2 units cost 50–200 VC on top of Cordite
- **Upgrades:** 3 Tier 2 upgrades available per faction once Tech Lab is complete

### Tier 3 — Advanced Tech Lab
- **Gate:** Upgrade existing Tech Lab to Level 2 (costs 1,500 C + 300 VC, 45 sec build time)
- **Unlocks:** Heavy vehicles, advanced jets, super helicopters, second faction-unique building (some factions)
- **VC requirement:** 200–400 VC for T3 units
- **Upgrades:** 2–3 Tier 3 upgrades available per faction once Tech Lab Lv.2 is complete
- **Note:** The Airfield must already be built before the most advanced jets unlock at T3

### Tier 4 — Superweapon
- **Gate building:** Faction-specific superweapon structure (3,500–4,000 C + 600 VC)
- **Requires:** Tech Lab Lv.2 at minimum; some factions require additional prerequisites
- **Unlocks:** Faction superunit (unique per faction, only 1 may exist at once)
- **VC requirement:** 600–800 VC for superunit itself
- **Design intent:** Reaching T4 signals economic dominance. Opponents must respond immediately.

---

## 3. Upgrade Design Philosophy

Upgrades in Cordite Wars are **permanent, faction-wide buffs** researched at specific buildings. Once purchased, an upgrade applies to all currently existing and future units of the target type. Research is instant-but-queued — only one upgrade can research at a time per building.

### Cost Tiers

| Upgrade Tier | Cordite Cost | VC Cost  | Research Time | Effect Magnitude    |
|--------------|--------------|----------|---------------|---------------------|
| T1           | 400–600 C    | 0 VC     | 20–25 sec     | Minor (+10–15%)     |
| T2           | 800–1,200 C  | 50–100 VC| 30–40 sec     | Moderate (+15–25%)  |
| T3           | 1,500–2,500 C| 200–400 VC| 45–60 sec    | Major (new ability or +25%+) |

### Faction Identity Reinforcement

Each faction's upgrade list is written to reinforce — not contradict — their design identity:

- **Valkyr:** Sortie duration, aircraft speed, air-to-ground damage, recon range. Nothing buffs ground units.
- **Kragmore:** Horde Protocol thresholds, armor, crush damage, artillery splash/range. No air upgrades.
- **Bastion:** Wall HP, turret range, repair speed, Fortification Network bonus radius. Defense-only.
- **Arcloft:** Overwatch radius, AA damage, helicopter armor, detection range. Air + defense hybrid.
- **Ironmarch:** FOB HP and radius, forward construction speed, tank armor, MLRS accuracy. Positional warfare.
- **Stormrend:** Momentum gain rate, speed bonuses, raid damage, jet rearm time. Pure aggression.

### Upgrade Scope Rules
- An upgrade can target a **Category** (Infantry, Tank, Helicopter, Jet, Building, All)
- Or it can target a **specific unit** (e.g., "valkyr_harrier" only)
- Effects use one of three modifiers:
  - `multiply` — multiplies the base stat by the value (1.15 = +15%)
  - `add_flat` — adds an absolute number to the stat
  - `add` — adds a percentage expressed as a decimal (0.1 = +10% of base)

---

## 4. Valkyr Tech Tree

> *Identity: Air supremacy, precision strikes, terrain immunity, sortie-based jet operations.*
> *Tree Shape: Wide air branch, thin ground branch. Fastest to Tier 2 air units.*

### 4.1 ASCII Tech Tree Diagram

```
TIER 0
  [Aerie Command HQ] ─────────────────────────────────────────────────
       │                                                               │
       ├── [Sky Refinery] (1500C)                            produces: Harvester
       ├── [Aerie Depot] (500C)                                       Windrunner
       └── Wind Wall / Proximity Mine (static, no prereq)

TIER 1
  [Wing Barracks] (800C) ──────────────────────────────────────────────
       │                                                               │
       ├── produces: Windrunner, Skyguard, Gale Trooper       [Voltaic Spire] (800C)
       │                                                               │
  [Valkyr Motor Pool] (1500C + 100VC) ─────────────────────           └── +5 VC/sec
       │
       ├── produces: Zephyr Buggy, Mistral APC
       └── produces: Kestrel Helicopter (T1 air unlock!)

TIER 2
  [Aerie Research] (1500C + 200VC) ─────────────────────────────────────
  req: Wing Barracks + Valkyr Motor Pool                               │
       │                                                               │
       ├── produces: Harrier Helicopter                       [Valkyr Airstrip] (2000C + 200VC)
       ├── UPGRADES: T2 research available                    req: Valkyr Motor Pool
       │                                                               │
       │                                                               ├── produces: Peregrine Jet
       │                                                               ├── produces: Tempest Bomber
       │                                                               └── produces: Shriek Interceptor

TIER 3
  [Aerie Research Lv.2] (upgrade, +1500C + 300VC) ────────────────────
       │
       ├── produces: Updraft (repair helicopter)
       ├── produces: Overwatch Drone
       ├── UPGRADES: T3 research available
       └── [Carrier Pad] req: Tech Lab Lv.2 + Voltaic Spire ─────────────────────────
                                                                       │
TIER 4                                                       produces: Stormcaller Jet (super)
  [Carrier Pad] (3500C + 600VC) ─────────────────────                 produces: Valkyrie Carrier
  req: Aerie Research Lv.2 + Voltaic Spire

DEFENSES (available from HQ / Airstrip, no Barracks needed)
  └── Wind Wall (T0), Downburst Turret (T1, Motor Pool req),
      Skypiercer AA Turret (T2, Airstrip req)
```

### 4.2 Complete Unlock Table

| Building / Unit          | Tier | Produced By         | Prerequisite                         | VC Cost |
|--------------------------|------|---------------------|--------------------------------------|---------|
| Aerie Command HQ         | 0    | Pre-placed          | None                                 | 0       |
| Sky Refinery             | 0    | Place               | HQ                                   | 0       |
| Aerie Depot              | 0    | Place               | HQ                                   | 0       |
| Harvester                | 0    | Aerie Command HQ    | None                                 | 0       |
| Windrunner               | 0    | Aerie Command HQ    | None (scout unlock from HQ)          | 0       |
| Wind Wall                | 0    | Place               | None                                 | 0       |
| Wing Barracks            | 1    | Place               | Refinery                             | 0       |
| Valkyr Motor Pool        | 1    | Place               | Barracks                             | 100     |
| Voltaic Spire            | 1    | Place               | Refinery                             | 0       |
| Windrunner (upgrade)     | 1    | Wing Barracks       | Wing Barracks                        | 0       |
| Skyguard                 | 1    | Wing Barracks       | Wing Barracks                        | 0       |
| Gale Trooper             | 1    | Wing Barracks       | Wing Barracks                        | 50      |
| Zephyr Buggy             | 1    | Valkyr Motor Pool   | Valkyr Motor Pool                    | 0       |
| Mistral APC              | 1    | Valkyr Motor Pool   | Valkyr Motor Pool                    | 0       |
| Kestrel Helicopter       | 1    | Valkyr Motor Pool   | Valkyr Motor Pool + Voltaic Spire    | 50      |
| Downburst Turret         | 1    | Place               | Valkyr Motor Pool                    | 0       |
| Aerie Research (T2)      | 2    | Place               | Barracks + Motor Pool + 200 VC       | 200     |
| Valkyr Airstrip          | 2    | Place               | Valkyr Motor Pool                    | 200     |
| Harrier Helicopter       | 2    | Aerie Research      | Aerie Research                       | 100     |
| Peregrine Jet            | 2    | Valkyr Airstrip     | Valkyr Airstrip                      | 200     |
| Tempest Bomber           | 2    | Valkyr Airstrip     | Valkyr Airstrip                      | 300     |
| Shriek Interceptor       | 2    | Valkyr Airstrip     | Valkyr Airstrip                      | 150     |
| Skypiercer AA Turret     | 2    | Place               | Valkyr Airstrip                      | 0       |
| Aerie Research Lv.2      | 3    | Upgrade             | Aerie Research + 300 VC              | 300     |
| Updraft (repair helo)    | 3    | Aerie Research Lv.2 | Aerie Research Lv.2                  | 150     |
| Overwatch Drone          | 3    | Aerie Research Lv.2 | Aerie Research Lv.2                  | 100     |
| Carrier Pad              | 4    | Place               | Aerie Research Lv.2 + Voltaic Spire  | 600     |
| Stormcaller Jet          | 4    | Carrier Pad         | Carrier Pad                          | 800     |
| Valkyrie Carrier         | 4    | Carrier Pad         | Carrier Pad                          | 700     |

### 4.3 Valkyr Upgrades

**TIER 1 UPGRADES** — Researched at: Wing Barracks

---
**Wingman Protocol**
- Tier: 1 | Cost: 500 C / 0 VC | Research Time: 20 sec
- Prerequisite: Wing Barracks
- Effect: Windrunner and Skyguard gain +15% movement speed.
- Targets: `["windrunner", "skyguard"]` / Stat: `Speed` / Modifier: `multiply` / Value: `1.15`
- *Design note: Gets infantry out of danger faster and improves scout coverage.*

---
**Lightweight Ordnance**
- Tier: 1 | Cost: 400 C / 0 VC | Research Time: 20 sec
- Prerequisite: Wing Barracks
- Effect: Gale Trooper rocket damage +15%.
- Target: `Gale Trooper` / Stat: `Damage` / Modifier: `multiply` / Value: `1.15`
- *Design note: Makes Valkyr's only real ground AT threat more relevant.*

---

**TIER 2 UPGRADES** — Researched at: Aerie Research

---
**Extended Fuel Tanks**
- Tier: 2 | Cost: 1,000 C / 100 VC | Research Time: 30 sec
- Prerequisite: Valkyr Airstrip
- Effect: All Jet units double their loiter time over the battlefield.
- Target: `Jet` / Stat: `LoiterTime` / Modifier: `multiply` / Value: `2.0`
- *Design note: The signature Valkyr upgrade. Transforms sortie pressure into sustained dominance.*

---
**Air-to-Ground Munitions**
- Tier: 2 | Cost: 900 C / 75 VC | Research Time: 35 sec
- Prerequisite: Aerie Research
- Effect: Tempest Bomber and Harrier gain +20% damage vs. buildings and ground vehicles.
- Target: `["valkyr_tempest", "valkyr_harrier"]` / Stat: `Damage` / Modifier: `multiply` / Value: `1.20`
- ArmorModifier override: `Building: +0.2`, `Medium: +0.2`, `Heavy: +0.2`
- *Design note: Makes Valkyr's precision assets even more punishing in surgical strikes.*

---
**Target Designation Network**
- Tier: 2 | Cost: 800 C / 50 VC | Research Time: 30 sec
- Prerequisite: Aerie Research
- Effect: Windrunner target designations increase air damage bonus from +10% to +25%.
- Target: `valkyr_windrunner` / Stat: `DesignationBonus` / Modifier: `add_flat` / Value: `0.15`
- *Design note: Rewards coordinated infantry + air play, a uniquely Valkyr combination.*

---

**TIER 3 UPGRADES** — Researched at: Aerie Research Lv.2

---
**Afterburner Array**
- Tier: 3 | Cost: 2,000 C / 300 VC | Research Time: 50 sec
- Prerequisite: Aerie Research Lv.2
- Effect: All Helicopter and Jet units gain +20% movement speed.
- Target: `["Helicopter", "Jet"]` / Stat: `Speed` / Modifier: `multiply` / Value: `1.20`
- *Design note: Makes an already-mobile faction terrifying to pin down. A momentum-shifting upgrade.*

---
**EMP Hardening**
- Tier: 3 | Cost: 1,500 C / 200 VC | Research Time: 45 sec
- Prerequisite: Aerie Research Lv.2 + Carrier Pad placed
- Effect: All Valkyr aircraft gain immunity to EMP effects and 20% resistance to AA missiles.
- Target: `["Helicopter", "Jet"]` / Stat: `EMPImmunity` / Modifier: `add_flat` / Value: `1`
- Bonus Effect: `ArmorClass` vs. Missile: `multiply` `0.80` (reduces AA damage by 20%)
- *Design note: A defensive upgrade for the air faction — answers the Stormcaller's own EMP paradox.*

---
**Recon Supremacy**
- Tier: 3 | Cost: 1,800 C / 250 VC | Research Time: 45 sec
- Prerequisite: Aerie Research Lv.2
- Effect: All Valkyr aircraft gain +4 SightRange. Overwatch Drone detection radius doubles.
- Target: `["Helicopter", "Jet"]` / Stat: `SightRange` / Modifier: `add_flat` / Value: `4.0`
- Secondary Effect: `valkyr_overwatch_drone` / Stat: `DetectionRadius` / Modifier: `multiply` / Value: `2.0`
- *Design note: Cements Valkyr's vision advantage. An enemy can barely breathe without being spotted.*

---

## 5. Kragmore Tech Tree

> *Identity: Ground dominance, Horde Protocol mass bonuses, slow but unstoppable armor column.*
> *Tree Shape: Massive ground branch (3 tank tiers), minimal air (1 helicopter only). No jets.*

### 5.1 ASCII Tech Tree Diagram

```
TIER 0
  [War Command HQ] ───────────────────────────────────────────────────
       │                                                               │
       ├── [Ore Refinery] (1500C)                            produces: Harvester (HeavyVehicle)
       ├── [Supply Bunker] (500C)                                      Spotter (scout)
       └── Tremor Mine (static, T0 defensive option)

TIER 1
  [Drill Barracks] (800C) ─────────────────────────────────────────────
       │                                                               │
       ├── produces: Vanguard, Ironclad, Mole Rat, Spotter    [Kragmore Reactor] (1000C)
       │                                                               │
  [Kragmore Foundry] (1500C + 100VC) ──────────────────────           └── +5 VC/sec
       │
       ├── produces: Dust Runner
       ├── produces: Hauler APC
       ├── produces: Wrecker (repair)
       └── produces: Minelayer

TIER 2
  [Kragmore Lab] (1500C + 200VC) ───────────────────────────────────────
  req: Drill Barracks + Kragmore Foundry                               │
       │                                                               │
       ├── produces: Bulwark Tank                             [Kragmore Airfield] (2000C + 200VC)
       ├── produces: Quake Battery (artillery)                req: Kragmore Foundry
       ├── UPGRADES: T2 research available                             │
       │                                                               └── produces: Strix Gunship
       └── [War Forge] (2500C + 400VC) ─────────────────────
           req: Kragmore Foundry
                │
                └── produces: Anvil Tank
                    produces: Grinder (siege)

TIER 3
  [Kragmore Lab Lv.2] (upgrade, +1500C + 300VC) ──────────────────────
       │
       ├── produces: Mammoth Heavy Tank (also requires War Forge)
       ├── UPGRADES: T3 research available
       └── [Kragmore Superweapon] ─────────── (no separate building — Mammoth IS the superweapon unit)
           Mammoth: req: War Forge + Kragmore Lab Lv.2

DEFENSES
  └── Tremor Mine (T0), Ironwall (T1, Foundry req), Flak Nest (T2, Lab req),
      Bunker (T2, Barracks req)
```

### 5.2 Complete Unlock Table

| Building / Unit          | Tier | Produced By          | Prerequisite                              | VC Cost |
|--------------------------|------|----------------------|-------------------------------------------|---------|
| War Command HQ           | 0    | Pre-placed           | None                                      | 0       |
| Ore Refinery             | 0    | Place                | HQ                                        | 0       |
| Supply Bunker            | 0    | Place                | HQ                                        | 0       |
| Harvester                | 0    | War Command HQ       | None                                      | 0       |
| Spotter                  | 0    | War Command HQ       | None                                      | 0       |
| Tremor Mine              | 0    | Place                | None                                      | 0       |
| Drill Barracks           | 1    | Place                | Refinery                                  | 0       |
| Kragmore Foundry         | 1    | Place                | Barracks                                  | 100     |
| Kragmore Reactor         | 1    | Place                | Refinery                                  | 0       |
| Vanguard                 | 1    | Drill Barracks       | Drill Barracks                            | 0       |
| Ironclad                 | 1    | Drill Barracks       | Drill Barracks                            | 0       |
| Mole Rat                 | 1    | Drill Barracks       | Drill Barracks                            | 50      |
| Dust Runner              | 1    | Kragmore Foundry     | Kragmore Foundry                          | 0       |
| Hauler APC               | 1    | Kragmore Foundry     | Kragmore Foundry                          | 0       |
| Wrecker                  | 1    | Kragmore Foundry     | Kragmore Foundry                          | 0       |
| Minelayer                | 1    | Kragmore Foundry     | Kragmore Foundry                          | 0       |
| Ironwall                 | 1    | Place                | Kragmore Foundry                          | 0       |
| Kragmore Lab (T2)        | 2    | Place                | Barracks + Foundry + 200 VC               | 200     |
| War Forge                | 2    | Place                | Kragmore Foundry + Kragmore Reactor       | 400     |
| Kragmore Airfield        | 2    | Place                | Kragmore Foundry                          | 200     |
| Bulwark Tank             | 2    | Kragmore Lab         | Kragmore Lab                              | 100     |
| Quake Battery            | 2    | Kragmore Lab         | Kragmore Lab                              | 150     |
| Flak Nest                | 2    | Place                | Kragmore Lab                              | 0       |
| Bunker                   | 2    | Place                | Drill Barracks                            | 0       |
| Anvil Tank               | 2    | War Forge            | War Forge                                 | 200     |
| Grinder                  | 2    | War Forge            | War Forge                                 | 200     |
| Strix Gunship            | 2    | Kragmore Airfield    | Kragmore Airfield                         | 150     |
| Kragmore Lab Lv.2        | 3    | Upgrade              | Kragmore Lab + 300 VC                     | 300     |
| Mammoth                  | 4    | War Forge            | War Forge + Kragmore Lab Lv.2             | 600     |

*Note: Kragmore has no separate Tier 4 building — the Mammoth serves as the faction's superweapon unit, unlocked at the War Forge once Lab Lv.2 is complete. Only 2 Mammoths may exist simultaneously.*

### 5.3 Kragmore Upgrades

**TIER 1 UPGRADES** — Researched at: Drill Barracks

---
**Commissar Training**
- Tier: 1 | Cost: 500 C / 0 VC | Research Time: 25 sec
- Prerequisite: Drill Barracks
- Effect: Horde Protocol bonus thresholds reduced. Bonus now activates at 3 / 7 / 15 units (from 5 / 10 / 20).
- Target: `FactionMechanic` / Stat: `HordeThreshold` / Modifier: `add_flat` / Value: `-2` (applied to each tier)
- *Design note: Core Kragmore identity upgrade. Makes even small engagements horde-boosted.*

---
**Heavy Plate Kits**
- Tier: 1 | Cost: 600 C / 0 VC | Research Time: 25 sec
- Prerequisite: Drill Barracks
- Effect: All infantry gain +15% MaxHealth.
- Target: `Infantry` / Stat: `MaxHealth` / Modifier: `multiply` / Value: `1.15`
- *Design note: Kragmore infantry are already tough — this makes attrition fights cost the enemy more.*

---

**TIER 2 UPGRADES** — Researched at: Kragmore Lab

---
**Depleted Cordite Shells**
- Tier: 2 | Cost: 1,200 C / 100 VC | Research Time: 40 sec
- Prerequisite: Kragmore Lab
- Effect: Bulwark and Anvil tanks gain +20% damage vs. Heavy ArmorClass units.
- Target: `["kragmore_bulwark", "kragmore_anvil"]` / Stat: `ArmorModifier.Heavy` / Modifier: `multiply` / Value: `1.20`
- *Design note: Tank-on-tank combat becomes decisively Kragmore-favored.*

---
**Seismic Artillery Fuses**
- Tier: 2 | Cost: 1,000 C / 75 VC | Research Time: 35 sec
- Prerequisite: Kragmore Lab
- Effect: Quake Battery splash radius +30%, damage to buildings +25%.
- Target: `kragmore_quake_battery` / Stat: `AreaOfEffect` / Modifier: `multiply` / Value: `1.30`
- Secondary: `kragmore_quake_battery` / Stat: `ArmorModifier.Building` / Modifier: `multiply` / Value: `1.25`
- *Design note: Artillery becomes a fortress-ender, not just a unit-killer.*

---
**Crush Override**
- Tier: 2 | Cost: 900 C / 50 VC | Research Time: 30 sec
- Prerequisite: War Forge
- Effect: Anvil Tank and Grinder gain Crush Strength +1 (Anvil goes from 3 to 4; Grinder from 2 to 3). Speed penalty for crushing infantry reduced by 50%.
- Target: `["kragmore_anvil", "kragmore_grinder"]` / Stat: `CrushStrength` / Modifier: `add_flat` / Value: `1`
- Secondary: `["kragmore_anvil", "kragmore_grinder"]` / Stat: `CrushSpeedPenalty` / Modifier: `multiply` / Value: `0.50`
- *Design note: Makes heavy units even more catastrophic for enemy infantry clumps.*

---

**TIER 3 UPGRADES** — Researched at: Kragmore Lab Lv.2

---
**Iron Collective**
- Tier: 3 | Cost: 2,500 C / 400 VC | Research Time: 60 sec
- Prerequisite: Kragmore Lab Lv.2
- Effect: Maximum Horde Protocol bonus increased — 4th tier added at 30+ units: +30% damage, +25% armor. Horde bonuses now apply to the Strix Gunship as well.
- Target: `FactionMechanic` / Stat: `HordeMaxTier` / Modifier: `add_flat` / Value: `1`
- Secondary: `kragmore_strix_gunship` / Stat: `HordeEligible` / Modifier: `add_flat` / Value: `1`
- *Design note: The endgame Kragmore fantasy — a massive army that grows stronger the larger it gets.*

---
**Behemoth Chassis**
- Tier: 3 | Cost: 2,000 C / 300 VC | Research Time: 50 sec
- Prerequisite: Kragmore Lab Lv.2 + War Forge
- Effect: Mammoth gains +25% MaxHealth and its HP regeneration increases from +1 to +3 HP/sec.
- Target: `kragmore_mammoth` / Stat: `MaxHealth` / Modifier: `multiply` / Value: `1.25`
- Secondary: `kragmore_mammoth` / Stat: `RegenRate` / Modifier: `add_flat` / Value: `2.0`
- *Design note: Makes the Mammoth a true end-boss — not just powerful but unkillable without sustained focus fire.*

---
**Fortified Advance**
- Tier: 3 | Cost: 1,800 C / 250 VC | Research Time: 45 sec
- Prerequisite: Kragmore Lab Lv.2
- Effect: Kragmore walls (Ironwall, Bunker) gain +30% MaxHealth. Bunker weapon upgrades cost 25% less.
- Target: `["kragmore_ironwall", "kragmore_bunker"]` / Stat: `MaxHealth` / Modifier: `multiply` / Value: `1.30`
- *Design note: Turns Kragmore from pure offense into a faction that holds what it takes.*

---

## 6. Bastion Tech Tree

> *Identity: Fortification Network, passive income, attrition defense, structure repair.*
> *Tree Shape: Massive defense branch (many turret/wall tiers), unique Command Hub chain, minimal offense.*

### 6.1 ASCII Tech Tree Diagram

```
TIER 0
  [Citadel Core HQ] ──────────────────────────────────────────────────
       │                                                               │
       ├── [Shield Refinery] (1500C, +15C/sec passive!)      produces: Harvester
       ├── [Bastion Depot] (500C)                                      Patrol Rover (scout)
       └── Denial Field / Citadel Wall segment (T0 defense)

TIER 1
  [Guard Barracks] (800C) ─────────────────────────────────────────────
       │                                                               │
       ├── produces: Warden, Sentinel, Keeper             [Bastion Reactor] (1000C, +7VC/sec!)
       │                                                               │
  [Bastion Armory] (1500C + 100VC) ────────────────────────           │
       │                                                               │
       ├── produces: Shield Bearer APC
       ├── produces: Restoration Rig
       └── produces: Constructor

TIER 2
  [Bastion Tech Lab] (1500C + 200VC) ───────────────────────────────────
  req: Guard Barracks + Bastion Armory                                 │
       │                                                               │
       ├── produces: Rampart Tank                          [Bastion Airfield] (2000C + 200VC)
       ├── produces: Phalanx (damage sponge)               req: Bastion Armory
       ├── produces: Bulwark Mortar (artillery)                        │
       ├── UPGRADES: T2 research available                             ├── produces: Guardian Drone
       │                                                               └── produces: Watcher Helo
       └── [Command Hub] (2200C + 300VC) ──────────────────
           req: Bastion Tech Lab
                │
                ├── Extends Fortification Network
                └── prerequisite for Aegis Generator

TIER 3
  [Bastion Tech Lab Lv.2] (upgrade, +1500C + 300VC) ─────────────────
       │
       ├── UPGRADES: T3 research available
       └── [Aegis Generator] (3500C + 600VC) ──────────────
           req: Bastion Tech Lab Lv.2 + Command Hub

TIER 4
  [Aegis Generator] ── becomes Tier 4 superstructure

DEFENSES (unlocked progressively)
  └── Denial Field/Wall (T0), Bastion Turret (T2, Tech Lab req),
      Spire Array AA Turret (T2, Tech Lab + Airfield req),
      Citadel Wall upgrade (T3, Tech Lab Lv.2 req)
```

### 6.2 Complete Unlock Table

| Building / Unit          | Tier | Produced By          | Prerequisite                              | VC Cost |
|--------------------------|------|----------------------|-------------------------------------------|---------|
| Citadel Core HQ          | 0    | Pre-placed           | None                                      | 0       |
| Shield Refinery          | 0    | Place                | HQ                                        | 0       |
| Bastion Depot            | 0    | Place                | HQ                                        | 0       |
| Harvester                | 0    | Citadel Core HQ      | None                                      | 0       |
| Patrol Rover             | 0    | Citadel Core HQ      | None                                      | 0       |
| Denial Field             | 0    | Place                | None                                      | 0       |
| Citadel Wall             | 0    | Place                | None                                      | 0       |
| Guard Barracks           | 1    | Place                | Refinery                                  | 0       |
| Bastion Armory           | 1    | Place                | Barracks                                  | 100     |
| Bastion Reactor          | 1    | Place                | Refinery                                  | 0       |
| Warden                   | 1    | Guard Barracks       | Guard Barracks                            | 0       |
| Sentinel                 | 1    | Guard Barracks       | Guard Barracks                            | 0       |
| Keeper (engineer)        | 1    | Guard Barracks       | Guard Barracks                            | 0       |
| Shield Bearer APC        | 1    | Bastion Armory       | Bastion Armory                            | 0       |
| Restoration Rig          | 1    | Bastion Armory       | Bastion Armory                            | 0       |
| Constructor              | 1    | Bastion Armory       | Bastion Armory                            | 50      |
| Bastion Tech Lab (T2)    | 2    | Place                | Barracks + Armory + 200 VC                | 200     |
| Bastion Airfield         | 2    | Place                | Bastion Armory                            | 200     |
| Command Hub              | 2    | Place                | Bastion Tech Lab + Bastion Reactor        | 300     |
| Rampart Tank             | 2    | Bastion Tech Lab     | Bastion Tech Lab                          | 100     |
| Phalanx                  | 2    | Bastion Tech Lab     | Bastion Tech Lab                          | 200     |
| Bulwark Mortar           | 2    | Bastion Tech Lab     | Bastion Tech Lab                          | 150     |
| Guardian Drone           | 2    | Bastion Airfield     | Bastion Airfield                          | 100     |
| Watcher Helicopter       | 2    | Bastion Airfield     | Bastion Airfield                          | 50      |
| Bastion Turret           | 2    | Place                | Bastion Tech Lab                          | 0       |
| Spire Array              | 2    | Place                | Bastion Tech Lab + Bastion Airfield       | 0       |
| Bastion Tech Lab Lv.2    | 3    | Upgrade              | Bastion Tech Lab + 300 VC                 | 300     |
| Aegis Generator          | 4    | Place                | Bastion Tech Lab Lv.2 + Command Hub       | 600     |

### 6.3 Bastion Upgrades

**TIER 1 UPGRADES** — Researched at: Guard Barracks

---
**Garrison Protocol**
- Tier: 1 | Cost: 500 C / 0 VC | Research Time: 20 sec
- Prerequisite: Guard Barracks
- Effect: All infantry gain +20% armor when garrisoned in any structure.
- Target: `Infantry` / Stat: `GarrisonArmorBonus` / Modifier: `add_flat` / Value: `0.20`
- *Design note: Entrenched infantry become dramatically harder to clear.*

---
**Rapid Mortar Assembly**
- Tier: 1 | Cost: 600 C / 0 VC | Research Time: 25 sec
- Prerequisite: Guard Barracks
- Effect: Keeper builds turrets and walls 30% faster (stacks with base Keeper bonus for 60% total).
- Target: `bastion_keeper` / Stat: `ConstructionSpeed` / Modifier: `multiply` / Value: `1.30`
- *Design note: Faster fortification setup is everything for a turtling faction.*

---

**TIER 2 UPGRADES** — Researched at: Bastion Tech Lab

---
**Network Amplifier**
- Tier: 2 | Cost: 1,000 C / 75 VC | Research Time: 35 sec
- Prerequisite: Command Hub built
- Effect: Command Hub network radius increases from 8 cells to 12 cells. Network bonuses increase to +20% HP, +15% damage, +25% repair speed.
- Target: `FactionMechanic` / Stat: `NetworkRadius` / Modifier: `add_flat` / Value: `4`
- Secondary: `FactionMechanic` / Stat: `NetworkHPBonus` / Modifier: `add_flat` / Value: `0.05`
- *Design note: One Command Hub now covers nearly an entire base sector.*

---
**Redundant Systems**
- Tier: 2 | Cost: 900 C / 50 VC | Research Time: 30 sec
- Prerequisite: Command Hub built
- Effect: Turrets and walls retain 80% effectiveness (instead of 60%) when disconnected from the network.
- Target: `FactionMechanic` / Stat: `DisconnectedEfficiency` / Modifier: `add_flat` / Value: `0.20`
- *Design note: Hub destruction is no longer crippling — a required upgrade for stable defense.*

---
**Reinforced Turret Mounts**
- Tier: 2 | Cost: 1,100 C / 100 VC | Research Time: 40 sec
- Prerequisite: Bastion Tech Lab
- Effect: Bastion Turret and Spire Array gain +25% MaxHealth and +1 range.
- Target: `["bastion_bastion_turret", "bastion_spire_array"]` / Stat: `MaxHealth` / Modifier: `multiply` / Value: `1.25`
- Secondary: `["bastion_bastion_turret", "bastion_spire_array"]` / Stat: `Range` / Modifier: `add_flat` / Value: `1.0`
- *Design note: Makes Bastion's turrets substantially harder to outrange with artillery.*

---

**TIER 3 UPGRADES** — Researched at: Bastion Tech Lab Lv.2

---
**Aegis Overcharge**
- Tier: 3 | Cost: 2,000 C / 300 VC | Research Time: 55 sec
- Prerequisite: Aegis Generator placed
- Effect: Aegis dome absorbs 50% more damage (750 HP capacity, up from 500) and regenerates 50% faster (15 HP/sec, up from 10).
- Target: `bastion_aegis_generator` / Stat: `DomeCapacity` / Modifier: `multiply` / Value: `1.50`
- Secondary: `bastion_aegis_generator` / Stat: `RegenRate` / Modifier: `multiply` / Value: `1.50`
- *Design note: The endgame fortress becomes nearly impenetrable.*

---
**Self-Repair Nanites**
- Tier: 3 | Cost: 2,200 C / 350 VC | Research Time: 60 sec
- Prerequisite: Bastion Tech Lab Lv.2
- Effect: All Bastion buildings gain passive self-repair at +2 HP/sec (structures that already had self-repair gain +3 HP/sec instead).
- Target: `Building` / Stat: `SelfRepairRate` / Modifier: `add_flat` / Value: `2.0`
- *Design note: The base becomes effectively immortal unless attacked continuously.*

---
**Citadel Expansion Protocol**
- Tier: 3 | Cost: 1,800 C / 250 VC | Research Time: 50 sec
- Prerequisite: Bastion Tech Lab Lv.2
- Effect: Bastion can now build a third Command Hub (limit increases from 2 to 3). Command Hub HP increases by 40%.
- Target: `bastion_command_hub` / Stat: `MaxInstances` / Modifier: `add_flat` / Value: `1`
- Secondary: `bastion_command_hub` / Stat: `MaxHealth` / Modifier: `multiply` / Value: `1.40`
- *Design note: Allows truly massive, multi-sector fortification networks in long games.*

---

## 7. Arcloft Tech Tree

> *Identity: Air superiority + AA defense hybrid, Overwatch Zones, counter-air specialist.*
> *Tree Shape: Two parallel branches — air branch (jets + helos) and defense branch (AA turrets). Merged by Overwatch mechanic.*

### 7.1 ASCII Tech Tree Diagram

```
TIER 0
  [Sky Citadel HQ] ───────────────────────────────────────────────────
       │                                                               │
       ├── [Cloud Refinery] (1500C)                          produces: Harvester (Helicopter)
       ├── [Arcloft Depot] (500C)                                      Cirrus Runner (scout)
       └── Rampart Wall / Interceptor Battery (early T0 defense)

TIER 1
  [Sentinel Barracks] (800C) ─────────────────────────────────────────
       │                                                               │
       ├── produces: Horizon Guard, Stormshot, Sky Marshal   [Arc Reactor] (1000C, +6VC/sec)
       │                                                               │
  [Arcloft Workshop] (1500C + 100VC) ─────────────────────
       │
       ├── produces: Cirrus Runner (upgrade)
       ├── produces: Nimbus Transport APC
       └── [Overwatch Tower] (1500C + 150VC) ─────────────── available from HQ, no Barracks req!
           Overwatch Tower is Arcloft's unique building; buildable at T1 with Workshop
                │
                └── grants Overwatch Zone control (3 zones max)

TIER 2
  [Arcloft Tech Lab] (1500C + 200VC) ───────────────────────────────────
  req: Sentinel Barracks + Arcloft Workshop                            │
       │                                                               │
       ├── UPGRADES: T2 research available               [Arcloft Skyport] (2000C + 200VC)
       │                                                  req: Arcloft Workshop
       └── Patch Drone, Aether Relay available (from Skyport)          │
                                                                       ├── produces: Stratos Helo
                                                                       ├── produces: Templar Helo
                                                                       ├── produces: Apex Jet
                                                                       └── produces: Vigilant AWACS

TIER 3
  [Arcloft Tech Lab Lv.2] (upgrade, +1500C + 300VC) ─────────────────
       │
       ├── produces: Sky Bastion (mobile fortress helo)
       ├── UPGRADES: T3 research available
       └── [Sky Bastion acts as T4 superunit — no separate superweapon building]

DEFENSES
  └── Rampart Wall (T0), Arc Turret (T1, Workshop req),
      Flak Citadel AA (T2, Skyport req), Interceptor Battery (T2, Tech Lab req)
```

### 7.2 Complete Unlock Table

| Building / Unit          | Tier | Produced By          | Prerequisite                              | VC Cost |
|--------------------------|------|----------------------|-------------------------------------------|---------|
| Sky Citadel HQ           | 0    | Pre-placed           | None                                      | 0       |
| Cloud Refinery           | 0    | Place                | HQ                                        | 0       |
| Arcloft Depot            | 0    | Place                | HQ                                        | 0       |
| Harvester                | 0    | Sky Citadel HQ       | None                                      | 0       |
| Cirrus Runner (basic)    | 0    | Sky Citadel HQ       | None                                      | 0       |
| Rampart Wall             | 0    | Place                | None                                      | 0       |
| Interceptor Battery      | 0    | Place                | None                                      | 0       |
| Sentinel Barracks        | 1    | Place                | Refinery                                  | 0       |
| Arcloft Workshop         | 1    | Place                | Barracks                                  | 100     |
| Arc Reactor              | 1    | Place                | Refinery                                  | 0       |
| Overwatch Tower          | 1    | Place                | Arcloft Workshop                          | 150     |
| Horizon Guard            | 1    | Sentinel Barracks    | Sentinel Barracks                         | 0       |
| Stormshot                | 1    | Sentinel Barracks    | Sentinel Barracks                         | 0       |
| Sky Marshal              | 1    | Sentinel Barracks    | Sentinel Barracks                         | 50      |
| Cirrus Runner (T1)       | 1    | Arcloft Workshop     | Arcloft Workshop                          | 0       |
| Nimbus Transport         | 1    | Arcloft Workshop     | Arcloft Workshop                          | 0       |
| Arc Turret               | 1    | Place                | Arcloft Workshop                          | 0       |
| Arcloft Tech Lab (T2)    | 2    | Place                | Barracks + Workshop + 200 VC              | 200     |
| Arcloft Skyport          | 2    | Place                | Arcloft Workshop                          | 200     |
| Stratos Helicopter       | 2    | Arcloft Skyport      | Arcloft Skyport                           | 100     |
| Templar Helicopter       | 2    | Arcloft Skyport      | Arcloft Skyport                           | 150     |
| Apex Jet                 | 2    | Arcloft Skyport      | Arcloft Skyport                           | 200     |
| Vigilant AWACS           | 2    | Arcloft Skyport      | Arcloft Skyport                           | 150     |
| Patch Drone              | 2    | Arcloft Skyport      | Arcloft Skyport                           | 50      |
| Aether Relay             | 2    | Arcloft Skyport      | Arcloft Skyport + Overwatch Tower         | 200     |
| Flak Citadel             | 2    | Place                | Arcloft Skyport                           | 0       |
| Arcloft Tech Lab Lv.2    | 3    | Upgrade              | Arcloft Tech Lab + 300 VC                 | 300     |
| Sky Bastion              | 4    | Arcloft Skyport      | Arcloft Tech Lab Lv.2 + Overwatch Tower   | 800     |

### 7.3 Arcloft Upgrades

**TIER 1 UPGRADES** — Researched at: Sentinel Barracks

---
**Expanded Overwatch**
- Tier: 1 | Cost: 600 C / 0 VC | Research Time: 25 sec
- Prerequisite: Overwatch Tower built
- Effect: Overwatch Zone limit increases from 3 to 5. Zone radius increases from 15 to 20 cells.
- Target: `FactionMechanic` / Stat: `OverwatchZoneLimit` / Modifier: `add_flat` / Value: `2`
- Secondary: `FactionMechanic` / Stat: `OverwatchZoneRadius` / Modifier: `add_flat` / Value: `5`
- *Design note: Map control via Overwatch becomes genuinely intimidating with 5 large zones.*

---
**AA Hardened Rounds**
- Tier: 1 | Cost: 500 C / 0 VC | Research Time: 20 sec
- Prerequisite: Sentinel Barracks
- Effect: Stormshot infantry and Arc Turrets gain +15% damage vs. Aircraft ArmorClass.
- Target: `["arcloft_stormshot", "arcloft_arc_turret"]` / Stat: `ArmorModifier.Aircraft` / Modifier: `multiply` / Value: `1.15`
- *Design note: Arcloft's AA is already good — this makes it absurd against light factions.*

---

**TIER 2 UPGRADES** — Researched at: Arcloft Tech Lab

---
**Overwatch Aggression Protocol**
- Tier: 2 | Cost: 1,100 C / 100 VC | Research Time: 40 sec
- Prerequisite: Arcloft Tech Lab + Overwatch Tower
- Effect: Aircraft in Overwatch mode gain +10% damage, +20% detection range (in addition to existing Overwatch bonuses).
- Target: `FactionMechanic` / Stat: `OverwatchDamageBonus` / Modifier: `add_flat` / Value: `0.10`
- Secondary: `FactionMechanic` / Stat: `OverwatchDetectionBonus` / Modifier: `add_flat` / Value: `0.20`
- *Design note: Makes Overwatch mode offensive, not just defensive.*

---
**Composite Airframe**
- Tier: 2 | Cost: 1,000 C / 75 VC | Research Time: 35 sec
- Prerequisite: Arcloft Skyport
- Effect: All Arcloft Helicopters gain +20% MaxHealth and +10% ArmorValue.
- Target: `Helicopter` / Stat: `MaxHealth` / Modifier: `multiply` / Value: `1.20`
- Secondary: `Helicopter` / Stat: `ArmorValue` / Modifier: `multiply` / Value: `1.10`
- *Design note: Compensates for the hybrid tax — Arcloft helos become nearly as durable as Kragmore equivalents.*

---
**AWACS Net Integration**
- Tier: 2 | Cost: 900 C / 75 VC | Research Time: 30 sec
- Prerequisite: Arcloft Tech Lab + Vigilant in production
- Effect: Vigilant AWACS radar range bonus increases from +5 to +10 cells. All friendly units in AWACS range gain +10% attack speed.
- Target: `arcloft_vigilant` / Stat: `RadarRangeBonus` / Modifier: `add_flat` / Value: `5`
- Secondary: `arcloft_vigilant` / Stat: `AuraAttackSpeedBonus` / Modifier: `add_flat` / Value: `0.10`
- *Design note: Vigilant becomes a force multiplier, not just a scouting asset.*

---

**TIER 3 UPGRADES** — Researched at: Arcloft Tech Lab Lv.2

---
**Sky Fortress Protocol**
- Tier: 3 | Cost: 2,500 C / 400 VC | Research Time: 60 sec
- Prerequisite: Arcloft Tech Lab Lv.2 + Sky Bastion placed
- Effect: Sky Bastion gains +30% MaxHealth and the AA batteries track an additional target (now 5 simultaneous AA locks instead of 4).
- Target: `arcloft_sky_bastion` / Stat: `MaxHealth` / Modifier: `multiply` / Value: `1.30`
- Secondary: `arcloft_sky_bastion` / Stat: `AALockCount` / Modifier: `add_flat` / Value: `1`
- *Design note: Makes the Sky Bastion unkillable by anything short of a superunit.*

---
**Quantum Detection Grid**
- Tier: 3 | Cost: 2,000 C / 300 VC | Research Time: 50 sec
- Prerequisite: Arcloft Tech Lab Lv.2
- Effect: All Arcloft units with SightRange gain +50% stealth detection radius. Overwatch Zones automatically detect stealth.
- Target: `All` / Stat: `StealthDetectionRadius` / Modifier: `multiply` / Value: `1.50`
- Secondary: `FactionMechanic` / Stat: `OverwatchSteathDetect` / Modifier: `add_flat` / Value: `1`
- *Design note: Counter to Kragmore Mole Rats and Valkyr Windrunner stealth.*

---

## 8. Ironmarch Tech Tree

> *Identity: Forward Operating Bases, methodical advance, positional warfare, armored push.*
> *Tree Shape: Ground branch (tanks + FOB) + moderate defense branch. Air is minimal (one helicopter). Fastest FOB access.*

### 8.1 ASCII Tech Tree Diagram

```
TIER 0
  [Field HQ] ─────────────────────────────────────────────────────────
       │                                                               │
       ├── [March Refinery] (1500C, +50% HP, built-in turret!)produces: Harvester
       ├── [March Depot] (500C)                                        Pathcutter (scout)
       └── Fieldwork Wall / Wire Field (cheap T0 defensive options)

       └── [Forward Assembly] (1000C + 100VC) ─────────── available from Field HQ at T0!
           req: Field HQ only — Ironmarch's earliest unique building
                │
                └── enables: forward turret placement, basic infantry at frontline

TIER 1
  [Ironmarch Barracks] (800C) ─────────────────────────────────────────
       │                                                               │
       ├── produces: Holdfast, Breacher, Field Engineer      [March Reactor] (1000C)
       │                                                               │
  [Iron Foundry] (1500C + 100VC) ──────────────────────────           │
       │                                                               │
       ├── produces: Trenchline APC
       ├── produces: Dozer
       ├── produces: FOB Truck ─────────────────────────────── key unit!
       ├── produces: Field Hospital
       └── produces: Signal Truck

TIER 2
  [Ironmarch Lab] (1500C + 200VC) ────────────────────────────────────
  req: Ironmarch Barracks + Iron Foundry                               │
       │                                                               │
       ├── produces: Basalt Tank                            [Ironmarch Airfield] (2000C + 200VC)
       ├── produces: Siegebreaker Tank                      req: Iron Foundry
       ├── produces: Stonehail MLRS (artillery)                        │
       ├── UPGRADES: T2 research available                             └── produces: Bullfrog Helo
       └── Watchtower available (buildable with FOB)

TIER 3
  [Ironmarch Lab Lv.2] (upgrade, +1500C + 300VC) ─────────────────────
       │
       ├── produces: Juggernaut (deploy-mode super unit)
       ├── UPGRADES: T3 research available
       └── [Iron Citadel] (superweapon building, 4000C + 600VC) ────────
           req: Ironmarch Lab Lv.2 + Forward Assembly active
                │
TIER 4        └── Juggernaut available (if not already built)
                  Iron Citadel: passive AoE damage suppression field around HQ

DEFENSES
  └── Fieldwork Wall/Wire Field (T0), Watchtower (T2, Lab + FOB req),
      Forward turrets via Forward Assembly (T0 forward area)
```

### 8.2 Complete Unlock Table

| Building / Unit          | Tier | Produced By          | Prerequisite                              | VC Cost |
|--------------------------|------|----------------------|-------------------------------------------|---------|
| Field HQ                 | 0    | Pre-placed           | None                                      | 0       |
| March Refinery           | 0    | Place                | HQ                                        | 0       |
| March Depot              | 0    | Place                | HQ                                        | 0       |
| Forward Assembly         | 0    | Place                | Field HQ only                             | 100     |
| Harvester                | 0    | Field HQ             | None                                      | 0       |
| Pathcutter               | 0    | Field HQ             | None                                      | 0       |
| Fieldwork Wall           | 0    | Place                | None                                      | 0       |
| Wire Field               | 0    | Place                | None                                      | 0       |
| Ironmarch Barracks       | 1    | Place                | Refinery                                  | 0       |
| Iron Foundry             | 1    | Place                | Barracks                                  | 100     |
| March Reactor            | 1    | Place                | Refinery                                  | 0       |
| Holdfast                 | 1    | Ironmarch Barracks   | Ironmarch Barracks                        | 0       |
| Breacher                 | 1    | Ironmarch Barracks   | Ironmarch Barracks                        | 0       |
| Field Engineer           | 1    | Ironmarch Barracks   | Ironmarch Barracks                        | 0       |
| Trenchline APC           | 1    | Iron Foundry         | Iron Foundry                              | 0       |
| Dozer                    | 1    | Iron Foundry         | Iron Foundry                              | 0       |
| FOB Truck                | 1    | Iron Foundry         | Iron Foundry                              | 100     |
| Field Hospital           | 1    | Iron Foundry         | Iron Foundry                              | 50      |
| Signal Truck             | 1    | Iron Foundry         | Iron Foundry                              | 0       |
| Ironmarch Lab (T2)       | 2    | Place                | Barracks + Foundry + 200 VC               | 200     |
| Ironmarch Airfield       | 2    | Place                | Iron Foundry                              | 200     |
| Basalt Tank              | 2    | Ironmarch Lab        | Ironmarch Lab                             | 100     |
| Siegebreaker Tank        | 2    | Ironmarch Lab        | Ironmarch Lab                             | 200     |
| Stonehail MLRS           | 2    | Ironmarch Lab        | Ironmarch Lab                             | 150     |
| Bullfrog Helicopter      | 2    | Ironmarch Airfield   | Ironmarch Airfield                        | 100     |
| Watchtower               | 2    | Place (FOB radius)   | Ironmarch Lab + Forward Assembly          | 0       |
| Ironmarch Lab Lv.2       | 3    | Upgrade              | Ironmarch Lab + 300 VC                    | 300     |
| Juggernaut               | 3    | Iron Foundry         | Ironmarch Lab Lv.2                        | 400     |
| Iron Citadel             | 4    | Place                | Ironmarch Lab Lv.2 + Forward Assembly     | 600     |

*Note: Iron Citadel is Ironmarch's T4 superweapon building. It acts as a massive FOB-equivalent that projects a damage suppression field (enemy units within 20 cells deal 20% less damage). Combined with the Juggernaut deploy-mode super unit, Ironmarch's T4 creates an impenetrable forward position.*

### 8.3 Ironmarch Upgrades

**TIER 1 UPGRADES** — Researched at: Ironmarch Barracks

---
**Tactical Entrenching Kits**
- Tier: 1 | Cost: 500 C / 0 VC | Research Time: 20 sec
- Prerequisite: Ironmarch Barracks
- Effect: Holdfast foxhole dig time reduced from 5 sec to 2 sec. Foxhole armor bonus increases from +20% to +30%.
- Target: `ironmarch_holdfast` / Stat: `FoxholeBuildTime` / Modifier: `add_flat` / Value: `-3.0`
- Secondary: `ironmarch_holdfast` / Stat: `FoxholeArmorBonus` / Modifier: `add_flat` / Value: `0.10`
- *Design note: Makes Holdfast a truly terrifying defensive infantry — they can entrench almost instantly.*

---
**FOB Rapid Deployment**
- Tier: 1 | Cost: 600 C / 0 VC | Research Time: 25 sec
- Prerequisite: Iron Foundry
- Effect: FOB Truck deploy time reduced from 15 sec to 8 sec. Undeploy time reduced from 10 sec to 5 sec. FOB build radius increases by 20%.
- Target: `ironmarch_fob_truck` / Stat: `DeployTime` / Modifier: `add_flat` / Value: `-7.0`
- Secondary: `ironmarch_fob_truck` / Stat: `FOBRadius` / Modifier: `multiply` / Value: `1.20`
- *Design note: FOBs are the faction's identity. Faster deployment = faster territory control.*

---

**TIER 2 UPGRADES** — Researched at: Ironmarch Lab

---
**Reinforced FOB**
- Tier: 2 | Cost: 1,200 C / 100 VC | Research Time: 40 sec
- Prerequisite: Ironmarch Lab + FOB Truck in use
- Effect: FOB gains +50% MaxHealth and a built-in flak cannon (light AA, 3 range, 4 damage, targets air only).
- Target: `ironmarch_fob_truck` / Stat: `FOBMaxHealth` / Modifier: `multiply` / Value: `1.50`
- Secondary: `ironmarch_fob_truck` / Stat: `FOBFlakCannon` / Modifier: `add_flat` / Value: `1`
- *Design note: The one upgrade that gives Ironmarch a prayer against helicopters targeting their FOBs.*

---
**Siege Tank Munitions**
- Tier: 2 | Cost: 1,000 C / 75 VC | Research Time: 35 sec
- Prerequisite: Ironmarch Lab
- Effect: Siegebreaker Tank damage vs. structures increases from +50% to +90%. Basalt Tank range increases by 1.
- Target: `ironmarch_siegebreaker` / Stat: `ArmorModifier.Building` / Modifier: `multiply` / Value: `1.267`
- Secondary: `ironmarch_basalt` / Stat: `Range` / Modifier: `add_flat` / Value: `1.0`
- *Design note: The Siegebreaker becomes a wall-deleting machine. One hit, one hole.*

---
**Forward Road Network**
- Tier: 2 | Cost: 900 C / 50 VC | Research Time: 30 sec
- Prerequisite: Ironmarch Lab + FOB Truck in use
- Effect: Pathcutter road tiles last 180 sec (up from 60). Ground units on Pathcutter roads gain +30% speed (up from baseline road bonus).
- Target: `ironmarch_pathcutter` / Stat: `RoadTileDuration` / Modifier: `multiply` / Value: `3.0`
- Secondary: `ironmarch_pathcutter` / Stat: `RoadSpeedBonus` / Modifier: `multiply` / Value: `1.30`
- *Design note: Ironmarch army mobility triples with road infrastructure — addresses the "glacial speed" weakness.*

---

**TIER 3 UPGRADES** — Researched at: Ironmarch Lab Lv.2

---
**Iron Curtain**
- Tier: 3 | Cost: 2,500 C / 400 VC | Research Time: 60 sec
- Prerequisite: Iron Citadel placed
- Effect: Iron Citadel suppression field radius increases from 20 to 30 cells. Enemy damage penalty in the field increases from 20% to 35%.
- Target: `ironmarch_iron_citadel` / Stat: `SuppressionRadius` / Modifier: `add_flat` / Value: `10`
- Secondary: `ironmarch_iron_citadel` / Stat: `SuppressionPenalty` / Modifier: `add_flat` / Value: `0.15`
- *Design note: The endgame fortress position becomes a true area-denial superstructure.*

---
**Juggernaut Overhaul**
- Tier: 3 | Cost: 2,000 C / 350 VC | Research Time: 55 sec
- Prerequisite: Ironmarch Lab Lv.2 + Juggernaut built
- Effect: Juggernaut deploy time reduces from 10 sec to 5 sec. Deployed DPS increases by 25%. Juggernaut can now move 2 cells while deployed (micro-repositioning).
- Target: `ironmarch_juggernaut` / Stat: `DeployTime` / Modifier: `add_flat` / Value: `-5.0`
- Secondary: `ironmarch_juggernaut` / Stat: `Damage` / Modifier: `multiply` / Value: `1.25`
- *Design note: The Juggernaut becomes truly immovable and terrifying.*

---

## 9. Stormrend Tech Tree

> *Identity: Blitz Doctrine momentum, combined arms at maximum speed, fast tech rush, fragile but devastating.*
> *Tree Shape: Fastest tech path in the game. Can partially bypass Tier 2 via Momentum bonuses. Broadest combined arms at every tier.*

### 9.1 ASCII Tech Tree Diagram

```
TIER 0
  [Storm Command HQ] ─────────────────────────────────────────────────
       │                                                               │
       ├── [Lightning Refinery] (1500C)                      produces: Harvester
       ├── [Storm Depot] (500C)                                        Bolt (scout vehicle)
       └── Chainlink Barrier / Flicker Mine (minimal T0 defense)

       └── [Storm Capacitor] (2000C + 300VC) ─────────────── Stormrend unique: Momentum charger
           req: Storm Command HQ + Storm Reactor (early T1 build)

TIER 1
  [Blitz Barracks] (800C) ─────────────────────────────────────────────
       │                                                               │
       ├── produces: Razorwind, Thunderjaw, Sparkrunner       [Storm Reactor] (1250C) [EXPENSIVE]
       │                                                               │
  [Storm Garage] (1500C + 100VC) ──────────────────────────           └── +5 VC/sec
       │
       ├── produces: Bolt (upgrade — armed version)
       ├── produces: Tempest Raider
       ├── produces: Gust APC
       ├── produces: Surge Node
       └── produces: Scrap Hawk Helicopter (T1 air unlock!)

TIER 2
  [Storm Lab] (1500C + 200VC) ─────────────────────────────────────────
  req: Blitz Barracks + Storm Garage                                   │
       │                                                               │
       ├── produces: Riptide Fast Tank                    [Storm Airstrip] (2000C + 200VC)
       ├── produces: Cyclone Glass Cannon Tank             req: Storm Garage
       ├── produces: Thunderclap Mobile Artillery                      │
       ├── UPGRADES: T2 research available                             ├── produces: Flickerhawk Helo
       │                                                               ├── produces: Vortex Helo
       │   MOMENTUM SHORTCUT: If Momentum is at 75+,                  └── produces: Razorbeak Jet
       │   Storm Lab build cost reduced by 200C (1300C + 200VC)
       │   (Storm Capacitor must be built)

TIER 3
  [Storm Lab Lv.2] (upgrade, +1500C + 300VC) ─────────────────────────
       │                                                               │
       ├── UPGRADES: T3 research available               MOMENTUM SHORTCUT (Tier 3):
       └── [Storm Citadel] (superweapon, 4000C + 600VC)  If Storm Capacitor fully charged,
           req: Storm Lab Lv.2                            Storm Lab Lv.2 upgrade costs -300C
                │
TIER 4        └── produces: Stormbreaker Superjet (only 1 allowed)

DEFENSES
  └── Chainlink Barrier (T0), Snap Turret (T1, Garage req),
      Flicker Mine (T0, base-radius only)
```

*Design note on Momentum Shortcut: The Storm Capacitor's Momentum system is the game's only mechanic that directly reduces research/building costs. This represents Stormrend's doctrine — aggression funds technology. A Stormrend player who raids constantly can reach Tier 3 faster than any other faction.*

### 9.2 Complete Unlock Table

| Building / Unit          | Tier | Produced By          | Prerequisite                              | VC Cost |
|--------------------------|------|----------------------|-------------------------------------------|---------|
| Storm Command HQ         | 0    | Pre-placed           | None                                      | 0       |
| Lightning Refinery       | 0    | Place                | HQ                                        | 0       |
| Storm Depot              | 0    | Place                | HQ                                        | 0       |
| Harvester                | 0    | Storm Command HQ     | None                                      | 0       |
| Bolt (basic scout)       | 0    | Storm Command HQ     | None                                      | 0       |
| Chainlink Barrier        | 0    | Place                | None                                      | 0       |
| Flicker Mine             | 0    | Place                | None (base radius only)                   | 0       |
| Blitz Barracks           | 1    | Place                | Refinery                                  | 0       |
| Storm Garage             | 1    | Place                | Barracks                                  | 100     |
| Storm Reactor            | 1    | Place                | Refinery                                  | 0       |
| Storm Capacitor          | 1    | Place                | Storm Reactor                             | 300     |
| Razorwind                | 1    | Blitz Barracks       | Blitz Barracks                            | 0       |
| Thunderjaw               | 1    | Blitz Barracks       | Blitz Barracks                            | 0       |
| Sparkrunner              | 1    | Blitz Barracks       | Blitz Barracks                            | 0       |
| Bolt (armed)             | 1    | Storm Garage         | Storm Garage                              | 0       |
| Tempest Raider           | 1    | Storm Garage         | Storm Garage                              | 0       |
| Gust APC                 | 1    | Storm Garage         | Storm Garage                              | 0       |
| Surge Node               | 1    | Storm Garage         | Storm Garage                              | 50      |
| Scrap Hawk               | 1    | Storm Garage         | Storm Garage + Storm Reactor              | 50      |
| Snap Turret              | 1    | Place                | Storm Garage                              | 0       |
| Storm Lab (T2)           | 2    | Place                | Barracks + Garage + 200 VC                | 200     |
| Storm Airstrip           | 2    | Place                | Storm Garage                              | 200     |
| Riptide Tank             | 2    | Storm Lab            | Storm Lab                                 | 100     |
| Cyclone Tank             | 2    | Storm Lab            | Storm Lab                                 | 150     |
| Thunderclap MLRS         | 2    | Storm Lab            | Storm Lab                                 | 100     |
| Flickerhawk Helicopter   | 2    | Storm Airstrip       | Storm Airstrip                            | 100     |
| Vortex Helicopter        | 2    | Storm Airstrip       | Storm Airstrip                            | 150     |
| Razorbeak Jet            | 2    | Storm Airstrip       | Storm Airstrip                            | 150     |
| Storm Lab Lv.2           | 3    | Upgrade              | Storm Lab + 300 VC                        | 300     |
| Storm Citadel            | 4    | Place                | Storm Lab Lv.2                            | 600     |
| Stormbreaker Jet         | 4    | Storm Airstrip       | Storm Lab Lv.2 + Storm Citadel            | 800     |

### 9.3 Stormrend Upgrades

**TIER 1 UPGRADES** — Researched at: Blitz Barracks

---
**Blitz Conditioning**
- Tier: 1 | Cost: 500 C / 0 VC | Research Time: 20 sec
- Prerequisite: Blitz Barracks
- Effect: All infantry gain +15% movement speed. Sprint cooldown for Razorwind reduced from 15 sec to 10 sec.
- Target: `Infantry` / Stat: `Speed` / Modifier: `multiply` / Value: `1.15`
- Secondary: `stormrend_razorwind` / Stat: `SprintCooldown` / Modifier: `multiply` / Value: `0.667`
- *Design note: Makes the already-fastest infantry even more relentless.*

---
**Momentum Overdrive**
- Tier: 1 | Cost: 600 C / 0 VC | Research Time: 25 sec
- Prerequisite: Storm Capacitor built
- Effect: Momentum gain rate from dealing damage increases by 25%. Momentum decay when not dealing damage slows from -3/sec to -2/sec.
- Target: `FactionMechanic` / Stat: `MomentumGainRate` / Modifier: `multiply` / Value: `1.25`
- Secondary: `FactionMechanic` / Stat: `MomentumDecayRate` / Modifier: `add_flat` / Value: `-1.0`
- *Design note: Stormrend reaches Stormbreak faster and holds it longer. Transforms the faction mechanic.*

---

**TIER 2 UPGRADES** — Researched at: Storm Lab

---
**Predator Loadouts**
- Tier: 2 | Cost: 1,000 C / 75 VC | Research Time: 30 sec
- Prerequisite: Storm Lab
- Effect: Cyclone Tank first-shot bonus increases from +30% to +50%. Riptide flanking bonus increases from +25% to +40%.
- Target: `stormrend_cyclone` / Stat: `FirstShotBonus` / Modifier: `add_flat` / Value: `0.20`
- Secondary: `stormrend_riptide` / Stat: `FlankingBonus` / Modifier: `add_flat` / Value: `0.15`
- *Design note: Elevates Stormrend's alpha-strike capability — devastating for ambush openings.*

---
**Rapid Rearm Systems**
- Tier: 2 | Cost: 1,100 C / 100 VC | Research Time: 40 sec
- Prerequisite: Storm Airstrip
- Effect: Razorbeak Jet rearm time reduces from 8 sec to 4 sec. All helicopter reload times reduce by 20%.
- Target: `stormrend_razorbeak` / Stat: `RearmTime` / Modifier: `add_flat` / Value: `-4.0`
- Secondary: `["stormrend_flickerhawk", "stormrend_vortex"]` / Stat: `ReloadTime` / Modifier: `multiply` / Value: `0.80`
- *Design note: Stormrend's air units cycle through combat nearly twice as fast.*

---
**Raid Protocol**
- Tier: 2 | Cost: 900 C / 50 VC | Research Time: 35 sec
- Prerequisite: Storm Lab
- Effect: Bolt, Tempest Raider, and Flickerhawk deal +25% damage to Harvester units and +20% damage to Refinery buildings.
- Target: `["stormrend_bolt", "stormrend_tempest_raider", "stormrend_flickerhawk"]` / Stat: `HarvesterDamageBonus` / Modifier: `add_flat` / Value: `0.25`
- *Design note: Codifies and amplifies Stormrend's most powerful macro strategy.*

---

**TIER 3 UPGRADES** — Researched at: Storm Lab Lv.2

---
**Stormbreak Cascade**
- Tier: 3 | Cost: 2,500 C / 400 VC | Research Time: 60 sec
- Prerequisite: Storm Lab Lv.2 + Storm Capacitor
- Effect: When Stormbreak activates (100 Momentum), duration extends from 15 sec to 22 sec. During Stormbreak, all new units complete production in 50% of normal time (up from base 50% — this stacks for 75% reduction total).
- Target: `FactionMechanic` / Stat: `StormbreakDuration` / Modifier: `add_flat` / Value: `7.0`
- Secondary: `FactionMechanic` / Stat: `StormbreakProductionBonus` / Modifier: `add_flat` / Value: `0.25`
- *Design note: Stormbreak becomes a genuine turning point that floods the field with fresh units.*

---
**Lightning Payload**
- Tier: 3 | Cost: 2,000 C / 300 VC | Research Time: 50 sec
- Prerequisite: Storm Lab Lv.2 + Storm Citadel
- Effect: Stormbreaker Jet carpet bomb area increases by 40% (30 → 42 cell path width). Cluster bomb damage increased by 25%.
- Target: `stormrend_stormbreaker` / Stat: `BombPathWidth` / Modifier: `add_flat` / Value: `12`
- Secondary: `stormrend_stormbreaker` / Stat: `Damage` / Modifier: `multiply` / Value: `1.25`
- *Design note: The superweapon becomes a true map-erasing event.*

---

## 10. Cross-Faction Tier Comparison

### Tier Reach Timings (estimated, standard 1v1 on medium map)

| Faction    | T1 Est. Time | T2 Est. Time | T3 Est. Time | T4 Est. Time | Notes                              |
|------------|-------------|-------------|-------------|-------------|-------------------------------------|
| Valkyr     | 1:30–2:00   | 5:00–6:00   | 8:00–10:00  | 15:00–18:00 | Cheapest Reactor → fastest VC       |
| Kragmore   | 2:00–2:30   | 6:00–7:00   | 9:00–11:00  | N/A*        | No T4 building; Mammoth is T4 unit  |
| Bastion    | 1:45–2:15   | 5:30–6:30   | 8:30–10:30  | 16:00–20:00 | Superior VC gen → slightly faster   |
| Arcloft    | 1:30–2:00   | 5:00–6:00   | 8:00–10:00  | 16:00–19:00 | Good VC gen, lower income ceiling   |
| Ironmarch  | 1:45–2:15   | 6:00–7:00   | 9:00–11:00  | 16:00–20:00 | FOB available at T0 is unique power |
| Stormrend  | 1:30–2:00   | 4:30–5:30†  | 7:00–9:00†  | 14:00–17:00 | †With full Momentum discount        |

*Kragmore's Mammoth acts as their T4 superunit unlocked from War Forge + T3 Lab. No separate T4 building.*

### Asymmetric Tech Design Summary

| Faction    | Unique Tech Advantage                          | Tech Weakness                              |
|------------|------------------------------------------------|--------------------------------------------|
| Valkyr     | Airstrip at T2 unlocks 3 jet types; cheapest Reactor | Ground branch has only 2 vehicle types |
| Kragmore   | War Forge unlocks 3 heavy unit types at T2     | No jet line at all; Airfield = 1 helicopter |
| Bastion    | Command Hub → Aegis chain requires no Airfield  | Offense-only units locked behind T2/T3    |
| Arcloft    | Overwatch Tower buildable at T1 (earliest unique) | No heavy vehicles whatsoever              |
| Ironmarch  | Forward Assembly available at T0 (only faction) | Weakest air tree (1 helicopter only)       |
| Stormrend  | Momentum discounts can rush T2/T3 faster       | Expensive Reactor delays first VC access   |

---

## 11. Design Notes & Balance Rationale

### 11.1 Why Each Tree Is Shaped Differently

**Valkyr** intentionally has a "wide air, thin ground" tree. The ground branch caps at two units (Zephyr Buggy, Mistral APC) — no tanks, no heavy vehicles. Every Cordite invested into Valkyr's ground presence is an acknowledgment of the faction's fundamental weakness. The tree nudges players toward the Airstrip as fast as possible.

**Kragmore** has the deepest ground tree — three tank/heavy lines gated behind the War Forge side-branch. This creates a meaningful decision: rush Lab for the Bulwark (cheaper, faster path) or invest in War Forge for the Anvil and eventually Mammoth (slower but dominant). The Airfield is a deliberately expensive afterthought — Kragmore players should rarely build it.

**Bastion** has a defense branch that is truly deeper than anyone else's, but the offensive units are scattered and unimpressive. The Command Hub → Aegis chain is a unique "building that empowers other buildings" sequence that no other faction has. Players who skip Command Hub get weaker turrets; players who rush it sacrifice offensive capability.

**Arcloft** has the only T1-accessible unique building (Overwatch Tower requires Workshop only). This gives Arcloft map control faster than anyone, but the tree's dual-branch structure (air + defense) means VC is always split. Arcloft never has as much of either as the pure specialists.

**Ironmarch** has the Forward Assembly accessible at T0 — the earliest-available faction unique. This defines the faction's "forward basing" identity from turn one. The tech tree is otherwise conventional, but the FOB Truck unlocking at T1 means Ironmarch can theoretically have a functioning forward production line before anyone else reaches T2.

**Stormrend** has the only cost-modifying mechanic in the game (Momentum discount on buildings). This creates a high-skill ceiling: a master Stormrend player who maintains constant pressure can reach T3 a full minute ahead of schedule. A losing Stormrend player may fall further behind in tech because they can't generate Momentum to offset their expensive Reactor.

### 11.2 Upgrade Accessibility and Skew Prevention

All Tier 1 upgrades intentionally cost 0 VC — they're accessible to players who haven't built a Reactor yet. This means even a player who delays VC investment can get meaningful upgrades during the T1 fighting phase.

Tier 2 upgrades require 50–100 VC, which aligns with the window where Reactor income has been building for 2–4 minutes. Players who built their Reactor first get these upgrades sooner — a meaningful reward for the tech investment.

Tier 3 upgrades cost 200–400 VC and are explicitly intended to be game-changers. They should feel like a significant decision, not a checkbox. A faction with both T3 upgrades researched should feel meaningfully different from the same faction without them.

### 11.3 Superweapon Differentiation

Each faction's T4 "superweapon" reflects their identity differently:

| Faction   | T4 Expression         | Form                 | Counter-play                           |
|-----------|-----------------------|----------------------|----------------------------------------|
| Valkyr    | Valkyrie Carrier      | Mobile aircraft unit | AA focus, destroy Carrier Pad first    |
| Kragmore  | Mammoth Tank          | Ground super-unit    | Artillery kiting, aerial assassination |
| Bastion   | Aegis Generator       | Static superstructure| Artillery siege, ignore dome, hit flanks |
| Arcloft   | Sky Bastion           | Mobile fortress unit | Mass AA, target while deploying        |
| Ironmarch | Iron Citadel (field)  | Suppression building | Airstrikes, long-range siege           |
| Stormrend | Stormbreaker Jet      | Sortie superunit     | AA mass, destroy Storm Citadel prereq  |

### 11.4 Upgrade Interaction Table

Some upgrades interact with other faction mechanics in non-obvious ways:

- **Valkyr Extended Fuel Tanks + Target Designation Network:** Windrunners can now maintain designation on targets for twice as long, dramatically improving Tempest Bomber efficiency.
- **Kragmore Commissar Training + Iron Collective:** Combined, Horde bonuses activate at 3 units and have a 4th tier at 30. A full Kragmore army becomes the highest-damage-per-unit faction in the game.
- **Bastion Network Amplifier + Redundant Systems:** Network benefits are stronger AND the fallback when hubs are destroyed is 80% effectiveness. Bastion defenses become extremely resilient to both direct assault and hub targeting.
- **Ironmarch FOB Rapid Deployment + Reinforced FOB:** FOBs deploy in 8 seconds and are 50% tougher. An Ironmarch player can build a forward position within the time it takes an opponent to reach it.
- **Stormrend Momentum Overdrive + Stormbreak Cascade:** Momentum fills 25% faster, decays slower, and Stormbreak lasts 22 seconds with enhanced production. In a prolonged fight, Stormrend generates multiple Stormbreaks.

### 11.5 Open Balance Questions

- **Valkyr T1 air access (Kestrel from Motor Pool):** The Kestrel helicopter is available at T1, which may be too early. Testing needed to determine if it dominates T1 ground engagements.
- **Stormrend Momentum discount stacking:** If a Stormrend player maintains 75+ Momentum throughout the game, they can save 200C + 300C = 500C on Tech Lab upgrades. Over a full game, this may over-reward skilled play.
- **Kragmore War Forge VC requirement (400 VC):** This is the highest non-T4 building VC cost in the game. May gate the War Forge too hard and prevent Kragmore players from accessing their tank fantasy.
- **Bastion Reactor VC rate (7/sec) and upgrade timing:** Bastion may be able to research T2 upgrades too early if they prioritize Reactor above all else. Monitor upgrade acquisition timing in testing.
- **Ironmarch Forward Assembly at T0:** No other faction has a unique building available before T1. This could create a significant early-game power spike — Ironmarch may be able to forward-fortify before the opponent has any T1 combat units.

---

*End of Tech Tree Design v0.1*  
*Cordite Wars: Six Fronts — Internal Design Reference*  
*Document length: ~900 lines*
