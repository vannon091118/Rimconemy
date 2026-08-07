using System.Collections.Generic;
using Rimconemy.Foundation.Models;
using Rimconemy.Foundation.Registry;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    /// Phase B Sprint — Capability-Mock-Tests.
    ///
    /// Self-contained unit tests for <see cref="CapabilityAudit"/>.
    /// Style matches StorySelectorTests: bespoke Assert helpers,
    /// RunAll() entry point, no NUnit/xUnit dependency.
    ///
    /// Strategy: register synthetic packages via the public
    /// <see cref="PackageRegistry.Register"/> path and verify gate behavior
    /// for the three states: capability missing, capability satisfied,
    /// once-warning cache.
    /// </summary>
    public static class FoundationCapabilityGateTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();

        public static bool RunAll()
        {
            ts = new TestSuite("Foundation", "CapabilityGate tests");

            _passed = 0;
            _failed = 0;
            _failures.Clear();

            // Each test starts with a clean warning cache so once-logic
            // is exercised fresh.
            CapabilityAudit.ClearWarningCache();

            TestHasCapabilityOrWarn_CapabilityMissing_LogsOnce();
            TestHasCapabilityOrWarn_CapabilitySatisfied_NoLog();
            TestHasCapabilityOrWarn_OnceWarningPerTuple();
            TestHasCapabilityOrWarn_DifferentReaderContexts();
            TestIsPackageActiveOrWarn_PackageMissing();
            TestMockRegisterSatisfiedCapability_NoWarn();

            string summary = "[Rimconemy.Foundation] CapabilityGate tests: " +
                _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                foreach (var f in _failures)
                    Log.Error("[Rimconemy.Foundation] TEST FAILED: " + f);
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return true;
        }

        // ── helpers (mirror StorySelectorTests) ──────────────────


        // ── tests ───────────────────────────────────────────────

        /// <summary>
        /// Audit-1.1: capability missing → false, log emitted exactly once.
        /// </summary>
        private static void TestHasCapabilityOrWarn_CapabilityMissing_LogsOnce()
        {
            CapabilityAudit.ClearWarningCache();

            const string CapId = "rimconemy.synthetic.missing_test";
            // Tip: capability must not exist by default. We don't register it.

            bool result1 = CapabilityAudit.HasCapabilityOrWarn(
                packageId: "rimconemy.synthetic",
                capabilityId: CapId,
                minVersion: 1,
                readerContext: "Test1.1");

            ts.Check(!(result1), "Audit-1.1: false on missing capability");
            ts.Check(Equals(1, CapabilityAudit.WarningCount()), "Audit-1.1: 1 warning logged");

            // Second call with same readerContext → no new warning.
            bool result2 = CapabilityAudit.HasCapabilityOrWarn(
                "rimconemy.synthetic", CapId, 1, "Test1.1");
            ts.Check(!(result2), "Audit-1.1: false on repeat");
            ts.Check(Equals(1, CapabilityAudit.WarningCount()), "Audit-1.1: still 1 warning");
        }

        /// <summary>
        /// Audit-1.2: capability satisfied → true, no warning.
        /// </summary>
        private static void TestHasCapabilityOrWarn_CapabilitySatisfied_NoLog()
        {
            // Use the `rimconemy.foundation.profile` capability that
            // PackageRegistry's static ctor registers (Foundation's own).
            CapabilityAudit.ClearWarningCache();

            bool result = CapabilityAudit.HasCapabilityOrWarn(
                packageId: "rimconemy.foundation",
                capabilityId: "rimconemy.foundation.profile",
                minVersion: 1,
                readerContext: "Test1.2");

            ts.Check(result, "Audit-1.2: true on satisfied capability");
            ts.Check(Equals(0, CapabilityAudit.WarningCount()), "Audit-1.2: 0 warnings logged");
        }

        /// <summary>
        /// Audit-1.3: once-warning per tuple (packageId, capId, version, context).
        /// </summary>
        private static void TestHasCapabilityOrWarn_OnceWarningPerTuple()
        {
            CapabilityAudit.ClearWarningCache();

            // Same package+cap, different readerContext: 2 warnings.
            bool r1 = CapabilityAudit.HasCapabilityOrWarn(
                "rimconemy.synthetic1", "rimconemy.synthetic.cap_x", 1, "CtxA");
            bool r2 = CapabilityAudit.HasCapabilityOrWarn(
                "rimconemy.synthetic1", "rimconemy.synthetic.cap_x", 1, "CtxB");

            ts.Check(!(r1), "Audit-1.3: r1 false");
            ts.Check(!(r2), "Audit-1.3: r2 false");
            ts.Check(Equals(2, CapabilityAudit.WarningCount()), "Audit-1.3: 2 warnings for 2 reader contexts");

            // Same package+cap+context again: still 2.
            bool r3 = CapabilityAudit.HasCapabilityOrWarn(
                "rimconemy.synthetic1", "rimconemy.synthetic.cap_x", 1, "CtxA");
            ts.Check(!(r3), "Audit-1.3: r3 false");
            ts.Check(Equals(2, CapabilityAudit.WarningCount()), "Audit-1.3: still 2 warnings");
        }

        /// <summary>
        /// Audit-1.4: readerContext string is captured in the warning message.
        /// </summary>
        private static void TestHasCapabilityOrWarn_DifferentReaderContexts()
        {
            CapabilityAudit.ClearWarningCache();

            CapabilityAudit.HasCapabilityOrWarn(
                "rimconemy.synthetic2", "rimconemy.synthetic.cap_y", 1, "MyTestReader");

            var warnings = CapabilityAudit.Warnings();
            ts.Check(Equals(1, warnings.Count), "Audit-1.4: 1 warning");
            ts.Check(warnings[0].Contains("MyTestReader"), "Audit-1.4: warning contains reader context label");
            ts.Check(warnings[0].Contains("rimconemy.synthetic.cap_y"), "Audit-1.4: warning contains capability id");
        }

        /// <summary>
        /// Audit-1.5: IsPackageActiveOrWarn without registration is false + warn.
        /// </summary>
        private static void TestIsPackageActiveOrWarn_PackageMissing()
        {
            CapabilityAudit.ClearWarningCache();

            bool result = CapabilityAudit.IsPackageActiveOrWarn(
                "rimconemy.nonexistent.package",
                "Test1.5");

            ts.Check(!(result), "Audit-1.5: false on missing package");
            ts.Check(Equals(1, CapabilityAudit.WarningCount()), "Audit-1.5: 1 warning");
        }

        /// <summary>
        /// Audit-1.6: Mock-register a synthetic package with the capability and
        /// verify HasCapabilityOrWarn returns true with no warning.
        /// Idempotent across re-runs: a second RunAll() does not assert duplicate
        /// registration failure (the package is reused).
        /// </summary>
        private static void TestMockRegisterSatisfiedCapability_NoWarn()
        {
            CapabilityAudit.ClearWarningCache();

            const string MockPackageId = "rimconemy.tests.synthetic";
            const string MockCapId = "rimconemy.tests.synthetic.feature";

            // Skip if already registered. Don't try to Register twice — the
            // static registry rejects duplicates and would log a warning we
            // don't want.
            if (!PackageRegistry.IsRegistered(MockPackageId))
            {
                var descriptor = new PackageDescriptor(
                    packageId: MockPackageId,
                    packageVersion: "9.9.9-test",
                    saveSchemaVersion: 1,
                    capabilities: new List<Capability> { new Capability(MockCapId, 1) },
                    profileCompatibility: ProfileCompatibility.StandaloneAndFull);
                bool registered = PackageRegistry.Register(descriptor);
                ts.Check(registered, "Audit-1.6: synthetic package registered");
            }

            bool result = CapabilityAudit.HasCapabilityOrWarn(
                MockPackageId, MockCapId, 1, "Test1.6");

            ts.Check(result, "Audit-1.6: true after registration");
            ts.Check(Equals(0, CapabilityAudit.WarningCount()), "Audit-1.6: no warning when satisfied");
        }
    }
}
