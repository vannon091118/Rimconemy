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
        /// Phase E — Driver-Variante: konvertiert bis zu <paramref name="maxCount"/>
        /// Tiere in einem einzigen Service-Call. Pro Tag (gestempelt via
        /// <paramref name="currentTick"/>) ruft die Driver-Pipeline diese
        /// Methode mit dem ShouldFireToday-Outcome auf.
        /// </summary>
        public static int TryInfectWildAnimals(int maxCount, long currentTick)
        {
            if (maxCount <= 0) return 0;
            if (Current.Game == null) return 0;

            try
            {
                PopulationLedger ledger = PopulationLedger.Get();
                string profileId = ledger?.ProfileId
                    ?? PopulationProfileMultipliers.ProfileSurvival;

                int profileQuota = PopulationProfileMultipliers.GetInoculationsPerDay(profileId);
                if (profileQuota <= 0)
                {
                    Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfectWildAnimals: profile '"
                        + profileId + "' InoculationsPerDay == 0 → skipping cycle.");
                    return 0;
                }

                Map map = Find.AnyPlayerHomeMap;
                if (map == null)
                {
                    Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfectWildAnimals: no player home map.");
                    return 0;
                }

                if (ledger == null || !ledger.IsInoculationCooldownElapsed())
                {
                    Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfectWildAnimals: cooldown gate active for profile '"
                        + profileId + "' → skipping.");
                    return 0;
                }

                IReadOnlyList<InoculationCandidate> candidates = BuildCandidateListFromMap(map);
                InoculationSelectorLogic.FilterCandidates(candidates, out var filtered);
                if (filtered == null || filtered.Count == 0) return 0;

                int actually = 0;
                int hardCeiling = System.Math.Min(maxCount, profileQuota);
                for (int i = 0; i < filtered.Count && actually < hardCeiling; i++)
                {
                    var picked = filtered[i];
                    // ApplyLiveConversion never throws (try/catch internal);
                    // a successful call counts toward `actually`. Candidates
                    // already filtered to non-infected animals so a real
                    // conversion always happens.
                    ApplyLiveConversion(picked, ledger);
                    actually++;
                }
                Log.Message("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfectWildAnimals: requested="
                    + maxCount + " cap=" + profileQuota + " converted=" + actually + " tick=" + currentTick);
                return actually;
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] RandomInoculationService.TryInfectWildAnimals exception: "
                    + ex.GetType().Name + ": " + ex.Message);
                return 0;
            }
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

                // Phase E — apply MoveSpeed-Booster + Infection-Marker Hediff.
                // Der Hediff macht das Tier (a) sichtbar als "infiziert" im
                // Health-Tab und (b) +50% schneller, sodass die aggressive-AI
                // einen spürbaren Bewegungs-Vorteil gegenüber unbefallenen
                // Tieren hat. Persistent bis pawn.Destroyed (Health-Hediffs
                // werden über den Pawn's Scribe-Strom mitgepeichert, daher
                // save/load-safe).
                TryApplyInfectionAggressionHediff(livePawn);

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

        // Phase E — apply the aggression hediff so the converted wildlife
        // gains +50 % MoveSpeed and a visible "infected wildlife" health-tab
        // marker. Idempotent: if the pawn already has the hediff, we skip
        // (HediffSet.GetFirstHediffOfDef) so duplicate calls are no-ops
        // — important because ApplyLiveConversion runs inside a defensive
        // try-block that can be retried on partial failure.
        private const string AggressionHediffDefName = "Rimconemy_InfectedWildlifeAggression";
        private static void TryApplyInfectionAggressionHediff(Pawn pawn)
        {
            if (pawn == null || pawn.health == null) return;
            var hediffDef = HediffDef.Named(AggressionHediffDefName);
            if (hediffDef == null) return;
            if (pawn.health.hediffSet == null) return;
            // Skip if already present.
            if (pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null) return;
            var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            if (hediff != null)
            {
                pawn.health.AddHediff(hediff);
            }
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
