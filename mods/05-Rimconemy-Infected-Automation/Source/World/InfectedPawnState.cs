using Rimconemy.Foundation.Save;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 2 — Per-pawn infected behavior state.
    ///
    /// Each spawned infected pawn has one of these, persisted via
    /// <see cref="ChunkController"/> (the owning MapComponent).
    /// Implements <see cref="IExposable"/> for save/load roundtrip
    /// and <see cref="ISchemaMigratable"/> per save contract.
    /// </summary>
    public sealed class InfectedPawnState : IExposable, ISchemaMigratable
    {
        /// <summary>thingIDNumber of the infected pawn.</summary>
        public int PawnThingId;

        /// <summary>Current behavior state.</summary>
        public InfectedBehaviorState CurrentBehavior;

        /// <summary>Game tick when the current behavior started.</summary>
        public long BehaviorStartTick;

        /// <summary>Target cell the pawn is moving toward.</summary>
        public IntVec3 TargetCell;

        /// <summary>thingIDNumber of the currently targeted colonist
        /// (only valid in Assault state). -1 if none.</summary>
        public int TargetColonistId = -1;

        /// <summary>Cell the infected was spawned at. Used as
        /// home/roaming anchor.</summary>
        public IntVec3 SpawnCell;

        /// <summary>Game tick of last state evaluation.</summary>
        public long LastEvaluateTick;

        /// <summary>Effective sight radius at last evaluation.</summary>
        public float LastSightRadius;

        /// <summary>Is this pawn currently dormant and should skip
        /// expensive evaluation? Optimization flag.</summary>
        public bool IsInactive;

        // ── ISchemaMigratable ────────────────────────────────

        public int SchemaVersion = 1;
        int ISchemaMigratable.CurrentSchemaVersion => 1;
        int ISchemaMigratable.SchemaVersion { get => SchemaVersion; set => SchemaVersion = value; }
        public string ClassId => "rimconemy.infectedautomation.infectedPawnState";
        public System.Collections.Generic.IList<SchemaStep> Steps => System.Array.Empty<SchemaStep>();
        public void MigrateIfNeeded() { this.RunMigration(); }

        // ── constructors ─────────────────────────────────────

        public InfectedPawnState() { }

        public InfectedPawnState(int pawnThingId, IntVec3 spawnCell, long currentTick)
        {
            PawnThingId = pawnThingId;
            SpawnCell = spawnCell;
            TargetCell = spawnCell;
            CurrentBehavior = InfectedBehaviorState.Dormant;
            BehaviorStartTick = currentTick;
            LastEvaluateTick = currentTick;
            TargetColonistId = -1;
            IsInactive = false;
        }

        /// <summary>How long the pawn has been in the current state.</summary>
        public long TicksInState(long currentTick)
        {
            return currentTick - BehaviorStartTick;
        }

        /// <summary>Transition to a new behavior state.</summary>
        public void TransitionTo(InfectedBehaviorState newState, long currentTick)
        {
            if (CurrentBehavior == newState) return;
            CurrentBehavior = newState;
            BehaviorStartTick = currentTick;
            TargetColonistId = -1;
        }

        public override string ToString()
        {
            return $"InfectedPawn(thingId={PawnThingId} state={CurrentBehavior} target={TargetCell} colonistId={TargetColonistId})";
        }

        // ── Scribe ──────────────────────────────────────────

        public void ExposeData()
        {
            Scribe_Values.Look(ref PawnThingId, "pawnThingId", 0);
            Scribe_Values.Look(ref CurrentBehavior, "currentBehavior", InfectedBehaviorState.Dormant);
            Scribe_Values.Look(ref BehaviorStartTick, "behaviorStartTick", 0L);
            Scribe_Values.Look(ref TargetCell, "targetCell");
            Scribe_Values.Look(ref TargetColonistId, "targetColonistId", -1);
            Scribe_Values.Look(ref SpawnCell, "spawnCell");
            Scribe_Values.Look(ref LastEvaluateTick, "lastEvaluateTick", 0L);
            Scribe_Values.Look(ref SchemaVersion, "schemaVersion", 1);

            // IsInactive is a runtime optimization flag — NOT persisted.
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                IsInactive = false;
                LastSightRadius = 0f;
            }
        }
    }
}
