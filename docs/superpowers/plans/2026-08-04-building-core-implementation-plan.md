# Building Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement and verify the Meilenstein-A Building-Core: physical ConstructionDebris wall/door construction, storage readback, fueled power generation, powered turret state, and capability-gated read models across all five mods.

**Architecture:** Package 03 remains the owner of physical resources, Defs, construction-cost patches, StorageQuery, PowerChainService, and the BuildingSnapshot read model. Package 01 exposes the capability/diagnostic contract; Package 02 consumes successful building output through a bounded, idempotent Building XP adapter; Package 04 consumes physical building inputs through a read-only/late-bound physical-input adapter without credit duplication; Package 05 consumes the building/power snapshot as threat input without implementing the later raid spawn. No package writes another package's state directly.

**Tech Stack:** RimWorld 1.6.4566, C#/.NET Standard 2.1, RimWorld Def/XML/PatchOperation, Harmony only where existing conventions require it, bootstrap regression tests using `RunAll()` and `Verse.Log`.

## Global Constraints

- Physical resources remain real RimWorld Things; Credits remain wallet data.
- Package 03 owns physical Building/Power/Storage state.
- Cross-package readers use existing Foundation capability gates or defensive late-bound reflection.
- No XP from ticks alone; one Building XP award per validated output/idempotency key.
- No global vanilla raid suppression and no Mechadroid/Raid implementation in Milestone A.
- Every new state has a schema/version marker and deterministic hash or ID where applicable.
- Runtime boot tests do not claim actual construction, Save/Load, or power operation without an interactive game evidence run.

---

### Task 1: Lock the current APIs and add the failing A-gate tests

**Files:**
- Create: `mods/03-Rimconemy-Scavenger-Infrastructure/Tests/BuildingCoreRegressionTests.cs`
- Create: `mods/02-Rimconemy-Survival-Progression/Tests/BuildingProgressionRegressionTests.cs`
- Create: `mods/04-Rimconemy-Economy-Territory/Tests/BuildingInputRegressionTests.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/BuildingThreatRegressionTests.cs`
- Modify: each package `Source/Bootstrap.cs` to call its new `RunAll()` after existing tests/markers.

**Interfaces:**
- Package 03 tests consume `ResourceCategory`, `PowerChainService` constants, `StorageSnapshot`, and the XML contract by deterministic source/Def assertions.
- Package 02 tests consume the new `BuildingProgressionAdapter` pure API.
- Package 04 tests consume the new late-bound/read-only physical input adapter.
- Package 05 tests consume the new `BuildingThreatAdapter` pure API.

- [ ] **Step 1: Write red tests for the existing broken patch and missing adapters.**

The tests must fail for these exact reasons before implementation:

```csharp
// Package 03: the patch must contain an Add operation that explicitly adds
// Stony to an existing Wall and Door stuffCategories node.
AssertTrue(ReadText("Patches/Bauschutt_Remap_Patches.xml")
    .Contains("<li>Stony</li>"), "Building: Stony category is declared");
AssertTrue(ReadText("Patches/Bauschutt_Remap_Patches.xml")
    .Contains("ThingDef[defName=\"Wall\"]/stuffCategories"),
    "Building: Wall category path exists");
AssertTrue(ReadText("Patches/Bauschutt_Remap_Patches.xml")
    .Contains("ThingDef[defName=\"Door\"]/stuffCategories"),
    "Building: Door category path exists");
```

The pure adapter tests must assert concrete contracts:

```csharp
AssertEqual(BuildingProgressionAdapter.BuildingWorkTypeId, "Building", "Building XP work type");
AssertTrue(BuildingProgressionAdapter.TryCreateAward("build-1", "pawn-1", 12, out var award), "valid build award");
AssertEqual(award.Amount, 12, "award amount");
AssertFalse(BuildingProgressionAdapter.TryCreateAward("build-1", "pawn-1", 12, out _), "duplicate award rejected");
AssertEqual(BuildingThreatAdapter.ComputePressure(2, 1, 0.5f), 0.35f, "building pressure deterministic");
AssertTrue(BuildingInputAdapter.IsPhysicalInput("Rimconemy_ConstructionDebris"), "debris remains physical input");
AssertFalse(BuildingInputAdapter.IsCreditInput("Rimconemy_ConstructionDebris"), "debris is not credits");
```

- [ ] **Step 2: Run the package builds/tests and confirm the new tests fail because the new APIs are absent or the patch contract is incomplete.**

Run:

```bash
./scripts/deploy.sh --all --no-build
```

Expected: the source assertions for the missing adapter types fail to compile, or the isolated test runner reports missing symbols. Do not treat the current runtime boot as a green A-gate yet.

---

### Task 2: Fix the physical ConstructionDebris Wall/Door cost path

**Files:**
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Patches/Bauschutt_Remap_Patches.xml`
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/ConstructionDebris.xml`
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Tests/BuildingCoreRegressionTests.cs`

**Interfaces:**
- Produces a RimWorld 1.6-compatible conditional patch that adds `Stony` to existing Wall/Door stuff categories without replacing unrelated categories.
- ConstructionDebris remains a `ResourceBase` ThingDef with `stuffProps.categories/Stony` and stable defName `Rimconemy_ConstructionDebris`.

- [ ] **Step 1: Replace the empty existing-node operations with explicit add operations.**

Use `PatchOperationAdd` under the existing category node and add only the missing category element:

```xml
<match Class="PatchOperationAdd">
  <xpath>Defs/ThingDef[defName="Wall"]/stuffCategories</xpath>
  <value><li>Stony</li></value>
</match>
```

Repeat for `Door`. Keep the no-node branch only for creating the complete category list. Do not remove `Woody` or `Metallic`.

- [ ] **Step 2: Add a source-level regression assertion against empty `<value>` branches and verify the resource material contract.**

- [ ] **Step 3: Run XML parsing and the package-03 build.**

Commands:

```bash
python3 - <<'PY'
import xml.etree.ElementTree as ET
ET.parse('mods/03-Rimconemy-Scavenger-Infrastructure/Patches/Bauschutt_Remap_Patches.xml')
ET.parse('mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/ConstructionDebris.xml')
print('xml-ok')
PY
dotnet build mods/03-Rimconemy-Scavenger-Infrastructure/Rimconemy.ScavengerInfrastructure.csproj -c Release
```

Expected: `xml-ok`, build succeeds with 0 errors.

---

### Task 3: Implement Package-03 BuildingSnapshot and truthful PowerChain state

**Files:**
- Create: `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Building/BuildingSnapshot.cs`
- Create: `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Building/BuildingSnapshotService.cs`
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Power/PowerChainService.cs`
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Source/UI/InfrastructureDashboard.cs`
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Tests/BuildingCoreRegressionTests.cs`

**Interfaces:**

```csharp
public enum BuildingConstructionState { Unknown, Planned, Built, Damaged, Destroyed }
public enum BuildingPowerState { Unknown, Offline, Blocked, Online }
public sealed class BuildingSnapshot
{
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion;
    public long SnapshotTick;
    public int ThingId;
    public int MapId;
    public string DefName;
    public BuildingConstructionState ConstructionState;
    public BuildingPowerState PowerState;
    public bool HasFuel;
    public string ContentHash;
}
public static List<BuildingSnapshot> BuildingSnapshotService.Read(long tick)
```

- [ ] **Step 1: Implement a read-only snapshot over loaded player-home maps.**

Include only DefNames owned by Package 03 (`Rimconemy_WoodCoalGenerator`, `Rimconemy_WaterTurbineGenerator`, `Rimconemy_TurbineWaterPump`, `Rimconemy_ArrowTurret_Power`). Use `CompPowerTrader.PowerOn` and `CompRefuelable.Fuel` where present. Use deterministic ThingId/MapId/DefName sorting and a stable FNV-1a hash.

- [ ] **Step 2: Make PowerChainService consume the same snapshot semantics.**

Do not claim a unit is fueled merely because no refuelable component exists; mark non-fuel consumers as `HasFuel=true` only for the turret/pump classification, and expose `PowerState` distinctly from `HasFuel`. Preserve the existing public `PowerChainSnapshot` shape for callers.

- [ ] **Step 3: Add Building section to InfrastructureDashboard.**

Display DefName/label, online/offline/blocked state, fuel state, map and snapshot tick. Keep the UI read-only and use explicit empty state when no Building exists.

- [ ] **Step 4: Run package-03 regression tests and build.**

Expected: all BuildingCore tests pass and package 03 builds with 0 errors.

---

### Task 4: Add Package-01 capability/diagnostic ownership

**Files:**
- Modify: `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs` or the existing capability declaration file identified during implementation.
- Modify: `mods/01-Rimconemy-Foundation/Source/Bootstrap.cs`
- Create/modify: `mods/01-Rimconemy-Foundation/Tests/FoundationBuildingCapabilityTests.cs`

**Interfaces:**
- Capability ID: `rimconemy.scavengerinfrastructure.building`, version `1`.
- Reader gate: `CapabilityAudit.HasCapabilityOrWarn("rimconemy.scavengerinfrastructure", "rimconemy.scavengerinfrastructure.building", 1, readerContext)`.

- [ ] **Step 1: Add the building capability to the canonical registry descriptor.**

- [ ] **Step 2: Add a Foundation regression test that the capability is declared and missing capability warnings are deduplicated.**

- [ ] **Step 3: Log a single Foundation marker for the Building capability without pretending live construction is verified.**

- [ ] **Step 4: Build package 01 and run its bootstrap tests.**

---

### Task 5: Add Package-02 validated Building XP adapter

**Files:**
- Create: `mods/02-Rimconemy-Survival-Progression/Source/Progression/BuildingProgressionAdapter.cs`
- Create/modify: `mods/02-Rimconemy-Survival-Progression/Tests/BuildingProgressionRegressionTests.cs`
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/Bootstrap.cs`

**Interfaces:**

```csharp
public static class BuildingProgressionAdapter
{
    public const string BuildingWorkTypeId = "Building";
    public static bool TryCreateAward(string idempotencyKey, string pawnId, int amount, out BuildingXpAward award);
}
public struct BuildingXpAward { public string Key; public string PawnId; public int Amount; }
```

- [ ] **Step 1: Implement deterministic, in-memory deduplication for the current game session.**

Reject null/empty keys, non-positive amounts and duplicate keys. Do not call this from a tick loop; a future job-output hook will supply validated completion events.

- [ ] **Step 2: Add runtime marker stating the adapter is available but job-output hook/live XP is still open.**

- [ ] **Step 3: Run package-02 build and tests.**

---

### Task 6: Add Package-04 physical Building input adapter

**Files:**
- Create: `mods/04-Rimconemy-Economy-Territory/Source/Building/BuildingInputAdapter.cs`
- Create/modify: `mods/04-Rimconemy-Economy-Territory/Tests/BuildingInputRegressionTests.cs`
- Modify: `mods/04-Rimconemy-Economy-Territory/Source/Bootstrap.cs`

**Interfaces:**

```csharp
public static class BuildingInputAdapter
{
    public static bool IsPhysicalInput(string defName);
    public static bool IsCreditInput(string defName);
    public static int RequiredUnits(string defName, string buildingDefName);
}
```

- [ ] **Step 1: Implement only classification and deterministic requirement lookup.**

`Rimconemy_ConstructionDebris`, `Rimconemy_DistilledWater`, `WoodLog`, `Chemfuel`, `Steel` are physical; `Credits`, `Silver` are not physical Building inputs. Unknown IDs return false/0. No inventory mutation and no wallet booking in A.

- [ ] **Step 2: Add marker that Economy can read Building input contracts but physical transfer remains a B gate.**

- [ ] **Step 3: Run package-04 build and tests.**

---

### Task 7: Add Package-05 Building threat adapter

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Building/BuildingThreatAdapter.cs`
- Create/modify: `mods/05-Rimconemy-Infected-Automation/Tests/BuildingThreatRegressionTests.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs`

**Interfaces:**

```csharp
public static class BuildingThreatAdapter
{
    public static float ComputePressure(int activeGenerators, int activeTurrets, float damageRatio);
    public static string BuildDeterminismKey(long tick, string buildingHash, string powerHash);
}
```

- [ ] **Step 1: Implement a bounded deterministic pressure adapter.**

Use explicit weighting and clamp to `[0,1]`: `generators * 0.10 + turrets * 0.15 + damageRatio * 0.25`, clamped. This is a read-model adapter only; it does not queue or spawn raids.

- [ ] **Step 2: Add it to the situation/Threat read path only if the existing snapshot can consume it without a cross-package compile dependency.**

Otherwise emit a capability-gated marker and leave the later StoryDirector bridge for B/C.

- [ ] **Step 3: Run package-05 build and tests.**

---

### Task 8: Build/deploy all five and run Milestone-A verification

**Files:**
- Modify: `mods/*/ROADMAP.md` and/or `docs/CODE_STATUS.md` only after runtime evidence is available.
- Modify: `docs/superpowers/specs/2026-08-04-building-feature-full-design.md` only if implementation changes the accepted contract.

- [ ] **Step 1: Build all packages in Release mode.**

```bash
./scripts/deploy.sh --all
```

Expected: all five projects build with 0 errors and deploy to the local RimWorld Mods directory.

- [ ] **Step 2: Run static gates.**

```bash
python3 - <<'PY'
import xml.etree.ElementTree as ET
from pathlib import Path
for p in Path('mods').rglob('*.xml'):
    ET.parse(p)
print('all-xml-ok')
PY
bash -n scripts/runtime_test.sh
```

- [ ] **Step 3: Run the bounded runtime boot test.**

```bash
./scripts/runtime_test.sh --require-scenario-tests
```

Expected: fresh Player.log, all five packages, Foundation capability marker, all existing regressions, new Building regression summaries, and zero forbidden Need/Sandbox/Patch/Market errors.

- [ ] **Step 4: Record the interactive A-gates separately.**

The boot script must not claim these automatically. Verify in RimWorld:

1. spawn/store ConstructionDebris;
2. place Wall and Door and verify the build-material UI accepts the intended material;
3. build generator and turret;
4. remove/refuel input and observe Offline/Blocked → Online transition;
5. confirm InfrastructureDashboard shows the same resource/power/building states;
6. save, exit, reload, and confirm state reconstruction.

- [ ] **Step 5: Review changed files and update docs only with evidence.**

Run `git diff --check` if a repository root is available; otherwise run whitespace/XML/Markdown checks and report repository boundary. Use code review before declaring A complete.

---
