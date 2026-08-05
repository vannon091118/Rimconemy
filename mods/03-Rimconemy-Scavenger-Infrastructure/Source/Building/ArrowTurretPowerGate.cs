using System;
using System.Reflection;
using Rimconemy.ScavengerInfrastructure.Power;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Building
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03).
    /// P6 — Task 11: Pfeilturm (Strom als harte Bedingung).
    ///
    /// Phase-3.6 (2026-08-04): Erweitert <c>ApplyBlockedStatus</c> als echte
    /// Mutation: bei GateState in (Blocked, Offline) wird der Turret vanilla-natural
    /// auf "no fire" gesetzt (CompPowerTrader.PowerOn = false) und via Reflection
    /// auf <c>Building_Turret</c>-Privatfelder ein laufender Fire-Loop hart beendet.
    ///
    /// Hook-Architektur (vanilla-natural + Reflection, KEIN Harmony-Transpiler):
    ///   1. Vanilla-Path: <c>turret.GetComp&lt;CompPowerTrader&gt;().PowerOn = false</c>
    ///      — Vanilla-Standardpfad, Arrow Turret-Tick respektiert das.
    ///   2. Reflection-Path: <c>ResetCurrentTarget()</c> und <c>burstCooldownTicksLeft=0</c>
    ///      werden via System.Reflection aufgerufen/gesetzt. Sorgt für Hard-Stop
    ///      auch wenn gerade ein Verb aktiv ist.
    ///   3. Test-Seams (alle default-null): Doppelpfad-Override für Reflection-Aufrufe
    ///      und Pipeline-Counter. Production-Pfad strikt wenn Seams null.
    ///
    /// Spec: docs/P6-PROGRESS.md Task 11, Phase-3.6 BLOCK_APPLY Erweiterung.
    /// </summary>
    public static class ArrowTurretPowerGate
    {
        public enum GateState
        {
            NoTurret,
            Active,
            Blocked,
            Offline,
            Damaged,
        }

        public struct GateReport
        {
            public GateState State;
            public string ReasonCode;
        }

        /// <summary>
        /// Result-Container für <see cref="ApplyBlockedStatus"/>. Belegt die
        /// tatsächliche Mutation am Turret.
        /// </summary>
        public struct GateApplyResult
        {
            /// <summary>State vor der Mutation (Kopie von <see cref="ClassifyState"/>).</summary>
            public GateState PreviousState;

            /// <summary>State nach der Mutation. Wird <see cref="Blocked"/> oder <see cref="Offline"/> sein.</summary>
            public GateState State;

            /// <summary>True wenn eine Mutation am Turret durchgeführt wurde.</summary>
            public bool Applied;

            /// <summary>True wenn CompPowerTrader.PowerOn erfolgreich auf false gesetzt wurde.</summary>
            public bool PowerOffSucceeded;

            /// <summary>True wenn Reflection-ResetCurrentTarget durchgelaufen ist.</summary>
            public bool ResetTargetSucceeded;

            /// <summary>Burst-Cooldown-Wert vor dem Reflection-Reset.</summary>
            public int BurstCooldownBefore;

            /// <summary>True wenn Burst-Cooldown via Reflection auf 0 gesetzt wurde.</summary>
            public bool BurstResetSucceeded;

            /// <summary>ReasonCode von ClassifyState (z.B. "below-30pct").</summary>
            public string ReasonCode;

            /// <summary>Wenn != null: ApplyBlockedStatus hat früh abgebrochen.</summary>
            public string ReasonBlocked;

            /// <summary>Reflection-spezifischer Diagnose-String. Nützlich für Tests.</summary>
            public string ReflectionReason;
        }

        // ── Test-Seams ─────────────────────────────
        // Default-Werte: null = Produktivverhalten (Reflection via System.Reflection).

        /// <summary>
        /// Optional Test-Hook: wenn != null, ersetzt dies den Reflection-Aufruf
        /// für <c>ResetCurrentTarget</c>. Tests können damit deterministisch
        /// ohne echten <c>Building_Turret</c> verifizieren, dass der Aufruf versucht wird.
        /// </summary>
        public static Func<object, (bool ok, string reason)> ResetTargetOverride = null;

        /// <summary>
        /// Optional Test-Hook: wenn != null, ersetzt dies das Reflection-Pendant
        /// für Burst-Cooldown-Read-Reset. Tests können damit den Wert vor/nach
        /// simulieren ohne echte RimWorld-Felder.
        /// </summary>
        public static Func<object, (bool ok, int before)> BurstCooldownOverride = null;

        /// <summary>
        /// Counter: wird bei jedem ApplyBlockedStatus-Aufruf inkrementiert,
        /// auch bei frühem Block (Null/NoTurret/Non-Blockable). Tests resetten
        /// und lesen den Counter; semantisch ist das eine Call-Counter, nicht
        /// ein Success-Counter.
        /// </summary>
        public static int ApplyAttempts = 0;

        /// <summary>
        /// Cleanup-Methode für Tests: setzt alle Seams auf Default zurück.
        /// </summary>
        public static void ResetTestSeams()
        {
            ResetTargetOverride = null;
            BurstCooldownOverride = null;
            ApplyAttempts = 0;
        }

        public static GateReport ClassifyState(Building_Turret turret)
        {
            var report = new GateReport { State = GateState.NoTurret };
            if (turret == null)
            {
                report.ReasonCode = "turret-null";
                return report;
            }

            // Damage check: turret HP below 30% → Damaged state.
            float hits = turret.MaxHitPoints > 0
                ? (float)turret.HitPoints / turret.MaxHitPoints
                : 1f;
            if (hits < 0.30f)
            {
                report.State = GateState.Damaged;
                report.ReasonCode = "below-30pct";
                return report;
            }

            // Power requirement: turret must expose a powered CompPowerTrader.
            var powerComp = turret.GetComp<CompPowerTrader>();
            if (powerComp == null)
            {
                report.State = GateState.Offline;
                report.ReasonCode = "no-power-comp";
                return report;
            }

            // Power-chain service is the source of truth for grid state.
            var chainState = PowerChainService.GetChainSnapshot(Find.TickManager?.TicksGame ?? 0L);
            bool chainOnline = chainState.ActiveGenerators > 0
                || chainState.HasSolidFuel
                || chainState.HasLiquidFuel
                || chainState.HasWaterPump;
            if (!chainOnline)
            {
                report.State = GateState.Offline;
                report.ReasonCode = chainState.ContentHash; // diagnostic check
                return report;
            }

            // Blocked reason from enemy threats / blockers.
            report.State = GateState.Active;
            report.ReasonCode = "online";
            return report;
        }

        /// <summary>
        /// Phasenkonsistenz: GateState ist "blockbar" (= wir dürfen eine
        /// Fire-Pause erzwingen), wenn der Zustand <see cref="Blocked"/>
        /// oder <see cref="Offline"/> ist. Andere Zustände sind entweder
        /// nicht-anwendbar (NoTurret, Damaged) oder bereits feuernd (Active).
        /// </summary>
        public static bool IsBlockableGateState(GateState state)
        {
            return state == GateState.Blocked || state == GateState.Offline;
        }

        /// <summary>
        /// Wendet die harte Feuer-Pause auf einen Turret an, dessen GateState
        /// blockbar ist. Setzt <c>CompPowerTrader.PowerOn = false</c> (vanilla-natural)
        /// und ruft via Reflection auf <c>ResetCurrentTarget()</c> und
        /// <c>burstCooldownTicksLeft = 0</c> (Hard-Stop).
        ///
        /// Sequence:
        ///   1. <see cref="ClassifyState"/> → Vorzustand
        ///   2. Frühausstieg wenn Zustand nicht blockbar (NoTurret/Damaged/Active)
        ///   3. PowerOff via <c>CompPowerTrader</c> (vanilla-natural)
        ///   4. Reflection: ResetCurrentTarget (Hard-Stop)
        ///   5. Reflection: burstCooldownTicksLeft = 0 (Hard-Stop)
        ///   6. ApplyAttempts++ und Result-Befüllung
        /// </summary>
        public static GateApplyResult ApplyBlockedStatus(Building_Turret turret)
        {
            ApplyAttempts += 1;

            var apply = new GateApplyResult
            {
                PreviousState = GateState.NoTurret,
                State = GateState.NoTurret,
                Applied = false,
                PowerOffSucceeded = false,
                ResetTargetSucceeded = false,
                BurstCooldownBefore = 0,
                BurstResetSucceeded = false,
                ReasonCode = null,
                ReasonBlocked = null,
                ReflectionReason = null,
            };

            try
            {
                var report = ClassifyState(turret);
                apply.PreviousState = report.State;
                apply.State = report.State;
                apply.ReasonCode = report.ReasonCode;

                if (turret == null)
                {
                    apply.ReasonBlocked = "turret-null";
                    return apply;
                }

                if (!IsBlockableGateState(report.State))
                {
                    apply.ReasonBlocked = "non-blockable-state: " + report.State;
                    return apply;
                }

                // 1) Vanilla-Path: PowerOff.
                var powerComp = turret.GetComp<CompPowerTrader>();
                if (powerComp != null)
                {
                    try
                    {
                        powerComp.PowerOn = false;
                        apply.PowerOffSucceeded = true;
                    }
                    catch (Exception ex)
                    {
                        apply.ReasonBlocked = "PowerOff-set failed: " + ex.GetType().Name;
                    }
                }
                else
                {
                    apply.PowerOffSucceeded = false;
                    apply.ReasonBlocked = (
                        apply.ReasonBlocked == null
                            ? "no-CompPowerTrader"
                            : apply.ReasonBlocked + ";no-CompPowerTrader");
                }

                // 2) Reflection-Path: ResetCurrentTarget
                var targetOk = TryResetCurrentTargetViaReflection(turret, out string resetReason);
                apply.ResetTargetSucceeded = targetOk;
                apply.ReflectionReason = resetReason;

                // 3) Reflection-Path: Burst-Cooldown-Reset
                var burstOk = TryResetBurstCooldownViaReflection(
                    turret, out int beforeBurst);
                apply.BurstCooldownBefore = beforeBurst;
                apply.BurstResetSucceeded = burstOk;

                apply.Applied = apply.PowerOffSucceeded
                    || apply.ResetTargetSucceeded
                    || apply.BurstResetSucceeded;

                return apply;
            }
            catch (Exception ex)
            {
                apply.ReasonBlocked = "Apply exception: " + ex.GetType().Name;
                return apply;
            }
        }

        /// <summary>
        /// Reflection-Wrapper für <c>Building_Turret.ResetCurrentTarget()</c>.
        /// </summary>
        public static bool TryResetCurrentTargetViaReflection(object turretLike, out string reason)
        {
            try
            {
                if (turretLike == null)
                {
                    reason = "turret-null";
                    return false;
                }

                // Test-Seam: Override komplett ersetzt Reflection.
                if (ResetTargetOverride != null)
                {
                    var (ok, why) = ResetTargetOverride(turretLike);
                    reason = "override: " + why;
                    return ok;
                }

                const BindingFlags flags =
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                var type = turretLike.GetType();

                // Vorzug 1: benannte Methode ResetCurrentTarget() falls vorhanden.
                var method = type.GetMethod("ResetCurrentTarget", flags);
                if (method != null)
                {
                    method.Invoke(turretLike, null);
                    reason = "method-invoked:ResetCurrentTarget";
                    return true;
                }

                // Vorzug 2: ResetCurrentTarget ohne Parameter via duck-typed Field
                // (z.B. private IAttackTarget currentTarget; .SetValue(null)).
                var field = type.GetField("currentTarget", flags);
                if (field != null)
                {
                    field.SetValue(turretLike, null);
                    reason = "field-cleared:currentTarget";
                    return true;
                }

                reason = "no-reset-method-or-field";
                return false;
            }
            catch (Exception ex)
            {
                reason = "exception: " + ex.GetType().Name;
                return false;
            }
        }

        /// <summary>
        /// Reflection-Wrapper für Burst-Cooldown-Reset:
        /// sucht <c>burstCooldownTicksLeft</c> und setzt es auf 0.
        /// </summary>
        /// <remarks>
        /// Field-Not-Found ist erwartetes Runtime-Verhalten (RimWorld-Version-Drift);
        /// wird NICHT als Log.Warning geflutet. Stille Rückgabe (false) mit
        /// <paramref name="before"/> = 0 ist die ehrliche No-Op-Antwort; Audit
        /// dokumentiert den Reflection-Pfad als defensive best-effort.
        /// </remarks>
        public static bool TryResetBurstCooldownViaReflection(object turretLike, out int before)
        {
            before = 0;
            try
            {
                if (turretLike == null) return false;

                if (BurstCooldownOverride != null)
                {
                    var (ok, prev) = BurstCooldownOverride(turretLike);
                    before = prev;
                    return ok;
                }

                const BindingFlags flags =
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                var type = turretLike.GetType();
                var field = type.GetField("burstCooldownTicksLeft", flags);
                if (field == null) return false;

                int currentValue = 0;
                try
                {
                    currentValue = (int)field.GetValue(turretLike);
                }
                catch
                {
                    currentValue = 0;
                }
                before = currentValue;

                field.SetValue(turretLike, 0);
                return true;
            }
            catch (Exception ex)
            {
                // Field-Not-Found ist erwartet (RimWorld-Version-Drift etc.).
                Log.Warning("[Rimconemy.ScavengerInfrastructure] TryResetBurstCooldownViaReflection: " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
