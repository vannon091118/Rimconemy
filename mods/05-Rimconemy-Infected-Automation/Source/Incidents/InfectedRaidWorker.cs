// Source/Incidents/InfectedRaidWorker.cs
//
// Owner: Infected & Automation
// §7: Wired to StoryDirector for real incident execution.
//
// CanFireNowSub: returns true when StoryDirector has a pending
//   Rimconemy_InfectedRaidIncident event.
// TryExecuteWorker: sends a Letter with the event label/text,
//   consumes the pending event from StoryDirector.
//
// Vanilla Wealth Raids remain independently operational
// (Vanilla Policy, H2 §6).

using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Incidents
{
    /// <summary>
    /// Concrete IncidentWorker for Rimconemy_InfectedRaidIncident.
    /// Driven by the StoryDirector GameComponent. Each selected
    /// story event produces a Letter with the event's label and
    /// descriptive text.
    ///
    /// Vanilla Wealth Raids: NOT deactivated. Both systems can
    /// fire independently — the StoryDirector's idempotency keys
    /// prevent duplicate story events, and vanilla raids follow
    /// their own wealth-based schedule.
    /// </summary>
    public class InfectedRaidWorker : IncidentWorker
    {
        private const string IncidentDefName = "Rimconemy_InfectedRaidIncident";

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            var director = Story.StoryDirector.Get();
            if (director == null)
                return false;

            return director.HasPendingIncident(IncidentDefName);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var director = Story.StoryDirector.Get();
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
            string resolvedLabel = Story.PlaceholderResolver.Resolve(label ?? "Story Event", ctx);
            string resolvedText = Story.PlaceholderResolver.Resolve(text ?? "A Rimconemy story event has occurred.", ctx);

            // Send a letter to the player with the event information.
            // No actual raid spawning yet (Phase 5+); this is the
            // notification layer for the Phase 1 MVP.
            Find.LetterStack.ReceiveLetter(
                label: resolvedLabel,
                text: resolvedText,
                textLetterDef: LetterDefOf.NeutralEvent,
                lookTargets: null,
                debugInfo: "Rimconemy StoryDirector");

            Log.Message($"[Rimconemy.InfectedAutomation] InfectedRaidWorker executed: {resolvedLabel}");

            return true;
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
