using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Power
{
    /// <summary>
    /// Owner: Scavenger and Infrastructure (Package 03).
    /// Replacement for the historical <c>PowerChainStub</c>.
    ///
    /// Reads Setting-spezifische Power-Komponenten aus dem Live-Spiel:
    ///   - Solid-Fuel-Generator  (Wood / Coal / Chemfuel)
    ///   - Liquid-Fuel-Generator (Water-Turbine)
    ///   - Arrow-Turret          (Setting-Turret mit Strombedarf)
    ///
    /// Wir ersetzen RimWorld's Power-Net NICHT — wir bieten einen
    /// Read-Model-Service für UI/Telemetrie und ein FuelClass-Tag für
    /// Mod 04 (Outposts/Wallet). Das Pattern entspricht
    /// INTERFACE_CONTRACT §3 (Storage-Bridge analog für Power).
    /// </summary>
    public static class PowerChainService
    {
        public const string LogMarker = "v1";

        public const string SolidFuelGeneratorDefName = "Rimconemy_WoodCoalGenerator";
        public const string LiquidFuelGeneratorDefName = "Rimconemy_WaterTurbineGenerator";
        public const string TurbineWaterPump = "Rimconemy_TurbineWaterPump";
        public const string ArrowTurretDefName = "Rimconemy_ArrowTurret_Power";

        // Cached refs to DefDatabase lookups. Null until Resolve runs.
        private static ThingDef _solidDef;
        private static ThingDef _liquidDef;
        private static ThingDef _pumpDef;
        private static ThingDef _turretDef;

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                Resolve();
                Log.Message(
                    "[Rimconemy.ScavengerInfrastructure] PowerChainService ready: " +
                    $"solid={(_solidDef != null ? _solidDef.defName : "<missing>")}, " +
                    $"liquid={(_liquidDef != null ? _liquidDef.defName : "<missing>")}, " +
                    $"pump={(_pumpDef != null ? _pumpDef.defName : "<missing>")}, " +
                    $"turret={(_turretDef != null ? _turretDef.defName : "<missing>")}.");
            }
        }

        /// <summary>Resolve cached def-references from DefDatabase.</summary>
        public static void Resolve()
        {
            _solidDef = DefDatabase<ThingDef>.GetNamedSilentFail(SolidFuelGeneratorDefName);
            _liquidDef = DefDatabase<ThingDef>.GetNamedSilentFail(LiquidFuelGeneratorDefName);
            _pumpDef = DefDatabase<ThingDef>.GetNamedSilentFail(TurbineWaterPump);
            _turretDef = DefDatabase<ThingDef>.GetNamedSilentFail(ArrowTurretDefName);
        }

        public static ThingDef SolidFuelDef => _solidDef;
        public static ThingDef LiquidFuelDef => _liquidDef;
        public static ThingDef WaterPumpDef => _pumpDef;
        public static ThingDef ArrowTurretDef => _turretDef;

        /// <summary>Classify a generator building into FuelClass.</summary>
        public static FuelClass ClassifyGenerator(ThingDef def)
        {
            if (def == null) return FuelClass.None;
            if (_solidDef != null && def == _solidDef) return FuelClass.SolidFuel;
            if (_liquidDef != null && def == _liquidDef) return FuelClass.LiquidFuel;
            return FuelClass.None;
        }

        /// <summary>
        /// Walk all player-home maps and collect Setting Power-State objects
        /// (generators + turrets). Returns defensive empty list on no-map.
        /// </summary>
        public static List<PowerUnitState> CollectAllPowerUnits()
        {
            var result = new List<PowerUnitState>();
            if (Find.Maps == null) return result;

            foreach (var map in Find.Maps.Where(m => m != null && m.IsPlayerHome))
            {
                if (map.listerThings == null) continue;
                if (_solidDef != null)
                    result.AddRange(CollectGeneratorsOfType(map, _solidDef, FuelClass.SolidFuel));
                if (_liquidDef != null)
                    result.AddRange(CollectGeneratorsOfType(map, _liquidDef, FuelClass.LiquidFuel));
                if (_pumpDef != null)
                    result.AddRange(CollectConsumersOfType(map, _pumpDef, PowerUnitType.WaterPump));
                if (_turretDef != null)
                    result.AddRange(CollectTurrets(map, _turretDef));
            }
            return result;
        }

        private static IEnumerable<PowerUnitState> CollectGeneratorsOfType(Map map, ThingDef def, FuelClass fc)
        {
            if (map?.listerThings?.AllThings == null) yield break;
            var things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                var t = things[i];
                if (t == null || t.def != def) continue;
                yield return new PowerUnitState
                {
                    ThingId = t.thingIDNumber,
                    Type = PowerUnitType.Generator,
                    FuelClass = fc,
                    Position = t.Position,
                    MapId = map.uniqueID,
                    DefName = t.def.defName,
                    IsActive = t.TryGetComp<CompPowerTrader>()?.PowerOn ?? false,
                    HasFuel = SampleHasFuel(t, requiresFuel: true),
                };
            }
        }

        private static IEnumerable<PowerUnitState> CollectConsumersOfType(Map map, ThingDef def, PowerUnitType type)
        {
            if (map?.listerThings?.AllThings == null) yield break;
            var things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                var t = things[i];
                if (t == null || t.def != def) continue;
                yield return new PowerUnitState
                {
                    ThingId = t.thingIDNumber,
                    Type = type,
                    FuelClass = FuelClass.None,
                    Position = t.Position,
                    MapId = map.uniqueID,
                    DefName = t.def.defName,
                    IsActive = t.TryGetComp<CompPowerTrader>()?.PowerOn ?? false,
                    HasFuel = SampleHasFuel(t, requiresFuel: false),
                };
            }
        }

        private static IEnumerable<PowerUnitState> CollectTurrets(Map map, ThingDef def)
        {
            if (map?.listerThings?.AllThings == null) yield break;
            var things = map.listerThings.AllThings;
            for (int i = 0; i < things.Count; i++)
            {
                var t = things[i];
                if (t == null || t.def != def) continue;
                yield return new PowerUnitState
                {
                    ThingId = t.thingIDNumber,
                    Type = PowerUnitType.ArrowTurret,
                    FuelClass = FuelClass.None,
                    Position = t.Position,
                    MapId = map.uniqueID,
                    DefName = t.def.defName,
                    IsActive = t.TryGetComp<CompPowerTrader>()?.PowerOn ?? false,
                    HasFuel = SampleHasFuel(t, requiresFuel: false), // Arrow turrets don't burn fuel in vanilla
                };
            }
        }

        private static bool SampleHasFuel(Thing thing, bool requiresFuel)
        {
            var refuelable = thing?.TryGetComp<CompRefuelable>();
            if (refuelable != null)
                return refuelable.Fuel > 0f;

            // Consumers without a refuelable slot are not blocked by fuel;
            // fuel-producing generators without their required slot are not
            // falsely reported as fueled.
            return !requiresFuel;
        }

        /// <summary>
        /// Aggregate Power-Chain snapshot. Cheap to compute (one pass per
        /// map). Capability-gated from callers via Foundation.CapabilityAudit.
        /// </summary>
        public static PowerChainSnapshot GetChainSnapshot(long tick)
        {
            var units = CollectAllPowerUnits();
            int activeGenerators = 0;
            int activeTurrets = 0;
            int fueledCount = 0;
            foreach (var u in units)
            {
                if (u.IsActive)
                {
                    if (u.Type == PowerUnitType.Generator) activeGenerators++;
                    else if (u.Type == PowerUnitType.ArrowTurret) activeTurrets++;
                }
                if (u.HasFuel) fueledCount++;
            }
            return new PowerChainSnapshot
            {
                Tick = tick,
                TotalUnits = units.Count,
                ActiveGenerators = activeGenerators,
                ActiveTurrets = activeTurrets,
                FueledUnits = fueledCount,
                HasSolidFuel = _solidDef != null,
                HasLiquidFuel = _liquidDef != null,
                HasWaterPump = _pumpDef != null,
                ContentHash = ComputeHash(units),
            };
        }

        private static string ComputeHash(List<PowerUnitState> units)
        {
            // FNV-1a over the sorted canonical unit identity/state summary.
            var canonical = new System.Text.StringBuilder();
            foreach (var unit in units
                .OrderBy(u => u.MapId)
                .ThenBy(u => u.ThingId)
                .ThenBy(u => (int)u.Type)
                .ThenBy(u => u.DefName, System.StringComparer.Ordinal))
            {
                canonical.Append(unit.MapId).Append('|')
                    .Append(unit.ThingId).Append('|')
                    .Append(unit.DefName ?? "").Append('|')
                    .Append((int)unit.Type).Append('|')
                    .Append((int)unit.FuelClass).Append('|')
                    .Append(unit.IsActive ? '1' : '0').Append('|')
                    .Append(unit.HasFuel ? '1' : '0').Append(';');
            }

            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in canonical.ToString())
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return hash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }

    public enum FuelClass
    {
        None = 0,
        SolidFuel = 1,
        LiquidFuel = 2,
    }

    public enum PowerUnitType
    {
        Unknown = 0,
        Generator = 1,
        ArrowTurret = 2,
        WaterPump = 3,
    }

    /// <summary>Per-unit read snapshot. No 'live' mutation; recompute on tick.</summary>
    public struct PowerUnitState
    {
        public int ThingId;
        public PowerUnitType Type;
        public FuelClass FuelClass;
        public IntVec3 Position;
        public int MapId;
        public string DefName;
        public bool IsActive;
        public bool HasFuel;
    }

    /// <summary>Aggregate read snapshot. Cheap to compute, capability-gated.</summary>
    public struct PowerChainSnapshot
    {
        public long Tick;
        public int TotalUnits;
        public int ActiveGenerators;
        public int ActiveTurrets;
        public int FueledUnits;
        public bool HasSolidFuel;
        public bool HasLiquidFuel;
        public bool HasWaterPump;
        public string ContentHash;
    }
}
