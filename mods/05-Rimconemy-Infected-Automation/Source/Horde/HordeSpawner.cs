// Source/Horde/HordeSpawner.cs
//
// Phase D — MapComponent orchestrator on the player-home map. Every
// 250 ticks it syncs the HordeWorldObject to the tick-derived tile;
// every 15 ticks it forces a regenerate of the two SectionLayer
// render-paths. Vanilla auto-instantiates SectionLayer subclasses per
// Section, but nothing ever marks custom layers dirty — the map drawer
// must be told to rebuild them or the pulse never renders.
// Mirrors PopulationLedgerReconciler pattern.

using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeSpawner : MapComponent
    {
        public HordeSpawner(Map map) : base(map) { }

        // Layer-regen cadence. MUST be a proper divisor of the 120-tick
        // pulse cycle with ≥4 samples: a 60-tick loop samples |sin(θ)| at
        // θ and θ+π which are equal → the pulse would freeze. 15 ticks
        // yields 8 samples per two-breath cycle, a visible beat.
        // internal so the startup regression D13 can assert the divisor
        // and sample-count contract.
        internal const int LayerRegenIntervalTicks = 15;

        private int _lastTick = -HordeUpdateLogic.TickInterval;
        private int _nextLayerRegenTick;

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null) return;
            if (Scribe.mode != LoadSaveMode.Inactive) return;

            // The horde is a home-map concept: world-object sync and layer
            // regeneration both target the primary player-home map only.
            Map homeMap = Rimconemy.Foundation.Maps.MapRegistry.GetPrimaryPlayerHomeMap();
            if (homeMap == null || map != homeMap) return;

            int now = Find.TickManager?.TicksGame ?? 0;

            // Shared live gate (same source the render paths use): ledger
            // + active profile + threshold. Hoisted so the regen driver
            // consumes the SAME gate — a regression that drops the early
            // return would fail D14 (the pure decision takes activeNow).
            bool active = HordeCalculator.IsActiveNow();
            if (!active)
            {
                DespawnAllHordes();
                return;
            }

            // World-object sync: 250-tick cadence, tile purely tick-derived
            // (spec §6 — no persisted drift state).
            if (now >= _lastTick + HordeUpdateLogic.TickInterval)
            {
                _lastTick = now;
                SyncHordeAtTile(HordeUpdateLogic.ComputeHordeTile(homeMap.Tile, now), homeMap.Tile);
            }

            // Layer pulse: force a rebuild of the two render layers on a
            // 15-tick cadence so the alpha actually animates. RegenerateLayerNow
            // checks Visible per section, so this is a no-op while inactive.
            DriveLayerRegen(now, ref _nextLayerRegenTick, active,
                type => map.mapDrawer?.RegenerateLayerNow(type));
        }

        /// <summary>
        /// Pure regen decision: rebuild the render layers only when the
        /// cadence tick is due AND the horde is active. The IsActiveNow
        /// gate is already guaranteed by the MapComponentTick early-return;
        /// this pure form lets the startup regression D14 assert the gate
        /// and the cadence without a live game. now/tick values are game
        /// ticks; cadence math stays consistent with
        /// <see cref="HordeCalculator.PulseCycleTicks"/>.
        /// </summary>
        internal static bool ShouldRegenerateLayerNow(int now, int nextLayerRegenTick, bool activeNow)
        {
            return activeNow && now >= nextLayerRegenTick;
        }

        /// <summary>
        /// Fires a RegenerateLayerNow request for the two render layers on
        /// the 15-tick cadence, but only while the horde is active and the
        /// cadence tick is due. The <paramref name="requestLayer"/> sink
        /// lets the startup regression D15 count actual requests without a
        /// live game: production passes the real MapDrawer call, the test
        /// injects a counting lambda. The sink may be invoked twice per
        /// fire (both layer types), exactly like the live driver.
        /// </summary>
        internal static void DriveLayerRegen(
            int now,
            ref int nextLayerRegenTick,
            bool activeNow,
            System.Action<System.Type> requestLayer)
        {
            if (!ShouldRegenerateLayerNow(now, nextLayerRegenTick, activeNow)) return;
            nextLayerRegenTick = now + LayerRegenIntervalTicks;
            requestLayer(typeof(HordeSectionLayer));
            requestLayer(typeof(HordeBurstLayer));
        }

        private static void DespawnAllHordes()
        {
            var all = Find.WorldObjects.AllWorldObjects;
            for (int i = all.Count - 1; i >= 0; i--)
                if (all[i] is HordeWorldObject ho) ho.Destroy();
        }

        private static void SyncHordeAtTile(int tile, int homeTile)
        {
            var def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("Rimconemy_HordeWorldObject");
            if (def == null)
            {
                Log.Error("[Rimconemy.InfectedAutomation] HordeSpawner: Def 'Rimconemy_HordeWorldObject' missing.");
                return;
            }

            var existing = Find.WorldObjects.AllWorldObjects.FirstOrDefault(
                wo => wo is HordeWorldObject);
            if (existing != null)
            {
                if (existing.Tile != tile)
                    existing.Tile = tile;
                return;
            }

            var ho = (HordeWorldObject)WorldObjectMaker.MakeWorldObject(def);
            ho.Tile = tile;
            Find.WorldObjects.Add(ho);
            Log.Message("[Rimconemy.InfectedAutomation] HordeSpawner: Spawning HordeWorldObject at tile=" + tile + " (home=" + homeTile + ")");
        }
    }
}
