# Phase C — Tier-Inokulation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `RandomInoculationService` (Package 05) that converts a wild animal into an infected wildlife pawn on the player-home map, driven by a profile-aware deterministic selector, with a new lifestyle `InfectedPackBehavior` and `Rimconemy_InfectedWildlife` PawnKindDef.

**Architecture:** Two-layer service. A pure helper (`InoculationSelectorLogic` + `InoculationConverter`) doing the deterministic candidate selection / conversion description; a façade service (`RandomInoculationService`) that talks to live Map/pawn state. Service runs once per day-tick inside `StoryDirector.GameComponentTick`, gated by `PopulationLedger.IsInoculationCooldownElapsed()` (Phase A data layer). Animal-HalfCap means 1 animal head consumes 0.5 cap-units. Tier AI branches into `InfectedPackBehavior` (Rudel wandering + chase-folgen, kein Assault-State).

**Tech Stack:** C# netstandard2.1, RimWorld 1.6.4566 Assembly-CSharp, Foundation `MigrationRegistry` (no new capability; data layer already covered), Scribe_Deep for tests, Scribe-Less for runtime State (transient), `DeterministicRng` for selection, `map.mapPawns.AllPawnsSpawned` for sample.

## Global Constraints

- **Package:** all logic lives in `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/` and `Source/World/`. No DLL-References to other mods; Foundation read-only via existing capability `rimconemy.infectedautomation.population`.
- **Phase A reuse (mandatory):** `PopulationLedger.NoteInoculation`, `GetCumulativeInoculations()`, `IsInoculationCooldownElapsed()`, Profile-driven `InoculationsPerDay`/`InoculationMinIntervalTicks`-tables.
- **Determinism:** only `DeterministicRng.BuildSeed` for selection. NO `UnityEngine.Random`, `Verse.Rand`, `DateTime.Now`. Replay-safety per Save/Load.
- **No Harmony:** map → snapshot via `map.mapPawns.AllPawnsSpawned`, faction switch via `pawn.SetFaction(...)`, kindDef switch via re-assignment of `pawn.kindDef` where allowed (RimWorld 1.6 supports this for in-place conversions).
- **Logging:** `Log.Message` for happy paths, `Log.Warning` for expected edge cases, never `Log.Error` for user-data issues.
- **Tests:** static `RunAll()` per the project convention; first-line ends with `"X passed, Y failed."`. Assert via inline helper functions, NO external frameworks.
- **Scribe-friendly:** the Selector is non-persisted (transient); only the Ledger counters persist. No game-state corruption if a save doesn't include the in-flight inoculation.
- **Bump version:** `scripts/bump_version.sh 05` at the end.

## File Structure

**New Files (Package 05):**
- `Source/Inoculation/InoculationCandidate.cs` (struct snapshot + outcome)
- `Source/Inoculation/InoculationSelectorLogic.cs` (pure helper)
- `Source/Inoculation/InoculationConverter.cs` (pure helper)
- `Source/Inoculation/RandomInoculationService.cs` (service façade; talks to Map+Pawn)
- `Source/World/InfectedPackBehavior.cs` (Rudel-Wanderer pure helper)
- `Source/Population/AnimalHalfCapHook.cs` (extension methods on PopulationLedger for `GetTotalCapBudget()`)
- `Def/PawnKinds/Rimconemy_InfectedWildlife.xml`
- `Tests/InoculationRegressionTests.cs`
- `Tests/InfectedPackBehaviorRegressionTests.cs`

**Modified Files:**
- `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs` — add Day-Tick-call to `RandomInoculationService.TryInfectRandom`.
- `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` — register new RunAll().
- `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationLedger.cs` — add `GetTotalCapBudget()` method.
- `mods/05-Rimconemy-Infected-Automation/VERSION` (bump).

---

## Task 1: InoculationCandidate DTO

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/InoculationCandidate.cs`

**Interfaces:**
```csharp
public struct InoculationCandidate {
    public string ThingId;
    public string KindDefName;
    public string RaceDefName;
    public string OriginalFactionDef;
    public bool IsHumanlike;
    public bool IsAnimal;
    public bool IsDead;
    public IntVec3 MapCell;
}

public struct InoculationOutcome {
    public string ThingId;
    public string OriginalKindDefName;
    public string OriginalRaceDefName;
    public string ConvertedFactionDef;
    public string ConvertedKindDefName;
    public int EffectiveCapDelta;       // 1 head = 0.5; rounded up to int for ledger add
    public string Reason;               // "selected", "no-candidates", "cooldown", etc.
    public InoculationCandidate? Source;
}
```

- [ ] **Step 1.1**: Write file with structs.
- [ ] **Step 1.2**: Build → must compile clean.
- [ ] **Step 1.3**: Commit.

---

## Task 2: Selector-Logic (deterministic tier-chosen)

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/InoculationSelectorLogic.cs`

**Interfaces:**
```csharp
public static class InoculationSelectorLogic {
    public static int BuildInoculationSeed(
        string profileId, int mapId, long currentTick, int populationFingerprint);

    public static InoculationCandidate? SelectCandidate(
        IReadOnlyList<InoculationCandidate> candidates,
        int seed, long currentTick);

    public static void FilterCandidates(
        IReadOnlyList<InoculationCandidate> all,
        out IReadOnlyList<InoculationCandidate> filtered);
}
```

`BuildInoculationSeed` is FNV-1a (mirrors `DeterministicRng.GetStableHashCode`). `SelectCandidate` does `rng.NextInt(0, filtered.Count)` after sort-by-ThingId-stable-order, then `NextFloat()` for the binary trial (e.g. probability per Cohort).

**Failure-Mode:**
- Empty list → returns null.
- All filtered as already-infected/dead → returns null.

- [ ] **Step 2.1**: Write failing tests I1-I5.
- [ ] **Step 2.2**: Build → expected fail (helper not yet implemented).
- [ ] **Step 2.3**: Implement `InoculationSelectorLogic` (above).
- [ ] **Step 2.4**: Tests pass; build clean.
- [ ] **Step 2.5**: Commit with message `feat(05/inoculation): deterministic selector logic`.

---

## Task 3: Converter (Candidate → Outcome description)

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/InoculationConverter.cs`

**Interfaces:**
```csharp
public static class InoculationConverter {
    public const string BrandedKindDefName = "Rimconemy_InfectedWildlife";
    public const string InfectedFactionDefName = "Rimconemy_HiddenInfectedFaction";

    public static InoculationOutcome Convert(
        InoculationCandidate candidate,
        bool kindMappingTableHit,
        string reason);

    public static int ComputeAnimalHalfCapDelta(
        int previousAnimalCount, int newAnimalCount);
}
```

- [ ] **Step 3.1**: Write failing tests I6-I9.
- [ ] **Step 3.2**: Implement Converter.
- [ ] **Step 3.3**: Tests pass; commit.

---

## Task 4: Service-Façade (Map+Pawn side-effects)

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs`

**Interfaces:**
```csharp
public static class RandomInoculationService {
    public static InoculationOutcome? TryInfectRandom(Map map, long currentTick)
}
```

Implementation flow (per spec §4):
1. Defensive null check.
2. Profile-gate via `PopulationProfileMultipliers.GetInoculationsPerDay(profile) == 0` → null.
3. Cooldown-Gate via `ledger.IsInoculationCooldownElapsed()` → null.
4. Build candidate list: walk `map.mapPawns.AllPawnsSpawned`, capture `InoculationCandidate`-snapshot.
5. Seed build via `InoculationSelectorLogic.BuildInoculationSeed`.
6. `SelectCandidate` via helper.
7. If null → no-op + Log.Message.
8. If hit → live switch:
   - `pawn.SetFaction(infectedFaction)` (1.6 API exists).
   - `pawn.kindDef = brandedKindDef` (when kindMappingTableHit).
   - `PopulationLedger.Get().NoteInoculation(originalKindDefName)`.

- [ ] **Step 4.1**: Write failing tests I11-I16 (with mock-Map via Injector pattern).
- [ ] **Step 4.2**: Implement Service.
- [ ] **Step 4.3**: Build clean. Tests pass.
- [ ] **Step 4.4**: Commit `feat(05/inoculation): service façade + map integration`.

---

## Task 5: PawnKindDef `Rimconemy_InfectedWildlife`

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Def/PawnKinds/Rimconemy_InfectedWildlife.xml`

**Required:** PawnKindDef with `defaultFactionDef=Rimconemy_HiddenInfectedFaction`, `humanlikeFaction=false`, `combatPower=30`, `initialResistanceRange 0~0`, `initialWillRange 0.4~0.6`. The `<race>` element is overwritten at runtime per chosen animal (RimWorld 1.6 supports this via `pawn.kindDef = ...`).

- [ ] **Step 5.1**: Write XML def. Use the existing `Rimconemy_InfectedRavager.xml` as template anchor; rename defName and faction.
- [ ] **Step 5.2**: Trigger `./scripts/dev_quick_test.sh` to validate Def-loading.
- [ ] **Step 5.3**: Commit `feat(05/defs): Rimconemy_InfectedWildlife PawnKind for hybrid tier conversion`.

---

## Task 6: Animal-HalfCap-Counter (`GetTotalCapBudget`)

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationLedger.cs` — add `GetTotalCapBudget()`.

**Interfaces:**
```csharp
public int GetTotalCapBudget()
{
    return System.Math.Max(0, Cap - (HumanoidLiveCount + (int)System.Math.Floor((double)AnimalLiveCount / 2)));
}
```

- [ ] **Step 6.1**: Write failing tests I10.
- [ ] **Step 6.2**: Implement.
- [ ] **Step 6.3**: Tests pass; commit `feat(05/population): AnimalHalfCap GetTotalCapBudget formula`.

---

## Task 7: PackBehavior `InfectedPackBehavior.cs`

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/World/InfectedPackBehavior.cs`

**Interfaces:**
```csharp
public enum InfectedPackState { Wandering, Tracking, Dissipating }

public static class InfectedPackBehavior {
    public const float WanderMinStep = 15f;
    public const float WanderMaxStep = 25f;

    /// <summary>Pure: returns the new state + a target cell for Wandering step.
    /// Tracking/Investigating replaced by Assault — animals never go Assault.</summary>
    public static InfectedPackState ComputeNext(
        InfectedPackState current,
        bool colonistVisible,
        long ticksSinceLastSight,
        long randomSeed);
}
```

- [ ] **Step 7.1**: Write failing tests P1-P5 (pure state transitions).
- [ ] **Step 7.2**: Implement.
- [ ] **Step 7.3**: Tests pass; commit `feat(05/world): InfectedPackBehavior (Rudel wandering, no Assault)`.

---

## Task 8: StoryDirector Day-Tick Hook

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs`

**Interfaces:** Insert in `GameComponentTick` after `EvaluateWithSnapshot` runs (or alongside):

```csharp
// Phase C — Tier-Inokulation Hook (after DailyGrowth + ResetDailyCounters)
if (currentTick >= LastDayTick + EvaluationIntervalTicks)
{
    var playerMap = MapRegistry.GetPrimaryPlayerHomeMap();
    if (playerMap != null) {
        Rimconemy.InfectedAutomation.Inoculation.RandomInoculationService
            .TryInfectRandom(playerMap, currentTick);
    }
}
```

- [ ] **Step 8.1**: Modify StoryDirector.
- [ ] **Step 8.2**: Build → must compile clean.
- [ ] **Step 8.3**: Commit `feat(05/story): Day-Tick hook for RandomInoculationService`.

---

## Task 9: Tests integration + Falsification Update

**Files:**
- Create/Update: `mods/05-Rimconemy-Infected-Automation/Tests/InoculationRegressionTests.cs` (I1-I16).
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` (add `Tests.InoculationRegressionTests.RunAll();`).

- [ ] **Step 9.1**: Bootstrap-Registration hinzufügen.
- [ ] **Step 9.2**: Falsification `docs/falsification/infected__ManualRaid.md` Block B reference Tier-Inoculation als "COMPILED" (Phase-C-Spec §Lückenfüller).
- [ ] **Step 9.3**: Commit + Bump `0.0.58 → 0.0.59`.

---

## Task 10: Final verification

**Files:** none (operational).

- [ ] **Step 10.1**: Run `./scripts/runtime_test.sh --skip-start --no-deploy`. Expect PASS.
- [ ] **Step 10.2**: Run `dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj` with RimWorld Managed Path. Expect 0/0.
- [ ] **Step 10.3**: Final commit `chore(05): bump 0.0.59 final Phase C Tier-Inokulation`.

---

## Self-Review

### Spec coverage

| Spec § | Implementation |
|---|---|
| §3 Architektur (alle Komponenten) | Tasks 1-7 ✅ |
| §4 Datenfluss (Daily Cycle) | Task 8 ✅ |
| §5 Determinismus | Task 2 mit `BuildInoculationSeed` |
| §6 Hybrid-PawnKind | Task 5 |
| §7 Animal-HalfCap | Task 6 |
| §8 PackBehavior | Task 7 |
| §9 Tests I1-I16 | Task 9 |
| §10 Bootstrap | Task 9 |
| §13 Akzeptanz-Gate C1-C7 | Tasks 9+10 |

### Placeholder scan

No "TBD"/"TODO" markers found in steps. Code blocks contain functional descriptions, not skeletal stubs.

### Type consistency

- `InoculationCandidate.ThingId` — `string` (matches `pawn.ThingID` semantics).
- `InoculationOutcome.ConvertedKindDefName` — `string` constant `"Rimconemy_InfectedWildlife"`.
- `PopulationLedger.GetTotalCapBudget()` — returns `int` floored, never negative.
- `InfectedPackBehavior.ComputeNext(...)` — pure, no Map-IO.

Plan complete and saved to `docs/superpowers/plans/2026-08-05-tier-inoculation.md`.
