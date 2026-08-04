# Rimconemy

> Das RimWorld-Overhaul, das Sie nicht bestellt haben — aber das Ihr Siedler definitiv nicht verdient.

**Rimconemy** verwandelt Ihre gemütliche Kolonie in ein vollautomatisiertes, infrastruktur-schweres Wirtschaftsimperium mit Credits, Territorien, Infizierten und Mechadroids. Weil RimWorld bekanntlich viel zu friedlich war, wenn man einfach nur Essen anbauen kann.

## Warum existiert dieses Projekt?

Fünf modulare Mods, ein einziger Traum: RimWorld so umzubauen, dass der Spieler mehr Zeit mit Dashboards und Read-Models verbringt als mit dem eigentlichen Spiel. Fortschritt wird gemessen, gespeichert, analysiert und in Regressionstests gegossen — denn nichts ist wichtiger als die Gewissheit, dass Ihr Credits-Ledger auch beim 256. Eintrag noch idempotent ist.

## Die fünf Pakete (in absteigender Verantwortungsübernahme)

| Paket | Was es verspricht | Was es wirklich tut |
|---|---|---|
| **01 · Foundation** | Integrationsverträge, DLC-Politik, Diagnose | Ein Vanilla-Dashboard, das Sie beruhigt, weil es Zahlen anzeigt |
| **02 · Survival & Progression** | Nahrung, Sicherheit, Spezialisierung, Forschung | Ein Bedürfnis-Service, der Vanilla-Needs auf 0..1 skaliert, *ohne* sie anzuhängen |
| **03 · Scavenger Infrastructure** | Bauschutt, Farmen, Hanf, Wasser, Strom, Pfeilturm | Read-only Storage-Snapshots mit ContentHash — Ordnung muss sein |
| **04 · Economy & Territory** | Credits, Märkte, Outposts, Weltkarten-Raids | Eine Wallet-Domäne, die physische Waren lieber nicht anfasst |
| **05 · Infected & Automation** | Eigener Storyteller, Bedrohungsdruck, Mechadroids | Ein deterministischer RNG, damit auch der Zufall reproduzierbar scheitert |

## Belegstufen (der ehrliche Teil)

`CODE` → `DEF` → `COMPILES` → `BOOT` → `LIVE` → `OPEN`

Alle fünf Pakete erreichen mindestens `BOOT`. `LIVE` bleibt bewusst offen — denn ein Overhaul, das behauptet, fertig zu sein, hat den Geist des frühen Zugangs noch nicht verstanden. Details: [`docs/CODE_STATUS.md`](docs/CODE_STATUS.md).

## Voraussetzungen

- RimWorld **1.6** (GOG oder Steam, was auch immer Sie da treibt)
- **Harmony** (die eine Abhängigkeit, die niemand hinterfragt)
- **Anomaly** und **Odyssey** als Hard-Require (Foundation weiß, was es will)
- Ein gesundes Maß an Selbstironie

## Entwicklung

```bash
./scripts/runtime_test.sh            # Build + Deploy + RimWorld-Start + Log-Gates
./scripts/runtime_test.sh --skip-start --no-deploy   # nur statischer Check
```

Der kanonische Boot-Test verlangt einen *frisch veränderten* `Player.log` — alte Logs werden abgelehnt. So stellen wir sicher, dass das Spiel wirklich gestartet wurde und nicht nur so tut.

## Offene Live-Gates (kuratiert, damit Sie nicht enttäuscht sind)

- Save/Load über alle fünf Pakete hinweg
- Event-Fire mit echten Raids statt Letter-only
- Kartenwechsel, Caravan-Maps, unloaded Storage
- Infizierten-Spawn, der nicht nur ein Brief ist
- Territorium, das tatsächlich die Weltkarte benutzt

## Haftungsausschluss

Dieses Projekt erhebt keinen Anspruch darauf, RimWorld spielbarer zu machen. Es erhebt Anspruch darauf, **verifiziert** zu sein. Unterschiedliche Dinge, zugegeben.

---

*Rimconemy — Mehr Dashboards. Weniger Spaß. Aber mit Regressionstests.*
