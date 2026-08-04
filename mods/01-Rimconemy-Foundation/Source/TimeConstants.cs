namespace Rimconemy.Foundation
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    /// Cross-package canonical time constants. Single source of truth
    /// for tick ↔ day/hour/real-second conversions so the literal
    /// 60000 / 2500 / 60 values live in exactly one place instead of
    /// being repeated as inline magic literals across Foundation UI,
    /// Story snapshot/event/director, and the threat dashboard.
    /// ROADMAP §8.7 F3 binds the 60.000 half here; F counts the
    /// historical "vs 30.000" framing only.
    ///
    /// Derivation (RimWorld 1.6 default tick rate):
    ///   TicksPerRealSecond × 60 sec/min × 60 min/hr × 24 hr/day
    ///     = 60 × 60 × 60 × 24 = 5_184_000 ticks per real day.
    /// RimWorld scales this down for an in-game day so that 1 in-game
    /// day = 60_000 ticks; subsequent divisions by 24 and 60 are the
    /// canonical hour and second counts on the in-game clock.
    ///
    /// Use across packages 01–05 in preference to inline literals;
    /// see INTERFACE_CONTRACT §1 (Capabilities) for the cross-package
    /// reference path.
    ///
    /// Implementation note: the three constants are exposed as
    /// <c>static readonly</c> rather than <c>const</c>. <c>const</c>
    /// would let the C# compiler inline the literal into every caller
    /// (each caller then carries the <c>ldc.r4 60000</c> in its IL),
    /// which hides call sites from the regression test in
    /// <see cref="Rimconemy.Foundation.Tests.FoundationTimeConstantsRegressionTests"/>
    /// and defeats its purpose. <c>static readonly</c> forces callers
    /// to emit <c>ldsfld TimeConstants.TicksPerDay</c> instead, so the
    /// "all call sites route through the constant" invariant is
    /// mechanical rather than documentary. The values are still
    /// immutable — only the compile-time inlining behavior changes.
    /// </summary>
    public static class TimeConstants
    {
        /// <summary>
        /// RimWorld ticks per in-game day. 60_000 ticks = 1 day at the
        /// vanilla 60 ticks/second rate. Use for all GameTick / n-day,
        /// days-to-ticks, and tick-cooldown conversions across packages.
        /// Mirrors the constant referenced in DECISIONS §7 and §14.
        /// </summary>
        public static readonly float TicksPerDay = 60000f;

        /// <summary>
        /// RimWorld ticks per in-game hour. 2_500 ticks = 1 hour in the
        /// in-game day (60_000 ÷ 24). Use when an event cadence or
        /// scheduling rule is described per-hour rather than per-day.
        /// </summary>
        public static readonly float TicksPerHour = 2500f;

        /// <summary>
        /// RimWorld ticks per real-world second. 60 ticks/second is the
        /// vanilla default tick rate and the divisor that converts
        /// wall-clock durations into tick deltas (e.g. for performance
        /// budgets in INTERFACE_CONTRACT §6). Use whenever a duration
        /// is expressed as "real seconds" rather than in-game time.
        /// </summary>
        public static readonly float TicksPerRealSecond = 60f;
    }
}
