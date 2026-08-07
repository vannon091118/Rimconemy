#!/usr/bin/env bash
# audit_summaries.sh — strict C# ↔ parser_config.json maintenance audit.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
MODE="${1:-}"
exec python3 - "$ROOT" "$SCRIPT_DIR/parser_config.json" "$MODE" <<'PY'
import json, re, sys
from pathlib import Path

root, config_path, mode = map(Path, sys.argv[1:])
strict = mode.name == "--strict"
config = json.loads(config_path.read_text(encoding="utf-8"))
entries = config.get("test_suites", {}).get("required", [])
files = [p for p in root.glob("mods/*/Tests/*.cs") if p.name not in {"TestSuite.cs", "ScribeRoundTripHelper.cs"}]
findings = []
passes = []

def finding(msg):
    findings.append(msg)
    print(f"  FINDING: {msg}")

def passing(msg):
    passes.append(msg)
    print(f"  PASS: {msg}")

def note(msg):
    print(f"  NOTE: {msg}")

def label_from_pattern(pattern):
    label = pattern.split(":", 1)[0]
    return re.sub(r"\\([()])", r"\1", label)

print("=== audit_summaries.sh ===")
print(f"  config entries: {len(entries)}")
print(f"  test files: {len(files)}")
texts = {p: p.read_text(errors="replace") for p in files}
users = [p for p, text in texts.items() if "new TestSuite(" in text]
print(f"  TestSuite users: {len(users)}")

# 1. Every regression file must use the shared harness.
missing_harness = [str(p) for p, text in texts.items() if "new TestSuite(" not in text]
if missing_harness:
    for p in missing_harness: finding(f"MIGRATION: {p}")
else:
    passing("all test files use TestSuite")

# 2. Every configured summary family must have a source label or source file.
for entry in entries:
    label = label_from_pattern(entry.get("pattern", ""))
    if not any(label.lower() in text.lower() or label.lower() in p.stem.lower() for p, text in texts.items()):
        finding(f"CONFIG-ORPHAN: package={entry.get('package')} label={label}")

# 3. Failure diagnostics may not be emitted as warnings/messages. This is a
# hard finding because it makes a real failure easy to miss in Player.log.
for p, text in texts.items():
    for line_no, line in enumerate(text.splitlines(), 1):
        if re.search(r"Log\.(?:Warning|Message)\s*\(.*(?:FAIL|FAILED|test FAILED)", line):
            finding(f"SWALLOW/ENVELOPE: {p}:{line_no}: {line.strip()}")

# 4. The shared harness is authoritative for the new contract. Legacy summary
# calls are retained deliberately during migration for backward-compatible
# parser evidence; report them as notes, never as a false green/fail finding.
for p, text in texts.items():
    if re.search(r"Log\.(?:Message|Error)\s*\(\s*summary\s*\)", text):
        note(f"legacy summary retained for compatibility: {p}")

# 5. Every harness user has an observable check and a positive literal or
# named minimum. Existing suites may use constants such as MinExpected.
min_pattern = r"ts\.RunSummary\(\s*(?:[1-9][0-9]*|[A-Za-z_][A-Za-z0-9_]*)\s*\)"
for p, text in texts.items():
    if "new TestSuite(" not in text: continue
    if not re.search(min_pattern, text):
        finding(f"NO-CANONICAL-SUMMARY: {p}")
    if not re.search(r"ts\.Check\s*\(", text):
        finding(f"NO-HARNESS-CHECK: {p}")

print(f"Audit complete: findings={len(findings)} passes={len(passes)}")
if findings and strict:
    raise SystemExit(1)
PY
