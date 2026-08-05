using System.Collections.Generic;
using Rimconemy.Foundation.Catalog;
using Rimconemy.Foundation.Events;
using Rimconemy.Foundation.Models;
using Rimconemy.Foundation.Profile;
using Rimconemy.Foundation.Registry;
using Rimconemy.Foundation.Save;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.Foundation.UI
{
    /// <summary>
    /// Owner: Foundation
    /// Main dashboard tab window showing profile, DLCs, packages,
    /// event log, and save diagnosis.
    ///
    /// Hook reason: RimconemyMainTabWindow provides the standard RimWorld bottom-tab
    /// integration. All data comes from read-only snapshots, never from
    /// mutable engine objects directly.
    /// </summary>
    public class FoundationDashboard : RimconemyMainTabWindow
    {
        private Vector2 _scrollPosition;
        private const float SectionSpacing = RimconemyTheme.SectionSpacing;
        private const float IndentSize = RimconemyTheme.IndentSize;
        private bool _profileExpanded = true;
        private bool _dlcExpanded = true;
        private bool _packagesExpanded = true;
        private bool _saveExpanded = true;
        private bool _inventoryExpanded = false;
        private bool _vanillaExpanded = false;
        private bool _eventsExpanded = true;

        private int _selectedHubTab = 0;
        private MainTabWindow _survivalWindow;
        private MainTabWindow _infrastructureWindow;
        private MainTabWindow _economyWindow;
        private MainTabWindow _threatWindow;
        private MainTabWindow _phaseProgressWindow;

        public override Vector2 InitialSize => new Vector2(720f, 620f);

        private static string T(string key)
        {
            return key.CanTranslate() ? key.Translate().ToString() : key;
        }

        private static System.Type FindType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private MainTabWindow GetSubWindow(int tabIndex)
        {
            switch (tabIndex)
            {
                case 1:
                    if (_survivalWindow == null)
                    {
                        var t = FindType("Rimconemy.SurvivalProgression.UI.SurvivalProgressionDashboard");
                        if (t != null) _survivalWindow = (MainTabWindow)System.Activator.CreateInstance(t);
                    }
                    return _survivalWindow;
                case 2:
                    if (_infrastructureWindow == null)
                    {
                        var t = FindType("Rimconemy.ScavengerInfrastructure.UI.InfrastructureDashboard");
                        if (t != null) _infrastructureWindow = (MainTabWindow)System.Activator.CreateInstance(t);
                    }
                    return _infrastructureWindow;
                case 3:
                    if (_economyWindow == null)
                    {
                        var t = FindType("Rimconemy.EconomyTerritory.Wallet.EconomyHub");
                        if (t != null) _economyWindow = (MainTabWindow)System.Activator.CreateInstance(t);
                    }
                    return _economyWindow;
                case 4:
                    if (_threatWindow == null)
                    {
                        var t = FindType("Rimconemy.InfectedAutomation.UI.ThreatDashboard");
                        if (t != null) _threatWindow = (MainTabWindow)System.Activator.CreateInstance(t);
                    }
                    return _threatWindow;
                case 5:
                    // Phase-Progress overlay (Mod-02 surface owner; reflection-routed, PHASE_PROGRESSION_CONTRACT §10).
                    if (_phaseProgressWindow == null)
                    {
                        var t = FindType("Rimconemy.SurvivalProgression.Phase.PhaseProgressWindow");
                        if (t != null) _phaseProgressWindow = (MainTabWindow)System.Activator.CreateInstance(t);
                    }
                    return _phaseProgressWindow;
                default:
                    return null;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float width = inRect.width - RimconemyTheme.Margin * 2f;
            float y = inRect.y;

            // ── Top Navigation Tabs ───────────────────────────────────
            var tabLabels = new List<string>
            {
                RimconemyUi.T("Rimconemy.Hub.Tab.Colony"),
                RimconemyUi.T("Rimconemy.Hub.Tab.Survival"),
                RimconemyUi.T("Rimconemy.Hub.Tab.Infrastructure"),
                RimconemyUi.T("Rimconemy.Hub.Tab.Economy"),
                RimconemyUi.T("Rimconemy.Hub.Tab.Threat"),
                // Phase-Progress overlay (PHASE_PROGRESSION_CONTRACT §10), routed via GetSubWindow(5).
                RimconemyUi.T("Rimconemy.Hub.Tab.Phase")
            };

            int newTab = RimconemyUi.DrawTabs(new Rect(inRect.x, y, inRect.width, 32f), tabLabels, _selectedHubTab);
            if (newTab != _selectedHubTab)
            {
                _selectedHubTab = newTab;
                _scrollPosition = Vector2.zero;
            }
            y += 36f;

            // ── Vanilla Quick-Navigation Toolbar ─────────────────────
            DrawVanillaQuickNav(new Rect(inRect.x, y, inRect.width, 24f));
            y += 28f;

            // ── Content Panel ─────────────────────────────────────────
            var contentRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            if (_selectedHubTab == 0)
            {
                DrawFoundationContent(contentRect);
            }
            else
            {
                var subWindow = GetSubWindow(_selectedHubTab);
                if (subWindow != null)
                {
                    subWindow.DoWindowContents(contentRect);
                }
                else
                {
                    RimconemyUi.DrawEmptyState(contentRect, RimconemyUi.T("RimconemyFoundation.Status.NotInstalled"));
                }
            }
            RimconemyUi.ResetTextFontAndColor();
        }

        private void DrawVanillaQuickNav(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            float btnW = (rect.width - 4f * 4f) / 5f;
            float x = rect.x;

            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height), "🗺️ Weltkarte"))
            {
                DefDatabase<MainButtonDef>.GetNamedSilentFail("World")?.Worker?.InterfaceTryActivate();
            }
            x += btnW + 4f;

            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height), "📜 Quests"))
            {
                DefDatabase<MainButtonDef>.GetNamedSilentFail("Quests")?.Worker?.InterfaceTryActivate();
            }
            x += btnW + 4f;

            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height), "🔬 Forschung"))
            {
                DefDatabase<MainButtonDef>.GetNamedSilentFail("Research")?.Worker?.InterfaceTryActivate();
            }
            x += btnW + 4f;

            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height), "🛠️ Arbeit"))
            {
                DefDatabase<MainButtonDef>.GetNamedSilentFail("Work")?.Worker?.InterfaceTryActivate();
            }
            x += btnW + 4f;

            if (Widgets.ButtonText(new Rect(x, rect.y, btnW, rect.height), "📈 Verlauf"))
            {
                DefDatabase<MainButtonDef>.GetNamedSilentFail("History")?.Worker?.InterfaceTryActivate();
            }
            Text.Font = GameFont.Small;
        }


        private void DrawFoundationContent(Rect inRect)
        {
            var viewRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            float width = viewRect.width - 20f;

            // Calculate total content height dynamically from sections
            float contentHeight = CalcHeaderHeight();
            contentHeight += SectionSpacing;
            contentHeight += CalcProfileHeight();
            contentHeight += SectionSpacing;
            contentHeight += CalcDlcHeight();
            contentHeight += SectionSpacing;
            contentHeight += CalcPackageHeight();
            contentHeight += SectionSpacing;
            contentHeight += CalcSaveHeight();
            contentHeight += SectionSpacing;
            contentHeight += CalcInventoryHeight();
            contentHeight += SectionSpacing;
            contentHeight += CalcVanillaSectionHeight();
            contentHeight += SectionSpacing;
            contentHeight += CalcEventHeight();
            contentHeight += 20f; // bottom padding

            Widgets.BeginScrollView(viewRect, ref _scrollPosition, new Rect(0f, 0f, width, contentHeight));

            float y = 0f;
            y = DrawHeader(0f, y, width);
            y += SectionSpacing;
            y = DrawProfileSection(0f, y, width);
            y += SectionSpacing;
            y = DrawDlcSection(0f, y, width);
            y += SectionSpacing;
            y = DrawPackageSection(0f, y, width);
            y += SectionSpacing;
            y = DrawSaveSection(0f, y, width);
            y += SectionSpacing;
            y = DrawInventorySection(0f, y, width);
            y += SectionSpacing;
            y = DrawVanillaSection(0f, y, width);
            y += SectionSpacing;
            y = DrawEventSection(0f, y, width);

            Widgets.EndScrollView();
        }


        private float DrawProfileSection(float x, float y, float width)
        {
            if (DrawCollapsibleHeader(new Rect(x, y, width, 30f), RimconemyUi.T("RimconemyFoundation.Title"), _profileExpanded))
                _profileExpanded = !_profileExpanded;
            if (!_profileExpanded)
                return y + 30f;
            y += 32f;

            Text.Font = GameFont.Small;

            var profile = ProfileDetector.CurrentProfile;
            string profileText = profile switch
            {
                ProfileStatus.Standalone => RimconemyUi.T("RimconemyFoundation.Profile.Standalone"),
                ProfileStatus.Partial => RimconemyUi.T("RimconemyFoundation.Profile.Partial"),
                ProfileStatus.FullOverhaul => RimconemyUi.T("RimconemyFoundation.Profile.Full"),
                _ => RimconemyUi.T("RimconemyFoundation.Profile.Unknown")
            };

            Color profileColor = profile switch
            {
                ProfileStatus.Standalone => RimconemyTheme.Muted,
                ProfileStatus.Partial => RimconemyTheme.Warn,
                ProfileStatus.FullOverhaul => RimconemyTheme.Success,
                _ => Color.white
            };

            GUI.color = profileColor;
            Widgets.Label(new Rect(x, y, width, 22f),
                $"{RimconemyUi.T("RimconemyFoundation.Profile.Label")}: {profileText}");
            GUI.color = Color.white;
            y += 22f;

            Widgets.Label(new Rect(x, y, width, 22f),
                $"{RimconemyUi.T("RimconemyFoundation.Profile.Packages")}: {PackageRegistry.RegisteredCount} " +
                $"{RimconemyUi.T("RimconemyFoundation.Profile.Loaded")}, " +
                $"{ProfileDetector.MissingPackageIds.Count} {RimconemyUi.T("RimconemyFoundation.Profile.Missing")}");
            y += 22f;

            if (profile != ProfileStatus.FullOverhaul)
            {
                GUI.color = RimconemyTheme.Warn;
                Widgets.Label(new Rect(x, y, width, 40f),
                    RimconemyUi.T("RimconemyFoundation.Profile.IntegrationUnavailable"));
                GUI.color = Color.white;
                y += 42f;
            }

            return y;
        }

        private float DrawDlcSection(float x, float y, float width)
        {
            if (DrawCollapsibleHeader(new Rect(x, y, width, 28f), RimconemyUi.T("RimconemyFoundation.Dlc.Title"), _dlcExpanded))
                _dlcExpanded = !_dlcExpanded;
            if (!_dlcExpanded)
                return y + 28f;
            y += 30f;

            Text.Font = GameFont.Small;

            foreach (var dlc in ProfileDetector.DlcStatuses)
            {
                GUI.color = dlc.IsLoaded ? RimconemyTheme.Success : RimconemyTheme.Error;
                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    $"{dlc.DlcName}: {(dlc.IsLoaded ? RimconemyUi.T("RimconemyFoundation.Status.Active") : RimconemyUi.T("RimconemyFoundation.Status.NotInstalled"))}");
                y += 20f;
            }
            GUI.color = Color.white;

            return y;
        }

        private float DrawPackageSection(float x, float y, float width)
        {
            if (DrawCollapsibleHeader(new Rect(x, y, width, 28f), RimconemyUi.T("RimconemyFoundation.Packages.Title"), _packagesExpanded))
                _packagesExpanded = !_packagesExpanded;
            if (!_packagesExpanded)
                return y + 28f;
            y += 30f;

            Text.Font = GameFont.Small;

            var expectedIds = new[]
            {
                "rimconemy.foundation",
                "rimconemy.survivalprogression",
                "rimconemy.scavengerinfrastructure",
                "rimconemy.economyterritory",
                "rimconemy.infectedautomation",
            };

            foreach (var id in expectedIds)
            {
                bool loaded = PackageRegistry.IsRegistered(id);
                var descriptor = PackageRegistry.GetDescriptor(id);

                GUI.color = loaded ? RimconemyTheme.Success : RimconemyTheme.Muted;
                string version = loaded && descriptor != null
                    ? $"v{descriptor.PackageVersion}"
                    : "";
                string status = loaded
                    ? (descriptor != null && descriptor.ProfileCompatibility == ProfileCompatibility.StandaloneAndFull
                        ? RimconemyUi.T("RimconemyFoundation.Packages.ActiveFull")
                        : RimconemyUi.T("RimconemyFoundation.Packages.ActiveStandalone"))
                    : RimconemyUi.T("RimconemyFoundation.Status.NotInstalled");

                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    $"{id}  {version}  [{status}]");
                y += 20f;
            }
            GUI.color = Color.white;

            return y;
        }

        private float DrawSaveSection(float x, float y, float width)
        {
            if (DrawCollapsibleHeader(new Rect(x, y, width, 28f), RimconemyUi.T("RimconemyFoundation.Save.Title"), _saveExpanded))
                _saveExpanded = !_saveExpanded;
            if (!_saveExpanded)
                return y + 28f;
            y += 30f;

            Text.Font = GameFont.Small;

            var saveData = Current.Game?.GetComponent<FoundationSaveData>();
            if (saveData != null)
            {
                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    $"{RimconemyUi.T("RimconemyFoundation.Save.Schema")}: v{saveData.SchemaVersion} " +
                    $"({RimconemyUi.T("RimconemyFoundation.Save.Current")}: v{FoundationSaveData.CurrentSchemaVersion})");
                y += 20f;

                if (saveData.WasMigrated)
                {
                    GUI.color = RimconemyTheme.Warn;
                    Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 40f),
                        $"{RimconemyUi.T("RimconemyFoundation.Save.MigrationApplied")}: {saveData.MigrationDetail}");
                    GUI.color = Color.white;
                    y += 42f;
                }
                else
                {
                    Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                        RimconemyUi.T("RimconemyFoundation.Save.NoMigration"));
                    y += 20f;
                }
            }
            else
            {
                GUI.color = RimconemyTheme.Muted;
                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    RimconemyUi.T("RimconemyFoundation.Save.NotLoaded"));
                GUI.color = Color.white;
                y += 20f;
            }

            return y;
        }

        // SPIKE: DefInventory section reads only stable Verse.* / Assembly-CSharp surface
        // through FoundationDefInventory; no reflection.
        private float DrawInventorySection(float x, float y, float width)
        {
            if (DrawCollapsibleHeader(new Rect(x, y, width, 28f), RimconemyUi.T("RimconemyFoundation.Inventory.Title"), _inventoryExpanded))
                _inventoryExpanded = !_inventoryExpanded;
            if (!_inventoryExpanded)
                return y + 28f;
            y += 30f;

            Text.Font = GameFont.Small;

            // Lazy one-shot capture. Safe to call repeatedly.
            FoundationDefInventory.EnsureCaptured();
            // Phase-5 (2026-08-05) late-bind Storyteller-Probe: if the static-ctor
            // capture found an empty DefDatabase, re-run here, when every mod's
            // Defs are fully parsed (dashboard runs after game load).
            StorytellerInventory.EnsureCaptured();

            var titleMap = FoundationDefInventory.OwnerTitles;
            var counts = FoundationDefInventory.OwnerDefCounts;
            int totalOwners = FoundationDefInventory.OwnerCount;
            int totalDefs = FoundationDefInventory.TotalDefCount;

            if (!FoundationDefInventory.IsPopulated)
            {
                GUI.color = RimconemyTheme.Muted;
                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    RimconemyUi.T("RimconemyFoundation.Inventory.NotLoaded"));
                GUI.color = Color.white;
                y += 22f;
                return y;
            }

            Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                $"{totalOwners} {RimconemyUi.T("RimconemyFoundation.Inventory.TotalOwners")}, " +
                $"{totalDefs} {RimconemyUi.T("RimconemyFoundation.Inventory.TotalDefs")}");
            y += 22f;

            if (totalOwners == 0)
            {
                GUI.color = RimconemyTheme.Muted;
                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    RimconemyUi.T("RimconemyFoundation.Inventory.Empty"));
                GUI.color = Color.white;
                y += 22f;
                return y;
            }

            // Pre-sort owners by ordinal package id so the listing is stable
            // across runs and reduces user confusion during regressions.
            var sortedOwners = new List<string>(counts.Keys);
            sortedOwners.Sort(System.StringComparer.Ordinal);

            foreach (var owner in sortedOwners)
            {
                string ownerDisplay = owner;
                if (titleMap != null && titleMap.TryGetValue(owner, out var t) && !string.IsNullOrEmpty(t))
                    ownerDisplay = $"{owner} ({t})";

                var perType = counts[owner];
                int sum = 0;
                foreach (var c in perType) sum += c.Value;

                GUI.color = sum > 0 ? RimconemyTheme.Success : RimconemyTheme.Muted;
                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    $"{ownerDisplay}: {sum} defs");
                GUI.color = Color.white;
                y += 22f;

                // Per-def-type breakdown, indented further, sorted by type label
                var sortedTypes = new List<string>(perType.Keys);
                sortedTypes.Sort(System.StringComparer.Ordinal);
                foreach (var typeLabel in sortedTypes)
                {
                    int c = perType[typeLabel];
                    if (c <= 0) continue;
                    Widgets.Label(new Rect(x + IndentSize * 2f, y, width - IndentSize * 2f, 18f),
                        $"  {typeLabel}: {c}");
                    y += 18f;
                }
            }

            return y;
        }

        // SPIKE: VanillaInventory section reads only stable Verse.* / Assembly-CSharp
        // surface through FoundationVanillaInventory; no reflection.
        // P30 partial closure of P25 (API-RESOURCE-01): provides the static
        // Def-surface chat with totals only (category bucketing deferred).
        // Live world state (power, inventories, research progress) stays
        // out of scope here on purpose.
        private float DrawVanillaSection(float x, float y, float width)
        {
            if (DrawCollapsibleHeader(new Rect(x, y, width, 28f), RimconemyUi.T("RimconemyFoundation.VanillaInventory.Title"), _vanillaExpanded))
                _vanillaExpanded = !_vanillaExpanded;
            if (!_vanillaExpanded)
                return y + 28f;
            y += 30f;

            Text.Font = GameFont.Small;

            // Lazy one-shot capture, parallel to DefInventory.
            FoundationVanillaInventory.EnsureCaptured();

            if (!FoundationVanillaInventory.IsPopulated)
            {
                GUI.color = RimconemyTheme.Muted;
                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    RimconemyUi.T("RimconemyFoundation.VanillaInventory.NotLoaded"));
                GUI.color = Color.white;
                y += 22f;
                return y;
            }

            int totalThings = FoundationVanillaInventory.TotalVanillaThingDefs;
            int totalStuff = FoundationVanillaInventory.TotalStuffDefs;
            int nonRimconemyOwners = FoundationVanillaInventory.TotalTrackedDlcIds;

            Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 38f),
                string.Format(RimconemyUi.T("RimconemyFoundation.VanillaInventory.Summary"),
                    totalThings, totalStuff, nonRimconemyOwners));
            y += 40f;

            if (totalThings == 0)
            {
                GUI.color = RimconemyTheme.Muted;
                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    RimconemyUi.T("RimconemyFoundation.VanillaInventory.Empty"));
                GUI.color = Color.white;
                y += 22f;
                return y;
            }

            GUI.color = RimconemyTheme.Warn;
            Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 36f),
                RimconemyUi.T("RimconemyFoundation.VanillaInventory.MissingPower"));
            GUI.color = Color.white;
            y += 38f;

            return y;
        }

        private float DrawEventSection(float x, float y, float width)
        {
            string eventTitle = $"{RimconemyUi.T("RimconemyFoundation.Events.Title")} ({EventLog.StoredCount} {RimconemyUi.T("RimconemyFoundation.Events.Entries")})";
            if (DrawCollapsibleHeader(new Rect(x, y, width, 28f), eventTitle, _eventsExpanded))
                _eventsExpanded = !_eventsExpanded;
            if (!_eventsExpanded)
                return y + 28f;
            y += 30f;

            Text.Font = GameFont.Small;

            if (EventLog.StoredCount == 0)
            {
                Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                    RimconemyUi.T("RimconemyFoundation.Events.Empty"));
                y += 20f;
            }
            else
            {
                // Show up to 20 most recent events without nested scrolling
                int shown = 0;
                const int maxShown = 20;
                foreach (var evt in EventLog.RecentEvents)
                {
                    if (shown >= maxShown) break;

                    GUI.color = evt.Category switch
                    {
                        "Save" => RimconemyTheme.Warn,
                        "Error" => RimconemyTheme.Error,
                        "Diagnostic" => RimconemyTheme.Info,
                        _ => Color.white
                    };

                    StatusLevel eventLevel = evt.Category == "Error" ? StatusLevel.Error
                        : evt.Category == "Save" ? StatusLevel.Warn
                        : evt.Category == "Diagnostic" ? StatusLevel.Info : StatusLevel.Muted;
                    RimconemyUi.DrawStatusBadge(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                        "[" + evt.Category + "] " + evt.EventType + ": " + evt.Message, eventLevel);
                    y += 22f;
                    shown++;
                }

                if (EventLog.StoredCount > maxShown)
                {
                    GUI.color = RimconemyTheme.Muted;
                    Widgets.Label(new Rect(x + IndentSize, y, width - IndentSize, 20f),
                        $"{RimconemyUi.T("RimconemyFoundation.Events.More")}: {EventLog.StoredCount - maxShown}");
                    GUI.color = Color.white;
                    y += 22f;
                }
            }

            return y;
        }

        private float DrawHeader(float x, float y, float width)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(x, y, width * 0.58f, 28f), "RIMCONEMY · KOLONIE");
            Text.Font = GameFont.Small;
            var profile = ProfileDetector.CurrentProfile;
            StatusLevel level = profile == ProfileStatus.FullOverhaul ? StatusLevel.Success
                : profile == ProfileStatus.Partial ? StatusLevel.Warn : StatusLevel.Info;
            RimconemyUi.DrawStatusBadge(new Rect(x + width * 0.60f, y, width * 0.40f, 24f),
                (profile == ProfileStatus.FullOverhaul ? "OK " : "! ") + profile, level);
            y += 34f;

            int loadedDlc = 0;
            foreach (var dlc in ProfileDetector.DlcStatuses)
                if (dlc.IsLoaded) loadedDlc++;
            int defCount = FoundationDefInventory.IsPopulated ? FoundationDefInventory.TotalDefCount : 0;
            float cardWidth = (width - 3f * RimconemyTheme.Margin) / 4f;
            RimconemyUi.DrawStatCard(new Rect(x, y, cardWidth, 62f), "#", "Pakete", PackageRegistry.RegisteredCount + "/5", -1f, level);
            RimconemyUi.DrawStatCard(new Rect(x + cardWidth + RimconemyTheme.Margin, y, cardWidth, 62f), "D", "DLCs", loadedDlc + "/" + ProfileDetector.DlcStatuses.Count, -1f, loadedDlc > 0 ? StatusLevel.Success : StatusLevel.Warn);
            RimconemyUi.DrawStatCard(new Rect(x + (cardWidth + RimconemyTheme.Margin) * 2f, y, cardWidth, 62f), "S", "Schema", "v" + FoundationSaveData.CurrentSchemaVersion, -1f, StatusLevel.Info);
            RimconemyUi.DrawStatCard(new Rect(x + (cardWidth + RimconemyTheme.Margin) * 3f, y, cardWidth, 62f), "⊙", "Defs", defCount.ToString(), -1f, defCount > 0 ? StatusLevel.Success : StatusLevel.Warn);
            return y + 74f;
        }

        private static bool DrawCollapsibleHeader(Rect rect, string title, bool expanded)
        {
            bool clicked = false;
            string prefix = expanded ? "▼  " : "▶  ";
            RimconemyUi.DrawHighlightedInteractable(rect, () => clicked = true, title);
            Text.Font = GameFont.Medium;
            GUI.color = RimconemyTheme.HeaderInk;
            Widgets.Label(rect, prefix + title);
            GUI.color = Color.white;
            return clicked;
        }

        // Height calculators matching the Draw* methods
        private float CalcHeaderHeight() => 34f + 74f;
        private float CalcProfileHeight()
        {
            if (!_profileExpanded) return 30f;
            float height = 32f + 22f + 22f;
            if (ProfileDetector.CurrentProfile != ProfileStatus.FullOverhaul)
                height += 42f;
            return height;
        }
        private float CalcDlcHeight() => _dlcExpanded ? 30f + 5 * 20f : 28f;
        private float CalcPackageHeight() => _packagesExpanded ? 30f + 5 * 20f : 28f;
        private float CalcSaveHeight()
        {
            var saveData = Current.Game?.GetComponent<FoundationSaveData>();
            if (!_saveExpanded) return 28f;
            float baseHeight = 30f + 20f;
            if (saveData != null && saveData.WasMigrated)
                baseHeight += 42f;
            return baseHeight;
        }
        private float CalcVanillaSectionHeight()
        {
            FoundationVanillaInventory.EnsureCaptured();
            if (!_vanillaExpanded) return 28f;
            if (!FoundationVanillaInventory.IsPopulated) return 30f + 22f;
            return 30f + 40f + 22f + 38f;
        }
        private float CalcInventoryHeight()
        {
            if (!_inventoryExpanded) return 28f;
            // Lazy capture-trigger so height matches what will be drawn.
            FoundationDefInventory.EnsureCaptured();
            if (!FoundationDefInventory.IsPopulated) return 30f + 22f;
            if (FoundationDefInventory.OwnerCount == 0) return 30f + 22f + 22f;

            var counts = FoundationDefInventory.OwnerDefCounts;
            float height = 30f + 22f; // title + summary line
            var sortedOwners = new List<string>(counts.Keys);
            sortedOwners.Sort(System.StringComparer.Ordinal);
            foreach (var owner in sortedOwners)
            {
                height += 22f; // owner row
                var perType = counts[owner];
                var sortedTypes = new List<string>(perType.Keys);
                sortedTypes.Sort(System.StringComparer.Ordinal);
                foreach (var typeLabel in sortedTypes)
                {
                    int c = perType[typeLabel];
                    if (c > 0) height += 18f;
                }
            }
            return height;
        }
        private float CalcEventHeight()
        {
            if (!_eventsExpanded) return 28f;
            if (EventLog.StoredCount == 0) return 30f + 20f;
            int shown = System.Math.Min(EventLog.StoredCount, 20);
            float h = 30f + shown * 22f;
            if (EventLog.StoredCount > 20) h += 22f;
            return h;
        }
    }
}
