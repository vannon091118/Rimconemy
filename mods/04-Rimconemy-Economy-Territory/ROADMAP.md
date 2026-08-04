# Roadmap 04 – Rimconemy Economy & Territory

> Eigenständige Paketaufgabe 4 von 5  
> Standalone zuerst, Full-Overhaul-Integration danach  
> Zielplattform: RimWorld 1.6 mit Royalty, Ideology, Biotech, Anomaly und Odyssey

## 1. Paketauftrag

Economy & Territory erweitert RimWorld von der lokalen Kolonie zu einer nachvollziehbaren Weltkarten-Wirtschaft. Es trennt vier Dinge, die nicht vermischt werden dürfen:

```text
physische Ware
+ digitale Wallet
+ Transport/Logistik
+ territoriale Verbindung
```

Allein definiert das Paket regionale Märkte, Credits, Fraktionen, Outposts und Gebietskontrolle mit Vanilla-Waren. Im aktuellen Code sind Wallet-, Market- und Outpost-State sowie Persistence-/Regression-Gates belegt; vollständige physische Transfers, WorldObject-/Proxy-Graph und Weltkartenlogistik bleiben offen.

## 2. Standalone-Ziel

Mit Vanilla-Waren, Vanilla-Fraktionen und dokumentierten Vanilla-Bedrohungen bietet das Paket:

```text
Ware anbieten/nachfragen
→ lokalen Preis beobachten
→ Credits im Wallet verwalten
→ in einen Outpost investieren
→ Proxy-Verbindung sichern
→ Produktions- gegen Verteidigungsanteil abwägen
→ Weltkarten-Risiko verwalten
```

Das Paket ist ohne Scavenger, Survival, Foundation oder Infected build-/bootstrap-fähig und erzeugt dabei keine Phantomressourcen, Phantom-Pawns oder Phantom-Bedrohungen. Ein vollständiger eigenständiger Wirtschafts-/Territory-Loop bleibt ein offenes Live-Gate.

## 3. Full-Overhaul-Ziel

Im gemeinsamen Profil:

- Bauschutt wird als physische Ware regional gehandelt.
- Credits bleiben ausschließlich Wallet-Daten.
- Nahrung, Brennstoffe, Wasser und Erze verwenden die Defs aus Paket 3.
- Forschung aus Paket 2 schaltet Märkte, Outposts, Proxies und Automationsstufen frei.
- Foundation zeigt Markt-, Wallet-, Verbindungs- und Transaktionssnapshots.
- Paket 5 liefert Infizierten-Raids und bedrohte Verbindungspunkte.
- Outposts können später Mechadroids aus Paket 5 einsetzen.

## 4. Geschlossenes Wirtschaftsmodell

### Physische Waren

Eine Ware besitzt:

- stabile Def-/Ware-ID,
- Menge,
- physischen Ort,
- Eigentümer/Marktzuordnung,
- Angebot/Nachfragewirkung,
- Ein-/Ausfuhrstatus,
- sichtbaren Lager- und Transportzustand.

### Credits

Credits besitzen:

- Wallet-ID,
- Kontostand,
- Eigentümer,
- Transaktionshistorie,
- zulässige Quellen und Ausgaben.

Credits sind kein `Thing`, kein Stack, kein Pawn-Inventar und keine Silber-Umbenennung.

### Markt

Ein lokaler Markt braucht mindestens:

```text
Ware
+ lokaler Bestand
+ lokales Angebot
+ lokale Nachfrage
+ Preisformel
+ Preisverlauf
+ Transaktionslog
```

Preise müssen deterministisch aus dem gespeicherten Marktstatus berechnet werden. Zufall darf nur ausdrücklich als Ereignis-/Marktrauschen modelliert und im UI erklärt werden.

### Logistik

Einnahmen und Waren sind nicht automatisch global verfügbar:

- aktive Verbindung: Netzwerk kann übertragen,
- unterbrochene Verbindung: Produktion/Einnahmen liegen lokal oder sind gesperrt,
- Ruine: nicht gesicherte lokale Bestände werden verloren, geplündert oder als Ruinenbeute markiert.

Die konkrete Regel wird als Datenstatus gespeichert, nicht nur im UI behauptet.

## 5. Outpost-Modell

### Gründung

Keine exponentiell steigenden Gründungskosten. Jeder Outpost besitzt stattdessen eine konkrete Investition aus:

- Credits,
- Bauschutt oder Vanilla-Baugut im Standalone-Modus,
- Nahrung,
- Technik-/Upgrade-Material,
- Bauzeit,
- Proxy-/Verbindungsinfrastruktur.

Mehr Outposts erhöhen nicht künstlich den Preis, sondern binden real mehr:

```text
Investition
+ Verteidigung
+ Wartung
+ Proxy-Risiko
+ Managementaufwand
```

### Produktion

Jeder aktive Outpost erzeugt immer:

- Credits,
- Bauressourcen.

Module und Standort können zusätzlich erzeugen:

- Nahrung,
- Erze,
- Upgrade-Material,
- Mechadroid-Input.

Der Ertrag muss in Brutto, Eigenschutz, Wartung, Netzwerk-Kosten und Netto getrennt angezeigt werden.

### Verteidigungsverteilung

Der Spieler steuert nur die Verteilung:

```text
Produktion: 70 %
Verteidigung: 30 %
```

Diese Einstellung muss dieselbe Quelle für folgende Werte sein:

- Nettoertrag,
- Verteidigungsstärke,
- Raid-Prognose,
- Ressourcenbindung.

Kein UI-Regler darf nur optisch wirken.

## 6. Proxy- und Territoriumsmodell

### Verbindungsgraph

```text
Hauptbasis → Proxy → Proxy → Outpost
```

Ein gültiger Graph muss:

- an die Hauptbasis angeschlossen sein,
- zusammenhängend sein,
- beschädigte/zerstörte Knoten kennen,
- Reichweite/Route/ETA berechnen,
- getrennte Teilnetze erkennen,
- beim Save/Load dieselben IDs rekonstruieren.

### Drei-Tage-Frist

Bei Verbindungsverlust:

```text
Tag 0: Verbindung getrennt, Countdown startet
Tag 1–2: Reparatur-/Umleitungsfenster
nach 3 Tagen: Outpost wird sofort zur Ruine
```

Die Frist ist exakt und sichtbar. Nach Ablauf:

- Produktion endet,
- Credit-Erzeugung endet,
- Outpost wird Ruine,
- nicht gesicherte lokale Daten werden nach definierter Beuteregel behandelt,
- späterer Wiederaufbau/Rückeroberung bleibt möglich,
- kein automatisches Game Over.

## 7. Sequenzielle Arbeitsschritte

### Task 4.1 – Wallet- und Transaktionsmodell

- Spieler-, Fraktions- und Outpost-Wallets definieren.
- Wallet-ID und Eigentümer stabil speichern.
- Credits erzeugen, buchen, sperren und ausgeben.
- jede Buchung mit Zeit, Quelle, Ziel, Betrag und Grund protokollieren.
- negative Kontostände oder Rundungsfehler explizit verhindern.

**Exit-Test:** Kauf, Einnahme, Rückbuchung und Verbindungssperre verändern Wallets korrekt und nachvollziehbar.

### Task 4.2 – Waren- und Marktmodell

- lokale Marktinstanz pro Region definieren.
- Angebot/Nachfrage und Preisformel definieren.
- Mindest-/Höchstpreise nur als Balancegrenzen verwenden, nicht als versteckte Wirtschaft.
- physische Ware und Wallet-Buchung atomar verknüpfen.
- Transaktion bei Fehler vollständig zurückrollen.

**Blindspot-Gate:** Markt darf keine Ware erzeugen, wenn kein Bestand/Produzent/Vertrag existiert.

**Exit-Test:** gleiche Marktinputs produzieren denselben Preis; Handel verändert Bestand, Wallet und Log gemeinsam.

### Task 4.3 – Vanilla-Fraktionen und Survivor-Fraktionen

- vorhandene Vanilla-Fraktionen im Standalone verwenden.
- Survivor-Fraktionen im Full Profile definieren.
- Fraktions-Wallet und Marktdaten getrennt halten.
- Goodwill, Handelsangebot, Nachfrage und Raidinteresse nicht in einen einzigen Wert mischen.
- Royalty-/Ideology-Fraktionen auf Markt- und Territorialregeln prüfen.

**Exit-Test:** zwei Fraktionen können mit unterschiedlichen Beständen unterschiedliche Preise und Angebote besitzen.

### Task 4.4 – Outpost-Gründung und Standort

- gültige Standorttypen und lokale Ressourcen bestimmen.
- konkrete Gründungskosten und Bauzeit festlegen.
- Standortpotenzial, Produktionsprofil und Risiko speichern.
- kein Outpost ohne Standort-ID oder Verbindungskontext.
- Outpost als Weltkartenobjekt/Domainzustand von einer lokalen Pawn-Kolonie trennen.

**Exit-Test:** ein Outpost wird gegründet, gespeichert, lädt wieder und erzeugt nachvollziehbare Bruttowerte.

### Task 4.5 – Produktion, Verbrauch und Verteidigung

- Credits- und Bauressourcen-Ertrag berechnen.
- automatische Verteidigungsbindung berechnen.
- Wartung und Proxy-Kosten abziehen.
- Brutto/gebunden/Netto persistieren oder deterministisch rekonstruieren.
- `Active`, `Blocked`, `Disconnected`, `Ruined` als echte Zustände definieren.

**Exit-Test:** Verteidigungsanteil erhöhen → Nettoertrag sinkt und Verteidigungsstärke steigt; beides erscheint im UI.

### Task 4.6 – Proxy-Graph und Drei-Tage-Countdown

- Graph erstellen, ändern, prüfen und speichern.
- Zerstörung eines Proxies propagiert Verbindungsstatus.
- Countdown in Weltzeit/Ticks eindeutig definieren.
- Umleitung oder Reparatur setzt Countdown korrekt zurück.
- Ablauf erstellt Ruine genau einmal.

**Exit-Test:** Proxy zerstören, Save laden, drei Tage verstreichen lassen; Ergebnis bleibt identisch und doppelte Ruinen/Erträge entstehen nicht.

### Task 4.7 – World-Map-Overlay und Raid-Verwaltung

Sichtbar:

- Symbol,
- Nummer,
- Fraktion,
- Einheitenanzahl,
- Ziel,
- Pfad,
- Richtung,
- ETA,
- Raidstärke,
- Outpost-Verteidigung,
- Prognose.

Im Standalone wird zunächst eine generische/Vanilla-Bedrohung verwendet. Paket 5 kann später den Infizierten-Provider registrieren.

**Exit-Test:** Ein Raidpfad bleibt nach Kartenwechsel und Save/Load sichtbar und besitzt eine erklärbare Ankunftszeit.

### Task 4.8 – Automatisierte Raids als Verwaltungsentscheidung

- Einsatzkräfte/Einheiten abstrahieren, ohne nicht vorhandene Mechadroids zu erfinden.
- Kosten, Ziel, Beute, Verluste und Rückkehrstatus vorab anzeigen.
- manuelle taktische Raids nicht mit Auto-Resolve vermischen.
- Ergebnis deterministisch oder mit offen sichtbarem Zufallsseed berechnen.

**Exit-Test:** Spieler kann vor Start Kosten, Risiko und erwartete Konsequenzen prüfen; Ergebnis wird protokolliert.

### Task 4.9 – Integrationen zu Paketen 1–3

- Foundation: Wallet-/Markt-/Outpost-Snapshots.
- Survival: Forschungsfreischaltungen, Arbeitserfahrung für Logistik.
- Scavenger: Bauschutt, Nahrung, Wasser, Brennstoffe, Erze als Ware.
- ohne diese Pakete bleiben Vanilla-Güter und Vanilla-Progression aktiv.

### Task 4.10 – DLC-/World-Map-/Save-Kompatibilität

Führe außerdem den verbindlichen Save-Fall aus `../../docs/SAVE_CONTRACT.md` aus: Outpost/Wallet ohne Owner- oder Route-ID wird entweder eindeutig migriert oder mit `LoadRejectedWithReason` abgelehnt; Credits und Waren werden nie still verworfen.

Prüfe besonders:

- Caravan Camps und temporäre Maps,
- Odyssey-Reise, Transporter und Gravship-Kontext,
- Anomaly-Pit-Gates und Kartenwechsel,
- Vanilla-Siedlungen, FactionBases und Quests,
- Weltkartenpfade und Bewegungsänderungen,
- WorldComponent-/WorldObject-Savezyklen,
- fehlende Weltkartenobjekte beim Laden,
- große Outpost-Anzahl und Tick-Kosten.

## 8. Blindspots und Gegenmaßnahmen

| Blindspot | Gegenmaßnahme |
|---|---|
| Credits werden doch als Item behandelt | Wallet-Domain ohne ThingDef und Inventarpfad |
| Ware teleportiert global | physischer Ort + Verbindungs-/Transportstatus |
| Preis ändert sich ohne Transaktion | deterministische Preisquelle und Log |
| Outpost produziert gratis | Brutto/Verteidigung/Wartung/Netto getrennt |
| Verteidigungsregler ist Kosmetik | gleicher Wert speist Ertrag und Raidprognose |
| Proxy-Verlust nur UI | Graph-Status beeinflusst Produktion/Wallet-Verfügbarkeit |
| 3 Tage nach Save/Load falsch | absolute Weltzeit statt lokaler Tick-Zähler |
| Ruine entsteht mehrfach | idempotenter Zustandsübergang |
| Vanilla-Siedlung wird heimlich Outpost | getrennte IDs und Objekttypen |
| Fraktionen besitzen keine Wirtschaft | Fraktionsbestand, Nachfrage und Wallet trennen |
| Auto-Raid ist Blackbox | Kosten-/Risiko-/Ergebnis-Snapshot vor Start |
| Odyssey/Caravan-Camp umgeht Gebiet | Adapter blockiert nicht verbundene Logistikpfade |
| Outpost-Ticks verursachen Late-Game-Lag | aggregierte Weltzeit-Updates, keine Pawn-Tick-Simulation |

## 9. Gemeinsamer Interface-Vertrag

- Kanonischer Besitzer von Wallets, Märkten, Transaktionen, Outposts, Proxies und Territory ist dieses Paket.
- Andere Pakete liefern Waren-/Bedrohungsinputs oder fordern validierte Commands an.
- `WalletSnapshot` und `TerritorySnapshot` werden gemäß `../../docs/INTERFACE_CONTRACT.md` veröffentlicht.

## 10. Kompatibilitätsregeln

- Keine direkte Compile-Abhängigkeit auf Pakete 1, 2, 3 oder 5.
- Foundation-/Survival-/Scavenger-/Infected-Integrationen laufen im Full Profile über den Foundation-Servicebus und versionierte Capabilities; ohne Foundation bleibt der Standalone-Modus.
- `WorldComponent`/`WorldObject`-Zustände besitzen stabile IDs und Save-Versionen.
- Wallet-Buchungen und Warenbewegungen müssen atomar oder rollbackfähig sein.
- Keine generischen `catch(Throwable)`-Blöcke um Wirtschaftsdaten.
- Native Weltkarten- und UI-Erweiterung bevorzugen; Harmony nur für notwendige Vanilla-Hooks.
- Keine steigenden Gründungskosten als künstliche Anti-Snowball-Regel.

## 11. Performance-Gate

Outposts, Märkte und Proxy-Graphen werden aggregiert aktualisiert. Es gibt keine Pawn-Simulation pro Outpost und keine doppelte Markt-/Produktionsermittlung durch UI und Simulation.

**Messbares Exit-Kriterium:** P1, P2 und P3 aus `../../docs/INTERFACE_CONTRACT.md` laufen zehn Ingame-Tage mit höchstens 2 ms durchschnittlicher und 5 ms 99.-Perzentil-Updatezeit pro 60-Tick-Update, höchstens 1 MiB Netto-Speicherwachstum pro Ingame-Tag, ohne doppelte Credits/Erträge und mit höchstens 20 deduplizierten Diagnoseeinträgen pro Ingame-Tag; Save/Load darf keine abweichenden Markt-, Wallet- oder Territory-Ergebnisse erzeugen.

## 12. Falsifizierungs-Gate

Vor Übergabe müssen die fünf Berichte `../../docs/FALSIFICATION_REPORTS/rimconemy.economyterritory__WalletCredits.md`, `../../docs/FALSIFICATION_REPORTS/rimconemy.economyterritory__Market.md`, `../../docs/FALSIFICATION_REPORTS/rimconemy.economyterritory__ReservePhysicalTransfer.md`, `../../docs/FALSIFICATION_REPORTS/rimconemy.economyterritory__OutpostProduction.md` und `../../docs/FALSIFICATION_REPORTS/rimconemy.economyterritory__TerritoryCountdown.md` jeweils `SURVIVED` erreichen. Jeder Bericht braucht A–G mit eigenem Test, Ergebnis und Beleg; Vanilla-WorldObjects, Caravan-/Vehicle-/Hospitality-/Empire-ähnliche Systeme und relevante Save-/Trade-/Load-Order-Konflikte werden nach `../../docs/COMPATIBILITY_MATRIX.md` klassifiziert.

## 13. Exit-Kriterien für Übergabe an Paket 5

- Standalone-Markt und Outpost-Loop sind im echten Spielablauf belegt.
- Credits sind vollständig von Items/Silber getrennt.
- Preis, Bestand, Wallet und Transaktion sind nachvollziehbar.
- Verteidigungsanteil beeinflusst Ertrag real.
- Proxy-Netzwerk bleibt verbunden, speicherbar und sichtbar.
- Drei-Tage-Verlust erzeugt genau eine Ruine.
- World-Map-Raids zeigen Symbol, Anzahl, Pfad und ETA.
- Economy-Quellen aus Paketen 2/3 sind über Verträge integrierbar.
- keine offenen kritischen Logistik-, Save- oder Performance-Blindspots.
