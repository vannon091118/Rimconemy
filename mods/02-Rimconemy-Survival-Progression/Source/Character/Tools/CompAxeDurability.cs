using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.Character.Tools
{
    /// <summary>
    /// Axe durability component. Tracks remaining uses per-instance.
    /// When uses reach zero, the axe is destroyed (vanishes).
    /// Max uses is set at creation time (default 50).
    /// </summary>
    public sealed class CompProperties_AxeDurability : CompProperties
    {
        public CompProperties_AxeDurability()
        {
            compClass = typeof(CompAxeDurability);
        }

        /// <summary>Maximum uses before the axe breaks.</summary>
        public int maxUses = 50;
    }

    /// <summary>Per-instance durability tracking for axes.</summary>
    public sealed class CompAxeDurability : ThingComp
    {
        private int remainingUses;

        public int RemainingUses => remainingUses;
        public int MaxUses => ((CompProperties_AxeDurability)props).maxUses;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && remainingUses <= 0)
            {
                remainingUses = MaxUses;
            }
        }

        public void SetMaxUses(int max)
        {
            remainingUses = Mathf.Clamp(max, 0, MaxUses);
        }

        public void UseOnce()
        {
            if (remainingUses > 0)
                remainingUses--;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remainingUses, "remainingUses", 0);
        }

        public override string CompInspectStringExtra()
        {
            if (remainingUses <= 0)
                return "Rimconemy.AxeDurability.Broken".Translate();
            return "Rimconemy.AxeDurability.Uses".Translate(remainingUses, MaxUses);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Reset axe durability",
                    action = () => remainingUses = MaxUses,
                };
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Break axe",
                    action = () => remainingUses = 0,
                };
            }
        }
    }
}