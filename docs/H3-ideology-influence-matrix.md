# H3 — Ideology-Einflussmatrix

> **SSOT-Hinweis:** Detail-Topic dieser Datei ist im Orient-Index [ARCHITECTURE.md §3](ARCHITECTURE.md). Topic-Landkarte: [INDEX.md §1](INDEX.md).
> **Owner:** Research/Design (kein Code)
> **Status:** `COMPILED` — 3 Setting-Regeln spezifiziert; Regel 2 (CollectiveDefense) Code-implementiert mit Harmony-Postfix-Fix (2026-08-04; explizit via `Pawn_PostApplyDamage_CollectiveDefense.Install()` am 2026-08-05 verdrahtet — Package 05 hat kein PatchAll)
> **Referenz:** [ROADMAP.md §2.4](../ROADMAP.md#24-setting-ideologie), [ROADMAP.md §8.3](../ROADMAP.md#83-phase-12--story-writer--setting-ideologie-offen)

## Zweck

Für **drei Setting-Regeln** wird eine vollständige Einflussmatrix ausgefüllt:
`RuleId → Setting-Bedeutung → Technischer Träger → Pawn-Zielgruppe → Mood/Social/AI-Wirkung → Trigger → UI-Erklärung → Save-Key → DLC-Abhängigkeit → Falsifikationsfall`.

Der technische Träger ist ein RimWorld-Ideology-Element, das **nachweisbar** die erwartete Charakterreaktion auslöst. Keine Regel wird nur aus Assembly-Strings oder Precept-Namen abgeleitet.

---

## Vorbemerkung: Was „Ideologie" in diesem Projekt bedeutet

- **Keine** zusätzliche Religion. Das vorhandene Ideology-Fenster wird als **Setting-/Erfahrungsfenster** genutzt.
- Das Setting besitzt die Regeln. RimWorld Ideology dient **nur** als technischer Träger.
- Begriffe wie „Glaube", „Gott", „Sünde" werden **nicht** verwendet. Stattdessen: „Regel", „Gruppe", „Konformität", „Konsequenz".

---

## Regel 1: Ressourcen-Gerechtigkeit

### Spezifikation

```yaml
RuleId: Rimconemy_SettingRule_ResourceFairness
SettingMeaning: >
  Ressourcen (Nahrung, Material, Medizin) müssen fair verteilt werden.
  Ungleiche Verteilung erzeugt soziale Spannung und individuelle Unzufriedenheit.

# Technischer Träger
Carrier:
  Primary: PreceptDef  # Rimconemy_ResourceFairness_Precept
  Secondary: ThoughtDef  # Rimconemy_Thought_UnfairDistribution
  Tertiary: ThoughtWorker  # Rimconemy_ThoughtWorker_ResourceFairness

# Pawn-Zielgruppe
PawnTarget:
  Scope: AllPlayerColonists
  Filter: IsAdult && !IsPrisoner && !IsSlave

# Wirkung
Effects:
  Mood:
    - Wenn Verteilung fair (Abweichung < 10%): MoodOffset +3 (Content)
    - Wenn Verteilung unfair (Abweichung > 30%): MoodOffset -5 (Resentful)
    - Stack mit Anzahl unfairer Tage (max -12)
  Social:
    - Unfairer Pawn erhält OpinionMalus -10 von fair behandelten Pawns
    - OpinionBonus +5 zwischen fair behandelten Pawns
  AI:
    - Unfair behandelte Pawns priorisieren eigene Ressourcen-Beschaffung
    - PsychoticBreak-Wahrscheinlichkeit +15% bei anhaltender Unfairness (> 3 Tage)

# Trigger
Trigger:
  Type: DailyCheck
  Condition: >
    Alle 60.000 Ticks: Berechne Ressourcen-Verteilungs-Gini-Koeffizient.
    Bei Überschreitung der Profil-Grenze: Thought anwenden.

# UI
UIExplanation: >
  "Ressourcen-Gerechtigkeit: {FairnessPercent}% gleich verteilt.
  {UnfairPawnCount} Kolonist(en) fühlen sich benachteiligt."

# Save
SaveKey: SettingRule_ResourceFairness_State
SaveData:
  LastFairnessCheckTick: long
  GiniCoefficient: float
  UnfairPawnIds: int[]
  ConsecutiveUnfairDays: int

# DLC
DLCDependency: Core (kein DLC) — nutzt nur PreceptDef + ThoughtDef + ThoughtWorker

# Falsifikation
FalsificationCase: >
  Zwei identische Kolonien mit unterschiedlicher Ressourcenverteilung
  (fair vs. unfair) erzeugen nach 3 Tagen unterschiedliche Mood-Werte,
  Social-Opinions und Break-Wahrscheinlichkeiten.
  Test: FAIR_COLONY vs UNFAIR_COLONY, Seed=42.
```

### Träger-Detail

| Träger | RimWorld-Typ | Rimconemy-DefName | Erwartete Wirkung | Verifikation |
|---|---|---|---|---|
| Precept | `PreceptDef` | `Rimconemy_ResourceFairness_Precept` | Definiert die Regel im Ideology-Fenster | DEF-LOAD: Precept erscheint im Fenster |
| Thought (fair) | `ThoughtDef` | `Rimconemy_Thought_FairDistribution` | +3 Mood für 1 Tag | RUNTIME: Mood-Änderung sichtbar |
| Thought (unfair) | `ThoughtDef` | `Rimconemy_Thought_UnfairDistribution` | -5 Mood für 1 Tag, stapelbar | RUNTIME: Mood-Änderung sichtbar |
| ThoughtWorker | `ThoughtWorker` | `Rimconemy_ThoughtWorker_ResourceFairness` | Berechnet Fairness täglich | RUNTIME: Thought erscheint/verschwindet |

---

## Regel 2: Kollektive Verteidigungspflicht

### Spezifikation

```yaml
RuleId: Rimconemy_SettingRule_CollectiveDefense
SettingMeaning: >
  Jeder koloniefähige Erwachsene trägt zur Verteidigung bei.
  Wer sich drückt, verliert an Ansehen und gefährdet die Gruppe.

# Technischer Träger
Carrier:
  Primary: RoleDef  # Rimconemy_Role_Defender (automatische Rolle)
  Secondary: ThoughtDef  # Rimconemy_Thought_DefenseShirking
  Tertiary: RitualDef  # Rimconemy_Ritual_PostDefense (nach Kampf)

# Pawn-Zielgruppe
PawnTarget:
  Scope: AllPlayerColonists
  Filter: IsAdult && !IsIncapacitated && !IsPrisoner

# Wirkung
Effects:
  Mood:
    - Teilnahme an Verteidigung: MoodOffset +5 (Valiant) für 2 Tage
    - Nicht-Teilnahme trotz Fähigkeit: MoodOffset -8 (Coward) für 3 Tage
    - Nach erfolgreicher Verteidigung: Gruppen-MoodBonus +3 (United)
  Social:
    - Drückeberger erhalten OpinionMalus -15 von Teilnehmern
    - Wiederholte Teilnahme: OpinionBonus +8 (RespectedDefender)
  AI:
    - Drückeberger priorisieren Fluchtwege bei nächstem Raid
    - Wiederholte Drückeberger: MentalBreak-Schwelle gesenkt um 0.1

# Trigger
Trigger:
  Type: PostCombatEvent
  Condition: >
    Nach jedem Raid/Threat-Event: Prüfe, welche Pawns in Kampf involviert waren.
    Vergleiche mit Liste der fähigen, aber nicht-teilnehmenden Pawns.

# UI
UIExplanation: >
  "Verteidigungspflicht: {DefenderCount} von {AbleCount} haben gekämpft.
  {ShirkerCount} Kolonist(en) haben sich gedrückt."

# Save
SaveKey: SettingRule_CollectiveDefense_State
SaveData:
  LastCombatTick: long
  ParticipatingPawnIds: int[]
  ShirkingPawnIds: int[]
  ConsecutiveShirkCount: dict[pawnId -> int]

# DLC
DLCDependency: Ideology (RoleDef, RitualDef)

# Falsifikation
FalsificationCase: >
  Ein Raid mit 3 fähigen Pawns, von denen 2 kämpfen und 1 sich drückt.
  Nach dem Raid: 2 Kämpfer haben +5 Mood, 1 Drückeberger hat -8 Mood
  und -15 Opinion von beiden Kämpfern.
  Test: COMBAT_3PAWNS_2FIGHT_1SHIRK, Seed=42.
```

### Träger-Detail

| Träger | RimWorld-Typ | Rimconemy-DefName | Erwartete Wirkung | Verifikation |
|---|---|---|---|---|
| Rolle | `RoleDef` | `Rimconemy_Role_Defender` | Automatisch vergeben an Kampfteilnehmer | DEF-LOAD: Rolle erscheint; RUNTIME: Vergabe sichtbar |
| Thought (Kampf) | `ThoughtDef` | `Rimconemy_Thought_ValiantDefense` | +5 Mood für 2 Tage | RUNTIME: Mood-Änderung nach Kampf |
| Thought (Drückeberger) | `ThoughtDef` | `Rimconemy_Thought_DefenseShirking` | -8 Mood für 3 Tage | RUNTIME: Mood-Änderung sichtbar |
| Ritual | `RitualDef` | `Rimconemy_Ritual_PostDefense` | +3 Gruppen-Mood nach erfolgreicher Verteidigung | RUNTIME: Ritual startet/funktioniert |

---

## Regel 3: Wahrheit und Transparenz

### Spezifikation

```yaml
RuleId: Rimconemy_SettingRule_Transparency
SettingMeaning: >
  Entscheidungen der Gruppenführung müssen erklärt werden.
  Unerklärte oder widersprüchliche Entscheidungen untergraben das Vertrauen.

# Technischer Träger
Carrier:
  Primary: PreceptDef  # Rimconemy_Transparency_Precept
  Secondary: ThoughtDef  # Rimconemy_Thought_UnexplainedDecision
  Tertiary: ThoughtWorker  # Rimconemy_ThoughtWorker_DecisionTransparency

# Pawn-Zielgruppe
PawnTarget:
  Scope: AllPlayerColonists
  Filter: IsAdult && !IsPrisoner

# Wirkung
Effects:
  Mood:
    - Erklärte Entscheidung: MoodOffset +2 (Informed) für 1 Tag
    - Unerklärte Entscheidung mit negativer Folge: MoodOffset -6 (Betrayed) für 2 Tage
    - Stack: Jede weitere unerklärte Entscheidung innert 5 Tagen: -2 kumulativ (max -14)
  Social:
    - Entscheider (Spieler-Proxy): OpinionMalus -5 pro unerklärter Entscheidung
    - Transparenter Entscheider: OpinionBonus +3 (Trusted)
  AI:
    - Bei > 3 unerklärten Entscheidungen: MentalBreak-Schwelle -0.15
    - Bei chronischer Intransparenz (> 7 Tage): IdeologyTension +0.05/Tag

# Trigger
Trigger:
  Type: OnDecisionEvent
  Condition: >
    Jede Event-Entscheidung (SupplyShortage, IdeologyConflict, ExternalThreat)
    die vom Spieler getroffen wird, erhöht oder senkt den TransparencyScore.
    Erklärte Entscheidungen = Event hat UIExplanation + sichtbaren Grund.
    Unerklärte = Event-Feuer ohne UI-Kontext.

# UI
UIExplanation: >
  "Transparenz: {ExplainedDecisions} von {TotalDecisions} Entscheidungen erklärt.
  Vertrauensniveau: {TrustLevel}%."

# Save
SaveKey: SettingRule_Transparency_State
SaveData:
  TotalDecisions: int
  ExplainedDecisions: int
  LastDecisionTick: long
  ConsecutiveUnexplainedDecisions: int
  TrustLevel: float

# DLC
DLCDependency: Core (kein DLC)

# Falsifikation
FalsificationCase: >
  Zwei identische Kolonien: Kolonie A trifft 3 erklärte Entscheidungen,
  Kolonie B trifft 3 unerklärte Entscheidungen.
  Nach 5 Tagen: A hat Mood +2 und Opinion +3, B hat Mood -10 und Opinion -10.
  Test: TRANSPARENCY_A vs TRANSPARENCY_B, Seed=42.
```

### Träger-Detail

| Träger | RimWorld-Typ | Rimconemy-DefName | Erwartete Wirkung | Verifikation |
|---|---|---|---|---|
| Precept | `PreceptDef` | `Rimconemy_Transparency_Precept` | Definiert Regel im Ideology-Fenster | DEF-LOAD: Precept sichtbar |
| Thought (erklärt) | `ThoughtDef` | `Rimconemy_Thought_InformedDecision` | +2 Mood für 1 Tag | RUNTIME: Mood sichtbar |
| Thought (unerklärt) | `ThoughtDef` | `Rimconemy_Thought_UnexplainedDecision` | -6 Mood, kumulativ | RUNTIME: Mood & Stack |
| ThoughtWorker | `ThoughtWorker` | `Rimconemy_ThoughtWorker_DecisionTransparency` | Berechnet Vertrauen | RUNTIME: Thought erscheint |

---

## Übersicht: Einflussmatrix

| Regel-ID | Bedeutung | Primärer Träger | Wirkung (Mood) | Wirkung (Social) | Wirkung (AI) | DLC |
|---|---|---|---|---|---|---|
| `ResourceFairness` | Faire Ressourcen-Verteilung | `PreceptDef` + `ThoughtDef` | +3 / -5 (stack -12) | Opinion ±10 | Break +15% | Core |
| `CollectiveDefense` | Verteidigungspflicht | `RoleDef` + `ThoughtDef` + `RitualDef` | +5 / -8 | Opinion ±15 | Break-Schwelle -0.1 | Ideology |
| `Transparency` | Erklärte Entscheidungen | `PreceptDef` + `ThoughtDef` | +2 / -6 (stack -14) | Opinion ±5 | Break-Schwelle -0.15 | Core |

---

## Vanilla-Precept-Policy

| Vanilla-Precept | Behandlung | Grund |
|---|---|---|
| `Cannibalism` | **Beobachten** — kein Override | Kann mit ResourceFairness interagieren, aber nicht ersetzt |
| `Execution` | **Beobachten** — kein Override | Interagiert mit CollectiveDefense (Hinrichtung als Strafe für Drückebergerei?) — User-Entscheidung |
| `Slavery` | **Neutralisieren** — auf `Horrible` | Passt nicht zum Setting-Konzept |
| `InsectMeat` | **Beobachten** | Ressourcen-Relevanz für ResourceFairness |
| Alle übrigen | **Beobachten** | Keine pauschale Löschung; Interaktion dokumentieren |

---

## Nächster Schritt (User)

1. `IdeoDef`-Signatur in DLC-Assembly lokal prüfen (H1-Sektion B)
2. Drei `PreceptDef`-XML-Defs anlegen und DEF-LOAD prüfen (RimWorld start → Ideology-Fenster öffnen)
3. `ThoughtWorker_ResourceFairness` kompilieren und testen, ob Thought auf Pawn erscheint
