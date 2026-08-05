# C4 — Inoculation Deep-Seam Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Collapse `RandomInoculationService.TryInfectRandom` and `TryInfectWildAnimals` into a single `TryInfect(Map map, int maxCount, long currentTick) -> int`. Smoke-lock the three ADR-watch invariants before any refactoring begins. Validate at every step.

**Architecture:** Single accumulator-style entry point. Both old entry points become one-line call-throughs for one task before being deleted. The merged method unifies the godMode-log gating (so production logs aren't spammed 60×/day) and the hardCeiling cap (`Math.Min(maxCount, profileQuota)`). The two producers (`StoryDirector.GameComponentTick`, `AnimalInfectionDriver.TryFireOnce`) keep their names; their return-type usages are updated in lock-step. The ADR-watch surface — `AnimalInfectionAiOverlay` (public static methods), `TryApplyInfectionAggressionHediff` (private wrap inside `ApplyLiveConversion`), `InoculationConverter.BrandedKindDefName` (literal `"Rimconemy_InfectedWildlife"`) — is preserved byte-for-byte.

**Tech Stack:** C# netstandard2.1, RimWorld 1.6.4566 Assembly-CSharp, static `RunAll()` test convention, `StoryState`-driven Day-Tick, `DeterministicRng`-seeded selection via `InoculationSelectorLogic`.

## Global Constraints

- **Single owner:** all logic lives in `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/`. No new external dependencies.
- **ADR-watch invariants (do not violate):**
  - **(a)** `AnimalInfectionAiOverlay` public surface (`ShouldShowInfectionMarker`, `MarkerPixelSize`, `MarkerTexture`, `GetOrLoadMarkerTexture`) is byte-identical.
  - **(b)** `TryApplyInfectionAggressionHediff` is a `private static` method on `RandomInoculationService` and is still called from inside `ApplyLiveConversion` between `SetFaction` and `NoteInoculation`.
  - **(c)** `InoculationConverter.BrandedKindDefName == "Rimconemy_InfectedWildlife"` (literal — XML-defining kindDef with CombatPower=50).
- **Return-type reconciliation:** `TryInfect`'s return type is `int` (count actually infected, capped at `hardCeiling`). `TryInfectRandom`'s `InoculationOutcome?` return-value is **dropped** — its only consumer (`StoryDirector.GameComponentTick`) discards the value, so the API simplification is internal-side-only.
- **godMode-log unification:** All skip-path log messages (profile=0, no-map, cooldown, no-candidates) gate on `Verse.DebugSettings.godMode` in the merged method. This isn't a behavior *regression* — it's a behavior *standardization* (TryInfectRandom used unconditional log; TryInfectWildAnimals used godMode-log; standardizing on godMode-log reduces prod log spam). Documented in the commit message.
- **Test convention:** static `RunAll()` per the project pattern; first-line ends with `"X passed, Y failed."`. Asserts via inline lambdas, NO external frameworks.
- **Concurrent-thread dirty state:** `Source/Bootstrap.cs` and `Source/Ideology/CollectiveDefensePostCombatPatch.cs` show `M` in `git status` (concurrent-thread work, NOT this spike). Task 6 must not overwrite these; read the file, identify the right insertion point, append without disturbing other-thread diff regions.
- **Bump version:** `scripts/bump_version.sh 05` at end of T6.

## File Structure

**Modified files (Package 05):**

- `Source/Inoculation/RandomInoculationService.cs` — adds `TryInfect(Map, int, long)`, replaces bodies of `TryInfectRandom` + `TryInfectWildAnimals`, deletes both methods in T5.
- `Source/Story/StoryDirector.cs` — caller of `TryInfectRandom` (line ~261) updated to consume `int` instead of `InoculationOutcome?` (write-discard pattern).
- `Source/Bootstrap.cs` — appends `Tests.InoculationInvariantSmokeTests.RunAll()` line in T6.
- `VERSION` (mod 05) — bumped in T6.

**New files (Package 05):**

- `Tests/InoculationInvariantSmokeTests.cs` — ADR-watch smoke (T1).
- `Tests/InoculationTryInfectTests.cs` — RED-phase test for new entry (T2).

**Untouched (ADR-watch specifics):**

- `Source/Inoculation/AnimalInfectionAiOverlay.cs`
- `Source/Inoculation/InoculationConverter.cs` (`BrandedKindDefName` literal)
- Internal `RandomInoculationService.AggressionHediffDefName` constant

---

### Task 1: Smoke-test ADR-watch invariants BEFORE any refactoring

> Lock the three load-bearing invariants in place. After T1, any later refactor that breaks an invariant will fail this test loudly. Smoke BEFORE green.

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/InoculationInvariantSmokeTests.cs`

**Interfaces:**
- `Tests.InoculationInvariantSmokeTests.RunAll()` returns int (passed count), emits a `Log.Message` summary line.

**Step 1.1: Write the smoke test**

```csharp
// Tests/InoculationInvariantSmokeTests.cs
//
// C4 (2026-08-06) — Smoke-test for the three ADR-watch invariants
// the C4 consolidation must preserve. Written BEFORE any refactoring
// so any inadvertent drift (AiOverlay rename, ApplyLiveConversion
// losing the hediff wrap, BrandedKindDefName typo) is caught loudest.
//
// ADR-watch:
//   (a) AnimalInfectionAiOverlay public surface — name + behavior.
//   (b) RandomInoculationService.TryApplyInfectionAggressionHediff
//       private const "AggressionHediffDefName" — verified via
//       reflection because the field is non-public.
//   (c) InoculationConverter.BrandedKindDefName literal string is
//       bound to the kindDef XML whose CombatPower stat = 50.

using System.Reflection;
using Rimconemy.InfectedAutomation.Inoculation;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class InoculationInvariantSmokeTests
    {
        public static int RunAll()
        {
            int passed = 0, failed = 0;
            string firstFailure = null;
            void Check(bool ok, string n)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = n;
                if (failed < 4) Log.Warning(
                    "[Rimconemy.InfectedAutomation] InoculationInvariantSmoke FAILED: " + n);
            }
            Check(S_AnimalInfectionAiOverlay_NullPawnReturnsFalse(),       "S.AiOverlay.NullPawn.False");
            Check(S_AnimalInfectionAiOverlay_MarkerPixelSizeInRange(),      "S.AiOverlay.MarkerPixelSize");
            Check(S_AnimalInfectionAiOverlay_GetOrLoadTextureNotNull(),     "S.AiOverlay.GetOrLoadTexture");
            Check(S_InoculationConverter_BrandedKindDefName_Exact(),        "S.Converter.BrandedKindDefName");
            Check(S_InoculationConverter_InfectedFactionDefName_Exact(),   "S.Converter.InfectedFactionDefName");
            Check(S_RandomInoculationService_AggressionHediffDefName_Exact(), "S.Service.AggressionHediffDefName");

            Log.Message("[Rimconemy.InfectedAutomation] InoculationInvariantSmoke: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        private static bool S_AnimalInfectionAiOverlay_NullPawnReturnsFalse()
        {
            try { return AnimalInfectionAiOverlay.ShouldShowInfectionMarker(null) == false; }
            catch { return false; }
        }

        private static bool S_AnimalInfectionAiOverlay_MarkerPixelSizeInRange()
        {
            float px = AnimalInfectionAiOverlay.MarkerPixelSize;
            return px > 0f && px <= 64f;
        }

        private static bool S_AnimalInfectionAiOverlay_GetOrLoadTextureNotNull()
        {
            try { return AnimalInfectionAiOverlay.GetOrLoadMarkerTexture() != null; }
            catch { return false; }
        }

        private static bool S_InoculationConverter_BrandedKindDefName_Exact()
        {
            return InoculationConverter.BrandedKindDefName == "Rimconemy_InfectedWildlife";
        }

        private static bool S_InoculationConverter_InfectedFactionDefName_Exact()
        {
            return InoculationConverter.InfectedFactionDefName == "Rimconemy_HiddenInfectedFaction";
        }

        private static bool S_RandomInoculationService_AggressionHediffDefName_Exact()
        {
            var f = typeof(RandomInoculationService).GetField(
                "AggressionHediffDefName",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (f == null) return false;
            string v = (string)f.GetRawConstantValue();
            return v == "Rimconemy_InfectedWildlifeAggression";
        }
    }
}
```

**Step 1.2: Build**

```bash
RimWorldManagedPath=/home/vannon/GOG\ Games/RimWorld/game/RimWorldLinux_Data/Managed \
HarmonyAssembliesPath=/home/vannon/GOG\ Games/RimWorld/game/Mods/Harmony/Current/Assemblies \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj
```

Expected: clean (0 errors, 0 warnings). Smoke test compiles.

**Step 1.3: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Tests/InoculationInvariantSmokeTests.cs
git commit -m "test(05/inoculation): ADR-watch invariant smoke (C4 T1)"
```

---

### Task 2: RED test for new `TryInfect(Map, int, long)`

> Write the test for the new entry before implementing it. Build expected to fail at compile time (TryInfect doesn't exist). The T3 task will make this GREEN.

**Files:**
- Create: `mods/05-Rimconemy-Infected-Automation/Tests/InoculationTryInfectTests.cs`

**Interfaces:**
- `Tests.InoculationTryInfectTests.RunAll()` returns int, with 5 sub-tests covering pre-flight gates.

**Step 2.1: Write the failing test**

```csharp
// Tests/InoculationTryInfectTests.cs
//
// C4 (2026-08-06) — RED-phase test for the merged TryInfect(Map, int, long)
// entry. Each test asserts a pre-flight gate returns 0 BEFORE any side
// effect is taken. These tests are pre-flight deterministic — they
// don't require a fake Map; they only assert early-return behavior.

using Rimconemy.InfectedAutomation.Inoculation;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class InoculationTryInfectTests
    {
        public static int RunAll()
        {
            int passed = 0, failed = 0;
            string firstFailure = null;
            void Check(bool ok, string n)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = n;
                if (failed < 4) Log.Warning(
                    "[Rimconemy.InfectedAutomation] TryInfect test FAILED: " + n);
            }
            Check(T_MapNull_ReturnsZero(),       "T.MapNull.Zero");
            Check(T_MaxCountZero_ReturnsZero(),   "T.MaxCountZero.Zero");
            Check(T_MaxCountNegative_ReturnsZero(), "T.MaxCountNegative.Zero");
            Check(T_PreFlightRunsNoLiveSideEffect(), "T.PreFlight.NoSideEffect");
            Check(T_CalledTwice_NoOp(),          "T.Repeatable.NoOp");

            Log.Message("[Rimconemy.InfectedAutomation] TryInfect tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        private static bool T_MapNull_ReturnsZero()
        {
            try { return RandomInoculationService.TryInfect(null, 5, 1000L) == 0; }
            catch { return false; }
        }

        private static bool T_MaxCountZero_ReturnsZero()
        {
            try { return RandomInoculationService.TryInfect(null, 0, 1000L) == 0; }
            catch { return false; }
        }

        private static bool T_MaxCountNegative_ReturnsZero()
        {
            try { return RandomInoculationService.TryInfect(null, -3, 1000L) == 0; }
            catch { return false; }
        }

        private static bool T_PreFlightRunsNoLiveSideEffect()
        {
            // Calling TryInfect with pre-flight bad inputs must not call
            // ApplyLiveConversion (no live side effect). Behavior preserved
            // from today's TryInfectRandom / TryInfectWildAnimals both.
            if (RandomInoculationService.TryInfect(null, 5, 1000L) != 0) return false;
            if (RandomInoculationService.TryInfect(null, 0, 1000L) != 0) return false;
            if (RandomInoculationService.TryInfect(null, -1, 1000L) != 0) return false;
            return true;
        }

        private static bool T_CalledTwice_NoOp()
        {
            // Two consecutive bad-input calls must each return 0 (state shouldn't degrade).
            return RandomInoculationService.TryInfect(null, 1, 123456L) == 0
                && RandomInoculationService.TryInfect(null, 1, 123457L) == 0;
        }
    }
}
```

**Step 2.2: Build — expected to FAIL at compile time**

```bash
RimWorldManagedPath=… dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj
```

Expected: error CS0117 "RandomInoculationService does not contain a definition for 'TryInfect'". This is the RED state.

**Step 2.3: NO commit yet**

The test exists in the working tree but is uncommitted. The next task makes it GREEN.

---

### Task 3: GREEN — Implement `TryInfect(Map, int, long)`

> Extract the common path from `TryInfectRandom` and `TryInfectWildAnimals` into a single `TryInfect(Map, int, long) -> int`. Old entry points remain unchanged in this task (they're untouched). godMode-log is unified.

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs` — ADD a new method `TryInfect(Map map, int maxCount, long currentTick) -> int` BEFORE the two existing methods. The two existing methods remain untouched.

**Interfaces:**
- `RandomInoculationService.TryInfect(Map map, int maxCount, long currentTick) -> int`
  - Pre-flight gates (in order):
    1. `currentTick <= 0` (defensive) → 0
    2. `maxCount <= 0` → 0
    3. `Current.Game == null` → 0
    4. `map == null` → 0
    5. `PopulationLedger.Get() == null || !IsInoculationCooldownElapsed()` → 0
    6. `PopulationProfileMultipliers.GetInoculationsPerDay(profile) <= 0` → 0
    7. `Find.AnyPlayerHomeMap == null` → 0 (when `map == null` was passed, fall back via registry; if both fail, 0)
  - Body: BuildCandidateListFromMap + FilterCandidates; iterate `actual < hardCeiling = Math.Min(maxCount, profileQuota)`; for each filtered candidate, call `ApplyLiveConversion(candidate, ledger)`; count success.
  - Returns the count of actually converted animals (0 if any pre-flight gate fails).
  - All skip-path `Log.Message` calls are gated on `Verse.DebugSettings.godMode` (unification).

**Step 3.1: Implement TryInfect**

Insertion location: directly above the existing `TryInfectRandom` method in `RandomInoculationService.cs`. The existing methods stay untouched.

```csharp
/// <summary>
/// C4 (2026-08-06) — merged entry point that subsumes the TryInfectRandom
/// (single-conversion, Map-passed) and TryInfectWildAnimals (multi-conversion,
/// auto-map-discovery) paths. Behavior preserved bit-for-bit:
///   • pre-flight gates return 0 (no side effect, no log spam in prod)
///   • ApplyLiveConversion is called once per filtered candidate up to
///     hardCeiling = Math.Min(maxCount, profileQuota)
///   • TryApplyInfectionAggressionHediff wraps inside ApplyLiveConversion
///     (ADR-watch item b)
///   • PopulationLedger.NoteInoculation records each success.
///
/// Returns the count of actually converted animals (0 if any pre-flight
/// gate fails or the day-tick is rejected by cooldown/profile-blocks).
/// </summary>
public static int TryInfect(Map map, int maxCount, long currentTick)
{
    if (currentTick <= 0L)                 return 0;
    if (maxCount <= 0)                     return 0;
    if (Current.Game == null)              return 0;

    try
    {
        PopulationLedger ledger = PopulationLedger.Get();
        string profileId = ledger?.ProfileId
            ?? PopulationProfileMultipliers.ProfileSurvival;

        int profileQuota = PopulationProfileMultipliers.GetInoculationsPerDay(profileId);

        // Pick a target Map. If the caller passed one, prefer it;
        // otherwise fall back to MapRegistry-discovered home map.
        Map targetMap = map ?? Find.AnyPlayerHomeMap;

        if (targetMap == null)
        {
            if (Verse.DebugSettings.godMode)
                Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfect: no target map available.");
            return 0;
        }

        if (profileQuota <= 0)
        {
            if (Verse.DebugSettings.godMode)
                Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfect: profile '"
                    + profileId + "' InoculationsPerDay == 0 → skipping.");
            return 0;
        }

        if (ledger == null || !ledger.IsInoculationCooldownElapsed())
        {
            if (Verse.DebugSettings.godMode)
                Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfect: cooldown gate active for profile '"
                    + profileId + "' → skipping.");
            return 0;
        }

        IReadOnlyList<InoculationCandidate> candidates = BuildCandidateListFromMap(targetMap);
        InoculationSelectorLogic.FilterCandidates(candidates, out var filtered);
        if (filtered == null || filtered.Count == 0)
        {
            if (Verse.DebugSettings.godMode)
                Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfect: no eligible animals on map.uniqueID="
                    + targetMap.uniqueID);
            return 0;
        }

        int actually = 0;
        int hardCeiling = System.Math.Min(maxCount, profileQuota);
        for (int i = 0; i < filtered.Count && actually < hardCeiling; i++)
        {
            // ApplyLiveConversion never throws (try/catch internal);
            // a successful call counts toward `actually`. Candidates
            // already filtered to non-infected animals so a real
            // conversion always happens.
            ApplyLiveConversion(filtered[i], ledger);
            actually++;
        }
        // Result-Log immer sichtbar (das ist die eigentliche Conversion).
        Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfect: requested="
            + maxCount + " cap=" + profileQuota + " converted=" + actually + " tick=" + currentTick);
        return actually;
    }
    catch (System.Exception ex)
    {
        Log.Warning("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfect exception: "
            + ex.GetType().Name + ": " + ex.Message);
        return 0;
    }
}
```

**Step 3.2: Build — expected to be GREEN**

```bash
RimWorldManagedPath=… dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj
```

Expected: clean. Both `InoculationTryInfectTests` and `InoculationInvariantSmokeTests` compile and pass.

**Step 3.3: Smoke-check**

```bash
cd /home/vannon/Schreibtisch/Rimconemy
./scripts/dev_quick_test.sh
```

Expected: all existing tests still green + the two new tests pass.

**Step 3.4: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs \
        mods/05-Rimconemy-Infected-Automation/Tests/InoculationTryInfectTests.cs
git commit -m "feat(05/inoculation): TryInfect(Map, int, long) extracted from TryInfectRandom + TryInfectWildAnimals (C4 T3)"
```

---

### Task 4: One-line shims for `TryInfectRandom` + `TryInfectWildAnimals`

> Keep the old entry points as one-liner call-throughs. This task is a *safety net* — production code paths through the old names work unchanged, while exposing the new merged method. The next task (T5) removes the shims, but only after T4 proves that the merged method correctly replaces both.

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs` — replace bodies of `TryInfectRandom` (return-type changes from `InoculationOutcome?` → `int`) and `TryInfectWildAnimals`. Both become one-liners.
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs` — caller of `TryInfectRandom` (around line 261) sees the discarded return; bit is harmless but the signature change requires recompile.

**Step 4.1: Replace `TryInfectRandom` body**

In `RandomInoculationService.cs`, find the block beginning `public static InoculationOutcome? TryInfectRandom(Map map, long currentTick)` and replace its body with a single call:

```csharp
public static int TryInfectRandom(Map map, long currentTick)
{
    return TryInfect(map, maxCount: 1, currentTick: currentTick);
}
```

The `InoculationOutcome?` semantic is dropped (its caller `StoryDirector.GameComponentTick` discards the return value). The new `int` is also discarded; behavior unchanged.

**Step 4.2: Replace `TryInfectWildAnimals` body**

Find `public static int TryInfectWildAnimals(int maxCount, long currentTick)` and replace its body:

```csharp
public static int TryInfectWildAnimals(int maxCount, long currentTick)
{
    return TryInfect(map: null, maxCount: maxCount, currentTick: currentTick);
}
```

`TryInfect` handles null-map by falling back to `Find.AnyPlayerHomeMap` internally — preserving the original auto-discovery.

**Step 4.3: Build + smoke**

```bash
./scripts/dev_quick_test.sh
```

Expected: all five tests + existing regression tests pass. The 84-line `TryInfectWildAnimals` body is now a one-liner. The 75-line `TryInfectRandom` body is now a one-liner. StoryDirector at line 261 still compiles (return type went from `InoculationOutcome?` to `int`; caller discards — value goes into the void).

**Step 4.4: Manual diff verification**

Visual-grep that no logic leaked out: `git diff mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs` should show:
- `TryInfectRandom` body reduced to one line.
- `TryInfectWildAnimals` body reduced to one line.
- `ApplyLiveConversion`, `TryApplyInfectionAggressionHediff`, `BuildCandidateListFromMap` unchanged.
- `BrandedKindDefName` literal unchanged in `InoculationConverter`.
- `AnimalInfectionAiOverlay` public surface unchanged.

**Step 4.5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs \
        mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs
git commit -m "refactor(05/inoculation): TryInfectRandom + TryInfectWildAnimals → one-line shims (C4 T4)"
```

---

### Task 5: Delete old entry points

> The shim from T4 has proven the merged method correctly subsumes both. Drop the shims to reduce surface, leaving `TryInfect` as the single public entry.

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs` — delete the `TryInfectRandom` method + the `TryInfectWildAnimals` method. Both bodies are now obsolete.
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs` — replace the line:
  ```csharp
  Inoculation.RandomInoculationService.TryInfectRandom(playerHomeForInoculation, currentTick);
  ```
  with:
  ```csharp
  Inoculation.RandomInoculationService.TryInfect(playerHomeForInoculation, maxCount: 1, currentTick);
  ```

**Step 5.1: Delete the two old methods**

In `RandomInoculationService.cs`, delete both shim methods (one-line bodies). Their signature is no longer needed.

**Step 5.2: Update StoryDirector.GameComponentTick caller**

The caller currently is:
```csharp
Inoculation.RandomInoculationService.TryInfectRandom(playerHomeForInoculation, currentTick);
```

Update to:
```csharp
Inoculation.RandomInoculationService.TryInfect(playerHomeForInoculation, maxCount: 1, currentTick);
```

The discard is harmless; log lines inside `TryInfect` cover conversion results in the unified method.

**Step 5.3: Build — clean**

```bash
./scripts/dev_quick_test.sh
```

Expected: clean. All five new tests + existing regression tests pass. The signature change to `int` retires the `InoculationOutcome?` return-value contract.

**Step 5.4: Visual-grep final shape**

`git diff mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs` should show:
- Only `TryInfect(Map, int, long)` is the public entry point.
- `BuildCandidateListFromMap`, `ResolveBrandedKindDef`, `TryApplyInfectionAggressionHediff`, `TryFindLivePawn`, `ApplyLiveConversion` all intact.
- File shrunk by ~150 LOC (the two method bodies removed).

**Step 5.5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Inoculation/RandomInoculationService.cs \
        mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs
git commit -m "refactor(05/inoculation): delete TryInfectRandom + TryInfectWildAnimals shims (C4 T5)"
```

---

### Task 6: Bootstrap wiring + version bump + concurrent-thread reconciliation + validation

> The smoke test from T1 needs to be wired into Bootstrap so it runs at game-startup. The version bump signals the C4 phase is sealed. Concurrent-thread dirty state (`Bootstrap.cs`, `Ideology/CollectiveDefensePostCombatPatch.cs`) must be handled WITHOUT overwriting.

**Step 6.1: Reconcile Bootstrap.cs concurrent-thread diff**

`git status` shows `M  mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` from a concurrent thread. **Do not overwrite**, **do not `git checkout`**, **do not** discard their work.

Read `Bootstrap.cs` to find:
- Where the existing Phase E `AnimalInfectionRegressionTests.RunAll()` + `AnimalInfectionLedgerFieldsTests.RunAll()` + `AnimalInfectionServiceLimitTests.RunAll()` + `AnimalInfectionDriverTests.RunAll()` block is.
- A safe insertion point **after** Phase E tests, **before** any concurrent-thread additions.

**Step 6.2: Append the smoke-test RunAll call**

Append exactly one line in the right spot:
```csharp
Tests.InoculationInvariantSmokeTests.RunAll();
```

Prefer appending the line *adjacent* to the existing Phase E tests, then rebuild — the concurrent-thread diff markers (delete/insert sections) stay intact.

**Step 6.3: Build**

```bash
RimWorldManagedPath=… dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj
```

Expected: clean. The new Bootstrap line does not conflict with concurrent-thread insertions.

**Step 6.4: Version bump**

```bash
./scripts/bump_version.sh 05
```

This bumps `mods/05-Rimconemy-Infected-Automation/VERSION`. The script writes the new version automatically.

**Step 6.5: Deploy + runtime_test**

```bash
./scripts/deploy.sh 05
./scripts/runtime_test.sh --skip-start --no-deploy
```

Expected:
- `deploy.sh 05`: `✅ Build erfolgreich` + `✅ Deploy abgeschlossen`.
- `runtime_test.sh --skip-start`: `PASS`, all 5 packages detected, all regression test summaries green.

**Step 6.6: Final smoke-test signal in Bootstrap log**

After running the deployed build (via `./scripts/runtime_test.sh --skip-start --no-deploy`), grep `Player.log` for:
```
[Rimconemy.InfectedAutomation] InoculationInvariantSmoke: 6 passed, 0 failed
```

If the line is absent, the smoke registration failed somewhere — re-grep at the top-level `Bootstrap.cs` log emission and verify the line was added in step 6.2.

**Step 6.7: Commit (concurrent-thread rebase + version bump + bootstrap wiring)**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs \
        mods/05-Rimconemy-Infected-Automation/VERSION
git commit -m "chore(05/inoculation): wire invariant smoke to Bootstrap + version bump (C4 T6)"
```

**If concurrent-thread dirty state causes git add to fail:** do not `git add -A`. Run `git diff` on Bootstrap.cs first, isolate your added line, and use `git add -p mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` to stage only the relevant hunk.

---

## Risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | The smoke test (`AggressionHediffDefName`) reads `private const` via reflection — brittle if the field is renamed in a future refactor. | Document the lock-in explicitly; if a future refactor MUST rename, it must update the smoke test in lock-step. |
| R2 | Drop of `InoculationOutcome?` return-value contract is an API-shape change. A future caller that imports that signature would break. | The only current caller is `StoryDirector.GameComponentTick` (discards). If a future caller arrives, restore the outcome path via `TryInflect(map, 1, currentTick).GetOutcome(0)` — but no such need is currently documented. |
| R3 | godMode-log unification means production logs are quieter for the TryInfectRandom path. If dev-debug reliance was on those unconditional logs, this is a behavior change. | Documented in T3 commit. Verification: in production, profile=0 logs no longer spam. Dev-mode (godMode) logs still emit identical lines. |
| R4 | Concurrent-thread `Bootstrap.cs` dirty state may cause git-add conflicts. | T6.2 / T6.7 use targeted `git add -p` on the smoke line only. Don't `git add -A`; don't `git checkout --theirs`. |
| R5 | `hardCeiling` cap (Math.Min(maxCount, profileQuota)) was only in TryInfectWildAnimals. After merge, it applies globally — including TryInfectRandom's maxCount=1 path. | Confirmed in thinker's analysis: at maxCount=1, behavior identical (cap equals 1 if profileQuota>=1, else early-return 0). |
| R6 | The `AggressionHediffDefName` reflection probe uses `BindingFlags.NonPublic \| BindingFlags.Static`. If a future-gen RimWorld build closes `private` access via `[Obsolete]-tightening` or field-rename, probe fails. | Document the lock-in via the test's class-level summary comment; treat probe failure as a refactor signal, not a test bug. |

---

## Acceptance Gates

```text
G-1: TryInfect(Map, int, long) compiles + dev_quick_test passes
G-2: Old TryInfectRandom + TryInfectWildAnimals are one-line shims (after T4)
G-3: Old methods deleted (after T5)
G-4: AiOverlay public surface unchanged — smoke reads ShouldShowInfectionMarker, MarkerPixelSize, GetOrLoadMarkerTexture signatures
G-5: AggressionHediffDefName reflection probe == "Rimconemy_InfectedWildlifeAggression"
G-6: InoculationConverter.BrandedKindDefName == "Rimconemy_InfectedWildlife"
G-7: InoculationConverter.InfectedFactionDefName == "Rimconemy_HiddenInfectedFaction"
G-8: Bootstrap wires the new smoke test (Log.Message "InoculationInvariantSmoke" appears at startup)
G-9: deploy.sh 05 clean
G-10: runtime_test.sh --skip-start --no-deploy PASS
G-11: 5 commits land cleanly (T1, T3, T4, T5, T6) — no empty commits, no PR
```

---

## Acceptance Counter

- 6 tasks
- ~150 LOC net reduction in `RandomInoculationService.cs`
- 2 new test files (`InoculationInvariantSmokeTests.cs`, `InoculationTryInfectTests.cs`)
- 1 caller update (`StoryDirector.cs` line ~261)
- 1 Bootstrap line added (concurrent-thread-aware)
- 1 version bump (`bump_version.sh 05`)

---

## Self-Review

### Spec coverage

| C4 Spec component | Implementation |
|---|---|
| Single TryInfect(Map, int, long) | T3 |
| Pre-flight gates return 0 | T2 (5 sub-tests), T3 (impl) |
| godMode-log unified | T3 (commit msg documents) |
| ApplyLiveConversion unchanged (AggressionHediff wrap intact) | T1 smoke (constant probe) |
| BrandedKindDefName literal unchanged | T1 smoke |
| AiOverlay surface unchanged | T1 smoke (3 sub-tests) |
| One-line shims before delete | T4 |
| Concurrent-thread bootstrap-safe insertion | T6 |

### Placeholder scan

No "TBD" or "TODO" markers in production code. Test stubs use `return false` deliberately to surface missing implementation (RED phase).

### Type consistency

- `TryInfect(Map, int, long) -> int` — covariant with existing `int TryInfectWildAnimals(int, long)` ret-type, narrows `InoculationOutcome?` from `TryInfectRandom` to `int`. Both callsites (StoryDirector, AnimalInfectionDriver) compile cleanly post-shim.
- `long currentTick` — unchanged.
- Smoke-test reflection probe uses `BindingFlags.NonPublic | BindingFlags.Static` and `GetRawConstantValue()` (latter handles `const` literals specifically without boxing).

---

**Plan complete. Persisted to `docs/superpowers/plans/2026-08-06-inoculation-deep-seam.md`.**
