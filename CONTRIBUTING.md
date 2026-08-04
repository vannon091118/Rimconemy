# Mitmachen bei Rimconemy

Willkommen im Maschinenraum. Hier werden fünf RimWorld-Mods gebaut, die sich gegenseitig über versionierte Verträge verständigen und trotzdem gelegentlich so tun, als wäre ein fehlender Save-State eine Designentscheidung.

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
./scripts/deploy.sh --all
```

Das ist der kanonische Build-/Deploypfad. Er baut die fünf Pakete gegen die lokale RimWorld-1.6-Installation und synchronisiert die öffentlichen Mod-Artefakte in den RimWorld-Modordner. Dabei verwendet das Skript `rsync --delete`: Der Zielordner wird also an den Quellstand angeglichen. Prüfe den Zielpfad, bevor du es ausführst; der rote Knopf ist klein, aber nicht harmlos.

### Statischer Installationscheck

```bash
./scripts/runtime_test.sh --skip-start --no-deploy
```

Dieser Check prüft installierte Mod-Artefakte, Paket-IDs, `supportedVersions` und Assemblies, ohne RimWorld zu starten.

### Vollständiger Runtime-Gate-Test

```bash
./scripts/runtime_test.sh
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
- [ ] `bash -n scripts/deploy.sh scripts/runtime_test.sh` ist erfolgreich.
- [ ] Der statische Check oder der passende Runtime-Gate-Test wurde ausgeführt.
- [ ] Öffentliche Statusangaben wurden aktualisiert, falls sich der Belegstatus ändert.
- [ ] `COMPILES` oder `BOOT` wird nicht als `LIVE` verkauft.
- [ ] Offene Runtime-Gates sind ausdrücklich dokumentiert.
- [ ] Keine generierten Assemblies, lokalen Logs oder Spielstände wurden eingecheckt.

## Wichtige Dokumente

- [`README.md`](README.md) — Spieler- und Developer-Einstieg
- [`ROADMAP.md`](ROADMAP.md) — kanonischer Plan und Backlog
- [`docs/CODE_STATUS.md`](docs/CODE_STATUS.md) — Belegstufen und Paketstatus
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — Architekturentscheidungen
- [`docs/INTERFACE_CONTRACT.md`](docs/INTERFACE_CONTRACT.md) — Capabilities und Paketverträge
- [`docs/SAVE_CONTRACT.md`](docs/SAVE_CONTRACT.md) — Persistenz und Migration
- [`docs/COMPATIBILITY_MATRIX.md`](docs/COMPATIBILITY_MATRIX.md) — RimWorld-/DLC-Kompatibilität
- [`01 Foundation BLUEPRINT`](mods/01-Rimconemy-Foundation/BLUEPRINT.md), [`02 Survival BLUEPRINT`](mods/02-Rimconemy-Survival-Progression/BLUEPRINT.md), [`03 Scavenger BLUEPRINT`](mods/03-Rimconemy-Scavenger-Infrastructure/BLUEPRINT.md), [`04 Economy BLUEPRINT`](mods/04-Rimconemy-Economy-Territory/BLUEPRINT.md), [`05 Infected BLUEPRINT`](mods/05-Rimconemy-Infected-Automation/BLUEPRINT.md) — technische Grenzen der einzelnen Pakete

## Schlusswort

Wenn ein Patch klein, überprüfbar und langweilig genug ist, um zuverlässig zu sein, ist er wahrscheinlich ein guter Patch. Die Infizierten werden das anders sehen. Das tun sie beruflich.
