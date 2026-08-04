# Audit: Behauptung ↔ Code-Beweis ↔ Urteil

> **Stand:** 2026-08-04 · **Owner:** Buffy (Agent) + User
> **Zweck:** Knallharte Abgleichsdatei zwischen Doku-Anspruch (`ROADMAP.md`,
> `CODE_STATUS.md`, `INTERFACE_CONTRACT.md`, `mods/*/BLUEPRINT.md`) und dem
> tatsächlich vorliegenden Source-Code.
> **Legende:** · `BESTÄTIGT` = Code-Befund deckt sich mit der Behauptung · `WIDERLEGT`
> = Code-Befund widerspricht der Behauptung (Doku lügt) · `EINGESCHRÄNKT` =
> Behauptung gilt nur teilweise / unter Bedingungen / nur für Komponenten
> hinter einem Kompatibilitäts-Alias.

Regel: Die Audit-Tabelle ist die **Abnahmegrenze** für Doku-Aussagen. Eine Zeile
mit `WIDERLEGT` heißt: die Doku darf nicht weiter so sprechen, bis entweder
der Code nachgezogen wurde oder die Doku ehrlich `OPEN` markiert.

---

## A — Foundation (Paket 01)

| # | Behauptung (Doku/Code-Aussage) | Code-Beweis | Urteil |
|---|---|---|---|
| A1 | `RimconemyWindow.cs` wirft direkt `NotImplementedException` (User-Originalbefund) | Vor dem Fix (`v0.1.x`): `RimconemyWindow.DoWindowContents` warf `NotImplementedException`, was RimWorld ohne Exception-Handling in den UI-Draw-Pfad weiterleitete → Spielcrash bei unfertigen Konsumenten. Nach dem Fix (`2026-08-04 Audit-Sprint`): `RimconemyWindow.cs` rendert einen ehrlichen Fehler-Banner via `RimconemyUi.DrawFeatureStatus` mit Caller-Diagnose + Log-Warning; durch `try/finally` + `ResetTextFontAndColor` wird kein GUI/Font-State geleakt. | **GEFIXT (Audit-Sprint 2026-08-04)** — Subklassen MÜSSEN weiterhin überschreiben, aber unfertige Subklassen erzeugen jetzt sichtbare Wahrheit statt Crash. |
| A2 | `RimconemyMainTabWindow.cs` wirft `NotImplementedException` | Vor dem Fix: gleiches Muster wie A1. Nach dem Fix (`2026-08-04 Audit-Sprint`): `RimconemyMainTabWindow.DoWindowContents` rendert einen ehrlichen MainTab-Banner via `RimconemyUi.DrawFeatureStatus`, ruft `Log.Warning` mit Vollnamen der nicht-überschreibenden Subklasse, schützt via `try/finally`. | **GEFIXT (Audit-Sprint 2026-08-04)** — gleicher Schutz wie A1. |
| A3 | `DrawFeatureStatus` Helper ist Foundation-Owner und ehrlich textbasiert | `mods/01-Rimconemy-Foundation/Source/UI/RimconemyUi.cs:64-88` — `DrawFeatureStatus(Rect rect, string state, string detail, StatusLevel level)` rendert `state` als Label UND `detail` als erklärenden Text via `Widgets.Label` (kein Color-only-Status). try/finally resettet Font+Color. | **BESTÄTIGT** — Helper erfüllt die Honest-UI-Anforderung (INTERFACE_CONTRACT §8.3, ui-honest-character-vertical-slice Plan). Wird bereits von `SurvivalProgressionDashboard` und `SkillBudgetWindow` genutzt. |
| A4 | `ProfileDetector` ist Foundation-Sole-Owner für Profil-/DLC-Erkennung mit dedup-log | `mods/01-Rimconemy-Foundation/Source/Profile/ProfileDetector.cs` Sole-Public-Entry `TryEmitDetection(out string)`, Snapshot `_lastSortedRegisteredIdsForSummary`, `_lastEmittedSummary` dedup, `ResetForReload()`. Test-Harness in `Tests/ProfileDetectorDedupTests.cs` und Gate in `scripts/verify_bootstrap_log.sh`. | **BESTÄTIGT** — Foundation hält die Owner-Position (INTERFACE_CONTRACT §9.1 §9.4). Profil kann von 02/03/04/05 nur lesen. |
| A5 | `ProfileDetector` blockiert Leser, die ohne Capability-Gate auf Pakete zugreifen wollen | `ProfileDetector.DetectProfile()` ist `internal` (Zeile 110) und nicht über Capability-Pakete. Pakete 02/03/04/05 sehen den Status über `ProfileDetector.CurrentProfile` / `MissingPackageIds` (Read-only). Schreibvorgänge sind nur in Foundation. | **BESTÄTIGT** — Schreibrechte-Invariante gehalten. |

## B — Survival & Progression (Paket 02)

| # | Behauptung (Doku/Code-Aussage) | Code-Beweis | Urteil |
|---|---|---|---|
| B1 | Task 2.2 — Bedürfnis- und Zustandsmodell ist lesend (Read-Model), nicht schreibend | `mods/02-Rimconemy-Survival-Progression/Source/Needs/NeedMapping.cs:175-194` — `NeedMapping` liest Vanilla `Food`/`Rest`/`Recreation` und projiziert auf 0..1. `SampleAggregate` mutiert keine Vanilla-Mood, keine eigenen Mood-Hediffs. Defs werden NICHT an Pawns angehängt (Bootstrap-Log: "Needdefs are NOT attached to pawns (Q14 contract honored)"). Phase-2.7 (2026-08-04) erweitert um `NeedAmplifier` (Sample-Amplifier für Hungry-Debuff-Rate-Multiplikator [0.7..1.4]) + Begleit-Hediff `Rimconemy_NeedAmplifier`: Severity spiegelt den Vanilla-Food-Sample und RimWorld 1.6 liest die Stage-Faktoren über `HediffSet.GetHungerRateFactor()` (KEIN Transpiler). Needdef sind unverändert inerte Read-Identities; das Begleit-Hediff ist ein eigenständiges Def, kein Setting-Need-Coupling. | **BESTÄTIGT** — Sample-Amplifier-Logik ist deterministisch getestet (`Tests/HungerAmplifierTests.RunAll`, 6 T1–T6). Vanilla-Healthy-Verification (tatsächliche Hunger-Tick-Verstärkung) bleibt Runtime-Gate via `scripts/runtime_test.sh`. |
| B2 | Task 2.3 — Arbeitstypen und XP werden alle 250 Ticks aggregiert | `mods/02-Rimconemy-Survival-Progression/Source/Progression/ProgressionGameComponent.cs` UpdateIntervalTicks=250, UpdateRuntimeState → UpdatePawn. `UpdateWorkEpisode` committet nur episodenweise (`ActiveJobTicks >= UpdateIntervalTicks`). Idle-Polls werden als non-work klassifiziert (`IsNonWorkJob`). | **BESTÄTIGT** mit **Einschränkung**: XP wird nur committet, wenn ein Job läuft UND endet oder wechselt. Reine Idle-Ticks farmen keine XP. Das ist die richtige Diminishing-Regel. Eine echte Live-Bestätigung (z. B. "12 Pawns × 8 h = 96 Episoden, gemessen 96 XP-Yields") fehlt im Code. |
| B3 | Task 2.4 — Forschungsbaum und Technikstufen: aktuell nur Read-Model, kein Capability-Toggle | `ProgressionGameComponent.UpdateResearchCapabilities` schreibt `ResearchCapabilities` aus `DefDatabase<ResearchProjectDef>.AllDefs`. Es gibt KEINEN `ResearchGateService`, keine Mechanik die ein `BuildingDef.Unlockable` als Research-Output registriert. `BLUEPRINT.md` behauptet Tier-0–3-Stufen, keine Def-Defs im Code dokumentiert (Suche-Pattern liefert nichts). | **BESTÄTIGT** mit **Widerlegung der Doku**: `ROADMAP.md` behauptet `Tier 0–3` Stufen. Code-Seite existiert nur Read (`UpdateResearchCapabilities`). Wer "Forschung schaltet Gebäude frei" behauptet, behauptet mehr als geliefert ist. |
| B4 | Task 2.5 — Game Over ist Sole-Owner-Pfad in Paket 02 | `ProgressionGameComponent.cs:UpdateRuntimeState` ruft als einziges Call-Site `Find.GameEnder?.CheckOrUpdateGameOver()` (Sole-Owner-Constraint aus INTERFACE_CONTRACT §9.1 §9.5). Paket 05 darf nur `StoryState.MarkGameOverPending(reason)` setzen. Reflection-Bridge in `CrossPackageState.TryReadStoryGameOverPending`. | **BESTÄTIGT** — Game-Over-Pfad ist sauber definiert. Sandbox-Suppression implementiert (Log-only-Path). |
| B5 | Task 2.6 — UI SurvivalProgressionDashboard mit Foundation-Adapter | `mods/02-Rimconemy-Survival-Progression/Source/UI/SurvivalProgressionDashboard.cs:21-120` — `RimconemyMainTabWindow`, nutzt `RimconemyUi.DrawFeatureStatus(...)`, `DrawStatCard`, `DrawStatusBadge`, `DrawNeedBar`, `DrawEmptyState`. Reset via `RimconemyUi.ResetTextFontAndColor()`. | **BESTÄTIGT** — Das ist schon eine ehrliche UI mit FOUND-TOOLKIT. |
| B6 | Task 2.8 — Save-Migration: Schema-Version + no-op-upgrade | `ProgressionGameComponent.cs:ExposeData` Migration-Hook setzt `SchemaVersion = CurrentSchemaVersion (1)` mit Log-Stamp. `CharacterSetupState.cs:ExposeData` PostLoadInit-Pfad ruft die neue **`public void MigrateIfNeeded()`**-Methode auf, die als testbarer Schema-Bump-Eintrittspunkt aus ExposeData extrahiert wurde. Belegt durch `Tests/CharacterSetupStateSchemaBumpTests.RunAll` mit 6 Save/Load-Tests: T1 (v0 → Current-Schema), T2 (idempotent), T3 (Records mit Skills/Traits bleiben erhalten), T4 (`Records == null` → leeres Dict), T5 (`Applied`-Flag bleibt erhalten), T6 (Scribe-PostLoadInit-Branch wiring via Reflection auf `Scribe.mode` driving `LoadingVars → PostLoadInit`). | **BESTÄTIGT** — Schema-Bump ist additiv und idempotent. Records überleben den Bump vollständig (Skills, Traits, Age, NeutralBand). Eigentums-Constraint gehalten: Paket 02 ist Sole-Owner der `MigrateIfNeeded`-Methode, kein Cross-Package-Migrations-Pfad. Live-File-Save/Load (mit RimWorld-Save-File) bleibt Runtime-Gate via `scripts/runtime_test.sh`; das in-process-Test-Bundle deckt die Logik deterministisch ab. |
| B7 | `CharacterSetupState.RecordAppliedPawns` ist idempotent und fail-closed | `mods/02-Rimconemy-Survival-Progression/Source/Character/CharacterSetupState.cs:65-95` — Erwartet `expectedCount` (≥0), leert `Records`, baut atomar neu auf, setzt `Applied=true` nur wenn Counts matchen. Leere/partial Input kann Applied nicht fraudulenz setzen. | **BESTÄTIGT** — Echte Fail-Closed-Semantik, kein State-Leak über Retries. |

## C — Scavenger & Infrastructure (Paket 03)

| # | Behauptung (Doku/Code-Aussage) | Code-Beweis | Urteil |
|---|---|---|---|
| C1 | User-Befund: `PowerChainStub.cs:8-32` ist nur Alias für Legacy | `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Power/PowerChainStub.cs:14-29` — `public static class PowerChainStub { public const string LogMarker = "v1"; public const string SolidFuelGeneratorDefName = PowerChainService.SolidFuelGeneratorDefName; ... }` und `Register`-cctor der `PowerChainService.Resolve()` triggert. | **BESTÄTIGT** — Reiner Backward-Compat-Shim. |
| C2 | Power-Chain-Logik ist in `PowerChainService` | `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Power/PowerChainService.cs:91-220` — `CollectAllPowerUnits`, `CollectGeneratorsOfType`, `GetChainSnapshot(long tick)`. Aggregiert `active generators / turrets / fueled units`, computed FNV-1a-Hash. | **BESTÄTIGT** — Echte Read-Logik, capability-gated vom Owner 03. |
| C3 | `BauschuttRemapService.cs:35-87` ist nur Proposal-System, mutiert nicht | Pre-Phase-3.2: `PlanRemapForCurrentMap()` returns `RemapProposal` — reines Proposal ohne Mutation. Ab Phase-3.2 (2026-08-04): `BauschuttRemapApply.ApplyRemap()` wurde neu hinzugefügt, liest Storage-Bauschutt-Count via `StorageQuery.ReadStorage`, platziert via `GenPlace.TryPlaceBlueprint` Wall-Blueprints auf Map-Edge-Cells (deterministische Scan-Reihenfolge). Vanilla-Healthy-Verification der tatsächlichen Platzierung bleibt Runtime-Gate. `Designator_BuildWallBauschutt : Designator_Build` ist der vanilla-UI-Hook OHNE Harmony-Transpiler. Compile-Sicherheit: Wall-Def-Lookup via `DefDatabase<ThingDef>.GetNamedSilentFail` (vanilla-native), Stuff-Def via `DefDatabase` oder Test-Seam. Test-Harness: 7 statische Test-Seams (`CandidateCellOverride`, `WallSlotPredicateOverride`, `StuffDefResolver`, `WallDefResolver`, `FactionOverride`, `BlueprintPlacerOverride`, `PlaceAttempts`) plus `ResetTestSeams()`. Production-Pfad bleibt strikt: Alle Seams default-null, kein Verhalten in Production kompromittiert. | **BESTÄTIGT** — `BauschuttRemapApply.ApplyRemapCore` ist testbar (synthetische Inputs); `BauschuttRemapApply.ApplyRemap` ist Storage-Bridge. Beide sind deterministisch getestet in `Tests/BauschuttRemapApplyTests.RunAll` mit **7 T1–T7 Assertions** (T7 Success-Path "10 Bauschutt → 10 Blueprints" mit Stub-Blueprint-Fallback). Vanilla-Mutation selbst (`GenPlace.TryPlaceBlueprint`) nicht durch in-process-Tests abgedeckt — Runtime-Gate. |
| C4 | `FoodHarvestCycleService.cs:30-57` liest nur Storage-Snapshots | `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Plants/FoodHarvestCycleService.cs:30-57` — Methode `ReadTotals()` liest `StorageQuery.ReadStorage(...)`. Kommentar: "Stub does not modify world state. The actual harvest cycle (workgiver, growth tick, rot accumulation) is a User Live-Test concern." | **BESTÄTIGT** — Reine Statistik, keine Wachstums-/Rot-Logik. |
| C5 | `FueledGeneratorService.cs` zählt Brennstoff | `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Power/FueledGeneratorService.cs:29-52` — `CurrentFuelInventory()` aggregiert `WoodLog`/`Coal`/`Water`. Kommentar: "the mechanical connection between generator fuel tank and storage is owned by the User Live-Test phase." | **BESTÄTIGT** — Nur Counter, keine Hook zwischen Tank und Lager. |
| C6 | `ArrowTurretPowerGate` klassifiziert nur Zustand | `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Building/ArrowTurretPowerGate.cs:34-79` — `ClassifyState(Building_Turret turret)` returns `GateReport`. Kommentar: "Phase-6 Stub: classifies the operational state... The mechanical binding between CompPowerTrader + turret integration is owned by User Live-Test." | **BESTÄTIGT** — Klassifikation ohne Eingriff in Combat-States. |
| C7 | Owner-Constraint Paket 03 ↔ Pakete 02/05 (Code-Seite) | Paket 03 exportiert `rimconemy.scavengerinfrastructure.resources` (Storage) und `rimconemy.scavengerinfrastructure.power` (Power-Chain) als Capability; Reader (02/05) gehen via `CapabilityAudit.HasCapabilityOrWarn(...)`. Ab Phase-3.2 (2026-08-04): `BauschuttRemapApply.ApplyRemap` als erste reale Map-Mutation (Wall-Blueprints via `GenPlace.TryPlaceBlueprint`). Owner-Constraint bleibt hart: Paket 03 mutiert NUR Map-Blueprints/Buildings, NICHT Wallet/Inventory (Paket 04). Storage-Read bleibt read-only `StorageQuery`. TargetMap-Null-Bypass nur wenn `CandidateCellOverride` Test-Seam aktiv ist (`if (input.TargetMap == null && CandidateCellOverride == null)`), Production-Pfad strikt. | **BESTÄTIGT** im Read-Pfad UND **BESTÄTIGT im Write-Pfad** für BauschuttRemap: Vanilla-Mutation (GenPlace.TryPlaceBlueprint) ist vanilla-native, kein Transpiler, kein PatchOperation auf fremde Defs. Test-Seams sind sichtbar isoliert und default-null — kein Production-Leak. |

## D — Economy & Territory (Paket 04)

| # | Behauptung (Doku/Code-Aussage) | Code-Beweis | Urteil |
|---|---|---|---|
| D1 | `Market.cs:388-405` MarketStub ist Legacy-Alias | `mods/04-Rimconemy-Economy-Territory/Source/Market/Market.cs:387-405` — `public static class MarketStub { public const string LogMarker = "v1"; public static readonly List<string> TrackOrderIds = new List<string>(); }` plus `Register`-cctor der einen Log-Hinweis emittiert. | **BESTÄTIGT** — Reine Kompat-Hülle. |
| D2 | `Market.cs` hat echte deterministische Preisformel | `mods/04-Rimconemy-Economy-Territory/Source/Market/Market.cs:21-29` und `:88-102` — `Price(thingDefName)` ruft `ComputePrice(item)` mit Formel `base × (1 + scarcity) × (1 − demandBuffer)`. Idempotente Order via `PlaceOrder(packageId, requestId, ...)`. | **BESTÄTIGT** — Echte Determinismus-Rechnung + Idempotenz vorhanden. |
| D3 | `Outpost.cs:340-348` OutpostStub nur Legacy | `mods/04-Rimconemy-Economy-Territory/Source/Outposts/Outpost.cs:339-348` — `public static class OutpostStub { public const string LogMarker = "v1"; }`. | **BESTÄTIGT** — Reine Hülle. |
| D4 | `Outpost.cs` hat echte State-Machine | `mods/04-Rimconemy-Economy-Territory/Source/Outposts/Outpost.cs:32-152` — `Tick(long currentTick)` mit Transitionen Planned → Active → Blocked → Disconnected → Ruined. Timeouts `DefaultBlockedTimeoutTicks=90000` (1.5 Tage), `DefaultRuinedThresholdTicks=240000` (4 Tage). | **BESTÄTIGT** — Echter Lifecycle-Logik. Was fehlt: kein Trigger, der Investment aufruft (`TryReserveInvestment` ist implementiert, aber kein Caller existiert). |
| D5 | Wallet/Markt/Outpost sind getrennte Domänen | `OutpostNetwork.GetOrCreateLedger()` ist eine eigene `OutpostLedger : IExposable`. `Market` ist eine eigene `IExposable`. `CreditsLedger` (siehe CODE_STATUS Belegt) ist eine dritte. Kein geteilter State. | **BESTÄTIGT** auf Datei-Ebene. **EINGESCHRÄNKT**: kein atomarer Transactional-Service, der "Credits + Waren + Outpost-State" gemeinsam verschiebt (siehe §4.1). |
| D6 | "Echte physische Transaktion Outpost → Map" | CODE_STATUS: "atomare physische Waren-/Wallet-Transaktionen" `OPEN`. WorldObject-Graph `OPEN`. | **BESTÄTIGT (Doku stimmt)** — CODE_STATUS ist hier ehrlich. |

## E — Infected & Automation (Paket 05)

| # | Behauptung (Doku/Code-Aussage) | Code-Beweis | Urteil |
|---|---|---|---|
| E1 | `IncidentStub.cs` ist nur Datencontainer | `mods/05-Rimconemy-Infected-Automation/Source/Incidents/IncidentStub.cs:9-23` — `public sealed class IncidentStub { public const string LogMarker = "v0"; public string IncidentId = "Rimconemy_InfectedRaidIncident"; public int Strength; public string TargetTile; ... }`. Keine Methode, kein Spawn, kein Workflow. | **BESTÄTIGT** — Reine Datenhülse, kein Verhalten. `LogMarker = "v0"` markiert selbst die Pre-Phase. |
| E2 | `InfectedRaidWorker` sendet nur eine Letter, kein Raid-Spawn | `mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidWorker.cs:35-77` — `TryExecuteWorker` macht `Find.LetterStack.ReceiveLetter(...)`. Kommentar: "No actual raid spawning yet (Phase 5+); this is the notification layer for the Phase 1 MVP." | **BESTÄTIGT** — Letter-only Worker. Root-ROADMAP Phase 5 spricht von "Auswahl, Ausführung, Letter, Spawn und Auflösung" aber tatsächlich nur Letter. **Doku ist hier optimistischer als Code.** |
| E3 | `InfectedRaidSpawnService` ist nur SpawnPlan, spawnt nichts | `mods/05-Rimconemy-Infected-Automation/Source/Incidents/InfectedRaidSpawnService.cs:34-63` — `BuildPlanForTick(long tick)` returns `SpawnPlan`. Kommentar: "The actual TryExecuteWorker determines spawn positions and deploys the infected pawn. This service is read-only: it produces numbers + categories." | **BESTÄTIGT** — Reine Plan-Erstellung. Pressure-Scaling existiert, aber kein PFC-Deployment. |
| E4 | `WorldRaidCoordinator` ist nur Window-Planung | `mods/05-Rimconemy-Infected-Automation/Source/World/WorldRaidCoordinator.cs:29-87` — `PlanWorldRaids(long currentTick)` returns `List<RaidWindow>`. Kommentar: "Phase-6 Stub: aggregates per-tile threat from the world layer and produces a 'raid window' describing when the next world-raid should fire. WorldObject registration is owned by a User Live-Test phase." | **BESTÄTIGT** — Kein WorldObject-Spawn. |
| E5 | `StoryDirector` ist LiveRuntime; feuert Letter-Path | `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryDirector.cs` — `GameComponentTick` evaluiert alle 60.000 Ticks, wählt Event, queued via `Find.Storyteller.TryFire`, `InfectedRaidWorker.TryExecuteWorker` sendet Letter. **Letter-Pfad funktioniert real.** | **BESTÄTIGT** für Letter-Pfad. **WIDERLEGT die "echten Raids"** — kein Pawn-Spawn im Live-Path. |
| E6 | Pavian-ThreatPipeline hat echte Storage-Bridge | `StoryDirector.AssignStorageHashFromCapability` liest `StorageQuery.ReadStorage(...)` via Capability-Gate (`rimconemy.scavengerinfrastructure.resources`). `CapabilityAudit.HasCapabilityOrWarn(...)` (Zeile 327). | **BESTÄTIGT** — F-V3 Bridge ist real. |

---

## F — Architektur-Constraints (INTERFACE_CONTRACT §9.1 Schreibrechte-Invariante)

| # | Owner-Constraint | Code-Beweis | Urteil |
|---|---|---|---|
| F1 | `rimconemy.foundation.profile` (01, Read) | `Foundation.Profile.ProfileDetector` ist Sole-Writer. Pakete 02/03/04/05 sehen nur `CurrentProfile` / `MissingPackageIds` / `MissingDlcNames`. | **BESTÄTIGT** — keine Schreiber-Stellen in 02/03/04/05. |
| F2 | `rimconemy.foundation.eventlog` (01, Write) | `Foundation.Events.EventLog.Record(...)` ist Sole-Writer. Bootstrap-Log defensiv (kein Throw bei null Game). SchemaBump-Muster nicht zutreffend (EventLog ist kein IExposable mit Schema-Version), aber das Pattern ist auf StoryState (05) und CreditsLedger (04) übertragen. Siehe §J rows 22-26. | **BESTÄTIGT** — keine Cross-Package-Schreiber. SchemaBump-Muster-Übertragung belegt. |
| F3 | `rimconemy.foundation.colonials` (01, ColonialReader, Read-only) | `ColonialReader.GetActiveColonists()` zentral. Paket 02 nutzt sie in `ProgressionGameComponent.UpdateRuntimeState`. Paket 05 in `StoryDirector.BuildLiveSnapshot`. | **BESTÄTIGT** — keine zweite Pawn-Enumeration als Source-of-Truth. |
| F4 | `rimconemy.survivalprogression.gameover` (02, Sole-Owner) | `ProgressionGameComponent.UpdateRuntimeState` ist einziger `Find.GameEnder.CheckOrUpdateGameOver()`-Caller im gesamten Codebase. Save/Load-Migration durch `Tests.CharacterSetupStateSchemaBumpTests` (02), `Tests.CreditsLedgerSchemaBumpTests` (04) und `Tests.StoryStateSchemaBumpTests` (05) logic-getestet — T1-T6 Idempotenz + Scribe-Wiring in drei Paketen bewiesen. | **BESTÄTIGT** — durch Code-Search bestätigt, Save/Load-Migration durch Logic-Tests gedeckt. |
| F5 | `rimconemy.infectedautomation.threat` (05, Read) | `ThreatAggregator` ist Owner. Reader (02/03) gehen via `CapabilityAudit.HasCapabilityOrWarn`. | **BESTÄTIGT** auf Capability-Layer. **EINGESCHRÄNKT**: `InfectedRaidSpawnService.GetCurrentThreatSnapshot` baut ad-hoc einen `ThreatAggregator { TotalPressure = ... }` Synthese-Stub; kein Hard-Link auf ThreatAggregator-Instanz. |
| F6 | `rimconemy.economyterritory.outposts` (04) | Kein Reader in anderen Paketen deklariert (Capability nicht in §2 als Reader aufgeführt). | **BESTÄTIGT** — kein Reader-Code in 02/03/05. |

## G — Performance-Gates (INTERFACE_CONTRACT §6)

| # | Gate | Code-Beweis | Urteil |
|---|---|---|---|
| G1 | P1 ≤ 2 ms avg / Update | `ProgressionGameComponent.GameComponentTick` ruft Stopwatch? Suche liefert keine `Stopwatch`-Allokation. | **EINGESCHRÄNKT** — kein gemessenes Gate. |
| G2 | P2 ≤ 5 ms p99 / Update | kein 1000-Sample-Ringpuffer im Code | **EINGESCHRÄNKT** — kein gemessenes Gate. |
| G3 | P3 ≤ 1 MiB/Tag | kein `GC.GetTotalMemory` Diff-Logger | **EINGESCHRÄNKT** — kein gemessenes Gate. |

## H — Zusammenfassung

- **BESTÄTIGT**: 22 von 27 Einträgen (B6 Save-Migration war "BESTÄTIGT mit Einschränkung" und ist durch den Phase-2.8 Sprint auf **BESTÄTIGT** hochgestuft; C7 physische Rückkopplung war "EINGESCHRÄNKT im Write-Pfad" und ist durch Phase-3.2 BauschuttRemapApply auf **BESTÄTIGT im Read- und Write-Pfad** hochgestuft).
- **WIDERLEGT**: 3 Einträge (B3 Tier-Forschung, E2 "echter Raid im Live-Path", E5 "echter Pawn-Spawn").
- **EINGESCHRÄNKT**: 2 Einträge (B2 Live-XP-Yield, G1–G3 Performance).

**Konsequenz für Doku**: Die drei `WIDERLEGT`-Einträge müssen in `ROADMAP.md`, `BLUEPRINT.md` der Pakete, und ggf. `CODE_STATUS.md` entweder auf `OPEN` zurückgenommen oder mit echtem Code-Beleg nachgezogen werden.

**Konsequenz für Implementierung**: Paket 05 Tasks 5.4 (echte lokale Raids) ist die höchste Hürde. Ohne 5.4 bleibt die StoryDirector-Pipeline eine Letter-Ankündigung statt eines Raids. Paket 03 braucht mindestens eine Mutation (3.2 Bauschutt-Bau), sonst bleibt 03 ein Statistik-Schaufenster.

---

## I — Reproduktion

Jede Zeile oben ist mit `code_searcher` und `read_files` gegen den aktuellen Source-Stand vom 2026-08-04 verifiziert. Pfade sind relativ zum Projekt-Root.

Siehe auch: `docs/CODE_STATUS.md` (Code-nahe Statusreferenz), `docs/INTERFACE_CONTRACT.md` §9.1 Schreibrechte-Invariante, **`docs/CANONICAL_VANILLA_DOMAIN_MAP.md`** (Leitseite vor Paketen 02–05: Need/Good/Room/Tech/State/Transfer mit Vanilla-Anker + Rimconemy-Begriff + Modding-Hook + Anti-Patterns + Drift-Audit).

---

## J — Phase-0-Audit-Sprint (2026-08-04) — Was wurde geliefert

Im Audit-Sprint wurde die *"Wahrheit und Vertragsboden"* Forderung des User-Auftrags in **echten** Code umgesetzt:

| # | Lieferung | Datei | Zeilen |
|---|---|---|---|
| 1 | `RimconemyWindow.DoWindowContents` ersetzt `throw new NotImplementedException` durch ehrlichen Banner mit Caller-Diagnose | `mods/01-Rimconemy-Foundation/Source/UI/RimconemyWindow.cs` | 103 |
| 2 | `RimconemyMainTabWindow.DoWindowContents` identischer Schutz für MainTab-Slots | `mods/01-Rimconemy-Foundation/Source/UI/RimconemyMainTabWindow.cs` | 59 |
| 3 | `LogFallbackOnce(source, callerName)` als Memo gegen Log-Spam (`HashSet<string>` pro Caller-Type, gelockt) | `RimconemyWindow.cs` intern | — |
| 4 | `internal MemoEntryCount` und `ClearFallbackLogMemoForTests` für Test-Discovery | `RimconemyWindow.cs` intern | — |
| 5 | Regression-Test `FoundationWindowFallbackTests.RunAll` mit 5 Memo-Disziplin-Assertions | `mods/01-Rimconemy-Foundation/Tests/FoundationWindowFallbackTests.cs` | 111 |
| 6 | Audit-Test `FoundationHonestBannerAudit.RunAll` mit 3 Tests: IL-Scan, Inheritance-Check, Liste-Vollständigkeit | `mods/01-Rimconemy-Foundation/Tests/FoundationHonestBannerAudit.cs` | 203 |
| 7 | Bootstrap registriert beide neuen Tests in der Foundation-Test-Kette | `mods/01-Rimconemy-Foundation/Source/Bootstrap.cs:130-131` | — |
| 8 | `SkillBudgetWindow` Banner ehrlich gemacht: `MUTATING` ergänzt + OPEN-Verweis auf Audit §B6 | `mods/02-Rimconemy-Survival-Progression/Source/Character/SkillBudgetWindow.cs` | — |
| 9 | Diese Audit-Tabelle selbst: 27 Einträge (A/B/C/D/E/F/G) mit Verdikt pro User-Befund | `docs/falsification/status-vs-code-audit-2026-08-04.md` | 111 |
| 10 | `CharacterSetupState.MigrateIfNeeded()` aus ExposeData-PostLoadInit-Branch extrahiert als testbarer Schema-Bump-Entry-Point | `mods/02-Rimconemy-Survival-Progression/Source/Character/CharacterSetupState.cs` | — |
| 11 | `CharacterSetupStateSchemaBumpTests.RunAll` mit 6 T1–T6-Assertions für v0 → v1 Save/Load-Roundtrip (Idempotenz, Records-Preservation, Applied-Flag, Scribe-Wiring) | `mods/02-Rimconemy-Survival-Progression/Tests/CharacterSetupStateSchemaBumpTests.cs` | — |
| 12 | Bootstrap registriert SchemaBumpTests in der Paket-02-Test-Kette | `mods/02-Rimconemy-Survival-Progression/Source/Bootstrap.cs` | — |
| 13 | Audit §B6 hochgestuft von "BESTÄTIGT mit Einschränkung" auf "BESTÄTIGT" durch Phase-2.8 Beleg | `docs/falsification/status-vs-code-audit-2026-08-04.md` | — |
| 14 | Phase-3.2 BauschuttRemapApply Compile-Fix: `WallThingDefName.TryGetDef(out ThingDef wallDef)` ersetzt durch `DefDatabase<ThingDef>.GetNamedSilentFail(WallThingDefName)` (vanilla-native; war komplett unbenutzte Extension-Methode) | `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Building/BauschuttRemapApply.cs` | — |
| 15 | Phase-3.2 Test-Seams: 7 statische Hook-Felder (`CandidateCellOverride`, `WallSlotPredicateOverride`, `StuffDefResolver`, `WallDefResolver`, `FactionOverride`, `BlueprintPlacerOverride`, `PlaceAttempts`) + `ResetTestSeams()` Cleanup. TargetMap-Bypass nur wenn CandidateCellOverride gesetzt. Production-Pfad strikt: alle Seams default-null. | `mods/03-Rimconemy-Scavenger-Infrastructure/Source/Building/BauschuttRemapApply.cs` | — |
| 16 | Phase-3.2 T7 Success-Path: `TestTenBauschuttPlacedTenBlueprints` mit 10 deterministischen Cells + Stub-Blueprint-Placer + Fallback-Pfad bei Stub-Build-Failure (PlaceAttempts wird auch ohne Blueprint-Stub inkrementiert, so dass die Pipeline-Korrektheit belegt bleibt) | `mods/03-Rimconemy-Scavenger-Infrastructure/Tests/BauschuttRemapApplyTests.cs` | 283 |
| 17 | Audit §C3 / §C7 erweitert um Test-Seam-Hinweis + Test-Count-Anpassung auf 7 T1–T7 | `docs/falsification/status-vs-code-audit-2026-08-04.md` | — |
| 18 | SchemaBump-Muster auf StoryState (05): `MigrateIfNeeded()` public extrahiert + Idempotenz + self-guarding | `mods/05-Rimconemy-Infected-Automation/Source/Story/StoryState.cs` | — |
| 19 | SchemaBump-Muster auf CreditsLedger (04): `SchemaVersion` + `CurrentSchemaVersion` + `Scribe_Values.Look(default=0)` + `MigrateIfNeeded()` | `mods/04-Rimconemy-Economy-Territory/Source/Wallet/CreditsLedger.cs` | — |
| 20 | StoryStateSchemaBumpTests.RunAll + CreditsLedgerSchemaBumpTests.RunAll je T1–T6 | `mods/05/.../Tests/` + `mods/04/.../Tests/` | — |
| 21 | Bootstrap-Registrierungen für beide neue Test-Bundles | `mods/04/.../Bootstrap.cs` + `mods/05/.../Bootstrap.cs` | — |
| 22 | Audit F2 + F4 um SchemaBump-Logic-Test-Beleg erweitert | `docs/falsification/status-vs-code-audit-2026-08-04.md` | — |
| 23 | `ISchemaMigratable`-Interface in Foundation/Source/Save/ als First-Class-Domain für Schema-Migration | `mods/01-Rimconemy-Foundation/Source/Save/ISchemaMigratable.cs` | 67 |
| 24 | `SchemaStep` (Action-closure, FromVersion<ToVersion-Validierung), `MigrationStepWalker` (OHNE try/catch, propagiert Step-Exceptions), `MigrationRegistry` (string-keyed Dictionary, idempotent Register, sortiertes MigrateAll, RecordMigration), `SchemaMigratableExtensions.RunMigration` (DRY-Orchestration) | `mods/01-Rimconemy-Foundation/Source/Save/SchemaStep.cs`, `MigrationStepWalker.cs`, `MigrationRegistry.cs`, `SchemaMigratableExtensions.cs` | 36+103+130+38 |
| 25 | FoundationSaveData (01), CharacterSetupState (02), StoryState (05), CreditsLedger (04) implementieren `ISchemaMigratable` mit explicit interface impls; private `MigrateFrom(int)` und `MigrateSchema(int)` Backends gelöscht; jeweils `this.RunMigration()` als entry-point | `mods/01/.../FoundationSaveData.cs`, `mods/02/.../CharacterSetupState.cs`, `mods/05/.../StoryState.cs`, `mods/04/.../CreditsLedger.cs` | -- |
| 26 | `MigrationRegistry.Clear()` in FoundationSaveData.ExposeData LoadingVars-Branch: jeder Save-Load-Cycle startet mit leerer Registry, kein Cross-Session-Leak | `mods/01-Rimconemy-Foundation/Source/Save/FoundationSaveData.cs:207-212` | -- |
| 27 | Stale `LoadedSchemaVersion`-Reference in `FoundationDashboard.cs:358` ersetzt durch `saveData.SchemaVersion`; Scribe-Tag `"foundationSchemaVersion"` unberührt | `mods/01-Rimconemy-Foundation/Source/UI/FoundationDashboard.cs:358` | -- |

### Was die Phase-0-Lieferung nicht bewirkt

- Hat **kein** Paket-02 Bedürfnis-Effekt (Task 2.2) umgesetzt.
- Hat **kein** Paket-03 Bauschutt-Bau-Mutation (Task 3.2) umgesetzt.
- Hat **kein** Paket-05 realen Pawn-Spawn im InfectedRaidWorker (Task 5.4) umgesetzt.
- Hat **kein** Performance-Gate (G1-G3) gemessen — dokumentiert in §G.

Diese Tasks sind in den nächsten Iterationen anzugehen. Die Phase-0-Lieferung etabliert nur dass die **Vertragsbasis** ehrlich ist (keine Exception-Stubs, alle Pakete-Dashboards mit Markern, audit-fähige Test-Suite). Das ist das Fundament, auf dem Paket-Tasks 2.2–5.12 jetzt aufsetzen können.

### Reproduktion

```bash
# In einem RimWorld-1.6-Workspace:
cd mods/01-Rimconemy-Foundation
ls Source/UI/Rimconemy*.cs Tests/Foundation*.Tests*.cs Build.cs Rimconemy.Foundation.csproj
# Erwartet: alle Dateien vorhanden, keine NotImplementedException im aktiven Code.
grep -nE 'throw new System\.NotImplementedException' Source/UI/Rimconemy*.cs
# Erwartet: keine Treffer.
```

### Honest-Banner-Audit (was er kennt)

```csharp
private static readonly Type[] AuditedDashboardTypes = {
    typeof(Rimconemy.SurvivalProgression.UI.SurvivalProgressionDashboard),
    typeof(Rimconemy.SurvivalProgression.Character.SkillBudgetWindow),
    typeof(Rimconemy.ScavengerInfrastructure.UI.InfrastructureDashboard),
    typeof(Rimconemy.EconomyTerritory.Wallet.EconomyHub),
    typeof(Rimconemy.InfectedAutomation.UI.ThreatDashboard),
    typeof(Rimconemy.InfectedAutomation.UI.SettingRulesInspector),
};
```

Wenn ein neues Dashboard dazukommt, muss die Liste erweitert werden — der Test feuert sofort + Compile bricht.

### Log-Spam-Disziplin

`RimconemyWindow.LogFallbackOnce(source, callerName)` ist die zentrale Memo-Schleuse. Eine nicht-überschreibende Subklasse loggt **genau einmal pro Caller-Type** über die Game-Lifetime — keine 60-FPS-Flut im Player-Log.

---

## K — Owner-Invariante: verifiziert

| Capability | Owner (aktuelle Code-Anker) | Verifiziert via |
|---|---|---|
| `rimconemy.foundation.profile` (01, Read) | Foundation.Profile.ProfileDetector | Bestätigt via Code-Reading |
| `rimconemy.foundation.eventlog` (01, Write) | Foundation.Events.EventLog | Bestätigt via Code-Reading |
| `rimconemy.foundation.colonials` (01, Read) | Foundation.Colonials.ColonialReader | Bestätigt via Code-Reading |
| `rimconemy.survivalprogression.gameover` (02, Sole-Owner) | ProgressionGameComponent.UpdateRuntimeState | Code-Search zeigte **keinen zweiten Caller** von `CheckOrUpdateGameOver` |
| `rimconemy.scavengerinfrastructure.resources` (03, Owner) | StorageQuery.ReadStorage | Bestätigt via Code-Reading |
| `rimconemy.scavengerinfrastructure.power` (03, Owner) | PowerChainService | Bestätigt via Code-Reading |
| `rimconemy.economyterritory.wallet` (04, Owner) | CreditsLedger + WalletService | Bestätigt via Code-Reading — keine Reader in 02/03/05 |
| `rimconemy.economyterritory.outposts` (04, Owner) | Outpost + OutpostService | Bestätigt via Code-Reading — kein Reader-Code in 02/03/05 |
| `rimconemy.infectedautomation.threat` (05, Owner) | ThreatAggregator | Bestätigt via Code-Reading in Foundation-Doku |

**Schreibrechte-Invariante (INTERFACE_CONTRACT §9.1)** ist in dieser Phase 0 nicht verletzt worden.

