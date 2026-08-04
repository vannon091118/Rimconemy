using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.Colonials;
using Rimconemy.Foundation.Save;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.GameOver
{
    /// <summary>
    /// Owner: Survival &amp; Progression.
    /// Game-over detection rooted exclusively at directly controlled player colonists.
    /// Outpost population and mechadroids never count.
    ///
    /// Track 2-C / S-T2: Sandbox-mode awareness.
    /// In <see cref="GameOverMode.Sandbox"/> the GameOver trigger is suppressed entirely —
    /// the player's choice is to keep playing until they manually load or end the run.
    /// The current GameOverMode is read from FoundationSaveData.IsSandboxMode.
    /// </summary>
    public static class GameOverDetector
    {
        public const string ReasonOutOfColonists = "All player-controlled colonists are dead";
        public const string ReasonResearchFailure = "Required base technology unobtainable";
        public const string ReasonSandboxSuppressed = "Sandbox-Mode suppressed automatic game-over";

        public static bool IsPlayerColonist(Pawn pawn)
        {
            if (pawn == null) return false;
            return pawn.IsColonist;
        }

        public static int CountPlayerColonists(Map map)
        {
            if (map == null) return 0;
            return map.mapPawns?.FreeColonistsSpawnedCount ?? 0;
        }

        /// <summary>
        /// Current GameOverMode derived from FoundationSaveData. Returns
        /// <see cref="GameOverMode.Sandbox"/> when the save data has the flag set,
        /// otherwise <see cref="GameOverMode.Standard"/>. Falls back to Standard
        /// when the GameComponent is not yet attached (e.g. main menu).
        /// </summary>
        public static GameOverMode CurrentMode
        {
            get
            {
                var sd = Current.Game?.GetComponent<FoundationSaveData>();
                if (sd == null) return GameOverMode.Standard;
                return sd.IsSandboxMode ? GameOverMode.Sandbox : GameOverMode.Standard;
            }
        }

        /// <summary>
        /// Returns the human-readable GameOver reason string for the given mode
        /// and empty-colonist observation. Sandbox mode returns the suppression
        /// reason; standard mode returns the colonist-loss reason.
        /// </summary>
        public static string ReasonForModeEmpty(GameOverMode mode)
        {
            return mode == GameOverMode.Sandbox ? ReasonSandboxSuppressed : ReasonOutOfColonists;
        }

        /// <summary>
        /// Backward-compat counter used by older callers. Defaults to the
        /// ColonialReader active count (which already excludes Dead, DestroyedOrNull
        /// and non-Humanlike pawns).
        /// </summary>
        public static int ActivePlayerColonistsAllMaps()
        {
            return ColonialReader.ActiveColonistCount;
        }

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                Log.Message("[Rimconemy.SurvivalProgression] Game-over rule registered: only directly controlled player colonists count.");
            }
        }
    }
}

