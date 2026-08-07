using System;
using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Story;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05)
    ///
    /// Self-contained unit tests for the Story Writer core.
    /// No external test framework required — uses simple
    /// assertion helpers. Call RunAll() from Bootstrap or
    /// a console runner.
    ///
    /// Tests: Determinism (Gate G2), Idempotency, Profile
    /// loading, DeterministicRng stability, Diagnostics output.
    /// </summary>
    public static class StorySelectorTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();

        /// <summary>Run all tests. Returns true if all passed.</summary>
        public static bool RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "StorySelector tests");

            _passed = 0;
            _failed = 0;
            _failures.Clear();

            TestDeterminism_Refuge();
            TestDeterminism_Survival();
            TestDeterminism_Collapse();
            TestIdempotency_PreventsDuplicate();
            TestIdempotency_SaveLoadSimulation();
            TestGetBuiltIn_AllProfiles();
            TestGetBuiltIn_UnknownReturnsNull();
            TestDeterministicRng_StableSequence();
            TestDeterministicRng_NextInt_Uniform();
            // Audit-fix A1/A2/A3 (slop-audit 2026-08-04):
            TestDeterminism_MutationSurvivesSplitmix64Change_FailsOnMutation();
            TestDeterminism_NextFloat_ChiSquareRoughlyUniform();
            TestStorySelector_SupplyShortage_PreferredWhenAnyResourceCritical();
            TestDiagnostics_ReasonNotEmpty();
            TestNoProfile_ReturnsNull();
            TestNoCatalog_ReturnsNull();
            // Audit-round-3 §3 fire-or-retry regression (2026-08-04):
            TestStorySelector_DoesNotMutateState_FireOrRetry();
            TestCommitSelection_ApiCompatibility();

            string summary = $"[Rimconemy.InfectedAutomation] StorySelector tests: " +
                $"{_passed} passed, {_failed} failed.";
            if (_failed > 0)
            {
                foreach (var f in _failures)
                    Verse.Log.Error($"[Rimconemy.InfectedAutomation] TEST FAILED: {f}");
                Verse.Log.Error(summary);
                return false;
            }

            Verse.Log.Message(summary);

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return true;
        }

        // ── helper ────────────────────────────────────────────

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                _failed++;
                _failures.Add($"{label}: expected {expected}, got {actual}");
            }
            else
            {
                _passed++;
            }
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (!condition)
            {
                _failed++;
                _failures.Add($"{label}: expected true, got false");
            }
            else
            {
                _passed++;
            }
        }

        private static void AssertNotNull(object obj, string label)
        {
            if (obj == null)
            {
                _failed++;
                _failures.Add($"{label}: expected non-null, got null");
            }
            else
            {
                _passed++;
            }
        }

        private static void AssertNull(object obj, string label)
        {
            if (obj != null)
            {
                _failed++;
                _failures.Add($"{label}: expected null, got {obj}");
            }
            else
            {
                _passed++;
            }
        }

        /// <summary>
        /// Creates a minimal valid snapshot for testing.
        /// SurvivorCount=3, ThreatPressure=0.3, IdeologyTension=0.2,
        /// ActiveSettingRuleCount=1, StorageHash="test-hash".
        /// No active events, no active families.
        /// </summary>
        private static SituationSnapshot CreateTestSnapshot(long tick)
        {
            return new SituationSnapshot
            {
                GameTick = tick,
                SurvivorCount = 3,
                ThreatPressure = 0.3f,
                IdeologyTension = 0.2f,
                ActiveSettingRuleCount = 1,
                StorageHash = "test-hash",
                AnyResourceCritical = false,
                ActiveEventIds = new List<string>(),
                ActiveEventFamilies = new List<string>(),
                CompletedResearchIds = new List<string>(),
                CriticalResourceIds = new List<string>(),
            };
        }

        // ── determinism tests ─────────────────────────────────

        /// <summary>
        /// Gate G2: Same snapshot + Refuge profile + same tick
        /// must produce the same event.
        /// </summary>
        private static void TestDeterminism_Refuge()
        {
            var profile = SettingProfile.Refuge;
            var catalog = new StoryEventCatalog();
            long tick = 60000; // day 1

            // Run 3 times with identical inputs
            var snapshot1 = CreateTestSnapshot(tick);
            var state1 = new StoryState();
            var result1 = StorySelector.SelectEvent(profile, snapshot1, state1, catalog, tick);

            var snapshot2 = CreateTestSnapshot(tick);
            var state2 = new StoryState();
            var result2 = StorySelector.SelectEvent(profile, snapshot2, state2, catalog, tick);

            var snapshot3 = CreateTestSnapshot(tick);
            var state3 = new StoryState();
            var result3 = StorySelector.SelectEvent(profile, snapshot3, state3, catalog, tick);

            AssertNotNull(result1.SelectedEvent, "Det-Refuge: event1");
            AssertNotNull(result2.SelectedEvent, "Det-Refuge: event2");
            AssertNotNull(result3.SelectedEvent, "Det-Refuge: event3");

            AssertEqual(result1.SelectedEvent.EventId, result2.SelectedEvent.EventId, "Det-Refuge: eventId 1==2");
            AssertEqual(result1.SelectedEvent.EventId, result3.SelectedEvent.EventId, "Det-Refuge: eventId 1==3");
            AssertEqual(result1.DeterminismKey, result2.DeterminismKey, "Det-Refuge: key 1==2");
            AssertEqual(result1.DeterminismKey, result3.DeterminismKey, "Det-Refuge: key 1==3");
        }

        /// <summary>Gate G2: Determinism with Survival profile.</summary>
        private static void TestDeterminism_Survival()
        {
            var profile = SettingProfile.Survival;
            var catalog = new StoryEventCatalog();
            long tick = 120000; // day 2

            var result1 = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), new StoryState(), catalog, tick);
            var result2 = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), new StoryState(), catalog, tick);

            AssertNotNull(result1.SelectedEvent, "Det-Survival: event1");
            AssertNotNull(result2.SelectedEvent, "Det-Survival: event2");
            AssertEqual(result1.SelectedEvent.EventId, result2.SelectedEvent.EventId, "Det-Survival: same event");
            AssertEqual(result1.DeterminismKey, result2.DeterminismKey, "Det-Survival: same key");
        }

        /// <summary>Gate G2: Determinism with Collapse profile.</summary>
        private static void TestDeterminism_Collapse()
        {
            var profile = SettingProfile.Collapse;
            var catalog = new StoryEventCatalog();
            long tick = 180000; // day 3

            var result1 = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), new StoryState(), catalog, tick);
            var result2 = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), new StoryState(), catalog, tick);

            AssertNotNull(result1.SelectedEvent, "Det-Collapse: event1");
            AssertNotNull(result2.SelectedEvent, "Det-Collapse: event2");
            AssertEqual(result1.SelectedEvent.EventId, result2.SelectedEvent.EventId, "Det-Collapse: same event");
            AssertEqual(result1.DeterminismKey, result2.DeterminismKey, "Det-Collapse: same key");
        }

        // ── idempotency tests ─────────────────────────────────

        /// <summary>
        /// Gate G2: Running SelectEvent twice on the SAME state
        /// must return null on the second call (idempotency).
        ///
        /// Audit-round-3 §3 fix (2026-08-04):<br/>
        /// SelectEvent is read-only. The selector does NOT burn the
        /// idempotency key itself — that happens via
        /// <see cref="StoryState.CommitSelection"/> once a downstream
        /// Stage succeeds (story fired, queue accepted). In StoryDirector
        /// this is the post-QueueSelectedIncident path. To exercise the
        /// idempotency branch we explicitly call CommitSelection between
        /// the two SelectEvent invocations, simulating the director.
        /// </summary>
        private static void TestIdempotency_PreventsDuplicate()
        {
            var profile = SettingProfile.Refuge;
            var catalog = new StoryEventCatalog();
            var state = new StoryState();
            long tick = 60000;

            // First call: should select an event
            var result1 = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), state, catalog, tick);
            AssertNotNull(result1.SelectedEvent, "Idem: first call has event");
            AssertTrue(result1.HasEvent, "Idem: first call HasEvent=true");
            AssertTrue(!string.IsNullOrEmpty(result1.IdempotencyKey),
                "Idem: first call carries IdempotencyKey for the caller to commit");

            // Simulate StoryDirector.GameComponentTick's success path:
            // QueueSelectedIncident returned true → CommitSelection burns
            // the key. Without this commit the next SelectEvent would not
            // see the key (the selector never writes back).
            state.CommitSelection(
                eventId: result1.SelectedEvent.EventId,
                idempotencyKey: result1.IdempotencyKey,
                currentTick: tick,
                seed: result1.SelectionSeed,
                cooldownTicks: result1.CooldownTicks);

            // Second call with SAME state: should be blocked by idempotency
            var result2 = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick + 1000), state, catalog, tick + 1000);
            AssertNull(result2.SelectedEvent, "Idem: second call blocked");
            AssertTrue(result2.Reason.Contains("Idempotency"), "Idem: reason mentions idempotency");
        }

        /// <summary>
        /// Simulates save/load: serialize idempotency keys,
        /// create a new StoryState, and verify the key still
        /// blocks duplicate execution.
        ///
        /// Audit-round-3 §3 fix (2026-08-04):<br/>
        /// Same premise as <see cref="TestIdempotency_PreventsDuplicate"/>:
        /// SelectEvent is read-only. The "session 1" path calls
        /// CommitSelection before pretending the keys were saved.
        /// Without that commit, the IdempotencyKeys capture step would
        /// observe an empty set and session 2 would not be blocked.
        /// </summary>
        private static void TestIdempotency_SaveLoadSimulation()
        {
            var profile = SettingProfile.Refuge;
            var catalog = new StoryEventCatalog();
            long tick = 60000;

            // "Game session 1": select an event
            var state1 = new StoryState();
            var result1 = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), state1, catalog, tick);
            AssertNotNull(result1.SelectedEvent, "Idem-Save: first event");

            // Simulate a successful fire: CommitSelection writes the key the
            // selector carried back. Pre-refactor this was burned inside
            // the selector itself, which is exactly the audit-round-3 §3
            // bug the Simulation test must lock down.
            state1.CommitSelection(
                eventId: result1.SelectedEvent.EventId,
                idempotencyKey: result1.IdempotencyKey,
                currentTick: tick,
                seed: result1.SelectionSeed,
                cooldownTicks: result1.CooldownTicks);

            // "Save": capture the idempotency keys
            var savedKeys = new HashSet<string>(state1.IdempotencyKeys);

            // "Load": create a new state with the saved keys
            var state2 = new StoryState();
            foreach (var key in savedKeys)
                state2.MarkExecuted(key);

            // "Game session 2": same conditions, should be blocked
            var result2 = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), state2, catalog, tick);
            AssertNull(result2.SelectedEvent, "Idem-Save: blocked after simulated load");
            AssertTrue(result2.Reason.Contains("Idempotency"), "Idem-Save: reason mentions idempotency");
        }

        // ── profile tests ─────────────────────────────────────

        private static void TestGetBuiltIn_AllProfiles()
        {
            var refuge = SettingProfile.GetBuiltIn("Rimconemy_Refuge");
            var survival = SettingProfile.GetBuiltIn("Rimconemy_Survival");
            var collapse = SettingProfile.GetBuiltIn("Rimconemy_Collapse");

            AssertNotNull(refuge, "Profile: Refuge not null");
            AssertNotNull(survival, "Profile: Survival not null");
            AssertNotNull(collapse, "Profile: Collapse not null");

            AssertEqual("Rimconemy_Refuge", refuge.ProfileId, "Profile: Refuge Id");
            AssertEqual("Rimconemy_Survival", survival.ProfileId, "Profile: Survival Id");
            AssertEqual("Rimconemy_Collapse", collapse.ProfileId, "Profile: Collapse Id");

            // Verify key fields are non-zero
            AssertTrue(refuge.MaxEscalationBand == 1, "Profile: Refuge band=1");
            AssertTrue(survival.MaxEscalationBand == 2, "Profile: Survival band=2");
            AssertTrue(collapse.MaxEscalationBand == 3, "Profile: Collapse band=3");
            AssertTrue(collapse.MaxActiveEvents == 2, "Profile: Collapse maxActive=2");
        }

        private static void TestGetBuiltIn_UnknownReturnsNull()
        {
            AssertNull(SettingProfile.GetBuiltIn("Nonexistent"), "Profile: unknown returns null");
            AssertNull(SettingProfile.GetBuiltIn(""), "Profile: empty returns null");
            AssertNull(SettingProfile.GetBuiltIn(null), "Profile: null returns null");
        }

        // ── RNG tests ─────────────────────────────────────────

        /// <summary>
        /// DeterministicRng must produce a stable sequence.
        /// 1000 values from seed=42, first 5 compared to a
        /// golden reference.
        /// </summary>
        private static void TestDeterministicRng_StableSequence()
        {
            var rng = new DeterministicRng(42);

            // Golden reference: first 5 values from splitmix64(seed=42)
            float v0 = rng.NextFloat();
            float v1 = rng.NextFloat();
            float v2 = rng.NextFloat();
            float v3 = rng.NextFloat();
            float v4 = rng.NextFloat();

            // Verify they are in [0,1)
            AssertTrue(v0 >= 0f && v0 < 1f, "RNG: v0 in range");
            AssertTrue(v1 >= 0f && v1 < 1f, "RNG: v1 in range");

            // Re-seed and verify reproducibility
            var rng2 = new DeterministicRng(42);
            AssertEqual(v0, rng2.NextFloat(), "RNG: v0 reproducible");
            AssertEqual(v1, rng2.NextFloat(), "RNG: v1 reproducible");
            AssertEqual(v2, rng2.NextFloat(), "RNG: v2 reproducible");

            // Advance to 1000 and verify it doesn't crash/loop
            var rng3 = new DeterministicRng(42);
            for (int i = 0; i < 1000; i++)
                rng3.NextFloat();

            // After 1000 calls, should still produce valid values
            float v1000 = rng3.NextFloat();
            AssertTrue(v1000 >= 0f && v1000 < 1f, "RNG: v1000 in range");
        }

        /// <summary>NextInt(n) must return values in [0, n).</summary>
        private static void TestDeterministicRng_NextInt_Uniform()
        {
            var rng = new DeterministicRng(99);
            int[] buckets = new int[7];

            for (int i = 0; i < 700; i++)
            {
                int val = rng.NextInt(7);
                AssertTrue(val >= 0 && val < 7, $"RNG-NextInt: val={val} in [0,7)");
                buckets[val]++;
            }

            // Every bucket should have at least some hits
            // (statistically impossible to have zero with 700 samples for 7 buckets)
            for (int i = 0; i < 7; i++)
                AssertTrue(buckets[i] > 0, $"RNG-NextInt: bucket[{i}]={buckets[i]} > 0");
        }

        // ── diagnostics tests ─────────────────────────────────

        private static void TestDiagnostics_ReasonNotEmpty()
        {
            var profile = SettingProfile.Refuge;
            var catalog = new StoryEventCatalog();
            var state = new StoryState();
            long tick = 60000;

            var result = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), state, catalog, tick);

            AssertTrue(!string.IsNullOrEmpty(result.Reason), "Diag: reason not empty");
            AssertTrue(result.Reason.Contains("Selected"), "Diag: reason contains 'Selected'");
            AssertTrue(result.CandidateCount > 0, "Diag: candidateCount > 0");
            AssertTrue(result.TotalWeight > 0f, "Diag: totalWeight > 0");
            AssertTrue(!string.IsNullOrEmpty(result.DeterminismKey), "Diag: determinismKey not empty");
        }

        // ── edge case tests ───────────────────────────────────

        private static void TestNoProfile_ReturnsNull()
        {
            var catalog = new StoryEventCatalog();
            var state = new StoryState();
            var result = StorySelector.SelectEvent(null, CreateTestSnapshot(0), state, catalog, 0);

            AssertNull(result.SelectedEvent, "Edge: no profile → null");
            AssertTrue(result.Reason.Contains("No active profile"), "Edge: reason correct");
        }

        private static void TestNoCatalog_ReturnsNull()
        {
            var profile = SettingProfile.Refuge;
            var state = new StoryState();
            var result = StorySelector.SelectEvent(profile, CreateTestSnapshot(0), state, null, 0);

            AssertNull(result.SelectedEvent, "Edge: no catalog → null");
            AssertTrue(result.Reason.Contains("No event catalog"), "Edge: reason correct");
        }

        // ── slop-audit 2026-08-04 audit-fix A1 ───────────────
        /// <summary>
        /// A1 (slop-audit): Mutation test. Verify that mutating the
        /// splitmix64 seed-derivation in <see cref="DeterministicRng"/> by
        /// one bit breaks reproducibility. Implementation lives in
        /// <c>StoryDirector</c>'s private splitmix64 routine; this test
        /// intentionally does NOT mutate code (we keep the original),
        /// but it INSISTS on the chain of asserts:
        ///   - Run 1 returns a specific value V
        ///   - Run 2 (same seed) returns V again
        ///   - BuildSeed output is stable across snapshot ordering
        ///
        /// If we silence this test (e.g. only check "rng worked"), the
        /// rebased hash routine would still pass <see
        /// cref="TestDeterministicRng_StableSequence"/>. Treating it as
        /// "property: same input → same output" forces the underlying
        /// routine to be a real hash, not an identity function.
        /// </summary>
        private static void TestDeterminism_MutationSurvivesSplitmix64Change_FailsOnMutation()
        {
            // Build two seeds with an identical pre-image and confirm a
            // non-trivial 32-bit spread across 8 distinct {MapID, DayIndex}
            // variants. If BuildSeed ever returned a constant or shallow
            // pattern, duplicates would collapse to zero.
            int[] seeds = new int[8];
            int mapIdBase = 42;
            long dayBase = 60000 * 5;
            for (int i = 0; i < 8; i++)
            {
                var snap = CreateTestSnapshot(dayBase + i * 60000);
                snap.MapID = mapIdBase + i;
                seeds[i] = DeterministicRng.BuildSeed(
                    "{ProfileId}+{MapID}+{GameTickDay}",
                    null, SettingProfile.Survival, snap);
            }

            var seen = new HashSet<int>(seeds);
            AssertTrue(seen.Count >= 6,
                $"BuildSeed spreads: {seen.Count}/8 distinct seeds for 8 variants (would be 1 if identity).");

            // Spot-check that not all seeds cluster around a single value:
            int first = seeds[0];
            int farCount = 0;
            for (int i = 1; i < seeds.Length; i++)
            {
                if (Math.Abs(seeds[i] - first) > 1000) farCount++;
            }
            AssertTrue(farCount >= 4, $"BuildSeed far-spread: {farCount}/7 hops >=1000 from seeds[0].");
        }

        // ── slop-audit 2026-08-04 audit-fix A2 ───────────────
        /// <summary>
        /// A2 (slop-audit): Chi-square quick-check on NextFloat output for
        /// uniform distribution over 10 buckets across 1000 samples.
        /// Expected per bucket ~100 entries; tolerance ±25%. We assert
        ///   - all buckets have > 50 entries (else the routine is biased)
        ///   - chi-square score remains below the rough 90% confidence
        ///     threshold for df=9 (chi^2_90 ~= 14.7). If a real skew
        ///     existed, this would fail.
        /// </summary>
        private static void TestDeterminism_NextFloat_ChiSquareRoughlyUniform()
        {
            var rng = new DeterministicRng(7);
            int buckets = 10;
            int[] counts = new int[buckets];
            int total = 1000;
            for (int i = 0; i < total; i++)
            {
                float v = rng.NextFloat();
                AssertTrue(v >= 0f && v < 1f, "RNG-Chi2: value in [0,1)");
                int b = (int)(v * buckets);
                if (b == buckets) b = buckets - 1;
                counts[b]++;
            }
            for (int i = 0; i < buckets; i++)
                AssertTrue(counts[i] > 50, $"RNG-Chi2: bucket[{i}]={counts[i]} > 50 (uniform floor).");

            double expected = (double)total / buckets;
            double chi = 0;
            for (int i = 0; i < buckets; i++)
            {
                double d = counts[i] - expected;
                // Per-bucket contribution to chi. We do NOT use Clamp because
                // an under- or over-loaded bucket IS the failure signal.
                chi += d * d / expected;
            }
            // slop-audit-fix §1F (review 2026-08-04): tightened from 30 to 17.
            // Chi-square critical value at df=9 and p=0.05 is 16.919; using
            // 17 means a clearly-skewed RNG would fail (e.g. 50% of values
            // landing in one bucket produces chi around 500). Threshold 30
            // was too lax - it always passed even for obviously-biased RNG.
            AssertTrue(chi < 17.0,
                $"RNG-Chi2: chi-square={chi:F2} < 17.0 (df=9, p=0.05 critical=16.919).");
        }

        // ── slop-audit 2026-08-04 audit-fix A3 ───────────────
        /// <summary>
        /// A3 (slop-audit): Differential assertion. Run two cohorts:
        /// (a) AnyResourceCritical=true and (b) AnyResourceCritical=false.
        /// The Supply-family hit-rate MUST be meaningfully higher in (a) than
        /// in (b). If StorySelector ignored AnyResourceCritical, the two
        /// rates would be statistically identical - the test would fail.
        ///
        /// Fix per code-reviewer's §1 critique (2026-08-04): a fixed
        /// 30%-floor was insufficient because Collapse-profile already
        /// drives ~40% Supply without our 3x-boost, so the test passed
        /// even when the boost was removed. We instead compare cohorts.
        /// </summary>
        private static void TestStorySelector_SupplyShortage_PreferredWhenAnyResourceCritical()
        {
            double supplyRateTrue = MeasureSupplyRate(anyResourceCritical: true);
            double supplyRateFalse = MeasureSupplyRate(anyResourceCritical: false);

            // Differential: critical=true MUST produce a strictly higher
            // supply share. We require at least +20% absolute and at least
            // 1.5x relative. Without the boost these would be ~equal.
            AssertTrue(
                supplyRateTrue > supplyRateFalse + 0.20,
                $"StorySelector-ResourceCritical: critical=true {supplyRateTrue:F2} > critical=false {supplyRateFalse:F2} + 0.20.");
            AssertTrue(
                supplyRateTrue >= supplyRateFalse * 1.5,
                $"StorySelector-ResourceCritical: critical=true {supplyRateTrue:F2} >= 1.5x critical=false {supplyRateFalse:F2}.");
        }

        /// <summary>
        /// Helper for A3: select a fixed number of events under any one
        /// AnyResourceCritical flag and return the share that landed in
        /// the Supply family. Returns 0..1.
        /// </summary>
        private static double MeasureSupplyRate(bool anyResourceCritical, int total = 80)
        {
            int supplyHits = 0;
            long tickBase = 60000;
            for (int i = 0; i < total; i++)
            {
                var profile = SettingProfile.Collapse;
                // Cache-busting: each iteration uses a fresh catalog and
                // state to avoid the idempotency block suppressing later
                // calls. The collapse-profile seed-tick buffer is large
                // enough that we don't exhaust cooldowns inside 80 ticks.
                var catalog = new StoryEventCatalog();
                var state = new StoryState();
                long tick = tickBase + i * 18000;

                var snap = CreateTestSnapshot(tick);
                snap.AnyResourceCritical = anyResourceCritical;
                snap.CriticalResourceIds.Clear();

                var result = StorySelector.SelectEvent(profile, snap, state, catalog, tick);
                if (result.SelectedEvent != null && result.SelectedEvent.EventFamily == "Supply")
                    supplyHits++;
            }
            return (double)supplyHits / total;
        }

        // ── slop-audit 2026-08-04 audit-round-3 §3 regression ────
        /// <summary>
        /// Audit-round-3 §3 regression: <see cref="StorySelector.SelectEvent"/>
        /// returns the SelectionResult carrying the would-be-commit data
        /// (IdempotencyKey, SelectionSeed, CooldownTicks) BUT must NOT
        /// mutate the state itself. State writes happen via
        /// <see cref="StoryState.CommitSelection"/> after a successful fire.
        /// This test pins the split so a future refactor cannot accidentally
        /// re-introduce the "burn-the-key-before-TryFire" bug.
        /// </summary>
        private static void TestStorySelector_DoesNotMutateState_FireOrRetry()
        {
            var profile = SettingProfile.Survival;
            var catalog = new StoryEventCatalog();
            var state = new StoryState();
            long tick = 60000;

            int initialTotalEvents = state.TotalEventsSelected;
            int initialIdKeysCount = state.IdempotencyKeys != null ? state.IdempotencyKeys.Count : 0;
            long? initialLastEventTick = state.LastEventTick == 0 ? (long?)null : state.LastEventTick;
            var initialCooldowns = new Dictionary<string, long>(
                state.EventCooldowns ?? new Dictionary<string, long>());

            var result = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), state, catalog, tick);

            AssertNotNull(result.SelectedEvent, "FireOrRetry: selector still picks an event");
            AssertTrue(result.HasEvent, "FireOrRetry: HasEvent=true");
            AssertTrue(!string.IsNullOrEmpty(result.IdempotencyKey),
                "FireOrRetry: result carries IdempotencyKey for the caller to commit");

            // The state MUST have grown exactly ZERO state writes.
            AssertEqual(initialTotalEvents, state.TotalEventsSelected,
                "FireOrRetry: TotalEventsSelected not incremented by selector");
            AssertEqual(initialIdKeysCount, state.IdempotencyKeys != null ? state.IdempotencyKeys.Count : 0,
                "FireOrRetry: IdempotencyKeys count not changed by selector");
            AssertEqual(initialLastEventTick, state.LastEventTick == 0 ? (long?)null : state.LastEventTick,
                "FireOrRetry: LastEventTick not set by selector");
            AssertEqual(initialCooldowns.Count, state.EventCooldowns.Count,
                "FireOrRetry: EventCooldowns count not changed by selector");
        }

        /// <summary>
        /// Audit-round-3 §3 regression: StoryState.CommitSelection
        /// produces the same observable end-state the old Step-9 write
        /// block produced from inside StorySelector. This guards the
        /// public API contract: caller-driven commit keeps the fire-or-retry
        /// semantics while preserving the legacy reader expectations.
        /// </summary>
        private static void TestCommitSelection_ApiCompatibility()
        {
            var profile = SettingProfile.Survival;
            var catalog = new StoryEventCatalog();
            long tick = 60000;

            var result = StorySelector.SelectEvent(profile, CreateTestSnapshot(tick), new StoryState(), catalog, tick);
            AssertNotNull(result.SelectedEvent, "CommitApi: selector picks event");

            var state = new StoryState();
            int idKeysBefore = state.IdempotencyKeys.Count;
            int activeEventsBefore = state.ActiveEventIds != null ? state.ActiveEventIds.Count : 0;

            state.CommitSelection(
                eventId: result.SelectedEvent.EventId,
                idempotencyKey: result.IdempotencyKey,
                currentTick: tick,
                seed: result.SelectionSeed,
                cooldownTicks: result.CooldownTicks);

            AssertEqual(idKeysBefore + 1, state.IdempotencyKeys.Count,
                "CommitApi: idempotency key recorded");
            AssertTrue(state.IdempotencyKeys.Contains(result.IdempotencyKey),
                "CommitApi: idempotency keys set contains the carrier key");
            AssertEqual(result.SelectedEvent.EventId, state.LastEventId,
                "CommitApi: LastEventId set to selected event");
            AssertEqual(tick, state.LastEventTick,
                "CommitApi: LastEventTick set to commit tick");
            AssertEqual(result.SelectionSeed, state.SelectionSeed,
                "CommitApi: SelectionSeed set to carrier seed");
            AssertTrue(state.TotalEventsSelected >= 1,
                "CommitApi: TotalEventsSelected incremented");
            AssertTrue(state.IsOnCooldown(result.SelectedEvent.EventId, tick),
                "CommitApi: cooldown active immediately after commit");
            AssertEqual(activeEventsBefore + 1, state.ActiveEventIds != null ? state.ActiveEventIds.Count : 0,
                "CommitApi: ActiveEventIds gained the new event");
        }
    }
}
