# Rimconemy — Code-Status und Beleggrenze

> **SSOT-Owner für:** Was ist `COMPILED` vs `LOADED` vs `RUNNING`/live; live-Beleg-Status aller Pakete. Wer ein Topic aus [docs/INDEX.md §1](INDEX.md) hier behandelt, hält eine SSOT-Verletzung fest.
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
- Bootstrap führt Bio-Remap-, Scenario-, Need-, Building-XP- und SchemaBump-Regressionstests aus; der XP-Adapter dedupliziert validierte Output-Keys, ist aber noch nicht an einen Live-Bau-Job-Hook angeschlossen.
- `CharacterSetupState` implementiert `ISchemaMigratable` (Foundation/Source/Save/) als First-Class-Domain für Save/Load-Schema-Migration. `MigrationStepWalker` + `MigrationRegistry` + `SchemaMigratableExtensions` sind gemeinsame Foundation-Orchestration. Phase-2.8-Beleg via `Tests/CharacterSetupStateSchemaBumpTests.RunAll()` (T1–T6) + `docs/falsification/survival__SaveMigration.md` (standalone Falsifizierungsbericht, 236 Z.). `StoryState` (05) und `CreditsLedger` (04) nutzen dasselbe Interface.

**Nicht belegt:** organischer Erfahrungsbaum mit echten Action-Completion-Hooks, Architektenfreigaben, vollständiger Job-/Output-Erfahrungsintegration, echter Save/Load-Roundtrip via Runtime-Game-Save-File, interaktiver Live-Test. `ResearchProjectDef.IsFinished` ist aktuell nur ein Forschungs-Read-Model und kein belegter Rimconemy-Unlockpfad.

**Neue Designentscheidung (noch kein Code-/Def-Beleg):** Die Early-Game-Waffe und Startmunition gehören in den Charakter-/Szenariovertrag; Gegner liefern keinen garantierten Drop, Ruinen-Loot bleibt zufällig, und fehlende Munition deaktiviert keine Arbeitstypen. Die spätere T2-Energy-Kette lautet: Stahl als Rezeptinput, Kohle über die Ofen-Refuelable-Mechanik für ausgewählte Rezepte, Kohle separat im Generator für das PowerNet.

**Neue Progressionsentscheidung (Design, noch kein Code-/Def-Beleg):** Rimconemy ersetzt die primäre Forschungs-Tisch-/Punkte-Logik durch einen Erfahrungsbaum: bestätigte Sammel-, Feuer-, Bau-, Verarbeitungs-, Energie- und Verteidigungsergebnisse geben genau einmal Erfahrung und können das Architektenmenü erweitern. Vanilla-`ResearchProjectDef`s bleiben als Kompatibilitätsschicht; der Forschungstisch ist keine notwendige Rimconemy-Freigabequelle.

### 03 — Scavenger Infrastructure

**Belegt:** `CODE`, `DEF`, `COMPILES`, `BOOT`

- `StorageSnapshot` und `StorageQuery.ReadStorage()` bilden das Read-only-Storage-Modell.
- Storage wird aus geladenen Maps und tatsächlichen Lagerorten (`Zone_Stockpile`, `Building_Storage`) gelesen; Pawn-Inventare und nicht gelagerte Bodenobjekte werden ausgeschlossen.
- Snapshots sind nach ResourceId sortiert, besitzen ContentHash und einen 250-Tick-Cache.
- `PowerChainService` liest Generator-/Turbinen-/Wasserpumpen-/Pfeilturm-Defs im Player-Home-Map-Scope und liefert einen deterministischen `PowerChainSnapshot` mit stabiler Einheiten-Signatur.
- `BuildingSnapshotService` rekonstruiert versionierte, read-only Building-Snapshots mit Bau-, Power-, Fuel-, Damage-, Input- und Owner-Feldern.
- Der InfrastructureDashboard zeigt Building-/Power-/Storage-Read-Models; Foundation registriert die Capability `rimconemy.scavengerinfrastructure.building`.
- Construction-Debris-, Hemp-, Water- und Power-Defs/Marker sowie der konditionale Wall-/Door-`Stony`-Patch sind vorhanden.
- **Arbeitsstand (CODE, Compile-/Runtime-Beleg für diese Ergänzung offen):** `StorageWriteMutationService` fordert nach erfolgreicher Wall-Blueprint-Platzierung einen best-effort Bauschutt-Abzug an (Teilabzug möglich, kein atomarer Rollback); `InfrastructureDashboard` bietet dafür eine Action; der Storage-Write-Pfad besitzt einen deterministischen Test-Seam. Die Screenshot-Serie vom 2026-08-04 (21:08:51–21:09:15) belegt zusätzlich, dass Architect-Designator, Campfire, Generatoren und die Infrastructure-Navigation im laufenden Spiel sichtbar sind; sie belegt keinen vollständigen Stack-Abzug, Vanilla-Baufortschritt oder Save/Load-Lifecycle.
- **Neu (P0 Coal Chain):** `Rimconemy_Coal` (ThingDef), `Rimconemy_MachineParts` (ThingDef), `Rimconemy_CraftingStations` (ThingCategoryDef).
- **Neu (P0 Recipes):** `Rimconemy_MakeCoal` (3 WoodLog + 2 HempLeafy → 4 Coal @ Campfire), `Rimconemy_SalvageMachineParts` (5 SteelScraps → 1 MachineParts @ Campfire).
- **Neu (P0 Generator):** WoodCoalGenerator besitzt separaten Refuelable für Coal mit `fuelConsumptionRate=0.67` (1.5× Effizienz ggü. WoodLog/Chemfuel 1.0).
- **Neu (P0 Campfire):** 3 Rezepte wired: BurnSteelScraps, MakeCoal, SalvageMachineParts.
- Bootstrap führt `BuildingCore`-Regressionstests aus; der frische Runtime-Gate-Lauf enthält den Building-Summary-Marker.

**Nicht belegt:** vollständiger Bau-/Farm-/Wasserverbrauchs-Loop im echten Spiel, end-to-end Live-Beleg des Bauschutt-Blueprint- und Storage-Abzugs, tatsächliche Wall-/Door-Materialauswahl, Caravan-/Temporary-Map-Enumeration, echte Fuel-/Power-Transition, echte Turmspielmechanik, **Live-Test der Coal-Kette (MakeCoal → Generator-Effizienz, SalvageMachineParts-Ausbeute)**, elektrischer T2-Hochofen, physischer Munitionsverbrauch/-output und Save/Load-Live-Gate.

**Neue Designentscheidung (Planung, nicht Implementierungsbeleg):** Der elektrische Hochofen soll nach dem Survival-/Power-Fundament als T2-Energy-Capability mit Kalkstein, Sandstein oder Granit sowie Eisen/Stahl gebaut werden. Er soll Kohle und Stahl physisch zu Munition verarbeiten; konkrete Defs, Rezepte, Energie- und Verbrauchsregeln sind noch offen.

### 04 — Economy & Territory

**Belegt:** `CODE`, `COMPILES`, `BOOT`

- `CreditsLedger` ist persistierbare Wallet-Domäne mit 256er History-Fenster, separatem `Key → TxId`-Idempotenzindex, Overflow-/Underflow-Rejection und `ActualAmount`.
- `Market` besitzt deterministische lokale Preisberechnung, Orders, parallele Scribe-Listen und `marketSnapshot`-Save-Envelope.
- `Outpost` besitzt eine State Machine (`Planned`, `Active`, `Blocked`, `Disconnected`, `Ruined`) und absolute Tick-Werte.
- Economy-Bootstrap führt CreditsLedger-, Market-Persistence- und Building-Input-Regressionstests aus; physische Building-Inputs bleiben von Credits getrennt, Transfer/Booking ist für Meilenstein B offen.
- `CreditsLedger` implementiert `ISchemaMigratable` (Foundation/Source/Save/) — Schema-Version via `Scribe_Values.Look`, Migration via `this.RunMigration()`, `Tests/CreditsLedgerSchemaBumpTests` mit T1–T6-Assertions.

**Nicht belegt:** atomare physische Waren-/Wallet-Transaktionen, echter WorldObject-/Proxy-Graph, vollständige Weltkartenlogistik und interaktiver Save/Load-Live-Test.

### 05 — Infected & Automation

**Belegt:** `CODE`, `DEF`, `COMPILES`, `BOOT`

- `SettingProfile`, `SituationSnapshot`, `StoryEventSpec`, `StoryEventCatalog`, `DeterministicRng`, `StorySelector` und `StoryState` bilden den deterministischen Story-Layer.
- `StoryState` persistiert Cooldowns, Selection-Seed, Snapshot-Hash, Idempotency-Keys und deren Ticks; Save/Load-Rebuild ist implementiert.
- `StoryDirector` evaluiert standardmäßig täglich, liest Storage bei vorhandener Capability und queued das definierte Incident über RimWorlds Storyteller.
- `InfectedRaidWorker` ist eine Letter-/Incident-Pfad mit echter Spawn-Bridge (Audit-Bündel C / F-09, 2026-08-04): `HiddenInfected`-Fraktion und `InfectedRavager`-PawnKind sind als DEF vorhanden; der Worker spawnt jetzt bis zu `MaxSpawnsPerWorkerRun=3` Pawns aus einem threat-basierten `SpawnPlan` auf Edge-Zellen der Zielkarte. Der Live-Runtime-Beleg (echter Save → echter Spawn → echter Load) ist nicht durch automatisierte Tests abgedeckt; er ist explizit User-Live-Test-Verantwortung über `scripts/runtime_test.sh`.
- `WorldRaidCoordinator` plant druckabhängige Raid-Fenster für Weltkarten-Tiles; WorldObject-Erzeugung, Ankunft, Auflösung und Save/Load-Lifecycle bleiben offen.
- `StoryState` implementiert `ISchemaMigratable` (Foundation/Source/Save/) — Schema-Version via `Scribe_Values.Look`, Migration via `this.RunMigration()`, `Tests/StoryStateSchemaBumpTests` mit T1–T6-Assertions.
- `MechadroidJobLedger` (State-Machine für Mechadroid-Aufträge), Threat-/Automation-Record-Typen und Ideology-ResourceFairness-Adapter sind vorhanden. (`MechadroidUnit`/`MechadroidJobRegistry`-Stubs entfernt 2026-08-05, siehe `docs/falsification/deadcode-audit-2026-08-05.md`.)
- Bootstrap führt StorySelector-, StoryState- und Building-Threat-Regressionstests aus; der Threat-Adapter ist bounded/deterministisch und erzeugt in A weder Incident noch Raid.

**Nicht belegt:** vollständige Raid-Skalierung und -Auflösung (der Arbeitsstand ist auf maximal einen Pawn begrenzt), eigener `StorytellerDef`/`StorytellerComp`, vollständiger World-Map-Raid-Lifecycle, Mechadroid-Aufträge und interaktiver Save/Load-/Event-Fire-Live-Test.

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

> **Vertikal-Scheibe „Die erste Nacht" (2026-08-04):** Der operative Plan für die nächste geschlossene Spielscheibe steht in `ROADMAP.md §9.1` (integriert, Plan-Datei gelöscht). Die Vertikal-Scheibe umfasst 12 Phasen mit 37 Subtasks (Phase 0 API/DLC → Phase 12 Research/DLC-Kompatibilität). Ihr `LIVE`-Beleg erfordert den Release-Ablauf aus Phase 0–7.3 inkl. Phase 8.1–8.4 und Phase 9.1–9.4 des Plans: `Single Survivor → Campfire → Tier-1-Barrikade → 1 Nacht → Save/Load ohne Drift`. Erst danach beginnen die Folgesprints Kohle, T2-Energy-Hochofen und Automation.
>
> **Primär-Status der Vertikalscheibe:** Die unten gelisteten Live-Gates sind nach Phasen geordnet. Phase 0–9.4 haben Vorrang vor Phase 10+. Save/Load, vollständiger Raid-Pfad, Idea/Mood-/Need-Snapshots und voller Bau-/Farm-/Wasser-/Turmloop werden erst nach der Vertikalscheibe als vollständiges `LIVE` anerkannt.



- Save → Spiel beenden → Reload mit unverändertem Foundation-, Progression-, Market- und StoryState.
- Event-Auswahl → Queue → Worker/Letter beziehungsweise echter Raidpfad im laufenden Spiel.
- Kartenwechsel, Caravan-/Temporary-Maps und unloaded Storage.
- vollständiger Infizierten-Spawn statt Letter-only Worker.
- vollständige Ideology-Einflussmatrix: aktuell ist `ResourceFairness` der belegte Code-Adapter; weitere Regeln sind Spezifikation.
- vollständige Scavenger-Bau-/Farm-/Wasser-/Turmmechanik.
- Economy-WorldObject-/Transfer-/Territory-Lifecycle.

## 5. Änderungsregel für diese Statusdatei

Bei Code-/Def-Änderungen werden nur Belege ergänzt, die durch Datei, Build oder frischen Runtime-Log nachvollziehbar sind. Ein geplantes Artefakt darf erst von `OPEN` auf `CODE`, `DEF`, `COMPILES`, `BOOT` oder `LIVE` wechseln, wenn die jeweilige Evidenz vorhanden ist.
