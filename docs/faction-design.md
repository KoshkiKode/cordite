# Faction Design & Unit Rosters

> **Document status:** First draft — v0.1  
> **Last updated:** 2026-04-01  
> **Purpose:** Creative bible for the faction system. Drives data files in `data/factions/`, `data/units/`, and `data/buildings/`.

---

## Table of Contents

1. [Design Philosophy](#design-philosophy)
2. [Shared Economy Model](#shared-economy-model)
3. [Movement Classes Reference](#movement-classes-reference)
4. [Faction Identities](#faction-identities)
   - [1. Valkyr — Air Primary](#1-valkyr--air-primary)
   - [2. Kragmore — Ground Primary](#2-kragmore--ground-primary)
   - [3. Bastion — Defense Primary](#3-bastion--defense-primary)
   - [4. Arcloft — Air + Defense Hybrid](#4-arcloft--air--defense-hybrid)
   - [5. Ironmarch — Ground + Defense Hybrid](#5-ironmarch--ground--defense-hybrid)
   - [6. Stormrend — Air + Ground Offense Hybrid](#6-stormrend--air--ground-offense-hybrid)
5. [Unit Rosters](#unit-rosters)
6. [Faction Matchup Matrix](#faction-matchup-matrix)
7. [Balance Notes & Open Questions](#balance-notes--open-questions)

---

## Design Philosophy

Every faction must satisfy three rules:

1. **Identity in 10 seconds.** A spectator watching a replay should be able to identify the faction within 10 seconds of seeing a base or army. Silhouettes, colours, movement patterns, and army composition must all be distinct.

2. **No dominant strategy.** Every faction has at least one matchup where they're the underdog. There are no S-tier factions—only S-tier players who understand their faction's strengths.

3. **Asymmetric, not unequal.** Factions differ in *kind*, not in *power*. A faction with fewer units compensates with stronger individual units or unique mechanics. Total army value at equal economy should be roughly equivalent across factions.

### The Three Pillars

| Pillar | Core Fantasy | Strength | Weakness |
|--------|-------------|----------|----------|
| **Air** | Speed, precision, vertical control | Mobility, map vision, surgical strikes | Fragile ground, expensive, AA-vulnerable |
| **Ground** | Mass, momentum, overwhelming force | Durability, sustained DPS, area denial | Slow, air-vulnerable, terrain-dependent |
| **Defense** | Control, attrition, economic advantage | Fortifications, economy scaling, area lockdown | Immobile, weak offense, vulnerable to siege |

### Archetype Matrix

|  | Air | Ground | Defense |
|--|-----|--------|---------|
| **Primary** | Valkyr | Kragmore | Bastion |
| **+ Defense** | Arcloft | Ironmarch | — |
| **+ Air** | — | Stormrend | Arcloft |
| **+ Ground** | Stormrend | — | Ironmarch |

---

## Shared Economy Model

All factions share the same underlying resource system. Faction-specific modifications sit on top.

### Primary Resource: **Cordite**

- Gathered from **Cordite Nodes** scattered across the map (typically 4–8 per map, with 2 near each starting position).
- Each node contains a finite amount (default: 10,000 Cordite). Nodes deplete and must be replaced by expanding to new ones.
- Harvested by **Harvester** units that travel between the node and a **Refinery** building.
- Harvester round-trip time is the primary economic bottleneck. Shorter distance = faster income.
- Base income rate (no modifiers): ~80 Cordite/sec per active harvester on a medium-distance node.

### Secondary Resource: **Voltaic Charge** (VC)

- Generated passively by **Reactor** buildings. Each Reactor produces +5 VC/sec.
- VC is required for advanced units, upgrades, and superweapons. Basic units cost only Cordite.
- VC does not stockpile beyond a cap (default: 500 VC). "Use it or lose it" — encourages spending on tech.
- Destroying enemy Reactors cripples their tech production without affecting their basic army.

### Tertiary Mechanic: **Supply Limit**

- Each faction has a population cap (default: 200 supply).
- Units consume supply proportional to their power level (infantry = 1, tanks = 3–5, superunits = 10+).
- Supply is increased by building **Depots** (each Depot adds +20 supply, max 10 Depots = 200 cap).

### Faction Economy Modifiers

| Faction | Cordite Modifier | VC Modifier | Supply Modifier | Economy Style |
|---------|-----------------|-------------|-----------------|---------------|
| **Valkyr** | Harvesters are fast (Helicopter-class) but carry less per trip | Reactors cost less | Standard | Fast but thin — high-APM economy that rewards constant attention |
| **Kragmore** | Harvesters are slow HeavyVehicle-class but carry 2x per trip | Standard Reactors | +10% supply cap (220 max) | Slow and steady — fewer trips, bigger payloads |
| **Bastion** | Standard harvesters, but Refineries generate +15 Cordite/sec passively | Reactors produce +2 VC/sec (7 total) | Standard | Passive income supplements — turtling pays off |
| **Arcloft** | Helicopter harvesters (like Valkyr) with standard capacity | Reactors produce +1 VC/sec (6 total) | -10% supply cap (180 max) | Air-based gathering is safe but army is smaller |
| **Ironmarch** | Standard harvesters, Refineries have +50% HP and built-in turret | Standard | +5% supply cap (210 max) | Fortified economy — hard to raid |
| **Stormrend** | Harvesters are LightVehicle-class (very fast, fragile) | Reactors cost +25% more | Standard | Aggressive economy — fast but raid-vulnerable |

---

## Movement Classes Reference

Pulled from `MovementProfile.cs`. All unit designs reference these canonical classes.

| Class | Footprint | Domain | Speed Tier | Slope Limit | Key Traits |
|-------|-----------|--------|------------|-------------|------------|
| Infantry | 1×1 | Ground | Slow (0.15 u/t) | 50° | Goes anywhere, light mass, crushable |
| LightVehicle | 2×2 | Ground | Fast (0.35 u/t) | 35° | Bouncy, high ground clearance, road bonus |
| HeavyVehicle | 3×3 | Ground | Very Slow (0.10 u/t) | 20° | Stiff, needs flat terrain, crushes infantry |
| APC | 2×2 | Ground | Medium (0.20 u/t) | 30° | Crushes infantry, decent all-terrain |
| Tank | 2×2 | Ground | Med-Heavy (0.18 u/t) | 28° | Crushes infantry, tracked traction |
| Artillery | 3×3 | Ground | Crawl (0.06 u/t) | 15° | Needs flat ground, road-dependent, massive |
| Helicopter | 2×2 | Air (Low) | Fast (0.30 u/t) | N/A | Ignores terrain, 2×2 collision box |
| Jet | 1×1 | Air (High) | Fastest (0.50 u/t) | N/A | Ignores everything, wide turning radius |

**Static** — Used for buildings, turrets, walls, mines. 0×0 movement (placed on grid, never moves).

---

## Faction Identities

---

### 1. Valkyr — Air Primary

**Archetype:** Air Primary  
**Codename:** VALKYR  
**Data path:** `data/factions/valkyr/`

#### Lore Summary
The Valkyr Ascendancy abandoned the scarred earth generations ago, building their civilization on airborne carrier platforms and mountain-top aeries. They view ground warfare as barbaric and wasteful. Their technology revolves around anti-gravity, precision optics, and lightweight alloys.

#### Playstyle Description
Playing Valkyr feels like controlling a swarm of wasps. You're everywhere and nowhere — hitting supply lines, sniping key buildings, dodging AA fire. Your ground forces exist only to hold captured points long enough for reinforcements to fly in. You never fight fair; if the enemy is ready for you, you're already gone. High APM required — idle aircraft are wasted aircraft.

#### Strategic Strengths
- **Air supremacy:** Best aircraft in the game in both variety and quality
- **Map mobility:** Can strike anywhere on the map within seconds
- **Vision advantage:** Fastest scouts, extended radar range via flying units
- **Surgical strikes:** Precision bombers can delete key structures without a ground assault
- **Terrain immunity:** Army largely ignores terrain restrictions

#### Strategic Weaknesses
- **Fragile ground presence:** Infantry and ground vehicles are the worst in the game
- **AA vulnerability:** Concentrated AA fire shreds the entire army composition
- **Expensive units:** Individual aircraft cost 30–50% more than equivalent ground units
- **No staying power:** Cannot hold ground against a determined ground push
- **Fuel/ammo mechanic:** Jets must return to airfields to rearm (see faction mechanic)

#### Economy Style
Fast and thin. Helicopter-based harvesters move quickly but carry small loads. Requires constant economic attention and map control to maintain income. Cheap Reactors allow early tech access, but the expensive unit roster drains resources fast. Valkyr games tend to be short — they win before the opponent can max out.

#### Aesthetic Direction
**Sleek, angular, white-and-cyan.** Aircraft have swept-forward wings and visible thruster glow. Ground units look like afterthoughts — repurposed aircraft frames with bolted-on legs/wheels. Buildings are elevated platforms and landing pads. Everything has a "lifted off the ground" feel, with anti-grav shimmer effects under structures.

#### Faction Mechanic: **Sortie System**
Valkyr jets do not linger over the battlefield. They operate on a **sortie** model:
- **Jets** launch from an **Airstrip**, perform their attack run, then must return to rearm. They have a limited loiter time (15–25 seconds depending on jet type).
- Destroying Airstrips strands jets — they can make one more pass then auto-land (becoming vulnerable ground targets).
- **Helicopters** are exempt from sorties (they hover freely) but are slower and less lethal than jets.
- Skilled players chain sortie timing to maintain constant pressure. Novice players leave gaps that get punished.
- **Upgrade: Extended Fuel Tanks** — doubles loiter time at the cost of reduced speed.

---

### 2. Kragmore — Ground Primary

**Archetype:** Ground Primary  
**Codename:** KRAGMORE  
**Data path:** `data/factions/kragmore/`

#### Lore Summary
The Kragmore Collective emerged from deep mining colonies, where survival demanded heavy machinery and an unshakable collective discipline. Their doctrine is simple: what cannot be outfought can be outweighed. They build big, march slow, and hit like a landslide.

#### Playstyle Description
Playing Kragmore feels like driving a freight train. Early game is agonizingly slow — your tanks lumber, your economy crawls, and aggressive opponents will test you. But once the Kragmore engine reaches critical mass, it's nearly unstoppable. You build an army that fills the screen, march it forward, and dare anyone to stand in the way. The satisfaction comes from the *crunch* — that moment when your wall of armor rolls over the enemy base and there's simply nothing they can do.

#### Strategic Strengths
- **Overwhelming ground force:** Most durable tanks, most devastating artillery
- **Infantry variety:** Best infantry roster — specialized squads for every situation
- **Crush mechanic:** Heavy units physically destroy enemy infantry and light structures
- **Terrain control:** Artillery denies entire map zones; tanks push through defenses
- **Late-game scaling:** Army value scales harder than any other faction past mid-game

#### Strategic Weaknesses
- **Air vulnerability:** Worst anti-air in the game — limited to a mediocre AA truck and infantry missiles
- **Slow everything:** Slowest expansion, slowest army, slowest tech-up
- **Terrain dependency:** Heavy vehicles need roads and flat ground — mountains and mud cripple Kragmore
- **No precision:** Can't surgically remove key targets — has to brute-force through everything
- **Supply hungry:** Big army = constant supply depot construction

#### Economy Style
Slow but massive. HeavyVehicle harvesters carry enormous loads but move at a crawl. Kragmore wants short harvesting distances and builds refineries close to nodes. The +10% supply cap allows bigger armies, and their units are moderately priced but consume high supply. Kragmore is the only faction that regularly hits the supply cap and wishes for more.

#### Aesthetic Direction
**Industrial, brutal, grey-and-orange.** Think Soviet-era heavy machinery crossed with mining equipment. Tanks have exposed treads, riveted plates, and belching exhaust. Infantry wear bulky hazmat-style suits. Buildings are squat concrete bunkers with smokestacks. Everything is covered in dust and grime. Satisfying visual weight — when a Kragmore tank rolls onto screen, you *feel* it.

#### Faction Mechanic: **Horde Protocol**
Kragmore units gain power when grouped together:
- **Horde Bonus:** When 5+ Kragmore ground units are within a small radius (10 cells), they gain +10% damage and +10% armor. At 10+ units, this increases to +20%/+15%. At 20+, it caps at +25%/+20%.
- This incentivizes deathball composition and discourages splitting your army.
- The bonus only applies to **ground units** (not aircraft).
- **Visual indicator:** Clustered units display synchronized marching animations and a faint amber glow.
- **Counter-play:** Splash damage, area denial, and forced splits (multi-prong attacks) break the horde bonus.
- **Upgrade: Commissar Training** — Reduces the horde threshold (bonus kicks in at 3/7/15 instead of 5/10/20).

---

### 3. Bastion — Defense Primary

**Archetype:** Defense Primary  
**Codename:** BASTION  
**Data path:** `data/factions/bastion/`

#### Lore Summary
The Bastion Protectorate believes the world is fundamentally hostile and that survival demands layers of protection. Their engineers are the finest fortification architects in existence, and their economy is built on efficiency — extracting maximum value from minimal territory. They don't conquer. They endure.

#### Playstyle Description
Playing Bastion feels like building a puzzle. Every turret placement matters, every wall angle creates a kill zone, every mine cluster forces the enemy into your firing lanes. You're not trying to kill the enemy — you're trying to make them kill themselves against your defenses. The satisfaction comes from watching a massive enemy assault dissolve into wreckage at the gates of your fortress. Offense is your weakness: every push outside your defense perimeter is a calculated risk.

#### Strategic Strengths
- **Unmatched static defense:** Best turrets, walls, mines, and bunkers in the game
- **Economic efficiency:** Passive income and superior Reactors mean you out-earn equal-sized opponents over time
- **Attrition mastery:** Every fight near your base is heavily in your favor
- **Repair and rebuild:** Fastest structure repair speed; buildings self-heal slowly
- **Upgrade depth:** More defensive upgrades than any other faction — walls get tougher, turrets gain range, etc.

#### Strategic Weaknesses
- **Glacial offense:** Worst offensive unit roster — mediocre at everything outside the base
- **Expansion cost:** Every expansion requires rebuilding the entire defense network
- **Mobility crisis:** Virtually no fast units; army is painfully slow
- **Siege vulnerability:** Long-range artillery outranges turrets and demolishes walls
- **Map control:** Ceding map control means ceding resource nodes, slowly starving

#### Economy Style
Passive and efficient. Standard harvesters, but Refineries generate passive Cordite income even without active harvesting. Superior Reactors (7 VC/sec vs. standard 5) accelerate tech. Bastion wins the long game — if left alone, they'll out-tech and out-produce everyone. The catch is that defending an economy is expensive in itself (turrets aren't free).

#### Aesthetic Direction
**Geometric, layered, grey-and-gold.** Structures are hexagonal or octagonal with visible armored plating and recessed weapon ports. Think star-fort architecture meets sci-fi bunker. Walls are thick and modular — you can see the layered construction. Units are boxy and utilitarian — function over form. Gold accent lighting marks active defenses, dimming when unpowered.

#### Faction Mechanic: **Fortification Network**
Bastion structures grow stronger when connected:
- **Network Bonus:** Turrets and walls within 8 cells of a **Command Hub** (Bastion's unique building) gain +15% HP, +10% damage, and +20% repair speed.
- Multiple Command Hubs extend the network — but each Hub is expensive and high-priority target.
- **Powered vs. Unpowered:** Turrets outside the network function at 60% effectiveness. Destroying the Hub cripples an entire defense sector.
- **Network Visualizer:** Connected structures show glowing conduit lines between them on the ground.
- **Upgrade: Redundant Systems** — Turrets retain 80% effectiveness (instead of 60%) when disconnected from the network.
- **Counter-play:** Surgical strikes on Command Hubs (via air or artillery) weaken entire defense sectors at once.

---

### 4. Arcloft — Air + Defense Hybrid

**Archetype:** Air + Defense  
**Codename:** ARCLOFT  
**Data path:** `data/factions/arcloft/`

#### Lore Summary
The Arcloft Sovereignty controls the skies above and the vaults below. Their doctrine pairs aerial dominance with impenetrable ground installations — the sky is their blade, the fortress is their shield. They rule from floating citadels ringed with flak batteries and launch bays.

#### Playstyle Description
Playing Arcloft feels like being a chess player who controls the board. Your base is a no-fly zone backed by the best AA in the game. Your aircraft aren't as lethal as Valkyr's, but they don't need to be — they operate with the safety net of a fortified home to retreat to. You win by establishing air superiority over the map while making your base untouchable. Your weakness is on the ground between your base and theirs — you have no heavy armor, no siege, and any ground push feels anemic.

#### Strategic Strengths
- **Air superiority + AA defense:** Dominates the airspace both offensively and defensively
- **Safe base:** Fortified enough to survive raids without pulling units back
- **Map denial:** AA turrets at expansions make enemy air operations difficult
- **Flexibility:** Can switch between air offense and base defense fluidly
- **Counter-air specialist:** Hard-counters pure air factions

#### Strategic Weaknesses
- **No ground punch:** Zero heavy vehicles, no tanks, no artillery — cannot break fortified positions on the ground
- **Small army cap:** 180 supply cap means fewer total units than other factions
- **Hybrid tax:** Aircraft are 10–15% less effective than Valkyr equivalents; defenses are 10–15% weaker than Bastion
- **Expansion vulnerability:** Ground-based expansions require defense investment with no native ground army to protect them
- **Predictable:** Opponents know you'll attack from the air, so they can pre-build AA

#### Economy Style
Air-harvesting keeps resources flowing safely (hard to raid a helicopter harvester), but the smaller supply cap limits peak army size. Slightly superior Reactors give a tech edge. Arcloft aims to win in the mid-game through sustained air pressure while their base defenses buy time.

#### Aesthetic Direction
**Elevated, angular, blue-and-silver.** Structures are built on raised platforms with visible anti-grav supports. Heavy use of angled armor plating and sensor arrays. Aircraft have a more armored look than Valkyr's (thicker wings, visible plating). AA turrets are prominent and threatening — tall, multi-barrel emplacements. The overall vibe is "fortified airport."

#### Faction Mechanic: **Overwatch Protocol**
Arcloft can designate zones for automatic air defense:
- **Overwatch Zones:** The player can designate up to 3 circular zones on the map (radius ~15 cells each). Any idle Arcloft aircraft will automatically patrol the nearest Overwatch Zone.
- Aircraft in Overwatch mode gain **+15% detection range** and **+10% reaction speed** (reduced target acquisition time).
- Overwatch Zones are visible to the owning player as subtle holographic circles on the terrain.
- **Upgrade: Expanded Overwatch** — Increases zone limit to 5 and zone radius to 20 cells.
- **Counter-play:** Overwatch zones are defensive — aircraft in Overwatch won't pursue enemies outside the zone, making them predictable. Ground armies can bypass Overwatch zones entirely.

---

### 5. Ironmarch — Ground + Defense Hybrid

**Archetype:** Ground + Defense  
**Codename:** IRONMARCH  
**Data path:** `data/factions/ironmarch/`

#### Lore Summary
The Ironmarch Compact fights wars like they build roads — one mile at a time, paved in steel. Their doctrine is methodical advance: establish a position, fortify it, then push forward to the next one. They've never lost a war of attrition, and they've never won one quickly.

#### Playstyle Description
Playing Ironmarch feels like an unstoppable glacier. You don't rush — you *arrive*. You build a forward base, throw up turrets and walls, then inch your armor column to the next ridge line. Every position you take becomes permanent. The satisfaction is strategic: watching the map slowly fill with your color as the enemy's territory shrinks. Your nightmare is aircraft — you have almost nothing to shoot them down with, and watching helicopters pick apart your beautiful armored column is genuinely painful.

#### Strategic Strengths
- **Forward basing:** Can build turrets and walls anywhere on the map, not just at home base
- **Armored push:** Strong tanks backed by mobile turret deployment — hard to dislodge
- **Positional warfare:** Excels in chokepoints, narrow passes, and urban terrain
- **Attrition resilience:** Fortified positions mean losing ground units doesn't mean losing territory
- **Mid-game dominance:** Strongest faction in the 10–20 minute window when ground armies peak

#### Strategic Weaknesses
- **Catastrophically air-vulnerable:** Worst anti-air in the game — a single helicopter squadron can dismantle an Ironmarch push
- **Glacial speed:** Slowest repositioning — if you push the wrong lane, you've committed for minutes
- **Resource intensive:** Forward bases are expensive; overextension leads to bankruptcy
- **No flanking ability:** Everything moves in a straight line, slowly — easy to predict and kite
- **Late-game ceiling:** Bastion out-turtles them; Valkyr/Stormrend out-maneuvers them

#### Economy Style
Fortified and resilient. Refineries have extra HP and a built-in point-defense turret, making economic raids difficult. Slightly higher supply cap (210) supports the combined arms of ground army + forward structures. Ironmarch doesn't earn fast but is very hard to disrupt economically.

#### Aesthetic Direction
**Rugged, modular, olive-and-steel.** Think military forward operating base. Vehicles have bolt-on armor kits and camouflage netting. Structures are prefabricated — you can see the assembly seams and deployment latches. Forward turrets look hastily constructed but effective. Infantry wear heavy plate carriers and full-face helmets. The aesthetic says "we're here, we're dug in, and we're not leaving."

#### Faction Mechanic: **Forward Operations**
Ironmarch can build a mobile forward base:
- **FOB Truck:** A unique HeavyVehicle-class unit that deploys into a **Forward Operating Base** — a mini-base that allows constructing turrets, walls, and basic units in the field.
- The FOB has a build radius of 20 cells. Turrets and walls built within FOB radius cost 20% less.
- FOB provides a local supply depot (+10 supply) and a repair zone (units within 5 cells slowly regenerate HP).
- Maximum 2 FOBs active at once (in addition to the main base).
- FOBs take 15 seconds to deploy and 10 seconds to undeploy (vulnerable during transition).
- **Upgrade: Reinforced FOB** — FOB gains +50% HP and a built-in flak cannon (light AA).
- **Counter-play:** FOBs are visible and targetable. Destroying an FOB removes all turrets/walls built within its radius after a 10-second warning period (giving the player time to evacuate units).

---

### 6. Stormrend — Air + Ground Offense Hybrid

**Archetype:** Air + Ground Offense  
**Codename:** STORMREND  
**Data path:** `data/factions/stormrend/`

#### Lore Summary
The Stormrend Pact was forged by mercenary companies and breakaway military units who believed the best defense is an overwhelming offense. Their doctrine is blitz: hit hard, hit fast, hit everywhere at once. They don't build empires — they shatter them.

#### Playstyle Description
Playing Stormrend feels like controlling a thunderstorm. You're always attacking — probing, raiding, flanking, pushing. The moment you stop moving, you're losing, because your base defenses are paper and your economy is fragile. You win by keeping the opponent off-balance, forcing them to react to three threats at once while you consolidate on the one that worked. Games are short, violent, and decisive. If you haven't won by minute 15, you probably won't.

#### Strategic Strengths
- **Combined arms blitz:** Can attack with air and ground simultaneously — incredibly hard to defend against
- **Speed across the board:** Fastest ground units, fast (if not top-tier) aircraft
- **Raid capability:** Best economic harassment in the game — light vehicles and helicopters shred harvesters
- **Initiative:** Stormrend dictates the pace of the game; opponents are always reacting
- **Burst damage:** Can concentrate overwhelming firepower on a single point for devastating alpha strikes

#### Strategic Weaknesses
- **Paper-thin base:** Worst static defenses in the game — practically nonexistent turrets
- **Fragile units:** Speed comes at the cost of armor — units die fast in sustained fights
- **Economy dependency:** Fast harvesters are extremely raid-vulnerable (LightVehicle class = fragile)
- **No attrition capability:** Cannot win long, grinding fights — must win fast or lose
- **Supply inefficiency:** Combined arms army requires both air and ground infrastructure, splitting the budget

#### Economy Style
Aggressive and volatile. LightVehicle harvesters zip between nodes quickly but are tissue-paper fragile. Reactors cost more, so VC comes slower — Stormrend compensates by ending the game before VC-heavy units matter. The economy is feast-or-famine: when you're winning and raiding the enemy, it's great; when you're on the back foot, it collapses.

#### Aesthetic Direction
**Sharp, aggressive, red-and-black.** Vehicles have angular stealth-inspired profiles with exposed engines and glowing red thrust nozzles. Aircraft are compact and mean-looking — no wasted surface area. Infantry are lightly armored with speed harnesses and targeting visors. Buildings look temporary — drop-pod deployment bays and collapsible hangars. Everything looks like it was designed to be deployed fast and abandoned without regret.

#### Faction Mechanic: **Blitz Doctrine**
Stormrend gains power from sustained aggression:
- **Momentum Gauge:** A faction-wide gauge (0–100) that fills when Stormrend units deal damage to enemy units/buildings and drains when no damage is dealt for 5+ seconds.
- **Momentum Bonuses:**
  - 25+ Momentum: All units gain +5% move speed
  - 50+ Momentum: All units gain +10% move speed, +5% damage
  - 75+ Momentum: All units gain +15% move speed, +10% damage, +5% attack speed
  - 100 Momentum: Unlocks **Stormbreak** — a 15-second window where all units gain +25% speed, +15% damage, and halved production time on new units. Triggers automatically at 100 and resets to 0.
- Momentum decays at -3/sec when not dealing damage; it rises at a rate proportional to damage dealt.
- **Visual indicator:** A storm-energy VFX around Stormrend units intensifies with momentum level. At 100, a red lightning storm effect covers the area.
- **Counter-play:** Kiting, retreating, and forcing Stormrend to chase resets their momentum. Turtling and refusing to engage bleeds their gauge dry.

---

## Unit Rosters

### Stat Scale Reference

All relative stats use a 1–10 scale:

| Rating | Meaning |
|--------|---------|
| 1–2 | Terrible — this is a clear weakness |
| 3–4 | Below average — functional but unimpressive |
| 5–6 | Average — middle of the pack |
| 7–8 | Strong — a notable advantage |
| 9–10 | Exceptional — among the best in the game |

**Cost** represents total resource investment (Cordite + VC equivalent). 1 = cheapest possible unit, 10 = superweapon-tier.

---

### Valkyr Unit Roster

> *"If you can see the sky, you're already in range."*

#### Infantry

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 1 | **Windrunner** | Infantry | Scout / Anti-infantry | 5 | 1 | 3 | 4 | 1 | Submachine gun | Stealth when stationary; designates targets for air strikes (+10% air damage to marked target) | Lightly-armed recon infantry that paints targets for the real killers above. |
| 2 | **Skyguard** | Infantry | Anti-air | 4 | 2 | 5 | 6 | 3 | Shoulder-mounted SAM | Lock-on mechanic — takes 2 sec to lock, then fires guaranteed-hit missile | Valkyr's only ground-based AA unit; essential but fragile. |
| 3 | **Gale Trooper** | Infantry | Anti-vehicle | 4 | 2 | 5 | 4 | 3 | Rocket launcher | Can deploy a one-use grapple to rapidly reposition up cliffs or onto elevated terrain | Jump-jet infantry that can reach elevated positions other infantry can't. |

#### Light Vehicles

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 4 | **Zephyr Buggy** | LightVehicle | Scout / Harass | 9 | 1 | 2 | 3 | 2 | Twin autocannon | Afterburner ability — 3 sec speed boost (50% faster) on 20 sec cooldown | Extremely fast scout that excels at running circles around slow armies. |
| 5 | **Mistral APC** | APC | Transport | 7 | 3 | 2 | 3 | 3 | Light machinegun | Carries 2 infantry squads; deploys them instantly (no exit animation) | Fast-deploying transport that keeps infantry relevant for Valkyr. |

#### Helicopters

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 6 | **Kestrel** | Helicopter | Anti-infantry / Scout | 8 | 3 | 4 | 5 | 4 | Nose-mounted chaingun | Recon Pulse — reveals a large area through fog of war for 5 sec (30 sec CD) | Fast attack helicopter that doubles as Valkyr's primary scouting platform. |
| 7 | **Harrier** | Helicopter | Anti-vehicle | 7 | 4 | 7 | 5 | 6 | Anti-tank guided missiles | Hover Lock — becomes stationary for +25% damage and +20% range (immobile while active) | Dedicated tank-hunter helicopter; the backbone of Valkyr's anti-armor capability. |

#### Jets

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 8 | **Peregrine** | Jet | Air superiority | 10 | 3 | 6 | 7 | 6 | Air-to-air missiles | Sortie unit — must return to Airstrip. Evasive Maneuver — dodges next incoming missile (30 sec CD) | The best dogfighter in the game; nothing outruns or outfights it in the sky. |
| 9 | **Tempest** | Jet | Precision bomber | 9 | 4 | 9 | 3 | 8 | Guided bombs | Sortie unit. Target Designator — if a Windrunner has marked the target, bomb damage +30% and splash +50% | Heavy precision bomber that deletes buildings in a single pass. |
| 10 | **Shriek** | Jet | Anti-air / Interceptor | 10 | 2 | 5 | 8 | 5 | Rapid-fire air-to-air cannon | Sortie unit. Afterburner — massive speed boost to intercept incoming aircraft. Longest range AA in the game | Fast interceptor jet that patrols airspace and shreds anything that enters. |

#### Support / Utility

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 11 | **Updraft** | Helicopter | Repair / Resupply | 6 | 3 | 0 | 0 | 4 | None | Airborne Repair — heals nearby friendly aircraft (+3 HP/sec in 8-cell radius). Can rearm jets in the field (acts as mobile Airstrip, 50% slower rearm rate) | Flying support craft that keeps the air fleet operational far from base. |
| 12 | **Overwatch Drone** | Helicopter | Detection / Recon | 9 | 1 | 0 | 0 | 2 | None | Permanent stealth detection in 12-cell radius. Auto-follows assigned unit. Cannot be manually attacked (must use area-effect weapons) | Tiny autonomous drone that provides continuous vision and stealth detection. |

#### Special / Unique

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 13 | **Stormcaller** | Jet | Superweapon strike | 8 | 5 | 10 | 4 | 10 | Electromagnetic pulse bomb | Sortie unit. EMP Strike — disables all enemy units and buildings in a large radius for 8 seconds (no damage, but total shutdown). 120 sec cooldown after rearm. Only 1 may exist. | Strategic bomber that doesn't kill — it paralyzes. Drop the EMP, then follow up with everything you have. |
| 14 | **Valkyrie Carrier** | Helicopter | Mobile airfield | 3 | 7 | 2 | 4 | 9 | Point-defense laser | Functions as a mobile Airstrip — jets can land and rearm on it. Carries up to 4 jet "slots." Has light self-defense laser. Only 1 may exist. | Massive flying aircraft carrier that extends Valkyr's operational range across the entire map. |

#### Base Defenses

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 15 | **Wind Wall** | Static | Wall | — | 4 | 0 | 0 | 1 | None | Low HP compared to other faction walls; generates a mild slow effect on enemy ground units passing nearby (-10% speed) | Lightweight energy barrier — stops infantry but crumbles under tank fire. |
| 16 | **Skypiercer Turret** | Static | Anti-air turret | — | 4 | 6 | 8 | 4 | Dual flak cannon | Strong vs. aircraft, cannot target ground units at all | Dedicated AA emplacement — Valkyr protects their airfields fiercely. |
| 17 | **Downburst Turret** | Static | Anti-ground turret | — | 3 | 4 | 5 | 3 | Autocannon | Weak general-purpose turret; exists only to deter light raids | Mediocre ground defense — Valkyr doesn't rely on turrets. |

**Total units: 17** (3 infantry, 2 light vehicles, 2 helicopters, 3 jets, 2 support, 2 special, 3 defenses)

---

### Kragmore Unit Roster

> *"More iron. More fire. More bodies. The math is simple."*

#### Infantry

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 1 | **Vanguard** | Infantry | Anti-infantry (mainline) | 4 | 3 | 4 | 4 | 1 | Assault rifle + bayonet | Garrisonable in buildings. In melee range, switches to bayonet for +50% damage. Benefits from Horde Protocol. | Bread-and-butter line infantry — cheap, tough for infantry, dangerous in groups. |
| 2 | **Ironclad** | Infantry | Anti-vehicle | 3 | 4 | 6 | 5 | 3 | Tandem-charge RPG | Can set up sandbag cover (3 sec deploy, gains +30% armor while stationary behind cover). Horde Protocol eligible. | Heavy weapons team that digs in and punishes vehicles that drive into range. |
| 3 | **Mole Rat** | Infantry | Demolition / Anti-building | 4 | 2 | 8 | 1 | 3 | Demolition charges | Can tunnel underground (stealth, slow, ignores walls) to emerge at target and plant charges. 10 sec plant timer, massive building damage. Single-use ability (unit consumed on detonation). | Suicidal sapper that burrows under defenses and blows them from within. |
| 4 | **Spotter** | Infantry | Scout / Support | 5 | 1 | 1 | 3 | 1 | Sidearm | Binoculars ability — extends vision range by 200% for 5 sec (10 sec CD). Designates targets for artillery (+15% accuracy for artillery firing at spotted targets). | Cheap, expendable forward observer that makes Kragmore's artillery terrifying. |

#### Light Vehicles

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 5 | **Dust Runner** | LightVehicle | Scout / Harass | 8 | 2 | 3 | 4 | 2 | Heavy machinegun | Smoke Screen — deploys smoke cloud that blocks vision (15 sec CD). One of Kragmore's only fast units. | Fast recon vehicle — used primarily for scouting, not fighting. |

#### Heavy Vehicles / Tanks

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 6 | **Bulwark** | Tank | Main battle tank | 4 | 7 | 6 | 5 | 5 | 120mm smoothbore cannon | Horde Protocol eligible. Can switch to AP rounds (+20% vs. vehicles) or HE rounds (+20% vs. infantry) | The workhorse of Kragmore — nothing fancy, just a lot of gun and a lot of armor. |
| 7 | **Anvil** | Tank | Heavy assault tank | 3 | 9 | 8 | 5 | 7 | Dual 150mm cannons | Horde Protocol eligible. Crush Strength 3 — drives through light structures and infantry without slowing. Slow turret traverse (weak vs. fast flankers). | When one cannon isn't enough. The Anvil is slow, expensive, and absolutely devastating. |
| 8 | **Mammoth** | HeavyVehicle | Super-heavy tank | 2 | 10 | 9 | 6 | 9 | Twin 200mm siege cannons + top-mounted rocket pod | Horde Protocol eligible. Regenerates HP slowly (+1 HP/sec). Only 2 may exist at once. Crushes everything. Cannot be run over or knocked back by any force. | The king of the battlefield — a mobile fortress that demands an answer or it wins the game. |

#### Artillery / Siege

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 9 | **Quake Battery** | Artillery | Long-range bombardment | 1 | 3 | 8 | 10 | 6 | 155mm howitzer | Must deploy (5 sec setup) before firing. Cannot fire while moving. Spotter-bonus eligible (+15% accuracy when Spotter is designating). Massive splash radius. | Classic artillery — sets up, rains shells, and turns the target area into a crater field. |
| 10 | **Grinder** | HeavyVehicle | Siege / Anti-building | 2 | 6 | 10 | 3 | 7 | Rotary demolition drill + short-range flame cannon | Must be adjacent to target structure. Drills through walls and buildings at terrifying speed. Flame cannon deters infantry. | A mobile building-eater. Walks up to a wall, and 10 seconds later there's a hole. |

#### Aircraft

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 11 | **Strix Gunship** | Helicopter | Ground attack | 5 | 5 | 5 | 4 | 5 | Chin-mounted rotary cannon + dumb rockets | Slow, armored attack helicopter. Not agile enough to dodge AA but tough enough to survive some hits. | Kragmore's only aircraft is a brute — a flying tank that trades speed for survivability. |

#### Support / Utility

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 12 | **Hauler APC** | APC | Infantry transport | 5 | 5 | 2 | 3 | 3 | Pintle machinegun | Carries 3 infantry squads (most capacity in the game). Deploying infantry gain +10% accuracy for 5 sec ("dug in" bonus). | Massive troop carrier that delivers infantry to the front in reasonable condition. |
| 13 | **Wrecker** | HeavyVehicle | Repair / Recovery | 3 | 5 | 0 | 0 | 3 | None | Repairs adjacent vehicles at +5 HP/sec. Can salvage destroyed enemy vehicles for 25% of their Cordite cost. | Mobile repair rig that keeps the armored column rolling and recycles enemy wrecks for parts. |
| 14 | **Minelayer** | LightVehicle | Area denial | 6 | 2 | 0 | 0 | 3 | None | Deploys anti-vehicle mines (invisible to enemy, 8 damage per mine, carries 8 mines). Can also deploy anti-infantry mines (4 damage, carries 12). | Fast mine-deploying vehicle that seeds the battlefield with nasty surprises. |

#### Base Defenses

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 15 | **Ironwall** | Static | Wall | — | 7 | 0 | 0 | 2 | None | Thickest wall in the game. Infantry can garrison on top (+elevation bonus to range and accuracy). | Massive concrete-and-steel wall segment — armies break against it. |
| 16 | **Flak Nest** | Static | Anti-air turret | — | 4 | 4 | 6 | 3 | Quad flak cannon | Kragmore's only static AA. Moderate damage, moderate range — adequate but not scary. | A reluctant concession that aircraft exist and must be dealt with. |
| 17 | **Bunker** | Static | Anti-ground turret / Garrison | — | 8 | 5 | 5 | 4 | Heavy machinegun (upgradeable) | Garrisons up to 2 infantry squads who fire from ports. Garrisoned infantry gain +40% armor. Can upgrade weapon to cannon. | Heavily fortified infantry fighting position — hard to crack without siege weapons. |
| 18 | **Tremor Mine** | Static | Area denial | — | 1 | 7 | 0 | 1 | Proximity-detonated explosive | Invisible to enemy. One-use. Deals massive damage to the first vehicle that drives over it. Infantry take reduced damage. | Buried anti-vehicle mine — cheap, lethal, and paranoia-inducing. |

**Total units: 18** (4 infantry, 1 light vehicle, 3 heavy/tanks, 2 artillery/siege, 1 helicopter, 3 support, 4 defenses)

---

### Bastion Unit Roster

> *"Let them come. We have walls, and walls have patience."*

#### Infantry

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 1 | **Warden** | Infantry | Garrison / Anti-infantry | 4 | 3 | 3 | 5 | 2 | Battle rifle | +30% damage when garrisoned in a structure. Can repair adjacent structures at +1 HP/sec (repair kit). | Defensive infantry that excels from within structures but is mediocre in the open. |
| 2 | **Sentinel** | Infantry | Anti-vehicle | 3 | 3 | 5 | 6 | 3 | Guided anti-tank missile | Deploy mode: goes prone (immobile, +50% armor, +20% range). Best infantry AT range in the game. | Long-range missile infantry that melts vehicles from deployed positions behind walls. |
| 3 | **Keeper** | Infantry | Engineer / Support | 4 | 2 | 1 | 2 | 2 | Sidearm | Builds turrets and walls 30% faster. Repairs structures at +3 HP/sec (double rate). Can plant proximity mines. | Bastion's engineer — the faction's most important non-combat unit. |

#### Light Vehicles

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 4 | **Patrol Rover** | LightVehicle | Scout / Patrol | 7 | 2 | 2 | 4 | 2 | Light autocannon | Motion Sensor — detects stealthed units in 10-cell radius. Automatically reports enemy positions to minimap. | Fast patrol vehicle with advanced sensors — Bastion's eyes beyond the walls. |
| 5 | **Shield Bearer** | APC | Transport / Support | 5 | 5 | 1 | 2 | 4 | Smoke launcher | Carries 2 infantry. Deployable Energy Shield — projects a directional shield (15-cell wide arc) that blocks 50% of incoming projectile damage for 8 sec (45 sec CD). | Mobile shield generator that creates temporary cover for advancing infantry. |

#### Heavy Vehicles / Tanks

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 6 | **Rampart** | Tank | Defensive tank | 3 | 8 | 4 | 5 | 5 | Medium cannon | Entrench mode: deploys stabilizers (immobile, +30% armor, +20% range, +15% accuracy). Essentially becomes a mobile turret. | A tank that wants to stop moving. Best used as a mobile hardpoint in a defensive line. |
| 7 | **Phalanx** | HeavyVehicle | Mobile wall / Damage sponge | 2 | 10 | 2 | 3 | 6 | Short-range flamethrower | Taunt Aura — enemy units within 6 cells auto-target Phalanx instead of other units (AI behavior override, player can manually override). Highest armor in the game for a non-super unit. | A rolling fortress that absorbs punishment meant for everything behind it. |

#### Artillery / Siege

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 8 | **Bulwark Mortar** | Artillery | Defensive bombardment | 1 | 3 | 6 | 8 | 5 | Heavy mortar | Shorter range than Kragmore artillery but fires faster. Deploy mode required. Can fire smoke rounds (blocks vision, no damage) or incendiary rounds (area denial, burns for 10 sec). | Defensive mortar that creates kill zones around your base — not designed for offensive pushes. |

#### Aircraft

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 9 | **Guardian Drone** | Helicopter | Anti-air patrol | 6 | 2 | 4 | 6 | 3 | Anti-air missiles | Autonomous — requires no player input. Automatically patrols within 15 cells of nearest allied structure and engages air targets. Cannot be manually controlled. | Set-and-forget AA drone that patrols the base perimeter autonomously. |
| 10 | **Watcher** | Helicopter | Recon | 7 | 1 | 0 | 0 | 2 | None | Extended vision range (20 cells). Stealth detection in 12-cell radius. Auto-retreats when damaged. | Unarmed observation helicopter that keeps Bastion informed of incoming threats. |

#### Support / Utility

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 11 | **Constructor** | HeavyVehicle | Mobile building platform | 2 | 4 | 0 | 0 | 4 | None | Can build any Bastion structure (turrets, walls, buildings) at any location. Builds 20% slower than base construction. Essential for expanding the defense network. | Bastion's expansion vehicle — where the Constructor goes, the fortress follows. |
| 12 | **Restoration Rig** | LightVehicle | Repair | 6 | 2 | 0 | 0 | 3 | None | Repairs structures and vehicles at +4 HP/sec. Can target 2 structures simultaneously. Self-repairs at +1 HP/sec when idle. | Fast repair vehicle that keeps the fortress in fighting shape. |

#### Special / Unique

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 13 | **Command Hub** | Static | Network nexus | — | 9 | 3 | 6 | 7 | Defensive laser | Core of the Fortification Network mechanic. All turrets/walls within 8 cells gain +15% HP, +10% damage, +20% repair speed. Provides vision in 15-cell radius. Only 3 may exist. | The beating heart of Bastion's defense network — protect it at all costs. |
| 14 | **Aegis Generator** | Static | Superweapon defense | — | 7 | 0 | 0 | 9 | None | Projects a dome shield (20-cell radius) that absorbs up to 500 HP of damage before collapsing. Regenerates 10 HP/sec when not taking damage. 60 sec recharge after collapse. Only 1 may exist. | The ultimate defensive structure — a literal energy dome over your base. |

#### Base Defenses

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 15 | **Citadel Wall** | Static | Wall | — | 9 | 0 | 0 | 2 | None | Heaviest wall in the game. Can be upgraded to have embedded gun ports (garrisonable for 1 infantry squad). Self-repairs at +0.5 HP/sec. | Massive fortified wall — takes concentrated siege fire or a Grinder to break through. |
| 16 | **Bastion Turret** | Static | Anti-ground (general) | — | 6 | 7 | 7 | 5 | Twin cannon | Upgradeable: Railgun (anti-vehicle, +30% damage, +20% range) or Gatling (anti-infantry, +50% fire rate, -20% range). Network-eligible. | The gold-standard turret — high damage, high range, built to last. |
| 17 | **Spire Array** | Static | Anti-air turret | — | 5 | 7 | 8 | 5 | Dual missile battery | Best static AA in the game. Tracks up to 3 air targets simultaneously. Network-eligible. | Terrifying for pilots — a Spire Array says "this airspace is closed." |
| 18 | **Denial Field** | Static | Area denial | — | 2 | 3 | 0 | 2 | Proximity energy pulse | Invisible to enemy. Triggers when enemy ground unit enters 3-cell radius — deals damage and slows by 40% for 5 sec. Recharges after 15 sec (reusable, not one-shot). | Reusable energy mine that slows and damages — forces enemies into kill zones. |

**Total units: 18** (3 infantry, 2 light vehicles, 2 heavy vehicles, 1 artillery, 2 aircraft, 2 support, 2 special, 4 defenses)

---

### Arcloft Unit Roster

> *"We own the sky. Everything below it is negotiable."*

#### Infantry

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 1 | **Horizon Guard** | Infantry | Anti-infantry / Garrison | 4 | 3 | 4 | 4 | 2 | Pulse rifle | Garrisonable. Gains +20% damage when within Overwatch Zone. | Versatile line infantry that excels when fighting within designated control zones. |
| 2 | **Stormshot** | Infantry | Anti-air specialist | 4 | 2 | 6 | 7 | 3 | Portable flak cannon | Best infantry AA in the game — high damage, long range. Can switch to anti-ground mode (reduced effectiveness). | Walking flak platform — Arcloft's answer to "what if infantry could shut down airspace?" |
| 3 | **Sky Marshal** | Infantry | Anti-vehicle / Support | 3 | 3 | 5 | 5 | 3 | Laser designator + anti-armor missile | Designates targets for aircraft — designated targets take +15% air damage. Also carries anti-vehicle missiles for self-defense. | Officer-class infantry that coordinates ground and air operations. |

#### Light Vehicles

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 4 | **Cirrus Runner** | LightVehicle | Scout / AA support | 8 | 2 | 3 | 5 | 2 | Rapid-fire AA autocannon | Dual-purpose weapon: effective vs. air (primary) and light ground targets. Radar Ping — reveals 15-cell radius for 3 sec (20 sec CD). | Fast scout with built-in AA — Arcloft never leaves home without air defense. |
| 5 | **Nimbus Transport** | APC | Transport | 6 | 4 | 2 | 3 | 3 | Smoke launcher | Carries 2 infantry. Point-defense system — intercepts 1 incoming missile every 10 sec (protects self and nearby units). | Armored transport with active missile defense — surprisingly survivable for its size. |

#### Helicopters

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 6 | **Stratos** | Helicopter | Anti-vehicle | 7 | 4 | 6 | 5 | 5 | Guided anti-tank rockets | Overwatch-eligible — gains bonuses in Overwatch Zones. Can fire while moving (no hover-lock needed). | Versatile attack helicopter that operates best within controlled airspace. |
| 7 | **Templar** | Helicopter | Air superiority | 8 | 3 | 5 | 6 | 5 | Air-to-air missiles | Excels at shooting down other helicopters and jets. +20% damage vs. air targets. Overwatch-eligible. | Dedicated air-to-air helicopter — Arcloft's primary tool for asserting sky dominance. |

#### Jets

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 8 | **Apex** | Jet | Multi-role fighter | 9 | 3 | 5 | 6 | 6 | Switchable loadout: AA missiles or ground-attack bombs | Sortie unit. Loadout selected at Airstrip before launch — AA mode or ground-attack mode. Cannot switch in flight. | Flexible fighter that adapts to the current threat — jack of both trades, master of neither. |
| 9 | **Vigilant** | Jet | Recon / AWACS | 8 | 2 | 2 | 4 | 4 | Light defensive missiles | Sortie unit. AWACS mode — while airborne, extends radar range of all friendly units by +5 cells and reveals stealthed units in 20-cell radius. | Flying early-warning system that makes sneaking up on Arcloft nearly impossible. |

#### Support / Utility

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 10 | **Aether Relay** | Helicopter | Network extender | 5 | 3 | 0 | 0 | 4 | None | Flying Overwatch Zone projector — creates a mobile Overwatch Zone (10-cell radius) centered on itself. Extends defense network bonuses to nearby turrets (airborne Command Hub, weaker bonuses: +8% HP, +5% damage). | Mobile command relay that projects Arcloft's defensive network forward. |
| 11 | **Patch Drone** | Helicopter | Repair | 7 | 1 | 0 | 0 | 2 | None | Repairs vehicles and aircraft at +3 HP/sec. Can repair structures at +2 HP/sec. Autonomous — auto-repairs nearest damaged friendly. | Small repair drone that keeps Arcloft's forces operational. |

#### Special / Unique

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 12 | **Sky Bastion** | Helicopter | Mobile fortress | 2 | 8 | 6 | 6 | 10 | Quad AA batteries + ground-attack cannon | Massive flying platform. Functions as Airstrip (2 jet slots), Overwatch Zone projector, and combat unit. Only 1 may exist. Incredibly slow for a helicopter. | The ultimate expression of Arcloft's doctrine — a flying fortress that dominates whatever airspace it occupies. |

#### Base Defenses

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 13 | **Rampart Wall** | Static | Wall | — | 6 | 0 | 0 | 2 | None | Standard wall with integrated sensor (detects units in 8-cell radius, including stealthed). | Wall that doubles as a detection network. |
| 14 | **Flak Citadel** | Static | Anti-air turret | — | 6 | 7 | 9 | 6 | Triple flak battery | Second-best static AA (behind Bastion's Spire Array). Tracks 2 air targets. Very long range. | Devastating AA emplacement — approaching Arcloft by air requires serious commitment. |
| 15 | **Arc Turret** | Static | Anti-ground turret | — | 5 | 5 | 6 | 4 | Chain lightning cannon | Hits primary target and chains to up to 2 additional enemies within 4 cells. Effective vs. clustered ground forces. | Anti-infantry/anti-light-vehicle turret with splash-chain damage. |
| 16 | **Interceptor Battery** | Static | Anti-missile / Support | — | 4 | 0 | 0 | 4 | Interceptor missiles | Point-defense system — intercepts incoming projectiles (missiles, rockets, artillery shells) within 10-cell radius. 70% intercept rate. Does not fire at units directly. | Anti-missile system that protects nearby structures from bombardment. |

**Total units: 16** (3 infantry, 2 light vehicles, 2 helicopters, 2 jets, 2 support, 1 special, 4 defenses)

---

### Ironmarch Unit Roster

> *"We don't retreat. We haven't finished building yet."*

#### Infantry

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 1 | **Holdfast** | Infantry | Anti-infantry / Garrison | 4 | 4 | 4 | 4 | 2 | Heavy battle rifle | Garrisonable. +25% armor when within FOB radius. Digs foxhole when stationary for 5+ sec (+20% armor, must stand up to move). | Tough defensive infantry that literally digs in wherever they stop. |
| 2 | **Breacher** | Infantry | Anti-building / CQC | 4 | 3 | 6 | 2 | 3 | Shotgun + demolition kit | Can breach garrisoned structures (clears garrison, deals structure damage). Plants explosives on buildings (+100% damage to structures at point-blank). | Close-quarters specialist that clears buildings and punches through walls. |
| 3 | **Field Engineer** | Infantry | Construction / Repair | 4 | 2 | 1 | 2 | 2 | Sidearm | Builds turrets and walls anywhere within FOB radius. Repairs structures and vehicles at +2 HP/sec. Can lay anti-vehicle mines (4 mines max). | The backbone of Ironmarch's forward-basing strategy. |

#### Light Vehicles

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 4 | **Pathcutter** | LightVehicle | Scout / Recon | 7 | 2 | 2 | 4 | 2 | Light autocannon | Marks enemy positions on minimap for 10 sec after spotting. Road Builder ability — can lay temporary road tiles behind it (+speed for following ground units, lasts 60 sec). | Scout that literally paves the way for the main army. |
| 5 | **Trenchline APC** | APC | Transport / Fortification | 5 | 5 | 2 | 3 | 4 | Pintle machinegun + smoke | Carries 3 infantry. Deploy mode: unfolds into a static bunker (3 garrison slots, +40% armor for occupants). Cannot move while deployed. 8 sec deploy/undeploy time. | APC that transforms into a forward bunker — the ultimate in mobile defense. |

#### Heavy Vehicles / Tanks

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 6 | **Basalt** | Tank | Main battle tank | 4 | 7 | 6 | 5 | 5 | 105mm cannon | Hull-down mode: when stationary on a ridge or behind cover, gains +25% armor (front arc only). Solid workhorse tank. | Reliable medium tank designed to fight from prepared positions. |
| 7 | **Siegebreaker** | Tank | Assault tank | 3 | 8 | 7 | 4 | 7 | 140mm siege cannon + coaxial flamer | +50% damage to structures. Crush Strength 2. Short range but devastating up close. Designed to smash through walls. | Purpose-built wall-breaker that leads the assault on enemy fortifications. |
| 8 | **Dozer** | HeavyVehicle | Bulldozer / Terrain modifier | 3 | 6 | 2 | 2 | 4 | Bulldozer blade (melee crush) | Can flatten terrain obstacles, clear mines, and create road surfaces. Removes enemy walls by driving through them (slow, takes damage). Unique utility vehicle. | Engineering vehicle that reshapes the battlefield to favor Ironmarch's advance. |

#### Artillery / Siege

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 9 | **Stonehail** | Artillery | Mobile bombardment | 1 | 3 | 7 | 9 | 6 | Rocket artillery (MLRS) | Deploy required. Fires salvos of 6 rockets — less accurate than Kragmore's howitzers but covers more area. Can fire within FOB build radius without deploying (pre-sighted). | Rocket artillery that blankets an area in explosions — less precise but more area denial. |

#### Aircraft

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 10 | **Bullfrog** | Helicopter | Transport / Light attack | 6 | 4 | 3 | 3 | 4 | Door-mounted machineguns | Carries 2 infantry or 1 light vehicle. Ironmarch's only aircraft — slow, tough, and primarily a transport. Can paradrop infantry. | Armored transport helicopter — Ironmarch's sole concession to aerial mobility. |

#### Support / Utility

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 11 | **FOB Truck** | HeavyVehicle | Mobile base | 2 | 5 | 0 | 0 | 6 | None | Deploys into Forward Operating Base (see faction mechanic). Core of Ironmarch's strategy. | The heart of Ironmarch's forward operations — where this truck stops, a fortress grows. |
| 12 | **Field Hospital** | HeavyVehicle | Healing / Support | 2 | 4 | 0 | 0 | 4 | None | Heals infantry within 8-cell radius at +2 HP/sec. Revive ability — can recover 50% of infantry casualties from a nearby destroyed squad (10 sec cast, 45 sec CD). | Mobile medical unit that keeps Ironmarch's infantry fighting longer. |
| 13 | **Signal Truck** | LightVehicle | Vision / Comms | 6 | 2 | 0 | 0 | 3 | None | Extends minimap radar in 20-cell radius. Disrupts enemy stealth in 10-cell radius. Jams enemy radar in 8-cell radius (enemies lose minimap vision in the area). | Electronic warfare vehicle that controls the information battlefield. |

#### Special / Unique

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 14 | **Juggernaut** | HeavyVehicle | Mobile super-turret | 1 | 9 | 8 | 8 | 9 | Twin heavy railguns | Deploy mode required. When deployed, becomes the highest-DPS stationary unit in the game. Cannot fire while moving. Deploy time: 10 sec. Only 1 may exist. | A turret with an engine. It moves to a position, deploys, and nothing survives in its line of fire. |

#### Base Defenses

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 15 | **Fieldwork Wall** | Static | Wall | — | 6 | 0 | 0 | 1 | None | Cheapest wall in the game. Quick to build (3 sec). Lower HP than Bastion/Kragmore walls but you can spam them. Infantry can garrison on top. | Rapidly-deployed field fortification — quantity has a quality all its own. |
| 16 | **Watchtower** | Static | Anti-ground turret | — | 5 | 5 | 6 | 4 | Medium cannon | Can be built within FOB radius. Garrisonable (1 infantry squad). Elevated — provides vision in 12-cell radius. | Multi-purpose defense tower that provides both firepower and intelligence. |
| 17 | **Wire Field** | Static | Area denial | — | 1 | 1 | 0 | 1 | Razor wire | Slows enemy ground units by 60% while in the wire. Deals minor damage over time. Infantry take extra damage. Vehicles slowly push through. | Cheap, effective area denial that channels enemy movement into kill zones. |

**Total units: 17** (3 infantry, 2 light vehicles, 3 heavy/tanks, 1 artillery, 1 helicopter, 3 support, 1 special, 3 defenses)

---

### Stormrend Unit Roster

> *"If they're reacting, we're winning."*

#### Infantry

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 1 | **Razorwind** | Infantry | Scout / Anti-infantry | 6 | 1 | 4 | 3 | 1 | Twin SMGs | Fastest infantry in the game. Sprint ability — +50% speed for 5 sec (15 sec CD). Deals damage while moving (run-and-gun). | Lightning-fast assault infantry that hits and vanishes before the enemy can respond. |
| 2 | **Thunderjaw** | Infantry | Anti-vehicle | 5 | 2 | 6 | 4 | 2 | Recoilless rifle | Can fire on the move (no deploy needed). +20% damage when Momentum is above 50. | Mobile anti-armor infantry that embodies Stormrend's aggressive doctrine. |
| 3 | **Sparkrunner** | Infantry | Anti-air | 5 | 1 | 5 | 6 | 2 | Man-portable SAM | Fastest SAM lock-on in the game (1 sec vs. standard 2 sec). Can fire on the move. | AA infantry that keeps pace with Stormrend's relentless advance. |

#### Light Vehicles

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 4 | **Bolt** | LightVehicle | Scout / Harass | 10 | 1 | 3 | 3 | 2 | Twin autocannon | Fastest ground unit in the game. Drift ability — maintains speed through sharp turns (normally vehicles slow to turn). Generates Momentum on every hit. | Insanely fast attack buggy that's hard to catch and hard to pin down. |
| 5 | **Tempest Raider** | LightVehicle | Anti-vehicle / Raid | 8 | 2 | 5 | 4 | 3 | Rear-firing missile rack | Can fire missiles while retreating (unique rear-facing weapon mount). Hit-and-run specialist. | Raider vehicle designed to shoot, scoot, and shoot again — the essence of Stormrend. |
| 6 | **Gust APC** | APC | Transport | 7 | 3 | 2 | 3 | 3 | Light machinegun | Carries 2 infantry. Rapid deploy — infantry exit instantly and gain +30% fire rate for 3 sec ("shock drop" bonus). Fastest APC in the game. | Blitz transport that deploys infantry in the middle of a firefight for maximum chaos. |

#### Heavy Vehicles / Tanks

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 7 | **Riptide** | Tank | Fast attack tank | 6 | 5 | 6 | 4 | 5 | Rapid-fire autocannon | Fastest tank in the game. +15% damage when moving (vs. stationary). Flanking bonus: +25% damage when attacking from side/rear arc. | A tank that fights best at full speed — Stormrend's answer to armored warfare. |
| 8 | **Cyclone** | Tank | Glass cannon tank | 5 | 4 | 8 | 5 | 6 | High-velocity railgun | Massive alpha strike damage but slow reload (4 sec). First-shot bonus: +30% damage if the Cyclone hasn't been hit in the last 5 sec (ambush predator). | Devastating first-strike tank that crumbles in sustained combat — hit hard, relocate, repeat. |

#### Artillery / Siege

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 9 | **Thunderclap** | LightVehicle | Mobile rocket platform | 7 | 2 | 7 | 7 | 5 | Truck-mounted rocket pod | Does NOT need to deploy — fires on the move (reduced accuracy). Fires a burst of 4 rockets then reloads for 6 sec. Can fire while driving. Glass cannon — if caught, dies instantly. | Mobile artillery that embodies Stormrend philosophy: it shoots, it moves, it never stops. |

#### Aircraft

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 10 | **Flickerhawk** | Helicopter | Anti-infantry / Harass | 8 | 2 | 4 | 4 | 3 | Nose-mounted minigun | Extremely agile. Generates Momentum rapidly. Can strafe-run (fly-by attacks that don't require stopping). | Fast, fragile attack helicopter that excels at chewing up infantry and harvesters. |
| 11 | **Vortex** | Helicopter | Anti-vehicle | 7 | 3 | 6 | 5 | 5 | Anti-tank missiles + rocket pods | Dual-weapon system: missiles for vehicles, rockets for buildings. Can empty full payload in a devastating alpha strike then must reload (10 sec). | Alpha-strike helicopter that dumps its entire payload and retreats to reload. |
| 12 | **Razorbeak** | Jet | Multi-role fighter | 9 | 3 | 5 | 5 | 5 | Switchable: AA missiles or ground-attack rockets | Sortie unit. Faster rearm time than other faction jets (8 sec vs. standard 12 sec). Generates Momentum. | Fast-cycling jet that spends more time in combat and less time on the ground. |

#### Support / Utility

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 13 | **Surge Node** | LightVehicle | Momentum amplifier | 7 | 1 | 0 | 0 | 3 | None | Aura — all Stormrend units within 10 cells generate +50% Momentum from dealing damage. Also provides +5% speed to nearby allies. Fragile but high-value target. | Force multiplier that supercharges Stormrend's aggression — protect it or lose the momentum edge. |
| 14 | **Scrap Hawk** | Helicopter | Repair / Salvage | 6 | 2 | 0 | 0 | 3 | None | Repairs aircraft and vehicles at +3 HP/sec. Auto-collects wreckage from destroyed enemy units — each wreck yields 15% of the unit's Cordite cost. | Flying salvage helicopter that turns enemy losses into Stormrend resources. |

#### Special / Unique

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 15 | **Stormbreaker** | Jet | Superweapon strike | 9 | 4 | 10 | 3 | 10 | Cluster bomb + napalm | Sortie unit. Carpet Bombing Run — flies in a straight line, dropping cluster bombs over a 30-cell path. Everything in the path takes massive damage. 90 sec rearm. Only 1 may exist. | The ultimate expression of Stormrend's doctrine — overwhelming destruction delivered at maximum speed. |

#### Base Defenses

| # | Name | Movement | Role | Spd | Arm | Dmg | Rng | Cost | Weapon | Special | Description |
|---|------|----------|------|-----|-----|-----|-----|------|--------|---------|-------------|
| 16 | **Chainlink Barrier** | Static | Wall | — | 3 | 0 | 0 | 1 | None | Weakest wall in the game. Only slows enemies, doesn't fully block (enemies push through at 30% speed). Cheap and fast to build. | More of a speed bump than a wall — Stormrend doesn't believe in standing still. |
| 17 | **Snap Turret** | Static | Anti-ground turret | — | 2 | 4 | 5 | 2 | Rapid-fire autocannon | Fastest target acquisition in the game. Low damage, low HP — fires first but dies fast. | A turret that's first to shoot and first to die — a brief deterrent, not a real defense. |
| 18 | **Flicker Mine** | Static | Area denial | — | 1 | 8 | 0 | 1 | Proximity explosive | One-use, invisible. Higher damage than other factions' mines but Stormrend can only place them in their own base radius. | Defensive mine for base protection — because even Stormrend needs SOMETHING at home. |

**Total units: 18** (3 infantry, 3 light vehicles, 2 tanks, 1 artillery, 3 aircraft, 2 support, 1 special, 3 defenses)

---

## Faction Matchup Matrix

Each cell describes the **row faction's** perspective against the **column faction**. Advantage ratings: **Favored** (55–60%), **Slight Edge** (50–55%), **Even** (50%), **Slight Disadvantage** (45–50%), **Unfavored** (40–45%).

### Valkyr (Air Primary)

| vs. | Rating | Analysis |
|-----|--------|----------|
| **Kragmore** | **Favored** | Kragmore's weak AA (only Flak Nest and Strix Gunship) makes them vulnerable to sustained air assault. Valkyr avoids the ground deathball entirely and picks apart Kragmore's slow-moving columns from above. Kragmore wins only if they can force a ground engagement or camp under heavy Flak Nest coverage. |
| **Bastion** | **Unfavored** | Bastion's Spire Arrays and Aegis Generator make direct air assault suicidal. Valkyr must find gaps in the defense network or snipe Command Hubs to weaken sectors. Bastion's passive economy means they can afford to wait. Valkyr's window is early-mid game before the fortress is complete. |
| **Arcloft** | **Slight Disadvantage** | Arcloft's AA defense + Templar helicopters contest Valkyr's air superiority. Valkyr has better individual aircraft, but Arcloft has the safety net of fortified airbases. Air-vs-air dogfights favor Valkyr, but Arcloft can retreat to their AA umbrella. |
| **Ironmarch** | **Favored** | Ironmarch's catastrophic AA weakness makes them easy prey. Valkyr helicopters dismantle FOBs and armored columns with impunity. Ironmarch can only survive by pushing aggressively to force base trades. |
| **Stormrend** | **Even** | A volatile matchup. Stormrend's multi-vector pressure forces Valkyr to split air defense between ground raids and air threats. Valkyr's air power can devastate Stormrend's fragile ground army, but Stormrend's momentum mechanic punishes passive play. Fast, chaotic games. |

### Kragmore (Ground Primary)

| vs. | Rating | Analysis |
|-----|--------|----------|
| **Valkyr** | **Unfavored** | Kragmore's weak AA is a critical liability. Must rush to close distance and force ground fights. Horde Protocol helps if Kragmore can group up, but Valkyr simply flies over the blob. |
| **Bastion** | **Slight Edge** | Kragmore's siege capability (Quake Battery, Grinder, Mole Rat) can crack Bastion's defenses. The Mammoth tanks can absorb turret fire while artillery demolishes walls. However, it's costly — Bastion bleeds Kragmore economically for every assault. |
| **Arcloft** | **Slight Edge** | Arcloft has no answer to a massed Kragmore ground push. Their aircraft can harass but lack the sustained DPS to stop Anvils and Mammoths. Kragmore's Horde Protocol makes the deathball terrifying. Arcloft must win before Kragmore hits critical mass. |
| **Ironmarch** | **Even** | The immovable object meets the unstoppable force. Both factions want to play slow ground games. Ironmarch's forward basing creates interesting positional battles. Kragmore has more raw power; Ironmarch has better terrain control. Games go long. |
| **Stormrend** | **Slight Edge** | Stormrend's fragile units melt against Kragmore's armor. However, Stormrend's speed advantage means they can raid Kragmore's slow economy mercilessly. Kragmore wins if they survive to late game; Stormrend wins if they end it early. |

### Bastion (Defense Primary)

| vs. | Rating | Analysis |
|-----|--------|----------|
| **Valkyr** | **Favored** | Bastion's AA network (Spire Array + Guardian Drones + Aegis Generator) neutralizes Valkyr's air power. Valkyr must commit to costly ground assaults they're ill-equipped for, or attempt surgical EMP strikes on Command Hubs. |
| **Kragmore** | **Slight Disadvantage** | Kragmore has the siege tools to crack fortresses. Quake Batteries outrange turrets, Grinders eat walls, and Mole Rats bypass everything. Bastion must invest heavily in anti-siege measures and hope Kragmore runs out of resources before the walls fall. |
| **Arcloft** | **Slight Edge** | Bastion's superior ground defenses deter Arcloft's weak ground game, and AA turrets neutralize air assaults. Arcloft can contest the mid-map but struggles to threaten Bastion's core. Stalemate-prone — both factions turtle effectively. |
| **Ironmarch** | **Slight Edge** | Ironmarch's forward basing is less effective against a faction that out-turtles them. Bastion's defense network is stronger than FOB turrets, and the Aegis Generator denies artillery bombardment. Ironmarch gets stuck grinding against superior fortifications. |
| **Stormrend** | **Favored** | Stormrend's paper-thin units crash against Bastion's walls. The Momentum mechanic is useless when every assault gets shredded by turrets. Stormrend must win very early (before turrets go up) or not at all. Denial Fields and Citadel Walls hard-counter the blitz. |

### Arcloft (Air + Defense)

| vs. | Rating | Analysis |
|-----|--------|----------|
| **Valkyr** | **Slight Edge** | Arcloft's AA defense network contests Valkyr's air dominance, and the Templar helicopter is a strong air-to-air counter. Valkyr has better jets, but Arcloft has the defensive safety net. |
| **Kragmore** | **Slight Disadvantage** | Arcloft cannot stop a full Kragmore ground push — no heavy vehicles, no tanks. Aircraft can harass but not halt Mammoths. Must win through sustained air attrition before Kragmore reaches critical mass. |
| **Bastion** | **Slight Disadvantage** | Both factions want to turtle, but Bastion does it better. Arcloft's air power can't break Bastion's AA-backed fortifications. Long, grindy games that slightly favor Bastion's economic advantage. |
| **Ironmarch** | **Favored** | Arcloft's aircraft devastate Ironmarch's AA-less army. FOBs are easy targets for air strikes. Ironmarch can only survive by pushing fast and forcing base trades before Arcloft's air force scales. |
| **Stormrend** | **Slight Edge** | Arcloft's defenses blunt Stormrend's ground raids, and air superiority contests Stormrend's helicopters. However, Stormrend's multi-vector pressure is hard to contain with Arcloft's small army cap. |

### Ironmarch (Ground + Defense)

| vs. | Rating | Analysis |
|-----|--------|----------|
| **Valkyr** | **Unfavored** | Ironmarch's Achilles' heel. Helicopters and jets dismantle FOBs and armor with impunity. The Bullfrog helicopter is insufficient AA. Ironmarch must rush aggressively or invest heavily in the Reinforced FOB upgrade for flak. |
| **Kragmore** | **Even** | Fascinating mirror-ish matchup. Kragmore has more raw armor; Ironmarch has better positioning. FOBs create chokepoints that slow Kragmore's advance. Positional chess that rewards the better strategist. |
| **Bastion** | **Slight Disadvantage** | Ironmarch can project power forward, but Bastion's home defense is superior. The Juggernaut can challenge Bastion turrets at range, but Bastion's Aegis Generator absorbs the opening salvo. |
| **Arcloft** | **Unfavored** | Arcloft's aircraft exploit Ironmarch's AA gap mercilessly. Forward bases become targets rather than assets. Ironmarch needs the Reinforced FOB flak upgrade as an absolute priority. |
| **Stormrend** | **Slight Edge** | Ironmarch's fortified positions blunt Stormrend's raids. Wire Fields and Watchtowers deny flanking routes. However, Stormrend's speed advantage means they can choose engagements and avoid Ironmarch's strength zones. |

### Stormrend (Air + Ground Offense)

| vs. | Rating | Analysis |
|-----|--------|----------|
| **Valkyr** | **Even** | Explosive, fast-paced matchup. Both factions favor offense. Stormrend's ground component gives them a dimension Valkyr lacks, but Valkyr's superior aircraft dominate the airspace. Winner is usually whoever strikes first. |
| **Kragmore** | **Slight Disadvantage** | Stormrend's fragile units struggle against Kragmore's armor. Raiding Kragmore's economy is effective, but if Kragmore weathers the storm, the Horde Protocol deathball ends it. Stormrend must close the game by minute 12-15. |
| **Bastion** | **Unfavored** | Stormrend's worst matchup. The blitz doctrine breaks against fortified walls. Denial Fields, Spire Arrays, and the Aegis Generator all hard-counter aggressive play. Stormrend must win before defenses are built. |
| **Arcloft** | **Slight Disadvantage** | Arcloft's defenses blunt ground raids and air superiority contests Stormrend's helicopters. Multi-vector pressure helps, but Arcloft's Overwatch Zones and AA make it hard to find openings. |
| **Ironmarch** | **Slight Disadvantage** | Ironmarch's fortified positions are frustrating for Stormrend. Wire Fields slow the blitz, FOBs create speed bumps. Stormrend needs to exploit Ironmarch's slow repositioning by attacking multiple locations simultaneously. |

### Summary Matrix (Row = attacker perspective)

|  | Valkyr | Kragmore | Bastion | Arcloft | Ironmarch | Stormrend |
|--|--------|----------|---------|---------|-----------|-----------|
| **Valkyr** | — | Favored | Unfavored | Slight Disadv. | Favored | Even |
| **Kragmore** | Unfavored | — | Slight Edge | Slight Edge | Even | Slight Edge |
| **Bastion** | Favored | Slight Disadv. | — | Slight Edge | Slight Edge | Favored |
| **Arcloft** | Slight Edge | Slight Disadv. | Slight Disadv. | — | Favored | Slight Edge |
| **Ironmarch** | Unfavored | Even | Slight Disadv. | Unfavored | — | Slight Edge |
| **Stormrend** | Even | Slight Disadv. | Unfavored | Slight Disadv. | Slight Disadv. | — |

### Key Matchup Dynamics

**Rock-Paper-Scissors Core:**
- **Air beats Ground** (mobility advantage, AA is Ground's weakness)
- **Defense beats Air** (AA turrets and shields counter aircraft)
- **Ground beats Defense** (siege weapons crack fortifications)

**Hybrid Interactions:**
- Hybrids inherit partial strengths and weaknesses from both parents.
- Air+Defense (Arcloft) is strong against pure Air (Valkyr) and pure Ground (Ironmarch) — it counters both attack vectors.
- Ground+Defense (Ironmarch) is strong against pure Ground (Kragmore) and Ground offense (Stormrend) — positional advantage.
- Air+Ground (Stormrend) is the "all offense" faction that threatens everyone early but struggles against anyone with staying power.

**The Stormrend Problem (by design):**
Stormrend has the fewest favorable matchups on paper but the highest skill ceiling. In tournament play at high levels, Stormrend's multi-vector pressure and Momentum mechanic can overcome theoretical disadvantages. At lower skill levels, Stormrend is the weakest faction because the blitz requires precise execution.

---

## Balance Notes & Open Questions

### Unit Count Summary

| Faction | Infantry | Light Veh | Heavy/Tank | Artillery | Aircraft | Support | Special | Defenses | **Total** |
|---------|----------|-----------|------------|-----------|----------|---------|---------|----------|-----------|
| Valkyr | 3 | 2 | 0 | 0 | 5 | 2 | 2 | 3 | **17** |
| Kragmore | 4 | 1 | 3 | 2 | 1 | 3 | 0 | 4 | **18** |
| Bastion | 3 | 2 | 2 | 1 | 2 | 2 | 2 | 4 | **18** |
| Arcloft | 3 | 2 | 0 | 0 | 4 | 2 | 1 | 4 | **16** |
| Ironmarch | 3 | 2 | 3 | 1 | 1 | 3 | 1 | 3 | **17** |
| Stormrend | 3 | 3 | 2 | 1 | 3 | 2 | 1 | 3 | **18** |

### Supply Cost Estimates (Preliminary)

| Unit Tier | Supply Cost | Examples |
|-----------|------------|---------|
| Basic Infantry | 1 | Vanguard, Windrunner, Razorwind |
| Specialist Infantry | 2 | Ironclad, Skyguard, Stormshot |
| Light Vehicle | 2 | Bolt, Dust Runner, Patrol Rover |
| APC / Transport | 3 | Mistral, Hauler, Gust |
| Medium Tank | 4 | Bulwark, Basalt, Riptide |
| Heavy Tank | 6 | Anvil, Siegebreaker, Cyclone |
| Super Unit | 10–12 | Mammoth, Juggernaut, Sky Bastion |
| Helicopter | 3–4 | Kestrel, Stratos, Flickerhawk |
| Jet | 4–5 | Peregrine, Apex, Razorbeak |
| Superweapon Unit | 10–15 | Stormcaller, Valkyrie Carrier, Stormbreaker |

### Open Design Questions

1. **Stealth mechanics:** How deep should stealth go? Currently only Windrunner and mines use stealth. Should there be a dedicated stealth faction or stealth options for more units?

2. **Naval units:** The terrain system includes Water and DeepWater. Should any faction have naval units, or is water purely a terrain obstacle? (Recommendation: water = obstacle for v1, naval expansion later.)

3. **Superweapon buildings:** Should each faction have a buildable superweapon structure (like Generals' Particle Cannon / Nuclear Missile / SCUD Storm) in addition to their unique superweapon units? Could create exciting late-game win conditions.

4. **Upgrade trees:** Each faction needs an upgrade tree (tech buildings that unlock advanced units and stat improvements). Not designed in this document — needs its own pass.

5. **Veterancy system:** Should units gain experience and level up (like C&C Generals)? If so, Kragmore's Horde Protocol and Stormrend's Momentum might interact interestingly with veterancy.

6. **Population mechanics:** The Mole Rat's self-destruct and potential infantry-sacrifice mechanics need careful balance testing. Suicide units are fun but can be frustrating.

7. **Symmetry check:** Ironmarch and Stormrend both lean unfavored in the matchup matrix. Ironmarch's AA problem needs a solution beyond "upgrade your FOB" — consider adding a dedicated AA unit. Stormrend's late-game weakness might be too punishing — consider a conditional scaling mechanic.

8. **Mirror matches:** How do Valkyr-vs-Valkyr or Kragmore-vs-Kragmore play? Mirror matches should still be interesting. The faction mechanic (sorties vs. horde vs. network) should create enough variation.

### Data File Schema (Preview)

Each unit roster entry should map to a JSON/TOML file in `data/units/{faction}/{unit_id}.toml`:

```toml
[unit]
id = "valkyr_peregrine"
faction = "valkyr"
name = "Peregrine"
movement_class = "Jet"     # maps to MovementProfile.Jet()
role = "air_superiority"

[stats]
speed = 10          # relative 1-10
armor = 3
damage = 6
range = 7
cost_cordite = 1200
cost_vc = 300
supply = 5
build_time_sec = 18

[weapon]
type = "missile"
subtype = "air_to_air"
damage_per_hit = 45
fire_rate = 1.2     # shots per second
projectile_speed = 0.8

[abilities]
sortie = true
loiter_time_sec = 20
evasive_maneuver = { cooldown_sec = 30, dodge_next_missile = true }

[limits]
max_count = -1      # unlimited
requires_building = "airstrip"
tech_level = 2
```

This schema is preliminary — the actual data format will be finalized when the unit system is implemented in code.

---

*End of document. Next steps: upgrade tree design, building roster, campaign mission briefs.*
