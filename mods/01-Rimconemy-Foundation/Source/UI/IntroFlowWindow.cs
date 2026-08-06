using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Cinematic Intro Window — Black Screen + Flow-Text + Kamera-Cuts + Zombie-Horde-Flash.
    /// Owner: Foundation (Paket 01) — UI Layer.
    /// KEINE Abhängigkeiten von anderen Paketen! Alle Paket-05-Logik läuft via Callbacks.
    ///
    /// Timing (Falsifikation 2026-08-06): Die Vorgängervariante nutzte
    /// <c>Find.TickManager.TicksGame</c> als Taktgeber. Da das Fenster
    /// <c>forcePause = true</c> setzt, steht der Spiel-Tick während der Sequenz
    /// still — die Intro hing am ersten Textblock fest. Die Sequenz läuft daher
    /// auf Realtime (<see cref="Time.realtimeSinceStartup"/>); das entspricht
    /// exakt dem Fix, der in der (inzwischen entfernten) Paket-05-Kopie lag.
    /// </summary>
    public class IntroFlowWindow : RimconemyWindow
    {
        // Flow-Text Blöcke (je BlockDurationSeconds Sekunden) — übersetzt via .Translate()
        private readonly string[] flowTexts = new[]
        {
            "Rimconemy.Intro.Flow.0".Translate().ToString(),
            "Rimconemy.Intro.Flow.1".Translate().ToString(),
            "Rimconemy.Intro.Flow.2".Translate().ToString(),
            "Rimconemy.Intro.Flow.3".Translate().ToString(),
            "Rimconemy.Intro.Flow.4".Translate().ToString(),
        };

        private const float BlockDurationSeconds = 3.5f;
        private const float FadeSeconds = 0.7f;
        private const float HordeFlashSeconds = 3.0f;
        private const float AutoCloseBufferSeconds = 1.2f;

        // Kamera-Cut Positionen (werden von ScenPart_IntroSequence gesetzt)
        public List<IntVec3> cameraCutPositions = new();
        private int currentCutIndex;
        private float lastCutRealtime;

        // Zombie-Horde-Flash — komplett generisch, KEINE Paket-05-Referenzen
        private bool hordeFlashed;
        private bool hordeDespawned;
        private float hordeFlashStartRealtime;
        private List<Pawn> tempHordePawns = new();

        // Callbacks für Paket-05-Logik (werden zur Runtime gesetzt)
        public System.Action OnClosed;
        public System.Func<string, Faction, PawnKindDef, IntVec3, Map, Pawn> SpawnHordePawn;
        public System.Action<Pawn> DespawnPawn;

        private float startRealtime;

        public override Vector2 InitialSize => new Vector2(1f, 1f); // Fullscreen

        public IntroFlowWindow()
        {
            forcePause = true;       // Window handles pausing via base class
            closeOnCancel = true;    // Esc = Intro überspringen (sauberer Exit via Close)
            closeOnAccept = false;
            doCloseButton = false;
            doCloseX = false;
            preventCameraMotion = false;
            draggable = false;
            resizeable = false;
            absorbInputAroundWindow = true;
        }

        protected override void SetInitialSizeAndPosition()
        {
            // Echte Vollbild-Fläche statt InitialSize (1,1): Klick-/Tastevents
            // (Skip-Button, Esc) bleiben überall im Fenster aktiv.
            windowRect = new Rect(0f, 0f, (float)Verse.UI.screenWidth, (float)Verse.UI.screenHeight);
        }

        public override void PreOpen()
        {
            base.PreOpen();
            startRealtime = Time.realtimeSinceStartup;
            lastCutRealtime = startRealtime;
            currentCutIndex = 0;
            hordeFlashed = false;
            hordeDespawned = false;
        }

        public override void PostClose()
        {
            base.PostClose();
            CleanupHorde();

            // Invoke callback
            OnClosed?.Invoke();
        }

        public override void DoWindowContents(Rect inRect)
        {
            float screenW = (float)Verse.UI.screenWidth;
            float screenH = (float)Verse.UI.screenHeight;

            // Fullscreen Black Background
            Widgets.DrawRectFast(new Rect(0, 0, screenW, screenH), Color.black);

            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - startRealtime);
            int blockIndex = Mathf.Min(flowTexts.Length - 1, (int)(elapsed / BlockDurationSeconds));

            // ── Letterbox (cinematic bars oben/unten) ──────────────────
            float barH = screenH * 0.10f;
            Widgets.DrawRectFast(new Rect(0, 0, screenW, barH), new Color(0f, 0f, 0f, 0.92f));
            Widgets.DrawRectFast(new Rect(0, screenH - barH, screenW, barH), new Color(0f, 0f, 0f, 0.92f));
            GUI.color = new Color(0.35f, 0.36f, 0.38f, 0.8f); // feine Trennlinien
            Widgets.DrawLineHorizontal(0f, barH - 1f, screenW);
            Widgets.DrawLineHorizontal(0f, screenH - barH + 1f, screenW);
            GUI.color = Color.white;

            // ── Flow-Text mit Fade-In (mittlere Bildzone) ──────────────
            float alpha = Mathf.Clamp01((elapsed - blockIndex * BlockDurationSeconds) / FadeSeconds);
            string text = flowTexts[blockIndex].Replace("{PawnName}", GetStartingPawnName());
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Widgets.Label(new Rect(0, screenH * 0.30f, screenW, screenH * 0.40f), text);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // ── Kamera-Cuts (ein Cut pro Block) ─────────────────────────
            if (cameraCutPositions != null && cameraCutPositions.Count > 0
                && elapsed - lastCutRealtime >= BlockDurationSeconds)
            {
                lastCutRealtime += BlockDurationSeconds;
                var pos = cameraCutPositions[currentCutIndex % cameraCutPositions.Count];
                if (Find.CameraDriver != null)
                    Find.CameraDriver.JumpToCurrentMapLoc(pos);
                currentCutIndex++;
            }

            // ── Fortschritts-Punkte ──────────────────────────────────────
            float dotW = 12f;
            float totalDots = flowTexts.Length * dotW + (flowTexts.Length - 1) * 8f;
            float dotX = screenW / 2f - totalDots / 2f;
            float dotY = screenH * 0.86f;
            for (int i = 0; i < flowTexts.Length; i++)
            {
                GUI.color = i <= blockIndex ? Color.white : new Color(1f, 1f, 1f, 0.22f);
                Widgets.DrawBoxSolid(new Rect(dotX, dotY, dotW, 6f), GUI.color);
                dotX += dotW + 8f;
            }
            GUI.color = Color.white;

            // ── Horde-Flash im letzten Block ─────────────────────────────
            if (blockIndex >= flowTexts.Length - 1 && !hordeFlashed)
            {
                hordeFlashed = true;
                hordeFlashStartRealtime = Time.realtimeSinceStartup;
                FlashHorde();
            }
            if (hordeFlashed && !hordeDespawned
                && Time.realtimeSinceStartup - hordeFlashStartRealtime >= HordeFlashSeconds)
            {
                hordeDespawned = true;
                DespawnHorde();
            }

            // Flash-Overlay-Text
            if (hordeFlashed && !hordeDespawned)
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = new Color(0.9f, 0.3f, 0.3f, 0.9f);
                Widgets.Label(new Rect(0, screenH * 0.66f, screenW, 40f), "Die Infizierten zeigen sich...");
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            // ── Skip-Button (überspringt das Intro) ─────────────────────
            var skipRect = new Rect(screenW - 190f, screenH - 74f, 170f, 34f);
            if (Widgets.ButtonText(skipRect, "Rimconemy.Intro.Skip".Translate()))
            {
                Close();
                return;
            }

            // ── Auto-Close nach letztem Block + Puffer ──────────────────
            float total = flowTexts.Length * BlockDurationSeconds;
            bool flashDoneOrUnavailable = hordeDespawned || SpawnHordePawn == null;
            if (elapsed >= total + AutoCloseBufferSeconds && flashDoneOrUnavailable)
            {
                Close();
            }
        }

        private string GetStartingPawnName()
        {
            if (Current.Game?.InitData?.startingAndOptionalPawns != null)
            {
                foreach (var p in Current.Game.InitData.startingAndOptionalPawns)
                {
                    if (p is Pawn pawn && pawn != null)
                        return pawn.Name?.ToStringShort ?? "Überlebender";
                }
            }
            return "Überlebender";
        }

        private void FlashHorde()
        {
            var map = Find.AnyPlayerHomeMap;
            if (map == null) return;

            // Use callback for spawning (provided by Paket 05)
            if (SpawnHordePawn != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    var cell = CellFinder.RandomEdgeCell(map);
                    var faction = Find.FactionManager?.FirstFactionOfDef(
                        DefDatabase<FactionDef>.GetNamedSilentFail("Rimconemy_HiddenInfectedFaction"));
                    var kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail("Rimconemy_InfectedRavager");

                    Pawn pawn = null;
                    try
                    {
                        pawn = SpawnHordePawn?.Invoke(
                            "Rimconemy_InfectedRavager",
                            faction,
                            kindDef,
                            cell,
                            map);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning(
                            $"[IntroFlowWindow] horde spawn callback failed: {ex.GetType().Name}: {ex.Message}");
                    }
                    if (pawn != null)
                    {
                        pawn.mindState.duty = null;
                        tempHordePawns.Add(pawn);

                        // Kamera auf ersten Pawn
                        if (i == 0 && Find.CameraDriver != null)
                            Find.CameraDriver.JumpToCurrentMapLoc(cell);
                    }
                }
            }
            else
            {
                // Fallback: no callback provided
                Log.Warning("[IntroFlowWindow] SpawnHordePawn callback not set — horde flash skipped");
            }
        }

        private void DespawnHorde()
        {
            foreach (var p in tempHordePawns.Where(p => !p.Destroyed))
            {
                if (DespawnPawn != null) DespawnPawn(p);
                else p.Destroy(DestroyMode.Vanish);
            }
            tempHordePawns.Clear();
        }

        private void CleanupHorde()
        {
            DespawnHorde();
        }
    }
}
