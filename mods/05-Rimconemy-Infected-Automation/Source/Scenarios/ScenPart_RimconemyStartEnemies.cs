using System;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Phase 1.4 — ScenPart that spawns exactly one weak starter-infected on the
    /// freshly generated home map. Hook: <see cref="ScenPart.PostMapGenerate(Map)"/>
    /// (vanilla-api-matrix §3.1 confirmed: RimWorld.ScenPart exposes this method).
    ///
    /// Constraints (DECISIONS §24):
    ///   - Exactly one pawn on Normal profile; max two on Hard profile.
    ///   - Drops are NOT guaranteed loot sources — the survivor may or may not
    ///     see scrap/munition drop from this pawn.
    ///   - Idempotent across MapRemoved and save-load via
    ///     <see cref="RimconemyStartEnemiesLedger"/>.
    ///
    /// Cross-package safety: this ScenPart only ever reads its own ledger. It is
    /// registered in mods/02/Defs/Scenarios/SingleSurvivor.xml with a class-attribute
    /// reference. Compilation requires Package-05 to expose the namespace; Package-02
    /// needs the class to be present at runtime via About.xml load-order.
    /// </summary>
    public class ScenPart_RimconemyStartEnemies : ScenPart
    {
        // Vanilla-defnames (stable across versions).
        public const string DefName_HiddenFaction  = "Rimconemy_HiddenInfectedFaction";
        public const string DefName_PawnKind       = "Rimconemy_InfectedRavager";

        // Phase-1.4 fixed count: Normal=1, Hard=2. Anti-Softlock: Hard is optional
        // and only if the difficulty flag is honoured — out of scope for this
        // MVP; we always commit Normal here.
        private const int NormalProfile_StarterCount = 1;

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            try
            {
                // Scenario diagnostic: warn if running in wrong scenario
                LogScenarioMismatchIfNeeded();

                if (map == null) return;
                var ledger = Current.Game?.GetComponent<RimconemyStartEnemiesLedger>();
                if (ledger == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies.PostMapGenerate: ledger missing; skipping spawn.");
                    return;
                }

                if (ledger.IsSpawnCompletedFor(map))
                {
                    Log.Message(
                        $"[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: starter already committed for map={map.uniqueID}; idempotent skip.");
                    return;
                }

                int spawned = SpawnStarterInfected(map, NormalProfile_StarterCount);
                if (spawned > 0)
                {
                    ledger.MarkSpawnCompleted(map);
                    Log.Message(
                        $"[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: spawned {spawned} starter infected on map={map.uniqueID}.");
                }
                else
                {
                    Log.Warning(
                        $"[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: starter-spawn returned 0 on map={map.uniqueID}; the survivor is alone.");
                }
            }
            catch (Exception ex)
            {
                // ScenPart errors must not crash Scribe.
                Log.Warning(
                    $"[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies.PostMapGenerate caught: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static int SpawnStarterInfected(Map map, int count)
        {
            // Use shared utility to ensure hidden faction exists (materializes on demand)
            var faction = InfectedFactionUtility.EnsureHiddenInfectedFaction();
            if (faction == null)
            {
                Log.Warning(
                    $"[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: faction '{InfectedFactionUtility.HiddenFactionDefName}' not available.");
                return 0;
            }

            var pawnKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(DefName_PawnKind);
            if (pawnKind == null)
            {
                Log.Warning(
                    $"[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: pawnKind '{DefName_PawnKind}' not registered.");
                return 0;
            }

            int placed = 0;
            for (int i = 0; i < count; i++)
            {
                Pawn pawn;
                try
                {
                    pawn = PawnGenerator.GeneratePawn(pawnKind, faction, null);
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        $"[Rimconemy.InfectedAutomation] PawnGenerator.GeneratePawn failed: {ex.GetType().Name}: {ex.Message}");
                    continue;
                }
                if (pawn == null) continue;

                IntVec3 cell;
                try
                {
                    cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 20);
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        $"[Rimconemy.InfectedAutomation] CellFinder.RandomClosewalkCellNear failed: {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                try
                {
                    GenSpawn.Spawn(pawn, cell, map);
                    placed++;
                }
                catch (Exception ex)
                {
                    Log.Warning(
                        $"[Rimconemy.InfectedAutomation] GenSpawn.Spawn failed: {ex.GetType().Name}: {ex.Message}");
                }
            }
            return placed;
        }

        /// <summary>
        /// Scenario diagnostic: logs a warning if this ScenPart runs outside
        /// the Rimconemy Single Survivor scenario. Helps players understand
        /// why no starter infected appears when using a vanilla scenario.
        ///
        /// RimWorld 1.6.4566 rev579 Scenario accessor drift: the public
        /// field name has moved between <c>parts</c> (1.5) and the public
        /// <c>ScenParts</c> property (1.6). Rather than pick a winner and
        /// break on the next point-release, we reflectively probe both
        /// candidates in priority order. If neither is found, we stay
        /// quiet rather than spam the log on an unrecognised API.
        /// </summary>
        private static void LogScenarioMismatchIfNeeded()
        {
            if (IsRimconemySingleSurvivorScenario(Find.Scenario)) return;

            var scenario = Find.Scenario;
            Log.Warning("[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies running outside Rimconemy Single Survivor scenario. " +
                "Starter infected requires 'Rimconemy Single Survivor' scenario. " +
                "Current scenario: " + (scenario?.name ?? "unknown"));
        }

        /// <summary>
        /// Reflective marker check for <see cref="RimWorld.Scenario"/>:
        /// walks the scenario's <c>parts</c> (1.5) or <c>ScenParts</c> (1.6)
        /// collection and looks for any <see cref="ScenPart"/> whose runtime
        /// type name matches the Rimconemy single-survivor marker. Defensive
        /// against both null scenario and reflection failures (returns false
        /// in either case; the caller logs the warning, never throws).
        /// </summary>
        private static bool IsRimconemySingleSurvivorScenario(RimWorld.Scenario scenario)
        {
            if (scenario == null) return false;
            try
            {
                System.Collections.IEnumerable partsEnum = null;
                var t = scenario.GetType();
                var prop = t.GetProperty("ScenParts",
                    System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic
                    | System.Reflection.BindingFlags.Instance);
                if (prop != null && prop.CanRead)
                {
                    partsEnum = prop.GetValue(scenario) as System.Collections.IEnumerable;
                }
                if (partsEnum == null)
                {
                    var field = t.GetField("parts",
                        System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        partsEnum = field.GetValue(scenario) as System.Collections.IEnumerable;
                    }
                }
                if (partsEnum == null) return false;
                foreach (var p in partsEnum)
                {
                    if (p != null && p.GetType().Name == "ScenPart_RimconemyStartEnemies")
                        return true;
                }
            }
            catch
            {
                // Defensive: diagnostic stays a no-op on reflection failure.
            }
            return false;
        }
    }
}
