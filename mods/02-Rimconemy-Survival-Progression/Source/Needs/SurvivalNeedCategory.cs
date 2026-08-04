using System.Collections.Generic;
using Verse;

namespace Rimconemy.SurvivalProgression.Needs
{
    /// <summary>
    /// Owner: Survival and Progression.
    /// Three Rimconemy core needs, exposed in two equivalent surface layers:
    ///   1. Legacy short-name constants ("Rimconemy.Food") for quick reference
    ///      in code, debug strings and Keyed lookups.
    ///   2. Full Setting <c>defName</c> identifiers ("Rimconemy_Need_Food")
    ///      that match the NeedDef entries and are used by
    ///      <see cref="NeedMappingService"/>.
    ///
    /// This file is intentionally a thin wrapper. Sampling always goes
    /// through <see cref="NeedMappingService"/> so we never sample from
    /// Vanilla directly. The legacy short names stay around for backward
    /// compatibility and descriptive logs.
    /// </summary>
    public static class SurvivalNeedCategory
    {
        // Legacy short labels used in debug logs and the UI dashboard. These
        // are NOT defNames - they are symbols for "what is being sampled".
        public const string Food = "Rimconemy.Food";
        public const string Safety = "Rimconemy.Safety";
        public const string Social = "Rimconemy.Social";

        // Setting defNames that match the entries in
        // Defs/Needs/Rimconemy_Needs.xml. Used by NeedMappingService.
        // Inlined as string literals (not references to NeedMappingService
        // constants) to avoid any cross-file initialization order risk at
        // C# compile-time const inference.
        public const string FoodDefName = "Rimconemy_Need_Food";
        public const string SafetyDefName = "Rimconemy_Need_Safety";
        public const string SocialDefName = "Rimconemy_Need_Social";

        public static readonly List<string> All = new List<string>
        {
            Food,
            Safety,
            Social,
        };

        /// <summary>DefName list - useful for cross-package readers.</summary>
        public static readonly List<string> AllDefNames = new List<string>
        {
            FoodDefName,
            SafetyDefName,
            SocialDefName,
        };

        [StaticConstructorOnStartup]
        private static class Register
        {
            static Register()
            {
                Log.Message(
                    "[Rimconemy.SurvivalProgression] Need read models registered: " +
                    "Food/Safety/Social (projected from Vanilla Food/Rest/Joy through NeedMappingService).");
            }
        }
    }
}
