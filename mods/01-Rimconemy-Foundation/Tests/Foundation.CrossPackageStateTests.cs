using System.Collections.Generic;
using Rimconemy.Foundation.CrossPackage;
using Rimconemy.Foundation.Registry;
using Verse;
using Rimconemy.Foundation.Tests;

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
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;
        private static readonly List<string> _failures = new List<string>();

        public static bool RunAll()
        {
            ts = new TestSuite("Foundation", "CrossPackageState tests");

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

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
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

        private static void AssertTrue(bool condition, string label)
        {
            if (!condition)
            {
                _failed++;
                _failures.Add(label + ": expected true, got false");
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
        //
        // ROOT-CAUSE-FIX (Phase 8.6 / 2026-08-05):
        //
        // The historic F-01 tests assumed a cold-start test profile
        // with Mod 04 (EconomyTerritory) NOT registered. In FullOverhaul
        // runtime-profile tests (which is what `runtime_test.sh` exercises),
        // Mod 04 IS registered and the wallet is reachable. The helper then
        // returns (true, realBalance), which the old assertions flagged as
        // a hard failure ("expected false, got true" / "expected 0L, got X").
        //
        // The tests are now environment-aware: they ask
        // `PackageRegistry.IsRegistered(...)` first and assert the
        // branch-specific contract. Both branches must hold their own
        // invariants (no NRE, sentinel balance always reset). This is the
        // same shape as the StoryDirector tests above, which already pass
        // in FullOverhaul because the helper can legitimately return false
        // when there is no pending game-over — regardless of Mod 05 status.

        private const string Mod04Id = "rimconemy.economyterritory";

        /// <summary>
        /// Verifies the wallet helper defends against the "Mod 04 not
        /// loaded" path. In that branch the helper MUST return
        /// <c>(false, 0L)</c>. In the "Mod 04 loaded" branch the helper
        /// MUST return <c>(true, realBalance)</c> with no sentinel survive.
        /// Either outcome is valid — both are tested through the same
        /// fixture depending on the runtime profile.
        /// </summary>
        private static void TestTryReadWalletBalance_NoMod04_ReturnsFalse()
        {
            // Snapshot Mod 04's presence BEFORE we clear warning caches,
            // because ClearWarningCache() does not touch the registry.
            bool mod04Active = PackageRegistry.IsRegistered(Mod04Id);

            CapabilityAudit.ClearWarningCache();

            long balance = 999L; // sentinel — must NOT survive the call.
            bool result = CrossPackageState.TryReadWalletBalance(out balance);

            if (!mod04Active)
            {
                // Absence branch: capability gate refuses the read.
                AssertFalse(result, "CPS-Wallet-Absent: returns false when Mod 04 not registered");
                AssertEqual(0L, balance, "CPS-Wallet-Absent: balance is 0 when capability missing");
            }
            else
            {
                // Presence branch: helper succeeds and returns a real
                // (possibly 0L-on-empty-wallet) total; the 999L sentinel
                // must NOT remain.
                AssertTrue(result, "CPS-Wallet-Present: returns true when Mod 04 registered");
                AssertTrue(balance != 999L, "CPS-Wallet-Present: sentinel 999L was overwritten");
                AssertTrue(balance >= 0L, "CPS-Wallet-Present: actual balance is non-negative");
            }
        }

        /// <summary>
        /// Repeated calls stay safe, idempotent and never throw. Same
        /// environment-awareness as
        /// <see cref="TestTryReadWalletBalance_NoMod04_ReturnsFalse"/>.
        /// </summary>
        private static void TestTryReadWalletBalance_NoNREOrCrash()
        {
            bool mod04Active = PackageRegistry.IsRegistered(Mod04Id);

            CapabilityAudit.ClearWarningCache();

            for (int i = 0; i < 3; i++)
            {
                long balance = -1L; // sentinel for the presence branch.
                bool result = CrossPackageState.TryReadWalletBalance(out balance);

                if (!mod04Active)
                {
                    AssertFalse(result, "CPS-Wallet-Nocrash iter " + i + ": returns false when Mod 04 absent");
                    AssertEqual(0L, balance, "CPS-Wallet-Nocrash iter " + i + ": balance is 0L when absent");
                }
                else
                {
                    AssertTrue(result, "CPS-Wallet-Nocrash iter " + i + ": returns true when Mod 04 present");
                    AssertTrue(balance != -1L, "CPS-Wallet-Nocrash iter " + i + ": sentinel -1L was overwritten");
                }
            }
        }

        /// <summary>
        /// Documents the dual contract for the <c>out balance</c> parameter:
        /// the helper MUST overwrite the caller's sentinel in BOTH branches:
        ///   - false-return path → balance == 0L (the helper's documented
        ///     "no wallet data" sentinel, not a real wallet total of 0).
        ///   - true-return  path → balance is the actual ledger total
        ///     (never the caller's input sentinel).
        /// </summary>
        private static void TestTryReadWalletBalance_DefaultZero()
        {
            CapabilityAudit.ClearWarningCache();

            long balance = 42L; // sentinel — must NOT survive the call.
            bool result = CrossPackageState.TryReadWalletBalance(out balance);

            if (result)
            {
                // Presence: 42L was overwritten by the actual wallet total
                // (which can legally be 0L on a fresh empty wallet).
                AssertTrue(balance != 42L, "CPS-Wallet-Default-Present: sentinel 42L was overwritten");
            }
            else
            {
                // Absence: 42L was overwritten by 0L.
                AssertEqual(0L, balance, "CPS-Wallet-Default-Absent: false-return sets balance=0L");
            }
        }
    }
}
