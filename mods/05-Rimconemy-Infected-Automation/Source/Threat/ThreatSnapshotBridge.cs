using Rimconemy.InfectedAutomation.Threat;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Threat
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    ///
    /// Audit-Finding 6 (2026-08-04) — Doppel-Snapshot-Pfad:
    /// Vor dem Bridge hatten <see cref="InfectedRaidSpawnService.GetCurrentThreatSnapshot"/>
    /// und <see cref="WorldRaidCoordinator.LatestThreatSnapshot"/> je eine eigene
    /// <c>new ThreatAggregator { TotalPressure = d.LastSnapshot.ThreatPressure }</c>-Zeile.
    /// Sobald beide Stubs ihre Druckberechnung live schalten, würde jeder Pfad
    /// einzeln lesen und doppelt konstruieren — eine Quelle, zwei Pfade.
    ///
    /// Der Bridge liefert stattdessen den **einen** Konstruktionspunkt für die
    /// ThreatAggregator-Sicht auf den StoryDirector-Read-Modell. Die beiden
    /// Stubs delegieren über <see cref="GetLatest"/>.
    ///
    /// Design (Stand 2026-08-04, nach Tick-Stamp-Refactor):
    ///   * Read-through cache: <see cref="GetLatest"/> builds a fresh
    ///     ThreatAggregator whose <see cref="ThreatAggregator.LastUpdatedTick"/>
    ///     is stamped from <see cref="Story.SituationSnapshot.SnapshotUpdatedTick"/>
    ///     (production tick of the source snapshot). When the snapshot
    ///     is missing or pre-stamp (<c>SnapshotUpdatedTick == 0</c>), the
    ///     stamp falls back to <c>Find.TickManager.TicksGame</c> so the
    ///     cache still has a usable anchor — the prior implementation
    ///     always stamped <c>TicksGame</c>; this version prefers the
    ///     snapshot's own age when available.
    ///   * Cache validity: <see cref="IsCachedForCurrentTick"/> compares the
    ///     cached snapshot's tick against the checker's current tick. A
    ///     match means "cache is still anchored to the same evaluation
    ///     cycle, safe to reuse"; a mismatch means "cache is stale,
    ///     call <see cref="GetLatest"/> first".
    ///   * Defensive defaults: without a loaded saved game or without a
    ///     LastSnapshot, the bridge returns <c>null</c> and the cache stays
    ///     empty; callers continue to use their <c>?? 0f</c> fallbacks.
    ///   * Single-thread invariant: the bridge is only written from
    ///     main-thread RimWorld hooks (GameComponentTick, Map paths, UI
    ///     button handlers). Concurrent writers do not exist in this
    ///     codebase; a "most-recent writer wins" cache is therefore safe.
    /// </summary>
    public static class ThreatSnapshotBridge
    {
        /// <summary>
        /// Lightweight property exposing the last resolved ThreatAggregator
        /// and the tick it was resolved at. The <see cref="LatestTick"/>
        /// companion (kept under the bridge's control) is the value
        /// <see cref="IsCachedForCurrentTick"/> compares against.
        /// </summary>
        public static ThreatAggregator Latest { get; private set; }

        /// <summary>
        /// Companion tick stamp for <see cref="Latest"/>. Mirrors the
        /// ThreatAggregator.LastUpdatedTick so callers can detect a
        /// mismatched cache without dereferencing the cached instance.
        ///
        /// <b>Sentinel:</b> 0L means "never produced" (cache empty or
        /// explicitly <see cref="ResetForTests"/>-cleared). Consumers
        /// calling <see cref="IsCachedForCurrentTick"/> must pass a
        /// <paramref name="currentTick"/> strictly greater than 0 —
        /// passing 0L would collide with this sentinel and falsely
        /// report a cache miss. The contract is identical to
        /// <c>StoryState.LastEventTick == 0L</c> and
        /// <c>StoryState.FirstWipeTick == 0L</c>.
        /// </summary>
        public static long LatestTick { get; private set; }

        /// <summary>
        /// Resolves a fresh ThreatAggregator from StoryDirector.LastSnapshot.
        /// Returns <c>null</c> when no saved game is loaded or when no
        /// snapshot has been produced yet — the caller treats null as
        /// "no pressure, no spawn".
        ///
        /// The aggregation stamps <see cref="ThreatAggregator.LastUpdatedTick"/>
        /// from <see cref="Story.SituationSnapshot.SnapshotUpdatedTick"/>,
        /// so the cached snapshot's age reflects the producer's tick,
        /// not the consumer's current <c>TicksGame</c>. This is what
        /// makes <see cref="IsCachedForCurrentTick"/> meaningful.
        /// </summary>
        public static ThreatAggregator GetLatest()
        {
            try
            {
                var director = Story.StoryDirector.Get();
                if (director == null || director.LastSnapshot == null) return null;

                long snapshotTick = director.LastSnapshot.SnapshotUpdatedTick;
                // Defensive: if the producer didn't stamp a tick (older
                // snapshots or a stub director), fall back to the
                // live tick manager so the cache still has a usable
                // anchor — and warn nothing because this is a
                // contract-on-upgrade path.
                if (snapshotTick == 0L && Find.TickManager != null)
                    snapshotTick = Find.TickManager.TicksGame;

                ThreatAggregator snapshot = new ThreatAggregator
                {
                    TotalPressure = director.LastSnapshot.ThreatPressure,
                    LastUpdatedTick = snapshotTick,
                    // Mark the bridge as the construction site for traceability.
                    ScopeId = "Rimconemy.ThreatSnapshotBridge",
                };

                Latest = snapshot;
                LatestTick = snapshotTick;
                return snapshot;
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] ThreatSnapshotBridge.GetLatest: "
                    + ex.GetType().Name + ": " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Read-through-cache helper. Returns <c>true</c> when the
        /// bridge has a cached ThreatAggregator whose stamp matches
        /// <paramref name="currentTick"/>, meaning a caller operating
        /// at <paramref name="currentTick"/> may reuse
        /// <see cref="Latest"/> without calling <see cref="GetLatest"/>
        /// again.
        ///
        /// Semantics:
        ///   * Empty cache (Latest == null or LatestTick == 0L) → false.
        ///   * LatestTick != currentTick → false (cache from a prior
        ///     evaluation cycle; caller must <see cref="GetLatest"/>
        ///     first to refresh).
        ///   * LatestTick == currentTick → true (caller may reuse).
        ///
        /// The check is intentionally permissive about the snapshot's
        /// own <c>SnapshotUpdatedTick == 0</c> corner case: if the
        /// producer hasn't stamped a tick, GetLatest does the
        /// fallback to Find.TickManager.TicksGame, so a tick match
        /// there is still meaningful; a cache-miss there is still
        /// a cache-miss.
        /// </summary>
        public static bool IsCachedForCurrentTick(long currentTick)
        {
            if (Latest == null) return false;
            if (LatestTick == 0L) return false;
            return LatestTick == currentTick;
        }

        /// <summary>
        /// Convenience: returns the cached ThreatAggregator if the
        /// cache is valid for <paramref name="currentTick"/>, otherwise
        /// resolves a fresh one and returns it. Always returns a
        /// non-null instance unless the director itself is missing
        /// or has no LastSnapshot (in which case the caller still
        /// gets <c>null</c> and treats it as "no pressure, no spawn").
        /// </summary>
        public static ThreatAggregator GetOrResolveForTick(long currentTick)
        {
            if (IsCachedForCurrentTick(currentTick))
                return Latest;
            return GetLatest();
        }

        /// <summary>
        /// Returns the pressure value or 0 when both the director is
        /// missing and the bridge cannot resolve a snapshot. Convenience
        /// shim for callers that previously used direct
        /// <c>PressureFromDirector()</c>-style fallbacks.
        /// </summary>
        public static float GetLatestPressure()
        {
            try
            {
                var s = GetLatest();
                return s != null ? s.TotalPressure : 0f;
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] ThreatSnapshotBridge.GetLatestPressure: "
                    + ex.GetType().Name + ": " + ex.Message);
                return 0f;
            }
        }

        /// <summary>
        /// Test-only reset hook so unit tests can clear the Latest
        /// property and the LatestTick companion without going through
        /// a live game session. Production code should not call this.
        /// </summary>
        public static void ResetForTests()
        {
            Latest = null;
            LatestTick = 0L;
        }

        /// <summary>
        /// Test-only injection helper: lets unit tests plant a known
        /// ThreatAggregator with a known tick stamp into the cache
        /// without going through the live StoryDirector path. Production
        /// code should not call this — it bypasses the read-from-Story
        /// contract by design.
        /// </summary>
        public static void SetLatestForTests(ThreatAggregator agg, long tick)
        {
            Latest = agg;
            LatestTick = tick;
        }
    }
}
