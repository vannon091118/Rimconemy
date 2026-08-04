using System.Collections.Generic;

namespace Rimconemy.ScavengerInfrastructure.Storage
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03)
    /// Read by: Story Writer, UI/Foundation, Economy.
    ///
    /// Immutable read-model of all physical storage resources.
    /// Single source of truth — no parallel ledger, no abstract inventory.
    /// Not persisted directly; rebuilt from Map.listerThings on demand.
    ///
    /// Specification: docs/H4-storage-query-contract.md
    /// </summary>
    public sealed class StorageSnapshot
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion = CurrentSchemaVersion;
        public long SnapshotTick;
        public StorageScope Scope;

        /// <summary>Aggregated entries, sorted by ResourceId ascending.</summary>
        public List<StorageEntry> Entries;

        /// <summary>Maps unavailable at snapshot time. Key = map uniqueID.</summary>
        public Dictionary<int, string> UnavailableMaps;

        /// <summary>Stable hash of all entries for cache validation.</summary>
        public string ContentHash;
    }

    /// <summary>
    /// Aggregated entry for one resource type across all in-scope maps.
    /// </summary>
    public sealed class StorageEntry
    {
        /// <summary>Stable ThingDef.defName (e.g. "WoodLog").</summary>
        public string ResourceId;

        /// <summary>Display label from ThingDef.label.</summary>
        public string Label;

        /// <summary>Sum of all stackCounts across all maps/stacks.</summary>
        public int TotalAmount;

        /// <summary>Number of distinct stacks.</summary>
        public int StackCount;

        /// <summary>Worst availability across all stacks.</summary>
        public StorageAvailability Availability;

        /// <summary>Map uniqueIDs where this resource is found.</summary>
        public List<int> MapIds;

        /// <summary>Quality aggregation (null if resource has no quality).</summary>
        public QualityAggregation? Quality;

        /// <summary>Rot/decay aggregation (null if not perishable).</summary>
        public RotAggregation? Rot;
    }

    public enum StorageAvailability
    {
        Available,
        Blocked,
        Unavailable,
        Frozen,
    }

    public struct QualityAggregation
    {
        public int MinQuality;   // 0 (Awful) – 6 (Legendary)
        public float AvgQuality;
        public int MaxQuality;
        public int StackCount;   // number of stacks with quality data
    }

    public struct RotAggregation
    {
        public float MinRotProgress;  // 0–1
        public float AvgRotProgress;
        public float MaxRotProgress;
        public int RottenStacks;      // past rot threshold
        public int FreshStacks;
    }
}
