# Canonical Vanilla Domain Map — Rimconemy

> **Stand:** 2026-08-04 · **Reihenfolge dieser Seite:** vor Paketen 02–05 · **Zweck:** Leitseite, kein zweiter Vertrag · **Owner-Invariante:** INTERFACE_CONTRACT §9.1 · **Anti-Claim:** hier steht *was Rimconemy meint*, nicht *was Rimconemy baut*.

## Prinzip — eine Zeile

> **Rimconemy erfindet keine neuen RimWorld-Begriffe. Rimconemy ulabelt, reichert an und verkabelt Vanilla-Strukturen.** Wer eine zweite Realität baut — eigener Need-Def, eigener ThingDef für Vanilla-Material, eigener Room-Layer, eigene Tech-Tabelle — baut zwei Bugs und verliert DLC-Kompatibilität auf Dauer.

Die richtigen Haken sind: `Defs` + `PatchOperation` + `DefModExtension` + `Comp` + `RoomDef`/`RoomRoleDef` + `ResearchProjectDef` + `HediffDef` + `NeedDef` + `IExposable`-Wallet. Alles andere ist Drift (§8).

---

## §1 — Die sechs Vanilla-Hooks

| Hook | Vanilla-Klasse | Rimconemy-Schicht | Kurzform |
|---|---|---|---|
| **Need** | `RimWorld.Need` + `NeedDef` | Setting-Druck, nicht zweite Realität | „Setting-Bedarf-Übersetzung" |
| **Good** | `RimWorld.ThingDef` + `Comp*` | Ware mit Rolle und Zustand | „Material/Ware/Resource" |
| **Room** | `RimWorld.RoomDef` + `RoomRoleDef` | Funktionsraum mit Rolle + Stats | „Werkstatt/Wache/Küche" |
| **Tech** | `RimWorld.ResearchProjectDef` | Freischaltgraph für Capabilities | „Tech-Stufe/Tier" |
| **State** | `HediffDef` + `HediffComp` | Zustand an Pawn / Tier / Welt | „Verletzung/Infektion/Buff" |
| **Transfer** | `Haul` + `Trade` + `Caravan` | Bewegung mit Empfänger-Sicherheit | „Wallet/Booking/Outpost" |

Pro Hook haben wir eine kanonische Vanilla-Quelle. Was Rimconemy beiträgt, ist *Konfiguration und Bedeutung*, nicht *neue Engine*.

---

## §2 — Übersetzungs-Tabelle (Rimconemy-Begriff → Vanilla-Anker)

### 2.1 Need

| Feld | Wert |
|---|---|
| Rimconemy-Begriff | Setting-Identität (Sprache des Settings) — Nahrung · Sicherheit · Sozial |
| Vanilla-Anker | `Need_Food` · `Need_Rest` · (Health: `Health.summaryHealth.SummaryHealthPercent`) · `Need_Recreation` / `Need_Joy` (RimWorld 1.6 hat einen umbenannten Pfad) |
| Modding-Hook | `Need.CurLevelPercentage` lesen, **nie** eigener Need-Def an Pawn anhängen |
| Translation | `0..1`-Projektion via `NeedMappingService` (Paket 02) — nur Anzeige, kein Eingreifen |
| Owner | Vanilla · Rimconemy-Reader: 02 · Optional-Modifier: 02 (Sample-Amplifier als Begleit-Hediff, **kein** Need-Override) |

**Anti-Pattern:** eigene `Rimconemy_Need_Food`-Def an Pawns hängen → Doppel-Need-Welt, Mood-Konflikt, DLC-Bruch.
**Sauber:** Vanilla-Needs lesen, in Setting-Sprache labeln.

### 2.2 Good

| Feld | Wert |
|---|---|
| Rimconemy-Begriff | Ware = ein `ThingDef` mit Materialrolle + Handelbarkeit + Zustand |
| Vanilla-Anker | ALLE `ThingDef`s mit `stuffProps` (Material), `comps` (Verhalten), `tradeTags` |
| Modding-Hook | **`PatchOperation`s** auf existierende Vanilla-ThingDefs, plus **`<defModExtensions>`** für Rimconemy-Annotationen |
| Identifier | stabiler `defName` aus Vanilla — `WoodLog`, `Steel`, `ChunkCoal`, `Medicine`, `MealSimple`, `Uranium`, … |
| Owner | Vanilla · Rimconemy-Annotations-Owner: 01 (PatchOperation-Sammlung) |

**Anti-Pattern:** `Rimconemy_Bauschutt` als neuer ThingDef statt `WoodLog` in anderem Zustand (`HitPoints < MaxHitPoints * 0.5`) → doppelte Inventar-Welt.
**Sauber:** PatchOperation auf `WoodLog.health`/`color`/`tradeTags`; DefModExtension `Rimconemy.SourceMarker` für „aus Trümmer geborgen".

### 2.3 Room

| Feld | Wert |
|---|---|
| Rimconemy-Begriff | Funktionsraum mit Setting-spezifischer Rolle |
| Vanilla-Anker | `RoomDef` mit `RoomRoleDef`-Tag · Role-Tags tragen Score-Berechnung |
| Modding-Hook | **`<defModExtensions>` auf RoomRoleDef** (kein eigener Room-Score) |
| Reader | Vanilla-Score-System · Rimconemy-Aggregation: 02 (Need-Reaktion), 03 (Bauschutt-Werkstatt-Coverage), 05 (Wacht-Room-Defense) |
| Owner | Vanilla · Rimconemy-Owner-Erweiterung: 01 (`Foundation/RoomDefinitionExt`) |

**Anti-Pattern:** Eigener Room-Mechanismus mit eigenem Score, eigenem Wachstum, eigener Wirkung → Konflikt mit Vanilla-RoomStats.
**Sauber:** Eigene RoomRoleDef oder PatchOperation auf existierende Roles mit ModExtension, die Rimconemy-Aggregation ohne Side-Effects trägt.

### 2.4 Tech

| Feld | Wert |
|---|---|
| Rimconemy-Begriff | Tech-Stufe, die Capabilities + Unlock-Pfade öffnet |
| Vanilla-Anker | `ResearchProjectDef` mit `prerequisites` + `costList` + `ResearchModExtension`-Framework |
| Modding-Hook | `ResearchProjectDef.researchModExtensions` und `<li Class="ResearchProjectFinished">`-Effects auf patche-t-up `BuildingDef`s, `WorkTypeDef`s, `TerrainDef`s |
| Owner | Vanilla · Research-Selection: 02 (ProgressionGameComponent); Research-Effects (Unlock-Pfade): Pakete 03/04/05 als **Consumer**, **nicht** selbst Definierer |
| Beispiel | `Rimconemy_Research_TechTier1_SolidFuelPlant` — `costList=Steel×30`, `researchModExtensions=[<Rimconemy.UnlockBuilding>Rimconemy_WoodCoalGenerator</…>]` |

**Anti-Pattern:** Eigene Tech-Tabelle außerhalb `ResearchProjectDef`, eigener `IsFinished`-Loop, eigene Cooldown-Welt.
**Sauber:** Vanilla-ResearchProjectDef + ResearchModExtension-Effect, der bei `ResearchProjectFinished` (RimWorld-Event) Vanilla-Defs patched/liftet.

### 2.5 State

| Feld | Wert |
|---|---|
| Rimconemy-Begriff | Zustand eines Pawns, Tieres oder der Welt-Region |
| Vanilla-Anker | `HediffDef` (gesundheitlich), `MentalStateDef` (mental), `Trait` (Charakter) |
| Modding-Hook | **`<defModExtensions>` auf HediffDef**, plus PatchOperations auf existierende Hediff-Chains. Severity-Offset statt Replacement |
| Cross-Paket | Vanilla + 02 (Trait/Need) + 05 (Infektion) |
| Owner | Vanilla · Rimconemy-Trait-Owner: 02 (`ThoughtDefs_CollectiveDefense` + `ThoughtDefs_Transparency`) · Infektion/Threat: 05 |

**Anti-Pattern:** Eigener `Rimconemy_Hediff_Injury`-Def als Replacement für `Hediff_Injury` → Konflikt mit Doktor/patchwork, DLC-Hediffs.
**Sauber:** PatchOperation auf existierende Hediff-Chains, plus `<defModExtensions>` für „Rimconemy-Stage", die mit Severity-Offset wirkt.

### 2.6 Transfer

| Feld | Wert |
|---|---|
| Rimconemy-Begriff | Ware-Bewegung mit Empfänger-Sicherheit (Wallet-Idempotenz, Outpost-Booking, Royalty-Konsens) |
| Vanilla-Anker | `Pawn` macht `Haul`-Task · `Caravan` transportiert · `TradeShip` handelt |
| Modding-Hook | **Booking-State** (Pre-Commit) + **Ledger** (Post-Commit) — kein Override der Transport-Logik |
| Owner | Vanilla · Booking/Ledger-Owner: 04 (`CreditsLedger`, `OutpostService`) |
| Beispiel | `Rimconemy_Wallet.Reserve("+50 Steel Outpost-2")` mit Idempotenz-Index, dann beim Haul-Drop Reconciliation |

**Anti-Pattern:** Eigene Transport-Schicht, eigene Inventar-Mirror, doppeltes `Map.thingList`-Tracking.
**Sauber:** Storage-Read (03) + Booking (04) + Reconciliation on Drop; **eine** Ledger-Instanz pro Save, idempotenter Key-Index.

---

## §3 — Anti-Patterns (was wir NICHT tun)

| Anti-Pattern | Warum verboten | Konsequenz (RimWorld-Risiko) |
|---|---|---|
| Eigener `Rimconemy_Need_*`-Def an Pawns anhängen | Override-Konflikt, Mood-Konflikt, DLC-Bruch | Mood-System kollidiert, Ideology>DLC friert |
| Eigener `Rimconemy_Bauschutt`-ThingDef für Holzrüde | Vanilla `WoodLog` mit beschädigtem `HitPoints` reicht | Doppel-Inventar, doppelte Storage-Snapshots |
| Eigener `Rimconemy_RoomDefCycle`-Mechanismus | Vanilla-RoomRole mit StatTags deckt 90 % der Use-Cases | Konflikt mit RoyalTSP/Hospitale-Mods |
| Eigene `Rimconemy_Research_All`-Tabelle | Vanilla-ResearchProjectDef ist die geneigte Engine | Doppelte Forschungsbahn, Save-Schema-Chaos |
| Eigener `Rimconemy_Hediff_Injury`-Def als Replacement | Vanilla `Hediff_Injury` ist die Heiler/Doc-Logik | Doc-Bots können nicht heilen, Save-Roundtrip bricht |
| Eigenes Wallet-Item als Thing | Credits sind reine Daten, kein Thing | Drop auf Map erzeugt Item-Konflikte |
| Eigener Pawn-Inventar-Spiegel neben Vanilla | Race-Conditions, Save-Konflikte | Spieler sieht erfundene Quantitäten |
| Eigener PatchOperationReplace auf Vanilla-Needs | Anstelle von Anzeige-Layer einen Override | Mood-System verliert Vanilla-Pfad |

---

## §4 — Eigentums-Matrix (Schreibrechte-Invariante, Kurzform)

| Hook | Owner (Schreiben) | Erlaubte Reader |
|---|---|---|
| Need | Vanilla + 02 (Translation, kein Override) | 02 (Snapshot), 03 (Vacancy-Reads), 05 (Snapshot) |
| Good | Vanilla + 01 (PatchOperation-Sammlung + DefModExtension) | 02, 03, 04, 05 |
| Room | Vanilla + 01 (RoomRoleExt) | 02, 03, 05 |
| Tech | Vanilla + 02 (Selection-Regel, Effect-Lift über ResearchModExtensions) | 03, 04, 05 als Consumer |
| State | Vanilla + 02 + 05 (PatchOperations + ThoughtWorker) | 02, 03, 05 |
| Transfer | Vanilla + 04 (Booking/Ledger) | 02 (Wallet-Balance-Read), 03 (Storage-Read), 05 (Threat-Read) |

Pattern: **1 Schreiber, N Reader über Capability-Gate ** (siehe INTERFACE_CONTRACT §9.1).

---

## §5 — Paket-Layer-Mapping (in Vanilla-Sprache, nicht in Paket-Sprache)

| Rimconemy-Schicht | Paket-Layer-Funktion | Vanilla-Hauptanker |
|---|---|---|
| **Bedarf-Layer** (war „Paket 02 Survival") | Setting-Bedürfnis-Übersetzung, Trait-Anker, Sole-Owner GameOver | `Need`, `HediffComp_SeverityPerDay`, `GameEnder` |
| **Werk-Layer** (war „Paket 03 Scavenger") | Storage-Reads, Power-Chain-Reads, Engine-Comps | `ThingDef.comps`, `CompPowerTrader`, `Map.thingList`, `Zone_Stockpile` |
| **Handels-Layer** (war „Paket 04 Economy") | Booking, Ledger, Outpost-Markt | `IExposable`, `TradeRequest`, `Caravan`-Storage |
| **Droh-Layer** (war „Paket 05 Infected") | State-Infektion, Threat-Aggregation, Ideology-Adapter | `HediffDef_Sickness`, `GameCondition`, `IncidentDef` |

Diese Schichten sind *Funktionsbezeichnungen*, keine Hierarchie. Cross-Layer-Reads laufen über die Capability-Registrierung (siehe INTERFACE_CONTRACT §2).

---

## §6 — Arbeits-Anleitung (für die nächste Task-Planung)

Wer eine neue Rimconemy-Funktion plant:

1. Erst klären: **welcher Vanilla-Hook passt?** (Need / Good / Room / Tech / State / Transfer)
2. Dann klären: **welche ModExtension oder PatchOperation auf den Vanilla-Hook?**
   - `DefModExtension` für eigene Annotation
   - `PatchOperation` für Modifikation bestehender Defs
   - `Comp` für Verhalten an ThingOrPawn
   - `ResearchModExtension` für Tech->Effect-Lift
   - `RoomRoleExt` für Room-Score-Side-Channel
3. Dann klären: **in welcher Owner-Spalte (INTERFACE_CONTRACT §9.1 / §4) wird geschrieben?**
4. Dann erst: **Code + Tests**

Wer eine bestehende Funktion liest:

1. Bei jedem `myCustomThing` prüfen: **gibt es das Vanilla-Pendant?**
2. Wenn ja: gibt es **PatchOperation-Pfade** statt das Ding neu zu erfinden?
3. Wenn nein: ist der Rimconemy-Begriff wirklich **nicht** mit Vanilla-Mitteln abbildbar?

---

## §7 — Lieferungs-Status gegen den Kanon

| Hook | Erreicht (heute) | Lücke / nächstes Ziel |
|---|---|---|
| Need | `NeedMappingService` (Paket 02) liest vanilla → `0..1`-Anzeige · nicht-Override garantiert | optional: `Need-Amplifier` als Begleit-Hediff (DECISIONS §1) |
| Good | Storage-Reads (03) funktionieren · `Rimconemy_Bauschutt` / `Rimconemy_Hemp*` als **eigene ThingDefs** → **Drift** | Migration: ersetzen durch PatchOperations auf Vanilla + DefModExtension |
| Room | nicht etabliert — kein Rimconemy-Room-Hook | Erstaufnahme: `Foundation/RoomDefinitionExt.cs` + ModExtension-Pattern auf RoomRoleDef |
| Tech | `ProgressionGameComponent.UpdateResearchCapabilities` (02) liest `ResearchProjectDef.IsFinished` · **keine** `researchModExtensions` | Migration: Tech-Effects statt Code-Pfad |
| State | `ThoughtWorker_ResourceFairness`, `ThoughtWorker_CollectiveDefense`, `ThoughtWorker_Transparency` · PatchOperations auf existierende Hediff-Chains | optional: Severity-Offset-Pattern auf Setting-spezifische HediffDefs |
| Transfer | `CreditsLedger` (04) · `OutpostService` (04) · Booking-Reservation infrastrukturell vorhanden | Migration: `Rimconemy_Wallet.Reserve` als Idempotenz-Schnittstelle + Reconciliation-on-Drop |

---

## §8 — Drift-Audit (wo Rimconemy vom Kanon abweicht)

| Pfad | Was es heute ist | Was Kanon verlangt | Migrationsweg |
|---|---|---|---|
| `mods/03/Defs/ThingDefs/Resources/ConstructionDebris.xml` | `Rimconemy_Bauschutt` als eigener `ThingDef` mit ParentName ResourceBase + `CompProperties_Forbiddable` + eigene `TexPath` | PatchOperation auf Vanilla `WoodLog` + `StuffProps.color/HitPoints/state` + DefModExtension `Rimconemy.SourceMarker = "rubble"` | a) PatchOperation-Remap auf `Wall.stuffCategories` (existiert schon via `Bauschutt_Remap_Patches.xml`); b) `Rimconemy_Bauschutt` def manipuliert `WoodLog` in LowHP-Phase per PatchOperation; c) DefModExtension signalisiert „gerade aus Trümmer geborgen" |
| `mods/03/Defs/ThingDefs/Resources/Hemp.xml` + `Plants/Hemp.xml` | `Rimconemy_Hemp` (Resource) + `Rimconemy_HempLeafy` (Leaf) + `Rimconemy_Hemp`-Plant-Def sind drei eigene ThingDefs | `Rimconemy_Hemp`-Plant mit `harvestedThingDef → Rimconemy_HempBulk`-Template statt komplett neu; Bestehende Vanilla-Pflanzen (Agave, Cotton) per PatchOperation anreichern | a) Bestehende Vanilla-Plant als `<defModExtensions>` mit `Rimconemy.PlantMorph = "fibre"` taggen; b) Leafy-Tag auf existierender Vanilla-Pflanze markieren |
| `mods/03/Defs/BuildingDefs/PowerPlants.xml` | `Rimconemy_WoodCoalGenerator` (Parent: `BuildingBase`) · `Rimconemy_WaterTurbineGenerator` · `Rimconemy_ArrowTurret_Power` als 3 eigene BuildingDefs mit `CompProperties_Power`/`CompProperties_TurretGun` | Vanilla hat `WoodFiredGenerator`/`WatermillGenerator`/`Turret_MiniTurret` (1.6 Standard) — PatchOperation auf Vanilla-Defs + `CompProperties_Refuelable`-Anreicherung statt Neuerschaffung | a) PatchOperation auf Vanilla-Defs; b) DefModExtension `Rimconemy.SettingFuelClass = "solid/liquid"`; c) Rimconemy-Building-Namen rausfallen lassen |
| `mods/04/Source/Building/BuildingInputAdapter.cs` | Hartkodierte Strings `Rimconemy_WoodCoalGenerator` → `30`, `Rimconemy_WoodCoalGenerator` → `WoodLog 30`, `Rimconemy_ArrowTurret_Power` → `Steel 25` | DefModExtension auf Vanilla `WoodFiredGenerator` mit `inputAmount` etc. | a) PatchOperation auf Vanilla-BuildingDefs mit `costList`-Anpassung; b) Adapter liest DefModExtensions statt hartkodierte Strings |
| (keine Datei heute) | Room-Hooks komplett fehlen | `Foundation/RoomDefinitionExt.cs` als DefModExtension-Bibliothek | a) Erstanlage |
| `mods/02/Source/Progression/ProgressionGameComponent.cs:389` | Hard-Code-Loop liest `ResearchProjectDef.IsFinished` und sammelt IDs in Liste | `ResearchModExtension`-Effects + `ResearchProjectFinished`-Event-Subscription | a) PatchOperation auf Vanilla + `Rimconemy.UnlockBuilding`-Extension auf relevante ResearchProjectDefs; b) Histogramm der Unlock-Pfade als State |
| `mods/05/Source/Incidents/IncidentStub.cs` (in Audit) | Reiner Datencontainer mit `LogMarker = "v0"` | `IncidentDef` + `IncidentWorker`-Subklasse (die existiert schon in `InfectedRaidWorker.cs`) | a) `IncidentStub` löschen, stattdessen `IncidentWorker`-Subklasse erweitern; b) Datenfelder als ModExtension auf Vanilla `IncidentDef` |

---

## §9 — Reproduktion

```bash
# Aktueller Source-Bestand gegen die Map prüfen:
for f in mods/*/Defs/**/*.xml; do
  echo "$f: ThingDefs=$(grep -c '<ThingDef' $f) PatchOperations=$(grep -c 'Operation' $f) ModExtensions=$(grep -c 'defModExtensions' $f)"
done

# Drift-Finder: sind parallel erfundene ThingDefs vorhanden?
grep -rn 'defName="Rimconemy_' mods/*/Defs/ThingDefs/ | head -20
```

Wenn ein neues Rimconemy-Element in §8 nicht auftaucht, ist es kanon-konform.
Wenn es in §8 auftaucht, ist es ein Migrationskandidat für die nächste Iteration.

---

## §10 — Aktualisierungs-Anker

Diese Seite ändert sich nur wenn:
1. ein neuer Vanilla-Hook als Rimconemy-resourceful identifiziert wird (sollte nicht passieren — RimWorld-API ist stabil),
2. ein Anti-Pattern in §3 obsolet wird (selten, z.B. wenn RimWorld eine Setting-API hinzufügt),
3. ein Drift-Eintrag in §8 abgebaut wurde (Phase-2+ Migrationspfad),
4. ein Paket-Layer-Refactoring in §5 die Vanilla-Sprache ändert.

Owner jeder Änderung: Foundation + User-Approval. Keine `PO`-Änderung ohne Audit-Trail in `docs/falsification/status-vs-code-audit-2026-08-04.md`.

---

## §11 — Lieferung dieser Seite (Audit-Sprint 2026-08-04)

| Lieferung | Pfad | Zeilen |
|---|---|---|
| Diese Leitseite | `docs/CANONICAL_VANILLA_DOMAIN_MAP.md` | (this file) |
| Audit-Bezug | `docs/falsification/status-vs-code-audit-2026-08-04.md` (Phase-0 Sektionen A-K) | 187 Z. |
| Honeymade Foundation | `mods/01-Rimconemy-Foundation/Source/UI/RimconemyWindow.cs` etc. | 162 Z. |
| Honest-Banner-Audit (Test) | `mods/01-Rimconemy-Foundation/Tests/FoundationHonestBannerAudit.cs` | 217 Z. |

**Was diese Seite NICHT macht:** sie codifiziert *was Rimconemy meint*, nicht *was Rimconemy tut*. Der nächste Sprint muss §8 in Code umsetzen — die Drift-Liste ist der handfeste Migrations-Fahrplan.
