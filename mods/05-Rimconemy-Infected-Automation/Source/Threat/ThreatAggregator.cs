using System.Collections.Generic;
using Verse;

namespace Rimconemy.InfectedAutomation.Threat
{
    /// <summary>
    /// Owner: Infected &amp; Automation.
    /// Single snapshot aggregator. Reads Farm-, Population-, Production-,
    /// Power-, Defense-, Combat- and Regionalinputs via the late-bound
    /// servicebus - never directly from foreign Pawn or Map mutations.
    /// One RidProvider in the Full Profile; vanilla Wealth-Raids are
    /// separately classified, never silently merged.
    /// SPIKE: API-INCIDENT-01 / API-WORLD-01.
    /// </summary>
    public sealed class ThreatAggregator
    {
        public const string LogMarker = "v0";
        public string ScopeId;
        public float FarmActivity;
        public float PopulationActivity;
        public float ProductionActivity;
        public float PowerActivity;
        public float DefenseActivity;
        public float RegionalActivity;
        public float TotalPressure;
        public float Trend;
        public long LastUpdatedTick;
        public List<string> PendingRaidIds = new List<string>();
    }
}
