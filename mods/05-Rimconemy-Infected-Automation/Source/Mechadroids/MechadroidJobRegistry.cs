using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Mechadroids
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// P6 — Task 13: Mechadroids (Grundsystem).
    ///
    /// Phase-6 Stub: a static job-count registry. Real spawning is owned
    /// by a future user Live-Test phase. The registry exposes the
    /// current "intended" count of mechadroids per role + per skill so
    /// the dashboard can render them before actual spawn.
    ///
    /// Spec: docs/P6-PROGRESS.md Task 13.
    /// </summary>
    public static class MechadroidJobRegistry
    {
        public struct JobCount
        {
            public string JobDefName;
            public int ActiveUnitCount;
            public int PendingJobCount;
        }

        public enum MechJobKind
        {
            Hauler,
            Builder,
            Defender,
            Forager,
        }

        private static readonly Dictionary<MechJobKind, JobCount> _state
            = new Dictionary<MechJobKind, JobCount>();

        public static IReadOnlyDictionary<MechJobKind, JobCount> Snapshot()
        {
            return _state;
        }

        /// <summary>Registers an active mechadroid unit (e.g. on spawn).</summary>
        public static void RegisterUnit(MechJobKind kind, string jobDefName)
        {
            if (!_state.TryGetValue(kind, out var count))
            {
                count = new JobCount { JobDefName = jobDefName };
            }
            count.ActiveUnitCount++;
            count.JobDefName = jobDefName;
            _state[kind] = count;
        }

        /// <summary>Registers a pending (not yet deployed) mechdroid job.</summary>
        public static void RegisterPending(MechJobKind kind)
        {
            if (!_state.TryGetValue(kind, out var count))
            {
                count = new JobCount { JobDefName = string.Empty };
            }
            count.PendingJobCount++;
            _state[kind] = count;
        }

        public static void Clear()
        {
            _state.Clear();
        }
    }
}
