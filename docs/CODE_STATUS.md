# Rimconemy — Code-Status und Beleggrenze

> **Stand:** 2026-08-04  
> **Basis:** lokaler Codebestand, RimWorld 1.6.4566, `scripts/runtime_test.sh`  
> **Zweck:** Diese Datei ist die code-nahe Statusreferenz. Planungstexte und Verkaufsbeschreibungen gelten nur insoweit als umgesetzt, wie hier ein konkreter Code-/Def-/Runtime-Beleg angegeben ist.

## 1. Belegstufen

| Stufe | Bedeutung |
|---|---|
| `CODE` | Implementierung ist im Repository vorhanden und auffindbar. |
| `DEF` | RimWorld-Def/XML-Struktur ist im Repository vorhanden. |
| `COMPILES` | Lokaler Build gegen RimWorld-1.6-Assemblies ist erfolgreich. |
| `BOOT` | Runtime-Bootstrap/Def-Load wurde im `Player.log` beobachtet. |
| `LIVE` | Verhalten wurde im echten Spielablauf beobachtet. |
| `OPEN` | Kein ausreichender Beleg; nicht als geliefert behandeln. |

`CODE`, `DEF` oder `COMPILES` sind kein Ersatz für `LIVE`.

## 2. Paketstatus

### 01 — Foundation

**Belegt:** `CODE`, `COMPILES`, `BOOT`

- `PackageRegistry` registriert Foundation und spät geladene Feature-Assemblies.
- `ProfileDetector` unterscheidet `Standalone`, `Partial` und `FullOverhaul` anhand registrierter Pakete, DLCs und Save-Schema-Kompatibilität.
- `CapabilityAudit`, `ColonialReader`, `DLCFilter` und Foundation-UI-Toolkit sind vorhanden.
- `FoundationSaveData` persistiert Profil-/Paketdiagnose, Eventlog, Schema v1 und Sandbox-Flag.
- `EventLog` besitzt deduplizierte Append-Only-Historie und escape-aware Save-Envelope.
- Bootstrap führt Capability-, Cross-Package- und EventLog-Regressionstests aus.

**Nicht belegt:** vollständige Standalone-Dashboard-Abdeckung, alle Save-/Map-/DLC-Kombinationen und der Falsifizierungsbericht `SURVIVED`.

### 02 — Survival & Progression

**Belegt:** `CODE`, `DEF`, `COMPILES`, `BOOT`

- `NeedMappingService` liest Vanilla `Food`, `Rest`/Health und `Recreation`/`Joy` auf eine 0..1-Setting-Skala.
- `Rimconemy_Need_*`-Defs verwenden die konkrete, inerte `Need_SettingIdentity`; sie werden absichtlich nicht an Pawns angehängt.
- `ProgressionGameComponent` aktualisiert Read-Models alle 250 Ticks, persistiert Snapshots/Research/Schema v1 und besitzt einen Sole-Owner-Game-Over-Pfad.
- `CharacterSetup` setzt Alter 18/18, bietet das kostenbewusste Skillbudget 30 für zwölf Skills und weist Traits über den Budget-Balance-Pfad zu.
- `Page_ConfigureStartingPawnsBioPatch` und `SkillBudgetWindow` sind vorhanden.
- Sandbox-Szenario und `ScenPart_StartInSandbox` sind als Def-/C#-Vertrag vorhanden.
- Bootstrap führt Bio-Remap-, Scenario-, Need- und Building-XP-Regressionstests aus; der XP-Adapter dedupliziert validierte Output-Keys, ist aber noch nicht an einen Live-Bau-Job-Hook angeschlossen.

**Nicht belegt:** vollständiger Research-Graph, vollständige Job-/Output-XP-Integration, persistenter Character-Setup-State als eigener Vertrag und interaktiver Save/Load-Live-Test.

### 03 — Scavenger Infrastructure

**Belegt:** `CODE`, `DEF`, `COMPILES`, `BOOT`

- `StorageSnapshot` und `StorageQuery.ReadStorage()` bilden das Read-only-Storage-Modell.
- Storage wird aus geladenen Maps und tatsächlichen Lagerorten (`Zone_Stockpile`, `Building_Storage`) gelesen; Pawn-Inventare und nicht gelagerte Bodenobjekte werden ausgeschlossen.
- Snapshots sind nach ResourceId sortiert, besitzen ContentHash und einen 250-Tick-Cache.
- `PowerChainService` liest Generator-/Turbinen-/Wasserpumpen-/Pfeilturm-Defs im Player-Home-Map-Scope und liefert einen deterministischen `PowerChainSnapshot` mit stabiler Einheiten-Signatur.
- `BuildingSnapshotService` rekonstruiert versionierte, read-only Building-Snapshots mit Bau-, Power-, Fuel-, Damage-, Input- und Owner-Feldern.
- Der InfrastructureDashboard zeigt Building-/Power-/Storage-Read-Models; Foundation registriert die Capability `rimconemy.scavengerinfrastructure.building`.
- Construction-Debris-, Hemp-, Water- und Power-Defs/Marker sowie der konditionale Wall-/Door-`Stony`-Patch sind vorhanden.
- Bootstrap führt `BuildingCore`-Regressionstests aus; der frische Runtime-Gate-Lauf enthält den Building-Summary-Marker.

**Nicht belegt:** vollständiger Bau-/Farm-/Wasserverbrauchs-Loop im echten Spiel, tatsächliche Wall-/Door-Materialauswahl, Caravan-/Temporary-Map-Enumeration, echte Fuel-/Power-Transition, echte Turmspielmechanik und Save/Load-Live-Gate.

### 04 — Economy & Territory

**Belegt:** `CODE`, `COMPILES`, `BOOT`

- `CreditsLedger` ist persistierbare Wallet-Domäne mit 256er History-Fenster, separatem `Key → TxId`-Idempotenzindex, Overflow-/Underflow-Rejection und `ActualAmount`.
- `Market` besitzt deterministische lokale Preisberechnung, Orders, parallele Scribe-Listen und `marketSnapshot`-Save-Envelope.
- `Outpost` besitzt eine State Machine (`Planned`, `Active`, `Blocked`, `Disconnected`, `Ruined`) und absolute Tick-Werte.
- Economy-Bootstrap führt CreditsLedger-, Market-Persistence- und Building-Input-Regressionstests aus; physische Building-Inputs bleiben von Credits getrennt, Transfer/Booking ist für Meilenstein B offen.

**Nicht belegt:** atomare physische Waren-/Wallet-Transaktionen, echter WorldObject-/Proxy-Graph, vollständige Weltkartenlogistik und interaktiver Save/Load-Live-Test.

### 05 — Infected & Automation

**Belegt:** `CODE`, `DEF`, `COMPILES`, `BOOT`

- `SettingProfile`, `SituationSnapshot`, `StoryEventSpec`, `StoryEventCatalog`, `DeterministicRng`, `StorySelector` und `StoryState` bilden den deterministischen Story-Layer.
- `StoryState` persistiert Cooldowns, Selection-Seed, Snapshot-Hash, Idempotency-Keys und deren Ticks; Save/Load-Rebuild ist implementiert.
- `StoryDirector` evaluiert standardmäßig täglich, liest Storage bei vorhandener Capability und queued das definierte Incident über RimWorlds Storyteller.
- `InfectedRaidWorker` ist weiterhin ein Letter-/Incident-Pfad; der vollständige Infizierten-Raid-Spawn ist nicht belegt.
- `MechadroidUnit`, Threat-/Automation-Record-Typen und Ideology-ResourceFairness-Adapter sind vorhanden.
- Bootstrap führt StorySelector-, StoryState- und Building-Threat-Regressionstests aus; der Threat-Adapter ist bounded/deterministisch und erzeugt in A weder Incident noch Raid.

**Nicht belegt:** vollständiger Infizierten-Spawn, eigener `StorytellerDef`/`StorytellerComp`, World-Map-Raids, Mechadroid-Aufträge und interaktiver Save/Load-/Event-Fire-Live-Test.

## 3. Gemeinsame Runtime-Belege

Der kanonische Boot-Test ist:

```bash
./scripts/runtime_test.sh --require-scenario-tests
```

Das Skript:

1. baut und deployt über `scripts/deploy.sh`, sofern nicht übersprungen;
2. prüft alle fünf installierten `About.xml`-Dateien, Paket-IDs, RimWorld-1.6-Support und DLLs;
3. startet RimWorld mit begrenztem Timeout;
4. verlangt einen tatsächlich veränderten Player.log mit Hash-/Metadatenvergleich;
5. prüft RimWorld-1.6-, Foundation-Bootstrap-, FullOverhaul- und Registry-Marker;
6. verweigert alte Logs bei unveränderter Signatur;
7. prüft auf Need-, Sandbox-, Patch- und Market-Scribe-Fehler;    8. prüft alle aktuell emittierten Boot-Regression-Summaries einschließlich der fünf Building-Summaries;
9. schreibt einen timestamped Report mit Exit-Status.

Der sichere lokale Installationscheck ohne Spielstart ist:

```bash
./scripts/runtime_test.sh --skip-start --no-deploy
```

`--skip-start` belegt nur installierte Artefakte. Es belegt weder Def-Load noch Save/Load oder Event-Fire.

## 4. Explizit offene Live-Gates

- Save → Spiel beenden → Reload mit unverändertem Foundation-, Progression-, Market- und StoryState.
- Event-Auswahl → Queue → Worker/Letter beziehungsweise echter Raidpfad im laufenden Spiel.
- Kartenwechsel, Caravan-/Temporary-Maps und unloaded Storage.
- vollständiger Infizierten-Spawn statt Letter-only Worker.
- vollständige Ideology-Einflussmatrix: aktuell ist `ResourceFairness` der belegte Code-Adapter; weitere Regeln sind Spezifikation.
- vollständige Scavenger-Bau-/Farm-/Wasser-/Turmmechanik.
- Economy-WorldObject-/Transfer-/Territory-Lifecycle.

## 5. Änderungsregel für diese Statusdatei

Bei Code-/Def-Änderungen werden nur Belege ergänzt, die durch Datei, Build oder frischen Runtime-Log nachvollziehbar sind. Ein geplantes Artefakt darf erst von `OPEN` auf `CODE`, `DEF`, `COMPILES`, `BOOT` oder `LIVE` wechseln, wenn die jeweilige Evidenz vorhanden ist.
