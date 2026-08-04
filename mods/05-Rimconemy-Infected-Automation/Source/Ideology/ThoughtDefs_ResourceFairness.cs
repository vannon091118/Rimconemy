using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05)
    /// Registers the ResourceFairness ThoughtDefs in code instead of XML.
    /// This avoids RimWorld XML-parsing issues with isMemory/workerClass.
    ///
    /// Migration note (S-T4 / I-T4): moved here from Package 02 because
    /// the Ideology domain belongs to Package 05 (X4 decision).
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ThoughtDefs_ResourceFairness
    {
        public static ThoughtDef FairDistribution;
        public static ThoughtDef UnfairDistribution;

        static ThoughtDefs_ResourceFairness()
        {
            FairDistribution = CreateFairThought();
            UnfairDistribution = CreateUnfairThought();

            DefDatabase<ThoughtDef>.Add(FairDistribution);
            DefDatabase<ThoughtDef>.Add(UnfairDistribution);

            Log.Message("[Rimconemy.InfectedAutomation] ResourceFairness ThoughtDefs registered in code.");
        }

        private static ThoughtDef CreateFairThought()
        {
            var def = new ThoughtDef
            {
                defName = "Rimconemy_Thought_FairDistribution",
                label = "gerechte Verteilung",
                description = "Ressourcen sind fair verteilt. Alle haben Zugang zu Nahrung und Medizin.",
                workerClass = typeof(ThoughtWorker_ResourceFairness),
                stages = new System.Collections.Generic.List<ThoughtStage>
                {
                    new ThoughtStage
                    {
                        label = "fair verteilt",
                        description = "Die Ressourcen in der Gruppe sind gerecht verteilt.",
                        baseMoodEffect = 3f,
                    },
                },
            };
            def.PostLoad();
            return def;
        }

        private static ThoughtDef CreateUnfairThought()
        {
            var def = new ThoughtDef
            {
                defName = "Rimconemy_Thought_UnfairDistribution",
                label = "ungerechte Verteilung",
                description = "Ressourcen sind ungleich verteilt. Einige haben zu wenig.",
                workerClass = typeof(ThoughtWorker_ResourceFairness),
                stages = new System.Collections.Generic.List<ThoughtStage>
                {
                    new ThoughtStage
                    {
                        label = "benachteiligt",
                        description = "Ich habe deutlich weniger Ressourcen als die anderen.",
                        baseMoodEffect = -5f,
                    },
                    new ThoughtStage
                    {
                        label = "stark benachteiligt",
                        description = "Seit Tagen bekomme ich weniger ab. Das ist nicht fair.",
                        baseMoodEffect = -8f,
                    },
                },
            };
            def.PostLoad();
            return def;
        }
    }
}
