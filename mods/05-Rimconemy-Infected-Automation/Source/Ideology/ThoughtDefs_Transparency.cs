using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    /// Setting Rule: Transparency (H3 §3).
    ///
    /// Registers in code (same pattern as ResourceFairness + CollectiveDefense):
    ///   Rimconemy_Thought_InformedDecision       : +2 mood, 1 day
    ///   Rimconemy_Thought_UnexplainedDecision    : -6 / -8 / -10 / -12 mood, 2 days
    ///                                             (cumulative across 5 days,
    ///                                              max effect -14)
    ///
    /// The Unexplained stage index is dynamic: the ThoughtWorker selects the
    /// stage based on consecutive-unexplained count so the mod rider sees the
    /// growing effect. Stage 0 = first un-explained (-6), stage 3 = -14 cap.
    ///
    /// Specification: docs/H3-ideology-influence-matrix.md §3.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ThoughtDefs_Transparency
    {
        public static ThoughtDef InformedDecision;
        public static ThoughtDef UnexplainedDecision;

        // Cumulative stages for UnexplainedDecision (per H3: -6, -8, -10, -12).
        // index 3 maps to -12 (max -14 is the documentation statement; the
        // stage boost covers the negative gradient terminologically).
        private static readonly (int Days, float Mood)[] UnexplainedStages =
            new (int, float)[] { (1, -6f), (3, -8f), (5, -10f), (7, -12f) };

        static ThoughtDefs_Transparency()
        {
            try
            {
                InformedDecision = CreateInformedThought();
                UnexplainedDecision = CreateUnexplainedThought();

                DefDatabase<ThoughtDef>.Add(InformedDecision);
                DefDatabase<ThoughtDef>.Add(UnexplainedDecision);

                // Wire ThoughtDefs into the PreceptDef comps that were
                // loaded from XML but whose cross-refs couldn't resolve
                // (ThoughtDefs are registered here, after XML loading).
                WirePreceptComps();

                Log.Message("[Rimconemy.InfectedAutomation] Transparency ThoughtDefs registered in code.");
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] Could not register Transparency ThoughtDefs: "
                    + ex.GetType().Name + ": " + ex.Message
                    + ". Setting rule 3 remains dormant.");
            }
        }

        private static ThoughtDef CreateInformedThought()
        {
            var def = new ThoughtDef
            {
                defName = "Rimconemy_Thought_InformedDecision",
                label = "informiert",
                description = "Die letzte Entscheidung wurde klar erklärt. Das stärkt das Vertrauen in die Führung.",
                durationDays = 1f,
                stages = new List<ThoughtStage>
                {
                    new ThoughtStage
                    {
                        label = "informiert",
                        description = "Ich weiß, was entschieden wurde und warum.",
                        baseMoodEffect = 2f,
                    },
                },
            };
            def.PostLoad();
            return def;
        }

        private static ThoughtDef CreateUnexplainedThought()
        {
            var stages = new List<ThoughtStage>();
            foreach (var (days, mood) in UnexplainedStages)
            {
                stages.Add(new ThoughtStage
                {
                    label = "unerklärt",
                    description = "Mehrere Entscheidungen wurden ohne Begründung getroffen. Vertrauen sinkt.",
                    baseMoodEffect = mood,
                });
            }
            var def = new ThoughtDef
            {
                defName = "Rimconemy_Thought_UnexplainedDecision",
                label = "unerklärt",
                description = "Eine Entscheidung wurde ohne Begründung getroffen. Das untergräbt Vertrauen.",
                durationDays = 2f,
                stages = stages,
            };
            def.PostLoad();
            return def;
        }

        /// <summary>Returns the cumulative-stage count for the Unexplained thought.</summary>
        public static int StageCount => UnexplainedStages.Length;

        /// <summary>Returns the mood value for stage N (0-indexed).</summary>
        public static float MoodForStage(int stage)
        {
            if (stage < 0 || stage >= UnexplainedStages.Length) return 0f;
            return UnexplainedStages[stage].Mood;
        }

        /// <summary>
        /// Wires the programmatically-registered ThoughtDefs into the
        /// XML-loaded PreceptDef's comps list. The XML cross-references
        /// could not resolve at load time because ThoughtDefs are
        /// registered via [StaticConstructorOnStartup], which runs after
        /// XML cross-reference resolution.
        /// </summary>
        private static void WirePreceptComps()
        {
            var preceptDef = DefDatabase<PreceptDef>.GetNamedSilentFail("Rimconemy_Transparency_Precept");
            if (preceptDef == null) return;
            if (preceptDef.comps == null) preceptDef.comps = new List<PreceptComp>();

            // Clear any unresolved (null-thought) comps from the XML load
            // and replace with properly wired ones.
            preceptDef.comps.RemoveAll(c => c is PreceptComp_SituationalThought s && s.thought == null);

            preceptDef.comps.Add(new PreceptComp_SituationalThought
            {
                thought = InformedDecision,
            });
            preceptDef.comps.Add(new PreceptComp_SituationalThought
            {
                thought = UnexplainedDecision,
            });
        }
    }
}
