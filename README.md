<p align="center">
  <img src="docs/banner.jpg" alt="Rimconemy – Survival · Economy · Automation" width="100%"/>
</p>

<p align="center">
  <a href="https://github.com/vannon091118/Rimconemy/releases"><img src="https://img.shields.io/badge/version-pre--alpha-orange?style=for-the-badge&logo=github" alt="Version"/></a>
  <img src="https://img.shields.io/badge/RimWorld-1.6.4566-9cf?style=for-the-badge&logo=steam" alt="RimWorld 1.6"/>
  <img src="https://img.shields.io/badge/build-passing-brightgreen?style=for-the-badge&logo=dotnet" alt="Build"/>
  <img src="https://img.shields.io/badge/packages-5-blueviolet?style=for-the-badge" alt="5 Packages"/>
  <img src="https://img.shields.io/badge/tests-35+-success?style=for-the-badge" alt="35+ Tests"/>
  <img src="https://img.shields.io/badge/license-MIT-blue?style=for-the-badge" alt="MIT"/>
</p>

<h1 align="center">RIMCONEMY</h1>

<p align="center">
  <em>Ein modularer Survival-Economy-Overhaul für RimWorld 1.6.</em><br/>
  <strong>Wachstum erzeugt Aufmerksamkeit. Das ist kein Bug. Das ist der Spielplan.</strong>
</p>

<p align="center">
  <a href="#-gameplay-loop">Gameplay Loop</a> ·
  <a href="#-pakete">Pakete</a> ·
  <a href="#-ressourcenketten">Ressourcen</a> ·
  <a href="#-installation">Installation</a> ·
  <a href="#-status">Status</a> ·
  <a href="ROADMAP.md">Roadmap</a>
</p>

---

## 🎮 Gameplay Loop

```mermaid
flowchart TD
    SURVIVE["🪓 Überleben<br/>1 Survivor, knappe Ressourcen"] --> SCAVENGE["🔍 Bergen<br/>Bauschutt, Stahlreste, Sammeln"]
    SCAVENGE --> BUILD["🏗️ Bauen<br/>Barrikade → Campfire → Schutz"]
    BUILD --> POWER["⚡ Energie<br/>Kohle-Brennstoff → Generator → Strom"]
    POWER --> CRAFT["🔧 Verarbeiten<br/>Maschinenteile → Waffenkomponenten → Munition"]
    CRAFT --> ECON["💰 Wirtschaft<br/>Credits-Wallet, lokale Märkte, Handel"]
    ECON --> EXPAND["🗺️ Expandieren<br/>Outposts, Territorium, Weltkarte"]
    EXPAND --> AUTO["⚙️ Automatisieren<br/>Mechadroids, Proxy-Graph, Aufträge"]
    AUTO --> DEFEND["🛡️ Verteidigen<br/>Story-Events, Infizierte, Raids"]
    DEFEND --> SURVIVE
```

### Spielphasen

| Phase | Name | Beschreibung |
|---|---|---|
| 🪓 | **Überleben** | Ein Siedler, knappe Ressourcen, echte Prioritäten. Luxus ist: wenn nichts brennt. |
| 🏗️ | **Aufbauen** | Bauschutt → Strukturen. Strom, Wasser, Brennstoff — kein magisches Grid. |
| 💰 | **Wirtschaften** | Credits-Wallet, Silber als physisches Material. Lokale Preise statt universeller Tauschlogik. |
| 🗺️ | **Expandieren** | Outposts, Weltkarte, Territorium. Mehr Besitz = mehr Arbeit = mehr Angriffsfläche. |
| ⚙️ | **Automatisieren** | Mechadroids, automatisierte Raids, Outpost-Proxy-Graph. |
| 🛡️ | **Verteidigen** | Deterministische Story-Events, Bedrohungsdruck mit erklärbaren Regeln. |

---

## 🏛️ Vision

Rimconemy verwandelt RimWorld in eine tiefgreifende Überlebenssimulation, in der jedes Entscheidungsgewicht trägt. Emergente Narrative entstehen aus der Interaktion von **Wirtschaft, Ökologie und Sozialdynamik** — kein RNG ohne Erklärung, kein Wachstum ohne Aufmerksamkeit.

> **Je stärker deine Basis wird, desto lauter fragt die Welt, ob du das wirklich durchdacht hast.**

---

## 📦 Pakete

Rimconemy ist in **5 unabhängige Pakete** aufgeteilt. Jedes Paket kann einzeln geladen werden; zusammen bilden sie das **Full-Overhaul-Profil**.

```mermaid
flowchart LR
    01["🏛️ 01 Foundation<br/>Registry, Diagnose, UI Toolkit"] --> 02["🛡️ 02 Survival<br/>Bedürfnisse, Skills, Progression"]
    01 --> 03["⚡ 03 Scavenger<br/>Storage, Power, Bauschutt"]
    01 --> 04["💰 04 Economy<br/>Credits, Märkte, Outposts"]
    01 --> 05["☣️ 05 Infected<br/>Story, Threat, Mechadroids"]
    03 -.->|"Capability-Bridge"| 05
```

| Nr. | Paket | Verantwortung | Status |
|---|---|---|---|
| `01` | **🏛️ Foundation** | Registry, Diagnose, Capabilities, Save-Metadaten, UI-Toolkit, DLC-Filter, ColonialReader | ✅ BOOT |
| `02` | **🛡️ Survival & Progression** | Charakter-Setup (Alter 18, Skill-Budget, Traits), Bedürfnisse, XP-Baum, Game-Over, Save-Migration | ✅ BOOT |
| `03` | **⚡ Scavenger Infrastructure** | Physische Lagerbestände (StorageSnapshot), Power-Chain (Generator→Netz), Bauschutt, Building-Snapshots, Coal-Kette | ✅ BOOT |
| `04` | **💰 Economy & Territory** | Credits-Wallet (Ledger), deterministische Märkte, Outposts (State-Machine), Territorium, Weltkarte | ✅ BOOT |
| `05` | **☣️ Infected & Automation** | Story-Director (deterministisch), Threat-Aggregator, Infizierten-Raids, Mechadroid-Aufträge, Ideologie-Adapter | ✅ BOOT |

> **Package-Isolation:** Keine Projekt-Referenzen zwischen den Paketen. Cross-Package-Kommunikation läuft über das **Capability-System** in Foundation.

---

## ⛓️ Ressourcenketten

### Coal Chain (P0 — implementiert)

```mermaid
flowchart LR
    WOOD["🪵 WoodLog<br/>3×"] -->|"Campfire<br/>MakeCoal"| COAL["🪨 Coal<br/>4×"]
    HEMP["🌿 HempLeafy<br/>2×"] -->|"Campfire<br/>MakeCoal"| COAL
    COAL -->|"Refuelable<br/>0.67× Rate"| GEN["⚡ Generator<br/>1.5× Effizienz"]
    STEEL["🔩 SteelScraps<br/>5×"] -->|"Campfire<br/>Salvage"| PARTS["⚙️ MachineParts<br/>1×"]
```

### StainlessSteel Chain (P1 — implementiert)

```mermaid
flowchart LR
    STEEL2["🔩 Steel"] -->|"Electric Smelter"| SS["🔧 StainlessSteel"]
    PARTS2["⚙️ MachineParts"] -->|"Electric Smelter"| SS
    SS -->|"Crafting"| TOWER["🏹 ArrowTurret"]
```

### Bauschutt → Waffen-Komponente

```mermaid
flowchart LR
    DEBRIS["🧱 ConstructionDebris<br/>aus Ruinen"] -->|"Salvage"| WCOMP["🔫 WeaponComponent"]
    WCOMP -->|"T3-Crafting"| TURRET["🏹 Turret"]
```

---

## 🎮 Das Hub-Dashboard

- **🏛️ Kolonie** — Foundation-Status, DLC-Erkennung, Log, Paketübersicht
- **🛡️ Überleben** — Progression, Bedürfnisse, Ressourcen-Snapshot
- **⚡ Infrastruktur** — Power-Grid, Storage, Baufortschritt
- **💰 Wirtschaft** — Credits-Wallet, Märkte, Live-Handelsaktionen
- **☣️ Bedrohung** — Threat-Pegel, Story-Snapshot, Dev-Auswertung

**Vanilla Quick-Nav:** Per Klick in Weltkarte, Quests, Forschung, Arbeit oder Verlauf — ohne den Hub zu schließen.

---

## ✅ Status

> **Pre-Alpha:** Alle 5 Pakete bauen, booten und testen sauber. 35+ Regression-Test-Summaries grün. `runtime_test.sh`-Gate: **PASS**.

| Bereich | Status |
|---|---|
| RimWorld 1.6.4566 Build | ✅ 5/5 Pakete |
| Runtime-Boot | ✅ FullOverhaul erkannt |
| Hub-Dashboard (5 Tabs) | ✅ funktionsfähig |
| Vanilla Quick-Nav | ✅ funktionsfähig |
| Credits-Wallet | ✅ Lesen + Live-Handel |
| Power-Grid-Scan | ✅ Vanilla-Generatoren erkannt |
| Story-Director | ✅ deterministisch, 8 Events |
| Ideologie-Regeln (3) | ✅ ResourceFairness, CollectiveDefense, Transparency |
| Character-Setup | ✅ Alter 18, Skill-Budget 30, Trait-Zuweisung |
| Rollen-System | ✅ Animals/Artistic → Hunting/Smithing, BuilderDurability |
| Cooking-System | ✅ CookSkill-Comp, MealSimple/Fine/Lavish/SurvivalPack |
| ISchemaMigratable | ✅ 4 Pakete + Walker + Registry |
| Coal Chain (MakeCoal, Salvage) | ✅ DEF + Rezepte + Generator |
| StainlessSteel Chain | ✅ DEF + Rezepte + Turret |
| Save/Load-Roundtrip (Logic-Tests) | ✅ T1–T6 in 4 Paketen |
| Save/Load-Roundtrip (Runtime) | 🔄 offen |
| Vollständige Gameplay-Loops | ⬜ in Arbeit |
| Fog-of-War (Sprint 2) | 🔄 ChunkController + InfectedBehavior (Dormant→Roaming→Investigating→Assault) |
| Raid-Auflösung live | 🔄 Sprint 2 — ChunkController, Fog-of-War, Infected-Behavior-State-Machine |

→ **[Vollständige Beleggrenze](docs/CODE_STATUS.md)** · **[Falsifikations-Berichte](docs/falsification/README.md)**

---

## 🚀 Installation

| Voraussetzung | Details |
|---|---|
| **RimWorld** | 1.6.4566 |
| **Harmony** | Pflicht |
| **DLC** | Anomaly + Odyssey für Full-Overhaul; Royalty/Ideology/Biotech optional |

### Ladefolge

```
Core / DLCs → Harmony → 01 Foundation → 02 Survival → 03 Scavenger → 04 Economy → 05 Infected
```

### Developer Build

```bash
# Alle 5 Pakete bauen & deployen
./scripts/deploy.sh

# Statischer Check (kein Spielstart)
./scripts/runtime_test.sh --skip-start --no-deploy

# Vollständiger Runtime-Gate (RimWorld-Boot + 35+ Regression-Summaries)
./scripts/runtime_test.sh
```

---

## 🔧 Für Entwickler

### Architektur

| Prinzip | Beschreibung |
|---|---|
| **Package-Isolation** | Keine Compile-Abhängigkeiten zwischen 02–05 |
| **Capability-System** | Feature-Abfrage via `HasCapabilityOrWarn()` |
| **DLL-Cross-Ref** | Nur `<Reference>` auf Foundation.dll, kein ProjectReference |
| **Harmony-Minimierung** | `Defs/XML` > `StaticConstructorOnStartup` > `GameComponent` > `Harmony` |

### Wichtige Docs

| Datei | Inhalt |
|---|---|
| [`CODE_STATUS.md`](docs/CODE_STATUS.md) | Belegstufen (CODE/DEF/COMPILES/BOOT/LIVE) |
| [`DECISIONS.md`](docs/DECISIONS.md) | Architekturentscheidungen mit Begründung |
| [`INTERFACE_CONTRACT.md`](docs/INTERFACE_CONTRACT.md) | Capabilities, Package-Grenzen |
| [`SAVE_CONTRACT.md`](docs/SAVE_CONTRACT.md) | Schema-Versionierung, Migration, kein stilles Vergessen |
| [`COMPATIBILITY_MATRIX.md`](docs/COMPATIBILITY_MATRIX.md) | RimWorld-/DLC-Kompatibilität |
| [`ROADMAP.md`](ROADMAP.md) | Entwicklungsplan |

### Paket-BLUEPRINTs

[01 Foundation](mods/01-Rimconemy-Foundation/BLUEPRINT.md) · [02 Survival](mods/02-Rimconemy-Survival-Progression/BLUEPRINT.md) · [03 Scavenger](mods/03-Rimconemy-Scavenger-Infrastructure/BLUEPRINT.md) · [04 Economy](mods/04-Rimconemy-Economy-Territory/BLUEPRINT.md) · [05 Infected](mods/05-Rimconemy-Infected-Automation/BLUEPRINT.md)

---

<p align="center">
  <strong>Rimconemy:</strong> Mehr Dashboards. Weniger Gewissheit. Aber mit Regressionstests.<br/>
  <em>Ein Projekt, das ehrlich darüber ist, was es noch nicht kann.</em>
</p>
