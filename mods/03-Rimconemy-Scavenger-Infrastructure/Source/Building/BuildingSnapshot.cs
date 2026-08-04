using System.Collections.Generic;
using System.Linq;

namespace Rimconemy.ScavengerInfrastructure.Building
{
    public enum BuildingConstructionState
    {
        Unknown = 0,
        Planned = 1,
        Built = 2,
        Damaged = 3,
        Destroyed = 4,
    }

    public enum BuildingPowerState
    {
        Unknown = 0,
        Offline = 1,
        Blocked = 2,
        Online = 3,
    }

    public enum BuildingFuelState
    {
        NotRequired = 0,
        Missing = 1,
        Available = 2,
    }

    public enum BuildingDamageState
    {
        Intact = 0,
        Damaged = 1,
        Destroyed = 2,
    }

    /// <summary>
    /// Read-only-by-contract representation of one Package-03 building.
    /// It is rebuilt from live Things and is not itself persisted.
    /// </summary>
    public sealed class BuildingSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public long SnapshotTick;
        public int ThingId;
        public int MapId;
        public string DefName;
        public string Label;
        public string OwnerId;
        public BuildingConstructionState ConstructionState;
        public BuildingPowerState PowerState;
        public BuildingFuelState FuelState;
        public BuildingDamageState DamageState;
        public bool HasFuel;
        public float DamageRatio;
        public List<string> InputResourceIds = new List<string>();
        public Dictionary<string, int> InputAmounts = new Dictionary<string, int>();
        /// <summary>True when one of the listed input resources is sufficient.</summary>
        public bool InputsAreAlternatives;
        public string ContentHash;

        public static string ComputeContentHash(IEnumerable<BuildingSnapshot> snapshots)
        {
            unchecked
            {
                uint hash = 2166136261;
                if (snapshots != null)
                {
                    foreach (var snapshot in snapshots)
                    {
                        string value = (snapshot?.ThingId ?? 0) + "|"
                            + (snapshot?.MapId ?? 0) + "|"
                            + (snapshot?.DefName ?? "") + "|"
                            + (int)(snapshot?.ConstructionState ?? BuildingConstructionState.Unknown) + "|"
                            + (int)(snapshot?.PowerState ?? BuildingPowerState.Unknown) + "|"
                            + (int)(snapshot?.FuelState ?? BuildingFuelState.NotRequired) + "|"
                            + (int)(snapshot?.DamageState ?? BuildingDamageState.Intact) + "|"
                            + (snapshot?.HasFuel == true ? "1" : "0") + "|"
                            + (snapshot?.OwnerId ?? "") + "|"
                            + (snapshot?.InputsAreAlternatives == true ? "alternatives" : "required") + "|"
                            + (snapshot?.DamageRatio ?? 0f).ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture);
                        if (snapshot?.InputAmounts != null)
                        {
                            foreach (var input in snapshot.InputAmounts
                                .OrderBy(pair => pair.Key, System.StringComparer.Ordinal))
                                value += "|" + input.Key + "=" + input.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        for (int i = 0; i < value.Length; i++)
                        {
                            hash ^= value[i];
                            hash *= 16777619;
                        }
                    }
                }
                return hash.ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
