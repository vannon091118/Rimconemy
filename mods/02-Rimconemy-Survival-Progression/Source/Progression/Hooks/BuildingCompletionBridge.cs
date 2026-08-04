using System;
using Rimconemy.SurvivalProgression.Progression;
using Rimconemy.SurvivalProgression.Progression.Unlocks;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Progression.Hooks
{
    /// <summary>
    /// Phase 8.3 — Building-completion bridge. Stand-alone static dispatch
    /// for an externally-discovered completion event, with idempotent
    /// recording into <see cref="DomainXpState"/> plus a legacy write
    /// into <see cref="BuildingProgressionAdapter"/> so the existing
    /// BuildingXP regression suite stays green.
    ///
    /// Vertical-Slice-Plan §Phase 8.3 (Spike-Pflicht geschlossen 2026-08-04):
    ///   The Vanilla 1.6.4566 hook is <c>RimWorld.Frame.CompleteConstruction(Pawn worker)</c>.
    ///   The Harmony Postfix lives in <see cref="FrameCompletionPatch"/>.
    ///   This bridge is the API surface those callers (and tests) reach into.
    ///
    /// The bridge never polls ticks and never rebuilds a snapshot; it is
    /// fed an explicit event, classifies the resulting def into a
    /// <see cref="ProgressionDomain"/>, and writes the record exactly once.
    /// </summary>
    public static class BuildingCompletionBridge
    {
        /// <summary>
        /// Canonical idempotency key shape. Stable across save-load because
        /// map.uniqueID, def.defName and frame.thingIDNumber come from the
        /// live objects and only change when the underlying construction
        /// identity changes.
        ///
        /// The domain segment of the key reflects the <em>classified</em>
        /// domain (see <see cref="ClassifyBuilding"/>), not a hard "Building"
        /// prefix. This way a manually-inspected save diff makes it clear
        /// that a Campfire construction lives in the Firecraft slice, even
        /// though the construction itself came in through the Building hook.
        /// </summary>
        public static string BuildIdempotencyKey(ThingDef def, Map map, Frame frame)
        {
            int mapId = map != null ? map.uniqueID : -1;
            string defName = def != null ? def.defName : "unknown";
            int frameId = frame != null ? frame.thingIDNumber : -1;
            int tick = Find.TickManager?.TicksGame ?? 0;
            ProgressionDomain domain = ClassifyBuilding(def);

            return "domain:" + ProgressionDomainUtility.Key(domain)
                + ":completed:map=" + mapId
                + ":def=" + defName
                + ":frame=" + frameId
                + ":tick=" + tick;
        }

        /// <summary>
        /// Heuristic classification: which <see cref="ProgressionDomain"/>
        /// does the completed building belong to? The mapping follows
        /// the Vertical-Slice-Plan §Phase 9.4 first learning path:
        ///   Defense:     Defensive/Turret/Wall/Barricade
        ///   Machinery:   Generator/Battery/Machine/Electric
        ///   Firecraft:   Campfire/Furnace/Forge/Kiln/Oven
        ///   Processing:  WorkTable/Bench/Smelter/Stirling
        ///   Building:    fallback
        /// </summary>
        public static ProgressionDomain ClassifyBuilding(ThingDef def)
        {
            if (def == null) return ProgressionDomain.Building;
            string n = def.defName ?? "";
            if (ContainsToken(n, "Defensive", "Turret", "Wall", "Barricade", "Bunker"))
                return ProgressionDomain.Defense;
            if (ContainsToken(n, "Generator", "Battery", "Machine", "Electric", "Reactor"))
                return ProgressionDomain.Machinery;
            if (ContainsToken(n, "Campfire", "Furnace", "Forge", "Kiln", "Oven", "Stove"))
                return ProgressionDomain.Firecraft;
            if (ContainsToken(n, "WorkTable", "Bench", "Smelter", "Smithy", "Stirling", "Refinery"))
                return ProgressionDomain.Processing;
            if (ContainsToken(n, "Salvage", "Scrap", "Recycler", "Recover"))
                return ProgressionDomain.Salvage;
            return ProgressionDomain.Building;
        }

        private static bool ContainsToken(string name, params string[] tokens)
        {
            if (string.IsNullOrEmpty(name)) return false;
            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                if (string.IsNullOrEmpty(t)) continue;
                if (name.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        /// <summary>
        /// Submits a validated completion event into the
        /// <see cref="DomainXpState"/> and the legacy
        /// <see cref="BuildingProgressionAdapter"/> award ledger with
        /// exactly the same idempotency key.
        ///
        /// Returns the canonical <see cref="ProgressionActionResult"/>;
        /// <see cref="ProgressionActionResult.WasAccepted"/> is false on
        /// replay (duplicate key), on invalid inputs, or when the
        /// <see cref="DomainXpState"/> is null.
        ///
        /// Acceptance ordering is symmetric: legacy <c>BuildingProgressionAdapter</c>
        /// only writes if DomainXpState.TryAward accepted the event AND the
        /// adapter-given amount stays &gt;= 1 (otherwise we drop the legacy
        /// log line with a single warning rather than diverge silently).
        /// </summary>
        public static ProgressionActionResult Submit(
            DomainXpState state,
            ThingDef def,
            Map map,
            Frame frame,
            Pawn worker,
            long tick,
            float baseXp = 1f)
        {
            if (def == null || map == null || state == null)
            {
                return ProgressionActionResult.Rejected("args-null");
            }

            string key = BuildIdempotencyKey(def, map, frame);
            ProgressionDomain domain = ClassifyBuilding(def);

            bool accepted = state.TryAward(
                domain,
                baseXp,
                key,
                def.defName ?? "",
                1,
                tick,
                out ProgressionActionResult result);

            if (accepted)
            {
                string pawnId = worker != null ? worker.thingIDNumber.ToString() : "n/a";
                int awardAmount = Mathf.Max(1, Mathf.RoundToInt(baseXp));
                bool legacyAccepted = BuildingProgressionAdapter.TryCreateAward(
                    key,
                    pawnId,
                    awardAmount,
                    out BuildingXpAward _);
                if (!legacyAccepted)
                {
                    Log.Warning(
                        "[Rimconemy.SurvivalProgression] BuildingCompletionBridge.Submit: "
                        + "DomainXpState accepted but legacy BuildingProgressionLedger rejected. "
                        + "ActionKey=" + key
                        + ", AwardAmount=" + awardAmount
                        + ". This indicates a save-state asymmetry between the hubs; "
                        + "the DomainXpState value is the source of truth.");
                }

                UnlockService.NotifyActionCompleted(key, domain);
            }

            return result;
        }
    }
}
