using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Needs
{
    /// <summary>
    /// Track 2-C / S-T1 — Setting Need Mapping Service.
    ///
    /// Mapping definition that links our documented <c>Rimconemy_Need_*</c>
    /// Setting identities to one or several Vanilla <see cref="NeedDef"/>
    /// entries plus an <see cref="Aggregator"/>. Sampling projects each
    /// Vanilla source onto a 0..1 percentage that mirrors the RimWorld
    /// <c>CurLevelPercentage</c> scale.
    ///
    /// Owner: Survival and Progression. The Vanilla need definitions stay
    /// untouched; the mapping only adds a read-side lens. Honors Spike
    /// API-NEED-01: we never override Vanilla Mood, never attach our
    /// Setting needdefs to pawns.
    /// </summary>
    public sealed class NeedMapping
    {
        /// <summary>Setting-defName (e.g. "Rimconemy_Need_Food").</summary>
        public string SettingDefName { get; }

        /// <summary>Resolved Setting <see cref="NeedDef"/>, set on <see cref="Resolve"/>.</summary>
        public NeedDef SettingDef { get; private set; }

        /// <summary>Vanilla <see cref="NeedDef"/> sources we aggregate.</summary>
        public IReadOnlyList<NeedDef> Sources { get; }

        /// <summary>How to fold multiple source percentages into one.</summary>
        public Aggregator Aggregator { get; }

        /// <summary>
        /// Optional override for the Safety aggregator. We compute a weighted
        /// combination of vanilla Health and Rest instead of a single
        /// <see cref="NeedDef"/>.
        /// </summary>
        public bool IsCompositeSafety { get; }

        /// <summary>Weight for Health in the Safety composite (default 0.65).</summary>
        public float SafetyHealthWeight { get; }

        /// <summary>Weight for Rest in the Safety composite (default 0.35).</summary>
        public float SafetyRestWeight { get; }

        public NeedMapping(
            string settingDefName,
            IReadOnlyList<NeedDef> sources,
            Aggregator aggregator,
            bool isCompositeSafety = false,
            float safetyHealthWeight = 0.65f,
            float safetyRestWeight = 0.35f)
        {
            SettingDefName = settingDefName;
            Sources = sources ?? new List<NeedDef>();
            Aggregator = aggregator;
            IsCompositeSafety = isCompositeSafety;
            SafetyHealthWeight = safetyHealthWeight;
            SafetyRestWeight = safetyRestWeight;
        }

        /// <summary>
        /// Resolves the <c>SettingDef</c> against <see cref="DefDatabase{NeedDef}"/>.
        /// Returns false gracefully when the def is missing - callers should
        /// fall back to a sane middle value (0.5).
        /// </summary>
        public bool Resolve()
        {
            if (SettingDef != null)
                return true;
            SettingDef = DefDatabase<NeedDef>.GetNamedSilentFail(SettingDefName);
            return SettingDef != null;
        }

        /// <summary>
        /// Sample aggregate percentage for a single pawn. Returns a clamped
        /// 0..1 value. Returns 0.5 as a neutral fallback when no sources
        /// resolve or the pawn is missing trackers.
        /// </summary>
        public float SampleAggregate(Pawn pawn)
        {
            if (pawn == null || pawn.needs == null)
                return 0.5f;

            if (IsCompositeSafety)
                return SampleCompositeSafety(pawn);

            if (Sources.Count == 0)
                return 0.5f;

            var samples = new List<float>(Sources.Count);
            foreach (var src in Sources)
            {
                if (src == null)
                    continue;

                var need = pawn.needs.TryGetNeed(src);
                if (need == null)
                    continue;

                float pct = need.CurLevelPercentage;
                samples.Add(Mathf.Clamp01(pct));
            }

            if (samples.Count == 0)
                return 0.5f;

            switch (Aggregator)
            {
                case Aggregator.Minimum:
                    float min = samples[0];
                    for (int i = 1; i < samples.Count; i++)
                        if (samples[i] < min) min = samples[i];
                    return min;
                case Aggregator.Maximum:
                    float max = samples[0];
                    for (int i = 1; i < samples.Count; i++)
                        if (samples[i] > max) max = samples[i];
                    return max;
                case Aggregator.Average:
                default:
                    float sum = 0f;
                    for (int i = 0; i < samples.Count; i++)
                        sum += samples[i];
                    return sum / samples.Count;
            }
        }

        private float SampleCompositeSafety(Pawn pawn)
        {
            float health = pawn.health?.summaryHealth?.SummaryHealthPercent ?? 0.5f;
            float rest = 0.5f;
            foreach (var src in Sources)
            {
                if (src == null) continue;
                var n = pawn.needs.TryGetNeed(src);
                if (n != null)
                {
                    rest = Mathf.Clamp01(n.CurLevelPercentage);
                    break;
                }
            }
            return Mathf.Clamp01(health * SafetyHealthWeight + rest * SafetyRestWeight);
        }
    }

    /// <summary>How multiple Vanilla source percentages fold into one Setting map.</summary>
    public enum Aggregator
    {
        Average = 0,
        Minimum = 1,
        Maximum = 2,
    }

    /// <summary>
    /// Static mapping catalog. Three Setting identities, all resolved at
    /// <c>PostLoadInit</c> time via <see cref="ResolveAll"/>.
    ///
    /// IMPORTANT: we resolve the Vanilla Quest NeedDef by defName rather
    /// than <c>NeedDefOf.Joy</c> because RimWorld 1.6 renamed the field
    /// across Core and DLC builds. Forces a stable lookup path.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class NeedMappingService
    {
        public const string FoodSetting = "Rimconemy_Need_Food";
        public const string SafetySetting = "Rimconemy_Need_Safety";
        public const string SocialSetting = "Rimconemy_Need_Social";

        public static readonly IReadOnlyList<NeedMapping> All;

        private static readonly NeedMapping Food = new NeedMapping(
            FoodSetting,
            sources: new List<NeedDef> { NeedDefOf.Food },
            aggregator: Aggregator.Average);

        private static readonly NeedMapping Safety = new NeedMapping(
            SafetySetting,
            sources: new List<NeedDef> { NeedDefOf.Rest },
            aggregator: Aggregator.Average,
            isCompositeSafety: true,
            safetyHealthWeight: 0.65f,
            safetyRestWeight: 0.35f);

        private static readonly List<NeedDef> SocialSources = new List<NeedDef>();
        private static readonly NeedMapping Social = new NeedMapping(
            SocialSetting,
            sources: SocialSources,
            aggregator: Aggregator.Maximum);

        static NeedMappingService()
        {
            // Resolve the recreation source slot from DefDatabase once.
            // Prefer the 1.6 recreation def-name, fall back to Joy for legacy
            // Core-only builds. Silent-fail on purpose: if neither resolves
            // the sampler returns the neutral 0.5 default.
            NeedDef recreation = DefDatabase<NeedDef>.GetNamedSilentFail("Recreation")
                ?? DefDatabase<NeedDef>.GetNamedSilentFail("Joy");
            if (recreation != null)
                SocialSources.Add(recreation);

            All = new List<NeedMapping> { Food, Safety, Social };

            ResolveAll();

            Log.Message(
                "[Rimconemy.SurvivalProgression] NeedMappingService registered: " +
                $"food={Food.SettingDef?.defName ?? "(missing)"}, " +
                $"safety={Safety.SettingDef?.defName ?? "(missing)"}, " +
                $"social={Social.SettingDef?.defName ?? "(missing)"} (source={recreation?.defName ?? "(missing)"}).");
        }

        public static void ResolveAll()
        {
            foreach (var m in All)
                m.Resolve();
        }

        /// <summary>
        /// Convenience: sample a Setting identity by <paramref name="settingDefName"/>.
        /// Returns a clamped 0..1 percentage. Returns 0.5 if the mapping or its
        /// sources are not available, so callers can use the result directly.
        /// </summary>
        public static float SampleByName(Pawn pawn, string settingDefName)
        {
            foreach (var m in All)
            {
                if (m.SettingDefName == settingDefName)
                    return m.SampleAggregate(pawn);
            }
            return 0.5f;
        }

        public static NeedMapping Get(string settingDefName)
        {
            foreach (var m in All)
                if (m.SettingDefName == settingDefName)
                    return m;
            return null;
        }
    }
}
