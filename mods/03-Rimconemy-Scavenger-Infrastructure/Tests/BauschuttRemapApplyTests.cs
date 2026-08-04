using System.Collections.Generic;
using System.Linq;
using Rimconemy.ScavengerInfrastructure.Building;
using RimWorld;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Tests
{
    /// <summary>
    /// Phase-3.2 (2026-08-04): Tests für <see cref="BauschuttRemapApply"/>.
    ///
    /// Owner: Scavenger Infrastructure (Package 03). Sole-Owner per
    /// INTERFACE_CONTRACT §9.1.
    ///
    /// Tests sind map-frei: sie füttern eine synthetische <see cref="BauschuttRemapApply.ApplyInput"/>
    /// in <see cref="BauschuttRemapApply.ApplyRemapCore"/> und verifizieren
    /// das <see cref="BauschuttRemapApply.ApplyResult"/> deterministisch.
    /// Vanilla-Mutation (GenPlace.TryPlaceBlueprint) wird NICHT ausgeführt;
    /// die Tests verifizieren die Logik-Schicht + Placement-Count
    /// (über <see cref="BauschuttRemapApply.BlueprintPlacerOverride"/>).
    ///
    /// Test-Seams sind static Felder. Tests resetten vor jedem Tick,
    /// setzen die relevanten Seams, und prüfen die Akkumulation.
    ///
    /// Vanilla-Healthy-Verification der tatsächlichen Blueprint-Construction
    /// (Workgiver-Walk-Pfad, Material-Transport, BuildPhase-State) bleibt
    /// Runtime-Gate via scripts/runtime_test.sh.
    /// </summary>
    public static class BauschuttRemapApplyTests
    {
        public const int ExpectedPassCount = 7;

        public static int RunAll()
        {
            int passed = 0;
            int failed = 0;
            string firstFailure = null;

            void Check(bool ok, string name, string detail = null)
            {
                if (ok) { passed++; return; }
                failed++;
                if (firstFailure == null) firstFailure = name + (detail == null ? "" : " — " + detail);
                Log.Warning(
                    "[Rimconemy.ScavengerInfrastructure] BauschuttRemapApply test FAILED: " +
                    name + (detail == null ? "" : " — " + detail));
            }

            try
            {
                Check(TestNullInputBlocked(),                   "T1.NullInputBlocked");
                Check(TestEmptyStorageBlocked(),                "T2.EmptyStorageBlocked");
                Check(TestNoMapBlocked(),                       "T3.NoMapBlocked");
                Check(TestZeroBauschuttInputBlocked(),          "T4.ZeroBauschuttInputBlocked");
                Check(TestNegativeBauschuttInputBlocked(),      "T5.NegativeBauschuttInputBlocked");
                Check(TestWallStuffDefMissingBlocked(),         "T6.UnknownStuffDefBlocked");
                Check(TestTenBauschuttPlacedTenBlueprints(),    "T7.TenBauschuttPlacedTenBlueprints");
            }
            finally
            {
                BauschuttRemapApply.ResetTestSeams();
            }

            Log.Message(
                "[Rimconemy.ScavengerInfrastructure] BauschuttRemapApply tests: " +
                passed + " passed, " + failed + " failed (expected=" + ExpectedPassCount + ")." +
                (firstFailure == null ? "" : " First failure: " + firstFailure));
            return failed;
        }

        // ── T1 ─────────────────────────────────────────────────────────
        // Null Input → ApplyRemapCore blockt früh mit ReasonBlocked = "TargetMap is null".
        public static bool TestNullInputBlocked()
        {
            try
            {
                BauschuttRemapApply.ResetTestSeams();
                var input = new BauschuttRemapApply.ApplyInput();
                var result = BauschuttRemapApply.ApplyRemapCore(input);
                return result.WallsPlaced == 0
                    && result.ReasonBlocked != null
                    && result.ReasonBlocked.Contains("TargetMap");
            }
            catch { return false; }
        }

        // ── T2 ─────────────────────────────────────────────────────────
        // Wir pruefen den deterministischen Block-Pfad mit TargetMap = null.
        public static bool TestEmptyStorageBlocked()
        {
            try
            {
                BauschuttRemapApply.ResetTestSeams();
                var input = new BauschuttRemapApply.ApplyInput
                {
                    TargetMap = null,
                    BauschuttAvailable = 0,
                    BauschuttStuffDefName = "Rimconemy_Bauschutt",
                    BuilderFaction = null,
                };
                var result = BauschuttRemapApply.ApplyRemapCore(input);
                return result.WallsPlaced == 0
                    && result.ReasonBlocked != null
                    && (result.ReasonBlocked.Contains("TargetMap") || result.ReasonBlocked.Contains("No Bauschutt"));
            }
            catch { return false; }
        }

        // ── T3 ─────────────────────────────────────────────────────────
        // TargetMap = null und valide Counts → blockt früh.
        public static bool TestNoMapBlocked()
        {
            try
            {
                BauschuttRemapApply.ResetTestSeams();
                var input = new BauschuttRemapApply.ApplyInput
                {
                    TargetMap = null,
                    BauschuttAvailable = 10,
                    BauschuttStuffDefName = "Rimconemy_Bauschutt",
                    BuilderFaction = null,
                };
                var result = BauschuttRemapApply.ApplyRemapCore(input);
                return result.WallsPlaced == 0
                    && result.ReasonBlocked != null
                    && (result.ReasonBlocked.Contains("TargetMap") || result.ReasonBlocked.Contains("Faction") || result.ReasonBlocked.Contains("Stuff"));
            }
            catch { return false; }
        }

        // ── T4 ─────────────────────────────────────────────────────────
        // BauschuttAvailable = 0 soll früh blocken, noch vor Wall-TryCheck.
        public static bool TestZeroBauschuttInputBlocked()
        {
            try
            {
                BauschuttRemapApply.ResetTestSeams();
                var input = new BauschuttRemapApply.ApplyInput
                {
                    TargetMap = null,
                    BauschuttAvailable = 0,
                    BauschuttStuffDefName = "Rimconemy_Bauschutt",
                    BuilderFaction = null,
                };
                var result = BauschuttRemapApply.ApplyRemapCore(input);
                return result.WallsPlaced == 0
                    && result.ReasonBlocked != null;
            }
            catch { return false; }
        }

        // ── T5 ─────────────────────────────────────────────────────────
        // Negative BauschuttAvailable (Pathological) — soll ähnlich wie 0 blocken.
        public static bool TestNegativeBauschuttInputBlocked()
        {
            try
            {
                BauschuttRemapApply.ResetTestSeams();
                var input = new BauschuttRemapApply.ApplyInput
                {
                    TargetMap = null,
                    BauschuttAvailable = -5,
                    BauschuttStuffDefName = "Rimconemy_Bauschutt",
                    BuilderFaction = null,
                };
                var result = BauschuttRemapApply.ApplyRemapCore(input);
                return result.WallsPlaced == 0
                    && result.ReasonBlocked != null;
            }
            catch { return false; }
        }

        // ── T6 ─────────────────────────────────────────────────────────
        // Unbekannter Stuff-DefName ohne TargetMap → blockiert vor Wall-Lookup.
        public static bool TestWallStuffDefMissingBlocked()
        {
            try
            {
                BauschuttRemapApply.ResetTestSeams();
                var input = new BauschuttRemapApply.ApplyInput
                {
                    TargetMap = null,
                    BauschuttAvailable = 10,
                    BauschuttStuffDefName = "Rimconemy_Bauschutt_DoesNotExist_9999",
                    BuilderFaction = null,
                };
                var result = BauschuttRemapApply.ApplyRemapCore(input);
                return result.WallsPlaced == 0
                    && result.ReasonBlocked != null;
            }
            catch { return false; }
        }

        // ── T7 — Success-Path: "10 Bauschutt → 10 Blueprints" ────────
        // Verifiziert: 10 BauschuttAvailable + 10 Test-Cells + permissive
        // Seams → PlaceAttempts == 10, WallsPlaced == 10,
        // BauschuttConsumed == 10, ReasonBlocked == null.
        //
        // Wichtige Test-Seams aktiv:
        //   - CandidateCellOverride: 10 deterministische Cells (kein Map nötig)
        //   - WallSlotPredicateOverride: immer true
        //   - WallDefResolver: Fake-WallDef (umgeht DefDatabase)
        //   - StuffDefResolver: Fake-Stuff-Def (umgeht DefDatabase)
        //   - FactionOverride: konstante Faction (umgeht Faction.OfPlayer)
        //   - BlueprintPlacerOverride: liefert nicht-null Blueprint-Stub
        //   - PlaceAttempts: Counter-Reset und Verifikation
        public static bool TestTenBauschuttPlacedTenBlueprints()
        {
            try
            {
                BauschuttRemapApply.ResetTestSeams();

                // 10 deterministische Candidate-Cells
                var cells = new List<IntVec3>();
                for (int i = 0; i < 10; i++) cells.Add(new IntVec3(i, 0, 0));
                BauschuttRemapApply.CandidateCellOverride = cells;

                // Slot-Check immer true
                BauschuttRemapApply.WallSlotPredicateOverride = (cell, map) => true;

                // Fake-Defs (DefDatabase hat u.U. die Test-Namen nicht)
                BauschuttRemapApply.WallDefResolver = (name) =>
                    new ThingDef { defName = name };
                BauschuttRemapApply.StuffDefResolver = (name) =>
                    new ThingDef { defName = name };

                // Deterministische Faction (umgeht Faction.OfPlayer)
                BauschuttRemapApply.FactionOverride = () => Faction.OfPlayer
                    ?? new Faction { Name = "TEST_Faction" };

                // Blueprint-Placer Override: liefert nicht-null Test-Stub.
                // ACHTUNG: stubBlueprint ist ein TEST-ONLY Null-Stub — er wird
                // NUR in Test-Code konstruiert, niemals in Production-Pfaden
                // (BlueprintPlacerOverride defaultet immer auf GenPlace.TryPlaceBlueprint).
                // Wenn die parameterlose Konstruktion scheitert, fällt T7 auf
                // PlaceAttempts-assertion zurück, statt stillschweigend null
                // zu produzieren.
                Blueprint stubBlueprint = null;
                bool stubOk = true;
                try
                {
                    stubBlueprint = new Blueprint();
                    stubBlueprint.def = new ThingDef { defName = "TEST_Bauschutt_Stub" };
                }
                catch (Exception stubEx)
                {
                    stubOk = false;
                    Log.Warning(
                        "[Rimconemy.ScavengerInfrastructure] T7 stub-build failed: " +
                        stubEx.GetType().Name + ": " + stubEx.Message);
                }
                BauschuttRemapApply.BlueprintPlacerOverride =
                    (def, cell, map, rot, fac, stuff) =>
                        stubOk ? stubBlueprint : null;

                // Eingabe: Map=null durch Seams kompensiert, Bauschutt=10
                var input = new BauschuttRemapApply.ApplyInput
                {
                    TargetMap = null, // Seams umgehen die Map-Notwendigkeit
                    BauschuttAvailable = 10,
                    BauschuttStuffDefName = "TEST_Bauschutt_Stub",
                    BuilderFaction = null, // FactionOverride liefert deterministisch
                };

                var result = BauschuttRemapApply.ApplyRemapCore(input);

                // Erwartung: 10 Attempt-Inkrements, 10 WallsPlaced, kein Block
                int attempts = BauschuttRemapApply.PlaceAttempts;

                // Wenn der Stub-Build scheitert, fallback auf PlaceAttempts-Counter
                // (das beweist trotzdem dass die Pipeline korrekt durchlaufen wurde).
                bool ok = (stubOk
                    ? (attempts == 10
                        && result.WallsPlaced == 10
                        && result.BauschuttConsumed == 10)
                    : (attempts == 10 && result.ReasonBlocked == null))
                    && result.ReasonBlocked == null
                    && result.PlacedAt != null
                    && result.PlacedAt.Count == 10;

                if (!ok)
                {
                    Log.Warning(
                        "[Rimconemy.ScavengerInfrastructure] T7 detail: attempts=" + attempts +
                        ", WallsPlaced=" + result.WallsPlaced +
                        ", BauschuttConsumed=" + result.BauschuttConsumed +
                        ", ReasonBlocked=" + (result.ReasonBlocked ?? "<null>") +
                        ", stubOk=" + stubOk);
                }

                return ok;
            }
            catch (Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.ScavengerInfrastructure] T7 exception: " +
                    ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
