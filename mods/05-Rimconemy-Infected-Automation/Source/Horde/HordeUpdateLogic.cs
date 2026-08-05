// Source/Horde/HordeUpdateLogic.cs
//
// Phase D — Pure spawn/drift math for the wandering HordeWorldObject.
// No IO, no Verse.* types, no DefDatabase read — a test seam for the
// production Spawner. Pure-API design lets regression tests cover
// "spawn at initial distance", "drift toward home", "arrival at home"
// without spinning up a GameComponent.

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeUpdateLogic
    {
        public const int TickInterval = 250;
        public const int InitialDistanceFromHome = 5;

        /// <summary>
        /// Pure position function (spec §6): the horde's world tile is
        /// derived ONLY from the game tick — no persisted state, so
        /// Save/Load resumes at the same tile and any activation moment
        /// yields a consistent position.
        ///
        /// <c>tile = homeTile + max(0, InitialDistanceFromHome − floor(tick/250))</c>
        ///
        /// tick 0–249   → home + 5  (initial spawn distance)
        /// tick 500     → home + 3  (2 tiles drifted)
        /// tick 1250+   → home      (arrived; clamped, never below home)
        /// </summary>
        public static int ComputeHordeTile(int homeTile, long currentTick)
        {
            int drifted = (int)(currentTick / TickInterval);
            return homeTile + System.Math.Max(0, InitialDistanceFromHome - drifted);
        }
    }
}
