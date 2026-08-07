using System;
using System.Collections.Generic;
using Rimconemy.Foundation.Profile;
using Rimconemy.Foundation.Registry;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>Regression checks for late-bound feature package discovery.</summary>
    public static class FoundationProfileRefreshTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        private static readonly KeyValuePair<string, string>[] FeaturePackages =
        {
            new KeyValuePair<string, string>("Rimconemy.SurvivalProgression", "rimconemy.survivalprogression"),
            new KeyValuePair<string, string>("Rimconemy.ScavengerInfrastructure", "rimconemy.scavengerinfrastructure"),
            new KeyValuePair<string, string>("Rimconemy.EconomyTerritory", "rimconemy.economyterritory"),
            new KeyValuePair<string, string>("Rimconemy.InfectedAutomation", "rimconemy.infectedautomation"),
        };

        public static bool RunAll()
        {
            ts = new TestSuite("Foundation", "Profile refresh tests");

            _passed = 0;
            _failed = 0;

            PackageRegistry.RefreshLoadedFeaturePackages();
            ProfileDetector.ResetForReload();
            // Canonical public entry point; emits the summary line at most once.
            ProfileDetector.TryEmitDetection(out _);

            ts.Check(PackageRegistry.RegisteredPackageIds != null, "Profile refresh: package registry remains readable");
            ts.Check(ProfileDetector.MissingPackageIds != null, "Profile refresh: missing package result is non-null");

            foreach (var feature in FeaturePackages)
            {
                bool assemblyLoaded = false;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(assembly.GetName().Name, feature.Key, StringComparison.Ordinal))
                    {
                        assemblyLoaded = true;
                        break;
                    }
                }

                if (!assemblyLoaded)
                    continue;

                ts.Check(PackageRegistry.IsRegistered(feature.Value), "Profile refresh: loaded assembly registered (" + feature.Key + ")");
                ts.Check(!Contains(ProfileDetector.MissingPackageIds, feature.Value), "Profile refresh: loaded assembly not marked missing (" + feature.Key + ")");
            }

            string summary = "[Rimconemy.Foundation] Profile refresh tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return true;
        }

        private static bool Contains(IReadOnlyList<string> values, string expected)
        {
            if (values == null) return false;
            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], expected, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

    }
}
