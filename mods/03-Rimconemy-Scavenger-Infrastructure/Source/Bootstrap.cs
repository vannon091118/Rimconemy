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
    /// remains UNVERIFIED. Construction debris, hemp, water and steel scraps
    /// are exposed in Defs/ThingDefs/ and consumed through Vanilla Comp pipelines.
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
            Log.Message("[Rimconemy.ScavengerInfrastructure] Resources (ConstructionDebris, Hemp, Water, SteelScraps) registered as ThingDefs.");
            Log.Message("[Rimconemy.ScavengerInfrastructure] Campfire (Rimconemy_Campfire) registered: burns steel scraps → steel.");
            Log.Message("[Rimconemy.ScavengerInfrastructure] Wood removed from Wall/Door stuffCategories; walls require Stony or Metallic.");
            Log.Message("[Rimconemy.ScavengerInfrastructure] Power chain defs (Generator, ArrowTurret) wired via Vanilla PowerNet comps.");

            // Force subsystems to run their static ctors / register lines.
            int resCount = Rimconemy.ScavengerInfrastructure.Resources.ResourceCategory.All.Count;
            string powerMarker = Rimconemy.ScavengerInfrastructure.Power.PowerChainService.LogMarker;
            string plantsMarker = Rimconemy.ScavengerInfrastructure.Plants.PlantHelper.LogMarker;
            Log.Message(
                $"[Rimconemy.ScavengerInfrastructure] Domain stubs ready: " +
                $"resources={resCount}, power={powerMarker}, plants={plantsMarker}");

            // C-T2: PowerChainService capability-gated readout is wired.
            // Other packages can read GetChainSnapshot(tick) when the
            // rimconemy.scavengerinfrastructure.power capability is active.
            Log.Message("[Rimconemy.ScavengerInfrastructure] PowerChainService exposes live PowerChainSnapshot for capability-gated readers.");
            Tests.BuildingCoreRegressionTests.RunAll();
            // Phase 3 / H4 §4 (Caravan extension): sentinel encoding,
            // decoding and roundtrip, plus empty-snapshot guard.
            Tests.CaravanStorageRegressionTests.RunAll();
            // Phase-3.2 (2026-08-04): BauschuttRemapApply first real map-mutation in Paket 03.
            Tests.BauschuttRemapApplyTests.RunAll();
            // Phase-3.6 (2026-08-04): ArrowTurret ApplyBlockedStatus — vanilla-natural PowerOff + Reflection Hard-Stop.
            Tests.ArrowTurretBlockTests.RunAll();
            // Phase-3.11 (2026-08-04): Campfire/Scraps loop + Woody removal.
            Tests.CampfireScrapsRegressionTests.RunAll();
            // P0 Coal Chain (2026-08-04): MakeCoal, SalvageMachineParts, Generator 0.67, CraftingStations.
            Tests.CoalChainRegressionTests.RunAll();
            // P1 StainlessSteel Chain (2026-08-04): MakeStainlessSteel, StainlessSteelTower, Campfire 4th recipe.
            Tests.StainlessSteelChainRegressionTests.RunAll();
        }
    }
}
