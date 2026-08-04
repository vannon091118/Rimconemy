using Rimconemy.Foundation.Save;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Scenarios
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02)
    /// Track 2-C / S-T3: ScenPart that toggles <see cref="FoundationSaveData.IsSandboxMode"/>
    /// on Scenario-start. Use this on a scenario that should always start in Sandbox mode
    /// (no auto GameOver upon colony-wipe).
    ///
    /// Hook reason: ScenPart.PostWorldGenerate is the canonical RimWorld hook for
    /// "scenario-started, world-generated, game-ready". Mod 02 reads the Foundation
    /// flag from ProgressionGameComponent; the flag is set here once per save.
    /// </summary>
    public class ScenPart_StartInSandbox : ScenPart
    {
        public override void PostWorldGenerate()
        {
            base.PostWorldGenerate();
            var sd = Current.Game?.GetComponent<FoundationSaveData>();
            if (sd == null)
            {
                Log.Warning("[Rimconemy.SurvivalProgression] ScenPart_StartInSandbox: FoundationSaveData missing; sandbox flag not set.");
                return;
            }
            sd.IsSandboxMode = true;
            Log.Message("[Rimconemy.SurvivalProgression] ScenPart_StartInSandbox: IsSandboxMode = true (Sandbox-Mode active).");
        }
    }
}
