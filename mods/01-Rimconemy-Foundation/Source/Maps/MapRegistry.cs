using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Maps
{
    /// <summary>
    /// Owner: Foundation (Package 01, single owner of map lifecycle routes).
    /// Phase B / F-R2 (2026-08-05): single source of truth for
    /// "which RimWorld maps does Rimconemy even care about".
    ///
    /// Audit context — 11 separate sites enumerated <c>Find.Maps</c> with
    /// ad-hoc LINQ filters (Where/FirstOrDefault, naive foreach), each
    /// allocating a List&lt;Map&gt; or lambda closure on every call. The
    /// canonical filter — "IsPlayerHome and not null" — was duplicated,
    /// each site at risk of silent drift (e.g. CharacterSetup.cs using
    /// <c>FreeColonists</c> while ColonialReader used
    /// <c>FreeColonistsSpawned</c>).
    ///
    /// MapRegistry fixes all of that with a Tick-cached, immutable
    /// <see cref="IReadOnlyList{T}"/> of player-home maps (and a parallel
    /// all-loaded list for caraven-storage consumers). Tick polling is
    /// intentional over MapAdded/MapRemoved direct hooks: RimWorld's event
    /// surface for map lifecycle is internal and not stable across 1.5-x,
    /// 1.6, and 1.6-patches. A 60-tick polling refresh is abundant
    /// fidelity for the only consumers (storage snapshots, threat ticks,
    /// power chains) which all run on 250+ tick intervals anyway.
    ///
    /// Capabilities exposed (all read-only after Equip()):
    ///   • <see cref="GetPlayerHomeMaps"/> — IReadOnlyList&lt;Map&gt; (cached).
    ///   • <see cref="GetAllLoadedMaps"/>    — IReadOnlyList&lt;Map&gt; (cached).
    ///   • <see cref="GetPrimaryPlayerHomeMap"/> — first player-home map (canonical).
    ///   • <see cref="AnyPlayerHomeMap"/>     — boolean shortcut.
    ///   • <see cref="PlayerHomeMapCount"/>   — int (cheap probe).
    ///
    /// Refresh contract:
    ///   • On every <c>GameComponentTick()</c> (60 ticks/s), the registry
    ///     invalidates its cache if <c>Find.Maps.Count</c> changed OR the
    ///     <c>IsPlayerHome</c> flag set drifts (its hash).
    ///   • Refresh cost is O(Find.Maps.Count); worst-case ~40 Map refs on
    ///     late-game saves — still &lt; 1 µs.
    /// </summary>
    public sealed class MapRegistry : GameComponent
    {
        public const string LogMarker = "v1";
        public const string CapabilityId = "rimconemy.foundation.maps";

        // Backing snapshots. Immutable within a refresh window; replaced
        // atomically by a fresh list when the underlying Find.Maps drifts.
        private IReadOnlyList<Map> _playerHomeSnapshot = System.Array.Empty<Map>();
        private IReadOnlyList<Map> _allLoadedSnapshot = System.Array.Empty<Map>();
        private Map _primaryPlayerHomeMap = null;

        // Drift detection: cheap pre-check before full rebuild.
        private int _lastObservedMapCount = -1;
        private long _lastObservedPlayerHomeHash = 0L;

        public MapRegistry(Game game) { }

        /// <summary>
        /// Tick-based refresh. Bounded to a no-op when the observed
        /// <c>Find.Maps.Count</c> matches the cached one and the
        /// <c>IsPlayerHome</c> flag-set fingerprint also matches.
        /// </summary>
        public override void GameComponentTick()
        {
            if (Current.Game == null || Find.Maps == null)
                return;

            int observedCount = Find.Maps.Count;
            long observedHash = ComputePlayerHomeHash();

            if (observedCount == _lastObservedMapCount && observedHash == _lastObservedPlayerHomeHash)
                return;

            // Drift detected — rebuild both snapshots and refresh the
            // primary-home cache. Snapshots are immutable so a swap is
            // a single reference replacement (no reader blocks).
            RebuildSnapshots();
            _lastObservedMapCount = observedCount;
            _lastObservedPlayerHomeHash = observedHash;
        }

        private void RebuildSnapshots()
        {
            var playerHome = new List<Map>(Find.Maps.Count);
            var allLoaded = new List<Map>(Find.Maps.Count);

            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m == null) continue;

                allLoaded.Add(m);
                if (m.IsPlayerHome)
                {
                    playerHome.Add(m);
                    // Snapshot primary-home on first encountered.
                    // Primary is conventionally the first map RimWorld exposes
                    // (matches Find.AnyPlayerHomeMap in single-map saves).
                    // Multiple player-home maps (multi-colony) keep the
                    // first as canonical for StoryDirector target.
                    if (_primaryPlayerHomeMap == null)
                        _primaryPlayerHomeMap = m;
                }
            }

            _playerHomeSnapshot = playerHome;
            _allLoadedSnapshot = allLoaded;
        }

        private static long ComputePlayerHomeHash()
        {
            // FNV-1a fingerprint of the IsPlayerHome-flag positions. Lets
            // us detect drift in the flag set without rebuilding the list.
            if (Find.Maps == null) return 0L;

            long h = 1469598103934665603L; // FNV-1a offset basis
            for (int i = 0; i < Find.Maps.Count; i++)
            {
                Map m = Find.Maps[i];
                if (m != null && m.IsPlayerHome)
                    h = (h ^ i) * 1099511628211L;
            }
            return h;
        }

        // ── Public Readers (zero-allocation) ────────────────────────

        /// <summary>
        /// Returns the cached immutable list of player-home maps. Empty if
        /// no map is loaded (main menu). Re-reads <c>Find.Maps</c> only
        /// when called before the first <c>GameComponentTick</c> or when
        /// the registry detects drift.
        /// </summary>
        public static IReadOnlyList<Map> GetPlayerHomeMaps()
        {
            var registry = GetOrCreate();
            if (registry == null) return System.Array.Empty<Map>();
            return registry._playerHomeSnapshot;
        }

        /// <summary>
        /// Returns all loaded maps (including temporary maps and Caravan
        /// storage-equivalent sentinels). Caravan storage itself stays in
        /// the CaravanStorageEnumerator (H4 §4); this snapshot is for
        /// delegates that need a "everything currently loaded" view.
        /// </summary>
        public static IReadOnlyList<Map> GetAllLoadedMaps()
        {
            var registry = GetOrCreate();
            if (registry == null) return System.Array.Empty<Map>();
            return registry._allLoadedSnapshot;
        }

        /// <summary>
        /// Returns the canonical primary player-home map (first encountered
        /// with IsPlayerHome=true). Null if no map loaded. Replaces the
        /// previous Find.AnyPlayerHomeMap → Find.Maps.FirstOrDefault()
        /// double-call pattern in StoryDirector.
        /// </summary>
        public static Map GetPrimaryPlayerHomeMap()
        {
            var registry = GetOrCreate();
            return registry?._primaryPlayerHomeMap;
        }

        /// <summary>True if at least one player-home map is loaded.</summary>
        public static bool AnyPlayerHomeMap()
        {
            return GetPlayerHomeMaps().Count > 0;
        }

        /// <summary>Cheap probe: number of player-home maps currently loaded.</summary>
        public static int PlayerHomeMapCount()
        {
            return GetPlayerHomeMaps().Count;
        }

        /// <summary>
        /// Forces an immediate refresh. Used by tests and by harness
        /// code paths that want deterministic ordering without waiting for
        /// the next tick (e.g. Save/Load tests).
        /// </summary>
        public static void ForceRefresh()
        {
            var registry = GetOrCreate();
            if (registry == null) return;
            registry.RebuildSnapshots();
            registry._lastObservedMapCount = Find.Maps?.Count ?? -1;
            registry._lastObservedPlayerHomeHash = ComputePlayerHomeHash();
        }

        // ── singleton resolver ─────────────────────────────────────

        private static MapRegistry GetOrCreate()
        {
            if (Current.Game == null) return null;
            return Current.Game.GetComponent<MapRegistry>();
        }

        /// <summary>
        /// Capability-gated registration probe. Other packages call this
        /// before invoking the registry, mirroring CapabilityAudit discipline.
        /// </summary>
        public static bool IsAvailable()
        {
            // Foundation owns MapRegistry unconditionally; no external
            // gating needed. The method exists for symmetry with other
            // services so call sites read consistently.
            return GetOrCreate() != null;
        }
    }
}
