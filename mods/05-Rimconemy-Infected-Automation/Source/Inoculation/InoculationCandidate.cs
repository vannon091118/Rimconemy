// Source/Inoculation/InoculationCandidate.cs
//
// Owner: Infected & Automation (Package 05).
// Phase C — P6-PROGRESS §12 Tier-Inokulation.
//
// Plain-data DTOs (struct) used by InoculationSelectorLogic to identify
// a wild animal candidate, and by InoculationConverter to describe the
// result of a successful conversion. No behaviour, no RimWorld state.
// Analog to PopulationLedgerReconciler.PawnSnapshot.

using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Inoculation
{
    /// <summary>
    /// Snapshot of a single wild animal candidate. Built from
    /// <c>map.mapPawns.AllPawnsSpawned</c> by the candidate-builder
    /// inside the service; consumed by the deterministic selector.
    /// </summary>
    public struct InoculationCandidate
    {
        /// <summary>RimWorld ThingID, used as a stable identifier.</summary>
        public string ThingId;
        /// <summary>Original PawnKind-defName (e.g. "Wolf", "Caribou", "Thrumbo").</summary>
        public string KindDefName;
        /// <summary>Race-defName (used for telemetry / debugging — kept for diagnostics).</summary>
        public string RaceDefName;
        /// <summary>Original Faction-defName (e.g. "WildFaction").</summary>
        public string OriginalFactionDef;
        /// <summary>Always false for Inject candidates; double-check on filter.</summary>
        public bool IsHumanlike;
        /// <summary>Always true for Inject candidates; double-check on filter.</summary>
        public bool IsAnimal;
        /// <summary>Dead candidates are filtered out before selection.</summary>
        public bool IsDead;
        /// <summary>Map cell (informational only; not used for selection).</summary>
        public IntVec3 MapCell;
    }

    /// <summary>
    /// Description of the conversion that the service will apply to a
    /// successful candidate. The service itself does the live Faction-
    /// and KindDef-switch; this struct documents the intent so tests
    /// can assert without instantiating real Pawns.
    /// </summary>
    public struct InoculationOutcome
    {
        /// <summary>Same as the Source.ThingId.</summary>
        public string ThingId;
        public string OriginalKindDefName;
        public string OriginalRaceDefName;
        public string ConvertedFactionDef;
        public string ConvertedKindDefName;
        /// <summary>
        /// Half-Cap delta in cap-units. One animal head consumes 0.5 cap
        /// units (rounded up to 1 here so the Ledger stays integral).
        /// </summary>
        public int EffectiveCapDelta;
        /// <summary>Diagnostic string: "selected", "no-candidates", "cooldown", "profile-blocks".</summary>
        public string Reason;
        public InoculationCandidate? Source;
    }
}
