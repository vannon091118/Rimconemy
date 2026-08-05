// Source/Horde/HordeManifest.cs
//
// Phase F — Wandering-Horde Manifest (persisted). Lightweight-Record-Schema
// pro Pawn (HealthPercent + EquipmentSeed + KindDef + FactionDef). Keine
// direkten Pawn-Objekte. Materialisierung via PawnGenerator-Mirror.
//
// Schema-Version 1 (kein Migration-Fallout, neu-Feature).
// Spec §3.1, §4.1.

using System.Collections.Generic;
using Rimconemy.Foundation.Save;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public sealed class HordeManifest : IExposable, ISchemaMigratable
    {
        public int LeaderTile;
        public string Profile;
        public long SpawnedAtTick;
        public List<HiddenPawnStamp> Stamps = new List<HiddenPawnStamp>();
        public List<TravelTileRecord> TileRecords = new List<TravelTileRecord>();
        public int Capacity;

        // ── Static instance tracking (single-instance per save) ──────────
        private static HordeManifest _active;

        public static HordeManifest Get() => _active;

        /// <summary>Test-only reset to wipe the static manifest reference.</summary>
        public static void ResetForTests() => _active = null;

        // ── ISchemaMigratable ──────────────────────────────────────────────
        public const int CurrentSchemaVersion = 1;
        int ISchemaMigratable.CurrentSchemaVersion => CurrentSchemaVersion;
        public string ClassId => "rimconemy.infectedautomation.horde_manifest";
        public int SchemaVersion { get; set; }

        private List<SchemaStep> _cachedSteps;
        public IList<SchemaStep> Steps
        {
            get
            {
                if (_cachedSteps != null) return _cachedSteps;
                _cachedSteps = new List<SchemaStep>();
                return _cachedSteps;
            }
        }

        public void MigrateIfNeeded()
        {
            // v0 → v1: no field-init, save-shape is current by construction.
            SchemaVersion = CurrentSchemaVersion;
        }

        /// <summary>
        /// Create-or-Expand. Initial Manifest or add Profile-Capacity Balance.
        /// Stamp-IDs deterministisch via FNV-1a (DeterministicRng.GetStableHashCode).
        /// </summary>
        public static HordeManifest CreateOrExpand(string profileId, long currentTick)
        {
            _active ??= new HordeManifest();
            int newCapacity = PopulationProfileMultipliers.GetHordeCapacity(profileId);
            int delta = newCapacity - _active.Stamps.Count;
            _active.Profile = profileId;
            _active.Capacity = newCapacity;
            if (_active.SpawnedAtTick == 0L) _active.SpawnedAtTick = currentTick;
            int seed = DeterministicRng.GetStableHashCode(profileId ?? "");
            for (int i = 0; i < delta; i++)
            {
                _active.Stamps.Add(new HiddenPawnStamp
                {
                    ThingID = $"Rimconemy_HiddenPawn_{DeterministicRng.GetStableHashCode((currentTick + i).ToString()):X8}",
                    KindDefName = "Rimconemy_InfectedRavager",
                    FactionDefName = "Rimconemy_HiddenInfectedFaction",
                    HealthPercent = 1.0f,
                    EquipmentSeedOffset = i * 7 + seed,
                    SpawnedAtTick = currentTick
                });
            }
            return _active;
        }

        // ── Materialization-Bitmap (Tile → bool via HashSet<int>) ─────
        private HashSet<int> _materializedTiles = new HashSet<int>();

        public bool IsTileMaterialized(int tile) => _materializedTiles.Contains(tile);

        public void MarkTileMaterialized(int tile, bool val)
        {
            if (val) _materializedTiles.Add(tile);
            else _materializedTiles.Remove(tile);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref LeaderTile, "hordeLeaderTile", 0);
            Scribe_Values.Look(ref Profile, "hordeProfile", "");
            Scribe_Values.Look(ref SpawnedAtTick, "hordeSpawnedAtTick", 0L);
            Scribe_Values.Look(ref Capacity, "hordeCapacity", 0);
            Scribe_Collections.Look(ref Stamps, "hordeStamps", LookMode.Deep);
            Scribe_Collections.Look(ref TileRecords, "hordeTileRecords", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _active = this;
            }
        }
    }
}
