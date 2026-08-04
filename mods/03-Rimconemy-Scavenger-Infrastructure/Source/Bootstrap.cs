using Verse;

namespace Rimconemy.ScavengerInfrastructure
{
    /// <summary>
    /// Owner: Scavenger Infrastructure.
    /// Standalone startup marker for Package 03 scaffold.
    ///
    /// Hook reason: StaticConstructorOnStartup binds before any map loads.
    /// Resources, plants, water, fuel and turret status live as def-only
    /// scaffolding; no runtime mutators are introduced while API-POWER-01
    /// remains UNVERIFIED. Construction debris, hemp and water are exposed
    /// in Defs/ThingDefs/ and consumed through Vanilla Comp pipelines.
    ///
    /// Cross-package integration remains late-bound; no compile reference
    /// to Foundation, Survival, Economy or Infected.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            Log.Message("[Rimconemy.ScavengerInfrastructure] Standalone bootstrap starting...");
            Log.Message("[Rimconemy.ScavengerInfrastructure] Resources (ConstructionDebris, Hemp, Water) registered as ThingDefs.");
            Log.Message("[Rimconemy.ScavengerInfrastructure] Power chain defs (Generator, ArrowTurret) wired via Vanilla PowerNet comps.");

            // Force subsystems to run their static ctors / register lines.
            int resCount = Rimconemy.ScavengerInfrastructure.Resources.ResourceCategory.All.Count;
            string powerMarker = Rimconemy.ScavengerInfrastructure.Power.PowerChainStub.LogMarker;
            string plantsMarker = Rimconemy.ScavengerInfrastructure.Plants.PlantHelper.LogMarker;
            Log.Message(
                $"[Rimconemy.ScavengerInfrastructure] Domain stubs ready: " +
                $"resources={resCount}, power={powerMarker}, plants={plantsMarker}");

            // C-T2: PowerChainService capability-gated readout is wired.
            // Other packages can read GetChainSnapshot(tick) when the
            // rimconemy.scavengerinfrastructure.power capability is active.
            Log.Message("[Rimconemy.ScavengerInfrastructure] PowerChainService exposes live PowerChainSnapshot for capability-gated readers.");
            Tests.BuildingCoreRegressionTests.RunAll();
        }
    }
}
