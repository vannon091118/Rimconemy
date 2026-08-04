# Roadmap 05 – Rimconemy Infected & Automation

> Eigenständige Paketaufgabe 5 von 5  
> Standalone zuerst, Full-Overhaul-Integration danach  
> Zielplattform: RimWorld 1.6 mit Royalty, Ideology, Biotech, Anomaly und Odyssey

## 1. Paketauftrag

Infected & Automation definiert die geplante Bedrohungs- und Automationskampagne. Im aktuellen Code sind deterministischer Story-Layer, StoryState, Storage-Bridge, Incident-/Letter-Pfad und Regression-Gates belegt; vollständiger Infizierten-Spawn, eigener Storyteller, World-Map-Raids und Mechadroid-Gameplay bleiben offen.

Im Full Overhaul soll es die vorhandenen Adapter mit den noch offenen Gameplay-Schichten verbinden:

```text
Farmen/Bewohner/Produktion/Verteidigung
→ Bedrohungsdruck
→ sichtbarer Raid
→ Verteidigungs- oder Automationsentscheidung
→ Ressourcenverbrauch/Schaden
→ erneute Planung
```

Es erzeugt weder eigene Credits, eigene Outposts noch eigene physische Ressourcen, wenn Economy/Territory oder Scavenger fehlen. Es konsumiert die Verträge der vorherigen Pakete.

## 2. Standalone-Ziel

Mit Vanilla-Ressourcen, lokaler Energieintegration, Vanilla-Handel und lokalen Zielen bietet das Paket:

- Infizierte als erkennbare Gegnerrolle,
- einen Setting-Director-/Incident-Modus als aktuelle Runtime-Basis,
- Bedrohungsdruck mit code-seitig definierter Snapshot-Grundlage,
- einen aktuell letter-basierten Incident-Pfad; echte lokale und World-Map-Raids bleiben geplant,
- Mechadroid-Datenmodelle als vorbereitete Domäne; echte Einheiten-/Auftragsmechanik bleibt geplant,
- Silber als technisches Upgrade-Material im eigenen Automation-Modus,
- lokale Automationsaufgaben,
- manuelle und automatisierte Raidentscheidungen.

Ohne Economy werden Vanilla-Fraktionen/Handel verwendet. Ohne Territory oder Scavenger bleiben die jeweiligen Integrationen capability-gated bzw. im deterministischen Fallback. Ein eigenständiger lokaler Mechadroid-/Raid-Loop ist noch nicht als `LIVE` belegt; das Full Profile wird nur bei tatsächlich registrierten Paketen erkannt.

## 3. Full-Overhaul-Ziel

Im Full Overhaul:

- Farmfläche, Bewohner, Generatoren, Produktionsaktivität und Verteidigung werden aus den bestehenden Scavenger-Snapshots gelesen.
- Sicherheit aus Paket 2 verarbeitet Bedrohungs- und Verbindungszustände.
- Economy/Territory liefert Outposts, Proxies, Fraktionen und Raidziele.
- Infizierte können Hauptbasis, Proxies und Outposts bedrohen.
- Mechadroids können Farmen, Energie, Wartung, Proxies und Outposts unterstützen.
- Silber bleibt physisches Upgrade-Material; Credits bleiben Wallet-Daten.
- letzter direkt kontrollierbarer Spielerbewohner bedeutet Game Over, unabhängig von Mechadroids/Outposts.

## 4. Ein einziges Bedrohungsmodell

### Infizierten-Druck

Der Druck besitzt sichtbare Komponenten:

```text
Farmaktivität
+ Bewohnerzahl
+ Produktionsaktivität
+ Generator-/Energieaktivität
+ Verteidigungsaktivität
+ vergangene Kämpfe/Infektionsereignisse
+ regionale/territoriale Faktoren
```

Jeder Faktor wird aus einer bestehenden Snapshotquelle gelesen. Paket 5 darf keine zweite Farm-, Bewohner- oder Produktionszählung erzeugen.

### Druck ist nicht gleich Raid

Trenne:

```text
Druckwert       → langfristige Sichtbarkeit/Bedrohung
Raid-Auslösung  → Storyteller-/Incident-Entscheidung
Raid-Stärke     → konkrete Gegnerparameter
Raid-Auflösung  → Kampf-/Verwaltungsresultat
```

Jeder Übergang muss im UI und Eventlog nachvollziehbar sein.

### Andere Fraktionen

Survivor-Fraktionsinteresse aus Economy/Territory ist nicht automatisch Infizierten-Druck. Es bleibt eine getrennte Bedrohungs-/Diplomatieachse.

## 5. Mechadroid-Modell

Mechadroids sind eigene Einheiten und nicht automatisch Vanilla-Mechanoids.

Jeder Mechadroid besitzt:

- stabile Einheiten-ID,
- Typ/Modul,
- Energie-/Wartungsbedarf,
- Auftrag,
- Standort,
- Besitzer,
- Zustand: `Idle`, `Assigned`, `Blocked`, `Damaged`, `Offline`, `Destroyed`,
- Ressourcen-/Upgradehistorie.

Silber wird für physische Upgrades verwendet. Credits werden nicht als Upgrade-Item missbraucht.

### Automationsgrenzen

Automation darf nicht bedeuten, dass alle Pawn-Jobs unsichtbar erledigt werden. Jeder Auftrag zeigt:

- Ziel,
- Fortschritt,
- Input,
- Output,
- Energieverbrauch,
- Wartung,
- Blockadegrund,
- letzte Aktion.

Outpost-Bevölkerung bleibt abstrakt; Mechadroids zählen nicht als kontrollierbare Spielerbewohner und verhindern kein Game Over.

## 6. Sequenzielle Arbeitsschritte

### Task 5.1 – Gegner- und Infektionsdomäne

- Infizierten-Defs/Fraktionen/Archetypen definieren.
- Unterschied zwischen Infizierten, Vanilla-Mechanoids, Tieren und Survivor-Fraktionen festlegen.
- Infektions-/Hediff-/Scaria-/Anomaly-Wechselwirkungen prüfen.
- eigene Gegnerrolle nicht nur durch Umbenennen einer Vanilla-Fraktion simulieren.

**Exit-Test:** Infizierte sind eindeutig identifizierbar, haben einen eigenen Spawn-/Raidpfad und werden im UI getrennt angezeigt.

### Task 5.2 – Bedrohungsaggregator

- Snapshotquellen aus Scavenger, Survival und Territory registrieren.
- Druckberechnung deterministisch machen.
- Beiträge, Trend und Prognose berechnen.
- Mindestdruck, Ruhephasen, Eskalationsstufen und Obergrenzen definieren.
- keine exponentielle Eskalation durch eine ungebremste positive Feedback-Schleife.

**Exit-Test:** gleiche Welt-/Koloniezustände ergeben denselben Druck; jede Änderung nennt den veränderten Faktor.

### Task 5.3 – Storyteller und Incidents

- eigener Storyteller für Infizierten-Ereignisse.
- Incident-Intervalle und Eskalationsstufen definieren.
- Vanilla-Raidpfade, Wealth-Raidpfade und DLC-Ereignisse prüfen.
- unpassende parallele Vanilla-Raids abschalten, integrieren oder im UI klar als separate Bedrohung ausweisen.
- keine doppelte Raidplanung durch zwei Storyteller.

**Exit-Test:** ein Druckanstieg erzeugt höchstens den vorgesehenen Incident und wird einmal protokolliert.

### Task 5.4 – Lokale Infizierten-Raids

- Spawn/Ankunft/Angriff/Rückzug definieren.
- Zielprioritäten: Farm, Strom, Proxy, Bewohner, Lager oder Hauptbasis.
- Verteidigungsanlagen und Stromabhängigkeit einbeziehen.
- Schäden, Infektion, Beute und Wiederherstellung speichern.
- Game-Over-Regel nur über kontrollierbare Bewohner auswerten.

**Exit-Test:** lokale Raidentscheidung zeigt Ursache, Stärke, Ziel und Konsequenz; Überleben und Scheitern sind reproduzierbar prüfbar.

### Task 5.5 – World-Map-Raids

- Raidobjekt mit stabiler ID erstellen.
- Symbol, Nummer, Einheitenanzahl, Fraktion, Ziel, Pfad, Richtung und ETA zeigen.
- Outpost-/Proxy-Ziele aus Economy/Territory erkennen.
- Verbindungsausfall und Raid nicht zu demselben Zustand vermischen.
- Ankunft/Auflösung idempotent speichern.

**Exit-Test:** World-Map-Raid überlebt Kartenwechsel und Save/Load; UI bleibt konsistent.

### Task 5.6 – Mechadroid-Grundsystem

- eigene Mechadroid-Defs und Einheitenzustände.
- Herstellung, Aktivierung, Auftrag, Wartung, Reparatur und Offline-Zustand.
- Silberkosten für Upgradepfade.
- Vanilla-Mechanoid-/Mechanitor-System separat behandeln.
- keine implizite Rettung des Game Over.

**Exit-Test:** ein Mechadroid kann lokal eine definierte Aufgabe übernehmen, benötigt Input/Energie/Wartung und fällt sichtbar aus, wenn etwas fehlt.

### Task 5.7 – Automation-Aufträge

MVP-Aufträge:

- Farmarbeit,
- Ressourcensammlung,
- Generator-/Energie-Wartung,
- Reparatur,
- Turm-/Verteidigungsbedienung.

Später:

- Proxywartung,
- Outpostbetrieb,
- Expedition-/Raidunterstützung.

Jeder Auftrag muss mit Vanilla-Reservations-/Job-Logik kollisionsarm sein. Für abstrakte Outpost-Aufträge keine lokalen Pawns spawnen, wenn das nicht ausdrücklich erforderlich ist.

**Exit-Test:** Auftrag besitzt Statusmaschine und Blockadegrund; keine stille Endlosschleife.

### Task 5.8 – Manuelle und automatisierte Spieler-Raids

#### Manuell

- echte kontrollierbare Pawns/Expedition,
- Zielkarte/Spielerentscheidungen,
- Verletzung, Verlust und Rückkehr nachvollziehbar.

#### Automatisiert

- eingesetzte Einheiten/Ressourcen,
- Kosten,
- Risiko,
- mögliche Beute,
- Schäden,
- Rückzugs-/Abbruchregel,
- Auflösungslog.

Die Prognose ist sichtbar; automatische Raids dürfen nicht als sichere Gratisproduktion wirken.

### Task 5.9 – Hauptstädte und Endgame

Erst nach stabilem MVP:

- große befestigte Survivor-Städte,
- einzigartige Technik-/Ressourcenbelohnungen,
- manuelle Endgame-Raids,
- automatisierte Endgame-Raids,
- Infizierten-Hochstufen,
- Anomaly-/Odyssey-Endgame-Adapter.

Hauptstädte sind nicht normale Outposts und nicht nur größere Zahlenwerte.

### Task 5.10 – Foundation-/Paket-Integrationen

- Foundation: Bedrohungs-/Raid-/Mechadroid-Snapshots.
- Survival: Sicherheit, XP, Forschung, Game Over.
- Scavenger: Farmen, Strom, Pfeilturm, physische Ressourcen.
- Economy/Territory: Credits, Outposts, Proxies, Fraktionen und World-Map-Ziele.
- fehlende Pakete liefern dokumentierte Standalone-Fallbacks, keine Phantomdaten.

### Task 5.11 – DLC- und Vanilla-Kompatibilität

Prüfe:

- Royalty: Psycasts/Titel gegen Bedrohung und Raidkontrolle,
- Ideology: Rollen/Rituale/Mood-/Social-Effekte,
- Biotech: Vanilla-Mechanoids, Mechanitors, Gene, Pollution, Kinder/Alterung,
- Anomaly: Entities, Pit Gates, Hediffs und alternative Ereignisse,
- Odyssey: Gravships, Transporter, Caravan Camps, Weltkartenpfade und Reiseziele.

Besonders kritisch:

- Vanilla-Wealth-Raids,
- Scaria-/Infektionsregeln,
- Anomaly-Events,
- automatische DLC-Rettungs-/Transportmechaniken,
- parallele Storyteller.

### Task 5.12 – Save, Performance und Determinismus

- den verbindlichen Save-Fall aus `../../docs/SAVE_CONTRACT.md` ausführen: aktiver Raid/Mechadroid-Auftrag ohne Status-ID wird eingefroren oder kontrolliert abgelehnt und niemals doppelt aufgelöst.
- Infektionsdruck und Raidstatus versionieren.
- Mechadroids und Aufträge mit stabilen IDs speichern.
- Weltkarten-Simulation aggregiert statt Pawn-Tick-Simulation für jeden Outpost.
- Raid-Auflösung deterministisch oder mit sichtbarem Seed.
- Performance bei vielen Outposts/Mechadroids messen.
- Save-/Load-, Kartenwechsel- und Crash-Recovery testen.
- **Phase-2.8 (2026-08-04):** `StoryState` implementiert `ISchemaMigratable` (Foundation/Source/Save/) — Schema-Version via `Scribe_Values.Look`, Migration via `this.RunMigration()`. Private `MigrateSchema(int)` Backend gelöscht, Schritte deklarativ in `Steps`-Liste. `Tests/StoryStateSchemaBumpTests` mit T1–T6-Assertions (v0→v1 Idempotenz).

## 7. Blindspots und Gegenmaßnahmen

| Blindspot | Gegenmaßnahme |
|---|---|
| Infizierte nur umbenannte Vanilla-Raids | eigene Fraktion/Spawn-/Incidentdomäne |
| Druck und Raid sind derselbe Wert | getrennte Datenmodelle und UI-Schritte |
| Vanilla-Wealth-Raids laufen parallel | Storyteller-/Incident-Inventur und klare Strategie |
| Verteidigung erzeugt Todes-Spirale | Druckbeitrag flacht ab; Feuerkraft und Druck getrennt balancieren |
| Mechadroids ersetzen alle Pawns | begrenzte Aufträge, Wartung, Energie und Slots |
| Mechadroids retten Game Over | nur kontrollierbare Pawns zählen |
| World-Map-Raid ist unsichtbar | Symbol/Anzahl/Pfad/ETA/Prognose verpflichtend |
| Auto-Raid ist Gratis-Output | Input, Risiko, Verlust und Ergebnislog |
| Anomaly/Biotech erzeugen parallele Gegnerlogik | Adapter oder bewusstes Deaktivieren |
| Scaria/Hediffs umgehen Infektionsmodell | gemeinsame Zustandsmatrix |
| Jobs/Reservations hängen | Statusmaschine, Reservierungs-Trace, Timeout/Abbruch |
| Outpost-Ticks skalieren schlecht | aggregierte Updates und Performance-Gates |
| Raid-Auflösung doppelt nach Save/Load | idempotente Zustandsübergänge |
| eigene Mechanoids werden Vanilla-Feinde | eigene Def-/Faction-/AI-Grenze |

## 8. Gemeinsamer Interface-Vertrag

- Kanonischer Besitzer von Infizierten-Druck, Infizierten-Raids, Mechadroids und Automation ist dieses Paket.
- Andere Pakete liefern Snapshots; dieses Paket darf keine Farm-, Ressourcen- oder Outpost-Bestände ein zweites Mal berechnen.
- `ThreatSnapshot` und Raid-/Automation-Capabilities folgen `../../docs/INTERFACE_CONTRACT.md`.

## 9. Kompatibilitätsregeln

- Keine direkte Compile-Abhängigkeit auf Pakete 1–4.
- Foundation-/Survival-/Scavenger-/Economy-Integrationen laufen im Full Profile über den Foundation-Servicebus und versionierte Capabilities; ohne Foundation bleibt der Standalone-Modus.
- Ein einziger Bedrohungsaggregator im Full Profile.
- Ein einziger Infizierten-Storyteller für die eigene Infizierten-Rolle.
- Harmony nur für notwendige Vanilla-Hooks.
- native Defs/PatchOperations bevorzugen.
- keine unbeschränkten globalen Pawn-Ticks für Outpost-/Automation-Simulation.
- jeder automatisierte Prozess meldet Zustand und Grund.
- jeder relevante Wert ist im UI und Eventlog nachvollziehbar.

## 10. Performance-Gate

Bedrohung, World-Map-Raids und Automation werden aggregiert aktualisiert. Outposts und Mechadroids erhalten keine unbeschränkten permanenten Pawn-Ticks. Raid-Auflösung ist idempotent und deterministisch oder besitzt einen sichtbaren Seed.

**Messbares Exit-Kriterium:** P1, P2 und P3 aus `../../docs/INTERFACE_CONTRACT.md` laufen zehn Ingame-Tage mit höchstens 2 ms durchschnittlicher und 5 ms 99.-Perzentil-Updatezeit pro 60-Tick-Update, höchstens 1 MiB Netto-Speicherwachstum pro Ingame-Tag, ohne doppelte Raids und mit höchstens 20 deduplizierten Diagnoseeinträgen pro Ingame-Tag; Save/Load darf keine unterschiedlichen Raid- oder Automationsergebnisse erzeugen.

## 11. Falsifizierungs-Gate

Vor dem Full-Overhaul-Review müssen die fünf Berichte `../../docs/FALSIFICATION_REPORTS/rimconemy.infectedautomation__ThreatPressure.md`, `../../docs/FALSIFICATION_REPORTS/rimconemy.infectedautomation__InfectedRaid.md`, `../../docs/FALSIFICATION_REPORTS/rimconemy.infectedautomation__MechadroidJob.md`, `../../docs/FALSIFICATION_REPORTS/rimconemy.infectedautomation__ManualRaid.md` und `../../docs/FALSIFICATION_REPORTS/rimconemy.infectedautomation__AutoResolve.md` jeweils `SURVIVED` erreichen. Jeder Bericht braucht A–G mit eigenem Test, Ergebnis und Beleg; Vanilla-Wealth-Raids, Storyteller, Biotech-Mechanoids, Anomaly, Combat Extended und Vehicle-/World-Map-Fälle werden nach `../../docs/COMPATIBILITY_MATRIX.md` klassifiziert.

## 12. Exit-Kriterien für das Full-Overhaul-Review

- Standalone-Infizierten-/Automation-Loop ist im echten Spielablauf belegt.
- Infizierten-Druck greift auf bestehende Snapshotquellen zurück.
- lokale und World-Map-Raids sind sichtbar und speicherbar.
- Mechadroids funktionieren lokal und mit Outposts, ohne Game Over zu umgehen.
- manuelle und automatisierte Raids sind klar getrennt.
- Vanilla-/DLC-Raidkonflikte sind entschieden und getestet.
- Performance- und Determinismus-Gates bestanden.
- alle vier vorherigen Pakete bleiben einzeln spielbar.
