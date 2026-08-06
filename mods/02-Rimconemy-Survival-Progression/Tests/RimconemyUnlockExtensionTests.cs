using Rimconemy.SurvivalProgression.Progression;
using Rimconemy.SurvivalProgression.Progression.Unlocks;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Phase 9.1 — regression tests for <see cref="RimconemyUnlockExtension"/>.
    /// Validates the DefModExtension serialization shape (IsGateDefined and
    /// ResolveDomain against the seven domain strings plus an unknown).
    /// </summary>
    public static class RimconemyUnlockExtensionTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            // Empty extension: defaults to invalid gate
            var empty = new RimconemyUnlockExtension();
            AssertFalse(empty.IsGateDefined(), "default extension is not a valid gate (no domain)");
            AssertTrue(empty.ResolveDomain() == null,
                "default extension has no resolved domain (null)");

            // Each domain slot maps to the right enum
            T("Survival", ProgressionDomain.Survival);
            T("Salvage", ProgressionDomain.Salvage);
            T("Firecraft", ProgressionDomain.Firecraft);
            T("Building", ProgressionDomain.Building);
            T("Processing", ProgressionDomain.Processing);
            T("Machinery", ProgressionDomain.Machinery);
            T("Defense", ProgressionDomain.Defense);

            // Unknown string returns null, IsKnownDomainString false
            var unk = new RimconemyUnlockExtension { domain = "Heuristik" };
            AssertTrue(unk.ResolveDomain() == null,
                "unknown domain string resolves to null (not silently Survival)");
            AssertFalse(unk.IsKnownDomainString(),
                "unknown domain string is not 'known'");

            // Gate validity requires level >= 1 even with valid domain
            var lowLevel = new RimconemyUnlockExtension { domain = "Building", requiredLevel = 0 };
            AssertFalse(lowLevel.IsGateDefined(), "requiredLevel 0 rejects gate definition");
            var okLevel = new RimconemyUnlockExtension { domain = "Building", requiredLevel = 1 };
            AssertTrue(okLevel.IsGateDefined(), "requiredLevel >= 1 accepts gate definition");

            string summary = "[Rimconemy.SurvivalProgression] RimconemyUnlockExtension tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static void T(string domainString, ProgressionDomain expected)
        {
            var ext = new RimconemyUnlockExtension { domain = domainString };
            ProgressionDomain? actual = ext.ResolveDomain();
            AssertTrue(actual.HasValue && actual.Value == expected,
                "domain string '" + domainString + "' maps correctly");
            AssertTrue(ext.IsGateDefined(), "domain '" + domainString + "' has IsGateDefined=true");
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[Rimconemy.SurvivalProgression] " + label); }
        }
        private static void AssertFalse(bool condition, string label) { AssertTrue(!condition, label); }
        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[Rimconemy.SurvivalProgression] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
