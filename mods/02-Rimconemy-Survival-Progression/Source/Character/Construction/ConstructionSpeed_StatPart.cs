using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Character.Construction
{
    /// <summary>
    /// Compatibility shim for saves or external XML that still names the old
    /// stat part. Construction skill no longer changes speed or has a +50%
    /// efficiency layer; it changes finished-building durability through
    /// <see cref="BuilderDurability"/>.
    /// </summary>
    [System.Obsolete("Construction skill now affects finished building HP via BuilderDurability.")]
    public sealed class ConstructionSpeed_StatPart : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val) { }
        public override string ExplanationPart(StatRequest req) { return null; }
    }
}
