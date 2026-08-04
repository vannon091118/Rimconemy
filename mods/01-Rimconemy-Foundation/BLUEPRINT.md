# Blueprint 01 – Rimconemy Foundation

## API-Hinweis

Die genannten Vanilla-Typen und UI-/Scribe-Anker sind Planungsanker. Exakte 1.6-Signaturen werden über `API-*`-Spikes bestätigt (Spike-/Baseline-Dokumente archiviert in `docs/archive-md-2026-08-04.tar.gz`; Spike-IDs kanonisch in `docs/H1-api-def-gate.md`).

## Ziel

Foundation liefert die sichtbare Diagnose- und Integrationsbasis, ohne selbst Needs, Wirtschaft, Infizierte oder Outposts zu simulieren.

## Standalone-Spielwert

Vanilla-Dashboard mit Ressourcen-/Verbrauchsübersicht, Mod-/DLC-Profil, Ereignislog, Save-Warnungen und `Unavailable`-Erklärung. Ein Spieler kann damit Engpässe verstehen, auch ohne Feature-Paket.

## Vanilla-/DLC-Anker

| Bereich | Anker | Status | Entscheidung |
|---|---|---|---|
| Mod-/DLC-Erkennung | lokale Mod-/DLC-Metadaten und RimWorld-Loader | Vanilla-Anker, lokale Buildprüfung nötig | Laufzeit-Capability statt harter Feature-Referenz |
| Save | Game-/World-/Map-Scribe-Lebenszyklus | Vanilla-Anker | Foundation-Schema klein halten |
| UI | Windows, Inspect-/Gizmo-/MainTab-Anker | Vanilla-Anker | read-only Snapshots, keine UI-Simulation |
| Harmony | gezielte Startup-/Lifecycle-Hooks | lokal vorhanden als externe Abhängigkeit | nur wenn Def-/Loaderweg nicht reicht |
| DLC | fünf Manifests der lokal belegten DLC-Baseline (archiviert) | lokal belegt | fehlend = Partial, nie still Full |

## Besitz und Schnittstelle

Foundation besitzt Profil, Capability-Registry, Diagnose, UI-Read-Models und gemeinsame Eventkategorien. Diese Read-Models liegen im bestehenden `Source/Models/`-Pfad; ein separates `Source/Diagnostics/`-Modul ist für den MVP nicht erforderlich. Feature-Pakete besitzen ihre Gameplaydaten. Foundation darf sie anzeigen, aber nicht schreiben.

Minimaler Descriptor:

```text
PackageId
PackageVersion
SaveSchemaVersion
Capabilities[]
ProfileCompatibility
```

## Artefaktinventar und Status

Die ursprünglich geplanten F1–F5-Zielpfade sind umgesetzt oder als `UNVERIFIED`-Testartefakte angelegt. Runtime-Belege bleiben User-Sache.

| Task | Dateien/Artefakte | Test-IDs |
|---|---|---|
| F1 | `About/About.xml`, `Defs/`, `Source/Bootstrap.cs` | `NEW_GAME`, `DLC_SCOPE` |
| F2 | `Source/Registry/`, `Source/Profile/` | `NEW_GAME`, `DLC_SCOPE`, `UI_REASON` |
| F3 | `Source/Models/PackageSnapshot.cs`, `Source/Events/EventLog.cs` (Spike-Doku `API-RESOURCE-01` archiviert) | `UI_REASON`, `DETERMINISM` |
| F4 | `Source/UI/`, `Source/Save/` | `SAVE_LOAD`, `UI_REASON` |
| F5 | `FALSIFICATION_REPORTS/rimconemy.foundation__Servicebus.md`, Archiv-Handoff | `SAVE_LOAD`, `MAP_CHANGE`, `DLC_SCOPE`, `DETERMINISM` |

## Fünf Build-Tasks

### F1 – Minimalmod und lokale Buildreferenzen

- Modordner, `About.xml`, Def-/Patch-Struktur und minimale Assembly anlegen.
- lokale Referenz auf `Assembly-CSharp.dll` und passende Unity-/Harmony-Assembly dokumentieren.
- Start ohne Featurepakete prüfen.

**Gate:** lädt allein auf `1.6.4566 rev575`; rote Logfehler und wiederholte Diagnoseeinträge sind blockierend.

### F2 – Registry und Profil

- stabile Paket-/Capability-IDs registrieren.
- Standalone, Partial und Full anhand erkannter Pakete/DLCs unterscheiden.
- Full nur bei allen fünf Paketen, fünf DLCs und kompatiblen Schemas aktivieren.

**Gate:** jedes fehlende Paket/DLC erscheint als konkrete Abweichung.

### F3 – Snapshot-/Event-Read-Model

- Resource-, Power-, Progression-, Wallet-, Territory- und Threat-Snapshots nur lesen; der konkrete Vanilla-Ressourcen-/Power-Anker bleibt bis `API-RESOURCE-01` offen.
- `0`, `Unavailable`, `Blocked`, `Frozen` und `Destroyed` unterscheiden.
- Logeinträge deduplizieren; Request-Cache und Transaktionshistorie sind getrennt begrenzt.

**Gate:** ein fehlendes Paket erzeugt keine Phantomdaten.

### F4 – UI und Save-Diagnose

- Dashboard, Modusbanner, DLCstatus, letzte Events und Migrationswarnung bauen.
- Featurezustände nie direkt aus mutable Engineobjekten rendern.
- Foundation-Save-Daten mit Schema und kontrollierter Migration versehen.

**Gate:** Save/Load und fehlendes Paket zeigen verständliche, nicht destruktive Warnungen.

### F5 – Integrationsspike

- Paketpaare und Full Profile laden.
- Capability-Versionen, doppelte Registrierung, Kartenwechsel und wiederholtes Laden prüfen.
- UI zeigt die aktive Version und nicht unterstützte Spikes.

**Exit:** Falsifizierungsbericht `rimconemy.foundation__Servicebus.md` bleibt bis zu realen Tests `UNVERIFIED`; Blueprint gilt danach als implementierungsbereit.

## UI-Minimum

- Profil: `Standalone | Partial | Full Overhaul`
- Paket-/DLC-Liste mit Grund
- aktuelle Ressourcen-/Power-Snapshots, sobald `API-RESOURCE-01` lokal bestätigt ist; bis dahin keine erfundenen Nullwerte
- letzte Änderungen und Fehlercodes
- Save-Schema/Migration
- Link zum zuständigen Paket bei Blockade

## Save und Performance

- nur eigene Konfiguration, Registry-/Profilstatus und Diagnoselog persistieren.
- keine Feature-Daten löschen, wenn Paket fehlt.
- Snapshot-Updates ereignis-/intervallgebunden, nicht pro UI-Frame.
- P1/P2/P3 und globale Budgets aus `INTERFACE_CONTRACT.md` gelten.

## DLC-Gates

Alle fünf DLCs werden erkannt; Foundation interpretiert deren Gameplay nicht. Fehlende DLCs schalten das Full Profile ab, nicht das Dashboard.

## Offene Spikes

- konkrete 1.6-Loader-/Lifecycle-Signaturen gegen lokale Assembly und User-Runtime bestätigen.
- `API-RESOURCE-01` für Ressourcen-/Power-Read-Model abschließen.
- UI-Anker je nach tatsächlichem Modtemplate verifizieren.
- optionaler Harmony-Patchumfang minimieren; MVP benötigt keinen Patch.

## Decision-Status (Track 2-C, 2026-08-04)

- **F-T1 ColonialReader**: DECIDED + DONE (`Source/Colonials/ColonialReader.cs`, Phase-B-Sprint).
- **F-T2 GameOverMode Enum**: DECIDED + DONE (in Mod 02, FoundationSaveData.IsSandboxMode hier ergänzt).
- **F-T3 ColonialReader einbinden**: DECIDED + DONE (Mod 02 + 03 + 05 migriert).
- **Cross-Package X-T1 HasCapability**: DECIDED + DONE (`Source/Registry/CapabilityAudit.cs`, Once-Warning-Pattern).
- **Cross-Package X-T2 INTERFACE_CONTRACT**: DECIDED + DONE (§9 ergänzt).
- Phase-B Sprint Phase B (Capability-Gates + Sole-Owner + Storage-Bridge) abgeschlossen. Owner-Map in `INTERFACE_CONTRACT §9.1` dokumentiert.
