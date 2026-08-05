# Mitmachen bei Rimconemy

Willkommen im Maschinenraum. Hier werden fünf RimWorld-Mods gebaut, die sich über versionierte Verträge verständigen und trotzdem gelegentlich so tun, als wäre ein fehlender Save-State eine Designentscheidung.

Rimconemy richtet sich an RimWorld 1.6 und befindet sich in der **Pre-Alpha**. Der wichtigste Beitrag ist nicht der größte Patch, sondern derjenige, der eine klare Grenze einhält, einen Test mitbringt und keine neue Parallelwelt für bereits vorhandene Daten erfindet.

## Vor dem ersten Build

Du brauchst lokal:

- RimWorld **1.6.4566** oder eine kompatible RimWorld-1.6-Installation;
- das **.NET SDK**;
- **Harmony** und die RimWorld-/Harmony-Assemblies für den lokalen Build;
- unter Linux eine RimWorld-Installation, die mit den vorhandenen Skripten erreichbar ist.

Die Deploy-Skripte verwenden standardmäßig:

```text
/home/vannon/GOG Games/RimWorld/game/
```

Wenn deine Installation anders liegt, prüfe die Pfade in `scripts/deploy.sh` und `scripts/runtime_test.sh`, bevor du den großen roten Knopf drückst. Der Knopf ist technisch nur ein Shell-Skript. Das macht ihn nicht weniger dramatisch.

## Build und Tests

### Alle Pakete bauen und deployen

```bash
./scripts/deploy.sh
```

Das ist der kanonische Build-/Deploypfad. Er baut die fünf Pakete gegen die lokale RimWorld-1.6-Installation und synchronisiert die öffentlichen Mod-Artefakte in den RimWorld-Modordner. Dabei verwendet das Skript `rsync --delete`: Der Zielordner wird also an den Quellstand angeglichen. Prüfe den Zielpfad, bevor du es ausführst; der rote Knopf ist klein, aber nicht harmlos.

### Statischer Installationscheck

```bash
./scripts/runtime_test.sh --skip-start --no-deploy
```

Dieser Check prüft installierte Mod-Artefakte, Paket-IDs, `supportedVersions` und Assemblies, ohne RimWorld zu starten.

### Vollständiger Runtime-Gate-Test

```bash
./scripts/runtime_test.sh --require-scenario-tests
```

Der Runtime-Test baut und deployed standardmäßig, startet RimWorld für ein begrenztes Zeitfenster und wertet ausschließlich einen frisch erzeugten `Player.log` aus. Er prüft Boot-, Registry- und Regression-Marker.

Wichtig: Ein grüner Runtime-Boot ist ein **BOOT-Beleg**, kein vollständiger **LIVE-Beleg**. Der Log kann nachweisen, dass die Mod geladen wurde. Er kann nicht nachweisen, dass dein Kolonist im dritten Anlauf eine sinnvolle Entscheidung getroffen hat.

## Architekturregeln

### Paketbesitz respektieren

Die fünf Pakete haben getrennte Verantwortungsbereiche:

- **Foundation:** Registry, Diagnose, gemeinsame Read-Models, Capabilities und Save-Metadaten.
- **Survival & Progression:** Pawn-/Character-Setup, Bedürfnisse, XP, Forschung und der alleinige Game-Over-Pfad.
- **Scavenger Infrastructure:** physische Lagerbestände, Gebäude-, Strom- und Infrastruktur-Read-Models.
- **Economy & Territory:** Credits, Märkte, Transfers, Outposts und Territorium.
- **Infected & Automation:** Setting-Profile, Story-Events, Bedrohungsdruck, Infizierte und Automation.

Neue Logik gehört in das Paket, das sie fachlich besitzt. Wenn eine Funktion nur funktioniert, weil drei Pakete heimlich dieselbe Variable lesen, funktioniert sie vermutlich vor allem im Review-Dokument.

### Keine parallelen Wahrheiten

- Physische Ressourcen werden aus dem Storage-Read-Model gelesen.
- Credits bleiben eine getrennte Wallet und werden nicht als physisches Material ausgegeben.
- UI, Story und Economy dürfen nicht jeweils ein eigenes Inventar pflegen.
- Zustandsänderungen brauchen stabile IDs, deterministische Eingaben und Idempotenzschlüssel.

### Save-State und Migration ernst nehmen

Neue persistente Zustände brauchen:

- ein eigenes, dokumentiertes Schema;
- eine Schema-Version;
- einen nachvollziehbaren Migrationspfad oder eine kontrollierte Ablehnung;
- Scribe-Roundtrip-Tests, wenn der Zustand gespeichert wird.

Fehlende Daten dürfen nicht still verschwinden. RimWorld hat bereits genug Möglichkeiten, Dinge zu verlieren.

### APIs nicht aus der Fantasie ableiten

Eine Assembly-Zeichenkette oder ein vertraut klingender Klassenname ist noch keine verifizierte RimWorld-1.6-API. Vor neuen Harmony-Patches oder Def-Annahmen gilt:

1. lokale Assemblys, Defs und vorhandene Projektmuster prüfen;
2. die konkrete Signatur oder den Def-Typ belegen;
3. native Defs, Components und Capabilities vor Harmony bevorzugen;
4. die Annahme mit einem gezielten Test oder Runtime-Gate absichern.

Harmony ist ein Werkzeug, kein Orakel.

### Determinismus und genau-einmalige Ausführung

Neue Story-, Wallet-, Transfer- oder World-State-Logik muss:

- stabile Sortierung verwenden;
- keine Systemzeit als unerklärte Spieleingabe verwenden;
- Auswahlgrund und relevante Eingabedaten speichern, wenn sie spielrelevant sind;
- dieselbe Anfrage über ihren Idempotency-Key höchstens einmal wirksam ausführen;
- Save/Load und Wiederholung explizit berücksichtigen.

Das System ist deterministisch. Das Chaos hat nur andere Eingabewerte.

## Änderungen an Defs und DLC-Policy

- Neue XML-Defs müssen zur RimWorld-1.6-Struktur passen und mit ihren Paketgrenzen übereinstimmen.
- DLC-Inhalte werden über die bestehende Foundation-DLC-Policy klassifiziert.
- Anomaly und Odyssey sind für das Foundation-basierte Full-Profile harte Voraussetzungen; einzelne Feature-Pakete deklarieren diese DLCs nicht jeweils selbst.
- Royalty, Ideology und Biotech werden nicht ohne dokumentierte Entscheidung zu neuen globalen Hard-Requires.
- Keine globale Unterdrückung fremder Vanilla-/DLC-Inhalte ohne Kompatibilitäts- und Runtime-Beleg.

## Pull-Request-Checkliste

Vor dem Öffnen eines Pull Requests:

- [ ] Änderung bleibt in der fachlich zuständigen Paketgrenze.
- [ ] Bestehende Schnittstellen, Save-Verträge und DLC-Policy wurden geprüft.
- [ ] Passende Regressionstests wurden ergänzt oder ausgeführt.
- [ ] Betroffene Pakete bauen gegen RimWorld 1.6.4566.
- [ ] `bash -n scripts/deploy.sh scripts/runtime_test.sh scripts/verify_bootstrap_log.sh` ist erfolgreich.
- [ ] Der statische Check oder der passende Runtime-Gate-Test wurde ausgeführt.
- [ ] Öffentliche Statusangaben wurden aktualisiert, falls sich der Belegstatus ändert.
- [ ] `COMPILES` oder `BOOT` wird nicht als `LIVE` verkauft.
- [ ] Offene Runtime-Gates sind ausdrücklich dokumentiert.
- [ ] Keine generierten Assemblies, lokalen Logs oder Spielstände wurden eingecheckt.

## Wichtige Dokumente

- [`README.md`](README.md) — Spieler- und Developer-Einstieg
- [`docs/GITHUB_RELEASE_METADATA.md`](docs/GITHUB_RELEASE_METADATA.md) — Repository-Description, Topics und Release-Copy
- [`ROADMAP.md`](ROADMAP.md) — kanonischer Plan und Backlog
- [`docs/CODE_STATUS.md`](docs/CODE_STATUS.md) — Belegstufen und Paketstatus
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — Architekturentscheidungen
- [`docs/INTERFACE_CONTRACT.md`](docs/INTERFACE_CONTRACT.md) — Capabilities und Paketverträge
- [`docs/SAVE_CONTRACT.md`](docs/SAVE_CONTRACT.md) — Persistenz und Migration
- [`docs/COMPATIBILITY_MATRIX.md`](docs/COMPATIBILITY_MATRIX.md) — RimWorld-/DLC-Kompatibilität
- [`01 Foundation BLUEPRINT`](mods/01-Rimconemy-Foundation/BLUEPRINT.md), [`02 Survival BLUEPRINT`](mods/02-Rimconemy-Survival-Progression/BLUEPRINT.md), [`03 Scavenger BLUEPRINT`](mods/03-Rimconemy-Scavenger-Infrastructure/BLUEPRINT.md), [`04 Economy BLUEPRINT`](mods/04-Rimconemy-Economy-Territory/BLUEPRINT.md), [`05 Infected BLUEPRINT`](mods/05-Rimconemy-Infected-Automation/BLUEPRINT.md) — technische Grenzen der einzelnen Pakete

## GitHub-Auftritt

Die kopierfertige Repository-Description, Topic-Liste und zweisprachige Release-Copy liegen in [`docs/GITHUB_RELEASE_METADATA.md`](docs/GITHUB_RELEASE_METADATA.md). Diese Datei dokumentiert Vorschläge; GitHub-Einstellungen werden nicht automatisch aus dem Repository geändert.

## Schlusswort

Wenn ein Patch klein, überprüfbar und langweilig genug ist, um zuverlässig zu sein, ist er wahrscheinlich ein guter Patch. Die Infizierten werden das anders sehen. Das tun sie beruflich.

<details>
<summary>🇬🇧 English version — click to expand</summary>

<a id="english-contributing-start"></a>

# Contributing to Rimconemy

<p align="center">
  <a href="#english-before-first-build">Before your first build</a> ·
  <a href="#english-build-and-tests">Build and tests</a> ·
  <a href="#english-architecture-rules">Architecture</a> ·
  <a href="#english-pull-request-checklist">Pull requests</a> ·
  <a href="#english-important-documents">Documents</a>
</p>

Welcome to the machine room. Five RimWorld mods are built here around versioned contracts, while occasionally pretending that a missing save state is a design decision.

Rimconemy targets RimWorld 1.6 and is currently **pre-alpha**. The most valuable contribution is not the largest patch; it is the one that respects a clear boundary, brings a test, and does not invent another parallel truth for data that already has an owner.

<a id="english-before-first-build"></a>

## Before your first build

You need locally:

- RimWorld **1.6.4566** or a compatible RimWorld 1.6 installation;
- the **.NET SDK**;
- **Harmony** and the RimWorld/Harmony assemblies required for the local build;
- on Linux, a RimWorld installation reachable by the existing scripts.

The deploy scripts use this path by default:

```text
/home/vannon/GOG Games/RimWorld/game/
```

If your installation is elsewhere, check the paths in `scripts/deploy.sh` and `scripts/runtime_test.sh` before pressing the big red button. Technically, the button is only a shell script. That does not make it less dramatic.

<a id="english-build-and-tests"></a>

## Build and tests

### Build and deploy all packages

```bash
./scripts/deploy.sh
```

This is the canonical build/deploy path. It builds all five packages against the local RimWorld 1.6 installation and synchronizes the public mod artifacts into the RimWorld Mods directory. The script uses `rsync --delete`, so the destination is made to match the source state. Check the destination before running it; the red button is small, but not harmless.

### Static installation check

```bash
./scripts/runtime_test.sh --skip-start --no-deploy
```

This check verifies installed mod artifacts, package IDs, `supportedVersions`, and assemblies without starting RimWorld.

### Full runtime gate test

```bash
./scripts/runtime_test.sh --require-scenario-tests
```

The runtime test builds and deploys by default, starts RimWorld for a bounded time window, and evaluates only a freshly generated `Player.log`. It checks boot, registry, and regression markers.

Important: a green runtime boot is **BOOT evidence**, not complete **LIVE evidence**. The log can prove that the mod loaded. It cannot prove that your colonist made a sensible decision on the third attempt.

<a id="english-architecture-rules"></a>

## Architecture rules

### Respect package ownership

The five packages have separate responsibilities:

- **Foundation:** registry, diagnostics, shared read models, capabilities, and save metadata.
- **Survival & Progression:** pawn/character setup, needs, XP, research, and the sole game-over path.
- **Scavenger Infrastructure:** physical storage, building, power, and infrastructure read models.
- **Economy & Territory:** credits, markets, transfers, outposts, and territory.
- **Infected & Automation:** setting profiles, story events, threat pressure, infected, and automation.

New logic belongs in the package that owns it technically and conceptually. If a feature works only because three packages secretly read the same variable, it probably works mainly in the review document.

### No parallel truths

- Physical resources are read from the storage read model.
- Credits remain a separate wallet and are not emitted as physical material.
- UI, story, and economy must not maintain separate inventories.
- State changes need stable IDs, deterministic inputs, and idempotency keys.

### Treat save state and migration seriously

New persistent state needs:

- its own documented schema;
- a schema version;
- a traceable migration path or controlled rejection;
- Scribe roundtrip tests when the state is saved.

Missing data must not disappear silently. RimWorld already offers enough ways to lose things.

### Do not invent APIs

A string found in an assembly or a familiar class name is not a verified RimWorld 1.6 API. Before adding Harmony patches or Def assumptions:

1. inspect local assemblies, Defs, and existing project patterns;
2. prove the concrete signature or Def type;
3. prefer native Defs, Components, and Capabilities over Harmony;
4. protect the assumption with a focused test or runtime gate.

Harmony is a tool, not an oracle.

### Determinism and exactly-once execution

New story, wallet, transfer, or world-state logic must:

- use stable sorting;
- avoid system time as unexplained game input;
- store the selection reason and relevant input data when gameplay-relevant;
- apply the same request at most once through its idempotency key;
- explicitly account for save/load and repetition.

The system is deterministic. The chaos only has different input values.

<a id="english-defs-and-dlc-policy"></a>

## Def changes and DLC policy

- New XML Defs must match the RimWorld 1.6 structure and respect package boundaries.
- DLC content is classified through the existing Foundation DLC policy.
- Anomaly and Odyssey are hard requirements for the Foundation-based Full profile; individual feature packages do not each declare these DLCs themselves.
- Royalty, Ideology, and Biotech are not made global hard requirements without a documented decision.
- Do not globally suppress foreign Vanilla/DLC content without compatibility and runtime evidence.

<a id="english-pull-request-checklist"></a>

## Pull request checklist

Before opening a pull request:

- [ ] The change stays within the responsible package boundary.
- [ ] Existing interfaces, save contracts, and DLC policy were checked.
- [ ] Relevant regression tests were added or run.
- [ ] Affected packages build against RimWorld 1.6.4566.
- [ ] `bash -n scripts/deploy.sh scripts/runtime_test.sh scripts/verify_bootstrap_log.sh` succeeds.
- [ ] The static check or appropriate runtime gate was run.
- [ ] Public status claims were updated if the evidence status changed.
- [ ] `COMPILES` or `BOOT` is not presented as `LIVE`.
- [ ] Open runtime gates are documented explicitly.
- [ ] No generated assemblies, local logs, or save files were committed.

<a id="english-important-documents"></a>

## Important documents

- [`README.md`](README.md) — player and developer entry point
- [`docs/GITHUB_RELEASE_METADATA.md`](docs/GITHUB_RELEASE_METADATA.md) — repository description, topics, and release copy
- [`ROADMAP.md`](ROADMAP.md) — canonical plan and backlog
- [`docs/CODE_STATUS.md`](docs/CODE_STATUS.md) — evidence levels and package status
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — architectural decisions
- [`docs/INTERFACE_CONTRACT.md`](docs/INTERFACE_CONTRACT.md) — capabilities and package contracts
- [`docs/SAVE_CONTRACT.md`](docs/SAVE_CONTRACT.md) — persistence and migration
- [`docs/COMPATIBILITY_MATRIX.md`](docs/COMPATIBILITY_MATRIX.md) — RimWorld/DLC compatibility
- [`01 Foundation BLUEPRINT`](mods/01-Rimconemy-Foundation/BLUEPRINT.md), [`02 Survival BLUEPRINT`](mods/02-Rimconemy-Survival-Progression/BLUEPRINT.md), [`03 Scavenger BLUEPRINT`](mods/03-Rimconemy-Scavenger-Infrastructure/BLUEPRINT.md), [`04 Economy BLUEPRINT`](mods/04-Rimconemy-Economy-Territory/BLUEPRINT.md), [`05 Infected BLUEPRINT`](mods/05-Rimconemy-Infected-Automation/BLUEPRINT.md) — technical boundaries for each package

## GitHub presence

The copy-ready repository description, topic list, and bilingual release copy are in [`docs/GITHUB_RELEASE_METADATA.md`](docs/GITHUB_RELEASE_METADATA.md). That file documents suggestions; GitHub settings are not changed automatically from the repository.

## Closing note

If a patch is small, verifiable, and boring enough to be reliable, it is probably a good patch. The infected will disagree. That is their profession.

</details>
