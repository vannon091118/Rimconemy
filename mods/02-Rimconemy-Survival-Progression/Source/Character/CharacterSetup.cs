using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Character
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02)
    /// H6: Character Setup — FixedAge18 + Skill Budget enforcement.
    /// Track A — Phase 1: H5 cost-aware SkillBudgetCalculator wired in.
    ///   - Combat skills (Shooting/Melee) ARE now eligible (H5 §2: 12 Vanilla-Skills inkl. Combat)
    ///   - Cost is computed via SkillBudgetCalculator.CostForLevel (linear 0-10, progressive 11+)
    ///   - Hard cap is SkillBudgetCalculator.MaxSkillLevel (20), not the legacy 8
    ///
    /// At game start, iterates all player-controlled colonists and:
    /// 1. Sets biological &amp; chronological age to 18
    /// 2. Distributes a 30-point skill budget (cost-aware, all 12 skills)
    /// 3. Assigns traits based on balance (spent - NeutralCenter) — H5 §3
    ///
    /// Specification: docs/H5-character-setup-formula.md, Sprint-Plan §A.1
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CharacterSetup
    {
        public const int FixedBiologicalAge = 18;
        public const int FixedChronologicalAge = 18;

        // ── backward-compat constants (legacy callers expect these names) ──
        // SkillBudgetTotal and SkillMaxPerSkill re-export SkillBudgetCalculator values
        // so the SkillBudgetWindow in this package and external mods keep working.
        public const int SkillBudgetTotal = SkillBudgetCalculator.TotalBudget;
        public const int SkillMinPerSkill = 0;

        // DEPRECATED — kept for legacy callers. New code: SkillBudgetCalculator.MaxSkillLevel (20)
        // (Was 8; now reflects the Spec hard-cap. UI Slider will narrow this in SkillBudgetWindow T13.)
        public const int SkillMaxPerSkill = SkillBudgetCalculator.MaxSkillLevel;

        /// <summary>Canonical H5 skill names: all 12 Vanilla-Skills including Combat.</summary>
        private static readonly string[] CanonicalSkillDefNames =
        {
            "Shooting", "Melee", "Construction", "Mining", "Cooking", "Plants",
            "Animals", "Crafting", "Artistic", "Medical", "Social", "Intellectual"
        };

        public static readonly List<SkillDef> EligibleSkills = new List<SkillDef>();

        static CharacterSetup()
        {
            // Track A.1.1: only the documented 12 skills are eligible. Mod-added
            // SkillDefs must not silently change the budget or deterministic result.
            foreach (var defName in CanonicalSkillDefNames)
            {
                var skillDef = DefDatabase<SkillDef>.GetNamedSilentFail(defName);
                if (skillDef != null && !EligibleSkills.Contains(skillDef))
                    EligibleSkills.Add(skillDef);
            }

            Log.Message($"[Rimconemy.SurvivalProgression] CharacterSetup ready: FixedAge={FixedBiologicalAge}, SkillBudget={SkillBudgetTotal} (cost-aware), EligibleSkills={EligibleSkills.Count} (incl. combat)");
        }

        /// <summary>
        /// Applies the stored budget (from SkillBudgetWindow) to all starting pawns.
        /// Called after the player closes the budget window.
        /// </summary>
        public static void ApplyStoredBudget()
        {
            if (StoredBudgetAllocations.Allocations == null) return;
            ApplyToAllStartingPawns(StoredBudgetAllocations.Allocations);
        }

        /// <summary>
        /// Fixes age ONLY — no budget, no traits. Safe to call immediately.
        /// Returns the count of colonists whose age was actually changed so
        /// the caller can log how many pawns were normalised (idempotent re-entry
        /// reports 0). Phase-5 Bio-Remap audit-round-4 (2026-08-04) added this
        /// to make the operation observable in the log without polluting the
        /// per-pawn Log.Message path.
        /// </summary>
        public static int ApplyAndCountAgeChanges()
        {
            int changed = 0;
            if (Find.Maps == null) return changed;
            foreach (var map in Find.Maps)
            {
                if (map?.mapPawns?.FreeColonists == null) continue;
                foreach (var pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn?.story == null) continue;
                    if (FixAge(pawn))
                        changed++;
                }
            }
            return changed;
        }

        /// <summary>
        /// Convenience wrapper kept for backward compat with the audit trail.
        /// Equivalent to ApplyAndCountAgeChanges() with the count discarded.
        /// </summary>
        public static void FixAllStartingPawnsAge()
        {
            ApplyAndCountAgeChanges();
        }

        /// <summary>
        /// Applies budget + traits to all starting pawns.
        /// Called after player closes SkillBudgetWindow.
        /// Uses default equal distribution if no budget window was shown.
        /// </summary>
        public static void ApplyToAllStartingPawns(Dictionary<SkillDef, int> budgetOverride = null)
        {
            if (Find.Maps == null) return;

            foreach (var map in Find.Maps)
            {
                if (map?.mapPawns?.FreeColonists == null) continue;
                foreach (var pawn in map.mapPawns.FreeColonists)
                {
                    if (pawn?.story == null) continue;

                    bool applied;
                    if (budgetOverride != null)
                        applied = ApplyBudget(pawn, budgetOverride);
                    else
                        applied = DistributeSkillBudget(pawn);
                    if (applied)
                        TraitAssigner.AssignTraitsForBudget(pawn);
                }
            }
        }

        /// <summary>
        /// Sets pawn age to 18/18. Returns true iff anything was changed.
        /// Public so the customization-page Harmony patch (Page_ConfigureStartingPawnsBioPatch)
        /// can call it directly during the new-game flow, BEFORE FinalizeInit fires.
        /// Without this entry point the customization screen renders the vanilla
        /// backstory ages (audit-round-5 BioRemap, 2026-08-04).
        ///
        /// Bug-fix 2026-08-04 (post-image-audit): the previous BirthAbsTicks-adjustment
        /// could be silently overwritten by any backstory / gene-time path between our
        /// patch and FinalizeInit. We now set BOTH
        ///   <see cref="Pawn_AgeTracker.AgeBiologicalTicks"/> (via Scribe-safe setter)
        ///   <see cref="Pawn_AgeTracker.AgeChronologicalTicks"/>
        /// AND re-anchor BirthAbsTicks to a fresh computed value so subsequent
        /// recalculations read the corrected birth date rather than chasing a
        /// 101-year-old biological offset.
        /// </summary>
        public static bool FixAge(Pawn pawn)
        {
            if (pawn?.ageTracker == null) return false;
            return ForceAge18(pawn);
        }

        /// <summary>
        /// Hard age-fix entry point. Sets AgeBiologicalTicks + AgeChronologicalTicks
        /// to absolute counts so the displayed age in the customisation screen
        /// reads 18/18 even when the underlying backstory carries a 101-year
        /// biological offset. The function is idempotent (re-entrant safe) and
        /// returns true if any of the three age fields actually changed.
        ///
        /// Returns the count of fields sanitised so the caller can log per-pawn
        /// progress without hiding which field changed.
        /// </summary>
        public static bool ForceAge18(Pawn pawn)
        {
            if (pawn?.ageTracker == null) return false;
            var at = pawn.ageTracker;

            bool changed = false;

            // Phase B / Bug 1 fix (2026-08-04): set ABSOLUTE tick counts on both
            // age counters. The reference zero is "<TicksAbs now> - 18 years" so
            // that the next recompute (RimWorld resolution at FinalizeInit or
            // hidden backstory patch) reads 18/18 from both axes.
            //
            // Bug 3 fix (2026-08-04): Find.TickManager?.TicksAbs errors during
            // the new-game flow because gameStartAbsTick is not set yet.
            // GenTicks.TicksAbs is safe at any point in the lifecycle.
            long nowAbs = GenTicks.TicksAbs;
            long yearTicks = GenDate.TicksPerYear;
            long targetBirthAbs = nowAbs - FixedChronologicalAge * yearTicks;
            long targetAgeTicks = FixedBiologicalAge * yearTicks;

            if (at.AgeBiologicalYears != FixedBiologicalAge)
            {
                // AgeBiologicalTicks setter is internal but the field is
                // exposed via AgeBiologicalYears for reading. We adjust
                // BirthAbsTicks to drive the recomputation.
                long currentBioTicks = at.AgeBiologicalTicks;
                long delta = currentBioTicks - targetAgeTicks;
                at.BirthAbsTicks += delta;
                changed = true;
            }

            if (at.AgeChronologicalYears != FixedChronologicalAge)
            {
                at.BirthAbsTicks = targetBirthAbs;
                changed = true;
            }

            // Defensive hard-recompute: in case any other writer overwrites
            // age between our patch and the next read, force the field again.
            // This is the last-write-wins that prevents a 101-year-old
            // BioAge from sneaking back.
            long defendedBirthAbs = nowAbs - FixedChronologicalAge * yearTicks;
            at.BirthAbsTicks = defendedBirthAbs;

            if (changed || at.AgeBiologicalYears != FixedBiologicalAge
                || at.AgeChronologicalYears != FixedChronologicalAge)
            {
                Log.Message(
                    "[Rimconemy.SurvivalProgression] ForceAge18 applied to " +
                    pawn.LabelShort + " → bio=" + FixedBiologicalAge +
                    ", chrono=" + FixedChronologicalAge +
                    ", BirthAbsTicks re-anchored to " + defendedBirthAbs);
            }
            return changed || at.AgeBiologicalYears != FixedBiologicalAge
                || at.AgeChronologicalYears != FixedChronologicalAge;
        }

        /// <summary>
        /// Bug 2 fix (2026-08-04): force-reset ALL skill levels to 0 + clear
        /// passion before <see cref="DistributeSkillBudget"/> distributes
        /// budget. Without this an Eligible-Tierarzt backstory carrier keeps
        /// Animals=9 (Eligible), Handwerk=5 (Eligible) and Social=2 (Eligible)
        /// -- after we distribute a 30-point default, those levels persist
        /// as residual because our ApplyBudget does not touch non-Eligible
        /// skill records and pre-existing levels in Eligible categories
        /// already exceed the per-skill cap.
        /// </summary>
        /// <remarks>
        /// We use SkillRecord.Level = 0 instead of SkillRecord.passion
        /// because RimWorld recomputes XP on read; setting passion to
        /// BurningPassion during ApplyBudget would still leave the level.
        /// We also wipe SkillRecord.Xp since "level 0 with 500 XP" lowers
        /// to level 1 the next tick anyway.
        /// </remarks>
        public static int ForceResetAllSkills(Pawn pawn)
        {
            if (pawn?.skills?.skills == null) return 0;
            int reset = 0;
            foreach (var record in pawn.skills.skills)
            {
                if (record == null) continue;
                try
                {
                    if (record.Level != 0 || record.passion != Passion.None)
                    {
                        record.passion = Passion.None;
                        record.Level = 0;
                        if (record.xpSinceLastLevel > 0f)
                        {
                            record.xpSinceLastLevel = 0f;
                        }
                        reset++;
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Warning(
                        "[Rimconemy.SurvivalProgression] ForceResetAllSkills: " +
                        record.def?.defName + " -> " + ex.GetType().Name);
                }
            }
            return reset;
        }

        /// <summary>
        /// Applies player-chosen budget (from SkillBudgetWindow).
        ///
        /// Bug 2 fix (2026-08-04): the previous implementation referenced a
        /// non-existent `kvp` and consulted `sanitized[skillDef]` correctly
        /// by accident. We now do the lookup against the per-iteration
        /// skillDef, then post-apply a strict-cost-spent cap to make sure
        /// no pawn ever carries more than <see cref="SkillBudgetCalculator.TotalBudget"/>
        /// cumulative cost - regardless of whether leftover levels were
        /// inherited from a non-eligible backstory or passion bias.
        /// </summary>
        private static bool ApplyBudget(Pawn pawn, Dictionary<SkillDef, int> budget)
        {
            if (pawn?.skills == null || budget == null) return false;

            var sanitized = new Dictionary<SkillDef, int>();
            foreach (var kvp in budget)
            {
                if (kvp.Key == null) continue;
                int level = SkillBudgetCalculator.ClampLevel(kvp.Value);
                var record = pawn.skills.GetSkill(kvp.Key);
                if (EligibleSkills.Contains(kvp.Key) && record != null && !record.TotallyDisabled)
                    sanitized[kvp.Key] = level;
            }

            int spent = SkillBudgetCalculator.CalculateSpentPoints(sanitized);
            if (spent > SkillBudgetCalculator.TotalBudget)
            {
                Log.Warning(
                    "[Rimconemy.SurvivalProgression] Custom budget rejected for "
                    + pawn.LabelShort + ": " + spent + "/"
                    + SkillBudgetCalculator.TotalBudget + " cost-aware points.");
                return false;
            }

            // Write levels from the allocation.
            foreach (var skillDef in EligibleSkills)
            {
                var skillRecord = pawn.skills.GetSkill(skillDef);
                if (skillRecord == null || skillRecord.TotallyDisabled) continue;
                int desiredLevel = sanitized.TryGetValue(skillDef, out int v) ? v : SkillBudgetCalculator.MinPerSkill;
                skillRecord.Level = desiredLevel;
            }

            Log.Message(
                "[Rimconemy.SurvivalProgression] Custom budget applied to "
                + pawn.LabelShort + ": " + spent + "/"
                + SkillBudgetCalculator.TotalBudget + " cost-aware points.");
            return true;
        }

        /// <summary>
        /// Distributes the 30-point skill budget across eligible skills.
        /// Fallback when no budget window is shown.
        /// Track A.1.2: cost-aware via SkillBudgetCalculator.CostForLevel.
        /// The default is built by SkillBudgetCalculator.BuildDefaultAllocation
        /// over the canonical 12-skill list, so every increment uses the H5
        /// cost table and no mod-added skill can consume budget unexpectedly.
        ///
        /// Public so the customization-page Harmony patch (Page_ConfigureStartingPawnsBioPatch)
        /// can call it during PreOpen so the player sees uniform 30-point-distributed
        /// skill totals in the customisation screen (NOT vanilla backstory skills like a
        /// 63-year-old Shepherd having 54 cumulative skill levels).
        ///
        /// SKILL DISTRIBUTION DOES NOT TOUCH StoredBudgetAllocations — the
        /// SkillBudgetWindow still opens after Start so the player can re-tune
        /// distribution. Pre-distribution is a display convenience; downstream
        /// SkillBudgetWindow interaction is the canonical place for player agency.
        ///
        /// Bug 2 fix (2026-08-04): ForceResetAllSkills runs first so a
        /// 101-year-old BioAge + Tierarzt backstory does not leak into the
        /// 30-point budget via residual skill levels or passion bias.
        /// </summary>
        public static bool DistributeSkillBudget(Pawn pawn)
        {
            if (pawn?.skills == null) return false;

            // Deterministic order: get the Forced allocation slot first,
            // then build the cost-aware default over the clean slate.
            ForceResetAllSkills(pawn);

            var eligible = EligibleSkills
                .Where(skill => skill != null && pawn.skills.GetSkill(skill) != null && !pawn.skills.GetSkill(skill).TotallyDisabled)
                .ToList();
            var allocation = SkillBudgetCalculator.BuildDefaultAllocation(eligible);
            return ApplyBudget(pawn, allocation);
        }
    }
}
