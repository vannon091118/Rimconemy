# Roadmap 02 – Rimconemy Survival & Progression

> Eigenständige Paketaufgabe 2 von 5  
> Standalone zuerst, Full-Overhaul-Integration danach  
> Zielplattform: RimWorld 1.6 mit Royalty, Ideology, Biotech, Anomaly und Odyssey

## 1. Paketauftrag

Survival & Progression definiert den geplanten Survival-/Progressions-Loop. Im aktuellen Code sind Need-Mapping, Progression-Read-Model, Character-Setup-Logik, Sandbox-/Game-Over-Anker und Regression-Gates belegt; vollständige Need-, Job-/Output-XP-, Research- und Save/Load-Live-Schichten bleiben offen.

Im Full Overhaul wird es zur Progressionsdomäne für Bauschuttbau, Energie, Verteidigung, Mechadroids und Outposts. Diese Verbindungen laufen über Capability-IDs und Arbeitstyp-Verträge, nicht über direkte Compile-Abhängigkeiten auf andere Rimconemy-Assemblies.

## 2. Standalone-Ziel

Mit Vanilla-Ressourcen, Vanilla-Gebäuden und Vanilla-Fraktionen bietet das Paket:

```text
Nahrung/Sicherheit/Soziales
→ Arbeitsentscheidung
→ Erfahrung und Spezialisierung
→ Forschung
→ höhere Überlebensfähigkeit
→ Game Over bei Verlust aller kontrollierbaren Bewohner
```

Das Paket muss auch ohne Bauschutt, Economy, Outposts und Infizierte build-/bootstrap-fähig bleiben; ein vollständiger eigenständiger Survival-Loop ist ein offenes Live-Gate.

## 3. Full-Overhaul-Ziel

Im gemeinsamen Profil:

- Scavenger-Arbeiten melden standardisierte Arbeitstypen für XP.
- Bauschutt-, Farm-, Energie- und Verteidigungsarbeit nutzt dieselbe Progression.
- Forschung schaltet Wasser, Strom, Pfeilturm, Mechadroids, Credits und Outposts frei.
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

### Forschung

ResearchProjectDefs bilden die sichtbare Technikprogression. Forschung allein erzeugt keine Ressourcen; sie schaltet Regeln und Gebäude frei. Jede Freischaltung besitzt:

- eindeutige Capability-ID,
- Kosten-/Zeitdefinition,
- sichtbaren Zweck,
- mindestens einen Testpfad,
- Full-Overhaul-Adapter, falls sie ein späteres Paket betrifft.

## 5. Sequenzielle Arbeitsschritte

### Task 2.1 – Pawn-, Start- und Szenariovertrag

- eigenen Startcharakter über RimWorld-kompatiblen Szenario-/Pawn-Generator definieren,
- Aussehen und zulässige Individualisierung erhalten,
- Backstories, Traits, Xenotypen und DLC-Pawn-Regeln prüfen,
- keine ungewollten Startimmunitäten oder Startressourcen aus Biotech/Odyssey übernehmen,
- Game-Over-Anker definieren: direkt kontrollierbare Spielerbewohner.

**Blindspot-Gate:** Pawn-Generierung, Backstories, Traits, Xenotypen, Kinder/Alterung, Gene und Start-Spot dürfen nicht unkontrolliert Vanilla-Progression oder Bedürfnisse umgehen.

**Exit-Test:** Neue Kampagne startet reproduzierbar mit einem zulässigen individuellen Startcharakter und dokumentiertem Startinventar.

### Task 2.2 – Bedürfnis- und Zustandsmodell

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

### Task 2.4 – Forschungsbaum und Technikstufen

Stufen für Standalone:

```text
Tier 0 – Grundversorgung
Tier 1 – Stabilisierung
Tier 2 – Spezialisierung
Tier 3 – Automatisierung
```

Im Full Overhaul werden weitere Capabilities nur aktiviert, wenn die zuständigen Pakete vorhanden sind:

```text
Scavenger.Power
Scavenger.ArrowTurret
Automation.Mechadroids
Economy.Credits
Territory.Outposts
```

**Exit-Test:** Forschung ist nicht nur Text: Jede abgeschlossene Stufe verändert Gebäude, Arbeit, UI oder zugängliche Aktionen.

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

### Task 2.8 – Save-Migration

- Pawn-Datenpräfix und Save-Schema-Version festlegen,
- neue XP-/Need-Felder mit Defaults migrieren,
- inkompatible alte Zustände kontrolliert ablehnen,
- keine alten Vanilla-Werte still umdeuten,
- den verbindlichen Fall aus `../../docs/SAVE_CONTRACT.md` ausführen: Pawn ohne neue XP-/Need-Felder wird mit definierten Defaults migriert, ohne Game Over zu erfinden.

**Exit-Test:** Standalone-Save vor/nach Update und Full-Profile-Save mit fehlendem optionalem Paket werden geprüft; Migration erzeugt `Migrated`, `FrozenWithWarning` oder `LoadRejectedWithReason`, niemals stilles Löschen.

## 6. Blindspots und Gegenmaßnahmen

| Blindspot | Gegenmaßnahme |
|---|---|
| Vanilla-Mood läuft parallel und widerspricht Needs | zentrale Einflussmatrix und sichtbare UI-Erklärung |
| Traits/Genes deaktivieren Bedürfnisse | Capability-/Hediff-Prüfung und Test-Pawns |
| XP wird durch Idle-Ticks gefarmt | XP nur bei validiertem Job-Output, mit Diminishing-Regel |
| Vanilla-Skills und neue XP konkurrieren | klare Rollen: Vanilla-Skill oder Adapter, niemals unklare Doppelboni |
| WorkGiver/Reservation-Lock | Job-/Reservation-Stresstest mit mehreren Pawns |
| Forschung schaltet nichts Reales frei | jede Forschung braucht überprüfbaren Output |
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

Vor Übergabe müssen die vier Berichte `../../docs/FALSIFICATION_REPORTS/rimconemy.survivalprogression__Needs.md`, `../../docs/FALSIFICATION_REPORTS/rimconemy.survivalprogression__WorkXp.md`, `../../docs/FALSIFICATION_REPORTS/rimconemy.survivalprogression__Research.md` und `../../docs/FALSIFICATION_REPORTS/rimconemy.survivalprogression__GameOver.md` jeweils `SURVIVED` erreichen. Jeder Bericht braucht die sieben Achsen A–G mit eigenem Test, Ergebnis und Beleg; Vanilla-Mood, Traits, Genes, Jobs, Reservations, DLCs und relevante Fremdmod-Need-/Jobpfade werden nach `../../docs/COMPATIBILITY_MATRIX.md` klassifiziert.

## 11. Exit-Kriterien für Übergabe an Paket 3

- Survival-RPG allein im echten Spielablauf belegt.
- Bedürfnisse, XP, Forschung und Game Over deterministisch getestet.
- Pawn-/Job-/Reservation-/DLC-Blindspots geprüft.
- Capability-IDs für spätere Scavenger-Arbeiten und Forschungsfreischaltungen eingefroren.
- Foundation-Adapter funktioniert optional.
- kein offener kritischer Save- oder Game-Over-Befund.
