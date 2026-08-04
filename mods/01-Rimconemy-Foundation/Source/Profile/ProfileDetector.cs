using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.Models;
using Verse;

namespace Rimconemy.Foundation.Profile
{
    /// <summary>
    /// Owner: Foundation
    /// Detects the current Rimconemy profile based on loaded packages and DLCs.
    ///
    /// Detection is purely runtime-based: it checks which Rimconemy packages have
    /// registered and which DLCs are active. No hard compile-time references to
    /// feature packages.
    ///
    /// Public entry point: <see cref="TryEmitDetection(out string)"/>. All callers
    /// that want detection triggered (static cctor, NotifyPackageRegistryChanged,
    /// save/load refresh, tests) MUST route through it so the `_lastEmittedSummary`
    /// dedup gate is exercised. Calling <see cref="DetectProfile()"/> directly
    /// bypasses logging entirely; intentionally internal to make this restriction
    /// enforced at compile time.
    ///
    /// Hook reason: Must run after all mods have loaded and registered their
    /// PackageDescriptors. Uses StaticConstructorOnStartup for ordering.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ProfileDetector
    {
        /// <summary>All five expected Rimconemy feature package IDs.</summary>
        private static readonly string[] ExpectedFeaturePackageIds =
        {
            "rimconemy.survivalprogression",
            "rimconemy.scavengerinfrastructure",
            "rimconemy.economyterritory",
            "rimconemy.infectedautomation",
        };

        /// <summary>All five target DLC names.</summary>
        private static readonly string[] TargetDlcNames =
        {
            "Royalty",
            "Ideology",
            "Biotech",
            "Anomaly",
            "Odyssey",
        };

        public static ProfileStatus CurrentProfile { get; private set; } = ProfileStatus.Standalone;

        public static IReadOnlyList<string> MissingPackageIds { get; private set; } = new List<string>();
        public static IReadOnlyList<string> MissingDlcNames { get; private set; } = new List<string>();

        public static IReadOnlyList<DlcStatus> DlcStatuses { get; private set; } = new List<DlcStatus>();

        private const int SupportedSaveSchemaVersion = 1;
        private static bool _detected;
        private static bool _initialized;
        private static bool _initializing;

        // Snapshot of `PackageRegistry.RegisteredPackageIds` sorted in
        // Ordinal-stable order. DetectProfile populates this when it recomputes
        // state; BuildSummaryMessage reads it. Eliminates the TOCTOU read where
        // a late-bound registration between DetectProfile and BuildSummaryMessage
        // would surface a different package list in the log line than what
        // DetectProfile just snapshotted.
        private static string[] _lastSortedRegisteredIdsForSummary;

        // Dedup token for the canonical "Profile detected" line. ResetForReload
        // clears it so a save/load cycle with an identical mod list still emits
        // the post-reload lifecycle confirmation; otherwise the post-reload call
        // would silently dedup against the pre-reload string.
        private static string _lastEmittedSummary;

        static ProfileDetector()
        {
            _initializing = true;
            if (TryEmitDetection(out string firstSummary))
                Log.Message(firstSummary);
            _initializing = false;
            _initialized = true;
        }

        /// <summary>
        /// Refreshes profile state after a late-bound package registration.
        /// PackageRegistry invokes this after each accepted descriptor so a
        /// feature package loading after Foundation cannot leave stale Partial state.
        /// </summary>
        public static void NotifyPackageRegistryChanged()
        {
            if (!_initialized || _initializing)
                return;

            _detected = false;
            if (TryEmitDetection(out string summary))
                Log.Message(summary);
        }

        /// <summary>
        /// Allows an explicit save/load refresh. The active mod list is normally
        /// stable during a session; the reset exists for lifecycle re-entry only.
        /// Clears the dedup token so the post-reload detection emits a fresh
        /// summary line even if the mod list is unchanged.
        /// </summary>
        public static void ResetForReload()
        {
            _detected = false;
            _lastEmittedSummary = null;
            _lastSortedRegisteredIdsForSummary = null;
        }

        /// <summary>
        /// Sole public entry point for triggering profile detection. Routes the
        /// summary emission through <c>_lastEmittedSummary</c> dedup so identical
        /// adjacent states (e.g. the Foundation static-cctor re-entry where the
        /// PackageRegistry.Register(rimconemy.survivalprogression) callback wakes
        /// ProfileDetector's cctor mid-flight) collapse to a single log line.
        ///
        /// Returns <c>true</c> when a NEW summary line was emitted; <c>false</c>
        /// when the summary matched the previous emission and was deduplicated.
        /// <paramref name="summary"/> is always populated so callers can surface
        /// the canonical line without rebuilding it.
        /// </summary>
        public static bool TryEmitDetection(out string summary)
        {
            DetectProfile();
            summary = BuildSummaryMessage();
            if (string.Equals(summary, _lastEmittedSummary, System.StringComparison.Ordinal))
                return false;
            _lastEmittedSummary = summary;
            return true;
        }

        /// <summary>
        /// Pure state mutation. No log emission. <see cref="TryEmitDetection(out string)"/>
        /// is the canonical public entry; this method is exposed as <c>internal</c>
        /// so external callers (tests, runtime hooks) cannot bypass the dedup
        /// gate. Hot path: called once per detection.
        /// </summary>
        internal static void DetectProfile()
        {
            if (_detected) return;
            _detected = true;
            var missingPackages = new List<string>();
            var missingDlcs = new List<string>();
            var dlcStatuses = new List<DlcStatus>();

            // Check which feature packages have registered
            var registeredIds = new HashSet<string>(Registry.PackageRegistry.RegisteredPackageIds);
            var sortedRegisteredIds = registeredIds.OrderBy(id => id, System.StringComparer.Ordinal).ToList();

            foreach (var expectedId in ExpectedFeaturePackageIds)
            {
                if (!registeredIds.Contains(expectedId))
                    missingPackages.Add(expectedId);
            }

            // Check DLCs
            foreach (var dlcName in TargetDlcNames)
            {
                bool loaded = IsDlcLoaded(dlcName);
                dlcStatuses.Add(new DlcStatus(dlcName, loaded,
                    loaded ? "Active" : "Not installed"));

                if (!loaded)
                    missingDlcs.Add(dlcName);
            }

            MissingPackageIds = missingPackages;
            MissingDlcNames = missingDlcs;
            DlcStatuses = dlcStatuses;

            // Determine profile
            if (missingPackages.Count == ExpectedFeaturePackageIds.Length)
            {
                // No feature packages at all -- standalone
                CurrentProfile = ProfileStatus.Standalone;
            }
            else if (missingPackages.Count == 0
                && missingDlcs.Count == 0
                && AreAllPackagesFullCompatible())
            {
                // All packages, all DLCs, and compatible save/profile contracts.
                CurrentProfile = ProfileStatus.FullOverhaul;
            }
            else
            {
                // Some but not all packages, or missing DLCs
                CurrentProfile = ProfileStatus.Partial;
            }

            // Snapshot for BuildSummaryMessage. Stores the same Ordinal-sorted
            // view so the helper can build a stable string without re-reading
            // the live registry (which could drift mid-summary-build).
            _lastSortedRegisteredIdsForSummary = sortedRegisteredIds.ToArray();
        }

        /// <summary>
        /// Builds the canonical "Profile detected" log line using the snapshot
        /// last set by <see cref="DetectProfile"/>. Pure function; no state
        /// mutation, no logging.
        /// </summary>
        private static string BuildSummaryMessage()
        {
            return "[Rimconemy.Foundation] Profile detected: " + CurrentProfile
                + " (packages registered: " + string.Join(",", _lastSortedRegisteredIdsForSummary)
                + ", missing: " + MissingPackageIds.Count
                + ", DLCs missing: " + MissingDlcNames.Count + ")";
        }

        private static bool AreAllPackagesFullCompatible()
        {
            foreach (var packageId in ExpectedFeaturePackageIds)
            {
                var descriptor = Registry.PackageRegistry.GetDescriptor(packageId);
                if (descriptor == null
                    || descriptor.SaveSchemaVersion != SupportedSaveSchemaVersion
                    || descriptor.ProfileCompatibility != ProfileCompatibility.StandaloneAndFull)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a DLC is loaded by inspecting ModContentPack metadata.
        /// Checks against both Name and PackageId to handle different RimWorld versions.
        /// Silent-by-design: TryEmitDetection's single dedup'd "Profile detected"
        /// line already reports the aggregate `DLCs missing: N` count, so per-DLC
        /// log lines here were redundant. The Foundation cctor re-entry (which
        /// triggers DetectProfile twice for the same state) would otherwise have
        /// duplicated the 5x2 `DLC detected by Name match` log lines alongside the
        /// `Profile detected` dedup.
        /// </summary>
        private static bool IsDlcLoaded(string dlcName)
        {
            foreach (var mod in LoadedModManager.RunningMods)
            {
                // Check both the display name and the package ID variants
                if (mod.Name == dlcName) return true;
                if (mod.PackageId == $"Ludeon.RimWorld.{dlcName}") return true;
                if (mod.PackageId == dlcName) return true;
            }
            return false;
        }
    }
}
