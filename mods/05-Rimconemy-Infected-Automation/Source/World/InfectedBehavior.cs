using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected & Automation (Package 05).
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
        /// chunks. Active mainly at night, but can be triggered by
        /// high noise/alert during day.</summary>
        Roaming = 1,

        /// <summary>Moving toward a suspicious chunk or disturbance
        /// source. Triggered by noise, light flicker, or chunk alert.</summary>
        Investigating = 2,

        /// <summary>Actively attacking a pawn. Triggered by
        /// line-of-sight contact with any pawn (colonist or animal).</summary>
        Assault = 3,
    }

    /// <summary>
    /// Stateless, deterministic transition logic for infected pawn
    /// behavior. All random decisions use <see cref="Story.DeterministicRng"/>
    /// with a per-pawn seed so the same input always produces the
    /// same behavior sequence.
    ///
    /// Rules (deterministic, seed-based):
    ///   DORMANT → ROAMING:  darkness > 0.5 AND (attraction > 0.3
    ///                        OR deterministic wake check passes)
    ///                        OR (daytime AND noise > 0.4 AND alert >= Suspicious)
    ///   ROAMING → INVESTIGATING: chunk alert >= Suspicious OR
    ///                            noise above threshold
    ///   INVESTIGATING → ASSAULT: any pawn spotted in sight range
    ///                             with line-of-sight
    ///   INVESTIGATING → ROAMING: timeout without new stimulus
    ///   ASSAULT → INVESTIGATING: target lost for too long
    ///   ASSAULT → ROAMING: all pawns gone from sight range
    ///   ROAMING → DORMANT: full daylight AND low attraction AND low noise
    /// </summary>
    public static class InfectedBehaviorTransition
    {
        /// <summary>Darkness threshold for Dormant→Roaming transition (night).</summary>
        public const float WakeDarknessThreshold = 0.5f;

        /// <summary>Daytime darkness threshold below which infected stay dormant unless provoked.</summary>
        public const float DaytimeDormantThreshold = 0.2f;

        /// <summary>Noise threshold for daytime Dormant→Roaming (provoked wake).</summary>
        public const float DaytimeWakeNoiseThreshold = 0.4f;

        /// <summary>Chunk attraction threshold to wake a dormant infected at night.</summary>
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

        /// <summary>Maximum ticks without ANY pawn in range before
        /// Assault drops to Roaming.</summary>
        public const long AssaultNoPawnTicks = 2000L; // ~33s

        /// <summary>
        /// Computes the next behavior state for an infected pawn based
        /// on its current state, the chunk data at its position, the
        /// global environment, and whether a pawn is visible.
        ///
        /// Pure function — no side effects, no mutable state.
        /// </summary>
        /// <param name="current">Current behavior state.</param>
        /// <param name="chunk">Chunk the pawn is in (or null).</param>
        /// <param name="env">Global environment snapshot.</param>
        /// <param name="pawnVisible">True if ANY pawn (colonist or animal) is visible.</param>
        /// <param name="ticksInState">How long the pawn has been in its current state.</param>
        /// <param name="rng">Deterministic RNG seeded per-pawn+tick.</param>
        /// <returns>The next behavior state.</returns>
        public static InfectedBehaviorState ComputeNext(
            InfectedBehaviorState current,
            ChunkState chunk,
            EnvironmentSnapshot env,
            bool pawnVisible,
            long ticksInState,
            ref Story.DeterministicRng rng)
        {
            float darkness = env?.DarknessFactor ?? 0f;
            float attraction = chunk?.Attraction ?? 0f;
            float noise = chunk?.NoiseLevel ?? 0f;
            ChunkAlertState alert = chunk?.AlertState ?? ChunkAlertState.Dormant;
            bool isDaytime = darkness < DaytimeDormantThreshold;

            switch (current)
            {
                case InfectedBehaviorState.Dormant:
                    // Night wake: high darkness + attraction or random chance
                    if (darkness >= WakeDarknessThreshold)
                    {
                        if (attraction >= WakeAttractionThreshold)
                            return InfectedBehaviorState.Roaming;
                        if (rng.NextFloat() < WakeRandomChance)
                            return InfectedBehaviorState.Roaming;
                    }
                    // Daytime provoked wake: high noise + alert
                    if (isDaytime && noise >= DaytimeWakeNoiseThreshold && alert >= ChunkAlertState.Suspicious)
                    {
                        return InfectedBehaviorState.Roaming;
                    }
                    return InfectedBehaviorState.Dormant;

                case InfectedBehaviorState.Roaming:
                    // Escalate to Investigating when the chunk is alert
                    // or noise exceeds the threshold (works day and night)
                    if (alert >= ChunkAlertState.Suspicious)
                        return InfectedBehaviorState.Investigating;
                    if (noise >= InvestigateNoiseThreshold)
                        return InfectedBehaviorState.Investigating;
                    // Drop back to Dormant at full daylight with low activity
                    if (isDaytime && attraction < 0.1f && noise < 0.1f)
                        return InfectedBehaviorState.Dormant;
                    // Also drop to Dormant at night if conditions are very calm
                    if (!isDaytime && darkness < 0.1f && attraction < 0.1f && noise < 0.1f)
                        return InfectedBehaviorState.Dormant;
                    return InfectedBehaviorState.Roaming;

                case InfectedBehaviorState.Investigating:
                    // Any pawn spotted → Assault (not just colonists!)
                    if (pawnVisible)
                        return InfectedBehaviorState.Assault;
                    // Timeout → drop back to Roaming.
                    if (ticksInState >= InvestigationTimeoutTicks)
                        return InfectedBehaviorState.Roaming;
                    // Alert decayed → drop back to Roaming.
                    if (alert == ChunkAlertState.Dormant && noise < InvestigateNoiseThreshold)
                        return InfectedBehaviorState.Roaming;
                    return InfectedBehaviorState.Investigating;

                case InfectedBehaviorState.Assault:
                    // All pawns gone for a long time → back to Roaming.
                    // MUST come first: NoPawnTicks (2000) > TargetLostTicks (600),
                    // so >=2000 matches BOTH. Check the stronger condition first.
                    if (!pawnVisible && ticksInState >= AssaultNoPawnTicks)
                        return InfectedBehaviorState.Roaming;
                    // Target lost briefly → back to Investigating.
                    if (!pawnVisible && ticksInState >= AssaultTargetLostTicks)
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
        /// Checks whether any pawn (colonist, animal, or other) is visible from the
        /// infected pawn's position within the given sight radius.
        /// Uses vanilla <see cref="GenSight.LineOfSight"/> for
        /// fair, blockable visibility.
        ///
        /// Deterministic: stable sort of candidates by thingIDNumber.
        /// </summary>
        /// <returns>The visible pawn, or null.</returns>
        public static Pawn FindVisiblePawn(Pawn infected, Map map, float sightRadius)
        {
            if (infected == null || map == null || sightRadius <= 0f) return null;

            // Collect ALL pawn candidates within sight radius (not just colonists).
            // This includes: FreeColonists, ColonyAnimals, factionless wild
            // animals (via AllPawnsSpawned + Faction == null), Prisoners, etc.
            var candidates = new List<Pawn>();
            
            // Add free colonists
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

            // Add colony animals (tamed)
            if (map.mapPawns?.ColonyAnimals != null)
            {
                foreach (var animal in map.mapPawns.ColonyAnimals)
                {
                    if (animal == null || animal.Dead || animal.Downed) continue;
                    float dist = infected.Position.DistanceTo(animal.Position);
                    if (dist > sightRadius) continue;
                    candidates.Add(animal);
                }
            }

            // Add wild animals (non-colony). RimWorld 1.6 MapPawns has no
            // WildAnimals property; wild animals live in AllPawnsSpawned and
            // are identified by Faction == null.
            var allPawns = map.mapPawns?.AllPawnsSpawned;
            if (allPawns != null)
            {
                foreach (var pawn in allPawns)
                {
                    if (pawn == null || pawn.Dead || pawn.Downed) continue;
                    if (pawn.RaceProps == null || !pawn.RaceProps.Animal) continue;
                    if (pawn.Faction != null) continue; // tamed/colony animals covered above
                    float dist = infected.Position.DistanceTo(pawn.Position);
                    if (dist > sightRadius) continue;
                    candidates.Add(pawn);
                }
            }

            // Add prisoners
            if (map.mapPawns?.PrisonersOfColony != null)
            {
                foreach (var prisoner in map.mapPawns.PrisonersOfColony)
                {
                    if (prisoner == null || prisoner.Dead || prisoner.Downed) continue;
                    float dist = infected.Position.DistanceTo(prisoner.Position);
                    if (dist > sightRadius) continue;
                    candidates.Add(prisoner);
                }
            }

            if (candidates.Count == 0) return null;

            // Stable sort by thingIDNumber for determinism.
            candidates.Sort((a, b) => a.thingIDNumber.CompareTo(b.thingIDNumber));

            // Find the first pawn with line-of-sight.
            foreach (var pawn in candidates)
            {
                if (GenSight.LineOfSight(infected.Position, pawn.Position, map, skipFirstCell: false))
                    return pawn;
            }

            return null;
        }

        /// <summary>
        /// Legacy method for compatibility — delegates to FindVisiblePawn.
        /// </summary>
        public static Pawn FindVisibleColonist(Pawn infected, Map map, float sightRadius)
        {
            return FindVisiblePawn(infected, map, sightRadius);
        }
    }
}
