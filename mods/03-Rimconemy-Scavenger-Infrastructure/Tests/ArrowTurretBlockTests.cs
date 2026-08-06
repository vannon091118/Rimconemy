using System;
using Rimconemy.ScavengerInfrastructure.Building;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Tests
{
    /// <summary>
    /// Phase-3.6 (2026-08-04): Tests für <see cref="ArrowTurretPowerGate.ApplyBlockedStatus"/>.
    ///
    /// Owner: Scavenger Infrastructure (Package 03). Sole-Owner per
    /// INTERFACE_CONTRACT §9.1.
    ///
    /// Tests sind duck-typed: <see cref="MockTurretForReflectionTest"/>
    /// deklariert die Field-Namen, die RimWorld <c>Building_Turret</c>
    /// trägt, mit den gleichen Namen. So funktioniert die Reflection-Hilfs-
    /// Methode auch ohne echte Map / echtes RimWorld-Runtime.
    ///
    /// Production-Pfad verlangt weiterhin echte <c>Building_Turret</c> mit
    /// gültiger CompPowerTrader-Comp. In-Process-Tests über Seams + Mock.
    /// </summary>
    public static class ArrowTurretBlockTests
    {
        public const int ExpectedPassCount = 7;

        /// <summary>
        /// Mock-Duck-Typed Turret. Hat exakt die Field-Namen die
        /// <see cref="ArrowTurretPowerGate.TryResetCurrentTargetViaReflection"/>
        /// und <see cref="ArrowTurretPowerGate.TryResetBurstCooldownViaReflection"/>
        /// scannen, so dass Reflection ohne echte Building_Turret-Instanz
        /// arbeiten kann.
        /// </summary>
        public class MockTurretForReflectionTest
        {
            /// <summary>Simuliert den Power-Comp "is on"-Zustand.</summary>
            public bool PowerOn = true;

            /// <summary>Simuliert aktuelles Ziel (object — entspricht IAttackTarget).</summary>
            public object currentTarget = new object();

            /// <summary>Simuliert Burst-Cooldown-Ticks-Left.</summary>
            public int burstCooldownTicksLeft = 60;
        }

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
                Log.Warning(
                    "[Rimconemy.ScavengerInfrastructure] ArrowTurretBlock test FAILED: " +
                    name + (detail == null ? "" : " — " + detail));
            }

            try
            {
                Check(TestNullTurretClassifyNoTurret(),            "T1.NullTurret");
                Check(TestIsBlockableGateState(),                  "T2.BlockableStates");
                Check(TestApplyCounterIncrements(),                "T3.ApplyCounter");
                Check(TestApplyOnNullNoOp(),                       "T4.NullApplyNoop");
                Check(TestApplyReflectionResetOverrideRecorded(), "T5.ResetOverrideRecorded");
                Check(TestApplyReflectionBurstOverrideRecorded(),  "T6.BurstOverrideRecorded");
                Check(TestDuckTypedReflectionResetsMock(),         "T7.DuckTypedResetOnMock");
            }
            finally
            {
                ArrowTurretPowerGate.ResetTestSeams();
            }

            Log.Message(
                "[Rimconemy.ScavengerInfrastructure] ArrowTurretBlock tests: " +
                passed + " passed, " + failed + " failed (min=" + ExpectedPassCount + ")." +
                (firstFailure == null ? "" : " First failure: " + firstFailure));
            return failed;
        }

        // ── T1 ─────────────────────────────────────────────────────────
        // ClassifyState(null) → NoTurret + "turret-null" ReasonCode.
        public static bool TestNullTurretClassifyNoTurret()
        {
            try
            {
                ArrowTurretPowerGate.ResetTestSeams();
                var report = ArrowTurretPowerGate.ClassifyState(null);
                return report.State == ArrowTurretPowerGate.GateState.NoTurret
                    && report.ReasonCode == "turret-null";
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod03A] test caught: " + ex); return false; }
        }

        // ── T2 ─────────────────────────────────────────────────────────
        // IsBlockableGateState ist true nur für Blocked und Offline.
        public static bool TestIsBlockableGateState()
        {
            try
            {
                ArrowTurretPowerGate.ResetTestSeams();
                if (!ArrowTurretPowerGate.IsBlockableGateState(ArrowTurretPowerGate.GateState.Blocked)) return false;
                if (!ArrowTurretPowerGate.IsBlockableGateState(ArrowTurretPowerGate.GateState.Offline)) return false;
                if (ArrowTurretPowerGate.IsBlockableGateState(ArrowTurretPowerGate.GateState.Active))   return false;
                if (ArrowTurretPowerGate.IsBlockableGateState(ArrowTurretPowerGate.GateState.NoTurret)) return false;
                if (ArrowTurretPowerGate.IsBlockableGateState(ArrowTurretPowerGate.GateState.Damaged))  return false;
                return true;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod03A] test caught: " + ex); return false; }
        }

        // ── T3 ─────────────────────────────────────────────────────────
        // Counter wird bei jedem Apply-Aufruf inkrementiert.
        public static bool TestApplyCounterIncrements()
        {
            try
            {
                ArrowTurretPowerGate.ResetTestSeams();
                int before = ArrowTurretPowerGate.ApplyAttempts;

                for (int i = 0; i < 5; i++)
                {
                    var _ = ArrowTurretPowerGate.ApplyBlockedStatus(null);
                }

                int delta = ArrowTurretPowerGate.ApplyAttempts - before;
                return delta == 5;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod03A] test caught: " + ex); return false; }
        }

        // ── T4 ─────────────────────────────────────────────────────────
        // ApplyBlockedStatus(null) blockt früh, ohne zu mutieren.
        public static bool TestApplyOnNullNoOp()
        {
            try
            {
                ArrowTurretPowerGate.ResetTestSeams();
                var apply = ArrowTurretPowerGate.ApplyBlockedStatus(null);

                // Null-Path → PreviousState == NoTurret, Not-Applied, ReasonBlocked != null.
                return !apply.Applied
                    && apply.PreviousState == ArrowTurretPowerGate.GateState.NoTurret
                    && apply.ReasonBlocked != null
                    && apply.ReasonCode == "turret-null";
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod03A] test caught: " + ex); return false; }
        }

        // ── T5 ─────────────────────────────────────────────────────────
        // Override-Seam: ResetTargetOverride liefert (true, reason)
        // → TryResetCurrentTargetViaReflection propagiert Result.
        public static bool TestApplyReflectionResetOverrideRecorded()
        {
            try
            {
                ArrowTurretPowerGate.ResetTestSeams();
                ArrowTurretPowerGate.ResetTargetOverride =
                    (turretLike) => (true, "mock-reason-42");

                string reason;
                bool ok = ArrowTurretPowerGate.TryResetCurrentTargetViaReflection(
                    new MockTurretForReflectionTest(), out reason);

                return ok
                    && reason != null
                    && reason.Contains("override: mock-reason-42");
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod03A] test caught: " + ex); return false; }
        }

        // ── T6 ─────────────────────────────────────────────────────────
        // Override-Seam: BurstCooldownOverride liefert (true, before=X)
        // → TryResetBurstCooldownViaReflection propagiert Burst-Cooldown-Wert.
        public static bool TestApplyReflectionBurstOverrideRecorded()
        {
            try
            {
                ArrowTurretPowerGate.ResetTestSeams();
                ArrowTurretPowerGate.BurstCooldownOverride =
                    (turretLike) =>
                    {
                        // Mutate the mock-burst so der Effekt "sichtbar" wird
                        // (das ist ein side-effect, der zeigt, dass der Override-Pfad
                        // die richtige Lambda war).
                        if (turretLike is MockTurretForReflectionTest m)
                            m.burstCooldownTicksLeft = 0;
                        return (true, 99);
                    };

                var mock = new MockTurretForReflectionTest();
                int before = -1;
                bool ok = ArrowTurretPowerGate.TryResetBurstCooldownViaReflection(mock, out before);

                return ok && before == 99 && mock.burstCooldownTicksLeft == 0;
            }
            catch (System.Exception ex) { Log.Error("[Rimconemy.Mod03A] test caught: " + ex); return false; }
        }

        // ── T7 ─────────────────────────────────────────────────────────
        // Duck-Typed Reflection auf Mock: kein Override gesetzt, Reflection
        // findet den "currentTarget"-Field automatisch. Beweist dass der
        // Production-Reflection-Pfad auf jedem duck-typed Trägerobjekt
        // funktioniert (also auch auf building_turret-ähnlichen Klassen).
        public static bool TestDuckTypedReflectionResetsMock()
        {
            try
            {
                ArrowTurretPowerGate.ResetTestSeams();
                var mock = new MockTurretForReflectionTest();
                // currentTarget != null vor Reset
                if (mock.currentTarget == null) return false;

                string reason;
                bool ok = ArrowTurretPowerGate.TryResetCurrentTargetViaReflection(mock, out reason);

                return ok
                    && mock.currentTarget == null
                    && reason != null
                    && reason.Contains("field-cleared:currentTarget");
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.ScavengerInfrastructure] T7 exception: " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
