#!/usr/bin/env python3
"""
parse_runtime_log.py — Structured Rimconemy Player.log Parser

Extrahiert aus dem RimWorld Player.log eine übersichtliche Debug-Übersicht:
  - Bootstrap-Status (Profile, Pakete)
  - Test-Ergebnisse pro Suite (PASS/FAIL mit Details)
  - Errors & Warnings (dedupliziert)
  - Cross-Reference-Fehler (vanilla + Rimconemy)
  - Harmony/Patch-Status
  - Nicht-Rimconemy Fehler (NullReference, XML error, etc.)
  - Completeness check against parser_config.json (SSOT)

Usage:
  python3 scripts/parse_runtime_log.py [--log PATH] [--out PATH] [--json]
  python3 scripts/parse_runtime_log.py --focused 05

Output:
  --print:  Markdown-Tabelle nach stdout (default)
  --json:   JSON nach stdout
  --focused: Nur Suiten eines Pakets prüfen (01-05)
  --out:    Datei schreiben (default: docs/runtime-parsed/<timestamp>.md)
"""

import re, sys, json, os
from datetime import datetime
from pathlib import Path

# ── Konfiguration ──────────────────────────────────────────────
SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_CONFIG = SCRIPT_DIR / "parser_config.json"
DEFAULT_LOG = os.path.expanduser(
    "~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log"
)
OUT_DIR = "docs/runtime-parsed"

# ── Pattern-Definitionen ──────────────────────────────────────
RX_RIMCONEMY = re.compile(r"^\[Rimconemy\.([^\]]+)\] (.+)$")
RX_TEST_SUMMARY = re.compile(
    r"(?P<suite>[A-Za-z0-9_\- )(]+?) tests?(?: \([^)]+\))?: (?P<passed>\d+)(?:/(?P<total2>\d+))? passed, (?P<failed>\d+) failed"
)
RX_TEST_FAILED = re.compile(r"test FAILED:?\s*(.+)", re.IGNORECASE)
RX_BOOTSTRAP = re.compile(r"bootstrap (START|COMPLETE)")
RX_PROFILE = re.compile(r"Profile detected: (\w+)")
RX_PACKAGE = re.compile(r"Package registered: (rimconemy\.\w+) v([\d.]+)")
RX_CROSSREF = re.compile(r"Could not resolve cross-reference to (\S+) named (\S+)")
RX_NULLREF = re.compile(r"NullReferenceException", re.IGNORECASE)
RX_XML_ERROR = re.compile(r"XML error:", re.IGNORECASE)
RX_EXCEPTION_RAW = re.compile(r"(Exception|FATAL|CRASH)", re.IGNORECASE)


def parse_player_log(path: str) -> dict:
    """Parse a Player.log file and return a structured dictionary."""
    try:
        with open(path, "r", encoding="utf-8", errors="replace") as f:
            lines = f.readlines()
    except FileNotFoundError:
        return {"error": f"Logfile not found: {path}"}

    result = {
        "source": path,
        "total_lines": len(lines),
        "rimconemy_lines": 0,
        "bootstrap": {"start": False, "complete": False, "packages": [], "profiles": []},
        "test_suites": [],
        "errors": [],
        "warnings": [],
        "cross_refs": [],
        "harmony_status": [],
        "vanilla_errors": [],
        "packages": {},
        "_raw_log": "".join(lines),  # cached for completeness check
    }

    pending_failures = []
    last_suite = None

    for lineno, raw in enumerate(lines, 1):
        line = raw.strip()

        # ── Vanilla / Non-Rimconemy errors ──
        if RX_NULLREF.search(line):
            result["vanilla_errors"].append({"line": lineno, "type": "NullReferenceException", "message": line[:200]})
        if RX_XML_ERROR.search(line):
            result["vanilla_errors"].append({"line": lineno, "type": "XML error", "message": line[:200]})
        if not line.startswith("[Rimconemy.") and RX_EXCEPTION_RAW.search(line):
            if "UnityEngine" not in line and "Fallback handler" not in line:
                result["vanilla_errors"].append({"line": lineno, "type": "Exception", "message": line[:200]})

        # ── Cross-reference issues (any line) ──
        crm = RX_CROSSREF.search(line)
        if crm:
            result["cross_refs"].append({
                "line": lineno, "type": crm.group(1), "name": crm.group(2),
            })

        # ── Rimconemy lines ──
        m = RX_RIMCONEMY.match(line)
        if not m:
            continue
        result["rimconemy_lines"] += 1
        pkg = m.group(1)
        msg = m.group(2)

        # Bootstrap markers
        if RX_BOOTSTRAP.search(msg):
            if "START" in msg:
                result["bootstrap"]["start"] = True
            if "COMPLETE" in msg:
                result["bootstrap"]["complete"] = True

        # Profile detection
        pm = RX_PROFILE.search(msg)
        if pm:
            result["bootstrap"]["profiles"].append(pm.group(1))

        # Package registration
        pkm = RX_PACKAGE.search(msg)
        if pkm:
            pid, ver = pkm.group(1), pkm.group(2)
            result["bootstrap"]["packages"].append({"id": pid, "version": ver})
            result["packages"][pid] = ver

        # Individual test failures (buffer until suite summary)
        tfm = RX_TEST_FAILED.search(msg)
        if tfm:
            pending_failures.append(tfm.group(1).strip())

        # Test summaries — flush pending failures
        tsm = RX_TEST_SUMMARY.search(msg)
        if tsm:
            suite = {
                "package": pkg,
                "suite": tsm.group("suite").strip(),
                "passed": int(tsm.group("passed")),
                "failed": int(tsm.group("failed")),
                "failures": list(pending_failures),
            }
            pending_failures.clear()
            result["test_suites"].append(suite)
            last_suite = suite

        # Errors & Warnings
        if "Error" in msg or "Exception" in msg or "FATAL" in msg:
            result["errors"].append({"line": lineno, "package": pkg, "message": msg})
        elif "Warning" in msg:
            result["warnings"].append({"line": lineno, "package": pkg, "message": msg})

        # Harmony status
        if "Harmony" in msg or "PatchAll" in msg:
            result["harmony_status"].append({"line": lineno, "message": msg})

    # Leftover failures → last suite
    if pending_failures and last_suite:
        last_suite["failures"].extend(pending_failures)

    return result


def format_markdown(result: dict) -> str:
    if "error" in result:
        return f"# ❌ Parser Error\n\n{result['error']}"

    b = result["bootstrap"]
    pkgs = result["packages"]
    suites = result["test_suites"]
    total_p = sum(s["passed"] for s in suites)
    total_f = sum(s["failed"] for s in suites)

    out = []
    out.append("# 🔍 Rimconemy Runtime Debug Summary")
    out.append(f"**Source:** `{os.path.basename(result['source'])}`")
    out.append(f"**Parsed:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    out.append(f"**Stats:** {result['total_lines']} lines · {result['rimconemy_lines']} Rimconemy · {len(suites)} suites · {total_p}✓/{total_f}✗")
    out.append("")

    # ── Quick Summary ──
    out.append("## ⚡ Quick Summary")
    status = "✅ ALL CLEAN" if total_f == 0 and len(result["errors"]) == 0 else "❌ ISSUES FOUND"
    out.append(f"**{status}**")
    out.append(f"- Bootstrap: {'✅' if b['start'] and b['complete'] else '❌'} | {len(pkgs)} packages | {len(b['profiles'])} profiles")
    out.append(f"- Tests: {total_p} passed, {total_f} failed across {len(suites)} suites")
    out.append(f"- Errors: {len(result['errors'])} | Warnings: {len(result['warnings'])} | Cross-refs: {len(result['cross_refs'])}")
    out.append(f"- Vanilla errors: {len(result['vanilla_errors'])} | Harmony issues: {len(result['harmony_status'])}")
    out.append("")

    # ── Bootstrap ──
    out.append("## 🚀 Bootstrap")
    out.append("| Item | Status |")
    out.append("|---|---|")
    out.append(f"| START marker | {'✅' if b['start'] else '❌ MISSING'} |")
    out.append(f"| COMPLETE marker | {'✅' if b['complete'] else '❌ MISSING'} |")
    out.append(f"| Packages registered | {len(pkgs)} |")
    out.append(f"| Profiles detected | {len(b['profiles'])} |")
    out.append("")

    if pkgs:
        out.append("### 📦 Packages")
        out.append("| # | Package | Version |")
        out.append("|---|---|---|")
        for i, (pid, ver) in enumerate(sorted(pkgs.items()), 1):
            out.append(f"| {i} | `{pid}` | {ver} |")
        out.append("")

    if b["profiles"]:
        out.append("### 🎭 Profiles")
        for p in b["profiles"]:
            out.append(f"- `{p}`")
        out.append("")

    # ── Test Results ──
    out.append("## 🧪 Test Results")
    out.append(f"**Total:** {total_p} passed, {total_f} failed")
    out.append("")
    out.append("| Package | Suite | ✓ | ✗ | Status |")
    out.append("|---|---|---|---|---|")
    for s in suites:
        status = "✅" if s["failed"] == 0 else "❌"
        out.append(f"| {s['package']} | {s['suite']} | {s['passed']} | {s['failed']} | {status} |")
    out.append("")

    # ── Failed Tests Detail ──
    failed = [s for s in suites if s["failed"] > 0]
    if failed:
        out.append("### ❌ Failed Tests — Details")
        for s in failed:
            out.append(f"**{s['package']} › {s['suite']}** ({s['passed']}✓ / {s['failed']}✗)")
            if s["failures"]:
                for f in s["failures"]:
                    out.append(f"  - `{f}`")
            else:
                out.append("  - _(no failure details captured)_")
            out.append("")
    else:
        out.append("### ✅ All Tests Passed")
        out.append("")

    # ── Errors ──
    if result["errors"]:
        out.append("## 🔴 Rimconemy Errors")
        for e in result["errors"]:
            out.append(f"- L{e['line']} `[{e['package']}]` {e['message'][:250]}")
        out.append("")

    # ── Warnings ──
    if result["warnings"]:
        out.append("## 🟡 Rimconemy Warnings")
        for w in result["warnings"]:
            out.append(f"- L{w['line']} `[{w['package']}]` {w['message'][:250]}")
        out.append("")

    # ── Cross-References ──
    if result["cross_refs"]:
        out.append("## 🔗 Cross-Reference Issues")
        out.append("| Line | Type | Missing Def |")
        out.append("|---|---|---|")
        for cr in result["cross_refs"]:
            out.append(f"| {cr['line']} | `{cr['type']}` | `{cr['name']}` |")
        out.append("")

    # ── Vanilla Errors ──
    if result["vanilla_errors"]:
        out.append("## ⚠️ Vanilla / Non-Rimconemy Errors")
        for ve in result["vanilla_errors"]:
            out.append(f"- L{ve['line']} `{ve['type']}` — {ve['message'][:200]}")
        out.append("")

    # ── Harmony ──
    if result["harmony_status"]:
        out.append("## 🎵 Harmony / Patch Status")
        for h in result["harmony_status"]:
            out.append(f"- L{h['line']} {h['message'][:250]}")
        out.append("")

    return "\n".join(out)



def check_conflicts(parsed: dict, log_text: str) -> dict:
    """Check for contradictions within the runtime evidence (K1-K6).
    Returns dict with: conflicts (list), ok (bool).
    These are the only hard FAILs — they detect internal contradictions,
    never prescribed expectations."""
    conflicts = []

    # K1: "PatchAll failed" + "Bootstrap complete" → FAIL
    #   Exclude known non-critical SurvivalProgression BioRemap PostOpen patch
    #   ("Non-critical; game continues.")
    patch_lines = [l for l in log_text.split('\n') if re.search(r'PatchAll.*failed|Harmony.*patching exception', l, re.IGNORECASE)]
    critical_patch_fails = [l for l in patch_lines if 'Non-critical; game continues' not in l]
    has_bootstrap_complete = parsed.get("bootstrap", {}).get("complete", False)
    if critical_patch_fails and has_bootstrap_complete:
        conflicts.append({"id": "K1", "msg": f"Critical patch-failure ({len(critical_patch_fails)} line(s)) + Bootstrap complete — contradiction"})

    # K2: "0 failed" in Summary + TEST-FAIL lines exist → FAIL
    test_fail_lines = [l for l in log_text.split('\n') if 'TEST-FAIL' in l and '[Rimconemy.' in l]
    suites_with_zero_failed = [s for s in parsed.get("test_suites", []) if s["failed"] == 0]
    if test_fail_lines and suites_with_zero_failed:
        conflicts.append({"id": "K2", "msg": f"TEST-FAIL lines ({len(test_fail_lines)}) exist but summaries claim 0 failed"})

    # K3: Duplicate suite summaries (same name, different packages)
    seen = {}
    for s in parsed.get("test_suites", []):
        name = s["suite"]
        if name in seen:
            conflicts.append({"id": "K3", "msg": f"Duplicate suite: '{name}' in {seen[name]} and {s['package']}"})
        seen[name] = s["package"]

    # K5: Suite not complete (no summary, no TEST-DEFERRED)
    has_deferred = 'TEST-DEFERRED' in log_text
    if not parsed.get("test_suites") and not has_deferred and parsed.get("rimconemy_lines", 0) > 0:
        conflicts.append({"id": "K5", "msg": "Rimconemy lines present but no suite summaries or TEST-DEFERRED found"})

    # K4: Dedup-Bruch — more than one "Profile detected:" line with identical content
    #   (delegates to verify_bootstrap_log.sh I3 for full-string dedup check)
    profile_lines = [l for l in log_text.split('\n') if 'Profile detected:' in l and '[Rimconemy.Foundation]' in l]
    if len(profile_lines) > 1:
        # Check for exact duplicates (I3 violation)
        unique = set(profile_lines)
        if len(unique) < len(profile_lines):
            conflicts.append({"id": "K4", "msg": f"Dedup-Bruch: {len(profile_lines)} Profile-detected lines, only {len(unique)} unique — I3 invariant violated"})

    # K6: TEST-FAIL before any suite summary
    first_summary_idx = -1
    for i, line in enumerate(log_text.split('\n')):
        if 'tests:' in line and 'passed' in line and 'failed' in line and '[Rimconemy.' in line:
            first_summary_idx = i
            break
    for i, line in enumerate(log_text.split('\n')):
        if 'TEST-FAIL' in line and '[Rimconemy.' in line:
            if first_summary_idx == -1 or i < first_summary_idx:
                conflicts.append({"id": "K6", "msg": f"TEST-FAIL before first suite summary at line {i+1}"})
                break

    return {"conflicts": conflicts, "ok": len(conflicts) == 0}


def format_conflicts_markdown(conflicts_result: dict) -> str:
    """Format conflict check results as markdown."""
    out = []
    out.append("## ⚠️ Conflict Checks (K1-K6)")
    if conflicts_result["ok"]:
        out.append("✅ No internal contradictions detected.")
    else:
        out.append("❌ Contradictions found in runtime evidence:")
        for c in conflicts_result["conflicts"]:
            out.append(f"- **{c['id']}**: {c['msg']}")
    out.append("")
    return "\n".join(out)

def format_json(result: dict) -> str:
    return json.dumps(result, indent=2, default=str)


def load_config(path: str) -> dict:
    """Load parser_config.json as SSOT."""
    try:
        with open(path, "r", encoding="utf-8") as f:
            return json.load(f)
    except (FileNotFoundError, json.JSONDecodeError) as e:
        return {"_error": str(e)}


def check_completeness(parsed: dict, config: dict, focused_pkg: str = None) -> dict:
    """Cross-check parsed log against parser_config.json SSOT.
    Directly greps the raw log text for each config pattern — same
    approach as runtime_test.sh, ensuring consistent results.
    Returns dict with: missing_suites, config_count, parsed_count, ok.
    """
    if "_error" in config:
        return {"missing_suites": [], "config_count": 0, "parsed_count": 0,
                "ok": False, "error": config["_error"]}

    suite_entries = config.get("test_suites", {}).get("required", [])
    parsed_suites = parsed.get("test_suites", [])

    # Use cached raw log text from parser (avoids double I/O)
    log_text = parsed.get("_raw_log", "")

    missing = []
    for entry in suite_entries:
        pkg = entry.get("package", "??")
        if focused_pkg and pkg != focused_pkg:
            continue
        pattern = entry.get("pattern", "")
        # Convert \\d+ to [0-9]+ for grep-compatible matching (same as runtime_test.sh)
        grep_pattern = pattern.replace("\\d+", "[0-9]+")
        if not re.search(grep_pattern, log_text):
            missing.append({"package": pkg, "pattern": pattern})

    config_count = len([e for e in suite_entries
                        if not focused_pkg or e.get("package") == focused_pkg])

    return {
        "missing_suites": missing,
        "config_count": config_count,
        "parsed_count": len(parsed_suites),
        "ok": len(missing) == 0,
    }


def format_completeness_markdown(completeness: dict) -> str:
    """Format the completeness check as markdown table."""
    out = []
    out.append("## 📋 Completeness vs SSOT (parser_config.json)")

    if "error" in completeness:
        out.append(f"⚠️ Config load error: {completeness['error']}")
        out.append("")
        return "\n".join(out)

    config_count = completeness["config_count"]
    parsed_count = completeness["parsed_count"]
    missing = completeness["missing_suites"]

    icon = "✅" if completeness["ok"] else "❌"
    out.append(f"**{icon} Completeness:** {parsed_count}/{config_count} suites found in log")

    if missing:
        out.append("")
        out.append("### ❌ Missing Suites (in config but not in log)")
        out.append("| Package | Pattern |")
        out.append("|---|---|")
        for m in missing:
            out.append(f"| {m['package']} | `{m['pattern'][:100]}` |")
    else:
        out.append("")
        out.append("✅ All configured suites present in log.")

    out.append("")
    return "\n".join(out)


def main():
    import argparse
    p = argparse.ArgumentParser(description="Rimconemy Player.log Parser")
    p.add_argument("--log", default=DEFAULT_LOG, help="Path to Player.log")
    p.add_argument("--config", default=str(DEFAULT_CONFIG), help="Path to parser_config.json")
    p.add_argument("--out", default=None, help="Output file")
    p.add_argument("--json", action="store_true", help="Output as JSON")
    p.add_argument("--print", action="store_true", help="Print to stdout")
    p.add_argument("--focused", default=None, choices=["01","02","03","04","05"],
                   help="Only check suites for this package")
    args = p.parse_args()

    result = parse_player_log(args.log)
    config = load_config(args.config)
    completeness = check_completeness(result, config, args.focused)
    result["completeness"] = completeness
    conflicts = check_conflicts(result, result.get("_raw_log", ""))
    result["conflicts"] = conflicts

    if args.json:
        output = format_json(result)
    else:
        md = format_markdown(result)
        cm = format_completeness_markdown(completeness)
        kx = format_conflicts_markdown(conflicts)
        output = md + "\n" + cm + "\n" + kx

    if args.out:
        outpath = args.out
    else:
        ts = datetime.now().strftime("%Y%m%d-%H%M%S")
        os.makedirs(OUT_DIR, exist_ok=True)
        outpath = os.path.join(OUT_DIR, f"parsed-{ts}.md")

    with open(outpath, "w") as f:
        f.write(output)

    if args.print or not args.out:
        print(output)

    print(f"\n📄 Saved to: {outpath}", file=sys.stderr)


if __name__ == "__main__":
    main()
