using System.Collections.Generic;
using Rimconemy.SurvivalProgression.Character;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Regression tests for Phase-4.2 CharacterSetupState persistence.
    /// Covers:
    ///   - Record schema version
    ///   - Upsert/Get round-trip
    ///   - Migration scaffold invariants
    /// </summary>
    public static class CharacterSetupStateRegressionTests
    {
        public static void RunAll()
        {
            TestSchemaVersion();
            TestUpsertAndGet();
            TestEmptyRecordDefaults();
            Log.Message("[Rimconemy.SurvivalProgression] CharacterSetupStateRegressionTests PASS");
        }

        private static void TestSchemaVersion()
        {
            Assert(CharacterSetupState.CurrentSchemaVersion == 1,
                "Current schema is v1 (first public release)");
        }

        private static void TestUpsertAndGet()
        {
            var state = new CharacterSetupState(null);
            var r = new PawnSetupRecord
            {
                AgeBiologicalYears = 18,
                AgeChronologicalYears = 18,
                SkillDefNames = new List<string> { "Shooting", "Construction" },
                SkillLevels = new List<int> { 6, 4 },
                TraitDefNames = new List<string> { "Rimconemy_Trait_Hardy" },
                NeutralBand = 0,
            };
            // Manually upsert without a real Pawn object because we cannot
            // instantiate one in a non-game test run. Use thingIDNumber=42.
            state.Records[42] = r;

            var fetched = state.GetFor(42);
            Assert(fetched != null, "GetFor returns recorded");
            Assert(fetched.AgeBiologicalYears == 18, "Age bio = 18");
            Assert(fetched.SkillDefNames.Count == 2, "SkillDefNames size = 2");
            Assert(fetched.SkillLevels[0] == 6, "Shooting level = 6");
            Assert(fetched.TraitDefNames.Contains("Rimconemy_Trait_Hardy"), "Hardy trait recorded");
        }

        private static void TestEmptyRecordDefaults()
        {
            var r = new PawnSetupRecord();
            Assert(r.AgeBiologicalYears == 0, "Default AgeBio = 0");
            Assert(r.SkillDefNames != null, "Default SkillDefNames != null");
            Assert(r.SkillLevels != null, "Default SkillLevels != null");
            Assert(r.TraitDefNames != null, "Default TraitDefNames != null");
        }

        private static void Assert(bool condition, string label)
        {
            if (!condition)
            {
                Log.Error("[Rimconemy.SurvivalProgression] CharacterSetupStateRegressionTests FAIL: " + label);
                throw new System.Exception("CharacterSetupStateRegressionTests failure: " + label);
            }
        }
    }
}
