using System;
using Verse;

namespace Rimconemy.InfectedAutomation.World
{
    /// <summary>
    /// Owner: Infected &amp; Automation (Package 05).
    /// Sprint 2.5 — Colonist Sight Cone Math.
    ///
    /// Computes directional sight cones for colonists based on
    /// light level, pawn facing direction, and nearby light sources.
    /// Modeled after Project Zomboid visibility: forward cone is
    /// brightest/widest, sides dim gradually, behind is near-dark.
    ///
    /// Light scaling: higher GlowGrid value = further sight.
    /// Torches and other glowers extend local visibility.
    /// Mouse cursor adds a weak 0.2 glow aura (3 tiles).
    /// </summary>
    public static class SightConeMath
    {
        /// <summary>Max forward sight at full daylight (GlowGrid=1.0).
        /// Aggressive 2026-08-05: shortened 25 → 18 → 16 so the daylight
        /// range stays compact and the distance-falloff reaches the full-black
        /// cliff earlier, even during day.</summary>
        public const float MaxForwardRadius = 16f;

        /// <summary>Base forward sight at night without artificial light.
        /// 2026-08-05: dropped 15 → 12 so Day (MaxForwardRadius=16) and
        /// Night differ by 4 tiles. At 5 tiles forward, visibility is
        /// ~47 % in daylight vs ~34 % in pitch black; beyond 10 tiles at
        /// night the cone is fully dark and the player must fall back to
        /// torchlight or retreat. Glowers and torches still scale this value
        /// up to MaxForwardRadius.</summary>
        public const float MinForwardRadius = 12f;

        /// <summary>Behind the pawn: fraction of forward radius.</summary>
        public const float BehindRadiusFactor = 0.15f;

        /// <summary>Side visibility at 90°: fraction of forward radius.</summary>
        public const float SideRadiusFactor = 0.35f;

        /// <summary>Width of the forward cone at full extent (degrees from centerline).</summary>
        public const float ConeHalfAngle = 60f;

        /// <summary>Light sources (glowers) add this bonus per unit of nearby
        /// GlowGrid value. 2026-08-05: raised 0.7 → 0.9 so a single torch
        /// next to the pawn at night now extends forward-radius from the
        /// base <see cref="MinForwardRadius"/> = 12 by ≈ +5 tiles (instead
        /// of +4), compensating the tighter Day/Night contrast that came
        /// from shrinking <see cref="MinForwardRadius"/> to 12. Two nearby
        /// torches still clamp to ~full daylight reach
        /// (<see cref="MaxForwardRadius"/> = 16).</summary>
        public const float LightSourceBonusMultiplier = 0.9f;

        /// <summary>Power-curve exponent for distance falloff. 1.0 = linear,
        /// 2.0 = quadratic. 2026-08-05 v2: raised 1.7 → 2.0 so mid-range
        /// cells darken FAST and the visible/hidden split becomes a clear
        /// cliff instead of a slow fade. At full daylight (MaxForward=16):
        ///   5 tiles → vis 0.473 (~53 % black)
        ///  10 tiles → vis 0.141 (~86 % black)
        ///  13 tiles → vis 0.035 (~97 % black)</summary>
        public const float DistanceFalloffExponent = 2.0f;

        /// <summary>How far glowers affect colonist sight (cells).</summary>
        public const float GlowerEffectRadius = 12f;

        /// <summary>Mouse cursor glow radius (cells).</summary>
        public const float MouseGlowRadius = 3f;

        /// <summary>Mouse cursor glow intensity (adds to visibility).</summary>
        public const float MouseGlowIntensity = 0.2f;

        /// <summary>
        /// Computes the directional sight radius from a pawn's position
        /// to a target cell. Returns a factor [0, 1] where 1 = fully visible
        /// and 0 = completely dark.
        ///
        /// Formula:
        ///   1. Base radius = lerp(min, max, lightLevel)
        ///   2. Directional factor = cone shape based on angle from facing
        ///   3. Distance falloff: linear from 1 at pawn to 0 at radius
        ///   4. Light source bonus: nearby glowers add to base radius
        ///   5. Mouse glow: small additive if near mouse cursor
        /// </summary>
        /// <param name="pawnPos">Position of the colonist.</param>
        /// <param name="targetCell">Cell to compute visibility for.</param>
        /// <param name="facingDir">Direction the pawn is facing/looking.</param>
        /// <param name="cellLightLevel">GlowGrid light level at pawn position [0, 1].</param>
        /// <param name="nearbyGlowerBonus">Bonus light from nearby glowers [0, 1].</param>
        /// <param name="mouseCell">Current mouse-over cell (or IntVec3.Invalid).</param>
        /// <returns>Visibility factor [0, 1].</returns>
        public static float ComputeCellVisibility(
            IntVec3 pawnPos,
            IntVec3 targetCell,
            IntVec3 facingDir,
            float cellLightLevel,
            float nearbyGlowerBonus,
            IntVec3 mouseCell)
        {
            float effectiveLight = Math.Min(1f, cellLightLevel + nearbyGlowerBonus);

            // Base forward radius from light level.
            float forwardRadius = MinForwardRadius + (MaxForwardRadius - MinForwardRadius) * effectiveLight;

            // Distance from pawn to target.
            float dist = pawnPos.DistanceTo(targetCell);
            if (dist < 0.001f) return 1f; // Pawn's own cell is always visible.

            // Directional factor: angle between facing and target.
            float directionalFactor = ComputeDirectionalFactor(pawnPos, targetCell, facingDir, forwardRadius);

            // Distance falloff: power-curve from 1 at pawn to 0 at max radius.
            // Aggressive 2026-08-05: linear falloff made mid-range cells nearly
            // full-bright even during day; the power curve makes distance
            // perceptible at every range, so the cone always looks dimmer
            // towards the edges regardless of light level.
            float maxRadius = forwardRadius * directionalFactor;
            if (dist >= maxRadius) return 0f;
            float normalizedDist = 1f - (dist / maxRadius);
            float distanceFactor = MathF.Pow(normalizedDist, DistanceFalloffExponent);

            // Combine.
            float visibility = distanceFactor * directionalFactor;

            // Mouse glow: add small visibility near cursor.
            visibility = Math.Max(visibility, MouseGlowFactor(targetCell, mouseCell));

            return Math.Min(1f, visibility);
        }

        /// <summary>
        /// Computes the directional factor based on angle from facing direction.
        /// Forward = 1.0, sides = ~0.35, behind = ~0.15.
        /// Uses a smooth cosine-based falloff.
        /// </summary>
        public static float ComputeDirectionalFactor(IntVec3 pawnPos, IntVec3 target, IntVec3 facing, float forwardRadius)
        {
            if (facing == IntVec3.Invalid || facing == IntVec3.Zero)
                return 1f; // No facing direction → full omnidirectional.

            IntVec3 toTarget = target - pawnPos;
            float angle = AngleBetween(facing, toTarget);

            // Cosine-based cone: angle 0° = 1, angle 90° = sideFactor, angle 180° = behindFactor.
            float angleDeg = angle * 180f / MathF.PI;

            if (angleDeg <= ConeHalfAngle)
            {
                // Within forward cone: full forward radius.
                return 1f;
            }
            else if (angleDeg <= 120f)
            {
                // Transition zone: cone edge → side.
                float t = (angleDeg - ConeHalfAngle) / (120f - ConeHalfAngle);
                return 1f - t * (1f - SideRadiusFactor);
            }
            else
            {
                // Side → behind transition.
                float t = (angleDeg - 120f) / (180f - 120f);
                return SideRadiusFactor - t * (SideRadiusFactor - BehindRadiusFactor);
            }
        }

        /// <summary>
        /// Returns the angle in radians between two vectors. Always positive [0, π].
        /// </summary>
        private static float AngleBetween(IntVec3 a, IntVec3 b)
        {
            float dot = a.x * b.x + a.z * b.z;
            float magA = MathF.Sqrt(a.x * a.x + a.z * a.z);
            float magB = MathF.Sqrt(b.x * b.x + b.z * b.z);
            if (magA < 0.001f || magB < 0.001f) return 0f;
            float cosAngle = dot / (magA * magB);
            cosAngle = Math.Clamp(cosAngle, -1f, 1f);
            return MathF.Acos(cosAngle);
        }

        /// <summary>
        /// Computes the nearby glower bonus for a colonist at the given position.
        /// Scans map glowers (CompGlower with Glows=true) within GlowerEffectRadius.
        /// </summary>
        public static float ComputeNearbyGlowerBonus(Map map, IntVec3 pawnPos)
        {
            if (map == null) return 0f;
            float bonus = 0f;

            // Single pass over AllThings. In RimWorld 1.6, buildings register
            // in listerThings.AllThings when spawned, so a separate iteration
            // over allBuildingsColonist would double-count building glowers.
            if (map.listerThings?.AllThings != null)
            {
                foreach (var t in map.listerThings.AllThings)
                {
                    if (t == null) continue;
                    var glower = t.TryGetComp<CompGlower>();
                    if (glower == null || !glower.Glows) continue;
                    float dist = pawnPos.DistanceTo(t.Position);
                    if (dist > GlowerEffectRadius) continue;
                    float contribution = (1f - dist / GlowerEffectRadius) * LightSourceBonusMultiplier;
                    bonus += contribution;
                }
            }

            return Math.Min(1f, bonus);
        }

        /// <summary>
        /// Returns the light level at a cell. Uses <see cref="LightSystem.DaylightCurve"/>
        /// and <see cref="LightSystem.WeatherAttenuation"/> — single source of truth.
        /// Value [0, 1] where 1 = full daylight/bright interior.
        /// </summary>
        public static float GetCellLightLevel(Map map, IntVec3 cell)
        {
            if (map == null || !cell.InBounds(map)) return 0f;

            long tick = Find.TickManager?.TicksGame ?? 0L;
            float hour = LightSystem.HourOfDay(tick);
            float daylight = LightSystem.DaylightCurve(hour);
            float weatherFactor = LightSystem.WeatherAttenuation(map);

            float outdoorLight = daylight * weatherFactor;

            // If roofed, outdoor light is blocked; only artificial light matters.
            if (map.roofGrid != null && map.roofGrid.Roofed(cell))
                outdoorLight = 0f;

            return outdoorLight;
        }

        /// <summary>
        /// Mouse cursor glow: adds MouseGlowIntensity within MouseGlowRadius.
        /// </summary>
        private static float MouseGlowFactor(IntVec3 cell, IntVec3 mouseCell)
        {
            if (!mouseCell.IsValid) return 0f;
            float dist = cell.DistanceTo(mouseCell);
            if (dist > MouseGlowRadius) return 0f;
            return MouseGlowIntensity * (1f - dist / MouseGlowRadius);
        }

        /// <summary>
        /// Gets the facing direction of a pawn based on its last movement
        /// or the direction to its current job target.
        /// Returns IntVec3.Invalid if direction cannot be determined.
        /// </summary>
        public static IntVec3 GetPawnFacing(Pawn pawn)
        {
            if (pawn == null) return IntVec3.Invalid;

            // Priority 1: direction of current job target.
            if (pawn.CurJob?.targetA.IsValid == true && pawn.CurJob.targetA.HasThing)
            {
                var targetPos = pawn.CurJob.targetA.Thing.Position;
                var dir = targetPos - pawn.Position;
                if (dir.LengthHorizontalSquared > 0)
                    return dir;
            }

            // Priority 2: last movement direction from pawn rotation.
            if (pawn.Rotation != Rot4.Invalid)
            {
                return pawn.Rotation.FacingCell;
            }

            return IntVec3.Invalid;
        }
    }
}
