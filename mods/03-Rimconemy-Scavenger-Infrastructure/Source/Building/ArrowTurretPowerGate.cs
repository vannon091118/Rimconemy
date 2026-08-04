using Rimconemy.ScavengerInfrastructure.Power;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Building
{
    /// <summary>
    /// Owner: Scavenger Infrastructure (Package 03).
    /// P6 — Task 11: Pfeilturm (Strom als harte Bedingung).
    ///
    /// Phase-6 Stub: classifies the operational state of an arrow turret
    /// based on its power + structural integrity. The mechanical binding
    /// between CompPowerTrader + turret integration is owned by User Live-Test.
    ///
    /// Spec: docs/P6-PROGRESS.md Task 11.
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
    }
}
