# Roadmap 02 – Rimconemy Survival & Progression

> **SSOT-Hinweis:** Owner-Matrix → [../../docs/INTERFACE_CONTRACT.md](../../docs/INTERFACE_CONTRACT.md); Tech/Wissen & Experience → [../../docs/CANONICAL_VANILLA_DOMAIN_MAP.md §2.4](../../docs/CANONICAL_VANILLA_DOMAIN_MAP.md); ISchemaMigratable → [../../docs/SAVE_CONTRACT.md](../../docs/SAVE_CONTRACT.md); Theme-Map → [../../docs/INDEX.md §1](../../docs/INDEX.md).
> Eigenständige Paketaufgabe 2 von 5  
> Standalone zuerst, Full-Overhaul-Integration danach  
> Zielplattform: RimWorld 1.6 mit Royalty, Ideology, Biotech, Anomaly und Odyssey

## 1. Paketauftrag

Survival & Progression definiert den geplanten Survival-/Progressions-Loop. Im aktuellen Code sind Need-Mapping, Progression-Read-Model, Character-Setup-Logik, Sandbox-/Game-Over-Anker und Regression-Gates belegt; vollständige Need-, Job-/Output-Erfahrungs-, Experience-/Unlock- und Save/Load-Live-Schichten bleiben offen. Das vorhandene Research-Read-Model ist Legacy-/Kompatibilitätsschicht.

Im Full Overhaul wird es zur Progressionsdomäne für Bauschuttbau, Energie, Verteidigung, Mechadroids und Outposts. Diese Verbindungen laufen über Capability-IDs und Arbeitstyp-Verträge, nicht über direkte Compile-Abhängigkeiten auf andere Rimconemy-Assemblies.

## 2. Standalone-Ziel

Mit Vanilla-Ressourcen, Vanilla-Gebäuden und Vanilla-Fraktionen bietet das Paket:

```text
Nahrung/Sicherheit/Soziales
→ Handlung und Arbeit
→ bestätigtes Ergebnis
→ Überlebenswissen / Freigabe
→ höhere Überlebensfähigkeit
→ Game Over bei Verlust aller kontrollierbaren Bewohner
```

Der Start beginnt mit einem fast leeren Architektenmenü (**Notlager**): Campfire, Schlafplatz, Lagerzone und Sammeln. Der Survivor lernt durch echte Ergebnisse; der Forschungstisch ist nicht die Quelle der Rimconemy-Freigaben.

Das Paket muss auch ohne Bauschutt, Economy, Outposts und Infizierte build-/bootstrap-fähig bleiben; ein vollständiger eigenständiger Survival-Loop ist ein offenes Live-Gate.

### Early-Game-Vertrag: Survivor, Startinventar und Arbeit

- Der Einzelstarter erhält die begrenzte Frühwaffe und Munition über den Startcharakter-/Szenariovertrag, nicht über einen verpflichtenden Gegner-Drop.
- Der erste schwache Gegner ist ein Drucktest. Gegner liefern keine garantierte Munition, keine garantierten Stahlreste und keine garantierten Maschinenteile.
- Zufällige Ruinenfunde dürfen die Startreserve ergänzen, sind aber keine notwendige Voraussetzung für den ersten Schutz- oder Produktionsschritt.
- Fehlende Munition sperrt keine Arbeitstypen. `Building`, `Farming`, `Scavenging`, `Power`, `Engineering`, `Combat`, `Social/Trade`, `Expedition` und `Automation` bleiben als Verträge verfügbar; konkrete Jobs können weiterhin nur durch ihre normalen Ressourcen-, Skill- und Reservation-Regeln blockiert werden.
- Der spätere Munitionspfad gehört in die physische Infrastruktur von Paket 03: Stahl → Munition im elektrischen Hochofen (T2 Energy); ausgewählte Rezepte verbrauchen Kohle über die Ofen-Refuelable-Mechanik, der Generator verbraucht Kohle separat.

**Status:** Design entschieden, Implementierung und Runtime-Beleg offen. Diese Regeln dürfen nicht als vorhandene Startwaffe, Munition oder Nachtspawn gelesen werden.

## 3. Full-Overhaul-Ziel

Im gemeinsamen Profil:

- Scavenger-Arbeiten melden standardisierte Arbeitstypen für XP.
- Bauschutt-, Farm-, Energie- und Verteidigungsarbeit nutzt dieselbe Progression.
- Wissen und organische Freigaben schalten Wasser, Strom, Pfeilturm, Mechadroids, Credits und Outposts frei.
- Der T2-Strom-Pfad umfasst später den elektrischen Hochofen für physische Munitionsproduktion; die Freigabe darf diese Aktion erst öffnen, wenn Def, Rezept, Arbeitsweg und Save-Verhalten belegt sind.
- Sicherheit verarbeitet Basiszustand, Infizierten-Druck und territoriale Verbindung.
- Game Over bleibt an direkt kontrollierbare Spielerbewohner gebunden.
- Foundation zeigt Needs, XP, Forschung, Modus und Blockadegründe in gemeinsamen Snapshots.

## 4. Geschlossene Systemdefinition

### Kernbedürfnisse

Spielerrelevante Kernbedürfnisse:

1. Nahrung
2. Sicherheit
3. Soziales

Körper- und Gesundheitszustände bleiben erhalten, sind aber keine versteckten vierten Kernbedürfnisse:

- Gesundheit
- Verletzung
- Blutverlust
- Krankheit
- Erschöpfung
- Temperatur

Für jeden beibehaltenen Vanilla-Zustand muss festgelegt werden, ob er die drei Kernbedürfnisse, Arbeitsfähigkeit oder direkte Gesundheit beeinflusst. Unveränderte Vanilla-Mood-Skalierung darf die neue Anzeige nicht heimlich überstimmen.

### Arbeit → Erfahrung

Jeder Arbeitstyp besitzt einen stabilen Rimconemy-Bereich:

```text
Building
Farming
Scavenging
Power
Engineering
Combat
Social/Trade
Expedition
Automation
```

Ein Arbeitsschritt kann Erfahrung geben, aber nicht unbegrenzt farmbar sein. XP muss an geleistete Arbeit, tatsächlichen Output, Risiko und sinnvolle Abkling-/Diminishing-Regeln gebunden werden.

### Wissen und Freigaben

Rimconemy verwendet einen **Erfahrungsbaum der Zivilisation** statt eines klassischen Forschungsbaums. Der Survivor entwickelt bereichsbezogenes Wissen durch echte Handlungen:

- **Überlebenswissen:** Nahrung finden, Kälte und Nächte überstehen, Verletzungen behandeln.
- **Bergung:** Holz, Stahlreste und Bauschutt tatsächlich bergen.
- **Feuerwissen:** Campfire entzünden, Brennstoff vorbereiten und Wärme kontrollieren.
- **Baukunst:** Barrikaden, Wände, Türen und Reparaturen fertigstellen.
- **Verarbeitung:** Kohle, Stahlreste und Maschinenteile physisch herstellen/verarbeiten.
- **Maschinenwissen:** Generatoren und Verbraucher stabil betreiben, Ausfälle beheben.
- **Verteidigung:** vorbereitete Gefahren überstehen und Zuflucht sinnvoll verteidigen.

Eine gültige Handlung läuft immer über:

```text
Material/Arbeitsauftrag
→ echter Abschluss
→ genau eine idempotente Erfahrung
→ Bereichsstufe + konkrete Voraussetzung
→ Freigabe im Architektenmenü
```

Keine Erfahrung entsteht durch Platzieren, Abbrechen, Menüöffnen, bloßes Verschieben oder billiges Spam-Rezept. Diminishing Returns, neue Material-/Rezeptboni und Situationsboni verhindern Exploits.

Vanilla-`ResearchProjectDef`s bleiben als Kompatibilitäts- und Übersichts­schicht für DLCs und Fremdmods erhalten. Sie ersetzen nicht den Erfahrungsbaum; Rimconemy benötigt für seine Freigaben keinen Forschungstisch und darf keine parallele widersprüchliche Progression erzeugen.

## 5. Sequenzielle Arbeitsschritte

### Task 2.1 – Pawn-, Start- und Szenariovertrag

> **Vertikal-Scheiben-Verankerung (2026-08-04):** Der konkrete Sub-task-Block zu Single-Survivor-Szene, Notwaffe und Ammo-Tank steht in Phase 1.1–1.4 des Vertical-Slice-Plans. Die Architekturentscheidungen sind in `DECISIONS.md §24` festgelegt; eine eigene Phase für die Vanilla-API-Verifikation des `PawnGenerator`-Spikes (`H6-pawn-generator-api-spike.md`, UNVERIFIED) bleibt offen.



- eigenen Startcharakter über RimWorld-kompatiblen Szenario-/Pawn-Generator definieren,
- Aussehen und zulässige Individualisierung erhalten,
- Backstories, Traits, Xenotypen und DLC-Pawn-Regeln prüfen,
- keine ungewollten Startimmunitäten oder Startressourcen aus Biotech/Odyssey übernehmen,
- Game-Over-Anker definieren: direkt kontrollierbare Spielerbewohner.

**Blindspot-Gate:** Pawn-Generierung, Backstories, Traits, Xenotypen, Kinder/Alterung, Gene und Start-Spot dürfen nicht unkontrolliert Vanilla-Progression oder Bedürfnisse umgehen.

**Exit-Test:** Neue Kampagne startet reproduzierbar mit einem zulässigen individuellen Startcharakter und dokumentiertem Startinventar.

### Task 2.2 – Bedürfnis- und Zustandsmodell

> **Vertikal-Scheiben-Verankerung (2026-08-04):** Die Kältemechanik (Hediff-Severity-Offset) ist als eigene Architekturentscheidung in `DECISIONS.md §26` dokumentiert; Sub-tasks stehen in Phase 3.1/3.2 des Vertical-Slice-Plans. Die Hediff-Logik gehört zu diesem Task.



- Nahrung, Sicherheit und Soziales als sichtbare Kernbedürfnisse registrieren,
- Vanilla-Mood-/Mental-Break-Anbindung festlegen,
- körperliche Zustände getrennt behandeln,
- Traits, Ideologies, Genes, Psycasts und Hediffs auf Einfluss prüfen,
- `Unavailable`/deaktivierte Zustände nicht als Nullwert anzeigen.

**Exit-Test:** Ein Test-Pawn kann jeden Kernbedarf verändern; UI erklärt Ursache, Trend und Konsequenz.

### Task 2.3 – Arbeitstypen und XP

- Arbeitstypen stabil definieren,
- XP aus realer Arbeit und nicht aus bloßem Tick-Aufenthalt vergeben,
- Effizienzformel definieren,
- Arbeitsgeschwindigkeit, Qualitätschance und Fehler-/Wartungseinfluss begrenzen,
- Vanilla-Skills entweder bewusst adaptieren oder sichtbar parallel behandeln.

**Blindspot-Gate:** WorkGivers, JobDrivers, Reservations, Hauling, Repair, Caravan- und Odyssey-Transportjobs müssen geprüft werden. Ein Pawn darf durch XP-Logik nicht in Reservierungs- oder Job-Loops hängen.

**Exit-Test:** dieselbe Arbeit erzeugt deterministisch nachvollziehbare XP; mehr Erfahrung verändert mindestens einen messbaren Output.

### Task 2.4 – Erfahrungsbaum und Architektenfreigaben

> **Vertikal-Scheiben-Verankerung (2026-08-04):** Die operative Sub-task-Struktur zu diesem Task steht in `ROADMAP.md §9.1` Phase 8.1–8.4 (Domain-XP, ActionResult-Vertrag, Bauabschluss-Hook, Rezeptabschluss-Hook) und Phase 9.1–9.4 (UnlockExtension, UnlockService, Architect-Gate, erster Lernpfad). Die zugehörige Designentscheidung ist `DECISIONS.md §25`. Der vorliegende Task bleibt als Ausgangspunkt im Paket-Roadmap stehen.



Der Startpfad bleibt bewusst klein:

```text
Notlager
→ Campfire erfolgreich entzündet
→ Zuflucht: Holz-Stahl-Barrikade (1 Holz + 1 Stahlrest)
→ Tür / Feuerüberdachung / Vorratszone
→ Kohle / Maschinenteile
→ Generator / Energie
→ Elektrohochofen / Munition
→ Arbeitsmaschinen / Automation
```

Jede Freigabe besitzt:

- eine Bereichs-ID,
- eine sichtbare Spielerbezeichnung,
- ein echtes Abschlussereignis,
- eine idempotente Action-ID,
- optionale zusätzliche Voraussetzungen,
- einen sichtbaren Architektenmenü-Output,
- eine Save-/Load-Version.

`ResearchProjectDef` bleibt für Vanilla-/DLC-Kompatibilität vorhanden, ist aber nicht die primäre Quelle der Rimconemy-Freigabe.

**Exit-Test:** Eine abgeschlossene Handlung erzeugt genau einmal Erfahrung, erweitert reproduzierbar das Architektenmenü und überlebt Save/Load ohne Phantomfreigabe oder Verlust.

### Task 2.5 – Game Over und Wiederaufnahme

- letzter direkt kontrollierbarer Bewohner löst Game Over aus,
- abstrakte Outpost-Bevölkerung und Mechadroids verhindern es nicht,
- Spielstand kann geladen werden,
- Game-Over-Ereignis wird geloggt,
- kein automatisches „Rettungs“-Fallback durch Fraktionen.

**Exit-Test:** Alle Spielerbewohner sterben in einem Testsave; Game Over tritt genau einmal und nachvollziehbar ein.

### Task 2.6 – UI und Foundation-Adapter

Standalone-UI:

- Kernbedürfnisse,
- XP nach Bereich,
- aktive Forschung,
- Arbeitsstatus,
- Start-/Game-Over-Status,
- Ursachen für Effizienzverlust.

Foundation-Adapter optional:

- gemeinsame Snapshots,
- Profilstatus,
- Eventlog,
- Save-Status.

**Exit-Test:** Paket funktioniert mit und ohne Foundation, ohne doppelte Balken oder widersprüchliche Werte.

### Task 2.7 – DLC- und Vanilla-Kompatibilität

Prüfe explizit:

- Royalty: Psycasts, Titel, Quests,
- Ideology: Roles, Rituals, Precepts, Social/Mood,
- Biotech: Genes, Xenotypes, Mechanitors, Children, Pollution,
- Anomaly: Hediffs, Entities, Quests, Mind-/Fear-Effekte,
- Odyssey: Transporter, Caravan Camps, Gravship-/Expeditionslogik.

**Exit-Test:** Full-DLC-Testsave läuft ohne Nullreferenzen, und jede DLC-Ausnahme ist im Adapter-/Kompatibilitätsbericht vermerkt.

### Task 2.8 – Save-Migration (Phase-2.8 geliefert)

**2026-08-04 — First-Class-Domain-Extraktion:**
- `CharacterSetupState.MigrateIfNeeded()` als public, testbarer Schema-Bump-Eintrittspunkt (via `ISchemaMigratable`-Interface, `Foundation/Source/Save/`).
- `Tests/CharacterSetupStateSchemaBumpTests.RunAll()` — 6 T1–T6-Assertions: v0→Current, Idempotenz, Records-Preservation, Null-Normalisierung, Applied-Flag, ScribeRoundTrip.
- `ScribeRoundTripHelper.RoundTrip<T>(IExposable)` — echter Scribe-Save→Load-PostLoadInit-Cycle via MemoryStream in Foundation/Tests/.
- `docs/falsification/survival__SaveMigration.md` (236 Z.) — standalone Falsifizierungsbericht mit 7-Sektion-Layout (Kontext, Vertrag/I1–I3, A–G, Reproduktion, Negativ-Test, Siehe auch).
- MigrationRegistry zentralisiert Save/Load-Migration (Clear am Cycle-Start, keine Cross-Session-Leaks).
- Schema-Scribe-Tag `"foundationSchemaVersion"` unberührt; `FoundationDashboard.cs`-Stale-Reference gefixt.

**Exit-Test:** `Tests/CharacterSetupStateSchemaBumpTests.RunAll()` produziert im Boot-Log `SchemaBump tests: 6 passed, 0 failed (expected=6)`. Standalone v0→v1 Save-Migration deterministisch abgedeckt; echter Runtime-Save-File-Roundtrip bleibt Live-Gate via `scripts/runtime_test.sh`.

## 6. Blindspots und Gegenmaßnahmen

| Blindspot | Gegenmaßnahme |
|---|---|
| Vanilla-Mood läuft parallel und widerspricht Needs | zentrale Einflussmatrix und sichtbare UI-Erklärung |
| Traits/Genes deaktivieren Bedürfnisse | Capability-/Hediff-Prüfung und Test-Pawns |
| XP wird durch Idle-Ticks gefarmt | XP nur bei validiertem Job-Output, mit Diminishing-Regel |
| Vanilla-Skills und neue XP konkurrieren | klare Rollen: Vanilla-Skill oder Adapter, niemals unklare Doppelboni |
| WorkGiver/Reservation-Lock | Job-/Reservation-Stresstest mit mehreren Pawns |
| Wissen schaltet nichts Reales frei | jede Freigabe braucht ein überprüfbares Handlungsergebnis |
| Game Over wird durch Outposts verhindert | ausschließlich kontrollierbare Pawns zählen |
| DLC startet eigene Progression | Adaptermatrix und Full-Profile-Test |
| Scenario-/Start-Spot-Änderungen in 1.6 | Szenario- und neue Map-Startlogik testen |

## 7. Gemeinsamer Interface-Vertrag

- Kanonischer Besitzer von Needs, XP, Forschung und Game Over ist dieses Paket.
- Andere Pakete liefern Arbeit-/Infrastrukturereignisse oder lesen `ProgressionSnapshot`.
- Capability- und Snapshot-Regeln stehen verbindlich in `../../docs/INTERFACE_CONTRACT.md`.

## 8. Kompatibilitätsregeln

- Keine direkten Compile-Referenzen auf Pakete 1, 3, 4 oder 5.
- Cross-Paket-Integration nur über Foundation-Servicebus und Capability-/Snapshot-Vertrag; ohne Foundation bleibt nur der Standalone-Modus.
- Vanilla-Storyteller bleibt im Standalone aktiv.
- Eigene Needs werden nicht zusätzlich zu gleichnamigen Vanilla-Needs erzeugt.
- Patchen nur an klar dokumentierten Vanilla-Einstiegspunkten.
- Keine pauschalen `catch(Throwable)`-Blöcke.
- Jeder Zustand muss savebar und UI-diagnostizierbar sein.

## 9. Performance-Gate

Das Paket darf XP, Needs und Forschung nicht für jeden Pawn mehrfach pro Tick berechnen. XP- und Need-Updates werden an definierte Updateintervalle oder echte Zustandsänderungen gebunden; Job-/Reservation-Traces werden nur bei Diagnose aktiviert.

**Messbares Exit-Kriterium:** P1, P2 und P3 aus `../../docs/INTERFACE_CONTRACT.md` laufen zehn Ingame-Tage mit höchstens 2 ms durchschnittlicher und 5 ms 99.-Perzentil-Updatezeit pro 60-Tick-Update, höchstens 1 MiB Netto-Speicherwachstum pro Ingame-Tag, ohne unbounded Listen oder doppelte XP-Buchungen und mit höchstens 20 deduplizierten Diagnoseeinträgen pro Ingame-Tag; Need-/XP-Werte bleiben nach Kartenwechsel identisch.

## 10. Falsifizierungs-Gate

Vor Übergabe müssen die vier Berichte `../../docs/falsification/survival__Needs.md`, `../../docs/falsification/survival__WorkXp.md`, `../../docs/falsification/survival__Research.md` und `../../docs/falsification/survival__GameOver.md` jeweils `SURVIVED` erreichen.

Der Bericht `survival__Research` prüft den bestehenden Legacy-/Kompatibilitäts-Read-Model-Pfad. Der neue primäre Fortschrittspfad wird zusätzlich als `ExperienceUnlocks` über bestätigte Action-Completion, idempotente Erfahrung, Architektenfreigabe und Save/Load geprüft.

Jeder Bericht braucht die sieben Achsen A–G mit eigenem Test, Ergebnis und Beleg; Vanilla-Mood, Traits, Genes, Jobs, Reservations, DLCs und relevante Fremdmod-Need-/Jobpfade werden nach `../../docs/COMPATIBILITY_MATRIX.md` klassifiziert.

## 11. Exit-Kriterien für Übergabe an Paket 3

- Survival-RPG allein im echten Spielablauf belegt.
- Bedürfnisse, XP, Forschung und Game Over deterministisch getestet.
- Pawn-/Job-/Reservation-/DLC-Blindspots geprüft.
- Capability-IDs für spätere Scavenger-Arbeiten und Forschungsfreischaltungen eingefroren.
- Foundation-Adapter funktioniert optional.
- kein offener kritischer Save- oder Game-Over-Befund.
