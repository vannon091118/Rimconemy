using Rimconemy.SurvivalProgression.Needs;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Phase-2.7 (2026-08-04): Hunger-Amplifier-Tests.
    ///
    /// Owner: Survival &amp; Progression (Paket 02). Sole-Owner der
    /// NeedAmplifier-Logik (INTERFACE_CONTRACT §9.1).
    ///
    /// Tests bestätigen, dass ein gleicher Setting-Need-Sample
    /// deterministisch zu dem gleichen Hunger-Rate-Multiplikator führt.
    /// Damit ist die zentrale Beleg-Aussage der Iteration
    /// "gleicher Sample → deterministisch verstärkter Hunger-Tick"
    /// in 6 deterministischen RunAll-Tests geprüft.
    ///
    /// Vanilla-Healthy-Verification: kein Test bestätigt, dass Vanilla
    /// tatsächlich mit 0.7..1.4 hungert. Die Vanilla-HungerRateFactor-
    /// Übertragung ist ein Runtime-Gate, das durch einen echten Spielstand
    /// oder durch <c>scripts/runtime_test.sh</c> belegt werden muss.
    /// </summary>
    public static class HungerAmplifierTests
    {
        public const int ExpectedPassCount = 6;

        public static int RunAll()
        {
            int passed = 0;
            int failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name, string detail = null)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name + (detail == null ? "" : " — " + detail);
                Log.Warning("[Rimconemy.SurvivalProgression] HungerAmplifier test FAILED: " +
                    name + (detail == null ? "" : " — " + detail));
            }

            Check(TestAmplifierAtSampleZero(),                "T1.AmplifierAtSampleZero",       "expected 1.4 (hungren verstärkt)");
            Check(TestAmplifierAtSampleHalf(),                "T2.AmplifierAtSampleHalf",        "expected 1.0 (neutraler Anker)");
            Check(TestAmplifierAtSampleOne(),                 "T3.AmplifierAtSampleOne",         "expected 0.7 (satiated suppressed)");
            Check(TestSampleClampingAtOutOfRange(),           "T4.SampleClampingAtOutOfRange",   "sample &lt; 0 oder &gt; 1 muss clampen");
            Check(TestDeterminismSameSampleSameFactor(),      "T5.DeterminismSameSample",        "gleicher sample → gleicher Faktor");
            Check(TestSeverityOffsetAlwaysNonNegative(),      "T6.SeverityAlwaysNonNegative",    "Severity muss &gt;= 0.05 sein, sonst Hediff-Despawn");

            Log.Message(
                "[Rimconemy.SurvivalProgression] HungerAmplifier tests: " + passed + " passed, " +
                failed + " failed (min=" + ExpectedPassCount + ")." +
                (firstFailure == null ? "" : " First failure: " + firstFailure));
            return failed;
        }

        // ── T1 ─────────────────────────────────────────────────────────
        // Sample = 0.0 (vollständig hungren) → AmplifierFactor = 1.4.
        // Dies ist das Maximum der Stückweise-Funktion.
        public static bool TestAmplifierAtSampleZero()
        {
            try
            {
                float f = NeedAmplifier.AmplifierFactor(0.0f);
                return Mathf.Approximately(f, 1.4f);
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02B] test caught: " + ex); return false; }
        }

        // ── T2 ─────────────────────────────────────────────────────────
        // Sample = 0.5 (neutral) → AmplifierFactor = 1.0 (kein Effekt).
        public static bool TestAmplifierAtSampleHalf()
        {
            try
            {
                float f = NeedAmplifier.AmplifierFactor(0.5f);
                return Mathf.Approximately(f, 1.0f);
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02B] test caught: " + ex); return false; }
        }

        // ── T3 ─────────────────────────────────────────────────────────
        // Sample = 1.0 (vollständig satiated) → AmplifierFactor = 0.7.
        // Dies ist das Minimum der Stückweise-Funktion.
        public static bool TestAmplifierAtSampleOne()
        {
            try
            {
                float f = NeedAmplifier.AmplifierFactor(1.0f);
                return Mathf.Approximately(f, 0.7f);
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02B] test caught: " + ex); return false; }
        }

        // ── T4 ─────────────────────────────────────────────────────────
        // Out-of-Range-Samples werden defensiv auf [0,1] geclampt.
        // Sample < 0 (z.B. NaN→0 per Default) muss den gleichen Wert wie 0 ergeben.
        public static bool TestSampleClampingAtOutOfRange()
        {
            try
            {
                float belowZero = NeedAmplifier.AmplifierFactor(-0.5f);
                float aboveOne = NeedAmplifier.AmplifierFactor(1.5f);
                float zero = NeedAmplifier.AmplifierFactor(0.0f);
                float one = NeedAmplifier.AmplifierFactor(1.0f);
                bool belowOk = Mathf.Approximately(belowZero, zero);
                bool aboveOk = Mathf.Approximately(aboveOne, one);
                bool nanOk = Mathf.Approximately(NeedAmplifier.AmplifierFactor(float.NaN), 1.0f);
                bool infOk = Mathf.Approximately(NeedAmplifier.AmplifierFactor(float.PositiveInfinity), 1.0f);
                return belowOk && aboveOk && nanOk && infOk;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02B] test caught: " + ex); return false; }
        }

        // ── T5 ─────────────────────────────────────────────────────────
        // Deterministische Invariante: gleicher Sample → gleicher Faktor.
        // Dies ist die zentrale Beleg-Aussage für
        // "gleicher Sample → deterministisch verstärkter Hunger-Tick".
        public static bool TestDeterminismSameSampleSameFactor()
        {
            try
            {
                float[] probes = new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
                foreach (var s in probes)
                {
                    float a = NeedAmplifier.AmplifierFactor(s);
                    float b = NeedAmplifier.AmplifierFactor(s);
                    float c = NeedAmplifier.AmplifierFactor(s);
                    if (!(Mathf.Approximately(a, b) && Mathf.Approximately(b, c))) return false;
                }
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02B] test caught: " + ex); return false; }
        }

        // ── T6 ─────────────────────────────────────────────────────────
        // Vanilla-HediffLifecycle-Stabilität: Severity muss immer > 0 sein.
        // Wenn Severity <= 0 erreicht, despawnt vanilla das Hediff automatisch
        // und wir verlieren den Hook. Sample = 1.0 (satiated) ist der
        // Worst-Case: muss Severity = 0.05 (MinSeverityEpsilon) liefern.
        public static bool TestSeverityOffsetAlwaysNonNegative()
        {
            try
            {
                float[] probes = new float[] { 0.0f, 0.2f, 0.4f, 0.5f, 0.6f, 0.8f, 1.0f };
                foreach (var s in probes)
                {
                    float sev = NeedAmplifier.SeverityOffset(s);
                    if (sev < NeedAmplifier.MinSeverityEpsilon) return false;
                    if (sev > 1.0f) return false;
                }
                // Worst-Case: satiated pawn darf Hediff nicht despawnen.
                float sat = NeedAmplifier.SeverityOffset(1.0f);
                if (sat <= 0.0f) return false;
                if (sat > 0.10f) return false; // erwartet ungefähr 0.05
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod02B] test caught: " + ex); return false; }
        }
    }
}
