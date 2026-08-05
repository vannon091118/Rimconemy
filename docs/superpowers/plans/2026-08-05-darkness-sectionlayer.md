# Darkness/Fog-of-War SectionLayer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the broken Screen-Space `GUI.DrawTexture` darkness grid with a lückenfreien RimWorld-1.6 World-Space SectionLayer and add line-of-sight occlusion.

**Architecture:** Keep `ColonistSightSystem` as the owner of the per-cell float visibility grid. Add a focused SectionLayer renderer using `LayerSubMesh` vertex colors and controlled Section lifecycle integration; patch the existing vanilla darkness layer only if its locally inspected lifecycle is safer than injecting a second layer. Keep the old GUI renderer disabled once the new layer has compiled and completed a map-load gate.

**Tech Stack:** C# netstandard2.1, RimWorld 1.6.4566 `Assembly-CSharp`, Unity built-in mesh/material APIs, Harmony already used by Package 05, existing in-game regression bootstrap.

## Global Constraints

- Package isolation: all implementation belongs to `mods/05-Rimconemy-Infected-Automation`; no new cross-package reference.
- No Vanilla-file edits: do not modify RimWorld installation files or `Data/Core/Languages/English/LangIcon.png`.
- No Screen-Space map overlay: no `GUI.DrawTexture`, `Camera.current.WorldToScreenPoint`, or per-block `Rect` rendering in the final path.
- Vanilla API evidence: use only signatures verified against local RimWorld 1.6.4566 `Assembly-CSharp.dll`.
- Existing float visibility grid `[0,1]` remains the data SSOT; do not introduce persistent `0/1/2` Fog state in this iteration.
- No Recursive Shadowcasting in this iteration; use `GenSight.LineOfSight(IntVec3, IntVec3, Map)` for occlusion.
- No concurrent old/new darkness layers: one active renderer only.
- Save/load must not throw; render state resets and regenerates after map initialization.
- Never claim visual success from static tests alone; the final no-gap criterion requires a live RimWorld test.

---

## File Map

### Existing files to modify

- `mods/05-Rimconemy-Infected-Automation/Source/World/ColonistSightSystem.cs`
  - retain visibility-grid ownership and tick scheduling;
  - add LOS gating, dirty invalidation, and Section regeneration requests;
  - remove the old `MapComponentOnGUI` block renderer only after the replacement path is active.
- `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs`
  - register new focused regression tests in the existing Package 05 bootstrap order.
- `mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj`
  - only if the verified SectionLayer implementation requires a Unity module reference not already present.
- `mods/05-Rimconemy-Infected-Automation/Tests/ColonistSightSystemRegressionTests.cs`
  - add pure visibility/occlusion and range assertions.

### Files to create

- `mods/05-Rimconemy-Infected-Automation/Source/World/DarknessSectionLayer.cs`
  - one SectionLayer implementation; owns only mesh generation and material setup.
- `mods/05-Rimconemy-Infected-Automation/Source/World/DarknessSectionLayerLifecycle.cs`
  - Harmony integration with `Section`/vanilla darkness layer, only after exact local method/field signatures are verified.
- `mods/05-Rimconemy-Infected-Automation/Tests/DarknessSectionLayerRegressionTests.cs`
  - pure mesh-buffer invariants that do not require a live Unity scene.

### Documentation

- `docs/superpowers/specs/2026-08-05-darkness-sectionlayer-design.md` is the design SSOT.
- `docs/vanilla-api-matrix-1.6.md` remains the API evidence reference; update it only if a new exact local signature is verified and documented.

---

## Task 1: Close the vanilla Darkness-layer lifecycle gate

**Files:**
- Inspect only: local `/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll`
- Modify: `docs/vanilla-api-matrix-1.6.md` only if new exact signatures are verified
- Test command output: `/tmp` probe output, no repo code changes

**Interfaces:**
- Produces the exact constructor, `Regenerate`, `GetBoundaryRect`, material, submesh, and Section registration signatures needed by Task 2/3.

- [ ] **Step 1: Enumerate `SectionLayer_Darkness` and `SectionLayer_FogOfWar` completely.**

Run the existing .NET-10-compatible Mono.Cecil probe pattern against `Assembly-CSharp.dll` and print constructors, all methods, fields, and base types. Confirm whether their `Regenerate` methods call accessible helpers or directly populate `LayerSubMesh`.

Expected evidence includes:

```text
Verse.SectionLayer_Darkness : Verse.SectionLayer
Verse.SectionLayer_FogOfWar : Verse.SectionLayer
ctor(Verse.Section)
Regenerate()
LayerSubMesh / material / relevantChangeTypes
```

- [ ] **Step 2: Enumerate `Section` construction and layer list initialization.**

Confirm the method that creates the standard layer list and whether a Harmony postfix can append `DarknessSectionLayer` without a transpiler. If the list is private, record the exact field name/type and use a reflection accessor only inside the lifecycle patch.

- [ ] **Step 3: Choose one path and record the reason.**

Use this decision rule:

```text
If the vanilla Darkness layer can be safely patched/replaced with a local postfix/prefix:
  use existing vanilla layer path.
Else:
  inject DarknessSectionLayer into Section's layer list with one narrow Harmony patch.
Never activate both paths.
```

- [ ] **Step 4: Run `git diff --check`.**

Expected: no output and exit code 0.

---

## Task 2: Add pure visibility and mesh-buffer tests first

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Tests/ColonistSightSystemRegressionTests.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/DarknessSectionLayerRegressionTests.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs`

**Interfaces:**
- `DarknessSectionLayerRegressionTests.RunAll() -> bool`.
- Pure helper contract from Task 3: a method that converts a section cell visibility value into an alpha byte and validates mesh-buffer counts.

- [ ] **Step 1: Add failing visibility tests.**

Add assertions for:

```text
Own cell -> 1
Visibility remains within [0,1]
A blocked candidate is rejected by the LOS gate
A non-blocked candidate remains eligible
```

Because a real `Map` is not constructible in these bootstrap tests, expose a small pure predicate/helper for the LOS decision rather than mocking RimWorld internals.

- [ ] **Step 2: Add failing mesh invariants.**

Test these exact invariants using plain lists/structs:

```csharp
vertexCount == colorCount;
triangleCount % 3 == 0;
alpha = Mathf.Clamp01(Mathf.Sqrt(1f - visibility)) * maxAlpha;
visibility=0 -> alpha=maxAlpha;
visibility=1 -> alpha=0;
```

The test must not instantiate `Mesh`, `Material`, or a Unity GameObject.

- [ ] **Step 3: Register `DarknessSectionLayerRegressionTests.RunAll()` in Package 05 bootstrap.**

Use the existing test registration style and ensure a failure logs an error rather than throwing from static initialization.

- [ ] **Step 4: Run the focused available static/build gate.**

Run:

```bash
./scripts/dev_quick_test.sh
```

Expected during this red phase: the new test contract may fail to compile until Task 3 supplies the helper. Do not hide the failure; record the exact symbol mismatch.

---

## Task 3: Implement the SectionLayer mesh builder

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/World/DarknessSectionLayer.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Tests/DarknessSectionLayerRegressionTests.cs`

**Interfaces:**
- `public sealed class DarknessSectionLayer : SectionLayer`.
- Constructor: `DarknessSectionLayer(Section section)`.
- `public override void Regenerate()`.
- Pure helpers exposed as `internal static` only if needed by tests:
  - `ComputeOverlayAlpha(float visibility, float maxAlpha) -> float`.
  - `ValidateMeshBuffers(int vertexCount, int colorCount, int triangleCount) -> bool`.

- [ ] **Step 1: Implement the constructor with the verified `Section` base constructor.**

Store no duplicate map reference beyond `section.map` unless the verified vanilla pattern requires it. Initialize no Unity objects until `Regenerate()` runs on the main thread.

- [ ] **Step 2: Implement safe regeneration guards.**

`Regenerate()` must return safely when:

```text
section == null;
section.map == null;
ColonistSightSystem.Get(section.map) == null;
```

Clear old submeshes using the verified `MapDrawLayer` helper, then rebuild the current section only.

- [ ] **Step 3: Build a single section mesh from world-cell quads.**

For each cell in `section.CellRect`:

```csharp
float visibility = sight.GetVisibility(cell);
float alpha = ComputeOverlayAlpha(visibility, 0.55f);
if (alpha <= 0f) continue;

// Add four world-space vertices at the cell corners.
// Add six indices for two triangles.
// Add four Color32 values with the same alpha.
```

Do not compute screen coordinates. Use `LayerSubMesh.verts`, `.tris`, `.colors`, the verified transparent material, and the verified `MeshParts` flags.

- [ ] **Step 4: Finalize and validate the submesh.**

Use the locally verified `LayerSubMesh` constructor and `FinalizeMesh`. Do not call Unity mesh APIs from a background thread. Empty sections must clear/disable the submesh without throwing.

- [ ] **Step 5: Run the mesh regression tests.**

Expected: all alpha and buffer invariants pass.

---

## Task 4: Integrate the layer lifecycle with one narrow Harmony patch

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/World/DarknessSectionLayerLifecycle.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/World/ColonistSightSystem.cs`

**Interfaces:**
- `DarknessSectionLayerLifecycle.Install(Harmony harmony) -> void` or the repository's established bootstrap patch-install convention.
- `ColonistSightSystem.MarkVisibilityDirty() -> void`.
- `ColonistSightSystem.RegenerateDirtyDarknessLayers() -> void`.

- [ ] **Step 1: Add LOS gating to visibility computation.**

In `ComputeSinglePawnSight`, after the radius/cone candidate passes and before writing `_visibilityGrid`, call the verified 3-argument API:

```csharp
if (!GenSight.LineOfSight(pawnPos, cell, map))
    continue;
```

Keep the Pawn's own cell visible without requiring a self-LOS call.

- [ ] **Step 2: Add dirty scheduling.**

Set a grid-dirty flag when the 60-tick recomputation runs. On a successful grid rebuild, mark affected sections dirty. During `MapComponentUpdate` or the verified Section lifecycle callback, regenerate only dirty Sections on the main thread.

Conservative first pass is allowed:

```csharp
mark every map section dirty after a visibility-grid rebuild;
```

Do not introduce a per-frame full-map mesh rebuild.

- [ ] **Step 3: Inject or replace exactly one darkness layer.**

Use the Task 1 decision. The patch must be narrow and fail closed:

```text
if the target field/method is absent: log one warning and disable the custom renderer;
if the layer already exists: do not add a duplicate;
if map is null: return;
```

Do not transpile `MapDrawer.DrawMapMesh`.

- [ ] **Step 4: Disable the old screen-space renderer.**

Remove `MapComponentOnGUI`'s block loop or make it an explicit inactive fallback. The final active path must contain no `GUI.DrawTexture` map darkness rendering.

- [ ] **Step 5: Compile and run the static gate.**

Run:

```bash
./scripts/dev_quick_test.sh
```

Expected: `RESULT: PASS`, zero build/static failures.

---

## Task 5: Add runtime-safe load and dirty-state behavior

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/World/ColonistSightSystem.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/World/DarknessSectionLayerLifecycle.cs`
- Modify: `mods/05-Rimconemy-Infected-Automation/Tests/DarknessSectionLayerRegressionTests.cs`

**Interfaces:**
- Existing `FinalizeInit()` and `MapComponentTick()` remain callable by RimWorld.
- Add no persisted mesh data; meshes are derived and rebuilt after load.

- [ ] **Step 1: Reset derived rendering state in `FinalizeInit()`.**

Keep `_visibilityGrid` safe, set `_lastUpdateTick = -1`, clear dirty-section state, and schedule a full derived rebuild after the map is valid.

- [ ] **Step 2: Ensure map switching is fail-closed.**

The layer must render only when its `section.map` is current/valid. No `Find.CurrentMap` check belongs in the data computation; only the renderer uses map validity.

- [ ] **Step 3: Add idempotence checks.**

Repeated lifecycle calls must not append duplicate `DarknessSectionLayer` objects and must not multiply overlay alpha.

- [ ] **Step 4: Run static validation.**

Run:

```bash
./scripts/dev_quick_test.sh
./scripts/runtime_test.sh --skip-start --no-deploy
```

Expected: both exit 0; all installed package and XML gates pass.

---

## Task 6: Review and live verification

**Files:**
- Review all changed files.
- Update `docs/CODE_STATUS.md` only with evidence-backed status.
- Do not edit Vanilla files.

- [ ] **Step 1: Run code review.**

Review specifically:

```text
No GUI.DrawTexture/WorldToScreenPoint remains in the active map darkness path.
No duplicate SectionLayer is possible.
LOS errors fail closed without killing MapComponent ticks.
Unity Mesh/Material operations stay on the main thread.
No mesh is persisted through Scribe.
```

- [ ] **Step 2: Run all static checks.**

```bash
git diff --check
./scripts/dev_quick_test.sh
./scripts/runtime_test.sh --skip-start --no-deploy
```

Expected: exit 0 and zero failures.

- [ ] **Step 3: Deploy and perform live map test.**

```bash
./scripts/deploy.sh 05
./scripts/runtime_test.sh
```

In RimWorld, verify at minimum:

```text
windowed mode: no grid gaps;
zoom in/out: no checkerboard;
pawn/building/item selection: overlay remains;
mountain/wall: LOS blocked behind obstacle;
fire/torch: local visibility returns;
save -> load: no crash and overlay returns;
1x/2x/3x speed: no visible layer duplication.
```

- [ ] **Step 4: Report evidence separately.**

Report static/build evidence, live evidence, and remaining open gates separately. Do not call visual behavior verified from build output alone.

---

## Self-review checklist

- **Spec coverage:** Covers Screen-Space root cause, World-Space mesh, LOS, dirty rebuilds, lifecycle, save/load, rollback, and live acceptance criteria.
- **Placeholder scan:** No `TBD`, `TODO`, `FIXME`, or vague “add appropriate handling” instructions.
- **Type consistency:** SectionLayer constructor, `Regenerate`, `LayerSubMesh` lists, and test helper contracts are named consistently.
- **Scope:** The work is split into one rendering subsystem plan; no unrelated Economy/Outpost or language-asset changes are included.
- **Known gate:** exact Vanilla Darkness-layer lifecycle remains Task 1 and must be completed before implementation code is added.
