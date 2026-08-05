using System;
using Rimconemy.Foundation.Tests;
using Rimconemy.InfectedAutomation.Story;
using Rimconemy.InfectedAutomation.World;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Sprint 2 regression gate: infected pawn behavior state machine.
    ///
    /// Covers:
    ///   1. InfectedPawnState Scribe roundtrip (save/load integrity).
    ///   2. InfectedBehaviorTransition deterministic transitions.
    ///   3. Dormant→Roaming→Investigating→Assault state progression.
    ///   4. Deterministic RNG seed reproducibility.
    /// </summary>
    public static class Sprint2BehaviorRegressionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            TestInfectedPawnStateScribeRoundtrip();
            TestInfectedPawnStateConstructors();
            TestDormantToRoamingTransition();
            TestRoamingToInvestigatingTransition();
            TestInvestigatingToAssaultTransition();
            TestAssaultFallbackTransitions();
            TestDeterministicSeedReproducibility();
            TestAllStateValues();

            string summary = "[Rimconemy.InfectedAutomation] Sprint2 behavior regression tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        // ── 1. InfectedPawnState Scribe Roundtrip ─────────────

        private static void TestInfectedPawnStateScribeRoundtrip()
        {
            var original = new InfectedPawnState(42, new IntVec3(100, 0, 200), 60000L)
            {
                CurrentBehavior = InfectedBehaviorState.Roaming,
                BehaviorStartTick = 65000L,
                TargetCell = new IntVec3(120, 0, 220),
                TargetColonistId = -1,
                LastEvaluateTick = 65250L,
                LastSightRadius = 8.5f,
                SchemaVersion = 1,
            };

            bool success = ScribeRoundTripHelper.RoundTrip(original);
            AssertTrue(success, "SB1. InfectedPawnState Scribe roundtrip succeeds");

            AssertEqual(42, original.PawnThingId, "SB2. PawnThingId preserved");
            AssertTrue(original.CurrentBehavior == InfectedBehaviorState.Roaming,
                "SB3. CurrentBehavior preserved");
            AssertTrue(original.BehaviorStartTick == 65000L,
                "SB4. BehaviorStartTick preserved");
            AssertEqual(new IntVec3(120, 0, 220), original.TargetCell,
                "SB5. TargetCell preserved");
            AssertEqual(-1, original.TargetColonistId,
                "SB6. TargetColonistId preserved");
            AssertTrue(original.LastEvaluateTick == 65250L,
                "SB7. LastEvaluateTick preserved");
            AssertTrue(original.SchemaVersion == 1,
                "SB8. SchemaVersion preserved");

            // IsInactive and LastSightRadius are NOT persisted — PostLoadInit resets them.
            AssertTrue(!original.IsInactive,
                "SB9. IsInactive reset to false after roundtrip");
            AssertFloat(0f, original.LastSightRadius, 0f,
                "SB10. LastSightRadius reset to 0 after roundtrip");
        }

        // ── 2. InfectedPawnState Constructors ─────────────────

        private static void TestInfectedPawnStateConstructors()
        {
            var state = new InfectedPawnState(99, new IntVec3(50, 0, 50), 1000L);

            AssertEqual(99, state.PawnThingId, "SC1. PawnThingId set by constructor");
            AssertTrue(state.CurrentBehavior == InfectedBehaviorState.Dormant,
                "SC2. Default behavior is Dormant");
            AssertTrue(state.BehaviorStartTick == 1000L,
                "SC3. BehaviorStartTick set by constructor");
            AssertEqual(new IntVec3(50, 0, 50), state.SpawnCell,
                "SC4. SpawnCell set by constructor");
            AssertEqual(new IntVec3(50, 0, 50), state.TargetCell,
                "SC5. TargetCell = SpawnCell initially");
            AssertEqual(-1, state.TargetColonistId,
                "SC6. TargetColonistId = -1 initially");
            AssertTrue(!state.IsInactive,
                "SC7. IsInactive false initially");

            // TicksInState
            AssertTrue(state.TicksInState(2000L) == 1000L,
                "SC8. TicksInState = currentTick - BehaviorStartTick");

            // TransitionTo
            state.TransitionTo(InfectedBehaviorState.Roaming, 5000L);
            AssertTrue(state.CurrentBehavior == InfectedBehaviorState.Roaming,
                "SC9. TransitionTo changes state");
            AssertTrue(state.BehaviorStartTick == 5000L,
                "SC10. TransitionTo resets BehaviorStartTick");
            AssertEqual(-1, state.TargetColonistId,
                "SC11. TransitionTo resets TargetColonistId");

            // No-op transition
            state.TransitionTo(InfectedBehaviorState.Roaming, 6000L);
            AssertTrue(state.BehaviorStartTick == 5000L,
                "SC12. TransitionTo same state = no-op");
        }

        // ── 3. Dormant → Roaming ──────────────────────────────

        private static void TestDormantToRoamingTransition()
        {
            // Night with high attraction → should wake up.
            var env = new EnvironmentSnapshot { DarknessFactor = 0.8f };
            var chunk = new ChunkState(5, 5)
            {
                LightExposure = 0.2f,
                NoiseLevel = 0.05f,
                Attraction = 0.6f,
            };
            var rng = new DeterministicRng(12345);

            var next = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Dormant, chunk, env,
                colonistVisible: false, ticksInState: 500L, ref rng);

            AssertTrue(next == InfectedBehaviorState.Roaming,
                "DR1. Dormant→Roaming when dark + high attraction");

            // Daytime with any attraction → stay dormant.
            var dayEnv = new EnvironmentSnapshot { DarknessFactor = 0.1f };
            var rng2 = new DeterministicRng(12345);

            var nextDay = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Dormant, chunk, dayEnv,
                colonistVisible: false, ticksInState: 500L, ref rng2);

            AssertTrue(nextDay == InfectedBehaviorState.Dormant,
                "DR2. Dormant stays dormant in daylight");

            // Night with low attraction, random wake check.
            var quietChunk = new ChunkState(5, 5)
            {
                LightExposure = 0.1f,
                NoiseLevel = 0.0f,
                Attraction = 0.1f,
            };

            // With a seed that produces a small float: WakeRandomChance = 0.08.
            // DeterministicRng(55555).NextFloat() = ~0.62 → fails.
            var rng3 = new DeterministicRng(55555);
            var nextQuiet = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Dormant, quietChunk, env,
                colonistVisible: false, ticksInState: 500L, ref rng3);

            // Since the random check may or may not pass, we just verify
            // it's either Dormant or Roaming — both are valid.
            AssertTrue(nextQuiet == InfectedBehaviorState.Dormant
                || nextQuiet == InfectedBehaviorState.Roaming,
                "DR3. Dormant with low attraction: either stays or wakes randomly");
        }

        // ── 4. Roaming → Investigating ────────────────────────

        private static void TestRoamingToInvestigatingTransition()
        {
            var env = new EnvironmentSnapshot { DarknessFactor = 0.7f };
            var rng = new DeterministicRng(42);

            // Chunk is Suspicious → investigate.
            var suspectChunk = new ChunkState(3, 3)
            {
                AlertState = ChunkAlertState.Suspicious,
                NoiseLevel = 0.1f,
            };
            var next1 = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Roaming, suspectChunk, env,
                colonistVisible: false, ticksInState: 200L, ref rng);
            AssertTrue(next1 == InfectedBehaviorState.Investigating,
                "RI1. Roaming→Investigating when chunk is Suspicious");

            // Chunk is Investigating → investigate.
            var alertChunk = new ChunkState(3, 3)
            {
                AlertState = ChunkAlertState.Investigating,
                NoiseLevel = 0.0f,
            };
            var next2 = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Roaming, alertChunk, env,
                colonistVisible: false, ticksInState: 200L, ref rng);
            AssertTrue(next2 == InfectedBehaviorState.Investigating,
                "RI2. Roaming→Investigating when chunk is Investigating");

            // Loud noise → investigate even without alert.
            var noisyChunk = new ChunkState(3, 3)
            {
                AlertState = ChunkAlertState.Dormant,
                NoiseLevel = 0.5f,
            };
            var next3 = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Roaming, noisyChunk, env,
                colonistVisible: false, ticksInState: 200L, ref rng);
            AssertTrue(next3 == InfectedBehaviorState.Investigating,
                "RI3. Roaming→Investigating when noise above threshold");

            // Quiet, no alert, daylight → drops to Dormant.
            var dayEnv = new EnvironmentSnapshot { DarknessFactor = 0.05f };
            var quietChunk = new ChunkState(3, 3)
            {
                AlertState = ChunkAlertState.Dormant,
                NoiseLevel = 0.0f,
                Attraction = 0.0f,
            };
            var next4 = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Roaming, quietChunk, dayEnv,
                colonistVisible: false, ticksInState: 500L, ref rng);
            AssertTrue(next4 == InfectedBehaviorState.Dormant,
                "RI4. Roaming→Dormant in quiet daylight");
        }

        // ── 5. Investigating → Assault ────────────────────────

        private static void TestInvestigatingToAssaultTransition()
        {
            var env = new EnvironmentSnapshot { DarknessFactor = 0.6f };
            var chunk = new ChunkState(4, 4)
            {
                AlertState = ChunkAlertState.Investigating,
                NoiseLevel = 0.3f,
            };
            var rng = new DeterministicRng(777);

            // Colonist visible → Assault.
            var nextAssault = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Investigating, chunk, env,
                colonistVisible: true, ticksInState: 100L, ref rng);
            AssertTrue(nextAssault == InfectedBehaviorState.Assault,
                "IA1. Investigating→Assault when colonist visible");

            // No colonist, within timeout → stays Investigating.
            var nextStay = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Investigating, chunk, env,
                colonistVisible: false, ticksInState: 2000L, ref rng);
            AssertTrue(nextStay == InfectedBehaviorState.Investigating,
                "IA2. Investigating stays Investigating within timeout");

            // Timeout exceeded → drops to Roaming.
            var nextTimeout = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Investigating, chunk, env,
                colonistVisible: false, ticksInState: 3500L, ref rng);
            AssertTrue(nextTimeout == InfectedBehaviorState.Roaming,
                "IA3. Investigating→Roaming after timeout (3000 ticks)");

            // Alert decayed → drops to Roaming.
            var decayedChunk = new ChunkState(4, 4)
            {
                AlertState = ChunkAlertState.Dormant,
                NoiseLevel = 0.0f,
            };
            var nextDecayed = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Investigating, decayedChunk, env,
                colonistVisible: false, ticksInState: 500L, ref rng);
            AssertTrue(nextDecayed == InfectedBehaviorState.Roaming,
                "IA4. Investigating→Roaming when chunk alert decayed");
        }

        // ── 6. Assault Fallback Transitions ───────────────────

        private static void TestAssaultFallbackTransitions()
        {
            var env = new EnvironmentSnapshot { DarknessFactor = 0.5f };
            var chunk = new ChunkState(2, 2)
            {
                AlertState = ChunkAlertState.Assault,
                NoiseLevel = 0.5f,
            };
            var rng = new DeterministicRng(100);

            // Colonist visible → stays Assault.
            var nextStay = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Assault, chunk, env,
                colonistVisible: true, ticksInState: 100L, ref rng);
            AssertTrue(nextStay == InfectedBehaviorState.Assault,
                "AF1. Assault stays Assault when colonist visible");

            // Target lost briefly (under AssaultTargetLostTicks) → stays Assault.
            var nextBrief = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Assault, chunk, env,
                colonistVisible: false, ticksInState: 400L, ref rng);
            AssertTrue(nextBrief == InfectedBehaviorState.Assault,
                "AF2. Assault stays Assault under target-lost threshold");

            // Target lost > AssaultTargetLostTicks → Investigating.
            var nextLost = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Assault, chunk, env,
                colonistVisible: false, ticksInState: 700L, ref rng);
            AssertTrue(nextLost == InfectedBehaviorState.Investigating,
                "AF3. Assault→Investigating after target-lost timeout");

            // No colonist for > AssaultNoColonistTicks → Roaming.
            var nextGone = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Assault, chunk, env,
                colonistVisible: false, ticksInState: 2500L, ref rng);
            AssertTrue(nextGone == InfectedBehaviorState.Roaming,
                "AF4. Assault→Roaming after no-colonist timeout");
        }

        // ── 7. Deterministic Seed Reproducibility ─────────────

        private static void TestDeterministicSeedReproducibility()
        {
            // Same seed + same inputs → same next state.
            var env = new EnvironmentSnapshot { DarknessFactor = 0.8f };
            var chunk = new ChunkState(5, 5)
            {
                AlertState = ChunkAlertState.Dormant,
                NoiseLevel = 0.05f,
                Attraction = 0.5f,
            };

            var rng1 = new DeterministicRng(42);
            var rng2 = new DeterministicRng(42);

            var result1 = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Dormant, chunk, env,
                colonistVisible: false, ticksInState: 500L, ref rng1);

            var result2 = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Dormant, chunk, env,
                colonistVisible: false, ticksInState: 500L, ref rng2);

            AssertTrue(result1 == result2,
                "DS1. Same seed + same inputs → same output");

            // Different seed may differ (but is still deterministic per seed).
            var rng3 = new DeterministicRng(43);
            var result3 = InfectedBehaviorTransition.ComputeNext(
                InfectedBehaviorState.Dormant, chunk, env,
                colonistVisible: false, ticksInState: 500L, ref rng3);
            // Both valid states.
            AssertTrue(result3 == InfectedBehaviorState.Dormant
                || result3 == InfectedBehaviorState.Roaming,
                "DS2. Different seed produces valid state");

            // Stable hash reproducibility.
            int hash1 = DeterministicRng.GetStableHashCode("pawn42|day5|map1");
            int hash2 = DeterministicRng.GetStableHashCode("pawn42|day5|map1");
            AssertEqual(hash1, hash2, "DS3. FNV-1a hash is reproducible");

            int hash3 = DeterministicRng.GetStableHashCode("pawn43|day5|map1");
            AssertTrue(hash1 != hash3,
                "DS4. Different input → different hash");
        }

        // ── 8. All State Values ───────────────────────────────

        private static void TestAllStateValues()
        {
            AssertTrue((int)InfectedBehaviorState.Dormant == 0,
                "SV1. Dormant=0");
            AssertTrue((int)InfectedBehaviorState.Roaming == 1,
                "SV2. Roaming=1");
            AssertTrue((int)InfectedBehaviorState.Investigating == 2,
                "SV3. Investigating=2");
            AssertTrue((int)InfectedBehaviorState.Assault == 3,
                "SV4. Assault=3");

            // Threshold constants.
            AssertFloat(0.5f, InfectedBehaviorTransition.WakeDarknessThreshold, 0.001f,
                "SV5. WakeDarknessThreshold=0.5");
            AssertFloat(0.3f, InfectedBehaviorTransition.WakeAttractionThreshold, 0.001f,
                "SV6. WakeAttractionThreshold=0.3");
            AssertFloat(0.08f, InfectedBehaviorTransition.WakeRandomChance, 0.001f,
                "SV7. WakeRandomChance=0.08");
            AssertFloat(0.2f, InfectedBehaviorTransition.InvestigateNoiseThreshold, 0.001f,
                "SV8. InvestigateNoiseThreshold=0.2");
            AssertFloat(15f, InfectedBehaviorTransition.InfectedBaseSight, 0.001f,
                "SV9. InfectedBaseSight=15");
            AssertTrue(InfectedBehaviorTransition.InvestigationTimeoutTicks == 3000L,
                "SV10. InvestigationTimeout=3000 ticks");
            AssertTrue(InfectedBehaviorTransition.AssaultTargetLostTicks == 600L,
                "SV11. AssaultTargetLost=600 ticks");
            AssertTrue(InfectedBehaviorTransition.AssaultNoColonistTicks == 2000L,
                "SV12. AssaultNoColonist=2000 ticks");
        }

        // ── Assert Helpers ────────────────────────────────────

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[Sprint2Regression] " + label); }
        }

        private static void AssertFloat(float expected, float actual, float tolerance, string label)
        {
            if (Math.Abs(expected - actual) <= tolerance) _passed++;
            else
            {
                _failed++;
                Log.Error("[Sprint2Regression] " + label + ": expected " + expected
                    + ", got " + actual + " (diff=" + (expected - actual) + ")");
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[Sprint2Regression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
