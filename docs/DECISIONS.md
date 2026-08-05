# DECISIONS.md — Architektonische Entscheidungen Rimconemy

> Stand: 2026-08-04 | Status: AKTIV | Owner: Buffy (Agent) + User

---

## Übersicht

Dieses Dokument fasst alle getroffenen architektonischen Entscheidungen zusammen, die sich nicht allein durch Code-Analyse klären lassen. Jede Entscheidung ist mit Begründung, Implikationen und betroffenen Tasks dokumentiert.

**Legende:**
- ✅ = Entscheidung getroffen
- 🔄 = Offen / needs clarification
- 📋 = Implementiert

---

## 1. Need-System (S1)

**Status:** ✅ Entschieden

**Entscheidung:** B/C Hybrid — Eigene NeedDefs die das Setting transportieren, aber über Vanilla-Needs lesen.

**Begründung:**
- Eigene NeedDefs sind nötig damit das Setting sichtbar wird ("Nahrung" statt "Food")
- Aber: Keine Konflikte mit Vanilla-Mood-System und DLCs
- Lösung: Adapter-Pattern das Vanilla-Werte in Setting-Sprache übersetzt

**Implikationen:**
- `Rimconemy_Need_Food` liest Vanilla `Food`
- `Rimconemy_Need_Safety` liest Vanilla `Rest` + `Health`
- `Rimconemy_Need_Social` liest Vanilla `Recreation`
- Synchronisation alle 250 Ticks (wie ProgressionGameComponent)
- Keine eigenen Mood-Effekte (nur Anzeige)

**Betroffene Tasks:** S-T1

---

## 2. GameOver-Logic (S5)

**Status:** ✅ Entschieden (refined 2026-08-04)

**Entscheidung:** Mod 02 (`ProgressionGameComponent`) ist Sole-Owner des Vanilla-Game-Ender-Triggers. Mod 05 (`StoryDirector`) schreibt nur `StoryState.MarkGameOverPending(reason)` — der eigentliche `Find.GameEnder.CheckOrUpdateGameOver()`-Aufruf läuft ausschließlich in Mod 02 via Reflection-Bridge (`CrossPackageState.TryReadStoryGameOverPending`). Kein direkt-Cycle zwischen den Paketen.

**Begründung:**
- Reihenfolge Vanilla-Inspiration: Mod 05 erkennt Story-Ende (z.B. alle relevanten Pawns tot), Mod 02 triggert das GameOver-Screen.
- Reflection-Pattern mit Capability-Gate schützt vor DLL-Cycle zwischen 02/05 und macht beide Komponenten unabhängig deploybar.
- Die ursprüngliche Decision-Formulierung "Kein `Find.GameEnder` Aufruf bei Pawn-Tod" war Wortspiel-mi­verständlich: sie meinte "nur Mod 02 ruft, nicht jeder Pawn-Tod einzeln". Jetzt als Sole-Owner-Modell explizit.

**Implikationen:**
- Mod 02: einziger Caller von `Find.GameEnder.CheckOrUpdateGameOver()` über 250-Tick-Intervall
- Mod 05: einziger Schreiber von `MarkGameOverPending` mit Capability-Gate-Check
- Zustand: `GameOverPending` ist eine Warteschlange, kein Single-Bool — Race-sicher
- Sandbox-Pfad (§10): Sandbox-Modus zeigt "LDEN ODER ENDE"-Dialog statt direktem `CheckOrUpdateGameOver`

**Betroffene Tasks:** S-T2, S-T3

---

## 3. Storyteller (I1)

**Status:** ✅ Entschieden (reality updated 2026-08-04)

**Entscheidung:** Standalone Storyteller mit zwei Datenquellen, die zur Ladezeit zusammengeführt werden:

1. **Hardcoded Basis-Katalog** (`StoryEventCatalog.SeedHardcodedCatalog`): 12 Events across 4 Familien (Supply / Social / Raid / Collapse) als deterministisch getestete Single Source of Truth. Diese Events laden auch ohne XML-Override.
2. **XML-Def-Overlay** (`StoryEventDef : Verse.Def`): Modder können via `<StoryEventDef>`-XML bestehende Events überstimmen (`_byId[defName] = spec` ersetzt Hardcoded) oder neue Events zufügen. XML-Einträge mit ungültigen Feldern werden stumm geschluckt (Hardcoded bleibt SSOT).

**Begründung:**
- Hardcoded-Basis garantiert dass das Spiel funktioniert wenn XML-Defs fehlen — kein Boot-Dependency auf Designer-XML.
- XML-Overlay erlaubt Tuning ohne Recompile. Designer können einen Hardcoded-Event identisch benennen (`rimconemy.supply.shortage`) und Felder überschreiben.
- Deterministische Auswahl läuft gegen den finalen `_byId`-State das heißt XML-Overrides partizipieren an Determinismus/Cooldown-Logik ohne Sonderpfad.

**Implikationen:**
- `StoryEventCatalog` ist kein bloßes Refactor-zu-DefDatabase — es ist dualer Source. Die ursprüngliche Decision-Beschreibung war unterspekzifiziert.
- `MergeFromDefDatabase()` läuft im Katalog-Konstruktor. 12 hardcoded + N XML = M total.
- Modder-Variablen-API (§13) ist via `EventCondition.FromXmlExpression` realisiert, nicht über separate Modder-API-Schicht

**Betroffene Tasks:** I-T2

---

## 4. StorageHash (I5)

**Status:** ✅ Entschieden

**Entscheidung:** B (Echter Snapshot) — echte Lagerbestände aus Paket 03 lesen.

**Begründung:**
- Events auf echtem Mangel basieren lassen
- Kein Tick-basierter Platzhalter-Hash mehr
- Determinismus bleibt erhalten

**Implikationen:**
- `StoryDirector.BuildLiveSnapshot()` ruft `StorageQuery.ReadStorage()` auf
- `StorageHash = snapshot.ContentHash` statt `"live-" + tick`
- `AnyResourceCritical` basiert auf echten Schwellenwerten
- Event-Auslösung bei echtem Ressourcenmangel

**Betroffene Tasks:** I-T1

---

## 5. Markt/Preise (E1)

**Status:** ✅ Entschieden

**Entscheidung:** Eigenes Modell — deterministische Preisformel, kein Vanilla-MarketValue.

**Begründung:**
- Eigene Preisformel gibt mehr Kontrolle
- Deterministisch: gleiche Inputs → gleicher Preis
- Kein Konflikt mit Vanilla-MarketValue

**Implikationen:**
- Preis = Basis × Knappheit × Nachfrage
- `MarketStub` wird zu `Market.cs`
- Deterministische Formel statt Vanilla-Referenz
- Transaktionslog für Nachverfolgung

**Betroffene Tasks:** E-T3

---

## 6. Wallet/Trade (E3)

**Status:** ✅ Entschieden

**Entscheidung:** Credits = Wallet, Silber = Upgrade-Material. Eigenes Trade-Panel mit Einnahmen/Ausgaben.

**Begründung:**
- Zwei verschiedene Währungssysteme für unterschiedliche Domänen
- Credits für Handel, Silber für direkte Upgrades
- Trade-Panel gibt Überblick über Finanzen

**Implikationen:**
- Credits sind reine Wallet-Daten (kein Thing)
- Silber bleibt physisches Item für Upgrades
- Trade-Panel zeigt: Balance, Einnahmen/Ausgaben, Transaktionshistorie
- Outpost-Production erzeugt Credits

**Betroffene Tasks:** E-T1, E-T2, E-T4

---

## 7. Pawn-Enumeration (X1)

**Status:** ✅ Entschieden

**Entscheidung:** A (Zentral) — ColonialReader in Foundation.

**Begründung:**
- Konsistente Pawn-Enumeration über alle Pakete
- Keine doppelten Durchläufe mit unterschiedlichen Filterlogiken
- Zentrale Wartung der Pawn-Filter

**Implikationen:**
- `ColonialReader.cs` in `mods/01-Rimconemy-Foundation/Source/`
- Methode: `GetActiveColonists() → List<Pawn>`
- Pakete 02 und 05 nutzen diese Methode
- Einheitlicher Filter: `IsColonist && !Dead && !DestroyedOrNull()`

**Betroffene Tasks:** F-T1, F-T3

---

## 8. Ideology-Ownership (X4)

**Status:** ✅ Entschieden

**Entscheidung:** B (Aufteilung) — Paket 02 = Setting Rules, Paket 05 = Ideology-Zuordnung.

**Begründung:**
- Klare Verantwortungsteilung
- Paket 02: Was gilt (Regeln)
- Paket 05: Wer glaubt was (Zuordnung)
- Keine doppelte Verwaltung

**Implikationen:**
- `ThoughtWorker_ResourceFairness` bleibt in Paket 02
- `IdeologyAssigner` bleibt in Paket 05
- Paket 05 setzt `GameOverPending` als Signal (nie als Trigger)
- Capability-Checks vor Cross-Package-Zugriff

**Betroffene Tasks:** S-T4, I-T4

---

## 9. Lebensmodell (N1)

**Status:** ✅ Entschieden (stochastischer Tod-Layer gestrichen 2026-08-04)

**Entscheidung:** Rimconemy führt keinen parallelen Zufalls-Tod-Kanal. Vanilla-Mechanismen — Nahrung = 0, Blutung, Hypothermie, Krankheit, Alter — bleiben die einzigen Tod-Ursachen. Rimconemy verstärkt Vanilla-Hunger-Debuffs mit Setting-spezifischen Need-Adaptern (§1), fügt aber keinen zweiten Todeswahrscheinlichkeits-Layer hinzu.

**Begründung (Audit-Korrektur):**
- Vanilla hat bereits ein konsistentes Todesmodell. Eine parallele Zufalls-Schicht erzeugt genau die Spiel-Momente die Survival frustrierend statt fordernd machen: "mein Pawn ist gestorben obwohl ich dachte ich bin okay".
- Die ursprünglich vorgeschlagene Wahrscheinlichkeits-Tabelle (100%→0%, 50%→5%, 20%→25%, 0%→80%) hatte **kein definiertes Intervall**. Pro Tick ergeben 80% bei 60.000 Ticks/Tag ≈ 99.9997% Tagessterblichkeit; pro Tag ergibt dieselbe Zahl 80%/Tag = statistisch 1,25 Tage Überlebenszeit. Faktor-240-Unterschied bei gleicher Zahl — komplett anderes Spielgefühl, keine Selbst-Validierung der Zahl.
- Need-Adapter (§1) als Vanilla-Verstärker ist die saubere Alternative: 20% Need-Wert → schnellerer Vanilla-Hunger-Debuff, kein separater Random-Tod.

**Implikationen:**
- Kein stochastischer Tod-Tracker im Codebase
- `GameOverDetector` (§2) ist Sole-Owner des Story-Ends, NICHT von individuellem Pawn-Tod — Vanilla-Tod und Game-Over sind getrennte Layer
- Need-Adapter beeinflusst Vanilla-Hunger-Rate, Mood-Restoration, Comfort-Debuffs — kein Tod
- Komplexität-Reduktion: ca. 40 Zeilen weniger in zukünftiger Story-Engine-Implementierung

**Betroffene Tasks:** S-T2 (re-scoped), S-T1 (Need-Adapter bleibt)

---

## 10. GameMode (N2)

**Status:** ✅ Entschieden (Load → Softcore umbenannt 2026-08-04)

**Entscheidung:** GameMode wird im NewGame-Screen gewählt. Bei 0 Colonists unterscheidet sich das Verhalten — Hardcore triggert Vanilla-Game-Over, Softcore lädt automatisch den letzten Save, Sandbox zeigt "LDEN ODER ENDE"-Dialog.

**Begründung:**
- Hardcore: klassisches Survival — alle Pawns tot = Run vorbei. Kein Auto-Load.
- Softcore: Auto-Load des letzten Save-Slots wenn alle Pawns tot sind. Spieler verliert Fortschritt zwischen Saves — sachlich fordernd ohne Permadeath.
- Sandbox: keine Auto-Reaktion. Spieler entscheidet selbst via "LDEN ODER ENDE"-Dialog ob er lädt oder das Ende akzeptiert.

**Naming-Korrektur (Audit 2026-08-04):** Die ursprüngliche Wahl `Load` war kein Mode sondern eine Tod-Reaktion-Aktion. Umbenannt zu `Softcore` für semantische Klarheit.

**Implikationen:**
- `GameMode` Enum: `Hardcore | Softcore | Sandbox`
- UI im NewGame-Screen für Modus-Auswahl
- Sandbox-Pfad: "LDEN ODER ENDE" als neuer GameState (siehe §2)
- Softcore-Pfad: Hook auf Save-Load-Cycle, Restore des letzten Autosaves
- GameMode wird mit Scribe persistiert (Save-Stable)

**Betroffene Tasks:** S-T3

---

## 11. Trade-Panel-Design (N3)

**Status:** ✅ Entschieden

**Entscheidung:** Eigenes Trade-Panel mit Einnahmen/Ausgaben und Transaktionshistorie.

**Begründung:**
- Überblick über Finanzen nötig
- Einnahmen/Ausgaben-Tracking für Balance
- Transaktionshistorie für Nachverfolgung

**Implikationen:**
- UI-Design mit Wallet-Balance, Einnahmen/Ausgaben, Transaktionshistorie
- Silber als separater Posten für Upgrades
- Upgrade-Panel mit Kosten in Silber

**Betroffene Tasks:** E-T2

---

## 12. Need-Adapter-Synchronisation (N4)

**Status:** ✅ Entschieden

**Entscheidung:** Adapter-Pattern mit Synchronisation alle 250 Ticks.

**Begründung:**
- Vanilla-Werte in Setting-Sprache übersetzen
- Regelmäßige Synchronisation für Konsistenz
- Keine eigenen Mood-Effekte (nur Anzeige)

**Implikationen:**
- Adapter-Klasse die Vanilla-Need-Values liest
- Mapping: Vanilla → Rimconemy-Need
- Synchronisation alle 250 Ticks (wie ProgressionGameComponent)
- Keine eigenen Effekte (nur Anzeige)

**Betroffene Tasks:** S-T1

---

## 13. Event-Variablen (N5)

**Status:** ✅ Entschieden (reality updated 2026-08-04)

**Entscheidung:** Jedes Event hat zwei Template-Layer:

1. **`DeterminismKeyTemplate`** (Determinismus-Slot): 7 kanonische Platzhaltern — `{ProfileId}`, `{EventId}`, `{StorageHash}`, `{IdeologyTension}`, `{ThreatPressure}`, `{PawnId}`, `{GameTickDay}`. Diese werden vor dem Idempotency-Check eingesetzt — sie sind Spielstand-determinantisch und entscheiden ob ein Event im aktuellen Spielstand neu ist oder bereits gefeuert hat.
2. **`LetterText`/`Label`/`Description` (Brief-Text-Slot)**: `{Variable}`-Platzhaltern für Setting-Texte. Werden zur Brief-Zeit ersetzt.

**Variablen-Resolution-Pattern:**
- `EventCondition.FromXmlExpression(raw)`: drei Parser-Pfade.
  - `NOT Name(arg)` → wird zu `Name, args, Description = "NOT ..."`  (Negation als Template-Marker).
  - `Name(arg)` → wird zu `ConditionId = Name, Parameter = arg` (kanonischer Condition-Aufruf).
  - freier Text (z.B. deutsche Designer-Beschreibung) → bleibt als Description stehen, ConditionId = `"FreeText"`. Der Evaluator überspringt diese Zeile still — Designer-Bug blockt nie das Event.

**Modder-API (geplant, Phase-3):** Eigene `ConditionId`-Werte können via Interface registriert werden. Aktueller Stand: 6 fest verdrahtete `EventCondition.ActiveX()`-Konstruktoren decken die internen Cases.

**Implikationen:**
- 7 deterministische Slots garantieren Save-/Load-Replizierbarkeit
- Brief-Texte unterstützen unbegrenzt freie Variablen — kein Maximum
- FreeText-Fallback ist defensiv, nicht destruktiv: ein Designer-Bug loggt einen Debug-Hinweis ohne Event-Blockade

**Betroffene Tasks:** I-T2

---

## 14. StorageSnapshot-Schwellenwerte (N6)

**Status:** ✅ Entschieden + Implementiert (slop-audit-fix 2026-08-04)

**Entscheidung:** Kritische Ressourcen müssen definiert werden. Slop-audit-fix H4 vom 2026-08-04 hat die "Mindest-Fraktion"-Semantik gepraezisiert:

- **Mit Mod 03 TargetStock** (geplant fuer spaeteres Release): die 0.20/0.10/0.15 sind Mindestquoten (food>=20%, medicines>=10%, materials>=15% des TargetStock).
- **Ohne TargetStock** (heute: `StorageEntry` hat kein `TargetStock`-Feld): die Implementation faellt auf **absolute Units 50/30/40** zurueck (conservative Werte). Beide semantiken ergeben denselben Trigger fuer die typischen RimWorld-Storage-Volumes.

**Begruendung:**
- Schwellenwerte fuer Event-Ausloesung noetig
- Konsistente Definition ueber alle Pakete
- Konfigurierbar fuer verschiedene Schwierigkeitsgrade

**Implikationen:**
- `CriticalFoodFraction = 0.20f`, `CriticalMedicineFraction = 0.10f`, `CriticalMaterialFraction = 0.15f`
- Standalone-Defaults: `CriticalFoodUnits = 50`, `CriticalMedicineUnits = 30`, `CriticalMaterialUnits = 40`
- `ResourceThresholds.IsBelowCritical(resourceId, currentAmount)` ist die kanonische Single-Source-of-Truth (Mod 05)
- Event-Ausloesung wenn Schwellenwert unterschritten

**Betroffene Tasks:** I-T1, slop-audit-fix C5/H4

---

## 15. DLC-Content-Policy (X-DLC)

**Status:** ✅ Entschieden + Implementiert (Phase 1+2, Version 0.1.34 ff.)

**Entscheidung:** Foundation (`Source/DLC/DLCContentPolicy.cs`) ist Single Source of Truth für alle DLC-Content-Entscheidungen. 21 `static readonly bool`-Flags in 5 Subklassen (`Royalty`, `Ideology`, `Anomaly`, `Odyssey`, `Biotech`). Optionaler XML-Override (`Defs/DLCContentPolicy_Default.xml`) via Reflection auf `static readonly`-Felder zur GameStart-Zeit.

**Begründung:**
- Hardcoded Phase-1: einmal gesetzt, immer so. Einfachste Variante, kein Config-Overhead.
- XML-Override Phase-2: Designer oder Spieler kann Werte ohne Code-Rebuild überschreiben für Custom-Profile.
- Reflection-Apply wird über `DLCPolicyComponent : GameComponent.FinalizeInit` defense-in-depth nochmal angewandt falls der Static-Constructor die Defs zu früh liest.

**Implikationen:**
- Konsumenten (Mods 02-05) lesen via `DLCFilter.IsContentEnabled(contentId)` — Default ist `false` für unbekannte IDs.
- 3 Patch-Files (`Patches/DLC_Royalty_Suppress.xml`, `DLC_Biotech_Suppress.xml`, `DLC_Anomaly_KillEntitiesVoid.xml`) suppressen concrete Defs bedingt auf `PatchOperationFindMod`-Conditional — sie machen die Policy real.
- Bootstrap loggt beim Startup die aktive Policy-Summe als Bootstrap-Summary.

**Betroffene Tasks:** X-DLC-T1 (Phase 1+2 ✓)

---

## 16. DLC-Hard-Requires (X-REQ)

**Status:** ✅ Entschieden + Implementiert (About.xml, Foundation 0.1.23 ff.)

**Entscheidung:** Mod 01 (Foundation) About.xml deklariert Anomaly (`Ludeon.RimWorld.Anomaly`) und Odyssey (`Ludeon.RimWorld.Odyssey`) als `<modDependencies>` Hard-Requires. Royalty, Ideology, Biotech bleiben optional (Suppress-Installed-Pattern).

**Begründung:**
- Anomaly ist Voraussetzung für die Infizierte-PawnKind-Erbung (Mod 05 §19). Ohne Anomaly startet das Spiel mit halbierter Bedrohungs-Domäne was ohne klares Signal zu Verwirrung führt.
- Odyssey ist Voraussetzung für die Gravship-Territory-Mechanik (Mod 04 §18). Ohne Odyssey kollabiert das Territory-Engine-Konzept.
- Royalty/Biotech/Ideology sind nicht erforderlich — sie werden bei Vorhandensein via Patches konditioniert unterdrückt (§15).

**Implikationen:**
- Spieler sieht klare Erwartung im Mod-Manager: "Anomaly und Odyssey sind nötig"
- Kein stilles Failure-Mode mehr: Mod startet gar nicht, sondern gibt deutliche Warnung
- Ideology bleibt opt-in (Fixed-Ideo via StoryDirector)

**Betroffene Tasks:** X-REQ-T1 ✓

---

## 17. Construction-Cost-Patches (X-BUILD)

**Status:** ✅ Teil-implementiert (Stand 2026-08-04) — Patch-File existiert; Sandbag/Barricade-Detail bleibt offen

**Entscheidung:** Vanilla-Wände und -Türen werden in Rimconemy-Bauschutt-Werte umgemappt. **Implementierter Mechanismus (2026-08-04):** Bauschutt wird über `stuffProps` (`Stony`) in `ConstructionDebris.xml` zu einem baufähigen Material gemacht; `Bauschutt_Remap_Patches.xml` remappt die `Wall`-/`Door`-Defs auf die Bauschutt-Bauweise (ursprünglich geplanter `costList`-Replace wurde durch stuff-basiertes Konstruieren ersetzt). Sandbags/Barricades/Barbed-Wire bleiben Vanilla für MVP.

**Begründung:**
- "Bauschutt ist dein erster Freund" ist Kern-Versprechen des Pitch — Vanilla-Builds kosten Stein oder Holz, das Setting verfehlt sich ohne Patches.
- Sandbags sind in RimWorld's Game-Logik anders behandelt (cover-Wert nicht cost), eigene Entscheidung steht aus.

**Implikationen (Ist-Stand):**
- `mods/03-Rimconemy-Scavenger-Infrastructure/Patches/Bauschutt_Remap_Patches.xml` + `stuffProps` (`Stony`) in `ConstructionDebris.xml` — **angelegt (2026-08-04)**; Runtime-Beleg über Falsifizierungsbericht `ConstructionDebris` ausstehend
- StorageFilter-XML für Bauschutt-Kategorie (Phase-B+)
- DLC-Conditional-Patches wenn Royalty-Build-Costs anders sind (Phase-3)

**Betroffene Tasks:** X-BUILD-T1 (teilweise; Rest im Falsifizierungs-Gate §8.2)

---

## 18. Gravship-Territory-Motor (M4-GRAV)

**Status:** ✅ Entschieden (Spezifikation; Code-Implementation post-MVP)

**Entscheidung:** Odyssey's Gravship ist der **primäre Expansions-Mechanismus** für Package 04 (Economy & Territory). Er ersetzt den ursprünglich geplanten statischen Territory-Graph (`MainBase → Proxy → Outpost`). Territory-Node wird über Gravship-Lifecycle definiert: Landung auf Tile → Kampf → Tile wird Node → Infizierter-Druck kann Node angreifen → Spieler muss Gravship zurückschicken oder Node verteidigen.

**Begründung:**
- Statischer Graph war nie implementiert; ein Odyssey-DLC-Schiff ist architektonisch eleganter und sichtbar.
- "Empire ohne Imperium" — Macht über Tile-Kontrolle ohne Titel-System (Royalty ist deaktiviert, §15).
- RimWar-Analogie: Infizierte-Fraktion kann Territory-Node angreifen und übernehmen wenn Spieler abwesend.

**Implikationen:**
- `OutpostWorldObject.cs` (Mod 04) wird Extension-Point auf Odyssey's Gravship-WorldObject — keine eigene WorldObject-Klasse von Grund auf.
- TerritoryNode wird zum Gravship-State-Tracker (welches Tile aktiv, welche gesichert, welche angegriffen).
- Ohne Odyssey (Hard-Require §16): Mod 04 läuft nicht. Statischer Fallback wird es nicht geben — der Gravship ist die Implementierung.
- `ThreatAggregator` (Mod 05) bekommt eine WorldMap-Dimension: pro Tile wird `InfectedPressure` getrackt.

**Betroffene Tasks:** M4-GRAV-T1 (Spezifikation; Code: Phase 3+)

---

## 19. Anomaly-Shambler Infected-Basis (M5-INF)

**Status:** ✅ Entschieden (Spezifikation; Code-Implementation Phase-B+)

**Entscheidung:** Mod 05 (Infected & Automation) erbt Rimconemy-Infizierte-PawnKinds von Vanillas `Shambler` PawnKindDef als `ParentName`. Eigene Modifikationen überschreiben Hediff-Struktur, Texturen und Aggression-Defaults. `InfectedRaidWorker` (Mod 05) spawnt die Shambler-basierten PawnKinds statt eigene von Grund auf.

**Begründung:**
- Shambler ist bereits das was Rimconemy braucht: infizierte Menschen, keine Fraktion, eigene Storyteller-Comps, visuell konsistent.
- Eigenbau von Grund auf wäre Code-Duplikation — Texture-Modelling, Hediff-Chains, Flee-Behavior, alle schon in Anomaly.
- Anomaly ist Hard-Require (§16) — die Hard-Require-Entscheidung stützt diese Decision.

**Implikationen:**
- `Rimconemy_InfectedPawnKindDef` ist abgeleitet von `Shambler` via `<parentName>` im XML.
- PawnKind-Erweiterungen via `<modExtensions>`-Slot oder Patch-Overlay auf Hediff-Defs.
- Anomaly-Storyteller-Comps werden via Patch suppressed (siehe `DLC_Anomaly_KillEntitiesVoid.xml`) — nur die PawnKind-Erbung wird genutzt, nicht Anomaly's eigener Director.

**Betroffene Tasks:** M5-INF-T1 (Spezifikation), I-T3 (Phase-B+ Code)

---

## 20. DLC-Detail-Ergänzungen (aus ehem. DECISIONS_DLC.md, Lock 2026-08-04)

> Konsolidiert aus `docs/DECISIONS_DLC.md` (Archiv: `docs/archive-md-2026-08-04.tar.gz`). §15–§19 bleiben kanonisch; §20 ergänzt die dort fehlenden Detail-Levels.

### 20.1 Architektur-Prinzip: Opt-In statt Opt-Out

Rimconemy kontrolliert, welche DLC-Inhalte aktiv sind — nicht die RimWorld-Defaults. Ein zentraler Gate (`Foundation.DLC.DLCFilter`) entscheidet; alle fünf Pakete fragen ihn, niemand greift direkt auf `ModsConfig.*Active` zu. Capability `rimconemy.foundation.dlc_filter` v1 registriert den Gate.

### 20.2 F4 Biotech (Mostly-Suppress) — Detail

- Mechanitor + Mech-Gestator: deaktiviert via `Biotech_DisableMechSystem.xml`.
- Children-System: deaktiviert via `Biotech_DisableChildren.xml`.
- Pollution: suppressed by default (`DLCFilter.IsContentEnabled("biotech.pollution") = false`); optional später über FoundationSaveData-Toggle aktivierbar.
- Genome-Editing + Toxifier Generator: suppressed. Mechadroids nutzen eigenen PawnKind (Shambler-Basis), nicht Biotech-Mechanoid-Bandwidth.

### 20.3 F5 Ideology (Trim-UX) — Detail

- PreceptDef/ThoughtDef/ThoughtWorker bleiben aktiv (technischer Träger).
- Ideoligion-Founder-Screen deaktiviert via `Ideology_RemovePlayerUI.xml`; Ritual-Performer-UI deaktiviert; Player-Edit-Ideology disabled.
- `IdeologyAssigner` weist Rimconemy-Setting-Ideologie automatisch zu (`Rimconemy_Ideo_Survival/Refuge/Collapse`).

### 20.4 DLC Content-IDs (Registry-Strings für Code + Patches)

| Content-ID | Aktiv? | Quelle |
|---|---|---|
| `royalty.psycasts` | ❌ nein | F3 |
| `royalty.shuttles` | ❌ nein | F3 |
| `royalty.imperial` | ❌ nein | F3 |
| `biotech.mechanitor` | ❌ nein | F4 |
| `biotech.children` | ❌ nein | F4 |
| `biotech.pollution` | ❌ nein (toggleable) | F4 |
| `anomaly.shamblers` | ✅ wenn Anomaly aktiv | F1 |
| `anomaly.entities` | ❌ nein | F1 |
| `anomaly.hold` | ❌ nein | F1 |
| `odyssey.gravship` | ✅ wenn Odyssey aktiv | F2 |
| `odyssey.fishing` | ❌ nein | F2 |
| `ideology.player_founder` | ❌ nein | F5 |
| `ideology.ritual_ui` | ❌ nein | F5 |

### 20.5 Item-Inventar (Architektur-Defaults, Auswahl)

- **Strukturen:** Wall/Door behalten + Cost→Bauschutt (Patch `mods/03/Patches/Bauschutt_Remap_Patches.xml`, §17); Sandbag, Barricade, Barbed Wire, Embrasure behalten (vanilla).
- **Power:** Solar/Wind/Geothermal/Battery behalten; Rimconemy-Generatoren (WoodCoal, WaterTurbine, TurbineWaterPump) aktiv (Mod 03); Toxifier Generator suppressed (F4).
- **Production:** Crafting Spot/Smithy/Smelter/Fueled Smithy vanilla; Mech Gestator suppressed; Bioferrite Processor (Anomaly) suppressed; Fishing Dock (Odyssey) suppressed.
- **Sicherheit:** Turret_MiniTurret behalten (parallel zu `Rimconemy_ArrowTurret_Power`); IED/Spike Traps vanilla; Ballista (Royalty) suppressed.

### 20.6 Patch-Layer-Standard (RimWorld 1.5+)

Zwei dokumentierte XML-Fixes (2026-08-04): Scenario-Root muss `<Defs>` sein; `PatchOperationTest` nutzt `<case>`/`<nomatch>` (nicht `<match>`). Field-Name-Matrix:

| PatchOp-Klasse | Field wenn xpath matched | Optionaler Else-Branch |
|---|---|---|
| `PatchOperationFindMod` | `<match>` | `<nomatch>` |
| `PatchOperationTest` | `<case>` | `<nomatch>` |
| `PatchOperationAddModExtension` | `<value>` | (kein else) |
| `PatchOperationSet` | `<value>` | (kein else; no-op bei no-match) |
| `PatchOperationRemove` | (kein Wert) | (kein else) |
| `PatchOperationSequence` | `<operations>/<li>` | (iteriert intern) |
| `PatchOperationAdd` | `<value>` | (kein else) |

Convention: xpath im Test und inner-Op identisch; `defName`-Lookups sind renamings-anfällig → Live-Test-Doppelcheck.

### 20.7 Out-of-Scope (vor Phase 7 nicht angefasst)

Biotech-Pollution × Wasser, Odyssey Travel-Events × Territory-Discovery, Biome-Caravan-Camps, StyleDef-Anpassung an Ideology-Style („kein Style-Remapping“).

---

## 21. Harmony-Strategie (aus ehem. docs/AUDIT.md §1–§3, 2026-08-04)

> Konsolidiert aus `docs/AUDIT.md` (Archiv: `docs/archive-md-2026-08-04.tar.gz`). Kurzfassung auch in `INTERFACE_CONTRACT.md §10`.

**Entscheidung:** Harmony-Minimierung zugunsten nativer RimWorld-Anker. Keine Implementierungslücke, sondern architektonische Grundsatzentscheidung (Stabilität, Kollisionsrisiko, Wartbarkeit).

**Anker-Hierarchie (Priorität 1 > 5):**

```
1. Defs / PatchOperation-XML          → Daten- und Inhaltsänderungen
2. [StaticConstructorOnStartup]        → Boot-Reihenfolge, Registry, einmalige Init
3. GameComponent / WorldComponent      → Runtime-State, Persistenz, Ticks
4. Harmony (Prefix/Postfix)            → Nur wenn 1–3 nicht reichen
5. Harmony (Transpiler)                → Nur nach gescheitertem Prefix/Postfix-Spike
```

**Aktive Patches:** `Page_ConfigureStartingPawnsBioPatch` (Mod 02, `[HarmonyPostfix]`) — einziger Weg, vor dem ersten Rendern des Customization-Screens einzugreifen. Kein Transpiler im gesamten Projekt; alle 5 Mods nutzen `brrainz.harmony` als `modDependency` (keine eigene `0Harmony.dll`).

**Falsifikation:** Ein dokumentierter Spike (`API-STORYTELLER-01`/`API-ANCHOR-01`) muss belegen, dass ein direkter Zugriff zwingend Harmony erfordert; bis dahin bleibt die Minimierung aktiv. Vor jedem neuen Harmony-Patch: IL-Body-Existenz per Decompile prüfen (`API-ILBODY-01`); Transpiler nur nach gescheitertem Prefix/Postfix-Spike und BypassGate-Freigabe.

---

## 22. Scribe-/ExposeData-Pattern (aus ehem. docs/AUDIT.md §4, 2026-08-04)

**Entscheidung:** Kein `LookMode.Reference` ohne parallele Backing-Liste (häufigstes NRE-Muster in RimWorld-Mods). Aktuelle Nutzung:

| Klasse | Scribe-Methode | LookMode | Backing-Listen? | Risiko |
|---|---|---|---|---|
| `StoryState.cs` | `Scribe_Collections.Look` (Dict/HashSet) | `LookMode.Value` | ✅ Parallel-Listen | Gering |
| `FoundationSaveData.cs` | `Scribe_Collections.Look` (`List<string>`) | `LookMode.Value` | Nein | Gering |
| `CreditsLedger.cs` | `Scribe_Collections.Look` (`List<Transaction>`) | `LookMode.Deep` | Nein | Mittel (ungetestet) |
| `ProgressionGameComponent.cs` | `Scribe_Collections.Look` | `LookMode.Deep` + `Value` | ✅ Null-Checks | Gering |

**Empfohlene Falsifikationstests (Phase 3+):** Roundtrip (Serialisierung → Deserialisierung → Wertevergleich), `LookMode.Reference` mit fehlender Backing-Liste → NRE-frei, Schema-Migration v0 → Current ohne Datenverlust.

---

## 23. GameComponent/MapComponent-Zuordnung (aus ehem. docs/AUDIT.md §5, 2026-08-04)

**Entscheidung:** Globale Daten → `GameComponent`; per-Map-Daten → `MapComponent`.

| Komponente | Mod | Typ | Zweck |
|---|---|---|---|
| `FoundationSaveData` | 01 | GameComponent | Persistenz, Schema-Version, Migration |
| `ProgressionGameComponent` | 02 | GameComponent | XP-Sampling, Game-Over-Detection |
| `MapMarketComponent` | 04 | MapComponent | Per-Map-Markt-Snapshot |
| `WalletGameComponent` / `OutpostService` | 04 | GameComponent | Wallet/Outpost-Persistenz (`SilverGameComponent` entfernt 2026-08-05 — Credits-Entscheidung „never silver", kein Caller) |
| `StoryDirector` | 05 | GameComponent | Story-Evaluation, Tick-basiert |

**Weitere bestätigte Audit-Befunde (docs/AUDIT.md §6–§10):** Namespace-Trennung korrekt; Decompile als API-Verifikation etabliert; keine HugsLib-Abhängigkeit; alle 5 Bootstrap nutzen `[StaticConstructorOnStartup]` mit `Current.Game == null`-Guard; HarmonyRimWorld als Shared Dependency (keine eigene DLL in allen 5 Mods).

---

## 24. Early-Game-Munitions- und Hochofenvertrag (E4-SURVIVAL)

**Status:** ✅ Entschieden (Design-Lock 2026-08-04; Implementierung offen)

**Entscheidung:** Der Early-Game-Kampf bleibt ein begrenzter Survival-Druckpuffer und wird nicht zu einer Pflichtabhängigkeit auf Combat Extended.

- Der Einzelstarter erhält eine definierte, knappe Startwaffe und Startmunition über Charakter-/Szenariovertrag und Startinventar.
- Der garantierte Startgegner ist ein Drucktest, kein Loot-Automat: Es gibt **keinen garantierten Gegner-Drop** für Munition, Stahlreste oder Maschinenteile.
- Ruinen dürfen Munition, Stahlreste oder technische Teile nur zufällig liefern. Der Start muss auch ohne diesen Fund spielbar bleiben.
- Wegen fehlender Munition werden **keine Vanilla-Arbeitstypen deaktiviert oder verweigert**. Arbeit bleibt über die normalen WorkGiver-/Job-Regeln verfügbar.
- Die spätere Munitionsproduktion ist ein physischer Midgame-Pfad: **Stahl → Munition im elektrischen Hochofen**, wobei ausgewählte Rezepte Kohle über die Ofen-`Refuelable`-Mechanik verbrauchen. Der Generator verbraucht Kohle separat für das PowerNet.
- Der elektrische Hochofen ist eine **T2-Energy-Capability**. Der konkrete Bau-/Forschungs- und Rezeptpfad benötigt mindestens einen geeigneten Steinbaustoff (Kalkstein, Sandstein oder Granit) sowie Eisen/Stahl und wird vor Implementierung lokal gegen RimWorld-1.6-Defs geprüft.
- Combat Extended bleibt optional. Der Rimconemy-Core definiert keine CE-Pflicht und darf ohne CE keinen Phantom-Munitionsstatus erzeugen.

**Begründung:** Die Startressourcen sollen Entscheidungen erzwingen, aber keinen zufälligen Softlock erzeugen. Gegner erzeugen Aufmerksamkeit; Produktion und Infrastruktur erzeugen Stabilität. Der Übergang Survival → Energie → Produktion bleibt dadurch sichtbar und paketübergreifend anschlussfähig.

**Betroffene Verträge:**

- Paket 02: Startcharakter, Startinventar, Arbeitstypen und Survival-Druck.
- Paket 03: Stahl als physischer Rezeptinput, Kohle als Ofen-/Generator-Refuelable, elektrischer Hochofen und Munitions-Thing-/Recipe-Defs.
- Paket 05: Startgegner-/Nachtbedrohung ohne garantierte Beute.
- `docs/COMPATIBILITY_MATRIX.md`: CE bleibt Adapter-/Kompatibilitätsfall, nicht Core-Abhängigkeit.

**Nicht behauptet:** Diese Entscheidung belegt noch keine vorhandene Startwaffe, keinen echten Munitionsverbrauch, keinen elektrischen Hochofen, kein Research-Unlock und keinen Live-Nachtspawn. Diese Punkte bleiben eigene CODE/DEF/COMPILES/LIVE-Gates.

## 25. Erfahrungsbaum statt Forschungsbaum (S-PROG-EXPERIENCE)

**Status:** ✅ Entschieden (Design-Lock 2026-08-04; Implementierung offen)

**Entscheidung:** Rimconemy verwendet für die Spielerprogression keinen klassischen Forschungsbaum mit Forschungstisch, Forschungspunkten und Warteauftrag als primäre Freischaltlogik. Wissen entsteht aus tatsächlich abgeschlossenen Handlungen:

```text
Handlung
  → gültiges Ergebnis
  → Erfahrung in einem passenden Bereich
  → Bereichsstufe / Wissen
  → Freigabe
  → neues Architektenmenü-Rezept oder Gebäude
  → neue Handlung
```

**Spielerregel:** Der Spieler beginnt mit einem fast leeren Architektenmenü („Notlager“). Campfire, Schlafplatz, Lagerzone und Sammeln bilden den Start. Das erste erfolgreich entzündete Campfire öffnet den Pfad **Zuflucht**; eine abgeschlossene Holz-Stahl-Barrikade (1 Holz + 1 Stahlrest) erzeugt Bau-Erfahrung und kann weitere Schutzoptionen freigeben.

**Erfahrungsbereiche:** Überlebenswissen, Bergung, Feuerwissen, Baukunst, Verarbeitung, Maschinenwissen und Verteidigung. Technisch dürfen diese Bereiche numerische Stufen besitzen; sichtbar werden sie als Unkundig, Anfänger, Geübt, Erfahren oder Meisterhaft.

**Gültiges Ergebnis:** Erfahrung entsteht nur nach physisch bestätigtem Abschluss — zum Beispiel erfolgreich gesammelte Ressource, entzündetes Campfire, fertiggestellte Wand, abgeschlossene Kohleherstellung, tatsächlich betriebener Generator oder überstandene vorbereitete Gefahr. Platzieren, Abbrechen, Menüöffnen, bloßes Verschieben und Campfire-Spam erzeugen keine gültige Erfahrung.

**Anti-Exploit-Regeln:** Jeder Bereich erhält Idempotency-Keys, Diminishing Returns für Wiederholungen, Boni für neue Rezepte/Materialien und situative Krisenboni sowie eine Begrenzung für triviale Aktionen. Ein billiges Objekt darf nicht endlos gespammt werden, um Meisterschaft zu erzeugen.

**Voraussetzungen:** Eine Freigabe hängt nicht nur an einer Stufe. Sie kann zusätzlich ein echtes Ergebnis verlangen, etwa „Kohle mindestens einmal hergestellt“, „Maschinenteile verarbeitet“ oder „stabile Energie erlebt“. Dadurch bleibt der Erfahrungsbaum handlungsgebunden.

**Forschungstisch und Vanilla-Forschung:** Vanilla-`ResearchProjectDef`s werden nicht global gelöscht, damit DLCs und andere Mods kompatibel bleiben. Sie sind jedoch nicht die Rimconemy-Quelle der Spielerfreigaben. Die Forschung kann als Übersicht/Kompatibilitätsansicht anzeigen, was gelernt wurde und was möglich wird; Rimconemy-Freigaben benötigen keinen Forschungstisch und dürfen keine parallele, widersprüchliche Fortschrittsbahn bilden.

**Eigentum:** Paket 02 besitzt Erfahrungsbereiche, Stufen, Freigaben und abgeschlossene Aktions-IDs. Paket 03 besitzt physische Defs, Rezepte, Bauwerke und meldet nur physisch bestätigte Outputs. Paket 05 liest Schutz-/Wissensfolgen für Bedrohung; Paket 04 erhält seine Handels-/Grenzfreigaben erst aus bestätigten Versorgungsvoraussetzungen.

**Nicht behauptet:** Der Erfahrungsbaum, organische Architektenfreigaben, echte Abschluss-Hooks, Diminishing Returns und die vollständige Save-/Load-Persistenz sind noch keine Code-, Def- oder Live-Belege.

## 26. KALT und Wärme als Hediff-Severity-Offset (E4-WARM)

**Status:** ✅ Entschieden (Design-Lock 2026-08-04; Implementierung offen)

**Entscheidung:** Der Kältezustand wird als RimWorld-Hediff (`Rimconemy_ColdExposure`) mit gestaffeltem Severity-Offset über `statOffsets` modelliert — nicht als zweiter Need, nicht als Override der Vanilla-Thermoregulation, nicht als eigener Tod-Kanal.

```text
Stages:
  fröstelnd (0.25..0.6):  MoveSpeed -5 %, WorkSpeedGlobal -5 %
  kalt    (0.6..1.0):      MoveSpeed -12 %, WorkSpeedGlobal -12 %
```

**Begründung:**
- Ein eigener Need würde Vanilla-Mood und DLC-Hediffs doppeln (§1 Anti-Pattern). Ein Hediff mit `statOffsets` bleibt in der RimWorld-Pipeline und respektiert Kleidung, Temperaturbereiche und Fire-Glower automatisch.
- Der Vanilla-Tod durch Hypothermie bleibt die einzige Kältesterben-Ursache (§9 unsere Anti-Softlock-Position). Hediff ist *Druck*, nicht *Tod*.
- Die Severity-Update-Rate wird über einen bounded Map-/Game-Tick (`ColdExposureService.Update`) aktualisiert, nicht durch freies Tick-Sampling. Dadurch bleibt die Last berechenbar und Save-stabil.

**Spike-Pflicht:** `ReadVanillaTemperature(pawn)` exakte 1.6-API vor Implementierung bestätigen — kein `strings`-Beweis. Genaue Bezugspunkte sind `RoomTemperature`/`GenTemperature` oder pawn-spezifische `Zone`/`TemperatureZone`-Felder; verbindlich wird die 1.6-Signatur per Decompile/Reflection.

**Eigentum:** Paket 02 (`Source/Needs/ColdExposureService`) liest Vanilla-Temperatur, mutiert die Hediff-Severity und meldet das Resultat nicht an andere Pakete. Paket 03 liest `ShelterSnapshot.EnclosureScore` + `FireSignature` und kann die KALT-Hediff-Sensitivity kontextualisieren — schreibt aber nicht direkt.

**Negativ-Pfad:** Feuer reduziert KALT durch Erhöhung der lokalen Temperatur (Vanilla); Nacht/Kälte erhöht KALT durch Vanilla-Source. Der Hediff ist nur eine *Anzeige- und Modifikationsschicht*, kein Ersatz für den Thermal-Layer.

**Bezug zu §24:** §26 folgt der Anti-Softlock-Linie aus §24 (kein garantierter Drop, keine Vanilla-Arbeitstypen-Sperre). KALT ist *zusätzlicher Druck*, kein Ersatz für Vanilla-Hypothermie-Tod oder eine globale Spielersperre. §9 (gestrichener stochastischer Tod-Layer) bleibt bestehen — §26 zitiert §24 als inhaltlichen Bezugspunkt.

**Betroffene Tasks:** Phase 3.1/3.2 aus `ROADMAP.md §9.1` (integrierte Early-Game-Vertikalscheibe).

**Nicht behauptet:** Diese Entscheidung belegt noch keinen Hediff-Def, keinen bounded Update-Service und keinen Live-Temperaturwert. Die Punkte bleiben CODE/DEF/COMPILES/LIVE-Gates.

## Zusammenfassung: Implementierungsreihenfolge

| Reihenfolge | Task | Beschreibung | Blockiert |
|-------------|------|--------------|-----------|
| 1 | F-T1 | ColonialReader.cs erstellen | F-T3 |
| 2 | S-T1 | Need-Adapter implementieren | — |
| 3 | S-T2 | GameOver-Wahrscheinlichkeit | S-T3 |
| 4 | S-T3 | GameMode-Enum + UI | — |
| 5 | S-T4 | ThoughtWorker in Paket 05 verschieben | I-T4 |
| 6 | I-T1 | StorageSnapshot-Bridge | — |
| 7 | I-T2 | XML-Event-System | — |
| 8 | I-T4 | ThoughtWorker übernehmen | — |
| 9 | E-T1 | CreditsLedger erweitern | E-T2 |
| 10 | E-T2 | TradePanel UI | — |
| 11 | E-T3 | Market.cs (Preisformel) | — |
| 12 | E-T4 | Silber als Upgrade-Material | — |
| 13 | F-T3 | ColonialReader einbinden | — |
| 14 | X-DLC-T1 | DLCContentPolicy: Phase-1 hardcoded + Phase-2 XML-Override | ✓ (0.1.34) |
| 15 | X-REQ-T1 | About.xml Hard-Requires für Anomaly + Odyssey | ✓ (0.1.23) |
| 16 | I-T3 | Infected-PawnKinds auf Shambler-Parent migrieren | X-REQ-T1 (✓), §19 |
| 17 | M4-GRAV-T1 | OutpostWorldObject → Gravship-Extension-Point | X-REQ-T1 (✓), §18 |
| 18 | X-BUILD-T1 | Construction-Cost-Patches (offen, §17) | — |

---

## Offene Punkte (noch zu klären)

| # | Punkt | Status | Nächster Schritt |
|---|-------|--------|------------------|
| 17 | Construction-Cost-Patches (Wände/Türen auf Bauschutt) | teil-implementiert | Patch-File existiert; Falsifizierungsbericht `ConstructionDebris` + Sandbag-Detail offen (§8.2) |
| 18 | Architect-Menu-Plan (welche Kategorien, wo leben Items) | offen | Keine Plan-Datei vorhanden, Design-Entscheidung steht aus |
| — | Need_NeuralHeat Safe-Disable (Royalty-Abwahl) | offen | Phase-2-Follow-up: Patch auf `<listInPawnNeeds>` statt Def-Remove |
| — | DLC-Anomaly Entity/Void Def-Name-Verification | offen | Aktuelle Patches basieren auf Name-Conventions; Ludeon Anomaly kann abweichen |
| — | Odyssey Travel-Events Handling | offen | Director-gesteuert vs. Patch-Suppressor, nicht entschieden |
| — | Ideology: Player-Founder-Screen entfernen | offen | Harmony-Prefix-Spike steht aus |
| — | Biotech: Children global disable? (PawnGeneration-Patch vs. GameOver-Logic-Erweiterung) | offen | Nicht entschieden — Spielverhalten in Biotech-Runs unklar |
| — | StoryDirector: MakeMech-RecipeDef-Remove für BIOTech-Tests | offen | Mechanitor komplett aus war Decision; Verify |

*Nach Audit 2026-08-04: Tabelle enthält jetzt 8 offene Punkte vs. zuvor "keine".*

---

## Änderungshistorie

| Datum | Änderung | Autor |
|-------|----------|-------|
| 2026-08-04 | Initiale Dokumentation aller Entscheidungen | Buffy |
| 2026-08-04 | Verweis auf Harmony-Strategie in AUDIT.md §1 hinzugefügt | Buffy |
| 2026-08-04 | **Audit-Runde:** §2 GameOver-Logic an INTERFACE_CONTRACT §9.5 angeglichen (Sole-Owner Mod 02); §3 Storyteller mit Dual-Source-Realität (Hardcoded + DefDatabase-Overlay); §9 stochastischen Tod-Layer gestrichen wegen unimplementierbarer Intervall-Definition; §10 GameMode `Load` → `Softcore` umbenannt; §13 Event-Variablen mit Determinism-Key-Template und FreeText-Fallback präzisiert; §15-§19 neu (DLC-Content-Policy, Hard-Requires, Construction-Cost-Patches, Gravship-Territory, Anomaly-Shambler-Basis); Offene-Punkte-Tabelle von "keine" auf 8 echte offene Punkte befüllt. | Buffy |
| 2026-08-04 | Code-Fix: `rimconemy.foundation.colonials` Capability in PackageRegistry.cs registriert (Phase-B / F-V1 audit-gap, gegen INTERFACE_CONTRACT §9.1) | Buffy |
| 2026-08-04 | **Dokument-Konsolidierung:** §20 DLC-Detail-Ergänzungen (aus `DECISIONS_DLC.md`), §21 Harmony-Strategie, §22 Scribe-/ExposeData-Pattern, §23 GameComponent-Zuordnung (aus `docs/AUDIT.md`) übernommen. Quell-Dokumente archiviert in `docs/archive-md-2026-08-04.tar.gz`. | Buffy |
| 2026-08-04 | **Design-Lock:** §24 Early-Game-Munitions-/Hochofenvertrag und §25 Erfahrungsbaum statt Forschungsbaum ergänzt; Start- und Midgame-Progression bleiben handlungs- und ergebnisgebunden, Vanilla-Forschung bleibt Kompatibilitätsschicht. | Buffy |
