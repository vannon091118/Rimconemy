// Source/Inoculation/InoculationSelectorLogic.cs
//
// Owner: Infected & Automation (Package 05).
// Phase C — P6-PROGRESS §12 Tier-Inokulation.
//
// Pure deterministic selector for "which wild animal does the
// RandomInoculationService convert this cycle?". Behind a small
// FNV-1a-derived seed and the DeterministicRng (Phase A helper), the
// selector returns one InoculationCandidate per call OR null when no
// candidate passes the filter.
//
// Determinism contract (Phase A spec §Determinismus): same
// (profileId, mapId, currentTick/60000, populationFingerprint) → same
// selected index → same InoculationOutcome.ConvertedKindDefName after
// the converter (Phase C-Task 3).

using System.Collections.Generic;
using System.Globalization;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Inoculation
{
    public static class InoculationSelectorLogic
    {
        /// <summary>
        /// DefName of the Hidden-Infected faction; mirrored locally to
        /// avoid an import cycle on InoculationConverter (which lives in
        /// a sibling file). Keep both in lockstep.
        /// </summary>
        public const string InfectedFactionDefName = "Rimconemy_HiddenInfectedFaction";

        /// <summary>
        /// FNV-1a-32 hash of "{profileId}|{mapId}|{dayIndex}|{fingerprint}".
        /// Stable across save/load because inputs are persisted or
        /// deterministic (ProfileId, map uniqueID, currentTick/60000,
        /// ledger-derived fingerprint).
        /// </summary>
        public static int BuildInoculationSeed(
            string profileId, int mapId, long currentTick, int populationFingerprint)
        {
            long dayIndex = currentTick / 60000L;
            string payload = string.Join("|",
                profileId ?? PopulationProfileMultipliers.ProfileSurvival,
                mapId.ToString(CultureInfo.InvariantCulture),
                dayIndex.ToString(CultureInfo.InvariantCulture),
                populationFingerprint.ToString(CultureInfo.InvariantCulture));
            return DeterministicRng.GetStableHashCode(payload);
        }

        /// <summary>
        /// Filter the input list into a new list of candidates that are
        /// animal, alive, and not already part of an infected faction.
        /// </summary>
        public static void FilterCandidates(
            IReadOnlyList<InoculationCandidate> all,
            out IReadOnlyList<InoculationCandidate> filtered)
        {
            if (all == null)
            {
                filtered = System.Array.Empty<InoculationCandidate>();
                return;
            }
            var list = new List<InoculationCandidate>(all.Count);
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i];
                if (c.IsDead) continue;
                if (!c.IsAnimal) continue;
                if (c.IsHumanlike) continue;
                if (c.OriginalFactionDef == InfectedFactionDefName) continue;
                list.Add(c);
            }
            filtered = list;
        }

        /// <summary>
        /// Select one candidate from <paramref name="candidates"/> using
        /// <paramref name="seed"/> as RNG seed. Sort by ThingId Ordinal
        /// before roll so different enumeration orders produce identical
        /// outcomes. Returns null when the list is empty.
        /// </summary>
        public static InoculationCandidate? SelectCandidate(
            IReadOnlyList<InoculationCandidate> candidates,
            int seed,
            long currentTick)
        {
            if (candidates == null || candidates.Count == 0) return null;

            var sorted = new List<InoculationCandidate>(candidates);
            sorted.Sort((a, b) => string.Compare(
                a.ThingId ?? "", b.ThingId ?? "",
                System.StringComparison.Ordinal));

            var rng = new DeterministicRng(seed);
            int idx = rng.NextInt(sorted.Count);
            return sorted[idx];
        }
    }
}
