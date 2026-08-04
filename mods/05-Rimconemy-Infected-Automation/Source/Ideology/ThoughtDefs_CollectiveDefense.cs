using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Rimconemy.InfectedAutomation.Ideology
{
    /// <summary>
    /// Owner: Infected and Automation (Package 05).
    /// Setting Rule: CollectiveDefense (H3 §2).
    ///
    /// Registers in code (XML proved fragile for workerClass pointers):
    ///   Rimconemy_Thought_ValiantDefense  : +5 mood for 2 days
    ///   Rimconemy_Thought_DefenseShirking : -8 mood for 3 days
    ///   Rimconemy_Thought_UnitedAfterDefense : +3 group mood for 2 days
    ///
    /// Activation logic lives in <see cref="CollectiveDefenseTracker"/> which
    /// is invoked from <see cref="CollectiveDefensePostCombatPatch"/> after
    /// each combat event. The ThoughtWorker here only acts as the vehicle
    /// for vanilla ThoughtDef registration so the mood offset, stage and
    /// label stay consistent with the surrounding pawn-needs machinery.
    ///
    /// Specification: docs/H3-ideology-influence-matrix.md §2.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ThoughtDefs_CollectiveDefense
    {
        public static ThoughtDef ValiantDefense;
        public static ThoughtDef DefenseShirking;
        public static ThoughtDef UnitedAfterDefense;

        static ThoughtDefs_CollectiveDefense()
        {
            try
            {
                ValiantDefense = CreateValiantThought();
                DefenseShirking = CreateShirkingThought();
                UnitedAfterDefense = CreateUnitedGroupThought();

                DefDatabase<ThoughtDef>.Add(ValiantDefense);
                DefDatabase<ThoughtDef>.Add(DefenseShirking);
                DefDatabase<ThoughtDef>.Add(UnitedAfterDefense);

                // Wire ThoughtDefs into the PreceptDef comps that were
                // loaded from XML but whose cross-refs couldn't resolve
                // (ThoughtDefs are registered here, after XML loading).
                WirePreceptComps();

                Log.Message("[Rimconemy.InfectedAutomation] CollectiveDefense ThoughtDefs registered in code.");
            }
            catch (System.Exception ex)
            {
                Log.Warning(
                    "[Rimconemy.InfectedAutomation] Could not register CollectiveDefense ThoughtDefs: "
                    + ex.GetType().Name + ": " + ex.Message
                    + ". Setting rule 2 remains dormant; combat post-effects skipped.");
            }
        }

        private static ThoughtDef CreateValiantThought()
        {
            var def = new ThoughtDef
            {
                defName = "Rimconemy_Thought_ValiantDefense",
                label = "mutig verteidigt",
                description = "Ich habe die Gemeinschaft verteidigt. Das war richtig.",
                durationDays = 2f,
                stages = new List<ThoughtStage>
                {
                    new ThoughtStage
                    {
                        label = "mutig verteidigt",
                        description = "Ich habe bei der Verteidigung mitgefochten. Das stärkt das Vertrauen.",
                        baseMoodEffect = 5f,
                    },
                },
            };
            def.PostLoad();
            return def;
        }

        private static ThoughtDef CreateShirkingThought()
        {
            // Single stage matches H3 spec: -8 mood for 3 days.
            // Cumulative stage is intentionally omitted: tracking duration of
            // shirking across multiple days requires a pawn-state accumulator
            // (planned for Phase-2 polishing iteration).
            var def = new ThoughtDef
            {
                defName = "Rimconemy_Thought_DefenseShirking",
                label = "gedrückt",
                description = "Ich hätte kämpfen können, habe es aber nicht getan. Das fühlt sich falsch an.",
                durationDays = 3f,
                stages = new List<ThoughtStage>
                {
                    new ThoughtStage
                    {
                        label = "gedrückt",
                        description = "Andere haben gekämpft. Ich hätte auch dabei sein sollen.",
                        baseMoodEffect = -8f,
                    },
                },
            };
            def.PostLoad();
            return def;
        }

        private static ThoughtDef CreateUnitedGroupThought()
        {
            var def = new ThoughtDef
            {
                defName = "Rimconemy_Thought_UnitedAfterDefense",
                label = "zusammengestanden",
                description = "Die Gemeinschaft hat den Angriff gemeinsam abgewehrt. Wir sind stärker geworden.",
                durationDays = 2f,
                stages = new List<ThoughtStage>
                {
                    new ThoughtStage
                    {
                        label = "vereint",
                        description = "Gemeinsam standen wir dem Druck entgegen. Dieses Band bleibt.",
                        baseMoodEffect = 3f,
                    },
                },
            };
            def.PostLoad();
            return def;
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
            var preceptDef = DefDatabase<PreceptDef>.GetNamedSilentFail("Rimconemy_Role_Defender");
            if (preceptDef == null) return;
            if (preceptDef.comps == null) preceptDef.comps = new List<PreceptComp>();

            // Clear any unresolved (null-thought) comps from the XML load
            // and replace with properly wired ones.
            preceptDef.comps.RemoveAll(c => c is PreceptComp_SituationalThought s && s.thought == null);

            preceptDef.comps.Add(new PreceptComp_SituationalThought
            {
                thought = ValiantDefense,
            });
            preceptDef.comps.Add(new PreceptComp_SituationalThought
            {
                thought = DefenseShirking,
            });
            preceptDef.comps.Add(new PreceptComp_SituationalThought
            {
                thought = UnitedAfterDefense,
            });
        }
    }
}
