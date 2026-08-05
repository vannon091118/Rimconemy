
using HarmonyLib;
using RimWorld;

namespace Rimconemy
{
    [HarmonyPatch(typeof(Verse.Log), nameof(Verse.Log.Message))]
    public static class MyPatch
    {
        public static void Postfix(string message)
        {
            // Hier kommt der Code, der ausgeführt werden soll
        }
    }
}
