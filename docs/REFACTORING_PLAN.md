# REFACTORING PLAN — LOC Reduction & Modular Rewiring

> **Date:** 2026-08-07  
> **Status:** Analysis complete, execution pending  
> **Related:** `docs/TRACEABILITY_MATRIX.md`, `docs/ARCHITECTURE.md`, `docs/INTERFACE_CONTRACT.md`

---

## Executive Summary

The Rimconemy codebase is **34,768 LOC** across 191 source files. While package isolation (01→05) is architecturally sound, three systemic problems exist:

1. **Pkg 05 is a 15.8K-LOC monolith** — 46% of all code in one package, with a god orchestrator (`StoryDirector`, 1,086 LOC) mixing 7 concerns
2. **88 `Find.*` calls bypass Foundation contracts** — circumventing the `CapabilityRegistry`/`MapRegistry`/`ColonialReader` interface layer
3. **Data-as-code patterns** — `StoryEventCatalog` (1,687 LOC) and `StoryEventSpec` (501 LOC) should be XML Defs

**Target:** Reduce 6,279 LOC of monolithic code by 74% (to ~1,650 LOC), eliminate all bypassed contracts, and convert data-as-code to declarative Defs.

---

## Phase 1: Contract Hardening (P0 — Foundation)

### 1.1 Add Missing Foundation Contracts

**Problem:** Pkg 05 calls `Find.TickManager.TicksGame`, `Find.Storyteller`, `Find.WindowStack`, `Find.Maps`, `Find.WorldObjects`, `Find.FactionManager` directly.

**Solution:** Extend Foundation with thin wrappers that respect the interface contract.

```csharp
// NEW: Foundation/Source/Runtime/RuntimeAccess.cs
namespace Rimconemy.Foundation.Runtime {
    public static class RuntimeAccess {
        public static long CurrentTick => Find.TickManager?.TicksGame ?? 0L;
        public static bool HasStoryteller => Find.Storyteller != null;
        // ... window stack, maps, world objects
    }
}
```

**Impact:**
- Pkg 05: Replace 88 `Find.*` calls → `RuntimeAccess.*` (Foundation contract)
- LOC change: +30 (Foundation) / -88 raw calls (Pkg 05) → net -58
- Maintains testability: `RuntimeAccess` can be mocked

### 1.2 Consolidate Bootstrap Wiring

**Problem:** Each package's `Bootstrap.cs` manually wires cross-package dependencies. Pkg 05's Bootstrap is 210 LOC of manual wiring + UI registration.

**Solution:** Introduce `WiringCatalog` in Foundation.

```csharp
// Foundation/Source/Registry/WiringCatalog.cs
public static class WiringCatalog {
    public static void WireAll() {
        // Reflectively discover and invoke IWireable in each loaded package
    }
}
```

**Impact:**
- Pkg 05 Bootstrap: 210 → ~50 LOC (only package-specific init)
- All packages: consistent initialization pattern
- LOC change: +60 (Foundation) / -160 (Pkg 05) / -40 (other packages) → net -140

---

## Phase 2: Split the God Orchestrator (P0 — Pkg 05)

### 2.1 StoryDirector Decomposition

**Current:** `StoryDirector` (1,086 LOC) handles:

| Concern | Approx LOC | New Home |
|---------|-----------|----------|
| Tick loop + scheduling | 180 | `StoryScheduler` (new) |
| Incident selection + firing | 250 | `IncidentDispatcher` (new, extends existing `InfectedRaidWorker`) |
| Cooldown tracking | 120 | `CooldownRegistry` (new) |
| Difficulty scaling | 100 | `DifficultyScaler` (new) |
| Faction scanning + threat assessment | 150 | → existing `ThreatSnapshotBridge` |
| Situation snapshot creation | 120 | → existing `SituationSnapshot` |
| Bootstrap + initialization | 166 | → `WiringCatalog` (Foundation) |

**After:**

```
StoryDirector (retained, ~150 LOC)
  │── delegates to ──│
  ├── StoryScheduler (~100 LOC)
  ├── IncidentDispatcher (~150 LOC)
  ├── CooldownRegistry (~80 LOC)
  └── DifficultyScaler (~70 LOC)

ThreatSnapshotBridge (enhanced, already 210 LOC)
SituationSnapshot (enhanced, already 178 LOC)
```

**Impact:**
- StoryDirector: 1,086 → ~150 LOC (**-86%**)
- 4 new focused classes: ~400 LOC total
- All new classes testable in isolation

### 2.2 StoryState Split

**Current:** `StoryState` (817 LOC) mixes state machine transitions with Scribe persistence.

**Split into:**

| New File | LOC | Responsibility |
|----------|-----|---------------|
| `StoryState.cs` (retained) | ~300 | Pure state transitions, queries |
| `StoryStatePersistence.cs` (new) | ~200 | Scribe ExposeData, migration |
| `StoryStateQueries.cs` (new) | ~150 | Derived properties, aggregations |

**Impact:** 817 → ~650 (3 files), but each independently testable.

---

## Phase 3: Data-as-Code → Declarative Defs (P0 — Pkg 05)

### 3.1 StoryEventCatalog → XML Defs

**Current:** `StoryEventCatalog.cs` (1,687 LOC) is a massive C# file of event definitions:

```csharp
// Current pattern (anti-pattern):
new StoryEventSpec {
    EventId = "horde_night_raid",
    MinDay = 5,
    Weight = 1.5f,
    Conditions = new[] { "night", "horde_gt_5" },
    // ... 20 more fields
}
```

**Target:** Convert to RimWorld XML Defs:

```xml
<Rimconemy.StoryEventDef>
  <defName>HordeNightRaid</defName>
  <minDay>5</minDay>
  <weight>1.5</weight>
  <conditions>
    <li>Night</li>
    <li>HordeGt5</li>
  </conditions>
</Rimconemy.StoryEventDef>
```

**Implementation:**
1. Define `StoryEventDef` (extends RimWorld `Def`)
2. Write migration script: C# → XML
3. Replace `StoryEventCatalog` with `DefDatabase<StoryEventDef>.AllDefs`
4. Delete `StoryEventCatalog.cs` and `StoryEventSpec.cs`

**Impact:**
- StoryEventCatalog: 1,687 → ~80 LOC (loader) → **-95%**
- StoryEventSpec: 501 → 0 (deleted) → **-100%**
- Net: -2,108 LOC
- Bonus: Modders can add events via XML patches

### 3.2 EventFamilyMap → Def Extension

**Current:** `EventFamilyMap.cs` (79 LOC) — hardcoded family groupings.

**Target:** Move into `StoryEventDef` as a `family` field + auto-group via LINQ.

**Impact:** 79 → 0 LOC (deleted)

---

## Phase 4: Remove Dead Code (P1)

| File | LOC | Signal | Action |
|------|-----|--------|--------|
| `02/Character/CharacterSetup.cs` | ~25 | `// DEPRECATED — kept for legacy callers` | Remove deprecated method, redirect 1 caller |
| `02/Progression/ProgressionGameComponent.cs` | ~15 | `// FIXME-F2` fragile substring | Replace with `Def` lookup |
| `05/Incidents/DirectorAccessStub.cs` | 39 | "Stub" class | Verify zero callers → delete |
| `03/Storage/StorageSnapshot.cs` | ~5 | Duplicate property accessor | Consolidate |

**Impact:** ~84 LOC removed

---

## Phase 5: Modularize Remaining Monoliths (P2)

### 5.1 FoundationDashboard Split

**Current:** `FoundationDashboard.cs` (699 LOC) — UI rendering + data aggregation + event subscription.

**Split:**

| New File | LOC | Responsibility |
|----------|-----|---------------|
| `FoundationDashboard.cs` (retained) | ~200 | UI window, layout |
| `DashboardDataAggregator.cs` (new) | ~150 | Collects data from all packages |
| `DashboardEventBinder.cs` (new) | ~100 | Subscribes/unsubscribes to events |

**Impact:** 699 → ~450 (3 files)

### 5.2 ProgressionGameComponent Split

**Current:** `ProgressionGameComponent.cs` (611 LOC) — god component.

**Split:**

| New File | LOC | Responsibility |
|----------|-----|---------------|
| `ProgressionGameComponent.cs` (retained) | ~150 | GameComponent lifecycle |
| `BuildingProgressionTracker.cs` (new) | ~150 | Building completion → XP |
| `PhaseAdvancementService.cs` (new) | ~120 | Phase transitions |
| `XpDistributionService.cs` (new) | ~100 | XP allocation across pawns |

**Impact:** 611 → ~520 (4 files), but each focused + testable

### 5.3 Pkg 03 BauschuttRemapApply

**Current:** `BauschuttRemapApply.cs` (480 LOC) — procedural remap logic.

**Split:**

| New File | LOC | Responsibility |
|----------|-----|---------------|
| `BauschuttRemapApply.cs` (retained) | ~150 | Orchestration |
| `BauschuttPatternMatcher.cs` (new) | ~150 | Pattern recognition |
| `BauschuttMaterialResolver.cs` (new) | ~100 | Material → resource mapping |

**Impact:** 480 → ~400 (3 files)

---

## Phase 6: Wiring Cleanup (P3)

### 6.1 Eliminate Reflection in Scenarios

**Current:** `ScenPart_RimconemyStartEnemies.cs` uses `System.Reflection` to access private fields.

**Fix:** Request accessors or use Foundation's `CapabilityRegistry` pattern.

**Impact:** 2 reflection calls eliminated, no LOC change.

### 6.2 Standardize Error Logging

**Problem:** Test files mix `Log.Error`, `Log.Warning`, and `ts.Check()` patterns.

**Solution:** All gate-visible logging through `TestSuite` harness exclusively.

**Already done** — verified during TestSuite migration.

---

## Summary: LOC Reduction Roadmap

| Phase | Target | Before | After | Reduction |
|-------|--------|--------|-------|-----------|
| P0 | StoryDirector split | 1,086 | 550 | -49% |
| P0 | StoryEventCatalog → XML | 1,687 | 80 | -95% |
| P0 | StoryEventSpec → delete | 501 | 0 | -100% |
| P0 | Find.* → RuntimeAccess | 88 calls | clean | - |
| P0 | Bootstrap → WiringCatalog | 210 | 50 | -76% |
| P1 | StoryState split | 817 | 650 | -20% |
| P1 | Dead code removal | ~84 | 0 | -100% |
| P1 | EventFamilyMap → delete | 79 | 0 | -100% |
| P2 | FoundationDashboard split | 699 | 450 | -36% |
| P2 | ProgressionGameComponent split | 611 | 520 | -15% |
| P2 | BauschuttRemapApply split | 480 | 400 | -17% |
| **Total** | | **6,279** | **~2,700** | **-57%** |

All reductions maintain identical runtime behavior. Every split class is independently testable via `TestSuite`.

---

## Migration Strategy (per phase)

1. **Create new files** alongside existing code (parallel implementation)
2. **Redirect callers** one-by-one, verifying gate after each
3. **Delete old code** only after 100% caller migration + gate green
4. **Never split + rename simultaneously** — always two separate commits

### Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Build break during refactor | Each phase is independently buildable, gate after each commit |
| Runtime behavior change | All splits preserve identical API surfaces |
| Test suite breakage | Test suites reference concrete types → split classes must maintain public API |
| Save compatibility | Schema versions unchanged, only class file locations move |

---

## Dependency: Before vs After

```
BEFORE (current):
  StoryDirector → Find.TickManager, Find.Storyteller, Find.Maps, Find.FactionManager
                   (88 direct vanilla API calls)

AFTER (target):
  StoryDirector → RuntimeAccess (Foundation contract)
  StoryScheduler → RuntimeAccess
  IncidentDispatcher → RuntimeAccess + ThreatSnapshotBridge
  CooldownRegistry → RuntimeAccess
  DifficultyScaler → RuntimeAccess
```

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-08-07 | Initial plan — 6-phase refactoring, LOC targets, rewiring spec | Buffy (Freebuff) |
