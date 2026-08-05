// Source/Horde/HordeWorldObject.cs
//
// Phase D — Verse.WorldObject subclass for the wandering Horde. Lives
// on the world-map; oriented to move toward the player home tile.
// Mirrors OutpostWorldObject pattern (Mod 04).
//
// Marker type: tile + drift state are owned entirely by HordeSpawner
// (the MapComponent pulls currentTick and assigns Tile via the Pure
// HordeUpdateLogic). No tick-time work or persistence needed here.

using RimWorld.Planet;

namespace Rimconemy.InfectedAutomation.Horde
{
    public class HordeWorldObject : WorldObject
    {
    }
}
