using System;
using System.Collections.Generic;
using System.Reflection;
using Rimconemy.InfectedAutomation.Story;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>Regression tests for deterministic StoryState idempotency pruning.</summary>
    public static class StoryStateRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        /// <summary>Matches <see cref="StoryState.ClassId"/>;
        /// used to clean up test instances between runs.</summary>
        private const string TestClassId = "rimconemy.infectedautomation.storyState";

        public static bool RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "StoryState regression tests");

            _passed = 0;
            _failed = 0;

            // Wipe any leftover registration from a previous hot-reload or
            // double-static-constructor edge case before the real tests start.
            Rimconemy.Foundation.Save.MigrationRegistry.Unregister(TestClassId);

            TestFreshKeysSurviveAgePrune();
            TestOldKeysPruneBeforeFreshKeys();
            TestCountCapAppliesAfterAgePrune();
            TestUnknownAgeKeysUseCountCapOnly();
            TestPostLoadTicksSurviveLateAgePrune();
            TestUnorderedLegacySetUsesDeterministicFallback();

            // TestPostLoadTicksSurviveLateAgePrune invokes RebuildAfterLoad
            // which calls MigrateIfNeeded → MigrationRegistry.Register.
            // Unregister so the SchemaBump tests and the real GameComponent
            // can register cleanly later.
            Rimconemy.Foundation.Save.MigrationRegistry.Unregister(TestClassId);

            string summary = "[Rimconemy.InfectedAutomation] StoryState regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return true;
        }

        private static void TestFreshKeysSurviveAgePrune()
        {
            var state = new StoryState();
            const long currentTick = 2_000_000L;
            for (int i = 0; i < 600; i++)
                state.MarkExecuted("fresh-" + i, currentTick);

            state.PruneOldKeys(currentTick);
            AssertEqual(600, state.IdempotencyKeys.Count,
                "StoryState: 600 fresh keys are not age-pruned or prematurely capped");
            AssertTrue(state.HasExecuted("fresh-0"), "StoryState: oldest fresh key survives");
            AssertTrue(state.HasExecuted("fresh-599"), "StoryState: newest fresh key survives");
        }

        private static void TestOldKeysPruneBeforeFreshKeys()
        {
            var state = new StoryState();
            const long currentTick = 2_000_000L;
            for (int i = 0; i < 3; i++)
                state.MarkExecuted("old-" + i, 1L);
            for (int i = 0; i < 3; i++)
                state.MarkExecuted("fresh-" + i, currentTick);

            state.PruneOldKeys(currentTick);
            AssertEqual(3, state.IdempotencyKeys.Count,
                "StoryState: expired keys are removed without deleting fresh keys");
            AssertTrue(!state.HasExecuted("old-0"), "StoryState: expired key removed");
            AssertTrue(state.HasExecuted("fresh-0"), "StoryState: fresh key retained");
        }

        private static void TestCountCapAppliesAfterAgePrune()
        {
            var state = new StoryState();
            const long currentTick = 2_000_000L;
            for (int i = 0; i < 600; i++)
                state.MarkExecuted("old-" + i, 1L);
            for (int i = 0; i < 600; i++)
                state.MarkExecuted("fresh-" + i, currentTick);

            state.PruneOldKeys(currentTick);
            AssertEqual(600, state.IdempotencyKeys.Count,
                "StoryState: age prune removes old keys before count cap");
            AssertTrue(state.HasExecuted("fresh-0"), "StoryState: fresh keys survive age prune");
            AssertTrue(!state.HasExecuted("old-599"), "StoryState: all old keys removed");
        }

        private static void TestUnknownAgeKeysUseCountCapOnly()
        {
            var state = new StoryState();
            for (int i = 0; i < 1500; i++)
                state.MarkExecuted("legacy-" + i);

            state.PruneOldKeys(2_000_000L);
            AssertEqual(500, state.IdempotencyKeys.Count,
                "StoryState: unknown-age legacy keys use deterministic count cap");
            AssertTrue(!state.HasExecuted("legacy-0"),
                "StoryState: oldest unknown-age key is removed by count cap");
            AssertTrue(state.HasExecuted("legacy-1499"),
                "StoryState: newest unknown-age key is retained");
        }

        private static void TestPostLoadTicksSurviveLateAgePrune()
        {
            const long currentTick = 2_000_000L;
            var state = new StoryState();

            // Simulate the serialized parallel lists that Scribe supplies
            // before StoryState.RebuildAfterLoad(). This is the exact boundary
            // that previously replaced every restored tick with 0.
            var keyList = new List<string>();
            var tickList = new List<long>();
            for (int i = 0; i < 50; i++)
            {
                keyList.Add("loaded-" + i);
                tickList.Add(currentTick - 10);
            }

            SetPrivateField(state, "_idempotencyList", keyList);
            SetPrivateField(state, "_idempotencyTicks", tickList);
            state.LastPruneTick = 0L;
            SetPrivateField(state, "_idempotencyInsertionOrder", null);
            state.IdempotencyKeys = new HashSet<string>();
            InvokePrivate(state, "RebuildAfterLoad");

            state.PruneOldKeys(currentTick);
            AssertEqual(50, state.IdempotencyKeys.Count,
                "StoryState: saved insertion ticks preserve late-age keys");
            AssertTrue(state.HasExecuted("loaded-0"),
                "StoryState: oldest loaded key survives late age prune");
            AssertTrue(state.HasExecuted("loaded-49"),
                "StoryState: newest loaded key survives late age prune");
        }

        private static void SetPrivateField(object instance, string name, object value)
        {
            var field = instance.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException("Missing private field: " + name);
            field.SetValue(instance, value);
        }

        private static void InvokePrivate(object instance, string name)
        {
            var method = instance.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("Missing private method: " + name);
            method.Invoke(instance, null);
        }

        private static void TestUnorderedLegacySetUsesDeterministicFallback()
        {
            var state = new StoryState();
            for (int i = 0; i < 1500; i++)
                state.IdempotencyKeys.Add("unordered-" + i.ToString("D4"));

            state.PruneOldKeys(2_000_000L);
            AssertEqual(500, state.IdempotencyKeys.Count,
                "StoryState: tracker-less legacy set gets deterministic count cap");
            AssertTrue(!state.HasExecuted("unordered-0000"),
                "StoryState: deterministic fallback removes lowest ordinal keys");
            AssertTrue(state.HasExecuted("unordered-1499"),
                "StoryState: deterministic fallback retains highest ordinal keys");
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[StoryStateRegression] " + label); }
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[StoryStateRegression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
