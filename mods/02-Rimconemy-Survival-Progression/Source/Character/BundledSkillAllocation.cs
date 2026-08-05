using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace Rimconemy.SurvivalProgression.Character
{
    /// <summary>UI/progression bundles over the canonical vanilla SkillDefs.</summary>
    public static class BundledSkillAllocation
    {
        public const int TotalBudget = SkillBudgetCalculator.TotalBudget;
        public const int MaxPointsPerBundle = 10;

        public sealed class BundleDefinition
        {
            public string Id { get; }
            public string Label { get; }
            public int Weight { get; }
            public IReadOnlyList<string> SkillDefNames { get; }

            public BundleDefinition(string id, string label, int weight, params string[] skillDefNames)
            {
                Id = id;
                Label = label;
                Weight = weight;
                SkillDefNames = Array.AsReadOnly(skillDefNames ?? new string[0]);
            }
        }

        private static readonly IReadOnlyList<BundleDefinition> Definitions =
            new List<BundleDefinition>
            {
                new BundleDefinition("Mining", "Bergen", 3, "Mining"),
                new BundleDefinition("Build", "Bauen & Gestalten", 3, "Construction", "Artistic"),
                new BundleDefinition("Food", "Nahrung", 3, "Cooking", "Plants", "Animals"),
                new BundleDefinition("Craft", "Handwerk", 2, "Crafting"),
                new BundleDefinition("Combat", "Kampf", 2, "Shooting", "Melee"),
                new BundleDefinition("Research", "Forschung", 2, "Intellectual"),
                new BundleDefinition("Care", "Medizin & Sozial", 1, "Medical", "Social"),
            }.AsReadOnly();

        private static readonly Dictionary<string, BundleDefinition> ById =
            Definitions.ToDictionary(definition => definition.Id, definition => definition, StringComparer.Ordinal);

        public static IReadOnlyList<BundleDefinition> All => Definitions;

        public static Dictionary<string, int> EmptyAllocation()
        {
            return Definitions.ToDictionary(definition => definition.Id, definition => 0, StringComparer.Ordinal);
        }

        public static Dictionary<string, int> BuildDefaultAllocation()
        {
            var result = EmptyAllocation();
            int remaining = TotalBudget;
            while (remaining > 0)
            {
                var candidate = Definitions
                    .Where(definition => result[definition.Id] < MaxPointsPerBundle)
                    .OrderByDescending(definition => definition.Weight - result[definition.Id])
                    .ThenBy(definition => definition.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (candidate == null) break;
                result[candidate.Id]++;
                remaining--;
            }
            return result;
        }

        public static int SumPoints(IReadOnlyDictionary<string, int> allocation)
        {
            return allocation == null ? 0 : allocation.Sum(pair => Math.Max(0, pair.Value));
        }

        public static bool Validate(IReadOnlyDictionary<string, int> allocation, out string reason)
        {
            reason = null;
            if (allocation == null)
            {
                reason = "allocation-null";
                return false;
            }

            foreach (var definition in Definitions)
            {
                int points = allocation.TryGetValue(definition.Id, out var value) ? value : 0;
                if (points < 0 || points > MaxPointsPerBundle)
                {
                    reason = definition.Id + "-cap";
                    return false;
                }
            }

            if (SumPoints(allocation) > TotalBudget)
            {
                reason = "budget-overrun";
                return false;
            }
            return true;
        }

        public static bool IsCanonicalComplete(IReadOnlyDictionary<string, int> allocation)
        {
            if (!Validate(allocation, out _)
                || allocation.Count != Definitions.Count) return false;
            return Definitions.All(definition => allocation.ContainsKey(definition.Id));
        }

        public static bool IsComplete(IReadOnlyDictionary<string, int> allocation)
        {
            return IsCanonicalComplete(allocation) && SumPoints(allocation) == TotalBudget;
        }

        public static Dictionary<string, int> ExpandToVanillaSkillNames(IReadOnlyDictionary<string, int> allocation)
        {
            var expanded = new Dictionary<string, int>(StringComparer.Ordinal);
            if (allocation == null) return expanded;
            foreach (var definition in Definitions)
            {
                int points = allocation.TryGetValue(definition.Id, out var value)
                    ? Math.Max(0, Math.Min(MaxPointsPerBundle, value)) : 0;
                foreach (var skillName in definition.SkillDefNames)
                    expanded[skillName] = points;
            }
            return expanded;
        }

        public static Dictionary<SkillDef, int> ExpandToVanillaSkills(
            IReadOnlyDictionary<string, int> allocation,
            IEnumerable<SkillDef> availableSkills)
        {
            var byName = (availableSkills ?? Enumerable.Empty<SkillDef>())
                .Where(skill => skill != null)
                .ToDictionary(skill => skill.defName, skill => skill, StringComparer.Ordinal);
            var expanded = new Dictionary<SkillDef, int>();
            foreach (var pair in ExpandToVanillaSkillNames(allocation))
            {
                if (byName.TryGetValue(pair.Key, out var skill))
                    expanded[skill] = pair.Value;
            }
            return expanded;
        }

        public static Dictionary<string, int> FromVanillaSkillNames(IReadOnlyDictionary<string, int> skillLevels)
        {
            var result = EmptyAllocation();
            if (skillLevels == null || skillLevels.Count == 0) return result;

            foreach (var definition in Definitions)
            {
                int max = 0;
                foreach (var skillName in definition.SkillDefNames)
                {
                    if (skillLevels.TryGetValue(skillName, out var level))
                        max = Math.Max(max, Math.Min(MaxPointsPerBundle, Math.Max(0, level)));
                }
                result[definition.Id] = max;
            }
            return NormalizeToBudget(result);
        }

        public static Dictionary<string, int> NormalizeToBudget(IReadOnlyDictionary<string, int> allocation)
        {
            var normalized = EmptyAllocation();
            foreach (var definition in Definitions)
            {
                int points = allocation != null && allocation.TryGetValue(definition.Id, out var value) ? value : 0;
                normalized[definition.Id] = Math.Max(0, Math.Min(MaxPointsPerBundle, points));
            }

            int sum = SumPoints(normalized);
            while (sum > TotalBudget)
            {
                var candidate = Definitions
                    .Where(definition => normalized[definition.Id] > 0)
                    .OrderBy(definition => normalized[definition.Id])
                    .ThenBy(definition => definition.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (candidate == null) break;
                normalized[candidate.Id]--;
                sum--;
            }

            if (sum == 0) return BuildDefaultAllocation();
            while (sum < TotalBudget)
            {
                var candidate = Definitions
                    .Where(definition => normalized[definition.Id] < MaxPointsPerBundle)
                    .OrderByDescending(definition => normalized[definition.Id])
                    .ThenByDescending(definition => definition.Weight)
                    .ThenBy(definition => definition.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (candidate == null) break;
                normalized[candidate.Id]++;
                sum++;
            }
            return normalized;
        }
    }

    public sealed class DerivedRoleResult
    {
        public string PrimaryBundleId { get; }
        public string SecondaryBundleId { get; }
        public string Label { get; }

        public DerivedRoleResult(string primary, string secondary, string label)
        {
            PrimaryBundleId = primary ?? string.Empty;
            SecondaryBundleId = secondary ?? string.Empty;
            Label = label ?? string.Empty;
        }
    }

    public static class DerivedRoleProjection
    {
        public static DerivedRoleResult Project(IReadOnlyDictionary<string, int> allocation)
        {
            var ranked = BundledSkillAllocation.All
                .Select(definition => new
                {
                    Definition = definition,
                    Points = allocation != null && allocation.TryGetValue(definition.Id, out var value) ? value : 0
                })
                .OrderByDescending(item => item.Points)
                .ThenBy(item => item.Definition.Id, StringComparer.Ordinal)
                .ToList();

            if (ranked.Count == 0 || ranked[0].Points <= 0)
                return new DerivedRoleResult(string.Empty, string.Empty, "Unassigned");

            string primary = ranked[0].Definition.Id;
            string secondary = ranked.Count > 1 && ranked[1].Points > 0 ? ranked[1].Definition.Id : string.Empty;
            string label = ranked[0].Definition.Label;
            if (!string.IsNullOrEmpty(secondary)) label += " / " + ranked[1].Definition.Label;
            return new DerivedRoleResult(primary, secondary, label);
        }
    }
}
