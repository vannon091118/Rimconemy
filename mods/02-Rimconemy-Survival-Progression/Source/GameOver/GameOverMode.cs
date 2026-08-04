namespace Rimconemy.SurvivalProgression.GameOver
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02)
    /// Track 2-C / F-T2: GameOver policy selection.
    ///
    /// Semantics:
    /// - <see cref="Standard"/>: Default RimWorld-style policy. Colony wipe ends the game.
    ///   Maps to existing GameOverDetector.ReasonOutOfColonists logic.
    /// - <see cref="Sandbox"/>: No automatic end on colony wipe. Player continues until they
    ///   choose to load another save or end the run manually. "Laden oder Ende" mechanic
    ///   watches for incoming new colonists (via scenario/incidents) but does not surface
    ///   urgent prompts in the player's normal gameplay loop.
    ///
    /// The policy is stored in <see cref="Rimconemy.Foundation.Save.FoundationSaveData.IsSandboxMode"/>.
    /// Scenarios (ScenPart_StartInSandbox) toggle the flag at save-start. The Dashboard
    /// Settings tab may also flip it (Phase 1: dashboard-only; Phase 2: in-game options).
    /// </summary>
    public enum GameOverMode
    {
        /// <summary>Default. Colony wipe ends the game.</summary>
        Standard = 0,

        /// <summary>Colony wipe does NOT end the game; no urgent "Laden oder Ende" prompt.</summary>
        Sandbox = 1,
    }
}
