using System;
using System.Collections.Generic;

namespace Rimconemy.EconomyTerritory.Building
{
    /// <summary>
    /// Package-04 read-only boundary for physical Building inputs.
    /// It never moves Things and never books wallet credits in Milestone A.
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
            };

        private static readonly HashSet<string> CreditInputs
            = new HashSet<string>(StringComparer.Ordinal)
            {
                "Credits",
            };

        private static readonly Dictionary<string, int> DebrisRequirements
            = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "Wall", 5 },
                { "Door", 8 },
                { "Rimconemy_WoodCoalGenerator", 30 },
                { "Rimconemy_WaterTurbineGenerator", 40 },
                { "Rimconemy_TurbineWaterPump", 20 },
                { "Rimconemy_ArrowTurret_Power", 25 },
                { "Rimconemy_Campfire", 10 },
            };

        public static bool IsPhysicalInput(string defName)
        {
            return !string.IsNullOrEmpty(defName) && PhysicalInputs.Contains(defName);
        }

        public static bool IsCreditInput(string defName)
        {
            return !string.IsNullOrEmpty(defName) && CreditInputs.Contains(defName);
        }

        public static int RequiredUnits(string defName, string buildingDefName)
        {
            if (!string.Equals(defName, ConstructionDebrisDefName, StringComparison.Ordinal)
                || string.IsNullOrEmpty(buildingDefName))
                return 0;

            return DebrisRequirements.TryGetValue(buildingDefName, out int amount)
                ? amount
                : 0;
        }
    }
}
