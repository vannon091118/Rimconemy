# Targeted API Spike — Critical Verification

Source: /home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll

## Verse.CameraDriver
BaseType: UnityEngine.MonoBehaviour · Sealed: False · Abstract: False

### Constructors
```csharp
new CameraDriver();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Void | Awake |  |  |  |
| System.Void | CameraDriverOnGUI |  |  |  |
| System.Void | Expose |  |  |  |
| System.Single | get_CellSizePixels |  |  |  |
| Verse.CellRect | get_CurrentViewRect |  |  |  |
| Verse.CameraZoomRange | get_CurrentZoom |  |  |  |
| System.Single | get_HitchReduceFactor |  | YES | static |
| UnityEngine.Vector3 | get_InverseFovScale |  |  |  |
| Verse.IntVec3 | get_MapPosition |  |  |  |
| System.Single | get_RootSize |  |  |  |
| UnityEngine.Vector2 | get_ViewSpacePosition |  |  |  |
| System.Single | get_ZoomRootSize |  |  |  |
| UnityEngine.Vector2 | GetExtraVelocityFromReleasingDragButton | List`1 dragTimeStamps, Single velocityFromMouseDragInitialFactor | YES | static |
| System.Boolean | InViewOf | Thing thing |  |  |
| System.Void | JumpToCurrentMapLoc | IntVec3 cell |  |  |
| System.Void | JumpToCurrentMapLoc | Vector3 loc |  |  |
| System.Void | OnPreCull |  |  |  |
| System.Void | PanToMapLoc | IntVec3 cell |  |  |
| System.Void | PanToMapLocAndSize | Vector3 loc, Single size, Single duration, PanCompletionCallback onComplete |  |  |
| System.Void | ResetSize |  |  |  |
| System.Void | SetRootPosAndSize | Vector3 rootPos, Single rootSize |  |  |
| System.Void | SetRootSize | Single size |  |  |
| System.Void | Update |  |  |  |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| System.Single | RootSize | yes |  |  |
| Verse.CameraZoomRange | CurrentZoom | yes |  |  |
| Verse.IntVec3 | MapPosition | yes |  |  |
| UnityEngine.Vector2 | ViewSpacePosition | yes |  |  |
| Verse.CellRect | CurrentViewRect | yes |  |  |
| System.Single | HitchReduceFactor | yes |  | YES |
| System.Single | CellSizePixels | yes |  |  |
| System.Single | ZoomRootSize | yes |  |  |
| UnityEngine.Vector3 | InverseFovScale | yes |  |  |

### CameraDriver Relevant Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Single | get_RootSize |  |  |  |
| System.Void | set_RootSize | Single value |  |  |
| UnityEngine.Vector3 | get_CurrentRealPosition |  |  |  |
| Verse.IntVec3 | get_MapPosition |  |  |  |
| UnityEngine.Vector2 | get_ViewSpacePosition |  |  |  |
| System.Single | get_CellSizePixels |  |  |  |
| System.Single | get_ZoomRootSize |  |  |  |
| System.Void | ApplyPositionToGameObject |  |  |  |
| System.Void | ResetSize |  |  |  |
| System.Void | JumpToCurrentMapLoc | IntVec3 cell |  |  |
| System.Void | JumpToCurrentMapLoc | Vector3 loc |  |  |
| System.Void | PanToMapLocAndSize | Vector3 loc, Single size, Single duration, PanCompletionCallback onComplete |  |  |
| System.Void | SetRootPosAndSize | Vector3 rootPos, Single rootSize |  |  |
| System.Void | SetRootSize | Single size |  |  |


## Verse.WeatherDef
BaseType: Verse.Def · Sealed: False · Abstract: False

### Constructors
```csharp
new WeatherDef();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Collections.Generic.IEnumerable`1<System.String> | ConfigErrors |  |  | virtual |
| Verse.WeatherWorker | get_Worker |  |  |  |
| Verse.WeatherDef | Named | String defName | YES | static |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| Verse.WeatherWorker | Worker | yes |  |  |

### All Fields (WeatherDef)
| Type | Name |
|---|---|
| Verse.IntRange | durationRange |
| System.Boolean | repeatable |
| System.Boolean | isBad |
| System.Boolean | canOccurAsRandomForcedEvent |
| Verse.Favorability | favorability |
| Verse.FloatRange | temperatureRange |
| Verse.SimpleCurve | commonalityRainfallFactor |
| System.Int32 | transitionTicksOverride |
| System.Int32 | minMonolithLevel |
| System.Boolean | canOccurInAmbientHorror |
| System.String | letterText |
| System.String | letterLabel |
| Verse.LetterDef | letterDef |
| System.Single | rainRate |
| System.Single | snowRate |
| System.Single | sandRate |
| System.Single | windSpeedFactor |
| System.Single | windSpeedOffset |
| System.Single | moveSpeedMultiplier |
| System.Single | accuracyMultiplier |
| System.Single | maxRangeCap |
| System.Single | perceivePriority |
| System.Boolean | doToxicBuildup |
| RimWorld.ThoughtDef | weatherThought |
| System.Single | maxGlow |
| System.Boolean | preventSkygaze |
| System.Boolean | preventsShuttleLaunch |
| System.Collections.Generic.List`1<Verse.SoundDef> | ambientSounds |
| System.Collections.Generic.List`1<Verse.WeatherEventMaker> | eventMakers |
| System.Collections.Generic.List`1<System.Type> | overlayClasses |
| Verse.SkyColorSet | skyColorsNightMid |
| Verse.SkyColorSet | skyColorsNightEdge |
| Verse.SkyColorSet | skyColorsDay |
| Verse.SkyColorSet | skyColorsDusk |
| System.Type | workerClass |

### All Properties (WeatherDef)
| Type | Name | Get | Set |
|---|---|---|---|
| Verse.WeatherWorker | Worker | yes |  |


## Verse.LetterStack
BaseType: System.Object · Sealed: True · Abstract: False

### Constructors
```csharp
new LetterStack();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Void | ExposeData |  |  | virtual final |
| Verse.BundleLetter | get_BundleLetter |  |  |  |
| System.Single | get_LastTopY |  |  |  |
| System.Collections.Generic.List`1<Verse.Letter> | get_LettersListForReading |  |  |  |
| System.Void | LettersOnGUI | Single baseY |  |  |
| System.Void | LetterStackTick |  |  |  |
| System.Void | LetterStackUpdate |  |  |  |
| System.Void | Notify_FactionRemoved | Faction faction |  |  |
| System.Void | Notify_LetterMouseover | Letter let |  |  |
| System.Void | OpenAutomaticLetters |  |  |  |
| System.Void | ReceiveLetter | TaggedString label, TaggedString text, LetterDef textLetterDef, LookTargets lookTargets, Faction relatedFaction, Quest quest, List`1 hyperlinkThingDefs, String debugInfo, Int32 delayTicks, Boolean playSound |  |  |
| System.Void | ReceiveLetter | TaggedString label, TaggedString text, LetterDef textLetterDef, String debugInfo, Int32 delayTicks, Boolean playSound |  |  |
| System.Void | ReceiveLetter | Letter let, String debugInfo, Int32 delayTicks, Boolean playSound |  |  |
| System.Void | RemoveLetter | Letter let |  |  |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| System.Collections.Generic.List`1<Verse.Letter> | LettersListForReading | yes |  |  |
| System.Single | LastTopY | yes |  |  |
| Verse.BundleLetter | BundleLetter | yes |  |  |

### ReceiveLetter Overloads
| Return | Params | Static | Notes |
|---|---|---|---|
| System.Void | Verse.TaggedString label, Verse.TaggedString text, Verse.LetterDef textLetterDef, Verse.LookTargets lookTargets, RimWorld.Faction relatedFaction, RimWorld.Quest quest, System.Collections.Generic.List`1<Verse.ThingDef> hyperlinkThingDefs, System.String debugInfo, System.Int32 delayTicks, System.Boolean playSound |  |  |
| System.Void | Verse.TaggedString label, Verse.TaggedString text, Verse.LetterDef textLetterDef, System.String debugInfo, System.Int32 delayTicks, System.Boolean playSound |  |  |
| System.Void | Verse.Letter let, System.String debugInfo, System.Int32 delayTicks, System.Boolean playSound |  |  |


## Verse.Window
BaseType: System.Object · Sealed: False · Abstract: True

### Constructors
```csharp
new Window(Verse.IWindowDrawing customWindowDrawing);
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Boolean | CausesMessageBackground |  |  | virtual |
| System.Void | Close | Boolean doCloseSound |  | virtual |
| System.Void | DoWindowContents | Rect inRect |  | virtual abstract |
| System.Void | ExtraOnGUI |  |  | virtual |
| System.String | get_CloseButtonText |  |  | virtual |
| RimWorld.QuickSearchWidget | get_CommonSearchWidget |  |  | virtual |
| UnityEngine.Vector2 | get_InitialSize |  |  | virtual |
| System.Boolean | get_IsDebug |  |  | virtual |
| System.Boolean | get_IsOpen |  |  |  |
| System.Single | get_Margin |  |  | virtual |
| System.Void | LateWindowOnGUI | Rect inRect |  | virtual |
| System.Void | Notify_ClickOutsideWindow |  |  | virtual |
| System.Void | Notify_CommonSearchChanged |  |  | virtual |
| System.Void | Notify_ResolutionChanged |  |  | virtual |
| System.Void | OnAcceptKeyPressed |  |  | virtual |
| System.Void | OnCancelKeyPressed |  |  | virtual |
| System.Boolean | OnCloseRequest |  |  | virtual |
| System.Void | PostClose |  |  | virtual |
| System.Void | PostOpen |  |  | virtual |
| System.Void | PreClose |  |  | virtual |
| System.Void | PreOpen |  |  | virtual |
| UnityEngine.Rect | QuickSearchWidgetRect | Rect winRect, Rect inRect |  | virtual |
| System.Void | SetInitialSizeAndPosition |  |  | virtual |
| System.Void | WindowOnGUI |  |  | virtual |
| System.Void | WindowUpdate |  |  | virtual |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| UnityEngine.Vector2 | InitialSize | yes |  |  |
| System.Single | Margin | yes |  |  |
| System.Boolean | IsDebug | yes |  |  |
| System.Boolean | IsOpen | yes |  |  |
| RimWorld.QuickSearchWidget | CommonSearchWidget | yes |  |  |
| System.String | CloseButtonText | yes |  |  |

### All Public/Protected Properties (Window)
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| UnityEngine.Vector2 | InitialSize | yes |  |  |
| System.Single | Margin | yes |  |  |
| System.Boolean | IsDebug | yes |  |  |
| System.Boolean | IsOpen | yes |  |  |
| RimWorld.QuickSearchWidget | CommonSearchWidget | yes |  |  |
| System.String | CloseButtonText | yes |  |  |


## Verse.KeyBindingDef
BaseType: Verse.Def · Sealed: False · Abstract: False

### Constructors
```csharp
new KeyBindingDef();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Boolean | get_IsDown |  |  |  |
| System.Boolean | get_IsDownEvent |  |  |  |
| System.Boolean | get_JustPressed |  |  |  |
| System.Boolean | get_KeyDownEvent |  |  |  |
| UnityEngine.KeyCode | get_MainKey |  |  |  |
| System.String | get_MainKeyLabel |  |  |  |
| UnityEngine.KeyCode | GetDefaultKeyCode | BindingSlot slot |  |  |
| Verse.KeyBindingDef | Named | String name | YES | static |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| UnityEngine.KeyCode | MainKey | yes |  |  |
| System.String | MainKeyLabel | yes |  |  |
| System.Boolean | KeyDownEvent | yes |  |  |
| System.Boolean | IsDownEvent | yes |  |  |
| System.Boolean | JustPressed | yes |  |  |
| System.Boolean | IsDown | yes |  |  |


## Verse.ContentFinder`1
BaseType: System.Object · Sealed: True · Abstract: True

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| T | Get | String itemPath, Boolean reportFailure | YES | static |
| System.Collections.Generic.IEnumerable`1<T> | GetAllInFolder | String folderPath | YES | static |
| T | TryFindAssetInModBundles | String itemPath | YES | static |


## RimWorld.MainTabDef — NOT FOUND

## RimWorld.ScenPart_GameStartDialog
BaseType: RimWorld.ScenPart · Sealed: False · Abstract: False

### Constructors
```csharp
new ScenPart_GameStartDialog();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Void | DoEditInterface | Listing_ScenEdit listing |  | virtual |
| System.Void | ExposeData |  |  | virtual |
| System.Int32 | GetHashCode |  |  | virtual |
| System.Void | PostGameStart |  |  | virtual |


## RimWorld.ScenPart_GameCondition
BaseType: RimWorld.ScenPart · Sealed: False · Abstract: False

### Constructors
```csharp
new ScenPart_GameCondition();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Boolean | CanCoexistWith | ScenPart other |  | virtual |
| System.Void | DoEditInterface | Listing_ScenEdit listing |  | virtual |
| System.Void | ExposeData |  |  | virtual |
| System.Void | GenerateIntoMap | Map map |  | virtual |
| System.String | get_Label |  |  | virtual |
| System.Int32 | GetHashCode |  |  | virtual |
| System.Boolean | HasNullDefs |  |  | virtual |
| System.Void | PostWorldGenerate |  |  | virtual |
| System.Void | Randomize |  |  | virtual |
| System.String | Summary | Scenario scen |  | virtual |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| System.String | Label | yes |  |  |


## RimWorld.ScenPart_PermaGameCondition
BaseType: RimWorld.ScenPart · Sealed: False · Abstract: False

### Constructors
```csharp
new ScenPart_PermaGameCondition();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Boolean | CanCoexistWith | ScenPart other |  | virtual |
| System.Void | DoEditInterface | Listing_ScenEdit listing |  | virtual |
| System.Void | ExposeData |  |  | virtual |
| System.Void | GenerateIntoMap | Map map |  | virtual |
| System.String | get_Label |  |  | virtual |
| System.Int32 | GetHashCode |  |  | virtual |
| System.Collections.Generic.IEnumerable`1<System.String> | GetSummaryListEntries | String tag |  | virtual |
| System.Boolean | HasNullDefs |  |  | virtual |
| System.Void | Randomize |  |  | virtual |
| System.String | Summary | Scenario scen |  | virtual |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| System.String | Label | yes |  |  |


## Verse.LetterStack
BaseType: System.Object · Sealed: True · Abstract: False

### Constructors
```csharp
new LetterStack();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Void | ExposeData |  |  | virtual final |
| Verse.BundleLetter | get_BundleLetter |  |  |  |
| System.Single | get_LastTopY |  |  |  |
| System.Collections.Generic.List`1<Verse.Letter> | get_LettersListForReading |  |  |  |
| System.Void | LettersOnGUI | Single baseY |  |  |
| System.Void | LetterStackTick |  |  |  |
| System.Void | LetterStackUpdate |  |  |  |
| System.Void | Notify_FactionRemoved | Faction faction |  |  |
| System.Void | Notify_LetterMouseover | Letter let |  |  |
| System.Void | OpenAutomaticLetters |  |  |  |
| System.Void | ReceiveLetter | TaggedString label, TaggedString text, LetterDef textLetterDef, LookTargets lookTargets, Faction relatedFaction, Quest quest, List`1 hyperlinkThingDefs, String debugInfo, Int32 delayTicks, Boolean playSound |  |  |
| System.Void | ReceiveLetter | TaggedString label, TaggedString text, LetterDef textLetterDef, String debugInfo, Int32 delayTicks, Boolean playSound |  |  |
| System.Void | ReceiveLetter | Letter let, String debugInfo, Int32 delayTicks, Boolean playSound |  |  |
| System.Void | RemoveLetter | Letter let |  |  |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| System.Collections.Generic.List`1<Verse.Letter> | LettersListForReading | yes |  |  |
| System.Single | LastTopY | yes |  |  |
| Verse.BundleLetter | BundleLetter | yes |  |  |

### ReceiveLetter Overloads
| Return | Params | Static | Notes |
|---|---|---|---|
| System.Void | Verse.TaggedString label, Verse.TaggedString text, Verse.LetterDef textLetterDef, Verse.LookTargets lookTargets, RimWorld.Faction relatedFaction, RimWorld.Quest quest, System.Collections.Generic.List`1<Verse.ThingDef> hyperlinkThingDefs, System.String debugInfo, System.Int32 delayTicks, System.Boolean playSound |  |  |
| System.Void | Verse.TaggedString label, Verse.TaggedString text, Verse.LetterDef textLetterDef, System.String debugInfo, System.Int32 delayTicks, System.Boolean playSound |  |  |
| System.Void | Verse.Letter let, System.String debugInfo, System.Int32 delayTicks, System.Boolean playSound |  |  |


## Verse.LetterDef
BaseType: Verse.Def · Sealed: False · Abstract: False

### Constructors
```csharp
new LetterDef();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| UnityEngine.Texture2D | get_Icon |  |  |  |
| System.Void | ResolveReferences |  |  | virtual |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| UnityEngine.Texture2D | Icon | yes |  |  |


## Verse.LookTargets
BaseType: System.Object · Sealed: False · Abstract: False

### Constructors
```csharp
new LookTargets();
new LookTargets(Verse.Thing t);
new LookTargets(RimWorld.Planet.WorldObject o);
new LookTargets(Verse.IntVec3 c, Verse.Map map);
new LookTargets(RimWorld.Planet.PlanetTile tile);
new LookTargets(System.Collections.Generic.IEnumerable`1<RimWorld.Planet.GlobalTargetInfo> targets);
new LookTargets(RimWorld.Planet.GlobalTargetInfo[] targets);
new LookTargets(System.Collections.Generic.IEnumerable`1<Verse.TargetInfo> targets);
new LookTargets(Verse.TargetInfo[] targets);
new LookTargets(System.Collections.Generic.IEnumerable`1<Verse.Thing> targets);
new LookTargets(System.Collections.Generic.IEnumerable`1<Verse.ThingWithComps> targets);
new LookTargets(System.Collections.Generic.IEnumerable`1<Verse.Pawn> targets);
new LookTargets(System.Collections.Generic.IEnumerable`1<Verse.Building> targets);
new LookTargets(System.Collections.Generic.IEnumerable`1<RimWorld.Plant> targets);
new LookTargets(System.Collections.Generic.IEnumerable`1<RimWorld.Planet.WorldObject> targets);
new LookTargets(System.Collections.Generic.IEnumerable`1<RimWorld.Planet.Caravan> targets);
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Void | ExposeData |  |  | virtual final |
| System.Boolean | get_Any |  |  |  |
| Verse.LookTargets | get_Invalid |  | YES | static |
| System.Boolean | get_IsValid |  |  |  |
| RimWorld.Planet.GlobalTargetInfo | get_PrimaryTarget |  |  |  |
| System.Void | Highlight | Boolean arrow, Boolean colonistBar, Boolean circleOverlay |  |  |
| System.Void | Notify_MapRemoved | Map map |  |  |
| Verse.LookTargets | op_Implicit | Thing t | YES | static |
| Verse.LookTargets | op_Implicit | WorldObject o | YES | static |
| Verse.LookTargets | op_Implicit | TargetInfo target | YES | static |
| Verse.LookTargets | op_Implicit | List`1 targets | YES | static |
| Verse.LookTargets | op_Implicit | GlobalTargetInfo target | YES | static |
| Verse.LookTargets | op_Implicit | List`1 targets | YES | static |
| Verse.LookTargets | op_Implicit | List`1 targets | YES | static |
| Verse.LookTargets | op_Implicit | List`1 targets | YES | static |
| Verse.LookTargets | op_Implicit | List`1 targets | YES | static |
| Verse.LookTargets | op_Implicit | List`1 targets | YES | static |
| Verse.LookTargets | op_Implicit | List`1 targets | YES | static |
| Verse.LookTargets | op_Implicit | List`1 targets | YES | static |
| Verse.LookTargets | op_Implicit | List`1 targets | YES | static |
| System.Boolean | SameTargets | LookTargets a, LookTargets b | YES | static |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| Verse.LookTargets | Invalid | yes |  | YES |
| System.Boolean | IsValid | yes |  |  |
| System.Boolean | Any | yes |  |  |
| RimWorld.Planet.GlobalTargetInfo | PrimaryTarget | yes |  |  |


## Verse.TaggedString
BaseType: System.ValueType · Sealed: True · Abstract: False

### Constructors
```csharp
new TaggedString(System.String dat);
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| Verse.TaggedString | AdjustedFor | Pawn p, String pawnSymbol, Boolean addRelationInfoSymbol |  |  |
| Verse.TaggedString | CapitalizeFirst |  |  |  |
| Verse.TaggedString | EndWithPeriod |  |  |  |
| System.Char | get_Item | Int32 i |  |  |
| System.Int32 | get_Length |  |  |  |
| System.String | get_RawText |  |  |  |
| System.Int32 | get_StrippedLength |  |  |  |
| System.Single | GetWidthCached |  |  |  |
| System.Boolean | NullOrEmpty |  |  |  |
| Verse.TaggedString | op_Addition | TaggedString t1, TaggedString t2 | YES | static |
| Verse.TaggedString | op_Addition | String t1, TaggedString t2 | YES | static |
| Verse.TaggedString | op_Addition | TaggedString t1, String t2 | YES | static |
| System.String | op_Implicit | TaggedString taggedString | YES | static |
| Verse.TaggedString | op_Implicit | String str | YES | static |
| Verse.TaggedString | Replace | String oldValue, String newValue |  |  |
| System.String | Resolve |  |  |  |
| Verse.TaggedString | Shorten |  |  |  |
| Verse.TaggedString | ToLower |  |  |  |
| System.String | ToString |  |  | virtual |
| Verse.TaggedString | Trim |  |  |  |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| System.String | RawText | yes |  |  |
| System.Char | Item | yes |  |  |
| System.Int32 | Length | yes |  |  |
| System.Int32 | StrippedLength | yes |  |  |


## RimWorld.IncidentWorker
BaseType: System.Object · Sealed: False · Abstract: False

### Constructors
```csharp
new IncidentWorker();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Boolean | CanFireNow | IncidentParms parms |  |  |
| System.Boolean | CanFireNowSub | IncidentParms parms |  | virtual |
| System.Single | ChanceFactorNow | IIncidentTarget target |  | virtual |
| System.Boolean | FiredTooRecently | IIncidentTarget target |  |  |
| System.Single | get_BaseChanceThisGame |  |  | virtual |
| System.Void | SendIncidentLetter | TaggedString baseLetterLabel, TaggedString baseLetterText, LetterDef baseLetterDef, IncidentParms parms, LookTargets lookTargets, IncidentDef def, NamedArgument[] textArgs | YES | static |
| System.Void | SendStandardLetter | IncidentParms parms, LookTargets lookTargets, NamedArgument[] textArgs |  |  |
| System.Void | SendStandardLetter | TaggedString baseLetterLabel, TaggedString baseLetterText, LetterDef baseLetterDef, IncidentParms parms, LookTargets lookTargets, NamedArgument[] textArgs |  |  |
| System.Boolean | TryExecute | IncidentParms parms |  |  |
| System.Boolean | TryExecuteWorker | IncidentParms parms |  | virtual |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| System.Single | BaseChanceThisGame | yes |  |  |

### Send* Methods (IncidentWorker)
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Void | SendIncidentLetter | TaggedString baseLetterLabel, TaggedString baseLetterText, LetterDef baseLetterDef, IncidentParms parms, LookTargets lookTargets, IncidentDef def, NamedArgument[] textArgs | YES | static |


## Verse.GameComponent
BaseType: System.Object · Sealed: False · Abstract: True

### Constructors
```csharp
new GameComponent();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Void | AppendDebugString | StringBuilder sb |  | virtual |
| System.Void | ExposeData |  |  | virtual |
| System.Void | FinalizeInit |  |  | virtual |
| System.Void | GameComponentOnGUI |  |  | virtual |
| System.Void | GameComponentTick |  |  | virtual |
| System.Void | GameComponentUpdate |  |  | virtual |
| System.Void | LoadedGame |  |  | virtual |
| System.Void | StartedNewGame |  |  | virtual |


## Verse.Def
BaseType: Verse.Editable · Sealed: False · Abstract: False

### Constructors
```csharp
new Def();
```

### Public/Protected Methods
| Return | Name | Params | Static | Notes |
|---|---|---|---|---|
| System.Void | ClearCachedData |  |  | virtual |
| System.Collections.Generic.IEnumerable`1<System.String> | ConfigErrors |  |  | virtual |
| System.Boolean | Equals | Def other |  | virtual final |
| Verse.TaggedString | get_LabelCap |  |  | virtual |
| System.Int32 | GetHashCode |  |  | virtual |
| T | GetModExtension |  |  |  |
| System.Boolean | HasModExtension |  |  |  |
| System.Void | PostSetIndices |  |  | virtual |
| System.Void | ResolveDefNameHash |  |  |  |
| System.Void | ResolveReferences |  |  | virtual |
| System.Collections.Generic.IEnumerable`1<RimWorld.StatDrawEntry> | SpecialDisplayStats | StatRequest req |  | virtual |
| System.String | ToString |  |  | virtual |

### Public/Protected Properties
| Type | Name | Get | Set | Static |
|---|---|---|---|---|
| Verse.TaggedString | LabelCap | yes |  |  |


