using System.Collections.Generic;
using Rimconemy.Foundation.Colonials;
using RimWorld;
using Verse;

namespace Rimconemy.EconomyTerritory.Outposts
{
    /// <summary>
    /// Owner: Economy &amp; Territory (Package 04).
    /// P6 — Task 14: Outposts &amp; Proxy-Graph (Drei-Tage-Countdown).
    ///
    /// Live Edge-Tracker Service: maintains the "parent ↔ outpost" edge
    /// list plus the 180,000-tick (3 in-game-day) report-window invariant,
    /// and exposes <see cref="GetOverdueOutposts"/> so the UI / raid
    /// escalation paths can flag stale links. Outpost definition expansion
    /// and the proxy-graph 3-day countdown mechanic are owned by a User
    /// Live-Test phase.
    ///
    /// Spec: docs/P6-PROGRESS.md Task 14; ROADMAP 4.6 / Drei-Tage-Countdown.
    /// </summary>
    public static class OutpostProxyGraph
    {
        public struct ProxyEdge
        {
            public int ParentMapId;   // canonical home map
            public int OutpostMapId;  // outpost uniqueID
            public long EstablishedTick;
            public long LastReportedTick;
        }

        // Hard contract: 3 in-game days = 60_000 * 3 = 180_000 ticks.
        public const long MaxReportIntervalTicks = 180_000L;

        private static readonly List<ProxyEdge> _edges = new List<ProxyEdge>();

        public static IReadOnlyList<ProxyEdge> Edges => _edges;

        public static void EstablishEdge(int parentMapId, int outpostMapId, long currentTick)
        {
            _edges.Add(new ProxyEdge
            {
                ParentMapId = parentMapId,
                OutpostMapId = outpostMapId,
                EstablishedTick = currentTick,
                LastReportedTick = currentTick,
            });
        }

        public static void RecordReport(int outpostMapId, long tick)
        {
            for (int i = 0; i < _edges.Count; i++)
            {
                var e = _edges[i];
                if (e.OutpostMapId != outpostMapId) continue;
                e.LastReportedTick = tick;
                _edges[i] = e;
            }
        }

        /// <summary>Returns the subset of edges that have not reported within
        /// the 3-day window. Used for "stale outposts" UI / raid escalation.</summary>
        public static List<int> GetOverdueOutposts(long currentTick)
        {
            var overdue = new List<int>();
            foreach (var e in _edges)
            {
                if (currentTick - e.LastReportedTick > MaxReportIntervalTicks)
                    overdue.Add(e.OutpostMapId);
            }
            return overdue;
        }

        public static void Clear()
        {
            _edges.Clear();
        }
    }
}
