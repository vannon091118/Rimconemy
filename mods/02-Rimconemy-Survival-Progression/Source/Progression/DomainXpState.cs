using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Progression
{
    /// <summary>
    /// Phase 8.1 — Domain-XP persistence hub. Holds per-domain XP totals,
    /// derived level per domain, and a single idempotency set of accepted
    /// action-keys. Save/load mirror uses the parallel-list pattern that
    /// <see cref="BuildingProgressionLedger"/> already established in this
    /// package (5 lists → 5 grid slots → 1 Awards list on PostLoadInit).
    ///
    /// Vertical-Slice-Plan §Phase 8.1: domain-XP-only progression. No
    /// ResearchProjectDef fast-path here.
    /// </summary>
    public sealed class DomainXpState : IExposable
    {
        public const int CurrentSchemaVersion = 1;
        public int SchemaVersion = CurrentSchemaVersion;

        // Runtime state
        private readonly Dictionary<string, float> _xpByDomain
            = new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly HashSet<string> _completionKeys
            = new HashSet<string>(StringComparer.Ordinal);

        // Save projection
        private List<string> _xpKeysForSave;
        private List<float> _xpValuesForSave;
        private List<string> _keysForSave;

        public int TotalAwards => _completionKeys.Count;
        public int AwardCountByDomain(ProgressionDomain domain)
        {
            if (!ProgressionDomainUtility.IsValid(domain)) return 0;
            string prefix = BuildDomainActionPrefix(domain);
            int c = 0;
            foreach (var key in _completionKeys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal)) c++;
            }
            return c;
        }
        public float GetXp(ProgressionDomain domain)
            => _xpByDomain.TryGetValue(ProgressionDomainUtility.Key(domain), out var xp) ? xp : 0f;

        public int GetLevel(ProgressionDomain domain)
        {
            float xp = GetXp(domain);
            // Level n requires (n-1)^2 * 100 cumulative XP; e.g.
            //   Level 1 -> 0, Level 2 -> 100, Level 3 -> 400, Level 4 -> 900.
            // This keeps early-game progression tractable while still allowing
            // measurable separation between adjacent levels.
            if (xp <= 0f) return 1;
            int level = 1 + Mathf.FloorToInt(Mathf.Sqrt(xp / 100f));
            return level;
        }

        public bool HasCompletedAction(string actionKey)
            => !string.IsNullOrEmpty(actionKey) && _completionKeys.Contains(actionKey);

        /// <summary>
        /// Acceptance contract. Returns true and a populated result on first
        /// commit. Returns false and a Rejected result on replay.
        ///
        /// Diminishing-returns formula follows the Vertical-Slice-Plan §Phase 8.1:
        ///   factor = 1 / [ 1 + completedInDomain / (completedInDomain + 5) ]
        /// The curve is monotone decreasing and approaches 0.5 from above as
        /// the player keeps doing the same action — never reaching zero.
        ///
        /// Domain-mismatch detection: if the idempotency key was poisoned by
        /// an invalid-domain attempt we strip it again before returning.
        /// </summary>
        public bool TryAward(
            ProgressionDomain domain,
            float baseAmount,
            string idempotencyKey,
            string outputDefName,
            int outputCount,
            long completedTick,
            out ProgressionActionResult result)
        {
            result = default;
            if (string.IsNullOrEmpty(idempotencyKey) || baseAmount <= 0f)
            {
                result = ProgressionActionResult.Rejected(idempotencyKey);
                return false;
            }

            if (!_completionKeys.Add(idempotencyKey))
            {
                result = ProgressionActionResult.Rejected(idempotencyKey);
                return false;
            }

            if (!ProgressionDomainUtility.IsValid(domain))
            {
                _completionKeys.Remove(idempotencyKey);
                result = ProgressionActionResult.Rejected(idempotencyKey);
                return false;
            }

            int completedInDomain = AwardCountByDomain(domain) - 1; // we just added, subtract self
            // Plan formula verbatim: denominator = (1 + completedInDomain / (completedInDomain + 5)).
            float divisor = completedInDomain + 5f;
            float factor = (divisor) / (divisor + completedInDomain);
            // Bound the factor away from the edges; if completedInDomain is
            // 0 we get exactly 1.0, and as N grows factor → 0.5 (asymptote).
            if (factor > 1f) factor = 1f;
            if (factor < 0.5f) factor = 0.5f;
            float actualAmount = baseAmount * factor;

            string key = ProgressionDomainUtility.Key(domain);
            if (!_xpByDomain.ContainsKey(key)) _xpByDomain[key] = 0f;
            _xpByDomain[key] += actualAmount;

            result = new ProgressionActionResult
            {
                ActionKey = idempotencyKey,
                Domain = domain,
                BaseExperience = baseAmount,
                ActualExperience = actualAmount,
                OutputDefName = outputDefName ?? "",
                OutputCount = outputCount,
                CompletedTick = completedTick,
                WasAccepted = true,
            };
            return true;
        }

        public static string BuildDomainActionPrefix(ProgressionDomain domain)
        {
            return "domain:" + ProgressionDomainUtility.Key(domain) + ":";
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref SchemaVersion, "domainXpSchema", CurrentSchemaVersion);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _xpKeysForSave = new List<string>(_xpByDomain.Count);
                _xpValuesForSave = new List<float>(_xpByDomain.Count);
                foreach (var pair in _xpByDomain)
                {
                    _xpKeysForSave.Add(pair.Key ?? "");
                    _xpValuesForSave.Add(pair.Value);
                }

                _keysForSave = new List<string>(_completionKeys);
            }

            Scribe_Collections.Look(ref _xpKeysForSave, "domainXpKeys", LookMode.Value);
            Scribe_Collections.Look(ref _xpValuesForSave, "domainXpValues", LookMode.Value);
            Scribe_Collections.Look(ref _keysForSave, "domainXpCompletionKeys", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                bool preMigrationHadData = (_xpKeysForSave != null && _xpKeysForSave.Count > 0)
                    || (_keysForSave != null && _keysForSave.Count > 0);
                int incomingSchema = SchemaVersion;

                _xpByDomain.Clear();
                _completionKeys.Clear();

                int xpCount = Mathf.Min(_xpKeysForSave?.Count ?? 0, _xpValuesForSave?.Count ?? 0);
                for (int i = 0; i < xpCount; i++)
                {
                    string k = ValueAt(_xpKeysForSave, i, "");
                    if (string.IsNullOrEmpty(k)) continue;
                    _xpByDomain[k] = ValueAt(_xpValuesForSave, i, 0f);
                }

                int keyCount = _keysForSave?.Count ?? 0;
                for (int i = 0; i < keyCount; i++)
                {
                    string k = ValueAt(_keysForSave, i, "");
                    if (!string.IsNullOrEmpty(k)) _completionKeys.Add(k);
                }

                // Migration safety log: a save predating the Phase 8 hub has
                // no rimconemyDomainXp entries at all. We log a single warning
                // so the operator notices without flooding scrollback.
                if (incomingSchema < CurrentSchemaVersion && preMigrationHadData)
                {
                    Log.Warning(
                        "[Rimconemy.SurvivalProgression] DomainXpState migrated from schema "
                        + incomingSchema + "→" + CurrentSchemaVersion
                        + ". Cross-domain rewards preserved: " + _xpByDomain.Count
                        + " domain(s), " + _completionKeys.Count + " completion key(s).");
                }

                SchemaVersion = CurrentSchemaVersion;
                _xpKeysForSave = null;
                _xpValuesForSave = null;
                _keysForSave = null;
            }
        }

        private static T ValueAt<T>(List<T> values, int index, T fallback)
        {
            return values != null && index >= 0 && index < values.Count ? values[index] : fallback;
        }
    }
}
