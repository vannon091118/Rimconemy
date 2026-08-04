# Rimconemy — Root Roadmap

> **Stand:** 2026-08-04 (Dokument-Konsolidierung: Handoffs/Audits/Tasklisten in §§1–8 integriert, alte MD-Dateien archiviert in `docs/archive-md-2026-08-04.tar.gz`)
> **Zielplattform:** RimWorld 1.6.4566; Royalty, Ideology, Biotech, Anomaly, Odyssey  
> **Status:** Phase 1 Coding-Cut mit Runtime-Boot-Gates belegt. Alle 5 Mods laden, Foundation erkennt FullOverhaul, alle Bootstraps und Regression-Summaries laufen; `scripts/runtime_test.sh` erzwingt frischen Log sowie Need-/Sandbox-/Patch-/Market-Gates. Save/Load, Kartenwechsel und vollständige Event-/Raid-Ausführung bleiben offen. Code-nahe Statusreferenz: `docs/CODE_STATUS.md`.
> **Kanonische Dokumente:** `ROADMAP.md` (dieses Dokument), `docs/DECISIONS.md`, Architektur-Verträge (`docs/INTERFACE_CONTRACT.md`, `docs/SAVE_CONTRACT.md`, `docs/COMPATIBILITY_MATRIX.md`, `docs/H1-…H5-…`, `docs/superpowers/specs/*`, `mods/*/BLUEPRINT.md`). Paket-Roadmaps: `mods/*/ROADMAP.md`.

## 1. Verbindliche Priorität

Die neue Produktpriorität überschreibt die bisherige Paket-Reihenfolge für die nächste Entwicklungsphase:

```text
Story Writer + Difficulty + Eventkatalog
        ↓
Setting-Ideologie als Verhaltensadapter
        ↓
Storage-only-Ressourcen-Snapshot
        ↓
Character Setup: Bio → Skillbudget → Traits
        ↓
Vanilla-/DLC-Adapter und echte Gameplay-Events
        ↓
Wirtschaft, Outposts, Infizierte, Mechadroids und Endgame
```

Die Paketnummern bleiben Eigentumsgrenzen. Sie sind aber nicht automatisch die Implementierungsreihenfolge. Story- und Ideology-Verträge müssen zuerst geklärt werden, weil sie spätere Charakter-, Ressourcen- und Evententscheidungen bestimmen.

## 2. Produktentscheidung für Phase 1

### 2.1 Story-Modell

Der Story Writer bewertet eine dynamische Lage und wählt daraus ein passendes Ereignis. Er ist zunächst ein **Setting-Director mit Vanilla-Adaptern**, nicht automatisch ein direkt implementierter Vanilla-`StorytellerDef`.

Ein direkter Storyteller wird erst nach einem lokalen API-/Def-/Runtime-Spike freigegeben. Assembly-String-Treffer gelten nicht als Signatur- oder Runtime-Beweis.

### 2.2 Difficulty-Profile

Schwierigkeit wird als benanntes Setting-Profil statt als unverständliche globale Zahl geführt. Die folgenden Namen und Regeln sind der Phase-1-Startpunkt; Zahlen werden erst nach dem Balance-Gate verhärtet:

| Profil-ID | Spielerlesbare Bedeutung | Eventdruck | Ressourcenregel | Ideologie-Regel | Ruhefenster |
|---|---|---|---|---|---|
| `Rimconemy_Refuge` | Aufbau einer kleinen Zuflucht | niedrige Eskalation; frühe Krisen bleiben begrenzt | Grundversorgung bleibt verfügbar, Engpässe werden angekündigt | Konflikte überwiegend als Dialog-/Thought-Folgen | lang |
| `Rimconemy_Survival` | hartes tägliches Überleben | mittlere Eskalation; Versorgung und Bedrohung konkurrieren | Lagerbestand ist entscheidend; Krisen können Folgeevents erzeugen | Konformität und Ressourcenentscheidungen erzeugen sichtbare soziale Folgen | mittel |
| `Rimconemy_Collapse` | Zusammenbruch unter permanentem Druck | hohe Eskalation; Wendepunkte früher möglich | keine kostenlose Erholung; knappe Lager verschärfen Eventgewicht | Konflikte können Rollen, Mood und Folgeevents stärker verändern | kurz |

Jedes Profil definiert zusätzlich:

- Eventfamilien und erlaubte Eskalationsstufen,
- Ruhe- und Schutzfenster,
- Bedrohungsdruck und Intensitätsgrenzen,
- Ressourcenknappheit als erklärbare Regel,
- Ideologie-Spannung und Konfliktfolgen,
- zulässige Wendepunkte,
- deterministische Version und Seed-Regeln.

Jede sichtbare Änderung muss im UI als Regel erklärt werden. Es gibt keine stillen globalen Multiplikatoren. Die konkrete Gewichtungs-/Cooldown-Tabelle wird im Phase-1-Story-Gate festgeschrieben.

### 2.3 Eventkatalog

Die Phase-1-MVP-Liste ist verbindlich als Content-Startpunkt:

| Event-ID | Familie | Mindestlage | Ausschluss/Cooldown | Primäre Folge |
|---|---|---|---|---|
| `Rimconemy_SupplyShortage` | Versorgungskrise | ein Storage-Snapshot unterschreitet das Profilminimum | nicht während aktiver Versorgungserholung; einmal pro Krisenfenster | Lagerentscheidung, Kosten oder Hilfsangebot |
| `Rimconemy_IdeologyConflict` | ideologischer Konflikt | Setting-Regel verletzt oder Ideologie-Spannung über Profilgrenze | kein zweites Konfliktereignis vor Auflösung des ersten | Thought-/Rollen-/Gruppenentscheidung |
| `Rimconemy_ExternalThreat` | äußere Bedrohung | Threat-Snapshot überschreitet die Profilschwelle | keine Doppelung mit aktivem Raid-/Threat-Event | Warnung, Vorbereitung oder Incident-Adapter |

Jedes Event erhält vor Implementierung konkrete Gewichtungs-, Cooldown- und Folgewerte pro Profil. Ein Event-Spec besitzt mindestens:

```text
EventId
EventVersion
EventFamily
Prerequisites
Exclusions
Weight
Cooldown
EscalationBand
TextKey
Choice/FollowUpIds
Effects
DeterminismKey
```

Eventauswahl folgt dieser Reihenfolge:

1. harte Profil-/DLC-/Lage-Ausschlüsse,
2. Save-/Cooldown-/Idempotency-Prüfung,
3. gültige Eventkandidaten,
4. deterministische Gewichtung,
5. Auswahlgrund und erwartete Konsequenz speichern,
6. genau einmal ausführen,
7. Folgeevents nur über gespeicherte IDs planen.

Erste Eventfamilien:

- Versorgungskrise,
- ideologischer Konflikt,
- äußere Bedrohung,
- Entdeckung,
- technische Chance,
- moralische Entscheidung,
- Ruhe-/Erholungsereignis,
- Wendepunkt.

### 2.4 Setting-Ideologie

Das Setting besitzt die Regeln. RimWorld Ideology dient als technischer Träger über einen Adapter:

| Setting-Konzept | Technischer Träger | Belegpflicht |
|---|---|---|
| gemeinsame Regel | `PreceptDef`/Precept | lokale Def-/API-Prüfung plus Pawn-/UI-Fall |
| soziale Verantwortung | `RoleDef`/Role | Rollenvergabe und Verhalten testen |
| Gruppenerfahrung | `RitualDef`/Ritual | Ritual lädt, wirkt und speichert |
| unmittelbare Reaktion | `ThoughtDef`/ThoughtWorker | Mood-/Social-Effekt reproduzieren |
| Verhaltenspriorität | native Ideology-/AI-Adapter oder expliziter Director-Command | keine Behauptung aus Precept allein |

„Ideologie“ bedeutet in diesem Projekt kein zusätzliches Religionssystem. Das vorhandene Ideology-Fenster wird als **Setting-/Erfahrungsfenster** genutzt: Regeln, Rollen, Gemeinschaftsreaktionen und Konsequenzen werden dort verständlich angezeigt.

Vanilla-Ideologie wird im Zielprofil nicht als primäre Spielerlogik vorausgesetzt. Ob technische Vanilla-Elemente geerbt, ersetzt oder nur adaptiert werden, wird pro Precept-/Role-/Ritual-Familie dokumentiert. Keine globale, stille Löschung fremder Ideology-Inhalte.

## 3. Phasen

### Phase 0 — Root-Verträge und Beweisgrenze

**Ziel:** Die Dokumentation wird wieder auffindbar und widerspruchsfrei.

**Aufgaben:**

- ✅ Kanonische Übergabe: `ROADMAP.md` (dieses Dokument), `docs/DECISIONS.md`, Architektur-Verträge und Paket-Roadmaps/Blueprints. Handoff-/Audit-/Tasklisten-Dokumente sind in §8 (Arbeits-Backlog) und §5 (DoD) konsolidiert und im Archiv `docs/archive-md-2026-08-04.tar.gz` gesichert.
- ✅ fehlende Referenzdokumente entweder anlegen oder alle Links ausdrücklich als offen markieren.
- ✅ Paket-README/ROADMAP-Overclaims als `planned`/`scaffold` kennzeichnen (AUDIT §E2/E3).
- ✅ Definition von `SettingProfile`, `SituationSnapshot`, `StoryEventSpec`, `IdeologySnapshot` und `StorageSnapshot` festschreiben (H1–H5 Docs).

**Gate:** ✅ Erfüllt. Kein Root-Dokument stellt nicht Implementiertes als geliefert dar.

### Phase 1 — Story Writer, Difficulty und Eventkatalog

**Besitzer:** zunächst Paket 05 als Bedrohungs-/Eventdomäne; gemeinsame Read-Models über Foundation-Verträge, ohne direkte Pflicht-Compile-Abhängigkeit.

**Artefakte (Stand 2026-08-04):**

- ✅ `SettingProfile` mit Difficulty-Regeln (`mods/05/.../Source/Story/SettingProfile.cs`)
- ✅ `SituationSnapshot` mit Storage-/Survivor-/Threat-/Ideology-Aggregaten (`SituationSnapshot.cs`)
- ✅ `StoryEventSpec` und Eventfamilien (`StoryEventSpec.cs`, `StoryEventCatalog.cs`)
- ✅ pure deterministische Auswahlfunktion (`StorySelector.cs`)
- ✅ Cooldown-/Idempotency-State (`StoryState.cs` mit `IExposable` + `ExposeData()`)
- ✅ Storage-Adapter (`mods/03/.../Source/Storage/StorageQuery.cs`)
- ✅ Ideology-Adapter, 1 Regel (`mods/02/.../Source/Ideology/ThoughtWorker_ResourceFairness.cs`)
- ✅ Unit-Tests, 12 Tests (`mods/05/.../Tests/StorySelectorTests.cs`)
- ✅ Incident-Ausführung (`StoryDirector.cs` + `InfectedRaidWorker.cs` Rewrite)
- ✅ UI-Read-Model mit Auswahlgrund (`StoryDirector.LastSelectionReason`, `ThreatHistory`, ThreatDashboard); die Live-Darstellung ist noch nicht durch einen vollständigen UI-Lauf verifiziert
- ✅ drei testbare Event-Specs (`Defs/StoryEvents/` XMLs + `StoryEventCatalog.cs`)

**MVP-Events:**

1. Versorgungskrise,
2. ideologischer Konflikt,
3. äußere Bedrohung.

**Nicht in Phase 1:** vollständiger Raid-Spawn, Mechadroids, Hauptstädte, Endgame, automatische globale Storyteller-Übernahme.

**Gate:** ✅ Code-seitig erfüllt. Gleicher Snapshot + Profil + Seed → gleicher `DeterminismKey`. `StorySelectorTests` verifiziert Determinismus (×3 Profile) und Idempotenz (×2). 

**Runtime-Beleg (2026-08-04):** ⬜ Spielstart ✅ — alle 5 Mods laden, FullOverhaul erkannt, Bootstrap-Kette komplett. ⬜ Save/Load + Event-Feuerung noch ausstehend.

**Bugfixes nach Live-Test (2026-08-04):**
- XML-Format-Fehler (18×): `StoryEventDef.weights`/`cooldownDays` von `Dictionary` auf `List<string>` (`Key=Value`) umgestellt
- Version-Mismatch: `PackageRegistry.cs` hartcodierte Versionen auf `VERSION`-Stand gebracht
- Idempotenz-Test (2 fails): Cooldown-Filter aus Kandidatenauswahl entfernt, stattdessen nach Idempotenz-Prüfung angewendet
- `StoryState.GetCooldownUntil()` ergänzt

### Phase 2 — Setting-Ideologie

**Besitzer:** Survival & Progression für Pawn-/Mood-/Setting-Reaktionen; Ideology-Adapter als klar getrennte Schnittstelle.

**Aufgaben:**

- Setting-Regeln als versionierte IDs definieren.
- Precepts, Roles, Rituals und Thoughts nur über belegte native Träger anbinden.
- Einflussmatrix anlegen: Regel → Träger → Pawn-Zielgruppe → Thought/Mood-/Social-/AI-Wirkung → UI-Erklärung.
- festlegen, welche Vanilla-Precepts geerbt, ersetzt, neutralisiert oder nur beobachtet werden.
- Ideologie-/Erfahrungsfenster anzeigen.
- Konflikt-, Konformitäts- und Rollenreaktionen speichern.

**Gate:** Mindestens drei Setting-Regeln erzeugen reproduzierbar dokumentierte Charakterreaktionen; kein globales Verhalten wird nur aus einem Namen oder Assembly-String abgeleitet.

**Status 2026-08-04 (P2-Fortschritt):**

- ✅ **Regel 1 — `ResourceFairness`** (code-fertig; `ThoughtWorker_ResourceFairness`, `ThoughtDefs_ResourceFairness`).
- ✅ **Regel 2 — `CollectiveDefense`** (neu in 0.0.36): `Rimconemy_Thought_ValiantDefense` (+5/2Tage), `Rimconemy_Thought_DefenseShirking` (-8/3Tage), `Rimconemy_Thought_UnitedAfterDefense` (+3/2Tage), `CollectiveDefenseTracker` (`GameComponent`, Scribe-fähig), `CollectiveDefensePostCombatPatch` (Harmony auf `Pawn.PostApplyDamage`), `Rimconemy_Role_Defender` (`Precept_RoleMulti`-basiert, Issue `Rimconemy_CollectiveDefenseIssue`), `CollectiveDefenseRegressionTests` (4 Tests).
- ⬜ **Regel 3 — `Transparency`** (`PreceptDef` + `ThoughtDef` + `ThoughtWorker`) offen.
- ⬜ **RitualDef-Realisierung** für `Ritual_PostDefense` offen — RimWorld 1.6 zerlegt Rituale in `RitualBehaviorDef` + Outcome/Visual; weitere Iteration nach Ideology-Spike.
- ⬜ **`Setting-/Erfahrungsfenster`**-Anzeige im Ideology-Tab offen.
- ⬜ **`Vanilla-Precept-Policy`**-Dokumentation pro Familie offen (Beobachten/Neutralisieren).

### Phase 3 — Storage-only-Ressourcenmodell

**Besitzer:** Scavenger Infrastructure für physische Bestände; Foundation für Read-only-Snapshots.

**Regel:** Survivor-Ressourcen werden ausschließlich aus tatsächlichen RimWorld-Storage-/Map-Beständen gelesen. Kein zweites abstraktes Survivor-Inventar und kein paralleles Ledger für dieselben physischen Items.

**Snapshot enthält:**

- stabile Ressource-/ThingDef-ID,
- aggregierte Menge,
- Map-/Storage-Ort,
- Qualitäts-/Verderbsklasse, soweit relevant,
- Timestamp,
- Availability (`Available`, `Blocked`, `Unavailable`, `Frozen`).

Credits bleiben davon getrennt und dürfen nicht als physische Lagerware ausgegeben werden.

**Gate:** UI, Story Writer und spätere Economy lesen dieselbe Storage-Snapshotquelle; Kartenwechsel und Save/Load erzeugen keine abweichenden Bestände.

### Phase 4 — Character Setup und Bio-Remap

**Besitzer:** Survival & Progression.

**Startregeln:**

- biologisches und chronologisches Startalter immer **18**, sofern der lokale Generatorvertrag dies nach Spike bestätigt;
- Bio-Generator liefert Herkunft, Ton, Backstory und Auswahlkontext;
- Bio-Text darf nicht unkontrolliert zusätzliche Werte erzeugen;
- feste Gesamtzahl an Skillpunkten, die der Spieler vor Spielbeginn verteilt;
- neutrale Pufferzone ohne Trait-Strafe und ohne Trait-Bonus;
- Unterdeckung erzeugt definierte negative Traits;
- Überdeckung erzeugt definierte positive Traits;
- Trait-Grenzen, Ausschlüsse und DLC-Kompatibilität sind explizit.

**Empfohlenes Berechnungsmodell:**

```text
skillBudget = feste Startsumme
spentPoints = Summe der verteilten Punkte
balance = spentPoints - neutralCenter

balance < negativeThreshold → negative Trait-Kandidaten
negativeThreshold <= balance <= positiveThreshold → neutral
balance > positiveThreshold → positive Trait-Kandidaten
```

Der Neutralbereich muss breit genug sein, dass normale Spielerentscheidungen nicht sofort bestraft oder belohnt werden. Die konkrete Punktzahl und Schwellen werden erst nach einer Balance-Entscheidung festgeschrieben.

**Gate:** Drei identische Bio-/Skill-Eingaben erzeugen dieselben Skills, Trait-Kandidaten und Startalterwerte; keine zufällige nachträgliche Trait-Änderung außerhalb des dokumentierten Seeds.

### Phase 5 — Story-Ausführung und Vanilla-/DLC-Adapter

**Aufgaben:**

- entscheiden, ob der Setting-Director genügt oder ein direkter Vanilla-Storyteller benötigt wird;
- `StorytellerDef`, `StorytellerComp`, `IncidentDef`, `IncidentWorker` und Difficulty-Anker lokal per Reflection/Decompilation/Def-Load prüfen;
- Vanilla-Wealth-Raids, Quest- und DLC-Incidents separat klassifizieren;
- genau einen Infizierten-Provider im Full Profile aktivieren;
- keine globale Raiddeaktivierung ohne Kompatibilitätsmatrix;
- Auswahl, Ausführung, Letter, Spawn und Auflösung idempotent speichern.

**Gate:** Ein Druckanstieg erzeugt höchstens ein vorgesehenes Event, Vanilla-/Quest-/DLC-Ereignisse bleiben nach Policy korrekt, und der Ausführungsfall überlebt Save/Load ohne Doppelauflösung.

### Phase 6 — echte Gameplay-Schichten

Erst danach:

- Infizierten-Raids,
- Mechadroid-Aufträge,
- Outposts und Proxy-Graph,
- Wirtschaft und Wallet-Transaktionen,
- Bauschutt-Baukosten,
- Produktionsketten,
- World-Map- und Endgame-Systeme.

Jede Domäne bleibt standalone-fähig und erhält eigene Save-, Performance- und Falsifizierungs-Gates.

## 4. Globale Verträge

### Ownership

- Foundation: Registry, Diagnose, gemeinsame Read-Models, Capability-/Save-Metadaten.
- Story Writer/Threat: SettingProfile, StoryEventSpec, StoryState und Eventauswahl.
- Survival & Progression: Character Setup, Skill-/Trait-Regeln, Setting-Reaktionen für Pawns.
- Scavenger Infrastructure: physische ThingDefs, Lagerbestände, StorageSnapshot.
- Ideology-Adapter: technische Bindung der Setting-Regeln an native Ideology-Träger; keine eigene parallele Religionseinheit.
- Economy & Territory: Credits, Märkte, Outposts und Transport; liest physische Ressourcen, besitzt sie nicht doppelt.

### Determinismus

- expliziter Seed oder deterministische Auswahl-ID,
- stabile Sortierung der Kandidaten,
- keine Systemzeit als Spielinput,
- keine Hintergrundthreads für Spielzustand,
- Auswahlgrund und Eingangs-Snapshot speichern,
- Idempotency-Key pro Eventausführung.

### Save

Mindestens versionieren:

- `SettingProfileId` und Version,
- `StoryStateSchemaVersion`,
- letzte Auswahl-/Event-ID,
- Cooldowns,
- Auswahlseed,
- Ideology-Rule-Version,
- Character-Setup-Version,
- Storage-Snapshot-Timestamp beziehungsweise Rebuild-Markierung.

Fehlende oder inkompatible Zustände werden migriert, eingefroren mit Warnung oder kontrolliert abgelehnt. Niemals still löschen.

### Performance

- Story Writer arbeitet auf aggregierten Snapshots und definierten Intervallen.
- Storage wird nicht bei jedem UI-Frame und nicht mehrfach von jedem Paket gescannt.
- UI liest Snapshots und mutiert keine Simulation.
- Runtime-Gates müssen echte Ingame-/Save-/Load-Belege besitzen; Kompilierung allein reicht nicht.

## 5. Definition of Done für Full-Profile-Übergabe

- [x] Story Writer besitzt ein versioniertes Difficulty-/Event-/State-Modell (`SettingProfile`, `StoryEventSpec`, `StoryState` mit Schema-Version).
- [x] mindestens drei Eventfamilien mit Voraussetzungen, Auswahlgrund, Cooldown und Folgepfad beschrieben (`StoryEventCatalog` + `StoryEventSpec`).
- [x] deterministische Auswahl und genau-einmalige Ausführung durch 12 Unit-Tests nachgewiesen (`StorySelectorTests`).
- [x] Ideology-Regeln: 1 von 3 MVP-Regeln implementiert (ResourceFairness). Vollständige Matrix in H3 spezifiziert, restliche 2 Regeln (RoleHierarchy, RitualTrigger) in Phase 2.
- [ ] das Setting-Ideologie-Fenster erklärt Regeln und Charakterreaktionen ohne zusätzliche Religionssimulation.
- [x] Storage-only-Read-Model implementiert (`StorageSnapshot` + `StorageQuery` + `ReadStorage()`), inklusive 250-Tick-Cache und Lagerortfilter. StoryDirector liest den Snapshot über Capability-Bridge und setzt `AnyResourceCritical`; vollständige UI-/Economy-Verwendung und Caravan-/Temporary-Map-Abdeckung bleiben offen.
- [~] Character Setup: kostenbewusste Skill-/Trait-Logik, Alter-18/18-Fix, Harmony-PreOpen-Patch und Regressionstests sind vorhanden; eigener Character-Setup-Save-State, Generator-API-Gate und Live-Balance-Test bleiben offen.
- [x] Skillbudget 30, NeutralCenter 25, Neutralzone [-5, +3] und Trait-Schwellen sind im H5-Vertrag und Code dokumentiert; Balance-Gate bleibt offen.
- [x] Vanilla-/Quest-/DLC-Incidents: `InfectedRaidWorker` rewritten — sendet Letter statt Spawn, deaktiviert keine Vanilla-Incidents. Vollständige Klassifikation in Phase 5.
- [ ] Save-Migration, Kartenwechsel, unloaded Maps und Game-Over-Fälle sind geprüft.
- [ ] alle relevanten Falsifizierungsberichte besitzen echte Belege und `SURVIVED`; fehlende Berichte gelten als Blocker.
- [x] alle fünf Pakete kompilieren; die betroffenen Pakete 01/04/05 kompilieren nach dem Falsifizierungsfix 0W/0E gegen lokale RimWorld 1.6.4566.
- [x] Runtime-Boot-Test: alle Mods laden, Foundation erkennt FullOverhaul, Bootstraps und Boot-Regression-Summaries erfolgreich; wiederholbarer Prüfer ist `scripts/runtime_test.sh`.
- [ ] Runtime-Standalone-Fähigkeit: echter Save/Load-Roundtrip, Kartenwechsel und vollständige Event-/Raid-Auflösung noch nicht verifiziert.

## 6. Harte Stop-Gates

Arbeit stoppt vor der nächsten Phase, wenn:

- ein Dokument ein Scaffold als geliefert bezeichnet,
- eine API-Annahme nur durch `strings` begründet wird,
- der Story Writer mehr als eine Eventausführung für denselben Idempotency-Key erzeugt,
- Story-/Ideology-State nach Save/Load driftet,
- Lagerbestand und UI-/Economy-Snapshot voneinander abweichen,
- Charaktere ohne explizite Regel außerhalb der Trait-Pufferzone landen,
- ein Mechadroid oder abstrakter Outpost ein Game Over verhindert,
- ein fehlendes DLC/Paket Phantomdaten erzeugt.

## 7. Infrastruktur & Werkzeuge

| Werkzeug | Pfad | Beschreibung |
|---|---|---|
| Version-Bump | `scripts/bump_version.sh` | Bumpt `VERSION`-Datei eines Pakets um +0.0.1 (Regel: nach jeder Code-/Def-/XML-Änderung) |
| Deploy | `scripts/deploy.sh` | Baut + deployt Pakete per `rsync --delete` in RimWorld Mods (`/home/vannon/GOG Games/RimWorld/game/Mods/`); Alternativen `./scripts/deploy.sh <nr>` und `--no-build` |
| Runtime-Boot-Test | `scripts/runtime_test.sh` | Frischer Player.log, installierte Artefakte, Profile-/Registry-/Regression-Gates; `--skip-start` prüft ohne Spielstart |
| Code-Status | `docs/CODE_STATUS.md` | Code-/Def-/Compile-/Boot-/Live-Beleggrenze und offene Runtime-Gates |
| Arbeits-Backlog | `ROADMAP.md` §8 | Kanonische Aufgabenliste für alle Phasen (konsolidiert aus ehem. `PLAN_GESAMTAUFGABEN.md`) |

**Aktuelle Paket-Versionen (Stand 2026-08-04):**

| Paket | Version |
|---|---|
| 01-Rimconemy-Foundation | 0.1.36 |
| 02-Rimconemy-Survival-Progression | 0.1.30 |
| 03-Rimconemy-Scavenger-Infrastructure | 0.0.23 |
| 04-Rimconemy-Economy-Territory | 0.0.26 |
| 05-Rimconemy-Infected-Automation | 0.0.35 |

## 8. Offener Arbeits-Backlog (Stand 2026-08-04)

> Konsolidiert aus ehem. `docs/PLAN_GESAMTAUFGABEN.md` (Blöcke A–F), `HANDOFF.md`, `docs/LLM-SLOP-AUDIT-CHECKLIST.md` und `findings.md`. Vollständiger Archivstand aller konsolidierten Dokumente: `docs/archive-md-2026-08-04.tar.gz`.

### 8.1 Offene API-Spikes (Gates)

| Spike | Gegenstand | Blockiert |
|---|---|---|
| `API-IDEOLOGY-01` | `IdeoDef`/`PreceptDef`/`RoleDef`/`RitualDef` (IdeoDef liegt in DLC-Assembly, lokal nicht prüfbar) | Phase 2 |
| `API-START-01` | `PawnGenerationRequest.FixedBiologicalAge`/`GeneratePawn`/`GenerateTraits` | Phase 4 (Character Setup) |
| `API-NEED-01` … `API-MECH-01` | `COMPILES` vs. `STRING`-Belege auflösen (Need, Job, Resource, Trade, World, Incident, Mech) | je Paket |
| netstandard2.1 | Laufzeitverhalten (kein Spielstart-Beleg) | Runtime-Gates |

### 8.2 Falsifizierungsberichte (20 Stück, Gate vor Übergabe)

Ohne `SURVIVED`-Berichte mit A–G-Belegen gilt keine Übergabe:

- Foundation (1): `Servicebus`
- Survival (4): `Needs`, `WorkXp`, `Research`, `GameOver`
- Scavenger (5): `ConstructionDebris`, `FoodAndHemp`, `WaterPowerArrowTurret`, `ExecutePhysicalTransfer`, `ReservePhysicalTransfer`
- Economy (5): `WalletCredits`, `Market`, `ReservePhysicalTransfer`, `OutpostProduction`, `TerritoryCountdown`
- Infected (5): `ThreatPressure`, `InfectedRaid`, `MechadroidJob`, `ManualRaid`, `AutoResolve`

### 8.3 Phase 1/2 — Story Writer + Setting-Ideologie (offen)

- Runtime-Beleg: Spielstart-/Def-Load-/Save-Load-/Letter-Beleg für StoryDirector → StorySelector → StoryState.
- UI-Read-Model mit Auswahlgrund (war Phase-1-Ausnahme).
- Cooldown-/Idempotenz-Verhalten nach Save/Load gegen Doppelausführung testen.
- Setting-Ideologie Regel 2 `CollectiveDefense` (RoleDef + ThoughtDef + RitualDef) und Regel 3 `Transparency` (PreceptDef + ThoughtDef).
- Vanilla-Precept-Policy je Familie dokumentieren (erben/ersetzen/neutralisieren/beobachten).
- Setting-/Erfahrungsfenster (Ideology-Fenster als Setting-Fenster, keine Religionssimulation).
- H3-Einflussmatrix → Code (Träger → Pawn-Zielgruppe → Wirkung → UI-Erklärung).

### 8.4 Phase 3/4 — Storage-only + Character Setup (offen)

- StorageSnapshot als **einzige** Quelle für UI + StoryDirector + Economy nachweisen (G4-Gate); Kartenwechsel-/Save-Load-Konsistenz und 11 H4-Randfälle (unloaded Map, Caravan, Cache, Credits …).
- Startalter 18/18 beim Pawn-Generator erzwingen (`FixedBiologicalAge`); `FixAge`-Fallback ist dokumentierter Workaround, kein API-Gate.
- `SingleSurvivor.xml` erweitern (aktuell 1 Pawn, 8 Kandidaten, kein Generator-Zwang).
- `CharacterSetupState`/Scribe-Schema mit Schema-Version, Seed, Skills, Trait-IDs; persistenter Runtime-Seed.
- Bio-Remap- und Save/Load-Live-Test; H5-Balance-Gate für Budget 30 und Neutralzone `[-5,+3]`.

### 8.5 Phase 5 — Vanilla-/DLC-Adapter (offen)

- Entscheiden: Setting-Director genügt oder direkter `StorytellerDef` (erst nach Storyteller-Spike).
- `StorytellerDef`/`StorytellerComp`/`IncidentDef`/`IncidentWorker`/Difficulty-Anker lokal (Reflection/Decompilation) prüfen.
- Vanilla-Wealth-Raids/Quest-/DLC-Incidents separat klassifizieren; genau **ein** Infizierten-Provider im Full Profile.
- Auswahl → Ausführung → Letter → Spawn → Auflösung idempotent speichern.

### 8.6 Phase 6 — Paket-Gameplay (offen, Stubs → echte Mechaniken)

- **02 Survival:** 2.1 Pawn-/Start-/Szenariovertrag; 2.2 Kernbedürfnisse (Nahrung/Sicherheit/Soziales); 2.3 Arbeit→XP ohne Tick-Sampling (GESAMTREPORT F2: XP nur bei validiertem Job-Output + Diminishing + Idempotency-ID); 2.4 Forschungsbaum Tier 0–3 als Capability-Graph; 2.5 Game Over exakt einmal; 2.7/2.8 DLC- und Save-Migration.
- **03 Scavenger:** 3.3 Nahrung/Hanf getrennt (WorkGiver/Ernte/Verderb); 3.4 Wasser-/Brennstoffmodell als physischer Pfad; 3.5 Stromnetz mit harten Input-Regeln; 3.6 Pfeilturm (Strom als harte Bedingung, Zustände Active/Blocked/Offline/Damaged); 3.7–3.10 XP-/Research-/Economy-/Threat-Adapter, DLC-/Save-Kompatibilität.
- **04 Economy:** 4.1 Wallet atomar/rollbackfähig; 4.2 Markt deterministisch; 4.4 Outpost-Gründung; 4.5 Produktion/Verteidigung (Brutto/Bindung/Wartung/Netto); 4.6 Proxy-Graph + Drei-Tage-Countdown (180.000 Ticks, F1-Fix); 4.7 World-Map-Overlay; 4.8 automatisierte Raids; 4.9/4.10 Integrationen + DLC-/Save-Kompatibilität.
- **05 Infected:** 5.1 Gegner-/Infektionsdomäne (IncidentStub → echter Spawn-Pfad); 5.2 Bedrohungsaggregator deterministisch; 5.4/5.5 lokale + World-Map-Raids idempotent; 5.6 Mechadroid-Grundsystem; 5.7 Automation-Aufträge; 5.9 Hauptstädte/Endgame; 5.10–5.12 Integrationen, DLC-/Vanilla-Kompatibilität, Save/Performance/Determinismus.

### 8.7 Qualitäts-Backlog (aus LLM-SLOP-Checkliste)

- **A:** Mutation-Testing-Setup (mutmut/Stryker); tautologische Tests A1–A5 erneuern (Rot-Test-Pflicht: Implementierung ändern → Test muss rot werden).
- **C:** StorageHash-Bridge C1–C5 — `snapshot.StorageHash` und `AnyResourceCritical` auf echten `StorageQuery.ReadStorage()`-Wert umstellen; Schwellenwerte (CriticalFood/Medicine/Material) aus DECISIONS §14 einbinden.
- **D:** Pawn-Filterung konsolidieren D1–D5 — alle Enumerationen auf `ColonialReader` (inkl. `ThoughtWorker_ResourceFairness` invertierte Logik, `StoryDirector` 2 Stellen).
- **E:** Dead Code / irreführende Namen E1–E3 (`TryApplyThreatDrivenXpBoost`, `ClassifyJob` Substring, toter null-Check in `QueueSelectedIncident`).
- **F:** ✅ Magic Numbers F1–F4 gebunden (Audit 2026-08-04, Stand 09:27):
  - **F1** `wealthFactor / 700000f` → `StoryDirector.WealthFullPressureThreshold = 700000f` (Mod 05, `slop-audit-fix F1`).
  - **F2** `ExperiencePerWorkSample = 0.25f` → `ProgressionGameComponent.ExperiencePerWorkSample = 0.25f` (Mod 02, `slop-audit-fix F2`).
  - **F3** `60.000 Ticks/Tag` → `Rimconemy.Foundation.TimeConstants.TicksPerDay = 60000f`; 7 Inline-Sites in `RimconemyUi`, `SettingProfile`, `SituationSnapshot` (×2), `StoryEventSpec`, `StoryDirector`, `ThreatDashboard` aktualisiert. Die „vs 30.000 Ticks"-Hälfte liefert 0 Treffer im aktuellen Code; nur historische Backlog-Phrasierung.
  - **F4** Grace-Intervalle → `EmptyColonistGraceIntervals = 12`, `UpdateIntervalTicks = 250`, `CacheMaxAgeTicks = 250`, `GameOverWipeCheckInterval = 250L`.
- **G:** Kommentar-Logik-Lücken G1–G3 prüfen (● offene Aufgabe).
- **H:** ✅ DECISIONS-Drift H1–H6 abgeglichen (Audit 2026-08-04, Stand 09:27):

  | § | Decision-These | Code-Realität | Status |
  |---|---|---|---|
  | §1 Need-System | `Rimconemy_Need_Food` liest Vanilla `Food`; `Safety` liest `Rest` + `Health`; `Social` liest `Recreation` (Fallback `Joy`) | `mods/02/Source/Needs/NeedMapping.cs`: `Food→NeedDefOf.Food`; `Safety→Rest` mit `isCompositeSafety` (0.65 Health / 0.35 Rest); `Social→Recreation ∥ Joy`-Fallback; `Aggregator.{Average, Minimum, Maximum}` | ✅ |
  | §2 GameOver-Logic | Mod 02 einziger Caller von `Find.GameEnder.CheckOrUpdateGameOver()` | `ProgressionGameComponent.UpdateRuntimeState`: einziger Aufruf; Mod 05 setzt nur `StoryState.MarkGameOverPending`; Mod 02 liest via `CrossPackageState.TryReadStoryGameOverPending` (INTERFACE_CONTRACT §9.5) | ✅ |
  | §3 Storyteller | Dual-Source: 12 hardcoded + `DefDatabase`-Overlay (Override OR Add); XML-Errors silent | `StoryEventCatalog.SeedHardcodedCatalog` (12 Events) + `MergeFromDefDatabase` (Override/Add); `try/catch` schluckt Parse-Fehler silent | ✅ |
  | §4 StorageHash | `StorageHash = snapshot.ContentHash` statt `"live-" + tick` | `StoryDirector.AssignStorageHashFromCapability` ruft `StorageQuery.ReadStorage(...).ContentHash` auf; Standalone-Fallback `"live-<tick>"` bleibt erhalten (dokumentiert in INTERFACE_CONTRACT §3) — DECISIONS §4 ist hier unterspezifiziert, aber nicht falsch | ✅ mit dokumentiertem Übergangs-Fallback |
  | §5 Markt/Preise | Deterministische Preisformel, kein Vanilla-`MarketValue` | `Market.cs` (Mod 04): `price = base * (1 + scarcity) * (1 - demandBuffer)`, gedeckelt auf `[MinPriceFactor, MaxPriceFactor] * base`; dokumentiert „publish a local snapshot instead" — keine Vanilla-Aliasierung | ✅ |
  | §6 Wallet/Trade | Credits = Wallet, Silber = physisches Upgrade-Material, separates Trade-Panel | `CreditsLedger`: pure Wallet (`CurrencyId="credits"`, `MaxBalance=1e9`, Transactions, Idempotenz-Map, Scribe) ✅; `TradePanel.cs` zeigt Credits-Balance/Einnahmen/Ausgaben/Historie ✅; `Source/Upgrades/SilverMaterial.cs` trennt Silber als eigenes physisches Material ✅; **Upgrade-Panel mit Silber-Kosten ⬜** (Phase-6, getrackt in §8.6) | ⚠ Credits+Silber ✅, Upgrade-Panel ⬜ |
- **I:** Architektur-Risiken I1–I5 beobachten (StorageHash-Platzhalter, Raid-Stub, XP-Boost No-Op, Reflection-Bridge, Vanilla-Raid-Interaktion).

### 8.8 Erledigte Härtungs-Fixes (Referenz, 2026-08-04)

- **CreditsLedger:** Idempotenz-Separation (persistierbare `Key → TxId`-Map, Replay unabhängig vom 256er-History-Cap), Rejection-Balance inkl. `long.MinValue`-Schutz.
- **StoryState:** Insertions-Ticks über parallele Scribe-Listen; `-1`-Sentinel für Legacy-Keys (kein falsches Tick 0); deterministisches Alters-Pruning.
- **FoundationSaveData/EventLog:** Pipe-Escape/Unescape escape-aware in Einzelpass-Scannern; Roundtrip-sicher für Pipes, Backslashes, trailing Backslashes.
- **Build-Fix:** CS0246 in `StoryStateRegressionTests.cs` — fehlendes `using System;` ergänzt.
- Regressionstests: `CreditsLedgerRegressionTests.cs`, `StoryStateRegressionTests.cs`, `FoundationEventLogRegressionTests.cs` (paketintern via Bootstrap, kein externes Framework).

### 8.9 Empfohlene Reihenfolge (erster Sprint)

1. Meta-Verträge prüfen (`INTERFACE_CONTRACT`, `SAVE_CONTRACT`, `COMPATIBILITY_MATRIX` sind in `docs/` vorhanden; Referenzen in Paket-Roadmaps auf `../docs/…` korrigiert).
2. Storage-Bridge und `AnyResourceCritical` gegen einen echten Save-/Event-Lauf belegen; danach Phase-1-Gate offiziell schließen.
3. Erste echte Gameplay-Mechanik: Bauschutt→Wand/Tür-Remap (Patches bereits angelegt, `mods/03/Patches/Bauschutt_Remap_Patches.xml`), größter sichtbarer Fortschritt.
4. Danach vertikale Full-Profile-Kette statt horizontales Fertigstellen einzelner Pakete.
