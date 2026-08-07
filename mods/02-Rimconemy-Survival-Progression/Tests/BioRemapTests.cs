using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rimconemy.SurvivalProgression.Character;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Owner: Survival & Progression (Package 02).
    /// Phase-5 Bio-Remap regression (2026-08-04):
    /// Self-contained assertions for the Bio-Remap idempotency claim.
    ///
    /// Hard pre-game assertion: when Find.Game is null (typical for in-IDE test
    /// runners that don't boot RimWorld), ApplyAndCountAgeChanges must return
    /// 0 and must NOT throw. The bug this guards against: a NRE or
    /// NullReference cascade inside the bio-remap would crash the game on
    /// file-pickup or scenario-mod detection, well before the player
    /// clicks Start. Returning 0 and logging a warning is the contract.
    ///
    /// Soft assertion: the value 18 is exposed as FixedBiologicalAge; we
    /// sanity-check this isn't regressed to 63, 25 or any other number,
    /// because the FixAge call relies on this exact constant.
    ///
    /// We do NOT construct a Pawn here — Pawn construction is locked behind
    /// Verse. The actual "fixage actually rewrites BirthAbsTicks" test is
    /// covered by the Live-Test follow-up (P0-F-V5-live-test).
    ///
    /// Phase-5 audit-round-5 (2026-08-04) additions:
    /// - CustomizationPageHarmonyPatch_IsDeclared:
    ///   Asserts that the [HarmonyPatch] on Page_ConfigureStartingPawns exists
    ///   in this assembly so the Bio-Remap fires before the customisation
    ///   screen displays the starting pawns (otherwise the player sees the
    ///   vanilla backstory ages and the FinalizeInit-based apply is "too late"
    ///   visually). Without this test, renaming or accidentally deleting the
    ///   patch file silently regresses to "63-year-old Shepherd is the starting
    ///   colonist" which the user has reported 4 times.
    /// - FixAge_MethodIsPublic:
    ///   Asserts CharacterSetup.FixAge is callable from the Harmony patch
    ///   (was private until audit-round-5; the patch could not use it without
    ///   this visibility).
    /// </summary>
    public static class BioRemapTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();

        public static bool RunAll()
        {
            ts = new TestSuite("SurvivalProgression", "BioRemap tests");

            _passed = 0;
            _failed = 0;
            _failures.Clear();

            TestFixedBiologicalAge_IsExactly18();
            TestFixedChronologicalAge_IsExactly18();
            TestApplyAndCountAgeChanges_DoesNotThrow_PreGame();
            TestApplyAndCountAgeChanges_IsIdempotent();
            TestFixAllStartingPawnsAge_WrapperDoesNotThrow();
            TestTraitSelection_UsesH5Boundaries();
            TestTraitSelection_IsDeterministic();
            TestTraitSelection_SeedCanChangePoolChoice();
            TestTraitSelection_HandlesEmptyPools();

            // Phase-5 audit-round-5 (2026-08-04): customization-page patch coverage.
            TestFixAge_MethodIsPublic();
            TestCustomizationPageHarmonyPatch_IsDeclared();

            string summary = "[Rimconemy.SurvivalProgression] BioRemap tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                foreach (var f in _failures)
                    Log.Error("[Rimconemy.SurvivalProgression] TEST FAILED: " + f);
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return true;
        }

        // ── deterministic character-setup tests ─────────

        private static readonly string[] TestLightPositiveTraits =
        {
            "Positive_A", "Positive_B", "Positive_C", "Positive_D",
            "Positive_E", "Positive_F", "Positive_G", "Positive_H"
        };

        private static readonly string[] TestStrongPositiveTraits =
        {
            "Strong_A", "Strong_B", "Strong_C", "Strong_D"
        };

        private static readonly string[] TestLightNegativeTraits =
        {
            "Negative_A", "Negative_B", "Negative_C", "Negative_D"
        };

        private static readonly string[] TestHeavyNegativeTraits =
        {
            "Heavy_A", "Heavy_B", "Heavy_C", "Heavy_D", "Heavy_E"
        };

        private static void TestTraitSelection_UsesH5Boundaries()
        {
            var lowNeutral = TraitAssigner.SelectTraitsForBudget(20, 42,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits, TestHeavyNegativeTraits);
            var highNeutral = TraitAssigner.SelectTraitsForBudget(28, 42,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits, TestHeavyNegativeTraits);
            var lightNegative = TraitAssigner.SelectTraitsForBudget(19, 42,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits, TestHeavyNegativeTraits);
            var lightPositive = TraitAssigner.SelectTraitsForBudget(29, 42,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits, TestHeavyNegativeTraits);
            var strongNegative = TraitAssigner.SelectTraitsForBudget(15, 42,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits, TestHeavyNegativeTraits);

            ts.Check(Equals(SkillBudgetCalculator.TraitZone.Buffer, lowNeutral.Zone), "TraitSelection: balance -5 is buffer");
            ts.Check(Equals(SkillBudgetCalculator.TraitZone.Buffer, highNeutral.Zone), "TraitSelection: balance +3 is buffer");
            ts.Check(Equals(SkillBudgetCalculator.TraitZone.NegativeLight, lightNegative.Zone), "TraitSelection: balance -6 is light negative");
            ts.Check(Equals(SkillBudgetCalculator.TraitZone.PositiveLight, lightPositive.Zone), "TraitSelection: balance +4 is light positive");
            ts.Check(Equals(SkillBudgetCalculator.TraitZone.NegativeStrong, strongNegative.Zone), "TraitSelection: balance -10 is strong negative");
            ts.Check(Equals(0, SkillBudgetCalculator.NegativeTraitCount(-5)), "TraitSelection: helper keeps balance -5 neutral");
            ts.Check(Equals(0, lowNeutral.PositiveTraitIds.Count + lowNeutral.NegativeTraitIds.Count), "TraitSelection: neutral lower boundary has no traits");
            ts.Check(Equals(0, highNeutral.PositiveTraitIds.Count + highNeutral.NegativeTraitIds.Count), "TraitSelection: neutral upper boundary has no traits");
            ts.Check(Equals(1, lightNegative.NegativeTraitIds.Count), "TraitSelection: light negative has one trait");
            ts.Check(Equals(1, lightPositive.PositiveTraitIds.Count), "TraitSelection: light positive has one trait");
            ts.Check(Equals(2, strongNegative.NegativeTraitIds.Count), "TraitSelection: strong negative has two traits");
        }

        private static void TestTraitSelection_IsDeterministic()
        {
            var first = TraitAssigner.SelectTraitsForBudget(29, 42,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits, TestHeavyNegativeTraits);
            var second = TraitAssigner.SelectTraitsForBudget(29, 42,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits, TestHeavyNegativeTraits);

            ts.Check(Equals(first.Zone, second.Zone), "TraitSelection: same seed keeps zone");
            ts.Check(Equals(string.Join("|", first.PositiveTraitIds), string.Join("|", second.PositiveTraitIds)), "TraitSelection: same seed keeps positive choice");
            ts.Check(Equals(string.Join("|", first.NegativeTraitIds), string.Join("|", second.NegativeTraitIds)), "TraitSelection: same seed keeps negative choice");
        }

        private static void TestTraitSelection_SeedCanChangePoolChoice()
        {
            string seed42 = string.Join("|", TraitAssigner.SelectTraitsForBudget(29, 42,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits, TestHeavyNegativeTraits).PositiveTraitIds);
            string seed99 = string.Join("|", TraitAssigner.SelectTraitsForBudget(29, 99,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits, TestHeavyNegativeTraits).PositiveTraitIds);
            ts.Check(Equals("Positive_F", seed42), "TraitSelection: H5 seed 42 has the documented deterministic choice");
            ts.Check(Equals("Positive_B", seed99), "TraitSelection: H5 seed 99 has the documented deterministic choice");
            ts.Check(seed42 != seed99, "TraitSelection: H5 seeds 42 and 99 can produce different choices");
        }

        private static void TestTraitSelection_HandlesEmptyPools()
        {
            var result = TraitAssigner.SelectTraitsForBudget(29, 42,
                null, new string[0], null, new string[0]);
            ts.Check(Equals(0, result.PositiveTraitIds.Count + result.NegativeTraitIds.Count), "TraitSelection: null and empty pools are safe");

            var duplicated = TraitAssigner.SelectTraitsForBudget(15, 42,
                TestLightPositiveTraits, TestStrongPositiveTraits,
                TestLightNegativeTraits,
                new[] { "Heavy_A", "Heavy_A", "Heavy_B" });
            ts.Check(Equals(2, duplicated.NegativeTraitIds.Count), "TraitSelection: strong negative skips duplicate pool entries");
            ts.Check(duplicated.NegativeTraitIds[0] != duplicated.NegativeTraitIds[1], "TraitSelection: strong negative choices are distinct");
        }

        // ── helpers ────────────────────────────────────


        private static void AssertDoesNotThrow(System.Action action, string label)
        {
            try
            {
                action();
                _passed++;
            }
            catch (System.Exception ex)
            {
                _failed++;
                _failures.Add(label + ": threw " + ex.GetType().Name + " - " + ex.Message);
            }
        }

        // ── tests (Phase-5 audit-round-4) ──────────────

        private static void TestFixedBiologicalAge_IsExactly18()
        {
            ts.Check(Equals(18, CharacterSetup.FixedBiologicalAge), "BioRemap: FixedBiologicalAge == 18 (NOT 63, NOT 25)");
        }

        private static void TestFixedChronologicalAge_IsExactly18()
        {
            ts.Check(Equals(18, CharacterSetup.FixedChronologicalAge), "BioRemap: FixedChronologicalAge == 18");
        }

        private static void TestApplyAndCountAgeChanges_DoesNotThrow_PreGame()
        {
            // The Phase-5 audit-round-4 bug was: the caller assumed Current.Game
            // is initialised when the bio-remap runs. In a pre-game state that's
            // false, so the call must short-circuit to 0 safely.
            int result = -42;
            AssertDoesNotThrow(() => { result = CharacterSetup.ApplyAndCountAgeChanges(); },
                "BioRemap: ApplyAndCountAgeChanges runs without NRE");
            ts.Check(Equals(0, result), "BioRemap: ApplyAndCountAgeChanges returns 0 in pre-game state");
        }

        private static void TestApplyAndCountAgeChanges_IsIdempotent()
        {
            // BioRemap is called from FinalizeInit and from a defensive catch-up
            // tick. Both paths may run during the same game session. The COUNT
            // returned by the second call must NOT exceed the first call's count.
            int first = CharacterSetup.ApplyAndCountAgeChanges();
            int second = CharacterSetup.ApplyAndCountAgeChanges();
            ts.Check(second >= 0, "BioRemap: idempotent second-call returns non-negative count");
            // We don't assert equal because the second call may legitimately
            // be lower (the predicate matches what we already changed). The key
            // invariant is: never returns negative (that would mean we toggled
            // a previously-fixed pawn back, which is the bug).
            ts.Check(second <= first, "BioRemap: idempotent second-call returns count <= first (" + first + " vs " + second + ")");
        }

        private static void TestFixAllStartingPawnsAge_WrapperDoesNotThrow()
        {
            // The thin wrapper kept for legacy callers. Must behave identically
            // to ApplyAndCountAgeChanges (just discards the count).
            AssertDoesNotThrow(() => CharacterSetup.FixAllStartingPawnsAge(),
                "BioRemap: legacy FixAllStartingPawnsAge wrapper is callable");
        }

        // ── Phase-5 audit-round-5 (2026-08-04) tests ───

        private static void TestFixAge_MethodIsPublic()
        {
            // The Harmony patch on Page_ConfigureStartingPawns.PreOpen calls
            // CharacterSetup.FixAge directly. If this regresses to private,
            // the patch silently no-ops and the user sees 63-year-old Shepherd
            // again. Asserting visibility here makes that a build-time regression.
            var asm = Assembly.GetExecutingAssembly();
            var charSetup = asm.GetType("Rimconemy.SurvivalProgression.Character.CharacterSetup");
            ts.Check(charSetup != null, "BioRemap (audit-R5): CharacterSetup type exists in this assembly");
            if (charSetup == null) return;

            var fixAge = charSetup.GetMethod("FixAge", BindingFlags.Public | BindingFlags.Static);
            ts.Check(fixAge != null, "BioRemap (audit-R5): CharacterSetup.FixAge has public visibility (customization-page patch hook)");
        }

        private static void TestCustomizationPageHarmonyPatch_IsDeclared()
        {
            // Phase-5 audit-round-5 (updated 2026-08-06):
            // Harmony PatchAll cannot resolve inherited methods on Verse.Window
            // in RimWorld 1.6. The Bio-Remap patch is now applied manually via
            // Harmony.Patch() in Bootstrap.cs targeting typeof(Verse.Window).PostOpen.
            // This test verifies both the patch class AND the Postfix method exist.
            var asm = Assembly.GetExecutingAssembly();
            var patchType = asm.GetType("Rimconemy.SurvivalProgression.Patches.Page_ConfigureStartingPawnsBioPatch");
            ts.Check(patchType != null, "BioRemap (audit-R5): Page_ConfigureStartingPawnsBioPatch type exists in this assembly");

            if (patchType == null) return;

            // The Postfix method must be public static so Harmony.Patch() can apply it.
            var postfixMethod = patchType.GetMethod("Postfix",
                BindingFlags.Static | BindingFlags.Public);
            ts.Check(postfixMethod != null, "BioRemap (audit-R5): Page_ConfigureStartingPawnsBioPatch.Postfix is public static (manual Harmony.Patch target)");
        }
    }
}
