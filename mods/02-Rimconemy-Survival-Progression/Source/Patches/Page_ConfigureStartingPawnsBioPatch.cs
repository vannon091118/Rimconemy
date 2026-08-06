using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Rimconemy.SurvivalProgression.Character;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Patches
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02)
    /// Hook reason: Phase 5 Bio-Remap audit-round-5 (2026-08-04).
    ///
    /// The new-game customisation screen (Page_ConfigureStartingPawns)
    /// renders the starting-pawn tile list BEFORE Verse.Game.InitNewGame.
    /// By the time ProgressionGameComponent.FinalizeInit fires (after
    /// the player clicks Start and the game is initialised), the player has
    /// already seen the vanilla backstory ages on the screen and the visual
    /// feedback is gone. The earlier BioRemap fix on FinalizeInit fixed the
    /// in-game save state — but the player still sees, e.g., a 63-year-old
    /// Shepherd on the customisation page.
    ///
    /// This patch hooks the page's PreOpen method so we apply FixAge AND
    /// DistributeSkillBudget to each starting pawn in
    /// Verse.GameInitData.startingAndOptionalPawns BEFORE the page first
    /// renders. After this Postfix runs:
    ///   - every starting colonist shows age 18 (no more 63-year-old Shepherd)
    ///   - every starting colonist shows a 30-point-budgeted skill pool
    ///     (no more 54 cumulative backstory levels on an 18-year-old body)
    ///
    /// We patch PreOpen (not DoWindowContents) so the work runs once on
    /// page-open instead of every render frame. PreOpen fires on the
    /// new-game path; it does NOT fire on save-load, so it does not
    /// interact with the Scribe persistence of `bioRemapApplied`.
    ///
    /// DistributeSkillBudget does NOT write to StoredBudgetAllocations, so
    /// the SkillBudgetWindow still opens after Start and the player keeps
    /// the canonical place to re-tune distribution. Pre-distribution is a
    /// visual fix only; downstream SKBW interaction is the canonical input
    /// channel.
    ///
    /// Skill allocation policy (audit-round-5 review-bug 1):
    /// Without skill pre-distribution the customize screen would show a
    /// 18-year-old Shepherd with 54 cumulative skill levels (17 shooting, 9
    /// crafting...) which is visible sign of broken consistency. Pre-distributing
    /// at PreOpen makes the screen uniform while preserving player agency for
    /// the SKBW post-Start window.
    /// </summary>
    // Patch target rationale (DECISIONS §24, 2026-08-05):
    // RimWorld 1.6.4566 rev579 renamed `PreOpen` -> `PostOpen` on the
    // `Page` base class. `Page_ConfigureStartingPawns` does NOT declare
    // its own override, so `nameof(Page_ConfigureStartingPawns.PreOpen)`
    // resolved to `null` and Harmony PatchAll threw
    // `Patching exception in method null. Customization-page BioRemap
    // skipped.`. We target the base-class `Page.PostOpen` and bound the
    // patch to the actual subclass via the `__instance` runtime check
    // inside the Postfix. This pattern keeps the Bio-Remap active even
    // if Ludeon renames the hook in a later point-release: only the
    // base-class binding needs updating then.
    // Phase-5 audit-round-5 (updated 2026-08-06):
    // Harmony PatchAll cannot resolve inherited methods on Verse.Window
    // in RimWorld 1.6 ("Patching exception in method null").
    // The Bio-Remap is applied manually via Harmony.Patch() in Bootstrap.cs.
    // This class retains no [HarmonyPatch] attribute; it serves as a
    // plain static container for the Postfix method.
    public static class Page_ConfigureStartingPawnsBioPatch
    {
        // Phase-5 audit-round-5 fix (harmony reflection-dedup):
        // Some RimWorld point releases rename internal Verse.GameInitData
        // fields. When that happens our patch falls back to a Log.Warning
        // and continues. We dedup the warning via a HashSet so a user who
        // cycles through 5 save-loads in a session does not see the same
        // warning five times - matches the project's existing
        // CapabilityAudit once-logging pattern.
        private static readonly HashSet<string> _warnedReflectionKeys
            = new HashSet<string>(System.StringComparer.Ordinal);

        // Postfix is applied manually via Harmony.Patch() in Bootstrap.cs.
        // The [HarmonyPostfix] attribute is retained so the method signature
        // is self-documenting as a Harmony postfix.
        [HarmonyPostfix]
        public static void Postfix(Page __instance)
        {
            // Base-class binding is broad by design; restrict the Bio-Remap
            // application to the page we actually want to mutate so we do
            // not re-anchor age / skill budgets on unrelated screen opens.
            if (!(__instance is Page_ConfigureStartingPawns))
            {
                return;
            }

            if (Current.Game == null || Current.Game.InitData == null)
                return;

            // Verse.GameInitData uses the field name 'startingAndOptionalPawns'
            // (RimWorld 1.6.9371) for the combined display list of required
            // and optional colonists shown on Page_ConfigureStartingPawns. The
            // field is non-public; we access it via reflection so the patch
            // keeps building against future renames surfaced by PointRelease.
            var initData = Current.Game.InitData;
            if (initData == null) return;

            const string fieldKey = "startingAndOptionalPawns";
            var field = initData.GetType().GetField(
                fieldKey,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var startingPawnsRaw = field?.GetValue(initData) as IList;
            if (startingPawnsRaw == null)
            {
                // Once-only warning dedup so we don't spam the log across
                // multiple PreOpen calls within one game session.
                if (_warnedReflectionKeys.Add(fieldKey))
                {
                    Log.Warning(
                        "[Rimconemy.SurvivalProgression] Bio-Remap (customization page): " +
                        $"could not resolve Verse.GameInitData.{fieldKey}; patch no-op " +
                        "for this session. (Phase-5 audit-round-5: name-rotated fallback.)");
                }
                return;
            }

            int ageChanges = 0;
            int skillsAllocated = 0;
            int skillsSkipped = 0;
            foreach (var raw in startingPawnsRaw)
            {
                var pawn = raw as Pawn;
                if (pawn == null) continue;
                if (pawn.ageTracker == null)
                {
                    skillsSkipped++;
                    continue;
                }

                // Bug 1 fix (2026-08-04, post-image-audit): use ForceAge18
                // instead of FixAge. The new entry point re-anchors
                // BirthAbsTicks to TickManager.TicksAbs - 18y AND defends
                // against Storyteller backstories that overwrite AgeBiological
                // mid-flight (so we stamp BirthAbsTicks again at the end of
                // the per-pawn loop).
                if (CharacterSetup.ForceAge18(pawn))
                    ageChanges++;

                // Skills: 30-point default distribution. Does NOT write to
                // StoredBudgetAllocations - the SKBW post-Start window is
                // preserved for player agency.
                if (pawn.skills == null || pawn.skills.skills == null)
                {
                    skillsSkipped++;
                    continue;
                }

                try
                {
                    bool ok = CharacterSetup.DistributeSkillBudget(pawn);
                    if (ok)
                    {
                        skillsAllocated++;
                    }
                    else
                    {
                        Log.Warning(
                            "[Rimconemy.SurvivalProgression] Bio-Remap: DistributeSkillBudget "
                            + "failed for " + pawn.LabelShort + ". Skills left as backstory.");
                        skillsSkipped++;
                    }
                }
                catch (System.Exception ex)
                {
                    skillsSkipped++;
                    if (_warnedReflectionKeys.Add("DistributeSkillBudget:" + ex.GetType().Name))
                    {
                        Log.Warning(
                            "[Rimconemy.SurvivalProgression] Bio-Remap (customization page): " +
                            $"DistributeSkillBudget threw on {pawn.LabelShort}: " +
                            $"{ex.GetType().Name}: {ex.Message}. Skills left as backstory.");
                    }
                }
                // No defensive re-stamp of BirthAbsTicks here on purpose:
                // ForceAge18's tail already anchors BirthAbsTicks to
                // nowAbs - 18 * yearTicks, and the operations above touch
                // SkillRecord only - none write to ageTracker. A second
                // ForceAge18 call would be a no-op (bio=18, chrono=18, same
                // defendedBirthAbs), so call once per pawn.
            }

            // Single combined log line so debugging this patch produces one entry
            // per page-open, not N pawn-level entries.
            if (ageChanges > 0 || skillsAllocated > 0 || skillsSkipped > 0)
            {
                Log.Message(
                    "[Rimconemy.SurvivalProgression] Bio-Remap (customization page): " +
                    "ages normalised=" + ageChanges +
                    ", budgets distributed=" + skillsAllocated +
                    ", skills skipped=" + skillsSkipped +
                    " (age target=" + CharacterSetup.FixedBiologicalAge +
                    "/" + CharacterSetup.FixedChronologicalAge +
                    ", skill budget=" + CharacterSetup.SkillBudgetTotal + ").");
            }
        }
    }
}
