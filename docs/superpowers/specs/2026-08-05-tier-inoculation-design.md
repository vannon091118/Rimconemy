# Spec — Tier-Inokulation (Phase C)

> **Stand:** 2026-08-05
> **Owner:** Infected & Automation (Package 05)
> **Phase:** C von 4 (siehe 4-Phasen-Decomp)
> **Code-Anker (geplant):**
> - `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs`
> - `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/InoculationSelectorLogic.cs` (pure helper)
> - `mods/05-Rimconemy-Infected-Automation/Source/World/InfectedPackBehavior.cs` (Rudel-Wanderer)
> - `mods/05-Rimconemy-Infected-Automation/Def/PawnKinds/Rimconemy_InfectedWildlife.xml`
> - `mods/05-Rimconemy-Infected-Automation/Def/Factions/InfectedWildlifeFaction.xml` (optional, falls Custom-Faction separated werden soll)
> - `mods/05-Rimconemy-Infected-Automation/Tests/InoculationRegressionTests.cs`

## 1. Zweck / Warum diese Phase

User-Anforderung 2026-08-05: Tiere sollen sich **ab und zu** (Profile-getrieben, **nicht jeden Tag**) infizieren und auf der Map umherwandern + (rudelbasiert) Colonists jagen. Verteilung: einmal pro **mehrere Tage** (Profile-Survival: max. 1× pro Woche), nicht jeden Tag.

**Was Phase A bereits liefert** (Daten-SSOT, seit `d02e392`/`7f2d1b3` committed):

| Hook | Datei | Status |
|---|---|---|
| `PopulationLedger.NoteInoculation(string)` | `Population/PopulationLedger.cs:127` | ✅ |
| `PopulationLedger.GetCumulativeInoculations()` | `Population/PopulationLedger.cs` | ✅ |
| `PopulationLedger.IsInoculationCooldownElapsed()` | Profile-getrieben | ✅ |
| `PopulationProfileMultipliers.InoculationsPerDay` | `Refuge=0, Survival=1, Collapse=3` | ✅ |
| `PopulationProfileMultipliers.InoculationMinIntervalTicks` | `Refuge=∞, Survival=7d, Collapse=3d` | ✅ |
| `PopulationLedgerReconciler` trennt `AnimalLiveCount` von `HumanoidLiveCount` | `Population/PopulationLedgerReconciler.cs` | ✅ |

**Was Phase C liefert** (Verhaltens-Schicht):

| Komponente | Zweck |
|---|---|
| `RandomInoculationService` | Day-Tick Entry-Point: Wahrscheinlichkeit wälzen + Service-Aufruf |
| `InoculationSelectorLogic` | Reiner Selector: Wählt ein Wildtier aus einer Snapshot-Liste (deterministisch) |
| `Rimconemy_InfectedWildlife` (PawnKindDef) | Hybrid-Kind: Race des Original-Tiers + branded Hidden-Infected |
| `InfectedPackBehavior` (MapComponent Sidekick) | Rudel-Wanderer und Chase-Logic für Animal-Infected |
| `StoryDirector` Day-Tick Hook | Aufruf von `RandomInoculationService.TryInfectRandom(Map)` nach DailyGrowth |

## 2. Design-Entscheidungen (User-Approval 2026-08-05)

| # | Frage | Entscheidung |
|---|---|---|
| 1 | Tier-Kind-Repräsentation | **Hybrid**: 1 generische `Rimconemy_InfectedWildlife` PawnKind mit Original-Race (Wolf/Bear/Hyena/etc. bleibt) + branded `InfectedPawnState`. Race-Property bleibt `Animal=true, Humanlike=false`. |
| 2 | Sample-Range | **Alle Wild-Tiere auf der Map** (kein 60-tile-Radius). Atmosphärisch dichter — Animal-Inokulation ist selten, daher nicht Map-lokal begrenzt nötig. |
| 3 | Cap-Coupling | **AnimalHalveCap**: 1 Tier zählt 0.5 gegen `Cap`. Reconciliation summiert `(HumanoidCount + floor(AnimalCount * 0.5))` und meldet das Free-Budget für StorySelection. So bleibt die Taktik "viele Tiere infizieren ohne Cap zu sprengen" möglich, aber Tiere tragen trotzdem zur Population-Pressure bei. |
| 4 | Tier-AI-Verhalten | **PackBehavior/Rudel**: Tiere wandern in kleinen Rudel-Step-Pfaden, jagen aber NICHT als Assault-State (kein direkter Colonist-Kill). Statt dessen: "Investigating"-Äquivalent — wenn Colonist in Sicht, folgen sie. Wenn out-of-sight, zurück zum Rudel. |

## 3. Architektur

### Komponenten

| Komponente | Typ | Verantwortung |
|---|---|---|
| `RandomInoculationService` | Static class + Service-Locator | Day-Tick Entry-Point, ruft Selector + Converter. Idempotenz via Cooldown-Gate. |
| `InoculationSelectorLogic` | Static class (Pure-Helper) | Deterministic Tier-Selection aus Snapshot-Liste. Testbar ohne Map. |
| `InoculationConverter` | Static class (Pure-Helper) | Wandelt einen `PawnSnapshot` → `InoculationOutcome { KindDefName, OriginalRaceDefName, ConvertedFactionDef }`. Testbar ohne Pawn-Instanz. |
| `Rimconemy_InfectedWildlife` | PawnKindDef XML | Hybrid-Kind: Race = Original-Tier (per kindDef-Profil zur Laufzeit ermittelt). |
| `InfectedPackBehavior` | Static helper | Tier-spezifische AI-Branch (Wander-Step + Chase-folgen). |
| `PopulationLedger.AdjustAnimalHalfCap(int deltaDeltaCap, int deltaAnimalCount)` | Method on PopulationLedger | Berechnet Cap-Effekt: floor(deltaAnimalHead * 0.5). |
| `StoryDirector.GameComponentTick` | Hook | Ruft `RandomInoculationService.TryInfectRandom(Map)` am Day-Tick (60000 ticks). |

### Datenstruktur

```csharp
// Inoculation-Candidate Snapshot (analog zu PopulationLedgerReconciler.PawnSnapshot)
public struct InoculationCandidate
{
    public string ThingId;
    public string KindDefName;          // original animal PawnKind.defName
    public string RaceDefName;          // e.g. "Wolf", "Bear", "Caribou"
    public string OriginalFactionDef;   // e.g. "WildFaction"
    public bool IsHumanlike;            // always false (rejection criterion)
    public bool IsAnimal;               // always true (selection criterion)
    public bool IsDead;
    public IntVec3 MapCell;             // for telemetry
}

// Inoculation-Outcome
public struct InoculationOutcome
{
    public string ThingId;
    public string OriginalKindDefName;
    public string OriginalRaceDefName;
    public string ConvertedFactionDef;
    public string ConvertedKindDefName; // = "Rimconemy_InfectedWildlife"
    public int CapDelta;                // = +1 (AnimalHalfCap: 1 head = 0.5, rounded up)
    public string Reason;               // e.g. "vision-selected", "cooldown", "no-candidates"
}
```

### Pure-Helper API (`InoculationSelectorLogic`)

```csharp
// Determinism-Quelle: Hash über ProfileId + Map-Tick + PopulationLedger-Fingerprint
public static int BuildInoculationSeed(string profileId, int mapId, long currentTick, int populationFingerprint);

// Wählt EINEN Tier aus der Liste oder null wenn keine.
public static InoculationCandidate? SelectCandidate(
    IReadOnlyList<InoculationCandidate> candidates,
    int seed,
    long currentTick);

// Filtert die Liste auf die kandidatenfähigen (animal && !dead && non-Infected)
public static void FilterCandidates(
    IReadOnlyList<InoculationCandidate> all,
    out List<InoculationCandidate> filtered);
```

### Pure-Helper API (`InoculationConverter`)

```csharp
// Wandelt einen Candidate + PawnKind-Mapping-Tabelle in einen Outcome.
// Wenn Original-Kind in der Mapping-Tabelle fehlt: Fallback "branded only".
public static InoculationOutcome Convert(inoculationCandidate, mappingTableOpt);

// Berechnet das Cap-Delta für eine eingetretene Inokulation.
public static int ComputeAnimalHalfCapDelta(int previousAnimalCount, int newAnimalCount);
```

### Service API (`RandomInoculationService`)

```csharp
public static class RandomInoculationService
{
    // Cooldown-Gate entscheidet, ob ein Inoculation-Versuch überhaupt
    // stattfindet (Profile-abhängig). Bei null/fehlendem Map: no-op + Warn.
    public static InoculationOutcome? TryInfectRandom(Map map, long currentTick);
}
```

## 4. Datenfluss (Daily Cycle in StoryDirector)

```
StoryDirector.GameComponentTick
    ├── alle 60.000 Ticks (1 Tag):
    │   1. ApplyDailyGrowthTick(B)              → Cap *= profile[D]
    │   2. ledger.ResetDailyCounters()           → RecentKillsToday = 0
    │   3. RandomInoculationService.TryInfectRandom(Map, currentTick)
    │       ├── (Phase A) ledger.IsInoculationCooldownElapsed()
    │       │   └── false → noop, log "CooldownGate"
    │       ├── InoculationSelectorLogic.BuildSeed(profileId, mapId, tick, fingerprint)
    │       ├── BuildSnapshotsFromMap(map)       → IList<InoculationCandidate>
    │       ├── SelectCandidate(candidates, seed, tick) → InoculationCandidate?
    │       ├── (null) → log "NoCandidatesForThisMap"
    │       └── (hit): InoculationConverter.Convert(candidate, mappingOpt)
    │           ├── Faction-Switch pawn.Faction → HiddenInfected
    │           ├── KindDef-Switch pawn.kindDef → Rimconemy_InfectedWildlife
    │           ├── populationLedger.NoteInoculation(originalKindDefName)
    │           └── ledger.AnimalLiveCount++ (durch Reconciler nächstes Tick)
    └── Spawnevent: Phase B's Story-Event verarbeitet StoryApply
```

## 5. Determinismus (Spec §7)

Baut auf `DeterministicRng` (Phase A-Vorgänger). Same seed → same candidate aus Liste:
- **Seed-Formula**: `BuildInoculationSeed(profileId + map.uniqueID + (currentTick / 60000) + populationFingerprint)`
- **Lückenfrei**: Wenn zwei Caches denselben `BuildSeed`-Output haben, wird derselbe Index und damit dasselbe Tier gewählt.
- **Save/Load-Replay**: Der Seed hängt nicht von `Find.Random` ab. Replay deterministisch.

## 6. Hybrid-PawnKind `Rimconemy_InfectedWildlife`

### XML-Skelett (Plan §Defs)

```xml
<Defs>
  <PawnKindDef>
    <defName>Rimconemy_InfectedWildlife</defName>
    <label>infected wildlife</label>
    <race>Human</race>          <!-- placeholder, runtime-swapped per chosen animal -->
    <defaultFactionDef>Rimconemy_HiddenInfectedFaction</defaultFactionDef>
    <initialResistanceRange>0~0</initialResistanceRange>
    <initialWillRange>0.4~0.6</initialWillRange>
    <combatPower>30</combatPower>
    <!-- Race wird zur Laufzeit überschrieben (praktisch: jeder Tier behält aber RaceProps.Humanlike=false, Animal=true) -->
  </PawnKindDef>
</Defs>
```

### Runtime-Verhalten

Da RimWorld 1.6 `<race>` nicht runtime-tauschbar ist (Race-Property am Pawn ist gecastet), wird:
- Original-Pawn bleibt mit Original-Race (Wolf bleibt Wolf).
- `pawn.kindDef` wird auf `Rimconemy_InfectedWildlife` gesetzt.
- `pawn.RaceProps.Humanlike` bleibt **false**.
- `pawn.Faction` wechselt zu `Rimconemy_HiddenInfectedFaction`.
- Branding via `InfectedPawnState` (Phase-A Pre-existing, geclont von Human-Infect-Pflicht).

Damit sieht das Tier aus wie ein Wolf, aber Health-Bar und Faction-Tab kennen es als "infected". UI/Logik sieht das via `pawn.RaceProps.Animal && pawn.Faction.def.defName == "Rimconemy_HiddenInfectedFaction"`.

## 7. Animal-HalfCap-Formel

`ledger.GetTotalCapBudget()` ist neu, wird in Phase C eingeführt:

```csharp
public int GetTotalCapBudget()
{
    // Human zählt 1, Tier zählt 0.5 → floor().
    return Cap - (HumanoidLiveCount + (int)System.Math.Floor((double)AnimalLiveCount / 2));
}
```

Pro Tier-Inokulation:
- `ledger.AnimalLiveCount += 1` (durch Reconciler nächste Tick)
- `ledger.Cap += 1` (Counter bleibt so), aber `GetTotalCapBudget()` reflektiert die 0.5-Wertung automatisch
- → keine explizite Cap-Delta-Behandlung nötig

Trade-off: `Cap` ist ein Brutto-Wert (wie viele Slots insgesamt), und die effektive Auslastung ist Humanoid + floor(AnimalCount/2). StoryDirector/SpawnService konsumieren `GetTotalCapBudget()` als Free-Slots.

## 8. Tier-AI `InfectedPackBehavior`

`InfectedPackBehavior` ist eine reine Static-Helper-Klasse (parallel zu `InfectedBehaviorTransition`). Wandert mit:

| State | Behavior |
|---|---|
| **Wandering** (Default) | Rudel wählt einen neuen Random-Wander-Step (15..25 tiles). Random-Direction; jeder Tier folgt mit DeterministicRng. |
| **Tracking** (in Sichtweite eines Colonist) | Tier folgt Colonist mit max-Speed bis out-of-Sight oder 60 Ticks ohne Sicht. |
| **Dissipating** (1d nach letzter Sichtung) | Rudel löst sich auf, Tiere gehen zurück zum Wandering und entfernen sich 50+ Tiles vom Spieler. |

`InfectedBehaviorTransition.ComputeNext()` für **tier-Infected** returnt direkt `PackBehavior`. Pro Tier-Pawn ein neuer tiny State-Block (kein Per-Pawn-Scribe weil Tier-Pawns sterben/sich auflösen fließend und nicht Save-relevanter als Human-Infected). PackBehavior-State ist **NICHT** persistiert (transient life-of-pawn).

## 9. Tests (`InoculationRegressionTests.cs`)

| # | Test | Was |
|---|---|---|
| I1 | SelectorDeterministicSameSeedSameCandidate | Seed=42 → same candidate; Seed=43 → different |
| I2 | SelectorEmptyListReturnsNull | Leere candidates → null |
| I3 | SelectorExcludesDeadAndHumanlike | 8 candidates mit 3 dead + 2 humanlike → 3 eligibles |
| I4 | SelectorExcludesAlreadyInfected | 5 candidates mit 1 Infected → 4 |
| I5 | SelectorRankingStable | Same seed, same list order → same index |
| I6 | ConverterMapsWolfToBrandedKind | OriginalKind="Wolf" + MappingTable → Outcome.KindDefName = "Rimconemy_InfectedWildlife" |
| I7 | ConverterFactionSwitch | Outcome.ConvertedFactionDef = "Rimconemy_HiddenInfectedFaction" |
| I8 | ConverterNoMappingFallback | Original Kind nicht in Mapping → Outcome.ConvertedFactionDef bleibt, KindDef-Name = Original |
| I9 | ComputeAnimalHalfCapDelta | previous=0, new=1 → +1 Cap-Budget-slot (effective 0.5 halved) |
| I10 | GetTotalCapBudgetTwoTierHalved | Cap=10, Human=4, Animal=4 → FreeBudget = 10 - (4+2) = 4 |
| I11 | ServiceCooldownGateNoop | `LastInoculationTick=100_000`, `currentTick=100_000` → returns null (within 7-day gate) |
| I12 | ServiceCooldownGateElapsed | `LastInoculationTick=100_000`, `currentTick=520_000` (420_000 ticks later) → fires |
| I13 | ServiceNullMapNoOpWarn | null map → returns null + Log.Warning |
| I14 | ServiceProfileBlocksInoculation | Profile="Refuge" → returns null (Refuge setzt Max=0) |
| I15 | ServiceBuildSeedDeterminism | Two seeds with same params → equal |
| I16 | NoteInoculationUpdatesLedger | After successful convert → ledger.CumulativeInoculations += 1 |

## 10. Bootstrap-Integration

In `Source/Bootstrap.cs`:
```csharp
Tests.InoculationRegressionTests.RunAll();
Log.Message("[Rimconemy.InfectedAutomation] Inoculation service ready (selector, converter, half-cap).");
```

`StoryDirector.GameComponentTick` :
```csharp
// Nach DailyGrowth-Tick + ResetRecentKills (Phase B Hook)
if (currentTick >= LastDayTick + EvaluationIntervalTicks) {
    ApplyDailyGrowthTick();
    ResetDailyCounters();
    RandomInoculationService.TryInfectRandom(playerMap, currentTick);  // Phase C
}
```

## 11. Fehlerbehandlung / Edge Cases

| Edge Case | Verhalten |
|---|---|
| `TryInfectRandom(null, tick)` | no-op + Log.Warning |
| `TryInfectRandom` mit Profile="Refuge" | no-op, InoculationsPerDay=0 |
| Selector returns null (alle candidates gefiltert) | no-op + Log.Message "no candidates this cycle" |
| `pawn.kindDef` nicht in Mapping-Tabelle | Fallback: nur Faction-Switch, Original-kindDef bleibt |
| Original-Tier ist Predator (Wolf, Bear) | Predator wird hostile. Nutzt Predator-AI (RimWorld Engine). Wandert + jagt. |
| Original-Tier ist Herbivore (Caribou, Boomalope) | Herbivore bleibt meistens passiv, jagt nicht aktiv. Folgt aber bei Sichtkontakt (Tracking-State). |
| Pawn stirbt vor/nach Inoculation | Reconciler korrigiert AnimalLiveCount via Tick-Reconcile. |
| Save/Load während Cooldown | Cooldown persist via `LastInoculationTick` in Phase A; wird korrekt geladen. |
| Inoculation während Map-Wechsel (Caravan) | Cooldown bleibt auf GameComponent-Ebene; Map-lokale Tier-Inokulation für temporäre Maps nicht unterstützt (Specs §Nicht-Ziele). |

## 12. Nicht-Ziele (Phase C)

- **Kein** World-Map-Inoculation (keine Caravan/Tile-Random-Inoculation, nur Map-Local).
- **Kein** Modifikation am RimWorld-Default-Wildlife-Verhalten — wir beobachten nur, schreiben nicht rein.
- **Kein** Custom-Mesh (kein "infected wolf"-Texture-Patch) — reine Faction-Switch.
- **Keine** Tier-Kill-Quote-Balance-Anpassung (Phase B wird StoryDirector-Revanche anpassen, falls Tier-Kills Revenge erhöhen sollen).
- **Keine** Cap-Hard-Limits-Anpassung — `Cap`-Ceiling aus Phase A (int.MaxValue/1000) bleibt.

## 13. Akzeptanz-Gate (Phase C SURVIVED)

- [ ] C1 — `InoculationRegressionTests.RunAll()` = 16/16 PASS im Bootstrap.
- [ ] C2 — `RandomInoculationService.TryInfectRandom` hat Live-Test-Beleg im Player.log.
- [ ] C3 — `Rimconemy_InfectedWildlife` PawnKindDef lädt, Brand=Race=Animal=true, Humanlike=false.
- [ ] C4 — `PopulationLedger.GetTotalCapBudget` API existiert und liefert Human+floor(Animal/2)-Formel.
- [ ] C5 — `InfectedPackBehavior` läuft in einer 5-Tile-Test-Szene: Tier wählt Zufallspfad, Colonist in Sicht, Tier folgt. Live-Beleg via Save+Inspect.
- [ ] C6 — `StoryDirector.GameComponentTick` ruft Service am Day-Tick auf.
- [ ] C7 — `./scripts/runtime_test.sh --skip-start --no-deploy` exit 0 nach Bump auf 0.0.59.

## 14. Verweise

- Phase A Spec: `docs/superpowers/specs/2026-08-05-population-ledger-design.md`
- Phase A Plan: `docs/superpowers/plans/2026-08-05-population-ledger.md`
- Phase C wird Phase B's DailyGrowth-Tick in StoryDirector mitbenutzen (siehe Spec-Folger).
- DECISIONS §24: Tier-Infection Scope (inoffiziell).
- Roadmap §8.2: World-Tier-Raid-Mechanik (out-of-scope für Phase C).
