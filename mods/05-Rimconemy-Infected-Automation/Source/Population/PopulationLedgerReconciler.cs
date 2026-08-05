// Source/Population/PopulationLedgerReconciler.cs
//
// Owner: Infected & Automation (Package 05).
// Phase A — P6-PROGRESS §12 Reconciliation MapComponent.
//
// Tick-based reconciler: every 60 ticks, scan a map's spawned pawns for
// those that belong to the Hidden-Infected faction and partition them
// into Humanoid vs Animal counts in the PopulationLedger.
//
// The reconciler is implemented as a thin MapComponent that delegates
// the actual counting to the static `ReconciliationLogic` helper so the
// pure logic can be unit-tested without an actual `Map` or live
// RimWorld context.

using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Population
{
    public sealed class PopulationLedgerReconciler : MapComponent
    {
        private const int TickInterval = 60;
        private const string HiddenInfectedFactionDef = "Rimconemy_HiddenInfectedFaction";
        private int _lastTick = -TickInterval;

        public PopulationLedgerReconciler(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            // Defensive: skip during Scribe; if map was disposed before us,
            // Map.mapPawns is null. Pattern matches the existing
            // ChunkController MapComponent.
            if (map == null) return;
            if (Scribe.mode != LoadSaveMode.Inactive) return;

            int now = Find.TickManager?.TicksGame ?? 0;
            if (now < _lastTick + TickInterval) return;
            _lastTick = now;

            // Build the snapshot from map.mapPawns (the only RimWorld-state
            // boundary). ReconciliationLogic itself is pure.
            var snapshots = BuildSnapshots(map);
            int humanoid = 0;
            int animal = 0;
            foreach (var snap in snapshots)
            {
                if (snap.IsHiddenInfected && !snap.IsDead)
                {
                    if (snap.IsHumanlike) humanoid++;
                    else animal++;
                }
            }

            var ledger = PopulationLedger.Get();
            if (ledger == null) return;
            ReconciliationLogic.ApplyCounts(ledger, humanoid, animal);
        }

        /// <summary>
        /// Extract a list of <see cref="PawnSnapshot"/> from <paramref
        /// name="map"/>. Exposed (internal) for tests that want to drive
        /// the pure reconciler without instantiating Reconciler itself.
        /// </summary>
        internal static List<PawnSnapshot> BuildSnapshots(Map map)
        {
            var snapshots = new List<PawnSnapshot>();
            if (map?.mapPawns == null) return snapshots;
            var all = map.mapPawns.AllPawnsSpawned;
            if (all == null) return snapshots;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p == null) continue;
                bool humanLike = p.RaceProps != null && p.RaceProps.Humanlike;
                bool hiddenFaction = p.Faction != null && p.Faction.def != null
                    && p.Faction.def.defName == HiddenInfectedFactionDef;
                snapshots.Add(new PawnSnapshot
                {
                    IsHumanlike = humanLike,
                    IsAnimal = p.RaceProps != null && !humanLike,
                    IsHiddenInfected = hiddenFaction,
                    IsDead = p.Dead,
                });
            }
            return snapshots;
        }

        /// <summary>
        /// Static accessor for whatever orchestrator wants to drive the
        /// reconciler manually (e.g. tests, StoryDirector pre-spawn hook).
        /// </summary>
        public static PopulationLedgerReconciler Get(Map map)
        {
            return map?.GetComponent<PopulationLedgerReconciler>();
        }
    }

    /// <summary>
    /// Plain-data view of a Pawn, used by ReconciliationLogic so unit
    /// tests can drive the count pass without instantiating real RimWorld
    /// pawns or maintaining a live Map.
    /// </summary>
    public struct PawnSnapshot
    {
        public bool IsHumanlike;
        public bool IsAnimal;
        public bool IsHiddenInfected;
        public bool IsDead;
    }

    /// <summary>
    /// Pure reconciler logic. Input is a list of snapshots; output is a
    /// pair of counts that <see cref="ReconciliationLogic.ApplyCounts"/>
    /// writes to a ledger. Deterministic, no RimWorld state.
    /// </summary>
    public static class ReconciliationLogic
    {
        /// <summary>
        /// Counts survived infected pawns from <paramref name="snapshots"/>.
        /// Returns (humanoid, animal). Both inclusive of all living
        /// HiddenInfected pawns regardless of underlying race.
        /// </summary>
        public static void CountSurvivingInfected(
            IReadOnlyList<PawnSnapshot> snapshots,
            out int humanoid, out int animal)
        {
            humanoid = 0;
            animal = 0;
            if (snapshots == null) return;
            for (int i = 0; i < snapshots.Count; i++)
            {
                var snap = snapshots[i];
                if (!snap.IsHiddenInfected) continue;
                if (snap.IsDead) continue;
                if (snap.IsHumanlike) humanoid++;
                else if (snap.IsAnimal) animal++;
            }
        }

        /// <summary>
        /// Replaces the ledger's <c>HumanoidLiveCount</c> and
        /// <c>AnimalLiveCount</c> with the values produced by the
        /// caller-supplied snapshot pass. Replacement semantics, not
        /// delta — the ledger is a snapshot of the live map at reconciliation
        /// time.
        /// </summary>
        public static void ApplyCounts(PopulationLedger ledger, int humanoid, int animal)
        {
            if (ledger == null) return;
            ledger.HumanoidLiveCount = System.Math.Max(0, humanoid);
            ledger.AnimalLiveCount = System.Math.Max(0, animal);
        }
    }
}
