using System.Collections.Generic;
using Rimconemy.Foundation.UI;
using Rimconemy.InfectedAutomation.Story;
using UnityEngine;
using Verse;

namespace Rimconemy.InfectedAutomation.UI
{
    /// <summary>
    /// P1 read-only threat surface. It exposes only persisted/public StoryDirector
    /// state; raid selection and execution remain owned by StoryDirector.
    ///
    /// §8.3 UI-Read-Model: shows last selection reason (LastSelectionReason),
    /// last event ID (StoryState.LastEventId), cooldown overview and a
    /// ThreatPressure sparkline (ThreatHistory).
    /// </summary>
    public sealed class ThreatDashboard : RimconemyMainTabWindow
    {
        private Vector2 _scrollPosition;

        public override Vector2 InitialSize => new Vector2(660f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            var director = StoryDirector.Get();
            if (director == null)
            {
                RimconemyUi.DrawEmptyState(inRect, "Rimconemy.Infected.Unavailable");
                return;
            }

            float width = inRect.width - RimconemyTheme.DefaultScrollbarWidth;
            // Estimate content height: header + pressure + snapshot + sections
            int cooldownCount = director.State?.EventCooldowns?.Count ?? 0;
            float contentHeight = 34f + 34f + RimconemyTheme.RowHeight * 2f + 6f + RimconemyTheme.SectionSpacing
                                + 62f + 56f   // header + badge + capability banner + pressure + snapshot
                                + 32f + 24f + 24f          // State section rows
                                + 32f + 30f                // Sparkline section
                                + 80f                      // Sparkline height
                                + 32f + 30f                // Selection reason section
                                + 44f                      // reason text
                                + 32f + 30f + (cooldownCount > 0 ? cooldownCount * 24f + 4f : 44f)  // cooldowns
                                + 32f + 30f + 48f + 48f;   // Pending raid section

            var view = new Rect(0f, 0f, width, Mathf.Max(contentHeight, 400f));
            Widgets.BeginScrollView(inRect, ref _scrollPosition, view);
            float y = 0f;

            // ── Header ───────────────────────────────────────────
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, width, 28f), "Rimconemy · Bedrohung & Story");
            y += 34f;

            SettingProfile profile = director.ActiveProfile;
            RimconemyUi.DrawStatusBadge(new Rect(0f, y, width, 24f),
                "Profil: " + (profile?.Label ?? "nicht verfügbar"),
                profile == SettingProfile.Collapse ? StatusLevel.Error : StatusLevel.Info);
            y += 34f;
            RimconemyUi.DrawFeatureStatus(
                new Rect(0f, y, width, RimconemyTheme.RowHeight * 2f + 6f),
                "READ-ONLY · Story-/Threat-Snapshot",
                "OPEN · echter Raid-Spawn und vollständige Eventauflösung sind noch nicht belegt.",
                StatusLevel.Warn);
            y += RimconemyTheme.RowHeight * 2f + 6f + RimconemyTheme.SectionSpacing;

            if (Prefs.DevMode)
            {
                if (Widgets.ButtonText(new Rect(0f, y, width, 26f), "⚡ Dev: Story-Auswertung jetzt ausführen"))
                {
                    director.EvaluateNow(Find.TickManager?.TicksGame ?? 0L);
                }
                y += 30f;
            }


            // ── Threat pressure gauge ────────────────────────────
            var snapshot = director.LastSnapshot;
            if (snapshot != null)
            {
                RimconemyUi.DrawPressureGauge(new Rect(0f, y, width, 34f),
                    snapshot.ThreatPressure, "Bedrohungspegel");
                y += 62f;
                RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Überlebende", snapshot.SurvivorCount.ToString());
                y += 24f;
                RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Kritische Ressourcen",
                    snapshot.AnyResourceCritical ? string.Join(", ", snapshot.CriticalResourceIds ?? new List<string>()) : "Keine",
                    snapshot.AnyResourceCritical ? RimconemyTheme.Warn : RimconemyTheme.Success);
                y += 28f;
            }
            else
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 56f), "Rimconemy.Infected.NoSnapshot");
                y += 64f;
            }

            // ── Sparkline: Bedrohungsverlauf ────────────────────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infected.ThreatHistory", GameFont.Medium);
            y += 30f;
            if (director.ThreatHistory != null && director.ThreatHistory.Count >= 2)
            {
                RimconemyUi.DrawSparkline(new Rect(0f, y, width, 72f), director.ThreatHistory,
                    RimconemyTheme.Error);
            }
            else
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 72f), "Rimconemy.Infected.NoHistory");
            }
            y += 80f;

            // ── §8.3 UI-Read-Model: Letzter Auswahlgrund ────────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infected.SelectionReason", GameFont.Medium);
            y += 30f;

            string lastEventId = director.State?.LastEventId;
            long lastEventTick = director.State?.LastEventTick ?? 0L;
            int totalSelected = director.State?.TotalEventsSelected ?? 0;

            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f),
                "Letztes Event", string.IsNullOrEmpty(lastEventId) ? "Noch kein Event" : lastEventId,
                string.IsNullOrEmpty(lastEventId) ? RimconemyTheme.Muted : RimconemyTheme.Info);
            y += 24f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f),
                "Bei Tick", lastEventTick > 0 ? lastEventTick.ToString() : "—");
            y += 24f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f),
                "Events gesamt", totalSelected.ToString());
            y += 28f;

            if (!string.IsNullOrEmpty(director.LastSelectionReason))
            {
                GUI.color = RimconemyTheme.Muted;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(0f, y, width, 44f), director.LastSelectionReason);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                y += 48f;
            }
            else
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 36f), "Rimconemy.Infected.NoReason");
                y += 40f;
            }

            // ── Cooldowns ───────────────────────────────────────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infected.Cooldowns", GameFont.Medium);
            y += 30f;
            var cooldowns = director.State?.EventCooldowns;
            long currentTick = Find.TickManager?.TicksGame ?? 0L;
            if (cooldowns != null && cooldowns.Count > 0)
            {
                foreach (var pair in cooldowns)
                {
                    long remaining = pair.Value - currentTick;
                    StatusLevel lvl = remaining > 0 ? StatusLevel.Warn : StatusLevel.Success;
                    string label = pair.Key + ":  " + (remaining > 0
                        ? (remaining / Rimconemy.Foundation.TimeConstants.TicksPerDay).ToString("0.0") + "d"
                        : "Abgelaufen");
                    RimconemyUi.DrawStatusBadge(new Rect(0f, y, width, 22f), label, lvl);
                    y += 24f;
                }
            }
            else
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 40f), "Rimconemy.Infected.NoCooldowns");
                y += 44f;
            }

            // ── State rows ──────────────────────────────────────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infected.State", GameFont.Medium);
            y += 30f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Letzte Auswertung", "Tick " + director.LastEvaluationTick);
            y += 24f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Wipe-Prüfung", "Tick " + director.LastWipeCheckTick);
            y += 24f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Story-Zustand", director.State == null ? "nicht verfügbar" : "aktiv",
                director.State == null ? RimconemyTheme.Warn : RimconemyTheme.Success);
            y += 28f;

            if (profile?.AllowedEventFamilies != null)
            {
                GUI.color = RimconemyTheme.Muted;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(0f, y, width, 22f), "Erlaubte Familien: " + string.Join(", ", profile.AllowedEventFamilies));
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                y += 24f;
            }

            // ── Pending raid ────────────────────────────────────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infected.NextRaid", GameFont.Medium);
            y += 30f;
            if (!string.IsNullOrEmpty(director.PendingIncidentDefName))
            {
                RimconemyUi.DrawStatusBadge(new Rect(0f, y, width, 24f),
                    "! " + (director.PendingEventLabel ?? director.PendingIncidentDefName), StatusLevel.Error);
                y += 28f;
                Widgets.Label(new Rect(0f, y, width, 42f), director.PendingEventText ?? "");
                y += 48f;
            }
            else
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 40f), "Rimconemy.Infected.NoPendingRaid");
                y += 46f;
            }

            Widgets.EndScrollView();
            RimconemyUi.ResetTextFontAndColor();
        }
    }
}
