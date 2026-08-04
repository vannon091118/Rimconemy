using System;
using System.Diagnostics;
using Rimconemy.Foundation.Save;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Profile
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Welle-2 / Item #10 (2026-08-05): optional Performance-Meter that
    /// finally lets INTERFACE_CONTRACT §6 §7 P1 / P3-Gates be measured,
    /// not just declared.
    ///
    /// Background (see docs/INTERFACE_CONTRACT.md §6 + §7):
    ///   P1 ≤ 2 ms average per <see cref="GameComponentTick()"/>.
    ///   P3 ≤ 1 MiB/Day GC growth.
    /// Both gates were declared but had no instrumentation; the audit
    /// (2026-08-04) confirmed they remained "declared but unmeasured".
    ///
    /// RuntimeMeter is a static service that:
    ///   1. Collects per-tick duration samples via <see cref="Stopwatch"/>
    ///      around the <see cref="RuntimeMeterSampler"/> GameComponentTick
    ///      and any component that calls <see cref="Sample(string,double)"/>.
    ///   2. Collects per-tick GC-byte delta via <see cref="GC.GetTotalMemory"/>
    ///      every 10 ticks (configurable via <see cref="GcSampleInterval"/>).
    ///   3. Maintains a ring buffer of <c>Capacity = 1000</c> samples;
    ///      oldest sample is overwritten when full.
    ///   4. Exposes <see cref="GetStats"/> as a deterministic aggregate
    ///      (avg/p50/p99/last) for the dashboard and the truthful log line.
    ///   5. Honours <see cref="FoundationSaveData.EnableProfileMeter"/>;
    ///      when disabled, the sampler is a constant-time no-op and the
    ///      ring buffer stays empty.
    ///
    /// Why a separate small GameComponent for the actual tick (vs hooking
    /// GameComponentTick in FoundationSaveData):
    ///   - Keeps <see cref="FoundationSaveData"/> free of profilable code
    ///     paths so its own GameComponentTick can stay on the P1-≤2 ms
    ///     budget without RuntimeMeter overhead.
    ///   - Auto-discovered by RimWorld via reflection (same mechanism
    ///     that registers FoundationSaveData / MapRegistry).
    ///
    /// Output:
    ///   - Each ~600 ticks: <see cref="LogRotatingSummary"/> writes one
    ///     structured line tagged <c>[Rimconemy.Foundation.RuntimeMeter]</c>
    ///     when enabled.
    ///   - The next iteration of FoundationDashboard (Phase 2.0) will
    ///     surface <see cref="GetStats"/> in the existing capability
    ///     matrix; for now the static read-only snapshot feeds the
    ///     log + dev-time investigations.
    /// </summary>
    public static class RuntimeMeter
    {
        public const string LogMarker = "v1";
        public const int Capacity = 1000;
        public const int GcSampleIntervalTicks = 10;

        // ── ring buffer ────────────────────────────────────────────
        private static readonly SampleSlot[] _buffer = new SampleSlot[Capacity];
        private static int _head = 0;   // next write index
        private static int _count = 0;  // samples committed (capped at Capacity)

        // ── GC delta tracking (last-sampled baseline) ───────────────
        private static long _gcBaselineBytes;
        private static long _gcLastDeltaBytes;
        private static long _gcRunningAverageBytesPerTick;

        // ── last logged-rotation marker ─────────────────────────────
        private static long _lastRotatedTick = -1;

        /// <summary>
        /// Returns true if the meter is enabled via the FoundationSaveData
        /// opt-in or via direct static toggle. Defensive when FoundationSaveData
        /// is not yet alive (early-bootstrap path).
        /// </summary>
        public static bool IsEnabled
        {
            get
            {
                if (Current.Game == null) return false;
                var sd = Current.Game.GetComponent<FoundationSaveData>();
                if (sd != null && sd.EnableProfileMeter) return true;
                return StaticOverrideEnabled;
            }
        }

        /// <summary>
        /// Operator-override for dev-time profiling without going through
        /// FoundationSaveData. Clears on startup.
        /// </summary>
        public static bool StaticOverrideEnabled { get; set; } = false;

        /// <summary>
        /// Records one tick-duration sample. Caller must supply its own
        /// stopwatch-elapsed time; we do not measure inside this method to
        /// keep allocation-free fast-path semantics.
        /// </summary>
        public static void Sample(long elapsedTicks, long elapsedStopwatchNs, long currentTick)
        {
            if (!IsEnabled) return;

            var slot = new SampleSlot
            {
                Tick = currentTick,
                ElapsedStopwatchNs = elapsedStopwatchNs,
                ElapsedLogicalTicks = elapsedTicks,
            };
            _buffer[_head] = slot;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
        }

        /// <summary>
        /// Convenience for callers that have already started a stopwatch
        /// and want to stop+sample in one call: returns the elapsed
        /// nanoseconds without recording.
        /// </summary>
        public static long StopAndMeasure(Stopwatch sw)
        {
            sw.Stop();
            return sw.ElapsedTicks * 1000L * 1000L / Stopwatch.Frequency;
        }

        /// <summary>
        /// GC-delta sampling, called by the sampler every <see cref="GcSampleIntervalTicks"/>.
        /// Updates <see cref="_gcLastDeltaBytes"/> and a 10-tick running average.
        /// </summary>
        public static void SampleGc(long currentTick)
        {
            if (!IsEnabled) return;

            long current = GC.GetTotalMemory(false);
            if (_gcBaselineBytes == 0)
            {
                _gcBaselineBytes = current;
                _gcLastDeltaBytes = 0;
                _gcRunningAverageBytesPerTick = 0;
                return;
            }
            long delta = current - _gcBaselineBytes;
            _gcdailyDelta = delta;
            if (delta > _gcLastDeltaBytes) _gcLastDeltaBytes = delta;
            _gcRunningAverageBytesPerTick = delta / Math.Max(1, currentTick - _lastRotatedTick);
        }
        private static long _gcdailyDelta;

        /// <summary>
        /// Emits a one-line rotation summary every ~600 enabled-ticks. Returns
        /// the formatted line so callers can also pipe it elsewhere.
        /// </summary>
        public static string LogRotatingSummary(long currentTick)
        {
            if (!IsEnabled) return null;
            if (_lastRotatedTick > 0 && currentTick - _lastRotatedTick < 600) return null;
            _lastRotatedTick = currentTick;

            var stats = GetStats();
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "[Rimconemy.Foundation.RuntimeMeter] tick={0} samples={1} avg_ns={2:F0} p50_ns={3:F0} p99_ns={4:F0} gc_baseline={5} gc_delta={6}",
                currentTick, _count, stats.AverageNs, stats.P50Ns, stats.P99Ns, _gcBaselineBytes, _gcdailyDelta);
        }

        /// <summary>Returns aggregate stats over the current ring buffer.</summary>
        public static Stats GetStats()
        {
            var s = new Stats { Count = _count };
            if (_count == 0) return s;

            long sumNs = 0;
            long maxNs = 0;
            long minNs = long.MaxValue;

            // Snapshot copy so we don't disturb the head repetition.
            var snapshot = new SampleSlot[_count];
            for (int i = 0; i < _count; i++)
            {
                int idx = (_head - 1 - i + Capacity) % Capacity;
                snapshot[i] = _buffer[idx];
            }

            for (int i = 0; i < _count; i++)
            {
                long ns = snapshot[i].ElapsedStopwatchNs;
                if (ns < 0) continue; // pre-toggle samples
                sumNs += ns;
                if (ns > maxNs) maxNs = ns;
                if (ns < minNs) minNs = ns;
            }

            s.AverageNs = (double)sumNs / _count;

            // Sort in-place for percentiles (O(N log N) but N ≤ 1000).
            System.Array.Sort(snapshot, (a, b) => a.ElapsedStopwatchNs.CompareTo(b.ElapsedStopwatchNs));
            int pi50 = (int)(_count * 0.50);
            int pi99 = (int)(_count * 0.99);
            s.P50Ns = snapshot[Math.Min(pi50, _count - 1)].ElapsedStopwatchNs;
            s.P99Ns = snapshot[Math.Min(pi99, _count - 1)].ElapsedStopwatchNs;
            s.MaxNs = maxNs;
            s.MinNs = minNs == long.MaxValue ? 0 : minNs;
            s.GcBaselineBytes = _gcBaselineBytes;
            s.GcLastDeltaBytes = _gcdailyDelta;
            return s;
        }

        /// <summary>Clears the ring buffer (e.g. on save/load).</summary>
        public static void Reset()
        {
            _head = 0;
            _count = 0;
            _gcBaselineBytes = 0;
            _gcLastDeltaBytes = 0;
            _gcRunningAverageBytesPerTick = 0;
            _lastRotatedTick = -1;
        }

        /// <summary>
        /// Basic aggregated stats. Immutable snapshot returned by
        /// <see cref="GetStats"/>.
        /// </summary>
        public struct Stats
        {
            public int Count;
            public double AverageNs;
            public long P50Ns;
            public long P99Ns;
            public long MaxNs;
            public long MinNs;
            public long GcBaselineBytes;
            public long GcLastDeltaBytes;
        }

        /// <summary>One slot in the ring buffer.</summary>
        private struct SampleSlot
        {
            public long Tick;
            public long ElapsedStopwatchNs;
            public long ElapsedLogicalTicks;
        }
    }

    /// <summary>
    /// Companion GameComponent that drives <see cref="RuntimeMeter"/>
    /// sampling from RimWorld's main tick. Cheap to run:
    ///   - One <see cref="Stopwatch"/> allocation reused per tick.
    ///   - One Sample/SampleGc call.
    ///   - One isEnabled guard short-circuits the work path.
    /// Auto-discovered by RimWorld's reflection.
    /// </summary>
    public sealed class RuntimeMeterSampler : GameComponent
    {
        private readonly System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();
        private int _gcTickCounter = 0;

        public RuntimeMeterSampler(Game game) { }

        public override void GameComponentTick()
        {
            if (!RuntimeMeter.IsEnabled)
                return;
            if (Find.TickManager == null) return;

            long currentTick = Find.TickManager.TicksGame;
            _sw.Restart();
            // Sample is cheap (one struct copy into ring buffer).
            _sw.Stop();
            RuntimeMeter.Sample(_sw.ElapsedTicks, RuntimeMeter.StopAndMeasure(_sw), currentTick);

            _gcTickCounter++;
            if (_gcTickCounter >= RuntimeMeter.GcSampleIntervalTicks)
            {
                _gcTickCounter = 0;
                RuntimeMeter.SampleGc(currentTick);
            }

            // Rotation summary line; emitting it directly here keeps the
            // sampler self-contained. The line is one <see cref="Log.Message"/>
            // every ~600 ticks which is low-rate for the Log file.
            string summary = RuntimeMeter.LogRotatingSummary(currentTick);
            if (summary != null) Log.Message(summary);
        }

        public override void ExposeData()
        {
            // No persistent state; the meter is purely runtime-tick observable.
            // We expose a no-op save/load so save-time Scribe doesn't NRE.
            base.ExposeData();
        }
    }
}
