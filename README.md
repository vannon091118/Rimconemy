# Rimconemy

<p align="center"><a href="banner.html"><img src="banner.svg" alt="Rimconemy Banner" width="1200"/></a></p>

> **Du bist der letzte Überlebende. Und je größer du wirst, desto lauter schreit die Welt nach dir.**

**Rimconemy** ist ein modulares RimWorld-Overhaul: Ein einzelner Überlebender, eine zerstörte Welt, ein Plan. Aus Bauschutt werden Wände, aus Farmen Nahrung, aus Wasser und Kohle Strom, aus Strom Verteidigung. Du wirtschaftest mit Credits, investierst in Outposts, expandierst über die Weltkarte — und jeder Erfolg erhöht den Druck. Denn die Infizierten haben einen eigenen Story-Writer, und der hat dich längst auf der Liste.

---

## 🎮 Was ist das? Der Elevator Pitch

Stell dir RimWorld vor, komprimiert auf die Essenz: **Ein Siedler. Kein Colony-Management-Überfluss. Ein langer Überlebenszug nach oben.**

Rimconemy nimmt dir das gemütliche Aufbauen nicht weg — es macht es zur **Entscheidung**. Jede Ressource ist knapp, jede Investition sichtbar, jede Bedrohung erklärbar. Es gibt keine stillen Schwierigkeits-Multiplikatoren. Wenn etwas schiefgeht, weißt du **warum**.

Die zentrale Fantasie: **Wachstum erzeugt Aufmerksamkeit.** Je stärker deine Basis wird, desto eher kommen sie. Du kannst dich klein und unsichtbar halten — oder groß werden und den Preis zahlen.

## 🧭 Wozu? Dein Spielkreislauf

```text
Überleben → Aufbauen → Wirtschaften → Expandieren → (Automatisieren) → Verteidigen → Game Over oder Endgame
```

1. **Überleben** — Ein Überlebender, 18 Jahre, mit deinem Skillbudget (30 Punkte, zwölf Skills). Deine Traits sind das Ergebnis deiner Entscheidungen, nicht des Zufalls.
2. **Aufbauen** — Bauschutt wird zur Wand, Hanf und Farmen ernähren dich, Wasser + Brennstoff betreiben den Generator, Strom den Pfeilturm. Ressourcen werden **echt** aus deinen Lagern gelesen — kein zweites unsichtbares Inventar.
3. **Wirtschaften** — Credits sind eine saubere Wallet, Silber ist physisches Material. Lokale Märkte mit deterministischen Preisen, kein Vanilla-Preismurks.
4. **Expandieren** — Outposts brauchen Investition und Verteidigung. Territorium ist eine **reale Verbindung** über die Weltkarte, kein UI-Symbol.
5. **Druck** — Der Story-Writer bewertet deine Lage (Lager, Bedrohung, Ideologie) und wählt erklärbare Events: Versorgungskrise, ideologischer Konflikt, äußere Bedrohung. Deterministisch, nachvollziehbar, fair.
6. **Das Ende** — Eines Tages ist Schluss. Das Game Over ist Teil des Designs — und kommt genau einmal.

## 🔭 Was soll es werden? Die Vision

Ein **komplettes, modulares Survival-Economy-Overhaul** — in fünf Paketen, die auch einzeln spielbar sind:

| # | Paket | Deine Rolle darin |
|---|---|---|
| **01** | **Foundation** | Die Basis: Dashboard, Diagnose, Mod-/DLC-Erkennung, Save-Status, Eventlog |
| **02** | **Survival & Progression** | Dein Charakter: Bedürfnisse, Arbeit → XP, Spezialisierung, Forschung, Game Over |
| **03** | **Scavenger Infrastructure** | Dein Basislager: Bauschutt, Farmen, Hanf, Wasser, Strom, Pfeilturm |
| **04** | **Economy & Territory** | Dein Imperium: Credits, Märkte, Outposts, Territorium, Weltkarten-Raids |
| **05** | **Infected & Automation** | Dein Feind & dein Werkzeug: Story-Writer, Bedrohungsdruck, Infizierte, Mechadroids |

---

## 🏁 Meilensteine

### ✅ Erreicht

| Meilenstein | Status | Details |
|---|---|---|
| **Phase 0 — Root-Verträge** | ✅ fertig | Kanonische Docs (ROADMAP, DECISIONS, Architektur-Verträge), kein Overclaim |
| **Phase 1 — Story Writer** | ✅ code-fertig | `SettingProfile` (3 Difficulty-Profile: Refuge/Survival/Collapse), `StoryEventSpec`, `StoryState` mit Schema-Version, `StorySelector` (deterministisch), `StoryEventCatalog` (12 hardcoded + Def-Overlay), 12 Unit-Tests, 3 MVP-Events (SupplyShortage, IdeologyConflict, ExternalThreat) |
| **Phase 3 — Storage-only** | ✅ implementiert | `StorageSnapshot` + `StorageQuery.ReadStorage()`, 250-Tick-Cache, ContentHash, Lagerortfilter — **einzige** Ressourcen-Quelle für UI + StoryDirector + Economy |
| **Phase 4 — Character Setup** | ✅ (teilweise) | Alter 18/18, Skillbudget 30, NeutralCenter 25, Neutralzone [-5, +3], Trait-Schwellen, Bio-Remap, Harmony-PreOpen-Patch, `SingleSurvivor`-Scenario — **offen:** Save-State, Generator-API-Gate, Live-Balance-Test |
| **5 Pakete kompilieren** | ✅ | Alle gegen RimWorld 1.6.4566 lokal; GameFont-Medium-Fix nach 1.6-Kompatibilitäts-Spike |
| **Runtime-Boot** | ✅ | Alle 5 Mods laden, FullOverhaul erkannt, 20 Boot-Regression-Summaries mit 0 Failed |
| **CreditsWallet** | ✅ | Persistente Wallet, 256er History, Idempotenz-Index (`Key → TxId`), Overflow/Underflow-Rejection |
| **Deterministischer Markt** | ✅ | Lokale Preisberechnung `base × (1+scarcity) × (1-demandBuffer)`, kein Vanilla-MarketValue |
| **Magic Numbers** | ✅ audit-fertig | F1–F4 gebunden (`TimeConstants.TicksPerDay`, `WealthFullPressureThreshold`, etc.) |
| **DECISIONS-Drift** | ✅ | H1–H6 abgeglichen (Need-System, GameOver, Storyteller, StorageHash, Markt, Wallet) |
| **Falsifizierungs-Härtung** | ✅ | CreditsLedger, StoryState, EventLog — Idempotenz, Escape-Awareness, Scribe-Roundtrip |
| **InfectedRaidWorker** | ✅ | Letter statt Spawn, Vanilla-Incidents nicht deaktiviert |
| **Ideology-Adapter** | ✅ (1/3) | `ThoughtWorker_ResourceFairness` als erste Regel implementiert |
| **UI-Toolkit** | ✅ | `RimconemyUi`, `RimconemyWindow`, `RimconemyMainTabWindow`, `RimconemyInspectTab`, Shared Tokens |
| **Dokumentation** | ✅ | CODE_STATUS.md, INTERFACE_CONTRACT, SAVE_CONTRACT, COMPATIBILITY_MATRIX, H1–H5-Spezis |

### 🔄 In Arbeit

| Meilenstein | Status | Was fehlt |
|---|---|---|
| **Save/Load-Roundtrip** | 🔄 | Story-State, Character-Setup-State, Credits-Ledger über Save/Load überlebt noch nicht verifiziert |
| **Live-Event-Feuerung** | 🔄 | StoryDirector → StorySelector → StoryState → IncidentWorker → Letter — kein echter Ingame-Run belegt |
| **StorageHash-Brücke** | 🔄 | `AnyResourceCritical` auf echten `StorageQuery.ReadStorage()`-Wert umstellen (C1–C5) |
| **Ingame-Verifikation** | 🔄 | Code kompiliert + deployed sauber; vollständiger Ingame-Lauf (Story-Writer + Event-Feuerung) noch nicht beobachtet |

### ⬜ Nächste Ziele (Phase 2–6)

| Meilenstein | Phase | Beschreibung |
|---|---|---|
| **Setting-Ideologie Regel 2–3** | P2 | `CollectiveDefense` (RoleDef + ThoughtDef + RitualDef), `Transparency` (PreceptDef + ThoughtDef) |
| **Setting-/Erfahrungsfenster** | P2 | Ideology-Fenster als Setting-Regel-Anzeige, keine Religionssimulation |
| **Kartenwechsel & Caravan** | P3 | StorageSnapshot muss unloaded Maps, Caravans und Temporary-Maps überleben |
| **Pawn-Generator-API** | P4 | `FixedBiologicalAge`, `GeneratePawn`, `GenerateTraits` — lokaler Spike nötig |
| **Character-Setup-Save-State** | P4 | Persistenter Seed, Skills, Trait-IDs als eigenes Scribe-Schema |
| **Vanilla-/DLC-Incident-Klassifikation** | P5 | Wealth-Raids, Quests, DLC-Incidents separat; genau **ein** Infizierten-Provider |
| **Bauschutt → Wand/Tür** | P6 | Erste sichtbare Gameplay-Mechanik (Patches bereits angelegt) |
| **Nahrung/Hanf getrennt** | P6 | WorkGiver, Ernte, Verderb als eigene Domäne |
| **Wasser-/Brennstoff-Physisch** | P6 | Wasser und Holz/Kohle als physische Pfade zum Generator |
| **Pfeilturm** | P6 | Strom als harte Bedingung, Zustände Active/Blocked/Offline/Damaged |
| **Infizierten-Raids** | P6 | Echter Spawn-Pfad statt Letter-only |
| **Mechadroids** | P6 | Grundsystem, Aufträge, Automation |
| **Outposts & Proxy-Graph** | P6 | Gründung, Produktion, Verteidigung, Drei-Tage-Countdown |
| **Weltkarten-Endgame** | P6 | Territorium, World-Map-Overlay, automatisierte Raids |
| **20 Falsifizierungsberichte** | Alle | Jedes Feature mit `SURVIVED`-Status und A–G-Belegen |

### 📊 Test-Übersicht

| Paket | Tests | Status |
|---|---|---|
| **01 Foundation** | CapabilityGate (19), ColonialReader (7), CrossPackageState (8), EventLog (10), ProfileRefresh (10), BuildingCapability (5), TimeConstants (6) | ✅ alle grün |
| **02 Survival** | BioRemap (31), NeedMapping (48), ScenarioContract (9), BuildingProgression (6+4) | ✅ alle grün |
| **03 Scavenger** | BuildingCore (12) | ✅ alle grün |
| **04 Economy** | CreditsLedger (14), Market (4), BuildingInput (8), PhysicalTransfer (17), OutpostInvestment (7) | ✅ alle grün |
| **05 Infected** | StorySelector (12+), StoryState, BuildingThreat, MechadroidJob | ✅ alle grün |

---

## ⚙️ Voraussetzungen

- RimWorld **1.6**
- **Harmony**
- **Anomaly** + **Odyssey** (Hard-Require — Rimconemy weiß, was es will)
- Ein gesundes Maß an Selbstironie

## 🛠️ Entwicklung

```bash
./scripts/runtime_test.sh                  # Build + Deploy + RimWorld-Start + Log-Gates
./scripts/runtime_test.sh --skip-start --no-deploy   # nur statischer Check
```

Der kanonische Boot-Test verlangt einen *frisch veränderten* `Player.log` — alte Logs werden abgelehnt. So stellen wir sicher, dass das Spiel wirklich gestartet wurde und nicht nur so tut.

## 📌 Roadmap-Kern

- ✅ Story-Writer: Difficulty-Profile (`Refuge` / `Survival` / `Collapse`), deterministische Eventauswahl, Cooldowns, Idempotenz
- ✅ Character Setup: Bio → Skillbudget 30 → Traits (Balance-Regeln dokumentiert)
- ✅ Storage-only-Read-Model: echte Lagerbestände, kein Parallelinventar
- 🔄 Als Nächstes: Save/Load-Roundtrip, Live-Event-Feuerung, Bauschutt → Wand/Tür
- ⬜ Später: Infizierten-Raids, Mechadroids, Outposts & Proxy-Graph, Weltkarten-Endgame

---

*Rimconemy — Mehr Dashboards. Weniger Spaß. Aber mit Regressionstests.*
