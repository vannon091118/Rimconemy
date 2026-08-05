namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 1 / 3 — Perception Math.
    ///
    /// Stateless computation of attraction and sight formulas,
    /// extracted here so ChunkGridComponent and the infected pawn
    /// adapter (Sprint 2) share the same math without duplication.
    ///
    /// Sprint 3 extends this with per-source-type weights and
    /// transient signal decay curves.
    /// </summary>
    public static class PerceptionMath
    {
        /// <summary>
        /// Computes a chunk's attraction score from its light and
        /// noise values, weighted by darkness.
        ///
        /// At night (high darkness), light and noise become more
        /// magnetic — a generator in the dark pulls infected harder
        /// than the same generator at noon.
        ///
        ///   lightWeight = 1 + darkness × 1.25
        ///   noiseWeight = 1 + darkness × 0.75
        ///   attraction  = light × lightWeight + noise × noiseWeight
        /// </summary>
        public static float ComputeAttraction(float light, float noise, float darknessFactor)
        {
            float lightWeight = 1f + darknessFactor * 1.25f;
            float noiseWeight = 1f + darknessFactor * 0.75f;
            return (light * lightWeight) + (noise * noiseWeight);
        }

        /// <summary>
        /// Computes a pawn's sight radius from its base sight,
        /// daylight, and weather.
        ///
        /// Sight is fair: same formula for survivors and infected.
        /// No one sees better than the player's own pawns. Darkness
        /// and weather reduce sight equally.
        ///
        ///   modifier = 0.5 + (daylight × 0.5)
        ///   modifier = modifier × (1 − weather × 0.35)
        ///   radius   = baseSight × modifier
        /// </summary>
        public static float ComputeSightRadius(float baseSight, float daylightFactor, float weatherFactor)
        {
            float modifier = 0.5f + (daylightFactor * 0.5f);
            modifier *= 1f - (weatherFactor * 0.35f);
            return baseSight * modifier;
        }
    }
}
