using System;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Storage
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03).
    /// Lightweight adapter that physically removes <c>amount</c> units of a
    /// resource (matched by defName) from a Map's spawn pool.
    ///
    /// Phase-B.2 (2026-08-04) — Loop-Closure: this closes the
    /// "Bauschutt-Wand platziert ohne Storage-Abzug"-Gap. Theore- tically
    /// a Blueprint placed via <see cref="BauschuttRemapApply.ApplyRemapCore"/>
    /// left the underlying Bauschutt stack untouched until Phase 6.
    /// From now on, every successful placement call site is expected to
    /// route consumption through this service so the player sees the
    /// physical cost.
    ///
    /// Algorithm:
    ///   1. Iterate <c>map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver)</c>
    ///      filtered by <c>thing.def.defName == resourceDefName</c>.
    ///   2. For each candidate stack:
    ///      - if <c>stackCount &gt;= remaining</c>: SplitOff(remaining).Destroy()
    ///      - else: SplitOff(stackCount).Destroy()     // consume the entire stack
    ///   3. Stop when remaining == 0.
    ///   4. Return total amount removed.
    ///
    /// Defensive Contract:
    ///   - Never throws. Try/catch around the iteration; returns the
    ///     partial count on failure and logs a Warning.
    ///   - Idempotent in the sense: if the requested amount cannot be
    ///     fully consumed, the function still removes what it can —
    ///     callers compare <c>removedCount</c> vs <c>requestedAmount</c>.
    ///   - Owner-Constraint: Mod-03 owns Stack mutation. Mod-04 (Economy)
    ///     must consume through its own <c>PhysicalTransferService</c>
    ///     pipeline; do not call this service from cross-package code.
    ///
    /// Test-Seam (default null = Produktivverhalten):
    ///   <see cref="MutateDownOverride"/> overrides the loop entirely,
    ///   so the regression tests in <c>BauschuttRemapApplyTests</c> can
    ///   verify hook invocation without a real Map.
    /// </summary>
    public static class StorageWriteMutationService
    {
        /// <summary>
        /// Optional Test-Hook: when != null, replaces the production
        /// loop. Tests use a synthetic counter map to assert the
        /// <c>amount</c> argument and the resource-defName match
        /// without standing up a Map.
        /// </summary>
        public static Func<Map, string, int, int> MutateDownOverride = null;

        /// <summary>
        /// Counter incremented for every MutateDown call (production or
        /// override). Tests reset to 0 and read after the operation to
        /// assert call-count.
        /// </summary>
        public static int InvocationCount = 0;

        /// <summary>
        /// Last mutation summary for diagnostics. Tests reset to null.
        /// </summary>
        public static string LastResourceDefName = null;
        public static int LastRequestedAmount = 0;
        public static int LastRemovedAmount = 0;

        /// <summary>
        /// Cleanup method for tests: resets all seams and counters.
        /// </summary>
        public static void ResetTestSeams()
        {
            MutateDownOverride = null;
            InvocationCount = 0;
            LastResourceDefName = null;
            LastRequestedAmount = 0;
            LastRemovedAmount = 0;
        }

        /// <summary>
        /// Remove up to <paramref name="amount"/> units of
        /// <paramref name="resourceDefName"/> from
        /// <paramref name="map"/>. Returns the actual amount removed
        /// (may be &lt; requested if the storage has fewer items).
        /// </summary>
        public static int MutateDown(Map map, string resourceDefName, int amount)
        {
            InvocationCount += 1;
            LastResourceDefName = resourceDefName;
            LastRequestedAmount = amount;

            if (map == null || string.IsNullOrEmpty(resourceDefName) || amount <= 0)
            {
                LastRemovedAmount = 0;
                return 0;
            }

            if (MutateDownOverride != null)
            {
                int removed = MutateDownOverride(map, resourceDefName, amount);
                LastRemovedAmount = removed;
                return removed;
            }

            int consumed = 0;
            int remaining = amount;
            try
            {
                // Walk the haulable items on the map. We intentionally do
                // NOT restrict to player-owned zones: the Bauschutt
                // fragments may sit in a container or on the floor after
                // a scavenge run. We project a fresh, locally-owned
                // ascending-stack list here so we NEVER mutate the shared
                // RimWorld-internal listerThings collection (side-effect
                // on unrelated ticks would be a debugging nightmare).
                var raw = map.listerThings?.ThingsInGroup(ThingRequestGroup.HaulableEver);
                if (raw == null)
                {
                    LastRemovedAmount = 0;
                    return 0;
                }

                // Loop-Closure 2026-08-04: changes prev. in-place sort to
                // a LINQ projection per code-review (avoid mutating
                // listerThings internals). Ascending stackCount means we
                // prefer to drain small scatter-stacks first; the
                // largest intact stack survives the placement. Allocation
                // is a single List<Thing> per call, n is bounded by the
                // scouts' haul budget so &lt;= 100 entries per call.
                System.Collections.Generic.List<Thing> candidates = null;
                foreach (var t in raw)
                {
                    if (t == null || t.Destroyed) continue;
                    if (t.def == null) continue;
                    if (t.def.defName != resourceDefName) continue;
                    if (candidates == null) candidates = new System.Collections.Generic.List<Thing>(8);
                    candidates.Add(t);
                }
                if (candidates == null || candidates.Count == 0)
                {
                    LastRemovedAmount = 0;
                    return 0;
                }

                // Insertion sort by ascending stackCount. List is small.
                for (int i = 1; i < candidates.Count; i++)
                {
                    var key = candidates[i];
                    int j = i - 1;
                    while (j >= 0 && candidates[j].stackCount > key.stackCount)
                    {
                        candidates[j + 1] = candidates[j];
                        j--;
                    }
                    candidates[j + 1] = key;
                }

                foreach (var thing in candidates)
                {
                    if (thing == null || thing.Destroyed) continue;
                    if (thing.def == null) continue;
                    if (thing.def.defName != resourceDefName) continue;

                    int stackCount = thing.stackCount;
                    if (stackCount <= 0) continue;

                    int toRemove = Math.Min(stackCount, remaining);
                    if (toRemove <= 0) break;

                    var split = thing.SplitOff(toRemove);
                    if (split != null)
                    {
                        split.Destroy();
                        consumed += toRemove;
                        remaining -= toRemove;
                    }
                    if (remaining <= 0) break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.StorageWriteMutation] MutateDown exception: " + ex.GetType().Name
                    + " resourceId=" + resourceDefName + " requested=" + amount + " removed=" + consumed);
            }

            LastRemovedAmount = consumed;
            return consumed;
        }
    }
}
