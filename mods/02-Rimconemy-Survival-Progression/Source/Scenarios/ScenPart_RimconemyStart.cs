using System;
using System.Collections.Generic;
using Rimconemy.SurvivalProgression.Progression;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Scenarios
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02).
    /// Phase 1.1 — Single-Survivor-Setup. Hook: <see cref="ScenPart.PostMapGenerate(Map)"/>
    /// (vanilla-api-matrix §3.1 / §4 bestätigt).
    ///
    /// Responsibilities:
    ///   - Idempotent survivor-count enforcement (exactly 1 colonist) — the FIRST map
    ///     pass authorities; replay/MapRemoved re-runs are caught by <see cref="RimconemyStartState"/>.
    ///   - Drops the <see cref="Rimconemy_ScrapRifle"/> from Phase 1.2 onto the survivor.
    ///   - Scatters <see cref="Rimconemy_SteelScraps"/> (Phase 2.1) in a small radius
    ///     around the centre of the player home map. Drops are *not guaranteed* —
    ///     they only honour the Phase-1.4 Anti-Softlock rule if the survivor survives
    ///     without ever finding one.
    ///
    /// Cross-package boundary: this ScenPart does NOT spawn the starting enemy. That
    /// is owned by <see cref="Rimconemy.InfectedAutomation.Scenarios.ScenPart_RimconemyStartEnemies"/>
    /// in Package 05 — it queries <see cref="RimconemyStartState"/> via the Foundation
    /// snapshot ref bus (capability-gated; see INTERFACE_CONTRACT §9).
    /// </summary>
    public class ScenPart_RimconemyStart : ScenPart
    {
        // Stable event-keys are exposed so Package 05 reads the canonical names
        // when checking the dedup set (no string drift between packages).
        public const string EventKey_SingleSurvivor = "single-survivor";
        public const string EventKey_ScrapRifleGiven = "scrap-rifle-given";
        public const string EventKey_SteelScrapsScattered = "steel-scraps-scattered";

        // 1.6-validated DefNames (do not rename — they cross DLL boundaries).
        public const string DefName_ScrapRifle = "Rimconemy_ScrapRifle";
        public const string DefName_SteelScraps = "Rimconemy_SteelScraps";

        // Spawn radius for the steel-scraps scatter (cells, squared).
        public const int SteelScrapsScatterRadius = 8;
        public const int SteelScrapsScatterCount = 3;

        public string EventKeySingleSurvivor   => EventKey_SingleSurvivor;
        public string EventKeyScrapRifleGiven  => EventKey_ScrapRifleGiven;
        public string EventKeySteelScrapsSet   => EventKey_SteelScrapsScattered;

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            try
            {
                if (map == null) return;
                var state = RimconemyStartState.Resolve();
                if (state == null)
                {
                    Log.Warning("[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart.PostMapGenerate: RimconemyStartState absent; skipping.");
                    return;
                }

                if (!state.IsCompletedFor(map, EventKey_SingleSurvivor))
                {
                    EnforceSingleSurvivor(map);
                    state.MarkCompleted(map, EventKey_SingleSurvivor);
                }

                if (!state.IsCompletedFor(map, EventKey_ScrapRifleGiven))
                {
                    GiveScrapRifleToColonist(map);
                    state.MarkCompleted(map, EventKey_ScrapRifleGiven);
                }

                if (!state.IsCompletedFor(map, EventKey_SteelScrapsScattered))
                {
                    ScatterSteelScraps(map);
                    state.MarkCompleted(map, EventKey_SteelScrapsScattered);
                }
            }
            catch (Exception ex)
            {
                // Phase 1.1 is non-fatal: a ScenPart crash must not crash Scribe.
                Log.Warning($"[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart.PostMapGenerate caught: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void EnforceSingleSurvivor(Map map)
        {
            // We don't delete 2nd/3rd pawns arbitrarily — that would break pawn
            // generation, story triggers and factional standing. The active Survivor-
            // count is instead enforced via the cost-aware budget path in Phase-5
            // Character Setup. Here we only LOG the observation so an operator can
            // confirm the contract is held without looking into Mod 02 internals.
            var colonists = new List<Pawn>();
            // 1.6 canonical: map.mapPawns.FreeColonistsSpawned (siehe vanilla-api-matrix
            // §3 sowie existierende Mods im Repo: ColonialReader.cs nutzt denselben Pfad).
            foreach (var p in map.mapPawns?.FreeColonistsSpawned ?? new List<Pawn>())
            {
                if (p == null || p.Dead || p.DestroyedOrNull()) continue;
                colonists.Add(p);
            }
            int count = colonists.Count;
            if (count == 1)
            {
                Log.Message(
                    $"[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: single-survivor contract holds (map={map.uniqueID}, pawn={colonists[0].LabelShortCap}).");
            }
            else
            {
                Log.Warning(
                    $"[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: expected 1 survivor, got {count} on map={map.uniqueID}. " +
                    "Force-prune is intentionally NOT applied here — Phase 5 Character-Setup budget is the enforcement layer.");
            }
        }

        private static void GiveScrapRifleToColonist(Map map)
        {
            var rifleDef = DefDatabase<ThingDef>.GetNamedSilentFail(DefName_ScrapRifle);
            if (rifleDef == null)
            {
                Log.Warning(
                    $"[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: ThingDef '{DefName_ScrapRifle}' not loaded. " +
                    "Make sure Defs/ThingDefs/Weapons/Rimconemy_ScrapRifle.xml is in this package.");
                return;
            }

            // Pick the colonist pawn — typically the only one. If multiple colonists
            // exist (custom scenario config), pick the first by ID for determinism.
            Pawn recipient = null;
            int bestId = int.MaxValue;
            foreach (var p in map.mapPawns?.FreeColonistsSpawned ?? new List<Pawn>())
            {
                if (p == null || p.Dead || p.DestroyedOrNull()) continue;
                if (p.thingIDNumber < bestId)
                {
                    bestId = p.thingIDNumber;
                    recipient = p;
                }
            }
            if (recipient == null) return;

            var rifle = ThingMaker.MakeThing(rifleDef);
            // Phase 1.3 / 1.5 wiring would attach an ammo TComp here. Out-of-scope
            // for Phase 1.1; left as a marker for the live-build refactor.
            recipient.inventory?.TryAddAndUnforbid(rifle);
        }

        private static void ScatterSteelScraps(Map map)
        {
            var scrapsDef = DefDatabase<ThingDef>.GetNamedSilentFail(DefName_SteelScraps);
            if (scrapsDef == null)
            {
                Log.Warning(
                    $"[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: ThingDef '{DefName_SteelScraps}' not loaded. " +
                    "Make sure Defs/ThingDefs/Resources/Rimconemy_SteelScraps.xml is in this package.");
                return;
            }

            // Scatter N piles around the player's home centre. Anti-Softlock compliance:
            // the count is small enough that the survivor can play through Phase 2
            // even if no scraps are ever discovered (early-game building still works
            // without Bauschutt).
            IntVec3 centre = map.Center;
            int placed = 0;
            for (int attempt = 0; attempt < SteelScrapsScatterCount * 4 && placed < SteelScrapsScatterCount; attempt++)
            {
                IntVec3 cell = centre + Rand.Range(-SteelScrapsScatterRadius, SteelScrapsScatterRadius + 1)
                    * new IntVec3(1, 0, 0)
                    + Rand.Range(-SteelScrapsScatterRadius, SteelScrapsScatterRadius + 1)
                    * new IntVec3(0, 0, 1);

                if (!cell.InBounds(map)) continue;
                if (!cell.Standable(map)) continue;
                if (cell.Fogged(map)) continue;

                var scraps = ThingMaker.MakeThing(scrapsDef);
                scraps.stackCount = 1;
                GenSpawn.Spawn(scraps, cell, map);
                placed++;
            }

            if (placed < SteelScrapsScatterCount)
            {
                Log.Warning(
                    $"[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: only {placed}/{SteelScrapsScatterCount} steel scraps placed on map={map.uniqueID}.");
            }
            else
            {
                Log.Message(
                    $"[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: scattered {placed} steel scraps around centre of map={map.uniqueID}.");
            }
        }
    }
}
