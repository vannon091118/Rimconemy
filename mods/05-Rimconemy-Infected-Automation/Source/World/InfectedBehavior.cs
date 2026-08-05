using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 2 — Infected pawn behavioral states.
    ///
    /// Four states representing the infected pawn lifecycle:
    ///   Dormant → Roaming → Investigating → Assault
    ///
    /// The chunk thinks, not the pawn. Behavior transitions are
    /// driven by chunk-level data: LightExposure, NoiseLevel,
    /// Attraction, AlertState, and DarknessFactor.
    /// </summary>
    public enum InfectedBehaviorState
    {
        /// <summary>Asleep or idle. Pawn stays in place, no movement.</summary>
        Dormant = 0,

        /// <summary>Wandering randomly, biased toward high-attraction
        /// chunks. Active mainly at night.</summary>
        Roaming = 1,

        /// <summary>Moving toward a suspicious chunk or disturbance
        /// source. Triggered by noise, light flicker, or chunk alert.</summary>
        Investigating = 2,

        /// <summary>Actively attacking a colonist. Triggered by
        /// line-of-sight contact with a survivor.</summary>
        Assault = 3,
    }

    /// <summary>
    /// Stateless, deterministic transition logic for infected pawn
    /// behavior. All random decisions use <see cref="Story.DeterministicRng"/>
    /// with a per-pawn seed so the same input always produces the
    /// same behavior sequence.
    ///
    /// Rules (deterministic, seed-based):
    ///   DORMANT → ROAMING:  darkness &gt; 0.5 AND (attraction &gt; 0.3
    ///                        OR deterministic wake check passes)
    ///   ROAMING → INVESTIGATING: chunk alert &gt;= Suspicious OR
    ///                            noise above threshold
    ///   INVESTIGATING → ASSAULT: colonist spotted in sight range
    ///                             with line-of-sight
    ///   INVESTIGATING → ROAMING: timeout without new stimulus
    ///   ASSAULT → INVESTIGATING: target lost for too long
    ///   ASSAULT → ROAMING: all colonists gone from sight range
    /// </summary>
    public static class InfectedBehaviorTransition
    {
        /// <summary>Darkness threshold for Dormant→Roaming transition.</summary>
        public const float WakeDarknessThreshold = 0.5f;

        /// <summary>Chunk attraction threshold to wake a dormant infected.</summary>
        public const float WakeAttractionThreshold = 0.3f;

        /// <summary>Probability (0..1) of waking per evaluation cycle when
        /// darkness is high but attraction is below threshold.</summary>
        public const float WakeRandomChance = 0.08f;

        /// <summary>Noise threshold for Roaming→Investigating.</summary>
        public const float InvestigateNoiseThreshold = 0.2f;

        /// <summary>Sight radius multiplier for infected pawns.
        /// SightRadius = baseSight * this multiplier (fair: same as survivors).</summary>
        public const float InfectedBaseSight = 15f;

        /// <summary>Maximum ticks an infected will investigate before
        /// giving up and returning to Roaming.</summary>
        public const long InvestigationTimeoutTicks = 3000L; // 50s

        /// <summary>Ticks without target before Assault drops back
        /// to Investigating.</summary>
        public const long AssaultTargetLostTicks = 600L; // 10s

        /// <summary>Maximum ticks without ANY colonist in range before
        /// Assault drops to Roaming.</summary>
        public const long AssaultNoColonistTicks = 2000L; // ~33s

        /// <summary>
        /// Computes the next behavior state for an infected pawn based
        /// on its current state, the chunk data at its position, the
        /// global environment, and whether a colonist is visible.
        ///
        /// Pure function — no side effects, no mutable state.
        /// </summary>
        /// <param name="current">Current behavior state.</param>
        /// <param name="chunk">Chunk the pawn is in (or null).</param>
        /// <param name="env">Global environment snapshot.</param>
        /// <param name="colonistVisible">True if a colonist is visible.</param>
        /// <param name="ticksInState">How long the pawn has been in its current state.</param>
        /// <param name="rng">Deterministic RNG seeded per-pawn+tick.</param>
        /// <returns>The next behavior state.</returns>
        public static InfectedBehaviorState ComputeNext(
            InfectedBehaviorState current,
            ChunkState chunk,
            EnvironmentSnapshot env,
            bool colonistVisible,
            long ticksInState,
            ref Story.DeterministicRng rng)
        {
            float darkness = env?.DarknessFactor ?? 0f;
            float attraction = chunk?.Attraction ?? 0f;
            float noise = chunk?.NoiseLevel ?? 0f;
            ChunkAlertState alert = chunk?.AlertState ?? ChunkAlertState.Dormant;

            switch (current)
            {
                case InfectedBehaviorState.Dormant:
                    // Wake up at night when the chunk has high attraction
                    // or on a deterministic random chance.
                    if (darkness >= WakeDarknessThreshold)
                    {
                        if (attraction >= WakeAttractionThreshold)
                            return InfectedBehaviorState.Roaming;
                        if (rng.NextFloat() < WakeRandomChance)
                            return InfectedBehaviorState.Roaming;
                    }
                    return InfectedBehaviorState.Dormant;

                case InfectedBehaviorState.Roaming:
                    // Escalate to Investigating when the chunk is alert
                    // or noise exceeds the threshold.
                    if (alert >= ChunkAlertState.Suspicious)
                        return InfectedBehaviorState.Investigating;
                    if (noise >= InvestigateNoiseThreshold)
                        return InfectedBehaviorState.Investigating;
                    // Drop back to Dormant at full daylight.
                    if (darkness < 0.1f && attraction < 0.1f)
                        return InfectedBehaviorState.Dormant;
                    return InfectedBehaviorState.Roaming;

                case InfectedBehaviorState.Investigating:
                    // Colonist spotted → Assault.
                    if (colonistVisible)
                        return InfectedBehaviorState.Assault;
                    // Timeout → drop back to Roaming.
                    if (ticksInState >= InvestigationTimeoutTicks)
                        return InfectedBehaviorState.Roaming;
                    // Alert decayed → drop back to Roaming.
                    if (alert == ChunkAlertState.Dormant && noise < InvestigateNoiseThreshold)
                        return InfectedBehaviorState.Roaming;
                    return InfectedBehaviorState.Investigating;

                case InfectedBehaviorState.Assault:
                    // All colonists gone for a long time → back to Roaming.
                    // MUST come first: NoColonistTicks (2000) > TargetLostTicks (600),
                    // so >=2000 matches BOTH. Check the stronger condition first.
                    if (!colonistVisible && ticksInState >= AssaultNoColonistTicks)
                        return InfectedBehaviorState.Roaming;
                    // Target lost briefly → back to Investigating.
                    if (!colonistVisible && ticksInState >= AssaultTargetLostTicks)
                        return InfectedBehaviorState.Investigating;
                    return InfectedBehaviorState.Assault;

                default:
                    return InfectedBehaviorState.Dormant;
            }
        }

        /// <summary>
        /// Computes the effective sight radius for an infected pawn
        /// given the current environment.
        /// </summary>
        public static float ComputeInfectedSightRadius(EnvironmentSnapshot env)
        {
            float daylight = env?.DaylightFactor ?? 0.5f;
            float weather = env?.WeatherFactor ?? 0.8f;
            return PerceptionMath.ComputeSightRadius(InfectedBaseSight, daylight, weather);
        }

        /// <summary>
        /// Checks whether any colonist pawn is visible from the
        /// infected pawn's position within the given sight radius.
        /// Uses vanilla <see cref="GenSight.LineOfSight"/> for
        /// fair, blockable visibility.
        ///
        /// Deterministic: stable sort of candidates by thingIDNumber.
        /// </summary>
        /// <returns>The visible colonist pawn, or null.</returns>
        public static Pawn FindVisibleColonist(Pawn infected, Map map, float sightRadius)
        {
            if (infected == null || map == null || sightRadius <= 0f) return null;

            // Collect colonist candidates within sight radius.
            var candidates = new List<Pawn>();
            if (map.mapPawns?.FreeColonists != null)
            {
                foreach (var colonist in map.mapPawns.FreeColonists)
                {
                    if (colonist == null || colonist.Dead || colonist.Downed) continue;
                    float dist = infected.Position.DistanceTo(colonist.Position);
                    if (dist > sightRadius) continue;
                    candidates.Add(colonist);
                }
            }

            if (candidates.Count == 0) return null;

            // Stable sort by thingIDNumber for determinism.
            candidates.Sort((a, b) => a.thingIDNumber.CompareTo(b.thingIDNumber));

            // Find the first colonist with line-of-sight.
            foreach (var colonist in candidates)
            {
                if (GenSight.LineOfSight(infected.Position, colonist.Position, map, skipFirstCell: false))
                    return colonist;
            }

            return null;
        }
    }
}
