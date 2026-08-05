// Source/Bootstrap.cs
//
// Owner: Infected & Automation.
// Standalone startup marker for Package 05 scaffold.
//
// Hook reason: StaticConstructorOnStartup binds before any map loads.
// Threat aggregator, infected raid provider, mechadroid units and
// automation jobs are exposed as record types and event stubs.
// Vanilla Storyteller/Wealth-Raids remain authoritative until A3
// validates an explicit IncidentDef/IncidentWorker wiring.
//
// Compile references (resolved at build time): Rimconemy.Foundation (01),
// Rimconemy.ScavengerInfrastructure (03). Survival (02) is reached via the
// Foundation servicebus. Economy (04) is reached via the late-bound
// reflection bridge in Foundation.CrossPackageState (audit-bundle B / F-01).
using Rimconemy.InfectedAutomation.Scenarios;
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
            World.DarknessSectionLayerLifecycle.Install();
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
            // The PostApplyDamage postfix must be installed explicitly (Package
            // 05 has no PatchAll; cf. DarknessSectionLayerLifecycle/HordeCameraOverlay).
            Ideology.Pawn_PostApplyDamage_CollectiveDefense.Install();
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
            // Sprint 1 (2026-08-05) — Perception infrastructure regression:
            // ChunkState Scribe roundtrip, LightSystem daylight curve,
            // NoiseSystem falloff, PerceptionMath formulas.
            Tests.Sprint1PerceptionRegressionTests.RunAll();
            // Sprint 2 (2026-08-05) — Infected behavior state machine:
            // Dormant→Roaming→Investigating→Assault transitions,
            // InfectedPawnState Scribe roundtrip, determinism verification.
            Tests.Sprint2BehaviorRegressionTests.RunAll();
            // Sprint 2.5 (2026-08-05) — Colonist sight cone / darkness overlay:
            // Directional vision, light-level-scaling, Project-Zomboid-style
            // dimming. MapComponent auto-registered; rendering via the
            // existing world-space SectionLayer_Darkness lifecycle.
            Tests.ColonistSightSystemRegressionTests.RunAll();
            Tests.DarknessSectionLayerRegressionTests.RunAll();
            // Phase A (2026-08-05) — Population-Ledger data layer. Pure-data,
            // no AI or Spawn yet; lays the SSOT before Phase B (Daily-Growth
            // tick integration) and Phase C (RandomInoculationService).
            Tests.PopulationProfileMultipliersRegressionTests.RunAll();
            Tests.PopulationLedgerRegressionTests.RunAll();
            // Phase C (2026-08-05) — Tier-Inokulation service + pack behavior.
            // The deterministic selector, branded KindDef
            // ("Rimconemy_InfectedWildlife"), and AnimalHalfCap accounting
            // must all pass before the StoryDirector Day-Tick fires its
            // first wild-animal conversion.
            Tests.InoculationRegressionTests.RunAll();
            Tests.InfectedPackBehaviorRegressionTests.RunAll();

            // Phase B (2026-08-05) — Daily-Growth + Revenge Coupling.
            // SpawnPlan.RevengeQuotaComponent, Worker post-spawn
            // DecrementPendingRevenge, StoryDirector.LastPendingRevenge
            // recompute, StoryEventCatalog.Revenge family.
            Tests.RevengeQuotaFlowRegressionTests.RunAll();

            // Phase D (2026-08-05) — Horde Overlay: World-Map wanderer +
            // SectionLayer-Kreis mittig + Per-Infected-Bursts + CameraEdge.
            Tests.HordeRegressionTests.RunAll();
            // Phase F (2026-08-05) — Wandering-Horde: Manifest + HiddenPawnStamp +
            // TravelTile-FSM + Reveal-Materialization + Reveal-Radius-Sync.
            Tests.HordeProfileMultipliersTests.RunAll();
            Tests.HordeManifestTests.RunAll();
            Tests.HordeMigrationDriverTests.RunAll();
            Tests.HordeMaterializationTests.RunAll();
            Horde.HordeCameraOverlay.Install();
            Log.Message("[Rimconemy.InfectedAutomation] Phase D+F: Horde overlay + migration wired (Calculator, WorldObject, Spawner, SectionLayer, BurstLayer, CameraEdge, Manifest, Driver, Materialization).");

            // Phase E (2026-08-05) — Tiersym-Infektion via Random Encounter.
            //   AnimalInfectionChance (Pure-Chance + Profile-Multipliers),
            //   PopulationLedger.LastAnimalInfectionTick / CountToday,
            //   RandomInoculationService.TryInfectWildAnimals.
            Tests.AnimalInfectionRegressionTests.RunAll();
            Tests.AnimalInfectionLedgerFieldsTests.RunAll();
            Tests.AnimalInfectionServiceLimitTests.RunAll();
            // Driver-Seam (AnimalInfectionDriver.TryFireOnce + ResetForTests)
            // and Overlay-Predikat (ShouldShowInfectionMarker) deterministisch
            // ohne RimWorld-Map-Setup testbar — schließt die Lücke aus
            // Falsification §G Anmerkung C-1.
            Tests.AnimalInfectionDriverTests.RunAll();
            Tests.AnimalInfectionAiOverlayTests.RunAll();
            Log.Message("[Rimconemy.InfectedAutomation] Phase E: AnimalInfection pipeline wired (Profile-Chance, Ledger, Service, Driver-Seam, Overlay-Predikat).");
            Log.Message("[Rimconemy.InfectedAutomation] Phase B: Daily-Growth+Reset+Revenge coupling wired.");
            Log.Message("[Rimconemy.InfectedAutomation] Building threat adapter available; Mechadroid job contracts are gated for Milestone B; no incident or raid is spawned.");

            // Phase-5 (2026-08-05) — IncidentClassifier summary log. Validates the
            // single Infected-Provider invariant and emits a per-Source breakdown.
            // Boundary: bootstrap runs after vanilla Defs are loaded so the count
            // is meaningful; full incident-classification report lives in the
            // IncidentClassifierRegressionTests (T1/T2).
            try
            {
                int rimconemy = 0, vanilla = 0, dlc = 0;
                var buckets = Incidents.IncidentClassifier.EnumerateAll();
                for (int i = 0; i < buckets.Count; i++)
                {
                    var b = buckets[i];
                    // IncidentBucket ist struct → kein null-Vergleich möglich.
                    // Stattdessen prüfen wir auf den Sentinel "leerer" DefName.
                    if (string.IsNullOrEmpty(b.DefName)) continue;
                    if (b.Source == Incidents.IncidentClassifier.IncidentSource.Rimconemy) rimconemy++;
                    else if (b.Source == Incidents.IncidentClassifier.IncidentSource.Vanilla) vanilla++;
                    else dlc++;
                }
                bool oneProvider = Incidents.IncidentClassifier.ValidateOneInfectedProvider();
                Log.Message("[Rimconemy.InfectedAutomation] IncidentClassifier: total="
                    + buckets.Count
                    + ", Rimconemy=" + rimconemy
                    + ", Vanilla=" + vanilla
                    + ", DLC/Quest=" + dlc
                    + ", InfectedProvider-1-of-1="
                    + (oneProvider ? "OK" : "VIOLATION")
                    + " (Phase-5 Klassifikation; ROADMAP §8.5)."
                    );
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] IncidentClassifier.Bootstrap-summary failed: " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
