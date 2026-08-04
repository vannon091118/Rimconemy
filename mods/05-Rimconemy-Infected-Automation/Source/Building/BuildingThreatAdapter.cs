using System.Globalization;

namespace Rimconemy.InfectedAutomation.Building
{
    /// <summary>
    /// Package-05 read-only boundary for Building/Power threat contribution.
    /// It computes no incident and never queues or spawns a raid.
    /// </summary>
    public static class BuildingThreatAdapter
    {
        public static float ComputePressure(
            int activeGenerators,
            int activeTurrets,
            float damageRatio)
        {
            float generators = activeGenerators < 0 ? 0f : activeGenerators;
            float turrets = activeTurrets < 0 ? 0f : activeTurrets;
            float damage = damageRatio < 0f ? 0f : damageRatio;
            float pressure = generators * 0.10f
                + turrets * 0.15f
                + damage * 0.25f;
            if (pressure < 0f) return 0f;
            if (pressure > 1f) return 1f;
            return pressure;
        }

        public static string BuildDeterminismKey(
            long tick,
            string buildingHash,
            string powerHash)
        {
            return tick.ToString(CultureInfo.InvariantCulture)
                + "|" + (buildingHash ?? "")
                + "|" + (powerHash ?? "");
        }
    }
}
