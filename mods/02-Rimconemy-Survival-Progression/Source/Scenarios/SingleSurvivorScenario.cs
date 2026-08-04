using Verse;

namespace Rimconemy.SurvivalProgression.Scenarios
{
    /// <summary>
    /// Owner: Survival &amp; Progression.
    /// Single-survivor scenario scaffold. The actual ScenarioDef lives in
    /// Defs/Scenarios/SingleSurvivor.xml; this class only formats the
    /// startup message and holds the readability boundary.
    /// SPIKE: API-RESEARCH-01 / API-GAMEOVER-01.
    /// </summary>
    public static class SingleSurvivorScenario
    {
        public const string DefName = "Rimconemy_SingleSurvivor";

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                Log.Message($"[Rimconemy.SurvivalProgression] Scenario scaffold '{DefName}' loaded. Vanilla storyteller authority retained.");
            }
        }
    }
}
