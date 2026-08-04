using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Threat;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// P6 — Task 15: Weltkarten-Endgame (World-Raids).
    ///
    /// Phase-6 Stub: aggregates per-tile threat from the world layer and
    /// produces a "raid window" describing when the next world-raid
    /// should fire. WorldObject registration is owned by a User Live-Test
    /// phase.
    ///
    /// Spec: docs/P6-PROGRESS.md Task 15.
    /// </summary>
    public static class WorldRaidCoordinator
    {
        public struct RaidWindow
        {
            public int TargetTile;
            public int CountdownTicks;
            public float PerTilePressure;
        }

        public static List<RaidWindow> PlanWorldRaids(long currentTick)
        {
            var results = new List<RaidWindow>();
            try
            {
                if (Current.Game == null || Find.World == null) return results;

                // We plan a world-raid per player base map (per Map.uniqueID,
                // tile-equivalence is established by Map.Tile).
                var maps = Find.Maps;
                if (maps == null) return results;

                var snapshot = LatestThreatSnapshot();
                float pressure = snapshot?.TotalPressure ?? 0f;

                foreach (var m in maps)
                {
                    if (m == null) continue;
                    if (m.IsPlayerHome == false && m.ParentHolder == null) continue;

                    int countdown = ComputeCountdown(pressure);
                    results.Add(new RaidWindow
                    {
                        TargetTile = m.Tile,
                        CountdownTicks = countdown,
                        PerTilePressure = pressure,
                    });
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] PlanWorldRaids exception: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
            return results;
        }

        // Pressure-driven countdown (Phase-6 MVP): higher pressure → shorter
        // countdown. Calendar reference: 1 in-game day = 60,000 ticks.
        private static int ComputeCountdown(float pressure)
        {
            if (pressure >= 0.6f) return 60_000 / 4;   // 6 in-game hours
            if (pressure >= 0.4f) return 60_000 / 2;   // 12 in-game hours
            if (pressure >= 0.2f) return 60_000;       // 1 day
            return 60_000 * 3;                          // 3 days baseline
        }

        /// <summary>Resolves the latest threat snapshot via ThreatAggregator / StoryDirector bridge.</summary>
        private static ThreatAggregator LatestThreatSnapshot()
        {
            try
            {
                var d = Story.StoryDirector.Get();
                if (d?.LastSnapshot == null) return null;
                return new ThreatAggregator { TotalPressure = d.LastSnapshot.ThreatPressure };
            }
            catch (System.Exception) { return null; }
        }
    }
}
