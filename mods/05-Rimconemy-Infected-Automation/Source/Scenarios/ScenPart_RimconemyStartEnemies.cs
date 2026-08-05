using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Scenarios
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05).
    /// Phase 1.4+1.5 — ScenPart that spawns starter-infected on the
    /// freshly generated home map, scaled by difficulty and map size.
    ///
    /// Hook: <see cref="ScenPart.PostMapGenerate(Map)"/>
    ///
    /// Constraints:
    ///   - Spawn count scales with difficulty (Refuge < Survival < Collapse)
    ///   - Spawn count scales with map size (Small < Medium < Large)
    ///   - Drops are NOT guaranteed loot sources
    ///   - Idempotent across MapRemoved and save-load via
    ///     <see cref="RimconemyStartEnemiesLedger"/>.
    /// </summary>
    public class ScenPart_RimconemyStartEnemies : ScenPart
    {
        // Vanilla-defnames (stable across versions).
        public const string DefName_HiddenFaction  = "Rimconemy_HiddenInfectedFaction";
        public const string DefName_PawnKind       = "Rimconemy_InfectedRavager";

        // Phase-1.5: Difficulty multipliers keyed by DifficultyDef.defName.
        // Keys mirror StoryDirector.ResolveProfileFromDifficulty (the SSOT for
        // 1.6 difficulty defNames: Peaceful/Easy/Medium/Rough/Hard/Extreme);
        // unknown defNames fall back to 1.0.
        private static readonly Dictionary<string, float> DifficultyMultipliers = new Dictionary<string, float>
        {
            { "Peaceful", 0.5f },
            { "Easy", 0.5f },
            { "Medium", 1.0f },
            { "Rough", 1.5f },
            { "Hard", 2.0f },
            { "Extreme", 3.0f },
        };

        // Phase-1.5: Map size multipliers (based on map width)
        private static readonly Dictionary<int, float> MapSizeMultipliers = new Dictionary<int, float>
        {
            { 150, 0.7f },   // Small (~150x150)
            { 200, 1.0f },   // Medium (~200x200)
            { 250, 1.3f },   // Large (~250x250)
            { 300, 1.5f },   // Huge (~300x300)
            { 400, 2.0f },   // Massive
        };

        // Base starter count (Normal/Adventure Story difficulty, Medium map)
        private const int BaseStarterCount = 1;

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

                int count = CalculateStarterCount(map);
                int spawned = SpawnStarterInfected(map, count);
                if (spawned > 0)
                {
                    ledger.MarkSpawnCompleted(map);
                    Log.Message(
                        $"[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: spawned {spawned} starter infected on map={map.uniqueID} (difficulty={GetCurrentDifficultyDefName() ?? "unknown"}, mapSize={map.Size.x}x{map.Size.z}).");
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

        /// <summary>
        /// Calculates the number of starter infected based on:
        /// - Current difficulty setting
        /// - Map size
        /// </summary>
        private static int CalculateStarterCount(Map map)
        {
            float multiplier = 1.0f;

            // Apply difficulty multiplier
            string difficulty = GetCurrentDifficultyDefName();
            if (difficulty != null && DifficultyMultipliers.TryGetValue(difficulty, out float diffMult))
            {
                multiplier *= diffMult;
            }

            // Apply map size multiplier (based on map width)
            int mapWidth = map.Size.x;
            float sizeMult = 1.0f;
            foreach (var kvp in MapSizeMultipliers.OrderBy(k => k.Key))
            {
                if (mapWidth <= kvp.Key)
                {
                    sizeMult = kvp.Value;
                    break;
                }
            }
            // If larger than all defined sizes, use the largest multiplier
            if (mapWidth > MapSizeMultipliers.Keys.Max())
            {
                sizeMult = MapSizeMultipliers.Values.Max();
            }
            multiplier *= sizeMult;

            int count = (int)Math.Ceiling(BaseStarterCount * multiplier);
            
            // Clamp to reasonable bounds
            count = Math.Max(1, Math.Min(count, 8)); // At least 1, at most 8

            return count;
        }

        private static string GetCurrentDifficultyDefName()
        {
            return Find.Storyteller?.difficultyDef?.defName;
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
                    // Spread spawns around the map, not all at center
                    cell = CellFinder.RandomClosewalkCellNear(map.Center, map, 25 + i * 10);
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
        /// the Rimconemy Single Survivor scenario.
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
        /// Reflective marker check for <see cref="RimWorld.Scenario"/>.
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
