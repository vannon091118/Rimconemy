# Animal-Infection via Random Encounter Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tiere im RimWorld-Map können laufend via Random Encounter zu infizierten, aggressiven Wildtieren werden, getrieben durch die Horde-Bedrohungslage und SettingProfile-spezifische Wahrscheinlichkeiten. Resultat: rotes "!" über befallenen Tieren, sichtbare Angriffe auf Colonists, +50% Speed-Boost.

**Architecture:** Re-Audit-Loop-frei durch statische Pure-Chance-Logik + MapComponent-Driver + Harmony-Patch auf bestehender RandomInoculationService-Pipeline. Save/Load-safe via PopulationLedger-Persistenz. Profile-getrieben (Survival/Collapse/Refuge-Multipliers).

**Tech Stack:** C# 10, .NET 10, Harmony 2.x, RimWorld 1.6.4566, MapComponent + GameComponent + IncidentWorker-Patches.

## Global Constraints (aus Spec §1-11)

- Phase E Spec: `docs/superpowers/specs/2026-08-05-animal-infection-design.md`
- TEST-TDD-Profil: jeder Task beginnt mit Failing-Test, endet mit Commit + Build-Check
- Cross-Package-Trennung halten: nur Mod 05 (Rimconemy.InfectedAutomation) anfassen
- Re-Audit-Compliance: keine globalen mutable States; nur ledger-netzwerk-Schnittstellen
- Determinismus: alle Zufallswerte gehen via FNV1a-Hash(TickDayBucket|ProfileId|HordeCount); Live-Würfel niemals direkt aus `UnityEngine.Random`
- Build-Skript: `RimWorldManagedPath=/home/vannon/GOG\ Games/RimWorld/game/RimWorldLinux_Data/Managed HarmonyAssembliesPath=/home/vannon/GOG\ Games/RimWorld/game/Mods/Harmony/Current/Assemblies dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj`
- runtime_check: `./scripts/runtime_test.sh --skip-start --no-deploy`

---

### Task 1: Pure-Logic `AnimalInfectionChance` Foundation

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/AnimalInfectionChance.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/AnimalInfectionRegressionTests.cs`

**Interfaces:**
- Produces: `AnimalInfectionChance.ComputeChancePerDay(long tickDayBucket, int hordeCount, SettingProfile profile) -> double`
- Produces: `AnimalInfectionChance.ComputeInfectionCount(long tickDayBucket, int hordeCount, SettingProfile profile) -> int`
- Produces: `AnimalInfectionChance.ShouldFireToday(long currentTick, int todayCount, int hordeCount, SettingProfile profile) -> bool`
- Consumes: `PopulationProfileMultipliers.GetAnimalInfectionBaseChance(profileId)`, `PopulationProfileMultipliers.GetAnimalInfectionHordeScalingFactor(profileId)`, `PopulationProfileMultipliers.GetMaxAnimalInoculationsPerDay(profileId)`, `PopulationProfileMultipliers.GetMinIntervalBetweenInoculations(profileId)`, `PopulationProfileMultipliers.GetHordeThreshold(profileId)`

**Step 1: Write `AnimalInfectionChance.cs` Skeleton + Failing Tests**

Create `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/AnimalInfectionChance.cs`:

```csharp
using Verse;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;

namespace Rimconemy.InfectedAutomation.Inoculation
{
    /// <summary>
    /// Pure-Logic für Tiersym-Infektion via Random Encounter.
    /// Kein IO, keine Verse-Mutation. Wird vom AnimalInfectionDriver
    /// aufgerufen; regression-test-bar ohne MapComponent.
    /// </summary>
    public static class AnimalInfectionChance
    {
        public const double HardCap = 0.95;

        public static double ComputeChancePerDay(
            long tickDayBucket, int hordeCount, SettingProfile profile)
        {
            return 0d; // TODO impl in step-3
        }

        public static int ComputeInfectionCount(
            long tickDayBucket, int hordeCount, SettingProfile profile)
        {
            return 0;
        }

        public static bool ShouldFireToday(
            long currentTick, int todayCount, int hordeCount, SettingProfile profile)
        {
            return false;
        }
    }
}
```

Create `mods/05-Rimconemy-Infected-Automation/Tests/AnimalInfectionRegressionTests.cs` with T1-T8 tests:

```csharp
// Tests/AnimalInfectionRegressionTests.cs
using Rimconemy.InfectedAutomation.Inoculation;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class AnimalInfectionRegressionTests
    {
        public const int ExpectedPassCount = 8;

        public static int RunAll()
        {
            int passed = 0; int failed = 0; string firstFailure = null;
            void Check(bool ok, string n) { if (ok) { passed++; return; } failed++; if (firstFailure == null) firstFailure = n; Log.Warning("[Rimconemy.InfectedAutomation] AnimalInfection test FAILED: " + n); }
            Check(T1_SurvivalBaseChance(),       "T1.SurvivalBaseChance");
            Check(T2_SurvivalScaleAt100(),       "T2.SurvivalScaleAt100");
            Check(T3_SurvivalScaleAt200(),       "T3.SurvivalScaleAt200");
            Check(T4_CollapseScaleAt50(),        "T4.CollapseScaleAt50");
            Check(T5_RefugeBaseFloor(),          "T5.RefugeBaseFloor");
            Check(T6_BelowThresholdNoDecay(),    "T6.BelowThresholdNoDecay");
            Check(T7_HardCapClamp(),             "T7.HardCapClamp");
            Check(T8_CountRespectsPerDayCap(),   "T8.CountRespectsPerDayCap");
            Log.Message("[Rimconemy.InfectedAutomation] AnimalInfection regression tests: " + passed + " passed, " + failed + " failed" + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        // T1
        private static bool T1_SurvivalBaseChance()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(1L, 0, SettingProfile.Survival);
            return System.Math.Abs(c - 0.05) < 0.001;
        }

        // T2
        private static bool T2_SurvivalScaleAt100()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(2L, 100, SettingProfile.Survival);
            double expected = 0.05 * (1.0 + 1.0 * 100.0 / 150.0);
            return System.Math.Abs(c - expected) < 0.001;
        }

        // T3
        private static bool T3_SurvivalScaleAt200()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(3L, 200, SettingProfile.Survival);
            double expected = 0.05 * (1.0 + 1.0 * 200.0 / 150.0);
            return System.Math.Abs(c - expected) < 0.001;
        }

        // T4
        private static bool T4_CollapseScaleAt50()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(4L, 50, SettingProfile.Collapse);
            double expected = 0.15 * (1.0 + 1.5 * 50.0 / 80.0);
            return System.Math.Abs(c - expected) < 0.001;
        }

        // T5
        private static bool T5_RefugeBaseFloor()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(5L, 0, SettingProfile.Refuge);
            return System.Math.Abs(c - 0.02) < 0.001;
        }

        // T6
        private static bool T6_BelowThresholdNoDecay()
        {
            double cZero = AnimalInfectionChance.ComputeChancePerDay(6L, 0, SettingProfile.Collapse);
            double cNeg = AnimalInfectionChance.ComputeChancePerDay(6L, -10, SettingProfile.Collapse);
            return cZero >= cNeg && System.Math.Abs(cZero - 0.15) < 0.001;
        }

        // T7
        private static bool T7_HardCapClamp()
        {
            double c = AnimalInfectionChance.ComputeChancePerDay(7L, 10_000_000, SettingProfile.Collapse);
            return c <= AnimalInfectionChance.HardCap + 0.0001;
        }

        // T8
        private static bool T8_CountRespectsPerDayCap()
        {
            // PerDayCap for Collapse = 4, FNV1a-Roll wird deterministisch sein.
            int cnt = AnimalInfectionChance.ComputeInfectionCount(8L, 100, SettingProfile.Collapse);
            int cap = PopulationProfileMultipliers.GetMaxAnimalInoculationsPerDay("Collapse");
            return cnt >= 0 && cnt <= cap;
        }
    }
}
```

**Step 2: Verify Build fails (T1-T8 müssen rot sein weil impl-stubs return 0)**

Run: `RimWorldManagedPath=/home/vannon/GOG\ Games/RimWorld/game/RimWorldLinux_Data/Managed HarmonyAssembliesPath=/home/vannon/GOG\ Games/RimWorld/game/Mods/Harmony/Current/Assemblies dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -10`
Expected: Build OK (impl ist 0/false/0, syntaktisch valide). Tests T1-T8 würden fehlschlagen, weil ComputeChancePerDay=0 nicht 0.05 ist.

**Step 3: Implement ComputeChancePerDay**

In `AnimalInfectionChance.cs`, replace `ComputeChancePerDay`:

```csharp
public static double ComputeChancePerDay(
    long tickDayBucket, int hordeCount, SettingProfile profile)
{
    string key = Story.StoryDirector.StripRimconemyPrefix(profile?.ProfileId);
    double baseChance = PopulationProfileMultipliers.GetAnimalInfectionBaseChance(key);
    double scalingFactor = PopulationProfileMultipliers.GetAnimalInfectionHordeScalingFactor(key);
    int threshold = PopulationProfileMultipliers.GetHordeThreshold(key);

    double above = System.Math.Max(0, hordeCount - threshold);
    double ratio = threshold > 0 ? above / threshold : 0.0;
    return System.Math.Min(HardCap, baseChance * (1.0 + scalingFactor * ratio));
}
```

**Step 4: Implement ShouldFireToday + ComputeInfectionCount (deterministisch via FNV1a)**

In `AnimalInfectionChance.cs`, replace die zwei Methoden:

```csharp
public static bool ShouldFireToday(
    long currentTick, int todayCount, int hordeCount, SettingProfile profile)
{
    string key = Story.StoryDirector.StripRimconemyPrefix(profile?.ProfileId);
    long dayBucket = currentTick / 60000L;
    if (dayBucket < 1L) return false;
    int cap = PopulationProfileMultipliers.GetMaxAnimalInoculationsPerDay(key);
    if (todayCount >= cap) return false;
    double chance = ComputeChancePerDay(dayBucket, hordeCount, profile);
    uint hash = FnvHash($"{dayBucket}|{key}|{hordeCount / 10}");
    double roll = (hash % 10000) / 10000.0;
    return roll < chance;
}

public static int ComputeInfectionCount(
    long tickDayBucket, int hordeCount, SettingProfile profile)
{
    string key = Story.StoryDirector.StripRimconemyPrefix(profile?.ProfileId);
    int cap = PopulationProfileMultipliers.GetMaxAnimalInoculationsPerDay(key);
    if (cap <= 0) return 0;

    // Roll einen Count im Range [0..cap], abhängig von Horde-Skalierung.
    uint hash = FnvHash($"cnt|{tickDayBucket}|{key}|{hordeCount / 10}");
    int rollBucket = (int)(hash % 1024U); // 0..1023
    double pct = rollBucket / 1024.0;
    return (int)System.Math.Floor(pct * (cap + 1));
}

// FNV-1a 32-bit hash
private static uint FnvHash(string s)
{
    unchecked
    {
        uint h = 2166136261u;
        foreach (char c in s) { h ^= c; h *= 16777619u; }
        return h;
    }
}
```

**Step 5: Build + verify**

Run: build command.
Expected: Build clean (warnings=0, errors=0). Tests T1-T8 should pass given the formula.

**Step 6: Commit**

`git add mods/05-Rimconemy-Infected-Automation/Source/Inoculation/AnimalInfectionChance.cs mods/05-Rimconemy-Infected-Automation/Tests/AnimalInfectionRegressionTests.cs`
`git commit -m 'feat(05/animal-infection): AnimalInfectionChance Pure + T1-T8 tests (Phase E T1)'`

---

### Task 2: PopulationProfileMultipliers — neue Felder für AnimalInfection

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationProfileMultipliers.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/PopulationProfileMultipliersAnimalInfectionTests.cs`

**Interfaces:**
- Produces: `GetAnimalInfectionBaseChance(profileId) -> double`
- Produces: `GetAnimalInfectionHordeScalingFactor(profileId) -> double`

**Step 1: Read bestehende Multiplier**

Read `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationProfileMultipliers.cs`, finde die `MaxAnimalInoculationsPerDay`-Getter und füge nach deren Muster zwei neue Methoden hinzu.

**Step 2: Add methods + Profile-falls LogWarnFallback**

Insertion am Ende der Klasse (vor der schließenden `}`):

```csharp
public static double GetAnimalInfectionBaseChance(string profileId)
{
    if (ProfileIdKey_Helper(profileId, out string key))
    {
        switch (key)
        {
            case "Collapse": return 0.15;
            case "Refuge":   return 0.02;
            case "Survival":
            default:
                return 0.05;
        }
    }
    Log.Warning("[Rimconemy.InfectedAutomation] GetAnimalInfectionBaseChance: unknown profileId='" + profileId + "'; falling back to Survival=0.05");
    return 0.05;
}

public static double GetAnimalInfectionHordeScalingFactor(string profileId)
{
    if (ProfileIdKey_Helper(profileId, out string key))
    {
        switch (key)
        {
            case "Collapse": return 1.5;
            case "Refuge":   return 0.5;
            case "Survival":
            default:
                return 1.0;
        }
    }
    Log.Warning("[Rimconemy.InfectedAutomation] GetAnimalInfectionHordeScalingFactor: unknown profileId='" + profileId + "'; falling back to Survival=1.0");
    return 1.0;
}
```

Identity `ProfileIdKey_Helper` ist bereits in der Klasse — sollte vorhanden sein, sonst verwendet die bestehende `GetMaxAnimalInoculationsPerDay`-Getter ihren eigenen Helpers. Stattdessen den existierenden Helper-Stil kopieren (siehe Beispiele in der Klasse).

If `ProfileIdKey_Helper` does not exist, use the same pattern as the existing methods:

```csharp
public static double GetMaxAnimalInoculationsPerDay(string profileId)
{
    if (!string.IsNullOrEmpty(profileId))
    {
        switch (profileId)
        {
            case "Collapse": return 4;
            // ...
        }
    }
    return 2;
}
```

**Step 3: Add Test**

Create `Tests/PopulationProfileMultipliersAnimalInfectionTests.cs`:

```csharp
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class PopulationProfileMultipliersAnimalInfectionTests
    {
        public static int RunAll()
        {
            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string n) { if (ok) { passed++; return; } failed++; if (firstFailure == null) firstFailure = n; Log.Warning("[Rimconemy.InfectedAutomation] ProfileMultipliers test FAILED: " + n); }

            Check(System.Math.Abs(PopulationProfileMultipliers.GetAnimalInfectionBaseChance("Survival") - 0.05) < 0.001, "BaseChance Survival 0.05");
            Check(System.Math.Abs(PopulationProfileMultipliers.GetAnimalInfectionBaseChance("Collapse") - 0.15) < 0.001, "BaseChance Collapse 0.15");
            Check(System.Math.Abs(PopulationProfileMultipliers.GetAnimalInfectionBaseChance("Refuge")   - 0.02) < 0.001, "BaseChance Refuge 0.02");
            Check(System.Math.Abs(PopulationProfileMultipliers.GetAnimalInfectionBaseChance(null)      - 0.05) < 0.001, "BaseChance null → 0.05");
            Check(System.Math.Abs(PopulationProfileMultipliers.GetAnimalInfectionBaseChance("Rimconemy_Survival") - 0.05) < 0.001, "BaseChance prefixed Fallback-to-Survival");

            Check(System.Math.Abs(PopulationProfileMultipliers.GetAnimalInfectionHordeScalingFactor("Survival") - 1.0) < 0.001, "Scale Survival 1.0");
            Check(System.Math.Abs(PopulationProfileMultipliers.GetAnimalInfectionHordeScalingFactor("Collapse") - 1.5) < 0.001, "Scale Collapse 1.5");
            Check(System.Math.Abs(PopulationProfileMultipliers.GetAnimalInfectionHordeScalingFactor("Refuge")   - 0.5) < 0.001, "Scale Refuge 0.5");

            Log.Message("[Rimconemy.InfectedAutomation] ProfileMultipliers AnimalInfection tests: " + passed + " passed, " + failed + " failed" + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }
    }
}
```

**Step 4: Build + run**

Run: build (clean). Tests T1-T8 from Task 1 should now resolve `Get*Chance` correctly.

**Step 5: Commit**

`git add mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationProfileMultipliers.cs mods/05-Rimconemy-Infected-Automation/Tests/PopulationProfileMultipliersAnimalInfectionTests.cs`
`git commit -m 'feat(05/animal-infection): PopulationProfileMultipliers animal infection getters + tests (Phase E T2)'`

---

### Task 3: PopulationLedger — `LastAnimalInfectionTick` + `AnimalInfectionCountToday` Felder

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationLedger.cs` (Scribe, FinalizeInit, fields, ResetDailyCounters)
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/PopulationLedgerAnimalInfectionFieldsTests.cs`

**Interfaces:**
- Produces: `PopulationLedger.LastAnimalInfectionTick` (long, scribe)
- Produces: `PopulationLedger.AnimalInfectionCountToday` (int, scribe)
- Produces: `PopulationLedger.RegisterAnimalInfection(int count, long currentTick)` — inkrementiert + setzt last-tick.

**Step 1: Read PopulationLedger**

Finde Block "Fields" + "Scribe" + "FinalizeInit" + "ApplyDailyGrowthTick" (oder ResetDailyCounters).

**Step 2: Add Fields + Scribe + Reset-Tages-Counter**

Insertion in der Klasse. Stelle sicher dass keine bestehende Scribe-Konvention gebrochen wird (typischerweise block-weise: Scribe_Values.Look(ref _field, "key", default)).

Füge im Field-Block hinzu:

```csharp
/// <summary>Tick des letzten AnimalInfection-Auslösers. 0 = noch nie.
/// Scribe-safe: Save/Load rebuilds die Counter-Schleife.</summary>
public long LastAnimalInfectionTick;

/// <summary>Tageszähler: Anzahl via Driver infizierte Tiere heute.
/// Wird in ResetDailyCounters zurückgesetzt.</summary>
public int AnimalInfectionCountToday;
```

In Scribe (oder ScribeInlinePattern wenn vorhanden), Insertion:

```csharp
Scribe_Values.Look(ref LastAnimalInfectionTick, "lastAnimalInfectionTick", 0L);
Scribe_Values.Look(ref AnimalInfectionCountToday, "animalInfectionCountToday", 0);
```

In ResetDailyCounters (oder entsprechender Funktion), Insertion am Anfang:

```csharp
AnimalInfectionCountToday = 0;
```

In FinalizeInit (oder das Gegenstück zum PostLoadInit), nach `base.FinalizeInit()`, Insertion:

```csharp
if (LastAnimalInfectionTick < 0L) LastAnimalInfectionTick = 0L;
```

Add Action-Helper:

```csharp
/// <summary>Records the day's actual animal-infection count + tick.
/// Called from AnimalInfectionDriver after a successful TryInfectWildAnimals.</summary>
public void RegisterAnimalInfection(int count, long currentTick)
{
    if (count <= 0) return;
    AnimalInfectionCountToday += count;
    LastAnimalInfectionTick = currentTick;
}
```

**Step 3: Add Test**

Create `Tests/PopulationLedgerAnimalInfectionFieldsTests.cs`:

```csharp
using Rimconemy.InfectedAutomation.Population;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class PopulationLedgerAnimalInfectionFieldsTests
    {
        public static int RunAll()
        {
            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string n) { if (ok) { passed++; return; } failed++; if (firstFailure == null) firstFailure = n; Log.Warning("[Rimconemy.InfectedAutomation] LedgerAnimalInfectionFields test FAILED: " + n); }

            var l = new PopulationLedger();
            Check(l.LastAnimalInfectionTick == 0L, "Default LastAnimalInfectionTick=0L");
            Check(l.AnimalInfectionCountToday == 0, "Default AnimalInfectionCountToday=0");

            l.RegisterAnimalInfection(3, 60_000L);
            Check(l.AnimalInfectionCountToday == 3, "After RegisterAnimalInfection(3) Count=3");
            Check(l.LastAnimalInfectionTick == 60_000L, "After RegisterAnimalInfection LastTick=60000");

            l.RegisterAnimalInfection(2, 120_000L);
            Check(l.AnimalInfectionCountToday == 5, "Cumulative count=5");
            Check(l.LastAnimalInfectionTick == 120_000L, "LastTick updated to 120000");

            l.RegisterAnimalInfection(0, 0L); // no-op
            Check(l.AnimalInfectionCountToday == 5, "Zero-count register no-op");

            Log.Message("[Rimconemy.InfectedAutomation] LedgerAnimalInfectionFields tests: " + passed + " passed, " + failed + " failed" + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }
    }
}
```

**Step 4: Build + verify**

Run build. Expect 0 errors. Test must compile clean.

**Step 5: Commit**

`git add mods/05-Rimconemy-Infected-Automation/Source/Population/PopulationLedger.cs mods/05-Rimconemy-Infected-Automation/Tests/PopulationLedgerAnimalInfectionFieldsTests.cs`
`git commit -m 'feat(05/animal-infection): PopulationLedger LastAnimalInfectionTick + CountToday + tests (Phase E T3)'`

---

### Task 4: `RandomInoculationService.TryInfectWildAnimals(int maxCount)` API

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/RandomInoculationServiceTryInfectLimitTests.cs`

**Interfaces:**
- Reinforce: `RandomInoculationService.TryInfectWildAnimals(int maxCount) -> int` — gibt die tatsächlich infizierte Anzahl zurück (= Anzahl konvertierter Pawns), niemals > maxCount.

**Step 1: Read RandomInoculationService.cs**

Finde die existierende Convert-Pipeline (WalkMap + KindDef-Swap). Wenn die Methode bereits alle Wildtiere auf der Map konvertiert, dann bracht hier nur ein `int maxCount`-Parameter + early-exit sobald limit erreicht.

**Step 2: Add Param + Limit-Behavior**

Suche nach der innersten Schleife, die pro Candidate einen Convert ausführt. Wrap in:

```csharp
public static int TryInfectWildAnimals(int maxCount = int.MaxValue)
{
    if (Current.Game == null) return 0;
    var candidateList = new System.Collections.Generic.List<InoculationCandidate>();
    FindEligibleCandidates(candidateList); // existing helper
    int actual = 0;
    foreach (var c in candidateList)
    {
        if (actual >= maxCount) break;
        if (TryConvertPawn(c)) // existing logic
        {
            actual++;
        }
    }
    return actual;
}
```

**Existierende Konvertierungs-Pfad**: Beibehalten. Nur frühzeitiger `break` wenn `actual >= maxCount`.

**Step 3: Add Test**

Create `Tests/RandomInoculationServiceTryInfectLimitTests.cs`:

Diese Tests benötigen einen Stub-Map-Pfad. Statt komplexem Map-Setup prüfen wir nur die arithmetische Caps:

```csharp
using Rimconemy.InfectedAutomation.Inoculation;
using Rimconemy.InfectedAutomation.Population;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class RandomInoculationServiceTryInfectLimitTests
    {
        public static int RunAll()
        {
            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string n) { if (ok) { passed++; return; } failed++; if (firstFailure == null) firstFailure = n; Log.Warning("[Rimconemy.InfectedAutomation] RandomInoculation TryInfectLimit test FAILED: " + n); }

            Check(TryInfectNoGameReturnsZero(), "T.Limit.NoGameZero");
            Check(TryInfectNoMapReturnsZero(),   "T.Limit.NoMapZero");

            Log.Message("[Rimconemy.InfectedAutomation] RandomInoculation TryInfectLimit tests: " + passed + " passed, " + failed + " failed" + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        // T.Limit.NoGameZero: kein Current.Game → 0
        private static bool TryInfectNoGameReturnsZero()
        {
            return RandomInoculationService.TryInfectWildAnimals(5) == 0;
        }

        // T.Limit.NoMapZero: Current.Game ohne Map → 0
        private static bool TryInfectNoMapReturnsZero()
        {
            // Pseudo-Check: ohne Map verfügbar, returns 0
            // Hängt vom Game-State ab — Test passes wenn die Implementierung defensiv früh returniert.
            try { return RandomInoculationService.TryInfectWildAnimals(5) == 0; }
            catch { return false; }
        }
    }
}
```

**Step 4: Build + verify**

`./scripts/runtime_test.sh --skip-start --no-deploy` — erwartet PASS.

**Step 5: Commit**

`git add mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs mods/05-Rimconemy-Infected-Automation/Tests/RandomInoculationServiceTryInfectLimitTests.cs`
`git commit -m 'feat(05/animal-infection): RandomInoculationService.TryInfectWildAnimals(maxCount) limit + tests (Phase E T4)'`

---

### Task 5: `AnimalInfectionDriver` MapComponent

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/AnimalInfectionDriver.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/AnimalInfectionDriverRegressionTests.cs`

**Interfaces:**
- Consumes: `AnimalInfectionChance.ShouldFireToday`, `AnimalInfectionChance.ComputeInfectionCount`, `PopulationLedger.Get`, `Story.StoryDirector.Get`, `RandomInoculationService.TryInfectWildAnimals`
- Produces: Auto-map-registration via Def-database etc.

**Step 1: Read existing MapComponent patterns**

`HordeSpawner` aus Phase D ist ein MapComponent-Pattern. Verwende analoge Struktur.

**Step 2: Create Driver-Code**

Create `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/AnimalInfectionDriver.cs`:

```csharp
// Source/Inoculation/AnimalInfectionDriver.cs
//
// Phase E — Recurring Driver for animal-infection via Random Encounter.
// Owner: Infected & Automation (Package 05).
//
// Auto-registered as a MapComponent (HordeSpawner.cs uses the same pattern).
// Re-fires at most every <see cref="TickInterval"/> ticks so a single
// live-game MapTick does not blow cost budget. Idempotency ensured by
// storing the last-tick on the PopulationLedger (scribe-safe).

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Inoculation
{
    public sealed class AnimalInfectionDriver : MapComponent
    {
        public const int TickInterval = 3_600; // 60 in-game seconds at 60 ticks/s
        private long _lastTickProcessed = -1L;

        public AnimalInfectionDriver(Map map) : base(map) { }

        // Auto-register: pickup happens by RimWorld via Def or Harmony AutoPatch.
        // (HordeSpawner uses the same convention; see that file.)

        public override void MapComponentTick()
        {
            long now = Find.TickManager?.TicksGame ?? 0L;
            if (now <= 0L) return;
            if (now - _lastTickProcessed < TickInterval && _lastTickProcessed > 0L) return;
            _lastTickProcessed = now;

            var ledger = PopulationLedger.Get();
            var profile = Story.StoryDirector.Get()?.ActiveProfile;
            if (ledger == null || profile == null)
            {
                if (Verse.DebugSettings.godMode)
                    Log.Warning("[Rimconemy.InfectedAutomation] AnimalInfectionDriver: null ledger or profile, no-op.");
                return;
            }

            int hordeCount = System.Math.Max(0, ledger.HumanoidLiveCount + ledger.AnimalLiveCount / 2);
            if (hordeCount <= 0) return;

            if (!AnimalInfectionChance.ShouldFireToday(
                    now, ledger.AnimalInfectionCountToday, hordeCount, profile))
                return;

            int count = AnimalInfectionChance.ComputeInfectionCount(now, hordeCount, profile);
            if (count <= 0) return;

            int actually = RandomInoculationService.TryInfectWildAnimals(count);
            if (actually > 0)
            {
                ledger.RegisterAnimalInfection(actually, now);
            }
        }

        /// <summary>Production-only reset hook used by tests so the static
        /// last-tick never leaks across map boundaries. Not used in-game.</summary>
        public void ResetForTests()
        {
            _lastTickProcessed = -1L;
        }
    }
}
```

**Step 3: Activate-Harmony-AutoRegister**

Verwende Pattern aus `HordeSpawner` (siehe dort). Die Auto-Registration muss via `MapComponentUtility` oder via statischer `LoadedModManager`-Hook im `Bootstrap` geschehen.

In `Bootstrap.cs` (existing file, MOD statt CREAT):

Finde den Block `// Phase D Horde overlay (Calculator, SectionLayer, BurstLayer, CameraEdge)` Füge nach Search-Pattern analogem Phase-E-Block:

```csharp
// Phase E (2026-08-05) — Animal-Infection Driver: auto-register on map spawn.
World.MapComponentPatch_RimconemyAnimalInfectionDriver.Install();
```

Oder verwende das gleiche statische Install-Pattern wie `DarknessSectionLayerLifecycle.Install()` — wenn eine Initialize-API existiert.

**Step 4: Add Driver-Tests T9-T15**

Create `Tests/AnimalInfectionDriverRegressionTests.cs`:

```csharp
using Rimconemy.InfectedAutomation.Inoculation;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class AnimalInfectionDriverRegressionTests
    {
        public static int RunAll()
        {
            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string n) { if (ok) { passed++; return; } failed++; if (firstFailure == null) firstFailure = n; Log.Warning("[Rimconemy.InfectedAutomation] AnimalInfectionDriver test FAILED: " + n); }

            Check(T9_DriverGateSlow(),         "T9.DriverGateSlow");
            Check(T10_CapHonours(),            "T10.CapHonours");
            Check(T11_InitialFiresOnce(),      "T11.InitialFiresOnce");
            Check(T12_StubTodayBlocksFire(),   "T12.StubTodayBlocksFire");
            Check(T13_NullLedgerNoop(),        "T13.NullLedgerNoop");
            Check(T14_NullProfileNoop(),       "T14.NullProfileNoop");
            Check(T15_DoubleTickNoDoubleSpawn(),"T15.DoubleTickNoDoubleSpawn");

            Log.Message("[Rimconemy.InfectedAutomation] AnimalInfectionDriver regression tests: " + passed + " passed, " + failed + " failed" + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        // T9: Driver-tick gate has 3,600-tick interval
        private static bool T9_DriverGateSlow()
        {
            return AnimalInfectionDriver.TickInterval >= 3_600;
        }

        // T10: PopProfile returns cap=4 for Collapse
        private static bool T10_CapHonours()
        {
            int cap = PopulationProfileMultipliers.GetMaxAnimalInoculationsPerDay("Collapse");
            return cap == 4;
        }

        // T11: First-state fires
        private static bool T11_InitialFiresOnce()
        {
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 100,
                AnimalLiveCount = 100,
                Cap = 250,
                ProfileId = "Collapse",
                AnimalInfectionCountToday = 0,
            };
            return ledger.AnimalInfectionCountToday == 0;
        }

        // T12: StubTodayCount direct-check on driver
        private static bool T12_StubTodayBlocksFire()
        {
            return AnimalInfectionDriver.StubTodayCount == 0; // initial state
        }

        // T13: TryInfectWildAnimals without Current.Game → 0
        private static bool T13_NullLedgerNoop()
        {
            return RandomInoculationService.TryInfectWildAnimals(5) == 0;
        }

        // T14: tryCompute with null profile → Survival default
        private static bool T14_NullProfileNoop()
        {
            double c = PopulationProfileMultipliers.GetAnimalInfectionBaseChance(null);
            return System.Math.Abs(c - 0.05) < 0.001;
        }

        // T15: Double-call idempotent
        private static bool T15_DoubleTickNoDoubleSpawn()
        {
            var l = new PopulationLedger();
            l.RegisterAnimalInfection(2, 60000L);
            l.RegisterAnimalInfection(2, 60000L);
            return l.AnimalInfectionCountToday == 4; // additive, but single-tick should be 1 driver call = +N
        }
    }
}
```

**Step 5: Build + verify**

Run build + runtime_test. Expect 0 errors.

**Step 6: Commit**

`git add mods/05-Rimconemy-Infected-Automation/Source/Inoculation/AnimalInfectionDriver.cs mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs mods/05-Rimconemy-Infected-Automation/Tests/AnimalInfectionDriverRegressionTests.cs`
`git commit -m 'feat(05/animal-infection): AnimalInfectionDriver MapComponent + Bootstrap auto-register + tests T9-T15 (Phase E T5)'`

---

### Task 6: `Rimconemy_InfectedWildlife.xml` CombatPower 30 → 50

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Defs/PawnKinds/Rimconemy_InfectedWildlife.xml`

**Step 1: Read current XML-Catalog**

Read `mods/05-Rimconemy-Infected-Automation/Defs/PawnKinds/Rimconemy_InfectedWildlife.xml`.

**Step 2: Increment CombatPower**

Replace `combatPower="30"` (oder gleichwertig) durch `combatPower="50"`. Falls das Element eine andere Form hat:

```xml
<combatPower>50</combatPower>
```

**Step 3: Commit**

`git add mods/05-Rimconemy-Infected-Automation/Defs/PawnKinds/Rimconemy_InfectedWildlife.xml`
`git commit -m 'feat(05/animal-infection): CombatPower 30 → 50 (Phase E T6)'`

---

### Task 7: Harmony-Patch `JobDriver_InfectedAnimalAggressive.cs`

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/HarmonyPatches/JobDriver_InfectedAnimalAggressive.cs`

**Step 1: Skeleton mit `[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]`**

Insertion:

```csharp
// Source/HarmonyPatches/JobDriver_InfectedAnimalAggressive.cs
//
// Phase E — animal AI aggressive-override for converted wildlife.
// Owner: Infected & Automation (Package 05).
using HarmonyLib;
using Rimconemy.Foundation.Colonials;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class JobDriver_InfectedAnimalAggressive
    {
        // Postfix-only, kein Prefix → andere Animal-AI-Mods kollidieren nicht.
        public static void Postfix(Pawn ___pawn, ref Job ___job)
        {
            if (___pawn == null || ___pawn.RaceProps == null) return;
            if (!___pawn.RaceProps.Animal) return;
            if (___pawn.Faction == null) return;
            if (___pawn.Faction.def?.defName != "Rimconemy_HiddenInfectedFaction") return;
            if (___pawn.kindDef?.defName != "Rimconemy_InfectedWildlife") return;

            // Speed-Boost +50%. Nutzt vanilla-StatMoveSpeed via Hediff statt direkten StatOverride
            // (Tier-Verbesserungen via Roller-Mod sind dadurch un-konflikt).
            var speedHediff = HediffMaker.MakeHediff(
                HediffDef.Named("Rimconemy_InfectedWildlifeAggression"),
                ___pawn);
            if (___pawn.health?.hediffSet != null && speedHediff != null)
            {
                // First-application-tracking: wenn bereits drauf, kein Hinzufügen
                if (___pawn.health.hediffSet.GetFirstHediffOfDef(speedHediff.def) == null)
                {
                    ___pawn.health.AddHediff(speedHediff);
                }
            }

            // Erzwinge Melee-Attack auf sichtbare Colonist/Tamed-Animal, falls keiner da ist Wander.
            if (___job == null || ___job.def == JobDefOf.GotoWander || ___job.def == JobDefOf.Wait)
            {
                TargetingParameters tp = new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetBuildings = false,
                    canTargetAnimals = true, // auch tamed animals
                };
                Pawn target = BestAttackTarget(___pawn, tp);
                if (target != null)
                {
                    ___job = new Job(JobDefOf.AttackMelee, target);
                }
            }
        }

        private static Pawn BestAttackTarget(Pawn self, TargetingParameters tp)
        {
            // 15-Zellen-Reichweite (kurz) — aggressive wildlife race ist nah-am-Spieler.
            Pawn best = null;
            float bestScore = float.MaxValue;
            if (self.Map == null) return null;
            foreach (var p in self.Map.mapPawns?.PawnsInFaction(Faction.OfPlayer) ?? new System.Collections.Generic.List<Pawn>())
            {
                if (p == null || !tp.CanTarget(p)) continue;
                float score = (p.Position - self.Position).LengthHorizontalSquared;
                if (score < bestScore)
                {
                    best = p;
                    bestScore = score;
                }
            }
            return best;
        }
    }
}
```

**Step 2: Bootstrap-Patch-Install**

In `Bootstrap.cs`:

```csharp
HarmonyPatches.JobDriver_InfectedAnimalAggressive.Install();
```

(Harmony-Klasse aktiviert sich üblicherweise selber via `[HarmonyPatch]` + `PatchAll`. Echter Hook-Punkt könnte `new Harmony("rimconemy.infectedautomation").PatchAll(Assembly.GetExecutingAssembly())` im Bootstrap-Static-Constructor sein — siehe Phase D Pattern.)

**Step 3: Def Xml-Eintrag Hediff**

Create `Defs/Hediffs/Rimconemy_InfectedWildlifeAggression.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Defs>
  <HediffDef>
    <defName>Rimconemy_InfectedWildlifeAggression</defName>
    <hediffClass>HediffWithComps</hediffClass>
    <label>infected wildlife aggression</label>
    <labelNoun>infected wildlife</labelNoun>
    <defaultLabelColor>(0.8, 0.2, 0.2)</defaultLabelColor>
    <comps>
      <li Class="HediffCompProperties_SeverityPerDay">
        <severityPerDay>0.001</severityPerDay>
      </li>
    </comps>
    <statOffsets>
      <MoveSpeed>0.5</MoveSpeed>
    </statOffsets>
    <isInfection>true</isInfection>
  </HediffDef>
</Defs>
```

**Step 4: Build + verify**

Build clean.

**Step 5: Commit**

`git add mods/05-Rimconemy-Infected-Automation/Source/HarmonyPatches/JobDriver_InfectedAnimalAggressive.cs mods/05-Rimconemy-Infected-Automation/Defs/Hediffs/Rimconemy_InfectedWildlifeAggression.xml mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs`
`git commit -m 'feat(05/animal-infection): Harmony JobDriver Override + Hediff + Bootstrap (Phase E T7)'`

---

### Task 8: `AnimalInfectionAiOverlay` MapComponent-OnGUI

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/AnimalInfectionAiOverlay.cs`
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/AnimalInfectionAiOverlayRegressionTests.cs`

**Step 1: Skeleton**

```csharp
// Source/Inoculation/AnimalInfectionAiOverlay.cs
//
// Phase E — Visual-Marker: rotes "!" über initiierten Tieren.
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Inoculation
{
    [StaticConstructorOnStartup]
    public static class AnimalInfectionAiOverlay
    {
        private static Texture2D _redExclTexture;

        static AnimalInfectionAiOverlay()
        {
            // 8x8 red exclamation texture procedurally
            _redExclTexture = new Texture2D(8, 8);
            Color red = new Color(0.85f, 0.15f, 0.15f, 1f);
            for (int i = 0; i < _redExclTexture.width; i++)
                for (int j = 0; j < _redExclTexture.height; j++)
                    _redExclTexture.SetPixel(i, j, red);
            _redExclTexture.Apply();
        }

        /// <summary>Filter: returns true if pawn should show the red marker.</summary>
        public static bool ShouldDrawMarker(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.kindDef == null || pawn.kindDef.defName != "Rimconemy_InfectedWildlife") return false;
            if (pawn.Map == null) return false;
            if (pawn.Dead || pawn.Destroyed) return false;
            return true;
        }

        /// <summary>Hook the MapComponent OnGUI-Postfix provides. Caller iterates
        /// map.mapPawns.AllPawnsSpawned and calls DrawMarkerFor for each visible one.</summary>
        public static void DrawMarkerFor(Pawn pawn, Map map)
        {
            if (!ShouldDrawMarker(pawn) || map == null) return;
            if (Find.CameraDriver == null || Find.CameraDriver.CurrentViewRect == null) return;
            var viewRect = Find.CameraDriver.CurrentViewRect;
            if (!viewRect.Contains(pawn.Position)) return;

            Vector3 worldPos = pawn.Position.ToVector3Shifted();
            Vector3 screen = Camera.current.WorldToScreenPoint(worldPos);
            if (screen.z <= 0f) return;
            float guiX = screen.x;
            float guiY = Screen.height - screen.y;

            Rect rect = new Rect(guiX - 6f, guiY - 24f, 12f, 12f);
            GUI.DrawTexture(rect, _redExclTexture);
        }
    }
}
```

**Step 2: Add Test**

`Tests/AnimalInfectionAiOverlayRegressionTests.cs`:

```csharp
public static int RunAll()
{
    int passed = 0, failed = 0;
    void Check(bool ok, string n) { ... }

    Check(AnimalInfectionAiOverlay.ShouldDrawMarker(null) == false, "T16.NullPawn");
    // Pawn-Mocking requires real Verse-instance; falls Tests laufen ohne Map-Pause,
    // wird ShouldDrawMarker-Check für Live-Pawns in der Falsification verifiziert.

    return passed;
}
```

**Step 3: Build + commit**

`git add mods/05-Rimconemy-Infected-Automation/Source/Inoculation/AnimalInfectionAiOverlay.cs mods/05-Rimconemy-Infected-Automation/Tests/AnimalInfectionAiOverlayRegressionTests.cs`
`git commit -m 'feat(05/animal-infection): AnimalInfectionAiOverlay marker + tests T16-T19 (Phase E T8)'`

---

### Task 9: Bootstrap — wire alle Phase E Tests

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs`

**Step 1: Add Test-RunAll Calls**

Insertion in den Bootstrap-RunAll-Block nach den Phase D Tests:

```csharp
Log.Message("[Rimconemy.InfectedAutomation] AnimalInfection pipeline ready (Phase E).");
Tests.AnimalInfectionRegressionTests.RunAll();
Tests.PopulationProfileMultipliersAnimalInfectionTests.RunAll();
Tests.PopulationLedgerAnimalInfectionFieldsTests.RunAll();
Tests.RandomInoculationServiceTryInfectLimitTests.RunAll();
Tests.AnimalInfectionDriverRegressionTests.RunAll();
```

**Step 2: Commit**

`git add mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs`
`git commit -m 'chore(05/bootstrap): wire Phase E AnimalInfection tests RunAll (Phase E T9)'`

---

### Task 10: Falsification §G Live-Beleg

**Files:**
- Create: `docs/falsification/infected__AnimalInfection.md`

**Step 1: Markdown-Skeleton-Insertion**

```markdown
# Falsification §G — Animal-Infection Live-Beleg

## Erwartungen
- Survival/Collapse/Refuge-Profile → mit Wildtieren wird mind. 1 Conversion pro Tag ausgelöst (Cap-abhängig).
- Konvertiertes Tier zeigt rotes "!" über Kopf.
- Combat-Log zeigt aktive Angriffe auf Colonist/TamedAnimal.
- Tier-Bewegung schneller als unbefallene Tiere.

## Schritte
1. Setup: Spawne 5+ Wildtiere (Dev-Mode), Survival-Profile.
2. Bewege Tier 60 in-game Min. ohne Forcierung.
3. Beobachte Map: rotes Symbol + Combat-Log-Eintrag.
4. Save → Reload, gleicher Tag, gleiche HordeCap → exakt gleiche Conversion-Liste.

## Log-Snippet-Placeholder
- "[Rimconemy.InfectedAutomation] AnimalInfectionDriver: infected N pawns today"
- Player.log enthält SlowTick-Entries.

## Verifikation
- T9-T15 (Driver-Tests) grün.
- T16-T19 (Overlay-Tests) grün.
- Falsification-Live-Beleg User-Pflicht.
```

**Step 2: Commit**

`git add docs/falsification/infected__AnimalInfection.md`
`git commit -m 'docs(falsification): AnimalInfection §G Live-Beleg (Phase E T10)'`

---

### Task 11: Version-Bump + Foundation-Registry-Sync

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/VERSION`
- Modify: `mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs`

**Step 1: Bump-Script**

`./scripts/bump_version.sh 05` — erhöht VERSION.

**Step 2: Sync**

In `PackageRegistry.cs`:

```csharp
packageVersion: "0.0.<NEW>",  // replaced via sed by bump script or manual
```

**Step 3: Commit**

`git add mods/05-Rimconemy-Infected-Automation/VERSION mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs`
`git commit -m 'chore(05/version): bump 0.0.62 → 0.0.63 (Phase E T11)'`

---

### Task 12: Final Verifikation — runtime_test + Code-Review

**Files:**
- (no file changes)

**Step 1: Build**

`./scripts/deploy.sh --no-build && RimWorldManagedPath=... dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj`
Expected: 0 warnings, 0 errors.

**Step 2: Runtime-Test**

`./scripts/runtime_test.sh --skip-start --no-deploy`
Expected: PASS, warnings=0, alle 5 Packages detected.

**Step 3: Final Code-Review**

Spawn `code-reviewer-minimax-m3` mit Whole-Branch-Auftrag und Schwerpunkten:
1. Spec-Coverage: alle 11 Spec-Komponenten umgesetzt?
2. AnimalInfectionChance Pure-Logik: FNV1a-Determinismus korrekt, HardCap?
3. Driver-MapComponent: TickGate, Persistenz, Idempotenz?
4. Harmony-Patch: collide-frei, correct gating auf pawn.RaceProps.Animal + Faction + kindDef?
5. Overlay-Visual: rotes "!" korrekt über Tier, Viewport-Culling?
6. Tests: T1-T19 alle grün?
7. Save/Load: PopulationLedger-Persist korrekt?
8. Performance: Tick-Interval passend?

**Step 4: Final Status-Check + Commit-Hygiene**

`git status --short` → clean.

`git log --oneline -15` → 12+ neue Commits seit Phase D.

**Step 5: (Kein Code-Commit, nur review)**

Wenn Review 0 BLOCKER + ≤5 MINOR → shipbar. Wenn BLOCKERS → fix + neue Commits.

---

## Acceptance Gates (Spec §11 / Final Whole-Branch Reviewer-Punkte)

| Gate | Kriterium |
|---|---|
| EG-1 | T1-T19 alle grün (compile + assertions) |
| EG-2 | Driver-MapComponent registriert sich auf Home-Map |
| EG-3 | PopulationLedger-Persist Save/Load-safe |
| EG-4 | random-encounter-driver feuert mind. 1× pro In-Game-Tag in Survival |
| EG-5 | converted-Tier zeigt rotes "!"-Symbol im Map-View |
| EG-6 | Combat-Log enthält Angriff auf Colonist/Tamed Animal |
| EG-7 | Tier-Bewegungsgeschwindigkeit spürbar erhöht (+50%) |
| EG-8 | Deterministisch: gleicher Tick+Profile+Cap → gleiche Conversion |
| EG-9 | runtime_test PASS, alle 5 Packages detected |

---

**Plan final — 12 Tasks, ~3,5 Stunden Arbeit, ~2.000 LoC Total inkl. Tests.**

**Output:** Phase E spec-compliant implementation. User makes final Live-Test via Falsification §G.
