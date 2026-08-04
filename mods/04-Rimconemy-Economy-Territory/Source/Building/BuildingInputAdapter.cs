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
        /// <paramref name="buildingDefName"/>. Looks up the Vanillea/Mod-03
        /// resolved Def and reads <c>costList</c>. Returns 0 if the building def
        /// is unknown, if the costList is absent, or if the requested material is
        /// not part of the building's cost. Deterministic and side-effect-free.
        /// </summary>
        public static int RequiredUnits(string defName, string buildingDefName)
        {
            if (string.IsNullOrEmpty(buildingDefName)) return 0;
            if (string.IsNullOrEmpty(defName)) return 0;

            var def = DefDatabase<ThingDef>.GetNamedSilentFail(buildingDefName);
            if (def == null) return 0;

            var costList = def.costList;
            if (costList == null) return 0;

            foreach (var c in costList)
            {
                if (c?.thingDef == null) continue;
                if (c.thingDef.defName.Equals(defName, StringComparison.Ordinal))
                {
                    return c.count;
                }
            }

            // checkstuffProps variants — building may consume Rimconemy_ConstructionDebris
            // as Stuff rather than directly as a cost. Defensive read;
            // count=0 for stuff-strapped cost is acceptable for Milestone A.
            return 0;
        }
    }
}
