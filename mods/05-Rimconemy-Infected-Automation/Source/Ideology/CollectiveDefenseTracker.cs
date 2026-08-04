using System.Collections.Generic;
using Rimconemy.Foundation.Colonials;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    /// Setting Rule: CollectiveDefense (H3 §2).
    ///
    /// Tracks per-pawn combat participation so the post-combat
    /// CollectiveDefensePostCombatPatch can compute:
    ///   - shirkers (able colonists that did not actively defend)
    ///   - valiant defenders (colonists that participated)
    ///   - aggregate combat counter for the StoryDirector / UI
    ///
    /// Persisted as <see cref="GameComponent"/> state via Scribe so the
    /// tracker survives Save/Load and the per-pawn counters do not drift.
    /// Aggregation runs every 600 ticks from GameComponentTick override
    /// (no Harmony patch). See H3 §2 + §8.3.
    /// </summary>
    public class CollectiveDefenseTracker : GameComponent
    {
        // Last combat tick (Game.TicksGame) used to scope aggregation.
        private long lastCombatTick = -1L;

        // Last tick when aggregation ran (persisted via Scribe).
        // Using TicksGame delta instead of static counter so Save/Load
        /// preserves cadence without resetting.
        private long lastAggregateTick = 0L;

        // Participating pawn ids (thingIDNumber) per combat round.
        // Reset by ApplyPostCombatEffects() after the thought pass.
        private HashSet<int> _lastParticipants = new HashSet<int>();
        private HashSet<int> _lastShirkers = new HashSet<int>();

        // Aggregate combat counters, persistent across the lifetime of the save.
        private int totalDefenders = 0;
        private int totalShirkers = 0;
        private int totalRounds = 0;

        // Visible to UI/test code (read-only copy after each Apply pass).
        public IReadOnlyCollection<int> LastParticipants => _lastParticipants;
        public IReadOnlyCollection<int> LastShirkers => _lastShirkers;
        public int TotalDefenders => totalDefenders;
        public int TotalShirkers => totalShirkers;
        public int TotalRounds => totalRounds;

        // Used by the post-combat patch and Harmony hooks to register
        // which pawns actively shot, meleed or drafted in this round.
        private readonly Dictionary<int, bool> _currentRoundParticipation = new Dictionary<int, bool>();

        public CollectiveDefenseTracker(Game game) { }

        /// <summary>
        /// Records participation state for a single pawn for the next combat
        /// round. Toggle from the post-combat patch when a colonist dealt
        /// damage, melee hit, or drafted in defense.
        /// </summary>
        public void RecordParticipation(int pawnId)
        {
            _currentRoundParticipation[pawnId] = true;
        }

        /// <summary>
        /// Computes the shirker/defender partition for the current round.
        /// Returns participants and shirkers separately so the post-combat
        /// patch can apply the thoughts in a single pass.
        /// </summary>
        public void ComputeAndApply(HashSet<int> outParticipants, HashSet<int> outShirkers)
        {
            outParticipants.Clear();
            outShirkers.Clear();

            // Snapshot the current round participation into the local sets.
            foreach (var kv in _currentRoundParticipation)
            {
                if (kv.Value)
                    outParticipants.Add(kv.Key);
            }

            var colonists = ColonialReader.GetActiveColonists();
            foreach (var pawn in colonists)
            {
                if (pawn == null || pawn.Dead) continue;
                if (!IsAbleToDefend(pawn)) continue;
                if (outParticipants.Contains(pawn.thingIDNumber))
                {
                    ApplyThoughtIfPossible(pawn, ThoughtDefs_CollectiveDefense.ValiantDefense);
                    totalDefenders++;
                }
                else
                {
                    ApplyThoughtIfPossible(pawn, ThoughtDefs_CollectiveDefense.DefenseShirking);
                    outShirkers.Add(pawn.thingIDNumber);
                    totalShirkers++;
                }
            }

            totalRounds++;
            _lastParticipants.Clear();
            foreach (var id in outParticipants) _lastParticipants.Add(id);
            _lastShirkers.Clear();
            foreach (var id in outShirkers) _lastShirkers.Add(id);
            _currentRoundParticipation.Clear();
            lastCombatTick = Current.Game != null ? Find.TickManager.TicksGame : 0L;

            if (outShirkers.Count == 0 && outParticipants.Count > 0)
            {
                // Group-unity thought when everyone who could fight did fight.
                foreach (var pawn in colonists)
                {
                    if (pawn == null || pawn.Dead) continue;
                    ApplyThoughtIfPossible(pawn, ThoughtDefs_CollectiveDefense.UnitedAfterDefense);
                }
            }
        }

        private static bool IsAbleToDefend(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.Dead || pawn.Downed) return false;
            if (!pawn.IsColonist) return false;
            if (!pawn.IsFreeNonSlaveColonist) return false;
            if (!pawn.ageTracker.Adult) return false;
            if (pawn.health == null) return false;
            // 1.6 path: a colonist is "able to defend" when their
            // health-tracker reports no critical system damage and the
            // pawn is not in shock. We avoid InFatalCondition (was renamed
            // in 1.6) and rely on the simpler state check instead.
            if (pawn.health.hediffSet?.GetFirstHediffOfDef(HediffDefOf.Anesthetic) != null) return false;
            return true;
        }

        private static void ApplyThoughtIfPossible(Pawn pawn, ThoughtDef def)
        {
            if (pawn == null || pawn.needs?.mood?.thoughts?.memories == null || def == null) return;
            // The MemoryThoughtHandler in 1.6 exposes TryGainMemory(ThoughtDef)
            // and TryAddMemory(ThoughtDef) variants; TryAddMemory is the
            // safest because it returns a boolean (no exception on duplicates).
            try
            {
                // 1.6 MemoryThoughtHandler.TryGainMemory(ThoughtDef) returns
                // bool (true = gained, false = already present / capped) so
                // duplicate Combat-Aggregate passes stay idempotent.
                pawn.needs.mood.thoughts.memories.TryGainMemory(def, pawn);
            }
            catch (System.Exception)
            {
                // Defensive: if signature changes we silently no-op rather
                // than break the post-combat pass.
            }
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager == null) return;
            long currentTick = Find.TickManager.TicksGame;
            const long AggregateIntervalTicks = 600L;

            if (currentTick < lastAggregateTick + AggregateIntervalTicks)
                return;

            lastAggregateTick = currentTick;

            if (Current.Game == null) return;

            var participants = new HashSet<int>();
            var shirkers = new HashSet<int>();
            ComputeAndApply(participants, shirkers);
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastCombatTick, "lastCombatTick", -1L);
            Scribe_Values.Look(ref lastAggregateTick, "lastAggregateTick", 0L);
            Scribe_Values.Look(ref totalDefenders, "totalDefenders", 0);
            Scribe_Values.Look(ref totalShirkers, "totalShirkers", 0);
            Scribe_Values.Look(ref totalRounds, "totalRounds", 0);
        }

        /// <summary>
        /// Returns the singleton tracker instance so external callers do not
        /// need to dig through Current.Game.components. The instance is created
        /// by the GameComponent registration in <see cref="Bootstrap"/>.
        /// </summary>
        public static CollectiveDefenseTracker Get()
        {
            if (Current.Game == null) return null;
            return Current.Game.GetComponent<CollectiveDefenseTracker>();
        }
    }
}
