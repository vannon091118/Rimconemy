using System.Collections.Generic;
using Rimconemy.Foundation.Maps;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Plants
{
    /// <summary>
    /// Owner: Scavenger and Infrastructure (Package 03).
    /// Read-only plant helper for the Survive setting.
    ///
    /// Live Read-Model Service resolving Setting-plant configurations and
    /// counting live plants on the map:
    ///   * <see cref="ResolvePlant"/> resolves a <see cref="ThingDef"/>
    ///     (with a <c>&lt;plant&gt;</c> sub-block) via DefDatabase and
    ///     yields its yield/soil/growth settings.
    ///   * <see cref="CollectSpawnedPlants"/> walks each player-home map
    ///     to count live plants of each known Setting-def.
    ///   * <see cref="ClassifyPlant"/> + <see cref="IsFoodPlant"/> enforce
    ///     the Hemp-is-Not-Food contract so future tweaks cannot
    ///     accidentally re-market hemp as food.
    /// </summary>
    public static class PlantHelper
    {
        public const string LogMarker = "v1";

        // Setting-plant-defnames (consumed from Defs/ThingDefs/Plants/).
        // NOTE: these are the Plant ThingDefs, NOT the harvested
        // Resource-defNames (which are Rimconemy_HempLeafy and
        // Rimconemy_QuickRice respectively).
        public const string HempDefName = "Rimconemy_Hemp";
        public const string FoodPlantDefName = "Rimconemy_QuickRicePlant";

        // Hemp is industrial fibre, never food. This contract is enforced
        // by PlantHelperService.IsFoodPlant so the assertion is testable.
        public const bool HempIsFood = false;

        // Cached DefDatabase lookups (Plant is stored as a ThingDef
        // with a <plant> sub-block, not a separate class).
        private static ThingDef _hempDef;
        private static ThingDef _foodPlantDef;

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                Resolve();
                Log.Message(
                    "[Rimconemy.ScavengerInfrastructure] PlantHelperService ready: " +
                    $"hemp={(_hempDef != null ? _hempDef.defName : "<missing>")}, " +
                    $"food-plant={(_foodPlantDef != null ? _foodPlantDef.defName : "<missing>")}, " +
                    $"hempIsFood={HempIsFood}.");
            }
        }

        /// <summary>Resolve cached def references.</summary>
        public static void Resolve()
        {
            _hempDef = DefDatabase<ThingDef>.GetNamedSilentFail(HempDefName);
            _foodPlantDef = DefDatabase<ThingDef>.GetNamedSilentFail(FoodPlantDefName);
        }

        public static ThingDef HempPlant => _hempDef;
        public static ThingDef FoodPlant => _foodPlantDef;

        /// <summary>
        /// Build a SettingPlantState read-snapshot for a Plant
        /// ThingDef name. Returns null if the defName is unknown.
        /// </summary>
        public static SettingPlantState ResolvePlant(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return default;

            var tdef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
            if (tdef == null) return default;

            PlantProperties plantBlock = tdef.plant;

            float minFertility = plantBlock != null ? Mathf.Clamp01(plantBlock.fertilityMin) : 0f;
            float optimalFertility = plantBlock != null && plantBlock.fertilitySensitivity > 0f
                ? minFertility + Mathf.Clamp01(plantBlock.fertilitySensitivity) * 0.15f
                : minFertility;

            return new SettingPlantState
            {
                DefName = tdef.defName,
                Label = tdef.label,
                Category = ClassifyPlant(tdef),
                HarvestYield = plantBlock?.harvestYield ?? 0f,
                GrowDays = plantBlock != null && plantBlock.growDays > 0f ? plantBlock.growDays : 60f,
                SowWork = plantBlock?.sowWork > 0.001f ? plantBlock.sowWork : 200f,
                MinFertility = minFertility,
                OptimalFertility = Mathf.Min(1f, optimalFertility),
                IsFood = IsFoodPlant(tdef),
            };
        }

        /// <summary>
        /// Classifies a plant-thing into a Setting category. Hemp always
        /// falls in Industrial regardless of patch overrides; food plants
        /// fall in Food; everything else Legacy (kept here so future
        /// expansion can route to other Setting categories).
        /// </summary>
        public static PlantCategory ClassifyPlant(ThingDef tdef)
        {
            if (tdef == null) return PlantCategory.Unknown;
            if (_hempDef != null && tdef == _hempDef) return PlantCategory.Industrial;
            if (IsFoodPlant(tdef)) return PlantCategory.Food;
            return PlantCategory.Legacy;
        }

        /// <summary>
        /// True if the plant can be eaten. Hemp is hard-locked to false;
        /// food is true; everything else falls back to the
        /// <c>ingestible.preferability != NeverForNutrition</c> signal.
        /// </summary>
        public static bool IsFoodPlant(ThingDef tdef)
        {
            if (tdef == null) return false;
            if (_hempDef != null && tdef == _hempDef) return false; // hard contract
            // food plants have an ingestible block whose preferability is
            // not NeverForNutrition. Hemp-XML enforces the NeverForNutrition
            // contract; we read it back here as the same signal so future
            // XML edits cannot silently re-market hemp as food.
            if (tdef.ingestible == null) return false;
            return tdef.ingestible.preferability != FoodPreferability.NeverForNutrition;
        }

        /// <summary>
        /// Walks the player-home maps and counts live plants per Setting
        /// defName. Useful as a snapshot for telemetry. Capability-gated
        /// by callers; this method does NOT enforce the gate itself.
        /// </summary>
        public static Dictionary<string, int> CollectSpawnedPlants()
        {
            var result = new Dictionary<string, int>();

            // Phase-2 / Welle 2 / Item #3 (2026-08-05): MapRegistry route.
            // MapRegistry.GetPlayerHomeMaps() returns the tick-cached immutable
            // snapshot; iterating it costs no LINQ closure allocation.
            foreach (var map in MapRegistry.GetPlayerHomeMaps())
            {
                if (map?.listerThings == null) continue;
                var things = map.listerThings.AllThings;
                if (things == null) continue;
                for (int i = 0; i < things.Count; i++)
                {
                    var plant = things[i] as Plant;
                    if (plant?.def == null) continue;
                    if (_hempDef != null && plant.def == _hempDef)
                        Increment(result, HempDefName);
                    else if (_foodPlantDef != null && plant.def == _foodPlantDef)
                        Increment(result, FoodPlantDefName);
                }
            }
            return result;
        }

        private static void Increment(Dictionary<string, int> dict, string key)
        {
            if (dict.TryGetValue(key, out int n))
                dict[key] = n + 1;
            else
                dict[key] = 1;
        }
    }

    public enum PlantCategory
    {
        Unknown = 0,
        Food = 1,
        Industrial = 2,
        Legacy = 3,
    }

    /// <summary>Read snapshot for one Setting plant.</summary>
    public struct SettingPlantState
    {
        public string DefName;
        public string Label;
        public PlantCategory Category;
        public float HarvestYield;
        public float GrowDays;
        public float SowWork;
        public float MinFertility;
        public float OptimalFertility;
        public bool IsFood;
    }
}
