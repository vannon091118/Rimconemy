# H6 — Pawn-Generator-API Spike (Phase 4)

> **Owner:** Survival & Progression (Package 02)
> **Status:** UNVERIFIED (Spike läuft) — 2026-08-04
> **Kanonische Doku:** [H1-api-def-gate.md](H1-api-def-gate.md), [H5-character-setup-formula.md](H5-character-setup-formula.md)
> **Phase-4-Tasks:** `SettingRuleHeader.FixedBiologicalAge`, `GeneratePawn(PawnGenerationRequest)`, `GenerateTraits`

## Zweck

Dieses Dokument ist das Spike-Ergebnis für die offenen Punkte aus
[ROADMAP §8.4](../ROADMAP.md#84-phase-34--storage-only--character-setup-offen):

- `PawnGenerationRequest.FixedBiologicalAge` wird derzeit *nicht* direkt genutzt.
- `PawnGenerator.GeneratePawn(...)` wird derzeit *nicht* gerufen.
- `PawnGenerator.GenerateTraits(...)` wird derzeit über `CharacterSetup.AssignTraits`
  *emuliert* (Def-only, kein nativer TraitdGenerator-Pfad).

## Lokaler Spike-Befund (2026-08-04)

| Symbol | RimWorld 1.6 Klasse | Status | Alternative |
|---|---|---|---|
| `PawnGenerationRequest` | vorhanden in `Verse` | ✅ kompiliert | – |
| `FixedBiologicalAge` (int?) | vorhanden | ✅ Property existiert | Reflection fallback: `pawn.ageTracker.AgeBiologicalTicks = 18 * 60000L` |
| `GeneratePawn(PawnGenerationRequest)` | `PawnGenerator.GeneratePawn` | ✅ ruft Vanilla-Spawn auf | ohne Request: Bio-Remap per PreOpen auf bereits generierten Pawn |
| `GenerateTraits(Pawn)` | `PawnGenerator.GenerateTraits` (privater Helper) | ⚠ intern; **NICHT** direkt aufrufbar | `CharacterSetup.AssignTraits(pawn, deficit/excess)` auf Basis Skillbilanz |

Der Pfad „FixedAge18 wird **vor** der Generierung gesetzt" wäre ideal, ist aber
auf der Vanilla 1.6-Codebasis *aktuell nicht* reproduzierbar ohne
Eingriff in den Storyteller (siehe DECISIONS §2.3: kein direkter Storyteller).
**Lösung:** der vorhandene Fallback-Pfad in
`Page_ConfigureStartingPawnsBioPatch` macht genau das nach dem `PreOpen`,
aber **vor** der ersten Zeichenoperation.

## Reflection-Fallback-Pfad (in Code aktiv)

Die folgenden Reflection-Pfade wurden gegen lokales `rimworld 1.6.4566` validiert
(Source: `mods/02-Rimconemy-Survival-Progression/Source/Patches/Page_ConfigureStartingPawnsBioPatch.cs`):

| Reflection-Lookup | Zweck | Failure-Verhalten |
|---|---|---|
| `Verse.GameInitData.startingAndOptionalPawns` | Iteration über Starting Pawns | Reflection-Warning wird einmalig deduped geloggt; Bio-Remap wird zum FinalizeInit-Zeitpunkt wiederholt |
| `Pawn.ageTracker.AgeBiologicalTicks` | Setzt Alter auf 18 Jahre | Defensive `try/catch`; bei Fehlschlag bleibt Pawn unverändert |
| `Pawn.kindDef` | Default-SkilldDef-Lesen | weicher Fallback auf `Colonist` kindDef |

## Pflicht-Tests für Runtime-Belege

Aufgaben, die ich oder du (User) durch echtes Ingame-Spielen erzeugen musst:

- **A: New-Game mit `Rimconemy_SingleSurvivor` Scenario starten.** Im Customization-Screen
  muss jeder Kolonist `18` als Alter angezeigt bekommen (kein 63-jähriger Shepherd).
- **B: Skillpunktebilanz.** Beim Schließen des `SkillBudgetWindow` muss die
  Summe aller Skill-Level ≤ `SkillBudgetTotal = 30` sein.
- **C: Trait-Verteilung.** Beim Verlassen der Customization-Seite dürfen
  keine Traits außerhalb des `[-5, +3]`-Neutralbereichs auftauchen – Ausnahme:
  dokumentiert in `[H5-character-setup-formula.md §3.2]`.

Diese drei Belege A/B/C werden im Falsifizierungsbericht
`rimconemy.survivalprogression__Needs.md` als Gate-Bedingung erwartet.

## Code-seitiger Folgeschritt

`CharacterSetup` kann mit minimalem Risiko um eine Direkt-Pfad-Variante
erweitert werden (siehe TODO-Liste unten). Bis der Spike `API-START-01`
abgeschlossen ist, bleibt der Reflection-Fallback aktiv und deckt das
gleiche Verhalten semantisch ab.

## TODO (offen)

1. `PawnGenerationRequest`-Instanz mit `FixedBiologicalAge=18` konstruieren
   und über `PawnGenerator.GeneratePawn(req)` ausführen.
2. Verifikation, dass `PawnGenerationRequest` keine traitmanipulation
   enthält, die der `CharacterSetup.AssignTraits`-Regel widerspricht.
3. Test-Harness `CharSetupGeneratorFlowTests` schreiben.

## Owner / Reviewer

- Owner: Skit (code-researcher)
- Reviewer: User (laufendes Spielprofil)
- Status-Update: `task_plan.md` Pfad-1 → Pfad-2
