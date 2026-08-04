# Vanilla-API-Matrix (RimWorld 1.6.4566)

> **SSOT-Owner für:** Vanilla-1.6-API-Matrix (Klassen, Methoden, Owner-Spalte). Orient-Entry siehe [docs/ARCHITECTURE.md §1](ARCHITECTURE.md).
> **Stand:** 2026-08-04 · **Spike-Tool:** `tools/inspect/` (Mono.Cecil-Reflection)
> **Quelle:** `/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed/Assembly-CSharp.dll`
> **Identitäts-Hash (SHA-256):** `A0AF57EF9162B569D3B052818BA3A29FCCCC8610F355F2BB9B08FAD5882982D3` · Größe: 15.746.048 Bytes
> **Bezugsvertrag:** Diese Matrix ist die kanonische Verbindlichkeit für jeden späteren Code-Task aus `docs/superpowers/plans/2026-08-04-early-game-vertical-slice.md`. Tasks mit Status `⚠ SPIKE-PFLICHT NICHT GESCHLOSSEN` dürfen vor einem weiteren Spike nicht implementiert werden.
> **Rohdaten:** `tools/inspect/api-matrix.raw.md` (34.231 Zeichen, vollständige Konstruktoren-/Methoden-/Properties-Listen je Anker).

## 1. Belegstufen

| Stufe | Bedeutung |
|---|---|
| ✅ | In Assembly-CSharp vorhanden, Mono.Cecil-public/protected enumeriert, Signatur verbatim verifiziert |
| ⚠ | Niederprior nicht gefunden, Spike-Pflicht offen (`ScenarioBase` etc.) |
| 🟡 | Heuristik-Sweep bestätigt Existenz, exakte Signatur(en) im Sweep-Bereich dokumentiert |

## 2. Klassen-Tabelle (15 Vanilla-Anker aus Phase 0 / Task 0.1)

| Anker | Vollständiger Name | Base | Sealed | Abstract | Public-Methods | Status |
|---|---|---|---|---|---|---|
| `ScenPart` | `RimWorld.ScenPart` | `System.Object` | ❌ | ✅ | 33 | ✅ |
| `GameComponent` | `Verse.GameComponent` | `System.Object` | ❌ | ✅ | 8 | ✅ |
| `MapComponent` | `Verse.MapComponent` | `System.Object` | ❌ | ✅ | 8 | ✅ |
| `WorldComponent` | `Verse.WorldComponent` | `System.Object` | ❌ | ✅ | 5 | ✅ |
| `ThingComp` | `Verse.ThingComp` | `System.Object` | ❌ | ✅ | 81 | ✅ |
| `IncidentWorker` | `RimWorld.IncidentWorker` | `System.Object` | ❌ | ❌ | 10 | ✅ |
| `RecipeWorker` | `RimWorld.RecipeWorker` | `System.Object` | ❌ | ❌ | 13 | ✅ |
| `Designator` | `RimWorld.Designator` | `Verse.Command` | ❌ | ✅ | 38 | ✅ |
| `GenSight` | `RimWorld.GenSight` | `System.Object` | ✅ | ✅ | 11 (alle statisch) | ✅ |
| `FogGrid` | `Verse.FogGrid` | `System.Object` | ✅ | ❌ | 11 | ✅ |
| `PawnGenerator` | `RimWorld.PawnGenerator` | `System.Object` | ✅ | ✅ | 15 (alle statisch) | ✅ |
| `ResearchManager` | `RimWorld.ResearchManager` | `System.Object` | ✅ | ❌ | 23 | ✅ |
| `CompRefuelable` | `RimWorld.CompRefuelable` | `Verse.ThingComp_VacuumAware` | ❌ | ❌ | 28 | ✅ |
| `CompGlower` | `RimWorld.CompGlower` | `Verse.ThingComp` | ❌ | ❌ | 18 | ✅ |
| `ScenarioBase` | `Verse.ScenarioBase` | — | — | — | — | ⚠ RENAMED / INLINED |

## 3. Sub-Typ-Auflistung (Owner-spezifische Auswahl)

### 3.1 `ScenPart` — abgeleitete Vanilla-Typen (sample 15)

- `RimWorld.ScenPart_GameCondition` (über `ScenPart_ConfigPage`)
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

→ Phase 1.1 Single-Survivor-Szenario: eigene `ScenPart_RimconemyStart` leitet von `RimWorld.ScenPart` ab, **nicht von einem vermeintlichen `ScenarioBase`**.

### 3.2 `GameComponent` — abgeleitete Vanilla-Typen (sample 6)

- `Verse.GameComponent_DebugTools`
- `Verse.GameComponent_OnetimeNotification`
- `RimWorld.GameComponent_Anomaly`
- `RimWorld.GameComponent_Bossgroup`
- `RimWorld.GameComponent_PawnDuplicator`
- `RimWorld.GameComponent_PsychicRitualManager`

### 3.3 `MapComponent` — abgeleitete Vanilla-Typen (sample 12)

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

### 3.4 `IncidentWorker` — relevante Methoden

```text
CanFireNow(IncidentParms parms) -> bool              Nicht-virtual, Vor-Check
CanFireNowSub(IncidentParms parms) -> bool           virtual (Override-Punkt)
ChanceFactorNow(IIncidentTarget target) -> float     virtual
TryExecute(IncidentParms parms) -> bool              Nicht-virtual, ruft TryExecuteWorker
TryExecuteWorker(IncidentParms parms) -> bool        virtual (Override-Punkt)
SendIncidentLetter / SendStandardLetter               Letter-API
```

### 3.5 `RecipeWorker` — kritischer Hook für Phase 8.4

```text
Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients) -> void
```

→ Phase 8.4 Rezeptabschluss: dieser Hook ist der einzige Vanilla-Weg, der nach erfolgreichem Output ausgelöst wird. Idempotenz muss auf `(recipe, billDoer, outputHash)` aufbauen.

### 3.6 `Designator` — kritische Hooks für Phase 4/9

```text
CanDesignateCell(IntVec3 loc) -> AcceptanceReport    virtual abstract
CanDesignateThing(Thing t) -> AcceptanceReport       virtual
ProcessInput(Event ev) -> void                        virtual
DesignateSingleCell(IntVec3 c) -> void               virtual
DesignateMultiCell(IEnumerable<IntVec3> cells) -> void   virtual
Visible (logical/control)                            virtual
```

→ Phase 4/9: `Designator_BuildRimconemy : Designator_Build` ist die bevorzugte Ableitung; `CanDesignateThing` ist der Gate für `UnlockService.IsUnlocked()`.

### 3.7 `GenSight` — kritischer LOS-Hook für Phase 6.2

Statische Methoden (alle `LineOfSight`-Varianten):

```text
LineOfSight(IntVec3 start, IntVec3 end, Map map) -> bool      (3-arg, Standard)
LineOfSight(IntVec3 start, IntVec3 end, Map map, bool skipFirstCell, Func<IntVec3,bool> validator, int halfXOffset, int halfZOffset) -> bool
LineOfSight(IntVec3 start, IntVec3 end, Map map, CellRect startRect, CellRect endRect, Func<...> validator, bool forLeaning) -> bool
LineOfSightToEdges(...) -> bool
LineOfSightToThing(IntVec3 start, Thing t, ...) -> bool
```

→ Phase 6.2: `GenSight.LineOfSight(start, end, map)` ist die einfache Variante für den `CanWarnPlayer`-Pfad.

### 3.8 `CompRefuelable` — Methoden für Phase 1.3 + Phase 5.2

```text
ConsumeFuel(float amount) -> void                       (Verbrauchs-Hook)
Refuel(List<Thing> fuelThings) -> void                   (Bulk-Refuel)
Refuel(float amount) -> void
get_Fuel / get_FuelPercentOfMax / get_FuelPercentOfTarget  (Lese-Properties)
get_HasFuel / get_IsFull                                 (Boolean-Gates)
Initialize(CompProperties props) -> void                 (Init-Hook, post-attach)
PostExposeData() -> void                                 (Save-/Load-Roundtrip)
CompTick() -> void                                       (Tick-Pfad)
```

→ Phase 1.3: Harmony-Postfix auf `ConsumeFuel` oder `Verb_Shoot.TryCastShot` — siehe Heuristik-Sweeps. `get_Fuel` ist ggf. relevanter als `get_FuelPercentOfMax`.

### 3.9 `CompGlower` — Methoden für Phase 5.2

```text
get_GlowColor / set_GlowColor -> ColorInt
get_GlowRadius / set_GlowRadius -> float
get_Glows -> bool                              (Phase 5.2 Haupt-Gate)
get_ShouldBeLitNow -> bool                     (virtual; wann glower aktiv ist)
ShouldBeLitNow(Map map) -> void                (UpdateLit)
ForceRegister(Map map) -> void
```

→ Phase 5.2 FireSignature: `comp.Glows && comp.ShouldBeLitNow` ist die Verfügbarkeits-Prüdikation, nicht nur Fuel.

### 3.10 `PawnGenerator` — kritische Methoden für Phase 1.4

```text
GeneratePawn(PawnKindDef kindDef, Faction faction, Nullable<PlanetTile> tile) -> Pawn
GeneratePawn(PawnGenerationRequest request) -> Pawn
GenerateTraitsFor(Pawn pawn, int traitCount, ...) -> List<Trait>
```

→ Phase 1.4: `PawnGenerator.GeneratePawn(new PawnGenerationRequest { kindDef = RimconemyPawnKind, ... })` ist die 1.6-empfohlene Form.

### 3.11 `ResearchManager` — kritische Methoden

```text
AddProgress(ResearchProjectDef proj, float amount, Pawn source) -> void
FinishProject(ResearchProjectDef proj, bool doCompletionDialog, Pawn researcher, bool doCompletionLetter) -> void
GetProgress(ResearchProjectDef proj) -> float
IsCurrentProject(ResearchProjectDef proj) -> bool
get_CurrentAnomalyKnowledgeProjects -> List<KnowledgeCategoryProject>
get_AnyProjectIsAvailable -> bool
```

→ Phase 12: `ResearchManager` bleibt für DLC-/Fremdmod-Kompatibilität als Read-Modell reachable. Rimconemy's eigene Freischaltlogik darf `FinishProject`/`AddProgress` nicht direkt aufrufen, ohne ein Capability-Gate zu durchlaufen.

## 4. Heuristik-Sweep-Befunde (Spike-Pflicht-Hooks)

### 4.1 Phase 1.3 Ammo-Verbrauch — ✅ bestätigt (3 Hook-Optionen)

Hook-Option A (frühester Schuss):

```text
Verse.Verb.TryStartCastOn(LocalTargetInfo castTarg, bool surpriseAttack, bool canHitNonTargetPawns, bool preventFriendlyFire, bool nonInterruptingSelfCast) -> bool
Verse.Verb.TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack, bool canHitNonTargetPawns, bool preventFriendlyFire, bool nonInterruptingSelfCast) -> bool
```

Hook-Option B (typischer Schuss-Hook, Klassenebene):

```text
Verse.Verb_Shoot.TryCastShot() -> bool
Verse.Verb_LaunchProjectile.TryCastShot() -> bool (Mehrfach-Override existiert)
```

Hook-Option C (Launch, spätester Punkt):

```text
Verse.Projectile.Launch(Thing launcher, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire, Thing equipment) -> void
Verse.Projectile.Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire, Thing equipment, ThingDef targetCoverDef) -> void
```

Empfehlung: Harmony-Prefix auf `Verb.TryStartCastOn` und Postfix auf `Verb_Shoot.TryCastShot` (zwei Hooks, einer vor- und einer nach-Verbrauch). CE-Kompatibilität: `Verb_Shoot.TryCastShot` ist *vor* `Projectile.Launch` und damit kompatibel mit `Verb_Shoot` von CE.

### 4.2 Phase 3.2 Kälte-Temperatur — ⚠ **SPIKE-PFLICHT NICHT GESCHLOSSEN**

Sweep-Verifikation gegen Assembly-CSharp:

```text
GenTemperature       : 0 Treffer
RoomTemperature      : 0 Treffer
TemperatureAtCell    : 0 Treffer
OutdoorTemperature   : 0 Treffer
```

Hypothese: die Helfer-Klassen wurden in 1.6 in eine andere Assembly verschoben (z.B. `System.Reflection`-Analyse deutet auf eine separate Bibliothek hin) oder heißen anders (`GetTemperatureAt`, `TypedTemperatureAt`, …). Vor Implementierung: weitere Spike mit erweiterten Such-Heuristiken (`Temperature` ohne Suffix, `RoomTemp`, `CellTemperature` etc.) oder Decompile-Snapshot aus `DebugSymbols`.

Phase-3.2-Task bleibt **BLOCKED** bis der Spike-Pflicht-Befund geschlossen ist.

### 4.3 Phase 5.2 Feuer-Signatur / Brennstoff — ✅ bestätigt

```text
Verse.FireUtility.IsBurning(TargetInfo t) -> bool
Verse.FireUtility.IsBurning(Thing t) -> bool
RimWorld.CompRefuelable.ConsumeFuel(float amount) -> void
RimWorld.CompRefuelable.Refuel(List<Thing> fuelThings) -> void
RimWorld.CompRefuelable.Refuel(float amount) -> void
```

Vermisst (0 Treffer):

```text
FuelPercent (kein Direktname — Property ist FuelPercentOfMax/FuelPercentOfTarget auf CompRefuelable)
IsBurning auf Thing (nur statisch via FireUtility verfügbar)
```

Empfehlung: `CompGlows(Glows, ShouldBeLitNow)` als primärer Feuer-Signal, nicht `IsBurning(thing)`.

### 4.4 Phase 6.2 LineOfSight — ✅ bestätigt

Siehe §3.7 oben. Drei `LineOfSight`-Überladungen plus `LineOfSightToThing` stehen direkt zur Verfügung.

### 4.5 Phase 8.3 Bauabschluss — ✅ bestätigt (2026-08-04)

Sweep-Verifikation (Mono.Cecil gegen `Assembly-CSharp.dll` 1.6.4566, SHA-256 `A0AF57EF9162B569D3B052818BA3A29FCCCC8610F355F2BB9B08FAD5882982D3`):

```text
Frame.CompleteConstruction(Pawn worker)  : 1 Treffer (RimWorld.Frame)
MakeFinished                            : 0 Treffer
FinishConstruction                      : 0 Treffer
SpawnFinished                           : 0 Treffer
ConstructionCompleted                   : 0 Treffer
Notify_BuildingComplete                 : 0 Treffer
FrameSpawned / FrameComplete            : 0 Treffer
FinishFrame / SpawnFrame                : 0 Treffer
Blueprint_Building                      : 0 Treffer
```

Plus Backup-Hooks (für Respawn-/Save-Load-Reparatur, falls Frame-Postfix Lücke lässt):

```text
Verse.Building.SpawnSetup(Map map, Boolean respawningAfterLoad) -> Void   virtual
Verse.Blueprint.IsCompleted() -> Boolean                                  virtual · final
RimWorld.Blueprint_Build.MakeSolidThing(Boolean& shouldSelect) -> Thing   virtual
RimWorld.Frame.IsCompleted()     -> Boolean                               virtual · final
RimWorld.Frame.get_PercentComplete() -> Single                             live
RimWorld.Frame.get_WorkLeft()    -> Single                                 live
```

Empfehlung:

```csharp
[HarmonyPatch(typeof(RimWorld.Frame), nameof(RimWorld.Frame.CompleteConstruction))]
public static class FrameCompletionPatch
{
    [HarmonyPostfix]
    public static void NotifyCompletion(Pawn worker, Frame __instance)
    {
        // 1) Resolve DomainXpState via ProgressionGameComponent.EnsureDomainXp()
        // 2) Call BuildingCompletionBridge.Submit(state, def, map, frame, worker, tick)
        // 3) Defensive null-guards for early startup, null Map, null def
    }
}
```

Raw-Sweep-Daten: `tools/inspect/phase-8.3-construction-hooks.raw.md` (12.151 Zeichen, Spike-Lauf gegen 1.6.4566).
Spike-Tool: `tools/inspect/Spike8_3.csproj` mit Mono.Cecil 0.11.5.

→ Phase 8.3 Hook-Pfad **SPIKE GESCHLOSSEN**. Phase 8.4 Hook (`RecipeWorker.Notify_IterationCompleted`) bleibt als Fallback-Beleg erhalten.

### 4.6 Phase 9.3 Spawn-Detection — Bonus-Befund

```text
Verse.FogGrid.IsFogged(IntVec3 c) -> bool
Verse.FogGrid.IsFogged(int index) -> bool
Verse.FogGrid.FloodUnfogAdjacent(IntVec3 c, bool sendLetters) -> void
```

→ `FogGrid.IsFogged` ist ein Vanilla-Hook für Phase 6.1/9.3 ohne Override des LOS-Systems.

## 5. Owner-Paket-Zuordnung (gemäß INTERFACE_CONTRACT §9.1)

| Vanilla-Anker | Owner-Paket | Geplanter Consumer |
|---|---|---|
| `ScenPart` | Survival (Spike-Phase 1.1) | Single-Survivor-Szenario |
| `GameComponent` / `MapComponent` / `WorldComponent` | 05 (Infected), 02 (Survival), 03 (Scavenger) | Save-/Tick-Subsysteme |
| `ThingComp` | Survival (Eigentümer) + Leser aller Pakete | Verhaltensanker für Verb-Gates, Recipe-XP, Fuel-Verbrauch |
| `IncidentWorker` | 05 (Infected) | Letter-/Spawn-Pfad (Phase 7.3) |
| `RecipeWorker` | Survival (Empfänger) | Rezeptabschluss-Hook (Phase 8.4) |
| `Designator` | Scavenger (Eigentümer) + Survival (Unlock-Leser) | Architect-Gate (Phase 4.1, 9.3) |
| `GenSight` | Survival (Reader) | LOS-Warnung (Phase 6.2) |
| `FogGrid` | Survival (Reader) | Spawn-Detection (Phase 6.1) |
| `PawnGenerator` | 05 (Infected) | Startgegner-Spawn (Phase 1.4) |
| `ResearchManager` | Kompatibilitätsschicht, alle Pakete als Read-Only | Legacy-Research-Read-Modell (Phase 12) |
| `CompRefuelable` | Scavenger (Eigentümer) + Survival (Reader via FuelHook) | Coal/Munitions-Output, Fuel-Verbrauch (Phase 1.3 mit Optionen) |
| `CompGlower` | Scavenger (Eigentümer) | Feuer-Signatur/Licht (Phase 5.2) |
| **`ScenarioBase`-Ersatz** | Survival | Scenario-Klasse direkt verwenden, kein eigener `ScenarioBase` |

## 6. DLC-Policy-Verträglichkeit (gemäß DECISIONS.md §15/§20)

Diese Auflistung wurde mit Mono.Cecil-Reflection gegen die **Core-Assembly** erzeugt. DLC-spezifische APIs (IdeoDef, PreceptDef, RoleDef, RitualDef, Anomaly-Storyteller, Odyssey-Gravship) sind in der lokalen Installation **nicht vorhanden** — die fünf DLCs sind im `Mods/`-Verzeichnis nicht installiert. Vor Phase-Aufgaben mit DLC-Anker ist ein eigener Spike mit DLC-Assemblies erforderlich.

Empfehlung: je DLC-Patch ein eigenes `tools/inspect-royalty`, `tools/inspect-ideology` etc. analog zum aktuellen Spike — nur jeweils die DLC-spezifischen Symbole enumeriert, mit identitäts-Hash-Protokoll.

## 7. Reproduktions-Skript

```bash
# 1. Restore + Build
dotnet restore tools/inspect/Inspect.csproj
dotnet build   tools/inspect/Inspect.csproj -c Release --no-restore

# 2. Spike ausführen (Rohdaten)
dotnet tools/inspect/bin/Release/net10.0/Rimconemy.Inspect.dll

# 3. Spike-Output lesen
cat tools/inspect/api-matrix.raw.md
```

Erwartung: SHA-256 stimmt mit Identitäts-Hash in §0 überein; alle Statusmarkierungen reproduzieren sich. Wenn nicht, ist die RimWorld-Installation aktualisiert worden und die Spike-Werte müssen in diesem Dokument gepflegt werden.

## 8. Pflicht-Lücken (offene Spike-Pflicht-Befunde)

| Task | Methode(n) | Status | Eintrittsbedingung |
|---|---|---|---|
| Phase 1.1 | `Verse.ScenarioBase` | ⚠ RENAMED | `RimWorld.Scenario` direkt verwenden, kein eigener `ScenarioBase`-Versuch |
| Phase 1.3 | `Verb.TryStartCastOn` / `Verb_Shoot.TryCastShot` / `Projectile.Launch` | ✅ bestätigt | Harmony-Prefix auf Verb.TryStartCastOn + Postfix auf Verb_Shoot.TryCastShot |
| Phase 3.2 | `GenTemperature.GetTemperatureAtCell` o. ä. | ⚠ 0 Treffer | Erweiterte Spike-Sweep-Heuristik oder Decompile-Snapshot nötig; **TASK BLOCKED** |
| Phase 5.2 | `FireUtility.IsBurning` / `CompRefuelable.ConsumeFuel` | ✅ bestätigt | `CompGlower.Glows && ShouldBeLitNow` als Haupt-Gate |
| Phase 6.2 | `GenSight.LineOfSight(3-arg)` | ✅ bestätigt | direkt einsetzbar |
| Phase 8.3 | `RimWorld.Frame.CompleteConstruction(Pawn worker)` | ✅ bestätigt 2026-08-04 | Harmony-Postfix auf 1.6-Frame.CompleteConstruction; Rohdaten siehe `tools/inspect/phase-8.3-construction-hooks.raw.md` |
| Phase 8.4 | `RecipeWorker.Notify_IterationCompleted` / `Bill.Notify_IterationCompleted` | ✅ bestätigt | Idempotenz auf `(recipeDef, billDoer, outputHash)` |
| Phase 9.3 | `Designator.CanDesignateCell` / `CanDesignateThing` | ✅ bestätigt | `UnlockService.IsUnlocked()`-Gate einbauen |

## 9. Verbindlichkeit und Pflege

Wenn die RimWorld-1.6-Installation aktualisiert wird (neuer Build, neuer Patch), muss:

1. SHA-256 aktualisiert werden (siehe §0).
2. `tools/inspect/api-matrix.raw.md` regeneriert werden.
3. Diese Matrix auf Inkonsistenzen durchsucht und die `STATUS`-Spalten angepasst werden.
4. Alle Tasks mit `⚠`-Status neu bewertet werden (vor Implementierung muss Spike-Pflicht geschlossen sein).

Diese Datei ist Single-Source-of-Truth für Vanilla-API-Annahmen. Inline-`strings`-Behauptungen (z.B. "GenTemperature gibt es in 1.6") sind nicht zulässig — ein Verweis auf §X dieser Matrix ist verpflichtend.
