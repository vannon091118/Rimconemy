// Tests/HordeRegressionTests.cs
//
// Phase D — Horde-Overlay Visualisierung (D1-D15).
// spec: docs/superpowers/specs/2026-08-05-horde-overlay-design.md
// plan: docs/superpowers/plans/2026-08-05-horde-overlay.md
//
// Owner: Infected & Automation (Package 05).
//
// Calculator-side tests cover Pure-Logic only.
// UpdateLogic tests cover the pure tick-derived tile (spawn / drift /
// arrival). Despawn is the Spawner's IsActive gate (covered by D1-D5).
// D11-D12 verify the hybrid-count route and the WorldObject Def load.
// D13-D15 verify the Spawner's layer-regen driver contract: the 15-tick
// cadence samples the 120-tick pulse with >=4 non-aliased samples, the
// pure decision fires only while active + due on SectionLayer subclasses,
// and D15 drives the actual fire path with a counting sink to prove
// RegenerateLayerNow is requested for both layer types per fire.

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

            // ── D13-D15: Spawner regen-driver contract ────────
            Check(D13_SpawnerCadenceSamplesPulse(),                  "D13.SpawnerCadenceSamplesPulse");
            Check(D14_SpawnerRegenOnlyWhileActive(),                 "D14.SpawnerRegenOnlyWhileActive");
            Check(D15_SpawnerActuallyFiresRegen(),                   "D15.SpawnerActuallyFiresRegen");

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

        // ── D13: 15-tick cadence divides the 120-tick pulse with ≥4
        //         samples and samples the alpha without aliasing ────────
        // The 60-tick trap: sampling |sin(θ)| at θ and θ+π yields equal
        // alphas → frozen pulse. 15 must give >= 4 samples per 120-tick
        // cycle AND the sampled phases must not all be equal (aliased).
        private static bool D13_SpawnerCadenceSamplesPulse()
        {
            int cadence = HordeSpawner.LayerRegenIntervalTicks;
            if (cadence <= 0 || HordeCalculator.PulseCycleTicks % cadence != 0) return false;
            int samplesPerCycle = HordeCalculator.PulseCycleTicks / cadence;
            if (samplesPerCycle < 4) return false;

            // Aliasing guard: the phase sequence sampled at cadence
            // offsets must contain more than one distinct value across a
            // full cycle, otherwise the pulse renders frozen.
            var seen = new System.Collections.Generic.HashSet<float>();
            for (int t = 0; t < HordeCalculator.PulseCycleTicks; t += cadence)
                seen.Add(HordeCalculator.ComputePulsePhase(t));
            return seen.Count > 1;
        }

        // ── D14: regen fires only while active + due; both driven
        //         layers are SectionLayer subclasses (RegenerateLayerNow
        //         contract) ────────────────────────────────────────
        private static bool D14_SpawnerRegenOnlyWhileActive()
        {
            // Inactive → never fires, even when due.
            if (HordeSpawner.ShouldRegenerateLayerNow(now: 100, nextLayerRegenTick: 0, activeNow: false)) return false;
            // Active but not yet due → no fire.
            if (HordeSpawner.ShouldRegenerateLayerNow(now: 10, nextLayerRegenTick: 15, activeNow: true)) return false;
            // Active + due → fires.
            if (!HordeSpawner.ShouldRegenerateLayerNow(now: 15, nextLayerRegenTick: 15, activeNow: true)) return false;
            // Exactly-on-boundary: next == now fires (MapComponentTick
            // resets _nextLayerRegenTick = now + cadence after firing).
            if (!HordeSpawner.ShouldRegenerateLayerNow(now: 30, nextLayerRegenTick: 30, activeNow: true)) return false;

            // Both layers the driver forces must be SectionLayer types so
            // MapDrawer.RegenerateLayerNow(Type) can regenerate them.
            return typeof(HordeSectionLayer).IsSubclassOf(typeof(SectionLayer))
                && typeof(HordeBurstLayer).IsSubclassOf(typeof(SectionLayer));
        }

        // ── D15: the driver ACTUALLY fires RegenerateLayerNow for both
        //         layers on the 15-tick cadence, only while active ────────
        // Drives HordeSpawner.DriveLayerRegen across a simulated tick
        // window with a counting sink (no live game needed). Active +
        // due → exactly one request per layer per fire; inactive → zero
        // requests even when due.
        private static bool D15_SpawnerActuallyFiresRegen()
        {
            var activeRequests = new System.Collections.Generic.List<System.Type>();
            int next = 0;

            // Simulate ticks 0..45 in 15-tick steps (the cadence).
            for (int tick = 0; tick <= 45; tick += 15)
                HordeSpawner.DriveLayerRegen(
                    now: tick, ref next, activeNow: true,
                    requestLayer: activeRequests.Add);

            // 4 fires × 2 layers = 8 requests, one of each type per fire.
            if (activeRequests.Count != 8) return false;
            for (int i = 0; i < 4; i++)
            {
                if (activeRequests[i * 2] != typeof(HordeSectionLayer)) return false;
                if (activeRequests[i * 2 + 1] != typeof(HordeBurstLayer)) return false;
            }

            // Inactive → nothing fires even when due.
            var inactiveRequests = new System.Collections.Generic.List<System.Type>();
            int inactiveNext = 0;
            for (int tick = 0; tick <= 45; tick += 15)
                HordeSpawner.DriveLayerRegen(
                    now: tick, ref inactiveNext, activeNow: false,
                    requestLayer: inactiveRequests.Add);
            if (inactiveRequests.Count != 0) return false;

            // Active but not yet due → no fire (sink untouched).
            var notDueRequests = new System.Collections.Generic.List<System.Type>();
            int notDueNext = 30;
            HordeSpawner.DriveLayerRegen(
                now: 20, ref notDueNext, activeNow: true,
                requestLayer: notDueRequests.Add);
            return notDueRequests.Count == 0;
        }
    }
}
