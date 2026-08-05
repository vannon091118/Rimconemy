using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.EconomyTerritory.Building
{
    /// <summary>
    /// Package-04 read-only boundary for physical Building inputs.
    /// Refactor Phase-First (Task 15): reads physical-input facts from the
    /// resolved Building Def (its <c>costList</c> and <c>stuffCategories</c>) rather
    /// than from a hard-coded table kept inside Mod 04. The hard-coded table was
    /// the single biggest cross-package drift surface after the Mod-03 SSOT
    /// (only one Def owner per resource). Now Mod 04 is a *consumer* of Vanilla
    /// + Mod-03 def data, not a parallel truth source.
    ///
    /// This adapter never moves Things and never books wallet credits in Milestone A.
    /// Wallet credits (non-physical) are kept in a stable string table because
    /// RimWorld's wallets carry their own Def signature ("Silver" / "Credits").
    /// </summary>
    public static class BuildingInputAdapter
    {
        public const string ConstructionDebrisDefName = "Rimconemy_ConstructionDebris";
        public const string DistilledWaterDefName = "Rimconemy_DistilledWater";

        private static readonly HashSet<string> PhysicalInputs
            = new HashSet<string>(StringComparer.Ordinal)
            {
                ConstructionDebrisDefName,
                DistilledWaterDefName,
                "WoodLog",
                "Chemfuel",
                "Steel",
                "Rimconemy_SteelScraps",
                "Rimconemy_Coal",
                "Rimconemy_MachineParts",
                "Rimconemy_StainlessSteel",
                "Rimconemy_WeaponComponent",
            };

        private static readonly HashSet<string> CreditInputs
            = new HashSet<string>(StringComparer.Ordinal)
            {
                "Credits",
            };

        public static bool IsPhysicalInput(string defName)
        {
            return !string.IsNullOrEmpty(defName) && PhysicalInputs.Contains(defName);
        }

        public static bool IsCreditInput(string defName)
        {
            return !string.IsNullOrEmpty(defName) && CreditInputs.Contains(defName);
        }

        /// <summary>
        /// Units of <paramref name="defName"/> required to construct
        /// <paramref name="buildingDefName"/>. Reads both <c>costList</c>
        /// (direct ingredient) and <c>costStuffCount</c> + <c>stuffCategories</c>
        /// (Stuff-substitution path). Two read-paths are summed so a Wall which
        /// has <c>costStuffCount=15</c> and a Stuff satisfying
        /// <see cref="ConstructionDebrisDefName"/> returns 15 for that
        /// construction input — honest, deterministic and side-effect-free.
        /// Returns 0 if the building def is unknown, or if the requested
        /// material does not appear in either path.
        /// </summary>
        public static int RequiredUnits(string defName, string buildingDefName)
        {
            if (string.IsNullOrEmpty(buildingDefName)) return 0;
            if (string.IsNullOrEmpty(defName)) return 0;

            var def = DefDatabase<ThingDef>.GetNamedSilentFail(buildingDefName);
            if (def == null) return 0;

            int totalCost = 0;

            // Path A: direct ingredient in costList (Steel/MachineParts etc.).
            if (def.costList != null)
            {
                foreach (var c in def.costList)
                {
                    if (c?.thingDef == null) continue;
                    if (c.thingDef.defName.Equals(defName, StringComparison.Ordinal))
                        totalCost += c.count;
                }
            }

            // Path B: Stuff-substitution. The building consumes costStuffCount
            // units of some Stuff picked from stuffCategories. If the requested
            // defName is a Stuff whose stuffProps.categories intersect the
            // building's stuffCategories, the whole costStuffCount is owed.
            if (def.costStuffCount > 0 && def.stuffCategories != null)
            {
                var stuffDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (stuffDef?.stuffProps?.categories != null)
                {
                    foreach (var cat in def.stuffCategories)
                    {
                        if (cat != null && stuffDef.stuffProps.categories.Contains(cat))
                        {
                            totalCost += def.costStuffCount;
                            break;
                        }
                    }
                }
            }

            return totalCost;
        }
    }
}
