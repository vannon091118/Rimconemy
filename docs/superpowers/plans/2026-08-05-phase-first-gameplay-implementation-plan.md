# Rimconemy Phase-First Gameplay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` or `superpowers:executing-plans` to implement this plan task-by-task. Every task ends at a verification gate. Do not claim `LIVE` from `CODE`, `DEF`, `COMPILES`, or `BOOT` evidence.

**Goal:** Build and validate a coherent Rimconemy progression from Early Survival through Production, Automation, Trade, Expansion, and Empire while using Vanilla/RimWorld DLC systems as adapters rather than parallel gameplay engines.

**Architecture:** Gameplay phases define when a resource or system becomes strategically relevant. Vanilla domains define where the implementation attaches (`ThingDef`, `RecipeDef`, work infrastructure, trade, ideology, biology, exploration). DLCs are optional phase adapters behind `DLCFilter`; Core-only play remains complete. Every physical resource has one owner, every live gate is computed from current state, and every cross-package read uses the existing capability boundary.

**Tech Stack:** RimWorld 1.6.4566; C#/.NET projects in `mods/*/*.csproj`; RimWorld Def XML/PatchOperations; Harmony only after an API spike; existing `runtime_test.sh`, `dev_quick_test.sh`, and in-process regression suites.

## Global Constraints

- `Rimconemy_SteelScraps` has exactly one Def owner: Mod 03, `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/SteelScraps.xml`.
- Vanilla `Steel` remains the only Steel identity; SteelScraps is a precursor with only two production outputs: Steel and MachineParts.
- `WoodLog` remains multi-use: fuel, early small construction, and later Coal input. Do not make WoodLog a SteelScraps substitute.
- Early visibility does not equal Early production: SteelScraps may be found early, but planned Steel/MachineParts production begins in Production/Automation.
- `Coal` is the first automation fuel; it must not dominate the starting loop merely because a Def or fuel filter exists.
- Steel mining is a live `Mining >= 8` gate for steel mineables only. No MiningGate save class exists unless persistent exceptions are introduced.
- `DefModExtension` is data, not behavior. Every live gate requires a verified Reader; do not claim that XML alone blocks mining.
- Do not invent `Rimconemy.Masonry` or another StuffCategory when `Stony`, `Metallic`, `Woody`, `Fabric`, and `Leathery` suffice.
- Do not add a second `CompProperties_Refuelable` to a building that already has one.
- Do not create a custom WorkType, JobDriver, TraderKind, research tree, or replacement IncidentWorker when a Vanilla/DLC anchor is sufficient.
- DLC integrations are optional and must be wrapped by the established DLC policy/`PatchOperationFindMod` path.
- Package boundaries from `docs/INTERFACE_CONTRACT.md` remain authoritative; Mod 04 must not hardcode Mod 03 building costs as its primary truth.
- Verification vocabulary is strict: `CODE`, `DEF`, `COMPILES`, `BOOT`, `LIVE`, `OPEN` as defined in `docs/CODE_STATUS.md`.
- No commit, deletion, rename, deployment, game start, or production-side effect is part of this planning task. Implementation requires a separate approval.

---

## 1. Canonical file and symbol map

### Gameplay contract and architecture

| Responsibility | Canonical file | Symbols/sections |
|---|---|---|
| Phase progression | `docs/PHASE_PROGRESSION_CONTRACT.md` (create) | `PhaseId`, resource availability matrix, phase transition gates |
| Vanilla technical domains | `docs/CANONICAL_VANILLA_DOMAIN_MAP.md` | Resource, Construction, Production, Automation, Trade, Faction, Ideology, Biology, Exploration |
| Cross-package ownership | `docs/INTERFACE_CONTRACT.md` | §§2, 5, 9; capability and write-owner rules |
| Evidence boundary | `docs/CODE_STATUS.md` | §1 status vocabulary and package status |
| Vanilla API evidence | `docs/H1-api-def-gate.md`, `docs/vanilla-api-matrix-1.6.md` | API spike records and evidence levels |
| Architecture index | `docs/ARCHITECTURE.md` | SSOT links and topic cross-walk |
| DLC policy | `mods/01-Rimconemy-Foundation/Source/DLC/DLCFilter.cs`, `DLCContentPolicy.cs`, `DLCPolicyConfig.cs` | `DLCFilter.IsContentEnabled`, `DLCFilter.EmitBootstrapSummary` |

### Resource owners

| Resource | Owner | Canonical Def/data reference | Allowed roles |
|---|---|---|---|
| `WoodLog` | Vanilla | local Core `ThingDefs_Items/Items_Resource_Stuff.xml` | fuel, early small construction, Coal input |
| `Steel` | Vanilla | local Core `ThingDefs_Items/Items_Resource_Stuff.xml` | construction, power, defense, trade |
| `Rimconemy_ConstructionDebris` | Mod 03 | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/ConstructionDebris.xml:13+` | early cover/building/scavenge role |
| `Rimconemy_SteelScraps` | Mod 03 | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/SteelScraps.xml:16+` | early loot; later 5:1 Steel or 5:1 MachineParts |
| `Rimconemy_Coal` | Mod 03 | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/Coal.xml:15+` | mid automation fuel |
| `Rimconemy_MachineParts` | Mod 03 | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/MachineParts.xml:16+` | automation/repair/high-tier production |
| `Rimconemy_StainlessSteel` | Mod 03 | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/StainlessSteel.xml:16+` | late infrastructure/expansion |
| `Rimconemy_DistilledWater` | Mod 03 | `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/Water.xml:12+` | optional water-power path |

---

## 2. Phase contract

### Phase matrix

| Phase | Planned resources | Allowed production | Main decisions | Primary anchors |
|---|---|---|---|---|
| `EarlySurvival` | Wood, Stone, ConstructionDebris, Food; rare SteelScraps visibility | Campfire food, primitive cover, basic furniture, salvage collection | fuel vs building, safe base vs ruin risk | Core Campfire, Wall, Door, Barricade, Sandbags, food recipes |
| `Production` | Early resources + controlled SteelScraps processing | `5 SteelScraps -> 1 Steel`; primitive salvage; better workstations | Steel vs future Parts, invest in Smithy or shelter | `RecipeDef`, `FueledSmithy`, Smithing research |
| `Automation` | Coal, Steel, MachineParts, ComponentIndustrial | Coal production, generator, power, repairs, machine chains | burn Wood now or make Coal; power allocation | `WoodFiredGenerator`, `CompRefuelable`, Biotech Mechanitor anchors |
| `Trade` | Steel, MachineParts, StainlessSteel, Silver/Credits | trade goods, tribute stock, reservations | sell, save, or satisfy political demands | `TraderKindDef`, `Caravan`, Royalty Empire anchors |
| `Expansion` | reserve fuel, Parts, StainlessSteel, Credits | Outposts, expeditions, transport logistics | local stability vs remote growth | `WorldObjectDef`, `SitePartDef`, Odyssey ruins/stashes |
| `Empire` | high-tier material, Credits, political resources | multi-site economy and political leverage | tribute, trade network, economic pressure | Royalty permits/titles, Odyssey orbital systems, Ideology social effects |

### Availability rule

Every resource must distinguish these states:

```text
Visible        = the player can see the resource or its existence.
Lootable       = the player can obtain a limited quantity as risk/reward.
Producible     = a repeatable bill/recipe is available.
Strategic      = the resource is a reliable major sink for the current phase.
```

A resource may be visible or rare loot one phase early, but its repeatable production and strategic dominance begin in its assigned phase.

### Phase transition gates

Do not use day-counts as the only gate. Use observed milestones:

```text
EarlySurvival -> Production:
  stable shelter + first salvage/processing station + confirmed food loop

Production -> Automation:
  controlled Steel output + accessible Smithing/Machining path + fuel reserve

Automation -> Trade:
  stable power/production + surplus high-value output + safe transport

Trade -> Expansion:
  reserve stock + caravan/outpost capability + reconciliation path

Expansion -> Empire:
  more than one strategic location or a validated political/territorial route
```

---

## Task 0: Freeze the baseline and record evidence

**Purpose:** Make the current repository state and current evidence boundary reproducible before changing gameplay semantics.

**Files:**
- Read: `docs/CODE_STATUS.md`, `docs/INTERFACE_CONTRACT.md`, `docs/ARCHITECTURE.md`
- Read: `mods/03-Rimconemy-Scavenger-Infrastructure/ROADMAP.md`
- Read: `docs/superpowers/plans/2026-08-04-early-game-vertical-slice.md`
- Read: current `git status`; do not stage or overwrite unrelated changes.

**Vanilla/DLC verification:**
- Record RimWorld assembly identity from `tools/inspect/api-matrix.raw.md` and `docs/vanilla-api-matrix-1.6.md`.
- Verify local Core and DLC paths exist under `/home/vannon/GOG Games/RimWorld/game/Data/`.
- Record current active DLC policy through `DLCFilter.EmitBootstrapSummary` evidence.

**Acceptance gate:**
- Baseline report lists current status as `CODE/DEF/COMPILES/BOOT/LIVE/OPEN` without promoting any status.
- Existing staged and untracked files are listed as pre-existing and excluded from implementation scope.
- No gameplay file is modified in this task.

---

## Task 1: Create the phase contract

**Files:**
- Create: `docs/PHASE_PROGRESSION_CONTRACT.md`
- Modify: `docs/ARCHITECTURE.md` to link the new phase contract.
- Modify: `docs/INDEX.md` if the repository index requires a new SSOT entry.

**Data to specify:**
- The six phases and matrix from §2 of this plan.
- Per-resource `Visible`, `Lootable`, `Producible`, and `Strategic` status.
- Exact phase assignments:
  - ConstructionDebris: Early
  - SteelScraps: Early loot / Production processing
  - Steel: Production output / Automation and later strategic material
  - Coal: Automation
  - MachineParts: Automation
  - StainlessSteel: Late Expansion/Empire
- Phase transition milestones.
- Negative rules: no repeatable Coal, Steel, or MachineParts production in Early.

**References:**
- Existing resource graph: `mods/03-Rimconemy-Scavenger-Infrastructure/BLUEPRINT.md:23-31`.
- Existing vertical slice: `docs/superpowers/plans/2026-08-04-early-game-vertical-slice.md:124+`.
- Existing code status: `docs/CODE_STATUS.md:1+`.

**Acceptance gate:**
- Every owned resource appears exactly once in the phase matrix.
- No row says “Early Steel unavailable” while also allowing an Early repeatable Steel recipe.
- The contract explicitly distinguishes early SteelScraps loot from midgame Steel production.
- Documentation-only validation; no build claim.

---

## Task 2: Enforce the SteelScraps single source of truth

**Files:**
- Delete only after explicit implementation approval: `mods/02-Rimconemy-Survival-Progression/Defs/ThingDefs/Resources/Rimconemy_SteelScraps.xml`.
- Keep: `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/ThingDefs/Resources/SteelScraps.xml:16+`.
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/Scenarios/ScenPart_RimconemyStart.cs:38,147-154`.
- Modify: any stale docs naming Mod 02 as the Def owner, especially `docs/falsification/earlygame__Survivor.md`.
- Add/update: Mod 03 owner matrix in `mods/03-Rimconemy-Scavenger-Infrastructure/BLUEPRINT.md`.

**Implementation rules:**
- Keep the runtime string `Rimconemy_SteelScraps` stable.
- Mod 02 may request the Def at runtime only when Mod 03 is registered; it must not own or compile-reference the Def.
- If Mod 03 is absent, the scenario must log a controlled warning and skip optional scrap scatter rather than fail the scenario.
- Remove stale text saying the Def must be inside Mod 02.

**Vanilla/DLC verification:**
- DefDatabase must contain one loaded `ThingDef.defName == Rimconemy_SteelScraps`.
- Core has no Vanilla SteelScraps Def; the Mod 03 Def is the only custom source.
- Use the existing package/capability registration rules in `docs/INTERFACE_CONTRACT.md:2,9`.

**Tests:**
- Add `TestSteelScrapsSingleOwner` to an existing Mod 03 regression suite or a new `ResourceOwnershipRegressionTests.cs`.
- Assert the loaded Def exists and the source scan finds one owner file, not two.
- Assert absent Mod 03 produces no hard exception in `ScenPart_RimconemyStart`.

**Acceptance gate:**
- No duplicate defName warning in `Player.log`.
- Mod 02 scenario does not claim Mod 02 owns the Def.
- `runtime_test.sh --require-scenario-tests` passes the ownership/scenario checks.

---

## Task 3: Make the Early scatter safe and phase-correct

**Files:**
- Modify: `mods/02-Rimconemy-Survival-Progression/Source/Scenarios/ScenPart_RimconemyStart.cs:34-184`.
- Modify: related scenario Def XML under `mods/02-Rimconemy-Survival-Progression/Defs/`.
- Test: existing scenario tests and `docs/falsification/earlygame__Survivor.md`.

**Design:**
- Keep a small deterministic optional SteelScraps scatter as early loot if the gameplay contract approves it.
- Do not scatter Coal, MachineParts, StainlessSteel, or repeatable Steel-output objects in Early.
- Preserve idempotence through the existing `EventKey_SteelScrapsScattered` state.
- Use the existing radius/count values only after confirming they do not guarantee the whole Production chain.

**Vanilla verification:**
- Verify map placement uses valid `ThingDef` and map cells.
- Verify the scenario runs with Mod 03 loaded and without Mod 03 loaded.
- Verify Core-only fallback does not create invalid Def references.

**Acceptance gate:**
- Same seed produces same scatter count and cells or the documented deterministic placement result.
- Re-running scenario setup does not duplicate scraps.
- Early gameplay remains possible with zero scraps found.
- No Coal/MachineParts/StainlessSteel scatter occurs.

---

## Task 4: Audit every early Vanilla blueprint before patching

**Files:**
- Create: `docs/vanilla-early-blueprint-matrix-1.6.md`.
- Inspect local Core XML under `/home/vannon/GOG Games/RimWorld/game/Data/Core/Defs/`.
- Target repository patches: `mods/03-Rimconemy-Scavenger-Infrastructure/Patches/Bauschutt_Remap_Patches.xml`, `Vanilla_Remap_Patches.xml` if present.

**Required Core anchors and evidence paths:**

| Def | Local Core evidence | Decision to record |
|---|---|---|
| `Wall` | `ThingDefs_Buildings/Buildings_Structure.xml:218` | preserve Vanilla Wall; adjust allowed stuff only if phase contract needs it |
| `Door` | `Buildings_Structure.xml:65` | preserve Door behavior; do not replace the whole Def |
| `Autodoor` | `Buildings_Structure.xml:88` | keep Smithing/Autodoors/Construction gate |
| `AnimalFlap` | `Buildings_Structure.xml:126` | keep Fabric/Leathery route |
| `Sandbags` | `Buildings_Security.xml:6` | keep Fabric/Leathery; do not force Debris into this role without a gameplay reason |
| `Barricade` | `Buildings_Security.xml:69` | evaluate Debris as an additional early route, not a whole-Def replacement |
| `TrapSpike` | `Buildings_Security.xml:133` | keep Construction 3; avoid Steel/Components early inflation |
| `CraftingSpot` | `Buildings_Production.xml:4` | only add a recipe if it is genuinely Early-safe |
| `HandTailoringBench` | `Buildings_Production.xml:290` | add fibre recipe only behind ComplexClothing and skill gate |
| `FueledSmithy` | `Buildings_Production.xml:435` | Production/Automation anchor for SteelScraps processing |
| `FueledStove` | `Buildings_Production.xml:766` | keep Vanilla food chain; do not overload with industrial recipes |
| `TableStonecutter` | `Buildings_Production.xml:860` | keep Stone route as Early/Production transition |
| `TableMachining` | `Buildings_Production.xml:592` | Automation anchor; no Early scrap processing |
| `ElectricTailoringBench` | `Buildings_Production.xml:356` | Automation/Research-gated textile route |
| `SimpleResearchBench` | `Buildings_Production.xml:1297` | preserve early research access unless phase contract says otherwise |
| `WoodFiredGenerator` | `Buildings_Power.xml:172` | Automation; one Refuelable only |
| `WindTurbine` | `Buildings_Power.xml:349` | Automation; preserve Electricity and Construction 4 |
| `Battery` | `Buildings_Power.xml:430` | Automation; preserve Batteries research |
| `SolarGenerator` | `Buildings_Power.xml:500` | Automation; preserve SolarPanels and Construction 6 |
| `Turret_MiniTurret` | `Buildings_Security_Turrets.xml:111` | late Production/Automation defense, preserve GunTurrets and Construction 5 |
| `TorchLamp` | `Buildings_Furniture.xml:1152` | early light; preserve Styleable/Glower |
| `TorchWallLamp` | `Buildings_Furniture.xml:1186` | early wood sink; do not make it consume advanced resources |
| `Campfire` | `Buildings_Temperature.xml:4+` | parity decision before replacing custom Campfire |

**Acceptance gate:**
- Each row has exact Vanilla cost, stuff categories, research prerequisite, construction skill, comps, and intended Rimconemy phase.
- Every proposed patch is classified as PatchOperation risk Stufe 1, 2, or 3.
- No cost is changed merely because it is technically patchable.

---

## Task 5: Decide Campfire migration by parity, not ideology

**Files:**
- Compare: `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/BuildingDefs/Campfire.xml`.
- Compare local Vanilla: `Core/Defs/ThingDefs_Buildings/Buildings_Temperature.xml:4+`.
- Update: `mods/03-Rimconemy-Scavenger-Infrastructure/Tests/CoalChainRegressionTests.cs`, `CampfireScrapsRegressionTests.cs`, `StainlessSteelChainRegressionTests.cs`.
- Update: `Scavenger_Building_Designation.xml`, localization, and `Bootstrap.cs` references if migration is approved.

**Parity checklist:**
- `graphicData`, `uiIconPath`, `PlaceWorker_PreventInteractionSpotOverlap`, `PlaceWorker_Heater`, `PlaceWorker_GlowRadius`.
- `CompProperties_Refuelable`, `CompProperties_Glower`, `CompProperties_HeatPusher`.
- `WorkTableWorkSpeedFactor`, `MaxHitPoints`, `WorkToBuild`, cost list, recipes, designation category, and research requirements.
- All current `Rimconemy_Campfire` references from tests, recipes, Bootstrap, localization, and Mod 04.

**Default recommendation:**
- Keep `Rimconemy_Campfire` temporarily if it represents a distinct salvage station or has intentional gameplay differences.
- Migrate to Vanilla `Campfire` only when a runtime parity test proves the Vanilla patch preserves required heat, UI, placement, and recipe behavior.
- Do not delete the custom Def as a preliminary cleanup step.

**Acceptance gate:**
- Decision is recorded as `KEEP_DISTINCT` or `MIGRATE_TO_VANILLA` with evidence.
- If migrating, Vanilla Campfire retains all native comps and place workers; only additive recipe/annotation patches are applied.
- If keeping, its unique gameplay role and phase assignment are explicit and not a Vanilla duplicate.

---

## Task 6: Rework the production recipes around phases

**Files:**
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/RecipeDefs/BurnSteelScraps.xml`.
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/RecipeDefs/MakeCoal.xml`.
- Inspect/modify: `SalvageMachineParts.xml`, `MakeStainlessSteel.xml`.
- Modify recipe-user Defs/Patches only after the phase gate is selected.
- Tests: `CoalChainRegressionTests.cs`, `CampfireScrapsRegressionTests.cs`, `StainlessSteelChainRegressionTests.cs`.

**Required data contracts:**

```text
BurnSteelScraps:
  ingredient: Rimconemy_SteelScraps x5
  product: Steel x1
  phase: Production, not repeatable Early

SalvageMachineParts:
  ingredient: Rimconemy_SteelScraps x5
  product: Rimconemy_MachineParts x1
  phase: Automation

MakeCoal:
  ingredients: WoodLog x3 + Rimconemy_HempLeafy x2
  product: Rimconemy_Coal x4
  workSkill: Cooking
  skill requirement: Cooking >= 3
  phase: Automation unless an additional research/recipe-user gate is explicitly selected

MakeStainlessSteel:
  preserve exact current 2 Steel + 1 MachineParts -> 2 StainlessSteel contract unless balance review changes it
  phase: late Production/Expansion
```

**Critical implementation decision:**
- Do not leave `BurnSteelScraps` and `SalvageMachineParts` as repeatable Early Campfire bills if the phase contract says Steel/MachineParts production begins later.
- Prefer adding the recipes to `FueledSmithy`/appropriate Vanilla station through `PatchOperationAdd`, gated by existing research and skill. Keep Campfire for Early survival tasks unless Task 5 explicitly chooses otherwise.
- `Cooking >= 3` is a skill gate, not by itself a Midgame gate.

**Acceptance gate:**
- Tests assert exact ingredient/product counts and phase availability.
- No Early station exposes repeatable Steel/MachineParts production unless the phase contract explicitly permits it.
- Recipe user lists contain no duplicate user and no nonexistent Def.
- Breaking 3:2 -> 5:1 is documented in affected falsification reports.

---

## Task 7: Add the Vanilla blueprint patches conservatively

**Files:**
- Modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Patches/Bauschutt_Remap_Patches.xml`.
- Create only when needed: `FueledSmithy_RecipeUses.xml`, `CraftingSpot_RecipeUses.xml`, `ElectricTailoringBench_RecipeUses.xml`.
- Do not create `Rimconemy.Masonry`.

**Patch risk rubric:**

```text
Stufe 1 (default): PatchOperationAdd, PatchOperationConditional,
                  PatchOperationSequence, PatchOperationFindMod.
Stufe 2 (review required): replace one scalar/list item value only.
Stufe 3 (exception): remove/replace complete critical list or full ThingDef.
```

**Rules:**
- Stufe 1 for adding a recipe to an existing recipe list when the XPath is conditional and idempotent.
- Stufe 2 for changing `costStuffCount`, a single cost entry, or a single prerequisite.
- Stufe 3 is forbidden for Graphics, PlaceWorkers, Comps, Power, Heat, Turret, or Door behavior unless a documented replacement test exists.
- Patches must be conditional on the target Def existing and, for DLC content, on `PatchOperationFindMod`.
- Preserve inherited Vanilla behavior; do not replace full `ThingDef` blocks.

**Acceptance gate:**
- Each XML patch has an XPath, target Def evidence, risk tier, and rollback description.
- Loading with Core only does not reference missing DLC Defs.
- Loading with all five DLCs does not produce patch errors or duplicate list entries.

---

## Task 8: Implement the live Mining >=8 gate only after API spike

**Files:**
- Inspect/extend: `tools/inspect/Program.cs`, `tools/inspect/TypeScanner.cs`.
- Record: `tools/inspect/api-matrix.raw.md`, `docs/H1-api-def-gate.md` under `API-MINING-02`.
- Create only after spike: `mods/02-Rimconemy-Survival-Progression/Source/HarmonyPatches/MiningYieldGate_Patch.cs`.
- Create: `mods/02-Rimconemy-Survival-Progression/Tests/MiningGateRegressionTests.cs`.
- Do not create: `mods/01-Rimconemy-Foundation/Source/Save/MiningGateSaveData.cs` or schema step at this stage.

**Spike needles:**

```text
RimWorld.Mineable
Mineable.EffectiveMineableYield / get_EffectiveMineableYield
Mineable.TrySpawnYield
Mineable.CanYieldNow
RimWorld.JobDriver_Mine
JobDriver_Mine.MakeNewToils
Designator_Mine
WorkGiver_Miner
```

**Spike acceptance:**
- Exact signature, declaring type, visibility, virtual/final status, parameters, return type, and call timing are recorded from the local 1.6.4566 assembly.
- `Mineable.YieldNow()` is not used unless the assembly actually proves it exists.
- Miner identity at the selected hook is proven; do not guess from a reservation manager without a runtime test.

**Implementation contract after spike:**
- Read `MiningGateExt` from the steel mineable Def.
- Skip all non-Steel mineables.
- Resolve the actual Pawn performing the mining operation.
- `Mining < 8` blocks Steel yield; `Mining >= 8` passes through unchanged.
- Null/unknown miner path must be deterministic and logged; it must not silently grant steel.
- No persistent gate state.
- Harmony is minimized to the smallest verified Reader surface; no Transpiler.

**Acceptance gate:**
- Skill 3/7 yields no Steel.
- Skill 8/10 yields the Vanilla Steel amount.
- Stone, components, and unrelated mineables pass through.
- Gene/title/Ideology modifiers are not added until the base gate is LIVE and their own APIs are verified.

---

## Task 9: Correct the generator and fuel model

**Files:**
- Inspect/modify: `mods/03-Rimconemy-Scavenger-Infrastructure/Defs/BuildingDefs/PowerPlants.xml`.
- Inspect: `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Power/PowerChainService.cs`.
- Inspect: `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Power/FueledGeneratorService.cs`.
- Tests: `CoalChainRegressionTests.cs`, `BuildingCoreRegressionTests.cs`.

**Vanilla reference:** local Core `Buildings_Power.xml:172` `WoodFiredGenerator` has one `CompProperties_Refuelable` with `WoodLog` fuel, plus Power/Flickable/Glower/Heat/Breakdown comps.

**Rules:**
- Do not use two `CompProperties_Refuelable` entries.
- If Coal is accepted, add it to the one existing `fuelFilter` only after Coal's Automation gate is established.
- Vanilla `CompRefuelable` does not provide per-fuel efficiency. Do not claim a 1.5x Coal efficiency without a verified custom component.
- Prefer balancing Coal through production yield and availability first.
- Migrate away from `Rimconemy_WoodCoalGenerator` only after a parity test proves Vanilla `WoodFiredGenerator` preserves intended gameplay.

**Acceptance gate:**
- Exactly one refuelable component on the target generator.
- Wood remains usable.
- Coal is unavailable as a repeatable Early route.
- Generator power, flick, heat, breakdown, inspection, and save behavior remain intact in LIVE testing.

---

## Task 10: Implement Core-only phase behavior before DLC adapters

**Files:**
- Core phase Patches under `mods/03-Rimconemy-Scavenger-Infrastructure/Patches/`.
- Core resource/recipe/building Defs under Mod 03.
- Tests in Mod 03 and scenario tests in Mod 02.

**Vanilla verification targets:**
- Campfire, Wall, Door, Barricade, Sandbags, TrapSpike.
- FueledSmithy, FueledStove, TableStonecutter, TableMachining.
- WoodFiredGenerator, Battery, WindTurbine, SolarGenerator.
- Core research and skill prerequisites from the local XML evidence matrix.

**Acceptance gate:**
- Core-only load supports Early Survival through at least the start of Automation.
- Removing every DLC does not create missing Def, missing class, or invalid PatchOperation errors.
- The player can survive without finding SteelScraps.
- The player cannot mass-produce Coal or MachineParts in Early.

---

## Task 11: Add Ideology as a phase adapter, not a second progression engine

**Files:**
- Verify local `HistoryEventsManager`, `HistoryEventDef`, `HistoryEventDefOf`, and `Notify_HistoryEvent` signatures in `docs/H1-api-def-gate.md` before code.
- Possible implementation owner: Mod 05 or Foundation only after ownership is assigned in `docs/INTERFACE_CONTRACT.md`.
- Existing design reference: `docs/H3-ideology-influence-matrix.md`.

**Required API spike:**
- Do not assume `Find.HistoryEvents.Add` or `AddListener` exists from string evidence.
- Prove the actual manager access, registration/listener API, event payload, and lifecycle.
- If no safe subscription API exists, use a verified existing Vanilla hook or a bounded 250/60000-tick Reader rather than inventing a global event bus.

**Gameplay scope:**
- Early: communal cleanup, survival cooperation, resource fairness.
- Mid: production success/failure and repair actions affect Ideology reactions.
- Late: rituals, roles, relic stories, and political identity can reference industrial achievements.

**Acceptance gate:**
- No new parallel Ideology progression tree.
- Without Ideology, Core behavior is unchanged.
- With Ideology, at least one action has a verified visible consequence in LIVE testing.
- History-event claims are labeled `OPEN` until the API and runtime event delivery are proven.

---

## Task 12: Add Biotech Mechanitor integration only in Automation

**Files:**
- DLC-gated patches under `mods/03-Rimconemy-Scavenger-Infrastructure/Patches/`.
- API/Def evidence: local Biotech `LayoutRoomDefs/LayoutRooms_MechanitorComplex.xml`, `Stats/Stats_Pawns_General.xml`, `JobDefs/Jobs_Misc.xml`, `Effects/Effecter_Misc.xml`, `AbilityDefs/Abilities.xml`.
- Possible code Reader only in the owning package after capability review.

**Anchors to verify individually:**
- `AncientMechGestatorRoom`.
- Subcore Softscanner/Ripscanner and encoding-speed stats.
- `MechBandwidth`, `MechRepairSpeed`, `MechRemoteRepairDistance`.
- `RepairMech`, `RepairMechRemote`, `MechCharge`.
- Mech resurrection ability and any actual repair recipe/bill; do not invent a `Recipe_RepairMech` name without Def evidence.

**Gameplay scope:**
- Rare Early Mech salvage may be loot only.
- MachineParts become repeatable repair/automation inputs in Automation.
- Do not make a Biotech-only Def required for Core progression.

**Acceptance gate:**
- Every Biotech patch is inside a DLC condition.
- Core-only load has no Biotech references.
- MachineParts affect a verified existing Mech process, not a speculative Def.
- Mechanitor gameplay remains playable when MachineParts are scarce.

---

## Task 13: Add Anomaly and Odyssey as staged exploration adapters

**Files:**
- DLC-gated patches under `mods/03-Rimconemy-Scavenger-Infrastructure/Patches/`.
- Documentation: `docs/CANONICAL_VANILLA_DOMAIN_MAP.md` Exploration domain.
- Falsification reports for ruin/loot behavior under `docs/falsification/`.

**Anomaly anchors to verify:**
- Actual entity/loot Defs and their leavings fields.
- Actual ruin/scatter/ThingSetMaker paths.
- Actual GameCondition/event state used for production pressure.

**Odyssey anchors to verify:**
- `AncientBarricades`, `AncientEngine`, `AncientWeaponStorage` RoomPartDefs.
- `AncientRuinsEmpty`, `AncientRuinsIndustrialStorage`, `AncientComputerRoom`.
- `OrbitalWreckRubble`, `OrbitalItemStash`, `MapGen_OrbitalItemStash`.
- Gravship upgrade rooms and any actual fuel/building Defs.

**Gameplay scope:**
- Early: small ruin loot may contain Debris/Scraps.
- Mid: industrial ruins may contain limited MachineParts.
- Late: orbital/Gravship/expedition systems become strategic expansion tools.
- Ruins accelerate progression but are never the only route to Production.

**Acceptance gate:**
- No loot patch points to a guessed XPath or guessed Def.
- Loot quantities are bounded and deterministic where Rimconemy relies on them.
- Core-only and DLC-disabled loads remain valid.
- Odyssey cannot accidentally unlock the entire Automation phase through one early stash.

---

## Task 14: Integrate Royalty only after Trade phase exists

**Files:**
- DLC-gated patches under Mod 04 or the owner package selected by `docs/INTERFACE_CONTRACT.md`.
- Local Royalty references: `TraderKindDefs/TraderKinds_Caravan_Empire.xml`, `RoyalTitles/RoyalPermits_Empire.xml`, Royalty quest Defs.
- Existing economy: `mods/04-Rimconemy-Economy-Territory/Source/Market/`, wallet/ledger and outpost code.

**Gameplay scope:**
- Empire can buy/sell or request high-tier outputs only after Trade phase.
- MachineParts/StainlessSteel may be valuable to Empire, but must not become a free early currency.
- Titles and permits may modify access only through verified Royalty APIs; do not use titles as an unverified Mining-gate override.
- Shuttle/quest rewards must be additive and optional.

**Acceptance gate:**
- No custom TraderKindDef.
- No Core-only missing reference.
- Royalty-off behavior remains unchanged.
- Trade transaction, reservation, and ledger reconciliation are LIVE-tested before calling the feature complete.

---

## Task 15: Refactor Mod 04 building inputs to read data, not duplicate ownership

**Files:**
- Modify: `mods/04-Rimconemy-Economy-Territory/Source/Building/BuildingInputAdapter.cs:10-45`.
- Tests: `mods/04-Rimconemy-Economy-Territory/Tests/BuildingInputRegressionTests.cs`.
- Reference: `docs/INTERFACE_CONTRACT.md:9.1`, `docs/CANONICAL_VANILLA_DOMAIN_MAP.md:§8`.

**Implementation rules:**
- Remove the hardcoded table as the primary cost truth.
- Read resolved `ThingDef.costList`/`costStuffCount` for a target building when available.
- Use a capability-gated Mod 03 reader only for physical-resource semantics that cannot be derived from Vanilla Def data.
- Preserve `Credits` as non-physical wallet state.
- Do not add a compile-time Mod 04 -> Mod 03 dependency.

**Acceptance gate:**
- Changing a Vanilla/Mod 03 cost Def changes the adapter result without changing Mod 04 C#.
- Unknown Defs return a deterministic safe result and diagnostic.
- Existing physical-vs-credit tests remain green.
- Source scan finds no hardcoded `Rimconemy_WoodCoalGenerator`/`Rimconemy_Campfire` cost table in the adapter.

---

## Task 16: Add phase-aware tests and falsification reports

**Files:**
- Existing suites: `mods/03/.../Tests/CoalChainRegressionTests.cs`, `CampfireScrapsRegressionTests.cs`, `StainlessSteelChainRegressionTests.cs`, `BuildingCoreRegressionTests.cs`.
- Scenario tests: `mods/02/.../Tests/` and `ScenPart_RimconemyStart`.
- Add focused tests only where an existing suite has no owner.
- Update: `docs/falsification/earlygame__Campfire.md`, `earlygame__Survivor.md`, `scavenger__FoodAndHemp.md`, `scavenger__WaterPowerArrowTurret.md`, and any report claiming the old 3:2 or 1:1 ratio.

**Required assertions:**

```text
SSOT:
  loaded SteelScraps has one owner source

Early:
  optional SteelScraps loot can be absent without soft-lock
  no repeatable Coal/MachineParts/Steel production is exposed

Production:
  BurnSteelScraps consumes 5 and produces 1 Steel

Automation:
  SalvageMachineParts consumes 5 and produces 1 MachineParts
  MakeCoal has Cooking and Cooking >= 3 plus its phase gate

Mining:
  Steel Mining <8 blocked; >=8 Vanilla yield preserved
  non-Steel mineables unaffected

Blueprints:
  target Vanilla Defs preserve native comps/placeWorkers/research gates
  patches are idempotent and do not duplicate recipe users

DLC:
  each adapter is absent-safe when its DLC is disabled
```

**Acceptance gate:**
- Tests distinguish `DEF_LOAD`, `BOOT`, and `LIVE` evidence.
- Falsification reports no longer claim old ratios or old owners.
- A failure in one DLC adapter does not hide a Core progression failure.

---

## Task 17: Run validation in dependency order

**Static/build gates:**

```bash
./scripts/dev_quick_test.sh --strict
./scripts/runtime_test.sh --skip-start --no-deploy
```

Run package builds individually when a package changes:

```bash
dotnet build mods/01-Rimconemy-Foundation/Rimconemy.Foundation.csproj
 dotnet build mods/02-Rimconemy-Survival-Progression/Rimconemy.SurvivalProgression.csproj
 dotnet build mods/03-Rimconemy-Scavenger-Infrastructure/Rimconemy.ScavengerInfrastructure.csproj
 dotnet build mods/04-Rimconemy-Economy-Territory/Rimconemy.EconomyTerritory.csproj
 dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj
```

**Runtime gates:**

```bash
./scripts/runtime_test.sh --require-scenario-tests
./scripts/verify_bootstrap_log.sh
```

**Mandatory scenarios:**

1. Core-only / no DLC.
2. All five DLCs active.
3. Temperate/Boreal resource-rich start.
4. Arid Shrubland wood-poor start.
5. Tropical/ruin-dense start.
6. Start with scraps absent.
7. Steel miner below Mining 8.
8. Steel miner at Mining 8.
9. Save -> quit -> reload after each phase transition.
10. Trade/outpost/expedition route after Automation.

**Completion rule:**

```text
CODE/DEF/COMPILES/BOOT = implementation evidence.
LIVE = actual gameplay observation in the named scenario.
A phase is not complete until its required LIVE gate passes.
```

---

## Task 18: Maintain the documentation cross-walk

**Files:**
- Modify: `docs/PHASE_PROGRESSION_CONTRACT.md` after every gameplay decision.
- Modify: `docs/CANONICAL_VANILLA_DOMAIN_MAP.md` after every technical anchor decision.
- Modify: `docs/INTERFACE_CONTRACT.md` after every owner/capability change.
- Modify: `docs/ARCHITECTURE.md` and `docs/INDEX.md` when SSOT topics move.
- Modify: `docs/CODE_STATUS.md` only with evidence-backed status changes.

**Required cross-reference fields for each new feature:**

```text
FeatureId
PrimaryPhase
TechnicalDomain
OwnerPackage
VanillaAnchor
DLCCondition
Def/XMLPath
CSharpSymbol
ReaderOrComp
SaveState: none | named schema
PatchRisk: 1 | 2 | 3
TestName
EvidenceLevel
FallbackWhenUnavailable
```

**Acceptance gate:**
- No implementation task is considered complete without all fields.
- New DLC content adds an adapter row; it does not redefine the phase contract.
- No documentation claims a feature is LIVE without a runtime report.

---

## Dependency DAG

```text
Task 0 Baseline
   ↓
Task 1 Phase contract
   ↓
Task 2 SSOT ownership ───────┐
   ↓                         │
Task 3 Early scatter          │
   ↓                         │
Task 4 Vanilla blueprint audit
   ↓                         │
Task 5 Campfire parity ──────┐
   ↓                         │
Task 6 Recipe phase gates    │
   ↓                         │
Task 7 Vanilla patches       │
   ↓                         │
Task 8 Mining API spike ──→ Mining Reader implementation
   ↓                         │
Task 9 Fuel/generator         │
   ↓                         │
Task 10 Core-only vertical slice
   ↓
Task 11 Ideology adapter
Task 12 Biotech adapter
Task 13 Anomaly/Odyssey adapters
Task 14 Royalty adapter
   ↓
Task 15 Economy boundary refactor
   ↓
Task 16 Falsification/test hardening
   ↓
Task 17 Full validation matrix
   ↓
Task 18 Documentation cross-walk
```

Tasks 11–14 can be implemented in parallel only after Task 10 proves the Core-only loop and each DLC task has its own API/Def evidence gate.

---

## Final scope decision

The first implementation slice is deliberately limited to:

```text
Task 0 → Task 10
```

That slice proves the actual game:

```text
Early Survival → controlled Scrap processing → Automation
```

Royalty, Ideology, Biotech, Anomaly, and Odyssey are then adapters around a working game, not prerequisites for the game to function.
