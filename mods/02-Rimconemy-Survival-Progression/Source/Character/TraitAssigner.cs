using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Character
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02)
    /// Track A — Phase 1: H5-spec trait assignment.
    ///
    /// Decision flow uses SkillBudgetCalculator.Classify(balance) where
    /// balance = spent - NeutralCenter = spent - 25.
    ///   Buffer            (balance ∈ [-5, +3])  → no trait
    ///   PositiveLight     (balance ∈ [+4, +5])  → 1 light positive
    ///   PositiveStrong    (balance > +5)         → 1 strong positive
    ///   NegativeLight     (balance ∈ [-9, -6])  → 1 light negative
    ///   NegativeStrong    (balance ≤ -10)        → 2 heavy negative (StackRule)
    ///
    /// Specialization bonus: at least one skill ≥ SpecializationThreshold (7)
    /// → log-only for Phase A-1; actual Passion.Skill is set in Phase A-3
    /// (the field is private and needs reflection or Harmony, see R4 in spec).
    ///
    /// All trait defs are looked up via DefDatabase<TraitDef>.GetNamedSilentFail
    /// so a missing XML doesn't crash the assembler.
    /// </summary>
    public sealed class TraitSelectionResult
    {
        public int SpentPoints { get; }
        public int Balance { get; }
        public SkillBudgetCalculator.TraitZone Zone { get; }
        public IReadOnlyList<string> PositiveTraitIds { get; }
        public IReadOnlyList<string> NegativeTraitIds { get; }

        public TraitSelectionResult(
            int spentPoints,
            int balance,
            SkillBudgetCalculator.TraitZone zone,
            List<string> positiveTraitIds,
            List<string> negativeTraitIds)
        {
            SpentPoints = spentPoints;
            Balance = balance;
            Zone = zone;
            PositiveTraitIds = new ReadOnlyCollection<string>(new List<string>(positiveTraitIds ?? new List<string>()));
            NegativeTraitIds = new ReadOnlyCollection<string>(new List<string>(negativeTraitIds ?? new List<string>()));
        }
    }

    public static class TraitAssigner
    {
        // ── trait pools (XML defs live in Defs/Traits/Rimconemy_Trait_*.xml) ──

        public static readonly string[] HeavyNegativeTraits =
        {
            "Rimconemy_Trait_Exhausted",
            "Rimconemy_Trait_Paranoid",
            "Rimconemy_Trait_Frail",
        };

        public static readonly string[] LightNegativeTraits =
        {
            "Rimconemy_Trait_Unfocused",
            "Rimconemy_Trait_Hesitant",
        };

        public static readonly string[] LightPositiveTraits =
        {
            "Rimconemy_Trait_Hardy",
            "Rimconemy_Trait_Attentive",
        };

        public static readonly string[] StrongPositiveTraits =
        {
            "Rimconemy_Trait_QuickLearner",
            "Rimconemy_Trait_Unbreakable",
        };

        // ── public entry point (modern API) ──

        /// <summary>
        /// Assigns traits for a pawn based on a known spent-points total
        /// (caller already aggregated over the budget).
        /// </summary>
        public static void AssignForBudget(Pawn pawn, int spentPoints)
        {
            AssignForBudget(pawn, spentPoints, BuildPawnSeed(pawn));
        }

        /// <summary>
        /// Applies a deterministic selection using an explicit setup seed.
        /// The overload is the persistence-ready path for CharacterSetupState.
        /// </summary>
        public static void AssignForBudget(Pawn pawn, int spentPoints, int seed)
        {
            if (pawn?.story?.traits == null) return;

            var selection = SelectTraitsForBudget(
                spentPoints,
                seed,
                LightPositiveTraits,
                StrongPositiveTraits,
                LightNegativeTraits,
                HeavyNegativeTraits);

            ApplySelectedTraits(pawn, selection.PositiveTraitIds, "positive", spentPoints, selection.Balance);
            ApplySelectedTraits(pawn, selection.NegativeTraitIds, "negative", spentPoints, selection.Balance);
            if (selection.Zone == SkillBudgetCalculator.TraitZone.Buffer)
            {
                Log.Message($"[Rimconemy.SurvivalProgression] TraitAssigner: {pawn.LabelShort} in Buffer zone (spent={spentPoints}, balance={FormatBalance(selection.Balance)}). No trait from this system.");
            }

            CheckSpecialization(pawn);
        }

        /// <summary>
        /// Pure, deterministic H5 trait selection. The overload keeps positive
        /// and negative pools separate so the zone decides which polarity is
        /// eligible without consulting DefDatabase or global Verse.Rand state.
        /// </summary>
        public static TraitSelectionResult SelectTraitsForBudget(
            int spentPoints,
            int seed,
            IReadOnlyList<string> lightPositivePool,
            IReadOnlyList<string> strongPositivePool,
            IReadOnlyList<string> lightNegativePool,
            IReadOnlyList<string> heavyNegativePool)
        {
            int balance = spentPoints - SkillBudgetCalculator.NeutralCenter;
            var zone = SkillBudgetCalculator.Classify(balance);
            var positive = new List<string>();
            var negative = new List<string>();

            switch (zone)
            {
                case SkillBudgetCalculator.TraitZone.PositiveLight:
                    AddSelected(positive, lightPositivePool, seed, 0);
                    break;
                case SkillBudgetCalculator.TraitZone.PositiveStrong:
                    AddSelected(positive, strongPositivePool, seed, 1);
                    break;
                case SkillBudgetCalculator.TraitZone.NegativeLight:
                    AddSelected(negative, lightNegativePool, seed, 2);
                    break;
                case SkillBudgetCalculator.TraitZone.NegativeStrong:
                    AddSelected(negative, heavyNegativePool, seed, 3);
                    AddSelected(negative, heavyNegativePool, seed, 4);
                    break;
            }

            return new TraitSelectionResult(spentPoints, balance, zone, positive, negative);
        }

        // ── legacy compatibility wrapper ──

        /// <summary>
        /// Backwards-compatible wrapper for callers that don't track spent points.
        /// Aggregates from <c>pawn.skills.skills</c> via SkillBudgetCalculator.CostForLevel.
        /// </summary>
        public static void AssignTraitsForBudget(Pawn pawn, int? totalBudgetOverride = null)
        {
            if (pawn?.skills == null) return;

            int spent;
            if (totalBudgetOverride.HasValue)
            {
                spent = totalBudgetOverride.Value;
            }
            else
            {
                spent = pawn.skills.skills
                    .Where(r => r?.def != null)
                    .Sum(r => SkillBudgetCalculator.CostForLevel(r.Level));
            }

            AssignForBudget(pawn, spent);
        }

        // ── internals ──

        private static void AddSelected(List<string> selected, IReadOnlyList<string> pool, int seed, int slot)
        {
            if (selected == null || pool == null || pool.Count == 0) return;

            var candidates = new List<string>();
            for (int i = 0; i < pool.Count; i++)
            {
                string candidate = pool[i];
                if (!string.IsNullOrEmpty(candidate) && !candidates.Contains(candidate))
                    candidates.Add(candidate);
            }
            candidates.Sort(System.StringComparer.Ordinal);
            if (candidates.Count == 0) return;

            int index = PositiveModulo(Mix(seed, slot), candidates.Count);
            for (int offset = 0; offset < candidates.Count; offset++)
            {
                string chosen = candidates[(index + offset) % candidates.Count];
                if (!selected.Contains(chosen))
                {
                    selected.Add(chosen);
                    return;
                }
            }
        }

        private static void ApplySelectedTraits(Pawn pawn, IReadOnlyList<string> selected, string category, int spent, int balance)
        {
            if (pawn?.story?.traits == null || selected == null) return;

            for (int i = 0; i < selected.Count; i++)
            {
                string defName = selected[i];
                var traitDef = DefDatabase<TraitDef>.GetNamedSilentFail(defName);
                if (traitDef == null)
                {
                    Log.Warning($"[Rimconemy.SurvivalProgression] TraitAssigner: TraitDef '{defName}' not found in DefDatabase. Skipping {category} trait.");
                    continue;
                }

                if (pawn.story.traits.HasTrait(traitDef))
                {
                    Log.Message($"[Rimconemy.SurvivalProgression] TraitAssigner: {pawn.LabelShort} already has '{defName}', skipping duplicate.");
                    continue;
                }

                pawn.story.traits.GainTrait(new Trait(traitDef));
                Log.Message($"[Rimconemy.SurvivalProgression] TraitAssigner: {pawn.LabelShort} got {category} trait '{defName}' (spent={spent}, balance={FormatBalance(balance)}).");
            }
        }

        /// <summary>
        /// Phase A-1: log-only. Phase A-3 will set SkillRecord.passion via reflection.
        /// </summary>
        private static void CheckSpecialization(Pawn pawn)
        {
            if (pawn?.skills == null) return;
            foreach (var r in pawn.skills.skills)
            {
                if (r.Level >= SkillBudgetCalculator.SpecializationThreshold)
                {
                    Log.Message($"[Rimconemy.SurvivalProgression] Specialization: {pawn.LabelShort} has {r.def.label} at level {r.Level} (passion assignment deferred to Phase A-3).");
                    break;
                }
            }
        }

        /// <summary>Stable FNV-1a hash used by the pure selector.</summary>
        public static int StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                if (value == null) return (int)hash;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619u;
                }
                return (int)hash;
            }
        }

        private static int BuildPawnSeed(Pawn pawn)
        {
            if (pawn == null) return 0;
            string thingId = pawn.ThingID;
            return StableHash(string.IsNullOrEmpty(thingId) ? pawn.LabelShort : thingId);
        }

        private static int Mix(int seed, int slot)
        {
            unchecked
            {
                uint value = (uint)seed + 0x9E3779B9u * (uint)(slot + 1);
                value ^= value >> 16;
                value *= 0x7FEB352Du;
                value ^= value >> 15;
                value *= 0x846CA68Bu;
                value ^= value >> 16;
                return (int)value;
            }
        }

        private static int PositiveModulo(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private static string FormatBalance(int b) => b.ToString("+0;-0;0");
    }
}
