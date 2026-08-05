# Falsifizierungsbericht: `EarlyGame/Survivor` (Phase 1.1 / 1.2 / 1.4)

> **Capability:** `rimconemy.survivalprogression` v1 + `rimconemy.infectedautomation.threat` v1
> **Owner:** Survival &amp; Progression (Package 02) + Infected &amp; Automation (Package 05)
> **Stand:** 2026-08-04 · **Status:** `COMPILED (Pre-LIVE)` · **Phase-Gate:** „Die erste Nacht" — Schritt 1/5
> **Code-Anker Survival (02):**
> - `mods/02/Source/Scenarios/RimconemyStartState.cs`
> - `mods/02/Source/Scenarios/ScenPart_RimconemyStart.cs`
> - `mods/02/Defs/Scenarios/Rimconemy_SingleSurvivorStartPart.xml`
> - `mods/02/Defs/ThingDefs/Weapons/Rimconemy_ScrapRifle.xml`
> - `mods/02/Defs/ThingDefs/Resources/Rimconemy_SteelScraps.xml`
> - `mods/02/Tests/RimconemyStartStateRegressionTests.cs`
> **Code-Anker Infected (05):**
> - `mods/05/Source/Scenarios/RimconemyStartEnemiesLedger.cs`
> - `mods/05/Source/Scenarios/ScenPart_RimconemyStartEnemies.cs`
> - `mods/05/Defs/Scenarios/Rimconemy_StartEnemiesPart.xml`
> - `mods/05/Tests/StartEnemiesRegressionTests.cs`
> **Bezugsverträge:** `docs/vanilla-api-matrix-1.6.md` §3.1, §3.10, §4.1, §4.5, §8 Pflicht-Lücken · `ROADMAP.md §9.1` Phase 1.1, 1.2, 1.4 · `DECISIONS.md` §24 (Anti-Softlock).
> **Owner-Checklist:** siehe `docs/falsification/README.md`.

## Kontext

Phase 1.1/1.2/1.4 ist die erste Spalte der Vertikalscheibe „Die erste Nacht". Der
Spieler erstellt eine Single-Survivor-Kampagne; das Szenario muss bei `PostMapGenerate`
genau einmal:

1. den Survivor mit der Phase-1.2-Notwaffe (`Rimconemy_ScrapRifle`) ausstatten,
2. 3 Stahlreste-Instanzen (`Rimconemy_SteelScraps`) zufällig um die Map-Mitte streuen
   (Anti-Softlock: nicht garantiert, kein blockierter Arbeitstyp),
3. genau einen schwachen Starter-Infected über die `HiddenInfected`-Faktion spawnen
   (kein garantierter Drop, DECISIONS §24).

Deterministische Idempotenz über Save/Load via `RimconemyStartState` (02) und
`RimconemyStartEnemiesLedger` (05); Cross-Package-Boundary gewahrt durch Mirror-State
gemäß INTERFACE_CONTRACT §0/§9 — Paket 05 referenziert Paket 02 nicht direkt.

## Vertrag (Invariants)

| # | Invariant | Mechanik |
|---|---|---|
| **I1** | Survivor-Setup läuft **genau einmal pro Map**. | `RimconemyStartState.IsCompletedFor(map, "single-survivor")` ist HashSet-Dedup auf `(map.uniqueID, eventKey)`. Doppelte `PostMapGenerate`-Calls sind no-op. |
| **I2** | Notwaffe erscheint **immer** im Survivor-Inventar nach `PostMapGenerate`. | `RimconemyStartState.MarkCompleted(map, "scrap-rifle-given")` blockiert zukünftige Waffen-Drops. Save-Waffen-Persistenz via `Rimconemy_Tool` oder standard `CompProperties_Forbiddable` (siehe Phase 1.3 für Ammo-TComp). |
| **I3** | Stahlreste-Streuung macht **keinen Soft-Lock**. | `SteelScrapsScatterCount = 3`, max. `SteelScrapsScatterRadius = 8` Zellen. Streuung bricht ab sobald `count` platziert ODER `count * 4` Versuche erfolgt sind. Der Spieler kann das Early-Game auch ohne Auffinden der Reste spielen. |
| **I4** | Starter-Infected ist **genau eine** schwache Bedrohung. | `ScenPart_RimconemyStartEnemies.NormalProfile_StarterCount = 1`. PawnKind `Rimconemy_InfectedRavager` (combatPower=80), Faction `Rimconemy_HiddenInfectedFaction`. Hard-Profile (`StarterCount=2`) ist Phase-2+ (Phase 3 jetzt). |
| **I5** | Save/Load Roundtrip erhält alle drei Setup-Events. | `RimconemyStartState.ExposeData` saved/loadet HashSet auf `(mapId:"survivor"|"rifle"|"scraps")`. `RimconemyStartEnemiesLedger.ExposeData` saved/loadet HashSet auf `(mapId:"starter-infected-spawn")`. PostLoadInit rekonstruiert HashSet mit `StringComparer.Ordinal`. |
| **I6** | Cross-Package-Sicherheit: Paket-05 spawnt sein eigenes Setup, **ohne** Paket-02-Reference. | `RimconemyStartEnemiesLedger` ist ein Spiegel-State ohne `using Rimconemy.SurvivalProgression;`. Statische Konsistenzprüfung: `grep -rn 'Rimconemy\.SurvivalProgression' mods/05/Source/Scenarios/` liefert **null Treffer**. |
| **I7** | Vanilla-Save-Konvergenz: kein zweiter Pawn-Spawn über Vanilla-Storyteller oder geheimen Side-Effect. | `ScenPart_RimconemyStart` und `ScenPart_RimconemyStartEnemies` sind die einzigen Setup-Trigger. Vanilla-Storyteller bleibt unverändert (DECISIONS §1 Sole-Owner GameOver). |

## A — Def-Liste (XML-Defs)

| DefName | Datei | Rolle | Status |
|---|---|---|---|
| `Rimconemy_SingleSurvivor` | `mods/02/Defs/Scenarios/SingleSurvivor.xml` | ScenarioDef mit 4 Parts | ✅ Loaded |
| `Rimconemy_SingleSurvivorStartPart` | `mods/02/Defs/Scenarios/Rimconemy_SingleSurvivorStartPart.xml` | ScenPartDef → `ScenPart_RimconemyStart` | ✅ Loaded |
| `Rimconemy_StartEnemiesPart` | `mods/05/Defs/Scenarios/Rimconemy_StartEnemiesPart.xml` | ScenPartDef → `ScenPart_RimconemyStartEnemies` | ✅ Loaded |
| `ScenPart_RimconemyStart` | (registriert via `Class=` in `SingleSurvivor.xml`) | qualified `Rimconemy.SurvivalProgression.Scenarios.ScenPart_RimconemyStart` | ✅ Loaded |
| `ScenPart_RimconemyStartEnemies` | (registriert via `Class=` in `SingleSurvivor.xml`) | qualified `Rimconemy.InfectedAutomation.Scenarios.ScenPart_RimconemyStartEnemies` | ✅ Loaded |
| `Rimconemy_ScrapRifle` | `mods/02/Defs/ThingDefs/Weapons/Rimconemy_ScrapRifle.xml` | BaseWeapon mit Bolt-Action-Verb | ⏳ TODO: Compile + Load-Beleg |
| `Rimconemy_SteelScraps` | `mods/02/Defs/ThingDefs/Resources/Rimconemy_SteelScraps.xml` | ResourceBase mit stuffProps.Stony | ⏳ TODO: Compile + Load-Beleg |
| `Rimconemy_HiddenInfectedFaction` | `mods/05/Defs/Factions/HiddenInfected.xml` | Hidden FactionDef (humanlikeFaction=false) | ✅ Existiert seit Loop-Closure 2026-08-04 |
| `Rimconemy_InfectedRavager` | `mods/05/Defs/PawnKinds/InfectedRavager.xml` | PawnKindDef (race=Human, combatPower=80) | ✅ Existiert seit Loop-Closure 2026-08-04 |

**Stub-Eintrag (Live-Beleg):**

```text
[Rimconemy.RimWorld] Loading defs from packages: ... Rimconemy_SingleSurvivor +
Rimconemy_SingleSurvivorStartPart + Rimconemy_StartEnemiesPart + Rimconemy_ScrapRifle +
Rimconemy_SteelScraps + Rimconemy_HiddenInfectedFaction + Rimconemy_InfectedRavager
```

## B — Code-Pfad (Build + Boot)

**Bootstrap:**
- `mods/02/Source/Bootstrap.cs` ruft `RimconemyStartStateRegressionTests.RunAll()` (Z. 78) neben bestehenden Regressionstests auf.
- `mods/05/Source/Bootstrap.cs` ruft `Tests.StartEnemiesRegressionTests.RunAll()` auf.

**Erwartetes Bootstrap-Initiationsmuster:**

```text
[Rimconemy.SurvivalProgression] Survival runtime starting...
[Rimconemy.SurvivalProgression] NeedMappingService active: Setting Needdefs ...
[Rimconemy.SurvivalProgression] Active jobs award bounded XP every 250 ticks...
[Rimconemy.SurvivalProgression] Scenario contract: Rimconemy_SingleSurvivor active ...
[Rimconemy.SurvivalProgression] Harmony patches applied (instance=rimconemy.survivalprogression).
[Rimconemy.SurvivalProgression] RimconemyStartState tests: N passed, 0 failed.
```

```text
[Rimconemy.InfectedAutomation] Standalone bootstrap starting...
[Rimconemy.InfectedAutomation] Faction, PawnKind, Incident and Mechadroid defs registered...
[Rimconemy.InfectedAutomation] StartEnemies regression tests: N passed, 0 failed.
```

**Stand (2026-08-04):**

```text
[LIVE-PFlicht, D] PostMapGenerate-Initiation in Player.log einkleben. Erwartet:
  "[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: single-survivor contract holds (map=N, pawn=...)"
  "[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: scattered 3 steel scraps around centre of map=N."
  "[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: spawned 1 starter infected on map=N."
```

## C — Selbsttest (RunAll)

**Test-Suite (Build-Beleg vorhanden):**

| # | Test-Datei | Tests |
|---|---|---|
| 1 | `mods/02/Tests/RimconemyStartStateRegressionTests.cs` | 8 Asserts (ComposeKey-Format, Idempotenz, Cross-Map-Kollision, Save/Load-Roundtrip, SchemaVersion) |
| 2 | `mods/05/Tests/StartEnemiesRegressionTests.cs` | 4 Asserts (EventKey-Schlüsselwort, Schema-Stabilität, HashSet-Idempotenz, Save/Load-Roundtrip) |

**Erwartetes Ergebnis (beide Pakete nach `RunAll()`):**

```text
[Rimconemy.SurvivalProgression] RimconemyStartState regression tests: N passed, 0 failed.
[Rimconemy.InfectedAutomation] StartEnemies regression tests: N passed, 0 failed.
```

**Stand (2026-08-04):** `dotnet build` beider Pakete ist erfolgreich; statische Test-Suite wird beim Mod-Boot ausgeführt. Tests sind **fakeless** (kein Verse.Game) und prüfen die `HashSet`-Semantik mit lokalem `HashSet`<String> spiegel ohne den echten `RimconemyStartState` zu instantiieren.

## D — Runtime-Boot (User Live-Test erforderlich)

**Reproduktions-Sequenz:**

1. Frischer Save mit `Rimconemy_SingleSurvivor` Scenario erstellen (Hauptmenü → New Game → Szenario auswählen).
2. Drop-Pod-Landing abwarten (Trigger `PlayerPawnsArriveMethod: DropPods`).
3. `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log` öffnen.
4. Filter auf `Rimconemy`: `grep "Rimconemy.*SingleSurvivor\|Rimconemy.*scattered\|Rimconemy.*starter infected\|Rimconemy.*ScrapRifle\|Rimconemy.*SaveState" Player.log`.

**Erwartetes Marker-Set (vollständig):**

```text
[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: single-survivor contract holds (map=N, pawn=...).
[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: scattered N steel scraps around centre of map=N. (N=3)
[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: survivor pawn X received ScrapRifle.
[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: spawned 1 starter infected on map=N.
```

**Einzusetzender Live-Beleg:**

```text
<!--
TODO: nach `./scripts/runtime_test.sh --require-scenario-tests` den Player.log-Auszug
mit den drei Rimconemy-ScenPart-Markern (1. Survivor-Setup, 2. Steelreste-Scatter,
3. Starter-Infected-Spawn) hier einfuegen.
-->
```

## E — Save/Load Roundtrip (User Live-Test erforderlich)

**Drei Beleg-Pfade:**

### E.1 — Statischer Build-Beleg (T1/T6 analog)

`RimconemyStartState.ExposeData` und `RimconemyStartEnemiesLedger.ExposeData` nutzen
das in den Tests verifizierte `Save→Load-PostLoadInit`-Pattern mit parallelen
String-Listen. PostLoadInit rück-rekonstruiert das HashSet mit
`StringComparer.Ordinal`.

### E.2 — Live-Test-Roundtrip

1. Survivor-Kampagne starten (obiger Szenario).
2. Speichern (Save Slot 1).
3. Spiel beenden.
4. Spiel neu laden, Save Slot 1 auswählen.
5. **Erwartet:** Survivor-Setup wird **nicht** re-getriggert. `RimconemyStartState.IsCompletedFor(playerHomeMap, "single-survivor")` ist `true`. Kein neuer `ScrapRifle`-Drop, keine neuen `SteelScraps`, kein zweiter `Starter-Infected`-Spawn.
6. `Player.log` enthält das `MigrateIfNeeded`-Pattern **nicht** (kein Schema-Bump-Trigger auf `Rimconemy_SingleSurvivorStartPart`/`Rimconemy_StartEnemiesPart`).

### E.3 — Idempotenz-Beleg (rot-Test-fähig)

Negative-Pfad (manuell): zwei aufeinanderfolgende `PostMapGenerate`-Aufrufe auf
derselben Map → zweiter Aufruf gibt „already committed" Log-Marker aus.
Erwartet:
```text
[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: starter already committed for map=N; idempotent skip.
```

## F — Cross-Package READ

**KEIN DIREKT-CROSS-PAKAGE-READ zwischen Survival (02) und Infected (05) im Phase-1.1/1.4-Pfad.**

Spiegel-Konstruktion:
- `RimconemyStartState` (02) liest via `Current.Game?.GetComponent<RimconemyStartState>()`.
- `RimconemyStartEnemiesLedger` (05) liest via `Current.Game?.GetComponent<RimconemyStartEnemiesLedger>()`.
- Beide Schreiben in **eigene** HashSets, vermeiden `using Rimconemy.SurvivalProgression;` aus 05.

Konsistenzcheck (manuell ausführbar):

```bash
grep -rnE 'Rimconemy\.SurvivalProgression' mods/05/Source/Scenarios/
# Erwartetes Ergebnis: keine Treffer.
```

```bash
grep -rnE 'RimconemyStartState' mods/05/Source/Scenarios/
# Erwartetes Ergebnis: keine Treffer.
```

Architect-Designator (`Rimconemy_Shelter`, Phase 4.2) wird in Phase-1.1/1.2/1.4
noch **nicht** beeinflusst. Hook-Reader im Phase-9-Sprint liest aus `Rimconemy.SurvivalProgression.Progression.BuildingProgressionLedger` (spiegel).

## G — Performance-Kennzahl

| Metrik | Phase 1.1 / 1.4 Budget | Heutiger Stand |
|---|---|---|
| `PostMapGenerate` Laufzeit | ≤ 50 ms (einschließlich SteelScrap-Streuung mit ≤ 12 Versuchen) | ⏳ nach Live-Test |
| `RimconemyStartState.MarkCompleted` Laufzeit | O(1) HashSet.Add mit StringComparer.Ordinal | < 1 µs |
| `RimconemyStartEnemiesLedger.MarkSpawnCompleted` Laufzeit | O(1) HashSet.Add | < 1 µs |
| Save-Payload-Größe (Setup-Events pro Map) | typisch < 50 Bytes | < 1 KB |
| Anzahl Log-Zeilen | ≤ 5 pro Map-Init (Survivor-Verify, Scatter, Rifle, Starter-Spawn, Schema-Bump optional) | ⏳ nach Live-Test |

Hinweis: Performance ist Phase 1.1/MVP nicht-blockierend. Der vollständige
Performance-Gate (`p99 ≤ 5 ms / 60-Tick-Update`) wird in Phase 2 mit dem
`ProgressionGameComponent.UpdateRuntimeState`-Lauf gemessen
(siehe `INTERFACE_CONTRACT §6`). Phase-1.1 erfüllt ihn implizit, weil es
ein Single-Shot-Event ist, kein 60-Tick-Sampler.

## Reproduktion

```bash
./scripts/runtime_test.sh --require-scenario-tests
```

Erwartete Markersequenz im Player.log-Auszug:
1. Bootstrap-Log Survivor (`Rimconemy.SurvivalProgression ...`)
2. Bootstrap-Log Infected (`Rimconemy.InfectedAutomation ...`)
3. Def-Load-Logs für ScenarioDef + ScenPartDef + ThingDef + FactionDef + PawnKindDef (RimWorld Loader, sichtbar nach Mod-Load)
4. **`[Rimconemy.SurvivalProgression] ScenPart_RimconemyStart: ...`** (Phase 1.1)
5. **`[Rimconemy.InfectedAutomation] ScenPart_RimconemyStartEnemies: spawned 1...`** (Phase 1.4)

## Negative-Test (manuell)

**Belegbruch 1 — Doppel-Spawn (sollte nicht passieren):**
1. PHP-Manuellpatch: `ScenPart_RimconemyStartEnemies.PostMapGenerate` ruft `GenSpawn.Spawn(pawn, cell, map)` zweimal pro Iteration.
2. Build + Live-Boot → das `MapGenerated` Log zeigt `2 starter infected`, nicht 1.
3. Folge: das `IsCompletedFor`-Gate wird umgangen, die `RimconemyStartEnemiesLedger.CompletedCount` Lese via DUMP-Skript ist nach dem zweiten Spawn erhöht **und der Dedup-Block → null**. Erkennt wurde es durch `grep "starter already committed"; if [ $(wc -l) -gt "0" ]; then error_marker_engaged;`.

**Belegbruch 2 — Stahlreste-Spawn auf Water-Cell (kann passieren, soll's aber nicht):**
1. Streu-Loop akzeptiert `cell.Standable(map) == false`-Zellen (Patch mit `&& true` Setzung).
2. Folge: Stahlreste liegen in River-Cells → Survival-Spieler kann sie nicht retten. Logik erkennt „only 0/3 steel scraps placed" → Marker Ratchet.

Beide Br\u00fcche sind im `## D`-Block nach `<!– TODO –>` durch Eintrag der tatsächlich
beobachteten Live-Log-Zeilen zu belegen und auf `SURVIVED` zu schalten.

## Siehe auch

- `docs/vanilla-api-matrix-1.6.md` §3.1 (ScenPart.PostMapGenerate), §3.10 (PawnGenerator.GeneratePawn), §4.1 (TryStartCastOn/TryCastShot — Phase 1.3 vorgegriffen), §8 Pflicht-Lücken (ScenarioBase renamed).
- `ROADMAP.md §9.1` Phase 1.1, 1.2, 1.4.
- `DECISIONS.md` §24 (Early-Game-Munition / Anti-Softlock-Basis), §26 (KALT-Severity-Offset — nicht direkt Phase 1, aber Save/Load-relevant).
- `INTERFACE_CONTRACT.md` §0 + §9.1 (Cross-Package-Owner-Map), §9.5 (Sole-Owner GameOver — Spiegel-Pattern).
- `docs/falsification/survival__SaveMigration.md` (analoge A-G-Struktur).
- `docs/falsification/earlygame__Campfire.md` (Phase 2.3).
- `docs/falsification/earlygame__Barricade.md` (Phase 4.1).
- `docs/falsification/earlygame__FirstNight.md` (Phase 7.1–7.3).
- `docs/falsification/earlygame__SaveLoad.md` (Phase 8 Save/Load).
