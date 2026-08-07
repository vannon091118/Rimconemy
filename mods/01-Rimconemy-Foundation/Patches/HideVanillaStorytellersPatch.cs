using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.Patches
{
    /// <summary>
    /// Hides vanilla storytellers (Cassandra, Phoebe, Randy) from the
    /// storyteller selection screen. Complements the XML listOrder=99999
    /// patch by removing them from the PreOpen def list entirely.
    ///
    /// Rimconemy is the ONLY selectable storyteller (DECISIONS §34).
    /// </summary>
    [HarmonyPatch(typeof(Page_SelectStoryteller), "PreOpen")]
    public static class HideVanillaStorytellersPatch
    {
        private static readonly HashSet<string> HiddenDefs = new HashSet<string>
        {
            "Cassandra",
            "Phoebe",
            "Randy",
        };

        /// <summary>
        /// After PreOpen populates the storyteller list, remove Cassandra/
        /// Phoebe/Randy so only Rimconemy appears.
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                var allDefs = DefDatabase<StorytellerDef>.AllDefsListForReading;
                if (allDefs == null) return;

                // Find the RimWorld storyteller selection page's private
                // list of visible defs. Page_SelectStoryteller stores its
                // storyteller defs in a private field; we access it via
                // Harmony's Traverse then filter.
                var page = Find.WindowStack?.WindowOfType<Page_SelectStoryteller>();
                if (page == null) return;

                // Use reflection to access the internal storyteller list.
                // RimWorld stores this in a private field of type List<StorytellerDef>
                // (commonly "_storytellerDefs" or referenced via the def database).
                //
                // Strategy: after PreOpen, any vanilla def still in the list
                // is removed by name. The private field names vary across
                // RimWorld versions; we probe the most common ones.
                TryRemoveFromField(page, "storytellerDefs");
                TryRemoveFromField(page, "tmpStorytellers");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Rimconemy.Foundation] HideVanillaStorytellersPatch: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void TryRemoveFromField(object instance, string fieldName)
        {
            var field = AccessTools.Field(instance.GetType(), fieldName);
            if (field == null) return;

            if (field.GetValue(instance) is List<StorytellerDef> list)
            {
                int removed = list.RemoveAll(def =>
                    def != null && HiddenDefs.Contains(def.defName));
                if (removed > 0)
                {
                    Log.Message($"[Rimconemy.Foundation] HideVanillaStorytellersPatch: removed {removed} vanilla storyteller(s) via field '{fieldName}'.");
                }
            }
        }
    }
}
