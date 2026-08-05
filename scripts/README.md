# scripts/ — Rimconemy Operational Harness

> **Owner:** Foundation · **Zweck:** automatisierter Build-, Deploy- und Runtime-Gate · **Modus:** Evidence-Bound (`Player.log` / `verify_bootstrap_log.sh`).

## Player.log-Rotation (LIVE vs. PREV)

Unity/RimWorld/Linux rotiert den Output direkt vor jedem Boot:

| Datei | Rolle | Trigger |
|---|---|---|
| `Player.log` | **LIVE** — der aktuell laufende oder gerade beendete Boot | RimWorld öffnet beim Start, schließt beim Beenden |
| `Player-prev.log` | der **vorige** Run | Unity verschiebt `Player.log` → `Player-prev.log` direkt vor jedem neuen Boot, wenn `Player.log` schon existiert |

Standard-Pfad: `$HOME/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`. Override: `--log <path>` an `runtime_test.sh` bzw. `verify_bootstrap_log.sh`. Die beiden Logs sind **nicht identisch groß**; wer die Rotation nicht versteht, wertet nach `--strict` den vorigen Boot erneut als grün — klassischer Stale-Log-Blindspot.

## Pinning-Gate (geplant)

`runtime_test.sh` prepend-ed einen `stat`-Sanity-Check vor `verify_bootstrap_log.sh`:

- `mtime(Player.log)` muss neuer sein als der `deploy.sh`-Trigger-Zeitstempel; sonst fail-fast.
- Verhindert, dass ein zweiter `runtime_test.sh`-Run ohne neuen Boot den alten Log-Accept.

## verify_bootstrap_log.sh-Pinning

`verify_bootstrap_log.sh` zielt **ausschließlich auf `Player.log`** — niemals `Player-prev.log`. Die ProfileDetector-Invariants `I1`, `I2`, `I3` ([`scripts/verify_bootstrap_log.sh:1-30`](verify_bootstrap_log.sh)) werden am LIVE-Log gemessen; jede Annahme über „vorigen Run" wäre Drift und ist als Failure zu melden.

## Parent-Doku

[`docs/CANONICAL_VANILLA_DOMAIN_MAP.md`](../docs/CANONICAL_VANILLA_DOMAIN_MAP.md) hält den Vanilla↔Rimconemy-SSOT. Diese README-Datei ist die Pipeline-Schicht darüber. Wird ein Rimconemy-Patch-Gate rot-grün gewertet, ist hier der Vertrag, welche Datei gemessen wird.
