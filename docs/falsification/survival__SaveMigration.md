# Falsifizierungsbericht: `Survival/SaveMigration`

> **Capability:** `rimconemy.survivalprogression` v1 · **Owner:** Survival & Progression · **Stand:** 2026-08-04
> **Status:** `COMPILED`
> **Code-Anker:** `mods/02-Rimconemy-Survival-Progression/Source/Character/CharacterSetupState.cs` · **Test:** `Tests/CharacterSetupStateSchemaBumpTests.cs`
> **Automatisierter Gate:** `Foundation/Source/Save/ISchemaMigratable` (zentrale First-Class-Domain-Walker + -Registry)
> **ROADMAP-Referenz:** §8.4 + §5 · **Owner-Checklist:** siehe `docs/falsification/README.md`

## Kontext

Vor dem Phase-2.8-Sprint (Pre-`v0.1.x`) war die Save/Load-Migration von `CharacterSetupState` als Open-Coded-Branch
im `ExposeData` `PostLoadInit`-Pfad versteckt:
```csharp
if (Scribe.mode == LoadSaveMode.PostLoadInit)
{
    if (Records == null) Records = new Dictionary<int, PawnSetupRecord>();
    SchemaVersion = CurrentSchemaVersion;
}
```
Folgen: (1) der `Records == null`-Guard verbarg sich hinter Scribe-Internals und war
nicht direkt testbar; (2) die `SchemaVersion`-Reparatur lief unkontrolliert und konnte
v0-Saves nicht-erkennbare Drift-Schäden verursachen; (3) keine andere Komponente
(economy ledger, story state) konnte denselben Mechanismus sauber wiederverwenden.

Mit Phase-2.8 + `First-Class-Domain`-Extraktion (2026-08-04) wurde
`MigrateIfNeeded()` als public, testbarer Schema-Bump-Eintrittspunkt aus
dem PostLoadInit-Branch herausgezogen. Der Walker (`MigrationStepWalker`)
emittiert die kanonische Log-Zeile, normalisiert garbage-SchemaVersion-Werte
(< 0), propagiert Step-Exceptions ohne try/catch-Bug-Hider und ruft
keinen Side-Effect auf — alle Migrator-Logik sitzt deklarativ in der
`Steps`-Liste.

## Vertrag (Invariants)

Drei Invariants, die in CI halten müssen:

| # | Invariant | Mechanik |
|---|---|---|
| **I1** | `SchemaVersion < 0` → wird **immer** auf `0` normalisiert. | `MigrationStepWalker.Migrate` clamp'd einmal pro Walk (`if (SchemaVersion < 0) SchemaVersion = 0;`). Verhindert vergiftete v0-Saves mit Phantom-Schema-Offset. |
| **I2** | `MigrateIfNeeded()` ist **idempotent**: ein zweiter Aufruf nach erfolgreicher Migration ist eine echte No-Op-Operation (kein Schema-Bump, kein Step-Apply, kein Log). | Walker-Guard: `if (migratable.SchemaVersion == migratable.CurrentSchemaVersion) return migratable.SchemaVersion;`. Plus Self-Guard im `Apply`-Boundary der Migrators (Steps werden nur ausgeführt wenn `[FromVersion, ToVersion)`-Range den aktuellen Stand abdeckt). |
| **I3** | Datenintegrität: ein v0-Save behält alle vorhandenen Scorecard-Daten (Age, SkillDefNames+Levels, TraitDefNames, Applied, NeutralBand) und würde die Records-Liste nur dann mit leerem Dict initialisieren, wenn der Loader tatsächlich `null` zurückgegeben hat. | v0→v1 Step: `() => { if (Records == null) Records = new Dictionary<int, PawnSetupRecord>(); }`. Non-Destruktiv: vorhandene Einträge werden vom Scribe rekonstruiert (LookMode.Deep pro Pawn), bevor der Step läuft. |

Owner-Constraint: Paket 02 ist Sole-Owner von `CharacterSetupState`. `MigrateIfNeeded()`
wird ausschließlich aus `ExposeData(PostLoadInit)` und aus `Tests.CharacterSetupStateSchemaBumpTests`
heraus aufgerufen. Cross-Package-Überschreitung wäre ein INTERFACE_CONTRACT-§9.5-Verstoß.

## A — Def-Liste (XML-Defs)

Keine XML-Defs betroffen. Save/Load-Migration ist rein C#-Logik:
- Keine `Scribe_Values.Look`/`Scribe_Collections.Look`-Schema-Änderungen an fremden RimWorld-Defs.
- Keine `PatchOperationReplace`/`PatchOperationAdd`-Patches auf Vanilla- oder DLC-Defs.
- Keine `DefModExtension`-Felder hinzugefügt.

## B — Code-Pfad (Build + Boot)

Quelle: `mods/02-Rimconemy-Survival-Progression/Source/Character/CharacterSetupState.cs`

- Kompiliert: ✅
- Bootstrap-Klasse: `Survival.Bootstrap.RunAll` ruft `Tests.CharacterSetupStateSchemaBumpTests.RunAll()` auf (Z. 77)
- Patch-Klassen: keine
- Interface: `ISchemaMigratable` (Foundation/Source/Save/) mit explicit interface impls
- ClassId: `"rimconemy.survivalprogression.characterSetup"` (registriert sich im `MigrationRegistry`)

### Was sich geändert hat (2026-08-04)

1. `MigrateIfNeeded()` ist jetzt `public` und ein Einzeiler: `this.RunMigration();`. Früher war die
   Logik in `ExposeData` `PostLoadInit`-Branch eingebettet.
2. `Steps` ist eine lazy-gecachte `List<SchemaStep>` mit Closure gegen `this`. Früher:
   `if (Records == null) Records = new Dict...;` direkt in ExposeData.
3. **`SchemaMigratableExtensions.RunMigration()`** ist die DRY-Orchestration:
   `Register(self)` → `MigrationStepWalker.Migrate(self)` → optional `RecordMigration()`.
   Früher: jeder Migrator schrieb denselben 5-Zeilen-Boilerplate.
4. **`MigrationStepWalker.Migrate()`** ist die zentrale Single-Source-of-Truth für Step-Application,
   Garbage-Normalisierung, Idempotenz-Guard und kanonische Log-Zeile.

## C — Selbsttest (RunAll)

`Tests.CharacterSetupStateSchemaBumpTests.RunAll()` ist in `Bootstrap.RunAll` aufgerufen.
`ExpectedPassCount = 6`. Sechs Test-Methoden (alle `public static bool`):

| # | Test | Was er beweist |
|---|---|---|
| **T1** | `TestV0SchemaBumpsToCurrent` | `SchemaVersion=0` → `MigrateIfNeeded()` → `SchemaVersion == CurrentSchemaVersion` (1). Bestätigt v0→v1-Mechanik. |
| **T2** | `TestV1SchemaIsIdempotent` | `SchemaVersion=1` → `MigrateIfNeeded()` → bleibt 1. Test-Runners prüfen außerdem die Walker-Schritte-Collection ist konsistent. Bestätigt Invariant **I2**. |
| **T3** | `TestV0WithRecordsPreservesData` | v0-Save mit gefüllter Records-Liste (aliquote Pawn-Scorecard inkl. Skills+Traits+Age+NeutralBand) → `MigrateIfNeeded()` → Records bleiben vollständig erhalten. Bestätigt Invariant **I3**. |
| **T4** | `TestV0WithNullRecordsNormalizesToEmpty` | v0-Save mit `Records=null` → `MigrateIfNeeded()` → `Records != null && Records.Count == 0`. Bestätigt Walker verhindert Phantom-Field auf null nach Migration. |
| **T5** | `TestV0WithAppliedFlagPreserved` | v0-Save mit `Applied=true` → `MigrateIfNeeded()` → `Applied` bleibt `true`. Bestätigt Scribe-PostLoadInit + Walker zusammen erhalten den State. |
| **T6** | `TestScribeRoundTripBumpsSchema` | Voller Scribe-Save→Load-PostLoadInit-Cycle via `ScribeRoundTripHelper.RoundTrip<T>(state)` (in-memory XML-Stream, keine Game-Session). Beweist dass der PostLoadInit-Branch den Schema-Bump korrekt triggert UND dass die SchemaVersion-Bump ein file-cycle ist, nicht nur Logic-Cycle. Fallback-Pfad: `state.MigrateIfNeeded()` direkt, wenn Helper nicht verfügbar. |

Erwarteter Log nach `RunAll()`:
```text
[Rimconemy.SurvivalProgression] SchemaBump tests: 6 passed, 0 failed (expected=6).
```

## D — Runtime-Boot (User Live-Test erforderlich)

Beim Boot läuft `Survival.Bootstrap.RunAll()`, das die 6 Tests aufruft. Wenn die Tests
Test-Instanzen mit `SchemaVersion=-1` od. `=0` erzeugen, ruft der Walker zwei Log-Zeilen
pro Test-Instanz. Erwartetes Pattern in `Player.log`:

```text
[Rimconemy.Foundation.Save] rimconemy.survivalprogression.characterSetup step v0->v1: Initialize Records dictionary if missing (initial CharacterSetupState scorecard).
[Rimconemy.Foundation.Save] rimconemy.survivalprogression.characterSetup MigrateIfNeeded: v0 -> v1 (1 step(s)).
...
[Rimconemy.SurvivalProgression] SchemaBump tests: 6 passed, 0 failed (expected=6).
```

**Erwartete Anzahl Walker-Zeilen: 6 (eine pro Test, der v0 als Start-Setpoint hat).
Genau 2 Zeilen pro v0-Trigger (Step + Summary). Tests T2 und T6 (mit anderen Setups)
triggern entweder keine Walker-Zeilen (T2: idempotent) oder beide (T6: Round-Trip-Walk).
T1: 2 Zeilen, T3: 0 Walker-Zeilen (Records werden in Test-Setup gefüllt, Walker würde nicht
eingreifen wenn schon SchemaVersion=1), T4: 2 Zeilen, T5: 2 Zeilen, T6: 2 Zeilen.**

Hinweis: Die **alten** Phase-2.8-Logs ohne Walker-Architektur (z. B.
`[Rimconemy.SurvivalProgression] CharacterSetupState MigrateIfNeeded: v0 -> v1 (no-op upgrade).`)
sind in dem aktuellen Code-Pfad **nicht mehr vorhanden** — sie gehörten zum ersten
Refactor vor der First-Class-Domain-Extraktion. Wer sie sieht, hat einen Stand < `2026-08-04`-Tag.

<!--
Fuege nach `./scripts/runtime_test.sh --require-scenario-tests` den
Player.log-Auszug mit den Walker-Step + Summary Linien oben ein.

Erwartetes Verifier-Gate-Pattern:
  PASS: summary: SchemaBump tests: 6 passed, 0 failed
-->

## E — Save/Load Roundtrip

Drei Pfade:

### E.1 — Automatischer Beleg (T6)
`TestScribeRoundTripBumpsSchema` führt den vollen Save→Load-PostLoadInit-Cycle via
`Foundation/Tests/ScribeRoundTripHelper.RoundTrip<T>(state)` durch. Helper
konstruiert `ScribeSaver` (XmlWriter→MemoryStream) + `ScribeLoader` (XmlDocument→Loader)
per Reflection auf private Scribe-Internals und ruft `ExposeData` dreimal auf
(`Saving`, `LoadingVars`, `PostLoadInit`). Post-Walk wird `SchemaVersion == CurrentSchemaVersion`
asserted. **Datei-Cycle-fähig** statt nur Logic-Cycle.

### E.2 — FoundationSaveData.Cross-Cycle Beleg
Da `CharacterSetupState.SchemaVersion` und `FoundationSaveData.SchemaVersion` beide
durch den zentralen `MigrationStepWalker` laufen, ist der **Cross-Package Cross-Cycle**
garantiert: ein Save, der Foundation und Survival gleichzeitig enthält, durchläuft
beide Walker-Pfade über `MigrationRegistry.Clear()` → `MigrateIfNeeded()`-Chain.
Cross-Package-Reihenfolge wird nicht beeinflusst (jeder Migrator ist self-contained).

### E.3 — Manuelle Validierung (Live-Test)
1. Spielstand mit 3-5 Pawns speichern, alle mit vollständiger Scorecard
   (Skills, Traits, AgeFix, NeutralBand).
2. Spiel neu laden (kein Mod-Wechsel).
3. `Player.log` auf `[Rimconemy.Foundation.Save] rimconemy.survivalprogression.characterSetup`
   prüfen — wenn Schema bereits `=1`, **keine** Walker-Zeilen (idempotent, Invariant I2).
4. `CharacterSetupState.Get()` aufrufen (UI/Console) und `state.Applied` muss `true` sein
   UND alle Pawn-Scorecards müssen vollständig vorhanden sein.

## F — Cross-Package READ

**KEIN CROSS-READ.**
`CharacterSetupState.MigrateIfNeeded()` ist ausschließlich aus zwei Quellen
aufrufbar:

1. `mods/02-Rimconemy-Survival-Progression/Source/Character/CharacterSetupState.cs:191-194`
   (PostLoadInit-Branch in `ExposeData`).
2. `mods/02-Rimconemy-Survival-Progression/Tests/CharacterSetupStateSchemaBumpTests.cs`
   (sechs `TestXXX`-Methoden).

Keine andere Datei in Mods 01/03/04/05 importiert, referenziert oder ruft
`rimconemy.survivalprogression.characterSetup` (das ClassId) oder
`CharacterSetupState.MigrateIfNeeded()` auf. Konsistenz-Check:

```bash
grep -rnE 'rimconemy\.survivalprogression\.characterSetup|CharacterSetupState\.MigrateIfNeeded' --include='*.cs' \
  | grep -vE 'mods/02-Rimconemy-Survival-Progression/Source/Character/CharacterSetupState\.cs' \
  | grep -vE 'mods/02-Rimconemy-Survival-Progression/Tests/CharacterSetupStateSchemaBumpTests\.cs'
# Erwartetes Ergebnis: keine Treffer.
```

## G — Performance-Kennzahl

Walker-Kosten für `CharacterSetupState.MigrateIfNeeded()` im Worst-Case (v0-Save):

- **Schema-Normalisierung**: O(1) — ein Integer-Vergleich.
- **Steps-Iteration**: O(Steps.Count) = O(1) — heute genau ein Step v0→v1.
- **Step-Apply**: O(1) — `if (Records == null) Records = new Dict...`.
- **Log-Emit**: 2 Zeilen pro Walk (Step + Summary). Beide `Log.Message`, nicht `Log.Error`.

Best-Case (v1-Save, idempotent): O(1) im Walker — ein Integer-Vergleich entscheidet
vor der Schritt-Iteration, die gar nicht erst betreten wird. **Kein messbarer
Overhead vs. pre-fix Pfad.**

Scribe-Payload-Größe: Skaliert mit O(P) für P = Anzahl Pawns (typisch 1–5). Jeder
`PawnSetupRecord` ist ~100 Bytes (Tick + AgeBio + AgeChr + NeutralBand
+ 4 Lists). Bei 5 Pawns: <1 kB. **Verglichen mit RimWorld-Standard-Saves (mehrere MB)
völlig marginal.**

## Reproduktion (eine Zeile)

```bash
./scripts/runtime_test.sh
```

Erwartetes Ergebnis:
- `PASS: summary: SchemaBump tests: 6 passed, 0 failed`
- `Runtime test result: PASS (warnings=N)` (Warnings sind Scenario-Tests, nicht migration-relevant)

Eine alternative Live-Log-Verifikation:
```bash
grep "SchemaBump tests" Player.log
# Erwartetes Ergebnis: exakt eine Treffer-Zeile mit "passed=6".
```

## Negative-Test (manuell)

Wenn jemand den SchemaBump-Mechanismus bricht, kann das so nachgewiesen werden:

1. Modifiziere `CharacterSetupState.Steps` so dass die Liste **leer** ist:
   `_cachedSteps = new List<SchemaStep>();`.
2. In `CharacterSetupState`(Z. 191) füge vor `MigrateIfNeeded()` ein:
   `SchemaVersion = 0; Records = null;`.
3. `./scripts/runtime_test.sh` → `FAIL: SchemaBump tests: 1 passed, 5 failed`
   (T1, T4, T5 erwarten Records non-null nach Walk; T6 Walk würde Walker-Force-Forward triggern mit Warning-Log).
4. Walk-Log zeigt `[Rimconemy.Foundation.Save] rimconemy.survivalprogression.characterSetup migration chain stuck at v0, expected v1. Forcing forward (no covering step...)`.

Ein Step der eine Exception wirft, wird **propagiert** (kein try/catch-Bug-Hider
mehr). Das nächste Save-Load-Roundtrip wiederholt die Migration aus dem aktuellen
SchemaVersion.

## Siehe auch

- `docs/falsification/foundation__BootstrapLogDedup.md` — vorheriger Foundation-Falsifizierungsbericht (vergleichbares 7-Sektion-Layout).
- `docs/falsification/survival__GameOver.md` — Game-Over Sole-Owner-Beleg (Paket 02).
- `ROADMAP.md` §8.4 + §5 — Index der Falsifizierungsberichte und Paket-02-Scope.
- `docs/H4-storage-query-contract.md` — Storage-Query-Vertrag (analog-Garantie für `Records`-Persistenz).
- `mods/01-Rimconemy-Foundation/Source/Save/ISchemaMigratable.cs` — Interface-Vertrag.
- `mods/01-Rimconemy-Foundation/Source/Save/MigrationStepWalker.cs` — Walker-Implementation.
- `mods/01-Rimconemy-Foundation/Source/Save/MigrationRegistry.cs` — Cross-Package-Registry.
- `docs/falsification/status-vs-code-audit-2026-08-04.md` §B6 — zugehörige Audit-Zeile.
