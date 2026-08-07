using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    ///
    /// RimconemyStorytellerComp — the StorytellerComp registered via
    /// <c>Rimconemy_Storyteller.xml</c>. Replaces the vanilla storyteller
    /// cycle. In the bootstrap phase, delegates to the existing
    /// StoryDirector.GameComponentTick.
    ///
    /// Phase 1 (2026-08-07): Bootstrap — enforces Single-Storyteller
    /// invariant via Props check, logs profile info.
    ///
    /// Phase 2 (planned): Full StoryDirector logic migrates here.
    ///   - BuildLiveSnapshot → static utility
    ///   - StorySelector → event selection
    ///   - TryFire(queued=false) → direct incident firing
    ///
    /// Design: DECISIONS §34 (korrigiert), STORYTELLER_ANALYSIS.md,
    ///         STORYTELLER_DESIGN_DECISIONS.md
    /// </summary>
    public class RimconemyStorytellerComp : StorytellerComp
    {
        private bool _bootstrapEmitted;

        /// <summary>
        /// Logs bootstrap info exactly once per game session.
        /// Called from <see cref="StoryDirector.GameComponentTick"/> on
        /// its first evaluation after this comp becomes active.
        /// </summary>
        public void EmitBootstrapIfNeeded()
        {
            if (_bootstrapEmitted) return;
            _bootstrapEmitted = true;

            var director = StoryDirector.Get();
            string profileId = director?.ActiveProfile?.ProfileId ?? "unknown";

            Log.Message(
                "[Rimconemy.InfectedAutomation] RimconemyStorytellerComp active. " +
                "Storyteller=Rimconemy, " +
                "profile=" + profileId + ", " +
                "difficulty=" + (Find.Storyteller?.difficultyDef?.defName ?? "unknown") + ", " +
                "tick=" + (Find.TickManager?.TicksGame ?? 0L));
        }
    }
}
