using System.Linq;
using Rimconemy.Foundation.UI;
using Rimconemy.Foundation.Registry;
using Rimconemy.ScavengerInfrastructure.Building;
using Rimconemy.ScavengerInfrastructure.Plants;
using Rimconemy.ScavengerInfrastructure.Power;
using Rimconemy.ScavengerInfrastructure.Storage;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimconemy.ScavengerInfrastructure.UI
{
    /// <summary>
    /// P3 read-only infrastructure status surface. It never changes power,
    /// plant, or building state; all values come from package read models.
    ///
    /// §8.9/Phase 3: extends with Storage snapshot section — reads StorageQuery
    /// directly (same package, no capability gate needed) and lists top
    /// resources by amount.
    /// </summary>
    public sealed class InfrastructureDashboard : RimconemyMainTabWindow
    {
        private Vector2 _scrollPosition;

        public override Vector2 InitialSize => new Vector2(680f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            PowerChainService.Resolve();
            long tick = Find.TickManager?.TicksGame ?? 0L;
            var power = PowerChainService.GetChainSnapshot(tick);
            var buildings = BuildingSnapshotService.Read(tick);
            var plants = PlantHelper.CollectSpawnedPlants();

            // §8.9: read storage snapshot directly (Mod 03 reads own StorageQuery — no gate needed).
            StorageSnapshot storage = null;
            try { storage = StorageQuery.ReadStorage(StorageScope.PlayerHomeMaps, null, tick); }
            catch (System.Exception ex) { Log.Warning("[Rimconemy.ScavengerInfrastructure] InfrastructureDashboard.StorageQuery: " + ex.Message); }

            int storageCount = storage?.Entries?.Count ?? 0;

            float width = inRect.width - RimconemyTheme.DefaultScrollbarWidth;
            float plantRows = plants != null && plants.Count > 0 ? plants.Count * 24f : 44f;
            float buildingRows = buildings != null && buildings.Count > 0 ? buildings.Count * 28f : 44f;
            float storageRows = storageCount > 0 ? Mathf.Min(storageCount, 20) * 26f : 44f;
            // Loop-Closure 2026-08-04: Action-Section (Bauschutt platzieren
            // 22px label + 28px button + 22px spacing) is part of the
            // discrepancy-aware content height. Without it, the action
            // button would scroll-clip below the Unity scrollview border.
            float actionRows = 22f + 28f + 22f;
            float contentHeight = 34f + 34f               // header + badge
                                + RimconemyTheme.RowHeight * 2f + 6f + RimconemyTheme.SectionSpacing // capability banner
                                + 32f + 58f + 24f + 24f + 32f  // power section
                                + 32f + buildingRows       // building section
                                + 32f + plantRows          // farms section
                                + 32f + 26f + storageRows  // storage section
                                + 32f + actionRows         // Loop-Closure 2026-08-04: Bauschutt-Action
                                + 32f + 48f;               // signal section

            var view = new Rect(0f, 0f, width, Mathf.Max(contentHeight, 420f));
            Widgets.BeginScrollView(inRect, ref _scrollPosition, view);
            float y = 0f;

            // ── Header ────────────────────────────────────────────
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, width, 28f), "Rimconemy · Infrastruktur");
            y += 34f;
            RimconemyUi.DrawStatusBadge(new Rect(0f, y, width, 24f),
                "Einheiten: " + power.TotalUnits + "  ·  Aktiv: " + (power.ActiveGenerators + power.ActiveTurrets),
                power.TotalUnits > 0 ? StatusLevel.Success : StatusLevel.Warn);
            y += 34f;
            RimconemyUi.DrawFeatureStatus(
                new Rect(0f, y, width, RimconemyTheme.RowHeight * 2f + 6f),
                "SNAPSHOT · Statusanzeigen bleiben read-only",
                "Bauschutt-Aktion schreibt best effort in den physischen Bestand; Vanilla-Bau-/Save-Lifecycle bleibt offen.",
                StatusLevel.Warn);
            y += RimconemyTheme.RowHeight * 2f + 6f + RimconemyTheme.SectionSpacing;

            // ── Stromnetz ──────────────────────────────────────────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infrastructure.Power", GameFont.Medium);
            y += 32f;
            RimconemyUi.DrawPressureGauge(new Rect(0f, y, width, 30f),
                power.TotalUnits <= 0 ? 0f : power.FueledUnits / (float)power.TotalUnits,
                "Brennstoff");
            y += 58f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Generatoren", power.ActiveGenerators + " aktiv");
            y += 24f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Pfeiltürme", power.ActiveTurrets + " aktiv");
            y += 24f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Wasserpumpe", power.HasWaterPump ? "Def vorhanden" : "nicht verfügbar",
                power.HasWaterPump ? RimconemyTheme.Success : RimconemyTheme.Muted);
            y += 32f;

            // ── Gebäude-Read-Model ────────────────────────────────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infrastructure.Buildings", GameFont.Medium);
            y += 32f;
            if (buildings == null || buildings.Count == 0)
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 42f), "Rimconemy.Infrastructure.NoBuildings");
                y += 48f;
            }
            else
            {
                foreach (var building in buildings)
                {
                    string state = building.PowerState.ToString();
                    if (building.ConstructionState == BuildingConstructionState.Damaged)
                        state += " · damaged";
                    string fuel = building.HasFuel ? "fuel ok" : "fuel blocked";
                    string inputMode = building.InputsAreAlternatives ? "one fuel input" : "required inputs";
                    RimconemyUi.DrawRow(new Rect(0f, y, width, 24f),
                        (building.Label ?? building.DefName) + " [" + building.DefName + "] · map " + building.MapId,
                        state + " · " + fuel + " · " + inputMode + " · tick " + building.SnapshotTick,
                        building.PowerState == BuildingPowerState.Online
                            ? RimconemyTheme.Success
                            : building.PowerState == BuildingPowerState.Blocked
                                ? RimconemyTheme.Warn
                                : RimconemyTheme.Muted);
                    y += 28f;
                }
            }

            // ── Felder & Pflanzen ─────────────────────────────────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infrastructure.Farms", GameFont.Medium);
            y += 32f;
            if (plants == null || plants.Count == 0)
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 42f), "Rimconemy.Infrastructure.NoPlants");
                y += 48f;
            }
            else
            {
                foreach (var pair in plants)
                {
                    RimconemyUi.DrawStatusBadge(new Rect(0f, y, width, 22f),
                        pair.Key + "  ·  " + pair.Value + " Pflanzen", StatusLevel.Info);
                    y += 24f;
                }
                y += 8f;
            }

            // ── Lager-Snapshot (§8.9 / Phase 3 Storage-UI) ───────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infrastructure.Storage", GameFont.Medium);
            y += 32f;

            if (storage == null || storage.Entries == null || storage.Entries.Count == 0)
            {
                RimconemyUi.DrawEmptyState(new Rect(0f, y, width, 42f), "Rimconemy.Infrastructure.NoStorage");
                y += 48f;
            }
            else
            {
                // Meta row: entry count + hash
                GUI.color = RimconemyTheme.Muted;
                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(0f, y, width, 18f),
                    storage.Entries.Count + " Ressourcentypen  ·  Hash " + (storage.ContentHash ?? "—")
                    + "  ·  Tick " + storage.SnapshotTick);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                y += 22f;

                // Show top 20 resources by TotalAmount descending
                var top = storage.Entries
                    .OrderByDescending(e => e.TotalAmount)
                    .Take(20);
                float colW = (width - 8f) / 2f;
                foreach (var entry in top)
                {
                    StatusLevel lvl = entry.Rot?.MaxRotProgress >= 0.7f ? StatusLevel.Warn
                        : entry.Availability != StorageAvailability.Available ? StatusLevel.Muted
                        : StatusLevel.Info;

                    string label = (entry.Label ?? entry.ResourceId) + "  ×" + entry.TotalAmount;
                    string detail = entry.Rot != null
                        ? "Verderb " + entry.Rot.Value.MaxRotProgress.ToString("P0")
                        : entry.Quality != null ? "Q " + entry.Quality.Value.AvgQuality.ToString("0.0") : "";

                    RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), label, detail,
                        lvl == StatusLevel.Warn ? RimconemyTheme.Warn
                        : lvl == StatusLevel.Muted ? RimconemyTheme.Muted
                        : null);
                    y += 26f;
                }
            }

            // ── Action: Bauschutt → Wand platzieren (Loop-Closure 2026-08-04) ──
            // Same code path as Designator_BuildWallBauschutt, but reachable
            // via the existing InfrastructureMainButton — no architect
            // category plumbing required. Respects BauschuttRemapApply
            // block-rules and the StorageWrite-Mutation gate.
            var proposal = BauschuttRemapService.PlanRemapForCurrentMap();
            int bauschuttCount = proposal.WallUnitCount;
            string actionLabel = bauschuttCount > 0
                ? "Bauschutt platzieren → " + bauschuttCount + " Wand-Blueprints"
                : "Bauschutt platzieren (0 Wand-Blueprints: " + (proposal.ReasonBlocked ?? "kein Bauschutt") + ")";
            GUI.color = RimconemyTheme.Info;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(0f, y, width, 18f),
                "Action: Bauschutt → Wand (1:1). Bauschutt-Stacks werden nach Platzierung physisch reduziert.");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += 22f;
            GUI.enabled = bauschuttCount > 0;
            if (Widgets.ButtonText(new Rect(0f, y, width, 28f), actionLabel))
            {
                var result = BauschuttRemapApply.ApplyRemap();
                if (!string.IsNullOrEmpty(result.ReasonBlocked))
                {
                    Messages.Message(
                        "Rimconemy Bauschutt-Remap blockiert: " + result.ReasonBlocked,
                        MessageTypeDefOf.RejectInput);
                }
                else
                {
                    string placementSummary =
                        "Rimconemy build: " + result.WallsPlaced + " Wall-Blueprints platziert "
                        + "(Bauschutt verbraucht: " + result.BauschuttConsumed + " physisch reduziert).";
                    Messages.Message(placementSummary, MessageTypeDefOf.PositiveEvent);
                    if (result.PlacementFailures != null && result.PlacementFailures.Count > 0)
                    {
                        foreach (var fail in result.PlacementFailures)
                        {
                            Log.Warning("[Rimconemy.ScavengerInfrastructure] Bauschutt-Remap placement issue: " + fail);
                        }
                    }
                }
            }
            GUI.enabled = true;
            y += 32f;

            // ── Signal / Diagnose ────────────────────────────────
            RimconemyUi.DrawSectionTitle(new Rect(0f, y, width, 26f), "Rimconemy.Infrastructure.Signal", GameFont.Medium);
            y += 32f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "PowerHash", power.ContentHash ?? "nicht verfügbar");
            y += 24f;
            RimconemyUi.DrawRow(new Rect(0f, y, width, 22f), "Tick", tick.ToString());

            Widgets.EndScrollView();
            RimconemyUi.ResetTextFontAndColor();
        }
    }
}
