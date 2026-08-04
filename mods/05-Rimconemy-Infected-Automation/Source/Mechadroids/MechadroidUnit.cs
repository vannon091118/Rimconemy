using System.Collections.Generic;
using Verse;

namespace Rimconemy.InfectedAutomation.Mechadroids
{
    /// <summary>
    /// Owner: Infected &amp; Automation.
    /// Mechadroid is a separate unit domain, distinct from Vanilla Mechanoids
    /// (Biotech). Energy and Maintenance are tracked; MainBase-resident
    /// pawns do NOT count as Game-Over replacements.
    /// SPIKE: API-MECH-01 (IsMechanoid/Mechanitor interaction unverified).
    /// </summary>
    public enum MechadroidState { Idle, Working, Recharging, Damaged, Salvaged }

    public sealed class MechadroidUnit
    {
        public const string LogMarker = "v0";
        public string UnitId;
        public string OwnerId;
        public float Energy;
        public float MaxEnergy;
        public float Maintenance;
        public MechadroidState State = MechadroidState.Idle;
        public List<string> UpgradeMaterialHistory = new List<string>();
    }
}
