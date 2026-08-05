// Source/Horde/HordeCameraOverlay.cs
//
// Phase D — Camera-Edge-Frame pulse renderer. The [HarmonyPatch]
// attribute registers the postfix automatically (same mechanism as
// CollectiveDefensePostCombatPatch), so no explicit Install() is needed.
// Each frame draws four alpha-driven thin borders (top/bottom/left/right)
// when the horde is active. The Pure alpha-calculation reuses
// HordeCalculator.ComputePulsePhase so Player-Home-Map and Camera-Edge
// pulse together.

using HarmonyLib;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Horde
{
    [HarmonyPatch(typeof(UIRoot), nameof(UIRoot.UIRootOnGUI))]
    public static class HordeCameraOverlay
    {
        private const float EdgeThickness = 8f;
        private const float EdgeAlphaMax = 0.4f;

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
