using System.Collections.Generic;
using Verse;

namespace Rimconemy.EconomyTerritory.Territory
{
    /// <summary>
    /// Owner: Economy &amp; Territory.
    /// Territory graph node. MainBase -> Proxy -> Outpost or Ruin.
    /// DisconnectDeadlineTick uses absolute world ticks; after 180,000 ticks
    /// (3 in-game days, 1 day = 60,000 ticks) a Ruined record replaces the Outpost.
    /// SPIKE: API-WORLD-01.
    /// </summary>
    public enum TerritoryNodeType { MainBase, Proxy, Outpost, Ruin }

    public sealed class TerritoryNode
    {
        public string NodeId;
        public TerritoryNodeType NodeType;
        public bool ConnectedToMainBase;
        public string RouteId;
        public bool ConnectionState;
        public long DisconnectDeadlineTick;
        public long LastUpdatedTick;

        public const string LogMarker = "v0";
    }
}
