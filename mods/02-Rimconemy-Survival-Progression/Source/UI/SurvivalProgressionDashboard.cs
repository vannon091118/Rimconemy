using Rimconemy.Foundation.Registry;
using Rimconemy.Foundation.UI;
using Rimconemy.SurvivalProgression.Progression;
using UnityEngine;
using Verse;

namespace Rimconemy.SurvivalProgression.UI
{
    /// <summary>
    /// Survival &amp; Progression Dashboard.
    ///
    /// Honest Mutation-Pfad-Sektion (Phase-2.6 / 2026-08-04): Paket 02 hat drei
    /// reale Mutations-Pfade, die hier ehrlich markiert werden, statt mit
    /// einem pauschalen "READ-ONLY":
    ///
    ///   - CharacterSetup.Apply: MUTATING (ApplyAndCountAgeChanges +
    ///     ApplyToAllStartingPawns + ForceAge18 + DistributeSkillBudget +
    ///     TraitAssigner.AssignTraitsForBudget). Schreibvorgang in
    ///     BirthAbsTicks, Skills, Traits. Save-Persistiert via
    ///     CharacterSetupState GameComponent (Scribe-Pfad).
    ///
    ///   - XP-Aggregation: LIVE (ProgressionGameComponent.GameComponentTick
    ///     alle 250 Ticks; UpdateRuntimeState → UpdatePawn → UpdateWorkEpisode
    ///     → snapshots.Experience += awarded-XP; Scribe-Persistenz über
    ///     ProgressionGameComponent.ExposeData).
    ///
    ///   - Game-Over-Trigger: SOLE-OWNER-LIVE (F-V2: Paket 02 ist der EINZIGE
    ///     Caller von Find.GameEnder.CheckOrUpdateGameOver; spiegelt zugleich
    ///     CrossPackageState.TryReadStoryGameOverPending via die late-bound
    ///     reflection bridge über Paket 05).
    ///
    /// Ehrlicher Capability-Vergleich: direkt unter dem Banner steht die
    /// Liste der Foundation- und Paket-02-Capabilities mit LIVE/OFF/GATED-
    /// Markern. Lookup über PackageRegistry.HasCapability (NICHT die
    /// CapabilityAudit.HasCapabilityOrWarn — die würde pro Frame eine
    /// Warning werfen, was den Log zumüllt).
    /// </summary>
    public sealed class SurvivalProgressionDashboard : RimconemyMainTabWindow
    {
        private Vector2 _scrollPosition;

        // Mission-critical labels used in the LIVE-banner and capabilities
        // table. Centralised so future i18n passes can hit one place.
        private const string MutationBannerLabel =
            "MUTATING  ·  LIVE  ·  SOLE-OWNER-LIVE  ·  Paket-02 Mutations-Pfade";
        private const string MutationBannerDetail =
            "CharacterSetup.Apply: MUTATING (Bio-Remap ApplyAndCountAgeChanges + " +
            "ApplyToAllStartingPawns + ForceAge18 + DistributeSkillBudget + " +
            "TraitAssigner.AssignTraitsForBudget). XP-Aggregation: LIVE " +
            "(ProgressionGameComponent.GameComponentTick @ 250-tick). " +
            "Game-Over: SOLE-OWNER-LIVE (F-V2: einziger Caller von " +
            "Find.GameEnder.CheckOrUpdateGameOver). " +
            "OPEN: vollständiger Save/Load-Live-Gate-Verifikation steht aus " +
            "(Audit §B6).";

        public override Vector2 InitialSize => new Vector2(720f, 720f);

        public override void DoWindowContents(Rect inRect)
        {
            var component = Current.Game?.GetComponent<ProgressionGameComponent>();
            if (component == null)
            {
                RimconemyUi.DrawEmptyState(inRect, "Rimconemy.Survival.Unavailable");
                return;
            }

            float width = inRect.width - RimconemyTheme.DefaultScrollbarWidth;
            int count = component.Snapshots?.Count ?? 0;
            float cardHeight = 122f;
            int columns = inRect.width - RimconemyTheme.DefaultScrollbarWidth >= 680f ? 4 : 2;
            int cardRows = count > 0 ? Mathf.CeilToInt(count / (float)columns) : 1;

            // Mutation-Banner (3 Zeilen) + Section-Spacing + Capabilities-
            // Sections-Titel + 9 Capabilities-Zeilen + Section-Spacing.
            // cardStartY-Offsets:
            //   4 Spalten: 462f (alt 226f + 236f für neue Sektion)
            //   2 Spalten: 534f (alt 298f + 236f für neue Sektion)
            const float capabilitiesSectionHeight = 236f;
            float cardStartY = (columns == 4 ? 226f : 298f) + capabilitiesSectionHeight;
            float contentHeight = cardStartY
                                + (count == 0 ? 80f : cardRows * cardHeight)
                                + RimconemyTheme.Margin * 2f;
            var outer = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            var view = new Rect(0f, 0f, width, contentHeight);
            Widgets.BeginScrollView(outer, ref _scrollPosition, view);

            float y = 0f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, width, 32f), "Rimconemy · Survival & Progression");
            y += 36f;

            // EHRLICHER MUTATIONS-PFAD-BANNER (Phase-2.6):
            // Ersetzt die READ-ONLY/Pseudo-LIVE-Behauptung der vorigen
            // Iteration. Drei Standard-Marker (MUTATING, LIVE, SOLE-OWNER-LIVE
            // und OPEN) sind gleichzeitig präsent, was FoundationHonestBannerAudit
            // als reichhaltigen Banner erkennt.
            RimconemyUi.DrawFeatureStatus(
                new Rect(0f, y, width, RimconemyTheme.RowHeight * 3f + 6f),
                MutationBannerLabel,
                MutationBannerDetail,
                StatusLevel.Success);
            y += RimconemyTheme.RowHeight * 3f + 6f + RimconemyTheme.SectionSpacing;

            // EHRLICHER CAPABILITY-VERGLEICH (Phase-2.6):
            // Zeigt pro Capability, ob sie registriert und version-konform
            // exposiert ist. Direkter PackageRegistry-Lookup ohne
            // HasCapabilityOrWarn (würde Frame-Spam verursachen).
            Text.Font = GameFont.Small;
            GUI.color = RimconemyTheme.HeaderInk;
            Widgets.Label(new Rect(0f, y, width, 22f), "AKTIVE FOUNDATION-CAPABILITIES");
            GUI.color = Color.white;
            y += 26f;
            y = DrawCapabilitiesSection(y, width);
            y += RimconemyTheme.SectionSpacing;

            Text.Font = GameFont.Tiny;
            GUI.color = RimconemyTheme.Muted;
            Widgets.Label(new Rect(0f, y, width, 18f),
                "Kolonisten: " + count + "  ·  Schema v" + ProgressionGameComponent.CurrentSchemaVersion
                + "  ·  Update: Tick " + component.LastUpdateTick);
            GUI.color = Color.white;
            y += 24f;

            // GAME-OVER-SPIEGEL (Phase-2.6): zeigt zusätzlich zum Banner den
            // aktuellen Live-Zustand aus ProgressionGameComponent
            // (GameOverTriggered + GameOverReason). Damit ist der SOLE-OWNER-
            // Pfad zur Laufzeit sichtbar statt nur deklarativ im Marker-Banner.
            StatusLevel loopStatus = component.GameOverTriggered
                ? StatusLevel.Error
                : component.HasObservedPlayerColonist ? StatusLevel.Success : StatusLevel.Warn;
            string loopLabel = component.GameOverTriggered
                ? "! GAME OVER  " + (component.GameOverReason ?? "")
                : component.HasObservedPlayerColonist ? "OK  Survival loop active" : "– Waiting for first colonist";
            RimconemyUi.DrawStatusBadge(new Rect(0f, y, width, 24f), loopLabel, loopStatus);
            y += 32f;

            float cardWidth = (width - (columns - 1) * RimconemyTheme.Margin) / columns;
            DrawHeaderCard(0, y, cardWidth, "#", "Kolonisten", count.ToString(),
                count > 0 ? StatusLevel.Success : StatusLevel.Warn, columns);
            DrawHeaderCard(1, y, cardWidth, "↻", "Social", component.RecreationAvailable ? "Aktiv" : "Wartet",
                component.RecreationAvailable ? StatusLevel.Success : StatusLevel.Warn, columns);
            DrawHeaderCard(2, y, cardWidth, "⌁", "Forschung", component.ResearchCapabilities.Count.ToString(), StatusLevel.Info, columns);
            DrawHeaderCard(3, y, cardWidth, "T", "Letzter Tick", component.LastUpdateTick.ToString(), StatusLevel.Info, columns);
            y += columns == 4 ? 72f : 144f;

            if (count == 0)
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 80f), "Rimconemy.Survival.Empty");
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var snapshot = component.Snapshots[i];
                    if (snapshot == null) continue;
                    int column = i % columns;
                    int row = i / columns;
                    DrawSnapshot(snapshot, new Rect(column * (cardWidth + RimconemyTheme.Margin), y + row * cardHeight, cardWidth, cardHeight));
                }
            }

            Widgets.EndScrollView();
            RimconemyUi.ResetTextFontAndColor();
        }

        private static void DrawHeaderCard(int index, float y, float cardWidth, string icon, string label,
            string value, StatusLevel level, int columns)
        {
            int row = index / columns;
            int column = index % columns;
            float x = column * (cardWidth + RimconemyTheme.Margin);
            RimconemyUi.DrawStatCard(new Rect(x, y + row * 72f, cardWidth, 60f), icon, label, value, -1f, level);
        }

        private static void DrawSnapshot(ProgressionSnapshot snapshot, Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 7f, rect.width - 16f, 22f),
                (snapshot.PawnLabel ?? "—") + "  —  " + (snapshot.WorkDomain ?? "Unassigned"));
            Text.Font = GameFont.Small;
            float y = rect.y + 32f;
            DrawNeed("Nahrung", snapshot.NeedFoodLevel, new Rect(rect.x + 8f, y, rect.width - 16f, 18f));
            y += 20f;
            DrawNeed("Sicherheit", snapshot.NeedSafetyLevel, new Rect(rect.x + 8f, y, rect.width - 16f, 18f));
            y += 20f;
            DrawNeed("Sozial", snapshot.NeedSocialLevel, new Rect(rect.x + 8f, y, rect.width - 16f, 18f));
            y += 20f;
            float xp = (snapshot.Experience % 100f) / 100f;
            RimconemyUi.DrawNeedBar(new Rect(rect.x + 8f, y, rect.width - 16f, 9f), xp,
                xp >= 0.7f ? RimconemyTheme.Success : RimconemyTheme.Info);
            GUI.color = RimconemyTheme.Muted;
            Widgets.Label(new Rect(rect.x + 8f, y + 10f, rect.width - 16f, 18f),
                "XP " + snapshot.Experience.ToString("0.0") + "  ·  L" + snapshot.Level.ToString("0.0")
                + "  ·  Effizienz " + snapshot.Efficiency.ToString("P0"));
            GUI.color = Color.white;
        }

        private static void DrawNeed(string label, float value, Rect rect)
        {
            RimconemyUi.DrawNeedBar(rect, value, value >= 0.65f ? RimconemyTheme.Success : value >= 0.35f ? RimconemyTheme.Warn : RimconemyTheme.Error, label + "  " + value.ToString("P0"));
        }

        /// <summary>
        /// PHASE-2.6 (2026-08-04): Renders one row per Foundation / Paket-02
        /// capability with a LIVE/OFF/GATED badge. Reads
        /// <see cref="PackageRegistry.HasCapability"/> directly to AVOID the
        /// once-warning emission from <see cref="CapabilityAudit.HasCapabilityOrWarn"/>
        /// (which would otherwise log every redraw, polluting the operator
        /// log with one warning per missing capability per second).
        ///
        /// Note: Paket-02 itself is SHIPPING (we are inside it); therefore all
        /// Paket-02 capabilities show as LIVE on every draw. Paket-05 is
        /// flagged GATED so the operator immediately sees whether the
        /// XP-Multiplier bridge through <c>rimconemy.infectedautomation.threat</c>
        /// is wired.
        /// </summary>
        private static float DrawCapabilitiesSection(float y, float width)
        {
            // 9 Capabilities we want to surface; the order matters:
            //oundation core (5) → Paket-02 (3) → Gated Paket-05 (1).
            string[,] caps = new string[,]
            {
                { "rimconemy.foundation",            "rimconemy.foundation.profile",                       "profile" },
                { "rimconemy.foundation",            "rimconemy.foundation.colonials",                     "colonials" },
                { "rimconemy.foundation",            "rimconemy.foundation.eventlog",                      "eventlog" },
                { "rimconemy.foundation",            "rimconemy.foundation.save_diagnosis",                "save_diagnosis" },
                { "rimconemy.foundation",            "rimconemy.foundation.dlc_filter",                    "dlc_filter" },
                { "rimconemy.survivalprogression",   "rimconemy.survivalprogression.needs",               "needs" },
                { "rimconemy.survivalprogression",   "rimconemy.survivalprogression.progression",         "progression" },
                { "rimconemy.survivalprogression",   "rimconemy.survivalprogression.gameover",             "gameover (SOLE-OWNER)" },
                { "rimconemy.infectedautomation",    "rimconemy.infectedautomation.threat",                "threat (XP-Multiplier)" },
            };

            const float rowHeight = 22f;
            float badgeWidth = Mathf.Max(180f, width * 0.30f);
            float labelWidth = Mathf.Max(160f, width - badgeWidth - 12f);

            for (int i = 0; i < caps.GetLength(0); i++)
            {
                string pkgId = caps[i, 0];
                string capId = caps[i, 1];
                string shortTag = caps[i, 2];

                bool available;
                // No try/catch: PackageRegistry.HasCapability only does a
                // lock-guarded Dictionary lookup and an OrderBy/Any walk
                // over a small list. It does not throw under any realistic
                // condition. Phase-2.6 code-review follow-up kept the call
                // bare for the same reason the once-warning helper was
                // rejected for dashboard display: dead-code exception
                // handling hides bugs instead of surfacing them.
                // (FoundationWindowFallbackTests.SafeCall antipattern avoided.)
                available = PackageRegistry.HasCapability(pkgId, capId, 1);

                StatusLevel status;
                string badgeText;
                bool isMod05Capability = pkgId == "rimconemy.infectedautomation";
                if (available)
                {
                    status = isMod05Capability ? StatusLevel.Success : StatusLevel.Success;
                    badgeText = "LIVE  ·  " + shortTag;
                }
                else
                {
                    status = isMod05Capability ? StatusLevel.Muted : StatusLevel.Warn;
                    badgeText = isMod05Capability
                        ? "GATED  ·  Paket 05 fehlt"
                        : "OFF  ·  nicht exposiert";
                }

                Text.Font = GameFont.Tiny;
                GUI.color = RimconemyTheme.Muted;
                Widgets.Label(new Rect(0f, y + 4f, labelWidth, 18f), capId);
                GUI.color = Color.white;
                Text.Font = GameFont.Tiny;
                RimconemyUi.DrawStatusBadge(new Rect(labelWidth, y, badgeWidth, 22f),
                    badgeText, status);
                y += rowHeight;
            }

            return y;
        }
    }
}
