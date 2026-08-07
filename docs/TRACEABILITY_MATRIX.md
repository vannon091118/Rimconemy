# TRACEABILITY MATRIX — Rimconemy Mod Suite

> **Generated:** 2026-08-07  
> **Scope:** All 5 packages, source files only (excl. Tests/)  
> **Purpose:** Full audit trail of when each module was created, what it depends on, and what concerns it mixes. Enables targeted refactoring with historical context.

---

## Summary Matrix

| Pkg | Name | Source Files | Total LOC | Avg LOC/file | Monoliths (>300) | Dead Code Candidates |
|-----|------|-------------|-----------|-------------|-------------------|---------------------|
| 01 | Foundation | 42 | 5,849 | 139 | 4 | 0 |
| 02 | Survival Progression | 42 | 7,237 | 172 | 4 | 1 (DEPRECATED) |
| 03 | Scavenger Infrastructure | 17 | 3,450 | 203 | 3 | 0 |
| 04 | Economy Territory | 11 | 2,401 | 218 | 4 | 0 |
| 05 | Infected Automation | 79 | 15,831 | 200 | 13 | 2 |
| **Total** | | **191** | **34,768** | **182** | **28** | **3** |

---

## Package 01 — Foundation (`mods/01-Rimconemy-Foundation/Source/`)

### Creation Timeline

| Batch | Date | Files | Description |
|-------|------|-------|-------------|
| Batch 1 | 2026-08-04 10:36 | 26 files | Core scaffolding: Save, UI, Registry, Profile, DLC, Catalog, Models |
| Batch 2 | 2026-08-04 16:16 | 3 files | Canonical layer: MaterialIdentity, RoomRoleResolver, SettingIdentity |
| Batch 3 | 2026-08-04 17:37 | 5 files | Schema migration: ISchemaMigratable, MigrationRegistry, etc. |
| Batch 4 | 2026-08-05 01:09 | 2 files | MapRegistry, RuntimeMeter |
| Batch 5 | 2026-08-05 05:02 | 1 file | StorytellerInventory |
| Batch 6 | 2026-08-05 19:30 | 4 files | RimPad UI |
| Batch 7 | 2026-08-05 19:32 | 2 files | CapabilityAudit, EventBridge |
| Batch 8 | 2026-08-06 22:52 | 1 file | IntroFlowWindow |
| Batch 9 | 2026-08-06 22:59 | 1 file | ITutorialTriggerBridge |

### Monolithic Files (>300 LOC)

| File | LOC | Concerns Mixed | Risk |
|------|-----|---------------|------|
| `UI/FoundationDashboard.cs` | 699 | UI rendering + Data aggregation + Event subscription + State display | High — UI mixed with business logic |
| `Save/FoundationSaveData.cs` | 463 | Save/Load + Schema versioning + Cross-ref validation | Medium — Scribe concerns |
| `Canonical/MaterialIdentity.cs` | 342 | Canonical identity + Comparison + Hash | Low — well-scoped |
| `UI/RimconemyUi.cs` | 339 | Shared UI primitives + Layout + Theme binding | Low — utility class |

### Dependency Map (selected)

```
FoundationSaveData → MigrationRegistry, ISchemaMigratable, PackageRegistry
FoundationDashboard → EventLog, ProfileDetector, PackageRegistry, CapabilityAudit
ProfileDetector → FoundationDefInventory, DLCFilter, PackageRegistry
PackageRegistry → PackageDescriptor, PackageSnapshot
CrossPackageState → PackageRegistry (reflection-based capability lookup)
MapRegistry → (standalone, consumed by Pkg 02/03/05)
```

---

## Package 02 — Survival Progression (`mods/02-Rimconemy-Survival-Progression/Source/`)

### Creation Timeline

| Batch | Date | Files | Description |
|-------|------|-------|-------------|
| Batch 1 | 2026-08-04 10:36 | 18 files | Character setup, Needs, Progression, GameOver, UI |
| Batch 2 | 2026-08-04 17:37 | 2 files | Need amplification hediffs |
| Batch 3 | 2026-08-05 00:03 | 10 files | Domain XP, Building completion hooks, Scenarios |
| Batch 4 | 2026-08-05 01:09 | 3 files | Mining gates, Harmony patches |
| Batch 5 | 2026-08-05 02:26 | 3 files | Phase progression |
| Batch 6 | 2026-08-05 03:45 | 1 file | BundledSkillAllocation |
| Batch 7 | 2026-08-05 05:02 | 1 file | ConstructionSpeed stat part |
| Batch 8 | 2026-08-05 06:18 | 7 files | Construction, Cooking, Farming, Roles |
| Batch 9 | 2026-08-05 13:38 | 2 files | Axe durability |
| Batch 10 | 2026-08-05 18:24 | 1 file | Tree cutting gate |
| Batch 11 | 2026-08-06 22:59 | 3 files | Survival bridges + stubs |

### Monolithic Files (>300 LOC)

| File | LOC | Concerns Mixed | Risk |
|------|-----|---------------|------|
| `Progression/ProgressionGameComponent.cs` | 611 | GameComponent lifecycle + Building progression + XP distribution + Phase tracking + FIXME-F2 | **High** — god component |
| `Character/CharacterSetupState.cs` | 420 | State machine + Schema migration + Serialization + Validation | Medium |
| `Character/CharacterSetup.cs` | 401 | Orchestration + Budget calculation + Trait assignment + **DEPRECATED** code | Medium |
| `Character/SkillBudgetWindow.cs` | 383 | UI window + Budget display + Interaction | Low |

### Dead Code Signals

| File | Line | Signal | Action |
|------|------|--------|--------|
| `Character/CharacterSetup.cs` | 38 | `// DEPRECATED — kept for legacy callers` | Remove, redirect callers to SkillBudgetCalculator |
| `Progression/ProgressionGameComponent.cs` | 506 | `// FIXME-F2` fragile substring match | Replace with typed approach |

### Dependency Map

```
ProgressionGameComponent → BuildingCompletionBridge, DomainXpState, UnlockService
CharacterSetup → SkillBudgetCalculator, TraitAssigner, BundledSkillAllocation
NeedMapping → NeedAmplifier, SurvivalNeedCategory
PhaseProgressResolver → PhaseContractGate, ProgressionDomain
BuildingCompletionBridge → BuildingProgressionAdapter, ProgressionSnapshot
```

---

## Package 03 — Scavenger Infrastructure (`mods/03-Rimconemy-Scavenger-Infrastructure/Source/`)

### Creation Timeline

| Batch | Date | Files | Description |
|-------|------|-------|-------------|
| Batch 1 | 2026-08-04 10:36 | 9 files | Storage, Power, Plants, Building snapshots, UI |
| Batch 2 | 2026-08-04 11:22 | 1 file | CaravanStorageEnumerator |
| Batch 3 | 2026-08-04 11:33 | 4 files | ArrowTurret, BauschuttRemap, FoodHarvest, FueledGenerator |
| Batch 4 | 2026-08-04 17:49 | 2 files | BauschuttRemapApply, Designator |
| Batch 5 | 2026-08-05 00:03 | 1 file | StorageWriteMutationService |

### Monolithic Files (>300 LOC)

| File | LOC | Concerns Mixed | Risk |
|------|-----|---------------|------|
| `Building/BauschuttRemapApply.cs` | 480 | Remap logic + Validation + Edge cases + Map iteration | High — procedural complexity |
| `Storage/StorageQuery.cs` | 471 | Query builder + Aggregation + Filter logic + Cache | Medium |
| `Building/ArrowTurretPowerGate.cs` | 381 | Power gate + Turret state + Capability checks | Medium |
| `Power/PowerChainService.cs` | 320 | Power network traversal + Calculation + Caching | Medium |

### Dependency Map

```
StorageQuery → StorageSnapshot, MapRegistry (Foundation)
PowerChainService → MapRegistry (Foundation), BuildingSnapshotService
BauschuttRemapApply → StorageQuery, MapRegistry (Foundation)
InfrastructureDashboard → BuildingSnapshotService, PowerChainService, StorageQuery, FoodHarvestCycleService, Foundation.UI
```

---

## Package 04 — Economy Territory (`mods/04-Rimconemy-Economy-Territory/Source/`)

### Creation Timeline

| Batch | Date | Files | Description |
|-------|------|-------|-------------|
| Batch 1 | 2026-08-04 10:36 | 10 files | Wallet, Market, Outposts, Transfers, UI |
| Batch 2 | 2026-08-04 11:33 | 1 file | OutpostProxyGraph |

### Monolithic Files (>300 LOC)

| File | LOC | Concerns Mixed | Risk |
|------|-----|---------------|------|
| `Wallet/CreditsLedger.cs` | 468 | Ledger entries + Balance + Schema migration + Query | Medium |
| `Market/Market.cs` | 388 | Trade matching + Price calculation + Order book | Medium |
| `Outposts/Outpost.cs` | 359 | Production + Timer + Investment + State | Medium |
| `Wallet/TradePanel.cs` | 358 | UI + Transaction + Validation | Medium |

### Dependency Map

```
EconomyHub → CreditsLedger, Market, PhysicalTransfer, Outpost
PhysicalTransfer → StorageSnapshot (Pkg 03), BuildingInputAdapter
OutpostProxyGraph → ColonialReader (Foundation)
```

---

## Package 05 — Infected Automation (`mods/05-Rimconemy-Infected-Automation/Source/`)

⚠️ **This is the primary monolith.** 79 files, 15,831 LOC, 13 files over 300 LOC.

### Creation Timeline

| Batch | Date | Files | Description |
|-------|------|-------|-------------|
| Batch 1 | 2026-08-04 10:36 | 17 files | Story core, Incidents, Ideology, Mechadroids |
| Batch 2 | 2026-08-04 11:09–11:33 | 8 files | Ideology defenses, Setting rules, Incidents, World |
| Batch 3 | 2026-08-05 00:03 | 3 files | Threat bridge, Start enemies, Scenarios |
| Batch 4 | 2026-08-05 06:18–07:07 | 11 files | World perception: Chunks, Sight, Light, Noise, Infected behavior |
| Batch 5 | 2026-08-05 11:59–12:41 | 7 files | Population ledger, Inoculation, Infected packs |
| Batch 6 | 2026-08-05 13:41–13:53 | 8 files | Horde system: Calculator, Spawner, Layers, Migration |
| Batch 7 | 2026-08-05 14:42–15:03 | 3 files | Animal infection |
| Batch 8 | 2026-08-05 16:17–16:39 | 6 files | Horde manifest, Materialization, Migration driver |
| Batch 9 | 2026-08-05 17:03–19:41 | 5 files | Faction utility, Scenarios, Intro sequence |
| Batch 10 | 2026-08-06 22:59 | 4 files | Tutorial system |

### Monolithic Files (>300 LOC) — The "Big 13"

| File | LOC | Concerns Mixed | Risk |
|------|-----|---------------|------|
| `Story/StoryEventCatalog.cs` | 1,687 | Event definitions + Descriptions + Conditions + Weights + DLC filtering + Placeholder expansion | **Critical** — data blob disguised as code |
| `Story/StoryDirector.cs` | 1,086 | Orchestration + Tick loop + Incident firing + Cooldowns + Difficulty scaling + Faction scanning + `Find.*` calls (15+) | **Critical** — god orchestrator |
| `Story/StoryState.cs` | 817 | State machine + Persistence + Query + Schema migration + Tick tracking + FirstWipe detection | **High** |
| `Story/StorySelector.cs` | 623 | Event selection + Weight calculation + Filtering + Phase awareness | **High** |
| `World/ChunkController.cs` | 502 | Chunk lifecycle + Spawning + Cleanup + Map iteration + Perception hooks | High |
| `Story/StoryEventSpec.cs` | 501 | Event spec definitions + Cooldowns + Conditions + Weight formulas | High — should be XML Defs |
| `Population/PopulationLedger.cs` | 464 | Ledger entries + Schema migration + Query + Reconciler integration + Save/Load | High |
| `Story/TutorialDirector.cs` | 394 | Tutorial state machine + Step progression + UI triggers + Letter dispatch | Medium |
| `World/ColonistSightSystem.cs` | 350 | Sight calculation + FOV + Perception layer + Chunk integration | Medium |
| `World/DarknessSectionLayerLifecycle.cs` | 326 | Section layer + Darkness calculation + Visibility thresholds | Medium |
| `Inoculation/RandomInoculationService.cs` | 318 | Inoculation logic + Candidate selection + Infection spread + Tick scheduling | Medium |
| `World/InfectedBehavior.cs` | 288 | Behavior tree + Pathfinding + Target selection | Medium |
| `World/SightConeMath.cs` | 270 | Math utility + Cone calculation + Distance | Low — utility |

### Critical Cross-Package Coupling (bypassing CapabilityRegistry)

Pkg 05 uses `Find.*` directly **88+ times**, including:

| Vanilla API | Occurrences | Files |
|-------------|-------------|-------|
| `Find.TickManager.TicksGame` | 22 | StoryDirector, StoryState, ChunkController, HordeCalculator, etc. |
| `Find.AnyPlayerHomeMap` | 12 | StoryDirector, PlaceholderResolver, RandomInoculationService, InfectedRaidSpawnService |
| `Find.Storyteller` | 8 | StoryDirector, ScenPart_RimconemyStartEnemies, StorySelector |
| `Find.WindowStack` | 5 | TutorialDirector, ScenPart_IntroSequence, RimconemyTutorialLetter |
| `Find.Maps` | 3 | ChunkController, InfectedRaidSpawnService |
| `Find.WorldObjects` | 3 | HordeSpawner, HordeMigrationDriver |
| `Find.FactionManager` | 2 | StoryDirector, InfectedFactionUtility |
| `Find.Scenario` | 1 | ScenPart_RimconemyStartEnemies |
| `Reflection` (System) | 2 | ScenPart_RimconemyStartEnemies |

**Problem:** These bypass the `MapRegistry`, `ColonialReader`, and `CapabilityRegistry` contracts established in Foundation. Pkg 05 should access these through the Foundation interfaces.

### Dead Code / Stale Artifacts

| File | LOC | Signal | Action |
|------|-----|--------|--------|
| `Incidents/DirectorAccessStub.cs` | 39 | "Stub" — likely test-only | Verify usage, remove if dead |
| `Bootstrap.cs` | 210 | Mixes boot + UI registration + cross-package wiring | Extract wiring to dedicated class |

### Concern Overlap Matrix (Pkg 05)

```
                    Story  World  Population  Horde  Inoculation  Ideology  Incidents
StoryDirector       ✦✦✦    ✦      ✦           ✦      ✦            ✦         ✦
StoryState          ✦✦✦    -      -           -      -            -         -
ChunkController     ✦      ✦✦✦    ✦           ✦      -            -         -
PopulationLedger    ✦      -      ✦✦✦         ✦      ✦            -         -
HordeMigrationDriver✦      ✦      ✦           ✦✦✦    -            -         -
RandomInoculation   ✦      ✦      ✦           -      ✦✦✦          -         -
ColonistSightSystem -      ✦✦✦    ✦           -      -            -         -
IdeologyAssigner    ✦      -      -           -      -            ✦✦✦       -
InfectedRaidWorker  ✦      -      -           -      -            -         ✦✦✦

✦ = mixed concern (should be isolated)     ✦✦✦ = primary concern
```

`StoryDirector` mixes **all 7 concerns** — it's the god orchestrator.

---

## Global Dependency Graph (Simplified)

```
                        ┌─────────────────┐
                        │    Foundation    │
                        │   (Pkg 01)       │
                        │ MapRegistry      │
                        │ CapabilityRegistry│
                        │ ColonialReader   │
                        │ TestSuite        │
                        └────────┬────────┘
                                 │
           ┌─────────────────────┼─────────────────────┐
           │                     │                     │
    ┌──────▼──────┐      ┌──────▼──────┐      ┌──────▼──────┐
    │  Survival   │      │  Scavenger  │      │   Economy   │
    │  (Pkg 02)   │      │  (Pkg 03)   │      │  (Pkg 04)   │
    │             │      │             │      │             │
    │ depends on: │      │ depends on: │      │ depends on: │
    │ Foundation  │      │ Foundation  │      │ Foundation  │
    │             │      │             │      │ Scavenger   │
    └─────────────┘      └─────────────┘      └─────────────┘
                                 │
                          ┌──────▼──────┐
                          │  Infected   │
                          │  (Pkg 05)   │
                          │             │
                          │ depends on: │
                          │ Foundation  │ (via contract)
                          │ BUT ALSO:   │
                          │ Find.* (88×)│ ← BYPASSES contract
                          │ Reflection  │ ← BYPASSES contract
                          └─────────────┘
```

**Key finding:** Pkg 05 should only depend on Foundation contracts, but 88 `Find.*` calls and 2 Reflection calls bypass the interface layer. This creates hidden coupling to vanilla RimWorld APIs that should be abstracted through Foundation.

---

## Refactoring Priority (by risk × impact)

| Priority | Target | Current LOC | Target LOC | Reduction |
|----------|--------|-------------|------------|-----------|
| 🔴 P0 | StoryEventCatalog → XML Defs | 1,687 | ~50 (loader) + XML | -97% |
| 🔴 P0 | StoryDirector → split into 4 classes | 1,086 | ~400 (4 × 100) | -63% |
| 🟠 P1 | StoryEventSpec → merge into XML migration | 501 | 0 (deleted) | -100% |
| 🟠 P1 | Replace Find.* with Foundation contracts | 88 calls | ~30 wrapper calls | -66% |
| 🟠 P1 | Bootstrap.cs → WiringRegistry | 210 | ~80 (stripped) | -62% |
| 🟡 P2 | FoundationDashboard → split UI from logic | 699 | ~350 (2 files) | -50% |
| 🟡 P2 | ProgressionGameComponent → split | 611 | ~300 (3 files) | -51% |
| 🟡 P2 | Remove dead code (DEPRECATED, FIXME-F2, stubs) | ~90 | 0 | -100% |
| 🟢 P3 | StoryState → split persistence from state | 817 | ~500 (2 files) | -39% |
| **Total** | | **6,279** | **~1,650** | **-74%** |

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-08-07 | Initial matrix — full file audit, concern mapping, dependency graph | Buffy (Freebuff) |
