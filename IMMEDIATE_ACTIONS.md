# Immediate Action Items

Updated: 2026-10-24

This is the short-term execution queue for Cordite Wars: Six Fronts. The
broader architecture, product status, and longer roadmap are in
[`docs/project-review.md`](docs/project-review.md).

## Completed

- [x] Retired AWS infrastructure and updated provider guidance to GoDaddy DNS
      plus Google Cloud for future managed services.
- [x] Integrated the substantial `macbook-updates` gameplay branch, including
      terrain, hierarchical pathfinding, VFX, production UI, and integration
      tests.
- [x] Added release metadata validation across project, export, installer, and
      mobile metadata.
- [x] Corrected documented macOS minimum support to macOS 12.0.
- [x] Fixed multiplayer startup gating so local tick confirmation occurs before
      the all-player gate is evaluated.
- [x] Routed UI-issued multiplayer commands through
      `LockstepManager.SubmitLocalCommand`.
- [x] Added command/checksum packet bounds and malformed-packet rejection.
- [x] Made Android release packaging fail closed when signing credentials are
      absent or invalid.
- [x] Separated unsigned iOS Xcode handoffs from signed IPA artifacts.
- [x] Upgraded the retained Python crypto dependency to `cryptography==50.0.2`
      and verified the Python suite with that exact version.
- [x] Removed the retired AWS-specific licensing/checkout handlers and their
      provider-specific route tests; retained the shared license protocol.
- [x] Removed the obsolete project-file backup and unreferenced temporary
      marketing-site prototype.
- [x] Verified locally: 1,481 .NET tests, 32 Python tests, release metadata
      validation, workflow YAML parsing, and diff hygiene.
- [x] Opened follow-up PR #182 for the hardening pass.

## Active blockers

### 1. Restore GitHub Actions reliability

PR #182 checks currently fail immediately across the test and CodeQL jobs
without downloadable job logs. Local validation passes, but the repository
needs a successful remote run before the release pipeline can be considered
verified.

- [ ] Resolve the GitHub Actions/account or runner failure.
- [ ] Rerun PR #182 checks.
- [ ] Record the first successful Ubuntu, Windows, macOS, and CodeQL run.

### 2. Confirm hosted security and test results

The active Python dependency manifest now pins `cryptography==50.0.2`, which
is newer than the patched versions for the reported advisories. The retired
AWS handlers and their dependency manifest are no longer part of the project.
Confirm the active alerts close after merge; dismiss only any residual alerts
that still point to the removed AWS manifest.

## Next engineering batch

### Multiplayer reliability

- [ ] Bind network peer IDs to lobby player IDs for commands and checksums.
- [ ] Reject commands from a sender claiming another player slot.
- [ ] Add disconnect, timeout, and match-abort behavior.
- [ ] Add desync reports with actionable tick/player diagnostics.
- [ ] Design snapshot resync, reconnect, and rollback/recovery behavior.
- [ ] Add exported two-process LAN smoke tests.

### Release and persistence

- [ ] Make save writes atomic with temporary files, flush/close, replacement,
      and recovery tests.
- [ ] Add save schema versioning and migrations.
- [ ] Eliminate or generate the duplicate shared export-preset reference.
- [ ] Add install/launch smoke tests for each published artifact.
- [ ] Validate Android emulator and iOS archive paths in CI or scheduled QA.

### RTS product depth

- [ ] Finish touch-first mobile controls: tap/drag selection, two-finger
      camera controls, long-press orders, safe-area layout, and lifecycle
      pause/resume.
- [ ] Specify and test splash damage, friendly fire, allied/neutral behavior,
      target priority, and fog-of-war combat rules.
- [ ] Expand production queues, rally behavior, attack/defend/hold/patrol
      orders, resource contesting, and reclaiming.
- [ ] Add faction asymmetry, counters, veterancy feedback, tutorials, replay
      and spectator tools, map validation, and mod support.
- [ ] Add accessibility settings and complete input rebinding.

## How to close an item

Every completed item should include a focused test or validation command, an
updated review note when the project status changes, and a link to the PR or
commit that delivered it.
