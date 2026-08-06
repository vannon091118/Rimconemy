#!/usr/bin/env python3
"""
test_parser_preflight.py — Preflight-Test for parse_runtime_log.py

Validates that ALL patterns defined in parser_config.json are found in the
Player.log. Serves as a gate: when a new function is added or renamed, the
developer only needs to update the config.

Usage:
  python3 scripts/test_parser_preflight.py [--log PATH] [--focused 05] [--json]
  python3 scripts/test_parser_preflight.py --focused 05  # only Mod 05 suites

Exit codes:
  0 = all required patterns found, 0 forbidden hits
  1 = missing pattern(s) or forbidden hit(s)
  2 = config/log file not found
"""

import re, sys, json, os, argparse
from datetime import datetime
from pathlib import Path

SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_CONFIG = SCRIPT_DIR / "parser_config.json"
DEFAULT_LOG = os.path.expanduser(
    "~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log"
)

PACKAGE_NAME_MAP = {
    "01": "Foundation",
    "02": "SurvivalProgression",
    "03": "ScavengerInfrastructure",
    "04": "EconomyTerritory",
    "05": "InfectedAutomation",
}


def load_config(path: str) -> dict:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def read_log(path: str) -> str:
    with open(path, "r", encoding="utf-8", errors="replace") as f:
        return f.read()


def check_patterns(log_text: str, patterns: list, label: str) -> dict:
    """Check that all string patterns appear in the log text. Returns {found, missing, ...}."""
    found = []
    missing = []
    for pat in patterns:
        if re.search(pat, log_text):
            found.append(pat)
        else:
            missing.append(pat)
    return {"label": label, "total": len(patterns), "found": len(found),
            "missing": len(missing), "missing_list": missing}


def check_suite_patterns(log_text: str, suite_entries: list, focused_pkg: str = None) -> dict:
    """Check test suite patterns, optionally filtered by package number."""
    found = []
    missing = []
    for entry in suite_entries:
        pkg = entry.get("package", "??")
        pat = entry.get("pattern", "")
        if focused_pkg and pkg != focused_pkg:
            continue
        if re.search(pat, log_text):
            found.append(pat)
        else:
            missing.append(pat)

    label = "Test-Suites"
    if focused_pkg:
        pkg_name = PACKAGE_NAME_MAP.get(focused_pkg, focused_pkg)
        label = f"Test-Suites (focused: {focused_pkg}-{pkg_name})"

    return {"label": label, "total": len(found) + len(missing),
            "found": len(found), "missing": len(missing), "missing_list": missing}


def check_forbidden(log_text: str, patterns: list) -> dict:
    """Check that NO forbidden patterns appear in the log."""
    hits = []
    for pat in patterns:
        for m in re.finditer(pat, log_text, re.IGNORECASE):
            line_no = log_text[:m.start()].count("\n") + 1
            hits.append({"pattern": pat, "line": line_no, "match": m.group()[:120]})
    return {"label": "Forbidden-Patterns", "total": len(patterns),
            "hits": len(hits), "hit_list": hits}


def main():
    p = argparse.ArgumentParser(description="Preflight test for parse_runtime_log.py")
    p.add_argument("--log", default=DEFAULT_LOG, help="Path to Player.log")
    p.add_argument("--config", default=str(DEFAULT_CONFIG), help="Path to parser_config.json")
    p.add_argument("--focused", default=None, choices=["01", "02", "03", "04", "05"],
                   help="Only check test suites for this package")
    p.add_argument("--json", action="store_true", help="JSON output")
    args = p.parse_args()

    if not os.path.exists(args.log):
        print(f"PREFLIGHT FAIL: Log not found: {args.log}", file=sys.stderr)
        sys.exit(2)
    if not os.path.exists(args.config):
        print(f"PREFLIGHT FAIL: Config not found: {args.config}", file=sys.stderr)
        sys.exit(2)

    config = load_config(args.config)
    log_text = read_log(args.log)

    results = []
    exit_code = 0

    # 1. Bootstrap markers
    bt = config.get("bootstrap", {})
    r = check_patterns(log_text, bt.get("required_markers", []), "Bootstrap-Marker")
    results.append(r)
    if r["missing"] > 0:
        exit_code = 1

    # 2. Runtime markers
    rm = config.get("runtime_markers", {})
    r = check_patterns(log_text, rm.get("required", []), "Runtime-Marker")
    results.append(r)
    if r["missing"] > 0:
        exit_code = 1

    # 3. Test suites (with package filtering for --focused)
    ts = config.get("test_suites", {})
    suite_entries = ts.get("required", [])
    r = check_suite_patterns(log_text, suite_entries, args.focused)
    results.append(r)
    if r["missing"] > 0:
        exit_code = 1

    # 4. Forbidden patterns
    fb = config.get("forbidden", {})
    r = check_forbidden(log_text, fb.get("patterns", []))
    results.append(r)
    if r["hits"] > 0:
        exit_code = 1

    # 5. Cross-refs
    cr = config.get("cross_refs", {})
    max_cr = cr.get("max_allowed", 0)
    cr_count = len(re.findall(r"Could not resolve cross-reference", log_text))
    cr_result = {"label": "Cross-Refs", "count": cr_count, "max_allowed": max_cr,
                 "ok": cr_count <= max_cr}
    results.append(cr_result)
    if not cr_result["ok"]:
        exit_code = 1

    # 6. Package count
    pkgs_match = re.search(r"Registry: (\d+) package", log_text)
    actual_count = int(pkgs_match.group(1)) if pkgs_match else 0
    min_pkgs = bt.get("min_packages", 5)
    pkg_result = {"label": "Package-Count", "actual": actual_count, "min": min_pkgs,
                  "ok": actual_count >= min_pkgs}
    results.append(pkg_result)
    if not pkg_result["ok"]:
        exit_code = 1

    # ── Output ──
    if args.json:
        output = {"timestamp": datetime.now().isoformat(), "log": args.log,
                  "focused": args.focused, "exit_code": exit_code, "results": results}
        print(json.dumps(output, indent=2, default=str))
    else:
        focused_tag = ""
        if args.focused:
            pkg_name = PACKAGE_NAME_MAP.get(args.focused, args.focused)
            focused_tag = f" [FOCUSED: {args.focused}-{pkg_name}]"
        print(f"Parser Preflight{focused_tag}")
        print(f"  Config: {args.config}")
        print(f"  Log:    {args.log}")
        print(f"  Time:   {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
        print()

        all_ok = True
        for r in results:
            label = r.get("label", "?")
            if "hit_list" in r:
                status = "PASS" if r["hits"] == 0 else "FAIL"
                icon = "OK" if r["hits"] == 0 else "!!"
                print(f"  [{icon}] {label}: {r['hits']}/{r['total']} hits")
                if r["hits"] > 0:
                    all_ok = False
                    for h in r["hit_list"]:
                        print(f"       L{h['line']}: {h['pattern']}")
            elif "ok" in r:
                status = "PASS" if r["ok"] else "FAIL"
                icon = "OK" if r["ok"] else "!!"
                print(f"  [{icon}] {label}: {r.get('count', r.get('actual', '?'))} "
                      f"(limit={r.get('max_allowed', r.get('min', '?'))})")
                if not r["ok"]:
                    all_ok = False
            else:
                status = "PASS" if r["missing"] == 0 else "FAIL"
                icon = "OK" if r["missing"] == 0 else "!!"
                print(f"  [{icon}] {label}: {r['found']}/{r['total']} found")
                if r["missing"] > 0:
                    all_ok = False
                    for m in r["missing_list"][:10]:
                        print(f"       MISSING: {m[:120]}")

        print()
        if all_ok:
            print("PREFLIGHT PASS — all required patterns found, 0 forbidden hits")
        else:
            print("PREFLIGHT FAIL — missing patterns or forbidden hits (see above)")

    sys.exit(exit_code)


if __name__ == "__main__":
    main()
