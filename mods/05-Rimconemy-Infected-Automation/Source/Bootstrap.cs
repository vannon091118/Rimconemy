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
    /// No Foundation, Scavenger, Survival or Economy compile references.
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
            var _raidLog = Incidents.IncidentStub.LogMarker;
            var _mechLog = Mechadroids.MechadroidUnit.LogMarker;
            Log.Message($"[Rimconemy.InfectedAutomation] Domain stubs ready: threat={_threatLog}, raid={_raidLog}, mechadroid={_mechLog}");

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
            // P2/H3 §2 (Setting Rule CollectiveDefense): regression for ThoughtDef
            // shape, tracker aggregation, and scribe-roundtrip invariants.
            Tests.CollectiveDefenseRegressionTests.RunAll();
            // P2/H3 §3 (Setting Rule Transparency): regression for ThoughtDef
            // shape, cumulative-stage mood chart, and tracker counters.
            Tests.TransparencyRegressionTests.RunAll();
            // P5 Vanilla-/DLC-Incident-Klassifikation: prefix detection +
            // one-Infected-Provider invariant validator.
            Tests.IncidentClassifierRegressionTests.RunAll();
            Log.Message("[Rimconemy.InfectedAutomation] Building threat adapter available; Mechadroid job contracts are gated for Milestone B; no incident or raid is spawned.");
        }
    }
}
