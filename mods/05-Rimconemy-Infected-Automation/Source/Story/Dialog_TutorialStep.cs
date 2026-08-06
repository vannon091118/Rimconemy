using System.Collections.Generic;
using System.Linq;
using Rimconemy.Foundation.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.Story
{
    /// <summary>
    /// Dialog for displaying a Tutorial step with Portrait, Text, Unlock preview,
    /// step progress and an objective line.
    /// Owner: Infected & Automation (Paket 05).
    ///
    /// UX-Audit 2026-08-06:
    ///   * Soft-Lock-Fix: JEDER Schließpfad (X, Esc, VERSTANDEN) markiert den
    ///     Schritt als abgeschlossen via <see cref="PreClose"/>. Vorher blockierte
    ///     ein X-Schließen alle Folge-Schritte (CurrentStepIndex blieb >= 0).
    ///   * Schritt-Fortschritt („Schritt 2 von 4"), Zielzeile (objective),
    ///     scrollbarer Text und Übersetzungsschlüssel statt Hardcode-Strings.
    ///   * „Tutorial überspringen" markiert alle verbleibenden Schritte als
    ///     abgeschlossen.
    /// </summary>
    public class Dialog_TutorialStep : Window
    {
        private readonly RimconemyTutorialLetter letter;
        private Vector2 scrollPos;
        private bool stepCompletedOnClose;

        public Dialog_TutorialStep(RimconemyTutorialLetter letter)
        {
            this.letter = letter;
            forcePause = true;
            closeOnCancel = true;
            preventCameraMotion = false;
            draggable = true;
            doCloseX = true;
            doCloseButton = false;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(640f, 560f);

        public override void PreClose()
        {
            base.PreClose();
            // Soft-Lock-Fix: egal wie geschlossen wird — Schritt gilt als gesehen.
            if (!stepCompletedOnClose && letter != null && !string.IsNullOrEmpty(letter.StepId))
            {
                stepCompletedOnClose = true;
                TutorialDirector.Get()?.MarkStepCompleted(letter.StepId);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            const float Pad = 18f;
            float x = inRect.x + Pad;
            float width = inRect.width - Pad * 2f;
            float y = inRect.y + Pad;

            // ── Kopfzeile: Fortschritt + Titel ──────────────────────────
            if (letter.TotalSteps > 0)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = RimconemyTheme.Info;
                Widgets.Label(new Rect(x, y, width, 20f),
                    "Rimconemy.Infected.Tutorial.StepOf".Translate(letter.StepNumber, letter.TotalSteps));
                GUI.color = Color.white;
                y += 22f;
            }

            Text.Font = GameFont.Medium;
            GUI.color = RimconemyTheme.HeaderInk;
            string title = string.IsNullOrEmpty(letter.Label)
                ? (letter.StepId ?? "?")
                : letter.Label;
            Widgets.Label(new Rect(x, y, width, 36f), title);
            GUI.color = Color.white;
            y += 40f;

            // ── Ziel-Zeile (objective) ──────────────────────────────────
            if (!string.IsNullOrEmpty(letter.Objective))
            {
                Text.Font = GameFont.Small;
                GUI.color = RimconemyTheme.Warn;
                Widgets.Label(new Rect(x, y, width, 26f),
                    "Rimconemy.Infected.Tutorial.Objective".Translate() + ": " + letter.Objective);
                GUI.color = Color.white;
                y += 30f;
            }

            // ── Portrait links ───────────────────────────────────────────
            var portraitRect = new Rect(x, y, 128, 128);
            var portrait = letter.Portrait
                ?? ContentFinder<Texture2D>.Get("UI/HeroArt/Storytellers/RimconemyLarge", false);
            if (portrait != null)
            {
                GUI.color = RimconemyTheme.PanelInk;
                Widgets.DrawBoxSolid(portraitRect, RimconemyTheme.PanelInk);
                GUI.color = Color.white;
                GUI.DrawTexture(new Rect(portraitRect.x + 4, portraitRect.y + 4,
                    portraitRect.width - 8, portraitRect.height - 8), portrait, ScaleMode.ScaleToFit);
                GUI.color = RimconemyTheme.DividerInk;
                Widgets.DrawBox(portraitRect);
                GUI.color = Color.white;
            }

            // ── Text rechts (scrollbar) ──────────────────────────────────
            float textX = portraitRect.xMax + 16f;
            float textWidth = inRect.xMax - Pad - textX;
            float textBottom = inRect.yMax - 90f;
            if (portrait == null)
            {
                textX = x;
                textWidth = width;
                textBottom = inRect.yMax - 70f;
            }

            var textOuter = new Rect(textX, y, textWidth, textBottom - y);
            Text.Font = GameFont.Small;
            float textHeight = Text.CalcHeight(letter.Text ?? string.Empty, textOuter.width - 18f);
            Widgets.BeginScrollView(textOuter, ref scrollPos, new Rect(0, 0, textOuter.width - 18f, textHeight));
            Widgets.Label(new Rect(0, 0, textOuter.width - 18f, textHeight), letter.Text ?? string.Empty);
            Widgets.EndScrollView();

            // ── Unlock-Vorschau ──────────────────────────────────────────
            float unlockY = portrait != null ? portraitRect.yMax + 14f : textBottom + 10f;
            var unlocks = letter.UnlockDefs?.Where(d => d != null).ToList() ?? new List<Def>();
            if (unlocks.Count > 0)
            {
                Text.Font = GameFont.Tiny;
                GUI.color = RimconemyTheme.Muted;
                Widgets.Label(new Rect(x, unlockY, width, 20f),
                    "Rimconemy.Infected.Tutorial.Unlocks".Translate());
                GUI.color = Color.white;

                Text.Font = GameFont.Small;
                float uy = unlockY + 22f;
                int shown = 0;
                foreach (var def in unlocks)
                {
                    if (shown >= 4) break;
                    GUI.color = RimconemyTheme.Info;
                    Widgets.Label(new Rect(x + 12f, uy, width - 12f, 24f), "▸ " + def.LabelCap);
                    GUI.color = Color.white;
                    uy += 26f;
                    shown++;
                }
            }

            // ── Buttons unten ────────────────────────────────────────────
            float btnY = inRect.yMax - 58f;

            var skipRect = new Rect(x, btnY, 200f, 40f);
            if (Widgets.ButtonText(skipRect, "Rimconemy.Infected.Tutorial.SkipAll".Translate()))
            {
                TutorialDirector.Get()?.SkipAllTutorials();
                Close();
            }

            var okRect = new Rect(inRect.xMax - Pad - 200f, btnY, 200f, 40f);
            if (Widgets.ButtonText(okRect, "Rimconemy.Infected.Tutorial.Understand".Translate()))
            {
                Close();
            }

            // RimPad-Button (nur, wenn ein Guide-Inhalt registriert ist)
            if (RimPadWindow.GuideTabDrawer != null)
            {
                var rimPadRect = new Rect(x, btnY + 46f, 200f, 36f);
                if (Widgets.ButtonText(rimPadRect, "Rimconemy.Infected.Tutorial.OpenRimPad".Translate()))
                {
                    RimPadWindow.OpenGuide();
                }
            }
        }
    }
}
