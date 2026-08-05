# Spec — Infected-Population Ledger (Phase A)

> **Stand:** 2026-08-05
> **Owner:** Infected & Automation (Package 05)
> **Phase:** A von 4 (siehe 4-Phasen-Decomp)
> **Code-Anker (geplant):**
> - `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationLedger.cs`
> - `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationLedgerTickable.cs` (Harmony-loses Reconciliation-Wrapper)
> - `mods/05-Rimconemy-Infected-Automation/Tests/PopulationLedgerRegressionTests.cs`
> - `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` (RunAll-Hook)
> - `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs` (Capability-Registrierung)

## 1. Zweck / Warum diese Phase zuerst

Die Mechanik "5–10 Infizierte passiv am Tag, ramp-up über Tage, Horde ab 150, Kill-Revanche, **seltene Tier-Inokulation via Random-Encounter**" braucht **eine einzige Source of Truth** für:

| Datenpunkt | Wofür |
|---|---|
| `HumanoidLiveCount` / `AnimalLiveCount` | Reconciliation-Anker beider Pfade + Trigger für Horde-Threshold (Phase D) |
| `Cap` | Tageswachstum-Multiplikator (Phase B). Deckt beide Count-Typen |
| `CumulativeKills` | Storyteller-Anzeige (Phase A). Mensch- UND Tier-Kills |
| `RecentKillsToday` | Revenge-Quote für nächste Nacht (Phase B) |
| `DayIndexSinceStart` | Save/Load-Endemie-Test |
| `LastInoculationTick` | Idempotenz für Tier-Inokulation (Phase C Mechanik, Phase A Daten) |
| `CumulativeInoculations` | Lifetime-Statistik für UI + Balance-Tuning |
| `ProfileId` | Profile-bewusste Multiplikator-Tabelle (auch für Inoculation-Chance) |

**Tier-Inokulation-Scope:** Ein zufälliges Wildtier der Home-Map wird in einen feindlichen Infected verwandelt (Human-Faction, aber Tier-Race). Spawn nur via `RandomInoculationService` (Phase C). Sterben oder fliehen ist Kill-Pfad. Mensch-Raids und Tier-Inokulation sind **unabhängige Spawn-Kanäle** und konkurrieren um dasselbe `Cap`-Budget.

Ohne diese SSOT verstreuen sich die Zahlen über `StoryDirector.LastSnapshot`, `InfectedRaidSpawnService`, `WorldRaidCoordinator`, ein zukünftiges `RandomInoculationService` — vier Schnitte auf denselben Druck bereits jetzt (siehe Audit-Finding 6, 2026-08-04).

## 2. Architektur

### Komponenten

| Komponente | Typ | Verantwortung |
|---|---|---|
| `PopulationLedger` | `GameComponent` + `ISchemaMigratable` | SSOT-Speicher + Lese-API |
| `PopulationLedgerTickable` | `MapComponent` | Reconciled `LiveCount` Map-lokal; ruft Ledger-Update |
| `PopulationMigration` | Foundation `MigrationStepWalker` | Schema-1-Step (Stub, dokumentiert) |
| `Capability("rimconemy.infectedautomation.population", 1)` | Foundation `Capability` | Cross-Package-Lese-Bridge |

### Datenstruktur (`PopulationLedger`)

```csharp
public sealed class PopulationLedger : GameComponent, ISchemaMigratable
{
    public const int CurrentSchemaVersion = 1;
    public string ClassId => "rimconemy.infectedautomation.population";

    // Persisted — Humanoid-Layer
    public int HumanoidLiveCount;
    public int CumulativeKills;          // Mensch- UND Tier-Kills zusammengezählt
    public int RecentKillsToday;
    public int Cap;                      // gemeinsames Cap für beide Count-Typen
    public int DayIndexSinceStart;
    public long LastDayTick;
    public string ProfileId;             // "Refuge" | "Survival" | "Collapse"

    // Persisted — Animal-Layer (Phase C Mechanik, Phase A Daten-SSOT)
    public int AnimalLiveCount;
    public int CumulativeInoculations;   // Lifetime Tier-Inokulationen
    public long LastInoculationTick;     // 0L = noch nie; >= 60.000 = einmaliger Cooldown garantiert

    public override void ExposeData();   // Scribe_Fields
    public int SchemaVersion => CurrentSchemaVersion;
    public void MigrateIfNeeded();
}
```

### Lese-API

```csharp
int GetHumanoidLiveCount();   // Menschen-Infizierte live (nach Reconciliation)
int GetAnimalLiveCount();     // Tier-Infizierte live (nach Reconciliation)
int GetTotalLiveCount();      // = Humanoid + Animal (für Horde-Threshold-Trigger)
int GetCap();                 // gemeinsames Wachstums-Cap
int GetCumulativeKills();     // Lifetime-Kills (Mensch + Tier)
int GetRecentKillsToday();    // Kills seit letztem Day-Tick
float GetRevengeQuota(int maxCap);  // = recentKills × profileModifier.Revenge, gedeckelt durch maxCap UND Free-Budget
string GetProfileId();
long GetLastInoculationTick();  // 0L = noch nie
int GetCumulativeInoculations();
```

### Schreib-API

```csharp
void RegisterKill(Pawn pawn);              // addiert 1 Kill; akzeptiert Race=Human ODER Animal
void ResetDailyCounters();                 // soft-reset am Day-Tick (RecentKillsToday = 0)
void ApplyDailyGrowthTick();               // Cap *= dailyGrowthMultiplier × profileModifier
void ReconcileLiveCountOnMap(Map map);     // Map-lokaler Reconciler (Mensch + Tier)
void NoteInoculation(string animalKindDefName);  // Phase C RandomInoculationService ruft auf
```

### Reconciliation-Strategie (kein Harmony)

`MapComponent.MapComponentTick` alle 60 Ticks. Pro Map:

**Humanoid-Track:**
1. Iteriere über `map.mapPawns.AllPawnsSpawned`, filter auf `pawn.Faction?.def?.defName == "Rimconemy_HiddenInfectedFaction"` UND `pawn.RaceProps.Humanlike == true` UND `!pawn.Dead`.
2. Zähle → Map-Reconciler-Result `humanoidOnMap`.

**Animal-Track:**
1. Iteriere über `map.mapPawns.AllPawnsSpawned`, filter auf `pawn.Faction?.def?.defName == "Rimconemy_HiddenInfectedFaction"` UND `pawn.RaceProps.Humanlike == false` (Animal) UND `pawn.kindDef?.defName` ∈ `RandomInoculationDefinition.AllowedAnimalKinds` UND `!pawn.Dead`.
2. Zähle → Map-Reconciler-Result `animalOnMap`.

**Aggregation:** `ledger.AdjustHumanoidLiveCount(delta)` + `ledger.AdjustAnimalLiveCount(delta)` → Idempotenz via Scribe.

**Wichtig:** read-side Reconciliation, kein Harmony-Hook auf `Pawn.Kill`. SAVE-Konsistenz gewährleistet `ISchemaMigratable` + `Scribe_Fields`.

### Animal-Inokulation-Datenflow (Phase C Mechanik, hier als Hook dokumentiert)

`RandomInoculationService.TryInfectRandomAnimal(Map map, SettingProfile profile)` wird in Phase C implementiert. Der Ledger liefert in Phase A die Daten-Slots:

1. **Trigger-Bedingung** (Phase C): `TicksGame - LastInoculationTick >= MinInterval(profile)` → würfelt mit `Profile.InoculationPerDay(profile)`.
2. **Tier-Auswahl** (Phase C): wählt ein zufälliges Wildtier aus `map.mapPawns.SpawnedPawns.Where(p => p.RaceProps.Animal && !p.Faction.IsPlayer)` (deterministisch via `DeterministicRng`).
3. **Conversion** (Phase C): wechselt `pawn.Faction` zu `Rimconemy_HiddenInfectedFaction` + brandet `pawn.kindDef` zu `Rimconemy_Infected_<Animal>` falls definiert, sonst bleibt Original-KindDef.
4. **Ledger-Update:** ruft `PopulationLedger.Get().NoteInoculation(animalKindDefName)` → erhöht `CumulativeInoculations`, stempelt `LastInoculationTick = currentTick`.

Profile-Multiplier für Inoculation:
```csharp
public static class PopulationProfileMultipliers
{
    public static readonly IReadOnlyDictionary<string, int> InoculationsPerDay = new Dictionary<string, int>
    {
        { "Refuge",   0 },     // aus
        { "Survival", 1 },     // ~1 pro Woche (deterministisch via Day-Index)
        { "Collapse", 3 },     // häufiger
    };

    public static readonly IReadOnlyDictionary<string, long> InoculationMinIntervalTicks = new Dictionary<string, long>
    {
        { "Refuge",   long.MaxValue / 2 },  // nie
        { "Survival", 60_000L * 7 },         // mindestens 7 Tage Abstand
        { "Collapse", 60_000L * 3 },         // mindestens 3 Tage Abstand
    };
}
```

Dieser Teil **erweitert** die Profile-Multiplier-Tabelle in §2.

### Profile-Multiplier-Tabelle

```csharp
public static class PopulationProfileMultipliers
{
    public static readonly IReadOnlyDictionary<string, float> DailyGrowth = new Dictionary<string, float>
    {
        { "Refuge",   1.08f },  // 1.15 × 0.94
        { "Survival", 1.15f },  // baseline
        { "Collapse", 1.28f },  // 1.15 × 1.11
    };

    public static readonly IReadOnlyDictionary<string, float> RevengeRatio = new Dictionary<string, float>
    {
        { "Refuge",   0.4f },   // weniger Revenge (Spieler werden belohnt)
        { "Survival", 0.7f },   // baseline (User-Spec)
        { "Collapse", 0.9f },   // fast 1:1
    };

    public static readonly IReadOnlyDictionary<string, int> HordeThreshold = new Dictionary<string, int>
    {
        { "Refuge",   220 },    // später
        { "Survival", 150 },    // User-Default
        { "Collapse", 80  },    // früher
    };

    public static readonly IReadOnlyDictionary<string, int> InoculationsPerDay = new Dictionary<string, int>
    {
        { "Refuge",   0 },      // Tier-Inokulation aus
        { "Survival", 1 },      // ~1 pro Woche (deterministisch via Day-Index)
        { "Collapse", 3 },      // häufiger
    };

    public static readonly IReadOnlyDictionary<string, long> InoculationMinIntervalTicks = new Dictionary<string, long>
    {
        { "Refuge",   long.MaxValue / 2 },  // nie
        { "Survival", 60_000L * 7 },         // mindestens 7 Tage Abstand
        { "Collapse", 60_000L * 3 },         // mindestens 3 Tage Abstand
    };
}
```

Diese Tabelle ist **deterministisch** — keine Randomisierung, keine Time-abhängige Variation. Die Inokulations-Raten sind Maximalwerte pro Spieltag, die eigentliche Auswahl des Tier-Inokulationszeitpunkts liegt in Phase C bei `RandomInoculationService`.

## 3. Datenfluss

### Tageszyklus (60.000 Ticks = 1 Spieltag)

```
MapComponent.MapComponentTick
    → alle 60 Ticks: Reconciliation der Live-Counts auf der Map
    → Ledger.AdjustLiveCount(delta)

PopulationLedger (GameComponent).GameComponentTick
    → am Day-Tick (TicksGame - LastDayTick >= 60000):
        1. ApplyDailyGrowthTick:  Cap = floor(Cap * profileMultiplier.DailyGrowth)
        2. BuildRevengeQuota:     plannedNightSpawn = min(Cap, RecentKills × 0.7)
        3. ResetDailyCounters:    RecentKillsToday = 0
        4. LastDayTick = currentTick
        5. DayIndexSinceStart += 1
```

### Kill-Pfad (ohne Harmony)

```
InfectedRaidWorker.TryExecuteWorker OR NightInfectedWorker.TryExecuteWorker OR CombatResolve
    → wenn pawn.KindDef defName == "Rimconemy_InfectedRavager":
        PopulationLedger.Get().RegisterKill(pawn)
```

`RegisterKill` muss idempotent pro `pawn.ThingID` sein (siehe T3-Test).

### Death-Pfad (Pawn stirbt → LiveCount muss nach unten)

Pawn.Death wird via `IS/PostLoadInit` Reconciliation erkannt; Reconciliation ist **tick-basiert**, NICHT Hook-basiert. Siehe Strategie oben.

## 4. Save / Migration

### `Scribe_Fields` Pattern

```csharp
public override void ExposeData()
{
    base.ExposeData();
    // Humanoid-Layer
    Scribe_Values.Look(ref HumanoidLiveCount, "rimconemyILedgerHumanoidLiveCount", 0);
    Scribe_Values.Look(ref CumulativeKills, "rimconemyILedgerKills", 0);
    Scribe_Values.Look(ref RecentKillsToday, "rimconemyILedgerKillsToday", 0);
    Scribe_Values.Look(ref Cap, "rimconemyILedgerCap", 5);
    Scribe_Values.Look(ref DayIndexSinceStart, "rimconemyILedgerDayIndex", 0);
    Scribe_Values.Look(ref LastDayTick, "rimconemyILedgerLastDayTick", 0L);
    Scribe_Values.Look(ref ProfileId, "rimconemyILedgerProfileId", "Survival");
    // Animal-Layer
    Scribe_Values.Look(ref AnimalLiveCount, "rimconemyILedgerAnimalLiveCount", 0);
    Scribe_Values.Look(ref CumulativeInoculations, "rimconemyILedgerInocCount", 0);
    Scribe_Values.Look(ref LastInoculationTick, "rimconemyILedgerLastInocTick", 0L);
}
```

### Migration

`MigrateIfNeeded()` ist ein No-Op für Schema=1 — aber ruft `MigrationRegistry.InvokeMigration(this)` auf, damit spätere Schema-Änderungen protokolliert werden.

```csharp
public void MigrateIfNeeded()
{
    int savedVersion = SchemaVersion;  // from Scribe
    if (savedVersion < CurrentSchemaVersion)
    {
        // Phase A hat keine Migration Steps. Hook-Punkt reserviert.
        Log.Message($"[Rimconemy.InfectedAutomation] PopulationLedger: schema {savedVersion} → {CurrentSchemaVersion} (no-op for Phase A).");
    }
}
```

### Cross-Package READ

`Capability Audit` liest via `CapabilityAudit.HasCapabilityOrWarn(packageId="rimconemy.infectedautomation", capabilityId="rimconemy.infectedautomation.population", minVersion=1, readerContext="PopulationLedger.Read")`. Package-02 (Survival-Progression) könnte in Phase 5+ davon lesen → outlayer Tests dokumentieren das.

## 5. Tests

`PopulationLedgerRegressionTests.RunAll()` ist statisch, keine RimWorld-Mocks nötig, läuft im Bootstrap des Package 05.

| # | Test | Was |
|---|---|---|
| T1 | SchemaBumpV0ToV1 | Ein `new PopulationLedger().SchemaVersion = 0` → nach `MigrateIfNeeded()` = 1 |
| T2 | ScribeRoundTrip | Bump-V0 → Save-Load → HumanoidLiveCount/Cap/Kills/ProfileId/AnimalLiveCount identisch |
| T3 | RegisterKillIdempotency | `RegisterKill(samePawn)` zweimal → CumulativeKills += 1 (nicht 2) |
| T4 | RegisterKillUnknownPawn | null-Pawn → no-op + Warning-Log |
| T5 | RegisterKillAcceptsAnimal | `pawn.RaceProps.Animal == true` Kill wird akzeptiert (CumulativeKills += 1) |
| T6 | ApplyDailyGrowthTick_ProfileModifier | Cap=10, Profile=Survival → Cap = 11 oder 12 (math.round) |
| T7 | ApplyDailyGrowthTick_ProfileVariance | Gleicher Start-Cap über 30 Tage für alle 3 Profile → Cap[Refuge] < Cap[Survival] < Cap[Collapse] |
| T8 | RevengeQuotaScalesWithCap | Profile=Survival, RecentKillsToday=10, Cap=10 → RevengeQuota(10) = 7 |
| T9 | RevengeQuotaClippedByCap | RecentKills=20, Cap=5 → RevengeQuota(5) = 4 (gedeckelt durch Cap=5, Kills×0.7 = 14) |
| T10 | ResetDailyCounters | Nach `ResetDailyCounters()`: RecentKillsToday = 0, andere unberührt |
| T11 | ReconciliationAdjustsDualCounters | Pre: Humanoid=8, Animal=4. Map hat 7 leben Infizierte Humanoid + 3 lebende Tier-Infizierte. Post: Humanoid=7, Animal=3 |
| T12 | ReconciliationAnimalSeparation | Map hat 10 lebende Humans (keine Infizierte) + 5 lebende Infizierte Animale → Ledger: Humanoid=0, Animal=5 |
| T13 | NoteInoculationStampsTick | `NoteInoculation("Wolf")`: CumulativeInoculations += 1, LastInoculationTick = currentTick |
| T14 | NoteInoculationCooldownEligible | LastInoculationTick=60000, currentTick=60000+InoculationMinIntervalTicks[Profile] → eligible. Mit currentTick darunter → kein zweiter Trigger (Test-seitig dokumentiert) |
| T15 | GetTotalLiveCountDualSum | Humanoid=10, Animal=4 → GetTotalLiveCount()=14 |
| T16 | AnimalDeathDoesNotReduceHumanoidCount | Reconciliation mit nur Animal-Death → nur AnimalLiveCount verringert, Humanoid unberührt |

## 6. Bootstrap-Integration

In `Source/Bootstrap.cs`:

```csharp
Tests.PopulationLedgerRegressionTests.RunAll();
Log.Message("[Rimconemy.InfectedAutomation] Population-Ledger bereit (Schema=1, ProfileId abhängig von StoryDirector).");
```

Failure-Modus: Test ruft `Log.Warning` + return false, wirft nicht. (Konsistent mit anderen Regressions-Suites.)

## 7. Fehlerbehandlung / Edge Cases

| Edge Case | Verhalten |
|---|---|
| `RegisterKill(null)` | no-op + Warning-Log |
| `RegisterKill(Race != Human)` | no-op (zukunftssicher für Tier-2-Infizierte) |
| `ReconcileLiveCountOnMap(null)` | no-op |
| `Map.Disposed` mid-reconciliation | try/catch → Log.Warning, weiter zur nächsten Map |
| Cap-Overflow (> int.MaxValue / 1000) | `Math.Min(int.MaxValue/1000, ...)` |
| ProfileId unbekannt | Default "Survival" + Warning |
| Reconciliation während Save | Scribe mode-check: skip if `Scribe.mode != Inactive` |
| Mutliple GameComponents in Tests | Statischer Reset-Hook `PopulationLedger.ResetForTests()` |

## 8. Nicht-Ziele (Phase A)

- **Kein** `InfectedPawnAIExtension` (Phase C).
- **Kein** `NightSpawnFormula` / `IncidentWorker_NightInfected` (Phase B/C Nacht-Schicht).
- **Kein** Horde-Overlay (Phase D).
- **Kein** StoryDirector-Snapshot-Update mit `SpawnPressure` (Phase A liefert nur die Daten-API; Phase-C/D füllt das Feld).
- **Kein** Harmony-Hook.
- **Keine** Implementation von `RandomInoculationService` (Phase C Mechanik). Phase A liefert nur die Daten-Slots.
- **Keine** PawnKindDefs für `Rimconemy_Infected_<Animal>` (Phase C/B Defs).

Phase A ist **rein** Daten + Tests + Save/Migration. Keine Verhaltensänderung am Spiel, kein Spawn, kein AI, keine Tier-Conversion. Nur Daten-Anker.

## 9. Konventionen & Kompatibilität

| Konvention | Befolgt |
|---|---|
| Package-Isolation (kein DLL-Ref auf anderen Mods) | ✅ |
| Capability via Foundation Registry | ✅ |
| `ISchemaMigratable` via Foundation | ✅ |
| `Scribe_Fields` mit `rimconemy`-Prefix | ✅ |
| Tests via `RunAll()` im Bootstrap | ✅ |
| Pure Determinismus (kein system time, keine Random) | ✅ + Reconciliation-Tick ist deterministisch über `Scribe.mode`-Check |
| Bump-Version via `scripts/bump_version.sh 05` | (Post-Implementation) |
| Logging-Konvention `Log.Message/Log.Warning` (Info/Warn, keine Errors bei erwarteten Edge-Cases) | ✅ |
| Steam-/Patch-Kompatibilität: keine Änderung an Vanilla-Dateien | ✅ |

## 10. Akzeptanz-Gate (Phase A SURVIVED)

- [ ] A1 — `PopulationLedgerRegressionTests.RunAll()` = 16/16 PASS, im Bootstrap-Log sichtbar.
- [ ] A2 — `git diff` in `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs` zeigt neue Capability-Zeile `rimconemy.infectedautomation.population` v1.
- [ ] A3 — `./scripts/deploy.sh 05` baut ohne Fehler.
- [ ] A4 — `./scripts/runtime_test.sh --skip-start --no-deploy` PASS (Statik-Check).
- [ ] A5 — Phase-B-Implementierung kann Phase-A-Ledger konsumieren (Build-Link via `using Rimconemy.InfectedAutomation.Population;`).
- [ ] A6 — Save-Load-Roundtrip (manuell oder via Test) erhält HumanoidLiveCount/AnimalLiveCount/Cap-Invarianten über Reseed.
- [ ] A7 — Profile-Multiplier-Tabellen decken alle drei Profile ab (Refuge/Survival/Collapse), auch für Tier-Inokulations-Cooldown.

## 11. Verweise

- `docs/P6-PROGRESS.md Task 12` (Infizierten-Raid-Spawn ← Ledger liefert Daten)
- `docs/falsification/infected__InfectedRaid.md`
- `docs/falsification/earlygame__FirstNight.md`
- `docs/INTERFACE_CONTRACT.md §2` (Capability-Tabelle)
- `docs/SAVE_CONTRACT.md §ISchemaMigratable`
