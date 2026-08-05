// Source/Horde/HordeSpawner.cs
//
// Phase D — MapComponent orchestrator on the player-home map. Every
// 250 ticks it syncs the HordeWorldObject to the tick-derived tile;
// every 15 ticks it forces a regenerate of the two SectionLayer
// render-paths. Vanilla auto-instantiates SectionLayer subclasses per
// Section, but nothing ever marks custom layers dirty — the map drawer
// must be told to rebuild them or the pulse never renders.
// Mirrors PopulationLedgerReconciler pattern.

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
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
        private const int LayerRegenIntervalTicks = 15;

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

            var ledger = PopulationLedger.Get();
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            var profile = StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival;

            if (!HordeCalculator.IsActive(effective, profile))
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
            if (now >= _nextLayerRegenTick)
            {
                _nextLayerRegenTick = now + LayerRegenIntervalTicks;
                map.mapDrawer?.RegenerateLayerNow(typeof(HordeSectionLayer));
                map.mapDrawer?.RegenerateLayerNow(typeof(HordeBurstLayer));
            }
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
