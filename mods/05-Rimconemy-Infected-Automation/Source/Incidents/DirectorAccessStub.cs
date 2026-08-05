// Source/Incidents/DirectorAccessStub.cs
//
// Owner: Infected & Automation (Package 05).
// Phase B — Test-Seam for InfectedRaidSpawnService stub-directors.
//
// Why a stub director?
//   InfectedRaidSpawnService.BuildPlanForTick is a static method that
//   reads the revenge-pending quota from the live StoryDirector instance
//   via StoryDirector.Get(). To exercise the merge in a unit test we
//   need a way to inject a deterministic value without booting a real
//   GameComponent. This seam flips that read path: when
//   InfectedRaidSpawnService.StubDirector is non-null, the merge reads
//   from this stub instead of the live director. Production code leaves
//   StubDirector at null so a regression test failure cannot leak into
//   a real game (the seam contains no caching and resets to null between
//   test invocations via the Boot-time RunAll wipe path).
//
// This is intentionally a single, minimal class: it does not pretend to
// be a StoryDirector mock (no Scribe, no ThreatHistory) — only the read
// that the merge needs.

namespace Rimconemy.InfectedAutomation.Incidents
{
    /// <summary>
    /// Phase B regression-test stub for the StoryDirector access point.
    /// Production code never touches this. The Boot RunAll sets the
    /// static field to null between tests; the regression test sets it
    /// to a deterministic value for the duration of one assertion then
    /// resets via a try/finally boundary when necessary.
    /// </summary>
    public sealed class DirectorAccessStub
    {
        /// <summary>Value to return from GetPendingRevengeance.</summary>
        public int PendingRevenge;

        /// <summary>Read-accessor used by InfectedRaidSpawnService.</summary>
        public int GetPendingRevengeance() => PendingRevenge;
    }
}
