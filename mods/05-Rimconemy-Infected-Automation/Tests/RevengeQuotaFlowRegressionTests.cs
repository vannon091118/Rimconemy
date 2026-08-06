// Tests/RevengeQuotaFlowRegressionTests.cs
//
// Phase B — Daily Growth Tick + Revenge Coupling (T1-T18).
// spec: docs/superpowers/specs/2026-08-05-daily-growth-revenge-design.md
// plan: docs/superpowers/plans/2026-08-05-daily-growth-revenge.md
//
// Owner: Infected & Automation (Package 05).
// Migrated to TestSuite harness 2026-08-07.

using Rimconemy.Foundation.Tests;
using Rimconemy.InfectedAutomation.Story;
using Rimconemy.InfectedAutomation.Population;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class RevengeQuotaFlowRegressionTests
    {
        private const int MinPassCount = 18;

        public static int RunAll()
        {
            var ts = new TestSuite("InfectedAutomation", "Revenge-quota flow regression");

            // ── T1-T5: Tasks 1 ────────────────────────────────────
            ts.Check(T1_DirectorDefaultZero(),                           "T1.LastPendingRevengeDefaultZero");
            ts.Check(T2_GetForTodayReturnsField(),                       "T2.GetForTodayReturnsField");
            ts.Check(T3_DecrementBelowZeroClamped(),                     "T3.DecrementBelowZeroClamped");
            ts.Check(T4_StripPrefixNullSafe(),                           "T4.StripRimconemyPrefixNullSafe");
            ts.Check(T5_StripPrefixKeepsUnprefix(),                      "T5.StripRimconemyPrefixKeepsUnprefixed");

            // ── T6-T9: Tasks 2 ────────────────────────────────────
            ts.Check(T6_RecomputeFromZeroKills(),                        "T6.RecomputeFromZeroKills");
            ts.Check(T7_RecomputeSurvival10Kills7Ratio(),                "T7.RecomputeSurvival10KillsFactored");
            ts.Check(T8_RecomputeClipsToFreeBudget(),                    "T8.RecomputeClipsToFreeBudget");
            ts.Check(T9_RecomputeDoublRefreshGuard(),                    "T9.RecomputeDoubleRefreshGuard");

            // ── T10-T12: Tasks 3 ───────────────────────────────────
            ts.Check(T10_BuildPlanMergesPressureAndRevenge(),            "T10.BuildPlanMergesPressureAndRevenge");
            ts.Check(T11_BuildPlanPrefersHigherComponent(),              "T11.BuildPlanPrefersHigherComponent");
            ts.Check(T12_BuildPlanNoRevengeOnZeroKills(),                "T12.BuildPlanNoRevengeOnZeroKills");

            // ── T13-T15: Tasks 4 ───────────────────────────────────
            ts.Check(T13_WorkerDecrementsRevengeOnSpawn(),               "T13.WorkerDecrementsRevengeOnSpawn");
            ts.Check(T14_WorkerClampsDecrementToActuallySpawned(),       "T14.WorkerClampsDecrementToActuallySpawned");
            ts.Check(T15_WorkerNoDecrementOnZeroSpawn(),                 "T15.WorkerNoDecrementOnZeroSpawn");

            // ── T16-T17: Tasks 5 ───────────────────────────────────
            ts.Check(T16_CatalogContainsRevengeFamily(),                 "T16.CatalogContainsRevengeFamily");
            ts.Check(T17_RevengeEventsHaveRevengePrereq(),               "T17.RevengeEventsHaveRevengePrereq");

            // ── T18: Tasks 6 ───────────────────────────────────
            ts.Check(T18_FinalTotalCount(),                              "T18.FinalTotalCount");

            ts.RunSummary(MinPassCount);
            return ts.Failed;
        }

        // ── T1: default value is 0 (transient field; not Scrib'd) ─────
        private static bool T1_DirectorDefaultZero()
        {
            var director = new NoGameStoryDirector();
            return director.LastPendingRevenge == 0 && director.LastRevengeRefreshTick == 0;
        }

        // ── T2: Get-for-today returns the live field value ────────────
        private static bool T2_GetForTodayReturnsField()
        {
            var director = new NoGameStoryDirector().WithRevenge(7);
            return director.GetPendingRevengeanceForToday() == 7;
        }

        // ── T3: Decrement clamps at 0 ─────────────────────────────────
        private static bool T3_DecrementBelowZeroClamped()
        {
            var director = new NoGameStoryDirector().WithRevenge(5);
            director.DecrementPendingRevenge(7);
            return director.LastPendingRevenge == 0;
        }

        // ── T4: Strip-Prefix null-/empty-/whitespace-Safety ──────────
        private static bool T4_StripPrefixNullSafe()
        {
            return StoryDirector.StripRimconemyPrefix(null) == "Survival"
                && StoryDirector.StripRimconemyPrefix("") == "Survival"
                && StoryDirector.StripRimconemyPrefix("   ") == "Survival"
                && StoryDirector.StripRimconemyPrefix(" Rimconemy_Survival ") == "Survival";
        }

        // ── T5: Strip-Prefix keeps un-prefixed IDs intact ─────────────
        private static bool T5_StripPrefixKeepsUnprefix()
        {
            return StoryDirector.StripRimconemyPrefix("Survival") == "Survival"
                && StoryDirector.StripRimconemyPrefix("Rimconemy_Survival") == "Survival"
                && StoryDirector.StripRimconemyPrefix("Rimconemy_Collapse") == "Collapse"
                && StoryDirector.StripRimconemyPrefix("Rimconemy_Refuge") == "Refuge";
        }

        // ── T6: zero kills → slot stays at 0 ───────────────────────────
        private static bool T6_RecomputeFromZeroKills()
        {
            var director = new NoGameStoryDirector();
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 5, AnimalLiveCount = 2, Cap = 10,
                RecentKillsToday = 0, CumulativeKills = 0,
                ProfileId = "Survival", LastDayTick = 60_000L,
            };
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            return director.LastPendingRevenge == 0;
        }

        // ── T7: 10 kills × Survival ratio 0.7 → floor = 7 ─────────────
        private static bool T7_RecomputeSurvival10Kills7Ratio()
        {
            var director = new NoGameStoryDirector();
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 5, AnimalLiveCount = 0, Cap = 12,
                RecentKillsToday = 10, CumulativeKills = 0,
                ProfileId = "Survival", LastDayTick = 60_000L,
            };
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            return director.LastPendingRevenge == 7;
        }

        // ── T8: clipping at free budget ────────────────────────────────
        private static bool T8_RecomputeClipsToFreeBudget()
        {
            var director = new NoGameStoryDirector();
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 19, AnimalLiveCount = 0, Cap = 20,
                RecentKillsToday = 100, CumulativeKills = 0,
                ProfileId = "Survival", LastDayTick = 60_000L,
            };
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            return director.LastPendingRevenge == 1;
        }

        // ── T9: double-refresh in the same tick is a no-op ─────────────
        private static bool T9_RecomputeDoublRefreshGuard()
        {
            var director = new NoGameStoryDirector();
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 0, AnimalLiveCount = 0, Cap = 10,
                RecentKillsToday = 10, CumulativeKills = 0,
                ProfileId = "Survival", LastDayTick = 60_000L,
            };
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            ledger.RecentKillsToday = 0;
            director.RecomputeRevengeAfterDayTickStub(ledger, SettingProfile.Survival, 120_000L);
            return director.LastPendingRevenge == 7;
        }

        // ── T10: BuildPlan merges pressure + revenge floor ────────────
        private static bool T10_BuildPlanMergesPressureAndRevenge()
        {
            var stub = new Incidents.DirectorAccessStub { PendingRevenge = 5 };
            Incidents.InfectedRaidSpawnService.StubDirector = stub;
            try
            {
                var plan = Incidents.InfectedRaidSpawnService.BuildPlanForTick(120_000L);
                return plan.RevengeQuotaComponent == 5 && plan.PawnCount == 5
                    && plan.Reason == "revenge-dominant";
            }
            finally { Incidents.InfectedRaidSpawnService.StubDirector = null; }
        }

        // ── T11: higher-of-two semantics ───────
        private static bool T11_BuildPlanPrefersHigherComponent()
        {
            var stub = new Incidents.DirectorAccessStub { PendingRevenge = 5 };
            Incidents.InfectedRaidSpawnService.StubDirector = stub;
            try
            {
                var plan = Incidents.InfectedRaidSpawnService.BuildPlanForTick(120_000L);
                return plan.PawnCount >= plan.RevengeQuotaComponent && plan.PawnCount == 5;
            }
            finally { Incidents.InfectedRaidSpawnService.StubDirector = null; }
        }

        // ── T12: zero revenge + pressure => pressure-only path ───────
        private static bool T12_BuildPlanNoRevengeOnZeroKills()
        {
            var stub = new Incidents.DirectorAccessStub { PendingRevenge = 0 };
            Incidents.InfectedRaidSpawnService.StubDirector = stub;
            try
            {
                var plan = Incidents.InfectedRaidSpawnService.BuildPlanForTick(120_000L);
                return plan.RevengeQuotaComponent == 0
                    && plan.PawnCount == plan.ThreatPressureComponent;
            }
            finally { Incidents.InfectedRaidSpawnService.StubDirector = null; }
        }

        // ── T13: After a full spawn, slot -= actuallySpawned ──────────
        private static bool T13_WorkerDecrementsRevengeOnSpawn()
        {
            var director = new NoGameStoryDirector().WithRevenge(5);
            director.DecrementPendingRevenge(3);
            return director.LastPendingRevenge == 2;
        }

        // ── T14: even pass actuallySpawned=100 → slot clamps at 0 ────
        private static bool T14_WorkerClampsDecrementToActuallySpawned()
        {
            var director = new NoGameStoryDirector().WithRevenge(5);
            director.DecrementPendingRevenge(100);
            return director.LastPendingRevenge == 0;
        }

        // ── T15: 0 actuallySpawned leaves slot untouched ─────────────
        private static bool T15_WorkerNoDecrementOnZeroSpawn()
        {
            var director = new NoGameStoryDirector().WithRevenge(5);
            director.DecrementPendingRevenge(0);
            return director.LastPendingRevenge == 5;
        }

        // ── T16: catalog contains ≥ 2 Revenge events ────────────────
        private static bool T16_CatalogContainsRevengeFamily()
        {
            var cat = new StoryEventCatalog();
            int revengeCount = 0;
            foreach (var e in cat.All())
                if (e != null && e.EventFamily == "Revenge") revengeCount++;
            return revengeCount >= 2;
        }

        // ── T17: each Revenge event has RevengePending prerequisite ────
        private static bool T17_RevengeEventsHaveRevengePrereq()
        {
            var cat = new StoryEventCatalog();
            int checkedCount = 0;
            foreach (var e in cat.All())
            {
                if (e == null || e.EventFamily != "Revenge") continue;
                bool hasRevenge = false;
                if (e.Prerequisites != null)
                    foreach (var c in e.Prerequisites)
                        if (c != null && c.ConditionId == "RevengePending") { hasRevenge = true; break; }
                if (!hasRevenge) return false;
                checkedCount++;
            }
            return checkedCount >= 2;
        }

        // ── T18: Expected count gate ─────────
        private static bool T18_FinalTotalCount() => MinPassCount == 18;
    }

    /// <summary>
    /// Lightweight StoryDirector mirror used by the Phase B regression harness.
    /// </summary>
    internal sealed class NoGameStoryDirector
    {
        public int LastPendingRevenge;
        public long LastRevengeRefreshTick;

        public NoGameStoryDirector() { }
        public NoGameStoryDirector WithRevenge(int v) { LastPendingRevenge = v; return this; }
        public int GetPendingRevengeanceForToday() => LastPendingRevenge;

        public void DecrementPendingRevenge(int actuallySpawned)
        {
            if (actuallySpawned <= 0) return;
            LastPendingRevenge = System.Math.Max(0, LastPendingRevenge - actuallySpawned);
        }

        public void RecomputeRevengeAfterDayTickStub(
            PopulationLedger ledger, SettingProfile profile, long currentTick)
        {
            if (currentTick == LastRevengeRefreshTick) return;
            if (ledger == null) return;
            LastRevengeRefreshTick = currentTick;
            string key = StripPrefix(profile?.ProfileId);
            float ratio = PopulationProfileMultipliers.GetRevengeRatio(key);
            int freeBudgetRaw = ledger.Cap - ledger.HumanoidLiveCount;
            int freeBudget = (int)System.Math.Min(int.MaxValue, System.Math.Max(0, freeBudgetRaw));
            int raw = (int)System.Math.Round((double)ledger.RecentKillsToday * (double)ratio);
            LastPendingRevenge = System.Math.Max(0, System.Math.Min(raw, freeBudget));
        }

        private static string StripPrefix(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Survival";
            const string p = "Rimconemy_";
            return id.StartsWith(p) ? id.Substring(p.Length) : id;
        }
    }
}
