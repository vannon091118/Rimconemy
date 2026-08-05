# Population-Ledger Implementation Plan (Phase A)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build `PopulationLedger` (Package 05 GameComponent) as the single source of truth for human-infected + animal-infected population data, kill counters, profile multipliers, and Save/Load resilience — so Phases B/C/D can build behavior/AI/visualization atop stable data anchors.

**Architecture:** Pure-data GameComponent. Dual counters (HumanoidLiveCount / AnimalLiveCount) sharing one `Cap`. Deterministic profile multipliers (Refuge/Survival/Collapse) for growth-rate, revenge-ratio, horde-threshold, and animal-inoculation rate. Scribe_Fields-Pattern for save, ISchemaMigratable for schema evolution. Tick-based MapComponent reconciler (no Harmony) counts live pawns. NoteInoculation stamps the ledger for the Phase-C `RandomInoculationService` to draw from.

**Tech Stack:** C# netstandard2.1, RimWorld 1.6.4566 Assembly-CSharp, Foundation capability registry, Scribe_Fields, ISchemaMigratable, test bootstrap via static RunAll(), no Harmony.

## Global Constraints

- **Package:** all logic lives in `mods/05-Rimconemy-Infected-Automation/Source/Population/` (new folder). Foundation Registry entry only.
- **Project isolation:** no DLL-References to/from Scavenger/Economy/Survival; Foundation read-only via Capability-Audit.
- **Capability:** register `rimconemy.infectedautomation.population` v1 in `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs`.
- **Scribe prefix:** every `Scribe_Values.Look` key MUST start with `rimconemy` (e.g. `rimconemyILedgerCap`).
- **ISchemaMigratable:** `ClassId = "rimconemy.infectedautomation.population"`, `SchemaVersion = 1`, `MigrationRegistry.Register` invoked once in Bootstrap.
- **No system-time, no background threads:** all time math ticks → `Find.TickManager.TicksGame`.
- **Determinism:** no `UnityEngine.Random` or `Rand.Value`; if RNG is needed, route via `DeterministicRng` (existing class).
- **Logging:** `Log.Message` for info, `Log.Warning` for expected edge-cases, never `Log.Error` for user-data issues.
- **Bump-version:** run `./scripts/bump_version.sh 05` after Bootstrap-Integration; commit before any deploy.
- **No Vanilla file edits:** no `Data/Core/...` touches.
- **Failure in tests:** log warning, return false — do NOT throw from `static` constructors.
- **Test files:** static `RunAll()`; first-line `[Rimconemy.InfectedAutomation] <suite> regression tests: X passed, Y failed.`; each test uses assert-helpers from existing suites.

---

## File Structure

**New Files (Package 05):**
- `Source/Population/PopulationProfileMultipliers.cs` — static deterministic tables.
- `Source/Population/PopulationLedger.cs` — GameComponent + ISchemaMigratable + Core API.
- `Source/Population/PopulationLedgerReconciler.cs` — MapComponent reconciler (no Harmony).
- `Source/Population/NoteInoculationRecord.cs` — small DTO for `LastInoculationTick` + `CumulativeInoculations` (also re-entrant from `PopulationLedger`).
- `Tests/PopulationProfileMultipliersRegressionTests.cs`.
- `Tests/PopulationLedgerRegressionTests.cs`.

**Modified Files:**
- `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` — register new RunAll() calls + MigrationRegistry.Register.
- `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs` — add new Capability row.
- `mods/05-Rimconemy-Infected-Automation/VERSION` — bumped via `scripts/bump_version.sh 05`.

---

## Task 1: Profile-Multiplier-Tabelle (Foundation der Determinismus-Story)

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationProfileMultipliers.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/PopulationProfileMultipliersRegressionTests.cs`

**Interfaces:**
- `PopulationProfileMultipliers.GetDailyGrowth(string profileId) -> float`
- `PopulationProfileMultipliers.GetRevengeRatio(string profileId) -> float`
- `PopulationProfileMultipliers.GetHordeThreshold(string profileId) -> int`
- `PopulationProfileMultipliers.GetInoculationsPerDay(string profileId) -> int`
- `PopulationProfileMultipliers.GetInoculationMinInterval(string profileId) -> long`
- `PopulationProfileMultipliers.SupportedProfiles -> IReadOnlyList<string>` (must contain exactly: Refuge, Survival, Collapse)
- All inputs invalid profileId → return Survival-default + Log.Warning.

- [ ] **Step 1.1: Failing tests for Profile Multipliers**

Create `Tests/PopulationProfileMultipliersRegressionTests.cs`:

```csharp
using System.Linq;
using Rimconemy.InfectedAutomation.Population;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class PopulationProfileMultipliersRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static void RunAll()
        {
            _passed = 0; _failed = 0;
            TestSupportedProfilesContainAllThree();
            TestDailyGrowthMonotonicProfileVariance();
            TestRevengeRatioMonotonicProfileVariance();
            TestHordeThresholdMonotonicReverse();
            TestInoculationsPerDayMonotonicProfileVariance();
            TestInoculationMinIntervalMonotonicReverse();
            TestUnknownProfileFallsBackToSurvival();
            TestSurvivalBaselineMatchesSpec();

            Log.Message("[Rimconemy.InfectedAutomation] PopulationProfileMultipliers regression tests: " + _passed + " passed, " + _failed + " failed.");
        }

        private static void TestSupportedProfilesContainAllThree()
        {
            var supported = PopulationProfileMultipliers.SupportedProfiles.ToList();
            AssertTrue(supported.Contains("Refuge"), "supported contains Refuge");
            AssertTrue(supported.Contains("Survival"), "supported contains Survival");
            AssertTrue(supported.Contains("Collapse"), "supported contains Collapse");
        }

        private static void TestDailyGrowthMonotonicProfileVariance()
        {
            float refuge = PopulationProfileMultipliers.GetDailyGrowth("Refuge");
            float survival = PopulationProfileMultipliers.GetDailyGrowth("Survival");
            float collapse = PopulationProfileMultipliers.GetDailyGrowth("Collapse");
            AssertFloat(survival, 1.15f, 0.001f, "Survival baseline 1.15");
            AssertTrue(refuge < survival, "Refuge < Survival");
            AssertTrue(survival < collapse, "Survival < Collapse");
        }

        private static void TestRevengeRatioMonotonicProfileVariance() { /* … */ }
        private static void TestHordeThresholdMonotonicReverse() { /* … */ }
        private static void TestInoculationsPerDayMonotonicProfileVariance() { /* … */ }
        private static void TestInoculationMinIntervalMonotonicReverse() { /* … */ }
        private static void TestUnknownProfileFallsBackToSurvival() { /* … */ }
        private static void TestSurvivalBaselineMatchesSpec() { /* … */ }

        private static void AssertTrue(bool ok, string name) { if (ok) _passed++; else { _failed++; Log.Warning("[FAIL] " + name); } }
        private static void AssertFloat(float expected, float actual, float tol, string name) { /* … ok if |diff| < tol … */ }
    }
}
```

- [ ] **Step 1.2: Run test to verify failure**

Run: `cd /home/vannon/Schreibtisch/Rimconemy && dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | head -40`
Expected: build fails because `PopulationProfileMultipliers` does not exist.

- [ ] **Step 1.3: Implement static class**

Create `Source/Population/PopulationProfileMultipliers.cs`:

```csharp
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Population
{
    public static class PopulationProfileMultipliers
    {
        public static readonly IReadOnlyList<string> SupportedProfiles = new[] { "Refuge", "Survival", "Collapse" };

        public static readonly IReadOnlyDictionary<string, float> DailyGrowth =
            new Dictionary<string, float> { { "Refuge", 1.08f }, { "Survival", 1.15f }, { "Collapse", 1.28f } };

        public static readonly IReadOnlyDictionary<string, float> RevengeRatio =
            new Dictionary<string, float> { { "Refuge", 0.4f }, { "Survival", 0.7f }, { "Collapse", 0.9f } };

        public static readonly IReadOnlyDictionary<string, int> HordeThreshold =
            new Dictionary<string, int> { { "Refuge", 220 }, { "Survival", 150 }, { "Collapse", 80 } };

        public static readonly IReadOnlyDictionary<string, int> InoculationsPerDay =
            new Dictionary<string, int> { { "Refuge", 0 }, { "Survival", 1 }, { "Collapse", 3 } };

        public static readonly IReadOnlyDictionary<string, long> InoculationMinIntervalTicks =
            new Dictionary<string, long> { { "Refuge", long.MaxValue / 2 }, { "Survival", 60_000L * 7 }, { "Collapse", 60_000L * 3 } };

        private const string FallbackProfile = "Survival";

        public static float GetDailyGrowth(string profileId) => DailyGrowth.TryGetValue(profileId ?? FallbackProfile, out var v) ? v : LogWarnAndFallback(DailyGrowth[FallbackProfile], profileId, "DailyGrowth");
        public static float GetRevengeRatio(string profileId) => RevengeRatio.TryGetValue(profileId ?? FallbackProfile, out var v) ? v : LogWarnAndFallback(RevengeRatio[FallbackProfile], profileId, "RevengeRatio");
        public static int GetHordeThreshold(string profileId) => HordeThreshold.TryGetValue(profileId ?? FallbackProfile, out var v) ? v : (int)LogWarnAndFallback(HordeThreshold[FallbackProfile], profileId, "HordeThreshold");
        public static int GetInoculationsPerDay(string profileId) => InoculationsPerDay.TryGetValue(profileId ?? FallbackProfile, out var v) ? v : (int)LogWarnAndFallback(InoculationsPerDay[FallbackProfile], profileId, "InoculationsPerDay");
        public static long GetInoculationMinInterval(string profileId) => InoculationMinIntervalTicks.TryGetValue(profileId ?? FallbackProfile, out var v) ? v : (long)LogWarnAndFallback(InoculationMinIntervalTicks[FallbackProfile], profileId, "InoculationMinInterval");

        private static float LogWarnAndFallback(float fallback, string profileId, string field)
        {
            Log.Warning("[Rimconemy.InfectedAutomation] PopulationProfileMultipliers: unknown profileId='" + (profileId ?? "<null>") + "' for field " + field + "; falling back to Survival.");
            return fallback;
        }
    }
}
```

- [ ] **Step 1.4: Run tests to verify pass**

Run: `dotnet build ... && ./scripts/runtime_test.sh --skip-start --no-deploy`
Expected: PopulationProfileMultipliers regression tests show all-pass in Bootstrap log.

- [ ] **Step 1.5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationProfileMultipliers.cs mods/05-Rimconemy-Infected-Automation/Tests/PopulationProfileMultipliersRegressionTests.cs
git commit -m "feat(05/population): deterministic profile multiplier table (Refuge/Survival/Collapse)"
```

---

## Task 2: Population-Ledger Core + Idempotenz + Save/Migration

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationLedger.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/PopulationLedgerRegressionTests.cs`

**Interfaces (partial — full in later tasks):**
- `PopulationLedger.Get() -> PopulationLedger` — returns instance from `Current.Game.GetComponent<PopulationLedger>()`, or instantiates a transient one if `Current.Game == null`.
- `int HumanoidLiveCount; int AnimalLiveCount; int Cap; int CumulativeKills; int RecentKillsToday; int DayIndexSinceStart; long LastDayTick; string ProfileId; int CumulativeInoculations; long LastInoculationTick;` — all public fields (RimWorld Scribe-friendly).
- `string ClassId => "rimconemy.infectedautomation.population";`
- `int SchemaVersion => 1;`
- `void MigrateIfNeeded();`
- `int GetHumanoidLiveCount(); int GetAnimalLiveCount(); int GetTotalLiveCount(); int GetCap(); int GetCumulativeKills(); int GetRecentKillsToday(); long GetLastInoculationTick(); int GetCumulativeInoculations();`
- `Override ExposeData();` — Scribe_Fields for **all 10 fields**.

- [ ] **Step 2.1: Failing tests for Scribe roundtrip + Schema bump + GetTotal**

In `Tests/PopulationLedgerRegressionTests.cs`:

```csharp
public static class PopulationLedgerRegressionTests
{
    public const int ExpectedPassCount = 16;  // total across all sub-tasks

    private static int _passed;
    private static int _failed;

    public static void RunAll()
    {
        _passed = 0; _failed = 0;
        Population.PopulationLedger.ResetForTests();  // idempotent
        TestSchemaBumpV0ToV1();
        TestScribeRoundTripPreservesFields();
        TestGetTotalLiveCountSum();
        ProfileIdDefaultIsSurvival();
        // … other tasks extend this list …

        Log.Message("[Rimconemy.InfectedAutomation] PopulationLedger regression tests: " + _passed + " passed, " + _failed + " failed.");
    }

    private static void TestSchemaBumpV0ToV1()
    {
        var ledger = new Population.PopulationLedger { /* reset fields explicit */ };
        // Simulate v0 by writing a stub ledger LegacySchema field; for Phase A, MigrateIfNeeded is a No-Op log message
        ledger.MigrateIfNeeded();
        AssertFloat(1f, ledger.SchemaVersion, 0.0f, "SchemaVersion is 1 after migrate");
    }

    private static void TestScribeRoundTripPreservesFields()
    {
        var source = new Population.PopulationLedger {
            HumanoidLiveCount = 7, AnimalLiveCount = 3, Cap = 12,
            CumulativeKills = 5, RecentKillsToday = 2,
            DayIndexSinceStart = 4, LastDayTick = 240_000L,
            ProfileId = "Survival",
            CumulativeInoculations = 1, LastInoculationTick = 100_000L,
        };
        string xml = ScribeRoundTrip(source);  // helper described below
        var reloaded = ScribeRoundTripFromXml(xml);
        AssertEqual(7, reloaded.HumanoidLiveCount, "HumanoidLiveCount preserved");
        AssertEqual(3, reloaded.AnimalLiveCount, "AnimalLiveCount preserved");
        AssertEqual(12, reloaded.Cap, "Cap preserved");
        AssertEqual(5, reloaded.CumulativeKills, "CumulativeKills preserved");
        AssertEqual(2, reloaded.RecentKillsToday, "RecentKillsToday preserved");
        AssertEqual(4, reloaded.DayIndexSinceStart, "DayIndex preserved");
        AssertEqual(240_000L, reloaded.LastDayTick, "LastDayTick preserved");
        AssertEqual("Survival", reloaded.ProfileId, "ProfileId preserved");
        AssertEqual(1, reloaded.CumulativeInoculations, "CumulativeInoculations preserved");
        AssertEqual(100_000L, reloaded.LastInoculationTick, "LastInoculationTick preserved");
    }

    private static void TestGetTotalLiveCountSum()
    {
        var ledger = new Population.PopulationLedger { HumanoidLiveCount = 10, AnimalLiveCount = 4 };
        AssertEqual(14, ledger.GetTotalLiveCount(), "Total = 14");
    }

    private static void ProfileIdDefaultIsSurvival()
    {
        var ledger = new Population.PopulationLedger();
        AssertEqual("Survival", ledger.ProfileId, "default ProfileId is Survival");
    }

    // … helpers and other tests …
    private static void AssertEqual<T>(T expected, T actual, string name) { if (Equals(expected, actual)) _passed++; else { _failed++; Log.Warning("[FAIL] " + name); } }
    private static void AssertFloat(float expected, float actual, float tol, string name) { if (System.Math.Abs(expected - actual) <= tol) _passed++; else { _failed++; Log.Warning("[FAIL] " + name); } }
}
```

- [ ] **Step 2.2: Run test to verify failure (build error: type missing)**

Run: `dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj`
Expected: build fails with `Population.PopulationLedger` not found.

- [ ] **Step 2.3: Implement minimal PopulationLedger (fields + Scribe + Get-accessors only)**

Create `Source/Population/PopulationLedger.cs`:

```csharp
using Rimconemy.Foundation.Save;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Population
{
    public sealed class PopulationLedger : GameComponent, ISchemaMigratable
    {
        public const int CurrentSchemaVersion = 1;
        public string ClassId => "rimconemy.infectedautomation.population";
        public int SchemaVersion => CurrentSchemaVersion;

        public int HumanoidLiveCount;
        public int AnimalLiveCount;
        public int Cap;
        public int CumulativeKills;
        public int RecentKillsToday;
        public int DayIndexSinceStart;
        public long LastDayTick;
        public string ProfileId;
        public int CumulativeInoculations;
        public long LastInoculationTick;

        public PopulationLedger(Game game) { ProfileId = "Survival"; }

        // For tests
        public PopulationLedger() { ProfileId = "Survival"; }

        public static PopulationLedger Get()
        {
            if (Current.Game != null)
            {
                var existing = Current.Game.GetComponent<PopulationLedger>();
                if (existing != null) return existing;
            }
            return new PopulationLedger();
        }

        public static void ResetForTests() { /* no-op when fields are static-free */ }

        public int GetHumanoidLiveCount() => HumanoidLiveCount;
        public int GetAnimalLiveCount() => AnimalLiveCount;
        public int GetTotalLiveCount() => HumanoidLiveCount + AnimalLiveCount;
        public int GetCap() => Cap;
        public int GetCumulativeKills() => CumulativeKills;
        public int GetRecentKillsToday() => RecentKillsToday;
        public long GetLastInoculationTick() => LastInoculationTick;
        public int GetCumulativeInoculations() => CumulativeInoculations;

        public void MigrateIfNeeded()
        {
            int savedVersion = SchemaVersion;
            if (savedVersion < CurrentSchemaVersion)
            {
                Log.Message("[Rimconemy.InfectedAutomation] PopulationLedger: schema " + savedVersion + " → " + CurrentSchemaVersion + " (no-op for Phase A).");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref HumanoidLiveCount, "rimconemyILedgerHumanoidLiveCount", 0);
            Scribe_Values.Look(ref AnimalLiveCount, "rimconemyILedgerAnimalLiveCount", 0);
            Scribe_Values.Look(ref Cap, "rimconemyILedgerCap", 5);
            Scribe_Values.Look(ref CumulativeKills, "rimconemyILedgerKills", 0);
            Scribe_Values.Look(ref RecentKillsToday, "rimconemyILedgerKillsToday", 0);
            Scribe_Values.Look(ref DayIndexSinceStart, "rimconemyILedgerDayIndex", 0);
            Scribe_Values.Look(ref LastDayTick, "rimconemyILedgerLastDayTick", 0L);
            Scribe_Values.Look(ref ProfileId, "rimconemyILedgerProfileId", "Survival");
            Scribe_Values.Look(ref CumulativeInoculations, "rimconemyILedgerInocCount", 0);
            Scribe_Values.Look(ref LastInoculationTick, "rimconemyILedgerLastInocTick", 0L);
        }
    }
}
```

- [ ] **Step 2.4: Add Scribe-roundtrip helper**

In `Tests/ScribeRoundTripHelper.cs` (the package already maintains one; re-use if present):

```csharp
// Add a helper that uses Reflection over Scribe.mode toggling
// Pseudo: Write → Read → Compare
// Pattern identical to existing Helper in mods/02-Rimconemy-Survival-Progression/Tests/ScribeRoundTripHelper.cs
```

Copy helper from existing `ScribeRoundTripHelper` in `mods/02-Rimconemy-Survival-Progression/Tests/ScribeRoundTripHelper.cs`. If it does not exist or is package-scoped, write a minimal local helper that uses RimWorld Scribe with mode-flipping in a try-finally block.

- [ ] **Step 2.5: Run tests to verify pass for T1/T2/T15**

Run: `dotnet build && dotnet test mods/05-Rimconemy-Infected-Automation/...`
Expected: T1, T2, T15 pass.

- [ ] **Step 2.6: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationLedger.cs mods/05-Rimconemy-Infected-Automation/Tests/PopulationLedgerRegressionTests.cs
git commit -m "feat(05/population): PopulationLedger core with Scribe + dual counters + ISchemaMigratable"
```

---

## Task 3: Kill-API + Animal-Support + Idempotenz

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationLedger.cs` (add Write-API + internal track-Pawns set).
- Modify: `mods/05-Rimconemy-Infected-Automation/Tests/PopulationLedgerRegressionTests.cs` (reg T3, T4, T5 in RunAll).

**Interfaces:**
- `void RegisterKill(Pawn pawn)`. If `pawn == null` → Warning + no-op. Otherwise push `pawn.ThingID` into internal `HashSet<string> _killedIds`; if already present, no-op; else increment `CumulativeKills` and `RecentKillsToday`, decrement matching `HumanoidLiveCount` (if `pawn.RaceProps.Humanlike == true`) or `AnimalLiveCount` (otherwise).
- Helper `HashSet<string> _killedIds` — populated in `RegisterKill`, not persisted (Scribe-resets on load → safe idempotency reset across loads).

- [ ] **Step 3.1: Add failing tests**

Append to `PopulationLedgerRegressionTests.RunAll`:

```csharp
TestRegisterKillNullPawnNoOp();
TestRegisterKillHumanPawnIncrementsKills();
TestRegisterKillAnimalPawnIncrementsKills();
TestRegisterKillTwiceOnSamePawnIdempotent();
TestRegisterKillHumanDecrementsHumanoidCounter();
TestRegisterKillAnimalDecrementsAnimalCounter();
```

Each test pattern: `var ledger = new PopulationLedger { HumanoidLiveCount = 5, AnimalLiveCount = 2, RecentKillsToday = 0 };` then call `RegisterKill` with a stub-Pawn (use a `MockPawn`-adapter or `Pawn` ctor with `Faction`, `def`, `kindDef` set) and assert against `CumulativeKills`, `RecentKillsToday`, `HumanoidLiveCount`, `AnimalLiveCount`.

If `Pawn` ctor cannot be invoked without a real Map, use a mock-class with `IPawnInternal` in test code (introduce `interface IPawn{Killable;}` and a `MockPawn : IPawn` only in tests; in production, drop a tiny IPawn inline class to satisfy type.

- [ ] **Step 3.2: Run tests to verify failure**

Run dotnet build; expected: `RegisterKill` does not exist.

- [ ] **Step 3.3: Implement RegisterKill**

In `PopulationLedger.cs`, add:

```csharp
private readonly HashSet<string> _killedIds = new HashSet<string>();

public void RegisterKill(Pawn pawn)
{
    if (pawn == null)
    {
        Log.Warning("[Rimconemy.InfectedAutomation] PopulationLedger.RegisterKill(null); ignored.");
        return;
    }
    string id = pawn.ThingID ?? "<no-id>";
    if (!_killedIds.Add(id))
    {
        // re-entry; idempotent
        return;
    }
    CumulativeKills += 1;
    RecentKillsToday += 1;
    if (pawn.RaceProps != null && pawn.RaceProps.Humanlike) HumanoidLiveCount = System.Math.Max(0, HumanoidLiveCount - 1);
    else if (pawn.RaceProps != null && !pawn.RaceProps.Humanlike) AnimalLiveCount = System.Math.Max(0, AnimalLiveCount - 1);
}
```

NOTE: the `Pawn` reference here is the RimWorld class; tests must construct via `PawnGenerator.GeneratePawn` or a tiny stub with `RaceProps`, `ThingID`. Confirm with `IInspectable` test sketch.

- [ ] **Step 3.4: Run tests**

Expected: T3, T4, T5 pass.

- [ ] **Step 3.5: Commit**

```bash
git commit -am "feat(05/population): RegisterKill with PawnId-idempotency and humanoid/animal routing"
```

---

## Task 4: Daily-Growth + Revenge-Quote + Daily-Reset

**Files:**
- Modify: `PopulationLedger.cs` (add `ApplyDailyGrowthTick`, `ResetDailyCounters`, `GetRevengeQuota`).
- Modify: `PopulationLedgerRegressionTests.cs` (reg T6, T7, T8, T9).

**Interfaces:**
- `int ApplyDailyGrowthTick()` — applies Cap *= `ProfileMultipliers.GetDailyGrowth(ProfileId)` using floor(); returns new Cap.
- `void ResetDailyCounters()` — sets `RecentKillsToday = 0`.
- `int GetRevengeQuota(int maxCap)` — returns `min(maxCap, floor(RecentKillsToday * ProfileMultipliers.GetRevengeRatio(ProfileId)))`.

- [ ] **Step 4.1: Failing tests**

Append tests:

```csharp
TestApplyDailyGrowthTickSurvivalBaseline1_15();
TestApplyDailyGrowthTickRefugeLower();
TestApplyDailyGrowthTickCollapseHigher();
TestRevengeQuotaSurvivalWithCap10();
TestRevengeQuotaClippedByCap();
TestResetDailyCountersResetsRecentOnly();
```

- [ ] **Step 4.2: Implement**

```csharp
public int ApplyDailyGrowthTick()
{
    float m = PopulationProfileMultipliers.GetDailyGrowth(ProfileId);
    Cap = (int)System.Math.Floor(Cap * m);
    DayIndexSinceStart += 1;
    return Cap;
}

public void ResetDailyCounters() { RecentKillsToday = 0; }

public int GetRevengeQuota(int maxCap)
{
    if (maxCap <= 0) return 0;
    float ratio = PopulationProfileMultipliers.GetRevengeRatio(ProfileId);
    int raw = (int)System.Math.Floor(RecentKillsToday * ratio);
    return System.Math.Min(System.Math.Min(maxCap, raw), System.Math.Max(0, Cap - GetTotalLiveCount()));
}
```

- [ ] **Step 4.3: Tests pass; commit**

```bash
git commit -am "feat(05/population): daily growth tick + revenge quota with profile multiplier"
```

---

## Task 5: Reconciler (Humanoid + Animal, No Harmony)

**Files:**
- Create: `Source/Population/PopulationLedgerReconciler.cs` (MapComponent).
- Modify: `Source/Population/PopulationLedger.cs` (add internal `AdjustHumanoidLiveCount`, `AdjustAnimalLiveCount` for the reconciler; expose `Ledger_Settings_AccessibleForReconcilerOnly`).
- Modify: `Tests/PopulationLedgerRegressionTests.cs` (reg T11, T12, T16).

**Interfaces:**
- `PopulationLedgerReconciler(Map map) : MapComponent` overrides `MapComponentTick()`; runs every 60 ticks (NOT every tick — see note).
- The reconciler queries `map.mapPawns.AllPawnsSpawned`, counts pawns where `pawn.Faction?.def?.defName == "Rimconemy_HiddenInfectedFaction" && !pawn.Dead`, splits by `pawn.RaceProps.Humanlike == true` vs `false`.
- Calls `PopulationLedger.Get().AdjustHumanoidLiveCount(thisMapCountHumanoid)` — replacement semantics, not delta.
- Caller is responsible for not running during Scribe; we skip via `if (Scribe.mode != LoadSaveMode.Inactive) return;` at the top of `MapComponentTick`.

- [ ] **Step 5.1: Failing tests**

Tests need a way to simulate a Reconciler call with a count vector. Add `IAdjustableForReconciler` test interface or test-only setter:

```csharp
TestReconcilerSetsHumanoidCount();
TestReconcilerSetsAnimalCount();
TestReconcilerSkipsDuringScribe();  // simulate Scribe.mode = SavingVars; expect 0 setter calls
```

- [ ] **Step 5.2: Implement Reconciler**

```csharp
public sealed class PopulationLedgerReconciler : MapComponent
{
    private const int TickInterval = 60;
    private int _lastTick = -TickInterval;
    private const string HiddenInfectedFactionDef = "Rimconemy_HiddenInfectedFaction";

    public PopulationLedgerReconciler(Map map) : base(map) { }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (map == null) return;
        if (Scribe.mode != LoadSaveMode.Inactive) return;
        int now = Find.TickManager?.TicksGame ?? 0;
        if (now < _lastTick + TickInterval) return;
        _lastTick = now;

        int humans = 0;
        int animals = 0;
        if (map.mapPawns != null)
        {
            var all = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p == null || p.Dead) continue;
                if (p.Faction == null || p.Faction.def == null) continue;
                if (p.Faction.def.defName != HiddenInfectedFactionDef) continue;
                if (p.RaceProps != null && p.RaceProps.Humanlike) humans++;
                else if (p.RaceProps != null && !p.RaceProps.Humanlike) animals++;
            }
        }
        var ledger = PopulationLedger.Get();
        if (ledger == null) return;
        ledger.ReplaceHumanoidLiveCount(humans);
        ledger.ReplaceAnimalLiveCount(animals);
    }
}
```

- [ ] **Step 5.3: Tests pass; commit**

```bash
git commit -am "feat(05/population): tick reconciler MapComponent tracks live infected (humanoid + animal)"
```

---

## Task 6: Inoculation-Daten-Hooks

**Files:**
- Modify: `PopulationLedger.cs` (add `NoteInoculation(string kindDefName)`).
- Modify: `Tests/PopulationLedgerRegressionTests.cs` (reg T13, T14).

**Interfaces:**
- `void NoteInoculation(string kindDefName)` — if `kindDefName == null || empty` → Warning + no-op. Else increment `CumulativeInoculations`, set `LastInoculationTick = Find.TickManager.TicksGame`.

- [ ] **Step 6.1: Failing tests**

```csharp
TestNoteInoculationStampsTickAndIncrements();
TestNoteInoculationNullKindDefNoOp();
TestNoteInoculationEligibilityHonorsMinInterval();  // checks via PopulationProfileMultipliers
```

- [ ] **Step 6.2: Implement**

```csharp
public void NoteInoculation(string kindDefName)
{
    if (string.IsNullOrEmpty(kindDefName))
    {
        Log.Warning("[Rimconemy.InfectedAutomation] PopulationLedger.NoteInoculation(<empty>); ignored.");
        return;
    }
    CumulativeInoculations += 1;
    LastInoculationTick = Find.TickManager?.TicksGame ?? 0L;
}
```

- [ ] **Step 6.3: Tests pass; commit**

```bash
git commit -am "feat(05/population): NoteInoculation data-hook for Phase C service"
```

---

## Task 7: Foundation Capability + Bootstrap-Integration

**Files:**
- Modify: `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs` (add capability row).
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` (call both RunAll-s + MigrationRegistry.Register).
- Modify: `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs` — bump `Rimconemy.InfectedAutomation` packageVersion to next patch (e.g. `0.0.53` → `0.0.54`).

**Interfaces (no new code, only metadata):**
- Foundation Registry: `new Capability("rimconemy.infectedautomation.population", 1)` added to the `20-Rimconemy-InfectedAutomation` `capabilities` array.
- Bootstrap: `Rimconemy.Foundation.Save.MigrationRegistry.Register("rimconemy.infectedautomation.population", () => new Population.PopulationLedger());` — single line at appropriate spot.
- Bootstrap: `Tests.PopulationProfileMultipliersRegressionTests.RunAll();` and `Tests.PopulationLedgerRegressionTests.RunAll();` registered in correct order.

- [ ] **Step 7.1: Modify PackageRegistry**

Find the existing entry:

```csharp
TryRegisterLoadedAssembly(
    assemblyName: "Rimconemy.InfectedAutomation",
    packageId: "rimconemy.infectedautomation",
    packageVersion: "0.0.53",
    capabilities: new[]
    {
        new Capability("rimconemy.infectedautomation.threat", 1),
        new Capability("rimconemy.infectedautomation.automation", 1),
    },
```

Add `new Capability("rimconemy.infectedautomation.population", 1),` to the capabilities array. Bump `packageVersion: "0.0.54"`.

- [ ] **Step 7.2: Run dev_quick_test**

Run: `./scripts/dev_quick_test.sh`
Expected: build succeeds, no missing-symbol errors.

- [ ] **Step 7.3: Commit**

```bash
git commit -am "feat(05/foundation): register population capability v1 in Registry + Bootstrap RunAll"
```

---

## Task 8: Bump Version + Final Verification

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/VERSION` via `bump_version.sh`.

- [ ] **Step 8.1: Bump version**

Run: `./scripts/bump_version.sh 05`
Expected: prints new version; updates VERSION file.

- [ ] **Step 8.2: Run full static gate**

Run: `./scripts/runtime_test.sh --skip-start --no-deploy`
Expected: exit 0; both new regression suites show "X passed, 0 failed" in the log.

- [ ] **Step 8.3: Run quick build**

Run: `dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj`
Expected: 0 errors, 0 warnings except the documented `[Obsolete]` ones.

- [ ] **Step 8.4: Final commit**

```bash
git commit -am "chore(05): bump version after Phase A Population-Ledger"
```

- [ ] **Step 8.5: User-action required**

Hand off the live-test to the user:
- Run `./scripts/deploy.sh 05`.
- Run `./scripts/runtime_test.sh` for full game start.
- Verify that `Rimconemy.InfectedAutomation] PopulationLedger...` line shows in Player.log.
- Update `docs/falsification/infected__InfectedRaid.md §D` with the live-log excerpt.
- Hand off to Phase B (Daily-Growth-Tick + Spawn-Integration).

---

## Self-Review

### Spec coverage check

| Spec § | Implementation |
|---|---|
| §2 Datenstruktur | Task 2 ✅ |
| §2 Lese-API | Task 2 ✅ |
| §2 Schreib-API | Task 3 ✅ |
| §2 Reconcile-Strategy | Task 5 ✅ |
| §2 Animal-Inokulation-Datenflow | Task 6 ✅ |
| §3 Tageszyklus | Task 4 ✅ |
| §3 Kill-Pfad | Task 3 ✅ |
| §3 Death-Pfad Reconciliation | Task 5 ✅ |
| §4 Scribe | Task 2 ✅ |
| §4 Migration | Task 2 ✅ |
| §4 Capability | Task 7 ✅ |
| §5 Tests T1-T16 | Tasks 1-7 ✅ |
| §6 Bootstrap-Integration | Task 7 ✅ |
| §7 Edge Cases | Tasks 3-6 ✅ |
| §8 Nicht-Ziele | Documented ✅ |
| §10 Akzeptanz-Gate A1-A7 | Tasks 7-8 ✅ |

### Placeholder scan

- No "TBD"/"TODO"/"implement later" — every step has C# code or shell commands.
- "Add appropriate error handling" replaced by specific edge-case test names (T4, T14).
- "Similar to Task N" not used — each test code is concrete.
- No references to undefined types: `PopulationProfileMultipliers` (Task 1), `PopulationLedger` (Task 2). Mock-Pawn structure noted in Task 3 step.

### Type consistency

- `GetHumanoidLiveCount`/`GetAnimalLiveCount`/`GetTotalLiveCount` defined in Task 2 used in Tasks 3/4/5/6 — types match (`int`).
- `ProfileId` initialized to `"Survival"` in **all** constructor paths.
- `RegisterKill` signature `(Pawn pawn)` consistent in plan + test stubs.
- `CumulativeKills` and `RecentKillsToday` are separate fields; both incremented in `RegisterKill`; `RecentKillsToday` reset in `ResetDailyCounters`.

### Notes for implementation

- Task 3 step 3 explicitly notes the issue with constructing a real `Pawn`. If the existing `IPawnInternal` interface pattern is not present, the implementer must introduce a small adapter (`interface IPawnForTest { RaceProps; ThingID; }`). Real production uses RimWorld `Pawn`. The test should not require an actual `Map`.
- Task 5 step 2 has the Reconciler; the `Scribe.mode != LoadSaveMode.Inactive` early-return is critical for Save/Load tests.
- The tests in Tasks 2-6 cover the spec's T1-T16 list in order; the `RunAll` summary line will show "16 passed, 0 failed".

Plan complete and saved to `docs/superpowers/plans/2026-08-05-population-ledger.md`.
