# H2 — Story-Vertrag: Profile, Events, Gewichtungen

> **Owner:** Research/Design (kein Code)
> **Status:** `CODE + SPEC` — Profile, Eventkatalog, deterministische Auswahl und StoryState sind im Code vorhanden; konkrete Balancewerte, vollständige Effekte und Live-Save-/Event-Gates bleiben offen.
> **Referenz:** [ROADMAP.md §2](../ROADMAP.md#2-produktentscheidung-für-phase-1), [ROADMAP.md §8.3](../ROADMAP.md#83-phase-12--story-writer--setting-ideologie-offen)

## Zweck

Die drei Difficulty-Profile und drei MVP-Events aus ROADMAP.md §2.2/2.3 erhalten **konkrete Zahlenwerte, Gewichtungen, Cooldowns und Folgeeffekte**. Diese Werte sind der Ausgangspunkt für die deterministische Auswahlfunktion in Phase 1. Sie werden erst nach dem Balance-Gate (User-Test) verhärtet.

---

## 1. Difficulty-Profile — Konkrete Regeln

### Profil: `Rimconemy_Refuge` (Zuflucht)

| Parameter | Wert | Erklärung |
|---|---|---|
| `ProfileId` | `Rimconemy_Refuge` | stabile ID |
| `ProfileVersion` | `1` | Schema-Version |
| `Label` | `Zuflucht` | UI-Name |
| `Description` | `Aufbau einer kleinen Zuflucht. Krisen sind selten und werden früh angekündigt. Konflikte lösen sich überwiegend durch Dialog.` | UI-Beschreibung |
| `MinThreatLevel` | `0.0` | kein Druckminimum |
| `MaxThreatLevel` | `0.40` | Eskalations-Obergrenze |
| `ThreatRiseRate` | `0.02 / Tag` | maximale Druckzunahme pro Tag |
| `ThreatFallRate` | `0.05 / Tag` | Druckabbau bei Ruhe |
| `RestWindowMin` | `3.0 Tage` | Mindest-Ruhefenster nach Event |
| `RestWindowMax` | `7.0 Tage` | maximales Ruhefenster |
| `EventCooldownGlobal` | `1.5 Tage` | globaler Cooldown zwischen Events |
| `MaxActiveEvents` | `1` | höchstens 1 aktives Event gleichzeitig |
| `MaxEscalationBand` | `1` | Eskalationsstufe (1=niedrig) |
| `AllowedEventFamilies` | `SupplyCrisis, IdeologyConflict, ExternalThreat, Discovery, RestRecovery` | erlaubte Familien |
| `ResourceScarcityMultiplier` | `0.7` | Ressourcenknappheit (1.0=neutral) |
| `IdeologyTensionCap` | `0.35` | maximale Ideologie-Spannung |
| `IdeologyTensionDecay` | `0.08 / Tag` | Spannungsabbau |
| `TurnPointMinDays` | `10` | frühester Wendepunkt |
| `SeedRule` | `MapID + GameTickDay` | deterministischer Seed |

### Profil: `Rimconemy_Survival` (Überleben)

| Parameter | Wert | Erklärung |
|---|---|---|
| `ProfileId` | `Rimconemy_Survival` | stabile ID |
| `ProfileVersion` | `1` | Schema-Version |
| `Label` | `Überleben` | UI-Name |
| `Description` | `Hartes tägliches Überleben. Versorgung und Bedrohung konkurrieren ständig. Lagerbestand ist entscheidend. Soziale Folgen von Entscheidungen werden sichtbar.` | UI-Beschreibung |
| `MinThreatLevel` | `0.05` | minimaler Grunddruck |
| `MaxThreatLevel` | `0.75` | Eskalations-Obergrenze |
| `ThreatRiseRate` | `0.05 / Tag` | maximale Druckzunahme |
| `ThreatFallRate` | `0.03 / Tag` | Druckabbau |
| `RestWindowMin` | `1.5 Tage` | Mindest-Ruhefenster |
| `RestWindowMax` | `4.0 Tage` | maximales Ruhefenster |
| `EventCooldownGlobal` | `1.0 Tage` | globaler Cooldown |
| `MaxActiveEvents` | `1` | höchstens 1 aktives Event |
| `MaxEscalationBand` | `2` | Eskalationsstufe (2=mittel) |
| `AllowedEventFamilies` | `SupplyCrisis, IdeologyConflict, ExternalThreat, Discovery, TechOpportunity, MoralChoice, RestRecovery` | alle außer TurnPoint |
| `ResourceScarcityMultiplier` | `1.0` | neutral |
| `IdeologyTensionCap` | `0.60` | mittlere Spannung |
| `IdeologyTensionDecay` | `0.05 / Tag` | Spannungsabbau |
| `TurnPointMinDays` | `20` | Wendepunkt später |
| `SeedRule` | `MapID + GameTickDay` | deterministischer Seed |

### Profil: `Rimconemy_Collapse` (Zusammenbruch)

| Parameter | Wert | Erklärung |
|---|---|---|
| `ProfileId` | `Rimconemy_Collapse` | stabile ID |
| `ProfileVersion` | `1` | Schema-Version |
| `Label` | `Zusammenbruch` | UI-Name |
| `Description` | `Zusammenbruch unter permanentem Druck. Keine kostenlose Erholung. Wendepunkte früh möglich. Konflikte können Rollen, Mood und Folgeevents stark verändern.` | UI-Beschreibung |
| `MinThreatLevel` | `0.15` | spürbarer Grunddruck |
| `MaxThreatLevel` | `1.00` | volle Eskalation |
| `ThreatRiseRate` | `0.10 / Tag` | schnelle Eskalation |
| `ThreatFallRate` | `0.01 / Tag` | kaum Erholung |
| `RestWindowMin` | `0.5 Tage` | kurzes Ruhefenster |
| `RestWindowMax` | `2.0 Tage` | maximales Ruhefenster |
| `EventCooldownGlobal` | `0.5 Tage` | kurzer Cooldown |
| `MaxActiveEvents` | `2` | bis zu 2 aktive Events |
| `MaxEscalationBand` | `3` | Eskalationsstufe (3=hoch) |
| `AllowedEventFamilies` | `Alle 8` | alle Familien erlaubt |
| `ResourceScarcityMultiplier` | `1.5` | erhöhte Knappheit |
| `IdeologyTensionCap` | `0.90` | hohe Spannung |
| `IdeologyTensionDecay` | `0.02 / Tag` | langsamer Abbau |
| `TurnPointMinDays` | `5` | frühe Wendepunkte |
| `SeedRule` | `MapID + GameTickDay` | deterministischer Seed |

---

## 2. Drei MVP-Events — Konkrete Spezifikation

### Event 1: `Rimconemy_SupplyShortage` (Versorgungskrise)

```yaml
EventId: Rimconemy_SupplyShortage
EventVersion: 1
EventFamily: SupplyCrisis
Label: "Versorgungskrise"
Description: "Ein kritischer Lagerbestand unterschreitet das Minimum."

# Voraussetzungen
Prerequisites:
  - StorageSnapshot.AnyResourceBelow(profile.ResourceScarcityMultiplier * minThreshold)
  - NOT ActiveEvent(SupplyCrisis)  # kein zweites parallel
  - GameTime > lastSupplyEvent + profile.EventCooldownGlobal

# Ausschlüsse
Exclusions:
  - ActiveRecoveryEvent  # nicht während aktiver Versorgungserholung
  - profile.MaxActiveEvents erreicht

# Gewichtung (pro Profil)
Weights:
  Rimconemy_Refuge:    20   # selten
  Rimconemy_Survival:  50   # regelmäßig
  Rimconemy_Collapse:  80   # häufig

# Cooldown (pro Profil)
Cooldowns:
  Rimconemy_Refuge:    5.0 Tage
  Rimconemy_Survival:  3.0 Tage
  Rimconemy_Collapse:  1.5 Tage

# Eskalation
EscalationBand: 1
EscalationModifier: +0.03 ThreatPressure bei Ignorieren

# Text/UI
TextKey: Rimconemy_SupplyShortage_Letter
LetterLabel: "Vorräte schwinden"
LetterText: "Der Bestand an {ResourceName} ist kritisch niedrig ({CurrentAmount}/{MinAmount})."

# Entscheidungsoptionen
Choices:
  - ChoiceId: RationResources
    Label: "Ressourcen rationieren"
    Effects:
      - StorageSnapshot.FreezeConsumption(resourceId, 2.0 Tage)
      - IdeologyTension += 0.05
      - MoodModifier: -3 für 2 Tage
  - ChoiceId: SeekExternalHelp
    Label: "Externe Hilfe suchen"
    Effects:
      - TriggerTradingOpportunity(resourceId)
      - IdeologyTension += 0.02
      - WalletCost: 50 Credits (falls Economy aktiv)
  - ChoiceId: Ignore
    Label: "Ignorieren"
    Effects:
      - ThreatPressure += EscalationModifier
      - PawnMoodModifier: -5 für 1 Tag
      - FollowUpEvent: Rimconemy_SupplyShortage_FollowUp nach 2 Tagen

# FollowUp
FollowUpIds:
  - Rimconemy_SupplyShortage_FollowUp  # verschärfte Krise

# Determinismus
DeterminismKey: ProfileId + EventId + StorageSnapshot.Hash + GameTickDay
```

### Event 2: `Rimconemy_IdeologyConflict` (Ideologischer Konflikt)

```yaml
EventId: Rimconemy_IdeologyConflict
EventVersion: 1
EventFamily: IdeologyConflict
Label: "Ideologischer Konflikt"
Description: "Eine Setting-Regel wurde verletzt oder die Spannung überschreitet die Profilgrenze."

# Voraussetzungen
Prerequisites:
  - IdeologySnapshot.Tension > profile.IdeologyTensionCap * 0.7
  - AtLeastOneActiveSettingRule()
  - NOT ActiveEvent(IdeologyConflict)

# Ausschlüsse
Exclusions:
  - NoActiveSettingRules  # keine Regeln → kein Konflikt
  - profile.MaxActiveEvents erreicht

# Gewichtung
Weights:
  Rimconemy_Refuge:    15   # meist Dialog
  Rimconemy_Survival:  40   # soziale Konsequenz
  Rimconemy_Collapse:  70   # häufige Konflikte

# Cooldown
Cooldowns:
  Rimconemy_Refuge:    7.0 Tage
  Rimconemy_Survival:  4.0 Tage
  Rimconemy_Collapse:  2.0 Tage

# Eskalation
EscalationBand: 1
EscalationModifier: +0.04 IdeologyTension bei Ignorieren

# Text/UI
TextKey: Rimconemy_IdeologyConflict_Letter
LetterLabel: "Glaubenskonflikt"
LetterText: "{PawnName} stellt die Regel '{RuleId}' in Frage. Die Gruppe ist gespalten."

# Entscheidungsoptionen
Choices:
  - ChoiceId: EnforceRule
    Label: "Regel durchsetzen"
    Effects:
      - TargetPawn.MoodModifier: -8 für 3 Tage
      - GroupCohesion += 0.1
      - IdeologyTension -= 0.05
      - Thought: "Erzwungene Konformität" auf TargetPawn
  - ChoiceId: Compromise
    Label: "Kompromiss suchen"
    Effects:
      - TargetPawn.MoodModifier: +3 für 2 Tage
      - GroupCohesion -= 0.05
      - IdeologyTension += 0.02
      - Thought: "Gehör gefunden" auf TargetPawn
  - ChoiceId: Ignore
    Label: "Ignorieren"
    Effects:
      - IdeologyTension += EscalationModifier
      - TargetPawn.MoodModifier: -3 für 5 Tage
      - FollowUpEvent: Rimconemy_IdeologyConflict_FollowUp nach 3 Tagen

# FollowUp
FollowUpIds:
  - Rimconemy_IdeologyConflict_FollowUp  # Eskalation oder Abspaltung

# Determinismus
DeterminismKey: ProfileId + EventId + IdeologySnapshot.Tension + PawnId + GameTickDay
```

### Event 3: `Rimconemy_ExternalThreat` (Äußere Bedrohung)

```yaml
EventId: Rimconemy_ExternalThreat
EventVersion: 1
EventFamily: ExternalThreat
Label: "Äußere Bedrohung"
Description: "Eine Bedrohung von außen nähert sich der Siedlung."

# Voraussetzungen
Prerequisites:
  - ThreatPressure > profile.MinThreatLevel + 0.1
  - NOT ActiveVanillaRaid  # kein doppelter Raid
  - NOT ActiveEvent(ExternalThreat)

# Ausschlüsse
Exclusions:
  - ActiveRaidOrThreatEvent  # kein paralleler Raid
  - profile.MaxActiveEvents erreicht

# Gewichtung
Weights:
  Rimconemy_Refuge:    15   # seltene Bedrohung
  Rimconemy_Survival:  45   # regelmäßig
  Rimconemy_Collapse:  90   # fast ständig

# Cooldown
Cooldowns:
  Rimconemy_Refuge:    6.0 Tage
  Rimconemy_Survival:  3.0 Tage
  Rimconemy_Collapse:  1.0 Tage

# Eskalation
EscalationBand: 2
EscalationModifier: +0.06 ThreatPressure bei Ignorieren

# Text/UI
TextKey: Rimconemy_ExternalThreat_Letter
LetterLabel: "Bedrohung gesichtet"
LetterText: "Ein {ThreatType} nähert sich von {Direction}. Geschätzte Stärke: {Strength}."

# Entscheidungsoptionen
Choices:
  - ChoiceId: PrepareDefense
    Label: "Verteidigung vorbereiten"
    Effects:
      - DefenseBonus: +0.25 für 1 Tag
      - PawnMobilization: Alle Pawns zu Verteidigungspositionen
      - ResourceCost: 10% der Munition/Vorräte
  - ChoiceId: Evacuate
    Label: "Evakuieren"
    Effects:
      - EvacuateCivilians()
      - StorageSnapshot.MarkSomeUnavailable(0.3)  # 30% temporär blockiert
      - IdeologyTension += 0.08
      - MoodModifier: -5 für 2 Tage
  - ChoiceId: Ignore
    Label: "Ignorieren"
    Effects:
      - ThreatPressure += EscalationModifier
      - DefenseMalus: -0.25 für 1 Tag
      - FollowUpEvent: Rimconemy_ExternalThreat_Attack nach 0.5 Tagen

# FollowUp
FollowUpIds:
  - Rimconemy_ExternalThreat_Attack  # tatsächlicher Angriff

# Determinismus
DeterminismKey: ProfileId + EventId + ThreatPressure + GameTickDay
```

---

## 3. Eventfamilien — Vollständige Liste

| # | FamilyId | Label | Beschreibung | Eskalationsband |
|---|---|---|---|---|
| 1 | `SupplyCrisis` | Versorgungskrise | Ressourcenengpass | 1 |
| 2 | `IdeologyConflict` | Ideologischer Konflikt | Regelverstoß oder Spannung | 1–2 |
| 3 | `ExternalThreat` | Äußere Bedrohung | Raid/Bedrohung | 2–3 |
| 4 | `Discovery` | Entdeckung | Neue Ressource/Ort/Technik | 1 |
| 5 | `TechOpportunity` | Technische Chance | Forschungsboost oder Fund | 1–2 |
| 6 | `MoralChoice` | Moralische Entscheidung | Dilemma mit Langzeitfolgen | 1–3 |
| 7 | `RestRecovery` | Ruhe/Erholung | Positives Erholungsereignis | 0 |
| 8 | `TurnPoint` | Wendepunkt | Story-Wendung, nur Collapse | 3 |

### 3.1 Cross-Walk Code ↔ Spec (Audit-Korrektur 2026-08-04)

Der Code-`StoryEventCatalog` (12 Events in `Source/Story/StoryEventCatalog.cs`)
nutzt eine 4-Familien-Vokabular (`Supply`, `Social`, `Raid`, `Collapse`),
weil die Events direkt an diese Strings gebunden sind und der
`SettingProfile.AllowedEventFamilies` diese Strings enthält.
Die 8-Familien-Vokabular dieser Spec ist eine feinere Aufteilung.
Beide Beschreibungen sind gültig; die Brücke ist
`Source/Story/EventFamilyMap.cs`:

| Code (4) | H2-Spec (8) unter diesem Code-Family |
|---|---|
| `Supply` | `SupplyCrisis`, `Discovery`, `TechOpportunity` |
| `Social` | `IdeologyConflict`, `MoralChoice`, `RestRecovery` |
| `Raid` | `ExternalThreat` |
| `Collapse` | `TurnPoint` |

Solange die Profile-Whitelist und der Catalog beide das 4-Vokabular
benutzen, ist diese Tabelle die kanonische Reconciliation; eine zukünftige
Phase kann StoryEventCatalog auf das 8-Vokabular migrieren und
Profile.AllowedEventFamilies entsprechend anpassen.

---

## 4. Eventauswahl — Algorithmus (Pseudocode)

```csharp
StoryEventSpec SelectEvent(SettingProfile profile, SituationSnapshot snapshot, int seed)
{
    var rng = new DeterministicRng(seed);

    // 1. Harte Ausschlüsse
    var candidates = EventCatalog.All
        .Where(e => profile.AllowedEventFamilies.Contains(e.EventFamily))
        .Where(e => e.EscalationBand <= profile.MaxEscalationBand)
        .Where(e => !snapshot.ActiveEventIds.Contains(e.EventId))
        .Where(e => snapshot.GameTime >= snapshot.LastEventTimes[e.EventId] + e.Cooldowns[profile.ProfileId])
        .Where(e => e.Prerequisites.All(p => p.Evaluate(snapshot, profile)))
        .Where(e => e.Exclusions.All(x => !x.Evaluate(snapshot, profile)))
        .ToList();

    if (candidates.Count == 0)
        return null;

    // 2. Stabile Sortierung
    candidates = candidates
        .OrderBy(e => e.EventId)  // deterministisch
        .ToList();

    // 3. Gewichtete Auswahl
    float totalWeight = candidates.Sum(e => e.Weights[profile.ProfileId]);
    float roll = rng.NextFloat() * totalWeight;
    float cumulative = 0;
    foreach (var candidate in candidates)
    {
        cumulative += candidate.Weights[profile.ProfileId];
        if (roll <= cumulative)
            return candidate;
    }

    return candidates[0];  // Fallback
}
```

---

## 5. Save-Schema `StoryState`

```yaml
StoryState:
  SchemaVersion: 1
  ProfileId: string
  ProfileVersion: int
  LastEventId: string | null
  LastEventTick: long
  ActiveEventIds: string[]
  EventCooldowns:
    EventId: long  # Tick when cooldown expires
  SelectionSeed: int
  LastSnapshotHash: string
  IdempotencyKeys: string[]  # bereits ausgeführte Keys
```

---

## 6. Vanilla-/DLC-Policy

| Policy | Regel |
|---|---|
| Vanilla Wealth Raids | **Nicht deaktivieren** — als separater Policy-Pfad klassifiziert |
| Quest-Incidents | **Nicht pauschal unterdrücken** — Quest-Ereignisse bleiben funktional |
| DLC-Incidents (Royalty, Ideology, Biotech, Anomaly, Odyssey) | **Koexistenz** — Adapter dokumentieren Interaktion |
| Doppelte Raid-Auslösung | StoryState.IdempotencyKeys verhindern Doppelausführung |
| Storyteller-Integration | Erst nach API-Spike: Setting-Director oder direkter StorytellerDef-Adapter |

---

## 7. Event-Preset-Ordner (DEF vorhanden, Engine teilweise aktiv)

> **Status:** `DEF + CODE` — RimWorld-ladbare Defs, `StoryEventDef`, `StoryEventCatalog` und `StorySelector` sind vorhanden. Die deklarativen Choice-/Effect-Strings sind keine vollständige Gameplay-Effekt-Engine. Die Profileinträge `weights` und `cooldownDays` werden als `List<string>` im Format `ProfileId=Value` geladen.

Die drei MVP-Events liegen als RimWorld-Defs im Preset-Ordner:

```text
mods/05-Rimconemy-Infected-Automation/Defs/StoryEvents/
  Rimconemy_SupplyShortage.xml
  Rimconemy_IdeologyConflict.xml
  Rimconemy_ExternalThreat.xml
  TEMPLATE_NewEvent.xml        # Kopiervorlage für neue Events
  README.md                    # Schema + Checkliste + Konventionen
```

### 7.1 Ladefähigkeit

Jede Datei wird über die Def-Klasse `Rimconemy.InfectedAutomation.Story.StoryEventDef`
(`Source/Story/StoryEventDef.cs`) von RimWorld geladen. Die Klasse bildet die
`StoryEventSpec`-Felder 1:1 ab:

| H2-Feld | Def-Feld | XML-Beispiel |
|---|---|---|
| `EventId` | `defName` (geerbt) | `<defName>Rimconemy_SupplyShortage</defName>` |
| `EventVersion` | `eventVersion` | `<eventVersion>1</eventVersion>` |
| `EventFamily` | `eventFamily` | `<eventFamily>SupplyCrisis</eventFamily>` |
| `Label` | `label` (geerbt) | `<label>Versorgungskrise</label>` |
| `Description` | `description` (geerbt) | `<description>…</description>` |
| `EscalationBand` | `escalationBand` | `<escalationBand>1</escalationBand>` |
| `EscalationModifier` | `escalationModifier` + `escalationTarget` | `<escalationModifier>0.03</escalationModifier>` |
| `TextKey` | `textKey` | `<textKey>Rimconemy_SupplyShortage_Letter</textKey>` |
| `LetterLabel` | `letterLabel` | `<letterLabel>Vorräte schwinden</letterLabel>` |
| `LetterText` | `letterText` | `<letterText>…{ResourceName}…</letterText>` |
| `Prerequisites` | `prerequisites` (`List<string>`) | `<prerequisites><li>…</li></prerequisites>` |
| `Exclusions` | `exclusions` (`List<string>`) | `<exclusions><li>…</li></exclusions>` |
| `Weights` | `weights` (`List<string>`, `ProfileId=Value`) | `<weights><li>Rimconemy_Refuge=20</li></weights>` |
| `Cooldowns` | `cooldownDays` (`List<string>`, `ProfileId=Value`, **Tage**) | `<cooldownDays><li>Rimconemy_Refuge=5.0</li></cooldownDays>` |
| `Choices` | `choices` (`List<StoryEventChoiceDef>`) | `<choices><li Class="Rimconemy.InfectedAutomation.Story.StoryEventChoiceDef">…</li></choices>` |
| `FollowUpIds` | `followUpIds` | `<followUpIds><li>Rimconemy_SupplyShortage_FollowUp</li></followUpIds>` |
| `DeterminismKey` | `determinismKey` | `<determinismKey>ProfileId + EventId + …</determinismKey>` |

`StoryEventChoiceDef` bildet pro Option `ChoiceId`, `Label`, `Effects` (`List<string>`) ab. `List<string>` ist die XML-Speicherform; die Story-Def-/Spec-Schicht überführt Profilwerte anschließend in profilbezogene Runtime-Lookups, die der Auswahlcode wie `Weights[profile.ProfileId]` verwendet.

### 7.2 Deklaratives Vokabular

`prerequisites`, `exclusions` und `effects` sind **deklarative Strings** — sie
werden von der Phase-1-Engine ausgewertet, nicht von RimWorld. Bis dahin dienen
sie als eindeutige Vertragsreferenz auf Snapshot-Felder (H3/H4). Konventionen:

- Prerequisites sind AND-verknüpft; jedes `exclusions`-Element verhindert das Event.
- Bezug auf `StorageSnapshot.*`, `IdeologySnapshot.*`, `ThreatPressure`,
  `GroupCohesion`, `PawnMoodModifier`, `WalletCost`, `FollowUpEvent` usw.
- `>` im XML als `&gt;` schreiben (XML-Escaping).
- `escalationModifier` wird bei der Choice `Ignore` auf `escalationTarget`
  (`ThreatPressure` oder `IdeologyTension`) addiert.

### 7.3 Neues Event anlegen (Kurzfassung)

1. `TEMPLATE_NewEvent.xml` kopieren → `Rimconemy_<Kurzname>.xml` (Präfix-Pflicht).
2. `defName` stabil vergeben; nie umbenennen nach Save-Referenz.
3. `eventFamily` aus §3, `escalationBand` <= Profil-`MaxEscalationBand`.
4. `weights`/`cooldownDays` pro Profil (Tage; 1 Tag = 60.000 Ticks).
5. `prerequisites`/`exclusions`/`choices` deklarativ füllen, `Ignore` immer vorsehen.
6. `followUpIds` nur bei existierendem/geplantem Folge-Event.
7. RimWorld-Start → Def-Load ohne Fehler in `Player.log` prüfen.

Vollständige Checkliste: `Defs/StoryEvents/README.md`.

### 7.4 Belegpflicht

- Def-Load im Spiel ist **noch nicht** verifiziert (`DEF-LOAD` offen, H1-Wortschatz).
  Der User startet RimWorld und prüft `Player.log` auf Fehler des Preset-Ordners.
- `DeterministicRng` und Auswahl sind implementiert und per Regressionstest abgedeckt; die vollständige Ausführung deklarativer Choice-/Effect-Strings bleibt offen.
- Solange keine Engine existiert, ist ein Preset eine inerte Daten-Def — kein
  stilles Feuern, kein Double-Fire (Determinismus-Gate G2 aus ROADMAP §4/§6 bleibt gültig).

---

## Nächster Schritt (User)

1. Zahlenwerte im Collapse-Profil prüfen (zu aggressiv?)
2. Event-Prerequisites gegen reale Storage-/Ideology-Snapshot-Felder abgleichen
3. `DeterministicRng`-Implementierung als pure Funktion ohne `System.Random`
4. **Runtime-Gate:** `./scripts/runtime_test.sh --require-scenario-tests` ausführen; für echtes Event-/Letter-/Raid-Verhalten zusätzlich einen Spielstand interaktiv testen. Der Boot-Test prüft Def-/Bootstrap-/Regression-Marker, ersetzt aber keinen Save/Load-/Gameplay-Beleg.
   auf `Rimconemy.InfectedAutomation`-Fehler prüfen, drei Events in Def-Debug sehen
