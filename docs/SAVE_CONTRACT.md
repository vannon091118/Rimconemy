# SAVE_CONTRACT.md — Rimconemy Save & Migration Specification

> **SSOT-Owner für:** `ISchemaMigratable`-Vertrag, Schema-Bump-Pattern, Save/Load-Lifecycle, Foundation-Save-Bridge, IExposable-Adapter, Schema-Version. Wer ein Topic aus [docs/INDEX.md §1](INDEX.md) hier behandelt, hält eine SSOT-Verletzung fest.
> **Stand:** 2026-08-07 (aktualisiert: Clean Break für Overhaul, v2 Save-Format, keine Vanilla-Migration)  
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

Alle Pakete mit persistierbaren Zuständen implementieren das `ISchemaMigratable`-Interface
(`Foundation/Source/Save/ISchemaMigratable.cs`) als First-Class-Domain für Schema-Migration.
Die zentrale `MigrationRegistry` (string-keyed, idempotent Register) + `MigrationStepWalker`
(Exception-propagierend, kein try/catch) + `SchemaMigratableExtensions.RunMigration` (DRY)
ersetzen die früheren Open-Coded-if/else-Cascades.

| Paket | Host-Klasse | Implementiert `ISchemaMigratable` | Current | Migration Trigger |
|---|---|---|---|---|
| `01 Foundation` | `FoundationSaveData` | ✅ (custom `MigrateIfNeeded` mit Foundation-Side-Effects) | `1` | `Scribe.mode == LoadingVars && SchemaVersion < CurrentSchemaVersion` |
| `02 Survival` | `CharacterSetupState` | ✅ (`this.RunMigration()`) | `1` | `ExposeData` PostLoadInit-Branch |
| `03 Scavenger` | `StorageSnapshot` (via State) | ⬜ (noch nicht migriert) | `1` | Sub-Envelope Check |
| `04 Economy` | `CreditsLedger` | ✅ (`this.RunMigration()`) | `1` | `ExposeData` PostLoadInit-Branch |
| `05 Infected` | `StoryState` | ✅ (`this.RunMigration()`) | `1` | `ExposeData` PostLoadInit-Branch |

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

## 5. Overhaul Clean Break — Keine Migration von Vanilla-Saves (2026-08-07)

> ⚠️ **User-Entscheidung (DECISIONS §34, STORYTELLER_DESIGN_DECISIONS Q3):** Rimconemy ist ein Total-Overhaul. Alte Spielstände (Vanilla oder mit anderen Storytellern) werden **nicht migriert**. Der Spieler MUSS ein neues Spiel starten.

### 5.1 Ablehnung alter Saves

```csharp
// RimconemyStorytellerComp.FinalizeInit()
if (Find.Storyteller.def.defName != "Rimconemy_Storyteller")
{
    Log.Error("[Rimconemy] Incompatible save — Rimconemy requires a new game.");
    Find.WindowStack.Add(new Dialog_RimconemyIncompatibleSave());
    Current.Game = null;  // return to main menu
    return;
}
```

### 5.2 ISchemaMigratable bleibt — aber nur für Rimconemy-interne Migration

Das `ISchemaMigratable`-Interface, `MigrationRegistry` und `MigrationStepWalker` bleiben aktiv — aber NUR für Migration zwischen Rimconemy-eigenen Save-Formaten (v2→v3, v3→v4 etc.). Migration von Vanilla-Saves (v0) zu Rimconemy (v2) wird nicht unterstützt.

### 5.3 Save-Format v2 — maximale Freiheit

Da keine Abwärtskompatibilität zu Vanilla-Saves nötig ist, kann das Save-Format v2:
- Neue Pflichtfelder ohne `LookMode.Undefined`-Fallbacks definieren
- Vanilla-Altlasten (DifficultyDef, IncidentQueue, StorytellerDef) ignorieren
- Radikal andere Scribe-Strukturen verwenden
- Ohne Migrations-Code auskommen (nur `LoadRejectedWithReason` für Fremd-Saves)

---

## 6. Stop-Gates für Save/Load

Ein Mod-Release oder PR wird blockiert, wenn:
1. `ExposeData()` im PostLoadInit-Schritt Objekt-Referenzen ohne Null-Check anspricht.
2. Ein Save/Load-Cycle (Save → Reload) geänderte Mod-Zustände oder verschobene Balances erzeugt.
3. Inkompatible Saves stillschweigend gelöscht werden, statt eine Warnung oder Migration auszulösen.
