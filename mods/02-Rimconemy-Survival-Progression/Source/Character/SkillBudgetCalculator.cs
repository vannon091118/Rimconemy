using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Character
{
    /// <summary>
    /// Owner: Survival &amp; Progression (Package 02)
    /// Track A — Phase 1: H5 cost-aware skill budget calculator.
    ///
    /// Pure logic — no Game/Map dependencies. Reused by SkillBudgetWindow
    /// for live validation, by CharacterSetup for default distribution,
    /// and by TraitAssigner for balance classification (spent - NeutralCenter).
    ///
    /// Spec: docs/H5-character-setup-formula.md §2 + §3
    ///   TotalBudget = 30, NeutralCenter = 25, LinearMax = 10
    ///   Levels 0-10: 1 point each. Levels 11+: progressive cost.
    ///   Stack: {2,2,3,3,4,5,6,7,8,10}
    /// </summary>
    public static class SkillBudgetCalculator
    {
        public const int TotalBudget = 30;
        public const int NeutralCenter = 25;
        public const int MaxSkillLevel = 20;
        public const int LinearMaxLevel = 10;
        public const int MinPerSkill = 0;

        /// <summary>Single skill ≥ 7 triggers the Specialization passion flag (H5 §3 callout).</summary>
        public const int SpecializationThreshold = 7;

        // H5 §3 — Buffer zone thresholds
        public const int NegativeThresholdMaxBalance = -5;   // balance < -5 → negative
        public const int PositiveThresholdMinBalance = 3;    // balance > +3 → positive
        // -5..+3 → neutral
        // Below -10 → 2 negative traits; -6..-9 → 1 negative trait
        public const int TwoNegativeTraitsBalanceCutoff = -10;

        // H5 §2 — progressive cost above level 10 (10 entries for levels 11..20)
        // indexed 0 → level 11 costs +2; 1 → level 12 costs +2; ... ; 9 → level 20 costs +10
        private static readonly int[] ProgressiveCosts = { 2, 2, 3, 3, 4, 5, 6, 7, 8, 10 };

        /// <summary>
        /// Returns the cumulative budget cost for reaching <paramref name="level"/>.
        /// Level 0 costs 0. Levels 1..10 cost 1 each. Levels 11+ scale progressively.
        /// </summary>
        public static int CostForLevel(int level)
        {
            if (level <= 0) return 0;
            if (level <= LinearMaxLevel) return level;

            int cost = LinearMaxLevel;
            int extra = level - LinearMaxLevel;
            for (int i = 0; i < extra && i < ProgressiveCosts.Length; i++)
                cost += ProgressiveCosts[i];
            return cost;
        }

        /// <summary>
        /// Sum of cumulative costs over the actual level map.
        /// </summary>
        public static int CalculateSpentPoints(Dictionary<SkillDef, int> distribution)
        {
            if (distribution == null) return 0;
            int total = 0;
            foreach (var kvp in distribution)
            {
                if (kvp.Value <= 0) continue;
                total += CostForLevel(kvp.Value);
            }
            return total;
        }

        /// <summary>
        /// Builds a stable, cost-aware default allocation for an ordered skill pool.
        /// The result spends as much of the 30-point budget as affordable, never
        /// exceeding the budget or the per-skill cap.
        /// </summary>
        public static Dictionary<SkillDef, int> BuildDefaultAllocation(IEnumerable<SkillDef> skills)
        {
            var ordered = (skills ?? Enumerable.Empty<SkillDef>())
                .Where(skill => skill != null)
                .OrderBy(skill => skill.defName, System.StringComparer.Ordinal)
                .ToList();
            var allocation = new Dictionary<SkillDef, int>();
            foreach (var skill in ordered) allocation[skill] = MinPerSkill;

            int remaining = TotalBudget;
            bool progressed;
            do
            {
                progressed = false;
                foreach (var skill in ordered)
                {
                    int current = allocation[skill];
                    if (current >= MaxSkillLevel) continue;
                    int nextCost = CostForLevel(current + 1) - CostForLevel(current);
                    if (nextCost > remaining) continue;

                    allocation[skill] = current + 1;
                    remaining -= nextCost;
                    progressed = true;
                }
            }
            while (remaining > 0 && progressed);

            return allocation;
        }

        /// <summary>
        /// Live snapshot of a player's budget allocation.
        /// SkillMap: SkillDef → effective level (0..MaxSkillLevel).
        /// </summary>
        public struct BudgetResult
        {
            public int TotalBudget;
            public int SpentPoints;
            /// <summary>Spent - NeutralCenter. Drives trait classification.</summary>
            public int Balance;
            public bool WithinBudget;
            /// <summary>"Negative" | "Neutral" | "Positive" — coarse zones for UI badges.</summary>
            public string ZoneLabel;
            /// <summary>"fine" or "warning" or "over"; UI badge colour.</summary>
            public string BudgetStatus;

            public override string ToString()
                => $"[Budget {SpentPoints}/{TotalBudget} (balance={Balance:+#;-#;0}) -> {ZoneLabel}/{BudgetStatus}]";
        }

        /// <summary>
        /// Classify a distribution into a BudgetResult. The pure function
        /// SkillBudgetWindow must call for live validation.
        /// </summary>
        public static BudgetResult ValidateBudget(Dictionary<SkillDef, int> distribution)
        {
            int spent = CalculateSpentPoints(distribution);
            int balance = spent - NeutralCenter;

            string zone;
            if (balance < NegativeThresholdMaxBalance) zone = "Negative";
            else if (balance > PositiveThresholdMinBalance) zone = "Positive";
            else zone = "Neutral";

            string status;
            if (spent > TotalBudget) status = "over";
            else if (spent == TotalBudget) status = "fine";
            else status = "warning";

            return new BudgetResult
            {
                TotalBudget = TotalBudget,
                SpentPoints = spent,
                Balance = balance,
                WithinBudget = spent <= TotalBudget,
                ZoneLabel = zone,
                BudgetStatus = status,
            };
        }

        /// <summary>
        /// Helper for the UI — recommended level cap a player should not
        /// exceed if they want a specific number of points. Used inside
        /// the SkillBudgetWindow to clamp stepper-buttons.
        /// </summary>
        public static int RecommendMaxLevel(int remainingBudget)
        {
            if (remainingBudget <= 0) return 0;
            int level = 0;
            // Greedy: push to highest affordable level
            while (level <= MaxSkillLevel && CostForLevel(level + 1) <= remainingBudget)
                level++;
            return level;
        }

        /// <summary>
        /// Hard ceiling from H5. SkillBudgetWindow clamps input to this.
        /// </summary>
        public static int ClampLevel(int requested)
        {
            if (requested < 0) return 0;
            if (requested > MaxSkillLevel) return MaxSkillLevel;
            return requested;
        }

        /// <summary>
        /// Stable debug-column for tests: cost cumulative chain per level.
        /// Returns cost values for levels 0..20. 21 entries; index = level.
        /// </summary>
        public static int[] CostTable()
        {
            var t = new int[MaxSkillLevel + 1];
            for (int i = 0; i <= MaxSkillLevel; i++) t[i] = CostForLevel(i);
            return t;
        }

        /// <summary>
        /// Number of negative traits a balance should produce (0, 1, or 2).
        /// 0 if balance ≥ -5; 1 if -10 < balance ≤ -5; 2 if balance ≤ -10.
        /// Positive zone never produces negative traits.
        /// </summary>
        public static int NegativeTraitCount(int balance)
        {
            if (balance >= NegativeThresholdMaxBalance) return 0;
            if (balance > TwoNegativeTraitsBalanceCutoff) return 1;
            return 2;
        }

        /// <summary>
        /// Number of positive traits a balance should produce (0 or 1).
        /// 0 if balance ≤ +3; 1 if balance > +3.
        /// </summary>
        public static int PositiveTraitCount(int balance)
        {
            if (balance > PositiveThresholdMinBalance) return 1;
            return 0;
        }

        /// <summary>Trait-zone classification (used by TraitAssigner to pick pool + count).</summary>
        public enum TraitZone
        {
            /// <summary>balance ∈ [-5, +3] → no trait from this system.</summary>
            Buffer,
            /// <summary>balance ∈ [+4, +5] → 1 light positive trait (per H5 §3, MaxPositiveTraits=1).</summary>
            PositiveLight,
            /// <summary>balance > +5 → 1 strong positive trait.</summary>
            PositiveStrong,
            /// <summary>balance ∈ [-9, -6] → 1 light negative trait.</summary>
            NegativeLight,
            /// <summary>balance ≤ -10 → 2 heavy negative traits (StackRule).</summary>
            NegativeStrong,
        }

        /// <summary>
        /// Classify a balance value into a TraitZone.
        /// H5 §3: balance thresholds -5 / +3 ; strong zone = balance ≤ -10 OR > +5.
        /// </summary>
        public static TraitZone Classify(int balance)
        {
            if (balance > 5) return TraitZone.PositiveStrong;
            if (balance > PositiveThresholdMinBalance) return TraitZone.PositiveLight;
            if (balance >= NegativeThresholdMaxBalance) return TraitZone.Buffer;
            if (balance > TwoNegativeTraitsBalanceCutoff) return TraitZone.NegativeLight;
            return TraitZone.NegativeStrong;
        }
    }
}
