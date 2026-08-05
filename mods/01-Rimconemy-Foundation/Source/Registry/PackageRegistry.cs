using System;
using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.Models;
using Verse;

namespace Rimconemy.Foundation.Registry
{
    /// <summary>
    /// Owner: Foundation
    /// Canonical package registry. Each Rimconemy package registers exactly once
    /// at startup. Feature packages are late-bound -- no direct assembly references.
    ///
    /// In standalone mode (only Foundation loaded), the registry contains only
    /// Foundation itself. In Full Profile, it contains all five packages.
    ///
    /// Hook reason: StaticConstructorOnStartup ensures registration runs after
    /// all mods have loaded their assemblies but before the game world initializes.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class PackageRegistry
    {
        private static readonly Dictionary<string, PackageDescriptor> _packages
            = new Dictionary<string, PackageDescriptor>();

        private static readonly object _lock = new object();

        /// <summary>All registered package IDs.</summary>
        public static IReadOnlyList<string> RegisteredPackageIds
        {
            get
            {
                lock (_lock)
                    return _packages.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
            }
        }

        /// <summary>Number of registered Rimconemy packages.</summary>
        public static int RegisteredCount
        {
            get
            {
                lock (_lock)
                    return _packages.Count;
            }
        }

        /// <summary>
        /// Registers a package descriptor. Rejects duplicate IDs.
        /// Returns true on success, false if the package ID is already registered.
        /// </summary>
        public static bool Register(PackageDescriptor descriptor)
        {
            if (!IsValidDescriptor(descriptor))
            {
                Log.Warning("[Rimconemy.Foundation] Package registration rejected: descriptor has missing or invalid fields.");
                return false;
            }

            lock (_lock)
            {
                if (_packages.ContainsKey(descriptor.PackageId))
                {
                    Log.Warning($"[Rimconemy.Foundation] Duplicate package registration rejected: {descriptor.PackageId}");
                    return false;
                }

                _packages[descriptor.PackageId] = descriptor;
                Log.Message($"[Rimconemy.Foundation] Package registered: {descriptor.PackageId} v{descriptor.PackageVersion}");
            }

            // Notify only late-bound feature registrations. Foundation's own
            // static registration must not recursively initialize ProfileDetector.
            if (!string.Equals(descriptor.PackageId, "rimconemy.foundation", StringComparison.Ordinal))
                Profile.ProfileDetector.NotifyPackageRegistryChanged();
            return true;
        }

        private static bool IsValidDescriptor(PackageDescriptor descriptor)
        {
            if (descriptor == null
                || string.IsNullOrEmpty(descriptor.PackageId)
                || string.IsNullOrEmpty(descriptor.PackageVersion)
                || descriptor.SaveSchemaVersion < 1
                || descriptor.Capabilities == null
                || !System.Enum.IsDefined(typeof(ProfileCompatibility), descriptor.ProfileCompatibility))
                return false;

            foreach (var capability in descriptor.Capabilities)
            {
                if (capability == null
                    || string.IsNullOrEmpty(capability.CapabilityId)
                    || capability.Version < 1)
                    return false;
            }

            return true;
        }

        /// <summary>Returns the descriptor for a package, or null if not registered.</summary>
        public static PackageDescriptor GetDescriptor(string packageId)
        {
            lock (_lock)
            {
                _packages.TryGetValue(packageId, out var descriptor);
                return descriptor;
            }
        }

        /// <summary>Returns true if the given package is registered.</summary>
        public static bool IsRegistered(string packageId)
        {
            lock (_lock)
            {
                return _packages.ContainsKey(packageId);
            }
        }

        /// <summary>
        /// Returns true if the package is registered and exposes the given capability
        /// with at least the specified version.
        /// </summary>
        public static bool HasCapability(string packageId, string capabilityId, int minVersion = 1)
        {
            lock (_lock)
            {
                if (!_packages.TryGetValue(packageId, out var descriptor))
                    return false;

                return descriptor.Capabilities.Any(c =>
                    c.CapabilityId == capabilityId && c.Version >= minVersion);
            }
        }

        /// <summary>Returns package IDs and versions in a stable, sorted order.</summary>
        public static IReadOnlyList<string> GetRegisteredPackageVersions()
        {
            lock (_lock)
            {
                return _packages
                    .OrderBy(pair => pair.Key)
                    .Select(pair => $"{pair.Key}={pair.Value.PackageVersion}")
                    .ToList();
            }
        }

        /// <summary>Returns all registered capabilities across all packages.</summary>
        public static IReadOnlyList<Capability> GetAllCapabilities()
        {
            lock (_lock)
            {
                return _packages
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .SelectMany(pair => pair.Value.Capabilities
                        .OrderBy(capability => capability.CapabilityId, StringComparer.Ordinal)
                        .ThenBy(capability => capability.Version))
                    .ToList();
            }
        }

        static PackageRegistry()
        {
            // Foundation registers itself first, then discovers optional feature
            // assemblies through the stable package/assembly contract. This keeps
            // Foundation standalone without compile-time references while making
            // the Full Overhaul profile truthful when all packages are loaded.
            Register(new PackageDescriptor(
                packageId: "rimconemy.foundation",
                packageVersion: "0.1.39",
                saveSchemaVersion: 1,
                capabilities: new List<Capability>
                {
                    new Capability("rimconemy.foundation.profile", 1),
                    new Capability("rimconemy.foundation.eventlog", 1),
                    new Capability("rimconemy.foundation.save_diagnosis", 1),
                    // Phase-B / F-V1 (2026-08-04 audit-fix 2026-08-04):
                    // ColonialReader.GetActiveColonists() is the single
                    // source of truth for pawn enumeration. Mods 02/03/05
                    // check this capability before iterating colonist
                    // lists. Audited gap: capability was documented but
                    // never registered — HasCapability() returned false
                    // silently for every consumer.
                    new Capability("rimconemy.foundation.colonials", 1),
                    // Phase-7 DLC Opt-In Architecture (2026-08-04): the
                    // DLCFilter is the single source of truth for which
                    // RimWorld DLC content is enabled. Consumers in
                    // Mod 02..05 check this capability before invoking
                    // any DLCFeature method.
                    new Capability("rimconemy.foundation.dlc_filter", 1),
                },
                profileCompatibility: ProfileCompatibility.StandaloneAndFull));

            RegisterLoadedFeaturePackages();
        }

        /// <summary>
        /// Registers only feature assemblies that are actually loaded by RimWorld.
        /// The assembly name is the stable boundary; no feature type is imported.
        /// This is intentionally centralized so package registration cannot drift
        /// between four independent bootstrap implementations.
        /// </summary>
        /// <summary>
        /// Rechecks optional assemblies after RimWorld has finished loading mods.
        /// Safe to call repeatedly; existing package IDs are left unchanged.
        /// </summary>
        public static void RefreshLoadedFeaturePackages()
        {
            RegisterLoadedFeaturePackages();
        }

        private static void RegisterLoadedFeaturePackages()
        {
            TryRegisterLoadedAssembly(
                assemblyName: "Rimconemy.SurvivalProgression",
                packageId: "rimconemy.survivalprogression",
                packageVersion: "0.1.40",
                capabilities: new[]
                {
                    new Capability("rimconemy.survivalprogression.needs", 1),
                    new Capability("rimconemy.survivalprogression.progression", 1),
                });

            TryRegisterLoadedAssembly(
                assemblyName: "Rimconemy.ScavengerInfrastructure",
                packageId: "rimconemy.scavengerinfrastructure",
                packageVersion: "0.0.28",
                capabilities: new[]
                {
                    new Capability("rimconemy.scavengerinfrastructure.resources", 1),
                    new Capability("rimconemy.scavengerinfrastructure.power", 1),
                    new Capability("rimconemy.scavengerinfrastructure.building", 1),
                });

            TryRegisterLoadedAssembly(
                assemblyName: "Rimconemy.EconomyTerritory",
                packageId: "rimconemy.economyterritory",
                packageVersion: "0.0.29",
                capabilities: new[]
                {
                    new Capability("rimconemy.economyterritory.wallet", 1),
                    new Capability("rimconemy.economyterritory.market", 1),
                    new Capability("rimconemy.economyterritory.outposts", 1),
                    new Capability("rimconemy.economyterritory.physical_transfer", 1),
                });

            TryRegisterLoadedAssembly(
                assemblyName: "Rimconemy.InfectedAutomation",
                packageId: "rimconemy.infectedautomation",
                packageVersion: "0.0.69",
                capabilities: new[]
                {
                    new Capability("rimconemy.infectedautomation.threat", 1),
                    new Capability("rimconemy.infectedautomation.automation", 1),
                    new Capability("rimconemy.infectedautomation.mechadroid_jobs", 1),
                    // Phase A (2026-08-05): PopulationLedger SSOT reads from
                    // Mod 02/03/05. Consumers check
                    // rimconemy.infectedautomation.population before
                    // calling PopulationLedger.Get(), so standalone 05 users
                    // without the ledger still see the legacy Threat path.
                    new Capability("rimconemy.infectedautomation.population", 1),
                });
        }

        private static void TryRegisterLoadedAssembly(
            string assemblyName,
            string packageId,
            string packageVersion,
            IEnumerable<Capability> capabilities)
        {
            if (IsRegistered(packageId))
                return;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                    continue;

                Register(new PackageDescriptor(
                    packageId,
                    packageVersion,
                    saveSchemaVersion: 1,
                    capabilities: capabilities.ToList(),
                    profileCompatibility: ProfileCompatibility.StandaloneAndFull));
                return;
            }

            Log.Message($"[Rimconemy.Foundation] Optional package assembly not loaded: {assemblyName}");
        }
    }
}
