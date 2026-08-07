using System;
using Rimconemy.SurvivalProgression.Phase;
using RimWorld;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Phase-Progress Resolver — Def-level SSOT probes.
    /// Owner: Survival &amp; Progression (Package 02).
    ///
    /// Validates the milestone table compiles and references known Def names.
    /// Does not require a live Map (resolver is null-Map-safe by contract).
    /// </summary>
    public static class PhaseProgressResolverTests
    {
        private static TestSuite ts;
        public const string TestGroup = "Rimconemy.SurvivalProgression";

        public static int RunAll()
        {
            ts = new TestSuite("SurvivalProgression", "PhaseProgress regression tests");

            int failures = 0;
            int probes = 0;

            // 1) Resolve(null) returns an honest empty snapshot.
            probes++;
            try
            {
                var snap = PhaseProgressResolver.Resolve(null);
                if (snap == null)
                {
                    Log.Error("[Rimconemy.SurvivalProgression] PhaseProgress FAIL: Resolve(null) returned null");
                    failures++;
                }
                else if (snap.EmptyReason == null || snap.TotalMilestonesAcrossPhases <= 0)
                {
                    Log.Error("[Rimconemy.SurvivalProgression] PhaseProgress FAIL: Resolve(null) must set EmptyReason with a positive milestone count");
                    failures++;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Rimconemy.SurvivalProgression] PhaseProgress FAIL: Resolve(null) threw: " + ex.GetType().Name + ": " + ex.Message);
                failures++;
            }

            // 2) PhaseId enum keeps the None + 6 phases contract.
            probes++;
            int phaseCount = Enum.GetValues(typeof(PhaseId)).Length;
            if (phaseCount != 7)
            {
                Log.Error("[Rimconemy.SurvivalProgression] PhaseProgress FAIL: expected 7 PhaseId entries (None + 6 phases), got " + phaseCount);
                failures++;
            }

            // 3) Def SSOT — ThingDefs referenced by early-phase milestones must resolve.
            probes++;
            try
            {
                string[] refs =
                {
                    "MealSimple",
                    "Rimconemy_Campfire", "FueledStove", "ElectricStove",
                    "Rimconemy_Coal", "Steel", "FueledSmithy",
                    "ComponentIndustrial", "TableMachining",
                    "Rimconemy_StainlessSteel", "Rimconemy_StainlessSteelTower",
                    "Rimconemy_WoodCoalGenerator",
                };
                int present = 0;
                for (int i = 0; i < refs.Length; i++)
                {
                    if (DefDatabase<ThingDef>.GetNamedSilentFail(refs[i]) != null) present++;
                }
                if (present == 0)
                {
                    Log.Error("[Rimconemy.SurvivalProgression] PhaseProgress FAIL: no referenced ThingDefs resolved — DefDatabase may not be initialised yet");
                    failures++;
                }
                else
                {
                    Log.Message("[Rimconemy.SurvivalProgression] PhaseProgress Def SSOT: " + present + "/" + refs.Length + " referenced ThingDefs resolved");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[Rimconemy.SurvivalProgression] PhaseProgress FAIL: Def SSOT probe threw: " + ex.Message);
                failures++;
            }

            int passed = probes - failures;
            Log.Message(string.Format(
                "[Rimconemy.SurvivalProgression] PhaseProgress regression tests: {0} passed, {1} failed",
                passed, failures));


            ts.Check(failures == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return failures;
        }
    }
}
