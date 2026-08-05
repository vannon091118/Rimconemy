// Source/Horde/HordeSpawner.cs
//
// Phase D — MapComponent orchestrator. Calls HordeUpdateLogic every
// 250 ticks, spawns / moves / despawns HordeWorldObjects accordingly.
// Mirrors PopulationLedgerReconciler pattern.

using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeSpawner : MapComponent
    {
        public HordeSpawner(Map map) : base(map) { }

        private int _lastTick = -HordeUpdateLogic.TickInterval;

        // Drift state externalized into a list so the Pure logic can be
        // unit-tested; it MUST persist across ticks or the horde would
        // re-spawn at homeTile+5 on every interval instead of drifting.
        private readonly List<int> _hordeTiles = new List<int>();

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null) return;
            if (Scribe.mode != LoadSaveMode.Inactive) return;

            int now = Find.TickManager?.TicksGame ?? 0;
            if (now < _lastTick + HordeUpdateLogic.TickInterval) return;
            _lastTick = now;

            var ledger = PopulationLedger.Get();
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            var profile = StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival;
            if (!HordeCalculator.IsActive(effective, profile))
            {
                _hordeTiles.Clear();
                DespawnAllHordes();
                return;
            }

            Map homeMap = Rimconemy.Foundation.Maps.MapRegistry.GetPrimaryPlayerHomeMap();
            if (homeMap == null) return;
            int homeTile = homeMap.Tile;

            HordeUpdateLogic.RunOncePure(true, homeTile, now, _hordeTiles);
            if (_hordeTiles.Count > 0)
                SyncHordeAtTile(_hordeTiles[0], homeTile);
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
