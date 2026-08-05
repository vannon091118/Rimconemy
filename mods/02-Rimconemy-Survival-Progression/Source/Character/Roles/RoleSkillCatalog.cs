using RimWorld;
using Verse;

namespace Rimconemy.SurvivalProgression.Character.Roles
{
    /// <summary>
    /// Single source of truth for which vanilla skills the character layer
    /// hides from the budget window. Hidden skills stay vanilla and
    /// save-compatible; the player sees the derived role read-models
    /// (Hunting, Smithing) instead of allocating two parallel identities.
    /// </summary>
    public static class RoleSkillCatalog
    {
        public static bool HiddenFromCharacterWindow(SkillDef skill)
        {
            return skill == SkillDefOf.Animals || skill == SkillDefOf.Artistic;
        }
    }
}
