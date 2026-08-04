# COMPATIBILITY_MATRIX.md — Rimconemy Load Order & Compatibility Matrix

> **Stand:** 2026-08-04  
> **Owner:** Foundation (01) & Full Suite  
> **Zielplattform:** RimWorld 1.6.4566  
> **Status:** Kanonische Kompatibilitätsmatrix (G1-Gate / Block A1)

---

## 1. L0–L7 Lade-Reihenfolge (Load Order Standard)

Die folgende Reihenfolge ist in `About.xml` (`loadAfter` / `loadBefore`) für alle Rimconemy-Pakete und fremden Mods verbindlich vorgegeben:

| Level | Mod-Typ / Paket | PackageID in About.xml | Beschreibung / Funktion |
|---|---|---|---|
| **L0** | RimWorld Core | `Ludeon.RimWorld` | Basisspiel RimWorld 1.6 |
| **L1** | Harmony | `brrainz.harmony` | Modding-API & Reflection-Patcher |
| **L2** | Vanilla DLCs | `Ludeon.RimWorld.Royalty`<br>`Ludeon.RimWorld.Ideology`<br>`Ludeon.RimWorld.Biotech`<br>`Ludeon.RimWorld.Anomaly`<br>`Ludeon.RimWorld.Odyssey` | Offizielle Erweiterungen (automatisch erkannt via `DLCDetector`) |
| **L3** | Rimconemy 01 Foundation | `rimconemy.foundation` | Basis-Registry, Diagnose, Tokens, Events |
| **L4a** | Rimconemy 02 Survival & Progression | `rimconemy.survivalprogression` | Pawn-Profile, Needs, Skills, Character Setup |
| **L4b** | Rimconemy 03 Scavenger Infrastructure | `rimconemy.scavengerinfrastructure` | Bauschutt, Hanf, Wasser, Strom, StorageSnapshot |
| **L5a** | Rimconemy 04 Economy & Territory | `rimconemy.economyterritory` | Wallet, Märkte, Outposts, Territory |
| **L5b** | Rimconemy 05 Infected & Automation | `rimconemy.infectedautomation` | StoryDirector, Bedrohung, Infizierte, Mechadroids |
| **L6** | Dritte Content-/Overhaul-Mods | Diverse | Combat Extended, Vanilla Expanded Series, etc. |
| **L7** | UI- / Theme-Overlays | `RimThemes` u.a. | Opt-in UI-Overrides via `GlobalThemeOverride` |

---

## 2. DLC-Kompatibilität & Fallback-Matrix

Rimconemy-Pakete sind **standalone-fähig**. Das Fehlen eines DLCs darf zu keinem Absturz, NullReferenceException oder XML-Ladefehler führen.

| DLC | Erkennung | Funktion in Rimconemy | Fallback bei Fehlen |
|---|---|---|---|
| **Royalty** | `ModLister.RoyaltyInstalled` | Titel & Genehmigungen beeinflussen Ideologie-Spannung & Bedrohung | Standard-Pawn-Formeln ohne Titel-Multiplikatoren |
| **Ideology** | `ModLister.IdeologyInstalled` | `PreceptDef`, `RoleDef`, `RitualDef`, `ThoughtWorker` als Träger von Setting-Regeln | Rules laufen im puren Code-Setting-Director; UI-Adapter schaltet auf Fallback-Overlay |
| **Biotech** | `ModLister.BiotechInstalled` | Mechadroids & Umweltverschmutzung speisen ThreatAggregator | ThreatAggregator nutzt ausschließlich Storage-/Survivor-Kennzahlen |
| **Anomaly** | `ModLister.AnomalyInstalled` | Okkulte Bedrohungen als Vorbedingungen in `StoryEventSpec` | Anomaly-Events aus `StoryEventCatalog` herausgefiltert |
| **Odyssey** | `ModLister.HasActiveModWithIdentifier(...)` | Map-Raid-Routen & Weltkarten-Zellen | Standard RimWorld WorldGrid Navigation |

---

## 3. Drittmod-Klassifikation & Verhaltensregeln

### 3.1 Combat Extended (CE)
- **Status:** Kompatibel.
- **Regel:** Rimconemy-Gebäude (z.B. Pfeilturm) und Bauschutt-Materialien nutzen Standard-`ThingDef`-Werte (`statBases`). CE-Munitions-Patcher greifen automatisch auf Vanilla-Verhalten zurück.

### 3.2 Vanilla Expanded Suite
- **Status:** Kompatibel.
- **Regel:** Keine Ersetzung fremder Defs. Bauschutt-Patches remappen ausschließlich Basis-Vanilla-Wände/Türen (`ThingDef.Wall`, `ThingDef.Door`).

### 3.3 RimThemes / Custom UI Mods
- **Status:** Opt-in Kompatibel (`EnableGlobalThemeOverride`).
- **Regel:** Theme-Overrides greifen nur, wenn der Nutzer die Option im `FoundationDashboard` explizit aktiviert. RimWorld-Standard-Widgets bleiben intakt.

---

## 4. Invarianten für Kompatibilitäts-Tests

1. Jedes Rimconemy-Paket muss ohne die presence von Paketen 02–05 kompilieren und laden können (`ActiveStandalone` Modus).
2. Das Entfernen eines optionalen DLCs mitten in einer laufenden Kolonie konvertiert betroffene In-Game-Referenzen in `FrozenWithWarning`-Zustände ohne Crash.
