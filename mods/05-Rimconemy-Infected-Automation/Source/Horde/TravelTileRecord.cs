// Source/Horde/TravelTileRecord.cs
//
// Phase F — Travel-Tile-State (FSM über 5-Tile Rolling-Window).
// Idle→Migrating→Staging (timer-decrement OR activate)→Attacking→Idle.
// Spec §3.3.

using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public enum TravelTileStatus { Idle = 0, Migrating = 1, Staging = 2, Attacking = 3 }

    public struct TravelTileRecord : IExposable
    {
        public int Tile;
        public TravelTileStatus Status;
        public long LastTransitionTick;
        public int ActiveStagingTicksLeft;
        public long LastSeenAtTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref Tile, "tile", 0);
            Scribe_Values.Look(ref Status, "status", TravelTileStatus.Idle);
            Scribe_Values.Look(ref LastTransitionTick, "lastTransitionTick", 0L);
            Scribe_Values.Look(ref ActiveStagingTicksLeft, "activeStagingTicksLeft", 0);
            Scribe_Values.Look(ref LastSeenAtTick, "lastSeenAtTick", 0L);
        }
    }
}
