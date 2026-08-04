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
    /// Phase-6 Stub: prepares an idempotent SpawnPlan for
    /// <see cref="InfectedRaidWorker"/> so a future iteration can drop
    /// real infected colonists near the colony. The plan is derived
    /// from the threat aggregator so the spawn scales with pressure.
    ///
    /// The actual TryExecuteWorker determines spawn positions and
    /// deploys the infected pawn. This service is read-only: it
    /// produces numbers + categories. No exceptions escape so
    /// <see cref="InfectedRaidWorker"/> stays safe.
    ///
    /// Audit-Finding 6 (2026-08-04): the snapshot read now delegates to
    /// <see cref="ThreatSnapshotBridge"/> so neither this service nor
    /// <see cref="WorldRaidCoordinator"/> constructs duplicate
    /// ThreatAggregator instances.
    ///
    /// Spec: docs/P6-PROGRESS.md Task 12.
    /// </summary>
    public static class InfectedRaidSpawnService
    {
        public struct SpawnPlan
        {
            public int PawnCount;
            public float ThreatPressureComponent;
            public int MapId;        // -1 if no map
            public string Reason;
        }

        public static SpawnPlan BuildPlanForTick(long tick)
        {
            var plan = new SpawnPlan
            {
                PawnCount = 0,
                ThreatPressureComponent = 0f,
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
                int pawnCount = ComputeSpawnCount(pressure);
                plan.PawnCount = pawnCount;
                plan.ThreatPressureComponent = pressure;
                plan.MapId = canonical.uniqueID;
                plan.Reason = pawnCount > 0 ? "ok" : "pressure-too-low";
            }
            catch (System.Exception ex)
            {
                plan.Reason = "exception: " + ex.GetType().Name;
            }
            return plan;
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
