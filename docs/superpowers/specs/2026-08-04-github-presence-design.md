# GitHub-Auftritt — Spielerfreundliche Developer-Landingpage

> Design-Spec, 2026-08-04

## Ziel

Der GitHub-Auftritt von Rimconemy soll zwei Zielgruppen bedienen, ohne beide mit derselben Dokumentationslawine zu erschlagen:

- **Spieler/Nutzer** erhalten einen schnellen, ehrlichen und humorvoll geschriebenen Einstieg mit Voraussetzungen, Installation, Modulübersicht und Entwicklungsstatus.
- **Developer/Contributors** erhalten darunter einen klaren Einstieg in Build, Tests, Architektur, Statusgrenzen und Contribution-Regeln.

Die Autorenstimme ist trocken, ironisch und selbstbewusst. Sie darf die absurden Konsequenzen des Spiels kommentieren, aber keine technische Realität beschönigen.

## Tonalität und redaktionelle Regeln

- Deutsch als primäre README-Sprache.
- Trockener RimWorld-Humor: knapp, beobachtend, leicht fatalistisch.
- Ironie richtet sich auf Kolonie- und Projektlogik, niemals auf Nutzer, Maintainer oder Beiträge.
- Technische Aussagen bleiben sachlich und werden nicht durch Witze entwertet.
- `CODE`, `DEF`, `COMPILES`, `BOOT` und `LIVE` werden nicht vermischt.
- Nicht live belegte Gameplay-Schichten werden als in Entwicklung, Scaffold oder offen markiert.
- Keine Behauptung eines vollständigen oder bedenkenlos spielbaren Overhauls.
- Wiederkehrende Stimme: „Das System ist deterministisch. Das Chaos hat nur andere Eingabewerte.“

## Informationsarchitektur

### `README.md`

Das README wird als zweistufige Landingpage neu strukturiert:

1. **Hero**
   - vorhandenes `banner.svg` behalten und auf `banner.html` verlinken;
   - kurzer Einzeiler mit der Kernfantasie „Wachstum erzeugt Aufmerksamkeit“;
   - sichtbarer Hinweis `Pre-Alpha / aktiv in Entwicklung`.

2. **Spieler-Einstieg**
   - kurzer Elevator Pitch;
   - „Was erwartet dich?“ mit Überleben, Aufbau, Wirtschaft, Expansion und Bedrohungsdruck;
   - humorvoller, aber klarer Hinweis, dass Boot-/Build-Belege nicht dasselbe wie ein vollständig verifiziertes Live-Spiel sind;
   - Voraussetzungen: RimWorld 1.6, Harmony, Anomaly und Odyssey;
   - Installationsanleitung inklusive Ladefolge `Core/DLCs → Harmony → Foundation → Survival → Scavenger → Economy → Infected`;
   - Link zur Kompatibilitätsmatrix.

3. **Was funktioniert wirklich?**
   - kompakte Statusampel für tatsächlich belegte Bereiche: fünf Pakete, lokaler Build, Runtime-Boot, Foundation-Diagnose, Regression-Summaries;
   - offen markieren: vollständiger Save/Load-Lauf, Kartenwechsel, vollständige Event-/Raid-Auflösung und echte Gameplay-Loops;
   - Verweis auf `docs/CODE_STATUS.md` für die Belegstufen.

4. **Die fünf Module**
   - je Modul: Spielerrolle, aktuell belegter Kern und ehrliche Einschränkung;
   - keine Übernahme von vollständigen `About.xml`-Versprechen, wenn der zentrale Status sie noch nicht als `LIVE` belegt.

5. **Roadmap**
   - drei kompakte Gruppen: `Jetzt`, `Als Nächstes`, `Später, wenn die Infizierten zustimmen`;
   - vollständige technische Aufgaben bleiben in `ROADMAP.md`.

6. **Developer-Einstieg**
   - kurze Build-/Test-Befehle;
   - Hinweis, dass `scripts/deploy.sh` den kanonischen Deploypfad besitzt;
   - Links auf `CONTRIBUTING.md`, `docs/CODE_STATUS.md`, `docs/DECISIONS.md`, `docs/INTERFACE_CONTRACT.md`, `docs/SAVE_CONTRACT.md` und `docs/COMPATIBILITY_MATRIX.md`.

7. **Footer**
   - Lizenz-/Mitwirkendenhinweis nur, sofern im Repository bereits belegt;
   - keine erfundenen Download-, Workshop- oder Release-Links.

### `CONTRIBUTING.md`

Neue, eigenständige Developer-Dokumentation:

- kurzer Projektkontext und gewünschter Schreib-/Arbeitsstil;
- Voraussetzungen: RimWorld 1.6.4566 lokal, .NET SDK, Harmony-Assemblies und vorhandene lokale Spielinstallation;
- Build: `./scripts/deploy.sh --all`;
- statischer Installations-/Artefaktcheck: `./scripts/runtime_test.sh --skip-start --no-deploy`;
- vollständiger Runtime-Gate-Test nur mit passender lokaler RimWorld-Installation;
- Änderungen an Code/Defs/XML müssen Paketgrenzen, Save-Verträge, Determinismus und DLC-Policy beachten;
- Regressionstests werden über die Paket-Bootstraps ausgeführt;
- Statussprache: niemals `LIVE` aus `COMPILES` oder `BOOT` ableiten;
- Pull-Request-Checkliste mit Build, passenden Tests, Statusdokumentation und explizitem Hinweis auf offene Runtime-Gates;
- Links auf die bestehenden Architektur- und Statusdokumente.

Die Datei wird bewusst nicht zu einem allgemeinen GitHub-Knigge. Sie erklärt, wie man an diesem RimWorld-Projekt arbeitet, ohne versehentlich eine zweite Wirtschaft, einen zweiten Save-State oder einen dritten Storyteller zu erfinden.

### `banner.html`

Das vorhandene visuelle Konzept bleibt erhalten: dunkle technische Oberfläche, rote Bedrohungsakzente, Raster und Modulchips. Es werden nur öffentliche Aussagen aktualisiert:

- Version nicht hart als veralteten Einzelstand präsentieren, sondern als `RIMWORLD 1.6 · PRE-ALPHA`;
- Modulchips auf den aktuellen fünf-Paket-Schnitt ausrichten;
- Unterzeile auf die Kernfantasie und den ehrlichen Entwicklungsstatus fokussieren;
- keine dynamischen Abhängigkeiten oder externen Assets hinzufügen.

`banner.svg` bleibt der README-kompatible statische Einstieg; eine SVG-Neuerstellung ist nicht Teil dieses Scopes, solange die bestehende Grafik die aktualisierten Aussagen nicht widerspricht.

## Statuswahrheit

Die öffentliche Formulierung basiert auf `docs/CODE_STATUS.md` und `ROADMAP.md`:

- belegt: RimWorld 1.6.4566 als Zielplattform, fünf Pakete, lokaler Build, Runtime-Boot und Regression-Summaries;
- offen: vollständiger Save/Load-Roundtrip, Kartenwechsel sowie vollständige Event-, Raid- und Gameplay-Ausführung;
- Anomaly und Odyssey werden als harte Abhängigkeiten dargestellt, weil sie in `Foundation/About/About.xml` deklariert sind;
- Royalty, Ideology und Biotech werden nicht als harte Voraussetzungen dargestellt.

## Scope und Nicht-Ziele

### Im Scope

- `README.md` redaktionell und strukturell neu schreiben;
- `CONTRIBUTING.md` neu anlegen;
- `banner.html` auf den öffentlichen Status und die fünf Module aktualisieren;
- Links, Befehle, Ladefolge und Statusaussagen gegen vorhandene Dateien prüfen;
- Markdown-/Shell-Sanity-Checks sowie eine Review der geänderten Dokumentation durchführen.

### Nicht im Scope

- kein Code, keine Defs und keine Mod-Logik ändern;
- keine Workshop-Veröffentlichung, kein Release und kein Git-Commit;
- keine Screenshots oder Videos erfinden;
- keine Lizenz ergänzen, die im Repository nicht belegt ist;
- keine bestehenden lokalen Änderungen anderer Arbeitsschritte überschreiben oder inhaltlich umformulieren.

## Erfolgskriterien

- Ein Spieler versteht innerhalb der ersten README-Abschnitte, was Rimconemy ist, welchen Status es hat und was für die Installation nötig ist.
- Ein Developer findet Build, Runtime-Check und die kanonischen Verträge ohne die gesamte Roadmap lesen zu müssen.
- Jede öffentliche Statusaussage lässt sich auf eine vorhandene Datei oder einen belegten Testpfad zurückführen.
- Humor und Autorenstimme sind sichtbar, aber kein Witz verschleiert eine offene technische Voraussetzung.
- Die Dokumentationsänderungen berühren keine fremden Code- oder Def-Änderungen.
