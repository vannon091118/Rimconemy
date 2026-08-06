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

Usage:
  python3 scripts/parse_runtime_log.py [--log PATH] [--out PATH] [--json]

Output:
  --print:  Markdown-Tabelle nach stdout (default)
  --json:   JSON nach stdout
  --out:    Datei schreiben (default: docs/runtime-parsed/<timestamp>.md)
"""

import re, sys, json, os
from datetime import datetime

# ── Konfiguration ──────────────────────────────────────────────
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


def format_json(result: dict) -> str:
    return json.dumps(result, indent=2, default=str)


def main():
    import argparse
    p = argparse.ArgumentParser(description="Rimconemy Player.log Parser")
    p.add_argument("--log", default=DEFAULT_LOG, help="Path to Player.log")
    p.add_argument("--out", default=None, help="Output file")
    p.add_argument("--json", action="store_true", help="Output as JSON")
    p.add_argument("--print", action="store_true", help="Print to stdout")
    args = p.parse_args()

    result = parse_player_log(args.log)

    output = format_json(result) if args.json else format_markdown(result)

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
