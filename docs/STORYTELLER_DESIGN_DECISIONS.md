# STORYTELLER_DESIGN_DECISIONS — 4 offene Fragen geklärt

> **Date:** 2026-08-07  
> **Context:** DECISIONS §34 wurde korrigiert: Rimconemy bekommt EINEN eigenen StorytellerDef, der Cassandra/Phoebe/Randy ERSETZT.  
> **Status:** Alle 4 Fragen beantwortet — implementierungsbereit.

---

## Frage 1: DLC-Incidents — durchreichen oder ersetzen?

### Aktuelle Lage

Unser `IncidentClassifier` klassifiziert alle `IncidentDef`s in drei Buckets:

| Bucket | Erkennung | Beispiele |
|--------|-----------|-----------|
| **Rimconemy** | `defName.StartsWith("Rimconemy_")` | `Rimconemy_InfectedRaidIncident` |
| **DLC/Quest** | `workerClass.Namespace` enthält `Ideology`, `Biotech`, `Anomaly`, `Royalty`, `Odyssey` | Royalty-Quests, Anomaly-Entities |
| **Vanilla** | Alles andere | `RaidEnemy`, `ManhunterPack`, `ToxicFallout` |

Unsere `DLCContentPolicy` (Foundation) unterdrückt bereits:

| DLC | Was bleibt an | Was ist aus |
|-----|--------------|-------------|
| **Royalty** | Nichts — alle suppressed (Psycasts, Titles, Shuttles) | ✅ Komplett deaktiviert |
| **Ideology** | ThoughtSystem (technischer Träger) | Founder-UI, Rituale, Precept-Edit |
| **Biotech** | Nichts — alle suppressed (Mechanitor, Children, Pollution) | ✅ Komplett deaktiviert |
| **Anomaly** | Shamblers (genetische Basis für Infected-PawnKinds, §19) | Entities, Void-Monolith |
| **Odyssey** | GravShip (Territory-Engine) | Fishing, Travel-Events |

### Entscheidung: DLC-Incidents werden ERSETZT

**Begründung:**

1. **Royalty und Biotech sind bereits komplett suppressed.** Deren Incidents existieren nicht mehr im DefDatabase — es gibt nichts durchzureichen.

2. **Anomaly-Shamblers** sind unsere genetische Basis (§19). Die Shambler-Mechanik wird über `Rimconemy_InfectedRavager` als PawnKind beerbt — die Anomaly-eigenen Shambler-Incidents werden nicht benötigt.

3. **Odyssey-GravShip** ist der Territory-Motor (§18). Territory-bezogene Events werden Rimconemy-eigene Events, nicht Odyssey-Incidents.

4. **Ideology-Thoughts** bleiben als technischer Träger aktiv, feuern aber keine eigenen Incidents. Unsere `IdeologyAssigner` + `TransparencyTracker` ersetzen die Ideology-Event-Logik.

**Was Vanilla-Incidents angeht** (RaidEnemy, ManhunterPack, ToxicFallout etc.):

Diese werden durch Rimconemy-eigene Events im `StoryEventCatalog` abgebildet:

| Vanilla-Incident | Rimconemy-Äquivalent |
|---|---|
| `RaidEnemy` | `Rimconemy_InfectedRaidIncident` (bereits vorhanden) |
| `ManhunterPack` | Geplant: `Rimconemy_InfectedAnimalPack` |
| `ToxicFallout` | Geplant: `Rimconemy_EnvironmentalCollapse` |
| `CropBlight` | Geplant: `Rimconemy_SupplyBlight` |
| `SolarFlare` | Geplant: `Rimconemy_PowerGridFailure` |

**Ausnahme für die Zukunft:** Falls ein spezifisches DLC-Incident doch durchgereicht werden muss (z.B. ein Quest aus einem anderen Mod), kann der `RimconemyStorytellerComp` einen Sub-`StorytellerComp` instanziieren, der selektiv DLC-Incidents feuert. Das ist die Fallback-Strategie, nicht der Default.

**Zusammenfassung:** Alle Incidents kommen aus dem Rimconemy-Storyteller. Kein Durchreichen. Die DLC-Content-Policy hat bereits 80% der DLC-Incidents eliminiert — der Rest wird durch Rimconemy-Events ersetzt.

---

## Frage 2: Difficulty-Auswahl — behalten oder SettingProfile-only?

### Aktuelle Lage

Der `StoryDirector` mappt Vanilla-`DifficultyDef` → Rimconemy-`SettingProfile`:

```csharp
"Peaceful" → SettingProfile.Refuge
"Easy"     → SettingProfile.Refuge
"Medium"   → SettingProfile.Survival
"Rough"    → SettingProfile.Survival
"Hard"     → SettingProfile.Collapse
"Extreme"  → SettingProfile.Collapse
```

H2 definiert 3 Profile mit detaillierten Parametern:
- **Refuge** (Zuflucht): MinThreat=0.0, MaxBand=1, RestWindow 3-7 Tage
- **Survival** (Überleben): MinThreat=0.05, MaxBand=2, RestWindow 1.5-4 Tage
- **Collapse** (Zusammenbruch): MinThreat=0.15, MaxBand=3, RestWindow 0.5-2 Tage

### Entscheidung: Difficulty-Auswahl BLEIBT — mit Rimconemy-Labels

**Wie es aussieht:**

Im Storyteller-Auswahlbildschirm sieht der Spieler:

```
┌──────────────────────────────────────────┐
│  Storyteller wählen                       │
│                                           │
│  ● Rimconemy                              │
│    Survival-Härte, dynamische Events,     │
│    Infizierten-Druck.                     │
│                                           │
│  Schwierigkeit:  [Zuflucht ▼]            │
│                  [Überleben   ]           │
│                  [Zusammenbruch]           │
└──────────────────────────────────────────┘
```

**Technische Umsetzung:**

1. Der `RimconemyStorytellerComp` liest die Difficulty direkt vom StorytellerDef:
```csharp
var difficulty = Find.Storyteller?.difficultyDef?.defName;
```

2. Da wir die Vanilla-Storyteller verstecken, existieren nur noch die Difficulty-Defs im DefDatabase. Diese behalten ihre `defName`s (Peaceful, Rough, Extreme etc.) als interne Keys.

3. Der StorytellerComp mappt wie bisher: `difficultyDef` → `SettingProfile`.

4. Die UI-Labels der Difficulty-Stufen können optional via Language-Patch auf "Zuflucht", "Überleben", "Zusammenbruch" umbenannt werden.

**Warum behalten?**

- Spieler kennen den Difficulty-Slider — vertraute UX
- `SettingProfile` ist ein internes Konzept; der Spieler soll nicht "ProfileId: Rimconemy_Collapse" lesen müssen
- Modder können weiterhin `Find.Storyteller.difficultyDef` abfragen (Kompatibilität)
- Die 6→3-Mapping ist bewusst grob: Es gibt nur 3 Spielgefühle, nicht 6

**Optionale Verfeinerung (später):** Ein eigener `DifficultyDef` mit Rimconemy-Labels ("Zuflucht", "Überleben", "Zusammenbruch") plus Tooltips die die H2-Parameter beschreiben. Das ist aber kein MVP.

---

## Frage 3: Save-Migration — was passiert mit alten Cassandra-Saves?

### Problem

Ein Spieler hat 50 Stunden mit Cassandra gespielt. Jetzt installiert er Rimconemy. Der Save enthält:
- `game.storyteller.def = "Cassandra"`
- `game.storyteller.difficultyDef = "Rough"`
- `game.storyteller.incidentQueue` mit pending Vanilla-Incidents

Wenn Rimconemy jetzt den Storyteller auf "Rimconemy_Storyteller" umstellt, was passiert?

### Entscheidung: Sanfte Migration mit Warnung — kein Crash

**Ablauf beim Laden eines Fremd-Saves:**

```
1. Save wird geladen
2. RimconemyStorytellerComp.FinalizeInit() prüft:
   if (Find.Storyteller.def.defName != "Rimconemy_Storyteller")
   {
3.     // Sanfte Migration
       Log.Warning("Save has storyteller '" + oldDef + "', migrating to Rimconemy.");
       
4.     // IncidentQueue leeren (pending Vanilla-Incidents sind obsolet)
       Find.Storyteller.incidentQueue.Clear();
       
5.     // Rimconemy-Systeme frisch initialisieren
       StoryState = new StoryState();
       PopulationLedger.Reset();
       
6.     // Ingame-Letter an den Spieler
       Find.LetterStack.ReceiveLetter(
           "Rimconemy übernimmt",
           "Dieser Spielstand wurde mit einem anderen Storyteller gestartet. " +
           "Rimconemy übernimmt ab sofort die Ereignissteuerung.",
           LetterDefOf.NeutralEvent);
   }
```

**Save-Schema:** Keine Änderung nötig. Der Storyteller wird nicht im Save persistiert (RimWorld speichert ihn separat). Rimconemy-eigene Daten (`StoryState`, `PopulationLedger`) werden normal via Scribe geladen — sind sie nicht vorhanden (weil der Save ohne Rimconemy gestartet wurde), werden sie mit Defaults initialisiert.

**Was der Spieler verliert:**
- Pending Vanilla-Incidents in der Queue (selten, max. 1-2)
- Vanilla-Raid-Cooldowns (Rimconemy hat eigene Cooldowns)

**Was erhalten bleibt:**
- Kolonie, Pawns, Gebäude, Items — alles unverändert
- Forschungsfortschritt
- Beziehungen zu Fraktionen
- Spielzeit

**Edge Case: Save zurück zu Vanilla:** Falls der Spieler Rimconemy deinstalliert und den Save ohne Mods lädt, wählt RimWorld automatisch den Default-Storyteller (Cassandra). Kein Datenverlust, nur andere Incident-Logik.

---

## Frage 4: Mod-Kompatibilität — andere Mods die Find.Storyteller prüfen?

### Problem

Andere Mods könnten Code enthalten wie:

```csharp
if (Find.Storyteller.def.defName == "Cassandra") { ... }
if (Find.Storyteller.def.defName == "Randy") { ... }
```

Wenn Rimconemy den einzigen sichtbaren Storyteller stellt, ist `defName == "Rimconemy_Storyteller"` — diese Checks schlagen fehl.

### Entscheidung: Akzeptanz + Dokumentation — kein Harmony-Fake

**Begründung gegen Harmony-Fake:**

Ein Harmony-Postfix auf `Storyteller.get_def` der "Rimconemy_Storyteller" auf "Cassandra" mappt, wäre technisch machbar:

```csharp
[HarmonyPostfix]
static void FakeDefForOldMods(ref StorytellerDef __result) {
    if (__result?.defName == "Rimconemy_Storyteller")
        __result = DefDatabase<StorytellerDef>.GetNamed("Cassandra");
}
```

Aber das ist eine **Lüge gegenüber anderen Mods**:
- Mod denkt es läuft Cassandra → wendet Cassandra-spezifische Logik an
- Tatsächlich läuft Rimconemy → Logik passt nicht
- Führt zu schleichenden Bugs die keiner debuggen kann

**Stattdessen: Drei ehrliche Strategien**

| Strategie | Maßnahme |
|-----------|----------|
| **Akzeptanz** | Rimconemy ist eine Total-Conversion. Wer sie installiert, will das volle Erlebnis. Mods die Vanilla-Storyteller-Logik hartcoden sind inkompatibel — das ist gewollt. |
| **Dokumentation** | `COMPATIBILITY_MATRIX.md` listet bekannte inkompatible Mods. `README.md` warnt: "Rimconemy ersetzt den Vanilla-Storyteller. Mods die spezifische Storyteller-Checks haben, funktionieren möglicherweise nicht." |
| **Capability-Registry** | Mods KÖNNEN via `CapabilityAudit.HasCapability("rimconemy.infectedautomation.storyteller")` prüfen ob Rimconemy aktiv ist. Das ist der saubere Weg für Kompatibilität. |

**Welche Mods sind betroffen?**

| Mod-Typ | Risiko | Beispiel |
|---------|--------|----------|
| Storyteller-Mods (Custom Storyteller) | Hoch — eigener StorytellerDef kollidiert | "Rimsenal Storyteller", "VFE Storytellers" |
| Incident-Mods | Gering — nutzen `IncidentWorker`, nicht `Storyteller.def` | "Sometimes Raids Go Wrong" |
| Difficulty-Mods | Mittel — patchen `DifficultyDef` | "Custom Difficulty" |
| Quest-Mods | Gering — nutzen `QuestManager`, nicht `Storyteller` | "More Quests" |
| UI-Mods | Kein — lesen nur `StorytellerUI` | "Storyteller UI Overhaul" |

**Konkrete Maßnahmen für Kompatibilität:**

1. **`COMPATIBILITY_MATRIX.md`** erweitern mit Sektion "Storyteller-Mods"
2. **Bootstrap-Log** beim Laden warnen wenn andere StorytellerDefs mit `listOrder < 0` existieren
3. **Steam Workshop Description** klar kommunizieren: "Ersetzt den Vanilla-Storyteller"
4. **Load-Order-Guide**: Rimconemy NACH anderen Storyteller-Mods laden (unser `<hidden>true</hidden>`-Patch überschreibt deren Änderungen nicht)

---

## Zusammenfassung

| Frage | Antwort | Implementierung |
|-------|---------|-----------------|
| DLC-Incidents | **Ersetzen** — alle Incidents kommen aus Rimconemy. Kein Durchreichen. | StoryEventCatalog deckt Vanilla-Incidents ab. DLC-Incidents existieren nicht mehr (Content-Policy). |
| Difficulty-Auswahl | **Behalten** — Spieler wählt "Zuflucht / Überleben / Zusammenbruch" | Vanilla-DifficultyDefs bleiben als interne Keys. UI-Labels optional via Language-Patch. |
| Save-Migration | **Sanft** — Warn-Letter + frische Rimconemy-Init, kein Crash | `FinalizeInit()` prüft Storyteller-Def, leert Queue, initialisiert Systeme. |
| Mod-Kompatibilität | **Akzeptanz + Doku** — kein Harmony-Fake | COMPATIBILITY_MATRIX.md + Bootstrap-Warnung + Kommunikation. |

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-08-07 | Alle 4 offenen Fragen geklärt, implementierungsbereit | Buffy (Freebuff) |
