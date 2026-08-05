using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    /// <summary>
    /// Shared utility for materializing/accessing the hidden infected faction.
    /// Used by both ScenPart_RimconemyStartEnemies (starter spawn) and
    /// InfectedRaidWorker (raid spawn) to guarantee a live faction instance
    /// exists even when the factionDef has hidden=true.
    /// </summary>
    public static class InfectedFactionUtility
    {
        public const string HiddenFactionDefName = "Rimconemy_HiddenInfectedFaction";

        /// <summary>
        /// Returns a live instance of the hidden infected faction, creating it
        /// on demand if it doesn't exist yet. Never throws; returns null only
        /// if the FactionDef itself is missing (def database error).
        /// </summary>
        public static Faction EnsureHiddenInfectedFaction()
        {
            // Fast path: already materialized
            var existing = Find.FactionManager?.AllFactionsListForReading?
                .FirstOrDefault(f => f?.def?.defName == HiddenFactionDefName);
            if (existing != null) return existing;

            // Load the FactionDef
            var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(HiddenFactionDefName);
            if (factionDef == null)
            {
                Log.Error($"[Rimconemy.InfectedAutomation] InfectedFactionUtility: FactionDef '{HiddenFactionDefName}' missing from DefDatabase.");
                return null;
            }

            // Materialize a live instance. hidden=true on the def is preserved.
            var faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(factionDef));
            if (faction != null)
            {
                Log.Message($"[Rimconemy.InfectedAutomation] InfectedFactionUtility: Materialized hidden faction '{HiddenFactionDefName}' (loadID={faction.loadID}).");
            }
            return faction;
        }
    }
}
