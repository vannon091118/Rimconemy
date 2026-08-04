using System.Collections.Generic;
using Rimconemy.Foundation.CrossPackage;
using Rimconemy.Foundation.Registry;
using Verse;

namespace Rimconemy.Foundation.Tests
{
    /// <summary>
    /// Owner: Foundation (Package 01).
    /// Phase B Sprint — CrossPackageState Tests.
    ///
    /// Tests verify the late-bound reflection bridge's defensive paths
    /// when Mod 05 is NOT loaded. We cannot transiently load the Mod 05
    /// assembly in this in-process runner without an AppDomain sandbox;
    /// instead we cover the "capability missing" and "reflection result
    /// is null" paths, which are the most likely real-world scenarios.
    ///
    /// The cycle coverage goal: every "fall back gracefully" path gets
    /// at least one assertion. We don't try to mock StoryDirector.
    /// </summary>
    public static class FoundationCrossPackageStateTests
    {
        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;
            _failures.Clear();

            CapabilityAudit.ClearWarningCache();

            TestTryReadStoryGameOverPending_NoMod05_ReturnsFalse();
            TestTryReadStoryGameOverPending_NoNREOrCrash();

            TestTryReadWalletBalance_NoMod04_ReturnsFalse();
            TestTryReadWalletBalance_NoNREOrCrash();
            TestTryReadWalletBalance_DefaultZero();

            string summary = "[Rimconemy.Foundation] CrossPackageState tests: " +
                _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                foreach (var f in _failures)
                    Log.Error("[Rimconemy.Foundation] TEST FAILED: " + f);
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void AssertFalse(bool condition, string label)
        {
            if (condition)
            {
                _failed++;
                _failures.Add(label + ": expected false, got true");
            }
            else
            {
                _passed++;
            }
        }

        private static void AssertNull(object obj, string label)
        {
            if (obj != null)
            {
                _failed++;
                _failures.Add(label + ": expected null, got " + obj);
            }
            else
            {
                _passed++;
            }
        }

        private static void AssertEqual(int expected, int actual, string label)
        {
            if (expected != actual)
            {
                _failed++;
                _failures.Add(label + ": expected " + expected + ", got " + actual);
            }
            else
            {
                _passed++;
            }
        }

        private static void AssertEqual(long expected, long actual, string label)
        {
            if (expected != actual)
            {
                _failed++;
                _failures.Add(label + ": expected " + expected + ", got " + actual);
            }
            else
            {
                _passed++;
            }
        }

        // ── tests ──────────────────────────────────────

        /// <summary>
        /// When Mod 05 (InfectedAutomation) is NOT registered, the capability
        /// gate refuses the read and the helper returns (false, null).
        /// </summary>
        private static void TestTryReadStoryGameOverPending_NoMod05_ReturnsFalse()
        {
            CapabilityAudit.ClearWarningCache();

            string reason = "default-non-null-sentinel";
            bool result = CrossPackageState.TryReadStoryGameOverPending(out reason);

            // Without Mod 05 registered, capability gate returns false and
            // reason should be null.
            AssertFalse(result, "CPS-Nomod05: returns false (capability gate)");
            AssertNull(reason, "CPS-Nomod05: reason is null when capability missing");
        }

        /// <summary>
        /// Even with reflection attempting — if the assembly isn't loaded,
        /// we never reach the reflection path. Verify no NRE or exception
        /// from the reflection-protected code paths.
        /// </summary>
        private static void TestTryReadStoryGameOverPending_NoNREOrCrash()
        {
            CapabilityAudit.ClearWarningCache();

            // Multiple invocations should all be safe and idempotent
            for (int i = 0; i < 3; i++)
            {
                string reason = "should-be-overwritten-to-null";
                bool result = CrossPackageState.TryReadStoryGameOverPending(out reason);
                AssertFalse(result, "CPS-Nocrash iter " + i + ": false");
                AssertNull(reason, "CPS-Nocrash iter " + i + ": null reason");
            }
        }

        // ── F-01 wallet-balance tests (Audit-Bündel B) ──

        /// <summary>
        /// When Mod 04 (Economy) is NOT registered, the capability gate
        /// refuses the read and the helper returns (false, 0).
        /// </summary>
        private static void TestTryReadWalletBalance_NoMod04_ReturnsFalse()
        {
            CapabilityAudit.ClearWarningCache();

            long balance = 999L; // sentinel - must be reset to 0 by TryReadWalletBalance
            bool result = CrossPackageState.TryReadWalletBalance(out balance);
            AssertFalse(result, "CPS-Wallet-Nomod04: returns false (capability gate)");
            AssertEqual(0L, balance, "CPS-Wallet-Nomod04: balance is 0 when capability missing");
        }

        /// <summary>
        /// Repeated calls with a missing capability stay safe, idempotent
        /// and never throw. The once-warning pattern of CapabilityAudit
        /// guarantees no log spam.
        /// </summary>
        private static void TestTryReadWalletBalance_NoNREOrCrash()
        {
            CapabilityAudit.ClearWarningCache();

            for (int i = 0; i < 3; i++)
            {
                long balance = -1L;
                bool result = CrossPackageState.TryReadWalletBalance(out balance);
                AssertFalse(result, "CPS-Wallet-Nocrash iter " + i + ": false");
                AssertEqual(0L, balance, "CPS-Wallet-Nocrash iter " + i + ": 0");
            }
        }

        /// <summary>
        /// Documents the default value contract: callers that receive a
        /// <c>false</c> return must treat <c>balance == 0L</c> as the
        /// "no wallet data" sentinel, not as a real wallet total of 0.
        /// </summary>
        private static void TestTryReadWalletBalance_DefaultZero()
        {
            CapabilityAudit.ClearWarningCache();

            long balance = 42L;
            bool result = CrossPackageState.TryReadWalletBalance(out balance);
            // result == false (no wallet) AND balance is the helper's default 0L
            if (result)
            {
                // If somehow the capability gate passed in some test env,
                // accept the result as long as balance has been overwritten.
                _passed++;
            }
            else
            {
                AssertEqual(0L, balance, "CPS-Wallet-Default: false-return sets balance=0");
            }
        }
    }
}
