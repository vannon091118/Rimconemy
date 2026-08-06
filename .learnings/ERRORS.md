# ERRORS

## ERR-20260806-001 — pip install skill-seekers blockiert durch PEP 668
- **Timestamp**: 2026-08-06T03:05
- **Priority**: P2
- **Status**: resolved
- **Area**: tool
- **Description**: `pip install skill-seekers` schlug fehl mit "externally-managed-environment" (Python 3.14 + Debian PEP 668).
- **Remediation**: `uv tool install skill-seekers` funktioniert, installiert 25 Executables in `~/.local/bin/`. Alternative: `pip install --break-system-packages`.
- **Related**: LRN-20260806-002

## ERR-20260806-002 — Skill_Seekers Claude-Code-Enhancement schlägt silent fehl
- **Timestamp**: 2026-08-06T03:05
- **Priority**: P3
- **Status**: wontfix
- **Area**: tool
- **Description**: 10× `Claude Code returned error code 1` während Dry-run. Output wird trotzdem erzeugt, aber Enhancement-Sections bleiben leer.
- **Remediation**: Für reine Codebase-Analyse reicht der Basis-Mode. AI-Enhancement braucht konfigurierten Agent.
