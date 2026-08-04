using Rimconemy.SurvivalProgression.Progression;
using Verse;

namespace Rimconemy.SurvivalProgression.Progression.Unlocks
{
    /// <summary>
    /// Phase 9.2 — central unlock gate. One service for every consumer:
    /// Architect, RecipeWorker / Bill, WorkGiver, Repair, Rebuild, DLC-Adapter.
    ///
    /// Vertical-Slice-Plan §Phase 9.2: same <c>IsUnlocked(Def, DomainXpState)</c>
    /// call site is shared. Vanilla-ResearchProjectDef.IsFinished stays as the
    /// legacy read-model and is no longer consulted as primary path.
    ///
    /// Defensive defaults:
    ///   - def == null              -> true (no def, no gate)
    ///   - state == null            -> true (treat as fresh, no gate)
    ///   - no extension             -> true (vanilla-content, always visible)
    ///   - invalid domain           -> false (suspicious extension; closed)
    /// </summary>
    public static class UnlockService
    {
        /// <summary>
        /// Returns true if the given def has no gate, OR if the gate's
        /// requirements are satisfied against <paramref name="state"/>.
        /// </summary>
        public static bool IsUnlocked(Def def, DomainXpState state)
        {
            if (def == null) return true;
            if (state == null) return true;

            var ext = def.GetModExtension<RimconemyUnlockExtension>();
            if (ext == null) return true;
            if (!ext.IsGateDefined()) return false; // malformed gate -> closed

            if (!ext.IsKnownDomainString()) return false; // misspelled domain -> closed
            ProgressionDomain? domainOrNull = ext.ResolveDomain();
            if (domainOrNull == null) return false;
            ProgressionDomain domain = domainOrNull.Value;
            if (!ProgressionDomainUtility.IsValid(domain)) return false;

            if (state.GetLevel(domain) < ext.requiredLevel)
                return false;

            if (ext.requiredActions != null)
            {
                for (int i = 0; i < ext.requiredActions.Count; i++)
                {
                    string action = ext.requiredActions[i];
                    if (string.IsNullOrEmpty(action)) continue;
                    if (!state.HasCompletedAction(action)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Records an action completion for the unlock cross-readers. The
        /// call only fires after a successful accept (WasAccepted == true);
        /// the Bridge guards each call so we never trigger a spurious
        /// "def just unlocked" event off a duplicate-replay rejection.
        /// Today: a single Log.Message line so Phase 9.3 visibility hooks
        /// can be wired through the same call site later without changing
        /// any consumer.
        /// </summary>
        public static void NotifyActionCompleted(string actionKey, ProgressionDomain domain)
        {
            if (string.IsNullOrEmpty(actionKey)) return;
            Log.Message(
                "[Rimconemy.SurvivalProgression] UnlockService: action accepted "
                + "(domain=" + domain + ", key=" + actionKey + ").");
        }
    }
}
