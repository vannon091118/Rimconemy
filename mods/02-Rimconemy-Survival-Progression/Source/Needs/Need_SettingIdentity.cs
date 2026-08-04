using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Needs
{
    /// <summary>
    /// Concrete reflection target for the three setting NeedDefs.
    ///
    /// RimWorld's Need base class is abstract and cannot be used directly as
    /// NeedDef.needClass. These setting definitions are read-side identities;
    /// they intentionally do not change level, mood, or any other gameplay
    /// state when a future condition causes RimWorld to materialize one.
    /// </summary>
    public sealed class Need_SettingIdentity : Need
    {
        public Need_SettingIdentity(Pawn pawn) : base(pawn)
        {
        }

        public override void NeedInterval()
        {
            // Setting values are projected by NeedMappingService from vanilla
            // needs. This identity need must never create a second simulation.
        }
    }
}
