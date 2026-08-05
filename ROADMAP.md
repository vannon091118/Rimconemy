# Rimconemy — Root Roadmap

> **SSOT-Owner für:** Master-Plan, 5-Paket-Übersicht, Sole-Owner-Map, Phase-Hierarchie P0–P8, Paket-Identitäten. Wer ein Topic aus [docs/INDEX.md §1](INDEX.md) hier behandelt, hält eine SSOT-Verletzung fest.
> **Stand:** 2026-08-05 (Plan-Konsolidierung: Implementierungspläne aus `docs/superpowers/plans/` in §9 integriert und gelöscht — git-History behält die Originale; alte MD-Dateien archiviert in `docs/archive-md-2026-08-04.tar.gz`)
> **Zielplattform:** RimWorld 1.6.4566; Royalty, Ideology, Biotech, Anomaly, Odyssey
> **Status:** Phase 1 Coding-Cut mit Runtime-Boot-Gates belegt. Alle 5 Mods laden, Foundation erkennt FullOverhaul, alle Bootstraps und Regression-Summaries laufen; `scripts/runtime_test.sh` erzwingt frischen Log sowie Need-/Sandbox-/Patch-/Market-Gates. Save/Load, Kartenwechsel und vollständige Event-/Raid-Ausführung bleiben offen. Code-nahe Statusreferenz: `docs/CODE_STATUS.md`.
> **SSOT-Landkarte:** Die vollständige Topic-Tabelle *welche Datei was final besitzt* liegt in [docs/INDEX.md §1](docs/INDEX.md). Wer ein Topic dupliziert, hält eine SSOT-Verletzung fest.
> **Kanonische Dokumente:** `ROADMAP.md` (dieses Dokument), `docs/DECISIONS.md`, Architektur-Verträge (`docs/INTERFACE_CONTRACT.md`, `docs/SAVE_CONTRACT.md`, `docs/COMPATIBILITY_MATRIX.md`, `docs/ARCHITECTURE.md`), Falsifikations-Index (`docs/falsification/README.md`). Paket-Roadmaps: `mods/*/ROADMAP.md`.

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

### 2.5 Early-Game-Vertikalscheibe: Survival → Schutz → Energie

Die nächste spielerische Scheibe beginnt beim einzelnen Survivor und endet erst bei einer reproduzierbaren Infrastrukturentscheidung:

```text
Startcharakter + knappes Inventar
  → begrenzte Waffe/Munition
  → garantierter schwacher Drucktest ohne garantierten Loot
  → Schutzraum, Licht und erste Verteidigung
  →  Stahl als Rezeptinput; Kohle als physischer Refuelable-Brennstoff für ausgewählte Ofenrezepte und den Generator
  → T2-Strom: elektrischer Hochofen
  → eigene Munitionsproduktion
```

Verbindliche Regeln:

- Ruinen-Loot ist zufällig und darf die Kampagne nicht voraussetzen.
- Gegner-Drops sind keine garantierte Progressionsquelle.
- Fehlende Munition verweigert keine Vanilla-Arbeitstypen; der Spieler darf weiterhin bauen, sammeln, farmen und produzieren, soweit die normalen Inputs/Jobs vorhanden sind.
- Combat Extended bleibt optional und wird nicht zur Core-Abhängigkeit.
- Der elektrische Hochofen ist ein geplanter T2-Strom-Freischaltpunkt. Er benötigt einen dokumentierten Stein-/Eisen-/Stahl-Baupfad. Stahl ist Rezeptinput; Kohle wird über die physische Ofen-Refuelable-Mechanik nur für ausgewählte Rezepte verbraucht. Der Generator verbraucht Kohle separat für das PowerNet.

**Beweisgrenze:** Startinventar, Verbrauch, Nachtspawn, Hochofen, Research-Capability und Save/Load sind noch keine `LIVE`-Belege. Die Scheibe wird erst als geschlossen gewertet, wenn ein Runtime-Save die physische Änderung und ihre Folge ohne Drift überlebt.

### 2.6 Erfahrungsbaum: Wissen entsteht aus Handlung

Die Spielerprogression läuft nicht über einen Forschungstisch oder abstrakte Forschungspunkte. Der Survivor lernt durch physisch abgeschlossene Handlungen:

```text
Sammeln / Bauen / Feuern / Verarbeiten / Verteidigen
  → bestätigtes Ergebnis
  → Bereichserfahrung
  → Wissen und Freigabe
  → neues Architektenmenü-Rezept oder Gebäude
  → nächste Handlung
```

Der Start bleibt bewusst klein: **Notlager** mit Campfire, Schlafplatz, Lagerzone und Sammelaufträgen. Das erste erfolgreich entzündete Campfire öffnet **Zuflucht**; eine fertiggestellte Holz-Stahl-Barrikade (1 Holz + 1 Stahlrest) erzeugt Baukunst-Erfahrung und kann Tür, Feuerüberdachung und weitere Schutzoptionen freigeben.

Die sichtbaren Bereiche heißen **Überlebenswissen**, **Bergung**, **Feuerwissen**, **Baukunst**, **Verarbeitung**, **Maschinenwissen** und **Verteidigung**. Freigaben brauchen neben einer Bereichsstufe konkrete Ergebnisse, zum Beispiel einmal hergestellte Kohle, verarbeitete Maschinenteile oder erlebte stabile Energie.

Erfahrung entsteht nur nach echtem Abschluss. Platzieren, Abbrechen, Menüöffnen, reines Verschieben und triviales Spammen erzeugen keine Fortschrittsbelohnung. Diminishing Returns, Idempotency-Keys und situative Ergebnisboni verhindern, dass 200 Campfires den Survivor zum Meisteringenieur machen.

Vanilla-`ResearchProjectDef`s bleiben als DLC-/Mod-Kompatibilitätsschicht bestehen. Sie sind nicht die primäre Rimconemy-Freischaltlogik; der Forschungstisch wird nicht benötigt. Die Übersicht darf gelerntes Wissen anzeigen, darf aber keine zweite widersprüchliche Progressionsbahn erzeugen.

**Beweisgrenze:** Erfahrungsbereiche, echte Abschluss-Hooks, organische Architektenfreigaben, Anti-Exploit-Regeln und Save/Load-Persistenz sind Designziele und noch keine `LIVE`-Belege.

### 2.7 Vertikalscheiben-Plan „Die erste Nacht" (operativ)

Die operative Umsetzung von §2.5 und §2.6 ist in **§9.1** dieser Roadmap integriert (früher eigenständiges Plan-Dokument, gelöscht). Der Plan führt die nächsten 37 Subtasks in 12 Phasen (Phase 0 API/DLC → Phase 12 Research/DLC-Kompatibilität) mit Vanilla-API-Ankern, Datei-Pfaden, Owner-Paket und Akzeptanz-Gate pro Task auf. Er übernimmt die Architekturleitlinie **Defs/Game-/Map-Components/Harmony/Architect-Gates**, lässt Vanilla-`ResearchProjectDef` als Kompatibilitätsschicht bestehen und definiert das Vertikal-Scheiben-Release-Gate (`Single Survivor → Campfire → Tier-1-Barrikade → 1 Nacht → Save/Load`).

Der Plan reflektiert den aktuellen Code-Stand und markiert vorhandene Vorstufen (z.B. `BuildingProgressionAdapter`, `InfectedRaidSpawnService` mit 1-Pawn-Bridge, P0 Coal Chain) sowie offene `LIVE`-Aufgaben (SingleSurvivor-Szenario, eigener Ammo-Tank, Stahlreste ThingDef, `IncidentWorker_NightInfected` mit echtem Spawn, organische Architektenfreigaben).

Neue Designentscheidungen aus dem Plan sind in `docs/DECISIONS.md §25 Vanilla-API-Strategie` und `§26 KALT-als-Hediff` verankert. Bestehende §24 zu Early-Game-Munition bleibt unverändert.

### 2.8 Vanilla-API-Matrix (Source-of-Truth für 1.6)

Die verbindliche Mono.Cecil-verifizierte Vanilla-API-Matrix steht in `docs/vanilla-api-matrix-1.6.md`. Sie wurde am 2026-08-04 gegen die lokale RimWorld-1.6.4566-Assembly gespikt (SHA-256 dokumentiert) und führt für jeden der 15 Vanilla-Anker-Exemplare den BaseType, die Anzahl Public/Protected Methods und die kritischen Hook-Signaturen auf. Drei `SPIKE-PFLICHT NICHT GESCHLOSSEN`-Befunde sind in §8 der Matrix explizit dokumentiert und blockieren die korrespondierenden Phasen-Tasks:

- Phase 1.1: `Verse.ScenarioBase` existiert nicht → `RimWorld.Scenario` direkt verwenden
- Phase 3.2: keine Vanilla-Temperature-Helfer mit den Heuristik-Namen gefunden → TASK BLOCKED
- Phase 8.3: `FrameCompleted`/`FinishBlueprint`/`InstallBlueprint` haben 0 Treffer → TASK BLOCKED

Jeder zukünftige API-Aufruf gegen RimWorld 1.6 MUSS in der Matrix verifiziert sein; Inline-`strings`-Behauptungen sind unzulässig. Reproduktion: `dotnet tools/inspect/bin/Release/net10.0/Rimconemy.Inspect.dll`.

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
- ✅ **Regel 3 — `Transparency`** (neu in 0.0.36): `Rimconemy_Thought_InformedDecision` (+2/1Tag), `Rimconemy_Thought_UnexplainedDecision` mit kumulativen Stages (-6/-8/-10/-12 Mood), `TransparencyTracker` (`GameComponent`, Scribe-fähig), `ThoughtWorker_Transparency` (Child-less-of-`ThoughtWorker`), `Rimconemy_Transparency_Precept` (Issue `Rimconemy_TransparencyIssue`), `TransparencyRegressionTests` (4 Tests). Bridge: `StoryDirector` füttert den Tracker bei jedem erfolgreichen Event mit `(explained=true, reason=LastSelectionReason)`.
- ⬜ **RitualDef-Realisierung** für `Ritual_PostDefense` offen — RimWorld 1.6 zerlegt Rituale in `RitualBehaviorDef` + Outcome/Visual; weitere Iteration nach Ideology-Spike.
- ✅ **`Setting-/Erfahrungsfenster`**-Anzeige (neu in 0.0.37): `SettingRulesCatalog.ActiveRules()` liefert die 3 aktiven Regeln mit Träger + Wirkung als reine Daten. `SettingRulesInspector` rendert das Fenster in Foundation-Style. `OpenMainMenu()` ist der Einstiegspunkt für Foundation/Infectded-Tabs.
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

### 8.4 Phase 3/4 — Storage-only + Character Setup (offen)

- StorageSnapshot als **einzige** Quelle für UI + StoryDirector + Economy nachweisen (G4-Gate); Kartenwechsel-/Save-Load-Konsistenz und 11 H4-Randfälle (unloaded Map, Caravan, Cache, Credits …).
- Startalter 18/18 beim Pawn-Generator erzwingen (`FixedBiologicalAge`); `FixAge`-Reflection-Fallback aktiv (siehe `Page_ConfigureStartingPawnsBioPatch`), direkter `PawnGenerationRequest`-Spike in `docs/H6-pawn-generator-api-spike.md` (UNVERIFIED).
- `SingleSurvivor.xml` erweitern (aktuell 1 Pawn, 8 Kandidaten, kein Generator-Zwang).
- `CharacterSetupState`/Scribe-Schema mit `SchemaVersion` + `Applied`-Flag + `Records[thingIDNumber]->PawnSetupRecord(SkillDefNames/SkillLevels parallel + TraitDefNames + NeutralBand)` vorhanden (`mods/02/Source/Character/CharacterSetupState.cs`); Live-Schema-Bump-Pfad in `ExposeData` PostLoadInit. Persistente Seed-Stütze noch in Phase-4.3 vorgesehen.
- Bio-Remap- und Save/Load-Live-Test; H5-Balance-Gate für Budget 30 und Neutralzone `[-5,+3]`.

**Fortschritt Phase 3 (0.0.24-Patch, 2026-08-04):**

- ✅ **Caravan-Erweiterung** der `StorageQuery.AllMapsIncludingCaravans`-Scope — `CaravanStorageEnumerator` aggregiert Caravan-Inventare + Equipment über `Find.WorldObjects`/`GetDirectlyHeldThings`. Sentinel-kodierte MapIDs (`-(Caravan.ID+1)`) garantieren unterscheidbare Aggregatzeilen zwischen Heimkarten und Caravans. `CaravanStorageRegressionTests` mit Sentinel-Encode/Decode/Runde-Trip.

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
| 01-Rimconemy-Foundation | 0.1.37 |
| 02-Rimconemy-Survival-Progression | 0.1.32 |
| 03-Rimconemy-Scavenger-Infrastructure | 0.0.25 |
| 04-Rimconemy-Economy-Territory | 0.0.27 |
| 05-Rimconemy-Infected-Automation | 0.0.40 |

## 8. Offener Arbeits-Backlog (Stand 2026-08-04)

> Konsolidiert aus ehem. `docs/PLAN_GESAMTAUFGABEN.md` (Blöcke A–F), `HANDOFF.md`, `docs/LLM-SLOP-AUDIT-CHECKLIST.md` und `findings.md`. Vollständiger Archivstand aller konsolidierten Dokumente: `docs/archive-md-2026-08-04.tar.gz`.

### 8.1 Offene API-Spikes (Gates)

| Spike | Gegenstand | Blockiert |
|---|---|---|
| `API-IDEOLOGY-01` | `IdeoDef`/`PreceptDef`/`RoleDef`/`RitualDef` (IdeoDef liegt in DLC-Assembly, lokal nicht prüfbar) | Phase 2 |
| `API-START-01` | `PawnGenerationRequest.FixedBiologicalAge`/`GeneratePawn`/`GenerateTraits` | Phase 4 (Character Setup) |
| `API-NEED-01` … `API-MECH-01` | `COMPILES` vs. `STRING`-Belege auflösen (Need, Job, Resource, Trade, World, Incident, Mech) | je Paket |
| netstandard2.1 | Laufzeitverhalten (kein Spielstart-Beleg) | Runtime-Gates |

### 8.2 Falsifizierungsberichte (27 Berichte, Gate vor Übergabe)

Ohne `SURVIVED`-Berichte mit A–G-Belegen gilt keine Übergabe:

- Foundation (2): `Servicebus`, `BootstrapLogDedup`
- Survival (5): `Needs`, `WorkXp`, `ExperienceUnlocks`, `GameOver`, `SaveMigration`; `Research` bleibt Legacy-/Kompatibilitäts-Read-Model
- Scavenger (5): `ConstructionDebris`, `FoodAndHemp`, `WaterPowerArrowTurret`, `ExecutePhysicalTransfer`, `ReservePhysicalTransfer`
- Economy (5): `WalletCredits`, `Market`, `ReservePhysicalTransfer`, `OutpostProduction`, `TerritoryCountdown`
- Infected (5): `ThreatPressure`, `InfectedRaid`, `MechadroidJob`, `ManualRaid`, `AutoResolve`

**Status 2026-08-04:** 29 Bericht-Dateien unter `docs/falsification/` (27 aktiv-tracked Berichte in der Tabelle + `status-vs-code-audit-2026-08-04.md` + `README.md`). Davon: 22 Domain-Berichte (2 Foundation + 5 Survival + 5 Scavenger + 5 Economy + 5 Infected) und 5 Vertical-Slice-Early-Game-Berichte (`COMPILED (Pre-LIVE)` oder `UNVERIFIED`). Lifecycle-Schritte `A/B/C` aus dem Code ableitbar (`COMPILED`); `D/E/F/G` benötigen Live-Test. `foundation__BootstrapLogDedup.md` (ProfileDetector-Dedup) und `survival__SaveMigration.md` (236 Z., 7-Sektion-Layout) sind die zwei neuesten Stammsberichte; `earlygame__Survivor.md` und `earlygame__SaveLoad.md` sind die Early-Game-Eintritts-Punkte mit `COMPILED (Pre-LIVE)`-Status.

**Aktionsanleitung:** `./scripts/runtime_test.sh --require-scenario-tests` startet die Beleg-Sammlung. Logs in Block D, Save-Roundtrip in Block E, Cross-Read in Block F, Perf in Block G eintragen — Status wandert von `COMPILED` → `LOADED` → `OBSERVED` → `SURVIVED`.

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
- Startalter 18/18 beim Pawn-Generator erzwingen (`FixedBiologicalAge`); `FixAge`-Reflection-Fallback aktiv (siehe `Page_ConfigureStartingPawnsBioPatch`), direkter `PawnGenerationRequest`-Spike in `docs/H6-pawn-generator-api-spike.md` (UNVERIFIED).
- `SingleSurvivor.xml` erweitern (aktuell 1 Pawn, 8 Kandidaten, kein Generator-Zwang).
- `CharacterSetupState`/Scribe-Schema mit `SchemaVersion` + `Applied`-Flag + `Records[thingIDNumber]->PawnSetupRecord(SkillDefNames/SkillLevels parallel + TraitDefNames + NeutralBand)` vorhanden (`mods/02/Source/Character/CharacterSetupState.cs`); Live-Schema-Bump-Pfad in `ExposeData` PostLoadInit. Persistente Seed-Stütze noch in Phase-4.3 vorgesehen.
- Bio-Remap- und Save/Load-Live-Test; H5-Balance-Gate für Budget 30 und Neutralzone `[-5,+3]`.

### 8.5 Phase 5 — Vanilla-/DLC-Adapter (offen)

- Entscheiden: Setting-Director genügt oder direkter `StorytellerDef` (erst nach Storyteller-Spike).
- `StorytellerDef`/`StorytellerComp`/`IncidentDef`/`IncidentWorker`/Difficulty-Anker lokal (Reflection/Decompilation) prüfen.
- Vanilla-Wealth-Raids/Quest-/DLC-Incidents separat klassifizieren; genau **ein** Infizierten-Provider im Full Profile.
- Auswahl → Ausführung → Letter → Spawn → Auflösung idempotent speichern.

### 8.6 Phase 6 — Paket-Gameplay (offen, Stubs → echte Mechaniken)

- **02 Survival:** 2.1 Pawn-/Start-/Szenariovertrag; 2.2 Kernbedürfnisse (Nahrung/Sicherheit/Soziales); 2.3 Arbeit→Erfahrung ohne Tick-Sampling (nur validierter Output + Diminishing + Idempotency-ID); 2.4 Erfahrungsbaum der Bereiche und organische Freigaben statt Forschungsbaum; 2.5 Game Over exakt einmal; 2.7/2.8 DLC- und Save-Migration.
- **03 Scavenger:** 3.2 Bauschutt→Waffen-Komponente (Vor-T3 Loot, Ab-T3 craftbar; kein Wand-Material); siehe DECISIONS §29. der End-to-end-Live-Beleg bleibt offen. Weiter offen: 3.3 Nahrung/Hanf getrennt (WorkGiver/Ernte/Verderb); 3.4 Wasser-/Brennstoffmodell als physischer Pfad; 3.5 Stromnetz mit harten Input-Regeln; 3.6 Pfeilturm (Strom als harte Bedingung, Zustände Active/Blocked/Offline/Damaged); 3.7–3.10 XP-/Research-/Economy-/Threat-Adapter, DLC-/Save-Kompatibilität.
- **04 Economy:** 4.1 Wallet atomar/rollbackfähig; 4.2 Markt deterministisch; 4.4 Outpost-Gründung; 4.5 Produktion/Verteidigung (Brutto/Bindung/Wartung/Netto); 4.6 Proxy-Graph + Drei-Tage-Countdown (180.000 Ticks, F1-Fix); 4.7 World-Map-Overlay; 4.8 automatisierte Raids; 4.9/4.10 Integrationen + DLC-/Save-Kompatibilität.
- **05 Infected:** 5.1 `HiddenInfected`/`InfectedRavager` plus begrenzte 1-Pawn-Spawn-Bridge sind im lokalen Arbeitsstand als CODE/DEF ergänzt; vollständige Raid-Skalierung, Auflösung und Live-Beleg bleiben offen. 5.2 Bedrohungsaggregator deterministisch; 5.4 lokale Raids; 5.5 `WorldRaidCoordinator` als Planungsgerüst, World-Map-Raid-Lifecycle offen; 5.6 Mechadroid-Grundsystem; 5.7 Automation-Aufträge; 5.9 Hauptstädte/Endgame; 5.10–5.12 Integrationen, DLC-/Vanilla-Kompatibilität, Save/Performance/Determinismus.

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
3. Bauschutt→Waffen-Komponente end-to-end verifizieren: lokaler Arbeitsstand enthält Loot-Pfad, MakeWeaponComponent-Rezept nach T3 und Tower-costList-Eintrag; `mods/03/Patches/WallDoorBarricade_Bauschutt_Patches.xml` wird im Zuge der D2/D3-Harmonisierung zu `WallDoorBarricade_Woody_Allowed.xml` umbenannt (siehe DECISIONS §28, §29).
4. Danach vertikale Full-Profile-Kette statt horizontales Fertigstellen einzelner Pakete.

## 9. Integrierte Implementierungspläne (Stand 2026-08-05)

> **Konsolidierung:** Die eigenständigen Implementierungspläne unter `docs/superpowers/plans/` wurden am 2026-08-05 in diese Roadmap integriert und die Dateien gelöscht. Die git-History behält die Originale; Querverweise in Doku, Falsifikations-Berichten und Tests zeigen auf §9.x. Neue Implementierungs-Tasklisten gehören ab sofort in diese Sektion statt in Einzeldateien.

### 9.1 Early-Game-Vertikalscheibe „Die erste Nacht" (2026-08-04)

> **Ehemalige Datei:** `2026-08-04-early-game-vertical-slice.md` (gelöscht) · **Status:** Plan; Phase 0–7.3 + 8.1–8.4 + 9.1–9.4 warten auf den `LIVE`-Save/Load-Beleg · **Verwandte Sektionen:** §2.5, §2.6, §2.7, §8.6, `docs/falsification/earlygame__*`.

**Release-Gate:** `Single Survivor → Campfire (Wärme+Licht) → KALT-Hediff → Tier-1-Barrikade (1 Holz + 1 Stahlrest) → 1 kontrollierter Nachtspawn → XP für echte Abschlüsse → Save/Load ohne Drift`.

**12 Phasen / 37 Subtasks (Owner):**

| Phase | Inhalt | Owner | Status |
|---|---|---|---|
| 0 | Vanilla-API-Matrix (0.1) + DLC-Policy (0.2) | Foundation | ✅ `docs/vanilla-api-matrix-1.6.md` + DLCFilter |
| 1 | Single-Survivor-Szenario (1.1), Notwaffe (1.2), Ammo-State (1.3), Startgegner getrennt vom Nachtspawn (1.4) | 02/05 | ⬜ LIVE offen |
| 2 | Stahlreste ThingDef (2.1), Startressourcen (2.2), Campfire (2.3), Campfire-Rezepte (2.4) | 03 | ⬜ LIVE offen (P0-Coal-Chain-Code vorhanden) |
| 3 | KALT-Hediff (3.1) + Temperatur-Update (3.2, **SPIKE BLOCKED**) | 02 | ⬜ |
| 4 | Tier-1-Barrikade Mischkosten (4.1) + Rimconemy-Architect-Kategorie (4.2) | 03 | ⬜ |
| 5 | `ShelterSnapshot` (5.1) + Feuer-Signatur (5.2) | 03 | ⬜ |
| 6 | `WatchRadiusSnapshot` (6.1) + Gefahrenwarnung LOS (6.2) | 02/05 | ⬜ |
| 7 | Nacht-Scheduler MapComponent (7.1), pure Spawnformel (7.2), `IncidentWorker_NightInfected` (7.3) | 05 | ⬜ (Stubs; siehe §9.8) |
| 8 | Domain-XP (8.1), ActionResult-Vertrag (8.2), Bauabschluss-Hook (8.3, SPIKE geschlossen: `Frame.CompleteConstruction`), Rezeptabschluss (8.4) | 02/03 | ~ (`BuildingProgressionAdapter` vorhanden) |
| 9 | UnlockExtension (9.1), `UnlockService` (9.2), Architect-Gate (9.3), erster Lernpfad (9.4) | 02 | ⬜ |
| 10 | Kohlekette (10.1), Machine-Parts (10.2), Power-Gate (10.3), erste Automation (10.4) | 03/05 | ~ P0-Coal-Chain-Code vorhanden, LIVE offen |
| 11 | Outpost-Gate (11.1), Kosten (11.2), Proxy-Graph (11.3), 4X-Bedrohung (11.4) | 04 | ⬜ |
| 12 | Vanilla-Research nicht zerstören (12.1), DLC-Fallbacks (12.2) | 01 | ⬜ |

**Wichtige Vertragspunkte (übernommen aus dem Plan):**
- Startgegner werden **nicht** als Nachtspawn gezählt (Task 1.4).
- Drei getrennte Nacht-Pfade: `StoryDirector` (Story-Events), `InfectedRaidSpawnService/InfectedRaidWorker` (Raid+Letter), `RimconemyNightComponent` (bounded Nacht-Auswertung, max. 1 Spawn/Nacht).
- Eigene Defs sind begründungspflichtig; Stuff-/Patch-Strategie (`ConstructionDebris` + `Stony`) hat Vorrang vor Listen-Replace (Drift-Acknowledgement, siehe §8 des Domain-Map).

### 9.2 Phase-First Gameplay-Implementierung (2026-08-05)

> **Ehemalige Datei:** `2026-08-05-phase-first-gameplay-implementation-plan.md` (gelöscht) · **Status:** Plan · **Scope-Slice (verbindlich zuerst):** Task 0 → 10 (`Early Survival → kontrollierte Stahlreste-Verarbeitung → Automation`); DLC-Adapter (11–14) danach.

**Kernregeln:** Ressourcen-SSO (SteelScraps genau ein Owner: Mod 03); Early = Visible/Lootable, nicht Producible; ein `CompProperties_Refuelable` pro Gebäude; keine erfundenen StuffCategories (`Stony`/`Metallic`/`Woody` genügen); Vanilla-/DLC-Anker vor Eigenbauten; kein MiningGate-Save-State; `DefModExtension` ist Daten, kein Verhalten.

**Task-DAG (0–18):** 0 Baseline → 1 Phasen-Vertrag → 2 SteelScraps-SSO → 3 Early-Scatter → 4 Vanilla-Blueprint-Audit → 5 Campfire-Parität → 6 Rezept-Phasen-Gates (BurnSteelScraps 5:1 Steel, MakeCoal Cooking≥3) → 7 Konservative Vanilla-Patches (Stufe 1/2/3-Rubrik) → 8 Mining≥8-API-Spike → 9 Generator-/Fuel-Modell → 10 Core-only-Vertikalscheibe → 11–14 DLC-Adapter (Ideology/Biotech/Anomaly+Odyssey/Royalty) → 15 Economy-Boundary-Refactor (`BuildingInputAdapter` liest Defs statt hartkodierter Tabelle) → 16 Tests/Falsifikation → 17 Validierungs-Matrix → 18 Doku-Crosswalk.

**Verifikations-Vokabular:** `CODE`/`DEF`/`COMPILES`/`BOOT` = Implementierungs-Beleg; `LIVE` = echter Gameplay-Beleg; Phase erst ab bestandenem LIVE-Gate abgeschlossen.

### 9.3 Building-Core Meilenstein A (2026-08-04)

> **Ehemalige Datei:** `2026-08-04-building-core-implementation-plan.md` (gelöscht) · **Status:** überwiegend implementiert (A-Gate: Code/Def/Compiles; interaktive Bau-/Strom-/Save-Load-Gates bleiben Runtime-Beleg).

**Ziel:** Physische ConstructionDebris-Wand/Tür, Storage-Readback, befeuerte Stromerzeugung, Turm-Zustand, Capability-Read-Models über alle 5 Pakete — ohne zweite Wahrheit für Credits/Ressourcen.

**Tasks (1–8):** 1 API-Lock + rote A-Gate-Tests → 2 Bauschutt-Wand/Tür-Kostenpfad (PatchOperationAdd `Stony` auf Wall/Door) → 3 `BuildingSnapshot`/`BuildingSnapshotService` + ehrlicher `PowerChainService`-Zustand → 4 Foundation-Capability `rimconemy.scavengerinfrastructure.building` v1 → 5 `BuildingProgressionAdapter` (idempotente XP-Awards) → 6 `BuildingInputAdapter` (physisch vs. Credits) → 7 `BuildingThreatAdapter` (`ComputePressure` gedeckelt `[0,1]`) → 8 Build/Deploy + Static-Gates + interaktive A-Gates.

### 9.4 UI P0–P4 (2026-08-04)

> **Ehemalige Datei:** `2026-08-04-rimconemy-ui-p0-p4.md` (gelöscht) · **Status:** überwiegend implementiert (Shared Toolkit + Foundation-Dashboard + Survival/Threat/Economy/Scavenger-Screens; in-game-Rendering bleibt Runtime-Gate).

**Ziel:** Gemeinsame Visual Language (Tokens + Helpers in `RimconemyTheme`/`RimconemyUi`), ehrliche Empty-/Error-States, UI read-only über Read-Models, keine Simulations-Mutation durch UI.

**Tasks (1–8):** 1 UI-Kontrakte/Runtime-Read-Models mappen (✅) → 2 Shared Toolkit (`DrawStatCard`/`DrawSparkline`/`DrawTabs`/`DrawCountdown`/`DrawPressureGauge`, DangerSoft/PanelInk/DividerInk) → 3 Survival-P0-Dashboard → 4 Infected-Threat-Screen → 5 Economy-Hub → 6 Scavenger-Infrastructure-Tab → 7 Foundation-Dashboard-Polish → 8 Build-Gates + Review.

### 9.5 UI Honest Character Vertical Slice (2026-08-04)

> **Ehemalige Datei:** `2026-08-04-ui-honest-character-vertical-slice.md` (gelöscht) · **Status:** implementiert (Bootstrap-Befund A3 im Audit bestätigt: `DrawFeatureStatus` in `RimconemyUi.cs:64-88`).

**Ziel:** `RimconemyUi.DrawFeatureStatus(state, detail, StatusLevel)` (textbasiert, try/finally GUI-Reset); `CharacterSetupState.RecordAppliedPawns` null-sicher/idempotent; ehrliche Capability-Banner (`LIVE`/`READ-ONLY`/`PARTIAL`/`OPEN`) auf allen 5 Dashboards. Kein Fake-Gameplay durch UI.

### 9.6 Runtime-Test-Erweiterung (2026-08-04)

> **Ehemalige Datei:** `2026-08-04-runtime-test-extension.md` (gelöscht) · **Status:** implementiert (`scripts/runtime_test.sh`; frischer Player.log, deploy-first-Ordering, `--require-scenario-tests` optional).

**Kernverträge:** `scripts/deploy.sh` als einzige Deploy-Implementierung; alter Log zählt nie als frischer Beleg; Exit-non-zero bei verfehlten Pflicht-Gates; Save/Load-/FinalizeInit-Gates bleiben explizit optional (Boot-only kann sie nicht beweisen).

### 9.7 GitHub-Presence (2026-08-04)

> **Ehemalige Datei:** `2026-08-04-github-presence.md` (gelöscht) · **Status:** implementiert (`README.md` player-first, `CONTRIBUTING.md`, `banner.html`/`banner.svg`).

**Regeln:** Deutsch als Primärsprache; trockener ironischer Ton ohne das Verstecken von Limits; keine erfundenen Releases/Workshop-Links/Lizenzen; `CODE`/`DEF`/`COMPILES`/`BOOT`/`LIVE` klar getrennt; kein Code-/Def-/Gameplay-Eingriff durch Doku-Tasks.

### 9.8 Infizierten-Sensorik (2026-08-05)

> **Ehemalige Datei:** `2026-08-05-infected-sensing-tasklist.md` (gelöscht) · **Status:** Plan (Analyse abgeschlossen, kein Code geändert) · **Quelle:** `docs/CHAT_PROTOCOL_2026-08-05.md` §1.3 · **User-Anforderung:** höhere Dichte, die auf Licht „hört", Radius X, begrenzte Sichtweite.

**Kernbefund:** Sensorik ist in §9.1 Phase 5–7 dokumentiert, aber **nicht als zellbasiertes Sensing** und nicht implementiert (`Source/Night/` existiert nicht; `GlowGrid`-API 0 Treffer in Matrix/Code).

**Tasklist (priorisiert):**
- **T1 [SPIKE] Licht-Level pro Zelle** (`Map.glowGrid`, `GlowGrid.GameGlowAt/GlowAt/PsychGlowAt`) → Blocker vor T2/T4.
- **T2** Pure `NightSensingFormula` (`ComputeDetectLevel(light, lightRadius, hearingRadiusX, sightRange, pawnCount, nightIndex, protection)`) — verschärft die §9.1-Phase-7.2-Formel um Licht/Radius/Sicht; Tests deterministisch ohne RimWorld.
- **T3** `RimconemyNightComponent` (MapComponent; max. 1 Auswertung/Nacht, savebarer `lastEvaluatedNight`).
- **T4** `IncidentWorker_NightInfected` + `Rimconemy_NightInfected` IncidentDef; echter Pawn-Spawn; ⚠️ nicht zu Startgegnern (Task 1.4) addieren.
- **T5** „Radius X Tiles": zellbasiertes Radius-Scan um Lichtquellen (`CompGlower.Glows && ShouldBeLitNow`, Matrix §3.9); Default ~20 Tiles.
- **T6** „Begrenzte Sichtweite": `GenSight.LineOfSight(3-arg)` als AI-Gate; Default ~15 Tiles; kein Combat-LOS-Override.
- **T7** „Höhere Dichte": `ComputeSpawnCount`-Skalierung (0..3 → formelgesteuert) + Threat-Pressure-Kopplung; keine exponentielle Druck-Schleife.
- **T8/T9** FirstNight-SURVIVED-Pfad (Gate A–H aus `docs/falsification/earlygame__FirstNight.md`) + Cross-Read `rimconemy.infectedautomation.threat`.
- **T-B1/T-B2** Bauschutt-Platzierungs-Bug (#6): Diagnose Kartenrand-Platzierung + Campfire-Icon (falsches `graphicData`/`uiIconPath`-Erbe, falsche ScenPart-Zielzelle) + Fix.

**Offene Design-Fragen (an den User, nächste Runde):**
1. Radius X / Sichtweite Defaults: X = 20 Tiles Hör-Radius, 15 Tiles Sichtweite — als Startwert ok?
2. Soll das „Hören" NUR auf Licht (Campfire/Glower) oder auch auf Geräusche (Bauen, Schüsse, Arbeit) reagieren? (Bisher: Licht-only gemäß User-Aussage.)
3. Dichte-Kurve: lineare Steigerung mit Licht-Exposition oder Stufen (Ruhe → erhöht → Hochalarm)?

**Gegengeprüfte Vanilla-Fakten:** `GenSight.LineOfSight` §3.7/§4.4 ✅ · `FogGrid.IsFogged` §4.6 ✅ · `CompGlower.Glows/ShouldBeLitNow` §3.9 ✅ · Anomaly-Shambler = PawnKind-Basis (DECISIONS §19, `DLCContentPolicy.cs:96`) · Anomaly + Odyssey Hard-Requires (DECISIONS §16/§18).

### 9.9 Design-Specs (konsolidiert 2026-08-05, Artefakte gelöscht)

> Die eigenständigen Design-Specs unter `docs/superpowers/specs/` wurden am 2026-08-05 in die jeweiligen SSOT-Homes integriert und gelöscht (git-History behält die Originale). Die Specs waren im Hauptbaum verwaist (nur eine interne Spec→Spec-Referenz, keine ROADMAP-/INDEX-/DECISIONS-Referenz). Neue Design-Spezifikationen gehören ab sofort direkt in das angegebene Home statt in Einzeldateien. **Wiederherstellung eines gelöschten Originals:** `git show HEAD:docs/superpowers/specs/<dateiname>.md` (bzw. `docs/superpowers/plans/<dateiname>.md` für §9.1–9.8).

| Ehemalige Spec (gelöscht) | Kern-Inhalt | SSOT-Home | Status der Inhalte |
|---|---|---|---|
| `2026-08-04-architecture-boundaries.md` | Capability-Owner-Map (F-V1..F-V5): `ColonialReader`-Zentralisierung, GameOver-Sole-Owner 02, Storage-Bridge, Ideology-Grenze, Capability-Gates | `docs/INTERFACE_CONTRACT.md` §9 (Capabilities/Owner-Map), `docs/DECISIONS.md` §21–23 | ✅ umgesetzt (CapabilityAudit, ColonialReader, StoryState.MarkGameOverPending) |
| `2026-08-04-building-feature-full-design.md` | Meilensteine A→B→C: Building-Core, Outposts/Automation, World-Map/Infected-Raids; Snapshot-/Transfer-/Idempotenz-Verträge | ROADMAP §9.3 (Building-Core), §8.6 Paket-03, `docs/SAVE_CONTRACT.md` | ~ Meilenstein A statisch/BOOT umgesetzt; interaktive A-Gates offen |
| `2026-08-04-github-presence-design.md` | README-2-Stufen-Landingpage, CONTRIBUTING, Banner-Kopie; Ton/Statuswahrheit | ROADMAP §9.7 (GitHub-Presence) | ✅ implementiert (`README.md`, `CONTRIBUTING.md`, `banner.html`/`.svg`) |
| `2026-08-04-ui-foundation-toolkit-design.md` | `RimconemyTheme`-Tokens, `RimconemyWindow`/`RimconemyMainTabWindow`/`RimconemyInspectTab`, `RimconemyUi`-Helper, RimThemes opt-in | `mods/01/Source/UI/` (Code ist SSOT), ROADMAP §9.4 | ✅ implementiert (`RimconemyTheme.cs`, `RimconemyUi.cs`, Base-Klassen) |
| `2026-08-04-track-a-character-design.md` | Track A: Startalter 18/18, Skillbudget 30, Neutralzone, Trait-Pools, Bio-Override, Spawn-Hook-Kette | `docs/H5-character-setup-formula.md`, ROADMAP Phase 4 / §8.4, `mods/02/Source/Character/` | ~ Kern umgesetzt (SkillBudgetCalculator, TraitAssigner, CharacterSetupState); Bio-Override + Generator-Hook offen |
| `2026-08-04-character-setup-hybrid-design.md` | Hybrid-Schnitt: Pure Budgetlogik + deterministische Trait-Auswahl, Seed-Vertrag, Runtime-Kompatibilitätsgrenze | `docs/H5-character-setup-formula.md` (Abschnitt „Erledigter Hybrid-Schnitt"), `mods/02/Source/Character/` | ✅ umgesetzt (Pure-Auswahl, FNV-1a-Seed, kein globaler Rand) |
