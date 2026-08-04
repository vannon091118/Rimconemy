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
        /// Returns the active ThreatAggregator instance owned by Story /
        /// Founders, or a stub with TotalPressure=0 when no aggregator is
        /// currently registered. Stub callers should treat the zero
        /// snapshot as "no pressure, no spawn".
        /// </summary>
        private static ThreatAggregator GetCurrentThreatSnapshot()
        {
            // The ThreatAggregator today is a passive record carried by
            // the StoryDirector pipeline (LastSnapshot) — we mirror its
            // TotalPressure. If StoryDirector is missing or hasn't run,
            // we fall back to zero so callers can still complete.
            try
            {
                Story.StoryDirector director = Story.StoryDirector.Get();
                if (director?.LastSnapshot == null) return null;
            }
            catch (System.Exception) { return null; }
            // StoryDirector.LastSnapshot is SituationSnapshot, not
            // ThreatAggregator. We synthesise a stub with the proxy
            // pressure so the caller's pressure&gt;0 path triggers
            // a sensible spawn count.
            return new ThreatAggregator { TotalPressure = PressureFromDirector() };
        }

        private static float PressureFromDirector()
        {
            try
            {
                var d = Story.StoryDirector.Get();
                return d?.LastSnapshot?.ThreatPressure ?? 0f;
            }
            catch (System.Exception) { return 0f; }
        }
    }
}
