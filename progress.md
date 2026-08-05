# Progress — Rimconemy

## 2026-08-04 (early)

- Persisted implementation plan in `ROADMAP.md §9.4` (integriert, Plan-Datei gelöscht).
- Added shared tokens: `DangerSoft`, `PanelInk`, `DividerInk`.
- Added toolkit helpers: stat cards, sparklines, inline tabs, countdowns, pressure gauges.
- Migrated Survival dashboard and ProgressionPawnTab to Foundation UI bases/tokens.
- First build: Foundation passed; Survival failed because local RimWorld 1.6 `GameFont` has no `Large` member.
- Resolution: mapped H1 presentation to supported `GameFont.Medium`; rerun Survival build next.

## 2026-08-04 (late) — Schema-Migration als First-Class-Domain

- `ISchemaMigratable`-Interface in `Foundation/Source/Save/` definiert — löst Open-Coded-Switch ab.
- `SchemaStep` (Action-Closure, FromVersion<ToVersion-Validation), `MigrationStepWalker` (OHNE try/catch, propagiert Exceptions), `MigrationRegistry` (string-keyed, idempotent Register, sortiertes MigrateAll, Clear am Save-Start), `SchemaMigratableExtensions.RunMigration` (DRY-Orchestration).
- 4 Migrators implementieren das Interface: `FoundationSaveData` (01), `CharacterSetupState` (02), `StoryState` (05), `CreditsLedger` (04).
- `FoundationDashboard.cs` stale `LoadedSchemaVersion`-Reference auf `saveData.SchemaVersion` gefixt.
- `ScribeRoundTripHelper.RoundTrip<T>(IExposable)` in `Foundation/Tests/` — echter Scribe-Save→Load-PostLoadInit-Cycle via MemoryStream.
- `Tests/CharacterSetupStateSchemaBumpTests.RunAll` mit 6 T1–T6-Assertions plus `Tests/StoryStateSchemaBumpTests`, `Tests/CreditsLedgerSchemaBumpTests`.
- `docs/falsification/survival__SaveMigration.md` (236 Z.) — standalone Falsifizierungsbericht für Phase-2.8 im Foundation-7-Sektion-Layout.
- Audit `§J rows 23–27` dokumentieren die Liefer-Bestandteile (Interface, Walker, Registry, Extension, Clear-Trigger, Stale-Reference-Fix). F2/F4 um First-Class-Domain-Verweis erweitert.
- `MigrationRegistry.GetMigrationLog()` ist vorbereitet für Dashboard-Integration (unified Cross-Package-Save/Load-Report).
