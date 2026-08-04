using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Incidents
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    ///
    /// Phase 5 / Vanilla-/DLC-Incident-Klassifikation:
    /// Walk <see cref="DefDatabase{T}"/> of <see cref="IncidentDef"/> and
    /// bucket each def into one of:
    ///   - Rimconemy      : defName starts with "Rimconemy_"
    ///   - Vanilla        : defName does NOT start with the prefix
    ///   - DLC / Quest    : defName matches DLC prefixes
    ///
    /// We also enforce the "exactly one InfectedProvider in Full Profile"
    /// rule by counting the Rimconemy_InfectedRaidIncident references
    /// in the DefDatabase and logging a violation if the count is not 1.
    ///
    /// Specification: docs/H4-storage-query-contract.md §3 (broadly
    /// inherits to incidents), ROADMAP §5 (Phase 5 — Story-Ausführung
    /// und Vanilla-/DLC-Adapter).
    /// </summary>
    public static class IncidentClassifier
    {
        private const string RimconemyPrefix = "Rimconemy_";
        private const string InfectedProviderDefName = "Rimconemy_InfectedRaidIncident";

        public enum IncidentSource
        {
            Rimconemy,
            Vanilla,
            DlcOrQuest,
        }

        public struct IncidentBucket
        {
            public string DefName;
            public IncidentSource Source;
            public string Category;
            public string WorkerClassName;
        }

        /// <summary>
        /// Walks the active IncidentDef database and returns one entry
        /// per def. Safe to call even when no Incidents are loaded.
        /// </summary>
        public static List<IncidentBucket> EnumerateAll()
        {
            var result = new List<IncidentBucket>();
            var defs = DefDatabase<IncidentDef>.AllDefsListForReading;
            if (defs == null) return result;

            foreach (var def in defs)
            {
                if (def == null) continue;

                IncidentSource src;
                if (def.defName != null && def.defName.StartsWith(RimconemyPrefix))
                    src = IncidentSource.Rimconemy;
                else if (LooksLikeDlc(def))
                    src = IncidentSource.DlcOrQuest;
                else
                    src = IncidentSource.Vanilla;

                result.Add(new IncidentBucket
                {
                    DefName = def.defName,
                    Source = src,
                    Category = def.category?.defName ?? "<none>",
                    WorkerClassName = def.workerClass?.Name ?? "<null>",
                });
            }
            return result;
        }

        /// <summary>
        /// Counts how many Rimconemy defs match the InfectedProvider
        /// rule. The Full-Profile contract requires exactly one.
        /// </summary>
        public static int CountInfectedProviders()
        {
            int count = 0;
            var defs = DefDatabase<IncidentDef>.AllDefsListForReading;
            if (defs == null) return 0;

            foreach (var def in defs)
            {
                if (def == null) continue;
                if (def.defName == InfectedProviderDefName) count++;
            }
            return count;
        }

        /// <summary>
        /// Returns true when the Full Profile invariant is satisfied:
        /// exactly one InfectedRaidIncident is loaded.
        /// Violations are reported via Log.Warning but never crash.
        /// </summary>
        public static bool ValidateOneInfectedProvider()
        {
            int count = CountInfectedProviders();
            if (count == 1)
            {
                return true;
            }
            Log.Warning(
                "[Rimconemy.InfectedAutomation] IncidentClassifier: Full-Profile invariant violation. " +
                "Exactly one Rimconemy_InfectedRaidIncident expected, found " + count + ".");
            return false;
        }

        // Heuristic: an incident is "DLC or quest" when its workerClass
        // is hosted in a DLC namespace (Ideology, Biotech, ...) or its
        // category points at one of the DLC ritual/quest categories.
        private static bool LooksLikeDlc(IncidentDef def)
        {
            if (def?.workerClass != null)
            {
                string ns = def.workerClass.Namespace ?? string.Empty;
                if (ns.Contains("Ideology") || ns.Contains("Biotech")
                    || ns.Contains("Anomaly") || ns.Contains("Royalty")
                    || ns.Contains("Odyssey"))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
