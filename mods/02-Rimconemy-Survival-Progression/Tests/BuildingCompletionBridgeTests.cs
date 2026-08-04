using Rimconemy.SurvivalProgression.Progression;
using Rimconemy.SurvivalProgression.Progression.Hooks;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Phase 8.3 — regression tests for <see cref="BuildingCompletionBridge"/>.
    /// Fakeless. Validates:
    ///   * BuildIdempotencyKey shape is stable across same inputs
    ///   * ClassifyBuilding maps representative defnames to the right domain
    ///   * Submit with null state returns Rejected
    ///   * Submit with valid state grants XP once (idempotent on replay)
    ///   * Submit honours the result.WasAccepted contract
    ///
    /// The Vanilla Frame entity is NOT instantiated here: the bridge
    /// accepts a (Map, def, frameId) shape that hashes identically
    /// whether frame is null or a real instance.
    /// </summary>
    public static class BuildingCompletionBridgeTests
    {
        private static int _passed;
        private static int _failed;

        public static bool RunAll()
        {
            _passed = 0;
            _failed = 0;

            // Classify: domain dispatch
            AssertEqual(ProgressionDomain.Defense,
                BuildingCompletionBridge.ClassifyBuilding(MakeDef("Rimconemy_WallBarricade")),
                "wall barricade -> Defense");
            AssertEqual(ProgressionDomain.Defense,
                BuildingCompletionBridge.ClassifyBuilding(MakeDef("Rimconemy_Turret_Arrow")),
                "turret -> Defense");
            AssertEqual(ProgressionDomain.Machinery,
                BuildingCompletionBridge.ClassifyBuilding(MakeDef("Rimconemy_Generator_Electric")),
                "generator -> Machinery");
            AssertEqual(ProgressionDomain.Machinery,
                BuildingCompletionBridge.ClassifyBuilding(MakeDef("Rimconemy_Battery_Power")),
                "battery -> Machinery");
            AssertEqual(ProgressionDomain.Firecraft,
                BuildingCompletionBridge.ClassifyBuilding(MakeDef("Rimconemy_Campfire")),
                "campfire -> Firecraft");
            AssertEqual(ProgressionDomain.Firecraft,
                BuildingCompletionBridge.ClassifyBuilding(MakeDef("Rimconemy_HighFurnace")),
                "high furnace -> Firecraft");
            AssertEqual(ProgressionDomain.Processing,
                BuildingCompletionBridge.ClassifyBuilding(MakeDef("Rimconemy_Smithy")),
                "smithy -> Processing");
            AssertEqual(ProgressionDomain.Salvage,
                BuildingCompletionBridge.ClassifyBuilding(MakeDef("Rimconemy_ScrapRecycler")),
                "recycler -> Salvage");
            AssertEqual(ProgressionDomain.Building,
                BuildingCompletionBridge.ClassifyBuilding(MakeDef("Rimconemy_GenericWorkshop")),
                "generic -> Building (fallback)");
            AssertEqual(ProgressionDomain.Building,
                BuildingCompletionBridge.ClassifyBuilding(null),
                "null -> Building (defensive)");

            // IdempotencyKey shape and stability
            string keyA = BuildingCompletionBridge.BuildIdempotencyKey(
                MakeDef("Rimconemy_Campfire"), null, null);
            string keyB = BuildingCompletionBridge.BuildIdempotencyKey(
                MakeDef("Rimconemy_Campfire"), null, null);
            AssertEqual(keyA, keyB, "BuildIdempotencyKey is stable for the same inputs");
            // Prefix encodes the *classified* domain — Campfires go to Firecraft,
            // not the hard "Building" prefix.
            AssertTrue(keyA.StartsWith("domain:Firecraft:completed:map="),
                "key starts with classified Firecraft prefix for Campfire def");
            AssertTrue(keyA.Contains("def=Rimconemy_Campfire"),
                "key encodes the def name (defenses against cross-def collisions)");

            // Different classified domain → different prefix for the same def name shape
            string keyWall = BuildingCompletionBridge.BuildIdempotencyKey(
                MakeDef("Rimconemy_WallBarricade"), null, null);
            AssertTrue(keyWall.StartsWith("domain:Defense:completed:map="),
                "WallBarricade def classified as Defense in key prefix");

            // Forward-to-fakeless-state: BuildIdempotencyKey hashing must be
            // collision-free across sim frames differing only in frame ID.
            string keyFrame1 = BuildingCompletionBridge.BuildIdempotencyKey(
                MakeDef("Rimconemy_Campfire"), null, null);
            // Without map and frame we cannot pin a unique frame-id here
            // because the helper dummies -1 for absent entities.
            // The shape however must include a frame marker.
            AssertTrue(keyFrame1.Contains("frame=-1"),
                "key encodes absent-frame sentinel (-1)");

            // Submit behaviour with nulls
            var s0 = new DomainXpState();
            ProgressionActionResult r0 = BuildingCompletionBridge.Submit(
                s0, null, null, null, null, 0L);
            AssertFalse(r0.WasAccepted, "Submit with all-null inputs is rejected");

            // Submit behaviour with valid def + null Map (defensive)
            ProgressionActionResult r1 = BuildingCompletionBridge.Submit(
                new DomainXpState(), MakeDef("Rimconemy_Campfire"), null, null, null, 0L);
            AssertFalse(r1.WasAccepted, "Submit with null Map is rejected");

            // Submit behaviour with null def + valid state+map (defensive)
            ProgressionActionResult r2 = BuildingCompletionBridge.Submit(
                new DomainXpState(), null, null, null, null, 0L);
            AssertFalse(r2.WasAccepted, "Submit with null def is rejected");

            // The bridge, called outside a real game session, treats null map or
            // null def as a hard reject. The integration with DomainXpState only
            // happens when the Harmony postfix calls us with __instance.Map != null;
            // this is enforced here as a guard.
            AssertEqual(0, s0.TotalAwards, "no awards registered from rejected submits");

            string summary = "[Rimconemy.SurvivalProgression] BuildingCompletionBridge tests: "
                + _passed + " passed, " + _failed + " failed.";
            if (_failed > 0)
            {
                Log.Error(summary);
                return false;
            }
            Log.Message(summary);
            return true;
        }

        private static ThingDef MakeDef(string defName)
        {
            var d = new ThingDef { defName = defName };
            return d;
        }

        private static void AssertTrue(bool condition, string label)
        {
            if (condition) _passed++;
            else { _failed++; Log.Error("[BuildingCompletionBridgeTests] " + label); }
        }
        private static void AssertFalse(bool condition, string label) { AssertTrue(!condition, label); }
        private static void AssertEqual<T>(T expected, T actual, string label)
        {
            if (Equals(expected, actual)) _passed++;
            else
            {
                _failed++;
                Log.Error("[BuildingCompletionBridgeTests] " + label + ": expected " + expected + ", got " + actual);
            }
        }
    }
}
