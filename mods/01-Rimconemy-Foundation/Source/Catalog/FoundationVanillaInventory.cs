using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Catalog
{
    /// <summary>
    /// Owner: Foundation
    /// Read-only chat of the canonical Vanilla + DLC ThingDef surface,
    /// populated lazily the first time the user opens the Foundation tab.
    ///
    /// P30 / partial closure of P25 (API-RESOURCE-01):
    /// Initial iteration tried to bucket Vanilla ThingDefs into per-category
    /// buckets via Def.category / thingCategories. The local
    /// Assembly-CSharp.dll surface did not confirm a stable bucketing
    /// identifier — neither `label` nor `defName` was exposed by the
    /// 1.6 `ThingCategory` value-type, and any synthesis of a stable
    /// identifier would have been guesswork. The current implementation
    /// therefore stops at total counts, which are stable and verifiable
    /// through public Verse.\*/Assembly-CSharp surface.
    ///
    /// All access goes through Verse.\*/Assembly-CSharp public surface.
    /// No reflection, no cross-package compile reference.
    ///
    /// SPIKE: API-FOUNDATION-VANILLA-01 - DefDatabase&lt;T&gt;.AllDefsListForReading,
    /// Def.modContentPack, Def.IsStuff (canonical 1.5+/1.6+), and a
    /// documented gap on stable 1.6 category-label access (deferred to
    /// a separate spike; current snapshot is category-less by design).
    /// </summary>
    public static class FoundationVanillaInventory
    {
        private static readonly object _lock = new object();
        private static bool _populated;
        private static int _totalVanillaThingDefs;
        private static int _totalStuffDefs;
        private static int _totalTrackedDlcIds;

        public static bool IsPopulated
        {
            get { lock (_lock) return _populated; }
        }

        public static int TotalVanillaThingDefs
        {
            get { lock (_lock) return _totalVanillaThingDefs; }
        }

        public static int TotalStuffDefs
        {
            get { lock (_lock) return _totalStuffDefs; }
        }

        public static int TotalTrackedDlcIds
        {
            get { lock (_lock) return _totalTrackedDlcIds; }
        }

        /// <summary>One-shot lazy capture. Returns true if at least one Vanilla ThingDef was observed.</summary>
        public static bool EnsureCaptured()
        {
            lock (_lock)
            {
                if (_populated)
                    return false;
                _populated = true;
            }

            try
            {
                CaptureInternal();
                return _totalVanillaThingDefs > 0;
            }
            catch (Exception ex)
            {
                Log.Warning("[Rimconemy.Foundation] VanillaInventory capture failed: " + ex.Message);
                lock (_lock) _totalVanillaThingDefs = 0;
                return false;
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _populated = false;
                _totalVanillaThingDefs = 0;
                _totalStuffDefs = 0;
                _totalTrackedDlcIds = 0;
            }
        }

        private static void CaptureInternal()
        {
            // 1. Identify non-Rimconemy mod owners to get their count.
            var runningMods = LoadedModManager.RunningMods;
            var rimconemyOwners = new HashSet<string>(StringComparer.Ordinal)
            {
                "rimconemy.foundation",
                "rimconemy.survivalprogression",
                "rimconemy.scavengerinfrastructure",
                "rimconemy.economyterritory",
                "rimconemy.infectedautomation",
            };

            int nonRimconemyCount = 0;
            if (runningMods != null)
            {
                foreach (var mod in runningMods)
                {
                    if (mod == null) continue;
                    string id = mod.PackageId;
                    if (string.IsNullOrEmpty(id)) continue;
                    if (rimconemyOwners.Contains(id)) continue;
                    nonRimconemyCount++;
                }
            }

            // 2. Enumerate DefDatabase<ThingDef>.AllDefsListForReading and
            //    tally vanilla (non-Rimconemy-owner) ThingDefs and Stuff.
            //    SPIKE: Def.IsStuff is the canonical Stuff marker; the
            //    property is documented stable across 1.5+/1.6+ and
            //    compiles against the local Assembly-CSharp.dll.
            int vanillaThingTotal = 0;
            int stuffTotal = 0;

            var list = DefDatabase<ThingDef>.AllDefsListForReading;
            if (list != null)
            {
                foreach (var def in list)
                {
                    if (def == null) continue;
                    var ownerPack = def.modContentPack;
                    string ownerId = ownerPack != null ? ownerPack.PackageId : null;
                    if (rimconemyOwners.Contains(ownerId ?? string.Empty)) continue;
                    vanillaThingTotal++;
                    if (def.IsStuff) stuffTotal++;
                }
            }

            lock (_lock)
            {
                _totalVanillaThingDefs = vanillaThingTotal;
                _totalStuffDefs = stuffTotal;
                _totalTrackedDlcIds = nonRimconemyCount;
            }
        }
    }
}
