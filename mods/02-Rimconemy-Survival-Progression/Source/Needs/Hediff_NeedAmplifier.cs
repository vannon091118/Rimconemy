using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Needs
{
    /// <summary>
    /// Phase-2.7 (2026-08-04): Begleit-Hediff-Klasse, die den
    /// Vanilla-Food-Sample in die aktuelle Hunger-Rate-Stage übersetzt.
    ///
    /// Owner: Survival & Progression (Paket 02).
    ///
    /// Hook-Mechanik OHNE Harmony-Transpiler: RimWorld 1.6 berechnet den
    /// Hungerfaktor aus <see cref="HediffStage.hungerRateFactor"/>. Diese
    /// Hediff hält ihre Severity auf dem aktuellen Vanilla-Food-Sample; die
    /// XML-Stages wählen dadurch 0.7, 1.0 oder 1.4 als Faktor.
    ///
    /// Vanilla-Healthy-Verification: Die Klasse mutiert nur ihre eigene
    /// Severity. Die eigentliche Multiplikation bleibt im nativen
    /// HediffSet.GetHungerRateFactor()-Pfad.
    ///
    /// Q14-Contract wird eingehalten: Die Setting-Identity-NeedDefs sind
    /// nach wie vor nicht an Pawns angehängt; das hier ist ein eigenständiges
    /// Hediff-Def, kein Need.
    /// </summary>
    public sealed class Hediff_NeedAmplifier : Hediff
    {
        public static float SeverityForPawn(Pawn pawn)
        {
            if (pawn == null) return NeedAmplifier.NeutralSample;
            return SanitizeSeverity(NeedAmplifier.SeverityOffsetForSetting(
                pawn, NeedMappingService.FoodSetting));
        }

        /// <summary>
        /// Severity-Wert wird auf einen gültigen Vanilla-Range-Clamp
        /// gehalten. Owner: Paket 02.
        /// </summary>
        public static float SanitizeSeverity(float severity)
        {
            if (float.IsNaN(severity) || float.IsInfinity(severity)) return NeedAmplifier.NeutralSample;
            return Mathf.Clamp(severity, NeedAmplifier.MinSeverityEpsilon, 1.0f);
        }
    }
}
