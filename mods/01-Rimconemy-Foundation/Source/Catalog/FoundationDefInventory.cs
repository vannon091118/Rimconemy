using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Catalog
{
    /// <summary>
    /// Owner: Foundation
    /// Read-only inventory of every Def that is owned by a Rimconemy package.
    /// Populated lazily the first time the user opens the Foundation tab
    /// (after RimWorld has finished parsing Defs for every loaded mod).
    ///
    /// All access goes through the public Verse.* / Assembly-CSharp stable
    /// surface. No reflection, no cross-package compile references.
    ///
    /// Hook reason: DefDatabase enumeration requires every mod's Defs
    /// to be fully parsed. StaticConstructorOnStartup runs too early.
    /// The dashboard's DoWindowContents runs on the game thread after
    /// load and is the safest late-bound trigger without patching.
    ///
    /// SPIKE: API-FOUNDATION-INVENTORY-01 - DefDatabase&lt;T&gt;.AllDefsListForReading,
    /// Def.modContentPack, ModContentPack.PackageId, LoadedModManager.RunningMods.
    /// All four are publicly documented RimWorld 1.5+/1.6+ modding surfaces.
    /// They are NOT in our local Assembly-CSharp.dll `strings`-only extraction
    /// because that extraction misses generic-typed properties and accessors.
    /// They will compile against the RimWorld managed assemblies
    /// or this file will fail to compile and force a documented fallback.
    /// </summary>
    public static class FoundationDefInventory
    {
        private static readonly object _lock = new object();
        private static bool _populated;
        private static readonly Dictionary<string, string> _ownerToTitle
            = new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Dictionary<string, int>> _ownerToDefCounts
            = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        private static int _totalOwners;
        private static int _totalDefs;

        /// <summary>True once the inventory has been captured at least once.</summary>
        public static bool IsPopulated
        {
            get { lock (_lock) return _populated; }
        }

        /// <summary>
        /// Owner package ID -> human-readable title as defined in About.xml.
        /// </summary>
        public static IReadOnlyDictionary<string, string> OwnerTitles
        {
            get { lock (_lock) return new Dictionary<string, string>(_ownerToTitle); }
        }

        /// <summary>
        /// Owner package ID -> (DefType -> count) counts across the public
        /// DefDatabase entries whose modContentPack matches that owner.
        /// </summary>
        public static IReadOnlyDictionary<string, Dictionary<string, int>> OwnerDefCounts
        {
            get { lock (_lock) return new Dictionary<string, Dictionary<string, int>>(_ownerToDefCounts); }
        }

        /// <summary>Number of distinct owners that contributed defs to the inventory.</summary>
        public static int OwnerCount
        {
            get { lock (_lock) return _totalOwners; }
        }

        /// <summary>Total counted defs across every owner (sum of types).</summary>
        public static int TotalDefCount
        {
            get { lock (_lock) return _totalDefs; }
        }

        /// <summary>
        /// Captures the inventory exactly once. Subsequent calls are no-ops.
        /// Returns true if the inventory was captured this call, false if it
        /// was already captured or no Rimconemy owners were resolvable.
        /// </summary>
        public static bool EnsureCaptured()
        {
            lock (_lock)
            {
                if (_populated)
                    return false;

                _populated = true; // capture is one-shot even on failure
            }

            try
            {
                CaptureInternal();
                return _ownerToDefCounts.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.Foundation] DefInventory capture failed: " + ex.Message);
                lock (_lock) _ownerToDefCounts.Clear();
                return false;
            }
        }

        /// <summary>
        /// Forces a re-capture on the next call to EnsureCaptured().
        /// Used by tests and reset paths only.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _populated = false;
                _ownerToDefCounts.Clear();
                _ownerToTitle.Clear();
                _totalOwners = 0;
                _totalDefs = 0;
            }
        }

        private static void CaptureInternal()
        {
            // 1. Inventory every loaded mod via LoadedModManager.RunningMods.
            // SPIKE: LoadedModManager.RunningMods is documented rimworld 1.5+/1.6+ surface.
            var runningMods = LoadedModManager.RunningMods;
            if (runningMods == null) return;

            // 2. Build set of Rimconemy owner ids (Foundation + 4 Feature packages).
            // The set is fixed; any owner not present in the running mod list is just absent.
            var rimconemyOwners = new HashSet<string>(StringComparer.Ordinal)
            {
                "rimconemy.foundation",
                "rimconemy.survivalprogression",
                "rimconemy.scavengerinfrastructure",
                "rimconemy.economyterritory",
                "rimconemy.infectedautomation",
            };

            // 3. For each owner id, allocate a per-type bucket.
            var bucket = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            foreach (var owner in rimconemyOwners)
                bucket[owner] = new Dictionary<string, int>(StringComparer.Ordinal);

            // 4. DefDatabase<T>.AllDefsListForReading is the canonical 1.5+/1.6+ property.
            // The non-generic DefDatabase has no public enumeration, but every concrete
            // DefDatabase<T> instance exposes AllDefsListForReading. Filter by
            // d.modContentPack.PackageId against our owner set.
            AccumulateOwnerCount<ThingDef>(bucket, "ThingDef");
            AccumulateOwnerCount<RecipeDef>(bucket, "RecipeDef");
            AccumulateOwnerCount<ResearchProjectDef>(bucket, "ResearchProjectDef");
            AccumulateOwnerCount<ScenarioDef>(bucket, "ScenarioDef");
            AccumulateOwnerCount<IncidentDef>(bucket, "IncidentDef");
            AccumulateOwnerCount<PawnKindDef>(bucket, "PawnKindDef");
            AccumulateOwnerCount<FactionDef>(bucket, "FactionDef");
            AccumulateOwnerCount<WorldObjectDef>(bucket, "WorldObjectDef");
            AccumulateOwnerCount<HediffDef>(bucket, "HediffDef");
            AccumulateOwnerCount<TraitDef>(bucket, "TraitDef");
            AccumulateOwnerCount<ThoughtDef>(bucket, "ThoughtDef");
            AccumulateOwnerCount<NeedDef>(bucket, "NeedDef");

            // 5. Resolve a stable per-owner title from the running mod's ModContentPack.
            var titles = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var mod in runningMods)
            {
                if (mod == null) continue;
                var pkgId = mod.PackageId;
                if (string.IsNullOrEmpty(pkgId)) continue;
                if (!rimconemyOwners.Contains(pkgId)) continue;
                if (titles.ContainsKey(pkgId)) continue;

                string title = mod.Name;
                if (string.IsNullOrEmpty(title))
                    title = pkgId;
                titles[pkgId] = title;
            }

            // 6. Compute totals and commit.
            int ownerCount = 0;
            int totalDefs = 0;
            var ownersToKeep = new List<string>();
            foreach (var kv in bucket)
            {
                int sum = 0;
                foreach (var c in kv.Value)
                    sum += c.Value;
                if (sum == 0 && !rimconemyOwners.Contains(kv.Key))
                    continue;
                ownerCount++;
                totalDefs += sum;
                ownersToKeep.Add(kv.Key);
            }

            lock (_lock)
            {
                _ownerToDefCounts.Clear();
                _ownerToTitle.Clear();
                foreach (var owner in ownersToKeep)
                {
                    if (bucket.TryGetValue(owner, out var counts))
                        _ownerToDefCounts[owner] = new Dictionary<string, int>(counts, StringComparer.Ordinal);
                    if (titles.TryGetValue(owner, out var t))
                        _ownerToTitle[owner] = t;
                }
                _totalOwners = ownerCount;
                _totalDefs = totalDefs;
            }
        }

        /// <summary>
        /// Helper: enumerate DefDatabase&lt;T&gt;.AllDefsListForReading, filter to
        /// Rimconemy owners, and accumulate per-owner counts into the bucket.
        /// SPIKE: DefDatabase&lt;T&gt;.AllDefsListForReading is the canonical
        /// 1.5+/1.6+ read-only list accessor; documented at rimworldwiki.com/wiki/Modding.
        /// If the property is renamed in 1.6.4566, the build will fail and we
        /// must mark this spike REFUTED and pick a public alternative.
        /// </summary>
        private static void AccumulateOwnerCount<T>(Dictionary<string, Dictionary<string, int>> bucket, string typeLabel)
            where T : Def
        {
            var list = DefDatabase<T>.AllDefsListForReading;
            if (list == null) return;

            foreach (var def in list)
            {
                if (def == null) continue;
                var ownerPack = def.modContentPack;
                if (ownerPack == null) continue;
                var ownerId = ownerPack.PackageId;
                if (string.IsNullOrEmpty(ownerId)) continue;
                if (!bucket.TryGetValue(ownerId, out var perType))
                    continue;
                if (!perType.TryGetValue(typeLabel, out var current))
                    current = 0;
                perType[typeLabel] = current + 1;
            }
        }
    }
}
