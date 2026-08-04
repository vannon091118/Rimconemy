using System.Collections.Generic;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.Resources
{
    /// <summary>
    /// Owner: Scavenger Infrastructure.
    /// Three primary resource categories. Both defName-pair (Def) and
    /// runtime-stack (Game) handling happen through vanilla pipelines.
    /// SPIKE: API-POWER-01 / API-PLANT-01 (1.6 ThingDef categories unknown).
    /// </summary>
    public static class ResourceCategory
    {
        public const string ConstructionDebris = "Rimconemy.ConstructionDebris";
        public const string Hemp = "Rimconemy.Hemp";
        public const string Water = "Rimconemy.Water";

        public static readonly List<string> All = new List<string>
        {
            ConstructionDebris,
            Hemp,
            Water,
        };

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                Log.Message("[Rimconemy.ScavengerInfrastructure] Resources stub list compiled (ConstructionDebris / Hemp / Water).");
            }
        }
    }
}
