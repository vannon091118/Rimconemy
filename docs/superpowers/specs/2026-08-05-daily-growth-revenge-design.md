# Spec — Daily-Growth-Tick + Revenge-Coupling (Phase B)

> **Stand:** 2026-08-05
> **Owner:** Infected & Automation (Package 05)
> **Phase:** B von 4 (Daily Growth Tick Integration)
> **Code-Anker (geplant):**
> - `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs` (Day-Tick-Order-Refactor)
> - `mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidSpawnService.cs` (Plan-Building mit Revenge)
> - `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryEventCatalog.cs` (Revenge-Family-Add)
> - `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs` (Revenge-Add als-family)
> - `Tests/RevengeQuotaFlowRegressionTests.cs`

## 1. Zweck / Warum diese Phase

User-Anforderung 2026-08-05: "Daily-Growth-Tick + ResetDailyCounters + Revenge-Quote-Aufruf an InfectedRaidSpawnService in StoryDirector einbinden".

**Bisheriger Stub-Status** (Phase A+B-Vorgänger):
- `PopulationLedger.ApplyDailyGrowthTick()` Committet, Profile-gated (`1.08/1.15/1.28`-Multiplier).
- `PopulationLedger.ResetDailyCounters()` Committet.
- `PopulationLedger.GetRevengeQuota(int maxCap)` Committet, returns `min(freeBudget, floor(RecentKillsToday × profileRatio))`.
- `InfectedRaidSpawnService.BuildPlanForTick(tick)` returns **Pressure-based** plan, NOT Revenge-aware.
- `InfectedRaidWorker.TryExecuteWorker` consumes Pressure-only.

**Was Phase B liefert:**
- StoryDirector Day-Tick-Block orchestriert: (1) WipeCheck → (2) StorySelector-Eval → (3) **ApplyDailyGrowthTick + ResetDailyCounters (NACH Eval)** → (4) Inoculation.
- StorySelector bekommt **Revenge-Family**: Wenn `PendingRevenge > 0`, kann Selector ein Revenge-Event feuern.
- `InfectedRaidSpawnService.BuildPlanForTick` integriert PendingRevenge: `plan.PawnCount = max(ComputeSpawnCount(pressure), GetPendingRevengeFromStoryDirector())`.
- `InfectedRaidWorker.TryExecuteWorker` nach Spawn: `storyDirector.DecrementPendingRevenge(actuallySpawned)`.

## 2. Design-Entscheidungen (User-Approval 2026-08-05 — bewusste Abweichung von Phase A spec)

| # | Frage | Entscheidung | Phase-A-spec-Konflikt |
|---|---|---|---|
| 1 | Tag-Tick-Order | **Growth+Reset NACH Eval** | ⚠ Phase A §3 sagte: "Growth vor Eval". User-Override: heute läuft Eval mit gestrigem Cap, morgen mit neuem. |
| 2 | Revenge-Pfad | **StorySelector als Familie** | ✅ kompatibel. Selector bekommt "Revenge"-Family. |
| 3 | Persistenz | **StoryDirector-State transient** | ⚠ Phase A hatte nur Ledger-Persistenz. User: einfacher Rebuild aus ledger.RecentKillsToday × ProfileId-Ratio. |
| 4 | AnimalCap auf Revenche | **Ratio-basiert 0.5 für Tiere-Inokulation, 1 für Revenge-Spawn** | ✅ kompatibel mit Phase A AnimalHalfCap. Revenge-Counter enteriert NICHT das AnimalHalfCap, nur Inoculation. |

**Konsequenz**: Phase-A-§3-Spec "Tag-Zyklus" wird obsolet. Diese Spec dokumentiert die korrekte Reihenfolge als Update der Daten-Theorie.

## 3. Korrigierte Day-Tick-Reihenfolge (StoryDirector.GameComponentTick)

```
StoryDirector.GameComponentTick (long currentTick)
    ├── WipeCheck (alle 250 Ticks): MaybeSignalGameOverForWipe(currentTick)
    │
    ├── Eval-Tick-Gate (60.000 Ticks): if (currentTick < LastEvaluationTick + EvaluationIntervalTicks) return;
    │                                  else LastEvaluationTick = currentTick;
    │
    ├── EVAL-BLOCK (StorySelector + Queue + IncidentWorker)
    │   ├── MinEventSpacing-Gate
    │   ├── BuildLiveSnapshot(currentTick, State)
    │   ├── EvaluateWithSnapshot(snapshot, currentTick)
    │   │     ├── StorySelector.SelectEvent(profile, snap, state, catalog, currentTick)
    │   │     ├── PendingIncidentDefName = "Rimconemy_InfectedRaidIncident"  [falls Event gefeuert]
    │   │     └── QueueSelectedIncident → IncidentWorker.TryExecuteWorker
    │   │
    │   └── RefreshRevengeQuota  ← NEU: jeden Day-Tick nach Eval
    │         ├── PendingRevengeBuildPlan(threshold=…)  wird NACH BuildLiveSnapshot berechnet
    │         └── storyDirector.LastPendingRevenge = GetRevengeQuoteForToday(currentTick)
    │
    ├── DAY-GROWTH+RESET-BLOCK ← NEU: nach Eval (User-Override Phase A spec)
    │   ├── ledger.ApplyDailyGrowthTick()           → Cap *= profile.multiplier
    │   ├── ledger.ResetDailyCounters()            → RecentKillsToday = 0
    │   └── ledger.PendingRevengeSpawns = GetRevengeQuotaForToday(currentTick)  ← NEU
    │
    └── INOCULATION-BLOCK (Phase C, unverändert)
        ├── RandomInoculationService.TryInfectRandom(playerHome, currentTick)
```

Wichtige Implikation: **Zu Beginn des Eval-Blocks** sind Cap + RecentKillsToday-Werte vom **gestrigen** Day-Tick. Das ist konsistent mit Rimworld's Day-Cycle-Semantik (heute-Pawns kämpfen, morgen Revenge-Quoten neu berechnet).

## 4. Datenfluss (Revenge in SpawnPlan)

```
[End of yesterday]
    StoryDirector.LastPendingRevenge = N (transient field)

[Today's Eval-Block]
    Snapshot.ThreatPressure (Today's wealth) — drives ComputeSpawnCount.

    StorySelector.SelectEvent(...)
         ├── candidate revenge-event if N > 0
         ├── press any raid-event with revenge-bonus
         └── result.SelectedEvent determines Raid-Worker's "revenge-mode" boolean.

[IncidentWorker.TryExecuteWorker]
    InfectedRaidWorker.TryExecuteWorker:
         ├── plan = InfectedRaidSpawnService.BuildPlanForTick(tick)
         │     ├── ComputeSpawnCount(pressure) = pressure-based 0..3
         │     └── final = max(ComputeSpawnCount, GetPendingRevengeFromStoryDirector())
         ├── toSpawn = min(final, MaxSpawnsPerWorkerRun)
         ├── SpawnHostileRavagers(toSpawn, parms)
         │     → actuallySpawned = 0..toSpawn (RimWorld Real-Spawn)
         └── storyDirector.DecrementPendingRevenge(actuallySpawned)
               → LastPendingRevenge = max(0, LastPendingRevenge - actuallySpawned)

[End of today's Eval]
    Day-Growth+Reset-Block:
        ledger.ApplyDailyGrowthTick() (cap grows by profile-multi)
        ledger.ResetDailyCounters() (recent-kills→0)
        ledger.PendingRevengeSpawns = StoryDirector.LastPendingRevenge (was decremented during eval)
```

## 5. Architektur — neue Komponenten

| Komponente | Typ | Was |
|---|---|---|
| `StoryDirector.LastPendingRevenge` | Field (transient) | Aktuelle Revenge-Slot-Höhe aus gestriger Berechnung. Rebuild on Save/Load. |
| `StoryDirector.GetPendingRevengeanceForToday()` | Method | Liest `LastPendingRevenge`, returnt oder 0 |
| `StoryDirector.SetPendingRevenge(int)` | Method | Von InfectedRaidWorker aufgerufen nach Spawn zum Dekrement |
| `StoryDirector.RecomputeRevengeAfterDayTick()` | Method (NACH Eval) | Liest `ledger.RecentKillsToday × profileMultiplier.GetRevengeRatio`, speichert als `LastPendingRevenge` |
| `InfectedRaidSpawnService.BuildPlanForTick(tick)` | Modified | Liest `StoryDirector.GetCurrent().GetPendingRevengeanceForToday()`, merged mit ComputeSpawnCount |
| `InfectedRaidWorker.TryExecuteWorker` | Modified | Nach Spawn: `StoryDirector.Get().SetPendingRevenge(LastPendingRevenge - actuallySpawned)` |
| `StoryEventCatalog` | Extended (Phase B) | Neue Family "Revenge" (1-2 EventSpecs mit `SpawnPressure:revenge`-Trigger) |
| `StorySelector.SelectEvent` | Modified | `AllowedEventFamilies` filter: 'Revenge' events eligible wenn `snapshot.ThreatPressure > 0.3` UND `storyDirector.LastPendingRevenge > 0` |

## 6. API / Interface

### `StoryDirector` (transient state)

```csharp
// Transient (not Scrib'd). Default 0. Refresh each Day-Tick after Eval.
public int LastPendingRevenge;   // int PendingRevenge-Stufe
public long LastRevengeRefreshTick;  // für Doppel-Refresh-Schutz im selben Tick

public int GetPendingRevengeanceForToday() => LastPendingRevenge;

public void DecrementPendingRevenge(int actuallySpawned)
{
    if (actuallySpawned <= 0) return;
    LastPendingRevenge = System.Math.Max(0, LastPendingRevenge - actuallySpawned);
}

/// Sync aus ledger.RecentKillsToday + ProfileId-ratio. Wird NACH Eval aufgerufen.
public void RecomputeRevengeAfterDayTick(PopulationLedger ledger, SettingProfile profile, long currentTick)
{
    if (currentTick == LastRevengeRefreshTick) return;
    LastRevengeRefreshTick = currentTick;
    if (ledger == null) return;
    float ratio = PopulationProfileMultipliers.GetRevengeRatio(profile?.ProfileId ?? profile.ProfileId);
    int minFreeBudget = (int)System.Math.Min(int.MaxValue, ledger.Cap - (long)ledger.HumanoidLiveCount);
    LastPendingRevenge = (int)System.Math.Floor((double)ledger.RecentKillsToday * ratio);
    LastPendingRevenge = System.Math.Max(0, System.Math.Min(LastPendingRevenge, minFreeBudget));
}
```

### `InfectedRaidSpawnService.BuildPlanForTick` (modified)

```csharp
public static SpawnPlan BuildPlanForTick(long tick)
{
    var plan = ... // existing scaffolding
    ...

    // Phase B: merge with revenge-pending.
    int pressurePlan = ComputeSpawnCount(pressure);
    int revengePending = Story.StoryDirector.Get()?.GetPendingRevengeanceForToday() ?? 0;

    plan.PawnCount = System.Math.Max(pressurePlan, revengePending);
    plan.ThreatPressureComponent = pressure;
    plan.RevengeQuotaComponent = revengePending;
    plan.MapId = canonical.uniqueID;
    plan.Reason = (revengePending > pressurePlan) ? "revenge-dominant"
                  : (pressurePlan > 0) ? "pressure-based"
                  : "ok";
    return plan;
}
```

### `InfectedRaidWorker.TryExecuteWorker` (consume revenge)

```csharp
// After SpawnHostileRavagers returns actuallySpawned:
int revengeConsumed = System.Math.Min(actuallySpawned, plan.RevengeQuotaComponent);
if (revengeConsumed > 0) {
    Story.StoryDirector.Get()?.DecrementPendingRevenge(revengeConsumed);
}
```

### `StoryEventCatalog` (1-2 Revenge-Events)

```csharp
new StoryEventSpec {
    EventId = "rimconemy.revenge.lesser",
    EventFamily = "Revenge",
    Label = "Lesser Revenge Swarm",
    Weights = new Dictionary<string,float> {
        { "Refuge", 0.0f }, { "Survival", 0.7f }, { "Collapse", 0.9f },
    },
    CooldownsDays = new Dictionary<string,long> {
        { "Refuge", 30 }, { "Survival", 14 }, { "Collapse", 7 },
    },
    Prerequisites = new List<StoryCondition> {
        new StoryCondition("RevengePending", ">=1"),
        ...
    }
}
```

## 7. Determinismus-Garantien

- `BuildPlanForTick(tick)` und `DecrementPendingRevenge(n)` sind deterministisch via gleiche `(tick, ledger-Reads, storyDirector-State)`.
- `RecomputeRevengeAfterDayTick` schreibt nur **transient** Fields → keine Scribe-Konflikte.
- `LastRevengeRefreshTick`-Gate verhindert Doppel-Refresh im selben Tick.

## 8. Edge Cases / Failure-Modes

| Edge Case | Verhalten |
|---|---|
| `RecomputeRevengeAfterDayTick(null)` | no-op + no Log |
| `RecomputeRevengeAfterDayTick` mit RecentKillsToday=0 | LastPendingRevenge bleibt =0 |
| Revanche > Cap-Raum | clip via min(freeBudget, ...) |
| `InfectedRaidWorker` Spawn fehlt (0 actuallySpawned) | kein Decrement, LastPendingRevenge bleibt für morgen |
| `LastPendingRevenge` kommt aus Save/Load | rebuild aus ledger.RecentKillsToday + ProfileId (1x nach Load) |
| Race Scenario: Spawn-Plan mehr als MaxSpawnsPerWorkerRun | hard-cap 3; Decrement = min(3, LastPendingRevenge) |

## 9. Tests (`Tests/RevengeQuotaFlowRegressionTests.cs`)

| # | Test | Asserts |
|---|---|---|
| B1 | RecomputeRevengeFromZeroKills | LastPendingRevenge == 0 |
| B2 | RecomputeSurvival10Kills5Ratio | LastPendingRevenge == floor(10 × 0.7) = 7 |
| B3 | DecrementBelowZeroClamped | Decrement(-3) auf 5 → 0, kein negative |
| B4 | BuildPlanMergesPressureAndRevenge | pressure<0.15 (plan=0), revenge=5 → plan=5 |
| B5 | BuildPlanUsesHigherOfTwo | pressure=0.4 (plan=2), revenge=5 → plan=5 |
| B6 | WorkerConsumesRevengeOnSpawn | After SpawnPlan+actuallySpawned, LastPendingRevenge -= |
| B7 | WorkerNotConsumeOnQualityFailure | SpawnPlan=5 actuallySpawned=2 → LastPendingRevenge -= 2 |
| B8 | DayCycleResetComputesPenquarters | Yesterday: 10 kills; Eval(tag 1); DayGrowth+Reset; New Penqaunt = floor(0×ratio)=0 |
| B9 | DayOrderGrowthAfterEval | Eval with old cap=10, then ApplyDailyGrowthTick → Cap=11 |
| B10 | RevengeFamilySelectorEligibleWhenNonZero | Profile=Survival, LastPendingRevenge>0 → Revenge-Family in AllowedEventFamilies ergibt event passed |
| B11 | RevengeFamilySelectorBypassedWhenZero | LastPendingRevenge=0 → Revenge-Family nicht wählbar |
| B12 | ProfileSurvivalBaseMatchesSpec | Survival ratio 0.7, Refuge 0.4, Collapse 0.9 (Spec §A Profile-Multiplier) |

## 10. StoryEventCatalog-Erweiterung

In Phase C sind 4 StoryEventFamilies aktiv: "Supply", "Social", "Raid", "Infection". Phase B fügt 1 hinzu: **"Revenge"**. Default 1 EventSpec pro RevengeFamily. Spec-Hinweis: Revenge wird nur dann eligible, wenn:
- `snapshot.ThreatPressure >= 0.0` (kein MinThreat-Gate für Revenge),
- UND `storyDirector.LastPendingRevenge > 0`.

Beide Bedingungen werden zu Prerequisites (siehe §6).

## 11. Bootstrap & Logging

`Bootstrap.cs`:
```csharp
Tests.RevengeQuotaFlowRegressionTests.RunAll();
Log.Message("[Rimconemy.InfectedAutomation] Phase B: DailyGrowth+Reset+Revenge coupling wired.");
```

`StoryDirector.GameComponentTick` Reihenfolge in Logging:
```
[StoryDirector: Day-Tick sequential. T=N] 
   Eval: <event-Reason or "no-event">
   DayGrowth: Cap=old C; newCap = old × m;
   ResetDailyCounters: RecentKillsToday -> 0;
   RecomputeRevenge: LastPendingRevenge = K × ratio, capped at freeBudget;
   Inoculation: <Inokulations-Service-Result or "noop">
```

## 12. Akzeptanz-Gate (Phase B SURVIVED)

- [ ] B1 — `RevengeQuotaFlowRegressionTests.RunAll()` = 12/12 PASS.
- [ ] B2 — `StoryDirector.LastPendingRevenge` aktualisiert korrekt nach DayGrowth+Reset.
- [ ] B3 — `InfectedRaidSpawnService.BuildPlanForTick` merges pressure + revenge.
- [ ] B4 — `InfectedRaidWorker.TryExecuteWorker` dekrementiert PendingRevenge nach Spawn.
- [ ] B5 — Reverse-Rebuild nach Save/Load (RecentKillsToday + ProfileId).
- [ ] B6 — `runtime_test.sh --skip-start --no-deploy` exit 0; Bump auf 0.0.60.
- [ ] B7 — Live-Beleg im Player.log (User-Pflicht): DayTick-Sequenz sichtbar.

## 13. Nicht-Ziele (Phase B)

- **Kein** neues IncidentDef (Revenge läuft über **existing** `Rimconemy_InfectedRaidIncident`).
- **Kein** neuer Worker (`InfectedRaidWorker` extended only).
- **Keine** Schema-Migration (Phase A `PendingRevengeSpawns` ist super-numerär; StoryDirector-PendingRevenge ist transient).
- **Kein** AnimalHalfCap auf Revenge (User-Override: volles 1-per-spawn für Revenge).

## 14. Verweise

- Phase A spec: `docs/superpowers/specs/2026-08-05-population-ledger-design.md` §3 Tag-Zyklus (überholt durch Phase B §3).
- Phase A plan: `docs/superpowers/plans/2026-08-05-population-ledger.md`.
- Phase C spec: `docs/superpowers/specs/2026-08-05-tier-inoculation-design.md` (Inoculation Tag-Block).
- INTERFACE_CONTRACT §2 (Capability-Tabelle): Population-Capability v1 reicht für Phase B.
