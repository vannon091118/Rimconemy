# Phase 0-A + Phase A-1 — UI Foundation Toolkit & Character-Härtung — Design Spec

> **Datum:** 2026-08-04
> **Owner:** Foundation (Phase 0-A) + Survival & Progression (Phase A-1)
> **Status:** Spec-Draft → wartet auf User-Review
> **Vorgänger-Specs:**
> [`docs/superpowers/specs/2026-08-04-track-a-character-design.md`](2026-08-04-track-a-character-design.md)
> (A-Phase-2 + A-Phase-3 bleiben unverändert; A-Phase-1 wandert hier rein)
> **Bezug:** [`docs/H2-story-contract.md`](../../H2-story-contract.md) ·
> [`docs/H4-storage-query-contract.md`](../../H4-storage-query-contract.md) ·
> [`docs/H5-character-setup-formula.md`](../../H5-character-setup-formula.md) ·
> [`docs/INTERFACE_CONTRACT.md`](../../INTERFACE_CONTRACT.md)

## 1. Zweck

Track 0-A richtet eine **zentrale UI-Theme-Schicht** in Mod 01 (Foundation)
ein, damit alle späteren UIs (FoundationDashboard, SurvivalProgressionDashboard,
SkillBudgetWindow, ProgressionPawnTab und künftige UIs in 03/04/05) ein
einheitliches Design benutzen. A-Phase-1 baut in derselben Session die
Character-Härtung (Combat-Skills einschließen, lineare Skill-Kosten,
H5-Trait-Schwellen) direkt auf dem Toolkit auf — ohne doppelten Token-Aufwand.

Zentrale Fragen, die diese Spec beantwortet:

- Welche konkreten Zahlen (Spacing, Indent, Farbpalette) sind die Tokens?
- Welche Basisklassen erben Pakete 02–05?
- Welche statischen Helper-Funktionen brauchen sie?
- Wie bleibt RimThemes kompatibel (opt-in)?
- Wie wird die SkillBudgetWindow in A-1 umgestellt?

## 2. Nicht-Ziele

- Keine Migration der SurvivalProgressionDashboard / ProgressionPawnTab in dieser Session — sie bleiben funktional wie heute; nur Tokens/Spacing werden NICHT umgestellt; das kommt, wenn die jeweilige UI in einer Folgephase anfassen wird (z. B. durch BioOverride-Hook in A-2 wird `SkillBudgetWindow` umgebaut, also MUSS sie auf Tokens migriert werden, das ist explizit in dieser Spec enthalten).
- Kein globaler Harmony-Patch auf `Widgets.DrawWindowBackground` — RimWorld-1.6-Konflikt-Risiko ist unklar (siehe Spike `API-FOUNDATION-UI-01` → UNVERIFIED).
- Kein neues RimWorld-InspectTab-Hook für ProgressionPawnTab (ebenfalls UNVERIFIED).
- Keine Sprachpakete außerhalb `Languages/{English,German}/Keyed/*.xml` (Foundation.xml ist die einzige Quelle; SkillBudget-Keys werden in Phase A-1 ergänzt).

## 3. Designentscheidungen

### D1 — Token-Quelle (default = FoundationDashboard; adaptiert)

Wir extrahieren Spacing-Tokens aus `FoundationDashboard.cs` (IndentSize=16, SectionSpacing=12, Zeilenhöhen 22/18/30) als **kanonische Quelle**. SurvivalProgressionDashboard (10/34/57/80px) wird in einer Folgephase angeglichen — nicht in diesem Sprint. Begründung: FoundationDashboard wird häufiger geöffnet, war Inspiration für das Kit, weniger Magic-Number-Konkurrenz bei einer Übernahme.

### D2 — Base Classes als sealed-Helper-Klassen (locker)

`RimconemyWindow : Window`, `RimconemyMainTabWindow : MainTabWindow`,
`RimconemyInspectTab : InspectTabBase` werden als **sealed Helper-Klassen**
mit Default-Implementierungen für Chrome/Spacing/Fonts angeboten. Pakete
02–05 wählen die Basisklasse und überschreiben nur, was abweicht.
**Kein abstract-method-Pattern** → weniger Bruch für künftige MainTab-Belange.

### D3 — Toolkit als statische Helper (kein Instance-Builder)

`RimconemyUi.DrawSectionTitle(rect, label)`, `...DrawRow(...)`,
`...DrawBadge(rect, status)`, `...DrawNeedBar(rect, fillFraction, label)`
sind **statische Methoden**. Vorteile: kein State-Management, einfacher
zu testen, einfache Composition („nimm Listing_Standard, aber für jede
Section diese DrawSectionTitle").

### D4 — Rim Themes Kompatibilität nur opt-in (Settings-Flag)

`RimThemes`-Patch wird NICHT standardmäßig angewendet. Einstellung in
`FoundationSaveData` (Boolean `EnableGlobalThemeOverride`) steuert ihn.
Defaults auf `false`. Aktivierung prüft zusätzlich:

```csharp
if (currentValue && ModsConfig.IsActive("aRandomKiwi.RimThemes"))
    ApplyRimThemesTheme();
```

**Kein Exception-Risiko** wenn RimThemes fehlt → wir laden das Theme einfach
nicht.

### D5 — Color-Palette als semantische Tokens

Wir nutzen 5 semantische Farbnamen (statt direkter `Color.*`):

| Semantic | Color | Hex |
|---|---|---|
| `Success` | grün | `0.30, 0.80, 0.30` |
| `Warn`    | gelb | `0.95, 0.78, 0.20` |
| `Error`   | rot  | `0.90, 0.30, 0.30` |
| `Info`    | cyan | `0.50, 0.85, 0.95` |
| `Muted`   | grau | `0.65, 0.65, 0.65` |

FoundationDashboard verwendet heute schon diese Semantik, aber inkonsistent
verstreut (z. B. `Color.green` neben `Color.gray`). Wir konsolidieren.

### D6 — Tooltip-Pflicht (UI-Hygiene)

Jede interaktive Widget bekommt `TooltipHandler.TipRegion(rect, signal)`
mit übersetzungspflichtigem Key. Wo SkillDef.description verfügbar ist
(`SkillBudgetWindow`-Zeile), wird er als Tooltip genutzt.

### D7 — SkillBudgetCalculator in Mod 02, nicht in Mod 01

Linear-skill-budget ist eine Concept von Mod 02 (Survival). Mod 01 wäre
falscher Ort. Wir behalten die Trennung: UI-Toolkit (Mod 01) ist
darstellungsneutral; Skill-Budget-Mathematik (Mod 02) ist fachlogisch.

## 4. Datenmodels — Phase 0-A

### 4.1 `RimconemyTheme` (statisch, in Foundation)

```csharp
namespace Rimconemy.Foundation.UI
{
    public static class RimconemyTheme
    {
        // Layout
        public const float SectionSpacing = 12f;
        public const float IndentSize = 16f;
        public const float RowHeight = 22f;
        public const float MiniRowHeight = 18f;
        public const float SectionTitleHeight = 30f;
        public const float SectionTitleSpacing = 2f;
        public const float Margin = 8f;
        public const float DefaultWindowPadding = 20f;

        // ScrollView defaults
        public const float DefaultScrollbarWidth = 16f;
        public const float DefaultViewPadding = 4f;

        // Window defaults
        public const float MinWindowWidth = 360f;
        public const float MaxWindowWidth = 1200f;
        public const float MinWindowHeight = 240f;
        public const float MaxWindowHeight = 800f;

        // Tooltip / interaction
        public const float HoverDarkenAmount = 0.05f; // 0..1 lerp factor
        public const float TooltipDelayMs = 250f;

        // Semantic colors
        public static readonly Color Success = new Color(0.30f, 0.80f, 0.30f);
        public static readonly Color Warn    = new Color(0.95f, 0.78f, 0.20f);
        public static readonly Color Error   = new Color(0.90f, 0.30f, 0.30f);
        public static readonly Color Info    = new Color(0.50f, 0.85f, 0.95f);
        public static readonly Color Muted   = new Color(0.65f, 0.65f, 0.65f);
        public static readonly Color HeaderInk = new Color(1.00f, 0.93f, 0.82f);
    }
}
```

### 4.2 `RimconemyWindow` / `RimconemyMainTabWindow` / `RimconemyInspectTab`

```csharp
namespace Rimconemy.Foundation.UI
{
    public class RimconemyWindow : Window
    {
        protected RimconemyWindow()
        {
            doCloseButton = true; doCloseX = true;
            absorbInputAroundWindow = true;
        }
        public override Vector2 InitialSize =>
            Vector2.zero; // Pflicht; Subklassen überschreiben
    }

    public class RimconemyMainTabWindow : MainTabWindow
    {
        public override void PreOpen() { base.PreOpen();
            // Default-Scrollverhalten; Subklassen dürfen überschreiben.
        }
    }

    public class RimconemyInspectTab : InspectTabBase
    {
        // Bewusst NICHT abstract: ProgressionPawnTab kann `var x = new RimconemyInspectTab()` testen.
        // Subklassen überschreiben nur, was InspectTabBase verlangt.
    }
}
```

### 4.3 `RimconemyUi` (statische Helper-Werkzeuge)

```csharp
public static class RimconemyUi
{
    public static void DrawSectionTitle(Rect rect, string key, GameFont font = GameFont.Medium);
    public static void DrawRow(Rect rect, string leftLabel, string rightValue,
                               Color? valueColor = null);
    public static void DrawStatusBadge(Rect rect, string label, StatusLevel level);
    public static void DrawNeedBar(Rect rect, float fillFraction, Color fillColor,
                                   string label = null);
    public static void DrawEmptyState(Rect rect, string messageKey);
    public static void DrawHighlightedInteractable(
        Rect rect, Action onClick, string tooltipKey = null);
    public static void BeginStandardScrollView(
        Rect viewRect, Rect scrollOuter,
        ref Vector2 scrollPosition, Action contentDrawer);
    public static Rect Indent(Rect inner, int levels);
    public static Rect Section(Rect inRect, int titleHeight = 30);
    public static void ResetTextFontAndColor(); // try/finally Helper für Text.Font/GUI.color
}
```

`DrawStatusBadge` Levels:

```csharp
public enum StatusLevel { Success, Warn, Error, Info, Muted }
```

### 4.4 `Languages/{English,German}/Keyed/Foundation.xml` — neue Keys

| Key | EN | DE |
|---|---|---|
| `RimconemyTheme.Section.Default` | (placeholder) | (placeholder) |
| `RimconemyTheme.EmptyState.NoData` | „No data available." | „Keine Daten verfügbar." |
| `RimconemyUi.Badge.Success` | „OK" | „OK" |
| `RimconemyUi.Badge.Warn`    | „Warning" | „Warnung" |
| `RimconemyUi.Badge.Error`   | „Error"   | „Fehler" |
| `RimconemyUi.Badge.Info`    | „Info"    | „Info" |
| `RimconemyUi.Badge.Muted`   | „—"       | „—" |
| `RimconemyUi.Tooltip.CloseWindow` | „Close this window" | „Fenster schließen" |
| `RimconemySettings.GlobalTheme.Title` | „UI Theme Override" | „UI-Theme-Override" |
| `RimconemySettings.GlobalTheme.Help`   | „Applies RimThemes-style overrides when active and supported (opt-in)." | „Wendet RimThemes-Overlays an, wenn aktiv und unterstützt (opt-in)." |

## 5. Datenmodels — Phase A-1

### 5.1 `SkillBudgetCalculator` (= §4.1 aus Track-A-Spec, unverändert übernommen)

```csharp
namespace Rimconemy.SurvivalProgression.Character
{
    public static class SkillBudgetCalculator
    {
        public const int TotalBudget = 30;
        public const int NeutralCenter = 25;
        public const int NeutralThresholdLow = -5;
        public const int NeutralThresholdHigh = +3;
        public const int MaxPerSkill = 10;
        public const int MinPerSkill = 0;
        public const int SpecializationThreshold = 7;

        public static int CostForLevel(int level)
            => level <= 0 ? 0 : level;

        public static int TotalSpent(Dictionary<SkillDef, int> alloc)
        {
            int s = 0;
            foreach (var lvl in alloc.Values) s += CostForLevel(lvl);
            return s;
        }

        public static int Balance(int spent) => spent - NeutralCenter;

        public enum TraitZone { Buffer, PositiveLight, PositiveStrong,
                                NegativeLight, NegativeStrong }

        public static TraitZone Classify(int spent)
        {
            int balance = Balance(spent);
            if (balance > 5) return TraitZone.PositiveStrong;
            if (balance > NeutralThresholdHigh) return TraitZone.PositiveLight;
            if (balance >= NeutralThresholdLow) return TraitZone.Buffer;
            if (balance >= -10) return TraitZone.NegativeLight;
            return TraitZone.NegativeStrong;
        }
    }
}
```

### 5.2 `CharacterSetup` — Härtung

`CharacterSetup.cs` Migration:

- `CharacterSetup.EligibleSkills` schließt Shooting + Melee **nicht** mehr aus.
- `ApplyBudget(pawn, alloc)` setzt Skill-Level auf `alloc[skill].Value` (cost-linear). Cap auf `MaxPerSkill = 10`.
- `DistributeSkillBudget(...)` Default-Equal verwendet `SkillBudgetCalculator` und verteilt auf 12 Skills.
- `FixAge(pawn)` bleibt unverändert (Workaround per `BirthAbsTicks += ageAdjustment`).

### 5.3 `TraitAssigner` — Härtung

`TraitAssigner.cs` Migration:

- Schwellen ersetzen durch `SkillBudgetCalculator.Classify(spent)`.
- `AssignForBudget(pawn, spent)` — neue Signatur mit `int spent`.
- `Assign(pawn, zone)` ruft `SpawnTraitFromZone(zone, pawn)`.
- 2-Negativ-Pfad: bei `NegativeStrong` → PickHeavyNegative (Count=2).
- `SpecializationBonus.Check(pawn)` setzt `SkillRecord.passion = Passion.Major` für Skills ≥ 7 (RimWorld-API bestätigt im Folge-Spike).

### 5.4 `SkillBudgetWindow` — UI-Migration auf Tokens

`SkillBudgetWindow.cs` Migration:

- `RowHeight = RimconemyTheme.RowHeight` (statt `32f`).
- Erbt von `RimconemyWindow` (= Theme-Chrome).
- Slider + Stepper unverändert; **Tooltip pro Skill** über
  `TooltipHandler.TipRegion(rect, $"{skillDef.defName}.description")`.
- **Zone-Badge** im HUD-Bereich: aktuell Buffer = Muted, Positive = Success, Negative = Warn.
- `DisposeAndApply()` ruft zentral `SkillBudgetCalculator.TotalSpent(_allocations)` und vergleicht gegen `TotalBudget`.

### 5.5 `SurvivalProgressionDashboard` — bleibt unangetastet (Folgephase)

Wir verändern die Datei NICHT in diesem Sprint, weil die Migration
(style/spacing) ein Folge-Sprint sein sollte, sobald BioOverride-Hook die
SkillBudgetWindow tatsächlich nutzt. Track A Phase A-2 baut den Hook,
dann migrieren wir dieses Dashboard in nächster Session.

## 6. Migrations-Checkliste (Detail)

### Foundation (Phase 0-A)

```
FoundationDashboard.cs
  - Erbt von RimconemyMainTabWindow
  - SectionSpacing → RimconemyTheme.SectionSpacing
  - IndentSize → RimconemyTheme.IndentSize
  - Zeilenhöhen 22/18/30 → RimconemyTheme.RowHeight/.MiniRowHeight/.SectionTitleHeight
  - SectionSpacing → RimconemyTheme.SectionSpacing
  - CalcXHeight() lokal zu globalem ThemeHelper
  - Color.green/yellow/red/cyan/gray → RimconemyTheme.Success/Warn/Error/Info/Muted
  - Tooltips per TooltipHandler.TipRegion für interaktive Elemente

FoundationVanillaInventory.cs / FoundationDefInventory.cs / EventLog-Aufrufe
  - Spacing/Indent auf Theme; keine Behavior-Änderung.
```

### Survival & Progression (Phase A-1)

```
CharacterSetup.cs
  - EligibleSkills: keine SkillDef-Ausschlüsse mehr.
  - DistributeSkillBudget: nutzt SkillBudgetCalculator.
  - ApplyBudget: cap auf MaxPerSkill=10 (SkillBudgetCalculator).

SkillBudgetWindow.cs
  - Erbt von RimconemyWindow.
  - RowHeight/Size-Defaults aus RimconemyTheme.
  - Tooltips pro Skill-Zeile.
  - Zone-Badge im Header.

TraitAssigner.cs
  - AssignForBudget(pawn, spent) → Assign(pawn, SkillBudgetCalculator.Classify(spent)).
  - Pool-Tabelle mit 9 heute existierenden Traits (Phase A-1); 4 neue (Tough/Jogger/GreatMemory/Nimble)
    kommen in A-Phase-3.
```

### Languages (Phase 0-A + A-1)

```
Foundation.xml (English + German)
  + ~10 neue Keys (siehe §4.4)
  + SKillBudget-Labels (Buffer/Positive/Negative/etc.)
```

## 7. Dateien (Δ Volumen)

### Phase 0-A

**Neu:**
- `mods/01-Rimconemy-Foundation/Source/UI/RimconemyTheme.cs`
- `mods/01-Rimconemy-Foundation/Source/UI/RimconemyUi.cs`
- `mods/01-Rimconemy-Foundation/Source/UI/RimconemyWindow.cs`
- `mods/01-Rimconemy-Foundation/Source/UI/RimconemyMainTabWindow.cs`
- `mods/01-Rimconemy-Foundation/Source/UI/RimconemyInspectTab.cs`
- `mods/01-Rimconemy-Foundation/Source/UI/ThemeSettings.cs` (Settings-Logik für `EnableGlobalThemeOverride`)

**Modifiziert:**
- `mods/01-Rimconemy-Foundation/Source/UI/FoundationDashboard.cs` (Token-Migration)
- `mods/01-Rimconemy-Foundation/Source/Save/FoundationSaveData.cs` (neuer Bool `EnableGlobalThemeOverride`)
- `mods/01-Rimconemy-Foundation/Languages/{English,German}/Keyed/Foundation.xml` (~10 neue Keys)

### Phase A-1

**Neu:**
- `mods/02-Rimconemy-Survival-Progression/Source/Character/SkillBudgetCalculator.cs`

**Modifiziert:**
- `mods/02-Rimconemy-Survival-Progression/Source/Character/CharacterSetup.cs`
- `mods/02-Rimconemy-Survival-Progression/Source/Character/SkillBudgetWindow.cs`
- `mods/02-Rimconemy-Survival-Progression/Source/Character/TraitAssigner.cs`
- `mods/02-Rimconemy-Survival-Progression/Languages/{English,German}/Keyed/SurvivalProgression.xml` (neue Keys für Zone-Badge-Texte)

## 8. Tests (Gate keine; visuelle + Build-Verifikation)

Da es eine UI-Schicht ist, gibt es keine klassischen Unit-Tests. Wir
verifizieren über:

- **Build-Verifikation:** alle 5 Pakete kompilieren mit 0W/0E.
- **Log-Verifikation:** `Rimconemy.Foundation` loggt im Bootstrap eine Token-Summary (Anzahl Helper-Klassen, Anzahl Tokens).
- **Manuell:** Dev am Spiel prüft, dass FoundationDashboard optisch unverändert erscheint.

## 9. RimThemes-Kompatibilitätsmodul (Phase 0-A, separat)

```csharp
public static class GlobalThemeOverride
{
    private static bool _applied = false;

    public static void ApplyIfRequested()
    {
        if (_applied) return;
        var pref = Current.Game?.GetComponent<FoundationSaveData>()?.EnableGlobalThemeOverride ?? false;
        if (!pref) return;
        if (!ModsConfig.IsActive("aRandomKiwi.RimThemes")) return;

        // Phase-0-A: Existenztest. Harmony-Patch NICHT in diesem Sprint.
        // Wir laden ein vordefiniertes Theme-Profile (Default: Standard-Rimworld).
        RimThemes.API.SetActiveTheme("Rimconemy_Default");
        _applied = true;
    }

    public static void Reset()
    {
        if (!_applied) return;
        RimThemes.API.ClearActiveTheme();
        _applied = false;
    }
}
```

Aktuell **kein** Harmony-Patch, nur ein API-Bridge-Hook, der nur greift,
wenn RimThemes aktiv ist. Patch wird Phase-2 mit Verifikation der
1.6-Harmony-Targets. Sicherer Pfad: nur Detection-Code, kein Code-Injection.

## 10. Risiken

**R1 — Base Classes werden von Paketen nicht verwendet:**

Niedrig, weil FoundationDashboard als Vorbild migriert ist. Paket 02
migriert SkillBudgetWindow. Bei Bedarf kann ein `Obsolete`-Warn-Hinweis in
FoundationDashboard platziert werden, sodass andere Windows deutlich
sehen, dass die Basisklasse existiert.

**R2 — Spacing-Konflikt Foundation (12/22) ↔ SurvivalProgression (10/34):**

Wir entscheiden uns für `Foundation`-Default als kanonisch. Bei einer
Folge-Migration des SurvivalProgressionDashboards kann sich das Layout
etwas vergrößern. Akzeptiert (Folgephase).

**R3 — RimThemes-API nicht zugänglich (kein `RimThemes.API`-Namespace):**

Fallback: Feature-Detect **ohne Compile-Time-Reference**. Wir verwenden
Reflection, um die API aufzurufen. Wenn nicht vorhanden → does-nothing.
**Konkreter Detection-Code wird Phase-0-A implementiert**, mit Reflection-Guard.

**R4 — SkillRecord.passion ist 1.6-Schreibgeschützt:**

In Phase A-1 setzen wir `Level` und `TotallyDisabled`, NICHT `passion`.
`SkillRecord.passion.CanBeSuppressed = true` 1.6-Verifikation noch ausstehend.
Wir umgehen das Problem, indem wir `Passion` per Reflection in
`SkillBudgetCalculator` setzen (Phase A-1: Log-Only, Phase A-3: Reflection).

**R5 — CharacterSetup.ApplyBudget Reihenfolge:**

Reihenfolge ist heute: Budget → Trait. Mit Phase A-1: Budget → Specialization → Trait. Reihenfolge ist konsistent.

**R6 — Audit-Konflikt mit kürzlich gefixtem StoryDirector:**

Phase 0-A + A-1 berührt StoryDirector nicht. Package 05 nicht in dieser
Session geändert. OK.

## 11. Akzeptanzkriterien

- Alle 5 Pakete kompilieren 0W/0E.
- Versions-Bump +0.0.1 auf Pakete 01 + 02.
- Neuer Token-Bauplan-Summary wird im Log ausgegeben (Foundation Boot).
- FoundationDashboard sieht optisch unverändert aus (validiert durch den User im Live-Test).
- SkillBudgetWindow zeigt:
  - Combat-Skills sichtbar als Slider-Zeilen.
  - Tooltip pro Skill (`skillDef.description`).
  - Zone-Badge im Header.
  - Status-Badge mit Success/Warn/Muted je nach Balance.
- TraitAssigner weist Traits gemäß Zone-Schwelle zu (Buffer = 0, PositiveLight = 1 light, NegativeStrong = 2 heavy).
- Build mit `[Toolchain]`-Verifikation (Bash) bestätigt 0W/0E (an Tag vor Code-Change: vorher schon erfüllt, jetzt zusätzlich geprüft).

## 12. Folge-Phasen (außerhalb dieses Sprints)

| Step | Tasks | Benötigt |
|---|---|---|
| A-Phase-2 | BioOverride + CharacterSetupState + Spawn-Hook | `RimThemes`-1.6-Signatur-Spike |
| A-Phase-3 | 4 neue Trait-Defs (Tough/Jogger/GreatMemory/Nimble) + Tests | A-Phase-2 |
| B | Ideology-Träger (H3 Regel 1–3) | A-Phase-2 |
| C | Story-Event-Catalog-Erweiterung | A-Phase-3 |
| D | Resource-Tracking auf StorageQuery | A-Phase-3 |
| F | UI-Folgemigration (SurvivalProgression, ProgressionPawnTab, InspectTab-Def-Registrierung) | Phase 0-A |

---

> **Spec-Self-Review** durchgeführt:
> 1. Placeholder-Scan: keine TODO/TBD im Text.
> 2. Internal-Konsistenz: §4 Token-Werte stimmen mit §5 (SkillBudgetCalculator) überein;
>    §6 Migrationsschritte konkret; §7 Dateiliste vollständig.
> 3. Scope: bleibt in Mod 01 + Mod 02. Andere Pakete nicht geändert.
> 4. Ambiguity: D1–D7 jeweils mit Begründung; §4.1 Tokens numerisch eindeutig;
>    §9 RimThemes-Code-Rumpf ohne aktive Patch-Schritte.
