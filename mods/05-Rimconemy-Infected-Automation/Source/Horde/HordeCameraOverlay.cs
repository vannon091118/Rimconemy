// Source/Horde/HordeCameraOverlay.cs
//
// Phase D — Camera-Edge-Frame pulse renderer. Package 05 has no
// Harmony PatchAll (Bootstrap registers patches explicitly, cf.
// DarknessSectionLayerLifecycle), so the postfix must be installed with
// an explicit harmony.Patch call — a bare [HarmonyPatch] attribute
// would be inert. Each frame draws four alpha-driven thin borders
// (top/bottom/left/right) when the horde is active. The Pure
// alpha-calculation reuses HordeCalculator.ComputePulsePhase so the
// Home-Map circle and the Camera-Edge pulse together.

using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    public static class HordeCameraOverlay
    {
        private const string HarmonyId = "rimconemy.infectedautomation.horde-camera-overlay";
        private const float EdgeThickness = 8f;
        private const float EdgeAlphaMax = 0.4f;

        private static bool _installed;

        /// <summary>Installs the UIRootOnGUI postfix once during Package 05 bootstrap.</summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            try
            {
                var target = AccessTools.Method(typeof(UIRoot), nameof(UIRoot.UIRootOnGUI));
                if (target == null)
                {
                    Log.Warning("[Rimconemy.InfectedAutomation] HordeCameraOverlay: UIRoot.UIRootOnGUI missing; edge pulse disabled.");
                    return;
                }

                var harmony = new Harmony(HarmonyId);
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(HordeCameraOverlay), nameof(Postfix)));
                Log.Message("[Rimconemy.InfectedAutomation] HordeCameraOverlay: edge-frame postfix installed.");
            }
            catch (Exception ex)
            {
                // Fail closed: a missing hook must not break the UI loop.
                Log.Warning("[Rimconemy.InfectedAutomation] HordeCameraOverlay install failed; edge pulse disabled: "
                    + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // Postfix — runs at end of UIRoot.UIRootOnGUI per frame.
        public static void Postfix()
        {
            if (!HordeCalculator.IsActiveNow()) return;
            DrawEdgeFrame();
        }

        private static void DrawEdgeFrame()
        {
            float phase = HordeCalculator.ComputePulsePhase(Find.TickManager?.TicksGame ?? 0L);
            float alpha = EdgeAlphaMax * phase;

            int width = Screen.width;
            int height = Screen.height;
            var prev = GUI.color;
            GUI.color = new Color(0.85f, 0.15f, 0.15f, alpha);

            GUI.DrawTexture(new Rect(0f, 0f, width, EdgeThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, height - EdgeThickness, width, EdgeThickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0f, 0f, EdgeThickness, height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(width - EdgeThickness, 0f, EdgeThickness, height), Texture2D.whiteTexture);

            GUI.color = prev;
        }
    }
}
