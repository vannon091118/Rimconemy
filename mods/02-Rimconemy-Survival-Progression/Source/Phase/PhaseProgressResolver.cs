using System;
using System.Collections.Generic;
using Rimconemy.SurvivalProgression.Scenarios;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Phase
{
    /// <summary>
    /// SSOT phase identifiers, mirroring PHASE_PROGRESSION_CONTRACT.md §2 / §10.
    /// Enum order is the progression order.
    /// </summary>
    public enum PhaseId
    {
        None = 0,
        EarlySurvival = 1,
        Production = 2,
        Automation = 3,
        Expansion = 4,
        Trade = 5,
        Empire = 6,
    }

    /// <summary>
    /// One milestone in a phase. <see cref="IsMet"/> is a pure, never-throwing
    /// predicate that tolerates a null Map (returns false). <see cref="Met"/> is
    /// filled by <see cref="PhaseProgressResolver.Resolve"/> during snapshot construction.
    /// </summary>
    public sealed class PhaseMilestone
    {
        public PhaseId Phase;
        public string Key;          // stable across saves; localisation = "Rimconemy.PhaseProgress.Milestone." + Key
        public Func<Map, bool> IsMet;
        public bool Met;
    }

    /// <summary>
    /// Read-only snapshot constructed by <see cref="PhaseProgressResolver.Resolve"/>.
    /// Never mutated by the consumer (the window).
    /// </summary>
    public sealed class PhaseProgressSnapshot
    {
        public string CurrentPhaseLabelKey;
        public string NextMilestoneLabelKey;   // null when the current phase is complete
        public int CurrentPhaseTotal;
        public int CurrentPhaseCompletions;
        public int TotalMilestonesMet;
        public int TotalMilestonesAcrossPhases;
        public int Percent;                     // current-phase completion 0..100
        public int OverallPercent;              // completion across all phases 0..100
        public string EmptyReason;              // non-null when not yet computable
    }

    /// <summary>
    /// Pure, static phase-domain logic (PHASE_PROGRESSION_CONTRACT.md §10).
    /// The milestone table below IS the contract surface: adding a milestone
    /// requires a §10 row + Def SSOT probe + language key update.
    /// </summary>
    public static class PhaseProgressResolver
    {
        private static readonly List<PhaseMilestone> Milestones = DefineMilestones();

        private static ThingDef DefThing(string defName) =>
            DefDatabase<ThingDef>.GetNamedSilentFail(defName);

        private static ResearchProjectDef DefResearch(string defName) =>
            DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName);

        private static bool MapHasBuilding(Map map, string defName)
        {
            if (map == null) return false;
            var def = DefThing(defName);
            // 1.6 API: colonist-built structures only (allBuildings is not present in 1.6.4566).
            var buildings = map.listerBuildings?.allBuildingsColonist;
            if (def == null || buildings == null) return false;
            for (int i = 0; i < buildings.Count; i++)
                if (buildings[i]?.def == def) return true;
            return false;
        }

        private static bool MapHasAtLeastBuildings(Map map, int minCount)
        {
            if (map == null) return false;
            var buildings = map.listerBuildings?.allBuildingsColonist;
            return buildings != null && buildings.Count >= minCount;
        }

        private static bool MapResourceAtLeast(Map map, string defName, int count)
        {
            if (map == null) return false;
            var def = DefThing(defName);
            return def != null && map.resourceCounter != null
                && map.resourceCounter.GetCount(def) >= count;
        }

        private static bool ResearchFinished(string defName)
        {
            // def.IsFinished is the canonical 1.6 "research completed" probe.
            // CostAmount is not on ResearchProjectDef in 1.6 — use the bool property.
            if (Find.ResearchManager == null) return false;
            var def = DefResearch(defName);
            return def != null && def.IsFinished;
        }

        private static bool StartStateMarked(Map map, string key)
        {
            if (map == null) return false;
            var state = RimconemyStartState.Resolve();
            return state != null && state.IsCompletedFor(map, key);
        }

        private static PhaseMilestone Milestone(PhaseId phase, string key, Func<Map, bool> isMet) =>
            new PhaseMilestone { Phase = phase, Key = key, IsMet = isMet };

        private static List<PhaseMilestone> DefineMilestones() => new List<PhaseMilestone>
        {
            // Phase 1 — EarlySurvival
            Milestone(PhaseId.EarlySurvival, "single-survivor-start",
                map => StartStateMarked(map, "single-survivor")),
            Milestone(PhaseId.EarlySurvival, "first-cooked-meal",
                // Truthful conjunction: meal in storage AND a colonist-built cook station exists.
                // Counter-only would falsely fire on trader-deposited Meals. Building-only
                // would fire since the player queued a stovetop before cooking once.
                // Campfire MUST be counted as an early-game cook station — otherwise
                // the milestone is unreachable before stovetop is built and Phase 1
                // deadlocks on a wall-condition (Code-Review 2026-08-05 R1).
                map => MapResourceAtLeast(map, "MealSimple", 1)
                    && (MapHasBuilding(map, "Rimconemy_Campfire")
                        || MapHasBuilding(map, "FueledStove")
                        || MapHasBuilding(map, "ElectricStove"))),

            Milestone(PhaseId.EarlySurvival, "campfire-built",
                map => MapHasBuilding(map, "Rimconemy_Campfire")),
            Milestone(PhaseId.EarlySurvival, "three-buildings-built",
                map => MapHasAtLeastBuildings(map, 3)),

            // Phase 2 — Production
            Milestone(PhaseId.Production, "first-coal-produced",
                map => MapResourceAtLeast(map, "Rimconemy_Coal", 1)),
            Milestone(PhaseId.Production, "smelting-research-finished",
                map => ResearchFinished("Rimconemy_SmeltingCoal")),
            Milestone(PhaseId.Production, "first-steel-smelted",
                // Truthful conjunction: Smithing research unlocked AND Steel in counter
                // AND a colonist-built Smithy exists. This triple is the tightest possible
                // for "we genuinely smelted something" — research without a smithy means
                // the Steel came from somewhere else; a smithy without research means
                // smelt fires are still gated (Code-Review 2026-08-05 R2).
                map => ResearchFinished("Smithing")
                    && MapHasBuilding(map, "FueledSmithy")
                    && MapResourceAtLeast(map, "Steel", 1)),

            Milestone(PhaseId.Production, "smithy-built",
                map => MapHasBuilding(map, "FueledSmithy")),

            // Phase 3 — Automation
            Milestone(PhaseId.Automation, "machine-parts-built",
                // Truthful conjunction: ≥5 components AND a Smithy OR TableMachining built.
                // Counter-only falsely fires on a van-trader side-dump of components.
                map => MapResourceAtLeast(map, "ComponentIndustrial", 5)
                    && (MapHasBuilding(map, "FueledSmithy") || MapHasBuilding(map, "TableMachining"))),

            Milestone(PhaseId.Automation, "stainless-smelted",
                map => MapResourceAtLeast(map, "Rimconemy_StainlessSteel", 1)),
            Milestone(PhaseId.Automation, "stainless-tower-built",
                map => MapHasBuilding(map, "Rimconemy_StainlessSteelTower")),
            Milestone(PhaseId.Automation, "power-grid-online",
                map => MapHasBuilding(map, "Rimconemy_WoodCoalGenerator")),

            // Phase 4 — Expansion (single anchor; expansion lives in Mod 03/04)
            Milestone(PhaseId.Expansion, "outpost-constructed",
                map => StartStateMarked(map, "outpost-constructed")),

            // Phase 5 — Trade
            Milestone(PhaseId.Trade, "credits-wallet-initialised",
                map => StartStateMarked(map, "credits-wallet-initialised")),

            // Phase 6 — Empire
            Milestone(PhaseId.Empire, "empire-tribute-paid",
                map => StartStateMarked(map, "empire-tribute-paid")),
        };

        /// <summary>
        /// Snapshot for one map. Pure: no side-effects. Tolerates a null Map
        /// (returns an empty snapshot with <see cref="PhaseProgressSnapshot.EmptyReason"/>).
        /// </summary>
        public static PhaseProgressSnapshot Resolve(Map map)
        {
            var snap = new PhaseProgressSnapshot
            {
                TotalMilestonesAcrossPhases = Milestones.Count,
            };
            if (map == null || Current.Game == null)
            {
                snap.EmptyReason = "no-map";
                return snap;
            }

            int totalMet = 0;
            for (int i = 0; i < Milestones.Count; i++)
            {
                var m = Milestones[i];
                m.Met = m.IsMet(map);
                if (m.Met) totalMet++;
            }

            // Current phase = earliest phase with an unmet milestone (Empire when all complete).
            PhaseId current = PhaseId.Empire;
            for (int i = 0; i < Milestones.Count; i++)
            {
                var m = Milestones[i];
                if (!m.Met && m.Phase < current) current = m.Phase;
            }

            int phaseTotal = 0;
            int phaseMet = 0;
            string nextMilestone = null;
            for (int i = 0; i < Milestones.Count; i++)
            {
                var m = Milestones[i];
                if (m.Phase != current) continue;
                phaseTotal++;
                if (m.Met) phaseMet++;
                else if (nextMilestone == null) nextMilestone = "Rimconemy.PhaseProgress.Milestone." + m.Key;
            }

            snap.TotalMilestonesMet = totalMet;
            snap.CurrentPhaseLabelKey = "Rimconemy.PhaseProgress.Phase." + current;
            snap.NextMilestoneLabelKey = nextMilestone;
            snap.CurrentPhaseTotal = phaseTotal;
            snap.CurrentPhaseCompletions = phaseMet;
            snap.Percent = PercentOf(phaseMet, phaseTotal);
            snap.OverallPercent = PercentOf(totalMet, Milestones.Count);
            return snap;
        }

        private static int PercentOf(int met, int total)
        {
            if (total <= 0) return 0;
            int pct = (int)(100f * met / total);
            return pct < 0 ? 0 : pct > 100 ? 100 : pct;
        }
    }
}
