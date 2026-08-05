using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 1 — Global Environment Snapshot.
    ///
    /// One per map, recomputed each refresh cycle. Holds the
    /// time-of-day, weather, blackout, and global threat factors
    /// that affect ALL chunks uniformly.
    ///
    /// This is the "brain in the ceiling" — every infected pawn
    /// shares the same daylight curve, darkness factor, and
    /// weather attenuation without computing it independently.
    /// </summary>
    public sealed class EnvironmentSnapshot
    {
        /// <summary>Game tick when this snapshot was produced.</summary>
        public long Tick;

        /// <summary>Daylight intensity [0, 1]. 0 = midnight, 1 = noon.</summary>
        public float DaylightFactor;

        /// <summary>Darkness complement: 1 - DaylightFactor after weather.
        /// Used as a multiplier for light/noise attraction at night.</summary>
        public float DarknessFactor;

        /// <summary>Weather attenuation of outdoor light/sound [0, 1].
        /// 1 = clear, 0.4 = thick fog, 0.7 = rain.</summary>
        public float WeatherFactor;

        /// <summary>Global alert level derived from the highest-chunk
        /// AlertState. Drives story escalation and raid pressure.</summary>
        public float GlobalAlert;

        /// <summary>True when the power grid is offline, making all
        /// artificial light sources dark and the colony blind.</summary>
        public bool IsBlackout;

        /// <summary>Number of noise sources currently active on the map
        /// (generators, fueled devices, fires). Diagnostic counter.</summary>
        public int ActiveLoudSources;

        /// <summary>Number of light sources (glowers, fires) currently
        /// emitting. Diagnostic counter.</summary>
        public int ActiveLightSources;

        public override string ToString()
        {
            return $"Env(day={DaylightFactor:F2} dark={DarknessFactor:F2} weather={WeatherFactor:F2} alert={GlobalAlert:F2} blackout={IsBlackout} loud={ActiveLoudSources} lit={ActiveLightSources})";
        }
    }
}
