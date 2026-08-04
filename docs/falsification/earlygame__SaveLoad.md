# Falsifizierungs-Stand-Bericht: Save/Load-Roundtrip Early Game

**Status:** 🟢 **COMPILED (Pre-LIVE)** — deterministische Idempotenz-Klassen und Tests sind grün (Paket 02 + 05 Build OK, Regressions-Tests grün); LIVE-Lauf wartet auf Runtime-Beleg D–G.
**Gate-Zuordnung:** Vertical-Slice-Plan §Phase 1.4 (Startgegner-Ledger) + Phase 5.2 (FireSignature-Hash) + Phase 9.2 (Unlock-State) + Foundation-`ISchemaMigratable`.
**Pflicht-Szenario für die LIVE-Belege:** `Rimconemy_SingleSurvivor` (`mods/02/Defs/Scenarios/SingleSurvivor.xml`); Save-Slot 1.
**Letzter belegter Code-Stand:** siehe unten A–F.
**Erforderliche Beweise:** siehe §7 Akzeptanz-Gate.

---

## 1. Ziel des Gates

Der Early-Game-Vertikalschnitt muss **Save/Load überleben**, ohne dass einer der folgenden Drift-Punkte auftritt:

- Doppelt-Spawn von Startgegnern (Phase 1.4)
- Verlust der Stahlreste-Tropfen (Phase 2.2)
- Verlust erlernter Architektenfreigaben (Phase 9)
- Verlust von Night-Evaluationsstatus (Phase 7.1)
- Verlust von XP-/Action-Completion-Keys (Phase 8.1)

Dieser Report fasst die Early-Game-spezifischen Save-Pfade zusammen; das übergreifende Migrationsgate liegt in `docs/falsification/survival__SaveMigration.md` und `docs/falsification/foundation__Servicebus.md`.

---

## 2. Vanilla-/Architektur-Anker

| Hook | 1.6-Status | Quelle |
|---|---|
| `GameComponent` als Hub | ✅ bestätigt | `docs/vanilla-api-matrix-1.6.md` §GameComponent |
| `Scribe_Values.Look` / `Scribe_Collections.Look` | ✅ bestätigt | `docs/vanilla-api-matrix-1.6.md` §ThingComp |
| `ISchemaMigratable` Foundation-Pattern | ✅ implementiert | `Source/Foundation/Save/` |
| `MapComponent.ExposeData` Pattern | ✅ bestätigt | `docs/vanilla-api-matrix-1.6.md` §MapComponent |
| Determine-Savegame-Path | ⚠️ spike-pflicht (Save-Inspector-Wahl) | Tooling |

> **Spike-Pflicht:** Vor Akzeptanz muss ein 1-Line-Tool gegen die lokale 1.6-`Assembly-CSharp.dll` zeigen, dass `Scribe_Collections.Look<HashSet<string>>` ohne Crash funktioniert (Bug-History in 1.4).

---

## 3. CODE — vorläufige Stubs

| Pfad | Save-Pfad-Typ | Zustand |
|---|---|---|
| `RimconemyStartState` (Paket 02) | `GameComponent`-Hub-Save via `Scribe_Collections` | 🟢 angelegt (`Source/Scenarios/RimconemyStartState.cs`) |
| `RimconemyStartEnemiesLedger` (Paket 05) | eigener State (kein Cross-Package-Ref) | 🟢 angelegt (`Source/Scenarios/RimconemyStartEnemiesLedger.cs`) |
| `RimconemyStartState.ExposeData()` | Scribe-Look mit HashSet | 🟢 angelegt, Build grün |
| `RimconemyStartEnemiesLedger.ExposeData()` | Scribe mit HashSet | 🟢 angelegt, Build grün |
| `ShelterSnapshot.ContentHash`-Persistenz | Scribe-Value | 🔴 offen (Phase 5.1) |
| `UnlockService.State` Persistenz | Save über `ProgressionGameComponent` | 🔴 offen (Phase 9.2) |
| `BuildingSnapshot.Component`-Persistenz | Vanilla-`ThingComp.ExposeData` | 🟢 vorhanden |
| `StoryState.GameOverPendingQueue` (Audit-Bündel C / F-13) | FIFO-Queue über drei parallele Scribe-Listen | 🟢 angelegt, Build grün, Regression grün (`GameOverPendingQueueRegressionTests` 7 Asserts) |

Aktuelle Stubs (Referenz):

```csharp
// RimconemyStartState.ExposeData()
public override void ExposeData()
{
    Scribe_Values.Look(ref CurrentSchemaVersion, "schemaVersion", 1);
    base.ExposeData();
    Scribe_Collections.Look(ref _keysForSave, "completedKeys",
        LookMode.Value);
    Scribe_Values.Look(ref _initialized, "initialized", false);

    if (Scribe.mode == LoadSaveMode.PostLoadInit)
    {
        var migration = new MigrationStepWalker<RimconemyStartStateMigration>();
        migration.Run(this, CurrentSchemaVersion, GetType());
    }
}
```

```csharp
// Eigenes HashSet-Save-Pattern (gleich in beiden Packages)
private HashSet<string> _keysForSave = new HashSet<string>();

public bool MarkCompleted(string key)
{
    if (string.IsNullOrEmpty(key)) return false;
    if (!_keysForSave.Add(key)) return false;
    _contentHash = ComputeContentHash();
    return true;
}
```

> **Warum kein `Dictionary`?** RimconemyStartState speichert nur den Schlüssel-HashSet; der Aufrufer (`ScenPart_RimconemyStart`) entscheidet die externe Konsequenz. Damit ist das Save korrekt klein und Migration-testbar.

---

## 4. TESTS — vorläufige Stubs

| Pfad | Zustand |
|---|---|
| `mods/02-Rimconemy-Survival-Progression/Tests/RimconemyStartStateRegressionTests.cs` | 🟢 angelegt, Build grün, Pre-LIVE |
| `mods/05-Rimconemy-Infected-Automation/Tests/StartEnemiesRegressionTests.cs` | 🟢 angelegt, Build grün, Pre-LIVE |

> **Tests sind Pre-LIVE:** Sie beweisen den deterministischen Idempotenz-Vertrag (gleiches Szenario → keine Doppelmark), nicht den tatsächlichen Save/Load-Roundtrip.

---

## 5. Bausteine / externe Verträge

| Vertrag | Quelle |
|---|---|
| `ISchemaMigratable` Foundation-Interface | `Source/Foundation/Save/` |
| `MigrationStepWalker` + `MigrationRegistry` | `Source/Foundation/Save/` |
| `CharacterSetupState` als First-Class-Domain (Vorlage für die Hub-GameComponent-Bauweise: Scribe_HashSet + MigrationStepWalker) | `mods/02/Source/Character/CharacterSetupState.cs` |
| `BuildingProgressionAdapter` (XP-Hook-Pattern, Save-Stabilität, Diminishing + Idempotency-Key) — **NICHT** die Scenario-Idempotenz-Vorlage | `mods/02/Source/Progression/BuildingProgressionAdapter.cs` |
| Save-Validierung gegen Doppelspawn | `mods/02/Source/Scenarios/ScenPart_RimconemyStart.cs` |

---

## 6. Was fehlt bis `SURVIVED`

- [ ] A — Pre-LIVE: `RimconemyStartStateRegressionTests.RunAll()` grün (Compile + Determinismus) — **bereits belegt, siehe `mods/02/Tests/RimconemyStartStateRegressionTests.cs`**
- [ ] B — Pre-LIVE: `StartEnemiesRegressionTests.RunAll()` grün — **bereits belegt, siehe `mods/05/Tests/StartEnemiesRegressionTests.cs`**
- [ ] C — Pre-LIVE: XML-Validität aller Early-Game-Defs — **bereits belegt (XML-Parse für `SingleSurvivor.xml`, `Rimconemy_StartEnemiesPart.xml` u.a.)**
- [ ] D — LIVE: ein Save/Load-Roundtrip in `runtime_test.sh` mit Szenario `Rimconemy_SingleSurvivor` zeigt keine Doppelspawn-Drift
- [ ] E — LIVE: Screenshot zeigt nach Load: 1 Startgegner (nicht 2), Survival-Items vorhanden, keine Warnung
- [ ] F — LIVE: Schema-Bump-Test `RunAll()` überlebt erzwungenen Versionssprung (Save-Datei alt → neu zu `SchemaVersion=2`)
- [ ] G — LIVE: Tag/Nacht-Wechsel über die Nacht hinweg: Night-Evaluationsstatus, FireSignature-Hash und Unlock-State bleiben erhalten

> A–C sind bereits pre-LIVE belegt (siehe `runtime-20260804-…txt`-Logs und Build-Berichte unten §9). D, E, F, G benötigen den Runtime-Gate-Lauf nach `./scripts/runtime_test.sh --require-scenario-tests`.

---

## 7. Akzeptanz-Gate

| Punkt | Beleg-Typ | Quelle |
|---|---|---|
| Determinismus Tests | NUnit-Regression | `RimconemyStartStateRegressionTests` |
| Build-Grünheit | `dotnet build` | `Assemblies/Rimconemy.SurvivalProgression.dll` |
| Save/Load Doppelspawn | Runtime | `runtime_test.sh` |
| Schema-Migration | Test-Seam | `ISchemaMigratable` Foundation |

---

## 9. Pre-LIVE-Belegartefakte

- `dotnet build mods/02-Rimconemy-SurvivalProgression.csproj` → `Rimconemy.SurvivalProgression.dll` grün (Phase-1.1-Lauf)
- `dotnet build mods/05-Rimconemy.InfectedAutomation.csproj` → `Rimconemy.InfectedAutomation.dll` grün
- `RimconemyStartStateRegressionTests.RunAll()`: 8 Asserts grün (Determinismus + Save/Load-Spiegel + Schema-Version)
- `StartEnemiesRegressionTests.RunAll()`: 4 Asserts grün (EventKey-Konsistenz + Idempotenz)

## 8. Verweise

- `docs/superpowers/plans/2026-08-04-early-game-vertical-slice.md` §Phase 1.1/1.4 + Phase 8
- `docs/falsification/survival__SaveMigration.md` (Schwesterbericht, übergreifende Migration)
- `docs/falsification/foundation__Servicebus.md` (Schwesterbericht, Servicebus-Save)
- `mods/02-Rimconemy-Survival-Progression/Source/Scenarios/RimconemyStartState.cs`
- `mods/05-Rimconemy-Infected-Automation/Source/Scenarios/RimconemyStartEnemiesLedger.cs`
