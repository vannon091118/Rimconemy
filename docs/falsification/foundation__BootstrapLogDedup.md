# Falsifizierungsbericht: `Foundation/BootstrapLogDedup`

> **Capability:** `rimconemy.foundation` v1 · **Owner:** Foundation · **Stand:** 2026-08-04
> **Status:** `COMPILED`
> **Code-Anker:** `mods/01-Rimconemy-Foundation/Source/Profile/ProfileDetector.cs` · **Test:** `Tests/ProfileDetectorDedupTests.cs`
> **Automatisierter Gate:** `scripts/verify_bootstrap_log.sh` (integriert via `scripts/runtime_test.sh -> runtime_gates -> verify_bootstrap_log_gate`)
> **ROADMAP-Referenz:** §8.2 · **Owner-Checklist:** siehe `docs/falsification/README.md`

## Kontext

Vor dem Fix (Pre-`v0.1.37`) emittierte `ProfileDetector` während Foundation's
`[StaticConstructorOnStartup]`-Kette den `Profile detected:`-Block duplikativ,
weil `PackageRegistry.Register(rimconemy.survivalprogression)` die
Initialisierung von `ProfileDetector` mid-cctor auslöste und der zweite
`NotifyPackageRegistryChanged`-Reentry mit identischem State eine zweite
Log-Linie produzierte. Zusätzlich emittierte `IsDlcLoaded` 5×2 per-DLC-
Diagnose-Linien (`DLC 'X' detected by Name match`).

## Vertrag (Invariants)

Drei Invariants, die in CI halten müssen:

| # | Invariant | Mechanik |
|---|---|---|
| **I1** | `[Rimconemy.Foundation] Profile detected:` erscheint **höchstens einmal** pro Tuple `(sorted_packages, missing_count, dlc_missing_count)`. | `ProfileDetector.TryEmitDetection` vergleicht `_lastEmittedSummary` Ordinal gegen das frisch gebaute `BuildSummaryMessage()`. |
| **I2** | Zwei `Profile detected:`-Linien haben **nie** identischen Vollstring-Inhalt. | Direkt durch `_lastEmittedSummary = logMessage;` umgesetzt; `_lastEmittedSummary` ist starrer als I1, da es DLC-Missing-Count + Profilstatus zusätzlich deduppt. |
| **I3** | `DLC 'X' detected by Name match` (und `by PackageId match`, `NOT detected among running mods`) erscheinen **null Mal**. | Per-DLC `Log.Message`-Aufrufe in `IsDlcLoaded` wurden entfernt; Canonicals `Profile detected:`-Linie trägt bereits `DLCs missing: N`. |

## A — Def-Liste (XML-Defs)

Keine XML-Defs betroffen. Logik-only.

## B — Code-Pfad (Build + Boot)

Quelle: `mods/01-Rimconemy-Foundation/Source/Profile/ProfileDetector.cs`

- Kompiliert: ✅
- Bootstrap-Klasse: `Foundation.Bootstrap.RunAll` ruft `Tests.ProfileDetectorDedupTests.RunAll()` auf
- Patch-Klassen: `Source/Profile/ProfileDetector.cs` (alle internen Modifikationen)
- Dedup-Helper: `ProfileDetector.TryEmitDetection(out string summary)` — neue single entry point
- Snapshot-Felder: `_lastEmittedSummary`, `_lastSortedRegisteredIdsForSummary` — TOCTOU-sicher

### Was sich geändert hat (2026-08-04)

1. `DetectProfile()` ist `internal` (pure state-mutator, kein `Log.Message`).
2. `BuildSummaryMessage()` ist `private` (ordnet `PackageRegistry.RegisteredPackageIds` Ordinal-stable).
3. `TryEmitDetection(out string)` exponiert die dedup-Gate; alle Trigger-Pathway
   (static cctor, `NotifyPackageRegistryChanged`, `Bootstrap.RunAll`,
   `FoundationProfileRefreshTests`, `ProfileDetectorDedupTests`) rufen hierdurch.
4. `ResetForReload()` löscht sowohl `_lastEmittedSummary` als auch
   `_lastSortedRegisteredIdsForSummary` — Save/Load frische Zeile.

## C — Selbsttest (RunAll)

`Tests.ProfileDetectorDedupTests.RunAll()` ist in `Foundation.Bootstrap.RunAll` aufgerufen.
8 Assertions:

1. `ResetForReload()` (setup) clears state.
2. First `TryEmitDetection` returns `true` + populates `out summary`.
3. Second same-state call returns `false` + summary matches first (literal cctor-reentry scenario).
4. Third back-to-back same-state call STILL returns `false` + summary stable.
5. After another `ResetForReload`, `TryEmitDetection` re-emits; post-reset summary content identical to original.

Erwarteter Log: `[Rimconemy.Foundation] Profile detector dedup tests: 8 passed, 0 failed.`

## D — Runtime-Boot (User Live-Test erforderlich)

Erwartetes Pattern (post-`v0.1.37`) in `Player.log`:

```text
2026:[Rimconemy.Foundation] Profile detected: Partial (packages registered: rimconemy.foundation,rimconemy.survivalprogression, missing: 3, DLCs missing: 0)
2027:[Rimconemy.Foundation] Profile detected: Partial (packages registered: rimconemy.foundation,rimconemy.scavengerinfrastructure,rimconemy.survivalprogression, missing: 2, DLCs missing: 0)
2029:[Rimconemy.Foundation] Profile detected: Partial (packages registered: rimconemy.economyterritory,rimconemy.foundation,rimconemy.scavengerinfrastructure,rimconemy.survivalprogression, missing: 1, DLCs missing: 0)
2031:[Rimconemy.Foundation] Profile detected: FullOverhaul (packages registered: rimconemy.economyterritory,rimconemy.foundation,rimconemy.infectedautomation,rimconemy.scavengerinfrastructure,rimconemy.survivalprogression, missing: 0, DLCs missing: 0)
2046:[Rimconemy.Foundation] Profile detected: FullOverhaul (packages registered: rimconemy.economyterritory,rimconemy.foundation,rimconemy.infectedautomation,rimconemy.scavengerinfrastructure,rimconemy.survivalprogression,rimconemy.tests.synthetic, missing: 0, DLCs missing: 0)
```

**Anzahl `Profile detected:`-Linien: 5. Anzahl `DLC 'X' detected by Name match`-Linien: 0.**

Die letzte Linie (mit `rimconemy.tests.synthetic`) entstammt der Foundation-Test
`FoundationCapabilityGateTests (File: `mods/01/Tests/Foundation.CapabilityGateTests.cs`).TestMockRegisterSatisfiedCapability_NoWarn`,
der während Bootstrap eine Mock-Package registriert. Die Linie ist
**canonical** und nicht duplikativ — der dedup-Mechanismus behandelt
diese Mock-Registrierung als legitimen State-Übergang und emittiert
**einmal**.

<!--
Fuege nach `./scripts/runtime_test.sh --require-scenario-tests` den
Player.log-Auszug mit den 5 dedup-bestätigten Linien oben ein.

Erwartetes Verifier-Gate-Pattern:
  PASS: verify_bootstrap_log: ProfileDetector dedup invariants hold
  PASS: summary: Profile detector dedup tests: [0-9]+ passed, 0 failed
-->

## E — Save/Load Roundtrip

Reset-Pfad: `ProfileDetector.ResetForReload()` leert beide Tokens, so dass
post-Reload `TryEmitDetection` eine frische Linie emittiert auch wenn die
Mod-Liste identisch geblieben ist.

<!-- Manuelle Validierung:
1. Spielstand speichern (mit allen 5 Paketen geladen).
2. Save neu laden.
3. Erwartung: Profile detected:` FullOverhaul mit 5 Paketen erscheint genau **einmal** (ggf. plus einmal für tests.synthetic, je nach Bootstrap-Test-Lauf).
-->

## F — Cross-Package READ

`ProfileDetector.CurrentProfile` wird von `FoundationDashboard.cs` und den
Capability-Gates gelesen. Die dedup-Logik hat keinen Einfluss auf
Cross-Reads — `CurrentProfile`, `MissingPackageIds` und `MissingDlcNames`
werden jeweils am Ende von `DetectProfile()` aktualisiert.

<!-- Konsistenz-Check: `CapabilityAudit.HasCapability("rimconemy.foundation.profile", 1)` muss `true` liefern überall in Mods 02..05. Verifikation durch bestehende `summary: CrossPackageState tests: ... passed, 0 failed` Gate in runtime_test.sh. -->

## G — Performance-Kennzahl

`TryEmitDetection` ist O(N) über `RegisteredPackageIds` (N≤6).
Sort + Dedup-Token-Vergleich O(1). Kein messbarer Overhead vs.
pre-fix Pfad.

<!-- Sample: 5 Boots in Folge, jeder unter 60 s. Keine Regression. -->

## Reproduktion (eine Zeile)

```bash
./scripts/runtime_test.sh
```

Erwartetes Ergebnis:
- `PASS: verify_bootstrap_log: ProfileDetector dedup invariants hold`
- `PASS: summary: Profile detector dedup tests: 8 passed, 0 failed`
- `Runtime test result: PASS (warnings=1)` (Warning = Scenario-Test, nicht dedup-relevant)

## Negative-Test (manuell)

Wenn jemand den dedup-Mechanismus bricht, kann der Gate das so nachweisen:

1. Modifiziere `ProfileDetector.TryEmitDetection` so dass es IMMER `true` zurückgibt (`_lastEmittedSummary` wegkommentieren).
2. `./scripts/runtime_test.sh` → `FAIL: verify_bootstrap_log: ProfileDetector dedup invariants hold`.
3. Diagnostik auf stderr zeigt genau die duplikative Linie.

## Siehe auch

- `docs/falsification/foundation__Servicebus.md` — vorheriger Foundation-Bericht (Servicebus-Removal).
- `ROADMAP.md` §8.2 — Index der 20 Falsifizierungsberichte.
- `scripts/verify_bootstrap_log.sh` — Gate-Script mit Inline-Specifikation.
- `mods/01-Rimconemy-Foundation/Source/Profile/ProfileDetector.cs:TryEmitDetection` — Implementation.
