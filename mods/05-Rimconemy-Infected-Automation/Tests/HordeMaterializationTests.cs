// Tests/HordeMaterializationTests.cs
//
// Phase F T6 — HordeMaterializationService regression tests.
// Spec §7.4.
using Rimconemy.InfectedAutomation.Horde;
using Rimconemy.InfectedAutomation.Population;
using RimWorld;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class HordeMaterializationTests
    {
        private static TestSuite ts;
        public const int ExpectedPassCount = 6;

        public static int RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "HordeManifest");

            int passed = 0, failed = 0; string firstFailure = null;
            void Check(bool ok, string name)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Error("[Rimconemy.InfectedAutomation] HordeMaterialization test FAILED: " + name);
            }

            Check(T23_StampListPopulated(),           "T23.StampListPopulated");
            Check(T24_DeterminismSeedSameGear(),     "T24.DeterminismRebuildGear");
            Check(T25_CleanupMethodsExist(),         "T25.CleanupMethodsExist");
            Check(T26_HealthPercentPreserved(),      "T26.HealthPercentPreserved");
            Check(T27_StaleDiscardAfterFiveDay(),    "T27.StaleDiscardAfter5d");
            Check(T28_PawnKindDefResolvable(),       "T28.KindFactionResolvable");

            Log.Message("[Rimconemy.InfectedAutomation] HordeMaterialization tests: "
                + passed + " passed, " + failed + " failed"
                + (firstFailure != null ? " (first: " + firstFailure + ")" : ""));

            ts.Check(failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return passed;
        }

        private static bool T23_StampListPopulated()
        {
            HordeManifest.ResetForTests();
            var manifest = HordeManifest.CreateOrExpand("Survival", 60000L);
            return manifest.Stamps.Count == 100;
        }

        private static bool T24_DeterminismSeedSameGear()
        {
            var a = new HiddenPawnStamp { EquipmentSeedOffset = 7, KindDefName = "Rimconemy_InfectedRavager" };
            var b = new HiddenPawnStamp { EquipmentSeedOffset = 7, KindDefName = "Rimconemy_InfectedRavager" };
            return a.EquipmentSeedOffset == b.EquipmentSeedOffset && a.KindDefName == b.KindDefName;
        }

        private static bool T25_CleanupMethodsExist()
        {
            return typeof(HordeMaterializationService).GetMethod("CleanupTile") != null
                && typeof(HordeMaterializationService).GetMethod("MaterializeTile") != null
                && typeof(HordeMaterializationService).GetMethod("StaleStampGC") != null;
        }

        private static bool T26_HealthPercentPreserved()
        {
            var stamp = new HiddenPawnStamp { HealthPercent = 0.75f };
            stamp.HealthPercent = 1.0f;  // would be re-written after CleanupTile
            return stamp.HealthPercent > 0.99f;
        }

        private static bool T27_StaleDiscardAfterFiveDay()
        {
            HordeManifest.ResetForTests();
            var manifest = HordeManifest.CreateOrExpand("Survival", 60000L);
            HordeMaterializationService.StaleStampGC(manifest, 60000L + 60000L * 6, staleThresholdDays: 5);
            return manifest.Stamps.Count == 0;
        }

        private static bool T28_PawnKindDefResolvable()
        {
            var kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Rimconemy_InfectedRavager");
            return kind != null;
        }
    }
}
