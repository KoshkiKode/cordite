# Cordite Wars: Six Fronts — Economy Balance Document

**Version:** 0.1 (Design Draft)  
**Last Updated:** April 2026  
**Scope:** Core economy constants, faction modifiers, building costs, tech tiers, income projections, unit pricing philosophy, and game timing targets.

---

## Table of Contents

1. [Core Economy Constants](#1-core-economy-constants)
2. [Harvester Income Model](#2-harvester-income-model)
3. [Faction Economy Modifiers](#3-faction-economy-modifiers)
4. [Building Costs & Stats — All Factions](#4-building-costs--stats--all-factions)
5. [Faction-Unique Buildings](#5-faction-unique-buildings)
6. [Tech Tier Progression](#6-tech-tier-progression)
7. [Income Projection Tables](#7-income-projection-tables)
8. [Unit Cost Balance Principles](#8-unit-cost-balance-principles)
9. [Game Timing Targets](#9-game-timing-targets)
10. [Balance Design Notes](#10-balance-design-notes)

---

## 1. Core Economy Constants

These values are global and apply to all factions unless a faction modifier explicitly overrides them.

| Constant                  | Value           | Notes                                              |
|---------------------------|-----------------|----------------------------------------------------|
| Cordite Node base capacity| 10,000          | Depletes as harvested. Nodes do not regenerate.    |
| Harvester carry capacity  | 500 Cordite     | Base. Faction modifiers may raise or lower this.   |
| Harvester load time       | 3.0 seconds     | Time to load at the Cordite Node.                  |
| Harvester unload time     | 3.0 seconds     | Time to unload at the Refinery.                    |
| Reactor VC generation     | 5 VC/sec        | Base rate. Faction modifiers may change.           |
| Reactor VC stockpile cap  | 500 VC          | Hard cap per Reactor. Multiple Reactors stack.     |
| Supply Depot supply bonus | +20 supply      | Each Depot adds 20 to the supply ceiling.          |
| Max Supply Depots         | 10              | Gives a hard supply cap of 200 (standard).         |
| Base supply cap           | 200             | HQ provides 10 starting supply; 10 Depots = +190. |
| Refinery passive income   | 0 Cordite/sec   | Base. Bastion faction overrides this to +15/sec.   |

### Cordite Node Lifecycle

A single node with 10,000 Cordite capacity will be exhausted after 20 full harvester trips (at base capacity 500). With multiple harvesters running simultaneously, nodes deplete faster, pressuring players to scout and contest secondary nodes early. Node exhaustion is intended to drive map control decisions by the mid-game.

**Design goal:** A single contested node should support ~4–6 minutes of active harvesting before depletion forces expansion.

---

## 2. Harvester Income Model

### Formula

```
Trip time (seconds) = (2 × distance_in_cells / speed) + load_time + unload_time
Income (Cordite/sec) = carry_capacity / trip_time
```

Where:
- `speed` is in cells/second
- `load_time` = 3.0 sec
- `unload_time` = 3.0 sec
- `distance_in_cells` = one-way distance from Refinery to Node

### Reference Distances

| Distance Class | One-Way Cells | Notes                                     |
|----------------|---------------|-------------------------------------------|
| Close          | 10 cells      | Node adjacent to base or FOB              |
| Medium         | 20 cells      | Standard expansion node                   |
| Far            | 35 cells      | Contested outer node, requires escort     |

### Base Harvester Income at Medium Distance (20 cells)

Using LightVehicle speed 0.35 cells/sec, capacity 500:

```
Trip time = (2 × 20 / 0.35) + 3 + 3
          = (40 / 0.35) + 6
          = 114.28 + 6
          = 120.28 seconds
Income    = 500 / 120.28 ≈ 4.16 Cordite/sec ≈ 80 Cordite/min (per harvester... wait)
```

> **Clarification on units:** The "~80 Cordite/sec" figure in design shorthand refers to 80 Cordite per minute normalized, or approximately 4.16 Cordite/sec actual. Throughout this document, all income rates expressed as "Cordite/min" use per-minute figures for readability. The /sec notation in faction summaries refers to per-minute normalized shorthand used in planning tools.

### Income Rate Scaling

All income figures below are **Cordite per minute** per harvester unless otherwise noted.

| Faction    | Harvester Type | Speed (cells/sec) | Capacity | Close Node | Medium Node | Far Node |
|------------|----------------|-------------------|----------|------------|-------------|----------|
| Valkyr     | Helicopter     | 0.30              | 350      | 97 C/min   | 72 C/min    | 50 C/min |
| Kragmore   | HeavyVehicle   | 0.10              | 1000     | 92 C/min   | 50 C/min    | 30 C/min |
| Bastion    | LightVehicle   | 0.35              | 500      | 126 C/min  | 83 C/min    | 56 C/min |
| Arcloft    | Helicopter     | 0.30              | 500      | 138 C/min  | 102 C/min   | 71 C/min |
| Ironmarch  | LightVehicle   | 0.35              | 500      | 126 C/min  | 83 C/min    | 56 C/min |
| Stormrend  | LightVehicle   | 0.35              | 400      | 101 C/min  | 66 C/min    | 45 C/min |

> Helicopter-class harvesters bypass terrain obstacles, effectively making all distances shorter on complex maps. This is factored into difficulty ratings but not the raw formula above.

---

## 3. Faction Economy Modifiers

Each faction has distinct economic traits that shape macro strategy and game pacing. All deviations from the base economy are listed explicitly.

---

### 3.1 Valkyr (Air Primary)

Valkyr harvesters are helicopter-class, making them fast across open terrain and immune to ground obstacles, but they carry less Cordite per trip.

| Modifier               | Value                    | vs. Base          |
|------------------------|--------------------------|-------------------|
| Harvester class        | Helicopter               | —                 |
| Harvester speed        | 0.30 cells/sec           | −14% vs Light     |
| Harvester capacity     | 350 Cordite              | −30% (70% of 500) |
| Reactor cost           | 800 Cordite              | −20%              |
| Reactor VC rate        | 5 VC/sec                 | Standard          |
| Refinery passive income| 0 Cordite/sec            | Standard          |
| Supply cap             | 200                      | Standard          |
| Max Depots             | 10                       | Standard          |

**Income per harvester (medium node):** ~72 Cordite/min  
**Income per harvester (close node):** ~97 Cordite/min

**Net Effect:** Valkyr harvesters are less efficient per trip but far harder to intercept. Terrain-locked factions (Kragmore, Ironmarch) cannot easily raid Valkyr harvesters without anti-air units. The cheaper Reactor allows Valkyr to reach VC-dependent aircraft earlier.

**Macro Strategy:**
- Run 3+ harvesters at all times to compensate for lower per-trip yields.
- Prioritize Refinery placement near the node (harvesters return faster to a closer Refinery).
- Build a second Reactor before Tier 2 to afford the air fleet's VC demands.
- Expand to secondary nodes early — low capacity means faster node depletion.

---

### 3.2 Kragmore (Ground Primary)

Kragmore harvesters are massive HeavyVehicle-class rigs that carry twice the Cordite of any other faction but move at a crawl.

| Modifier               | Value                    | vs. Base          |
|------------------------|--------------------------|-------------------|
| Harvester class        | HeavyVehicle             | —                 |
| Harvester speed        | 0.10 cells/sec           | −71% vs Light     |
| Harvester capacity     | 1000 Cordite             | +100% (2× base)   |
| Reactor cost           | 1000 Cordite             | Standard          |
| Reactor VC rate        | 5 VC/sec                 | Standard          |
| Refinery passive income| 0 Cordite/sec            | Standard          |
| Supply cap             | 220                      | +10% (+20 supply) |
| Max Depots             | 10 (but HQ gives 20)     | Standard 10 Depots|

> Kragmore HQ grants 20 starting supply (vs 10 base), effectively one free Depot equivalent.

**Income per harvester (medium node):** ~50 Cordite/min  
**Income per harvester (close node):** ~92 Cordite/min

**Net Effect:** Kragmore's harvester is extremely valuable. A single destroyed harvester represents an enormous income loss. Trip times at far nodes are brutally long (~7 minutes per trip). Kragmore is heavily rewarded for keeping Refineries close to nodes — a "Forward Refinery" strategy is almost mandatory.

**Macro Strategy:**
- Place Refineries adjacent to or within 5 cells of nodes.
- Never send harvesters to far nodes without a full escort.
- Run 2 harvesters maximum on close nodes, 1 on medium nodes.
- Compensate slow VC income with overwhelming ground unit spam (high supply cap helps).

---

### 3.3 Bastion (Defense Primary)

Bastion has standard harvester performance, but their Refineries generate passive income and their Reactors are superior VC generators.

| Modifier               | Value                    | vs. Base          |
|------------------------|--------------------------|-------------------|
| Harvester class        | LightVehicle             | Standard          |
| Harvester speed        | 0.35 cells/sec           | Standard          |
| Harvester capacity     | 500 Cordite              | Standard          |
| Reactor cost           | 1000 Cordite             | Standard          |
| Reactor VC rate        | 7 VC/sec                 | +40%              |
| Refinery passive income| +15 Cordite/sec (900/min)| Unique to Bastion |
| Supply cap             | 200                      | Standard          |
| Max Depots             | 10                       | Standard          |

**Income per harvester (medium node):** ~83 Cordite/min  
**Income per harvester (close node):** ~126 C/min  
**Refinery passive income:** +900 Cordite/min per Refinery (15/sec × 60)

**Break-even on Refinery (passive only):** 1500 cost / 15/sec = 100 seconds  

**Net Effect:** Bastion's passive Refinery income is a powerful long-game engine. Two Refineries alone generate 1800 Cordite/min passively — equivalent to 21 standard harvesters. Combined with their superior VC rate, Bastion naturally snowballs over time. Their vulnerability is the early game, before multiple Refineries are established.

**Macro Strategy:**
- Build second Refinery before second Barracks.
- Turtle aggressively — every minute of survival earns more passive income.
- Target 3 Refineries by 8 minutes for dominant late-game economy.
- Use superior VC income to field VC-expensive units earlier than opponents expect.

---

### 3.4 Arcloft (Air + Defense)

Arcloft fields helicopter harvesters at full base capacity with a bonus VC rate, but at the cost of a reduced supply cap.

| Modifier               | Value                    | vs. Base          |
|------------------------|--------------------------|-------------------|
| Harvester class        | Helicopter               | —                 |
| Harvester speed        | 0.30 cells/sec           | −14% vs Light     |
| Harvester capacity     | 500 Cordite              | Standard          |
| Reactor cost           | 1000 Cordite             | Standard          |
| Reactor VC rate        | 6 VC/sec                 | +20%              |
| Refinery passive income| 0 Cordite/sec            | Standard          |
| Supply cap             | 180                      | −10% (max 9 Depots)|
| Max Depots             | 9                        | −1 Depot          |

**Income per harvester (medium node):** ~102 Cordite/min  
**Income per harvester (close node):** ~138 C/min

**Net Effect:** Arcloft has excellent harvester efficiency (full capacity + fly-over) and good VC generation. The supply penalty forces quality-over-quantity composition. Arcloft players must spend more per-unit on elite options to be competitive.

**Macro Strategy:**
- Prioritize tech upgrades over raw army count.
- Use reduced supply cap as a discipline forcing function — do not waste pop on inefficient units.
- Field 2 harvesters with early Depot investment to offset the supply constraint on unit production pace.

---

### 3.5 Ironmarch (Ground + Defense)

Ironmarch harvesters are standard, but their Refineries are armored mini-fortresses, making economic harassment nearly impossible.

| Modifier               | Value                    | vs. Base          |
|------------------------|--------------------------|-------------------|
| Harvester class        | LightVehicle             | Standard          |
| Harvester speed        | 0.35 cells/sec           | Standard          |
| Harvester capacity     | 500 Cordite              | Standard          |
| Reactor cost           | 1000 Cordite             | Standard          |
| Reactor VC rate        | 5 VC/sec                 | Standard          |
| Refinery HP            | 2250 HP                  | +50% vs base 1500 |
| Refinery turret        | Built-in point-defense   | Unique to Ironmarch|
| Refinery passive income| 0 Cordite/sec            | Standard          |
| Supply cap             | 210                      | +5% (+10 supply)  |
| Max Depots             | 10                       | Standard          |

**Income per harvester (medium node):** ~83 Cordite/min  
**Income per harvester (close node):** ~126 C/min

**Net Effect:** Ironmarch's Refineries resist raiding far better than any other faction. The built-in point-defense turret threatens light infantry and scout vehicles that attempt harvester harassment. With 2250 HP, a Refinery survives a full artillery volley or skirmish assault long enough for defenders to respond.

**Macro Strategy:**
- Expand confidently — economic disruption is costly for opponents.
- Lean into forward expansion; fortified Refineries serve as mini-outposts.
- Push up supply cap quickly (+5% base helps) to maximize the ground/defense hybrid army.

---

### 3.6 Stormrend (Air + Ground Offense)

Stormrend harvesters are fast but low-capacity, and Reactors are expensive — designed around aggressive play to compensate.

| Modifier               | Value                    | vs. Base          |
|------------------------|--------------------------|-------------------|
| Harvester class        | LightVehicle             | Standard          |
| Harvester speed        | 0.35 cells/sec           | Standard          |
| Harvester capacity     | 400 Cordite              | −20% (80% of 500) |
| Reactor cost           | 1250 Cordite             | +25%              |
| Reactor VC rate        | 5 VC/sec                 | Standard          |
| Refinery passive income| 0 Cordite/sec            | Standard          |
| Supply cap             | 200                      | Standard          |
| Max Depots             | 10                       | Standard          |

**Income per harvester (medium node):** ~66 Cordite/min  
**Income per harvester (close node):** ~101 C/min

**Net Effect:** Stormrend's economic model is intentionally fragile. Lower capacity means more frequent (but smaller) deliveries, and the expensive Reactor delays VC access. Stormrend compensates by pressure — their offensive units cost less and train faster (unit data), designed to cripple enemy economies before the late-game penalty manifests.

**Macro Strategy:**
- Attack fast. Raiding enemy harvesters equalizes the economic disadvantage.
- Avoid prolonged games — Stormrend's late-game economy is the weakest of all six.
- Build Reactor later; Stormrend's Tier 1 roster is powerful enough to win before VC is needed.

---

## 4. Building Costs & Stats — All Factions

All costs are in Cordite unless "(VC)" is specified. Build times are in seconds. HP values are base; no faction has building armor bonuses except Ironmarch Refinery (noted separately).

### 4.1 Shared Building Reference Table

| Building Type    | Cost (C) | Cost (VC) | HP    | Build Time | Supply | VC Gen | Passive Income |
|------------------|----------|-----------|-------|------------|--------|--------|----------------|
| Command Center   | Free     | 0         | 3000  | Pre-placed  | +10    | 0      | 0              |
| Refinery         | 1500     | 0         | 1500  | 25s         | 0      | 0      | 0 (Bastion: 15/s)|
| Reactor          | 1000     | 0         | 800   | 20s         | 0      | 5/s    | 0              |
| Supply Depot     | 500      | 0         | 600   | 15s         | +20    | 0      | 0              |
| Barracks         | 800      | 0         | 1000  | 20s         | 0      | 0      | 0              |
| Vehicle Factory  | 1500     | 100       | 1500  | 30s         | 0      | 0      | 0              |
| Airfield         | 2000     | 200       | 1200  | 35s         | 0      | 0      | 0              |
| Tech Lab         | 1500     | 200       | 800   | 30s         | 0      | 0      | 0              |

### 4.2 Faction Overrides

| Building  | Faction    | Cost (C) | Cost (VC) | HP    | Special Override                     |
|-----------|------------|----------|-----------|-------|--------------------------------------|
| Reactor   | Valkyr     | 800      | 0         | 800   | −20% cheaper                        |
| Reactor   | Bastion    | 1000     | 0         | 800   | VC gen +7/sec instead of 5           |
| Reactor   | Arcloft    | 1000     | 0         | 800   | VC gen +6/sec instead of 5           |
| Reactor   | Stormrend  | 1250     | 0         | 800   | +25% more expensive                  |
| Refinery  | Bastion    | 1500     | 0         | 1500  | Passive +15 Cordite/sec              |
| Refinery  | Ironmarch  | 1500     | 0         | 2250  | +50% HP, built-in point-defense turret|

### 4.3 Per-Faction Building Names

| Building Type    | Valkyr            | Kragmore         | Bastion           | Arcloft           | Ironmarch         | Stormrend          |
|------------------|-------------------|------------------|-------------------|-------------------|-------------------|--------------------|
| Command Center   | Aerie Command     | War Command      | Citadel Core      | Sky Citadel       | Field HQ          | Storm Command      |
| Refinery         | Sky Refinery      | Ore Refinery     | Shield Refinery   | Cloud Refinery    | March Refinery    | Lightning Refinery |
| Reactor          | Voltaic Spire     | Kragmore Reactor | Bastion Reactor   | Arc Reactor       | March Reactor     | Storm Reactor      |
| Supply Depot     | Aerie Depot       | Supply Bunker    | Bastion Depot     | Arcloft Depot     | March Depot       | Storm Depot        |
| Barracks         | Wing Barracks     | Drill Barracks   | Guard Barracks    | Sentinel Barracks | Ironmarch Barracks| Blitz Barracks     |
| Vehicle Factory  | Valkyr Motor Pool | Kragmore Foundry | Bastion Armory    | Arcloft Workshop  | Iron Foundry      | Storm Garage       |
| Airfield         | Valkyr Airstrip   | Kragmore Airfield| Bastion Airfield  | Arcloft Skyport   | Ironmarch Airfield| Storm Airstrip     |
| Tech Lab         | Aerie Research    | Kragmore Lab     | Bastion Tech Lab  | Arcloft Tech Lab  | Ironmarch Lab     | Storm Lab          |

---

## 5. Faction-Unique Buildings

Each faction has one (or more) unique structures that define their strategic identity.

| Faction    | Unique Building     | Cost (C) | Cost (VC) | HP   | Category      | Effect                                              |
|------------|---------------------|----------|-----------|------|---------------|-----------------------------------------------------|
| Valkyr     | Carrier Pad         | 3500     | 600       | 2000 | Superweapon   | Produces the Carrier superunit; requires Tech Lab    |
| Kragmore   | War Forge           | 2500     | 400       | 2000 | Tech           | Unlocks Mammoth heavy tank; requires Vehicle Factory |
| Bastion    | Command Hub         | 2200     | 300       | 1800 | Defense        | Extends defense network; grants passive scan radius  |
| Bastion    | Aegis Generator     | 3500     | 600       | 1500 | Superweapon   | Generates faction-wide damage reduction field        |
| Arcloft    | Overwatch Tower     | 1500     | 150       | 900  | Defense/Utility| Grants Overwatch Zone control in radius             |
| Ironmarch  | Forward Assembly    | 1000     | 100       | 1200 | Utility        | Deploys at FOB; enables production at the front line |
| Stormrend  | Storm Capacitor     | 2000     | 300       | 1000 | Utility        | Charges Momentum resource for Stormrend abilities    |

### Design Notes on Unique Buildings

- **Valkyr Carrier Pad:** The Carrier is the most expensive unit in the game. The Pad has a long build time (90s) and requires both a Tech Lab and a Voltaic Spire. Gate behind significant investment to prevent rush strategies.
- **Kragmore War Forge:** Acts as a secondary tech gate. Must build Vehicle Factory first. The Mammoth costs 4000C / 700VC and 10 pop — building one should be a match-defining moment.
- **Bastion Command Hub + Aegis Generator:** Two-stage progression. The Hub is the prerequisite for the Aegis Generator. The passive scan radius from the Hub makes Bastion players very hard to ambush.
- **Arcloft Overwatch Tower:** Cheap and early (no prerequisites beyond Barracks). This is Arcloft's map-control tool — towers deny area and feed sensor data. Can be spammed on choke points.
- **Ironmarch Forward Assembly:** Requires Field HQ only. This is what makes Ironmarch's "push and fortify" strategy viable — building a functioning production facility near the front without returning to base.
- **Stormrend Storm Capacitor:** Charges over time passively or from kills. The Momentum resource powers Stormrend's aggressive ability cooldowns. Losing the Capacitor resets accumulated Momentum.

---

## 6. Tech Tier Progression

Tech tiers define when units and buildings become available. All factions share this tier structure, though the specific unit pools differ.

### Tier Summary

| Tier | Unlock Requirement              | Available Content                                                         | Cordite Cost to Enter |
|------|---------------------------------|--------------------------------------------------------------------------|----------------------|
| 0    | Game start (HQ only)            | HQ, Refinery, Supply Depot, scout units, harvesters                      | Free                 |
| 1    | Barracks + Vehicle Factory      | Full infantry roster, light vehicles, APCs, Reactor                      | ~2300 C              |
| 2    | Tech Lab                        | Medium vehicles, helicopters, basic upgrades, VC-gated units             | +1500 C, 200 VC      |
| 3    | Airfield + Tech Lab (upgraded)  | Jets, artillery, heavy vehicles, advanced upgrades                       | +2000 C, 200 VC      |
| 4    | Faction superweapon building    | Faction-specific superunit, endgame abilities                            | +3500 C, 600 VC      |

### Tier 0 — Starting State

Every faction begins with their Command Center fully operational. The Command Center produces the faction's harvester unit and a basic scout unit. No other production is available until Refinery and Depot are built.

**Recommended build order for all factions:**
1. HQ → Harvester (auto-queue)
2. Refinery (1500C)
3. Supply Depot (500C) — or second Harvester depending on faction
4. Barracks (800C) — moves to Tier 1

### Tier 1 — Combat Begins

Barracks enables basic infantry. Vehicle Factory enables light vehicles and APCs. These are the workhorses of early skirmishes. Reactor is buildable at Tier 1, though most players delay it to Tier 1.5 (after establishing army presence).

**Minimum Tier 1 investment:** ~2300 Cordite (Barracks 800 + Factory 1500)  
**Time to reach (standard income):** approximately 60–90 seconds from game start, assuming 1 harvester running.

### Tier 2 — Technology Divergence

The Tech Lab (1500C, 200 VC) marks the true faction divergence point. Tier 2 units require VC, meaning players who haven't built a Reactor are effectively locked out. Medium tanks and helicopter gunships appear at Tier 2.

**Gate requirement:** Must have Barracks and/or Vehicle Factory active before Tech Lab can be placed (except Arcloft, who gains early access via Overwatch Tower path).

### Tier 3 — Air Power & Artillery

The Airfield (2000C, 200 VC) unlocks jets and higher-tier aircraft. Artillery and superweapon-prerequisite buildings unlock here. Most games do not reach Tier 3 fully — doing so is a deliberate choice to invest heavily in production infrastructure over army mass.

### Tier 4 — Superweapon

Faction-specific superweapon buildings are the ultimate tech milestone. Reaching Tier 4 in a 12–25 minute game requires sustained economic dominance. Superunits cost so much that their construction is often the "win condition" signal — if a player is building the Carrier Pad, the enemy must respond immediately or accept defeat.

---

## 7. Income Projection Tables

All values are **Cordite per minute** total, combining harvester income across all active harvesters. Refinery passive income (Bastion only) is added separately.

### 7.1 Valkyr Income (Helicopter, 350 cap, 0.30 speed)

| Harvesters | Close Node (10c) | Medium Node (20c) | Far Node (35c) |
|------------|------------------|-------------------|----------------|
| 1          | 97 C/min         | 72 C/min          | 50 C/min       |
| 2          | 194 C/min        | 144 C/min         | 100 C/min      |
| 3          | 291 C/min        | 216 C/min         | 150 C/min      |
| 4          | 388 C/min        | 288 C/min         | 200 C/min      |

### 7.2 Kragmore Income (HeavyVehicle, 1000 cap, 0.10 speed)

| Harvesters | Close Node (10c) | Medium Node (20c) | Far Node (35c) |
|------------|------------------|-------------------|----------------|
| 1          | 92 C/min         | 50 C/min          | 30 C/min       |
| 2          | 184 C/min        | 100 C/min         | 60 C/min       |
| 3          | 276 C/min        | 150 C/min         | 90 C/min       |

> Kragmore rarely runs more than 2 harvesters simultaneously given the cost per unit. 3 harvesters represents total economic commitment.

### 7.3 Bastion Income (LightVehicle, 500 cap, 0.35 speed + passive Refinery)

| Harvesters | Refineries | Close Node | Medium Node | Far Node   |
|------------|------------|------------|-------------|------------|
| 1          | 1          | 1026 C/min | 983 C/min   | 956 C/min  |
| 2          | 1          | 1152 C/min | 1066 C/min  | 1012 C/min |
| 2          | 2          | 1952 C/min | 1866 C/min  | 1812 C/min |
| 3          | 2          | 2078 C/min | 1949 C/min  | 1868 C/min |
| 3          | 3          | 2978 C/min | 2849 C/min  | 2768 C/min |

> Each Bastion Refinery adds 900 C/min (15/sec × 60) base passive income.

### 7.4 Arcloft Income (Helicopter, 500 cap, 0.30 speed)

| Harvesters | Close Node (10c) | Medium Node (20c) | Far Node (35c) |
|------------|------------------|-------------------|----------------|
| 1          | 138 C/min        | 102 C/min         | 71 C/min       |
| 2          | 276 C/min        | 204 C/min         | 142 C/min      |
| 3          | 414 C/min        | 306 C/min         | 213 C/min      |

> Arcloft harvesters are the most efficient per unit (full cap + fly-over). Their supply cap limits army size, not economy.

### 7.5 Ironmarch Income (LightVehicle, 500 cap, 0.35 speed)

| Harvesters | Close Node (10c) | Medium Node (20c) | Far Node (35c) |
|------------|------------------|-------------------|----------------|
| 1          | 126 C/min        | 83 C/min          | 56 C/min       |
| 2          | 252 C/min        | 166 C/min         | 112 C/min      |
| 3          | 378 C/min        | 249 C/min         | 168 C/min      |
| 4          | 504 C/min        | 332 C/min         | 224 C/min      |

### 7.6 Stormrend Income (LightVehicle, 400 cap, 0.35 speed)

| Harvesters | Close Node (10c) | Medium Node (20c) | Far Node (35c) |
|------------|------------------|-------------------|----------------|
| 1          | 101 C/min        | 66 C/min          | 45 C/min       |
| 2          | 202 C/min        | 132 C/min         | 90 C/min       |
| 3          | 303 C/min        | 198 C/min         | 135 C/min      |
| 4          | 404 C/min        | 264 C/min         | 180 C/min      |

---

## 8. Unit Cost Balance Principles

Unit costs follow a tiered philosophy: the lower the tier, the more accessible the unit (Cordite-only), and the higher the tier, the more VC becomes a gating resource.

### 8.1 Cost Brackets

| Unit Class            | Cordite Range     | VC Range   | Pop Cost | Tier    | Notes                                      |
|-----------------------|-------------------|------------|----------|---------|--------------------------------------------|
| Infantry              | 200–700 C         | 0 VC       | 1–2      | 0–1     | Backbone of early game. Never VC-gated.    |
| Light Vehicles        | 400–700 C         | 0 VC       | 1–2      | 0–1     | Scouts, bikes, light artillery. Fast.      |
| Medium Vehicles (APC) | 700–1100 C        | 0–50 VC    | 2–3      | 1–2     | APCs carry infantry. Low VC threshold.     |
| Medium Vehicles (Tank)| 1000–1400 C       | 50–100 VC  | 2–3      | 2       | Core tank roster. Moderate VC investment.  |
| Heavy Vehicles        | 1400–2200 C       | 100–300 VC | 3–5      | 2–3     | Heavies and super-heavies.                 |
| Aircraft (Helicopter) | 700–1800 C        | 0–200 VC   | 2–4      | 1–3     | Lower-tier helos have no VC cost.          |
| Aircraft (Jet)        | 1000–2800 C       | 50–400 VC  | 2–6      | 2–3     | Jets always require some VC.               |
| Super Units           | 3500–4500 C       | 600–800 VC | 8–10     | 4       | Win-condition units. Extremely rare.       |
| Static Defenses       | 200–3500 C        | 0–600 VC   | 0        | 0–4     | 0 pop cost; balanced by build time.        |

### 8.2 Pricing Philosophy

**Accessibility curve:** At Tier 0, a player earns ~80–100 C/min with one harvester. A basic infantry unit at 200C should be buildable within 2–3 minutes. The first combat unit must arrive on the field by 30–45 seconds, implying the HQ builds a free scout or the first unit is subsidized.

**VC as a luxury gate:** VC is not meant to block access to combat — it blocks access to elite combat. A Stormrend player with no Reactor should still have a fully viable Tier 1 army. VC-free options must remain competitive at their tier.

**Pop efficiency:** Heavy units cost more pop but must deliver proportional combat value. A 5-pop Heavy Tank should match approximately 5× a 1-pop infantry unit in relevant combat scenarios. The game should punish "pop stuffing" (filling supply with cheap units) as well as "pop dumping" (running one super unit at 10 pop).

**Static defense 0-pop rule:** Defenses take up zero population to encourage defensive players to build them without supply anxiety. Their gate is build time and cost, not pop cap.

### 8.3 Cost/Pop Efficiency Reference

| Tier  | Target C/pop ratio | Target VC/pop ratio |
|-------|--------------------|----------------------|
| 1     | 200–350 C/pop      | 0 VC/pop             |
| 2     | 400–500 C/pop      | 25–50 VC/pop         |
| 3     | 500–600 C/pop      | 75–150 VC/pop        |
| 4     | 400–500 C/pop      | 70–90 VC/pop (super) |

> Super units have a lower C/pop ratio because VC is the true gate — their pop cost reflects strategic weight, not just economic cost.

---

## 9. Game Timing Targets

These targets define the intended experience for a 1v1 game on a standard medium map. All times are approximate and faction-adjusted.

| Milestone                        | Target Time      | Notes                                                        |
|----------------------------------|------------------|--------------------------------------------------------------|
| Game start / harvester deployed  | 0:00             | Harvester auto-deploys from HQ or is pre-placed.             |
| First Refinery complete          | 0:25–0:40        | Refinery build time 25s. Must be queued immediately.         |
| First combat unit produced       | 0:30–0:45        | HQ or Barracks must produce a unit in this window.           |
| First scouting contact           | 1:00–1:30        | Scout units must reach opponent's base perimeter.            |
| Barracks complete                | 1:00–1:30        | Assuming ~800C in hand at ~0:50 (early harvester + start C). |
| First skirmish / harass          | 2:00–3:00        | Light vehicle or infantry skirmish at contested node.        |
| Vehicle Factory complete         | 2:30–3:30        | Unlocks APCs, opens Tier 1 transition.                       |
| First Reactor complete           | 4:00–5:30        | Players delaying Reactor spend this time on army instead.    |
| First VC-requiring unit          | 5:00–7:00        | Must have Reactor + accumulated stockpile (≥100 VC).         |
| Tech Lab complete                | 5:00–6:30        | Opens Tier 2. Requires 200 VC on completion.                 |
| Mid-game army peak               | 10:00–15:00      | Both players near supply cap, multiple nodes contested.      |
| Airfield complete (if pursued)   | 7:00–10:00       | Jets and high-tier aircraft come online here.                |
| Superweapon building starts      | 13:00–17:00      | Requires dominant economy or sacrifice of army investment.   |
| Superweapon unit available       | 15:00–20:00      | 90s+ build time for the superunit after building is up.      |
| Typical game length              | 12:00–25:00      | Median target 18 minutes. Below 10 is a rush imbalance.      |

### Timing Red Flags

- **Game ends before 8 minutes:** Likely rush imbalance. One faction's early aggression units are too strong, or defensive options are too weak at Tier 0.
- **No VC units appear before 10 minutes:** Economy scaling too slow, or VC costs too high. Tech Lab build cost may need reduction.
- **Superweapon before 13 minutes:** Either harvester income is too high or superweapon costs are too low.
- **Game drags past 30 minutes:** Supply cap is too high, or units aren't strong enough to break entrenched positions. Consider increasing super unit impact or late-game damage scaling.

---

## 10. Balance Design Notes

### 10.1 Economy Diversity Goals

The six factions are designed to create meaningfully different economic interactions in matchups:

| Matchup                | Economic Dynamic                                                                 |
|------------------------|---------------------------------------------------------------------------------|
| Valkyr vs. Kragmore    | Valkyr harries Kragmore's slow harvesters; Kragmore's single trip worth 2.8× more.|
| Bastion vs. Stormrend  | Stormrend must kill Bastion before passive income compounds. Rush or lose.      |
| Arcloft vs. Ironmarch  | Ironmarch resists economic harassment; Arcloft can't out-army, must out-tech.   |
| Kragmore vs. Ironmarch | Mirror ground factions — income is comparable, difference is survivability.     |
| Valkyr vs. Arcloft     | Both fly-over harvesters; economy nearly equal, resolved by unit quality.       |
| Bastion vs. Ironmarch  | Both defensive — likely long games. Bastion's passive income usually wins.      |

### 10.2 Harvester Raiding Balance

Raiding is a valid strategy for all six factions, but the payoff varies:

- **Killing a Valkyr harvester:** 350C lost to enemy, plus trip time. Moderate damage.
- **Killing a Kragmore harvester:** 1000C lost to enemy, often mid-trip. Devastating.
- **Killing an Ironmarch harvester:** Same as base but the Refinery point-defense may kill the raider.
- **Killing a Bastion harvester:** Moderate (Refinery passive income continues regardless).

This creates natural strategic risk gradients: raids on Kragmore are high-reward, raids on Bastion are low-reward.

### 10.3 VC Economy Tiers

At max Reactor output (5 VC/sec, cap 500), a player who builds a Reactor at 4:30 will have a full 500 VC stockpile by 6:10 (100 seconds of accumulation). This aligns with the first VC-unit timing target of 5:00–7:00.

Bastion's 7 VC/sec means full cap in ~71 seconds — they'll have VC units available ~30 seconds earlier. Stormrend's expensive Reactor (1250C) means they build it ~45 seconds later, further compressing their VC access window.

### 10.4 Cordite Drain Targets

The following table shows how long a starting income sustains construction of the standard opening:

| Opening Build          | Total Cost | Time to Afford (1 harvester, medium node) |
|------------------------|------------|-------------------------------------------|
| Refinery               | 1500 C     | ~18 min alone — must use starting credits |
| Refinery + Depot       | 2000 C     | Starting credits ~500C; need ~1:45 harvest |
| Refinery + Barracks    | 2300 C     | ~2:00 harvest + starting credits          |
| Full Tier 1 foundation | 4300 C     | ~3:30–4:00 with 2 harvesters              |

**Starting credit recommendation:** Each faction should begin with 400–600 Cordite to allow immediate Refinery construction without waiting. The Refinery's 25-second build time means queuing it instantly is critical to economy ramp-up.

### 10.5 Supply Cap Interaction with Economy

The supply cap creates a natural ceiling on army size that interacts with economy pacing. At 200 supply:
- A fully-stocked Tier 1 army (1-pop infantry, 2-pop light vehicles) could field 66–100 units.
- A mixed Tier 2 army (2–3 pop per unit) fields 40–66 units.
- A late-game Tier 3 army (3–5 pop) fields 20–40 units.

Economy surplus in late game (when army cap is hit) should flow into building upgrades, additional Reactors, and eventually superweapon infrastructure. Players who hit supply cap and have no tech investment have "wasted" economic potential — a valid punishable state.

---

*End of Economy Balance Document v0.1*  
*Cordite Wars: Six Fronts — Internal Design Reference*
