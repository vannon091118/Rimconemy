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

## Frage 3: Save-Migration — was passiert mit alten Cassandra-Saves? (KORRIGIERT 2026-08-07)

> ⚠️ **User-Korrektur:** Der ursprüngliche "sanfte Migration"-Ansatz wurde verworfen. Rimconemy ist ein Total-Overhaul mit Anpassung ALLER Systeme. Vanilla-Save-Kompatibilität würde uns zu sehr einschränken.

### Entscheidung: Clean Break — keine Migration, neues Spiel erforderlich

**Spieler-Flow:**

```
1. Spieler installiert Rimconemy
2. Beim Start: Dialog "Rimconemy ist ein Total-Overhaul.
   Alte Spielstände sind NICHT kompatibel.
   Bitte erstelle ein Backup deiner Saves vor dem ersten Start.
   [Backup-Ordner öffnen] [Ich verstehe, neues Spiel starten]"
3. Load-Game-Screen: Alte Saves werden ausgegraut mit
   Warnung "Inkompatibel — benötigt Rimconemy v2+ Save-Format"
4. Nur "Neues Spiel" ist möglich
```

**Technische Umsetzung:**

```csharp
// RimconemyStorytellerComp.FinalizeInit()
if (Find.Storyteller.def.defName != "Rimconemy_Storyteller")
{
    // Kein Migrationsversuch — harter Abbruch mit klarer Meldung
    Log.Error("[Rimconemy] Incompatible save detected. " +
        "Rimconemy requires a new game. " +
        "Please back up your old saves before starting.");
    
    // Zeige Dialog im Hauptmenü
    Find.WindowStack.Add(new Dialog_RimconemyIncompatibleSave());
    
    // Verhindere dass das Spiel weiterläuft
    Current.Game = null; // force return to main menu
    return;
}
```

**Was das für uns bedeutet (positiv):**

| Freiheit | Warum |
|----------|-------|
| **Keine Schema-Migration v0→v2** | Alte StoryState/PopulationLedger-Daten existieren nicht — kein Migration-Code nötig |
| **Keine Altlasten** | Vanilla-DifficultyDefs, IncidentQueue, Cooldowns — alles irrelevant |
| **Def-Database sauber** | Keine DLC-Incident-Reste die "noch da sind weil der Save alt ist" |
| **Save-Format kann radikal anders sein** | Neue Felder, andere Scribe-Struktur — kein `LookMode.Undefined`-Fallback |
| **Kein "was wenn"-Support** | Keine Bugreports à la "mein Cassandra-Save crashed nach 3 Stunden" |

**Backup-Disclaimer (im Launcher/Workshop):**

> ⚠️ **Rimconemy ist ein Total-Overhaul.**  
> Alte Spielstände (Vanilla oder mit anderen Storytellern) sind **nicht kompatibel**.  
> **Bitte sichere deine Saves** vor der Installation:  
> `C:\Users\[Name]\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Saves`  
> Rimconemy benötigt ein **neues Spiel**.

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
| Save-Migration | **Clean Break** — keine Migration, neues Spiel erforderlich, Backup-Disclaimer | `FinalizeInit()` erkennt Fremd-Save → Error-Dialog → Return to Main Menu. Kein Migrations-Code. Maximale Freiheit für Save-Format v2. |
| Mod-Kompatibilität | **Akzeptanz + Doku** — kein Harmony-Fake | COMPATIBILITY_MATRIX.md + Bootstrap-Warnung + Kommunikation. |

---

## Change Log

| Date | Change | Author |
|------|--------|--------|
| 2026-08-07 | Alle 4 offenen Fragen geklärt, implementierungsbereit | Buffy (Freebuff) |
| 2026-08-07 | **Korrektur Q3:** User-Entscheidung — "sanfte Migration" verworfen, Clean Break mit Backup-Disclaimer. Total-Overhaul braucht keine Altlast-Kompatibilität. | Buffy (Freebuff) |
