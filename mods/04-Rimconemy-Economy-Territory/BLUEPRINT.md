# Blueprint 04 – Rimconemy Economy & Territory

## API-Hinweis

Die genannten Trade-, WorldObject-, Caravan- und WorldComponent-Anker sind Planungsanker. Exakte 1.6-Semantik wird über `API-TRADE-01`, `API-WORLD-01` und die lokale Baseline bestätigt (Spike-/Baseline-Dokumente archiviert in `docs/archive-md-2026-08-04.tar.gz`).

## Ziel

Das Paket macht Wachstum zu einer sichtbaren wirtschaftlichen Entscheidung: physische Waren bewegen sich, Credits bleiben Wallet-Daten, Outposts brauchen Investition und Verteidigung, und Territorium ist eine reale Verbindung statt ein UI-Symbol.

## Standalone-Spielwert (Planungsziel; Live-Beleg offen)

Wallet-, Market- und Outpost-State sowie Persistence-/Regression-Gates sind im Code vorhanden. Physische Transfers, WorldObject-/Proxy-Graph und vollständige Weltkartenlogistik bleiben offen.

```text
Vanilla-Ware → lokaler Markt → Credits-Wallet → Outpost-Investition → Proxy-Risiko
```

Ohne Scavenger werden Vanilla-Güter verwendet; ohne Infected bleiben Vanilla-/generische Bedrohungen der Kontext.

## Vanilla-/DLC-Anker

| Bereich | Anker | Entscheidung | Spike |
|---|---|---|---|
| Wallet | Game-/World-Daten, kein Item | eigene persistente Wallet | Save-/Purge- und Migrationstest |
| Trade | Vanilla TradeSession/Trader/Faction-Pfade | UI/Handel adaptieren, nicht Markt doppeln | exakte 1.6-Trade-Signaturen |
| Preis | `MarketValue` existiert in Vanilla | Rimconemy-Preis parallel als lokaler Preis; kein globaler Patch im MVP | Wealth-/Quest-/Trader-Divergenz |
| Outpost | WorldObject/WorldComponent-Anker | eigene savebare Domäne; Site/CaravanCamp nicht ungeprüft übernehmen | Lifecycle-/GC-/Maptest |
| Caravan/Transport | Caravan/WorldMap/temporary maps | MVP abstrahierte Route mit sichtbarem Status oder klarer physischer Route | Odyssey-Transporter/Gravship |
| Territory | eigener Graph mit absoluten Ticks | MainBase/Proxy/Outpost/Ruin IDs | mobile MainBase später |
| DLC | Royalty/Ideology/Anomaly/Odyssey WorldMap/Questpfade | adapterweise koexistieren | Territory-Bypass prüfen |

## Artefaktziele (geplante Zielpfade, noch keine vorhandenen Belege)

| Task | Dateien/Artefakte | Test-IDs |
|---|---|---|
| E1 | `Source/Wallet/`, `Source/Market/`, `Tests/WalletAtomicity.md` | `NEW_GAME`, `SAVE_LOAD`, `DETERMINISM` |
| E2 | `Source/Transfers/`, `Source/Logistics/`, `Tests/PhysicalTransferRecovery.md` | `SAVE_LOAD`, `MAP_CHANGE`, `WORLD_STEP` |
| E3 | `Source/Outposts/`, `Source/Production/`, `Tests/OutpostGrossNet.md` | `NEW_GAME`, `WORLD_STEP`, `UI_REASON` |
| E4 | `Source/Territory/`, `Source/WorldObjects/`, `Tests/TerritoryCountdown.md` | `SAVE_LOAD`, `MAP_CHANGE`, `TEMP_MAP`, `DETERMINISM` |
| E5 | `Source/UI/`, `Tests/WorldMapDlcMatrix.md`, fünf Economy-Berichte | `UI_REASON`, `DLC_SCOPE`, `WORLD_STEP`, `TEMP_MAP` |

## Blockierende Architekturspikes vor Economy-Implementierung

Die Spike-IDs sind kanonisch in `docs/H1-api-def-gate.md` registriert (ehem. SPIKE_INDEX archiviert).

Die folgenden Punkte sind keine unverbindlichen Ideen. Sie blockieren den jeweiligen Task, bis ein lokaler Spike mit Beleg abgeschlossen oder bewusst als `BLOCKED` mit Nutzerentscheidung dokumentiert ist:

| Spike | Blockiert | Entscheidungskriterium |
|---|---|---|
| `API-TRADE-01` MarketValue/Trade-Semantik | E1/E5 | Rimconemy-Preis darf Vanilla-Trade, Questbelohnungen und Wealth nicht unmarkiert widersprechen |
| `API-WORLD-01` WorldObject-/Cleanup-/Save-Lifecycle | E3/E4 | Outpost darf nach Save/Load, Kartenwechsel und GC nicht verschwinden oder duplizieren |
| Wealth-/Outpost-Zählung | E3/E5 | Threat-/Wealth-Beitrag wird gemessen; `MarketValue=0` gilt nicht als Beweis |
| Transport-/Temporary-Map-Modell | E2/E5 | Route, unloaded Map, Caravan Camp und Verbindung besitzen eindeutige Zustände |

## Fünf Build-Tasks

### E1 – Wallet und Marktgrundlage

**Voraussetzung:** `API-TRADE-01` und die Wealth-/MarketValue-Entscheidung aus der Spike-Tabelle.

- Wallet-ID, Eigentümer, Balance, LockedBalance und Transaktions-ID speichern.
- Quellen/Senken und Rundungsregeln definieren.
- lokale Marktinstanz mit Bestand, Angebot, Nachfrage und deterministischer Preisformel anlegen.
- Credits niemals als `ThingDef` oder Silberalias implementieren.

**Gate:** Kauf/Einnahme/Rückbuchung ändern Ware, Wallet und Log atomar.

### E2 – Physische Waren und Transport

- ResourceId, Menge, Ort, Eigentümer, Reservation und Route speichern.
- Reserve/Execute/Cancel als getrennte Commands implementieren.
- keine Ware global teleportieren, solange Route/Verbindung nicht verfügbar ist.
- UI zeigt lokale Ware, Transport, ETA und Blockade.

**Gate:** unterbrochener Transfer dupliziert weder Ware noch Credits und überlebt Save/Load.

### E3 – Outpost und Produktion

**Voraussetzung:** `API-WORLD-01` und Wealth-/Outpost-Zählung sind als `READY` oder bewusst `BLOCKED` dokumentiert.

- Standort, Gründungskosten, Bauzeit, Brutto, Schutz, Wartung, Netzwerk und Netto modellieren.
- keine steigenden Gründungskosten; reale laufende Bindung ist der Anti-Snowball.
- Outpost-Zustände `Planned`, `Active`, `Blocked`, `Disconnected`, `Ruined`.
- `WorldObject`-Lifecycle gegen lokale 1.6-Assembly testen; SiteDef/CaravanCamp nur als Spike.

**Gate:** Produktionsverteilung verändert Ertrag und Verteidigung tatsächlich.

### E4 – Proxy-Graph und Kartenstatus

- MainBase → Proxy → Outpost als stabile ID-Graphstruktur speichern.
- alternative Route, Reichweite, ETA und Verbindung prüfen.
- bei Verlust absolute `DisconnectDeadlineTick` setzen.
- Reparatur/Umleitung beendet Countdown; nach drei Tagen genau eine Ruine.

**Gate:** Zerstörung, Save/Load, Kartenwechsel und Ablauf liefern dasselbe Ergebnis.

### E5 – WorldMap/UI/DLC-Integration

**Voraussetzung:** MarketValue-/Wealth-, WorldObject- und Transport-Spikes sind entschieden; ohne diese Belege bleibt die Paketübergabe geschlossen.

- Symbol, Nummer, Anzahl, Ziel, Pfad, Richtung, ETA und Prognose anzeigen.
- Survival-/Scavenger-Capabilities lesen; Infected-Raidprovider später einhängen.
- Caravan Camps, Transporter, Gravship, Anomaly-Pit-Gates, Quests und WorldObjects prüfen.
- MarketValue nicht pauschal auf null setzen; Vanilla Wealth-Raids separat klassifizieren.

**Exit:** Wallet, Market, Transfers, Outpost, Territory und Countdown bleiben bis zu A–G-Belegen `UNVERIFIED`.

## Schnittstellen

- besitzt Wallets, Märkte, Transaktionen, Transfers, Outposts, Proxies, Territory.
- liest Resource-/Power-/Progression-Snapshots.
- veröffentlicht Wallet-, Territory- und Outpost-Snapshots.
- Infected fordert Raidziele an, schreibt aber keine Markt-/Territorydaten direkt.

## UI-Minimum

Wallet/Bilanz, lokaler Bestand, Preis und Ursache, offene Transfers, Outpost-Brutto/Kosten/Netto, Verteidigungsregler, Graphstatus, Countdown und Ruinengrund.

## Save-/Performance-Gates

Transaktionshistorie begrenzen/aggregieren; Request-Deduplication purgen. Keine per-Pawn-Outpost-Simulation. Absolute Weltzeit statt lokaler Countdown-Zähler. Unloaded Maps besitzen `InactiveMap`/`CatchUpPending`-Status statt falscher Aktivwerte.

## Offene Spikes

- MarketValue-vs.-Rimconemy-Preis endgültig als parallel, adaptierend oder ersetzend festlegen.
- WealthWatcher-/WorldObject-Beitrag messen; `selectionRadius=0` ist kein bewiesener globaler Ausschluss.
- SiteDef und CaravanCamp nicht als dauerhafte Outpost-Abkürzung übernehmen, bevor Cleanup-/Save-Semantik belegt ist.
- Gravship als MobileMainBase erst nach statischem Graph.
