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

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (map == null) return;
            if (Scribe.mode != LoadSaveMode.Inactive) return;

            int now = Find.TickManager?.TicksGame ?? 0;
            if (now < _lastTick + HordeUpdateLogic.TickInterval) return;
            _lastTick = now;

            try
            {
                var ledger = PopulationLedger.Get();
                int effective = HordeCalculator.GetEffectiveCount(ledger);
                var director = StoryDirector.Get();
                var profile = director?.ActiveProfile ?? SettingProfile.Survival;
                bool active = HordeCalculator.IsActive(effective, profile);

                // 1. Despawn all if below threshold
                if (!active)
                {
                    DespawnAllHordes();
                    return;
                }

                // 2. Find player home map
                Map homeMap = ResolveCanonicalPlayerMap();
                if (homeMap == null) return;
                int homeTile = homeMap.Tile;

                // 3. Run pure logic for spawn/drift state
                var tileList = new List<int>();
                HordeUpdateLogic.RunOncePure(effective, true, homeTile, now, tileList);

                // 4. Sync with actual WorldObjects
                SyncHordesAtTiles(tileList, homeTile, now);
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeSpawner: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static Map ResolveCanonicalPlayerMap()
        {
            // Reuse Foundation helper; falls back to AnyPlayerHomeMap.
            try
            {
                return Rimconemy.Foundation.Maps.MapRegistry.GetPrimaryPlayerHomeMap()
                    ?? Find.AnyPlayerHomeMap;
            }
            catch
            {
                return Find.AnyPlayerHomeMap;
            }
        }

        private static void DespawnAllHordes()
        {
            if (Find.WorldObjects == null) return;
            var all = Find.WorldObjects.AllWorldObjects;
            if (all == null) return;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (all[i] is HordeWorldObject ho) ho.Destroy();
            }
        }

        private static void SyncHordesAtTiles(List<int> tileList, int homeTile, long currentTick)
        {
            if (tileList.Count == 0) return;
            var def = DefDatabase<WorldObjectDef>.GetNamedSilentFail("Rimconemy_HordeWorldObject");
            if (def == null)
            {
                Log.Error("[Rimconemy.InfectedAutomation] HordeSpawner: Def 'Rimconemy_HordeWorldObject' missing.");
                return;
            }
            // Spawn one Horde at the drifted tile (one and only one per home map).
            int tile = tileList[0];
            var existing = Find.WorldObjects?.AllWorldObjects.FirstOrDefault(
                wo => wo is HordeWorldObject);
            if (existing == null)
            {
                try
                {
                    var ho = (HordeWorldObject)WorldObjectMaker.MakeWorldObject(def);
                    ho.Tile = tile;
                    ho.LastMoveTick = currentTick;
                    Find.WorldObjects.Add(ho);
                    Log.Message("[Rimconemy.InfectedAutomation] HordeSpawner: Spawning HordeWorldObject at tile=" + tile + " (home=" + homeTile + ")");
                }
                catch (System.Exception ex)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] HordeSpawner: MakeWorldObject failed: " + ex.Message);
                }
            }
            else
            {
                if (existing.Tile != tile)
                {
                    existing.Tile = tile;
                    Log.Message("[Rimconemy.InfectedAutomation] HordeSpawner: Move HordeWorldObject → tile=" + tile);
                }
            }
        }
    }
}
