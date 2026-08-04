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

            // Run self-tests at startup (determinism, idempotency, profiles, RNG)
            Tests.StorySelectorTests.RunAll();
            Tests.StoryStateRegressionTests.RunAll();
            Tests.BuildingThreatRegressionTests.RunAll();
            Tests.MechadroidJobRegressionTests.RunAll();
            Log.Message("[Rimconemy.InfectedAutomation] Building threat adapter available; Mechadroid job contracts are gated for Milestone B; no incident or raid is spawned.");
        }
    }
}
