using System.Collections.Generic;

namespace Rimconemy.ScavengerInfrastructure.Storage
{
    /// <summary>
    /// Which maps/storages to include in a snapshot.
    /// </summary>
    public enum StorageScope
    {
        /// <summary>All player-home maps (IsPlayerHome == true).</summary>
        PlayerHomeMaps,

        /// <summary>All currently loaded maps.</summary>
        AllLoadedMaps,

        /// <summary>Specific map by uniqueID (requires mapId).</summary>
        SpecificMap,

        /// <summary>All maps including caravan/temporary maps.</summary>
        AllMapsIncludingCaravans,
    }

    /// <summary>
    /// Filter rules for ReadStorage. If null, all resources pass.
    /// </summary>
    public struct ResourceFilter
    {
        /// <summary>If non-empty, only include these ResourceIds (defNames).</summary>
        public List<string> WhitelistResourceIds;

        /// <summary>If non-empty, exclude these ResourceIds.</summary>
        public List<string> BlacklistResourceIds;

        /// <summary>Minimum availability. Default: Available.</summary>
        public StorageAvailability MinAvailability;

        /// <summary>If non-null, only include these ThingCategory defNames.</summary>
        public List<string> WhitelistCategories;

        /// <summary>Include items in pawn inventories.</summary>
        public bool IncludePawnInventory;

        /// <summary>Include items lying on the ground.</summary>
        public bool IncludeGroundItems;

        /// <summary>Include items in storage zones/shelves.</summary>
        public bool IncludeStorageZones;
    }
}
