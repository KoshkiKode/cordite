# Naval Units — Staged Data (Naval Expansion)

> **Status:** STAGED — NOT IN ACTIVE GAME  
> **Target update:** Naval Expansion DLC (date TBD)  
> **Last updated:** 2026-04-01

---

## Overview

This directory contains pre-authored unit data files for the **Naval Expansion** update, a planned
future DLC for Cordite Wars: Six Fronts. These units are **not referenced by any active tech tree**
and will not appear in standard matches until the expansion is integrated.

The files are stored here to:
- Preserve design work across engine iterations
- Allow QA pre-testing of the Naval movement class before the DLC branch
- Serve as reference for naval balance discussions

---

## New Movement Class: Naval

All units in this directory use `MovementClassId: "Naval"`. This movement class does not yet exist
in the main build. When the Naval Expansion branch is integrated, the following class parameters
will be registered in `MovementProfile.cs`:

| Property     | Value             | Notes                                       |
|--------------|-------------------|---------------------------------------------|
| ClassId      | Naval             | New class — not in base game                |
| Domain       | Water             | Cannot traverse land tiles                  |
| Base Speed   | 0.25 cells/sec    | Overridden per unit via SpeedOverride        |
| Slope Limit  | N/A               | Water surface only                          |
| Footprint    | Varies            | 2×2 patrol, 3×3 destroyer/sub, 4×4 capital  |
| Road Bonus   | None              | Does not apply to Naval class               |
| Crush Infty  | false             | Naval units cannot crush land units         |

---

## Unit Roster Summary

| Faction    | Patrol Boat         | Destroyer               | Submarine         | Capital Ship           |
|------------|---------------------|-------------------------|-------------------|------------------------|
| Valkyr     | Seabird Cutter      | Tempest Frigate         | Depth Hunter      | —                      |
| Kragmore   | Iron Trawler        | Kragmore Dreadnought    | Deep Bore         | Leviathan              |
| Bastion    | Shield Skiff        | Bastion Corvette        | Depth Ward        | Bastion Citadel Ship   |
| Arcloft    | Sky Skimmer         | Arc Cruiser             | Phantom Sub       | —                      |
| Ironmarch  | March Barge         | Ironclad Frigate        | Trench Diver      | Ironmarch Battleship   |
| Stormrend  | Lightning Skiff     | Storm Destroyer         | Riptide Sub       | —                      |

**Total files:** 21 (3 factions without capital ship × 3 units + 3 factions with capital × 4 units)

---

## Design Intent

Each faction's naval roster mirrors their ground doctrine:

- **Valkyr** — fastest patrol boat, targeting-focused frigate, no capital (air faction loses naval depth)
- **Kragmore** — most armoured across all classes; Leviathan is the most devastating capital
- **Bastion** — highest health-pool ships; Citadel Ship extends the Fortification Network to sea
- **Arcloft** — AA-focused patrol and cruiser; no capital (small army penalty applies to naval)
- **Ironmarch** — infantry landing capability on the March Barge; Battleship acts as naval FOB
- **Stormrend** — fastest boats, lowest armour, alpha-strike oriented; no capital (faction is all aggression)

---

## Cost Reference

| Class        | Cordite Range | VC Range  | Pop Cost | Notes                                       |
|--------------|---------------|-----------|----------|---------------------------------------------|
| Patrol Boat  | 400–600 C     | 0 VC      | 1–2      | No VC — accessible early in naval gameplay  |
| Destroyer    | 1,600–2,200 C | 150–300 VC| 3–5      | Moderate VC — mid-game naval investment      |
| Submarine    | 1,850–2,000 C | 200–250 VC| 4–5      | Moderate-high VC; stealth premium           |
| Capital Ship | 4,500–5,000 C | 700–800 VC| 11–12    | Major investment; win-condition naval asset  |

---

## Integration Notes (for Naval Expansion branch)

1. Register `Naval` in `MovementProfile.cs` before importing these unit files.
2. Create naval production buildings (Shipyard) per faction — not yet designed.
3. Tech tree placement: Naval units will gate behind a **Shipyard** building
   at Tier 2 (requires Vehicle Factory equivalent + Tech Lab).
4. AI behaviour for Naval units requires new `NavalBehaviourTree.cs` — see AI roadmap.
5. All `SpecialAbilityId` values in these files reference abilities not yet implemented.
   See `docs/naval-abilities-spec.md` (TBD) for ability design.

---

*All files in this directory are design-complete drafts pending gameplay integration.*  
*Do not reference these IDs in active tech tree data until the Naval Expansion branch is merged.*
