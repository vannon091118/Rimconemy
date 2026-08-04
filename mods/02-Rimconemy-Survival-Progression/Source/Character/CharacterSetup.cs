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
        /// </summary>
        public static bool FixAge(Pawn pawn)
        {
            if (pawn.ageTracker == null) return false;

            bool changed = false;
            if (pawn.ageTracker.AgeBiologicalYears != FixedBiologicalAge)
            {
                // RimWorld's AgeTracker uses AgeBiologicalTicks internally.
                // Setting it via reflection is fragile; instead we adjust
                // the birth date so the computed age equals 18.
                long targetBiologicalTicks = FixedBiologicalAge * GenDate.TicksPerYear;
                long currentBiologicalTicks = pawn.ageTracker.AgeBiologicalTicks;
                long ageAdjustment = currentBiologicalTicks - targetBiologicalTicks;

                // Adjust birth date to make biological age = 18
                pawn.ageTracker.BirthAbsTicks += ageAdjustment;
                changed = true;
            }

            if (pawn.ageTracker.AgeChronologicalYears != FixedChronologicalAge)
            {
                pawn.ageTracker.BirthAbsTicks = Find.TickManager.TicksAbs
                    - FixedChronologicalAge * GenDate.TicksPerYear;
                changed = true;
            }

            if (changed)
            {
                Log.Message($"[Rimconemy.SurvivalProgression] Fixed age for {pawn.LabelShort}: bio={FixedBiologicalAge}, chrono={FixedChronologicalAge}");
            }
            return changed;
        }

        /// <summary>
        /// Applies player-chosen budget (from SkillBudgetWindow).
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
                Log.Warning($"[Rimconemy.SurvivalProgression] Custom budget rejected for {pawn.LabelShort}: {spent}/{SkillBudgetTotal} cost-aware points.");
                return false;
            }

            foreach (var skillDef in EligibleSkills)
            {
                var skillRecord = pawn.skills.GetSkill(skillDef);
                if (skillRecord != null && !skillRecord.TotallyDisabled)
                    skillRecord.Level = sanitized.ContainsKey(skillDef) ? sanitized[skillDef] : SkillBudgetCalculator.MinPerSkill;
            }

            Log.Message($"[Rimconemy.SurvivalProgression] Custom budget applied to {pawn.LabelShort}: {spent}/{SkillBudgetTotal} cost-aware points");
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
        /// </summary>
        public static bool DistributeSkillBudget(Pawn pawn)
        {
            if (pawn?.skills == null) return false;

            var eligible = EligibleSkills
                .Where(skill => skill != null && pawn.skills.GetSkill(skill) != null && !pawn.skills.GetSkill(skill).TotallyDisabled)
                .ToList();
            var allocation = SkillBudgetCalculator.BuildDefaultAllocation(eligible);
            return ApplyBudget(pawn, allocation);
        }
    }
}
