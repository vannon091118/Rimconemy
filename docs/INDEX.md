# Rimconemy — Document Index (SSOT Map)

> **Stand:** 2026-08-04
> **Pflicht-Reihenfolge beim Schreiben:** Lieber Querverweis als Textwiederholung. Wer Inhalt aus diesem Index anfasst, hält eine SSOT-Verletzung fest und migriert die Stellen.

## 1. Single Source of Truth — Tabelle

| Topic | SSOT-Owner | Pfad |
|---|---|---|
| 5-Paket-Identitäten, Owner-Spalte, Mod-Liste | **ROADMAP.md** | [../ROADMAP.md §2](../ROADMAP.md) + **INTERFACE_CONTRACT.md §9** | [INTERFACE_CONTRACT.md](INTERFACE_CONTRACT.md) |
| Paket-Compile-Dependencies (Skript `Bootstrap`) | **INTERFACE_CONTRACT.md** | [INTERFACE_CONTRACT.md §3](INTERFACE_CONTRACT.md) |
| Paket-Eigentumsgrenzen (welches Package besitzt was) | **INTERFACE_CONTRACT.md** | [INTERFACE_CONTRACT.md §9.1](INTERFACE_CONTRACT.md) |
| Servicebus-Vertrag, Capability-IDs, Read/Write-Lanes | **INTERFACE_CONTRACT.md** | [INTERFACE_CONTRACT.md §3](INTERFACE_CONTRACT.md) |
| `ISchemaMigratable`-Vertrag, Schema-Bump-Pattern | **SAVE_CONTRACT.md** | [SAVE_CONTRACT.md](SAVE_CONTRACT.md) |
| Save/Load-Lifecycle (Foundation-Save-Bridge, IExposable-Adapter) | **SAVE_CONTRACT.md** | [SAVE_CONTRACT.md](SAVE_CONTRACT.md) |
| Vanilla↔Rimconemy-Domain-Mapping (Worker/Good/Room/Tech/State/Transfer) | **CANONICAL_VANILLA_DOMAIN_MAP.md** | [CANONICAL_VANILLA_DOMAIN_MAP.md](CANONICAL_VANILLA_DOMAIN_MAP.md) |
| Tech/Wissen, Experience-Bäume, Unlock-Extension | **CANONICAL_VANILLA_DOMAIN_MAP.md §2.4** | [CANONICAL_VANILLA_DOMAIN_MAP.md §2.4](CANONICAL_VANILLA_DOMAIN_MAP.md) |
| DLC-Kompatibilitätstabelle (Royalty/Ideology/Biotech/Anomaly/Odyssey) | **COMPATIBILITY_MATRIX.md** | [COMPATIBILITY_MATRIX.md](COMPATIBILITY_MATRIX.md) |
| Third-Party-Mod-Klassifikation (CE, VSE, ...) | **COMPATIBILITY_MATRIX.md** | [COMPATIBILITY_MATRIX.md §3](COMPATIBILITY_MATRIX.md) |
| Design-Entscheidungen (Architektur-Wahlen mit Datum & Kontext) | **DECISIONS.md** | [DECISIONS.md](DECISIONS.md) |
| Vanilla-1.6-API-Matrix (Klassen, Methoden, Owner-Spalte) | **docs/ARCHITECTURE.md §1** | [ARCHITECTURE.md §1](ARCHITECTURE.md) |
| Story-Profile, Event-Katalog, Story-Vertrag | **docs/ARCHITECTURE.md §2** | [ARCHITECTURE.md §2](ARCHITECTURE.md) |
| Ideology-Influence-Matrix (Setting Rule 1–3) | **docs/ARCHITECTURE.md §3** | [ARCHITECTURE.md §3](ARCHITECTURE.md) |
| Storage-Query-Vertrag | **docs/ARCHITECTURE.md §4** | [ARCHITECTURE.md §4](ARCHITECTURE.md) |
| Character-Setup-Formel | **docs/ARCHITECTURE.md §5** | [ARCHITECTURE.md §5](ARCHITECTURE.md) |
| Pawn-Generator-API-Spike | **docs/ARCHITECTURE.md §6** | [ARCHITECTURE.md §6](ARCHITECTURE.md) |
| Was ist `COMPILED`/`LOADED`/`RUNNING`/live | **CODE_STATUS.md** | [CODE_STATUS.md](CODE_STATUS.md) |
| Falsifikations-Berichts-Index (20/22/27-Count) | **docs/falsification/README.md** | [falsification/README.md](falsification/README.md) |
| Falsifikationsbericht-Eintragspunkt pro Task | **docs/falsification/<topic>__<subtopic>.md** (siehe Index) | [falsification/](falsification/) |

## 2. Role-Of-Categories

**Kanonische Vertragsdokumente** (SSOT-Owner; kein Duplikat-Inhalt woanders):
- `ROADMAP.md` — Master-Plan, Paket-Übersicht, Sole-Owner-Map
- `docs/INTERFACE_CONTRACT.md` — Architectural Inheritance Boundaries
- `docs/SAVE_CONTRACT.md` — Save/Lifecycle-Vertrag
- `docs/COMPATIBILITY_MATRIX.md` — DLC/Mod-Klassifikation
- `docs/CANONICAL_VANILLA_DOMAIN_MAP.md` — Vanilla↔Rimconemy-Mapping
- `docs/DECISIONS.md` — Entscheidungs-Log
- `docs/CODE_STATUS.md` — Live-Status aller Pakete
- `docs/ARCHITECTURE.md` — Vanilla-API-Matrix + Story-Contract + Ideology + Storage + Character + Pawn-Generator

**Implementierungs-Specs** (sprintspezifisch, veränderbar, dürfen duplizieren):
- `docs/superpowers/specs/` — Design-Specs (vor Implementation)
- `docs/superpowers/plans/` — Implementationspläne (mit Task-Lists)

**Gameplay-Maps & Fortschritte** (sprintspezifisch, keep local):
- `task_plan.md` — UI-Toolkit-Sprint-Scope
- `docs/P6-PROGRESS.md` — P6 Multi-Task-Fortschritt
- `mods/0X/ROADMAP.md`, `mods/0X/BLUEPRINT.md` — Paket-spezifische Pläne & Eigentumsgrenzen
- `docs/falsification/*.md` — Task-bezogene Falsifikationsberichte

**Pitches** (Werbung, schnellster Einstieg):
- `README.md` — Pitch + Überblick, kein technischer SSOT
- `banner.html` — Visuelle Darstellung

## 3. Schreib-Regeln

1. Wer einen oben gelisteten Topic behandelt, schreibt in die SSOT-Quelle und fügt anderswo nur `→ siehe [docs/INDEX.md §1]`.
2. Wer einen Topic einführt, der noch nicht gemappt ist, ergänzt zuerst die Tabelle in §1 und schreibt dann die SSOT-Datei.
3. Konflikte zwischen SSOT-Dateien sind sofort in `DECISIONS.md §<nnn>` zu dokumentieren.
4. Modul-Eigentum in Tabellen mehrmals gelistet? → ein Verweis auf `INTERFACE_CONTRACT §9.1`.
5. Versions-Snapshot-Tabelle (z. B. `0.1.37`) wiederholt? → ein Verweis auf `mods/<paket>/VERSION`.

## 4. Pflicht-Verweise

Jede SSOT-Datei beginnt mit einer `> **SSOT-Owner für:** <topic-line>`-Notiz, damit Leser nicht in einer falschen Datei landet. Die Topics sind in §1 abschließend gelistet; jede Datei kennt genau ihr Topic.

## 5. Was diese Konsolidierung NICHT ist

Es werden keine bestehenden Routen, Codes, Build-Pfade oder Falsifikationsberichts-Inhalte gelöscht. Nur Redundanz auf der Dokumentationsebene wird abgebaut. Datei-Inhalte werden entweder konsolidiert (z. B. H1–H6 in `ARCHITECTURE.md`) oder durch SSOT-Verweise ersetzt.
