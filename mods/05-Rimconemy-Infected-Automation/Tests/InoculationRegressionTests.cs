// Tests/InoculationRegressionTests.cs
//
// Owner: Infected & Automation (Package 05).
// Phase C — P6-PROGRESS §12 Tier-Inokulation.
//
// Pattern: static RunAll() mit Inline-Assertions, log+counter statt
// throw. Tests I1-I5 (Selector) — höhere Tests I6+.x kommen mit
// InoculationConverter (Task 3) und RandomInoculationService (Task 4).

using System.Collections.Generic;
using Rimconemy.InfectedAutomation.Inoculation;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using Verse;
using Rimconemy.Foundation.Tests;

namespace Rimconemy.InfectedAutomation.Tests
{
    public static class InoculationRegressionTests
    {
        private static TestSuite ts;
        private static int _passed;
        private static int _failed;

        public static void RunAll()
        {
            ts = new TestSuite("InfectedAutomation", "Inoculation test");

            _passed = 0;
            _failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name)
            {
                if (ok) { _passed++; return; }
                _failed++;
                if (firstFailure == null) firstFailure = name;
                Log.Error("[Rimconemy.InfectedAutomation] Inoculation test FAILED: " + name);
            }

            // I1-I5: Selector pure-logic.
            Check(TestSelectorDeterministicSameSeedSameCandidate(), "I1.SelectorDeterministicSameSeedSameCandidate");
            Check(TestSelectorEmptyReturnsNull(),                     "I2.SelectorEmptyReturnsNull");
            Check(TestSelectorExcludesDeadAndHumanlike(),             "I3.SelectorExcludesDeadAndHumanlike");
            Check(TestSelectorExcludesAlreadyInfected(),              "I4.SelectorExcludesAlreadyInfected");
            Check(TestSelectorRankingStableForSameInput(),            "I5.SelectorRankingStableForSameInput");

            // I6-I9: Converter pure-logic.
            Check(TestConverterMapsWolfToBrandedKind(),               "I6.ConverterMapsWolfToBrandedKind");
            Check(TestConverterFactionSwitch(),                       "I7.ConverterFactionSwitch");
            Check(TestConverterNoMappingFallback(),                   "I8.ConverterNoMappingFallback");
            Check(TestConverterHalfCapDelta(),                        "I9.ConverterHalfCapDelta");

            // I10: GetTotalCapBudget AnimalHalfCap.
            Check(TestGetTotalCapBudgetTwoTierHalved(),              "I10.GetTotalCapBudgetTwoTierHalved");

            Log.Message(
                "[Rimconemy.InfectedAutomation] Inoculation regression tests (Phase C subset): "
                + _passed + " passed, " + _failed + " failed."
                + (firstFailure != null ? " First failure: " + firstFailure : ""));

            ts.Check(_failed == 0, "legacy assertion aggregate");
            ts.RunSummary(1);
        }

        // ── I1: Same Seed → Same Candidate ────────────────
        private static bool TestSelectorDeterministicSameSeedSameCandidate()
        {
            var cands = BuildCandidateList(8);
            var r1 = InoculationSelectorLogic.SelectCandidate(cands, 42, 0L);
            var r2 = InoculationSelectorLogic.SelectCandidate(cands, 42, 0L);
            return r1.HasValue && r2.HasValue && r1.Value.ThingId == r2.Value.ThingId;
        }

        // ── I2: Empty List → Null ──────────────────────────
        private static bool TestSelectorEmptyReturnsNull()
        {
            var empty = new List<InoculationCandidate>();
            return !InoculationSelectorLogic.SelectCandidate(empty, 1, 0L).HasValue;
        }

        // ── I3: Excludes Dead + Humanlike Animals ────────
        private static bool TestSelectorExcludesDeadAndHumanlike()
        {
            var cands = new List<InoculationCandidate>
            {
                Cand("a", alive: true, animal: true, humanlike: false, faction: "WildFaction"),
                Cand("b", alive: false, animal: true, humanlike: false, faction: "WildFaction"),
                Cand("c", alive: true, animal: false, humanlike: false, faction: "PlayerColony"),
                Cand("d", alive: true, animal: true, humanlike: false, faction: "WildFaction"),
            };
            InoculationSelectorLogic.FilterCandidates(cands, out var filtered);
            return filtered.Count == 2
                && filtered[0].ThingId == "a"
                && filtered[1].ThingId == "d";
        }

        // ── I4: Excludes Already-Infected ──────────────────
        private static bool TestSelectorExcludesAlreadyInfected()
        {
            var cands = new List<InoculationCandidate>
            {
                Cand("a", alive: true, animal: true, humanlike: false, faction: "WildFaction"),
                Cand("b", alive: true, animal: true, humanlike: false, faction: "Rimconemy_HiddenInfectedFaction"),
                Cand("c", alive: true, animal: true, humanlike: false, faction: "WildFaction"),
            };
            InoculationSelectorLogic.FilterCandidates(cands, out var filtered);
            return filtered.Count == 2
                && System.Linq.Enumerable.All(filtered, c =>
                    c.OriginalFactionDef != "Rimconemy_HiddenInfectedFaction");
        }

        // ── I5: Stable Ranking ─────────────────────────────
        private static bool TestSelectorRankingStableForSameInput()
        {
            var a = BuildCandidateList(6);
            // Same physical list, but fed in a different order to verify
            // the Sort-by-ThingId guarantee.
            var b = new List<InoculationCandidate>(a);
            b.Reverse();

            var rA = InoculationSelectorLogic.SelectCandidate(a, 100, 0L);
            var rB = InoculationSelectorLogic.SelectCandidate(b, 100, 0L);
            return rA.HasValue && rB.HasValue && rA.Value.ThingId == rB.Value.ThingId;
        }

        // ── helpers ────────────────────────────────────────
        private static List<InoculationCandidate> BuildCandidateList(int n)
        {
            var list = new List<InoculationCandidate>(n);
            for (int i = 0; i < n; i++)
            {
                list.Add(Cand(
                    thingId: "wild-" + i.ToString("D2"),
                    alive: true, animal: true, humanlike: false,
                    faction: "WildFaction"));
            }
            return list;
        }

        private static InoculationCandidate Cand(
            string thingId, bool alive, bool animal, bool humanlike, string faction)
        {
            return new InoculationCandidate
            {
                ThingId = thingId,
                KindDefName = "Wolf",
                RaceDefName = "Wolf",
                OriginalFactionDef = faction,
                IsAnimal = animal,
                IsHumanlike = humanlike,
                IsDead = !alive,
                MapCell = new IntVec3(10, 0, 10),
            };
        }

        // ── I6: Converter maps Wolf → Branded kind ────────────────────
        private static bool TestConverterMapsWolfToBrandedKind()
        {
            var cand = Cand("wild-01", true, true, false, "WildFaction");
            var outcome = InoculationConverter.Convert(cand, kindMappingTableHit: true, "selected");
            return outcome.ConvertedKindDefName == "Rimconemy_InfectedWildlife";
        }

        // ── I7: Faction switches to Hidden-Infected ────────────────────
        private static bool TestConverterFactionSwitch()
        {
            var cand = Cand("wild-02", true, true, false, "WildFaction");
            var outcome = InoculationConverter.Convert(cand, kindMappingTableHit: true, "selected");
            return outcome.ConvertedFactionDef == "Rimconemy_HiddenInfectedFaction";
        }

        // ── I8: No-Mapping Fallback keeps original Kind ────────────────
        private static bool TestConverterNoMappingFallback()
        {
            var cand = Cand("wild-03", true, true, false, "WildFaction");
            var outcome = InoculationConverter.Convert(cand, kindMappingTableHit: false, "selected");
            // Faction switches; Kind falls back to "Wolf" (original).
            return outcome.ConvertedFactionDef == "Rimconemy_HiddenInfectedFaction"
                && outcome.ConvertedKindDefName == "Wolf";
        }

        // ── I9: Converter Half-Cap Delta ────────────────────────────────
        private static bool TestConverterHalfCapDelta()
        {
            int deltaUp = InoculationConverter.ComputeAnimalHalfCapDelta(0, 1);
            int deltaEqual = InoculationConverter.ComputeAnimalHalfCapDelta(3, 3);
            int deltaDown = InoculationConverter.ComputeAnimalHalfCapDelta(5, 4);
            return deltaUp == 1 && deltaEqual == 0 && deltaDown == 0;
        }

        // ── I10: GetTotalCapBudget AnimalHalfCap rule ────────────────
        private static bool TestGetTotalCapBudgetTwoTierHalved()
        {
            var ledger = new Population.PopulationLedger
            {
                Cap = 10,
                HumanoidLiveCount = 4,
                AnimalLiveCount = 4,
            };
            // floor(4/2) = 2; consumed = 4 + 2 = 6; budget = 10 - 6 = 4.
            return ledger.GetTotalCapBudget() == 4;
        }
    }
}
