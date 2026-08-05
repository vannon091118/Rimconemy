// Source/Horde/HordeMigrationDriver.cs
//
// Phase F — Wandering-Horde MapComponent (player-home map only).
// 250-Tick cadence. FSM pro Tile (5-Tile Rolling-Window). Phase-D
// HordeUpdateLogic.ComputeHordeTile bleibt die Single-Source-of-Truth
// fuer den Leader-Tile. Spec §3.4, §5.

using System.Collections.Generic;
using Rimconemy.Foundation.Maps;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeMigrationDriver : MapComponent
    {
        public const int CadenceTicks = 250;
        public const int RollingWindow = 5;

        public HordeMigrationDriver(Map map) : base(map) { }

        public static HordeMigrationDriver Get(Map map) =>
            map?.GetComponent<HordeMigrationDriver>();

        public int GetLeaderTile()
        {
            Map home = MapRegistry.GetPrimaryPlayerHomeMap();
            return home != null ? HordeUpdateLogic.ComputeHordeTile(home.Tile, Find.TickManager.TicksGame) : 0;
        }

        public TravelTileStatus GetTileState(int tile)
        {
            var manifest = HordeManifest.Get();
            if (manifest == null) return TravelTileStatus.Idle;
            for (int i = 0; i < manifest.TileRecords.Count; i++)
                if (manifest.TileRecords[i].Tile == tile) return manifest.TileRecords[i].Status;
            return TravelTileStatus.Idle;
        }

        public List<TravelTileRecord> GetActiveTileRecords(int window = RollingWindow)
        {
            var list = new List<TravelTileRecord>();
            var manifest = HordeManifest.Get();
            int leader = GetLeaderTile();
            if (manifest == null) return list;
            for (int i = 0; i < manifest.TileRecords.Count; i++)
            {
                int d = leader - manifest.TileRecords[i].Tile;
                if (d >= 0 && d <= window) list.Add(manifest.TileRecords[i]);
            }
            return list;
        }

        private int _lastCadenceTick = -CadenceTicks;

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (Scribe.mode != LoadSaveMode.Inactive) return;
            Map home = MapRegistry.GetPrimaryPlayerHomeMap();
            if (home == null || map != home) return;

            long currentTick = Find.TickManager?.TicksGame ?? 0L;
            if (currentTick < _lastCadenceTick + CadenceTicks) return;
            _lastCadenceTick = (int)currentTick;

            if (!HordeCalculator.IsActiveNow())
            {
                DespawnManifestAndWorldObjects();
                return;
            }

            HordeManifest manifest = HordeManifest.Get();
            var profile = StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival;
            string key = StoryDirector.StripRimconemyPrefix(profile.ProfileId);

            if (manifest == null)
            {
                manifest = HordeManifest.CreateOrExpand(key, currentTick);
            }

            int leaderTile = HordeUpdateLogic.ComputeHordeTile(home.Tile, currentTick);

            for (int tile = leaderTile - RollingWindow + 1; tile <= leaderTile; tile++)
            {
                var rec = GetOrCreateTileRecord(manifest, tile);
                AdvanceTileFSM(ref rec, key, currentTick);
                UpdateRecord(manifest, rec);
            }

            // Reveal-Radius sync (chunk cleanup / materialization on tile-distance boundary).
            HordeChunkCleanupService.SyncRevealRadius(manifest, home.Tile, currentTick, home);
        }

        /// <summary>
        /// Pure FSM-advance. Idempotent given same profile + tick.
        /// Migrating (set Staging with timer); Staging (timer-decrement OR activate);
        /// Attacking (set Idle); Idle (set Migrating).
        /// </summary>
        public static void AdvanceTileFSM(ref TravelTileRecord rec, string profileKey, long currentTick)
        {
            long elapsed = currentTick - rec.LastTransitionTick;
            switch (rec.Status)
            {
                case TravelTileStatus.Idle:
                    rec.Status = TravelTileStatus.Migrating;
                    rec.LastTransitionTick = currentTick;
                    rec.LastSeenAtTick = currentTick;
                    break;
                case TravelTileStatus.Migrating:
                    rec.Status = TravelTileStatus.Staging;
                    rec.ActiveStagingTicksLeft = PopulationProfileMultipliers.GetHordeStagingDurationTicks(profileKey);
                    rec.LastTransitionTick = currentTick;
                    rec.LastSeenAtTick = currentTick;
                    break;
                case TravelTileStatus.Staging:
                    if (elapsed >= rec.ActiveStagingTicksLeft)
                    {
                        rec.Status = TravelTileStatus.Attacking;
                        rec.ActiveStagingTicksLeft = 0;
                        rec.LastTransitionTick = currentTick;
                        rec.LastSeenAtTick = currentTick;
                    }
                    else
                    {
                        rec.ActiveStagingTicksLeft -= (int)(elapsed / CadenceTicks * CadenceTicks);
                    }
                    break;
                case TravelTileStatus.Attacking:
                    rec.Status = TravelTileStatus.Idle;
                    rec.LastTransitionTick = currentTick;
                    rec.LastSeenAtTick = currentTick;
                    break;
            }
        }

        private static TravelTileRecord GetOrCreateTileRecord(HordeManifest manifest, int tile)
        {
            for (int i = 0; i < manifest.TileRecords.Count; i++)
                if (manifest.TileRecords[i].Tile == tile) return manifest.TileRecords[i];
            var rec = new TravelTileRecord { Tile = tile, Status = TravelTileStatus.Idle, LastTransitionTick = 0L };
            manifest.TileRecords.Add(rec);
            return rec;
        }

        private static void UpdateRecord(HordeManifest manifest, TravelTileRecord rec)
        {
            for (int i = 0; i < manifest.TileRecords.Count; i++)
                if (manifest.TileRecords[i].Tile == rec.Tile)
                {
                    manifest.TileRecords[i] = rec;
                    return;
                }
        }

        private static void DespawnManifestAndWorldObjects()
        {
            var all = Find.WorldObjects.AllWorldObjects;
            for (int i = all.Count - 1; i >= 0; i--)
                if (all[i] is HordeWorldObject) all[i].Destroy();
        }
    }
}
