# Falsifizierungs-Stand-Bericht: Erste Nacht (Phase 7.1 / 7.2 / 7.3)

**Status:** 🔴 **NICHT BELegt** — Template-Stand, wartet auf `SURVIVED`-Lauf.
**Gate-Zuordnung:** Early-Game-Vertikalscheibe §Phase 7 (`ROADMAP.md §9.1`).
**Letzter belegter Code-Stand:** siehe unten A–H.
**Erforderliche Beweise:** siehe §7 Akzeptanz-Gate.

---

## 1. Ziel des Gates

Der Survivor erlebt **eine kontrollierte erste Nacht** mit genau einem (maximal zwei) garantierten Infizierten-Spawn. Die Spawn-Anzahl hängt nachweislich von Schutz-Score und Feuer-Signatur ab (nicht von Vanilla-Storyteller). Save/Load über die Nacht hinweg darf keinen zweiten Spawn auslösen, und das Ende der Nacht muss eine echte XP-Vergabe für "erste Nacht überlebt" ermöglichen (Phase 8.5 Hook).

Phase 7 hat drei Sub-Gates: Scheduler (7.1), Spawn-Formel (7.2), IncidentWorker (7.3).

**Pflicht-Szenario für LIVE-Belege:** `Rimconemy_SingleSurvivor` (`mods/02/Defs/Scenarios/SingleSurvivor.xml`), Save-Slot 1. Vorbedingung: Phase-1-Gate (Survivor-Setup) + Phase-2-Gate (Campfire + Stahlreste) + Phase-4-Gate (Barrikade) müssen soweit belegt sein, dass Schutz + Feuer-Signatur auf der Map existieren, sonst ist der Spawn-Max-Wert (Night 1 → max. 1) trivialerweise erfüllt und kein Beleg.

---

## 2. Vanilla-/Architektur-Anker

| Hook | 1.6-Status | Quelle |
|---|---|---|
| Eigenes `MapComponent` für Tick-Scheduling | ✅ bestätigt | `docs/vanilla-api-matrix-1.6.md` §MapComponent |
| `IncidentWorker` Subklasse | ✅ bestätigt | `docs/vanilla-api-matrix-1.6.md` §IncidentWorker |
| `IncidentDef` Registration | ⚠️ spike-pflicht | Vanilla-`IncidentDef`-Def-Schema |
| `GenSpawn.Spawn(Pawn, IntVec3, Map)` | ✅ bestätigt | `docs/vanilla-api-matrix-1.6.md` §PawnGenerator |
| `CellFinder.RandomClosewalkCellNear` | ⚠️ spike-pflicht (überladen) | Vanilla-Method-Signatur |
| `GenSight.LineOfSight` | ⚠️ spike-pflicht (1.6-Signatur) | `docs/vanilla-api-matrix-1.6.md` §Spike-Pflicht #3 |
| `Rimconemy_NightInfected` als `IncidentDef` | 🔴 offen (Phase 7.3) | Vertical-Slice-Plan §7.3 |

> **Spike-Pflicht:** Vor Implementierung muss Phase 7.2 zuerst kommen: eine reine, deterministische Funktion (kein RimWorld-State), die für gleiche Inputs denselben Spawn-Wert erzeugt. Dieser Test ist ohne LIVE-Lauf beweisbar.

---

## 3. CODE — vorläufige Stubs

| Pfad | Zustand |
|---|---|
| `mods/05-Rimconemy-Infected-Automation/Source/Night/RimconemyNightComponent.cs` (MapComponent) | 🔴 offen |
| `mods/05-Rimconemy-Infected-Automation/Source/Night/NightSpawnFormula.cs` (pure function) | 🔴 offen |
| `mods/05-Rimconemy-Infected-Automation/Source/Incidents/IncidentWorker_NightInfected.cs` | 🔴 offen |
| `mods/05-Rimconemy-Infected-Automation/Defs/Incidents/Rimconemy_NightInfected.xml` | 🔴 offen |

Aktuelle Stubs (Referenz — Vertical-Slice-Plan §Phase 7.1–7.3):

```xml
<!-- mods/05-Rimconemy-Infected-Automation/Defs/Incidents/Rimconemy_NightInfected.xml -->
<IncidentDef>
  <defName>Rimconemy_NightInfected</defName>
  <label>Bewegung in der Dunkelheit</label>
  <workerClass>Rimconemy.InfectedAutomation.Incidents.IncidentWorker_NightInfected</workerClass>
  <category>ThreatBig</category>
  <letterDef>ThreatBig</letterDef>
</IncidentDef>
```

```csharp
// NightSpawnFormula.cs - reine Funktion, ohne RimWorld-Deps testbar
public static int ComputeNightSpawnCount(
    int baseCount, float protection, float fireSignature,
    float threat, int nightIndex)
{
    float exposure = 1f - Mathf.Clamp01(protection);
    float multiplier = 0.6f + exposure * 1.4f
        + fireSignature * 1.2f + threat * 0.8f;
    int count = Mathf.CeilToInt(baseCount * multiplier);
    if (nightIndex == 1) count = Mathf.Min(count, 1);
    if (nightIndex <= 3) count = Mathf.Min(count, 2);
    return Mathf.Max(0, count);
}
```

```csharp
// RimconemyNightComponent.cs
public sealed class RimconemyNightComponent : MapComponent
{
    private int lastEvaluatedNight = -1;
    public override void MapComponentTick() { /* ... siehe Plan ... */ }
    public override void ExposeData() { Scribe_Values.Look(ref lastEvaluatedNight, ...); }
}
```

```csharp
// IncidentWorker_NightInfected.cs
public sealed class IncidentWorker_NightInfected : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms) { /* ... */ }
    protected override bool TryExecuteWorker(IncidentParms parms) { /* echter Spawn */ }
}
```

---

## 4. TESTS — vorläufige Stubs

| Pfad | Zustand |
|---|---|
| `mods/05-Rimconemy-Infected-Automation/Tests/NightSpawnFormulaRegressionTests.cs` | 🔴 offen |
| `mods/05-Rimconemy-Infected-Automation/Tests/IncidentWorkerNightInfectedRegressionTests.cs` | 🔴 offen |

> **Priorisierter Test (Phase 7.2):** `NightSpawnFormulaTests` kann ohne RimWorld-Lauf beweisen, dass Schutz die Spawn-Zahl senkt, Feuer-Signatur sie erhöht, und Nacht 1 auf max. 1 begrenzt ist.

---

## 5. Bausteine / externe Verträge

| Vertrag | Quelle |
|---|---|
| `ShelterSnapshot` (Schutzscore, Feuer-Signatur) | Phase 5.1/5.2 |
| `WatchRadiusSnapshot` | Phase 6.1 |
| `HiddenInfected` Fraktion + `InfectedRavager` PawnKind | bereits angelegt |
| `ThreatPressure` Aggregation | docs/CODE_STATUS.md §05 |
| XP-Vergabe "erste Nacht überlebt" | Phase 8.5 |

---

## 6. Was fehlt bis `SURVIVED`

- [ ] A — `NightSpawnFormulaTests.RunAll()` grün (deterministisch, ohne RimWorld)
- [ ] B — `RimconemyNightComponent` MapComponent kompiliert
- [ ] C — Scheduler prüft Tag-zu-Nacht-Übergang (`GenDate.DayOfTwelfth`-Helper 1.6 spike-pflicht)
- [ ] D — `IncidentWorker_NightInfected` ist registriert und feuert in max. 1 Spawn
- [ ] E — FirstNight-Lauf endet mit exakt 1 HiddenInfected auf der Map
- [ ] F — Save/Load in der Nacht: keine zweite `lastEvaluatedNight`-Auswertung
- [ ] G — Refuge-Schutz senkt Spawn-Zahl nachweislich auf 0
- [ ] H — Feuer-Signatur (offenes Campfire) hebt Spawn-Zahl nachweislich

> Hinweis A–H entspricht den Akzeptanz-Punkten aus Vertical-Slice-Plan §Phase 7. Die Items A, D, F sind pre-LIVE beweisbar (Tests + Compilation); B, C, E, G, H sind LIVE-Belege.

---

## 7. Akzeptanz-Gate

| Punkt | Beleg-Typ | Quelle |
|---|---|---|
| Determinismus | NUnit-Regression | `NightSpawnFormulaTests` |
| Incident-Registrierung | Def-Log + Companion | `IncidentDef`-Load |
| Maximaler Spawn | Live-Test | Runtime-Reports |
| Save/Load Konsistenz | Save-Inspect | `RimconemyNightComponent` |

---

## 8. Verweise

- `ROADMAP.md §9.1` §Phase 7
- `docs/vanilla-api-matrix-1.6.md` §IncidentWorker §MapComponent
- `docs/falsification/infected__InfectedRaid.md` (Schwesterbericht, Raid-Skalierung)
- `docs/falsification/infected__AutoResolve.md` (Auflösungs-Pfad)

---

## 9. Update 2026-08-05 — User-Sensorik-Anforderung + Tasklist

> **Quelle:** `docs/CHAT_PROTOCOL_2026-08-05.md` §1.3 · `ROADMAP.md §9.8` (T1–T9, kein Code geändert).

**User (2026-08-05):** „höhrere infizierten dichte die auf licht und im radius von X tiles hören + sichtweitee beschränken." — betrifft direkt dieses Gate (Phase 7).

- Die dokumentierte `NightSpawnFormula` (0.6 + exposure*1.4 + fireSignature*1.2 + threat*0.8) wird durch die neue pure `NightSensingFormula` (Tasklist T2) verschärft: zellbasiertes **Licht-Level** (T1-Spike, `Map.glowGrid`), **Hör-Radius X** (T5), **Sichtweite** (T6, `GenSight.LineOfSight`), **Dichte-Skalierung** (T7).
- Stubs aus §3/§4 (RimconemyNightComponent, IncidentWorker_NightInfected, NightSpawnFormulaTests) sind weiterhin **offen** — Tasklist T3/T4 sind die konkreten Implementierungs-Schritte.
- Akzeptanz-Punkte A–H bleiben gültig; A/D/F pre-LIVE beweisbar, B/C/E/G/H LIVE.
