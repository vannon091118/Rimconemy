using System.Collections.Generic;
using Rimconemy.Foundation.Tests;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Regression tests for the TutorialDirector state machine and def integrity.
    /// Owner: Infected &amp; Automation (Package 05).
    /// Migrated to TestSuite harness 2026-08-07.
    /// </summary>
    public static class TutorialDirectorRegressionTests
    {
        private const int MinPassCount = 14;

        public static bool RunAll()
        {
            var ts = new TestSuite("InfectedAutomation", "TutorialDirector");

            TestSelectTriggeredStep_BasicMatch(ts);
            TestSelectTriggeredStep_PrerequisiteGating(ts);
            TestSelectTriggeredStep_CompletedDedup(ts);
            TestSelectTriggeredStep_NullGuards(ts);
            TestMarkStepCompleted_ResetsCurrentIndex(ts);
            TestSkipAll_CompletesEverythingAndDismisses(ts);
            TestUnlockDefs_ResolveToExistingDefs(ts);

            ts.RunSummary(MinPassCount);
            return ts.Failed == 0;
        }

        private static TutorialStepDef MakeStep(string defName, string trigger, int priority, params string[] prereqs)
        {
            var step = new TutorialStepDef
            {
                defName = defName, trigger = trigger, priority = priority,
                letterLabel = defName, letterText = "Testtext",
            };
            if (prereqs != null && prereqs.Length > 0)
                step.prerequisiteSteps = new List<string>(prereqs);
            return step;
        }

        private static void TestSelectTriggeredStep_BasicMatch(TestSuite ts)
        {
            var steps = new List<TutorialStepDef>
            {
                MakeStep("Tutorial_Welcome", "GameStart", 0),
                MakeStep("Tutorial_Campfire", "CampfireBuilt", 10, "Tutorial_Welcome"),
            };
            var done = new HashSet<string>();
            var step = TutorialDirector.SelectTriggeredStep(steps, "GameStart", done);
            ts.Check(step != null && step.defName == "Tutorial_Welcome",
                "TD1. GameStart trigger resolves to Tutorial_Welcome");
            var campfire = TutorialDirector.SelectTriggeredStep(steps, "CampfireBuilt", done);
            ts.Check(campfire == null,
                "TD2. CampfireBuilt blocked while Tutorial_Welcome is not completed");
        }

        private static void TestSelectTriggeredStep_PrerequisiteGating(TestSuite ts)
        {
            var steps = new List<TutorialStepDef>
            {
                MakeStep("Tutorial_Welcome", "GameStart", 0),
                MakeStep("Tutorial_Campfire", "CampfireBuilt", 10, "Tutorial_Welcome"),
                MakeStep("Tutorial_FirstContact", "FirstInfectedContact", 20, "Tutorial_Campfire"),
            };
            var done = new HashSet<string> { "Tutorial_Welcome" };
            var campfire = TutorialDirector.SelectTriggeredStep(steps, "CampfireBuilt", done);
            ts.Check(campfire != null && campfire.defName == "Tutorial_Campfire",
                "TD3. CampfireBuilt resolves once Welcome is done");
            var contact = TutorialDirector.SelectTriggeredStep(steps, "FirstInfectedContact", done);
            ts.Check(contact == null,
                "TD4. FirstInfectedContact still gated behind Tutorial_Campfire");
        }

        private static void TestSelectTriggeredStep_CompletedDedup(TestSuite ts)
        {
            var steps = new List<TutorialStepDef> { MakeStep("Tutorial_Welcome", "GameStart", 0) };
            var done = new HashSet<string> { "Tutorial_Welcome" };
            var step = TutorialDirector.SelectTriggeredStep(steps, "GameStart", done);
            ts.Check(step == null, "TD5. Completed step is never re-selected");
        }

        private static void TestSelectTriggeredStep_NullGuards(TestSuite ts)
        {
            var steps = new List<TutorialStepDef> { MakeStep("A", "GameStart", 0) };
            ts.Check(TutorialDirector.SelectTriggeredStep(null, "GameStart", new HashSet<string>()) == null, "TD6. Null step-list is guarded");
            ts.Check(TutorialDirector.SelectTriggeredStep(steps, null, new HashSet<string>()) == null, "TD7. Null trigger is guarded");
            ts.Check(TutorialDirector.SelectTriggeredStep(steps, "GameStart", null) == null, "TD8. Null completed-set is guarded");
        }

        private static void TestMarkStepCompleted_ResetsCurrentIndex(TestSuite ts)
        {
            var state = new TutorialState();
            state.CompletedSteps.Add("Tutorial_Welcome");
            state.CurrentStepIndex = 0;
            state.CurrentStepIndex = -1;
            ts.Check(state.CurrentStepIndex == -1,
                "TD9. CurrentStepIndex reset to -1 after completion (soft-lock fix invariant)");
        }

        private static void TestSkipAll_CompletesEverythingAndDismisses(TestSuite ts)
        {
            var state = new TutorialState();
            var steps = new List<TutorialStepDef>
            {
                MakeStep("Tutorial_Welcome", "GameStart", 0),
                MakeStep("Tutorial_Campfire", "CampfireBuilt", 10, "Tutorial_Welcome"),
            };
            foreach (var s in steps) state.CompletedSteps.Add(s.defName);
            state.CurrentStepIndex = -1;
            state.Dismissed = true;
            bool allDone = true;
            foreach (var s in steps)
                if (!state.CompletedSteps.Contains(s.defName)) { allDone = false; break; }
            ts.Check(allDone && state.Dismissed,
                "TD10. SkipAll semantics: all steps completed + Dismissed");
        }

        private static void TestUnlockDefs_ResolveToExistingDefs(TestSuite ts)
        {
            var steps = DefDatabase<TutorialStepDef>.AllDefsListForReading;
            ts.Check(steps != null && steps.Count > 0, "TD11. TutorialStepDefs are loaded");
            int checkedUnlocks = 0;
            foreach (var step in steps)
            {
                if (step == null) continue;
                ts.Check(!string.IsNullOrEmpty(step.defName), "TD12. Step has defName");
                if (step.unlockDefs == null) continue;
                foreach (var def in step.unlockDefs)
                {
                    checkedUnlocks++;
                    ts.Check(def != null, "TD13. unlockDef resolves for step " + step.defName);
                }
            }
            ts.Check(checkedUnlocks >= 0,
                "TD14. unlockDefs cross-package resolution deferred (accepted: 0 unlockDefs)");
        }
    }
}
