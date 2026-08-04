# Rimconemy Building Feature — Full Ausbau Design

> **Datum:** 2026-08-04  
> **Status:** Design freigegeben; Meilenstein A statisch/BOOT umgesetzt, interaktive A-Gates offen  
> **Scope:** Meilensteine A → B → C über alle fünf Rimconemy-Mods  
> **Zielplattform:** RimWorld 1.6.4566

## 1. Ziel und Beweisgrenze

Der Building-Ausbau soll nicht nur Defs oder Bootstrap-Marker liefern, sondern eine
prüfbare vertikale Gameplay-Kette:

```text
physische Ressource
→ Gebäude-/Baukosten
→ tatsächlicher Bau-/Reparatur-Output
→ Lager-/Power-Zustand
→ Outpost-/Automation-Nutzung
→ Threat-/Raid-Folge
```

„Fertig“ bedeutet pro Meilenstein:

- Code und Defs kompilieren/laden;
- ein frischer Runtime-Log enthält die erwarteten Marker;
- der konkrete Ingame-Ablauf ist reproduzierbar;
- Save/Load und Idempotenz sind für den betroffenen Zustand geprüft;
- kein geplanter Stub wird als fertige Mechanik bezeichnet.

Die Paketnummern bleiben Ownership-Grenzen. Ein Paket implementiert nur die Daten
und Commands, deren Owner es ist; andere Pakete lesen Snapshots oder verwenden
Capability-Gates.

## 2. Meilenstein A — Building-Core

### 2.1 Gameplay-Kette

```text
ConstructionDebris
  → Wall/Door aus Bauschutt
  → tatsächlicher Storage-Snapshot
  → Wasser-/Fuel-Input
  → Generator
  → PowerNet
  → powered Arrow Turret
```

Der Core muss mindestens abdecken:

- stabile `ConstructionDebris`-ThingDef;
- Wall-/Door-Kostenpfad über RimWorld-1.6-kompatible Def-/Patch-Mechanik;
- physische Lagerung und `StorageQuery`-Erfassung;
- Wasser-/Fuel-Inputs mit sichtbarem Blockadegrund;
- Generator mit Vanilla-PowerNet-Anbindung;
- Turmzustände `Active`, `Blocked`, `Offline`, `Damaged`;
- UI-Lesemodell für Bestand, Kosten, Power und Turmstatus.

Nicht ausreichend sind allein ein `ThingDef`, eine Logzeile oder ein UI-Regler.

### 2.2 Ownership der fünf Mods

#### 01 Foundation

Foundation besitzt keine Bau- oder Ressourcenmutation. Es liefert:

- Capability-/Profil-Gates für Building- und Storage-Leser;
- Diagnose-/Save-Metadaten für Building-Schema und Migration;
- gemeinsame UI-/Status-Tokens;
- zentrale Runtime-Marker für den Building-Core.

#### 02 Survival & Progression

Survival besitzt die Progressionsreaktion:

- stabiler Arbeitstyp `Building`;
- XP nur nach validiertem Bau-/Reparatur-Output;
- Idempotency-Key pro Output;
- keine XP aus Pawn-Ticks oder bloßer Jobannahme;
- UI-Read-Model für Bau-XP und Spezialisierung.

Die bestehende Character-/Need-Logik wird nicht durch Building-Code dupliziert.

#### 03 Scavenger Infrastructure

Scavenger ist Owner des physischen Building-Cores:

- `ConstructionDebris`, Wasser, Hanf und relevante Input-Defs;
- Wall-/Door-Baukostenpfad;
- Generator-/Power-/Turret-Defs;
- StorageQuery und PowerChainSnapshot;
- tatsächliche Zustandsprüfung für Fuel, Wasser und Power;
- Infrastruktur-Dashboard und Building-Regressionstests.

#### 04 Economy & Territory

Economy liest physische Building-Inputs und besitzt wirtschaftliche Folgen:

- keine Credits als Ersatz für physische Materialien;
- Reserve/Commit/Cancel für physische Bau-/Upgrade-Transfers;
- Outpost-Investition aus validierten physischen Inputs;
- Wallet-Buchung nur für explizite Gebühren/Services;
- deterministische Markt-/Outpost-Snapshots.

#### 05 Infected & Automation

Infected liest Building-/Power-/Production-Snapshots und besitzt die Bedrohungsreaktion:

- Building-/Power-Faktoren im ThreatSnapshot;
- deterministische Bedrohungsbeiträge;
- Mechadroid-Auftragsdefinitionen für Bau, Wartung, Generator und Verteidigung;
- zunächst keine echte Raid-Auflösung im Core-Gate;
- späterer Übergang von Letter-only zu echtem Spawn nur in Meilenstein C.

## 3. Meilenstein B — Outposts und Automation

Nach bestandenem Meilenstein A werden Building-Inputs in Outposts und
Mechadroid-Aufträge integriert.

### 3.1 Outpost-Zustände

Die bestehende Outpost-State-Machine bleibt kanonisch:

```text
Planned → Active → Blocked → Disconnected → Ruined
                 ↘ Active (nach gültiger Reparatur)
```

Jeder Übergang braucht:

- stabile Outpost-ID;
- absolute Welt-Ticks;
- Grund und Eintritts-Tick;
- idempotente Zustandsänderung;
- Save-/Load-Rekonstruktion ohne Doppelproduktion.

Building-Inputs werden als physische Investition/Produktion gelesen. Credits,
Waren und Bauzustände bleiben getrennt.

### 3.2 Physischer Transfer

Ein Building-/Outpost-Transfer verwendet drei getrennte Operationen:

```text
ReservePhysicalTransfer
→ ExecutePhysicalTransfer
→ CancelPhysicalTransfer
```

Regeln:

- Reserve verändert den physischen Bestand noch nicht dauerhaft;
- Execute verbraucht exakt die reservierte Menge;
- Cancel gibt die Reservierung frei;
- Replay desselben Request-/Idempotency-Keys erzeugt keinen zweiten Verbrauch;
- fehlendes Paket oder fehlender Bestand erzeugt keinen Phantom-Transfer.

### 3.3 Mechadroid-Aufträge

Jeder Auftrag besitzt:

- `JobId`;
- `UnitId`;
- Ziel-/Gebäude-ID;
- Input-/Output-Referenzen;
- Energie-/Wartungsbedarf;
- Status `Queued`, `Assigned`, `Blocked`, `Completed`, `Cancelled`, `Failed`;
- `LastActionTick`;
- Idempotency-Key;
- sichtbaren Blockadegrund.

Der Auftrag darf keine lokalen Pawns oder Ressourcen erzeugen, wenn die
entsprechende Capability nicht aktiv ist.

## 4. Meilenstein C — World-Map und Infected-Raids

Erst nach A und B wird der vollständige Bedrohungs-/Raidpfad geöffnet:

```text
Building-/Power-Snapshot
→ ThreatPressure
→ StorySelector
→ StoryState commit
→ IncidentQueue
→ Spawn-/Raid-Worker
→ Kampf-/Verwaltungsauflösung
→ genau einmaliger Abschluss
```

### 4.1 Raid-Grenzen

- Vanilla-Wealth-Raids bleiben nach Policy unabhängig aktiv;
- Rimconemy erzeugt keinen zweiten Raid für denselben Idempotency-Key;
- `InfectedRaidWorker` muss für „echter Spawn“ eigene Faction-/Pawn-/Incident-Daten
  besitzen;
- Letter-only ist bis zum bestandenen Spawn-Gate ausdrücklich ein nicht-fertiger
  Zwischenstatus;
- Raid-Ziel, Stärke, Seed, Spawnstatus und Auflösung werden gespeichert;
- kein Outpost oder Mechadroid verhindert ein Game Over bei null kontrollierbaren
  Spieler-Pawns.

### 4.2 World-Map-Zustand

World-Map-Raid-/Outpost-Daten benötigen:

- stabile Objekt-/Raid-ID;
- Ziel-/Quell-ID;
- Route/Pfad oder expliziten `Unavailable`-/`Frozen`-Status;
- ETA aus absoluten Welt-Ticks;
- Save-/Load-Rebuild;
- Kartenwechsel- und Temporary-Map-Verhalten;
- genau-einmalige Ankunft und Auflösung.

## 5. Daten- und Capability-Verträge

### 5.1 BuildingSnapshot

Das gemeinsame Read-Model enthält mindestens:

```text
SchemaVersion
SnapshotTick
BuildingId
BuildingKind
MapId / WorldObjectId
ConstructionState
InputResourceIds
InputAmounts
PowerState
FuelState
DamageState
OwnerId
ContentHash
```

Das Snapshot-Objekt ist Read-only by contract. Der Owner mutiert den Spielzustand;
Konsumenten lesen nur Snapshots.

### 5.2 Capability-Gates

Neue Capability-IDs werden erst beim Implementierungsplan endgültig eingefroren,
aber die Semantik ist festgelegt:

- `rimconemy.scavengerinfrastructure.building` — Building-/Material-Read-Model;
- `rimconemy.scavengerinfrastructure.power` — PowerChainSnapshot;
- `rimconemy.economyterritory.physical_transfer` — Reserve/Execute/Cancel;
- `rimconemy.infectedautomation.building_threat` — Building-Faktoren für Threat;
- `rimconemy.infectedautomation.mechadroid_jobs` — validierte Auftragsdomäne.

Kein Leser verwendet einen Vertrag, wenn `HasCapability` nicht erfolgreich ist.

## 6. Save- und Idempotenz-Vertrag

Jeder Meilenstein versioniert seinen Zustand:

- `BuildingSchemaVersion`;
- `TransferSchemaVersion`;
- `OutpostSchemaVersion`;
- `AutomationJobSchemaVersion`;
- `RaidSchemaVersion`.

Für jeden mutierenden Command gilt:

```text
RequestId + PackageId + IdempotencyKey
```

Save/Load-Regeln:

- fehlende optionale Zustände erhalten sichere Defaults;
- inkompatible mutierende Zustände werden eingefroren oder kontrolliert abgelehnt;
- niemals still doppelt verbrauchen, produzieren, bauen, spawnen oder auflösen;
- Snapshot-Daten werden rekonstruiert, wenn sie aus physischen Maps stammen;
- absolute Welt-Ticks statt lokaler Countdown-Zähler.

## 7. UI- und Diagnoseanforderungen

Der Spieler muss sehen können:

- benötigte und vorhandene Bauressourcen;
- tatsächliche Kostenquelle;
- Power-/Fuel-/Wasserstatus;
- Blockade- oder Fehlergrund;
- Outpost-Brutto, Wartung, Verteidigung und Netto;
- Mechadroid-Auftrag und Status;
- Threat-Beitrag mit Faktor;
- Raid-Ziel, Stärke, Seed, ETA und Auflösungsstatus.

„Nicht verfügbar“ darf nicht als `0` angezeigt werden.

## 8. Test- und Runtime-Gates

### Meilenstein A

- alle fünf Projekte `dotnet build -c Release` erfolgreich;
- Def-Load ohne XML-/Config-Fehler;
- ConstructionDebris liegt physisch im Lager;
- Wall und Door verwenden den vorgesehenen Baukostenpfad;
- Generator blockiert ohne Input und läuft mit gültigem Input;
- powered Arrow Turret blockiert ohne Power und aktiviert mit Power;
- Storage-/Power-Snapshot entspricht dem Spielzustand;
- Building-Output erzeugt höchstens einmal XP.

### Meilenstein B

- physischer Transfer Reserve/Execute/Cancel ohne Duplikat;
- Outpost-Building-Investition überlebt Save/Load;
- Mechadroid-Auftrag durchläuft Statusmaschine;
- fehlende Capability erzeugt Fallback/Blocked statt Phantomdaten;
- Kartenwechsel verändert keine deterministischen Ergebnisse.

### Meilenstein C

- StoryDirector liest Building-/Power-Snapshot;
- Eventauswahl bleibt deterministisch;
- Incident wird höchstens einmal queued und ausgeführt;
- echter Infected-Spawn wird im Spiel beobachtet;
- World-Map-Raid überlebt Save/Load und Kartenwechsel;
- Vanilla-/Quest-/DLC-Incidents bleiben gemäß Policy funktionsfähig.

Der bestehende `scripts/runtime_test.sh` darf Boot-/Def-/Regression-Gates
belegen, aber nicht allein Save/Load, tatsächlichen Bau, Mechadroid-Arbeit oder
Raid-Spawn als bestanden melden. Dafür werden separate Runtime-Testschritte
benötigt.

## 9. Nicht-Ziele und Sicherheitsgrenzen

- keine neue globale Item-/Credits-Inventarquelle;
- kein pauschales Abschalten aller Vanilla-Raids;
- keine Thread-Simulation des Spiels;
- keine XP aus Tick-Sampling;
- keine stillen Save-Migrationen;
- kein direkter Cross-Package-State-Schreibzugriff;
- kein „completed“-Marker ohne passenden Code-/Def-/Runtime-Beleg.

## 10. Freigabe- und Reihenfolgeregel

Implementierung erfolgt strikt in dieser Reihenfolge:

1. Spec-Review und Implementierungsplan;
2. Meilenstein A inklusive Tests und Runtime-Gate;
3. Code-Review und User-Abnahme von A;
4. Meilenstein B inklusive Tests und Runtime-Gate;
5. Code-Review und User-Abnahme von B;
6. Meilenstein C inklusive Tests und Runtime-Gate;
7. abschließender Full-Profile-Runtime-Lauf.
