// Source/Horde/HordeUpdateLogic.cs
//
// Phase D — Pure Spawn/Move/Despawn state-machine for the wandering
// HordeWorldObject. Mirrors PopulationLedgerReconciler.ReconciliationLogic:
// no IO, no Verse.* types, no DefDatabase read — a test seam for the
// production Spawner. Pure-API design lets regression tests cover
// "spawn at threshold", "drift toward home", "despawn below threshold"
// without spinning up a GameComponent.

using System.Collections.Generic;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeUpdateLogic
    {
        public const int TickInterval = 250;
        public const int InitialDistanceFromHome = 5;

        /// <summary>
        /// Pure entry-point: spawn / drift / despawn one Horde per home tile.
        /// Mutates <paramref name="hordeTiles"/> in place to keep
        /// "where the horde is" as an externalized state. The Spawner
        /// (MapComponent) translates the result into actual Verse.WorldObject
        /// placement; the tests inspect the list directly.
        /// </summary>
        public static void RunOncePure(
            int effective, bool active, int homeTile, long currentTick,
            List<int> hordeTiles)
        {
            if (hordeTiles == null) return;
            if (!active)
            {
                hordeTiles.Clear();
                return;
            }
            if (homeTile < 0) return; // defensive: no player home → no spawn.

            // First spawn: place at homeTile + InitialDistanceFromHome.
            if (hordeTiles.Count == 0)
            {
                hordeTiles.Add(homeTile + InitialDistanceFromHome);
                return;
            }

            // Drift: each TickInterval, move 1 tile toward home.
            int moves = (int)(currentTick / TickInterval);
            if (moves < 1) return;
            int slotIndex = hordeTiles[0] - homeTile;
            if (slotIndex <= 0)
            {
                // Already at home — clamp to home so subsequent runs panick-free.
                hordeTiles[0] = homeTile;
                return;
            }
            int newSlot = System.Math.Max(0, slotIndex - moves);
            hordeTiles[0] = homeTile + newSlot;
        }
    }
}
