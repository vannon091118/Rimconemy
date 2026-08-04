using System.Collections.Generic;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Resources
{
    /// <summary>
    /// Owner: Scavenger Infrastructure.
    /// Resource categories. Both defName-pair (Def) and
    /// runtime-stack (Game) handling happen through vanilla pipelines.
    /// 2026-08-04: Added SteelScraps for campfire → steel loop.
    /// </summary>
    public static class ResourceCategory
    {
        public const string ConstructionDebris = "Rimconemy.ConstructionDebris";
        public const string Hemp = "Rimconemy.Hemp";
        public const string Water = "Rimconemy.Water";
        public const string SteelScraps = "Rimconemy.SteelScraps";

        public static readonly List<string> All = new List<string>
        {
            ConstructionDebris,
            Hemp,
            Water,
            SteelScraps,
        };

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] Resources stub list compiled (ConstructionDebris / Hemp / Water / SteelScraps).");
            }
        }
    }
}
