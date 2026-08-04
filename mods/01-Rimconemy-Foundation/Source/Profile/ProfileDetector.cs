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

        static ProfileDetector()
        {
            _initializing = true;
            DetectProfile();
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
            DetectProfile();
        }

        /// <summary>
        /// Allows an explicit save/load refresh. The active mod list is normally
        /// stable during a session; the reset exists for lifecycle re-entry only.
        /// </summary>
        public static void ResetForReload()
        {
            _detected = false;
        }

        /// <summary>
        /// Re-runs profile detection. In normal operation this runs once after
        /// all mods initialize, since DLCs and package registrations don't change
        /// during gameplay. Save/Load or mod-list changes require re-detection.
        /// </summary>
        public static void DetectProfile()
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

            Log.Message($"[Rimconemy.Foundation] Profile detected: {CurrentProfile} " +
                $"(packages registered: {string.Join(",", sortedRegisteredIds)}, missing: {missingPackages.Count}, " +
                $"DLCs missing: {missingDlcs.Count})");
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
        /// </summary>
        private static bool IsDlcLoaded(string dlcName)
        {
            foreach (var mod in LoadedModManager.RunningMods)
            {
                // Check both the display name and the package ID variants
                if (mod.Name == dlcName)
                {
                    Log.Message($"[Rimconemy.Foundation] DLC '{dlcName}' detected by Name match: '{mod.Name}'");
                    return true;
                }
                if (mod.PackageId == $"Ludeon.RimWorld.{dlcName}")
                {
                    Log.Message($"[Rimconemy.Foundation] DLC '{dlcName}' detected by PackageId match: '{mod.PackageId}'");
                    return true;
                }
                if (mod.PackageId == dlcName)
                {
                    Log.Message($"[Rimconemy.Foundation] DLC '{dlcName}' detected by exact PackageId match: '{mod.PackageId}'");
                    return true;
                }
            }
            Log.Message($"[Rimconemy.Foundation] DLC '{dlcName}' NOT detected among running mods");
            return false;
        }
    }
}
