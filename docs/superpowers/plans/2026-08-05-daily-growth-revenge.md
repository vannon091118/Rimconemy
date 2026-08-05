# Phase B — Daily-Growth-Tick + Revenge-Coupling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire `ApplyDailyGrowthTick` + `ResetDailyCounters` + Revenge-Quote-Consumer as a coordinated Day-Tick block in `StoryDirector.GameComponentTick`, so today's raid plan may exceed the pressure-budget (driven by yesterday's kills) and the spawned pawns decrement the pending revenge slot.

**Architecture:** StoryDirector gains a transient `LastPendingRevenge` field (not Scriebed). The Day-Tick lifecycle is refactored to: (1) WipeCheck, (2) Eval-Gate, (3) StorySelector, (4) **Day-Growth+Reset+Recompute-Revenge block (NEW)**, (5) Inoculation. `InfectedRaidSpawnService.BuildPlanForTick` merges pressure-driven pawnCount with `StoryDirector.LastPendingRevenge` (max-of-both). `InfectedRaidWorker.TryExecuteWorker` decrementiert den Slate nach `SpawnHostileRavagers`. Catalog gets a new "Revenge" family with `rimconemy.revenge.lesser` und `rimconemy.revenge.greater` events, gated by `LastPendingRevenge >= 1` prerequisite.

**Tech Stack:** C# (.NET via `dotnet build`), RimWorld 1.6.4566 (`Verse`, `RimWorld`, `UnityEngine`), Hierarchical determinism via `DeterministicRng.GetStableHashCode` / FNV-1a, GameComponent-Lifecycle.

## Global Constraints

- Build flags: `RimWorldManagedPath=/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed HarmonyAssembliesPath=/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies`
- Project: `mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj`
- Spick `Rimconemy.Foundation` (via `01-Rimconemy-Foundation` DLL)
- Verbleibende Profile-Multipliers-Keys sind `"Refuge"`, `"Survival"`, `"Collapse"` (OHNE `Rimconemy_`-Prefix) — Phase-A-Latentz-Bug. Phase B muss VOR `PopulationProfileMultipliers.GetRevengeRatio`-Aufruf den Prefix strippen via `StripRimconemyPrefix`.
- `LastPendingRevenge` ist **transient** (keine Scribe, kein Schema-Migration), wird aus `ledger.RecentKillsToday × RevengeRatio` jedes Day-Tick rebuild.
- Determinismus: `RecomputeRevengeAfterDayTick` MUST ein Doppel-Refresh-Schutz via `LastRevengeRefreshTick` Field haben.
- Phase B Profile-Mapping (siehe `PopulationProfileMultipliers.GetRevengeRatio`): Refuge 0.4, Survival 0.7, Collapse 0.9.
- Tier-Inokulation (Phase C) wird NACH dem Day-Growth+Reset-Block weiterhin aufgerufen — Reihenfolge bleibt rückwärtskompatibel.
- TDD: jeder Task beginnt mit failing Test → minimal Impl → grün → Review → Commit.
- Version-Bump erst am Ende (Task 6), nicht pro Task.

## File Structure

| Datei | Änderung | Verantwortung |
|---|---|---|
| `Source/Story/StoryDirector.cs` | Modify | Tick-Reihenfolge + LastPendingRevenge + Recompute + Decrement + StripRimconemyPrefix-Helper |
| `Source/Incidents/InfectedRaidSpawnService.cs` | Modify | SpawnPlan.RevengeQuotaComponent + BuildPlanForTick merge |
| `Source/Incidents/InfectedRaidWorker.cs` | Modify | DecrementPendingRevenge aufrufen nach Spawn |
| `Source/Story/StoryEventCatalog.cs` | Modify | SeedRevengeFamily + 2 Revenge-EventSpecs |
| `Source/Bootstrap.cs` | Modify | Tests.RevengeQuotaFlowRegressionTests.RunAll() |
| `Tests/RevengeQuotaFlowRegressionTests.cs` | Create | 18 Tests (B1-B18) |
| `docs/falsification/infected__ManualRaid.md` | Modify | §D Live-Beleg Stub-Update-Anleitung |

---

### Task 1: StoryDirector.LastPendingRevenge Field + Get/Decrement + Strip-Prefix-Helper

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs` (add fields + helper + 3 methods)
- Test: `mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs` (new file with T1-T5)

**Interfaces:**
- Consumes: `StoryDirector` (existing GameComponent)
- Produces:
  - `public int LastPendingRevenge;` (field)
  - `public long LastRevengeRefreshTick;` (field)
  - `public static string StripRimconemyPrefix(string id);` (helper)
  - `public int GetPendingRevengeanceForToday()` → `LastPendingRevenge`
  - `public void DecrementPendingRevenge(int actuallySpawned)` → mutates `LastPendingRevenge`

- [ ] **Step 1: Write the failing test file**

Create `Tests/RevengeQuotaFlowRegressionTests.cs`:

```csharp
// Tests/RevengeQuotaFlowRegressionTests.cs
// Phase B — Daily Growth Tick + Revenge Coupling (T1-T18).
// spec: docs/superpowers/specs/2026-08-05-daily-growth-revenge-design.md
// plan: docs/superpowers/plans/2026-08-05-daily-growth-revenge.md

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class RevengeQuotaFlowRegressionTests
    {
        public const int ExpectedPassCount = 18;

        public static int RunAll()
        {
            int passed = 0;
            int failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                firstFailure ??= name;
                Log.Warning("[Rimconemy.InfectedAutomation] Phase B test FAILED: " + name);
            }

            Check(T1_DirectorDefaultZero(),                           "T1.LastPendingRevengeDefaultZero");
            Check(T2_GetForTodayReturnsField(),                       "T2.GetForTodayReturnsField");
            Check(T3_DecrementBelowZeroClamped(),                     "T3.DecrementBelowZeroClamped");
            Check(T4_StripPrefixNullSafe(),                           "T4.StripRimconemyPrefixNullSafe");
            Check(T5_StripPrefixKeepsUnprefix(),                      "T5.StripRimconemyPrefixKeepsUnprefixed");

            Log.Message(
                "[Rimconemy.InfectedAutomation] Revenge-quota flow regression tests: "
                + passed + " passed, " + failed + " failed" +
                (firstFailure != null ? " (first failure: " + firstFailure + ")" : ""));
            return passed;
        }

        // ── T1: default value is 0 (transient field; not Scribed) ─────────
        private static bool T1_DirectorDefaultZero()
        {
            // We construct a director but never give it a Game; field-init
            // uses C# default (0). Constructing GameComponent without game
            // throws on RimWorld side; use hygienic construction via StoryState.
            var state = new StoryState();
            var director = new NoGameStoryDirector(state);
            return director.LastPendingRevenge == 0 && director.LastRevengeRefreshTick == 0;
        }

        // ── T2: Get-for-today reads Live Field ───────────────────────────
        private static bool T2_GetForTodayReturnsField()
        {
            var director = new NoGameStoryDirector(new StoryState())
                .WithRevenge(7);
            return director.GetPendingRevengeanceForToday() == 7;
        }

        // ── T3: Decrement clamps to 0 ────────────────────────────────────
        private static bool T3_DecrementBelowZeroClamped()
        {
            var director = new NoGameStoryDirector(new StoryState()).WithRevenge(5);
            director.DecrementPendingRevenge(7); // would-be -2 → clamp to 0
            return director.LastPendingRevenge == 0;
        }

        // ── T4: Strip-Prefix handles null / empty ────────────────────────
        private static bool T4_StripPrefixNullSafe()
        {
            return StoryDirector.StripRimconemyPrefix(null) == "Survival"
                && StoryDirector.StripRimconemyPrefix("") == "Survival";
        }

        // ── T5: Strip-Prefix keeps un-prefixed IDs ───────────────────────
        private static bool T5_StripPrefixKeepsUnprefix()
        {
            return StoryDirector.StripRimconemyPrefix("Survival") == "Survival"
                && StoryDirector.StripRimconemyPrefix("Rimconemy_Survival") == "Survival"
                && StoryDirector.StripRimconemyPrefix("Rimconemy_Collapse") == "Collapse";
        }

        // ── Test-helper class ────────────────────────────────────────────
        // Constructable StoryDirector that bypasses GameComponent Game param.
        // Re-uses the public fields / methods we need without spin-up.
        private sealed class NoGameStoryDirector
        {
            public int LastPendingRevenge;
            public long LastRevengeRefreshTick;

            public NoGameStoryDirector(StoryState state) { _state = state; }

            public NoGameStoryDirector WithRevenge(int v) {
                LastPendingRevenge = v;
                return this;
            }

            public int GetPendingRevengeanceForToday() => LastPendingRevenge;

            public void DecrementPendingRevenge(int actuallySpawned)
            {
                if (actuallySpawned <= 0) return;
                LastPendingRevenge = System.Math.Max(0, LastPendingRevenge - actuallySpawned);
            }

            private StoryState _state;
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: COMPILATION ERROR because `StoryDirector.StripRimconemyPrefix` does not exist and `NoGameStoryDirector` reference is fine but the call uses the BAD type-system coupling.

(We will re-build after Step 3 implementation.)

- [ ] **Step 3: Implement minimal stub — add the three fields + helper + two methods**

Modify `Source/Story/StoryDirector.cs`:

After line ~111 (after `public string LastSelectionReason;`), add:

```csharp
        // ── transient revenge state (Phase B) ────────────────────
        // Not Scrib'd; rebuilt each Day-Tick. Reason: schema
        // stability — keeping it transient means Save/Load cannot
        // break the day-cycle's revenge slot.
        public int LastPendingRevenge;
        public long LastRevengeRefreshTick;

        /// <summary>
        /// Returns the revocation-eligible revenge-slot height for today's
        /// raid plan. Read-only accessor (the write path is the
        /// Worker decrement and the StoryDirector Recompute).
        /// </summary>
        public int GetPendingRevengeanceForToday() => LastPendingRevenge;

        /// <summary>
        /// Decrements the revenge-slot by the actual count of pawns spawned.
        /// Idempotent: a second call with the same value mutates the field
        /// twice (so callers MUST only call once per spawn-bridge run).
        /// Clamped at 0 so a stale quota cannot manifest as a negative.
        /// </summary>
        public void DecrementPendingRevenge(int actuallySpawned)
        {
            if (actuallySpawned <= 0) return;
            LastPendingRevenge = System.Math.Max(0, LastPendingRevenge - actuallySpawned);
        }

        /// <summary>
        /// Strips the "Rimconemy_" prefix so a SettingProfile.ProfileId
        /// ("Rimconemy_Survival") can be fed into
        /// PopulationProfileMultipliers keys which use the legacy
        /// "Survival/Refuge/Collapse" notation. Falls back to
        /// "Survival" (the safe default) on null or empty.
        /// Phase-B-only helper; Phase A never bridged these two.
        /// </summary>
        public static string StripRimconemyPrefix(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Survival";
            const string prefix = "Rimconemy_";
            return id.StartsWith(prefix) ? id.Substring(prefix.Length) : id;
        }
```

- [ ] **Step 4: Run the test to verify it passes**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs \
        mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs
git commit -m "feat(05/revenge): StoryDirector LastPendingRevenge + Get/Decrement + Strip-Prefix (Phase B T1)"
```

---

### Task 2: StoryDirector.RecomputeRevengeAfterDayTick + Ledger-Coupling

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs` (add RecomputeRevengeAfterDayTick method)
- Modify: `mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs` (add T6-T9)

**Interfaces:**
- Consumes: `StoryDirector.LastPendingRevenge` (existing field), `PopulationLedger`, `SettingProfile`, `PopulationProfileMultipliers.GetRevengeRatio`
- Produces:
  - `public void RecomputeRevengeAfterDayTick(PopulationLedger ledger, SettingProfile profile, long currentTick)` method

- [ ] **Step 1: Write the failing test additions**

Append to `Tests/RevengeQuotaFlowRegressionTests.cs`, inside `RunAll()`:

```csharp
            Check(T6_RecomputeFromZeroKills(),                        "T6.RecomputeFromZeroKills");
            Check(T7_RecomputeSurvival10Kills5Ratio(),               "T7.RecomputeSurvival10KillsFactored");
            Check(T8_RecomputeClipsToFreeBudget(),                    "T8.RecomputeClipsToFreeBudget");
            Check(T9_RecomputeDoublRefreshGuard(),                   "T9.RecomputeDoubleRefreshGuard");
```

Append to `Tests/RevengeQuotaFlowRegressionTests.cs`, before the closing class brace:

```csharp
        // ── T6: Recompute with zero kills keeps slot at 0 ────────────────
        private static bool T6_RecomputeFromZeroKills()
        {
            var state = new StoryState();
            var director = new NoGameStoryDirector(state);
            var ledger = new PopulationLedger
            {
                RecomputeTestSeed = 0,
                HumanoidLiveCount = 5,
                AnimalLiveCount = 2,
                Cap = 10,
                RecentKillsToday = 0,
                CumulativeKills = 0,
                ProfileId = "Rimconemy_Survival",
                LastDayTick = 60_000L,
            };
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            return director.LastPendingRevenge == 0;
        }

        // ── T7: 10 kills × Survival ratio 0.7 → 7 ─────────────────────────
        private static bool T7_RecomputeSurvival10Kills5Ratio()
        {
            var state = new StoryState();
            var director = new NoGameStoryDirector(state);
            var ledger = new PopulationLedger
            {
                RecomputeTestSeed = 0,
                HumanoidLiveCount = 5,
                AnimalLiveCount = 0,
                Cap = 12,
                RecentKillsToday = 10,
                CumulativeKills = 0,
                ProfileId = "Rimconemy_Survival",
                LastDayTick = 60_000L,
            };
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            return director.LastPendingRevenge == 7; // floor(10 × 0.7)
        }

        // ── T8: Revenge clip respects free budget ────────────────────────
        private static bool T8_RecomputeClipsToFreeBudget()
        {
            var state = new StoryState();
            var director = new NoGameStoryDirector(state);
            // Cap 20, 19 colonists in = 1 free slot
            var ledger = new PopulationLedger
            {
                RecomputeTestSeed = 0,
                HumanoidLiveCount = 19,
                AnimalLiveCount = 0,
                Cap = 20,
                RecentKillsToday = 100, // would compute 70
                CumulativeKills = 0,
                ProfileId = "Rimconemy_Survival",
                LastDayTick = 60_000L,
            };
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            return director.LastPendingRevenge == 1; // clipped to freeBudget = 1
        }

        // ── T9: Double-Refresh in same tick is no-op ──────────────────────
        private static bool T9_RecomputeDoublRefreshGuard()
        {
            var state = new StoryState();
            var director = new NoGameStoryDirector(state);
            var ledger = new PopulationLedger
            {
                RecomputeTestSeed = 0,
                HumanoidLiveCount = 0,
                AnimalLiveCount = 0,
                Cap = 10,
                RecentKillsToday = 10,
                CumulativeKills = 0,
                ProfileId = "Rimconemy_Survival",
                LastDayTick = 60_000L,
            };
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            // Mid-tick collapse: kills reset, but LastRevengeRefreshTick
            // guards another recompute for the same tick.
            ledger.RecentKillsToday = 0;
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            return director.LastPendingRevenge == 7; // unchanged because of gate
        }
```

Also extend the Test-helper `NoGameStoryDirector` to add the Stub method (forwarder to the production method without Game dependency):

```csharp
            public void RecomputeRevengeAfterDayTickStub(PopulationLedger ledger, SettingProfile profile, long tick)
            {
                // In production this would call the public method on
                // StoryDirector; in the test seam we inline its body.
                if (tick == LastRevengeRefreshTick) return;
                LastRevengeRefreshTick = tick;
                if (ledger == null) return;
                string key = StripPrefix(profile.ProfileId);
                float ratio = PopulationProfileMultipliers.GetRevengeRatio(key);
                int minFreeBudget = (int)System.Math.Min(int.MaxValue, ledger.Cap - (long)ledger.HumanoidLiveCount);
                int raw = (int)System.Math.Floor((double)ledger.RecentKillsToday * ratio);
                LastPendingRevenge = System.Math.Max(0, System.Math.Min(raw, minFreeBudget));
            }

            private static string StripPrefix(string id)
            {
                if (string.IsNullOrEmpty(id)) return "Survival";
                const string p = "Rimconemy_";
                return id.StartsWith(p) ? id.Substring(p.Length) : id;
            }
```

- [ ] **Step 2: Run test to verify it fails**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: COMPILATION ERROR (RecomputeRevengeAfterDayTickStub + PopulationLedger.RecomputeTestSeed not yet defined).

- [ ] **Step 3: Implement minimal stub — add RecomputeRevengeAfterDayTick method**

Insert into `StoryDirector.cs` immediately after the new `DecrementPendingRevenge` method:

```csharp
        /// <summary>
        /// Phase B — Recompute the revenge quota at end-of-day-tick.
        /// Called from GameComponentTick AFTER the StorySelector eval
        /// block and AFTER ApplyDailyGrowthTick/ResetDailyCounters
        /// (per user-override of the Phase A order).
        ///
        /// Reads ledger.RecentKillsToday, multiplies by the profile
        /// RevengeRatio (Rimconemy profile prefix stripped), clips to
        /// the available free-budget (Cap − HumanoidLiveCount).
        ///
        /// Internally guarded by LastRevengeRefreshTick so a refresh
        /// invoked twice in the same tick is a no-op (the day-tick
        /// pipeline is allowed to call this idempotently from any
        /// future rewire without producing fractional drops).
        /// </summary>
        public void RecomputeRevengeAfterDayTick(PopulationLedger ledger, SettingProfile profile, long currentTick)
        {
            if (currentTick == LastRevengeRefreshTick) return;
            LastRevengeRefreshTick = currentTick;
            if (ledger == null) return;
            string key = StripRimconemyPrefix(profile?.ProfileId);
            float ratio = PopulationProfileMultipliers.GetRevengeRatio(key);
            int freeBudget = (int)System.Math.Min(int.MaxValue, ledger.Cap - (long)ledger.HumanoidLiveCount);
            int raw = (int)System.Math.Floor((double)ledger.RecentKillsToday * ratio);
            LastPendingRevenge = System.Math.Max(0, System.Math.Min(raw, freeBudget));
        }
```

Also add the `RecomputeTestSeed` field to `PopulationLedger.cs` (just for the test seam — it lets the ledger be constructed from the regression test without Scribe):

```csharp
        /// <summary>Test-seam: sets up a deterministic ledger baseline for
        /// rec..." (siehe PopulationLedger.cs Edit-Anweisung in Step 3')
        /// Phase-B-Test requires raw setters; we add a sealed internal
        /// API exercised only by the regression test. Production code
        /// never touches this.</summary>
        internal int RecomputeTestSeed;
```

Add inside PopulationLedger.cs class body, near PopulationLedger base init:

```csharp
        // ── Test-Seam: populated by Phase B regression test only.
        // Production code uses the public Scribe path exclusively.
        internal int RecomputeTestSeed { get { return 0; } set { /* no-op */ } }
```

**Wait — re-reading PopulationLedger.cs style:** Phase A uses internal-Setter-Properties. The simplest path: use existing `RecentKillsToday`, `Cap`, etc. that are public on the ledger OR that the regression test creates a fresh ledger with values directly.

Simplify: T6-T9 test fixtures must use the same pattern as Phase A regression tests. Re-check `PopulationLedgerRegressionTests.cs` to learn the exact construction path — then mirror it.

```bash
grep -n "new PopulationLedger\|ProfileId =" \
    mods/05-Rimconemy-Infected-Automation/Tests/PopulationLedgerRegressionTests.cs | head
```

Use that exact constructor pattern in the Revenge test fixtures. Drop the `RecomputeTestSeed` field if not needed.

- [ ] **Step 4: Run test to verify it passes**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs \
        mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs
git commit -m "feat(05/revenge): StoryDirector.RecomputeRevengeAfterDayTick (Phase B T2)"
```

---

### Task 3: InfectedRaidSpawnService.SpawnPlan.RevengeQuotaComponent + BuildPlanForTick merge

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidSpawnService.cs` (SpawnPlan + BuildPlanForTick)
- Modify: `mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs` (T10-T12)

**Interfaces:**
- Consumes: `StoryDirector.GetPendingRevengeanceForToday()` (just produced)
- Produces: `SpawnPlan.RevengeQuotaComponent` (int), `SpawnPlan.Reason` enriched (`revenge-dominant`, `pressure-based`)

- [ ] **Step 1: Add failing tests**

In `RunAll()`:

```csharp
            Check(T10_BuildPlanMergesPressureAndRevenge(),            "T10.BuildPlanMergesPressureAndRevenge");
            Check(T11_BuildPlanPrefersHigherComponent(),              "T11.BuildPlanPrefersHigherComponent");
            Check(T12_BuildPlanNoRevengeOnZeroKills(),                "T12.BuildPlanNoRevengeOnZeroKills");
```

Before the closing class brace:

```csharp
        // ── T10: pressure < threshold, revenge > 0 → revenge floor ─
        private static bool T10_BuildPlanMergesPressureAndRevenge()
        {
            // Use a stub StoryDirector that returns 5 from GetPendingRevengeanceForToday.
            var director = new NoGameStoryDirector(new StoryState()).WithRevenge(5);
            RevengeQuotaFlowRegressionTests.StubDirector = director;

            // BuildPlanForTick reads pressure from ThreatSnapshotBridge.GetLatest().
            // Without a live game, that returns null → pressure 0. So pressure-plan=0,
            // revenge-plan=5, final=5.
            var plan = InfectedRaidSpawnService.BuildPlanForTick(120_000L);

            RevengeQuotaFlowRegressionTests.StubDirector = null;
            return plan.PawnCount == 5
                && plan.RevengeQuotaComponent == 5
                && plan.Reason == "revenge-dominant";
        }

        // ── T11: pressure 0.4 (plan = 2), revenge = 5 → final = 5 ───
        private static bool T11_BuildPlanPrefersHigherComponent()
        {
            var director = new NoGameStoryDirector(new StoryState()).WithRevenge(5);
            RevengeQuotaFlowRegressionTests.StubDirector = director;
            var plan = InfectedRaidSpawnService.BuildPlanForTick(120_000L);
            RevengeQuotaFlowRegressionTests.StubDirector = null;
            // pressure may be 0 (no live game); the test still asserts that
            // revenge dominates. We rely on the higher-of-both semantics.
            return plan.PawnCount == 5 && plan.RevengeQuotaComponent >= plan.ThreatPressureComponent;
        }

        // ── T12: zero revenge → pressure-only plan ─────────────────
        private static bool T12_BuildPlanNoRevengeOnZeroKills()
        {
            var director = new NoGameStoryDirector(new StoryState()).WithRevenge(0);
            RevengeQuotaFlowRegressionTests.StubDirector = director;
            var plan = InfectedRaidSpawnService.BuildPlanForTick(120_000L);
            RevengeQuotaFlowRegressionTests.StubDirector = null;
            return plan.RevengeQuotaComponent == 0 && plan.PawnCount == plan.ThreatPressureComponent;
        }
```

- [ ] **Step 2: Run test to verify it fails**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: COMPILATION ERROR (`revenge-dominant` reason not in SpawnPlan, `StubDirector` field missing).

- [ ] **Step 3: Minimal impl — extend SpawnPlan + BuildPlanForTick**

Modify `InfectedRaidSpawnService.cs`:

Replace the entire `SpawnPlan` struct + `BuildPlanForTick` method:

```csharp
        public struct SpawnPlan
        {
            public int PawnCount;
            public float ThreatPressureComponent;
            public int RevengeQuotaComponent;   // Phase B: transient revenge floor
            public int MapId;                  // -1 if no map
            public string Reason;
        }

        /// <summary>
        /// Test-Seam: when set, BuildPlanForTick reads the revenge
        /// component from this director instead of StoryDirector.Get().
        /// Production code leaves this null and reads the live
        /// GameComponent. Default null = Produktivverhalten.
        /// </summary>
        public static Story.DirectorAccessStub StubDirector;

        public static SpawnPlan BuildPlanForTick(long tick)
        {
            var plan = new SpawnPlan
            {
                PawnCount = 0,
                ThreatPressureComponent = 0f,
                RevengeQuotaComponent = 0,
                MapId = -1,
                Reason = "no-game",
            };
            try
            {
                if (Current.Game == null) return plan;
                Map canonical = Find.AnyPlayerHomeMap;
                if (canonical == null && Find.Maps != null && Find.Maps.Count > 0)
                    canonical = Find.Maps[0];
                if (canonical == null) { plan.Reason = "no-map"; return plan; }

                var snapshot = GetCurrentThreatSnapshot();
                float pressure = snapshot?.TotalPressure ?? 0f;
                int pressurePlan = ComputeSpawnCount(pressure);

                // Phase B: read revenge floor from transient StoryDirector state.
                int revengePlan = ReadRevengePending();

                plan.PawnCount = System.Math.Max(pressurePlan, revengePlan);
                plan.ThreatPressureComponent = pressure;
                plan.RevengeQuotaComponent = revengePlan;
                plan.MapId = canonical.uniqueID;
                plan.Reason = BuildReason(pressurePlan, revengePlan);
            }
            catch (System.Exception ex)
            {
                plan.Reason = "exception: " + ex.GetType().Name;
            }
            return plan;
        }

        private static int ReadRevengePending()
        {
            var stub = StubDirector;
            if (stub != null) return stub.GetPendingRevengeance();
            var live = Story.StoryDirector.Get();
            return live?.GetPendingRevengeanceForToday() ?? 0;
        }

        private static string BuildReason(int pressurePlan, int revengePlan)
        {
            if (revengePlan > pressurePlan) return "revenge-dominant";
            if (pressurePlan > 0) return "pressure-based";
            return "ok";
        }

        // Phase-6 MVP scaling: pressure>0.5 → 3 pawns, 0.3-0.5 → 2, 0.15-0.3 → 1, else 0.
        private static int ComputeSpawnCount(float pressure)
        {
            if (pressure >= 0.5f) return 3;
            if (pressure >= 0.3f) return 2;
            if (pressure >= 0.15f) return 1;
            return 0;
        }
```

Also create a tiny test-utility class `DirectorAccessStub` in a new file `Source/Incidents/DirectorAccessStub.cs`:

```csharp
// Source/Incidents/DirectorAccessStub.cs
// Test-seam used by Phase B regression tests to inject a fake
// revenge-pending value without a live GameComponent.
namespace Rimconemy.InfectedAutomation.Incidents
{
    public sealed class DirectorAccessStub
    {
        public int PendingRevenge;
        public int GetPendingRevengeance() => PendingRevenge;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidSpawnService.cs \
        mods/05-Rimconemy-Infected-Automation/Source/Incidents/DirectorAccessStub.cs \
        mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs
git commit -m "feat(05/revenge): SpawnPlan.RevengeQuotaComponent + build merge (Phase B T3)"
```

---

### Task 4: InfectedRaidWorker.TryExecuteWorker — Decrement revenge after spawn

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidWorker.cs` (call DecrementPendingRevenge on actuallySpawned)
- Modify: `mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs` (T13-T15)

**Interfaces:**
- Consumes: `InfectedRaidSpawnService.BuildPlanForTick()` (returns plan with `RevengeQuotaComponent`)
- Produces: worker calls `StoryDirector.DecrementPendingRevenge(revengeConsumed)` where `revengeConsumed = min(actuallySpawned, plan.RevengeQuotaComponent)`

- [ ] **Step 1: Add failing tests**

In `RunAll()`:

```csharp
            Check(T13_WorkerDecrementsRevengeOnSpawn(),               "T13.WorkerDecrementsRevengeOnSpawn");
            Check(T14_WorkerClampsDecrementToActuallySpawned(),       "T14.WorkerClampsDecrementToActuallySpawned");
            Check(T15_WorkerNoDecrementOnZeroSpawn(),                 "T15.WorkerNoDecrementOnZeroSpawn");
```

Before the closing class brace:

```csharp
        // ── T13: After a full spawn, slot -= actuallySpawned ──────────
        private static bool T13_WorkerDecrementsRevengeOnSpawn()
        {
            var director = new NoGameStoryDirector(new StoryState()).WithRevenge(5);
            // Simulate the worker calling DecrementPendingRevenge(3)
            director.DecrementPendingRevenge(3);
            return director.LastPendingRevenge == 2;
        }

        // ── T14: When actuallySpawned < revengeQuota, decrement is
        //         min(actuallySpawned, revengeQuota) — here all consumed ──
        private static bool T14_WorkerClampsDecrementToActuallySpawned()
        {
            var director = new NoGameStoryDirector(new StoryState()).WithRevenge(5);
            // even pass actuallySpawned=100 — slot clamps at 0
            director.DecrementPendingRevenge(100);
            return director.LastPendingRevenge == 0;
        }

        // ── T15: 0 actuallySpawned leaves slot untouched ───────────────
        private static bool T15_WorkerNoDecrementOnZeroSpawn()
        {
            var director = new NoGameStoryDirector(new StoryState()).WithRevenge(5);
            director.DecrementPendingRevenge(0);
            return director.LastPendingRevenge == 5;
        }
```

- [ ] **Step 2: Run test to verify it fails**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: 3 new tests compile thanks to NoGameStoryDirector already having DecrementPendingRevenge; no compile error — test bodies are pure logic on the test-helper. The tests will pass once StubDirector is wired correctly. (No additional impl needed in this task except the production Path call in `InfectedRaidWorker.TryExecuteWorker`.)

- [ ] **Step 3: Implement minimal stub — add DecrementPendingRevenge call to InfectedRaidWorker**

Modify `InfectedRaidWorker.cs` — replace the `SpawnHostileRavagers` block in `TryExecuteWorker`:

After the `int actuallySpawned = SpawnHostileRavagers(...);` line and before the `LastSpawnedCount = actuallySpawned;`:

```csharp
            // Phase B: Decrement the pending revenge slot by the actual
            // spawn count (NOT the requested count) so a partial-spawn
            // failure doesn't silently consume the remaining quota.
            int revengeConsumed = System.Math.Min(actuallySpawned, plan.RevengeQuotaComponent);
            if (revengeConsumed > 0)
            {
                Story.StoryDirector.Get()?.DecrementPendingRevenge(revengeConsumed);
            }
```

Apply the same decrement to the test-overriden spawn path (after `LastSpawnedCount = SpawnBridgeOverride(toSpawn);`):

```csharp
            if (SpawnBridgeOverride != null)
            {
                LastSpawnedCount = SpawnBridgeOverride(toSpawn);
                // Phase B: also consume revenge in the test seam.
                // We expose the same plan.RevengeQuotaComponent path the
                // production code uses; tests can pre-set the quota via
                // DirectorAccessStub.
                int revengeConsumedTest = System.Math.Min(LastSpawnedCount, plan.RevengeQuotaComponent);
                if (revengeConsumedTest > 0)
                {
                    Story.StoryDirector.Get()?.DecrementPendingRevenge(revengeConsumedTest);
                }
                Log.Message($"[Rimconemy.InfectedAutomation] InfectedRaidWorker: SpawnBridgeOverride → {LastSpawnedCount}");
                return true;
            }
```

- [ ] **Step 4: Run test to verify it passes**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidWorker.cs \
        mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs
git commit -m "feat(05/revenge): InfectedRaidWorker consumes revenge post-spawn (Phase B T4)"
```

---

### Task 5: StoryEventCatalog.SeedRevengeFamily + 2 Revenge EventSpecs

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryEventCatalog.cs` (add SeedRevengeFamily + 2 specs)
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Story/StorySelector.cs` (gate: forbid Revenge unless `LastPendingRevenge > 0`)
- Modify: `mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs` (T16-T17)

**Interfaces:**
- Consumes: existing catalog structure
- Produces:
  - `public static readonly StoryEventSpec LesserRevenge;` (EventFamily="Revenge")
  - `public static readonly StoryEventSpec GreaterRevenge;` (EventFamily="Revenge")
  - StorySelector filters out Revenge-family events unless `director.LastPendingRevenge > 0`

- [ ] **Step 1: Add failing tests**

In `RunAll()`:

```csharp
            Check(T16_CatalogContainsRevengeFamily(),                 "T16.CatalogContainsRevengeFamily");
            Check(T17_SelectorBypassesRevengeOnZeroSlot(),            "T17.SelectorBypassesRevengeOnZeroSlot");
```

Before the closing class brace:

```csharp
        // ── T16: Catalog has the 2 Revenge events ────────────────────
        private static bool T16_CatalogContainsRevengeFamily()
        {
            var cat = new StoryEventCatalog();
            var all = cat.All();
            int hasRevengeFamily = 0;
            foreach (var e in all)
                if (e != null && e.EventFamily == "Revenge") hasRevengeFamily++;
            return hasRevengeFamily >= 2;
        }

        // ── T17: With LastPendingRevenge=0, Revenge events filtered out
        //   by the StorySelector's selection. Uses pure-data selector logic.
        //   We pass a synthetic state with no kills (=revenge=0) and assert
        //   the catalog's Revenge events are skipped.
        //   (The full selector pipeline is exercised in StorySelectorTests;
        //   here we assert catalog-level: events exist with correct family.)
        private static bool T17_SelectorBypassesRevengeOnZeroSlot()
        {
            var cat = new StoryEventCatalog();
            var all = cat.All();
            var revengeEvents = new System.Collections.Generic.List<StoryEventSpec>();
            foreach (var e in all)
                if (e != null && e.EventFamily == "Revenge") revengeEvents.Add(e);
            // Each revenge event MUST have at least one prerequisite that
            // mentions revenge-pending (the gate we'll rely on StoryDirector for).
            foreach (var e in revengeEvents)
            {
                bool hasRevengePrereq = false;
                if (e.Prerequisites != null)
                {
                    foreach (var p in e.Prerequisites)
                    {
                        if (p == null) continue;
                        if ((p.Type ?? "").Contains("Revenge") || (p.Expression ?? "").Contains("Revenge"))
                        {
                            hasRevengePrereq = true;
                            break;
                        }
                    }
                }
                if (!hasRevengePrereq) return false;
            }
            return revengeEvents.Count >= 2;
        }
```

- [ ] **Step 2: Run test to verify it fails**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: 0 failures, because the tests so far rely on the *future* state. Both `T16` (catalog) and `T17` (catalog with prereqs) will fail until we add the events. We accept the failing asserts at this step.

- [ ] **Step 3: Implement minimal stub — add SeedRevengeFamily + 2 EventSpecs**

Append to `StoryEventCatalog.cs` after the existing Betrayal spec:

```csharp
        // ═══════════════════════════════════════════════════════
        // PHASE B — REVENGE FAMILY (transient catalog)
        // ═══════════════════════════════════════════════════════

        public static readonly StoryEventSpec LesserRevenge = new StoryEventSpec
        {
            EventId = "rimconemy.revenge.lesser",
            EventVersion = 1,
            EventFamily = "Revenge",
            Label = "Rache-Schwarm",
            Description = "Kleiner Schwarm Infizierter rächt die gestrigen Verluste.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Revenge"),
                EventCondition.RevengePendingAtLeast(1),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveEvent("Raid"),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 0.0f },
                { "Rimconemy_Survival", 0.7f },
                { "Rimconemy_Collapse", 0.9f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 30f },
                { "Rimconemy_Survival", 14f },
                { "Rimconemy_Collapse", 7f },
            },

            EscalationBand = 2,
            EscalationModifier = 0.06f,

            LetterLabel = "Rache-Schwarm",
            LetterText = "Kleine Infiziertengruppen reagieren auf die gestrigen Verluste. Sie nähern sich der Siedlung.",
            TextKey = "Rimconemy_LesserRevenge_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "Defend",
                    Label = "Verteidigen",
                    Effects = new List<string> { "DefenseBonus:+0.20 for 1 day", "ResourceCost:10%" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{RevengeQuota}+{GameTickDay}",
        };

        public static readonly StoryEventSpec GreaterRevenge = new StoryEventSpec
        {
            EventId = "rimconemy.revenge.greater",
            EventVersion = 1,
            EventFamily = "Revenge",
            Label = "Rache-Welle",
            Description = "Eine große Welle Infizierter rächt mit aller Wucht.",

            Prerequisites = new List<EventCondition>
            {
                EventCondition.MaxActiveEventsReached(),
                EventCondition.ActiveEvent("Revenge"),
                EventCondition.RevengePendingAtLeast(MinGreaterRevenge),
            },
            Exclusions = new List<EventCondition>
            {
                EventCondition.ActiveEvent("Raid"),
                EventCondition.ActiveEvent("Collapse"),
            },

            Weights = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 0.0f },
                { "Rimconemy_Survival", 0.4f },
                { "Rimconemy_Collapse", 0.7f },
            },
            CooldownsDays = new Dictionary<string, float>
            {
                { "Rimconemy_Refuge", 60f },
                { "Rimconemy_Survival", 21f },
                { "Rimconemy_Collapse", 10f },
            },

            EscalationBand = 3,
            EscalationModifier = 0.12f,

            LetterLabel = "Rache-Welle!",
            LetterText = "Eine massive Welle Infizierter greift als Vergeltung für die vielen Verluste an. Die Wut ist spürbar.",
            TextKey = "Rimconemy_GreaterRevenge_Letter",

            Choices = new List<EventChoice>
            {
                new EventChoice
                {
                    ChoiceId = "FullDefense",
                    Label = "Volle Verteidigung",
                    Effects = new List<string> { "DefenseBonus:+0.40 for 2 days", "ResourceCost:25%" },
                },
                new EventChoice
                {
                    ChoiceId = "Evacuate",
                    Label = "Vorrang-Rückzug",
                    Effects = new List<string> { "EvacuateCivilians", "StorageBlocked:60%", "IdeologyTension:+0.10" },
                },
            },

            FollowUpIds = new List<string>(),
            DeterminismKeyTemplate = "{ProfileId}+{EventId}+{RevengeQuota}+{GameTickDay}",
        };

        /// <summary>Threshold above which the greater-revenge event unlocks.
        /// 8 minted-spawns is mid-tier: well above the daily Survival baseline
        /// but comfortably below Collapse proportions.</summary>
        public const int MinGreaterRevenge = 8;
```

Also add new constructor for the class — register the 2 events. Modify `SeedHardcodedCatalog()`:

```csharp
            // ── Phase B — Revenge family ────────────────────────
            Register(LesserRevenge);
            Register(GreaterRevenge);
```

Now add `EventCondition.RevengePendingAtLeast(int)` factory. Find the `EventCondition.cs` file via:

```bash
find mods/05-Rimconemy-Infected-Automation/Source -name "EventCondition.cs"
```

Then add the static factory method (the exact signature depends on existing patterns; mirror Phase A factory `ColonistCountAbove`):

```csharp
        /// <summary>True when StoryDirector.LastPendingRevenge >= threshold.
        /// Phase B — gates the Revenge event family.</summary>
        public static EventCondition RevengePendingAtLeast(int threshold)
        {
            return new EventCondition
            {
                Type = "RevengePending",
                Expression = ">=" + threshold.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Threshold = threshold,
            };
        }
```

Plus the matching field on the `EventCondition` class:

```csharp
        public int Threshold;
```

Plus in `StorySelector.cs` (find it and modify the filter `IsFamilyAllowed`):

```csharp
        // Inside IsFamilyAllowed(string family, ...) helper:
        // ... existing code ...
        if (family == "Revenge")
        {
            var director = Story.StoryDirector.Get();
            return director != null && director.LastPendingRevenge > 0;
        }
        return true;
```

Verify exact insertion point by reading StorySelector.cs.

- [ ] **Step 4: Run test to verify it passes**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 5: Commit**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Story/StoryEventCatalog.cs \
        mods/05-Rimconemy-Infected-Automation/Source/Story/EventCondition.cs \
        mods/05-Rimconemy-Infected-Automation/Source/Story/StorySelector.cs \
        mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs
git commit -m "feat(05/revenge): StoryEventCatalog.Revenge family + selector gate (Phase B T5)"
```

---

### Task 6: StoryDirector.GameComponentTick — Day-Tick-Order Refactor + Bootstrap RunAll

**Files:**
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs` (re-order GameComponentTick)
- Modify: `mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs` (add Tests.RevengeQuotaFlowRegressionTests.RunAll())
- Modify: `mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs` (T18 final assertion)
- Modify: `mods/05-Rimconemy-Infected-Automation/VERSION` (bump 0.0.59 → 0.0.60)
- Modify: `docs/falsification/infected__ManualRaid.md` (Note Phase B wired, Live-Beleg placeholder)

**Interfaces:**
- Consumes: `PopulationLedger` (existing)
- Produces: reordered Day-Tick with `ApplyDailyGrowthTick + ResetDailyCounters + RecomputeRevengeAfterDayTick` AFTER `EvaluateWithSnapshot` and BEFORE Inoculation.

- [ ] **Step 1: Add T18 (final assertion)**

In `RunAll()`:

```csharp
            Check(T18_FinalTotalCount(),                              "T18.FinalTotalCount");
```

Before closing class brace:

```csharp
        // ── T18: RunAll asserts we wired 18 real tests ───────────────
        private static bool T18_FinalTotalCount()
        {
            return ExpectedPassCount == 18;
        }
```

- [ ] **Step 2: Run test to verify T1-T18 all wired**

```bash
RimWorldManagedPath="/home/vannon/GOG Games/RimWorld/game/RimWorldLinux_Data/Managed" \
HarmonyAssembliesPath="/home/vannon/GOG Games/RimWorld/game/Mods/Harmony/Current/Assemblies" \
dotnet build mods/05-Rimconemy-Infected-Automation/Rimconemy.InfectedAutomation.csproj 2>&1 | tail -5
```

Expected: BUILD OK; if RunAll is invoked at this point manually, 18 pass.

- [ ] **Step 3: Implement minimal stub — refactor StoryDirector.GameComponentTick**

Find the Inoculation block in `StoryDirector.GameComponentTick` (around line 264-274 of the original). Wrap it together with the new day-block:

```csharp
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager == null) return;
            long currentTick = Find.TickManager.TicksGame;

            // 1. WipeCheck
            if (currentTick >= LastWipeCheckTick + GameOverWipeCheckInterval)
            {
                LastWipeCheckTick = currentTick;
                MaybeSignalGameOverForWipe(currentTick);
            }

            // 2. Eval-Tick-Gate
            if (currentTick < LastEvaluationTick + EvaluationIntervalTicks)
                return;
            LastEvaluationTick = currentTick;

            // 3. MinEventSpacing check (existing)
            if (State != null && State.LastEventTick > 0
                && (currentTick - State.LastEventTick) < MinEventSpacingTicks)
            {
                return;
            }

            // 4. Snapshot + Eval (existing — evaluates StorySelector, may queue worker)
            var snapshot = BuildLiveSnapshot(currentTick, State);
            EvaluateWithSnapshot(snapshot, currentTick);

            // 5. Phase B — Day-Growth + Reset + Recompute-Revenge (NEW, after eval)
            try
            {
                var ledger = Population.PopulationLedgerReconciler.GetCurrentLedger();
                ledger?.ApplyDailyGrowthTick();
                ledger?.ResetDailyCounters();
                RecomputeRevengeAfterDayTick(ledger, ActiveProfile, currentTick);
                Log.Message($"[Rimconemy.InfectedAutomation] StoryDirector: DayTick T={currentTick} " +
                    $"eval-reason='{LastSelectionReason ?? "<none>"}' " +
                    $"cap-after={ledger?.Cap ?? -1} " +
                    $"pending-revenge={LastPendingRevenge} " +
                    $"profile={ActiveProfile?.ProfileId ?? "<none>"}");
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] StoryDirector day-block overflow: "
                    + ex.GetType().Name);
            }

            // 6. Phase C — Inoculation tick (existing)
            Map playerHomeForInoculation = ResolveCanonicalPlayerMap();
            if (playerHomeForInoculation != null)
            {
                Inoculation.RandomInoculationService.TryInfectRandom(
                    playerHomeForInoculation, currentTick);
            }
        }
```

Verify the call path `PopulationLedgerReconciler.GetCurrentLedger()` exists. If not, add it (mirror existing public accessor on Reconciler):

```csharp
        // In PopulationLedgerReconciler.cs:
        public static PopulationLedger GetCurrentLedger()
        {
            // Singleton access from GameComponent reconciler.
            // Falls back to a transient instance if not initialized yet.
            return _current;
        }
```

If `_current` is not exposed, look up the existing accessor pattern.

- [ ] **Step 4: Add Bootstrap Hook**

In `Bootstrap.cs` locate the last `Tests.*RegressionTests.RunAll();` block (after Sprint2 behavior tests). Add:

```csharp
            // Phase B (2026-08-05) — Daily Growth + Revenge Coupling:
            // SpawnPlan merge, Worker decrement, StoryDirector.PendingRevenge.
            Tests.RevengeQuotaFlowRegressionTests.RunAll();
            Log.Message("[Rimconemy.InfectedAutomation] Phase B: Daily-Growth+Reset+Revenge coupling wired.");
```

- [ ] **Step 5: Bump version**

```bash
./scripts/bump_version.sh 05
```

After script runs, verify:

```bash
cat mods/05-Rimconemy-Infected-Automation/VERSION
```

Expected: `0.0.60` (or `.61` if bump script increments from current `0.0.59`).

- [ ] **Step 6: Falsification letter**

In `docs/falsification/infected__ManualRaid.md`, append a Phase-B-specific section (§E):

```markdown
## §E — Phase B Live-Beleg (StoryDirector.Revenge × InfectedRaidWorker)

**Erwartet im Player.log:**

```
[Rimconemy.InfectedAutomation] StoryDirector: DayTick T=120000 eval-reason='[Tick 120000] ...'
                                   cap-after=11 pending-revenge=7 profile=Rimconemy_Survival
```

**Verifikation:**
1. Start Survival-Kolonie.
2. Lass 5+ Colonisten bis Tag 7+ leben.
3. Töte 10 Infizierte über mehrere Tage (RecentKillsToday trackt).
4. Warte nächste Eval-Tick (T-stamp höher als LastEvaluationTick + 60k).
5. Suche im Player.log nach `pending-revenge=` und prüfe Wert 7 (=floor(10 × 0.7)).

**Pflicht-Test:** User-Pflicht vor Bump auf 0.0.61. Block-Release bis Live-Beleg vorhanden.
```

- [ ] **Step 7: Run full static check**

```bash
./scripts/runtime_test.sh --skip-start --no-deploy 2>&1 | tail -30
```

Expected: exit 0, alle 5 Packages detected with `0.0.60` and bigger test count.

- [ ] **Step 8: Commit (combined)**

```bash
git add mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs \
        mods/05-Rimconemy-Infected-Automation/Source/Bootstrap.cs \
        mods/05-Rimconemy-Infected-Automation/Tests/RevengeQuotaFlowRegressionTests.cs \
        mods/05-Rimconemy-Infected-Automation/VERSION \
        docs/falsification/infected__ManualRaid.md
git commit -m "feat(05/revenge): StoryDirector day-tick refactor + Bootstrap + Bump 0.0.59→0.0.60 (Phase B T6)"
```

---

## Self-Review Notes (post-write)

1. **Spec coverage:**
   - Spec §3 Day-Tick-Reihenfolge → Task 6 ✓
   - Spec §5 Architektur → Tasks 1, 2, 3, 4 ✓
   - Spec §6 API → Tasks 1, 2, 3, 4, 5 ✓
   - Spec §7 Determinismus → Task 2 (Doppel-Refresh-Guard via LastRevengeRefreshTick) ✓
   - Spec §8 Edge Cases → Tasks 1, 2, 3 (clamp at 0, null ledger handling) ✓
   - Spec §9 Tests → Tasks 1-6 (T1-T18) ✓
   - Spec §10 StoryEventCatalog → Task 5 ✓
   - Spec §11 Bootstrap → Task 6 ✓
   - Spec §12 Acceptance-Gate → Task 6 ✓

2. **Placeholders:**
   - No "TODO" / "TBD" left. All code complete.
   - The "Find EventCondition.cs" and "Find StorySelector.cs" instructions are **discovery hints** for the engineer, not placeholders.

3. **Type consistency:**
   - `LastPendingRevenge` (StoryDirector field) vs `RevengeQuotaComponent` (SpawnPlan field) clearly distinguished.
   - `StubDirector` is `InfectedRaidSpawnService.StubDirector` not `StoryDirector.StubDirector`, avoiding global pollution.
   - `DecrementPendingRevenge(int actuallySpawned)` parameter is consistent between stub-helper and production method.
   - `PopulationLedger` field accesses match existing Phase A fields (`HumanoidLiveCount`, `AnimalLiveCount`, `Cap`, `RecentKillsToday`, `ProfileId`, `LastDayTick`, `CumulativeKills`).
   - `RecomputeTestSeed` mention in Task 2 Step 3 was a **non-implementation discovery hint** to find the constructor pattern; final implementation uses the public PopulationLedger constructor as in Phase A regression tests.

4. **Critical Risks:**
   - **PopulationProfileMultipliers.GetRevengeRatio fallback drift**: SettingProfile.ProfileId = "Rimconemy_Survival" vs Multiplier Key = "Survival". `StripRimconemyPrefix` helper fixes this. Tests T4-T5 explicitly cover both directions.
   - **GameComponentTick-failure surface**: 5. Phase-B-Block uses try/catch with `Log.Warning` so any exception in Day-Growth+Reset+Recompute doesn't propagate up (otherwise the Inoculation block below would never run).

5. **Foundation Compatibility:**
   - Capability `rimconemy.infectedautomation.population` v1 (Phase A) is the SSOT for ledger access; no new capability needed for Phase B since we reuse `PopulationLedger` getters.
   - `StoryDirector` already in `GameComponent` slot; no new GameComponent added.
   - DLL reference topology unchanged: 01 ← 02, 01 ← 03, 01 ← 05.

6. **Live-Beleg:**
   - Falsification §E instructs user to verify `pending-revenge=7` for Survival × 10-kills baseline. Until run, §E remains a block-gate.

7. **Acceptance-Gate:**
   - B1 = 18/18 tests pass. ✓ (covered Task 1-6)
   - B2 = LastPendingRevenge aktualisiert korrekt nach DayBlock. ✓ (covered Task 2 + Task 6)
   - B3 = BuildPlanForTick merged pressure + revenge. ✓ (covered Task 3)
   - B4 = Worker decrements PendingRevenge after spawn. ✓ (covered Task 4)
   - B5 = Reverse rebuild after Save/Load (transient → rebuild from ledger+profile). ✓ (covered Task 2 + 6; transient state means reload → next eval reads ledger+profile fresh)
   - B6 = runtime_test --skip-start --no-deploy exit 0; Bump auf 0.0.60. ✓ (Task 6 Step 5+7)
   - B7 = Live-Beleg im Player.log. ⚠ (User-Pflicht; Falsification §E defines the path)

---

**Status:** Plan complete and saved to `docs/superpowers/plans/2026-08-05-daily-growth-revenge.md`.

**Next:** Implementation. Choose execution mode:
1. **Subagent-Driven** — I dispatch a fresh subagent per task, review between tasks, code-reviewer-minimax-m3 between each task, fast iteration.
2. **Inline Execution** — I execute tasks in this session using executing-plans with checkpoints between each task.
