# Findings — Rimconemy UI P0–P4

- Shared toolkit files: `mods/01-Rimconemy-Foundation/Source/UI/RimconemyTheme.cs`, `RimconemyUi.cs`, `RimconemyWindow.cs`, `RimconemyMainTabWindow.cs`, `RimconemyInspectTab.cs`.
- Existing Foundation dashboard already uses `RimconemyMainTabWindow`; Survival dashboard still extends vanilla `MainTabWindow`.
- Existing ProgressionPawnTab extends vanilla `InspectTabBase`; migration can use `RimconemyInspectTab` without changing registration yet.
- Existing data sources: `ProgressionGameComponent`, `CreditsLedger`, `MarketService`, `OutpostService`, `StoryDirector`, `ThreatAggregator`, `PowerChainService`, `PlantHelper`.
- UI must remain read-only and null-safe because constructors/rendering can occur before `Current.Game` or package components exist.
- Inspect-tab registration is a RimWorld 1.6 API hazard; do not invent registration changes without local verification.
- Current five-package builds previously passed against local RimWorld managed references.
