using Verse;

namespace Rimconemy.InfectedAutomation
{
    /// <summary>
    /// Owner: Infected &amp; Automation.
    /// Standalone startup marker for Package 05 scaffold.
    ///
    /// Hook reason: StaticConstructorOnStartup binds before any map loads.
    /// Threat aggregator, infected raid provider, mechadroid units and
    /// automation jobs are exposed as record types and event stubs.
    /// Vanilla Storyteller/Wealth-Raids remain authoritative until A3
    /// validates an explicit IncidentDef/IncidentWorker wiring.
    ///
    /// Compile references (resolved at build time): Rimconemy.Foundation (01),
    /// Rimconemy.ScavengerInfrastructure (03). Survival (02) is reached via the
    /// Foundation servicebus. Economy (04) is reached via the late-bound
    /// reflection bridge in Foundation.CrossPackageState (audit-bundle B / F-01).
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            Log.Message("[Rimconemy.InfectedAutomation] Standalone bootstrap starting...");
            Log.Message("[Rimconemy.InfectedAutomation] Faction, PawnKind, Incident and Mechadroid defs registered (one provider per Full Profile).");
            Log.Message("[Rimconemy.InfectedAutomation] Vanilla storyteller and Wealth-Raids remain authoritative while API-INCIDENT-01 / API-MECH-01 stay UNVERIFIED.");

            var _threatLog = Threat.ThreatAggregator.LogMarker;
            // IncidentStub / MechadroidUnit removed 2026-08-05 (dead-code audit,
            // Sprint A): the real raid path is InfectedRaidWorker; mechadroid
            // jobs live in MechadroidJobs (MechadroidJobLedger).
            Log.Message(
                "[Rimconemy.InfectedAutomation] Domain markers ready: threat=" + _threatLog +
                ", raid=InfectedRaidWorker, mechadroid=MechadroidJobLedger");

            // P2/H3 §2: CollectiveDefense tracker is a GameComponent subclass -
            // RimWorld auto-registers any GameComponent subclass on mod load.
            // We log a one-shot marker so the operator sees the ThoughtDef
            // registration succeeded before the post-combat patch takes over.
            Log.Message(
                "[Rimconemy.InfectedAutomation] CollectiveDefense setting rule (H3 §2): " +
                "thoughts=" + (Ideology.ThoughtDefs_CollectiveDefense.ValiantDefense != null) +
                ", tracker=GameComponent-auto-registry");

            // Run self-tests at startup (determinism, idempotency, profiles, RNG)
            Tests.StorySelectorTests.RunAll();
            Tests.StoryStateRegressionTests.RunAll();
            Tests.StoryStateSchemaBumpTests.RunAll();
            Tests.BuildingThreatRegressionTests.RunAll();
            Tests.MechadroidJobRegressionTests.RunAll();
            // Phase-1.4 (2026-08-04): deterministic dedup for the starter-infected spawn.
            Tests.StartEnemiesRegressionTests.RunAll();
            // P2/H3 §2 (Setting Rule CollectiveDefense): regression for ThoughtDef
            // shape, tracker aggregation, and scribe-roundtrip invariants.
            Tests.CollectiveDefenseRegressionTests.RunAll();
            // P2/H3 §3 (Setting Rule Transparency): regression for ThoughtDef
            // shape, cumulative-stage mood chart, and tracker counters.
            Tests.TransparencyRegressionTests.RunAll();
            // P5 Vanilla-/DLC-Incident-Klassifikation: prefix detection +
            // one-Infected-Provider invariant validator.
            Tests.IncidentClassifierRegressionTests.RunAll();
            // P6/F-6 (2026-08-04) — ThreatSnapshotBridge single-source read.
            // Both InfectedRaidSpawnService and WorldRaidCoordinator route
            // through ThreatSnapshotBridge.GetLatest(). The regression suite
            // covers the defensive null path with no Current.Game loaded.
            Tests.ThreatSnapshotBridgeRegressionTests.RunAll();
            // Audit-Bündel C / F-13 (2026-08-04) — FIFO edge-trigger queue
            // for game-over pendings (replaces single-pending tuple).
            Tests.GameOverPendingQueueRegressionTests.RunAll();
            Log.Message("[Rimconemy.InfectedAutomation] Building threat adapter available; Mechadroid job contracts are gated for Milestone B; no incident or raid is spawned.");
        }
    }
}
