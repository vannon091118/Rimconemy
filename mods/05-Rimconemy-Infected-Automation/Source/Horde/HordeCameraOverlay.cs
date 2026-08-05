// Source/Horde/HordeCameraOverlay.cs
//
// Phase D — Camera-Edge-Frame pulse renderer. Subscribes a Harmony
// Postfix on UIRoot_Update so each frame draws four alpha-driven thin
// borders (top/bottom/left/right) when the horde is active. The Pure
// alpha-calculation reuses HordeCalculator.ComputePulsePhase so
// Player-Home-Map and Camera-Edge pulse together.

using HarmonyLib;
using Rimconemy.InfectedAutomation.Population;
using Rimconemy.InfectedAutomation.Story;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    /// <summary>
    /// Phase D — Camera-Edge-Frame overlay. Static install-once pattern
    /// matching DarknessSectionLayerLifecycle: a single Subscription
    /// flag prevents double-installation; the OnGUI postfix reads the
    /// live HordeCalculator and renders 4 borders each frame.
    /// </summary>
    [HarmonyPatch(typeof(UIRoot), nameof(UIRoot.UIRootOnGUI))]
    public static class HordeCameraOverlay
    {
        private const float EdgeThickness = 8f;
        private const float EdgeAlphaMax = 0.4f;

        private static bool _installed = false;

        public static void Install()
        {
            if (_installed) return;
            try
            {
                _installed = true;
                Log.Message("[Rimconemy.InfectedAutomation] HordeCameraOverlay installed (UIRootOnGUI Postfix).");
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeCameraOverlay.Install failed: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // Postfix — runs at end of UIRoot.UIRootOnGUI per frame.
        public static void Postfix()
        {
            try
            {
                if (!IsHordeActive()) return;
                DrawEdgeFrame();
            }
            catch (System.Exception ex)
            {
                Log.Warning("[Rimconemy.InfectedAutomation] HordeCameraOverlay.Postfix: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool IsHordeActive()
        {
            var ledger = PopulationLedger.Get();
            if (ledger == null) return false;
            int effective = HordeCalculator.GetEffectiveCount(ledger);
            var profile = Story.StoryDirector.Get()?.ActiveProfile ?? SettingProfile.Survival;
            return HordeCalculator.IsActive(effective, profile);
        }

        private static void DrawEdgeFrame()
        {
            long currentTick = Find.TickManager?.TicksGame ?? 0L;
            float phase = HordeCalculator.ComputePulsePhase(currentTick);
            float alpha = EdgeAlphaMax * phase;

            int width = Screen.width;
            int height = Screen.height;
            var prev = GUI.color;
            GUI.color = new Color(0.85f, 0.15f, 0.15f, alpha);

            if (Texture2D.whiteTexture != null)
            {
                GUI.DrawTexture(new Rect(0f, 0f, width, EdgeThickness), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, height - EdgeThickness, width, EdgeThickness), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0f, 0f, EdgeThickness, height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(width - EdgeThickness, 0f, EdgeThickness, height), Texture2D.whiteTexture);
            }

            GUI.color = prev;
        }
    }
}
