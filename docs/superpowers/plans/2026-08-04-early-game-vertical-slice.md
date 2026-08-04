# Vertical Slice Plan — „Die erste Nacht"

> **For agentic workers:** Plan-Dokument, keine Implementierung. Spikephase wird pro Task durch lokale API-Verifikation gegen die RimWorld-1.6-Assembly (decompiliert oder reflektiert) abgeschlossen, bevor Code geschrieben wird.
>
> **Owner über alle Phasen:** Foundation (01) ist Vertrags-Owner für Capability-/Save- und DLC-Policy. Phasen-Eigentümer stehen pro Task.
>
> **Bezugsvertrag:** ROADMAP.md §2.5 + §2.6 (Early-Game-Vertikalscheibe und Erfahrungsbaum) sind die Rahmenentscheidung. Dieses Dokument ist die operative Umsetzung. Designentscheidungen stehen in DECISIONS.md §24/§25/§26.

## 0. Ziel und Release-Gate

Die Vertikalscheibe endet erst, wenn dieser Ablauf im Live-Spiel reproduzierbar funktioniert und ein Save/Load-Roundtrip ohne Drift überlebt:

```text
Single Survivor startet
  → sieht nur den Startbereich
  → findet Beeren und sammelt Holz
  → findet Stahlreste (zufällig)
  → baut Campfire, erhält Wärme und Licht
  → erhält KALT bei ungeschützter Kälte
  → Campfire erhöht Feuer-Signatur
  → baut Tier-1-Barrikade für 1 Holz + 1 Stahlrest
  → Nacht beginnt: genau ein kontrollierter Nachtspawn
  → Schutz und Feuer beeinflussen Spawn
  → Nacht endet
  → Erfahrung wird nur für echte Abschlüsse vergeben
  → Barrikaden-Rezept bleibt nach Save/Load freigeschaltet
```

Erst nach diesem `LIVE`-Beleg beginnen Kohle, T2-Energy und Automation.

## Technische Leitlinie (alle Phasen)

```text
XML/Defs:    statische Inhalte, Kosten, Rezepte, Gebäude, Pflanzen, IncidentDefs
C#:          dynamischer Zustand, Erfahrung, Freischaltungen, Sichtweite,
             Feuer-Signatur, Schutz, Nachtlogik, Save-State
Harmony:     nur wo Vanilla keinen Erweiterungspunkt bietet:
             Jobabschluss, Schussverbrauch, Architect-Visibility
```

Bestehende Paket-Eigentumsgrenzen sind unverändert: 01 Foundation, 02 Survival, 03 Scavenger, 04 Economy, 05 Infected.

## Phase 0 — API- und DLC-Grundlage

### Task 0.1 — Lokale Vanilla-API-Matrix

| Feld | Wert |
|---|---|
| Owner | Foundation (`mods/01/`) |
| Vanilla-Anker | `ScenarioBase`, `ScenPart`, `GameComponent`, `MapComponent`, `WorldComponent`, `ThingComp`, `IncidentWorker`, `RecipeWorker`, `Designator`, `GenSight`, `FogGrid`, `PawnGenerator`, `ResearchManager`, `CompRefuelable`, `CompGlower` |
| Ergebnis | `docs/vanilla-api-matrix-1.6.md` (neu) mit Klasse/Methode, Assembly, RimWorld-Version, Owner-Paket, DLC-Abhängigkeit, C#-Beispiel, Runtime-Gate |
| Abnahme | Kein späterer Task darf eine nicht geprüfte API-Signatur als sicher voraussetzen |

### Task 0.2 — DLC-Policy im Capability-System

```csharp
public static class RimconemyDlcCapabilities
{
    public const string Royalty  = "dlc.royalty";
    public const string Ideology = "dlc.ideology";
    public const string Biotech  = "dlc.biotech";
    public const string Anomaly  = "dlc.anomaly";
    public const string Odyssey  = "dlc.odyssey";
}
```

| Feld | Wert |
|---|---|
| Owner | Foundation |
| Anker | existierende `DLCFilter`/`CapabilityRegistry` |
| Regel | Core + Harmony reichen. Ideology liefert Precepts/Roles/Rituals. Biotech liefert Gene/Mech-Adapter. Anomaly liefert Shambler-Infizierte. Odyssey liefert Gravship-Weltkarte. Royalty liefert Psycast/Royalty-Integration. **Early-Survival-Loop ist unabhängig von Royalty, Biotech oder Odyssey**. |
| Wichtig | Die aktuelle `DLCContentPolicy_Default.xml`-Override (`mods/01/Defs/`) bleibt kompatibel, neue IDs werden an derselben Stelle geführt. |
| Vorhandener Code | `mods/01/Source/DLC/DLCContentPolicy.cs`, `Patches/DLC_Royalty_Suppress.xml`, `DLC_Biotech_Suppress.xml`, `DLC_Anomaly_KillEntitiesVoid.xml` bereits vorhanden. |

## Phase 1 — Szenario und Survivor

### Task 1.1 — Single-Survivor-Szenario auf `ScenarioDef`

| Feld | Wert |
|---|---|
| Owner | Survival (`mods/02/`) |
| Vanilla-Anker | `ScenarioDef`, `RimWorld.Scenario` (direkt; **`Verse.ScenarioBase` existiert in 1.6 NICHT** — siehe `docs/vanilla-api-matrix-1.6.md` §2 + §8 Pflicht-Lücken), `ScenPart_ConfigPage_ConfigureStartingPawns`, eigener `ScenPart_RimconemyStart` |
| Dateien | `mods/02/Defs/Scenarios/Rimconemy_SingleSurvivor.xml` (neu), `mods/02/Source/Scenarios/ScenPart_RimconemyStart.cs` (neu) |
| Existierend | `ScenPart_StartInSandbox` und `Rimconemy_SandboxScenario.xml` sind Sandbox-Pendant; Single-Survivor-Variante fehlt |
| Abnahme | genau ein Start-Survivor, Sandbox bleibt getrennt, Startzustand savebar, kein doppelter Start bei Reload oder Map-Init |

**Spike-Befund (2026-08-04):** `Verse.ScenarioBase` ist in der 1.6.4566-Assembly nicht enumeriert — RimWorld verwendet `RimWorld.Scenario` direkt ohne Basisklasse. Der Plan wird entsprechend von `ScenarioBase` auf `RimWorld.Scenario` umgestellt.

### Task 1.2 — Notwaffe und Startmunition definieren

| Feld | Wert |
|---|---|
| Owner | Survival |
| Vanilla-Anker | `ThingDef` (ParentName `BaseWeapon`), `VerbProperties`, `Verb_Shoot`, `ScenPart_StartingThing_Defined` |
| Dateien | `mods/02/Defs/ThingDefs/Weapons/Rimconemy_ScrapRifle.xml` (neu), `mods/02/Defs/Scenarios/StartingThings_Notwaffe.xml` (neu) |
| Wichtig | Realer Ammo-Verbrauch ist nicht automatisch Teil von `Verb_Shoot`; siehe Task 1.3 |
| Abnahme | Waffe erscheint im Charakterinventar und in der Startlisten-XML |

### Task 1.3 — Eigenen Ammo-State speichern

| Feld | Wert |
|---|---|
| Owner | Survival |
| Vanilla-Anker | `ThingComp`, `CompProperties`, `IExposable` |
| Dateien | `mods/02/Source/Combat/CompProperties_RimconemyAmmo.cs` (neu), `mods/02/Source/Combat/CompRimconemyAmmo.cs` (neu) |
| Hook-Strategie (bestätigt) | **Harmony-Prefix auf `Verse.Verb.TryStartCastOn`(LocalTargetInfo, bool, bool, bool, bool)** + **Postfix auf `Verse.Verb_Shoot.TryCastShot()`**. Vollständige Signaturen siehe `docs/vanilla-api-matrix-1.6.md` §4.1. Backup-Pfad: `Projectile.Launch(...)`. |
| Abnahme | jeder gültige Schuss reduziert Munition, leerer Zustand verhindert Schuss, Save/Load erhält Restmunition, CE-Installation verursacht keinen Fehler |

**Spike-Befund (2026-08-04):** drei Hook-Optionen verifiziert (TryStartCastOn/Verb_Shoot.TryCastShot/Projectile.Launch). Empfehlung: prefix+postfix auf Verb.TryStartCastOn+Verb_Shoot.TryCastShot, da das den CE-Pfad (Verb_Shoot-Override) und Vanilla-Pfad (Verb-Projektile) gleichermaßen abdeckt.

### Task 1.4 — Erste Gegner getrennt vom Nachtspawn erzeugen

| Feld | Wert |
|---|---|
| Owner | Infected (`mods/05/`) |
| Vanilla-Anker | `PawnGenerator`, `GenSpawn`, `ScenPart.PostMapGenerate` |
| Dateien | `mods/05/Source/Scenarios/ScenPart_RimconemyStartEnemies.cs` (neu), `mods/05/Source/Incidents/HiddenInfected_Provider.cs` (neu, capability-gated Read) |
| Existierend | `HiddenInfected`-Fraktion und `InfectedRavager` als DEF vorhanden; `InfectedRaidSpawnService.cs` bereits mit begrenzter 1-Pawn-Bridge |
| Wichtig | Startgegner werden **nicht** als Nachtspawn gezählt |
| Abnahme | Normalprofil: ein schwacher Gegner, hartes Profil: maximal zwei, kein doppelter Spawn, `RimconemyStartState` save-sicher |

## Phase 2 — Ressourcen und Campfire

### Task 2.1 — Stahlreste (`Rimconemy_SteelScraps`)

| Feld | Wert |
|---|---|
| Owner | Scavenger (`mods/03/`) |
| Vanilla-Anker | `ThingDef`, `ThingCategoryDef` (`ParentName="ResourceBase"`) |
| Datei | `mods/03/Defs/ThingDefs/Resources/Rimconemy_SteelScraps.xml` |
| Existierend | P0 Coal Chain (`Rimconemy_Coal`, `Rimconemy_MachineParts`) ist etabliert. SteelScraps sind als 5→1 Rezept-Input vorhanden, aber **als eigener ThingDef fehlt die explizite SteelScraps-Datei**. |
| Abnahme | echte Ressource, im Storage-Snapshot sichtbar, nicht mit Credits verwechselt, save-fähig |

### Task 2.2 — Startressourcen sichtbar und vorsichtig verteilen

| Feld | Wert |
|---|---|
| Owner | Scavenger; Foundation-Vertrag für Scenario-Pfad |
| Vanilla-Anker | `ScenPart.PostMapGenerate` oder eine kontrollierte `RimconemyStartResourceComponent : MapComponent`-Lösung |
| Datei | `mods/03/Source/Scenarios/RimconemyStartResourceComponent.cs` (neu) |
| Wichtig | Initialisierung in validiertem Map-Generation-Pfad ziehen, kein Tick-basiertes Spawnen |
| Abnahme | Ressourcen liegen im begrenzten Suchbereich, Save/Load erzeugt keine Duplikate |

### Task 2.3 — Campfire (`Rimconemy_Campfire`)

| Feld | Wert |
|---|---|
| Owner | Scavenger |
| Vanilla-Anker | `ThingDef` (ParentName `BuildingBase`), `CompRefuelable`, `CompGlower` |
| Datei | `mods/03/Defs/BuildingDefs/Rimconemy_Campfire.xml` |
| Existierend | Campfire-Def und Recipes (`BurnSteelScraps`, `MakeCoal`, `SalvageMachineParts`) sind im P0-Coal-Chain-Stand vorhanden |
| Abnahme | Campfire erzeugt Wärme und Licht, Brennstoff wird real verbraucht, Feuer kann ausgehen, Zustand im `BuildingSnapshotService` erkennbar |

### Task 2.4 — Campfire-Rezepte als erster Werkpfad

| Feld | Wert |
|---|---|
| Owner | Scavenger |
| Vanilla-Anker | `RecipeDef`, `recipeUsers → Rimconemy_Campfire` |
| Datei | `mods/03/Defs/RecipeDefs/Rimconemy_BurnSteelScraps.xml` u. a. |
| Existierend | Campbell-Rezepte sind im aktuellen Code wired; Balance-Check offen |
| Wichtig | Rezepte dürfen die erste Holz-Stahl-Barrikade nicht aushungern (Balance) |

## Phase 3 — KALT und Wärme

### Task 3.1 — KALT als Hediff (`Rimconemy_ColdExposure`)

| Feld | Wert |
|---|---|
| Owner | Survival |
| Vanilla-Anker | `HediffDef`, `HediffWithComps`, Severity-Stages mit `statOffsets` |
| Datei | `mods/02/Defs/HediffDefs/Rimconemy_ColdExposure.xml` |
| Stages | `fröstelnd` (0.25..0.6), `kalt` (0.6..1.0) |
| Wichtig | Vanilla-Mood und Kleidungslogik bleiben funktional; kein zweites Temperaturmodell |
| Abnahme | Feuer reduziert KALT, Nacht/Kälte erhöht KALT, kein Override der Vanilla-Thermoregulation |

### Task 3.2 — Kälte-Update als begrenzter Map-/Game-Tick

| Feld | Wert |
|---|---|
| Owner | Survival |
| Vanilla-Anker | lokale Temperature-API (1.6, **Spike-Pflicht BLOCKED**) |
| Datei | `mods/02/Source/Needs/ColdExposureService.cs` |
| **SPIKE-PFLICHT** | `GenTemperature`, `RoomTemperature`, `TemperatureAtCell`, `OutdoorTemperature` haben in der 1.6.4566-Assembly 0 Treffer. Heuristik-Sweep in `docs/vanilla-api-matrix-1.6.md` §4.2 dokumentiert. **TASK BLOCKED** bis erweiterte Spike-Heuristik (alternative Namen wie `GetTemperatureAt`, `TypedTemperatureAt`, `Cell.Temperature`) oder Decompile-Snapshot eine authentische 1.6-Signatur liefert. |
| Abnahme | bounded, kein Tick-Storm, save-stabil |

## Phase 4 — Tier-1-Barrikade

### Task 4.1 — Mischkosten-Barrikade (`Rimconemy_Tier1Barricade`)

| Feld | Wert |
|---|---|
| Owner | Scavenger |
| Vanilla-Anker | eigene `ThingDef` statt Vanilla-Stuff-Wand (Stuff-Pfad mit PatchOperation auf Vanilla-Def ist Alternative) |
| Datei | `mods/03/Defs/BuildingDefs/Rimconemy_Tier1Barricade.xml` |
| Kosten | `WoodLog=1`, `Rimconemy_SteelScraps=1` |
| Existierend | `Designator_BuildWallBauschutt.cs` und `BauschuttRemapApply.cs` decken den Designator-Pfad ab — Barrikade als kostengetreue Mischung ist neu |
| Abnahme | exakte Kosten beim Abschluss, Abbruch gibt Materialien zurück, `BuildableDef.costList` Roundtrip |

### Task 4.2 — Rimconemy-Architect-Kategorie

| Feld | Wert |
|---|---|
| Owner | Scavenger |
| Vanilla-Anker | `DesignationCategoryDef` |
| Datei | `mods/03/Defs/DesignationCategoryDefs/Rimconemy_Shelter.xml` |
| Initial | nur Tier-1-Barrikade freigeschaltet; Folge durch Experience-Unlocks aus Phase 9 |

## Phase 5 — Schutzwert und Feuer-Signatur

### Task 5.1 — `ShelterSnapshot` als Foundation-Vertrag

| Feld | Wert |
|---|---|
| Owner | Scavenger; Foundation-Vertrag |
| Datei | `mods/03/Source/Building/ShelterSnapshot.cs` |
| Felder | `MapId`, `FocusCell`, `EnclosureScore`, `LightScore`, `DefenseScore`, `FireSignature`, `ProtectionScore`, `SnapshotTick`, `ContentHash` |
| Wichtig | Werte 0..1 begrenzt; Determinismus; Capability-Gate für 05 (Threat-Reader) |
| Abnahme | Foundation-Vertrag in INTERFACE_CONTRACT §9 ergänzt; keine direkten 05→03-Schreibzugriffe |

### Task 5.2 — Feuer-Signatur aus aktiven Campfires

| Feld | Wert |
|---|---|
| Owner | Scavenger; Infected als Reader |
| Datei | `mods/03/Source/Building/FireSignatureComputer.cs` |
| Wichtig | **Spike-Pflicht:** `IsBurning`, Überdachung, Brennstoffstatus gegen Vanilla-/Comp-APIs der lokalen Assembly prüfen (1.6-Reflection/Decompile). Kein `strings`-Beweis. Mögliche Anker: `CompRefuelable.Fuel`/`FuelPercent`, `CompHeatPusher`/`CompGlower`-Aktivitätsmarker, `Map.emperature`/`RoomTemperature`, `Thing.Position.Roofed()`. |

## Phase 6 — Sicht- und Wachradius

### Task 6.1 — `WatchRadiusSnapshot`

| Feld | Wert |
|---|---|
| Owner | Survival |
| Datei | `mods/02/Source/Combat/WatchRadiusSnapshot.cs` |
| Anker | Pawn-Position, Map-Zellen, `GenSight.LineOfSight` (1.6 Spike-Pflicht) |
| Scope R1 | Wachradius für Warnungen, Ressourcenmarkierung, Infiziertenentdeckung |
| Kein Scope | vollständige Neuverdeckung sämtlicher Vanilla-Fog-Zellen |

### Task 6.2 — Gefahrenwarnung

| Feld | Wert |
|---|---|
| Owner | Infected (Trigger) + Survival (Reader) |
| Datei | `mods/05/Source/Story/CanWarnPlayer.cs` |
| Spike | `GenSight.LineOfSight(pawn.Position, infected.Position, pawn.Map)` exakte 1.6-Signatur vor Implementierung bestätigen |
| Wichtig | kein Override des RimWorld-Combat-LOS |

## Phase 7 — Nacht und Infizierten-Spawn

### Task 7.1 — Nacht-Scheduler als `MapComponent`

| Feld | Wert |
|---|---|
| Owner | Infected |
| Datei | `mods/05/Source/World/RimconemyNightComponent.cs` |
| Wichtig | maximal eine Auswertung pro Nacht, Save/Load erzeugt keinen zweiten Spawn, Mapwechsel bleibt getrennt |

### Task 7.2 — Schutz-/Feuerformel als pure Funktion

```csharp
public static int ComputeNightSpawnCount(
    int baseCount, float protection, float fireSignature,
    float threat, int nightIndex)
{
    float exposure    = 1f - Mathf.Clamp01(protection);
    float multiplier  = 0.6f + exposure * 1.4f + fireSignature * 1.2f + threat * 0.8f;
    int count         = Mathf.CeilToInt(baseCount * multiplier);
    if (nightIndex == 1) count = Mathf.Min(count, 1);
    if (nightIndex <= 3) count = Mathf.Min(count, 2);
    return Mathf.Max(0, count);
}
```

| Feld | Wert |
|---|---|
| Datei | `mods/05/Source/World/NightSpawnFormula.cs` |
| Wichtig | bewusst ohne RimWorld-Abhängigkeit testbar; gleicher Input → gleicher Output |
| Spike | `Tests/NightSpawnFormulaTests.cs` für deterministische Verifikation |

### Task 7.3 — Eigener `IncidentWorker`

| Feld | Wert |
|---|---|
| Owner | Infected |
| Dateien | `mods/05/Defs/IncidentDefs/Rimconemy_NightInfected.xml`, `mods/05/Source/Incidents/IncidentWorker_NightInfected.cs` |
| Existierend | `InfectedRaidWorker`-Pfad ist Letter-/Incident; echter Pawn-Spawn-Bridge ist neu |
| Abnahme | echter Pawn-Spawn, gültige Spawnzellen, keine Letter-only-Implementierung, Spawn-ID gespeichert, kein Doppelspawn bei Reload |

## Phase 8 — Progress by Doing

### Task 8.1 — Domain-XP-State

```csharp
public enum ProgressionDomain
{
    Survival, Salvage, Firecraft, Building, Processing, Machinery, Defense
}
```

| Feld | Wert |
|---|---|
| Owner | Survival |
| Datei | `mods/02/Source/Progression/DomainXpState.cs` |
| Existierend | `ProgressionGameComponent` mit `ExperiencePerWorkSample`-Konstante (F-Fix F2) und Sole-Owner-GameOver-Pfad |
| Wichtig | Persistenz via `Scribe_Collections.Look` mit stabiler LookMode-Kombination |

### Task 8.2 — Action-Result-Vertrag

```csharp
public sealed class ProgressionActionResult
{
    public string ActionKey;
    public ProgressionDomain Domain;
    public float BaseExperience;
    public string OutputDefName;
    public int OutputCount;
    public long CompletedTick;
}
```

| Feld | Wert |
|---|---|
| Owner | Foundation (Vertrag) + Survival (Eigentum) |
| Datei | `mods/02/Source/Progression/ProgressionActionResult.cs` (Vertrag und Speicherort in Survival; folgt bestehendem Muster von `ProgressionSnapshot.cs`/`BuildingProgressionAdapter.cs`; Foundation liest über Capability-Gate, schreibt nicht) |
| Beispiel-Schlüssel | `harvest:plantId:tick`, `build:thingId:completionTick`, `recipe:billId:outputHash`, `salvage:jobId:outputHash`, `night:defense:nightIndex` |
| Negativ-Beispiele (keine XP) | Bauplatz gesetzt, Job abgebrochen, Bill nur erstellt, Rezept geöffnet |

### Task 8.3 — Bauabschluss an XP anbinden

| Feld | Wert |
|---|---|
| Owner | Survival (Empfänger) + Scavenger (Trigger) |
| **SPIKE-PFLICHT** | `FrameCompleted`, `FinishBlueprint`, `InstallBlueprint` haben in der 1.6.4566-Assembly **0 Treffer**. Vor Implementierung: Spike-Search nach `Building.MakeFinished`, `Frame.CompleteConstruction` oder verwandten 1.6-API-Methoden. Bis dahin **TASK BLOCKED**. Fallback: Phase 8.4 Recipe-Hook. |
| Datei | `mods/02/Source/Progression/BuildingProgressionAdapter.cs` |
| Existierend | `BuildingProgressionAdapter` als deterministische, idempotente XP-Bridge ist im aktuellen Code vorhanden (siehe Job-Output-Hook-Status in CODE_STATUS §2) |
| Wichtig | Hook darf nicht aus Building-Snapshot rekonstruiert werden; einmaliger Output-Event |

### Task 8.4 — Rezeptabschluss an XP

| Feld | Wert |
|---|---|
| Owner | Survival + Scavenger |
| Hook (Spike-Pflicht) | `RecipeWorker.Notify_IterationCompleted`, `Bill_Production.Notify_IterationCompleted` oder `JobDriver_DoBill.MakeNewToils` |
| Datei | `mods/02/Source/Progression/RecipeCompletionBridge.cs` |
| Wichtig | nur nach echtem Output; Reading über `BuildingProgressionAdapter` zur Idempotenz |

## Phase 9 — Tech und Architect-Menü

### Task 9.1 — Freischaltmetadaten als `DefModExtension`

```csharp
public sealed class RimconemyUnlockExtension : DefModExtension
{
    public string domain;
    public int requiredLevel;
    public List<string> requiredActions;
}
```

| Feld | Wert |
|---|---|
| Owner | Survival |
| Datei | `mods/02/Source/Progression/Unlocks/RimconemyUnlockExtension.cs` |

### Task 9.2 — `UnlockService` zentral

| Feld | Wert |
|---|---|
| Owner | Survival |
| Datei | `mods/02/Source/Progression/Unlocks/UnlockService.cs` |
| Reader | Architect, RecipeWorker/Bill, WorkGiver, Repair, Rebuild, DLC-Adapter |
| Wichtig | dieselbe Service-Stelle; Vanilla-`ResearchProjectDef.IsFinished` bleibt Read-Model nebenbei |

### Task 9.3 — Dynamische Architect-Sichtbarkeit

| Feld | Wert |
|---|---|
| Owner | Survival + Scavenger |
| Datei | `mods/02/Source/Progression/Unlocks/Designator_BuildRimconemy.cs` |
| Spike | `Designator_Build.Visible`/`CanDesignateThing` exakte 1.6-Signatur bestätigen |
| Abnahme | Startmenü zeigt nur Notwendiges, Barrikade erscheint nach Freischaltung, gesperrte Inhalte nicht über Bills oder Reparatur umgehbar, Vanilla-/DLC-Architect bleibt erhalten |

### Task 9.4 — Erster Lernpfad

```text
Campfire fertig → Firecraft XP
Stahlreste geborgen → Salvage XP
Tier-1-Barrikade fertig → Building XP
erste Nacht überlebt → Survival XP

Baukunst Stufe 1 + CampfireBuilt + SteelScrapRecovered
  → Tier-1-Barrikade sichtbar

Freischaltungen:
  Campfire: Start
  Tier-1-Barrikade: CampfireBuilt + Stahlreste
  Tür: Building Stufe 2 + Barrikade fertig
  Feuerabdeckung: Firecraft Stufe 2 + Tür gelernt
  Kohle: Firecraft Stufe 2 + Holz/Hanf verfügbar
  Machine Parts: Salvage Stufe 2
  Generator: Processing Stufe 2 + CoalMade + MachinePartsMade
```

## Phase 10 — Mid Game: Produktion

### Task 10.1 — Kohlekette

| Feld | Wert |
|---|---|
| Owner | Scavenger |
| Status | P0 Coal Chain (`Rimconemy_Coal`, `Rimconemy_MachineParts`, `Rimconemy_MakeCoal`, `Rimconemy_SalvageMachineParts`) ist codeseitig vorhanden — Live-Beleg bleibt offen |
| Spike | MakeCoal-Durchlauf, Generator-Effizienz, Save/Load der neuen Ressourcen |

### Task 10.2 — Machine-Parts-Kette

| Feld | Wert |
|---|---|
| Owner | Scavenger |
| Wichtig | Credits ersetzen weder Stahlreste noch Maschinenteile |

### Task 10.3 — Energie als harte Produktionsbedingung

| Feld | Wert |
|---|---|
| Owner | Scavenger (`PowerChainService`) |
| Datei | `mods/03/Source/Power/PowerChainService.cs` |
| Existierend | `BuildingSnapshot`/`PowerChainSnapshot` mit `BuildingPowerState`-Enum und `Online`/`Offline`/`Blocked`-Marker |
| Wichtig | Zustände `Active`, `Blocked`, `Offline`, `Damaged` aus den vorhandenen Pfaden wiederverwenden, kein paralleler Def |

### Task 10.4 — Erste Automation

| Feld | Wert |
|---|---|
| Owner | Infected |
| Wichtig | erste Aufgabe klein halten: Campfire-Brennstoff oder Werkstatt→Lager-Transport, **kein** autonomer Koloniebetrieb |

## Phase 11 — Economy und 4X

### Task 11.1 — Vorposten-Gate

```text
stabile Zuflucht + aktive Energie + erste Automation + überschüssige Produktion
  → Vorpostenplanung
```

| Feld | Wert |
|---|---|
| Owner | Economy (`mods/04/`) |

### Task 11.2 — Outpost-Gründung mit physischen Kosten

```text
Bauschutt + Nahrung + Machine Parts + Credits → Vorposten
```

| Feld | Wert |
|---|---|
| Owner | Economy + Scavenger |
| Wichtig | Credits bleiben Wallet; physische Materialien bleiben Things |

### Task 11.3 — Proxy-Graph und Versorgung

```text
Zuflucht → Handels-/Versorgungsroute → Vorposten

Connected / Blocked / Disconnected / Ruined
```

### Task 11.4 — 4X-Bedrohung

```text
mehr Territorium
  → mehr Produktion + mehr Handelswert
  → längere Wege + mehr Infektionsdruck + mehr Verteidigungsbedarf
```

## Phase 12 — Vanilla-Research und DLC-Kompatibilität

### Task 12.1 — Vanilla-ResearchManager nicht zerstören

| Feld | Wert |
|---|---|
| Owner | Foundation |
| Regel | Vanilla-`ResearchManager` bleibt aktiv für DLC-Content, fremde Mod-Rezepte, Vanilla-Kompatibilität. Rimconemy-Content verwendet eigene Freischaltgates (`UnlockService`). Kein globales Auto-Complete aller ResearchProjects. Kein Ersetzen des Vanilla-Managers. Kein Ausblenden fremder Forschung. |
| Datei | `mods/01/Source/Research/ResearchCompatibilityLayer.cs` |
| Existierend | `ProgressionGameComponent.UpdateResearchCapabilities` als Legacy-Read-Model ist im aktuellen Code — organischer Experience-Pfad ergänzt diese. |

### Task 12.2 — DLC-Fallbacks

| DLC | Bei vorhanden | Bei nicht vorhanden |
|---|---|---|
| Royalty | Royalty-Recipes/Buildings erhalten zusätzliche Freischaltungen | Core-Freischaltung bleibt funktional |
| Ideology | `PreceptDef`, `RoleDef`, `ThoughtDef`, `RitualDef` | Thought-/Mood-/Story-Fallback |
| Biotech | Mech-/Gene-/Biotech-Adapter optional | Arbeitsmaschinen als Rimconemy-eigene Systeme |
| Anomaly | Shambler-basierte Infizierte | Rimconemy-eigener PawnKind/Faction-Fallback |
| Odyssey | Odyssey-World-/Gravship-/Map-Adapter | Vorposten/Weltkarte bleiben Core-System |

## Empfohlene Reihenfolge (Sprint-Sicht)

```text
0.1 API-/DLC-Matrix            (Foundation)
0.2 Capability-/DLC-Policy       (Foundation)
1.1 ScenarioBase-Single-Survivor (Survival)
1.2 Notwaffe                     (Survival)
1.3 Ammo-State                   (Survival)
1.4 Startgegner                  (Infected)
2.1 Stahlreste                   (Scavenger)
2.2 Startressourcen              (Scavenger)
2.3 Campfire                     (Scavenger)
2.4 Campfire-Rezepte             (Scavenger)
3.1 KALT-Hediff                  (Survival)
3.2 Temperatur-Update            (Survival)
4.1 Tier-1-Barrikade             (Scavenger)
4.2 Architect-Kategorie          (Scavenger)
5.1 ShelterSnapshot              (Scavenger + Foundation)
5.2 FireSignature                (Scavenger)
6.1 Wachradius                   (Survival)
6.2 Gefahrenwarnung              (Infected)
7.1 Nacht-Scheduler              (Infected)
7.2 Spawnformel                  (Infected)
7.3 IncidentWorker Nachtinfiziert(Infected)
8.1 Domain-XP                    (Survival)
8.2 ActionResult-Vertrag         (Foundation + Survival)
8.3 Bauabschluss-XP              (Survival + Scavenger)
8.4 Rezeptabschluss-XP           (Survival + Scavenger)
9.1 UnlockExtension              (Survival)
9.2 UnlockService                (Survival)
9.3 Architect-Gate               (Survival + Scavenger)
9.4 erster Lernpfad              (Survival)
10.1 Kohlekette                  (Scavenger)   — Live-Beleg offen
10.2 Machine-Parts-Kette         (Scavenger)
10.3 Power-Gate                  (Scavenger)
10.4 erste Automation            (Infected)
11.1 Outpost-Gate                (Economy)
11.2 Outpost-Kosten              (Economy + Scavenger)
11.3 Proxy-/Versorgungsgraph     (Economy + Scavenger)
11.4 globale Threat-Skalierung   (Infected)
```

Erster akzeptabler Release (`Die erste Nacht`) ist erreicht, sobald Phase 0..7.3 inkl. Phase 8.1..8.4 und Phase 9.1..9.4 ein **LIVE**-Save/Load-Beleg vorliegen. Phase 10..12 sind Folge-Sprints.

## Drift-Acknowledgement (gemäß CANONICAL_VANILLA_DOMAIN_MAP §8)

Mehrere Sub-tasks dieses Plans führen neue `ThingDef`s ein (`Rimconemy_Campfire`, `Rimconemy_Tier1Barricade`, ggf. weitere). Das ist exakt das Drift-Muster, das CANONICAL_VANILLA_DOMAIN_MAP §8 für `Rimconemy_WoodCoalGenerator`, `Rimconemy_Bauschutt` und `Rimconemy_Hemp*` als migrationskandidig markiert. Verbindlich vor Implementierung gilt:

1. **Eigene Defs sind begründungspflichtig.** Wenn RimWorld keine passende Vanilla-Basis hat (z.B. Campfire als Feuerwärme-Quelle für Phase 3), ist ein eigener Def begründet. In diesem Fall wird die Datei in der zugehörigen Liste des Domain-Map §8 mit Begründung dokumentiert.
2. **Stuff-/Patch-Strategie hat Vorrang** bei Bauschutt/Wand/Tür. Die existierende `Bauschutt_Remap_Patches.xml` zusammen mit `stuffProps.Stony` in `ConstructionDebris.xml` nutzt den Vanilla-Stuff-Mechanismus und ist *nicht* durch Phase 4.1 zu ersetzen, sondern zu ergänzen. Phase 4.1 (Tier-1-Barrikade) führt *eine eigene* Def ein, weil die Mischkosten `WoodLog=1 + SteelScraps=1` von Vanilla-Stuff-Definitionen nicht abgedeckt sind — diese Barrikade ist funktional eigenständig (1 Holz + 1 Stahlrest) und nicht Teil der `Bauschutt_Remap_Patches.xml`-Stuff-Logik.
3. **Migrations-Fahrplan** für spätere Phasen: PatchOperation auf Vanilla-`ThingDef`s + DefModExtension, sobald ein passender Vanilla-Anker existiert (z.B. Sandbag-Variante für Tier-1-Barrikade in Phase 5+).

## Disambiguation der drei Nacht-Pfade

Der Plan führt drei parallele Nacht-/Story-Pfade ein. Ihre Verantwortung ist eindeutig getrennt:

| Pfad | Owner | Aufgabe | Vertrag |
|---|---|---|---|
| `StoryDirector` (StoryEventCatalog + StorySelector) | 05 | tägliche StoryEvent-Auswahl basierend auf SituationSnapshot | StoryEventSpec-Catalog + Selectable Family |
| `InfectedRaidSpawnService` / `InfectedRaidWorker` | 05 | aktiver Raid-Path mit Brief und begrenzter 1-Pawn-Bridge | IncidentWorker + Brief + SpawnPlan |
| `RimconemyNightComponent` (Phase 7.1) | 05 | bounded Night-Auswertung, maximale eine Inkarnation pro Nacht | MapComponent + letzteAusgewerteteNacht-Index |

Disambiguation: `RimconemyNightComponent` ist die *Tier-1-Druck-Schicht* der Vertikalscheibe. Sie prüft pro lokalem Karenzraum einmal pro Nacht, ob Schutz-/Feuer-Schwellen einen begrenzten Spawn zulassen. Sie ist *kein* StoryDirector-Ersatz und *kein* Brief-/Incident-Pfad. Die Spawn-Anzahl wird über die pure Funktion `NightSpawnFormula.ComputeNightSpawnCount` (Phase 7.2) berechnet. Erweiterungen (T2+ / mehrere Pawns / Brief-Pfad) folgen erst nach Live-Beleg der Vertikalscheibe.

## Bezug zu vorhandenen Arbeitsständen (Status-Sync 2026-08-04)

Dieser Plan spiegelt die aktuelle Code-Realität aus `docs/CODE_STATUS.md` und den Paket-Roadmaps wider. Bereits vorhanden:

- `BauschuttRemapApply`, `Designator_BuildWallBauschutt`, `Bauschutt_Remap_Patches.xml` (Phase 1-Analog-Pfad)
- `BuildingProgressionAdapter` mit deterministischer Idempotenz (Phase 8.3-Vorstufe)
- `StorageWriteMutationService` mit best-effort Storage-Abzug (Phase 1.4-Analog)
- `InfectedRaidSpawnService` mit 1-Pawn-Bridge und `HiddenInfected`/`InfectedRavager` als DEF (Phase 7.3-Vorstufe)
- `CoalChainRegressionTests`, `CampfireScrapsRegressionTests` als fertige P0-Regression (Phase 2.3/2.4-Beleg)
- `BuildingThreatRegressionTests` als Phase 5/7-Druckmodell-Vorstufe

Offen für `LIVE`-Beleg (Vertikal-Scheibe):

- SingleSurvivor-Szene mit eigener `ScenPart_RimconemyStart`/`ScenPart_RimconemyStartEnemies`
- Eigener Ammo-Tank mit Save/Load-Roundtrip (Phase 1.3)
- Stahlreste als ThingDef (Phase 2.1)
- `Rimconemy_ColdExposure`-Hediff mit bounded Map-/Game-Tick (Phase 3.1/3.2)
- Tier-1-Barrikade mit Mischkosten (Phase 4.1)
- `IncidentWorker_NightInfected` mit echtem Pawn-Spawn (Phase 7.3, Erweiterung der bestehenden Bridge)
- organische Architektenfreigaben via Domain-XP (Phase 8-9)

## Querverweise

- `ROADMAP.md` §2.5 (Early-Game-Vertikalscheibe), §2.6 (Erfahrungsbaum), §2.7 (Vertikal-Scheiben-Plan), §8.6 (Phase 6 Paket-Gameplay)
- `mods/02-Rimconemy-Survival-Progression/ROADMAP.md` Task 2.1..2.8
- `mods/03-Rimconemy-Scavenger-Infrastructure/ROADMAP.md` Task 3.1..3.10
- `docs/DECISIONS.md` §24 (Early-Game-Munition), §25 (Erfahrungsbaum statt Forschungsbaum), §26 (KALT als Hediff-Severity-Offset), §21 (Harmony-Strategie als Anker-Hierarchie)
- `docs/CANONICAL_VANILLA_DOMAIN_MAP.md` §2.4 (Tech/Wissen), §8 (Drift-Acknowledgement)
- `docs/INTERFACE_CONTRACT.md` §9 (Architecture Boundaries, Owner-Map)
- `docs/CODE_STATUS.md` §2 (Paketstatus), §4 (Live-Gates)
- `docs/superpowers/plans/2026-08-04-building-core-implementation-plan.md` (Format-Vorbild)
- `docs/falsification/survival__SaveMigration.md` (Falsifizierungsbericht-Format)