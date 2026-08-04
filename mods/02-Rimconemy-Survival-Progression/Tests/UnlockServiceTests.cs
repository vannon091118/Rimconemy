using Rimconemy.SurvivalProgression.Progression;
using Rimconemy.SurvivalProgression.Progression.Unlocks;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Phase 9.2 — Regression tests for <see cref="UnlockService.IsUnlocked"/>.
    /// Fakeless. Validates the gate contract: no extension = always unlocked,
    /// extension present with satisfied level+actions = unlocked,
    /// extension present with missing level or actions = locked, invalid
    /// domains = locked, defensive nulls = unlocked.
    /// </summary>
    public static class UnlockServiceTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            // Defensive defaults
            AssertTrue(UnlockService.IsUnlocked(null, null), "null def + null state -> true");
            AssertTrue(UnlockService.IsUnlocked(new FakeDef(), null), "real def + null state -> true");
            AssertTrue(UnlockService.IsUnlocked(null, new DomainXpState()), "null def + state -> true");

            var defNoGate = new FakeDef();
            AssertTrue(UnlockService.IsUnlocked(defNoGate, new DomainXpState()),
                "def without RimconemyUnlockExtension -> always unlocked");

            var state = new DomainXpState();
            // Stage level 2 in Building
            state.TryAward(ProgressionDomain.Building, 200f, "warm", "", 0, 0L, out _);

            // All conditions met
            var defOk = new FakeDef("Rimconemy_Tier2Door");
            defOk.modExtensions = new System.Collections.Generic.List<DefModExtension>
            {
                new RimconemyUnlockExtension
                {
                    domain = "Building",
                    requiredLevel = 2,
                    requiredActions = new System.Collections.Generic.List<string>
                    {
                        "domain:Building:completed:map=1:def=Warm:frame=1:tick=2",
                        "domain:Building:completed:map=2:def=Warm:frame=4:tick=5",
                    },
                },
            };
            AssertFalse(UnlockService.IsUnlocked(defOk, state),
                "extension with held action set returns false (action not recorded)");

            state.TryAward(
                ProgressionDomain.Building,
                1f,
                "domain:Building:completed:map=1:def=Warm:frame=1:tick=2",
                "", 0, 0L, out _);
            state.TryAward(
                ProgressionDomain.Building,
                1f,
                "domain:Building:completed:map=2:def=Warm:frame=4:tick=5",
                "", 0, 0L, out _);
            AssertTrue(UnlockService.IsUnlocked(defOk, state),
                "after required actions are recorded the gate opens");

            // Level-not-met
            var defLevel = new FakeDef();
            defLevel.modExtensions = new System.Collections.Generic.List<DefModExtension>
            {
                new RimconemyUnlockExtension
                {
                    domain = "Machinery",
                    requiredLevel = 99,
                },
            };
            AssertFalse(UnlockService.IsUnlocked(defLevel, state),
                "Machinery Level 99 extension against Level-1 state is closed");

            // Missing domain string => IsGateDefined()==false => closed
            var defMalformed = new FakeDef();
            defMalformed.modExtensions = new System.Collections.Generic.List<DefModExtension>
            {
                new RimconemyUnlockExtension
                {
                    domain = "",       // empty => malformed gate
                    requiredLevel = 1,
                },
            };
            AssertFalse(UnlockService.IsUnlocked(defMalformed, state),
                "extension with empty domain string is malformed and closed");

            // domain string pointing to a non-existent slot resolves to null
            // via ResolveDomain(); the gate stays closed.
            var defUnknown = new FakeDef();
            defUnknown.modExtensions = new System.Collections.Generic.List<DefModExtension>
            {
                new RimconemyUnlockExtension
                {
                    domain = "Wizardry",  // unknown -> null | not Survival
                    requiredLevel = 1,
                },
            };
            AssertFalse(UnlockService.IsUnlocked(defUnknown, state),
                "unknown-domain extension does NOT silently map to Survival; gate stays closed");

            // RequiredActions list with empty entries must be ignored
            var defEmptyAction = new FakeDef();
            defEmptyAction.modExtensions = new System.Collections.Generic.List<DefModExtension>
            {
                new RimconemyUnlockExtension
                {
                    domain = "Building",
                    requiredLevel = 1,
                    requiredActions = new System.Collections.Generic.List<string> { "" },
                },
            };
            // state already has Building>=Level2 -> true; empty action ignored
            AssertTrue(UnlockService.IsUnlocked(defEmptyAction, state),
                "empty-string requiredAction entries are skipped");

            string summary = "[Rimconemy.SurvivalProgression] UnlockService tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        // --- Test stand-in: a Def that lets us attach a DefModExtension without
        //     depending on full RimWorld mod loader infra. RimWorld 1.6's
        //     DefModExtension lookup goes through Def.modExtensions.
        private sealed class FakeDef : Def
        {
            public FakeDef() : base()
            {
                // Verse.Def allows null-safety; we treat this as a stand-in.
            }
            public FakeDef(string defName) : this() { this.defName = defName; }
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[UnlockServiceTests] " + label); }
        }

        private static void AssertFalse(bool condition, string label) { AssertTrue(!condition, label); }
    }
}
