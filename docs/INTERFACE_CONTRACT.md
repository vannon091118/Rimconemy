# INTERFACE_CONTRACT.md — Rimconemy Cross-Package Interfaces

> **SSOT-Owner für:** Paket-Compile-Dependencies, Paket-Eigentumsgrenzen (welches Package besitzt was), Servicebus-Vertrag & Capability-IDs, Read/Write-Lanes, 5-Paket-Owner-Matrix. Wer ein Topic aus [docs/INDEX.md §1](INDEX.md) hier behandelt, hält eine SSOT-Verletzung fest.
> **Stand:** 2026-08-04  | Owner: Foundation (01) | Code-Status-Referenz: `docs/CODE_STATUS.md`

## Zweck

Pakete kompilieren unabhängig ohne Projekt-Referenzen. Schnittstellen
werden über den Foundation-Servicebus und versionierte Capabilities definiert.
Dieses Dokument ist die verbindliche Referenz für alle Paket-Entwickler.

---

## 1. ServiceBus (Phase-3-Plan, kein Konsument bisher)

> **Stand 2026-08-04 (Audit-Falsifizierung):** Der zentrale `FoundationServiceBus`
> wurde aus dem Code entfernt, weil weder Publisher noch Subscriber außerhalb
> von `Foundation` existierten. Die Thread-Maschinerie
> (`Monitor.Wait`, `[ThreadStatic]`, `WouldCreateWaitCycle`) war reine
> Hypothese. Cross-Paket-Kommunikation erfolgt aktuell nur über:
> - `PackageRegistry.IsRegistered(packageId)` und `HasCapability(...)`.
> - Direkte Adapter-Schichten, z.B. `StorageQuery` (Paket 03) für Paket 05.

### Wieder-Einführungs-Bedingungen (Phase 3+)

Der zurückkehrende Bus wird:
- **Main-thread-only** sein — RimWorld-Spielcode ist single-threaded.
- **Typed Topics** tragen (z.B. `Subscribe<StorageSnapshot>("storage.snapshot.updated", ...)`).
- **Keine** Thread-Synchronisation benötigen, dafür Dictionary<key, handler>
  + Limit-LRU + Append-Only-Event-Log für Repro.
- **Erst** eingeführt, sobald der erste reale Konsument (Publisher oder
  Subscriber) benannt ist. Solange bleibt das Schéma **geplant**, nicht
  implementiert.

### Geplante Topics (Treiber-Liste, nicht Vertrag)

| Topic | Treiber | Vermutlicher Subscriber | Daten |
|---|---|---|---|
| `storage.snapshot.updated` | 03 | 05, 04 | `StorageSnapshot` |
| `threat.pressure.changed` | 05 | 02, 04 | `float` (0.0–1.0) |
| `progression.xp_awarded` | 02 | 05 | `(pawnId, xp, domain)` |
| `economy.wallet.changed` | 04 | 05 | `(balance, delta)` |
| `game.over.imminent` | 02 | 05 | `GameOverReason` |

Diese Liste bleibt informativ; Vertrag folgt erst mit der Implementierung.

---

## 2. Capabilities (Package 01 → alle)

Jedes Paket deklariert versionierte Capabilities bei der Registry.
Andere Pakete prüfen `HasCapability()` vor der Nutzung.

Stand 2026-08-04 (Phase-B-Sprint): §2 und §9 sind kanonisch konsolidiert.
§9 referenziert diese Tabelle und erweitert nur um Cross-Package-Ownership-Aspekte.

| Capability ID | Paket | Version | Beschreibung |
|---|---|---|---|
| `rimconemy.foundation.profile` | 01 | 1 | Profil-Erkennung |
| `rimconemy.foundation.eventlog` | 01 | 1 | Append-Only-Event-Log |
| `rimconemy.foundation.save_diagnosis` | 01 | 1 | Save-State-Diagnose |
| `rimconemy.foundation.colonials` | 01 | 1 | **Phase-B / F-V1:** `ColonialReader.GetActiveColonists()` (single source of truth) |
| `rimconemy.foundation.dlc_filter` | 01 | 1 | **DLC-Policy:** `DLCFilter.IsContentEnabled(contentId)` — zentraler Gate für alle DLC-Content-Entscheidungen (DECISIONS §15/§20) |
| `rimconemy.survivalprogression.needs` | 02 | 1 | Need-Read-Models |
| `rimconemy.survivalprogression.progression` | 02 | 1 | XP/Progression |
| `rimconemy.survivalprogression.gameover` | 02 | 1 | **Phase-B / F-V2:** Sole-Owner GameOver-Trigger |
| `rimconemy.scavengerinfrastructure.resources` | 03 | 1 | Storage-Snapshot |
| `rimconemy.scavengerinfrastructure.power` | 03 | 1 | Power-Chain |
| `rimconemy.economyterritory.wallet` | 04 | 1 | Credits-Ledger |
| `rimconemy.economyterritory.market` | 04 | 1 | Markt-Preise |
| `rimconemy.economyterritory.outposts` | 04 | 1 | Outpost-Domain |
| `rimconemy.infectedautomation.threat` | 05 | 1 | Threat-Aggregator |
| `rimconemy.infectedautomation.automation` | 05 | 1 | Mechadroid/Aufträge/Ideology-Layer |

---

## 3. StorageSnapshot-Bridge (Package 03 → Package 05)

**Status:** Code-Bridge vorhanden, vollständige Runtime-/Gameplay-Abdeckung offen.

`StoryDirector.AssignStorageHashFromCapability()` prüft die Capability
`rimconemy.scavengerinfrastructure.resources` und ruft bei aktivem Paket 03
`StorageQuery.ReadStorage(StorageScope.PlayerHomeMaps, null, tick)` auf. Der
StoryDirector übernimmt den Storage-ContentHash, setzt `AnyResourceCritical`
über `ResourceThresholds` und verwendet im Fehler-/Standalone-Fall einen
deterministischen FNV-1a-Fallback. Die Bridge ist damit kein reiner Phase-3-Plan
mehr; UI-/Economy-Konsum, Caravan-/Temporary-Map-Abdeckung und Live-Save-
Konsistenz bleiben offen.

Der `FoundationServiceBus` bleibt weiterhin entfernt. Cross-Package-Kommunikation
läuft aktuell über Registry-/Capability-Gates und die direkte, late-bound
Assembly-Grenze zwischen Paket 03 und 05.

Die folgende Bus-API ist Zukunftsplanung, kein aktueller Vertrag:

```csharp
// In StoryDirector.FinalizeInit():
FutureBus.Subscribe<StorageSnapshot>(
    "storage.snapshot.updated",
    snapshot => _lastStorageSnapshot = snapshot);

// In BuildLiveSnapshot():
snapshot.StorageHash = _lastStorageSnapshot?.ContentHash ?? "live-" + tick;
snapshot.AnyResourceCritical = _lastStorageSnapshot?.Entries.Any(
    e => e.TotalAmount < CriticalThreshold) ?? false;
```

**Aktueller Stand:** Bei aktivem Paket 03 ist der Hash ressourcen-sensitiv,
weil `StorageQuery.ReadStorage()` die gelagerten ResourceIds und Mengen hasht.
Im Standalone-/Fehler-Fallback bleibt `live-<tick>` deterministisch, aber nicht
ressourcen-sensitiv. Vollständige UI-/Economy-Nutzung sowie Caravan-/Temporary-
Map-Abdeckung bleiben offen.

---

## 4. SituationSnapshot-Vertrag

`SituationSnapshot` ist das Read-Model, das StorySelector für die
Event-Auswahl benötigt. Es wird vom StoryDirector aus Live-Daten gebaut; bei
registriertem Paket 03 werden die Storage-Felder über die capability-gated
`StorageQuery.ReadStorage()`-Bridge aus geladenen Player-Home-Maps befüllt.

### Pflichtfelder (P1)

| Feld | Quelle | Kritisch? |
|---|---|---|
| `GameTick` | `Find.TickManager.TicksGame` | Ja — Determinismus |
| `SurvivorCount` | `Find.Maps` → `FreeColonistsSpawned` | Ja — Event-Gating |
| `ThreatPressure` | `map.wealthWatcher.WealthTotal` | Ja — Event-Filter |
| `ActiveEventIds` | `StoryState.ActiveEventIds` | Ja — Idempotenz |
| `StorageHash` | `StorageSnapshot.ContentHash` über `StoryDirector.AssignStorageHashFromCapability()` | Ja — Bridge vorhanden; Caravan-/Temporary-Map-Abdeckung offen |

### Optionale Felder (P2+)

| Feld | Quelle | Wann |
|---|---|---|
| `IdeologyTension` | `IdeoManager` | Phase 2 |
| `ActiveSettingRuleCount` | ThoughtWorker-Registry | Phase 2 |
| `AverageSurvivorHealth` | `pawn.health.summaryHealth` | Phase 1 ✅ |
| `AnyResourceCritical` | `StorageQuery` | Phase 3 |

---

## 5. Save-Vertrag

Jedes Paket speichert seinen State über `GameComponent.ExposeData()` via
RimWorlds Scribe-System. Foundation (`FoundationSaveData`) ist das
kanonische Save-Modell für Schema-Version und Migration.

| Paket | Save-Klasse | Scribe-Mode |
|---|---|---|
| 01 | `FoundationSaveData` | `Scribe_Deep.Look` |
| 02 | `ProgressionGameComponent` | `Scribe_Collections.Look` |
| 03 | `StorageQuery` (static, cache-only) | Snapshot wird aus Map-Storage rekonstruiert; kein eigener Save-State |
| 04 | `CreditsLedger`, `Market`/`MapMarketComponent` | IExposable-/Scribe-Envelopes vorhanden; vollständiger World-/Transfer-State offen |
| 05 | `StoryDirector` → `StoryState` | `Scribe_Deep.Look` |

### Migrations-Regeln

- Schema-Version wird in FoundationSaveData erhöht
- `FinalizeInit()` prüft `SchemaVersion < CurrentSchemaVersion` → Migration
- Alte Felder werden gelesen (`Scribe_Values.Look`) und auf neue Defaults gemappt
- Kein stiller Datenverlust: bei unbekannten Schema-Versionen → Warn-Log + Defaults

---

## 6. Performance-Gates

| Gate | Schwellwert | Messung |
|---|---|---|
| P1 | ≤2 ms avg / Update | `Stopwatch` in `GameComponentTick` |
| P2 | ≤5 ms p99 / Update | 1000-Sample-Ringbuffer |
| P3 | ≤1 MiB/Tag Speicher | `GC.GetTotalMemory` Delta |

---

## 7. Änderungshistorie

| Datum | Version | Änderung |
|---|---|---|
| 2026-08-04 | 1.0 | Initial. Definiert ServiceBus-Topics, Capabilities, Snapshot-Vertrag, Save-Regeln. |
| 2026-08-04 | 1.1 | Audit-Falsifizierung: `FoundationServiceBus` entfernt (kein Konsument). §1 als Phase-3-Plan markiert. Capability `rimconemy.foundation.servicebus` aus §2 entfernt. §3-Storage-Bridge Ziel angepasst. |
| 2026-08-04 | 1.2 | Phase 0-A §8 dokumentiert: Toolkit-Token, Base-Classes, Helpers, ThemeSettings, GlobalThemeOverride. DLL-Cross-Ref-Pattern (Mod 02 → Mod 01) freigegeben; Projekt-Ref bleibt verboten. |
| 2026-08-04 | 1.3 | Phase B Sprint: §9 Architecture Boundaries. F-V4 (Capability-Audit) + F-V2 (Sole-Owner GameOver via Reflection-Bridge) + F-V1 (ColonialReader) + F-V3 (Storage-Bridge) + F-V5 (Ideology-Grenze) abgeschlossen. Topologie: 02/03/05 ref auf 01; 05 zusätzlich ref auf 03. |

---

## 8. Phase 0-A — UI Foundation Toolkit (Package 01, ab 0.1.17 ff.)

> **Stand 2026-08-04:** UI-Toolkit shipped. Konsumenten (Pakete 02–05) binden
> sich über **DLL-Referenz** (nicht Projekt-Referenz — Projekt-Ref verboten
> nach §0) an `Rimconemy.Foundation.dll`. Load-Order erzwingt
> `<loadAfter>rimconemy.foundation</loadAfter>` in `About.xml` der Pakete.

### 8.1 Tokens — `RimconemyTheme` (Mod 01)

Statische, frozen Werte. Pakete 02–05 lesen nur; niemals überschreiben.

| Konstante | Wert | Zweck |
|---|---|---|
| `SectionSpacing` | `12f` | Abstand zwischen Sektionen |
| `IndentSize` | `16f` | Indent pro Stufe |
| `RowHeight` | `22f` | Standard-Zeilenhöhe |
| `MiniRowHeight` | `18f` | Detail-Zeile |
| `SectionTitleHeight` | `30f` | Section-Titel-Höhe |
| `SectionTitleSpacing` | `2f` | Padding unter Section-Titel |
| `Margin` | `8f` | Standard-Rand |
| `DefaultWindowPadding` | `20f` | Window-Innenrand |
| `DefaultScrollbarWidth` | `16f` | Scrollbar-Breite |
| `DefaultViewPadding` | `4f` | Scroll-View-Innenrand |
| `MinWindowWidth` / `MaxWindowWidth` | `360f` / `1200f` | Window-Klammerung |
| `MinWindowHeight` / `MaxWindowHeight` | `240f` / `800f` | dito für Höhe |
| Status-Farben | `Success`/`Warn`/`Error`/`Info`/`Muted`/`HeaderInk` | semantische Farben — kein `Color.green|red|gray|...` in 02–05 |
| `HoverDarkenAmount` | `0.05f` | Hover-Variante (Reserved) |
| `TooltipDelayMs` | `250f` | Tooltip-Delay (Reserved) |

### 8.2 Base-Classes (Mod 01)

| Klasse | Basis | Zweck |
|---|---|---|
| `RimconemyWindow` | `RimWorld.Window` | Window-Chrome vordefiniert (doClose/X + draggable + Min/Max-Größe). |
| `RimconemyMainTabWindow` | `RimWorld.MainTabWindow` | MainTab ohne `doCloseButton/X` (Bottom-Tabs). |
| `RimconemyInspectTab` | `RimWorld.InspectTabBase` | Hook für Pawn-Inspect-Tabs. |

Inherit-Hinweis: `RimconemyWindow.InitialSize` ist `Vector2.zero` als Default;
**alle Subklassen überschreiben diesen** (Default-Klammer greift sonst nicht).

### 8.3 Static Helpers — `RimconemyUi` (Mod 01)

| Methode | Zweck |
|---|---|
| `T(string key)` | Keyed-Übersetzung mit Raw-Key-Fallback (null-/empty-guarded); zentrale Übersetzungs-Helper für alle Dashboards (2026-08-05 aus lokalen `T()`-Duplikaten extrahiert) |
| `DrawSectionTitle(Rect, key, font)` | Titel-Zeile (Keyed-Lookup + `try/finally` color/font reset) |
| `DrawRow(Rect, leftLabel, rightValue, Color?)` | 2-Spalten-Zeile |
| `DrawStatusBadge(Rect, label, StatusLevel)` | Inline-Status-Badge (mit `try/finally`) |
| `DrawNeedBar(Rect, fillFraction, fillColor, label?)` | Bedarfs-/Fortschrittsbalken |
| `DrawEmptyState(Rect, messageKey)` | Leerer-Zustand-Hinweis |
| `DrawHighlightedInteractable(Rect, onClick, tooltipKey?)` | Hover+Click+Tooltip |
| `BeginStandardScrollView(viewRect, scrollOuter, ref pos, Action)` | Scroll-View-Wrapper |
| `Indent(inner, levels)` / `Section(inRect)` / `ResetTextFontAndColor()` | Layout + State-Reset |

Helper arbeiten alle mit `try/finally`-Resets auf `GUI.color` und `Text.Font`,
damit Caller-Code keinen Farb-/Font-State leakt. Konsumenten (02–05) wrappen
mit `using Rimconemy.Foundation.UI;`.

### 8.4 ThemeSettings (Mod 01, opt-in)

| Methode | Zweck |
|---|---|
| `ThemeSettings.IsOverrideEnabled` (static get) | Opt-in Flag (Scribe-persistent in `FoundationSaveData.EnableGlobalThemeOverride`). |
| `ThemeSettings.SetOverride(bool)` | Setzt das Flag zur Laufzeit. |
| `ThemeSettings.DrawSettingsRow(Listing_Standard)` | Reference-Drawer für ein Mod-Settings-Panel (Phase 1+: im Mods-Tab). |

### 8.5 GlobalThemeOverride (Mod 01, opt-in)

Reflection-only Bridge zu `RimThemes`. Wird in `FoundationSaveData.FinalizeInit()`
einmal pro Save aufgerufen, wenn Nutzer es via ThemeSettings enabled hat. **Throws
keine Exceptions** — alle Reflection-Fehler werden gefangen und geloggt.

### 8.6 DLL-Cross-Ref (02 → 01)

`mods/02-Rimconemy-Survival-Progression/Rimconemy.SurvivalProgression.csproj`
referenziert `../01-Rimconemy-Foundation/Assemblies/Rimconemy.Foundation.dll`
per `<Reference>`. **Kein ProjectReference**. About.xml hat
`<loadAfter>rimconemy.foundation</loadAfter>` → RimWorld-Loader resolved
Foundation.dll aus Mod 01's `Assemblies/` automatisch.

Pakete 03–05 können das gleiche Pattern bei Bedarf übernehmen.

---

## 9. Architecture Boundaries — Phase B Sprint (ab 2026-08-04)

> **Stand 2026-08-04:** 5 Audit-Konflikte strukturell geschlossen (F-V1 bis F-V5).
> Schalter für Cross-Package-Crossings ist `CapabilityAudit.HasCapabilityOrWarn(...)`
> in Foundation; jeder Reader MUSS diesen Gate nutzen. Solange die Fähigkeit nicht
> exposed ist, geht der Reader auf einen dokumentierten Fallback.

### 9.1 Owner-Map

> **Kanonische Capability-Liste: §2.** Diese §9.1-Tabelle wiederholt nur die
> Owner-Eigentums-Aspekte; Capability-IDs und Versionen siehe §2.

| Capability | Owner (cross-package) | Erlaubte Reader |
|---|---|---|
| `rimconemy.foundation.profile` | 01 (Read) | alle |
| `rimconemy.foundation.eventlog` | 01 (Write) | 02, 05 |
| `rimconemy.foundation.save_diagnosis` | 01 (Read) | UI |
| `rimconemy.foundation.colonials` | 01 (`ColonialReader`) | 02, 03, 05 |
| `rimconemy.survivalprogression.needs` | 02 (Read) | 05 (Snapshot) |
| `rimconemy.survivalprogression.progression` | 02 (`ProgressionGameComponent`) | 04, 05 |
| `rimconemy.survivalprogression.gameover` | 02 (**Sole-Owner** `CheckOrUpdateGameOver`) | 05 (write pending only) |
| `rimconemy.scavengerinfrastructure.resources` | 03 (`StorageSnapshot.ContentHash`) | 04, 05 |
| `rimconemy.scavengerinfrastructure.power` | 03 (Power-Chain) | 02 (XP-Domain) |
| `rimconemy.economyterritory.wallet` | 04 (CreditsLedger) | 02, 05 |
| `rimconemy.economyterritory.market` | 04 (MarketSnapshot) | 02 |
| `rimconemy.economyterritory.outposts` | 04 (OutpostState) | — |
| `rimconemy.infectedautomation.threat` | 05 (ThreatAggregator) | 02 (XP-Multiplier), 03 (Power-Drain) |
| `rimconemy.infectedautomation.automation` | 05 (Ideology/Stories) | 02 (Capability-Gate-Trigger) |

**Schreibrechte-Invariante** (Phase-B): Wer in der Spalte "Owner" steht, MUSS die einzige Quelle für Schreibvorgänge auf den gekoppelten State sein. Reader sind zu Capability-Read (`HasCapabilityOrWarn`) verpflichtet.

### 9.2 Phase-B Fixes

| Fix | Pfad | Status |
|---|---|---|
| F-V4 Capability-Audit | `Foundation/Registry/CapabilityAudit.cs` | ✅ |
| F-V2 Sole-Owner Game-Over | `Foundation/CrossPackage/CrossPackageState.cs` (Reflection-Bridge) + StoryState.MarkGameOverPending | ✅ |
| F-V1 ColonialReader | `Foundation/Colonials/ColonialReader.cs` | ✅ |
| F-V3 Storage-Bridge | `StoryDirector.AssignStorageHashFromCapability` | ✅ |
| F-V5 Ideology-Grenze | `ThoughtWorker_ResourceFairness` capability-gated | ✅ |

### 9.3 DLL-Cross-Refs (Topologie nach Phase B)

```
01 (Foundation)
   ↑ (DLL-Ref)
   ├── 02 (SurvivalProgression)
   ├── 03 (ScavengerInfrastructure)
   └── 05 (InfectedAutomation)
            ↑ (DLL-Ref, neu Phase B / F-V3)
            └── 03 (StorageQuery.ReadStorage)
            (Economy 04: late-bound reflection only, no DLL-Ref)
```

**Kein inter-Paket-Cycle.** Mod 02 hat Ref auf 01. Mod 05 hat Ref auf 01+03. Mod 03 hat Ref auf 01.
Mod 04 hat Ref auf 01 alleine.

**Audit-Bündel B / F-01 (2026-08-04):** Mod 05 → Mod 04 Wallet-balance lookup war ein
direkter Compile-Ref auf `Rimconemy.EconomyTerritory.Wallet`. INTERFACE_CONTRACT §0
verbietet non-adjacent Compile-Refs; der Ref wurde entfernt und durch
`Rimconemy.Foundation.CrossPackage.CrossPackageState.TryReadWalletBalance` ersetzt,
Capability-gate `rimconemy.economyterritory.wallet`. About.xml `loadAfter="rimconemy.economyterritory"`
bleibt unverändert (Runtime-Lade-Reihenfolge).

### 9.4 Capability-Audit (Foundation-Mirror für Cross-Package-Reads)

- `CapabilityAudit.HasCapabilityOrWarn(packageId, capabilityId, minVersion, readerContext)`:
  Boolean + einmaliger `Log.Warning` und Test-Snapshot-Eintrag.
- Test-Helpers: `ClearWarningCache()`, `Warnings()`, `WarningCount()`.

### 9.5 StorytellerRace-Free Game-Over-Ablauf

```
[Mod 05 executes raid] ─► [Mod 05 detects 0 players] ─► [StoryState.MarkGameOverPending(reason)]
                                                                                  ▲
                                                                                  │ reflection-bridge
                                                                                  │
[Mod 02 next 250-tick tick] ─► [CrossPackageState.TryReadStoryGameOverPending(reason)] ─► [PROGRESSION_GAME_OVER_TRIGGERED = reason] ─► [Find.GameEnder.CheckOrUpdateGameOver()] (Sole-Owner)
```

- **Nur Mod 02 ruft `CheckOrUpdateGameOver()`.** Mod 05 darf nur Pending setzen.
- Der Reflection-Bridge ist defensive: fehlende Mod 05 → CapabilityAudit-Warning + Fallback auf Detect-Reason.

### 9.6 Risiken-Log (Phase B)

- **R1 (DLL-Zyklus):** Aufgelöst durch Owner-Map + Read-only-Brücke.
- **R2 (Save-SchemaCompat):** StoryState-Erweiterung ist additiv; Scribe-Felder haben Standardwerte; alte Saves laden weiter.
- **R3 (Reflection-Performance):** Auf einem 250-tick-Tick-Intervall vernachlässigbar (<0.5 ms). Hot-Path-Instrumentation erst ab 100 Colonisten sinnvoll.
- **R5 (Vergessene Marker):** Beim Set-GameOverPending wurde MarkIdempotently-Missing-Pattern dokumentiert; Code-Reviewer-Pfad nach F-V2 implementation.

---

## 10. Harmony-Strategie — Minimierung zugunsten nativer Anker

> **Stand 2026-08-04:** Harmony-Strategie ist KEINE Implementierungslücke, sondern
> bewusste Design-Entscheidung. Siehe vollständige Dokumentation in `DECISIONS.md §21`.

### Prinzip

Rimconemy verwendet Harmony nur, wenn native RimWorld-Anker (`GameComponent`,
`StaticConstructorOnStartup`, `Defs`, `PatchOperation-XML`) nicht ausreichen.
Das ist die umgekehrte Priorität vieler Community-Mods, aber eine bewusste
Entscheidung für Stabilität und geringeres Kollisionsrisiko.

### Anker-Hierarchie

| Priorität | Mechanismus | Einsatzbereich |
|---|---|---|
| 1 (höchste) | `Defs` / `PatchOperation-XML` | Daten- und Inhaltsänderungen |
| 2 | `[StaticConstructorOnStartup]` | Boot-Reihenfolge, Registry, einmalige Init |
| 3 | `GameComponent` / `WorldComponent` | Runtime-State, Persistenz, Ticks |
| 4 | Harmony `[HarmonyPrefix]` / `[HarmonyPostfix]` | Nur wenn 1–3 nicht reichen |
| 5 (niedrigste) | Harmony Transpiler | Nur nach gescheitertem Prefix/Postfix-Spike |

### Aktive Patches (2026-08-04)

| Patch | Mod | Typ | Grund |
|---|---|---|---|
| `Page_ConfigureStartingPawnsBioPatch` | 02 | `[HarmonyPostfix]` | Einziger Weg, vor erstem UI-Render in `PreOpen` einzugreifen |

**Kein Transpiler im gesamten Projekt.** Alle 5 Mods nutzen `brrainz.harmony` als
`modDependency` (About.xml) — keine eigene `0Harmony.dll`.

### Wann Harmony ausgeweitet wird

- Ein Spike (z.B. `API-STORYTELLER-01`) belegt, dass Prefix/Postfix nicht reicht.
- Ein Spike (`API-ILBODY-01`) belegt, dass die Zielmethode einen IL-Body besitzt.
- Der Patch durchläuft das **BypassGate**-Verfahren (analog SyxEconomyMod).

## 11. Änderungshistorie (Fortsetzung)

| Datum | Version | Änderung |
|---|---|---|
| 2026-08-04 | 1.4 | §10 Harmony-Strategie dokumentiert: Minimierung zugunsten nativer Anker. Referenz auf AUDIT.md §1. |
| 2026-08-05 | 1.5 | §8.3: `RimconemyUi.T(string key)` als zentrale Übersetzungs-Helper ergänzt (ersetzt lokale `T()`-Duplikate in FoundationDashboard + PhaseProgressWindow; `DrawSectionTitle`/`DrawEmptyState` nutzen sie intern). |

