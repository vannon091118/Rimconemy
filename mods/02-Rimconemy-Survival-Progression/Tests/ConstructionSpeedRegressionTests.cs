using System;
using Rimconemy.SurvivalProgression.Character.Construction;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Regression tests for DECISIONS §30 — Two-Layer ConstructionSpeed-Skill-Kurve.
    /// Owner: Survival &amp; Progression (Package 02).
    ///
    /// Ziel: Wenn jemand die Skill-Kurve oder den StatPart-Multiplier
    /// auf Vanilla zurücksetzt, muss dieser Test rot werden.
    /// </summary>
    public static class ConstructionSpeedRegressionTests
    {
        public const string TestGroup = "ConstructionSpeed";

        public static int RunAll()
        {
            int failures = 0;
            int probes = 0;

            // Kurven-Endpunkte (D1 Layer A Specification)
            probes++;
            float f1 = ConstructionSpeed_StatPart.SkillCurveFactor(1);
            if (Math.Abs(f1 - 0.5f) > 0.01f)
            {
                Log.Error($"[ConstructionSpeed] FAIL: SkillCurveFactor(1) == {f1:0.00}, expected 0.50");
                failures++;
            }
            probes++;
            float f20 = ConstructionSpeed_StatPart.SkillCurveFactor(20);
            if (Math.Abs(f20 - 2.5f) > 0.01f)
            {
                Log.Error($"[ConstructionSpeed] FAIL: SkillCurveFactor(20) == {f20:0.00}, expected 2.50");
                failures++;
            }

            // Monotonie (skill steigt -> Faktor steigt)
            probes++;
            float prev = ConstructionSpeed_StatPart.SkillCurveFactor(1);
            for (int lvl = 2; lvl <= 20; lvl++)
            {
                float cur = ConstructionSpeed_StatPart.SkillCurveFactor(lvl);
                if (cur < prev - 0.001f)
                {
                    Log.Error($"[ConstructionSpeed] FAIL: curve non-monotonic at level {lvl}: {cur:0.00} < {prev:0.00}");
                    failures++;
                    break;
                }
                prev = cur;
            }

            // Klemmung: skill 0 <= 1, skill >= 20 am maxFactor.
            probes++;
            float f0 = ConstructionSpeed_StatPart.SkillCurveFactor(0);
            if (f0 < 0.5f - 0.001f || f0 > 0.5f + 0.05f)
            {
                Log.Error($"[ConstructionSpeed] FAIL: SkillCurveFactor(0) == {f0:0.00}, ~0.50 erwartet (kein Hardcap-Bug)");
                failures++;
            }
            probes++;
            float f999 = ConstructionSpeed_StatPart.SkillCurveFactor(999);
            if (Math.Abs(f999 - 2.5f) > 0.001f)
            {
                Log.Error($"[ConstructionSpeed] FAIL: SkillCurveFactor(999) == {f999:0.00}, 2.50 erwartet (Klemm-Guard)");
                failures++;
            }

            // Layer B (Efficiency) ist konstant +50 % = 1.5x.
            probes++;
            float eff = ConstructionSpeed_StatPart.DefaultBuilderEfficiencyMultiplier;
            if (Math.Abs(eff - 1.5f) > 0.001f)
            {
                Log.Error($"[ConstructionSpeed] FAIL: Layer B DefaultBuilderEfficiencyMultiplier == {eff:0.00}, 1.50 erwartet");
                failures++;
            }

            // Effective composite: Skill × Layer B gibt 0.5×1.5=0.75 und 2.5×1.5=3.75 — die "+50 %" Deutung B.
            probes++;
            float composite1 = f1 * eff;
            float composite20 = f20 * eff;
            if (Math.Abs(composite1 - 0.75f) > 0.01f)
            {
                Log.Error($"[ConstructionSpeed] FAIL: composite Skill=1 == {composite1:0.00}, expected 0.75");
                failures++;
            }
            if (Math.Abs(composite20 - 3.75f) > 0.05f)
            {
                Log.Error($"[ConstructionSpeed] FAIL: composite Skill=20 == {composite20:0.00}, expected 3.75 (Layer A 2.5× * Layer B 1.5)");
                failures++;
            }

            // StatPart-Registrierung: die Klasse ist geladen (kein MissingMethod).
            probes++;
            try
            {
                var part = new ConstructionSpeed_StatPart();
                if (part == null) { failures++; }
                else { /* TransformValue würde GameState brauchen — nur Type-Check hier. */ }
            }
            catch (Exception ex)
            {
                Log.Error($"[ConstructionSpeed] FAIL: StatPart construction threw: {ex.GetType().Name}");
                failures++;
            }

            int passed = probes - failures;
            Log.Message(string.Format(
                "[ConstructionSpeed] ConstructionSpeed regression tests: {0} passed, {1} failed",
                passed, failures));

            return failures;
        }
    }
}
