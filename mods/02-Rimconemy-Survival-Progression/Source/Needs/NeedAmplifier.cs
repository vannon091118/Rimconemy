using Rimconemy.Foundation.Canonical;
using UnityEngine;

namespace Rimconemy.SurvivalProgression.Needs
{
    /// <summary>
    /// Phase-2.7 (2026-08-04): Sample-Amplifier für Vanilla-Hunger-Debuff-Rate.
    ///
    /// Owner: Survival &amp; Progression (Paket 02).
    ///
    /// Hook-Mechanik: Diese Klasse ist eine pure Rechenlogik ohne
    /// Harmony-Transpiler. Die tatsächliche Hunger-Rate wird nicht
    /// direkt überschrieben; stattdessen wird ein Multiplikator
    /// bereitgestellt, der über das Begleit-Hediff
    /// <c>Rimconemy_NeedAmplifier</c> am Pawn getragen wird. Das Hediff
    /// überschreibt die Vanilla-virtuelle Property
    /// <c>Hediff.HungerRateFactor</c>; Vanilla iteriert über alle Hediffs
    /// und multipliziert deren Werte — kein Patch auf Need_Food.Tick.
    ///
    /// Sample-Amplifier-Funktion: ein Van­illa-Need-Sample in [0,1]
    /// wird auf einen Multiplikator in [0.7, 1.4] abgebildet:
    ///   sample = 0.0 → multiplier = 1.4 (hungren verstärkt)
    ///   sample = 0.5 → multiplier = 1.0 (neutraler Anker)
    ///   sample = 1.0 → multiplier = 0.7 (satiated suppressed)
    /// Piecewise-linear mit neutralem Center bei 0.5.
    ///
    /// Severity-Mirror: der Multiplier wird auf eine strikt positive
    /// Hediff-Severity abgebildet, weil Vanilla Hediffs automatisch despawnt,
    /// sobald Severity &lt;= 0 erreicht. Mapping:
    ///   sample = 0.0   → Severity = 1.0    (hungry: amplification)
    ///   sample = 0.5   → Severity = 0.5    (neutraler Anker)
    ///   sample = 1.0   → Severity = 0.05   (satiated: suppression, &gt; Epsilon)
    /// Das Hediff überlebt damit die gesamte Sample-Range ohne Auto-Remove.
    /// </summary>
    public static class NeedAmplifier
    {
        /// <summary>
        /// Untere Grenze des Multiplikators wenn Sample = 1.0 (satiated).
        /// Vanilla-Hunger-Tick läuft dann mit dieser Rate.
        /// </summary>
        public const float MinMultiplier = 0.7f;

        /// <summary>
        /// Obere Grenze des Multiplikators wenn Sample = 0.0 (hungren).
        /// Vanilla-Hunger-Tick läuft dann mit dieser Rate.
        /// </summary>
        public const float MaxMultiplier = 1.4f;

        /// <summary>
        /// Sample-Wert, an dem der Multiplikator genau 1.0 ergibt (neutraler
        /// Anker). Definiert als 0.5 für symmetrische Skala.
        /// </summary>
        public const float NeutralSample = 0.5f;

        /// <summary>
        /// Minimum-Severity-Epsilon für das Hediff-Lifecycle. Vanilla
        /// despawnt Hediffs automatisch wenn Severity &lt;= 0; wir halten
        /// Severity immer ≥ epsilon damit der Hook-Mechanismus erhalten bleibt.
        /// </summary>
        public const float MinSeverityEpsilon = 0.05f;

        public const float MinToNeutralSpan = MinMultiplier - 1.0f; // -0.3
        public const float MaxToNeutralSpan = MaxMultiplier - 1.0f; // +0.4

        /// <summary>
        /// Deterministischer Sample-Amplifier. Pure Funktion, ohne
        /// State, ohne Scribe-Coupling. Safe bei NaN, Infinity und Out-of-Range.
        /// </summary>
        /// <param name="sample">Need-Sample aus NeedMappingService.SampleByName,
        /// wird auf [0,1] geclampt.</param>
        /// <returns>Multiplikator in [0.7, 1.4].</returns>
        public static float AmplifierFactor(float sample)
        {
            if (float.IsNaN(sample) || float.IsInfinity(sample)) return 1.0f;
            float clamped = Mathf.Clamp01(sample);
            if (clampled <= NeutralSample)
            {
                // sample in [0, 0.5] → multiplier in [1.0, 1.4]
                float t = clamped / NeutralSample; // 0..1
                return 1.0f + (MaxToNeutralSpan * t); // 1.0 → 1.4
            }
            else
            {
                // sample in [0.5, 1.0] → multiplier in [0.7, 1.0]
                float t = (clampled - NeutralSample) / (1.0f - NeutralSample); // 0..1
                return 1.0f + (MinToNeutralSpan * t); // 1.0 → 0.7
            }
        }

        /// <summary>
        /// Strikt-positive Severity-Map für das Begleit-Hediff. Severity
        /// ist immer in [MinSeverityEpsilon, 1.0]. Niedriger Sample
        /// (hungren) → hohe Severity; hoher Sample (satiated) → Severity
        /// nahe am Epsilon (aber > 0, keine Auto-Despawn).
        /// </summary>
        public static float SeverityOffset(float sample)
        {
            if (float.IsNaN(sample) || float.IsInfinity(sample)) return NeutralSample;
            float clamped = Mathf.Clamp01(sample);
            float severity = Mathf.Clamp(1.0f - clamped, MinSeverityEpsilon, 1.0f);
            return severity;
        }

        /// <summary>
        /// Convenience: liefert Severity für eine konkrete Setting-Need-Auswahl
        /// an einem konkreten Pawn. Wenn Setting fehlt oder Pawn null:
        /// Severity = NeutralSample (0.5, neutral).
        /// </summary>
        public static float SeverityOffsetForSetting(Verse.Pawn pawn, string settingDefName)
        {
            if (pawn == null || string.IsNullOrEmpty(settingDefName)) return NeutralSample;
            float sample = NeedMappingService.SampleByName(pawn, settingDefName);
            return SeverityOffset(sample);
        }

        /// <summary>
        /// Liefert den Hunger-Rate-Faktor für einen konkreten
        /// <see cref="RimWorld.Need_Food"/>. Liest <c>CurLevelPercentage</c>,
        /// clamped defensiv, und gibt den deterministischen Multiplikator
        /// zurück. Wird von <c>Hediff_NeedAmplifier.HungerRateFactor</c>
        /// aufgerufen. Liefert 1.0 für null Need oder vanilla-Ausreißer.
        /// </summary>
        public static float HungerRateFor(RimWorld.Need_Food need)
        {
            if (need == null) return 1.0f;
            try
            {
                float pct = need.CurLevelPercentage;
                if (float.IsNaN(pct) || float.IsInfinity(pct)) return 1.0f;
                return AmplifierFactor(pct);
            }
            catch
            {
                return 1.0f;
            }
        }

        /// <summary>
        /// Convenience-Wrapper über Foundation-Canonical-Settings,
        /// kein expliziter NeedMappingService-Lookup.
        /// </summary>
        public static float AmplifierForSettingIdentity(Verse.Pawn pawn, SettingIdentity id)
        {
            if (pawn == null) return 1.0f;
            string settingDefName = id == SettingIdentity.Food
                ? NeedMappingService.FoodSetting
                : id == SettingIdentity.Safety
                    ? NeedMappingService.SafetySetting
                    : id == SettingIdentity.Social
                        ? NeedMappingService.SocialSetting
                        : null;
            if (string.IsNullOrEmpty(settingDefName)) return 1.0f;
            float sample = NeedMappingService.SampleByName(pawn, settingDefName);
            return AmplifierFactor(sample);
        }
    }
}
