// Source/Horde/HordeWorldObject.cs
//
// Phase D — Verse.WorldObject subclass for the wandering Horde. Lives
// on the world-map; oriented to move toward the player home tile.
// Mirrors OutpostWorldObject pattern from Mod 04.

using RimWorld.Planet;

namespace Rimconemy.InfectedAutomation.Horde
{
    /// <summary>
    /// Phase D — Verse.WorldObject subclass for the wandering Horde.
    /// Lives on the world-map; oriented to move toward the player home
    /// tile. Mirrors OutpostWorldObject pattern (Mod 04).
    ///
    /// Drift state and tile are owned by <see cref="HordeSpawner"/>
    /// (the MapComponent pulls currentTick and assigns Tile via the
    /// Pure <see cref="HordeUpdateLogic"/>). No tick-time work needed
    /// in this class itself.
    /// </summary>
    public class HordeWorldObject : WorldObject
    {
        // Transient (no Scribe). Drift-state derived from currentTick.
        public long LastMoveTick;
    }
}
