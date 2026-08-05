// Tests/HordeRegressionTests.cs
//
// Phase D — Horde-Overlay Visualisierung (D1-D12).
// spec: docs/superpowers/specs/2026-08-05-horde-overlay-design.md
// plan: docs/superpowers/plans/2026-08-05-horde-overlay.md
//
// Owner: Infected & Automation (Package 05).
//
// Calculator-side tests cover Pure-Logic only.
// UpdateLogic tests cover the pure tick-derived tile (spawn / drift /
// arrival). Despawn is the Spawner's IsActive gate (covered by D1-D5).
// D11-D12 verify the hybrid-count route and the WorldObject Def load.

using Rimconemy.InfectedAutomation.Horde;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class HordeRegressionTests
    {
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

            // ── D7-D10: UpdateLogic Pure (tick-derived tile) ──────
            Check(D7_UpdatePureSpawnAtInitialDistance(),             "D7.UpdatePureSpawnAtInitialDistance");
            Check(D8_UpdatePureDriftsOnePerInterval(),               "D8.UpdatePureDriftsOnePerInterval");
            Check(D9_UpdatePureArrivesAndClampsAtHome(),             "D9.UpdatePureArrivesAndClampsAtHome");
            Check(D10_UpdatePureDeterministicFromTick(),             "D10.UpdatePureDeterministicFromTick");

            // ── D11-D12: hybrid route + Def load ─────────────
            Check(D11_AnimalHalfCapRoute(),                          "D11.AnimalHalfCapRoute");
            Check(D12_WorldObjectExistsInDefDB(),                    "D12.WorldObjectExistsInDefDB");

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

        // ── D5: null profile → Survival fallback (150) ─────────────
        private static bool D5_CalculatorProfileFallbackNull()
        {
            return !HordeCalculator.IsActive(120, null)
                && HordeCalculator.IsActive(160, null);
        }

        // ── D6: PulsePhase periodic 0→1 over 120 ticks ─────────────
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

        // ── D7: tick 0 → spawn at home + 5 ────────────────────
        private static bool D7_UpdatePureSpawnAtInitialDistance()
        {
            // Spec §6: tile = home + max(0, 5 − floor(tick/250)).
            return HordeUpdateLogic.ComputeHordeTile(homeTile: 50, currentTick: 0L) == 55;
        }

        // ── D8: floor(tick/250) moves, 1 tile per interval ──────
        private static bool D8_UpdatePureDriftsOnePerInterval()
        {
            // tick 249 → 0 moves (still 55); tick 250 → 1 move (54).
            return HordeUpdateLogic.ComputeHordeTile(50, 249L) == 55
                && HordeUpdateLogic.ComputeHordeTile(50, 250L) == 54
                && HordeUpdateLogic.ComputeHordeTile(50, 500L) == 53;
        }

        // ── D9: reaches home at tick 1250 and clamps, never below ──
        private static bool D9_UpdatePureArrivesAndClampsAtHome()
        {
            return HordeUpdateLogic.ComputeHordeTile(50, 1249L) == 51
                && HordeUpdateLogic.ComputeHordeTile(50, 1250L) == 50
                && HordeUpdateLogic.ComputeHordeTile(50, 100000L) == 50;
        }

        // ── D10: deterministic — same tick → same tile, no state ──
        private static bool D10_UpdatePureDeterministicFromTick()
        {
            // Pure function of (homeTile, tick): repeated calls agree and
            // a different home tile shifts the result by the same delta.
            return HordeUpdateLogic.ComputeHordeTile(50, 500L) == HordeUpdateLogic.ComputeHordeTile(50, 500L)
                && HordeUpdateLogic.ComputeHordeTile(7, 250L) == 11; // 7 + (5 − 1)
        }

        // ── D11: hybrid route at Refuge threshold 220 ────────────
        private static bool D11_AnimalHalfCapRoute()
        {
            var ledgerRefuge = new PopulationLedger { HumanoidLiveCount = 100, AnimalLiveCount = 100, ProfileId = "Refuge" };
            int eRefuge = HordeCalculator.GetEffectiveCount(ledgerRefuge); // 100 + 50 = 150
            return eRefuge == 150 && !HordeCalculator.IsActive(eRefuge, SettingProfile.Refuge); // Refuge=220, not active
        }

        // ── D12: WorldObjectDef loads from DefDatabase ───────────
        private static bool D12_WorldObjectExistsInDefDB()
        {
            var def = DefDatabase<RimWorld.WorldObjectDef>.GetNamedSilentFail("Rimconemy_HordeWorldObject");
            return def != null && def.worldObjectClass == typeof(HordeWorldObject);
        }
    }
}
