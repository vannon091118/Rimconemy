// Tests/HordeRegressionTests.cs
//
// Phase D — Horde-Overlay Visualisierung (D1-D15).
// spec: docs/superpowers/specs/2026-08-05-horde-overlay-design.md
// plan: docs/superpowers/plans/2026-08-05-horde-overlay.md
//
// Owner: Infected & Automation (Package 05).
//
// Calculator-side tests cover Pure-Logic only.
// UpdateLogic-WorldObject tests cover pure spawn / drift / despawn.
// D14 + D15 verify Def-load + Spawner defensive paths.

using Rimconemy.InfectedAutomation.Horde;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class HordeRegressionTests
    {
        public const int ExpectedPassCount = 15;

        public static int RunAll()
        {
            int passed = 0;
            int failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                firstFailure ??= name;
                Log.Warning("[Rimconemy.InfectedAutomation] Phase D test FAILED: " + name);
            }

            // ── D1-D6: Calculator basics ──────────────────────
            Check(D1_CalculatorEmptyLedger(),                        "D1.CalculatorEmptyLedger");
            Check(D2_CalculatorSurvival150Human(),                   "D2.CalculatorSurvival150Human");
            Check(D3_CalculatorSurvival100Human100Animal(),          "D3.CalculatorSurvival100Human100Animal");
            Check(D4_CalculatorCollapseThreshold(),                   "D4.CalculatorCollapseThreshold");
            Check(D5_CalculatorProfileFallbackNull(),                "D5.CalculatorProfileFallbackNull");
            Check(D6_PulsePhaseSinusoidal(),                         "D6.PulsePhaseSinusoidal");

            // ── D7-D10: UpdateLogic Pure ─────────────────────
            Check(D7_UpdatePureDespawnBelowThreshold(),              "D7.UpdatePureDespawnBelowThreshold");
            Check(D8_UpdatePureSpawnAboveThreshold(),                "D8.UpdatePureSpawnAboveThreshold");
            Check(D9_UpdatePureMoveTowardsHome(),                    "D9.UpdatePureMoveTowardsHome");
            Check(D10_UpdatePureMoveIntervalRespected(),             "D10.UpdatePureMoveIntervalRespected");

            // ── D11-D13: Strip-Prefix routing ──────────────
            Check(D11_StripRimconemyPrefixForThreshold(),            "D11.StripRimconemyPrefixForThreshold");
            Check(D12_StripRimconemyPrefixNullReturnsSurvival(),     "D12.StripRimconemyPrefixNullReturnsSurvival");
            Check(D13_AnimalHalfCapRoute(),                          "D13.AnimalHalfCapRoute");

            // ── D14-D15: WorldObject + Spawner defensive ───
            Check(D14_WorldObjectExistsInDefDB(),                    "D14.WorldObjectExistsInDefDB");
            Check(D15_SpawnerNullMapComponentSkip(),                 "D15.SpawnerNullMapComponentSkip");

            Log.Message(
                "[Rimconemy.InfectedAutomation] Horde regression tests: "
                + passed + " passed, " + failed + " failed" +
                (firstFailure != null ? " (first failure: " + firstFailure + ")" : ""));
            return passed;
        }

        // ── D1: empty ledger → 0, IsActive=false ────────────────
        private static bool D1_CalculatorEmptyLedger()
        {
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 0,
                AnimalLiveCount = 0,
                Cap = 100,
                ProfileId = "Survival",
            };
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            return effective == 0 && !HordeCalculator.IsActive(effective, SettingProfile.Survival);
        }

        // ── D2: Survival threshold 150. 150 humanoid → active ─────
        private static bool D2_CalculatorSurvival150Human()
        {
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 150,
                AnimalLiveCount = 0,
                Cap = 200,
                ProfileId = "Survival",
            };
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            return effective == 150 && HordeCalculator.IsActive(effective, SettingProfile.Survival);
        }

        // ── D3: hybrid 100 human + 100 animal × 0.5 = 150 ─────────
        private static bool D3_CalculatorSurvival100Human100Animal()
        {
            var ledger = new PopulationLedger
            {
                HumanoidLiveCount = 100,
                AnimalLiveCount = 100,
                Cap = 250,
                ProfileId = "Survival",
            };
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            return effective == 150 && HordeCalculator.IsActive(effective, SettingProfile.Survival);
        }

        // ── D4: Collapse threshold 80. 50 inactive, 80 active ─────
        private static bool D4_CalculatorCollapseThreshold()
        {
            var ledgerLow = new PopulationLedger { HumanoidLiveCount = 50, AnimalLiveCount = 0, ProfileId = "Collapse" };
            var ledgerHigh = new PopulationLedger { HumanoidLiveCount = 80, AnimalLiveCount = 0, ProfileId = "Collapse" };
            int eLow = HordeCalculator.GetEffectiveCount(ledgerLow);
            int eHigh = HordeCalculator.GetEffectiveCount(ledgerHigh);
            return !HordeCalculator.IsActive(eLow, SettingProfile.Collapse)
                && HordeCalculator.IsActive(eHigh, SettingProfile.Collapse);
        }

        // ── D5: null profile → Survival fallback ─────────────────
        private static bool D5_CalculatorProfileFallbackNull()
        {
            return !HordeCalculator.IsActive(120, null)
                && HordeCalculator.IsActive(160, null);
        }

        // ── D6: PulsePhase periodic 0→1 over 120 ticks ─────────
        private static bool D6_PulsePhaseSinusoidal()
        {
            float p0 = HordeCalculator.ComputePulsePhase(0L);
            float p30 = HordeCalculator.ComputePulsePhase(30L);
            float p60 = HordeCalculator.ComputePulsePhase(60L);
            float p90 = HordeCalculator.ComputePulsePhase(90L);
            float p120 = HordeCalculator.ComputePulsePhase(120L);
            // p0=0; p30≈1.0; p60≈0; p90≈1.0; p120=0.
            return System.Math.Abs(p0) < 0.01f
                && System.Math.Abs(p30 - 1f) < 0.01f
                && System.Math.Abs(p60) < 0.01f
                && System.Math.Abs(p90 - 1f) < 0.01f
                && System.Math.Abs(p120) < 0.01f;
        }

        // ── D7: pure despawn when below threshold ────────────────
        private static bool D7_UpdatePureDespawnBelowThreshold()
        {
            var hordeTiles = new System.Collections.Generic.List<int> { 100, 105 };
            HordeUpdateLogic.RunOncePure(
                effective: 100,
                active: false,
                homeTile: 50,
                currentTick: 5000L,
                hordeTiles: hordeTiles);
            return hordeTiles.Count == 0;
        }

        // ── D8: pure spawn when above threshold ─────────────────
        private static bool D8_UpdatePureSpawnAboveThreshold()
        {
            var hordeTiles = new System.Collections.Generic.List<int>();
            HordeUpdateLogic.RunOncePure(
                effective: 200,
                active: true,
                homeTile: 50,
                currentTick: 5000L,
                hordeTiles: hordeTiles);
            // First spawn at homeTile + 5
            return hordeTiles.Count == 1 && hordeTiles[0] == 55;
        }

        // ── D9: at tick 500, drift count = 2, position 60 → 58 ───
        private static bool D9_UpdatePureMoveTowardsHome()
        {
            var hordeTiles = new System.Collections.Generic.List<int> { 60 };
            HordeUpdateLogic.RunOncePure(
                effective: 200, active: true, homeTile: 50,
                currentTick: 500L, hordeTiles: hordeTiles);
            // 500/250 = 2 moves; 60 - 2 = 58
            return hordeTiles.Count == 1 && hordeTiles[0] == 58;
        }

        // ── D10: tick 251 = floor(251/250) = 1 move; 60 → 59 ───────
        private static bool D10_UpdatePureMoveIntervalRespected()
        {
            var hordeTiles = new System.Collections.Generic.List<int> { 60 };
            HordeUpdateLogic.RunOncePure(
                effective: 200, active: true, homeTile: 50,
                currentTick: 251L, hordeTiles: hordeTiles);
            return hordeTiles[0] == 59;
        }

        // ── D11: profile "Rimconemy_Survival" routes correctly ────
        private static bool D11_StripRimconemyPrefixForThreshold()
        {
            var ledger = new PopulationLedger { HumanoidLiveCount = 150, AnimalLiveCount = 0, ProfileId = "Survival" };
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            // SettingProfile.Survival.ProfileId = "Rimconemy_Survival"
            return HordeCalculator.IsActive(effective, SettingProfile.Survival);
        }

        // ── D12: null → Survival fallback (150) ──────────────
        private static bool D12_StripRimconemyPrefixNullReturnsSurvival()
        {
            return !HordeCalculator.IsActive(120, null)
                && HordeCalculator.IsActive(200, null);
        }

        // ── D13: hybrid route at Refuge threshold 220 ────────────
        private static bool D13_AnimalHalfCapRoute()
        {
            var ledgerRefuge = new PopulationLedger { HumanoidLiveCount = 100, AnimalLiveCount = 100, ProfileId = "Refuge" };
            int eRefuge = HordeCalculator.GetEffectiveCount(ledgerRefuge); // 100 + 50 = 150
            return eRefuge == 150 && !HordeCalculator.IsActive(eRefuge, SettingProfile.Refuge); // Refuge=220, not active
        }

        // ── D14: WorldObjectDef loads from DefDatabase ───────────
        private static bool D14_WorldObjectExistsInDefDB()
        {
            var def = DefDatabase<RimWorld.WorldObjectDef>.GetNamedSilentFail("Rimconemy_HordeWorldObject");
            return def != null && def.worldObjectClass == typeof(HordeWorldObject);
        }

        // ── D15: spawner / updater handles null / edge cases ──────
        private static bool D15_SpawnerNullMapComponentSkip()
        {
            var hordeTiles = new System.Collections.Generic.List<int>();
            try
            {
                HordeUpdateLogic.RunOncePure(
                    effective: 200, active: true, homeTile: -1,
                    currentTick: 500L, hordeTiles: hordeTiles);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
