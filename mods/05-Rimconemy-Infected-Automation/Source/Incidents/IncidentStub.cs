using System.Collections.Generic;
using Verse;

namespace Rimconemy.InfectedAutomation.Incidents
{
    /// <summary>
    /// Owner: Infected &amp; Automation.
    /// Hostile-incident stub. Exactly one Raid provider per Full Profile;
    /// vanilla Wealth-Raids remain operational but are classified
    /// separately. DLC Quests/Anomaly Events are not consumed.
    /// SPIKE: API-INCIDENT-01.
    /// </summary>
    public sealed class IncidentStub
    {
        public const string LogMarker = "v0";
        public string IncidentId = "Rimconemy_InfectedRaidIncident";
        public int Strength;
        public string TargetTile;
        public string PathTile;
        public long SeedTick;
        public bool Resolved;
        public List<string> OutcomeTrace = new List<string>();
    }
}
