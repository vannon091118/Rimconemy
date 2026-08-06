# .learnings/ — Agent Self-Improvement Log

Project-local log of agent observations, errors, corrections, and feature requests.
Format modeled on `openclaw-skills-self-improving-agent-1-0-0` (LobeHub marketplace).

## File Layout

| File | Purpose |
|------|---------|
| `LEARNINGS.md` | LRN-YYYYMMDD-XXX — neue Erkenntnisse, bessere Ansätze |
| `ERRORS.md` | ERR-YYYYMMDD-XXX — aufgetretene Fehler + Workarounds |
| `FEATURE_REQUESTS.md` | FR-YYYYMMDD-XXX — fehlende Capabilities |
| `.gitkeep` | hält das Verzeichnis im Git |

## ID-Schema

```
LRN-20260806-001   (learning)
ERR-20260806-001   (error)
FR-20260806-001    (feature request)
```

## Pflichtfelder pro Eintrag

- `id`
- `timestamp` (ISO 8601)
- `priority` (P0/P1/P2/P3)
- `status` (open / in_progress / resolved / wontfix)
- `area` (build / test / deploy / doc / ux / tool)
- `description`
- `remediation` (bei ERR)
- `promoted_to` (z.B. AGENTS.md, wenn eine Erkenntnis global relevant wurde)

## Promotion-Workflow

Wird ein LRN/ERR in einem Skill oder in AGENTS.md aufgenommen, hier den
`promoted_to`-Pfad eintragen, damit Doppel-Tracking vermieden wird.
