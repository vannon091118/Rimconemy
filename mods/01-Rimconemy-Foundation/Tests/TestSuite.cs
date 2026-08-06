using System.Runtime.CompilerServices;
using Verse;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Shared test harness for all Rimconemy packages.
    ///
    /// Principles (deskriptive Wahrheit):
    ///   - Tracks what happened, never prescribes expectations.
    ///   - min= is a lower execution bound („Suite lief mit ≥ min Checks"),
    ///     not a world assertion.
    ///   - Failures use Log.Error with auto @file:line via
    ///     CallerFilePath/CallerLineNumber — compile-time, zero runtime cost.
    ///   - Crash-safe: never throws from Check(), RunSummary(), or Defer().
    ///
    /// Contract formats emitted:
    ///   Summary: [Rimconemy.&lt;Pkg&gt;] &lt;Suite&gt;: N passed, M failed (min=E).
    ///            First failure: &lt;name&gt;
    ///   Fail:    [Rimconemy.&lt;Pkg&gt;] TEST-FAIL &lt;Suite&gt; &lt;name&gt; @&lt;file&gt;:&lt;line&gt;
    ///   Defer:   [Rimconemy.&lt;Pkg&gt;] &lt;Suite&gt; TEST-DEFERRED &lt;reason&gt;
    /// </summary>
    public class TestSuite
    {
        private readonly string _package;
        private readonly string _suite;
        private int _passed;
        private int _failed;
        private string _firstFailure;

        public TestSuite(string package, string suite)
        {
            _package = package;
            _suite = suite;
            _passed = 0;
            _failed = 0;
            _firstFailure = null;
        }

        /// <summary>
        /// Record a single test check. Auto-tags with source location
        /// (file + line) via CallerFilePath/CallerLineNumber — compile-time
        /// embedding, zero runtime cost, survives Release builds.
        /// </summary>
        public void Check(bool ok, string name,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (ok)
            {
                _passed++;
                return;
            }

            _failed++;
            if (_firstFailure == null)
                _firstFailure = name;

            string fileName = System.IO.Path.GetFileName(file);
            Log.Error(
                $"[Rimconemy.{_package}] TEST-FAIL {_suite} {name} @{fileName}:{line}");
        }

        /// <summary>
        /// Emit the canonical summary line.
        ///   - If _passed ≥ min AND _failed == 0 → Log.Message (PASS).
        ///   - If _passed &lt; min → Log.Error (BELOW MINIMUM — suite ran fewer
        ///     checks than the lower bound).
        ///   - If _failed > 0 → Log.Error (FAIL).
        /// </summary>
        public void RunSummary(int min)
        {
            if (_passed < min)
            {
                Log.Error(
                    $"[Rimconemy.{_package}] {_suite}: {_passed} passed, " +
                    $"{_failed} failed (min={min}) — BELOW MINIMUM. " +
                    $"First failure: {_firstFailure ?? "none"}");
                return;
            }

            string summary =
                $"[Rimconemy.{_package}] {_suite}: {_passed} passed, " +
                $"{_failed} failed (min={min}).";

            if (_firstFailure != null)
                summary += $" First failure: {_firstFailure}";

            if (_failed > 0)
                Log.Error(summary);
            else
                Log.Message(summary);
        }

        /// <summary>
        /// Declare that the suite cannot be tested in the current environment.
        /// The parser counts this as „nicht geprüft" — never as a bug.
        /// </summary>
        public static void Defer(string package, string suite, string reason)
        {
            Log.Message(
                $"[Rimconemy.{package}] {suite} TEST-DEFERRED {reason}");
        }

        public int Passed => _passed;
        public int Failed => _failed;
        public string FirstFailure => _firstFailure;
        public string Suite => _suite;
    }
}
