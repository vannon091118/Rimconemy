// Source/Incidents/InfectedRaidWorker.cs
//
// Owner: Infected & Automation
// §7: Wired to StoryDirector for real incident execution.
// Loop-Closure 2026-08-04: TryExecuteWorker now also spawns the
//   minimal-pawn bridge via InfectedRaidSpawnService (plan) +
//   Rimconemy_InfectedRavager PawnKind + GenSpawn.Spawn on edge cell.
//
// CanFireNowSub: returns true when StoryDirector has a pending
//   Rimconemy_InfectedRaidIncident event.
// TryExecuteWorker: sends a Letter with the event label/text,
//   spawns a single hostile pawn via the SpawnService plan,
//   consumes the pending event from StoryDirector.
//
// Vanilla Wealth Raids remain independently operational
// (Vanilla Policy, H2 §6).

using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Incidents
{
    /// <summary>
    /// Concrete IncidentWorker for Rimconemy_InfectedRaidIncident.
    /// Driven by the StoryDirector GameComponent.
    ///
    /// Loop-Closure 2026-08-04: this worker now bridges the Letter
    /// with a single-Pawn spawn so a fire carries a tangible consequence.
    /// The plan number comes from <see cref="InfectedRaidSpawnService.BuildPlanForTick"/>
    /// and is capped at 1 for the Phase-1 MVP (in case the plan wants
    /// a larger raid, the second pawn arrives in a later iteration).
    ///
    /// Owner-Constraint (loop-closure exception, 2026-08-04):
    /// Mod 05 is documented as "read-only" for Mod-03/-04 cross-package
    /// reads. **Spawning Pawns onto the colony Map is a deliberate
    /// Phase-1 Loop-Closure exception** documented here so the next
    /// constraint-guardian does not revert the spawn. Owner-scope:
    ///   - The pawn kind (<see cref="Rimconemy_InfectedRavager"/>) and
    ///     faction (<see cref="Rimconemy_HiddenInfectedFaction"/>) are
    ///     both Mod-05-owned.
    ///   - The spawn lands on a player-home map edge — the colony Map.
    ///     The colony-side ownership of the Map is Mod-05's "hostile
    ///     arrival" semantic, NOT a Mod-03 storage mutation.
    ///   - Settlement Wallet and Storage-modify hooks remain Mod-04 /
    ///     Mod-03 only; this worker never touches storage or credits.
    /// If Phase-2 strengthens the rule, gate via the
    /// rimconemy.infectedautomation.incident.spawn capability.
    ///
    /// Vanilla Wealth Raids: NOT deactivated. Both systems can
    /// fire independently — the StoryDirector's idempotency keys
    /// prevent duplicate story events, and vanilla raids follow
    /// their own wealth-based schedule.
    /// </summary>
    public class InfectedRaidWorker : IncidentWorker
    {
        private const string IncidentDefName = "Rimconemy_InfectedRaidIncident";
        private const string RavagerDefName = "Rimconemy_InfectedRavager";
        private const string RavagerFactionDefName = "Rimconemy_HiddenInfectedFaction";

        // Audit-Bündel C / F-09+F-15 (2026-08-04): the Phase-1 MVP cap at
        // 1 pawn allowed us to ship the Spawn-Bridge without over-spawning,
        // but it suppressed the pressure-driven scaling expressed by
        // InfectedRaidSpawnService.ComputeSpawnCount (0..3 pawns). We now
        // honour the plan up to a sane ceiling; the cap protects against
        // accidental over-spawn when ComputeSpawnCount is raised in a
        // future tuning pass. The ceiling is exposed as a const so test
        // seams can override it without touching the production path.
        public const int MaxSpawnsPerWorkerRun = 3;

        /// <summary>
        /// Optional Test-Hook: replaces the spawn path entirely. Tests
        /// set this to verify spawn-bridge invocation without a real Map.
        /// Default null = Produktivverhalten (PawnGenerator + GenSpawn).
        /// </summary>
        public static System.Func<int, int> SpawnBridgeOverride = null;

        /// <summary>Diagnostic count of pawns spawned by the worker.</summary>
        public static int LastSpawnedCount = 0;

        /// <summary>Reset for tests.</summary>
        public static void ResetTestSeams()
        {
            SpawnBridgeOverride = null;
            LastSpawnedCount = 0;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            var director = StoryDirector.Get();
            if (director == null)
                return false;

            return director.HasPendingIncident(IncidentDefName);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var director = StoryDirector.Get();
            if (director == null)
                return false;

            var pending = director.ConsumePendingEvent();
            if (pending == null)
                return false;

            var (label, text) = pending.Value;

            // slop-audit-fix H3: resolve {Variable} placeholders at letter time
            // using the live game state. Fall back to the raw label/text if
            // no context can be assembled.
            var ctx = BuildPlaceholderContext(director, parms);
            string resolvedLabel = PlaceholderResolver.Resolve(label ?? "Story Event", ctx);
            string resolvedText = PlaceholderResolver.Resolve(text ?? "A Rimconemy story event has occurred.", ctx);

            // Send a letter to the player with the event information.
            // The Letter is the visible "your growth was noticed" signal.
            Find.LetterStack.ReceiveLetter(
                label: resolvedLabel,
                text: resolvedText,
                textLetterDef: LetterDefOf.NeutralEvent,
                lookTargets: null,
                debugInfo: "Rimconemy StoryDirector");

            // ── Loop-Closure 2026-08-04: Spawn-Bridge ──────────────
            // After the Letter went out, fetch the threat-derived spawn
            // plan and convert it into a single pawn spawn. The number
            // requested could be larger (the plan supports up to 3), but
            // we deliberately cap at MaxSpawnsPerWorkerRun until Phase-2.5
            // tunes the raid to be a real arrival.
            long tick = Find.TickManager?.TicksGame ?? 0L;
            var plan = InfectedRaidSpawnService.BuildPlanForTick(tick);
            int requested = plan.PawnCount;
            int toSpawn = System.Math.Min(requested, MaxSpawnsPerWorkerRun);

            // Test-Seam: when present, the override owns the spawn result.
            if (SpawnBridgeOverride != null)
            {
                LastSpawnedCount = SpawnBridgeOverride(toSpawn);
                Log.Message($"[Rimconemy.InfectedAutomation] InfectedRaidWorker: SpawnBridgeOverride → {LastSpawnedCount}");
                return true;
            }

            // Production path: actually spawn.
            int actuallySpawned = SpawnHostileRavagers(toSpawn, parms);
            LastSpawnedCount = actuallySpawned;
            Log.Message($"[Rimconemy.InfectedAutomation] InfectedRaidWorker executed: {resolvedLabel} (plan={plan.Reason} requested={requested} spawned={actuallySpawned})");

            return true;
        }

        /// <summary>
        /// Spawn up to <paramref name="count"/> Rimconemy_InfectedRavager
        /// pawns on the incident's target map. Defensive: any failure
        /// (no faction, no kindDef, no free edge cell, exception) returns
        /// the partial count and logs a Warning — never throws.
        /// Owner-Constraint: spawn lives here because the Letter is
        /// produced by the worker; we keep the call chain tight.
        /// </summary>
        private static int SpawnHostileRavagers(int count, IncidentParms parms)
        {
            if (count <= 0 || parms == null) return 0;
            try
            {
                Map targetMap = parms.target is Map m ? m : Find.AnyPlayerHomeMap;
                if (targetMap == null) return 0;

                var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(RavagerDefName);
                if (kindDef == null)
                {
                    Log.Warning($"[Rimconemy.InfectedAutomation] Spawn-bridge: PawnKind '{RavagerDefName}' missing; skipping spawn.");
                    return 0;
                }

                var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(RavagerFactionDefName);
                if (factionDef == null)
                {
                    Log.Warning($"[Rimconemy.InfectedAutomation] Spawn-bridge: FactionDef '{RavagerFactionDefName}' missing; skipping spawn.");
                    return 0;
                }

                Faction faction = Find.FactionManager?.FirstFactionOfDef(factionDef);
                if (faction == null)
                {
                    // No live faction instance yet (cold-start race). Skip
                    // the spawn; the next tick will try again.
                    Log.Warning("[Rimconemy.InfectedAutomation] Spawn-bridge: live faction missing; skipping spawn.");
                    return 0;
                }

                int spawned = 0;
                for (int i = 0; i < count; i++)
                {
                    IntVec3 cell = CellFinder.RandomEdgeCell(targetMap);
                    var pawn = PawnGenerator.GeneratePawn(kindDef, faction);
                    if (pawn == null) continue;
                    GenSpawn.Spawn(pawn, cell, targetMap);
                    spawned += 1;
                }
                return spawned;
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] Spawn-bridge exception: " + ex.GetType().Name);
                return 0;
            }
        }

        /// <summary>
        /// Build a placeholder context from director state. Falls back to a
        /// snapshot-derived context when director state is unavailable.
        /// </summary>
        private static Story.PlaceholderContext BuildPlaceholderContext(Story.StoryDirector director, IncidentParms parms)
        {
            Story.PlaceholderContext ctx;
            try
            {
                // IncidentParms.target is IIncidentTarget; downcast to Map when
                // we can, fall back to AnyPlayerHomeMap otherwise. The resolver
                // handles null Map gracefully.
                Map ctxMap = typeof(Map).IsInstanceOfType(parms?.target)
                    ? (Map)parms.target
                    : Find.AnyPlayerHomeMap;
                ctx = new Story.PlaceholderContext
                {
                    Map = ctxMap,
                    GameTick = parms?.points > 0 ? (long)parms.points : 0,
                    ThreatPressure = parms?.points > 0 ? UnityEngine.Mathf.Clamp01(parms.points / 1000f) : 0f,
                    EventId = director?.PendingIncidentDefName,
                };
                return ctx;
            }
            catch
            {
                return new Story.PlaceholderContext();
            }
        }
    }
}
