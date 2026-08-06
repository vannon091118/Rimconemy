using System;
using System.Reflection;
using RimWorld;
using Verse;
using Rimconemy.SurvivalProgression;

namespace Rimconemy.SurvivalProgression.Bridge
{
    /// <summary>
    /// Forwards survival events to the optional Package 05 tutorial bridge
    /// without adding a compile-time dependency from Package 02 to Package 05.
    /// </summary>
    public static class SurvivalTutorialBridge
    {
        private const string BridgeTypeName =
            "Rimconemy.InfectedAutomation.Scenarios.TutorialTriggerBridge, Rimconemy.InfectedAutomation";

        private static bool initialized;

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            CampfireManager.OnCampfireBuilt += OnCampfireBuilt;
            WallBuilder.OnWallBuilt += OnWallBuilt;
            ResourceCollector.OnResourceCollected += OnResourceCollected;
        }

        private static void OnCampfireBuilt()
        {
            Invoke("FireCampfireBuilt");
        }

        private static void OnWallBuilt()
        {
            Invoke("FireWallBuilt");
        }

        private static void OnResourceCollected(ThingDef def)
        {
            Invoke("FireResourceCollected", def);
        }

        private static void Invoke(string methodName, ThingDef def = null)
        {
            try
            {
                var bridgeType = Type.GetType(BridgeTypeName, false);
                if (bridgeType == null) return;

                MethodInfo method = def == null
                    ? bridgeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null)
                    : bridgeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null,
                        new[] { typeof(ThingDef) }, null);
                if (method == null) return;

                method.Invoke(null, def == null ? null : new object[] { def });
            }
            catch (Exception ex)
            {
                Log.Warning($"[Rimconemy.SurvivalProgression] Tutorial trigger '{methodName}' failed: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}