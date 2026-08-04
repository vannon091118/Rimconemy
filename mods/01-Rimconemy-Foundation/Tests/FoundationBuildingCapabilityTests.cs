using System.Collections.Generic;
using Rimconemy.Foundation.Models;
using Rimconemy.Foundation.Registry;
using Verse;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>Regression gate for the canonical Building capability contract.</summary>
    public static class FoundationBuildingCapabilityTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            TestBuildingCapabilityDeclaration();
            TestMissingCapabilityWarningIsDeduplicated();

            string summary = "[Rimconemy.Foundation] Building capability tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void TestBuildingCapabilityDeclaration()
        {
            const string packageId = "rimconemy.scavengerinfrastructure";
            const string capabilityId = "rimconemy.scavengerinfrastructure.building";
            bool packageAssemblyLoaded = false;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(assembly.GetName().Name,
                    "Rimconemy.ScavengerInfrastructure",
                    System.StringComparison.Ordinal))
                {
                    packageAssemblyLoaded = true;
                    break;
                }
            }

            if (!packageAssemblyLoaded)
            {
                Log.Message("[Rimconemy.Foundation] Building capability test: Package 03 assembly not loaded; standalone gate is not applicable.");
                return;
            }

            // StaticConstructorOnStartup ordering can load the assembly before
            // Foundation has completed its first discovery pass. Refresh here
            // so this gate tests the canonical registry, not timing.
            PackageRegistry.RefreshLoadedFeaturePackages();
            AssertTrue(PackageRegistry.IsRegistered(packageId),
                "Building capability: loaded Package 03 is registered");
            AssertTrue(PackageRegistry.HasCapability(packageId, capabilityId, 1),
                "Building capability: Package 03 exposes v1");
        }

        private static void TestMissingCapabilityWarningIsDeduplicated()
        {
            CapabilityAudit.ClearWarningCache();
            const string packageId = "rimconemy.synthetic.building-test";
            const string capabilityId = "rimconemy.synthetic.building-test.missing";
            AssertFalse(CapabilityAudit.HasCapabilityOrWarn(packageId, capabilityId, 1, "BuildingCapabilityTest"),
                "Building capability: missing gate returns false");
            AssertFalse(CapabilityAudit.HasCapabilityOrWarn(packageId, capabilityId, 1, "BuildingCapabilityTest"),
                "Building capability: repeated missing gate remains false");
            AssertEqual(1, CapabilityAudit.WarningCount(),
                "Building capability: missing warning is emitted once");
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[FoundationBuildingCapability] " + label); }
        }

        private static void AssertFalse(bool condition, string label) { AssertTrue(!condition, label); }

        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (EqualityComparer<T>.Default.Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[FoundationBuildingCapability] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
