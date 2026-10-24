# Project Review and RTS Roadmap

Updated: 2026-10-24

This document is the current engineering and product review for Cordite Wars:
Six Fronts. It is intentionally separate from the short-term checklist in
[`IMMEDIATE_ACTIONS.md`](../IMMEDIATE_ACTIONS.md).

## Executive status

Cordite Wars is a Godot 4.6 C#/.NET 9, offline-first RTS with deterministic
fixed-point simulation and LAN/local lockstep networking. The project has a
substantial playable foundation and broad platform packaging, but it is not
yet a fully verified commercial release or a complete Command & Conquer-style
multiplayer product.

The current priority is no longer repository-wide cleanup. It is proving the
release paths, finishing multiplayer recovery, hardening persistence, and
turning the existing systems into a coherent, readable RTS experience.

| Area | Current state | Confidence | Next proof |
| --- | --- | --- | --- |
| Godot/.NET runtime | Godot 4.6, C#/.NET 9 project builds locally | High | Exported launch smoke tests |
| Deterministic simulation | Fixed-point math, seeded RNG, fixed tick, deterministic ordering | High | Long-run cross-process determinism |
| Multiplayer lockstep | Core command/checksum path exists; startup and UI routing hardened | Medium | Identity validation, disconnect/reconnect, LAN tests |
| Desktop exports | Windows, Linux, and macOS presets/package paths exist | Medium | Successful remote exports and install/launch tests |
| Android | APK/AAB paths exist; release signing now fails closed | Medium | Signed artifact plus emulator/device smoke test |
| iOS/iPadOS | Xcode handoff and signed IPA paths exist | Low/Medium | Repeatable Xcode archive and device/TestFlight test |
| Persistence | Save/load exists | Low/Medium | Atomic-write, recovery, migration tests |
| RTS depth | Units, buildings, factions, economy, tech, fog, combat, AI, terrain | Medium | Balance and end-to-end match scenarios |
| CI/release | Local checks pass; PR #182 remote checks fail immediately without logs | Low until restored | Successful GitHub Actions run |

## What is in the repository

- Approximately 202 C# source files, 124 C# test files, 17 scenes, and 310
  JSON data files were present during the full audit.
- Core systems include game lifecycle, fixed-point simulation, seeded
  deterministic RNG, economy, tech trees, unit/building systems, AI,
  pathfinding, terrain, fog of war, combat, VFX, UI, persistence, and
  networking.
- Platform targets represented in the export/release configuration include
  Windows, Linux/X11, macOS, Android APK, Android AAB, and iOS/Xcode handoff.
- The application is offline-first. It does not require an online runtime
  backend for the core game.
- GoDaddy is the registrar/DNS direction. AWS has been retired. Google Cloud
  is the intended future provider for managed services such as storage,
  databases, secrets, or optional backend services.

## Completed project milestones

- Retired the former AWS deployment stack and removed obsolete AWS release
  wiring.
- Merged the substantial `macbook-updates` branch into the main development
  line through PR #181, including terrain, hierarchical pathfinding, VFX,
  performance/UI work, and deterministic integration tests.
- Added release metadata validation for the canonical version, Godot project,
  export presets, Android metadata, Linux packages, Apple plists, Windows
  packages, and installer metadata.
- Corrected documentation to match the macOS 12.0 package minimum.
- Hardened lockstep startup and multiplayer UI command submission in PR #182.
- Added network packet size/count/truncation/trailing-byte validation.
- Upgraded the Python license-protocol dependency to `cryptography==50.0.2`
  and removed the retired AWS-specific license/checkout service handlers.
- Changed Android release packaging to fail instead of publishing debug-signed
  release artifacts.
- Separated unsigned iOS Xcode handoffs from signed IPA artifacts.

## Verification snapshot

The latest local verification completed successfully:

```text
dotnet test tests/CorditeWars.Tests/CorditeWars.Tests.csproj --no-restore
1,481 passed

python3 -m pytest -q
50 passed

python3 tools/validate_release_metadata.py
Release metadata is consistent: version 1.0.0, build 10000
```

The follow-up hardening work is committed as `a02cd6d` and proposed in
[PR #182](https://github.com/KoshkiKode/cordite/pull/182). Its GitHub checks
currently fail immediately across test and CodeQL jobs without downloadable
logs; this is a release verification blocker, not a substitute for local
test results.

## Remaining release blockers

### Multiplayer safety and recovery

The protocol still needs an authoritative peer-to-player identity binding.
Embedded player IDs must not be trusted without checking the sender mapping.
Malformed packets are now bounded and rejected, but disconnects, timeouts,
desync recovery, reconnects, and snapshot resync are not complete.

### Platform proof

The export metadata is substantially more consistent, but packaging is not the
same as device validation. Each release needs install/launch checks for
Windows, Linux, macOS, Android, and iOS. Android needs a signed APK/AAB test
on representative hardware or an emulator. iOS needs a repeatable Xcode
archive/device or TestFlight validation.

### Persistence safety

Save writes need crash-safe replacement and recovery behavior. Add schema
versions, migrations, corruption detection, backup retention, and tests for
interrupted writes before treating saves as production-ready.

### Supply-chain and CI health

GitHub reports open high/medium security alerts for Python
`cryptography` in the pre-merge default branch. This cleanup pins the retained
protocol dependency to `50.0.2`, removes the obsolete AWS service code, and
passes the Python suite against the patched release. Confirm Dependabot closes
the active alerts after merge and clear any alerts that still reference the
removed AWS manifest. GitHub Actions also needs a successful run so release
confidence is based on both local and hosted validation.

## RTS product roadmap

### Phase 1: Competitive match reliability

1. Complete peer identity validation and protocol violation handling.
2. Add timeout/disconnect behavior and a clear match-end state.
3. Add desync diagnostics, snapshot resync, reconnect, and recovery tests.
4. Run long matches with large armies and measure simulation/network budgets.

### Phase 2: Core RTS clarity and balance

1. Define combat rules: splash, friendly fire, allies, neutral units, target
   priority, armor/counters, stealth, and fog-of-war interactions.
2. Make production queues, rally points, construction, and resource flow
   predictable and readable.
3. Complete attack-move, stop, hold, patrol, defend, and queued-order UX.
4. Balance faction asymmetry around distinct strengths, counters, and
   meaningful strategic choices rather than stat-only differences.

### Phase 3: Player-facing completeness

1. Add tutorial/onboarding, campaign pacing, readable mission objectives, and
   failure/retry flows.
2. Add replay, spectator, match history, and deterministic replay validation.
3. Add map validation, custom maps, mod-safe data loading, and versioned map
   compatibility.
4. Add accessibility, input rebinding, localization completeness, and
   controller/touch support.

### Phase 4: Mobile and production quality

1. Implement touch-first selection, camera, context commands, safe-area UI,
   and pause/resume lifecycle handling.
2. Establish device tiers, frame/memory/battery budgets, and large-army
   performance tests.
3. Add opt-in crash reporting/telemetry with a documented privacy policy.
4. Complete store assets, signing, notarization, archive, and submission
   checklists for every supported platform.

## Definition of release readiness

The project should not be called production-ready until all of the following
are true:

- Hosted CI passes on Ubuntu, Windows, macOS, and CodeQL.
- Every release artifact is signed where required and has a successful
  install/launch smoke test.
- Two-process LAN matches remain synchronized through normal play, packet
  loss, disconnect/reconnect, and desync recovery scenarios.
- Saves survive interrupted writes and migrate across supported versions.
- Core combat, economy, production, fog-of-war, and faction rules have
  deterministic tests and documented player-facing behavior.
- Mobile touch flows and real-device performance meet the documented support
  targets.
- No open high-severity dependency alerts remain without an explicit,
  documented exception.
