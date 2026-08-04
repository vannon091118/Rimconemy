# Rimconemy

> **Du bist der letzte Überlebende. Und je größer du wirst, desto lauter schreit die Welt nach dir.**

**Rimconemy** ist ein modulares RimWorld-Overhaul: Ein einzelner Überlebender, eine zerstörte Welt, ein Plan. Aus Bauschutt werden Wände, aus Farmen Nahrung, aus Wasser und Kohle Strom, aus Strom Verteidigung. Du wirtschaftest mit Credits, investierst in Outposts, expandierst über die Weltkarte — und jeder Erfolg erhöht den Druck. Denn die Infizierten haben einen eigenen Story-Writer, und der hat dich längst auf der Liste.

---

## 🎮 Was ist das? Der Elevator Pitch

Stell dir RimWorld vor, komprimiert auf die Essenz: **Ein Siedler. Kein Colony-Management-Überfluss. Ein langer Überlebenszug nach oben.**

Rimconemy nimmt dir das gemütliche Aufbauen nicht weg — es macht es zur **Entscheidung**. Jede Ressource ist knapp, jede Investition sichtbar, jede Bedrohung erklärbar. Es gibt keine stillen Schwierigkeits-Multiplikatoren. Wenn etwas schiefgeht, weißt du **warum**.

Die zentrale Fantasie: **Wachstum erzeugt Aufmerksamkeit.** Je stärker deine Basis wird, desto eher kommen sie. Du kannst dich klein und unsichtbar halten — oder groß werden und den Preis zahlen.

## 🧭 Wozu? Dein Spielkreislauf

```text
Überleben → Aufbauen → Wirtschaften → Expandieren → (Automatisieren) → Verteidigen → Game Over oder Endgame
```

1. **Überleben** — Ein Überlebender, 18 Jahre, mit deinem Skillbudget (30 Punkte, zwölf Skills). Deine Traits sind das Ergebnis deiner Entscheidungen, nicht des Zufalls.
2. **Aufbauen** — Bauschutt wird zur Wand, Hanf und Farmen ernähren dich, Wasser + Brennstoff betreiben den Generator, Strom den Pfeilturm. Ressourcen werden **echt** aus deinen Lagern gelesen — kein zweites unsichtbares Inventar.
3. **Wirtschaften** — Credits sind eine saubere Wallet, Silber ist physisches Material. Lokale Märkte mit deterministischen Preisen, kein Vanilla-Preismurks.
4. **Expandieren** — Outposts brauchen Investition und Verteidigung. Territorium ist eine **reale Verbindung** über die Weltkarte, kein UI-Symbol.
5. **Druck** — Der Story-Writer bewertet deine Lage (Lager, Bedrohung, Ideologie) und wählt erklärbare Events: Versorgungskrise, ideologischer Konflikt, äußere Bedrohung. Deterministisch, nachvollziehbar, fair.
6. **Das Ende** — Eines Tages ist Schluss. Das Game Over ist Teil des Designs — und kommt genau einmal.

## 🔭 Was soll es werden? Die Vision

Ein **komplettes, modulares Survival-Economy-Overhaul** — in fünf Paketen, die auch einzeln spielbar sind:

| # | Paket | Deine Rolle darin |
|---|---|---|
| **01** | **Foundation** | Die Basis: Dashboard, Diagnose, Mod-/DLC-Erkennung, Save-Status, Eventlog |
| **02** | **Survival & Progression** | Dein Charakter: Bedürfnisse, Arbeit → XP, Spezialisierung, Forschung, Game Over |
| **03** | **Scavenger Infrastructure** | Dein Basislager: Bauschutt, Farmen, Hanf, Wasser, Strom, Pfeilturm |
| **04** | **Economy & Territory** | Dein Imperium: Credits, Märkte, Outposts, Territorium, Weltkarten-Raids |
| **05** | **Infected & Automation** | Dein Feind & dein Werkzeug: Story-Writer, Bedrohungsdruck, Infizierte, Mechadroids |

**Wo das Projekt gerade steht:** Alle fünf Pakete kompilieren und booten in RimWorld 1.6 — die Story-Schicht (Determinismus, Cooldowns, Eventkatalog), die Charakter-Regeln, die Storage-Read-Models und die Wallet-Domäne sind code- und runtime-belegt. Was noch fehlt: der **echte Spiel-Loop** — Save/Load-Roundtrips, Live-Event-Feuerung, echte Infizierten-Spawns, Territorium auf der Weltkarte. Genau daran wird gebaut. Der ehrliche Status steht in [`docs/CODE_STATUS.md`](docs/CODE_STATUS.md).

## ⚙️ Voraussetzungen

- RimWorld **1.6**
- **Harmony**
- **Anomaly** + **Odyssey** (Hard-Require — Rimconemy weiß, was es will)
- Ein gesundes Maß an Selbstironie

## 🛠️ Entwicklung

```bash
./scripts/runtime_test.sh                  # Build + Deploy + RimWorld-Start + Log-Gates
./scripts/runtime_test.sh --skip-start --no-deploy   # nur statischer Check
```

Der kanonische Boot-Test verlangt einen *frisch veränderten* `Player.log` — alte Logs werden abgelehnt. So stellen wir sicher, dass das Spiel wirklich gestartet wurde und nicht nur so tut.

## 📌 Roadmap-Kern

- ✅ Story-Writer: Difficulty-Profile (`Refuge` / `Survival` / `Collapse`), deterministische Eventauswahl, Cooldowns, Idempotenz
- ✅ Character Setup: Bio → Skillbudget 30 → Traits (Balance-Regeln dokumentiert)
- ✅ Storage-only-Read-Model: echte Lagerbestände, kein Parallelinventar
- 🔄 Als Nächstes: erster echter Spiel-Moment — Bauschutt → Wand/Tür, dann die vertikale Full-Profile-Kette
- ⬜ Später: Infizierten-Raids, Mechadroids, Outposts & Proxy-Graph, Weltkarten-Endgame

---

*Rimconemy — Mehr Dashboards. Weniger Spaß. Aber mit Regressionstests.*
