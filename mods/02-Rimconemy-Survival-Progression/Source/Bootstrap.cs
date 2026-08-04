using System.Reflection;
using HarmonyLib;
using Rimconemy.SurvivalProgression.Needs;
using Rimconemy.SurvivalProgression.Progression;
using Rimconemy.SurvivalProgression.Scenarios;
using Rimconemy.SurvivalProgression.Tests;
using Verse;

namespace Rimconemy.SurvivalProgression
{
    /// <summary>
    /// Package-02 startup contract. Runtime state lives in
    /// ProgressionGameComponent; this constructor only verifies registration.
    /// Phase B additions: startup contracts for F-V2 (sole-owner GameOver),
    /// F-V4 (capability-gates) and Track 2-C / S-T1 (NeedMappingService).
    /// Phase-5 Bio-Remap (2026-08-04): runs the BioRemap regression suite.
    /// Phase-5 Bio-Remap audit-round-5 (2026-08-04): applies Harmony patches
    /// so the new-game customisation screen shows the corrected age=18.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        // Phase-5 audit-round-5: unique Harmony instance id for this assembly
        // so Race conditions / cross-package patches are debuggable.
        private const string HarmonyInstanceId = "rimconemy.survivalprogression";

        static Bootstrap()
        {
            Log.Message("[Rimconemy.SurvivalProgression] Survival runtime starting...");
            Log.Message(
                "[Rimconemy.SurvivalProgression] NeedMappingService active: " +
                "Setting Needdefs (Rimconemy_Need_Food/Safety/Social) project Vanilla Food/Rest/Joy onto 0..1 read scale. " +
                "Needdefs are NOT attached to pawns (Q14 contract honored).");
            Log.Message("[Rimconemy.SurvivalProgression] Active jobs award bounded XP every 250 ticks; state is persisted per pawn.");
            Log.Message(
                $"[Rimconemy.SurvivalProgression] Scenario contract: " +
                $"{SingleSurvivorScenario.DefName} active; Sandbox toggle wired to Foundation/FoundationSaveData.RimconemySandboxMode.");
            Log.Message(
                $"[Rimconemy.SurvivalProgression] Runtime ready: " +
                $"needs={SurvivalNeedCategory.All.Count}, " +
                $"mappings={NeedMappingService.All.Count}, " +
                $"schema={ProgressionGameComponent.CurrentSchemaVersion}, " +
                $"scenario={SingleSurvivorScenario.DefName}");

            // Phase-5 Bio-Remap audit-round-5 (2026-08-04): register all
            // [HarmonyPatch] classes in this assembly. PatchAll is idempotent
            // (Harmony skips already-patched methods) so re-runs across
            // sessions are safe. Current patch: Page_ConfigureStartingPawnsBioPatch
            // on Page_ConfigureStartingPawns.PreOpen - applies age=18 to each
            // starting pawn before the customisation screen first renders.
            try
            {
                var harmony = new Harmony(HarmonyInstanceId);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message(
                    $"[Rimconemy.SurvivalProgression] Harmony patches applied " +
                    $"(instance={HarmonyInstanceId}).");
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    $"[Rimconemy.SurvivalProgression] Harmony PatchAll failed: " +
                    $"{ex.GetType().Name}: {ex.Message}. Customization-page BioRemap skipped.");
            }

            // Phase-5 Bio-Remap regression (2026-08-04): ensure every patch
            // to the age-fix / skill-budget pipeline still passes its invariants
            // before any in-game colonist is touched.
            BioRemapTests.RunAll();
            NeedMappingServiceTests.RunAll();
            ScenarioContractTests.RunAll();
            BuildingProgressionRegressionTests.RunAll();
            BuildingProgressionPersistenceRegressionTests.RunAll();
            // Phase-1.1 (2026-08-04): deterministic dedup for single-survivor setup.
            RimconemyStartStateRegressionTests.RunAll();
            // Phase-8 (2026-08-04): organic XP tree — Domain, Unlock, Bridge.
            DomainXpStateTests.RunAll();
            UnlockServiceTests.RunAll();
            RimconemyUnlockExtensionTests.RunAll();
            BuildingCompletionBridgeTests.RunAll();
            // Phase-4.2 Character Setup Save-State: schema + upsert + Get round-trip.
            Tests.CharacterSetupStateRegressionTests.RunAll();
            // Phase-2.8 (2026-08-04): Save/Load-SchemaBump v0 → v1 Beleg for Audit §B6.
            Tests.CharacterSetupStateSchemaBumpTests.RunAll();
            // Phase-2.7 (2026-08-04): Bedürfnis-Effekt — NeedAmplifier deterministic Hunger-Tick.
            Tests.HungerAmplifierTests.RunAll();
            // Image-audit (2026-08-04): Bio-Remap + Skill-Budget hardening.
            // ForceAge18 / ForceResetAllSkills / EnforceHardBudgetCap invariants.
            Tests.BioRemapHardeningRegressionTests.RunAll();
            Log.Message("[Rimconemy.SurvivalProgression] Building XP adapter available; live construction-output hook remains an interactive A-gate.");
        }
    }
}
