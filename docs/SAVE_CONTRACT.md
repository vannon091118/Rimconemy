# SAVE_CONTRACT.md — Rimconemy Save & Migration Specification

> **Stand:** 2026-08-04  
> **Owner:** Foundation (01) & Story/Threat (05) & Economy (04) & Progression (02) & Scavenger (03)  
> **Zielplattform:** RimWorld 1.6.4566 (Unity/Mono Scribe XML)  
> **Status:** Kanonischer Save-Vertrag (G1-Gate / Block A1)

---

## 1. Übersicht & Zielsetzung

Dieses Dokument definiert das strikte Verhalten aller Rimconemy-Pakete bei der **Speicherung**, **Initialisierung** und **Migration** von Spielständen (`ExposeData` via RimWorld Scribe).

Jede modifizierte oder erstellte Datenstruktur muss genau eine der drei definierten Lade-Reaktionen abbilden:
1. `Migrated`: Älteres Schema wird verlustfrei und rückwärtskompatibel auf das aktuelle Schema gehoben.
2. `FrozenWithWarning`: Unbekannte oder inkompatible Teilzustände werden sicher eingefroren und mit Warnmeldung geladen, ohne das Spiel zu zerstören oder Phantomdaten zu erzeugen.
3. `LoadRejectedWithReason`: Inkompatible oder korrumpierte Spielstände werden explizit mit lesbarem Grund im Log/UI abgelehnt.

---

## 2. Schema-Versionierung per Paket

Jedes Paket, das ein `GameComponent` oder `WorldObjectComp` besitzt, verwaltet eine eigene `CurrentSchemaVersion`:

| Paket | Host-Klasse | Schema-Feld | Current | Migration Trigger |
|---|---|---|---|---|
| `01 Foundation` | `FoundationSaveData` | `foundationSchemaVersion` | `1` | `LoadedSchemaVersion < CurrentSchemaVersion` |
| `02 Survival` | `ProgressionGameComponent` | `progressionSchemaVersion` | `1` | `SchemaVersion < CurrentSchemaVersion` |
| `03 Scavenger` | `StorageSnapshot` (via State) | `storageSchemaVersion` | `1` | Sub-Envelope Check |
| `04 Economy` | `CreditsLedger` | `ledgerSchemaVersion` | `1` | `_historyCompletenessKnown` Flag |
| `05 Infected` | `StoryState` | `storyStateSchema` | `1` | `SchemaVersion < CurrentSchemaVersion` |

---

## 3. Verbindliche Lade-Fälle

### 3.1 Fall `Migrated` (Automatische Schema-Migration)
- **Kriterien:** `SchemaVersion < CurrentSchemaVersion` und Migrationspfad ist im Code vorhanden.
- **Verhalten:**
  1. Default-Werte für neu eingeführte Felder setzen.
  2. Bestehende Daten in das neue Datenformat konvertieren.
  3. `SchemaVersion` auf `CurrentSchemaVersion` anheben.
  4. Ereignis im `Foundation.EventLog` protokollieren (`Category="Save"`, `EventType="Migration"`).
  5. UI-Diagnose aktualisieren (`WasMigrated = true`).

### 3.2 Fall `FrozenWithWarning` (Teilweise inkompatibel / Unvollständige Historie)
- **Kriterien:** Daten fehlen (z.B. Trimmed-History bei alten `CreditsLedger`-Saves) oder verhalten sich unvollständig, zerstören aber nicht den Spielstand.
- **Verhalten:**
  1. Bestehende validierbare Daten erhalten.
  2. Fehlende Daten nicht durch erfundene Zufallswerte auffüllen (`_historyCompletenessKnown = false`).
  3. Warning im RimWorld-Log ausgeben: `[PackageName] Historical state partially trimmed; freezing legacy snapshot.`.
  4. Spiel läuft ohne Absturz weiter; Recomputes stützen sich auf konservative Fallbacks.

### 3.3 Fall `LoadRejectedWithReason` (Harte Inkompatibilität / Zerstörte Daten)
- **Kriterien:** Schema-Version ist größer als `CurrentSchemaVersion` (Zukunftssave) oder Pflicht-Strukturen sind unlesbar/korrupt.
- **Verhalten:**
  1. Scribe bricht das Laden des betroffenen Components kontrolliert ab.
  2. `Log.Error("[Rimconemy] Unhandled schema version X (current is Y). Loading rejected.")`.
  3. Kein Stillschweigen, keine Mutation von Daten beim fehlgeschlagenen Laden.

---

## 4. Idempotenz & Envelopes

### 4.1 String-Escape & Envelopes (`FoundationSaveData`)
- **Format:** Escaped Pipe-Delimited Envelope (`SequenceId|Tick|Category|EventType|SourcePackageId|Message|Detail`).
- **Escape-Grammatik:** Scanner-basiertes `PipeEscape` (`\\` → `\\\\`, `|` → `\\p`).
- **Invariante:** Replay und Split erfolgen ausschließlich escape-aware über `SplitEscapedFields`. Globales `String.Replace` ist untersagt.

### 4.2 Replay-Schutz & Age-Pruning (`StoryState` / `CreditsLedger`)
- **Key Format:** `EventId:DeterminismKey` (`StoryState`) bzw. `PackageId|RequestId` (`CreditsLedger`).
- **Scribe-Verhalten:** Idempotenz-Keys und deren Erstellungs-Ticks (`_idempotencyTicks` / `_idempotencyTxIdList`) werden synchron in FIFO-Listen serialisiert.
- **Pruning:** Pruning erfolgt erst nach Altersprüfung (`IdempotencyKeyMaxAgeTicks = 30 Tage`). Legacy-Keys ohne Tick-Eintrag erhalten Sentinel `-1` und werden durch Age-Pruning geschützt (nur Count-Cap greift).

---

## 5. Stop-Gates für Save/Load

Ein Mod-Release oder PR wird blockiert, wenn:
1. `ExposeData()` im PostLoadInit-Schritt Objekt-Referenzen ohne Null-Check anspricht.
2. Ein Save/Load-Cycle (Save → Reload) geänderte Mod-Zustände oder verschobene Balances erzeugt.
3. Inkompatible Saves stillschweigend gelöscht werden, statt eine Warnung oder Migration auszulösen.
