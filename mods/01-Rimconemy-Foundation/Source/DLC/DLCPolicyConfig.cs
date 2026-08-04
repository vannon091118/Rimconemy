using System.Linq;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.DLC
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    ///
    /// Phase-7 DLC-Content-Policy-Layer, Phase-2 Runtime-Layer (2026-08-04):
    /// Statischer Loader für die Phase-2-Override-Schicht.
    ///
    /// ## Funktion
    ///
    /// <see cref="ApplyFromLoadedDefs"/> iteriert alle im DefDatabase
    /// registrierten <see cref="DLCContentPolicyDef"/>-Defs und wendet jeden
    /// Override auf die statischen Felder von <see cref="DLCContentPolicy"/>
    /// an. Bei mehreren Defs gewinnt pro Field der zuletzt angewandte
    /// Override (last-writer-wins).
    ///
    /// ## Aufruf-Punkt
    ///
    /// Foundation-Bootstrap ruft einmal <see cref="ApplyFromLoadedDefs"/>
    /// nach <see cref="DLCFilter.EmitBootstrapSummary"/> auf. Bei Re-Apply
    /// (Hot-Reload) ist die Methode idempotent.
    ///
    /// Wenn der DefDatabase leer ist (kein DLCContentPolicyDef gefunden),
    /// läuft die Phase-1-Logik unverändert weiter mit hartkodierten Defaults.
    ///
    /// ## Cache
    ///
    /// Nach erfolgreichem Apply wird <see cref="DLCFilter.InvalidateCache"/>
    /// aufgerufen damit Konsumenten, die vor dem Override abgefragt haben,
    /// die neuen Werte sehen. Ohne Invalidate würden die alten
    /// Cache-Einträge weiterwirken.
    /// </summary>
    public static class DLCPolicyConfig
    {
        /// <summary>
        /// Recommended defName-Prefix. Defs mit diesem Prefix werden typisch
        /// in Defs/DLCContentPolicy_*.xml angelegt.
        /// </summary>
        public const string DefNamePrefix = "Rimconemy_DLCContentPolicy_";

        /// <summary>
        /// Apply alle DLCContentPolicyDef-Overrides aus dem DefDatabase auf
        /// die statischen Felder von DLCContentPolicy.
        /// </summary>
        /// <returns>Summe der überschriebenen Felder über alle angewandten Defs.
        /// 0 wenn keine Override-Defs geladen sind.</returns>
        public static int ApplyFromLoadedDefs()
        {
            var defs = DefDatabase<DLCContentPolicyDef>.AllDefsListForReading;
            if (defs == null || defs.Count == 0)
            {
                // Kein Override-Def -> Phase-1-Werte bleiben aktiv. Kein Log
                // weil das der Normalfall ist (kein Override gewünscht).
                return 0;
            }

            int totalApplied = 0;
            int overrideDefCount = 0;
            foreach (var def in defs)
            {
                if (def == null) continue;
                totalApplied += def.ApplyToPolicy();
                overrideDefCount++;
            }

            // Cache invalidieren damit Konsumenten die neuen Werte sehen.
            // Der DLCFilter._cache ist nicht thread-safe (single-threaded-
            // GameLoop reicht), aber Invalidate ist eine einmal-pro-Boot-
            // Operation.
            DLCFilter.InvalidateCache();

            if (overrideDefCount > 0)
            {
                Log.Message(
                    $"[Rimconemy.Foundation] DLCPolicyConfig: applied {overrideDefCount} override-def(s), " +
                    $"{totalApplied} field-overrides total.");
            }

            return totalApplied;
        }
    }
}
