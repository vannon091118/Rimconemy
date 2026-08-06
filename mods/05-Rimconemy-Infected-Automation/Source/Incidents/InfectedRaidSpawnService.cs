using Rimconemy.InfectedAutomation.Story;
using Rimconemy.InfectedAutomation.Threat;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Incidents
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// P6 — Task 12: Infizierten-Raids (echter Spawn-Pfad).
    ///
    /// Idempotent SpawnPlan-Builder for <see cref="InfectedRaidWorker"/>:
    /// derives the next-tick plan from the threat aggregator so the
    /// spawn scales with pressure, then merges in a Phase-B revenge-pending
    /// quota sourced from <see cref="Story.StoryDirector.LastPendingRevenge"/>.
    /// Pawn count is the higher of pressure-plan and revenge-floor; the
    /// reason string distinguishes which path drove the merge.
    ///
    /// The actual TryExecuteWorker still determines spawn positions and
    /// deploys the infected pawn; this service is read-only — it produces
    /// numbers + categories and swallows no exceptions so
    /// <see cref="InfectedRaidWorker"/> stays safe.
    ///
    /// Audit-Finding 6 (2026-08-04): the snapshot read delegates to
    /// <see cref="Threat.ThreatSnapshotBridge"/> so neither this service
    /// nor <see cref="World.WorldRaidCoordinator"/> constructs duplicate
    /// ThreatAggregator instances.
    ///
    /// Spec: docs/P6-PROGRESS.md Task 12; docs/superpowers/specs/2026-08-05-daily-growth-revenge-design.md §5+§6.
    /// </summary>
    public static class InfectedRaidSpawnService
    {
        public struct SpawnPlan
        {
            public int PawnCount;
            public float ThreatPressureComponent;
            /// <summary>Phase B: transient revenge-pending floor that drove
            /// the merge. 0 if no revenge slot was active. Always equals
            /// the value read from StoryDirector.GetPendingRevengeanceForToday()
            /// (or the StubDirector override) at plan-time.</summary>
            public int RevengeQuotaComponent;
            public int MapId;        // -1 if no map
            public string Reason;
        }

        /// <summary>Test-Seam: when non-null, BuildPlanForTick reads the
        /// pending revenge quota from this stub instead of the live
        /// StoryDirector (so regression tests do not need a running
        /// GameComponent). Default null = Produktivverhalten. Reset to
        /// null by the Boot RunAll wipe path.</summary>
        public static DirectorAccessStub StubDirector;

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
                // Test-Seam: StubDirector erlaubt Revenge-Only-Plan ohne
                // Current.Game/Map (Regression-Tests). Live-Pfad braucht
                // Current.Game für ThreatSnapshot + Map-Read.
                if (Current.Game == null)
                {
                    if (StubDirector != null)
                    {
                        int revengeOnly = ReadRevengePending();
                        plan.PawnCount = revengeOnly;
                        plan.RevengeQuotaComponent = revengeOnly;
                        plan.Reason = revengeOnly > 0 ? "revenge-dominant" : "ok";
                        return plan;
                    }
                    return plan;
                }

                Map canonical = Find.AnyPlayerHomeMap;
                if (canonical == null && Find.Maps != null && Find.Maps.Count > 0)
                    canonical = Find.Maps[0];
                if (canonical == null) { plan.Reason = "no-map"; return plan; }

                var snapshot = GetCurrentThreatSnapshot();
                float pressure = snapshot?.TotalPressure ?? 0f;
                int pressurePlan = ComputeSpawnCount(pressure);

                // Phase B: merge with revenge-pending floor. Higher-of-both
                // semantics — a revenge quota can lift a non-event pressure
                // into a real spawn, but a hot pressure-plan still wins over
                // a stale revenge slot.
                int revengePlan = ReadRevengePending();

                plan.PawnCount = System.Math.Max(pressurePlan, revengePlan);
                plan.ThreatPressureComponent = pressure;
                plan.RevengeQuotaComponent = revengePlan;
                plan.MapId = canonical.uniqueID;
                plan.Reason = MergeReason(pressurePlan, revengePlan);
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
            return live != null ? live.GetPendingRevengeanceForToday() : 0;
        }

        // Reason metadata so the dashboard / log can see WHICH path drove
        // the spawn when both are non-zero.
        private static string MergeReason(int pressurePlan, int revengePlan)
        {
            if (revengePlan > pressurePlan) return "revenge-dominant";
            if (pressurePlan > 0) return "pressure-based";
            if (revengePlan > 0) return "revenge-fallback"; // would never elect to spawn because of revenge-only when pressurePlan==0; defensive
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

        /// <summary>
        /// Returns the latest threat snapshot via the central
        /// <see cref="Threat.ThreatSnapshotBridge"/>. Returns null when no
        /// StoryDirector snapshot is available.
        /// </summary>
        private static ThreatAggregator GetCurrentThreatSnapshot()
        {
            // Audit-Finding 6 (2026-08-04) consolidation: single-source read.
            return Threat.ThreatSnapshotBridge.GetLatest();
        }
    }
}
