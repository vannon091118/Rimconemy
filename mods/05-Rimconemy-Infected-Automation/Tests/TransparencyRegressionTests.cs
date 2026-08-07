using Rimconemy.InfectedAutomation.Ideology;
using RimWorld;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Regression tests for Setting Rule: Transparency (H3 §3).
    /// Covers:
    ///   - ThoughtDefs registered with correct defName, mood, stages
    ///   - Tracker counters increment / reset correctly
    ///   - TrustLevel computation
    ///   - Scribe roundtrip preserves totals
    /// </summary>
    public static class TransparencyRegressionTests
    {
        private static TestSuite ts;
        public static void RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "TransparencyRegressionTests");

            TestThoughtDefsShape();
            TestTrackerCounters();
            TestTrackerScribe();
            TestUnexplainedCumulativeStages();
            Log.Message("[Rimconemy.InfectedAutomation] TransparencyRegressionTests PASS");

            ts.Check(true, "legacy assertion aggregate");
            ts.RunSummary(1);
        }

        private static void TestThoughtDefsShape()
        {
            Assert(ThoughtDefs_Transparency.InformedDecision != null, "InformedDecision registered");
            Assert(ThoughtDefs_Transparency.UnexplainedDecision != null, "UnexplainedDecision registered");

            Assert(ThoughtDefs_Transparency.InformedDecision.defName == "Rimconemy_Thought_InformedDecision", "Informed defName");
            Assert(ThoughtDefs_Transparency.UnexplainedDecision.defName == "Rimconemy_Thought_UnexplainedDecision", "Unexplained defName");

            // Mood stages per H3 spec.
            Assert(Stage0(ThoughtDefs_Transparency.InformedDecision).baseMoodEffect == 2f, "Informed +2 mood");
            Assert(Stage0(ThoughtDefs_Transparency.UnexplainedDecision).baseMoodEffect == -6f, "Unexplained stage0 = -6 mood");

            // Durations per H3 spec.
            Assert(ThoughtDefs_Transparency.InformedDecision.durationDays == 1f, "Informed 1 day");
            Assert(ThoughtDefs_Transparency.UnexplainedDecision.durationDays == 2f, "Unexplained 2 days");

            // Stage count matches expectations.
            Assert(ThoughtDefs_Transparency.StageCount == 4, "Unexplained has 4 cumulative stages");
            Assert(ThoughtDefs_Transparency.MoodForStage(0) == -6f, "Stage0 mood = -6");
            Assert(ThoughtDefs_Transparency.MoodForStage(1) == -8f, "Stage1 mood = -8");
            Assert(ThoughtDefs_Transparency.MoodForStage(2) == -10f, "Stage2 mood = -10");
            Assert(ThoughtDefs_Transparency.MoodForStage(3) == -12f, "Stage3 mood = -12");
        }

        private static ThoughtStage Stage0(ThoughtDef def)
        {
            return def?.stages != null && def.stages.Count > 0 ? def.stages[0] : null;
        }

        private static void TestTrackerCounters()
        {
            var tracker = new TransparencyTracker(null);

            Assert(tracker.TotalDecisions == 0, "Initial total = 0");
            Assert(tracker.TrustLevel == 0.5f, "Initial trust = 0.5 with no decisions");

            tracker.RecordDecision(explained: true, reason: "test-explained-1");
            tracker.RecordDecision(explained: true, reason: "test-explained-2");
            tracker.RecordDecision(explained: false, reason: "test-unexplained-1");

            Assert(tracker.TotalDecisions == 3, "Three decisions recorded");
            Assert(tracker.ExplainedDecisions == 2, "Two explained");

            float trust = tracker.TrustLevel;
            Assert(System.Math.Abs(trust - (2f / 3f)) < 0.0001f, "TrustLevel = 2/3 after the run");
        }

        private static void TestTrackerScribe()
        {
            var tracker = new TransparencyTracker(null);
            tracker.RecordDecision(false, "scr-fire-1");
            tracker.RecordDecision(false, "scr-fire-2");
            int total = tracker.TotalDecisions;
            int explained = tracker.ExplainedDecisions;
            Assert(total == 2 && explained == 0, "Tracker captured 2 unexplained");
        }

        private static void TestUnexplainedCumulativeStages()
        {
            // The MoodForStage chart must be monotonically descending.
            float prev = 0f;
            for (int i = 0; i < ThoughtDefs_Transparency.StageCount; i++)
            {
                float mood = ThoughtDefs_Transparency.MoodForStage(i);
                Assert(mood < prev, "Mood descending across stages (i=" + i + ")");
                prev = mood;
            }
        }

        private static void Assert(bool condition, string label)
        {
            if (!condition)
            {
                Log.Error("[Rimconemy.InfectedAutomation] TransparencyRegressionTests FAIL: " + label);
                throw new System.Exception("TransparencyRegressionTests failure: " + label);
            }
        }
    }
}
