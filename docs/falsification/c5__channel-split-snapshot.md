# C5 Channel-Split `BuildLiveSnapshot` — Spike Falsification

**Date:** 2026-08-05
**Status:** ❌ **DROPPED** — measured below threshold.
**Spike scope:** Two channels only (Threat + Power), per user instruction.

## What was tried

Extract `BuildLiveSnapshot`'s Threat-pressure block and Power-grid block into
separate `ISnapshotChannel` implementations, dispatched from a shared
`SnapshotContext` so the orchestrator loop becomes a noop
`foreach(channel in channels) channel.Populate(snapshot, ctx)`.

Three new files written under `mods/05-Rimconemy-Infected-Automation/Source/Story/Snapshots/`:

- `ISnapshotChannel.cs` — interface + readonly `SnapshotContext`
- `ThreatChannel.cs` — fills `ColonyWealth` + `ThreatPressure`
- `PowerChannel.cs` — fills `PowerGridActive`

`StoryDirector.BuildLiveSnapshot` was modified to compute the context once
and dispatch the two channels inline. The remaining 6+ inline blocks
(HostileFactionCount, Research placeholder, Injury, DaysSinceLastEvent,
Ideology, StorageHash, MapID, PawnRosterFingerprint,
DeterministicTargetPawnId) were left untouched — the spike was scoped to
two channels so we could measure.

## Measurements

### A) Boilerplate ratio — FAIL

| bucket | LOC |
|---|---|
| New files (ISnapshotChannel + 2 channels, with doc) | ~130 |
| LOC removed from `BuildLiveSnapshot` | ~38 |
| LOC added to `StoryDirector.cs` (using + field + dispatcher) | ~21 |
| **Net new LOC for behavior-equivalent extraction** | **+113** |

Ratio: **3.96×** the work it replaced. The 1.5× drop-threshold was breached
even after stripping legitimate doc-debt tracking.

### B) Tractability / "channel test < 15 lines" — FAIL

`SnapshotContext` exposes `IReadOnlyList<Map>` directly. To unit-test
`ThreatChannel.Populate` a test must construct a `Map` whose
`wealthWatcher.WealthTotal` is determinate. The codebase has **no
`FakeMap` infrastructure today** — only `Fake ThingDef` test doubles
(see `BauschuttRemapApplyTests.cs` lines ~205–224). `Map` is per-game-state
and not reflection-mockable without significant scaffolding (~50 LOC +
per-channel fake factory). The "<15-line test per channel" claim is not
deliverable on this spike alone.

### C) Compile — FAIL

`SnapshotContext` `init` properties triggered CS0518 errors at build:

```
Source/Story/Snapshots/ISnapshotChannel.cs(39,33): error CS0518:
Der vordefinierte Typ "System.Runtime.CompilerServices.IsExternalInit"
ist nicht definiert oder importiert.
```

The RimWorld 1.6 / Mono target framework doesn't ship the
`IsExternalInit` shim. The spike would need either a stub
`Source/IsExternalInit.cs` or a rewrite of `SnapshotContext` to use
regular setters.

A second-instance `Source/IsExternalInit.cs` already exists in the
working tree (untracked — from a concurrent thread, *not* this spike),
indicating another agent needed the same workaround, but it remains
*outside* the spike and **must not** be merged as the spike's load-bearing
fix because a third-party artifact never justifies its own use.

## Drop rationale

The spike failed all three independent gates. Adding more channels (the
proposed scale to 8) does not rescue it because:

- The interface cost (~130 LOC) is already paid; channel #3-#8 add ~15
  LOC each en route to a *better* ratio (the 8-channel estimate lands at
  ~250 LOC total / ~150 LOC of orchestrator removal = ~1.7× — still
  above threshold).
- The tractability test does not improve with channel count; it
  requires a `FakeMap` infra-track requested first and out-of-scope
  for this spike.

The cleanest defensive read: **the inline `BuildLiveSnapshot` is
already dominantly cohesive**. The function reads top-to-bottom with
real dependency order (activeColonists are used by Survivor, Mood,
Injury, MapID, Fingerprint). Splitting it into channels forces
cross-channel ordering through the `SnapshotContext` scratchpad, which
costs ~21 LOC of dispatch + ctx and yields no local-test benefit
today.

## Conditions to re-open C5

C5 should be revisited *only* if **all three** conditions hold:

1. A `FakeMap` test infra exists (`Source/Tests/Maps/FakeMap.cs`) AND
   the existing tests use it for at least 4 producers/consumers.
2. We can demonstrate a sub-15-line test for the **simplest** channel
   (e.g. TimeIndicator: `DaysSinceStart = tick / TicksPerDay`).
3. The 8-channel scale-up's projected ratio re-measures below 1.5×.

Until those, the spike is recorded here as falsified.

## What survives this spike

- The architecture-review tag for C5 changes from `Speculative` to
  `Dropped` (logged in the architecture-review HTML, not in repository
  metadata).
- The `BauschuttRemapApply`-style static-field seam remains the
  established test-double idiom for the codebase. C1 (RimWorldIO
  adapter) is the next candidate to convert that idiom into a
  real interface; C5 does not partner with that effort.

## Files reverted

- `mods/05-Rimconemy-Infected-Automation/Source/Story/Snapshots/` —
  entire directory removed (3 files + empty dir).
- `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs`
  reverted via `git checkout HEAD -- <path>`. `git diff --stat` for that
  file is empty post-revert.

Working-tree state post-revert: only concurrent-thread dirty work
remains (`docs/H3-ideology-influence-matrix.md`,
`mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs`,
`mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs`,
`mods/05-Rimconemy-Infected-Automation/Source/Ideology/CollectiveDefensePostCombatPatch.cs`,
`mods/05-Rimconemy-Infected-Automation/Tests/CollectiveDefenseRegressionTests.cs`,
`mods/05-Rimconemy-Infected-Automation/VERSION`,
`mods/05-Rimconemy-Infected-Automation/Source/IsExternalInit.cs`).
None of these are spike-related. `./scripts/deploy.sh 05` is clean.
