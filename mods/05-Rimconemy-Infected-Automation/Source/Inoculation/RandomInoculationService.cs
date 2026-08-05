// Source/Inoculation/RandomInoculationService.cs
//
// Owner: Infected & Automation (Package 05).
// Phase C — P6-PROGRESS §12 Tier-Inokulation Service-Schicht.
//
// Façade over the deterministic selector + converter that performs the
// real Map/Pawn side-effects:
//   1. capture candidate list from map.mapPawns.AllPawnsSpawned,
//   2. gate via Profile (Refuge=0 → noop), Cooldown via Ledger,
//   3. compute deterministic seed,
//   4. select one candidate,
//   5. describe the conversion via InoculationConverter,
//   6. apply live Faction/KindDef-switch (RimWorld 1.6 API),
//   7. record on PopulationLedger.NoteInoculation.
//
// The StoryDirector Day-Tick calls this once per day-cycle.

using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Inoculation
{
    public static class RandomInoculationService
    {
        /// <summary>
        /// Attempts to convert one wild animal into an infected-wildlife
        /// pawn. Returns the outcome (with reason "selected", "no-candidates",
        /// "cooldown", "profile-blocks") on success, null on no-op states.
        ///
        /// Pure failure-modes (do not throw):
        ///   • map == null → null + Log.Warning
        ///   • Profile == "Refuge" or InoculationsPerDay == 0 → null + Log.Message
        ///   • Cooldown gate elapsed? → null + Log.Message
        ///   • No eligible candidate after FilterCandidates → null + Log.Message
        /// </summary>
        public static InoculationOutcome? TryInfectRandom(Map map, long currentTick)
        {
            // Defensive: missing map or unload.
            PopulationLedger ledger = PopulationLedger.Get();
            string profileId = ledger?.ProfileId
                ?? PopulationProfileMultipliers.ProfileSurvival;

            int profileQuota = PopulationProfileMultipliers.GetInoculationsPerDay(profileId);
            if (profileQuota <= 0)
            {
                Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService: profile '" + profileId
                    + "' InoculationsPerDay == 0 → skipping cycle.");
                return null;
            }
            if (map == null)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfectRandom(map=null); ignored.");
                return null;
            }

            // Cooldown gate (Phase A §Animal-Inokulation-Datenflow).
            if (ledger == null || !ledger.IsInoculationCooldownElapsed())
            {
                Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService: cooldown gate active for profile '"
                    + profileId + "' → skipping cycle.");
                return null;
            }

            // Build candidate list once.
            IReadOnlyList<InoculationCandidate> candidates = BuildCandidateListFromMap(map);
            InoculationSelectorLogic.FilterCandidates(candidates, out var filtered);
            if (filtered == null || filtered.Count == 0)
            {
                Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService: no eligible wild animals on map.uniqueID="
                    + map.uniqueID + "; skipping.");
                return null;
            }

            // Deterministic seed → candidate.
            int seed = InoculationSelectorLogic.BuildInoculationSeed(
                profileId, map.uniqueID, currentTick, ledger.GetTotalCapBudget());
            var picked = InoculationSelectorLogic.SelectCandidate(filtered, seed, currentTick);
            if (!picked.HasValue)
            {
                Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService: selector returned null post-filter; edge case.");
                return null;
            }

            // Live side-effects (RimWorld 1.6).
            InoculationOutcome outcome = ApplyLiveConversion(picked.Value, ledger);
            return outcome;
        }

        /// <summary>
        /// Walk the live map and produce an InoculationCandidate snapshot
        /// list. Animal-only filter is applied here as well as in
        /// FilterCandidates so the Service output is consistent with
        /// the user's spec ("alle Wild-Tiere auf Map").
        /// </summary>
        public static IReadOnlyList<InoculationCandidate> BuildCandidateListFromMap(Map map)
        {
            var list = new List<InoculationCandidate>();
            if (map?.mapPawns == null) return list;
            var all = map.mapPawns.AllPawnsSpawned;
            if (all == null) return list;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p == null || p.Dead) continue;
                if (p.RaceProps == null) continue;
                if (p.RaceProps.Humanlike) continue;
                if (!p.RaceProps.Animal) continue;
                // Skip already-infected.
                if (p.Faction != null && p.Faction.def != null
                    && p.Faction.def.defName == InoculationSelectorLogic.InfectedFactionDefName)
                    continue;
                list.Add(new InoculationCandidate
                {
                    ThingId = p.ThingID ?? "<no-id>",
                    KindDefName = p.kindDef?.defName ?? "<no-kind>",
                    RaceDefName = p.def?.defName ?? "<no-def>",
                    OriginalFactionDef = p.Faction?.def?.defName ?? "<no-faction>",
                    IsAnimal = true,
                    IsHumanlike = false,
                    IsDead = false,
                    MapCell = p.Position,
                });
            }
            return list;
        }

        /// <summary>
        /// Apply the kindDef / faction conversion + record on ledger.
        /// Returns the InoculationOutcome. Pure-Pawn mutations in one
        /// try/catch so a single failing animal does not crash the
        /// GameComponent-Tick.
        /// </summary>
        private static InoculationOutcome ApplyLiveConversion(
            InoculationCandidate candidate,
            PopulationLedger ledger)
        {
            bool mappingHit = ResolveBrandedKindDef(out PawnKindDef branded);
            var outcome = InoculationConverter.Convert(candidate, mappingHit, "selected");

            try
            {
                Map map = Find.AnyPlayerHomeMap;
                Pawn livePawn = TryFindLivePawn(candidate.ThingId, map);
                if (livePawn == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] RandomInoculationService: live pawn lookup failed for ThingID="
                        + candidate.ThingId + "; recording outcome without live mutation.");
                    ledger?.NoteInoculation(candidate.KindDefName);
                    return outcome;
                }

                FactionDef infectedFactionDef = DefDatabase<FactionDef>.GetNamedSilentFail(
                    InoculationConverter.InfectedFactionDefName);
                if (infectedFactionDef == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] RandomInoculationService: FactionDef '"
                        + InoculationConverter.InfectedFactionDefName + "' missing in DefDatabase; recording outcome without live mutation.");
                    ledger?.NoteInoculation(candidate.KindDefName);
                    return outcome;
                }
                Faction infectedFaction = Find.FactionManager?.FirstFactionOfDef(infectedFactionDef);
                if (infectedFaction == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] RandomInoculationService: live Faction instance for '"
                        + InoculationConverter.InfectedFactionDefName + "' missing; recording outcome without live mutation.");
                    ledger?.NoteInoculation(candidate.KindDefName);
                    return outcome;
                }

                if (mappingHit && branded != null)
                {
                    livePawn.kindDef = branded;
                }
                livePawn.SetFaction(infectedFaction);
                ledger?.NoteInoculation(candidate.KindDefName);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] RandomInoculationService.ApplyLiveConversion failed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
            return outcome;
        }

        private static bool ResolveBrandedKindDef(out PawnKindDef branded)
        {
            branded = DefDatabase<PawnKindDef>.GetNamedSilentFail(InoculationConverter.BrandedKindDefName);
            return branded != null;
        }

        private static Pawn TryFindLivePawn(string thingId, Map map)
        {
            if (map?.mapPawns == null || string.IsNullOrEmpty(thingId)) return null;
            var all = map.mapPawns.AllPawnsSpawned;
            if (all == null) return null;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p != null && p.ThingID == thingId) return p;
            }
            return null;
        }
    }
}
