// Source/Inoculation/InoculationConverter.cs
//
// Owner: Infected & Automation (Package 05).
// Phase C — P6-PROGRESS §12 Tier-Inokulation.
//
// Pure helper that converts an InoculationCandidate into an
// InoculationOutcome describing what the live service will apply. The
// live side-effects (Faction-set, KindDef-set, Ledger-note) live in
// RandomInoculationService. Tests can assert on the outcome without
// instantiating a real Pawn.

using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Inoculation
{
    public static class InoculationConverter
    {
        /// <summary>Live Faction-defName that all converted wildlife lands in.</summary>
        public const string InfectedFactionDefName = "Rimconemy_HiddenInfectedFaction";

        /// <summary>
        /// Hybrid branded-PawnKind-defName. Runtime keeps the original
        /// Race (Wolf/Bear/etc.) but the kindDef is replaced with this
        /// generic infected-wildlife kind so any UI/health-bar surface
        /// that reads <c>pawn.kindDef.defName</c> sees a consistent
        /// "infected" label.
        /// </summary>
        public const string BrandedKindDefName = "Rimconemy_InfectedWildlife";

        /// <summary>
        /// Build an InoculationOutcome from a successful candidate. The
        /// <paramref name="kindMappingTableHit"/> flag determines whether
        /// the converter emits the branded KindDef-Name (true) or falls
        /// back to the original KindDef (false → original-kind remains,
        /// only Faction switches). Caller decides via DefDatabase lookup.
        /// </summary>
        public static InoculationOutcome Convert(
            InoculationCandidate candidate,
            bool kindMappingTableHit,
            string reason)
        {
            return new InoculationOutcome
            {
                ThingId = candidate.ThingId,
                OriginalKindDefName = candidate.KindDefName,
                OriginalRaceDefName = candidate.RaceDefName,
                ConvertedFactionDef = InfectedFactionDefName,
                ConvertedKindDefName = kindMappingTableHit
                    ? BrandedKindDefName
                    : candidate.KindDefName,
                // AnimalHalfCap: one head = 0.5 cap-units; rounded up to
                // 1 so the Ledger stays integral. The full effective free
                // budget is read via PopulationLedger.GetTotalCapBudget.
                EffectiveCapDelta = 1,
                Reason = reason ?? "selected",
                Source = candidate,
            };
        }

        /// <summary>
        /// Pure half-cap delta. Returns 1 if the new animal count is
        /// strictly greater than the previous count; 0 otherwise. The
        /// actual fractional 0.5-per-head rule is enforced by the
        /// PopulationLedger.GetTotalCapBudget() formulation so this
        /// helper just signals the integer-step bump.
        /// </summary>
        public static int ComputeAnimalHalfCapDelta(
            int previousAnimalCount,
            int newAnimalCount)
        {
            if (newAnimalCount <= previousAnimalCount) return 0;
            return 1;
        }
    }
}
