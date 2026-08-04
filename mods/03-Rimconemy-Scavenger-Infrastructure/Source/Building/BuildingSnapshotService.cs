using System;
using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.Maps;
using RimWorld;
using Rimconemy.ScavengerInfrastructure.Power;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Building
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03).
    /// Rebuilds the Building read model from loaded physical Things.
    /// No mutation and no direct cross-package state writes.
    /// </summary>
    public static class BuildingSnapshotService
    {
        private static readonly HashSet<string> OwnedDefNames
            = new HashSet<string>(StringComparer.Ordinal)
            {
                PowerChainService.SolidFuelGeneratorDefName,
                PowerChainService.LiquidFuelGeneratorDefName,
                PowerChainService.TurbineWaterPump,
                PowerChainService.ArrowTurretDefName,
            };

        public static List<BuildingSnapshot> Read(long tick)
        {
            var result = new List<BuildingSnapshot>();
            // Phase-2 / Welle 2 / Item #3 (2026-08-05): route through
            // MapRegistry.GetPlayerHomeMaps() (Foundation-owned) instead of
            // an ad-hoc LINQ Where(...) allocation per call. Snapshot list
            // is reused; no List<Map> closure allocated here.
            foreach (var map in MapRegistry.GetPlayerHomeMaps())
            {
                if (map.listerThings?.AllThings == null)
                    continue;

                foreach (var thing in map.listerThings.AllThings)
                {
                    if (thing?.def == null || !OwnedDefNames.Contains(thing.def.defName))
                        continue;

                    var refuelable = thing.TryGetComp<CompRefuelable>();
                    bool hasFuel = refuelable == null
                        ? IsNonFuelConsumer(thing.def.defName)
                        : refuelable.Fuel > 0f;
                    bool requiresFuel = refuelable != null;
                    var power = thing.TryGetComp<CompPowerTrader>();
                    bool powerOn = power == null || power.PowerOn;
                    bool destroyed = thing.DestroyedOrNull();
                    bool damaged = !destroyed && thing.MaxHitPoints > 0
                        && thing.HitPoints < thing.MaxHitPoints;
                    float damageRatio = thing.MaxHitPoints <= 0
                        ? 0f
                        : 1f - thing.HitPoints / (float)thing.MaxHitPoints;

                    result.Add(new BuildingSnapshot
                    {
                        SchemaVersion = BuildingSnapshot.CurrentSchemaVersion,
                        SnapshotTick = tick,
                        ThingId = thing.thingIDNumber,
                        MapId = map.uniqueID,
                        DefName = thing.def.defName,
                        Label = thing.LabelCap,
                        OwnerId = "player",
                        ConstructionState = destroyed
                            ? BuildingConstructionState.Destroyed
                            : damaged ? BuildingConstructionState.Damaged
                            : BuildingConstructionState.Built,
                        PowerState = destroyed
                            ? BuildingPowerState.Unknown
                            : requiresFuel && !hasFuel ? BuildingPowerState.Blocked
                            : !powerOn ? BuildingPowerState.Offline
                            : BuildingPowerState.Online,
                        FuelState = !requiresFuel
                            ? BuildingFuelState.NotRequired
                            : hasFuel ? BuildingFuelState.Available
                            : BuildingFuelState.Missing,
                        DamageState = destroyed
                            ? BuildingDamageState.Destroyed
                            : damaged ? BuildingDamageState.Damaged
                            : BuildingDamageState.Intact,
                        HasFuel = hasFuel,
                        DamageRatio = damageRatio,
                        InputResourceIds = InputIdsFor(thing.def.defName),
                        InputAmounts = InputAmountsFor(thing.def.defName),
                        InputsAreAlternatives = thing.def.defName == PowerChainService.SolidFuelGeneratorDefName,
                    });
                }
            }

            result = result
                .OrderBy(snapshot => snapshot.MapId)
                .ThenBy(snapshot => snapshot.ThingId)
                .ThenBy(snapshot => snapshot.DefName, StringComparer.Ordinal)
                .ToList();

            string hash = BuildingSnapshot.ComputeContentHash(result);
            foreach (var snapshot in result)
                snapshot.ContentHash = hash;
            return result;
        }

        private static bool IsNonFuelConsumer(string defName)
        {
            return defName == PowerChainService.TurbineWaterPump
                || defName == PowerChainService.ArrowTurretDefName;
        }

        private static List<string> InputIdsFor(string defName)
        {
            var ids = new List<string>();
            foreach (var pair in InputAmountsFor(defName)) ids.Add(pair.Key);
            return ids;
        }

        private static Dictionary<string, int> InputAmountsFor(string defName)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            switch (defName)
            {
                case PowerChainService.SolidFuelGeneratorDefName:
                    // WoodLog OR Chemfuel; the amount applies to one valid fuel type.
                    result["WoodLog"] = 30;
                    result["Chemfuel"] = 30;
                    break;
                case PowerChainService.LiquidFuelGeneratorDefName:
                    result["Rimconemy_DistilledWater"] = 40;
                    break;
                case PowerChainService.TurbineWaterPump:
                    result["Rimconemy_ConstructionDebris"] = 20;
                    break;
                case PowerChainService.ArrowTurretDefName:
                    result["Rimconemy_ConstructionDebris"] = 25;
                    result["Steel"] = 10;
                    break;
            }
            return result;
        }
    }
}
