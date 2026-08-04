using System.Collections.Generic;
using Rimconemy.Foundation.Registry;
using Verse;

namespace Rimconemy.Foundation.Registry
{
    /// <summary>
    /// Owner: Foundation (Package 01)
    /// Architecture-Boundaries Sprint — F-V4.
    ///
    /// CapabilityAudit wraps <see cref="PackageRegistry.HasCapability"/> with two
    /// audit guarantees:
    ///
    /// 1. **Once-Logging per (packageId, capabilityId, readerContext) tuple.**
    ///    When a feature is missing we log a single warning, not one per tick.
    ///    This keeps RimWorld-Logs readable while still making silent feature-loss
    ///    observable (R5 in spec — "Lost feature flag" complaints).
    ///
    /// 2. **Identity-equality in tests:** in unit-test mode the warns are collected
    ///    into a static buffer instead of being written to Verse.Log, so test
    ///    assertions can verify gate behavior without engaging the log pipeline.
    ///
    /// The contract for readers is: replace every cross-package feature read
    /// that previously assumed co-loading with a `HasCapabilityOrWarn` call.
    /// Adopting this helper is F-V4 of the sprint; gates go live in steps 2-5.
    ///
    /// Thread-safety: GameComponent ticks run on the main thread; static fields
    /// are only mutated from Bootstrap or first-tick reads. We keep an explicit
    /// lock to be safe for future reflection-driven introspection.
    /// </summary>
    public static class CapabilityAudit
    {
        private static readonly object _lock = new object();
        private static readonly HashSet<string> _warnedKeys = new HashSet<string>();
        // Public test-buffer: tests can call ClearWarnings() and inspect via Warnings().
        private static readonly List<string> _testWarnings = new List<string>();

        // Reserved placeholders. Defined as constants because C# interpolation
        // chokes when expressions contain literal '<' / '>' chars (parser tries
        // to interpret them as a generic type signature).
        private const string UnknownContext = "[unknown]";
        private const string UnspecifiedContext = "[unspecified]";

        /// <summary>
        /// Returns true if <paramref name="capabilityId"/> is exposed by
        /// <paramref name="packageId"/> at the requested minimum version.
        /// Logs a one-shot warning identifying the reader context when false.
        /// </summary>
        /// <param name="packageId">Owner package ID, e.g. "rimconemy.infectedautomation".</param>
        /// <param name="capabilityId">Capability ID, e.g. "rimconemy.infectedautomation.threat".</param>
        /// <param name="minVersion">Minimum capability version required (default 1).</param>
        /// <param name="readerContext">Short identifier of the reader (log readability).</param>
        public static bool HasCapabilityOrWarn(
            string packageId,
            string capabilityId,
            int minVersion = 1,
            string readerContext = null)
        {
            if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(capabilityId))
                return false;

            bool has = PackageRegistry.HasCapability(packageId, capabilityId, minVersion);
            if (has) return true;

            // Compose a stable key for once-warning.
            string contextKey = readerContext ?? UnspecifiedContext;
            string key = $"{packageId}|{capabilityId}|v{minVersion}|{contextKey}";
            lock (_lock)
            {
                if (_warnedKeys.Add(key))
                {
                    string contextLabel = readerContext ?? UnknownContext;
                    string msg =
                        "[Rimconemy.Foundation.CapabilityAudit] Reader '" + contextLabel + "': " +
                        "capability '" + capabilityId + "' (>=v" + minVersion + ") not available " +
                        "(package '" + packageId + "' not registered or capability not exposed). " +
                        "Feature gated off.";
                    Log.Warning(msg);
                    _testWarnings.Add(msg);
                }
            }
            return false;
        }

        /// <summary>
        /// Caller-side helper for "is package loaded at all" — used when the reader
        /// wants to know about a package's presence without binding to a specific
        /// capability.
        /// </summary>
        public static bool IsPackageActiveOrWarn(string packageId, string readerContext = null)
        {
            if (string.IsNullOrEmpty(packageId)) return false;
            bool active = PackageRegistry.IsRegistered(packageId);
            if (active) return true;

            string contextKey = readerContext ?? UnspecifiedContext;
            string key = $"{packageId}|_package_loaded|{contextKey}";
            lock (_lock)
            {
                if (_warnedKeys.Add(key))
                {
                    string contextLabel = readerContext ?? UnknownContext;
                    string msg =
                        "[Rimconemy.Foundation.CapabilityAudit] Reader '" + contextLabel + "': " +
                        "package '" + packageId + "' not registered.";
                    Log.Warning(msg);
                    _testWarnings.Add(msg);
                }
            }
            return false;
        }

        // ── test helpers ─────────────────────────────────────────────────

        /// <summary>Test-only: clear the once-warning cache so tests can re-fire.</summary>
        public static void ClearWarningCache()
        {
            lock (_lock)
            {
                _warnedKeys.Clear();
                _testWarnings.Clear();
            }
        }

        /// <summary>Test-only: snapshot of warnings emitted since last ClearWarningCache().</summary>
        public static IReadOnlyList<string> Warnings()
        {
            lock (_lock) return _testWarnings.ToArray();
        }

        /// <summary>Count of unique warn-keys emitted (test introspection).</summary>
        public static int WarningCount()
        {
            lock (_lock) return _testWarnings.Count;
        }
    }
}
