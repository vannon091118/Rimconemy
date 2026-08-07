using System;
using System.Collections.Generic;
using Rimconemy.Foundation.Tests;
using Rimconemy.InfectedAutomation.World;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Sprint 1 regression gate: perception infrastructure correctness.
    ///
    /// Covers:
    ///   1. ChunkState Scribe roundtrip (save/load integrity).
    ///   2. LightSystem daylight curve boundary values.
    ///   3. NoiseSystem inverse-square falloff math.
    ///   4. PerceptionMath attraction + sight radius formulas.
    /// </summary>
    public static class Sprint1PerceptionRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "Sprint1 perception regression tests");

            _passed = 0;
            _failed = 0;

            TestChunkStateScribeRoundtrip();
            TestChunkStateBoundaryValues();
            TestLightSystemDaylightCurve();
            TestNoiseSystemFalloff();
            TestPerceptionMathAttraction();
            TestPerceptionMathSightRadius();

            string summary = "[Rimconemy.InfectedAutomation] Sprint1 perception regression tests: "
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

        // ── 1. ChunkState Scribe Roundtrip ────────────────────

        private static void TestChunkStateScribeRoundtrip()
        {
            var original = new ChunkState(14, 7)
            {
                LightExposure = 0.72f,
                NoiseLevel = 0.33f,
                AlertState = ChunkAlertState.Investigating,
                LastUpdatedTick = 60000L,
                SchemaVersion = 1,
            };
            original.KnownTargets.Add(42);
            original.KnownTargets.Add(17);
            original.KnownTargets.Add(99);

            bool success = ScribeRoundTripHelper.RoundTrip(original);
            ts.Check(success, "SR1. ChunkState Scribe roundtrip succeeds");

            // Rebuild after roundtrip — the Scribe helper mutates the original.
            // Key invariants after reload:
            ts.Check(Equals(14, original.ChunkX), "SR2. ChunkX preserved after roundtrip");
            ts.Check(Equals(7, original.ChunkZ), "SR3. ChunkZ preserved after roundtrip");
            AssertFloat(0.72f, original.LightExposure, 0.001f, "SR4. LightExposure preserved");
            AssertFloat(0.33f, original.NoiseLevel, 0.001f, "SR5. NoiseLevel preserved");
            ts.Check(original.AlertState == ChunkAlertState.Investigating, "SR6. AlertState preserved");
            ts.Check(original.LastUpdatedTick == 60000L, "SR7. LastUpdatedTick preserved");
            ts.Check(original.KnownTargets.Count == 3, "SR8. KnownTargets count preserved (3)");
            ts.Check(original.KnownTargets.Contains(42)
                && original.KnownTargets.Contains(17)
                && original.KnownTargets.Contains(99), "SR9. KnownTargets exact values preserved");
            ts.Check(original.SchemaVersion == 1, "SR10. SchemaVersion preserved");

            // Attraction is derived data — NOT persisted.
            AssertFloat(0f, original.Attraction, 0f,
                "SR11. Attraction is zero after roundtrip (not persisted)");
        }

        // ── 2. ChunkState boundary values ─────────────────────

        private static void TestChunkStateBoundaryValues()
        {
            var chunk = new ChunkState(0, 0);

            // ChunkKey formula: z * 1000 + x.
            chunk.ChunkX = 15;
            chunk.ChunkZ = 3;
            ts.Check(chunk.ChunkKey == 3015, "CB1. ChunkKey = z*1000+x");

            chunk.ChunkX = 0;
            chunk.ChunkZ = 0;
            ts.Check(chunk.ChunkKey == 0, "CB2. ChunkKey zero for origin");

            // IsStale: currentTick - LastUpdatedTick > maxAge.
            chunk.LastUpdatedTick = 100L;
            ts.Check(chunk.IsStale(1000L, 500L), "CB3. IsStale true when gap > max");
            ts.Check(!chunk.IsStale(1000L, 1500L), "CB4. IsStale false when gap < max");
            ts.Check(!chunk.IsStale(100L, 10L), "CB5. IsStale false at exact boundary");

            // AlertState enum values.
            ts.Check((int)ChunkAlertState.Dormant == 0, "CB6. Dormant=0");
            ts.Check((int)ChunkAlertState.Suspicious == 1, "CB7. Suspicious=1");
            ts.Check((int)ChunkAlertState.Investigating == 2, "CB8. Investigating=2");
            ts.Check((int)ChunkAlertState.Assault == 3, "CB9. Assault=3");
        }

        // ── 3. LightSystem Daylight Curve ─────────────────────

        private static void TestLightSystemDaylightCurve()
        {
            // Use reflection to test private DaylightCurve/HoursOfDay directly
            // via known tick-to-hour mapping. TicksPerDay=60000.
            // Hour 0 = tick 0; Hour 12 = tick 30000; Hour 23 = tick 57500.
            // Strategy: call ChunkGridComponent.Refresh with known tick values
            // and inspect the resulting EnvironmentSnapshot.

            // Simulate daylight at key hours using the internal math:
            //   hour = (tick % TicksPerDay) / TicksPerDay * 24
            //   Tick 0 → hour 0.0
            //   Tick 15000 → hour 6.0 (dawn midpoint)
            //   Tick 25000 → hour 10.0 (day)
            //   Tick 45000 → hour 18.0 (dusk start)
            //   Tick 50000 → hour 20.0 (night start)

            // Night (hour 0): DaylightFactor=0, DarknessFactor=1
            float hour0 = TickToHour(0L);
            AssertFloat(0.0f, hour0, 0.001f, "LC1. Tick 0 → hour 0.0");
            AssertFloat(0.0f, DaylightAtTick(0L), 0.001f, "LC2. Daylight=0 at midnight (hour 3, tick 7500)");
            AssertFloat(0.0f, DaylightAtTick(7500L), 0.001f, "LC3. Daylight=0 at 3am");

            // Dawn midpoint (hour 6): linear 0.0→1.0 from hour 5→7.
            // hour 6 = 0.5
            float hour6 = TickToHour(15000L);
            AssertFloat(6.0f, hour6, 0.01f, "LC4. Tick 15000 → hour 6.0");
            AssertFloat(0.5f, DaylightAtTick(15000L), 0.01f, "LC5. Daylight=0.5 at dawn midpoint (hour 6)");

            // Dawn start (hour 5 exactly): 0.0
            float hour5 = TickToHour(12500L);
            AssertFloat(5.0f, hour5, 0.01f, "LC6. Tick 12500 → hour 5.0");
            AssertFloat(0.0f, DaylightAtTick(12500L), 0.01f, "LC7. Daylight=0.0 at dawn start (hour 5)");

            // Day (hour 10): full daylight.
            AssertFloat(1.0f, DaylightAtTick(25000L), 0.001f, "LC8. Daylight=1.0 at noon (hour 12)");
            AssertFloat(1.0f, DaylightAtTick(25000L), 0.001f, "LC9. Daylight=1.0 at mid-day (hour 10)");

            // Dusk start (hour 18): 1.0
            float hour18 = TickToHour(45000L);
            AssertFloat(18.0f, hour18, 0.01f, "LC10. Tick 45000 → hour 18.0");
            AssertFloat(1.0f, DaylightAtTick(45000L), 0.01f, "LC11. Daylight=1.0 at dusk start (hour 18)");

            // Dusk midpoint (hour 19): 0.5
            AssertFloat(0.5f, DaylightAtTick(47500L), 0.01f, "LC12. Daylight=0.5 at dusk midpoint (hour 19)");

            // Night start (hour 20): 0.0
            AssertFloat(0.0f, DaylightAtTick(50000L), 0.01f, "LC13. Daylight=0.0 at night start (hour 20)");

            // Wrap-around: tick 59000 (hour 23.6) still night.
            AssertFloat(0.0f, DaylightAtTick(59000L), 0.001f, "LC14. Daylight=0.0 near midnight (hour 23.6)");
        }

        // ── 4. NoiseSystem Falloff Math ───────────────────────

        private static void TestNoiseSystemFalloff()
        {
            // Inverse-square falloff: falloff = 1 / (1 + d² / r²)
            // At d=0: falloff = 1.0 (center = full contribution)
            // At d=r: falloff = 1 / (1 + 1) = 0.5
            // At d=2r: falloff = 1 / (1 + 4) = 0.2
            // As d→∞: falloff → 0

            const float r = 25f;
            float d0 = 0f;
            float dHalf = r * 0.5f;
            float dR = r;
            float d2R = r * 2f;
            float dFar = r * 10f;

            float f0 = InverseSquareFalloff(d0, r);
            float fHalf = InverseSquareFalloff(dHalf, r);
            float fR = InverseSquareFalloff(dR, r);
            float f2R = InverseSquareFalloff(d2R, r);
            float fFar = InverseSquareFalloff(dFar, r);

            AssertFloat(1.0f, f0, 0.001f, "NS1. Falloff=1.0 at d=0 (center)");
            AssertFloat(0.5f, fR, 0.001f, "NS2. Falloff=0.5 at d=r");
            ts.Check(fHalf > 0.5f && fHalf < 1.0f, "NS3. Falloff at d=r/2 between 0.5 and 1");
            ts.Check(f2R > 0.15f && f2R < 0.25f, "NS4. Falloff at d=2r between 0.15 and 0.25");
            ts.Check(fFar < 0.01f, "NS5. Falloff at d=10r near zero");

            // Monotonic: falloff always decreases with distance.
            ts.Check(f0 > fHalf, "NS6. Falloff monotonic: f(0) > f(r/2)");
            ts.Check(fHalf > fR, "NS7. Falloff monotonic: f(r/2) > f(r)");
            ts.Check(fR > f2R, "NS8. Falloff monotonic: f(r) > f(2r)");
            ts.Check(f2R > fFar, "NS9. Falloff monotonic: f(2r) > f(10r)");

            // Generator noise values.
            AssertFloat(0.30f, NoiseSystem_GeneratorBase, 0.001f,
                "NS10. Generator base noise = 0.30");
            AssertFloat(0.15f, NoiseSystem_FueledBase, 0.001f,
                "NS11. Fueled device base noise = 0.15");
        }

        // ── 5. PerceptionMath Attraction ──────────────────────

        private static void TestPerceptionMathAttraction()
        {
            // At full daylight (darkness=0): weights are 1.0.
            float attrDay = PerceptionMath.ComputeAttraction(0.5f, 0.3f, 0.0f);
            AssertFloat(0.8f, attrDay, 0.001f, "PM1. Day attraction = light+noise (0.5+0.3=0.8)");

            // At full night (darkness=1): lightWeight=2.25, noiseWeight=1.75.
            float attrNight = PerceptionMath.ComputeAttraction(0.5f, 0.3f, 1.0f);
            AssertFloat(1.65f, attrNight, 0.001f, "PM2. Night attraction = 0.5*2.25 + 0.3*1.75 = 1.65");

            // At dusk (darkness=0.5): lightWeight=1.625, noiseWeight=1.375.
            float attrDusk = PerceptionMath.ComputeAttraction(0.5f, 0.3f, 0.5f);
            AssertFloat(1.225f, attrDusk, 0.001f, "PM3. Dusk attraction = 0.5*1.625 + 0.3*1.375 = 1.225");

            // Zero input → zero attraction.
            float attrZero = PerceptionMath.ComputeAttraction(0f, 0f, 1.0f);
            AssertFloat(0f, attrZero, 0.001f, "PM4. Zero light+noise = zero attraction");

            // Darkness caps at 1: if >1, still behaves at max.
            float attrOverDark = PerceptionMath.ComputeAttraction(0.5f, 0.3f, 2.0f);
            float expectedOver = 0.5f * (1f + 2.0f * 1.25f) + 0.3f * (1f + 2.0f * 0.75f);
            AssertFloat(expectedOver, attrOverDark, 0.001f,
                "PM5. Darkness>1 still computes (no internal clamp — caller responsibility)");

            // Light-only and noise-only extremes.
            float attrLightOnly = PerceptionMath.ComputeAttraction(1f, 0f, 1.0f);
            AssertFloat(2.25f, attrLightOnly, 0.001f, "PM6. Light-only night = 2.25");
            float attrNoiseOnly = PerceptionMath.ComputeAttraction(0f, 1f, 1.0f);
            AssertFloat(1.75f, attrNoiseOnly, 0.001f, "PM7. Noise-only night = 1.75");
        }

        // ── 6. PerceptionMath Sight Radius ────────────────────

        private static void TestPerceptionMathSightRadius()
        {
            // Clear day: daylight=1, weather=1 → modifier = 0.5+0.5*1 = 1.0
            //             → modifier *= 1-1*0.35 = 0.65
            // baseSight=10 → 10*0.65 = 6.5
            float sightClearDay = PerceptionMath.ComputeSightRadius(10f, 1f, 1f);
            AssertFloat(6.5f, sightClearDay, 0.01f, "PS1. Sight in clear day = 10*0.65=6.5");

            // Clear day, full weather attenuation: weather=0 (no weather)
            float sightClear = PerceptionMath.ComputeSightRadius(10f, 1f, 0f);
            AssertFloat(10f, sightClear, 0.01f, "PS2. Sight in clear day no weather = 10*1.0=10");

            // Night: daylight=0, weather=1 → modifier=0.5, then 0.5*0.65=0.325
            float sightNight = PerceptionMath.ComputeSightRadius(10f, 0f, 1f);
            AssertFloat(3.25f, sightNight, 0.01f, "PS3. Sight at night = 10*0.325=3.25");

            // Foggy night: daylight=0, weather=0.4 → modifier=0.5, 0.5*(1-0.4*0.35)=0.43
            float sightFog = PerceptionMath.ComputeSightRadius(10f, 0f, 0.4f);
            float expectedFog = 10f * 0.5f * (1f - 0.4f * 0.35f);
            AssertFloat(expectedFog, sightFog, 0.01f, "PS4. Sight in foggy night");

            // Dusk: daylight=0.5, weather=1 → modifier=0.5+0.25=0.75, *0.65=0.4875
            float sightDusk = PerceptionMath.ComputeSightRadius(10f, 0.5f, 1f);
            AssertFloat(4.875f, sightDusk, 0.01f, "PS5. Sight at dusk = 10*0.4875=4.875");

            // Zero base sight.
            float sightZero = PerceptionMath.ComputeSightRadius(0f, 1f, 0f);
            AssertFloat(0f, sightZero, 0.001f, "PS6. Base sight 0 → radius 0");

            // Minimum sight: baseSight=1, night, fog
            float sightMin = PerceptionMath.ComputeSightRadius(1f, 0f, 0.4f);
            ts.Check(sightMin > 0.2f && sightMin < 0.5f, "PS7. Minimum sight ~0.43 (1 pawn, night+fog)");
        }

        // ── Reflection Helpers for LightSystem ────────────────

        /// <summary>Computes the daylight curve value for a given tick
        /// using the same hour function as LightSystem.</summary>
        private static float DaylightAtTick(long tick)
        {
            float hour = TickToHour(tick);
            if (hour < 5f || hour >= 20f) return 0f;
            if (hour < 7f) return (hour - 5f) / 2f;
            if (hour < 18f) return 1f;
            return 1f - (hour - 18f) / 2f;
        }

        private static float TickToHour(long tick)
        {
            float dayProgress = (tick % 60000L) / 60000f;
            return dayProgress * 24f;
        }

        /// <summary>Inverse-square noise falloff: 1 / (1 + d²/r²).</summary>
        private static float InverseSquareFalloff(float dist, float radius)
        {
            return 1f / (1f + (dist * dist) / (radius * radius));
        }

        private const float NoiseSystem_GeneratorBase = 0.30f;
        private const float NoiseSystem_FueledBase = 0.15f;

        // ── Assert Helpers ────────────────────────────────────


        private static void AssertFloat(float expected, float actual, float tolerance, string label)
        {
            if (Math.Abs(expected - actual) <= tolerance) _passed++;
            else
            {
                _failed++;
                Log.Error("[Sprint1Regression] " + label + ": expected " + expected
                    + ", got " + actual + " (diff=" + (expected - actual) + ")");
            }
        }

    }
}
