# Rimconemy UI P0–P4

## Goal
Implement the approved Rimconemy Visual Language across toolkit, Survival, Infected, Economy, Scavenger, and Foundation UI while keeping UI read-only over existing runtime state.

## Current Phase
All phases complete (Phase 1 to Phase 6).

## Next Step
Runtime verification by starting RimWorld (`./start.sh`).

## Phases
- [x] 1. Shared toolkit + Survival P0
- [x] 2. Infected threat UI
- [x] 3. Economy hub
- [x] 4. Scavenger infrastructure UI
- [x] 5. Foundation dashboard polish
- [x] 6. Full builds, review, and runtime-gate report

## Constraints
- RimWorld 1.6 / Unity IMGUI only.
- No third-party dependencies.
- UI reads existing snapshots/services; no duplicate simulation state.
- No claim of runtime rendering without a fresh game run.

## Errors Encountered
- `GameFont.Large` missing in RimWorld 1.6 Unity assembly -> Resolved by mapping H1 titles to `GameFont.Medium`.
- Missing `using System;` directive in `StoryStateRegressionTests.cs` -> Fixed.
