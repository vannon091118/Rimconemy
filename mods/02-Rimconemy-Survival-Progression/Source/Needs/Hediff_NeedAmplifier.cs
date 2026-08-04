using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Needs
{
    /// <summary>
    /// Phase-2.7 (2026-08-04): Begleit-Hediff-Klasse, die den
    /// Vanilla-Hunger-Rate-Faktor auf Basis des Sample-Amplifiers überschreibt.
    ///
    /// Owner: Survival &amp; Progression (Paket 02).
    ///
    /// Hook-Mechanik OHNE Harmony-Transpiler: Vanilla bietet eine virtuelle
    /// Property <see cref="Hediff.HungerRateFactor"/>, die pro Hediff mit
    /// eigenem Hunger-Rate-Multiplikator überschrieben werden kann. Unsere
    /// Klasse nutzt dieses Pattern — kein Patch auf Need_Food.Tick.
    ///
    /// Vanilla-Healthy-Verification: Die Klasse ruft nur die standardisierte
    /// Property-Override-API auf. Vanilla iteriert über alle Hediffs und
    /// multipliziert deren HungerRateFactor-Werte. Wenn unser Override den
    /// Wert 1.0 zurückliefert (neutral anchor), ist unser Hook ein no-op;
    /// wenn 0.7 oder 1.4, ist es ein ATA-Verhalten für den entsprechenden
    /// Hunger-Zustand des Pawns.
    ///
    /// Q14-Contract wird eingehalten: Die Setting-Identity-NeedDefs sind
    /// nach wie vor nicht an Pawns angehängt; das hier ist ein eigenständiges
    /// Hediff-Def, kein Need.
    /// </summary>
    public sealed class Hediff_NeedAmplifier : HediffWithComps
    {
        /// <summary>
        /// Override der Vanilla-HungerRateFactor-Virtual. Liest das
        /// Vanilla-<see cref="Need_Food"/> vom Pawn und liefert den
        /// Sample-Amplifier-Faktor zur&uuml;ck.
        ///
        /// Orphan-Loophole: ist kein Need_Food vorhanden (z.B. Maschine,
        /// nicht-menschlicher Pawn), wird 1.0 zurückgegeben — neutral.
        /// </summary>
        public override float HungerRateFactor
        {
            get
            {
                if (pawn == null) return 1.0f;

                Need_Food food = pawn.needs?.TryGetNeed<Need_Food>();
                if (food == null) return 1.0f;

                try
                {
                    return NeedAmplifier.HungerRateFor(food);
                }
                catch
                {
                    // Defensive: vanilla-Ausreißer oder ein unerwarteter
                    // Need-Zustand sollen unsere Hook nicht crashen.
                    return 1.0f;
                }
            }
        }

        /// <summary>
        /// Vanilla ruft <see cref="Hediff.TendencySeverityChangePerDay"/>
        /// in einigen Health-Pfaden auf; unsere Hediff-Klasse soll den
        /// Severity-Wert statisch halten, weil sie nicht wirklich krank ist.
        /// </summary>
        public override float TendencySeverityChangePerDay
        {
            get { return 0f; }
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
