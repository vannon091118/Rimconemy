using System;
using System.Collections.Generic;
using System.Reflection;
using Rimconemy.InfectedAutomation.Story;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    ///
    /// Audit-Bündel C / F-13 (2026-08-04): Regression tests for the new
    /// FIFO queue behind <see cref="StoryState.GameOverPendingQueue"/>.
    /// The legacy single-pending pair <c>(bool, string)</c> overwrote on
    /// every consecutive tick with 0 colonists, dropping intermediate
    /// events and erasing wipe chronology. The queue now keeps every
    /// edge-trigger entry; consumers drain FIFO. The tests below pin
    /// the new contract.
    /// </summary>
    public static class GameOverPendingQueueRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        /// <summary>Matches <see cref="StoryState.ClassId"/>;
        /// used to clean up test instances between runs.</summary>
        private const string TestClassId = "rimconemy.infectedautomation.storyState";

        public static bool RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "GameOverPendingQueue regression tests");

            _passed = 0;
            _failed = 0;

            // Wipe any leftover registration from a previous hot-reload or
            // double-static-constructor edge case before the real tests start.
            Rimconemy.Foundation.Save.MigrationRegistry.Unregister(TestClassId);

            TestEnqueueWhenNoColonists();
            TestEdgeTriggerFirstWipeTickIsImmutable();
            TestMultipleWipeTickSignalsAccumulate();
            TestFifoDrainOrder();
            TestLegacyPreF13SinglePendingSurvivesReload();
            // LegacyPreF13 invokes RebuildAfterLoad → MigrateIfNeeded →
            // MigrationRegistry.Register. Unregister before the next test
            // so ModernPreF13 doesn't see a stale instance.
            Rimconemy.Foundation.Save.MigrationRegistry.Unregister(TestClassId);
            TestModernPreF13QueueSurvivesRoundTrip();
            // ModernPreF13 also invokes RebuildAfterLoad. Unregister again.
            Rimconemy.Foundation.Save.MigrationRegistry.Unregister(TestClassId);
            TestPeekDoesNotDrain();

            string summary = "[Rimconemy.InfectedAutomation] GameOverPendingQueue regression tests: "
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

        // ── tests ──────────────────────────────────────

        private static void TestEnqueueWhenNoColonists()
        {
            var state = new StoryState();
            long t = 10_000L;

            state.MarkGameOverPending("wipe-1", colonistsPresent: false, atTick: t);
            AssertEqual(1, state.GameOverPendingQueue.Count,
                "Queue: enqueue adds exactly one entry when colonists absent");
            AssertTrue(state.GameOverPending, "Queue: legacy mirror flag is true after enqueue");
            AssertEqual("wipe-1", state.GameOverReasonPending,
                "Queue: legacy mirror reason matches oldest entry");
        }

        private static void TestEdgeTriggerFirstWipeTickIsImmutable()
        {
            var state = new StoryState();
            long t1 = 10_000L;
            long t2 = 20_000L;

            state.MarkGameOverPending("wipe-1", colonistsPresent: false, atTick: t1);
            state.MarkGameOverPending("wipe-2", colonistsPresent: false, atTick: t2);
            AssertEqual(t1, state.FirstWipeTick,
                "Queue: FirstWipeTick remains the first wipe tick and doesn't get overwritten");
        }

        private static void TestMultipleWipeTickSignalsAccumulate()
        {
            var state = new StoryState();
            state.MarkGameOverPending("wipe-1", colonistsPresent: false, atTick: 10_000L);
            state.MarkGameOverPending("wipe-2", colonistsPresent: false, atTick: 20_000L);
            state.MarkGameOverPending("wipe-3", colonistsPresent: false, atTick: 30_000L);
            AssertEqual(3, state.GameOverPendingQueue.Count,
                "Queue: 3 consecutive wipe ticks yield 3 distinct entries");
        }

        private static void TestFifoDrainOrder()
        {
            var state = new StoryState();
            state.MarkGameOverPending("wipe-1", colonistsPresent: false, atTick: 10_000L);
            state.MarkGameOverPending("wipe-2", colonistsPresent: false, atTick: 20_000L);
            state.MarkGameOverPending("wipe-3", colonistsPresent: false, atTick: 30_000L);

            string drained1, drained2, drained3;
            AssertTrue(state.ConsumeGameOverPending(out drained1) && drained1 == "wipe-1",
                "Queue: first Consume yields oldest entry (wipe-1)");
            AssertTrue(state.ConsumeGameOverPending(out drained2) && drained2 == "wipe-2",
                "Queue: second Consume yields the next-oldest entry (wipe-2)");
            AssertTrue(state.ConsumeGameOverPending(out drained3) && drained3 == "wipe-3",
                "Queue: third Consume yields the newest entry (wipe-3)");
            AssertEqual(0, state.GameOverPendingQueue.Count,
                "Queue: 3 Consumes drain the queue");

            string drainedEmpty;
            AssertFalse(state.ConsumeGameOverPending(out drainedEmpty),
                "Queue: Consume on empty queue returns false (and nulls out parameter)");
        }

        private static void TestLegacyPreF13SinglePendingSurvivesReload()
        {
            // Simulate legacy save: old schema persisted (bool, string) with no
            // queue lists. RebuildAfterLoad reconstructs one queue entry from
            // the mirror fields so the next ConsumeGameOverPending surfaces what
            // the legacy reader would have.
            var state = new StoryState();
            state.GameOverPending = true;
            state.GameOverReasonPending = "legacy-wipe";
            state.FirstWipeTick = 12_500L;

            InvokePrivate(state, "RebuildAfterLoad");

            AssertEqual(1, state.GameOverPendingQueue.Count,
                "Legacy: RebuildAfterLoad synthesises one queue entry from mirror fields");
            AssertEqual("legacy-wipe", state.GameOverPendingQueue[0].Reason,
                "Legacy: synthesised entry carries legacy reason");
            AssertEqual(12_500L, state.GameOverPendingQueue[0].Tick,
                "Legacy: synthesised entry carries FirstWipeTick as tick anchor");
        }

        private static void TestModernPreF13QueueSurvivesRoundTrip()
        {
            // Simulate the modern save: parallel lists contain 3 entries.
            var state = new StoryState();
            SetPrivateField(state, "_queueReasons", new List<string> { "r1", "r2", "r3" });
            SetPrivateField(state, "_queueTicks", new List<long> { 100L, 200L, 300L });
            SetPrivateField(state, "_queueTriggerIds", new List<string> { "wipe", "fire", "shuttle" });

            InvokePrivate(state, "RebuildAfterLoad");

            AssertEqual(3, state.GameOverPendingQueue.Count,
                "Modern: RebuildAfterLoad reconstructs every queue entry from parallel lists");
            AssertEqual("r1", state.GameOverPendingQueue[0].Reason, "Modern: queue[0].Reason");
            AssertEqual(100L, state.GameOverPendingQueue[0].Tick, "Modern: queue[0].Tick");
            AssertEqual("wipe", state.GameOverPendingQueue[0].TriggerId, "Modern: queue[0].TriggerId");
            AssertEqual("shuttle", state.GameOverPendingQueue[2].TriggerId, "Modern: queue[2].TriggerId");
            AssertTrue(state.GameOverPending, "Modern: legacy mirror flipped true after RebuildAfterLoad");
            AssertEqual("r1", state.GameOverReasonPending, "Modern: legacy mirror matches oldest queue entry");
        }

        private static void TestPeekDoesNotDrain()
        {
            var state = new StoryState();
            state.MarkGameOverPending("peek-1", colonistsPresent: false, atTick: 1000L);
            state.MarkGameOverPending("peek-2", colonistsPresent: false, atTick: 2000L);

            string reason; long tick; string triggerId;
            AssertTrue(state.PeekGameOverPending(out reason, out tick, out triggerId),
                "Peek: returns true when queue has entries");
            AssertEqual("peek-1", reason, "Peek: returns oldest reason");
            AssertEqual(1000L, tick, "Peek: returns oldest tick");
            AssertEqual(2, state.GameOverPendingQueue.Count,
                "Peek: queue length unchanged after Peek");
        }

        // ── helpers ──────────────────────────────────────

        private static void SetPrivateField(object instance, string name, object value)
        {
            var field = instance.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException("Missing private field: " + name);
            field.SetValue(instance, value);
        }

        private static void InvokePrivate(object instance, string name)
        {
            var method = instance.GetType().GetMethod(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("Missing private method: " + name);
            method.Invoke(instance, null);
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[Rimconemy.InfectedAutomation] " + label); }
        }

        private static void AssertFalse(bool condition, string label)
        {
            AssertTrue(!condition, label);
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[Rimconemy.InfectedAutomation] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
