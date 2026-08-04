# Phase 8.3 Spike-Rohdaten: 1.6-Bauabschluss-Hooks

Quelle: `/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll` · Datum: 2026-08-04T20:50:45Z

## 1) Heuristik-Sweep: Namens-Kandidaten

- `MakeFinished`: 0 Treffer
- `FinishConstruction`: 0 Treffer
- `CompleteConstruction`: 1 Treffer
  - `RimWorld.Frame.CompleteConstruction(Pawn worker) -> Void`
- `SpawnFinished`: 0 Treffer
- `ConstructionCompleted`: 0 Treffer
- `Notify_BuildingComplete`: 0 Treffer
- `FrameSpawned`: 0 Treffer
- `FrameComplete`: 0 Treffer
- `FinishFrame`: 0 Treffer
- `SpawnFrame`: 0 Treffer
- `BuildingFrame`: 0 Treffer
- `Blueprint_Building`: 0 Treffer
- `GenConstruct`: 0 Treffer

## 2) Targeted Class-Enumerierung

### RimWorld.GenConstruct — vorhanden
- `Boolean BlocksConstruction(Thing constructible, Thing t)` · static
- `BuildableDef BuiltDefOf(ThingDef def)` · static
- `Boolean CanBuildOnTerrain(BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore, ThingDef stuffDef)` · static
- `Boolean CanConstruct(Thing t, Pawn pawn, WorkTypeDef workType, Boolean forced, JobDef jobForReservation)` · static
- `Boolean CanConstruct(Thing t, Pawn p, Boolean checkSkills, Boolean forced, JobDef jobForReservation)` · static
- `AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, Boolean godMode, Thing thingToIgnore, Thing thing, ThingDef stuffDef, Boolean ignoreEdgeArea, Boolean ignoreInteractionSpots, Boolean ignoreClearableFreeBuildings)` · static
- `Boolean CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef, ThingDef newStuff, ThingDef oldStuff)` · static
- `Boolean CanReplace(BuildableDef placing, BuildableDef existing, ThingDef placingStuff, ThingDef existingStuff)` · static
- `Boolean CanTouchTargetFromValidCell(Thing constructible, Pawn worker)` · static
- `Thing FirstBlockingThing(Thing constructible, Pawn pawnToIgnore)` · static
- `List`1 GetAttachedBuildings(Thing thing)` · static
- `Thing GetWallAttachedTo(Thing thing)` · static
- `Thing GetWallAttachedTo(IntVec3 pos, Rot4 rot, Map map)` · static
- `Job HandleBlockingThingJob(Thing constructible, Pawn worker, Boolean forced)` · static
- `Boolean HasMatchingReplacementTag(ThingDef a, ThingDef b)` · static
- `AcceptanceReport InteractionCellStandable(ThingDef thingDef, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore)` · static
- `Thing MiniToInstallOrBuildingToReinstall(Blueprint b)` · static
- `AcceptanceReport NotBlockingAnyInteractionCells(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore)` · static
- `Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff, Precept_ThingStyle styleSource, ThingStyleDef styleDef, Boolean sendBPSpawnedSignal)` · static
- `Blueprint_Install PlaceBlueprintForInstall(MinifiedThing itemToInstall, IntVec3 center, Map map, Rot4 rotation, Faction faction, Boolean sendBPSpawnedSignal)` · static
- `Blueprint_Install PlaceBlueprintForReinstall(Building buildingToReinstall, IntVec3 center, Map map, Rot4 rotation, Faction faction, Boolean sendBPSpawnedSignal)` · static
- `Void Reset()` · static
- `Boolean TerrainCanSupport(CellRect rect, Map map, ThingDef thing)` · static

### Verse.GenSpawn — vorhanden
- `Boolean CanSpawnAt(ThingDef thingDef, IntVec3 c, Map map, Nullable`1 rot, Boolean canWipeEdifices)` · static
- `Void CheckMoveItemsAside(IntVec3 thingPos, Rot4 thingRot, ThingDef thingDef, Map map)` · static
- `Void Refund(Thing thing, Map map, CellRect avoidThisRect, Boolean forbid, Boolean willReplace)` · static
- `Thing Spawn(ThingDef def, IntVec3 loc, Map map, WipeMode wipeMode)` · static
- `Thing Spawn(ThingDef def, IntVec3 loc, Map map, Rot4 rot, WipeMode wipeMode)` · static
- `Thing Spawn(Thing newThing, IntVec3 loc, Map map, WipeMode wipeMode)` · static
- `Thing Spawn(Thing newThing, IntVec3 loc, Map map, Rot4 rot, WipeMode wipeMode, Boolean respawningAfterLoad, Boolean forbidLeavings)` · static
- `Void SpawnBuildingAsPossible(Building building, Map map, Boolean respawningAfterLoad)` · static
- `Boolean SpawningWipes(BuildableDef newEntDef, BuildableDef oldEntDef, Boolean ignoreDestroyable)` · static
- `Void SpawnIrregularLump(ThingDef thing, IntVec3 pos, Map map, IntRange countRange, IntRange distRange, WipeMode wipeMode, Predicate`1 validator, List`1 area, List`1 spawned, ThingDef stuff, Faction faction)` · static
- `Boolean TrySpawn(ThingDef def, IntVec3 loc, Map map, Thing& thing, WipeMode wipeMode, Boolean canWipeEdifices)` · static
- `Boolean TrySpawn(ThingDef def, IntVec3 loc, Map map, Rot4 rot, Thing& thing, WipeMode wipeMode, Boolean canWipeEdifices)` · static
- `Void WipeAndRefundExistingThings(IntVec3 thingPos, Rot4 thingRot, BuildableDef thingDef, Map map, Boolean forbid)` · static
- `Void WipeExistingThings(IntVec3 thingPos, Rot4 thingRot, BuildableDef thingDef, Map map, DestroyMode mode)` · static
- `Boolean WouldWipeAnythingWith(IntVec3 thingPos, Rot4 thingRot, BuildableDef thingDef, Map map, Predicate`1 predicate)` · static
- `Boolean WouldWipeAnythingWith(CellRect cellRect, BuildableDef thingDef, Map map, Predicate`1 predicate)` · static

### Verse.Blueprint — vorhanden
- `Thing BlockingHaulableOnTop()`
- `Void DeSpawn(DestroyMode mode)` · virtual
- `Void DrawAt(Vector3 drawLoc, Boolean flip)` · virtual
- `BuildableDef EntityToBuild()` · virtual · abstract
- `ThingDef EntityToBuildStuff()` · virtual · abstract
- `ThingStyleDef EntityToBuildStyle()` · virtual · abstract
- `Graphic get_Graphic()` · virtual
- `String get_Label()` · virtual
- `Single get_WorkTotal()` · virtual · abstract
- `IEnumerable`1 GetGizmos()` · virtual
- `String GetInspectString()` · virtual
- `Void InheritStyle(Precept_ThingStyle styleSource, ThingStyleDef styleDef)`
- `Boolean IsCompleted()` · virtual · final
- `Thing MakeSolidThing(Boolean& shouldSelect)` · virtual · abstract
- `Int32 SpaceRemainingFor(ThingDef stuff)` · virtual · final
- `Void SpawnSetup(Map map, Boolean respawningAfterLoad)` · virtual
- `Int32 ThingCountNeeded(ThingDef stuff)` · virtual · final
- `List`1 TotalMaterialCost()` · virtual · abstract
- `Boolean TryReplaceWithSolidThing(Pawn workerPawn, Thing& createdThing, Boolean& jobEnded)` · virtual

### RimWorld.Blueprint_Build — vorhanden
- `Void DeSpawn(DestroyMode mode)` · virtual
- `BuildableDef EntityToBuild()` · virtual
- `ThingDef EntityToBuildStuff()` · virtual
- `ThingStyleDef EntityToBuildStyle()` · virtual
- `Void ExposeData()` · virtual
- `ThingDef get_BuildDef()`
- `String get_Label()` · virtual
- `Single get_WorkTotal()` · virtual
- `IEnumerable`1 GetGizmos()` · virtual
- `String GetInspectString()` · virtual
- `Thing MakeSolidThing(Boolean& shouldSelect)` · virtual
- `List`1 TotalMaterialCost()` · virtual

### RimWorld.Frame — vorhanden
- `Boolean Accepts(Thing t)`
- `Void CompleteConstruction(Pawn worker)`
- `Void Destroy(DestroyMode mode)` · virtual
- `Void DrawAt(Vector3 drawLoc, Boolean flip)` · virtual
- `ThingDef EntityToBuildStuff()` · virtual · final
- `Void ExposeData()` · virtual
- `Void FailConstruction(Pawn worker)`
- `ThingDef get_BuildDef()`
- `EffecterDef get_ConstructionEffect()`
- `Color get_DrawColor()` · virtual
- `Boolean get_DrawStorageTab()` · virtual · final
- `String get_Label()` · virtual
- `String get_LabelEntityToBuild()`
- `Single get_PercentComplete()`
- `String get_StorageGroupTag()` · virtual · final
- `Single get_WorkLeft()`
- `Single get_WorkToBuild()`
- `Void GetChildHolders(List`1 outChildren)` · virtual · final
- `ThingOwner GetDirectlyHeldThings()` · virtual · final
- `IEnumerable`1 GetGizmos()` · virtual
- `String GetInspectString()` · virtual
- `IEnumerable`1 GetInspectTabs()` · virtual
- `StorageSettings GetParentStoreSettings()` · virtual · final
- `StorageSettings GetStoreSettings()` · virtual · final
- `Boolean IsCompleted()` · virtual · final
- `Int32 SpaceRemainingFor(ThingDef stuff)` · virtual · final
- `Int32 ThingCountNeeded(ThingDef stuff)` · virtual · final
- `Int32 ThingCountNeededWithEnroute(ThingDef stuff, Pawn excludeEnrouteFor)`
- `List`1 TotalMaterialCost()` · virtual · final

### Verse.Building — vorhanden
- `Void ChangePaint(ColorDef colorDef)`
- `AcceptanceReport ClaimableBy(Faction by)` · virtual
- `AcceptanceReport DeconstructibleBy(Faction faction)` · virtual
- `Void DeSpawn(DestroyMode mode)` · virtual
- `Void Destroy(DestroyMode mode)` · virtual
- `Void DrawExtraSelectionOverlays()` · virtual
- `Void ExposeData()` · virtual
- `Color get_DrawColor()` · virtual
- `Boolean get_ExchangeVacuum()` · virtual
- `Boolean get_IsAirtight()` · virtual
- `Boolean get_IsClearableFreeBuilding()`
- `Int32 get_MaxItemsInCell()` · virtual
- `Int32 get_MinTickIntervalRate()` · virtual
- `ColorDef get_PaintColorDef()`
- `CompPower get_PowerComp()`
- `Boolean get_TransmitsPowerNow()` · virtual
- `IEnumerable`1 GetGizmos()` · virtual
- `String GetInspectStringLowPriority()` · virtual
- `Int32 HaulToContainerDuration(Thing thing)` · virtual
- `Boolean IsDangerousFor(Pawn p)` · virtual
- `Boolean IsWorking()` · virtual
- `UInt16 PathWalkCostFor(Pawn p)` · virtual
- `Void PostApplyDamage(DamageInfo dinfo, Single totalDamageDealt)` · virtual
- `Void PostGeneratedForTrader(TraderKindDef trader, PlanetTile forTile, Faction forFaction)` · virtual
- `Void PreApplyDamage(DamageInfo& dinfo, Boolean& absorbed)` · virtual
- `Gizmo SelectContainedItemGizmo(Thing container, Thing item)` · static
- `Void set_HitPoints(Int32 value)` · virtual
- `Void SetFaction(Faction newFaction, Pawn recruiter)` · virtual
- `Void SpawnSetup(Map map, Boolean respawningAfterLoad)` · virtual
- `IEnumerable`1 SpecialDisplayStats()` · virtual

## 3) Heat-Analyse: GenSpawn vs. GenConstruct

### RimWorld.GenConstruct — 26 Methods total

- `Boolean BlocksConstruction(Thing constructible, Thing t)`
- `BuildableDef BuiltDefOf(ThingDef def)`
- `Boolean CanBuildOnTerrain(BuildableDef entDef, IntVec3 c, Map map, Rot4 rot, Thing thingToIgnore, ThingDef stuffDef)`
- `Boolean CanConstruct(Thing t, Pawn pawn, WorkTypeDef workType, Boolean forced, JobDef jobForReservation)`
- `Boolean CanConstruct(Thing t, Pawn p, Boolean checkSkills, Boolean forced, JobDef jobForReservation)`
- `AcceptanceReport CanPlaceBlueprintAt(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, Boolean godMode, Thing thingToIgnore, Thing thing, ThingDef stuffDef, Boolean ignoreEdgeArea, Boolean ignoreInteractionSpots, Boolean ignoreClearableFreeBuildings)`
- `Boolean CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef, ThingDef newStuff, ThingDef oldStuff)`
- `Boolean CanReplace(BuildableDef placing, BuildableDef existing, ThingDef placingStuff, ThingDef existingStuff)`
- `Boolean CanTouchTargetFromValidCell(Thing constructible, Pawn worker)`
- `Thing FirstBlockingThing(Thing constructible, Pawn pawnToIgnore)`
- `List`1 GetAttachedBuildings(Thing thing)`
- `Thing GetWallAttachedTo(Thing thing)`
- `Thing GetWallAttachedTo(IntVec3 pos, Rot4 rot, Map map)`
- `Job HandleBlockingThingJob(Thing constructible, Pawn worker, Boolean forced)`
- `Boolean HasMatchingReplacementTag(ThingDef a, ThingDef b)`
- `AcceptanceReport InteractionCellStandable(ThingDef thingDef, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore)`
- `Thing MiniToInstallOrBuildingToReinstall(Blueprint b)`
- `AcceptanceReport NotBlockingAnyInteractionCells(BuildableDef entDef, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore)`
- `Blueprint_Build PlaceBlueprintForBuild(BuildableDef sourceDef, IntVec3 center, Map map, Rot4 rotation, Faction faction, ThingDef stuff, Precept_ThingStyle styleSource, ThingStyleDef styleDef, Boolean sendBPSpawnedSignal)`
- `Blueprint_Install PlaceBlueprintForInstall(MinifiedThing itemToInstall, IntVec3 center, Map map, Rot4 rotation, Faction faction, Boolean sendBPSpawnedSignal)`
- `Blueprint_Install PlaceBlueprintForReinstall(Building buildingToReinstall, IntVec3 center, Map map, Rot4 rotation, Faction faction, Boolean sendBPSpawnedSignal)`
- `Void Reset()`
- `Boolean TerrainCanSupport(CellRect rect, Map map, ThingDef thing)`

## Identitäts-Hash (Trunc.): `A0AF57EF9162B569`
