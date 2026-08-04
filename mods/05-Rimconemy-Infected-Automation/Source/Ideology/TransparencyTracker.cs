using System.Collections.Generic;
using Rimconemy.Foundation.Colonials;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    /// Setting Rule: Transparency (H3 §3).
    ///
    /// Tracker for:
    ///   - Total decisions made by StoryDirector
    ///   - "Explained" decisions (Decision has UIExplanation or visible reason)
    ///   - Last decision tick
    ///   - Consecutive-unexplained per pawn
    ///
    /// Aggregate() is invoked by the ThoughtWorker each time it runs,
    /// so the pawn-side counter is whatever was last recorded.
    ///
    /// Public API:
    ///   RecordDecision(bool explained, string reason) - called from
    ///     StoryDirector when an event is fired; pushes a "snapshot"
    ///     to all colonists.
    ///   TrustLevel (read-only) - 0..1 trust meter for UI.
    ///
    /// Scribed for Save/Load stability.
    /// </summary>
    public class TransparencyTracker : GameComponent
    {
        // Counters
        private int totalDecisions;
        private int explainedDecisions;
        private long lastDecisionTick = -1L;

        // Per-pawn consecutive unexplained counter
        private readonly Dictionary<int, int> consecutiveUnexplained = new Dictionary<int, int>();

        public int TotalDecisions => totalDecisions;
        public int ExplainedDecisions => explainedDecisions;
        public long LastDecisionTick => lastDecisionTick;

        /// <summary>
        /// 0..1 trust level. 1.0 = every decision explained; 0.0 = every
        /// decision unexplained. Used by UI and the ThoughtWorker stage
        /// selection. Falls back to 0.5 when no decisions have been made.
        /// </summary>
        public float TrustLevel
        {
            get
            {
                if (totalDecisions == 0) return 0.5f;
                return (float)explainedDecisions / totalDecisions;
            }
        }

        public TransparencyTracker(Game game) { }

        public static TransparencyTracker Get()
        {
            if (Current.Game == null) return null;
            return Current.Game.GetComponent<TransparencyTracker>();
        }

        public void RecordDecision(bool explained, string reason)
        {
            totalDecisions++;
            if (explained) explainedDecisions++;
            lastDecisionTick = Current.Game != null ? Find.TickManager.TicksGame : 0L;

            // Update per-pawn consecutiveUnexplained counter.
            // Each unexplained decision nudges every colonist's counter up
            // (capped). Explained decisions reset counters.
            var colonists = ColonialReader.GetActiveColonists();
            if (explained)
            {
                foreach (var pawn in colonists)
                {
                    if (pawn == null) continue;
                    consecutiveUnexplained[pawn.thingIDNumber] = 0;
                }
            }
            else
            {
                foreach (var pawn in colonists)
                {
                    if (pawn == null) continue;
                    consecutiveUnexplained.TryGetValue(pawn.thingIDNumber, out int cur);
                    consecutiveUnexplained[pawn.thingIDNumber] = System.Math.Min(cur + 1, 4);
                }
            }
            // Logging: keep one line of diagnostics so the operator can
            // verify the rule is being fed.
            Log.Message(
                "[Rimconemy.InfectedAutomation] TransparencyTracker: decision="
                + (explained ? "EXPLAINED" : "UNEXPLAINED")
                + ", total=" + totalDecisions
                + ", explained=" + explainedDecisions
                + ", reason='" + (reason ?? "<none>") + "'");
        }

        /// <summary>Aggregate read-only view used by the ThoughtWorker.</summary>
        public void Aggregate(out int total, out int explained, out float trust)
        {
            total = totalDecisions;
            explained = explainedDecisions;
            trust = TrustLevel;
        }

        /// <summary>Returns the consecutive-unexplained count for a pawn (0..4).</summary>
        public int GetConsecutiveUnexplained(Pawn pawn)
        {
            if (pawn == null) return 0;
            return consecutiveUnexplained.TryGetValue(pawn.thingIDNumber, out int v) ? v : 0;
        }

        public override void GameComponentTick() { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref totalDecisions, "ttTotal", 0);
            Scribe_Values.Look(ref explainedDecisions, "ttExplained", 0);
            Scribe_Values.Look(ref lastDecisionTick, "ttLastTick", -1L);
            // Per-pawn dict is rebased from scratch each game, so we keep it
            // in memory only - no Scribe needed (deterministic on rebuild).
        }
    }
}
