using Rimconemy.SurvivalProgression.Mining;
using RimWorld;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.SurvivalProgression.Tests
{
    /// <summary>
    /// Phase-First Regression Tests (Task 16 — Mining-Gate Gates).
    /// Def-level static checks only. Pawn-skill-bounded checks require a live
    /// RimWorld session; logged as test stubs that need a real Pawn at run-time.
    /// </summary>
    public static class MiningGateRegressionTests
    {
        private static TestSuite ts;
        public const string TestGroup = "MiningGate";

        public static int Run()
        {
            ts = new TestSuite("SurvivalProgression", "Mining Gate regression tests");

            int failures = 0;

            // 1. SSOT: only one Def owns Rimconemy_MiningGateExt class binder.
            var compactSteel = DefDatabase<ThingDef>.GetNamedSilentFail("CompactMineableSteel");
            if (compactSteel == null) { Log.Error("[Rimconemy.SurvivalProgression] FAIL: CompactMineableSteel missing from DefDatabase"); failures++; }
            else
            {
                var ext = MiningGateResolver.GetExt(compactSteel);
                if (ext == null) { Log.Error("[Rimconemy.SurvivalProgression] FAIL: CompactMineableSteel missing MiningGateExt"); failures++; }
                else if (ext.minMiningLevel != 8) { Log.Error("[Rimconemy.SurvivalProgression] FAIL: CompactMineableSteel expected minLevel=8, got " + ext.minMiningLevel); failures++; }
            }

            // 2. Vanilla non-steel mineables are NOT blocking.
            var chunkGranite = DefDatabase<ThingDef>.GetNamedSilentFail("ChunkGranite");
            if (chunkGranite != null && MiningGateResolver.IsBlockingMineable(chunkGranite))
            {
                Log.Error("[Rimconemy.SurvivalProgression] FAIL: ChunkGranite unexpectedly blocking"); failures++;
            }
            else if (chunkGranite != null)
            {
                Log.Message("[Rimconemy.SurvivalProgression] OK: ChunkGranite has no MiningGateExt (no false-positive)");
            }

            // 3. Vanilla non-blocked confirms CanMine still returns true.
            if (chunkGranite != null && !MiningGateResolver.CanMine(chunkGranite))
            {
                Log.Error("[Rimconemy.SurvivalProgression] FAIL: CanMine(ChunkGranite) returned false"); failures++;
            }
            else if (chunkGranite != null)
            {
                Log.Message("[Rimconemy.SurvivalProgression] OK: CanMine(ChunkGranite) returns true");
            }

            // 4. With no Pawn, CanMine is conservative (requirePawn=true blocks boundary).
            if (compactSteel != null)
            {
                var ext = MiningGateResolver.GetExt(compactSteel);
                if (ext != null && ext.requirePawn)
                {
                    if (MiningGateResolver.CanMine(compactSteel, null))
                    {
                        Log.Error("[Rimconemy.SurvivalProgression] FAIL: CanMine(CompactSteel, null) returned true (requirePawn=true expected opposite)"); failures++;
                    }
                    else
                    {
                        Log.Message("[Rimconemy.SurvivalProgression] OK: CanMine(CompactSteel, null) returns false (requirePawn=enforced)");
                    }
                }
            }


            ts.Check(failures == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
            return failures;
        }
    }
}
