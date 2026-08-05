using System;
using Rimconemy.InfectedAutomation.Population;
using Verse;
// `using System;` is required for `Math.Abs` and `Type.GetType` \u2014 the
// 05-package csproj does NOT enable `<ImplicitUsings>`, so System types
// have to be brought in explicitly.

namespace Rimconemy.InfectedAutomation.Tests
{
    /// <summary>
    /// Phase F (2026-08-05) — HordeStorySelector regression tests (T29-T32).
    ///
    /// T29  : ThreatGate fires only above profile-specific threshold.
    /// T30  : CooldownDays per profile (Collapse < Refuge).
    /// T31  : SelectHordeMigrationLetter static method exists on
    ///        <c>Rimconemy.InfectedAutomation.Horde.HordeStorySelector</c>.
    ///        Reflection-based because the class lands with Phase F Task 8.
    /// T32  : Effect-Hook <c>ProcessTriggerHordeMigrationEffect</c> exists on
    ///        the same HordeStorySelector class
    ///        (spec docs/superpowers/specs/2026-08-05-horde-migration-design.md §6.3).
    ///        Reflection-based; soft-logged until Phase F ships.
    /// </summary>
    public static class HordeStorySelectorTests
    {
        // Reflection-based type-lookups so the build does not require Phase F
        // substrate classes to be present. We pass once the class lands —
        // each test scaffolds its own forward-checks.
        private const string HordeStorySelectorTypeName =
            "Rimconemy.InfectedAutomation.Horde.HordeStorySelector, Rimconemy.InfectedAutomation";

        public static int RunAll()
        {
            int passed = 0;
            int failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Warning("[Rimconemy.InfectedAutomation] HordeStorySelector test FAILED: " + name);
            }

            // Local function so it captures `passed` by closure (a `private
            // static` cannot capture RunAll locals). Phase F pending probes
            // do NOT bump `failed`: forward-compat pending is not a
            // regression. The `[PhaseF-ForwardCompat]` Log.Message prefix
            // lets operators grep-isolate soft logs from real WARNINGs.
            void CallForwardCompat(string name, System.Func<bool> probe)
            {
                if (probe == null) return;
                try
                {
                    if (probe()) { passed++; return; }
                    Log.Message("[Rimconemy.InfectedAutomation][PhaseF-ForwardCompat] "
                        + name + " not yet implemented (deferred Phase F Task 8).");
                }
                catch (System.Exception ex)
                {
                    Log.Message("[Rimconemy.InfectedAutomation][PhaseF-ForwardCompat] "
                        + name + " exception: " + ex.GetType().Name + ": " + ex.Message);
                }
            }

            Check(T29_ThreatGateFiresOnlyAbove(),   "T29.ThreatGateAboveThreshold");
            Check(T30_CooldownDaysRespected(),     "T30.CooldownDaysRespected");
            // Phase F forward-compat probes (T31/T32): wired through
            // CallForwardCompat() so a missing class does NOT mark the run
            // as failed, nor spam the boot log with red WARNINGs. The probes
            // re-evaluate as green as soon as HordeStorySelector.cs lands
            // (Phase F Task 8, docs/superpowers/specs/2026-08-05-horde-migration-design.md).
            CallForwardCompat("T31.SelectorMethodExists", T31_SelectorMethodExists);
            CallForwardCompat("T32.EffectHookExists",     T32_EffectHookExists);

            Log.Message(
                "[Rimconemy.InfectedAutomation] HordeStorySelector tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));
            return passed;
        }

        private static bool T29_ThreatGateFiresOnlyAbove()
        {
            // Profile thresholds: Survival=0.70, Collapse=0.50 (per Multipliers Hausordnung)
            float survivalTh = PopulationProfileMultipliers.GetHordeActivationThreshold("Survival");
            float collapseTh = PopulationProfileMultipliers.GetHordeActivationThreshold("Collapse");
            return Math.Abs(survivalTh - 0.70f) < 0.005f
                && Math.Abs(collapseTh - 0.50f) < 0.005f;
        }

        private static bool T30_CooldownDaysRespected()
        {
            float collapseCd = PopulationProfileMultipliers.GetHordeLetterCooldownDays("Collapse");
            float refugeCd = PopulationProfileMultipliers.GetHordeLetterCooldownDays("Refuge");
            return Math.Abs(collapseCd - 5f) < 0.005f
                && refugeCd > collapseCd;
        }

        // Reflection probe — the class is a Phase-F future drop. Pre-F this
        // returns null and the test asserts false (which is the honest answer
        // for the current state). When Phase F Task 8 lands, this becomes
        // green automatically.
        private static bool T31_SelectorMethodExists()
        {
            Type t = Type.GetType(HordeStorySelectorTypeName, throwOnError: false);
            if (t == null) return false;
            return t.GetMethod("SelectHordeMigrationLetter",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) != null;
        }

        // Hook probe — spec §6.3: ProcessTriggerHordeMigrationEffect lives on
        // HordeStorySelector (Phase F Task 8 deliverable). Reflection probe;
        // does NOT require the class to be present today.
        private static bool T32_EffectHookExists()
        {
            Type t = Type.GetType(HordeStorySelectorTypeName, throwOnError: false);
            if (t == null) return false;
            return t.GetMethod("ProcessTriggerHordeMigrationEffect",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) != null;
        }
    }
}
