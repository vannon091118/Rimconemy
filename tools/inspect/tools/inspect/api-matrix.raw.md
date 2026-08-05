# Vanilla-API-Matrix (Spike-Rohdaten, 2026-08-04)

Quelle: `/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll` · RimWorld 1.6.4566 (Linux/GOG Build).

Hinweis: Rohdaten. Verbindliche Matrix: `docs/vanilla-api-matrix-1.6.md` (manuell kuratiert).

## ScenarioBase — `Verse.ScenarioBase`

**STATUS: NICHT GEFUNDEN** — Klasse fehlt in der lokalen 1.6-Assembly (oder umbenannt/vor DLL geladen).

## ScenPart — `RimWorld.ScenPart`

BaseType: `System.Object` · Sealed: False · Abstract: True

### Constructors

```csharp
// Family, HideBySig, SpecialName, RTSpecialName
new ScenPart();
```

### Public/Protected Methods (33 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Boolean` | `AllowPlayerStartingPawn` | `Pawn pawn, Boolean tryingToRedress, PawnGenerationRequest req` |  | virtual |
| `System.Boolean` | `CanCoexistWith` | `ScenPart other` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<System.String>` | `ConfigErrors` | `` |  | virtual |
| `RimWorld.ScenPart` | `CopyForEditing` | `` |  |  |
| `RimWorld.ScenPart` | `CopyForEditingInner` | `` |  | virtual |
| `System.Void` | `DoEditInterface` | `Listing_ScenEdit listing` |  | virtual |
| `System.Void` | `ExposeData` | `` |  | virtual |
| `System.Void` | `GenerateIntoMap` | `Map map` |  | virtual |
| `System.String` | `get_Label` | `` |  | virtual |
| `System.Boolean` | `get_OverrideDangerMusic` | `` |  | virtual |
| `System.Single` | `get_RowHeight` | `` | ✅ | static |
| `System.Collections.Generic.IEnumerable`1<RimWorld.Alert>` | `GetAlerts` | `` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<RimWorld.Page>` | `GetConfigPages` | `` |  | virtual |
| `System.Int32` | `GetHashCode` | `` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<System.String>` | `GetSummaryListEntries` | `String tag` |  | virtual |
| `System.Boolean` | `HasNullDefs` | `` |  | virtual |
| `System.Void` | `MapRemoved` | `Map map` |  | virtual |
| `System.Void` | `Notify_NewPawnGenerating` | `Pawn pawn, PawnGenerationContext context` |  | virtual |
| `System.Void` | `Notify_PawnDied` | `Corpse corpse` |  | virtual |
| `System.Void` | `Notify_PawnGenerated` | `Pawn pawn, PawnGenerationContext context, Boolean redressed` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<Verse.Thing>` | `PlayerStartingThings` | `` |  | virtual |
| `System.Void` | `PostGameStart` | `` |  | virtual |
| `System.Void` | `PostGravshipLanded` | `Map map` |  | virtual |
| `System.Void` | `PostIdeoChosen` | `` |  | virtual |
| `System.Void` | `PostMapGenerate` | `Map map` |  | virtual |
| `System.Void` | `PostWorldGenerate` | `` |  | virtual |
| `System.Void` | `PreConfigure` | `` |  | virtual |
| `System.Void` | `PreMapGenerate` | `` |  | virtual |
| `System.Void` | `Randomize` | `` |  | virtual |
| `System.String` | `Summary` | `Scenario scen` |  | virtual |
| `System.Void` | `Tick` | `` |  | virtual |
| `System.Boolean` | `TryMerge` | `ScenPart other` |  | virtual |
| `System.Boolean` | `Valid` | `` |  | virtual |

### Public/Protected Properties (3 total)

| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| `System.Single` | `RowHeight` | ✓ |  | ✅ |
| `System.String` | `Label` | ✓ |  |  |
| `System.Boolean` | `OverrideDangerMusic` | ✓ |  |  |

### Derived Types (sample of 15)

- `RimWorld.ScenPart_GameCondition`
- `RimWorld.ScenPart_PermaGameCondition`
- `RimWorld.ScenPart_NoPossessions`
- `RimWorld.ScenPart_OnPawnDeathExplode`
- `RimWorld.ScenPart_PawnModifier`
- `RimWorld.ScenPart_Rule`
- `RimWorld.ScenPart_ConfigPage`
- `RimWorld.ScenPart_CreateQuest`
- `RimWorld.ScenPart_DisableMapGen`
- `RimWorld.ScenPart_DisableQuest`
- `RimWorld.ScenPart_ForcedMap`
- `RimWorld.ScenPart_GameStartDialog`
- `RimWorld.ScenPart_IncidentBase`
- `RimWorld.ScenPart_AutoActivateMonolith`
- `RimWorld.ScenPart_PawnFilter_Age`

## GameComponent — `Verse.GameComponent`

BaseType: `System.Object` · Sealed: False · Abstract: True

### Constructors

```csharp
// Family, HideBySig, SpecialName, RTSpecialName
new GameComponent();
```

### Public/Protected Methods (8 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Void` | `AppendDebugString` | `StringBuilder sb` |  | virtual |
| `System.Void` | `ExposeData` | `` |  | virtual |
| `System.Void` | `FinalizeInit` | `` |  | virtual |
| `System.Void` | `GameComponentOnGUI` | `` |  | virtual |
| `System.Void` | `GameComponentTick` | `` |  | virtual |
| `System.Void` | `GameComponentUpdate` | `` |  | virtual |
| `System.Void` | `LoadedGame` | `` |  | virtual |
| `System.Void` | `StartedNewGame` | `` |  | virtual |

### Derived Types (sample of 6)

- `Verse.GameComponent_DebugTools`
- `Verse.GameComponent_OnetimeNotification`
- `RimWorld.GameComponent_Anomaly`
- `RimWorld.GameComponent_Bossgroup`
- `RimWorld.GameComponent_PawnDuplicator`
- `RimWorld.GameComponent_PsychicRitualManager`

## MapComponent — `Verse.MapComponent`

BaseType: `System.Object` · Sealed: False · Abstract: True

### Constructors

```csharp
// Public, HideBySig, SpecialName, RTSpecialName
new MapComponent(Verse.Map map);
```

### Public/Protected Methods (8 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Void` | `ExposeData` | `` |  | virtual |
| `System.Void` | `FinalizeInit` | `` |  | virtual |
| `System.Void` | `MapComponentDraw` | `` |  | virtual |
| `System.Void` | `MapComponentOnGUI` | `` |  | virtual |
| `System.Void` | `MapComponentTick` | `` |  | virtual |
| `System.Void` | `MapComponentUpdate` | `` |  | virtual |
| `System.Void` | `MapGenerated` | `` |  | virtual |
| `System.Void` | `MapRemoved` | `` |  | virtual |

### Derived Types (sample of 12)

- `Verse.CustomMapComponent`
- `Verse.LavaFXComponent`
- `Verse.PollutionInfo`
- `Verse.RoadInfo`
- `Verse.VacuumComponent`
- `Verse.FishShadowComponent`
- `Verse.WaterInfo`
- `RimWorld.BiomeConditionMapComponent`
- `RimWorld.FleshmassMapComponent`
- `RimWorld.MixedBiomeMapComponent`
- `RimWorld.TileMutatorConditionMapComponent`
- `RimWorld.BreakdownManager`

## WorldComponent — `Verse.WorldComponent`

BaseType: `System.Object` · Sealed: False · Abstract: True

### Constructors

```csharp
// Public, HideBySig, SpecialName, RTSpecialName
new WorldComponent(RimWorld.Planet.World world);
```

### Public/Protected Methods (5 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Void` | `ExposeData` | `` |  | virtual |
| `System.Void` | `FinalizeInit` | `Boolean fromLoad` |  | virtual |
| `System.Void` | `WorldComponentOnGUI` | `` |  | virtual |
| `System.Void` | `WorldComponentTick` | `` |  | virtual |
| `System.Void` | `WorldComponentUpdate` | `` |  | virtual |

## ThingComp — `Verse.ThingComp`

BaseType: `System.Object` · Sealed: False · Abstract: True

### Constructors

```csharp
// Family, HideBySig, SpecialName, RTSpecialName
new ThingComp();
```

### Public/Protected Methods (81 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Boolean` | `AllowStackWith` | `Thing other` |  | virtual |
| `Verse.AcceptanceReport` | `CanEnterPortal` | `` |  | virtual |
| `System.Boolean` | `CompAllowVerbCast` | `Verb verb` |  | virtual |
| `System.Void` | `CompDrawWornExtras` | `` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<Verse.FloatMenuOption>` | `CompFloatMenuOptions` | `Pawn selPawn` |  | virtual |
| `System.Boolean` | `CompForceDeconstructable` | `` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<Verse.Gizmo>` | `CompGetGizmosExtra` | `` |  | virtual |
| `System.Single` | `CompGetSpecialApparelScoreOffset` | `` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<Verse.Gizmo>` | `CompGetWornGizmosExtra` | `` |  | virtual |
| `System.String` | `CompInspectStringExtra` | `` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<Verse.FloatMenuOption>` | `CompMultiSelectFloatMenuOptions` | `IEnumerable`1 selPawns` |  | virtual |
| `System.Boolean` | `CompPreventClaimingBy` | `Faction faction` |  | virtual |
| `System.Void` | `CompPrintForPowerGrid` | `SectionLayer layer` |  | virtual |
| `System.Collections.Generic.List`1<Verse.PawnRenderNode>` | `CompRenderNodes` | `` |  | virtual |
| `System.Void` | `CompTick` | `` |  | virtual |
| `System.Void` | `CompTickInterval` | `Int32 delta` |  | virtual |
| `System.Void` | `CompTickLong` | `` |  | virtual |
| `System.Void` | `CompTickRare` | `` |  | virtual |
| `System.String` | `CompTipStringExtra` | `` |  | virtual |
| `System.Boolean` | `DontDrawParent` | `` |  | virtual |
| `System.Void` | `DrawAt` | `Vector3 drawLoc, Boolean flip` |  | virtual |
| `System.Void` | `DrawGUIOverlay` | `` |  | virtual |
| `System.Nullable`1<UnityEngine.Color>` | `ForceColor` | `` |  | virtual |
| `Verse.IThingHolder` | `get_ParentHolder` | `` |  | virtual · final |
| `System.Collections.Generic.IEnumerable`1<Verse.ThingDefCountClass>` | `GetAdditionalHarvestYield` | `` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<Verse.ThingDefCountClass>` | `GetAdditionalLeavings` | `Map map, DestroyMode mode` |  | virtual |
| `System.String` | `GetDescriptionPart` | `` |  | virtual |
| `System.Single` | `GetStatFactor` | `StatDef stat` |  | virtual |
| `System.Single` | `GetStatOffset` | `StatDef stat` |  | virtual |
| `System.Void` | `GetStatsExplanation` | `StatDef stat, StringBuilder sb, String whitespace` |  | virtual |
| `System.Void` | `Initialize` | `CompProperties props` |  | virtual |
| `System.Void` | `Notify_AbandonedAtTile` | `PlanetTile tile` |  | virtual |
| `System.Void` | `Notify_AddBedThoughts` | `Pawn pawn` |  | virtual |
| `System.Void` | `Notify_Arrested` | `Boolean succeeded` |  | virtual |
| `System.Void` | `Notify_BecameInvisible` | `` |  | virtual |
| `System.Void` | `Notify_BecameVisible` | `` |  | virtual |
| `System.Void` | `Notify_ColorChanged` | `` |  | virtual |
| `System.Void` | `Notify_DefsHotReloaded` | `` |  | virtual |
| `System.Void` | `Notify_Downed` | `` |  | virtual |
| `System.Void` | `Notify_DuplicatedFrom` | `Pawn source` |  | virtual |

### Public/Protected Properties (1 total)

| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| `Verse.IThingHolder` | `ParentHolder` | ✓ |  |  |

### Derived Types (sample of 15)

- `Verse.CompGlower`
- `Verse.CompAffectsSky`
- `Verse.CompAIUsablePack`
- `Verse.CompAttachBase`
- `Verse.CompColorable`
- `Verse.CompEquippable`
- `Verse.CompHeatPusher`
- `Verse.CompLifespan`
- `Verse.CompTemperatureDamaged`
- `Verse.CompWindSource`
- `Verse.ThingComp_VacuumAware`
- `RimWorld.CompReadable`
- `RimWorld.CompEffecter`
- `RimWorld.CompDestroyAfterEffect`
- `RimWorld.EffecterOnDeath`

## IncidentWorker — `RimWorld.IncidentWorker`

BaseType: `System.Object` · Sealed: False · Abstract: False

### Constructors

```csharp
// Public, HideBySig, SpecialName, RTSpecialName
new IncidentWorker();
```

### Public/Protected Methods (10 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Boolean` | `CanFireNow` | `IncidentParms parms` |  |  |
| `System.Boolean` | `CanFireNowSub` | `IncidentParms parms` |  | virtual |
| `System.Single` | `ChanceFactorNow` | `IIncidentTarget target` |  | virtual |
| `System.Boolean` | `FiredTooRecently` | `IIncidentTarget target` |  |  |
| `System.Single` | `get_BaseChanceThisGame` | `` |  | virtual |
| `System.Void` | `SendIncidentLetter` | `TaggedString baseLetterLabel, TaggedString baseLetterText, LetterDef baseLetterDef, IncidentParms parms, LookTargets lookTargets, IncidentDef def, NamedArgument[] textArgs` | ✅ | static |
| `System.Void` | `SendStandardLetter` | `IncidentParms parms, LookTargets lookTargets, NamedArgument[] textArgs` |  |  |
| `System.Void` | `SendStandardLetter` | `TaggedString baseLetterLabel, TaggedString baseLetterText, LetterDef baseLetterDef, IncidentParms parms, LookTargets lookTargets, NamedArgument[] textArgs` |  |  |
| `System.Boolean` | `TryExecute` | `IncidentParms parms` |  |  |
| `System.Boolean` | `TryExecuteWorker` | `IncidentParms parms` |  | virtual |

### Public/Protected Properties (1 total)

| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| `System.Single` | `BaseChanceThisGame` | ✓ |  |  |

### Derived Types (sample of 15)

- `RimWorld.IncidentWorker_Ambush`
- `RimWorld.IncidentWorker_CaravanDemand`
- `RimWorld.IncidentWorker_CaravanMeeting`
- `RimWorld.IncidentWorker_MakeGameCondition`
- `RimWorld.IncidentWorker_AggressiveAnimals`
- `RimWorld.IncidentWorker_Alphabeavers`
- `RimWorld.IncidentWorker_AmbrosiaSprout`
- `RimWorld.IncidentWorker_AnimalInsanitySingle`
- `RimWorld.IncidentWorker_AnimalInsanityMass`
- `RimWorld.IncidentWorker_BoomshroomSprout`
- `RimWorld.IncidentWorker_CrashedShipPart`
- `RimWorld.IncidentWorker_CropBlight`
- `RimWorld.IncidentWorker_DeepDrillInfestation`
- `RimWorld.IncidentWorker_Disease`
- `RimWorld.IncidentWorker_EntitySwarm`

## RecipeWorker — `RimWorld.RecipeWorker`

BaseType: `System.Object` · Sealed: False · Abstract: False

### Constructors

```csharp
// Public, HideBySig, SpecialName, RTSpecialName
new RecipeWorker();
```

### Public/Protected Methods (13 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Void` | `ApplyOnPawn` | `Pawn pawn, BodyPartRecord part, Pawn billDoer, List`1 ingredients, Bill bill` |  | virtual |
| `System.Boolean` | `AvailableOnNow` | `Thing thing, BodyPartRecord part` |  | virtual |
| `Verse.AcceptanceReport` | `AvailableReport` | `Thing thing, BodyPartRecord part` |  | virtual |
| `System.Void` | `CheckForWarnings` | `Pawn billDoer` |  | virtual |
| `System.Void` | `ConsumeIngredient` | `Thing ingredient, RecipeDef recipe, Map map` |  | virtual |
| `Verse.TaggedString` | `GetConfirmation` | `Pawn pawn` |  | virtual |
| `System.Single` | `GetIngredientCount` | `IngredientCount ing, Bill bill` |  | virtual |
| `System.String` | `GetLabelWhenUsedOn` | `Pawn pawn, BodyPartRecord part` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<Verse.BodyPartRecord>` | `GetPartsToApplyOn` | `Pawn pawn, RecipeDef recipe` |  | virtual |
| `System.Boolean` | `IsViolationOnPawn` | `Pawn pawn, BodyPartRecord part, Faction billDoerFaction` |  | virtual |
| `System.String` | `LabelFromUniqueIngredients` | `Bill bill` |  | virtual |
| `System.Void` | `Notify_IterationCompleted` | `Pawn billDoer, List`1 ingredients` |  | virtual |
| `System.Void` | `ReportViolation` | `Pawn pawn, Pawn billDoer, Faction factionToInform, Int32 goodwillImpact, HistoryEventDef overrideEventDef` |  |  |

## Designator — `RimWorld.Designator`

BaseType: `Verse.Command` · Sealed: False · Abstract: True

### Constructors

```csharp
// Public, HideBySig, SpecialName, RTSpecialName
new Designator();
```

### Public/Protected Methods (38 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `Verse.AcceptanceReport` | `CanDesignateCell` | `IntVec3 loc` |  | virtual · abstract |
| `Verse.AcceptanceReport` | `CanDesignateThing` | `Thing t` |  | virtual |
| `System.Boolean` | `CanRemainSelected` | `` |  | virtual |
| `System.Boolean` | `CheckCanInteract` | `` |  |  |
| `Verse.Command_Action` | `CreateReverseDesignationGizmo` | `Thing t` |  |  |
| `System.String` | `DescReverseDesignating` | `Thing t` |  | virtual |
| `System.Void` | `Deselected` | `` |  | virtual |
| `System.Void` | `DesignateMultiCell` | `IEnumerable`1 cells` |  | virtual |
| `System.Void` | `DesignateSingleCell` | `IntVec3 c` |  | virtual |
| `System.Void` | `DesignateThing` | `Thing t` |  | virtual |
| `System.Void` | `DoExtraGuiControls` | `Single leftX, Single bottomY` |  | virtual |
| `System.Void` | `DrawMouseAttachments` | `` |  | virtual |
| `System.Void` | `DrawPanelReadout` | `Single& curY, Single width` |  | virtual |
| `System.Void` | `Finalize` | `Boolean somethingSucceeded` |  |  |
| `System.Void` | `FinalizeDesignationFailed` | `` |  | virtual |
| `System.Void` | `FinalizeDesignationSucceeded` | `` |  | virtual |
| `System.Boolean` | `get_AlwaysDoGuiControls` | `` |  | virtual |
| `Verse.DesignationDef` | `get_Designation` | `` |  | virtual |
| `System.Boolean` | `get_DoTooltip` | `` |  | virtual |
| `System.Boolean` | `get_DragDrawMeasurements` | `` |  | virtual |
| `System.Boolean` | `get_DrawHighlight` | `` |  | virtual |
| `Verse.DrawStyleCategoryDef` | `get_DrawStyleCategory` | `` |  | virtual |
| `System.String` | `get_HighlightTag` | `` |  | virtual |
| `Verse.Map` | `get_Map` | `` |  |  |
| `System.Single` | `get_PanelReadoutTitleExtraRightMargin` | `` |  | virtual |
| `System.Collections.Generic.IEnumerable`1<Verse.FloatMenuOption>` | `get_RightClickFloatMenuOptions` | `` |  | virtual |
| `System.String` | `get_TutorTagDesignate` | `` |  |  |
| `System.String` | `get_TutorTagSelect` | `` |  | virtual |
| `Verse.GizmoResult` | `GizmoOnGUI` | `Vector2 topLeft, Single maxWidth, GizmoRenderParms parms` |  | virtual |
| `UnityEngine.Texture2D` | `IconReverseDesignating` | `Thing t, Single& angle, Vector2& offset` |  | virtual |
| `System.String` | `LabelCapReverseDesignating` | `Thing t` |  | virtual |
| `System.Void` | `ProcessInput` | `Event ev` |  | virtual |
| `System.Boolean` | `RemoveAllDesignationsAffects` | `LocalTargetInfo target` |  | virtual |
| `System.Void` | `RenderHighlight` | `List`1 dragCells` |  | virtual |
| `System.Void` | `Selected` | `` |  | virtual |
| `System.Void` | `SelectedProcessInput` | `Event ev` |  | virtual |
| `System.Void` | `SelectedUpdate` | `` |  | virtual |
| `System.Boolean` | `ShowWarningForCell` | `IntVec3 c` |  | virtual |

### Public/Protected Properties (12 total)

| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| `Verse.Map` | `Map` | ✓ |  |  |
| `System.Boolean` | `DragDrawMeasurements` | ✓ |  |  |
| `System.Boolean` | `DrawHighlight` | ✓ |  |  |
| `System.Boolean` | `DoTooltip` | ✓ |  |  |
| `System.Boolean` | `AlwaysDoGuiControls` | ✓ |  |  |
| `Verse.DesignationDef` | `Designation` | ✓ |  |  |
| `System.Single` | `PanelReadoutTitleExtraRightMargin` | ✓ |  |  |
| `Verse.DrawStyleCategoryDef` | `DrawStyleCategory` | ✓ |  |  |
| `System.String` | `TutorTagSelect` | ✓ |  |  |
| `System.String` | `TutorTagDesignate` | ✓ |  |  |
| `System.String` | `HighlightTag` | ✓ |  |  |
| `System.Collections.Generic.IEnumerable`1<Verse.FloatMenuOption>` | `RightClickFloatMenuOptions` | ✓ |  |  |

## GenSight — `RimWorld.GenSight`

BaseType: `System.Object` · Sealed: True · Abstract: True

### Public/Protected Methods (11 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Collections.Generic.List`1<Verse.IntVec3>` | `BresenhamCellsBetween` | `IntVec3 a, IntVec3 b` | ✅ | static |
| `System.Collections.Generic.List`1<Verse.IntVec3>` | `BresenhamCellsBetween` | `Int32 x0, Int32 y0, Int32 x1, Int32 y1` | ✅ | static |
| `System.Void` | `DebugDrawFOVSymmetry_Update` | `` | ✅ | static |
| `Verse.IntVec3` | `LastPointOnLineOfSight` | `IntVec3 start, IntVec3 end, Func`2 validator, Boolean skipFirstCell` | ✅ | static |
| `System.Boolean` | `LineOfSight` | `IntVec3 start, IntVec3 end, Map map, Boolean skipFirstCell, Func`2 validator, Int32 halfXOffset, Int32 halfZOffset` | ✅ | static |
| `System.Boolean` | `LineOfSight` | `IntVec3 start, IntVec3 end, Map map, CellRect startRect, CellRect endRect, Func`2 validator, Boolean forLeaning` | ✅ | static |
| `System.Boolean` | `LineOfSight` | `IntVec3 start, IntVec3 end, Map map` | ✅ | static |
| `System.Boolean` | `LineOfSightToEdges` | `IntVec3 start, IntVec3 end, Map map, Boolean skipFirstCell, Func`2 validator` | ✅ | static |
| `System.Boolean` | `LineOfSightToThing` | `IntVec3 start, Thing t, Map map, Boolean skipFirstCell, Func`2 validator` | ✅ | static |
| `System.Collections.Generic.IEnumerable`1<Verse.IntVec3>` | `PointsOnLineOfSight` | `IntVec3 start, IntVec3 end` | ✅ | static |
| `System.Void` | `PointsOnLineOfSight` | `IntVec3 start, IntVec3 end, Action`1 visitor` | ✅ | static |

## FogGrid — `Verse.FogGrid`

BaseType: `System.Object` · Sealed: True · Abstract: False

### Constructors

```csharp
// Public, HideBySig, SpecialName, RTSpecialName
new FogGrid(Verse.Map map);
```

### Public/Protected Methods (11 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Void` | `ClearAllFog` | `` |  |  |
| `System.Void` | `Dispose` | `` |  | virtual · final |
| `System.Void` | `ExposeData` | `` |  | virtual · final |
| `System.Void` | `FloodUnfogAdjacent` | `IntVec3 c, Boolean sendLetters` |  |  |
| `System.Void` | `FloodUnfogAdjacent` | `Thing thing, Boolean sendLetters` |  |  |
| `System.Boolean` | `IsFogged` | `IntVec3 c` |  |  |
| `System.Boolean` | `IsFogged` | `Int32 index` |  |  |
| `System.Void` | `Notify_FogBlockerRemoved` | `Thing thing` |  |  |
| `System.Void` | `Notify_PawnEnteringDoor` | `Building_Door door, Pawn pawn` |  |  |
| `System.Void` | `Refog` | `CellRect rect` |  |  |
| `System.Void` | `Unfog` | `IntVec3 c` |  |  |

## PawnGenerator — `RimWorld.PawnGenerator`

BaseType: `System.Object` · Sealed: True · Abstract: True

### Public/Protected Methods (15 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Void` | `AdjustXenotypeForFactionlessPawn` | `Pawn pawn, PawnGenerationRequest& request, XenotypeDef& xenotype` | ✅ | static |
| `Verse.Pawn` | `GeneratePawn` | `PawnKindDef kindDef, Faction faction, Nullable`1 tile` | ✅ | static |
| `Verse.Pawn` | `GeneratePawn` | `PawnGenerationRequest request` | ✅ | static |
| `System.Collections.Generic.List`1<RimWorld.Trait>` | `GenerateTraitsFor` | `Pawn pawn, Int32 traitCount, Nullable`1 req, Boolean growthMomentTrait` | ✅ | static |
| `RimWorld.BodyTypeDef` | `GetBodyTypeFor` | `Pawn pawn` | ✅ | static |
| `RimWorld.XenotypeDef` | `GetXenotypeForGeneratedPawn` | `PawnGenerationRequest request` | ✅ | static |
| `System.Boolean` | `IsBeingGenerated` | `Pawn pawn` | ✅ | static |
| `System.Boolean` | `IsPawnBeingGeneratedAndNotAllowsDead` | `Pawn pawn` | ✅ | static |
| `System.Void` | `PawnGenerationHistogram` | `` | ✅ | static |
| `System.Void` | `PostProcessGeneratedGear` | `Thing gear, Pawn pawn` | ✅ | static |
| `System.Int32` | `RandomTraitDegree` | `TraitDef traitDef` | ✅ | static |
| `System.Void` | `RedressPawn` | `Pawn pawn, PawnGenerationRequest request` | ✅ | static |
| `System.Void` | `Reset` | `` | ✅ | static |
| `System.Void` | `TryGenerateSexualityTraitFor` | `Pawn pawn, Boolean allowGay` | ✅ | static |
| `System.Collections.Generic.Dictionary`2<RimWorld.XenotypeDef,System.Single>` | `XenotypesAvailableFor` | `PawnKindDef kind, FactionDef factionDef, Faction faction` | ✅ | static |

## ResearchManager — `RimWorld.ResearchManager`

BaseType: `System.Object` · Sealed: True · Abstract: False

### Constructors

```csharp
// Public, HideBySig, SpecialName, RTSpecialName
new ResearchManager();
```

### Public/Protected Methods (23 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Void` | `AddProgress` | `ResearchProjectDef proj, Single amount, Pawn source` |  |  |
| `System.Void` | `AddTechprints` | `ResearchProjectDef proj, Int32 amount` |  |  |
| `System.Boolean` | `AnyProjectsAvailableWithKnowledgeCategory` | `KnowledgeCategoryDef category` |  |  |
| `System.Void` | `ApplyKnowledge` | `KnowledgeCategoryDef category, Single amount` |  |  |
| `System.Boolean` | `ApplyKnowledge` | `ResearchProjectDef project, Single amount, Single& remainder` |  |  |
| `System.Void` | `ApplyTechprint` | `ResearchProjectDef proj, Pawn applyingPawn` |  |  |
| `System.Void` | `DebugSetAllProjectsFinished` | `` |  |  |
| `System.Void` | `ExposeData` | `` |  | virtual · final |
| `System.Void` | `FinishProject` | `ResearchProjectDef proj, Boolean doCompletionDialog, Pawn researcher, Boolean doCompletionLetter` |  |  |
| `System.Boolean` | `get_AnyProjectIsAvailable` | `` |  |  |
| `System.Collections.Generic.List`1<RimWorld.ResearchManager/KnowledgeCategoryProject>` | `get_CurrentAnomalyKnowledgeProjects` | `` |  |  |
| `System.Single` | `GetKnowledge` | `ResearchProjectDef proj` |  |  |
| `System.Single` | `GetProgress` | `ResearchProjectDef proj` |  |  |
| `Verse.ResearchProjectDef` | `GetProject` | `KnowledgeCategoryDef category` |  |  |
| `System.Int32` | `GetTechprints` | `ResearchProjectDef proj` |  |  |
| `System.Boolean` | `IsCurrentProject` | `ResearchProjectDef proj` |  |  |
| `System.Void` | `Notify_MonolithLevelChanged` | `Int32 newLevel` |  |  |
| `System.Void` | `ReapplyAllMods` | `` |  |  |
| `System.Void` | `ResearchPerformed` | `Single amount, Pawn researcher` |  |  |
| `System.Void` | `ResetAllProgress` | `` |  |  |
| `System.Void` | `SetCurrentProject` | `ResearchProjectDef proj` |  |  |
| `System.Void` | `StopProject` | `ResearchProjectDef proj` |  |  |
| `System.Boolean` | `TabInfoVisible` | `ResearchTabDef tab` |  |  |

### Public/Protected Properties (2 total)

| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| `System.Boolean` | `AnyProjectIsAvailable` | ✓ |  |  |
| `System.Collections.Generic.List`1<RimWorld.ResearchManager/KnowledgeCategoryProject>` | `CurrentAnomalyKnowledgeProjects` | ✓ |  |  |

## CompRefuelable — `RimWorld.CompRefuelable`

BaseType: `Verse.ThingComp_VacuumAware` · Sealed: False · Abstract: False

### Constructors

```csharp
// Public, HideBySig, SpecialName, RTSpecialName
new CompRefuelable();
```

### Public/Protected Methods (28 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `Verse.AcceptanceReport` | `CanEjectFuel` | `` |  |  |
| `System.Collections.Generic.IEnumerable`1<Verse.Gizmo>` | `CompGetGizmosExtra` | `` |  | virtual |
| `System.String` | `CompInspectStringExtra` | `` |  | virtual |
| `System.Void` | `CompTick` | `` |  | virtual |
| `System.Void` | `ConsumeFuel` | `Single amount` |  |  |
| `System.Void` | `EjectFuel` | `` |  |  |
| `System.Single` | `get_Fuel` | `` |  |  |
| `System.Single` | `get_FuelPercentOfMax` | `` |  |  |
| `System.Single` | `get_FuelPercentOfTarget` | `` |  |  |
| `System.Boolean` | `get_FunctionsInVacuum` | `` |  | virtual |
| `System.Boolean` | `get_HasFuel` | `` |  |  |
| `System.Boolean` | `get_IsFull` | `` |  |  |
| `RimWorld.CompProperties_Refuelable` | `get_Props` | `` |  |  |
| `System.Boolean` | `get_ShouldAutoRefuelNow` | `` |  |  |
| `System.Boolean` | `get_ShouldAutoRefuelNowIgnoringFuelPct` | `` |  |  |
| `System.Single` | `get_TargetFuelLevel` | `` |  |  |
| `System.Int32` | `GetFuelCountToFullyRefuel` | `` |  |  |
| `System.Void` | `Initialize` | `CompProperties props` |  | virtual |
| `System.Void` | `Notify_UsedThisTick` | `` |  |  |
| `System.Void` | `PostDestroy` | `DestroyMode mode, Map previousMap` |  | virtual |
| `System.Void` | `PostDraw` | `` |  | virtual |
| `System.Void` | `PostExposeData` | `` |  | virtual |
| `System.Void` | `PostSpawnSetup` | `Boolean respawningAfterLoad` |  | virtual |
| `System.Void` | `Refuel` | `List`1 fuelThings` |  |  |
| `System.Void` | `Refuel` | `Single amount` |  |  |
| `System.Void` | `set_TargetFuelLevel` | `Single value` |  |  |
| `System.Boolean` | `ShouldBeLitNow` | `` |  | virtual · final |
| `System.Collections.Generic.IEnumerable`1<RimWorld.StatDrawEntry>` | `SpecialDisplayStats` | `` |  | virtual |

### Public/Protected Properties (10 total)

| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| `System.Boolean` | `FunctionsInVacuum` | ✓ |  |  |
| `System.Single` | `TargetFuelLevel` | ✓ | ✓ |  |
| `RimWorld.CompProperties_Refuelable` | `Props` | ✓ |  |  |
| `System.Single` | `Fuel` | ✓ |  |  |
| `System.Single` | `FuelPercentOfTarget` | ✓ |  |  |
| `System.Single` | `FuelPercentOfMax` | ✓ |  |  |
| `System.Boolean` | `IsFull` | ✓ |  |  |
| `System.Boolean` | `HasFuel` | ✓ |  |  |
| `System.Boolean` | `ShouldAutoRefuelNow` | ✓ |  |  |
| `System.Boolean` | `ShouldAutoRefuelNowIgnoringFuelPct` | ✓ |  |  |

## CompGlower — `RimWorld.CompGlower`

BaseType: `Verse.ThingComp` · Sealed: False · Abstract: False

### Constructors

```csharp
// Public, HideBySig, SpecialName, RTSpecialName
new CompGlower();
```

### Public/Protected Methods (18 total)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Collections.Generic.IEnumerable`1<Verse.Gizmo>` | `CompGetGizmosExtra` | `` |  | virtual |
| `System.Void` | `ForceRegister` | `Map map` |  |  |
| `Verse.ColorInt` | `get_GlowColor` | `` |  | virtual |
| `System.Single` | `get_GlowRadius` | `` |  | virtual |
| `System.Boolean` | `get_Glows` | `` |  |  |
| `System.Boolean` | `get_HasGlowColorOverride` | `` |  |  |
| `RimWorld.CompProperties_Glower` | `get_Props` | `` |  |  |
| `System.Boolean` | `get_ShouldBeLitNow` | `` |  | virtual |
| `System.Void` | `PostDeSpawn` | `Map map, DestroyMode mode` |  | virtual |
| `System.Void` | `PostExposeData` | `` |  | virtual |
| `System.Void` | `PostMapInit` | `` |  | virtual |
| `System.Void` | `PostSpawnSetup` | `Boolean respawningAfterLoad` |  | virtual |
| `System.Void` | `PostSwapMap` | `` |  | virtual |
| `System.Void` | `ReceiveCompSignal` | `String signal` |  | virtual |
| `System.Void` | `set_GlowColor` | `ColorInt value` |  | virtual |
| `System.Void` | `set_GlowRadius` | `Single value` |  | virtual |
| `System.Void` | `SetGlowColorInternal` | `Nullable`1 color` |  | virtual |
| `System.Void` | `UpdateLit` | `Map map` |  |  |

### Public/Protected Properties (6 total)

| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| `RimWorld.CompProperties_Glower` | `Props` | ✓ |  |  |
| `Verse.ColorInt` | `GlowColor` | ✓ | ✓ |  |
| `System.Single` | `GlowRadius` | ✓ | ✓ |  |
| `System.Boolean` | `ShouldBeLitNow` | ✓ |  |  |
| `System.Boolean` | `Glows` | ✓ |  |  |
| `System.Boolean` | `HasGlowColorOverride` | ✓ |  |  |

### PhaseProgress-ResearchProbe

- `IsFinished`: 0 Treffer
- `TotalCost`: 0 Treffer
- `CostAmount`: 0 Treffer
- `BaseCost`: 0 Treffer
- `totalCost`: 0 Treffer

## PhaseProgress-Reach — ResearchProjectDef direct

BaseType: `Verse.Def` - Sealed: False - Abstract: False

### Public/Protected Methods (filtered to 'Finished/Cost/Progress')

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Single` | `get_CostApparent` | `` |  |  |
| `System.Single` | `get_ProgressReal` | `` |  |  |
| `System.Single` | `get_ProgressApparent` | `` |  |  |
| `System.String` | `get_ProgressApparentString` | `` |  |  |
| `System.Single` | `get_ProgressPercent` | `` |  |  |
| `System.Boolean` | `get_IsFinished` | `` |  |  |
| `System.Single` | `get_Cost` | `` |  |  |
| `System.Boolean` | `get_PrerequisitesCompleted` | `` |  |  |
| `System.Int32` | `get_AnalyzedThingsCompleted` | `` |  |  |
| `System.Single` | `CostFactor` | `TechLevel researcherTechLevel` |  |  |
| `System.Boolean` | `AnyOtherVisiblePrerequisitesCompleted` | `ResearchProjectDef prerequisite` |  |  |

### Public Properties (filtered)
| Type | Name | Get | Set |
|---|---|---|---|
| `System.Single` | `CostApparent` | x |  |
| `System.Boolean` | `IsFinished` | x |  |
| `System.Single` | `Cost` | x |  |

### Public Fields (filtered)
| Type | Name |
|---|---|
| `System.Single` | `baseCost` |
| `System.Single` | `knowledgeCost` |
## Spike-Pflicht-Heuristik-Sweeps

### TryStartCastOn / TryCastShot / Launch

- `TryStartCastOn`: 4 Treffer
  - `Verse.Verb.TryStartCastOn(LocalTargetInfo castTarg, Boolean surpriseAttack, Boolean canHitNonTargetPawns, Boolean preventFriendlyFire, Boolean nonInterruptingSelfCast) -> Boolean`
  - `Verse.Verb.TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, Boolean surpriseAttack, Boolean canHitNonTargetPawns, Boolean preventFriendlyFire, Boolean nonInterruptingSelfCast) -> Boolean`
  - `Verse.Verb_ShootBeam.TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, Boolean surpriseAttack, Boolean canHitNonTargetPawns, Boolean preventFriendlyFire, Boolean nonInterruptingSelfCast) -> Boolean`
  - `RimWorld.Verb_CastAbility.TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, Boolean surpriseAttack, Boolean canHitNonTargetPawns, Boolean preventFriendlyFire, Boolean nonInterruptingSelfCast) -> Boolean`
- `TryCastShot`: 12 Treffer
  - `Verse.Verb_LaunchProjectileStaticOneUse.TryCastShot() -> Boolean`
  - `Verse.Verb.TryCastShot() -> Boolean`
  - `Verse.Verb_AbilityShoot.TryCastShot() -> Boolean`
  - `Verse.Verb_ArcSprayIncinerator.TryCastShot() -> Boolean`
  - `Verse.Verb_LaunchProjectile.TryCastShot() -> Boolean`
  - `Verse.Verb_Shoot.TryCastShot() -> Boolean`
  - `Verse.Verb_ShootBeam.TryCastShot() -> Boolean`
  - `Verse.Verb_SpewFire.TryCastShot() -> Boolean`
- `Launch`: 3 Treffer
  - `Verse.Projectile.Launch(Thing launcher, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Boolean preventFriendlyFire, Thing equipment) -> Void`
  - `Verse.Projectile.Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Boolean preventFriendlyFire, Thing equipment, ThingDef targetCoverDef) -> Void`
  - `RimWorld.Beam.Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, Boolean preventFriendlyFire, Thing equipment, ThingDef targetCoverDef) -> Void`

### Temperature-Readout (1.6-API)

- `GenTemperature`: 0 Treffer
- `RoomTemperature`: 0 Treffer
- `TemperatureAtCell`: 0 Treffer
- `OutdoorTemperature`: 0 Treffer

### Burning/Refuelable/Fuel

- `IsBurning`: 2 Treffer
  - `RimWorld.FireUtility.IsBurning(TargetInfo t) -> Boolean`
  - `RimWorld.FireUtility.IsBurning(Thing t) -> Boolean`
- `Fuel`: 0 Treffer
- `FuelPercent`: 0 Treffer
- `ConsumeFuel`: 2 Treffer
  - `RimWorld.Building_GravEngine.ConsumeFuel(PlanetTile tile) -> Void`
  - `RimWorld.CompRefuelable.ConsumeFuel(Single amount) -> Void`
- `Refuel`: 2 Treffer
  - `RimWorld.CompRefuelable.Refuel(List`1 fuelThings) -> Void`
  - `RimWorld.CompRefuelable.Refuel(Single amount) -> Void`

### LineOfSight (1.6-API)

- `LineOfSight`: 5 Treffer
  - `Verse.GenSight.LineOfSight(IntVec3 start, IntVec3 end, Map map, Boolean skipFirstCell, Func`2 validator, Int32 halfXOffset, Int32 halfZOffset) -> Boolean`
  - `Verse.GenSight.LineOfSight(IntVec3 start, IntVec3 end, Map map, CellRect startRect, CellRect endRect, Func`2 validator, Boolean forLeaning) -> Boolean`
  - `Verse.GenSight.LineOfSight(IntVec3 start, IntVec3 end, Map map) -> Boolean`
  - `RimWorld.Sketch.LineOfSight(IntVec3 start, IntVec3 end, Boolean skipFirstCell, Func`2 validator) -> Boolean`
  - `RimWorld.Sketch.LineOfSight(IntVec3 start, IntVec3 end, CellRect startRect, CellRect endRect, Func`2 validator) -> Boolean`
- `LineOfSightTo`: 0 Treffer
- `VisibleTo`: 0 Treffer

### Pawn-Bauabschluss-Hooks

- `FrameCompleted`: 0 Treffer
- `FinishBlueprint`: 0 Treffer
- `InstallBlueprint`: 0 Treffer
- `Notify_IterationCompleted`: 6 Treffer
  - `Verse.RecipeWorker.Notify_IterationCompleted(Pawn billDoer, List`1 ingredients) -> Void`
  - `RimWorld.Bill.Notify_IterationCompleted(Pawn billDoer, List`1 ingredients) -> Void`
  - `RimWorld.Bill_Medical.Notify_IterationCompleted(Pawn billDoer, List`1 ingredients) -> Void`
  - `RimWorld.Bill_Autonomous.Notify_IterationCompleted(Pawn billDoer, List`1 ingredients) -> Void`
  - `RimWorld.Bill_Production.Notify_IterationCompleted(Pawn billDoer, List`1 ingredients) -> Void`
  - `RimWorld.Bill_ProductionWithUft.Notify_IterationCompleted(Pawn billDoer, List`1 ingredients) -> Void`

### Mineable.Yield-Hooks (API-MINING-02)

- `DestroyMined`: 1 Treffer
  - `RimWorld.Mineable.DestroyMined(Pawn pawn) -> Void`
- `TrySpawnYield`: 0 Treffer
- `TrySpawnYieldFromDamage`: 0 Treffer
- `YieldNow`: 1 Treffer
  - `RimWorld.Plant.YieldNow() -> Int32`
- `SpawnYield`: 0 Treffer
- `SpawnYieldAt`: 0 Treffer
- `SpawnItems`: 0 Treffer
- `YieldCount`: 0 Treffer

## API-MINING-02 — Mineable-class direct enumeration

BaseType: `Verse.Building` · Sealed: False · Abstract: False

### Public/Protected Methods (filtered to Mineable.Yield-bearing)

| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| `System.Void` | `Destroy` | `DestroyMode mode` |  | virtual |
| `System.Void` | `DestroyMined` | `Pawn pawn` |  |  |
| `System.Void` | `ExposeData` | `` |  | virtual |
| `System.Void` | `Kill` | `Nullable`1 dinfo, Hediff exactCulprit` |  | virtual |
| `System.Void` | `Notify_TookMiningDamage` | `Int32 amount, Pawn miner` |  |  |
| `System.Void` | `PreApplyDamage` | `DamageInfo& dinfo, Boolean& absorbed` |  | virtual |

## Identität

- Datei: `/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll`
- Größe: 15.746.048 Bytes
- SHA-256: `A0AF57EF9162B569D3B052818BA3A29FCCCC8610F355F2BB9B08FAD5882982D3`
- Erfasst am: 2026-08-05T16:30:43Z

