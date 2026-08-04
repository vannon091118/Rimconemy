using System;
using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.Maps;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Colonials
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Phase B / F-V1: single source of truth for "active player colonists".
    ///
    /// Why a centralised reader (during the audit on 2026-08-04):
    /// - Mod 02 enumerated maps in 4 sites (GameOverDetector, ProgressionGC,
    ///   CharacterSetup, ThoughtWorker).
    /// - Mod 03 has PowerChain, Mod 05 has StoryDirector. Each used a
    ///   slightly different filter ('!Dead', '!DestroyedOrNull', 'IsColonist'
    ///   with various qualifiers). DRY-violation: each site risked silent
    ///   drift.
    ///
    /// Now: every site that needs "the active player colonists" goes through
    /// <see cref="GetActiveColonists"/> or <see cref="ActiveColonistCount"/>
    /// or <see cref="AverageHealthPercent"/>. The filter is defined once.
    ///
    /// Filter semantics:
    ///   1. Map != null
    ///   2. mapPawns != null
    ///   3. FreeColonistsSpawned != null
    ///   4. pawn.IsColonist AND !pawn.Dead AND !pawn.DestroyedOrNull
    ///   5. Deduplicated by thingIDNumber
    ///   6. Sorted by thingIDNumber for determinism
    ///   7. Filtered to Humanlike (animals/vehicles are not colonists even
    ///      if vanilla marks them as such)
    ///
    /// Defensive properties:
    /// - Returns an empty list in main menu / no-map-loaded contexts.
    /// - Never throws (try/catch around Find.Maps use).
    /// - AverageHealthPercent guards against zero-count divide.
    ///
    /// Mods that need a different filter (e.g., caravan-inclusive) should
    /// introduce a SIBLING reader in their own package and not mutate this.
    /// </summary>
    public static class ColonialReader
    {
        /// <summary>
        /// Ergonomic filter constants exposed for tests and for downstream
        /// packages that want to compose custom filters while keeping the
        /// 'is it a real player colonist' heuristic shared.
        /// </summary>
        public static bool IsPlayerColonist(Pawn p)
        {
            return p != null
                && p.IsColonist
                && !p.Dead
                && !p.DestroyedOrNull()
                && p.RaceProps != null
                && p.RaceProps.Humanlike;
        }

        /// <summary>
        /// Returns the active player colonists as a deterministic, dedup'd list.
        /// Empty when no map loaded. Never null.
        /// </summary>
        public static List<Pawn> GetActiveColonists()
        {
            List<Pawn> result = new List<Pawn>();
            try
            {
                // Phase-2 / Welle 2 / Item #3 (2026-08-05): use MapRegistry
                // (Foundation-owned, tick-cached IReadOnlyList<Map>) instead
                // of enumerating Find.Maps directly. Eliminates LINQ-clause
                // allocation per call and aligns semantics with sibling
                // consumers in Mod 03 / Mod 05.
                var seen = new HashSet<int>();
                foreach (var map in MapRegistry.GetPlayerHomeMaps())
                {
                    if (map?.mapPawns?.FreeColonistsSpawned == null) continue;
                    foreach (var p in map.mapPawns.FreeColonistsSpawned)
                    {
                        if (!IsPlayerColonist(p)) continue;
                        if (!seen.Add(p.thingIDNumber)) continue;
                        result.Add(p);
                    }
                }

                // Stable sort to keep callers' iteration order reproducible
                // (this matters when Mod 02/05 use the list to derive seeds
                // or roster fingerprints).
                result.Sort((a, b) => a.thingIDNumber.CompareTo(b.thingIDNumber));
                return result;
            }
            catch (Exception ex)
            {
                // Defensive — never let a RimWorld quirk crash a reader path.
                Log.Warning("[Rimconemy.Foundation.Colonials] GetActiveColonists swallowed exception: " + ex.GetType().Name + ": " + ex.Message);
                return result;
            }
        }

        /// <summary>Count of active player colonists. 0 in main menu / no-map.</summary>
        public static int ActiveColonistCount
        {
            get { return GetActiveColonists().Count; }
        }

        /// <summary>
        /// Average health percentage across active colonists. Returns 0 when no
        /// colonists (caller decides what that means — 0 for "fresh colony"
        /// treatments, 0.5 for a "neutral default" treatment).
        /// </summary>
        public static float AverageHealthPercent
        {
            get
            {
                var colonists = GetActiveColonists();
                if (colonists.Count == 0) return 0f;
                float sum = 0f;
                foreach (var p in colonists)
                    sum += p.health?.summaryHealth?.SummaryHealthPercent ?? 0.5f;
                return sum / colonists.Count;
            }
        }

        /// <summary>
        /// True if there are zero active player colonists. Idempotent and
        /// cheap. Used by StoryDirector's MaybeSignalGameOverForWipe.
        /// </summary>
        public static bool NoColonists
        {
            get { return ActiveColonistCount == 0; }
        }
    }
}
