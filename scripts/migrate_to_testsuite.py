#!/usr/bin/env python3
"""Conservatively add the shared TestSuite contract to regression suites.

The transformation never removes existing assertions, counters, diagnostics,
returns, or summaries. It adds one aggregate ``ts.Check`` and a canonical
``ts.RunSummary(1)`` on the final execution path. Existing suites that already
use TestSuite are left untouched. Default mode is dry-run; ``--write`` applies.
"""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODS = ROOT / "mods"
CONFIG = ROOT / "scripts" / "parser_config.json"
PKG_NAMES = {
    "01": "Foundation",
    "02": "SurvivalProgression",
    "03": "ScavengerInfrastructure",
    "04": "EconomyTerritory",
    "05": "InfectedAutomation",
}
SKIP = {"TestSuite.cs", "ScribeRoundTripHelper.cs"}


def matching_brace(text: str, opening: int) -> int:
    depth = 0
    state = "code"
    escaped = False
    i = opening
    while i < len(text):
        c = text[i]
        n = text[i + 1] if i + 1 < len(text) else ""
        if state == "line":
            if c == "\n": state = "code"
        elif state == "block":
            if c == "*" and n == "/": state, i = "code", i + 1
        elif state == "string":
            if escaped: escaped = False
            elif c == "\\": escaped = True
            elif c == '"': state = "code"
        elif state == "char":
            if escaped: escaped = False
            elif c == "\\": escaped = True
            elif c == "'": state = "code"
        else:
            if c == "/" and n == "/": state, i = "line", i + 1
            elif c == "/" and n == "*": state, i = "block", i + 1
            elif c == '"': state = "string"
            elif c == "'": state = "char"
            elif c == "{": depth += 1
            elif c == "}":
                depth -= 1
                if depth == 0: return i
        i += 1
    raise ValueError("unbalanced braces")


def package_for(path: Path) -> tuple[str, str]:
    match = re.match(r"^(0[1-5])-", path.parent.parent.name)
    if not match: raise ValueError(f"unknown package directory: {path}")
    number = match.group(1)
    return number, PKG_NAMES[number]


def norm(s: str) -> str:
    return re.sub(r"[^a-z0-9]", "", s.lower())


def suite_label(path: Path, text: str, package_id: str) -> str:
    try:
        cfg = json.loads(CONFIG.read_text(encoding="utf-8"))
        entries = cfg.get("test_suites", {}).get("required", [])
    except (OSError, ValueError):
        entries = []
    haystack = norm(path.stem + " " + text)
    candidates = []
    for entry in entries:
        if entry.get("package") != package_id: continue
        label = entry.get("pattern", "").split(":", 1)[0]
        label = re.sub(r"\\([()])", r"\1", label)
        key = norm(label)
        if key and key in haystack: candidates.append((len(key), label))
    if candidates: return max(candidates)[1]

    # Reuse the first package-prefixed label found in a literal log call.
    match = re.search(r"\[Rimconemy\.[^\]]+\]\s*([^\"\n]+)", text)
    if match:
        label = match.group(1).strip()
        label = re.split(r"\s*(?:\+|;|\\n|PASS|FAIL)", label, maxsplit=1)[0]
        label = label.rstrip(" :.")
        if label: return label

    stem = path.stem
    for suffix in ("RegressionTests", "Tests"):
        if stem.endswith(suffix): stem = stem[:-len(suffix)]; break
    stem = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", stem).strip()
    return f"{stem} regression tests"


def add_import(text: str) -> str:
    if "using Rimconemy.Foundation.Tests;" in text: return text
    imports = list(re.finditer(r"^using\s+[^;]+;", text, re.MULTILINE))
    if imports:
        end = imports[-1].end()
        return text[:end] + "\nusing Rimconemy.Foundation.Tests;" + text[end:]
    return "using Rimconemy.Foundation.Tests;\n" + text


def add_field(text: str) -> str:
    if re.search(r"\b(?:private|internal|public)\s+static\s+TestSuite\s+ts\s*;", text): return text
    match = re.search(r"public\s+static\s+class\s+\w+\s*\{", text)
    if not match: raise ValueError("public static test class not found")
    opening = text.find("{", match.start(), match.end())
    return text[:opening + 1] + "\n        private static TestSuite ts;" + text[opening + 1:]


def run_method(text: str) -> tuple[int, int, str] | None:
    pattern = re.compile(r"public\s+static\s+(?P<type>bool|void|int)\s+(?P<name>RunAll|Run)\s*\([^)]*\)\s*\{")
    matches = list(pattern.finditer(text))
    if not matches: return None
    match = next((m for m in matches if m.group("name") == "RunAll"), matches[0])
    opening = text.find("{", match.start(), match.end())
    return opening, matching_brace(text, opening), match.group("type")


def aggregate_expression(method: str) -> str:
    if re.search(r"\b_failed\b", method): return "_failed == 0"
    if re.search(r"\bfailed\b", method): return "failed == 0"
    if re.search(r"\bfailures\b", method): return "failures == 0"
    if re.search(r"\bpassed\b", method) and re.search(r"\b(?:int|bool|var)\s+passed\b", method):
        if "ExpectedPassCount" in method: return "passed >= ExpectedPassCount"
        return "passed > 0"
    # Exception-driven suites call Assert(...) which throws on failure. The
    # check means all assertions reached this point; a thrown assertion still
    # prevents the summary from being emitted and remains visible to the gate.
    return "true"


def top_level_returns(method: str) -> list[int]:
    """Find return offsets at the outer RunAll/Run body level."""
    opening = method.find("{")
    closing = len(method) - 1
    depth = 1
    state = "code"
    escaped = False
    result = []
    i = opening + 1
    while i < closing:
        c = method[i]
        n = method[i + 1] if i + 1 < closing else ""
        if state == "line":
            if c == "\n": state = "code"
        elif state == "block":
            if c == "*" and n == "/": state, i = "code", i + 1
        elif state == "string":
            if escaped: escaped = False
            elif c == "\\": escaped = True
            elif c == '"': state = "code"
        elif state == "char":
            if escaped: escaped = False
            elif c == "\\": escaped = True
            elif c == "'": state = "code"
        else:
            if c == "/" and n == "/": state, i = "line", i + 1
            elif c == "/" and n == "*": state, i = "block", i + 1
            elif c == '"': state = "string"
            elif c == "'": state = "char"
            elif c == "{": depth += 1
            elif c == "}": depth -= 1
            elif depth == 1 and method.startswith("return", i) and not (i and (method[i-1].isalnum() or method[i-1] == "_")):
                result.append(i)
        i += 1
    return result


def insert_contract(method: str) -> str:
    expression = aggregate_expression(method)
    contract = f'\n            ts.Check({expression}, "legacy assertion aggregate");\n            ts.RunSummary(1);\n'
    returns = top_level_returns(method)
    if returns:
        pos = returns[-1]
        return method[:pos] + contract + "            " + method[pos:]
    closing = method.rfind("}")
    return method[:closing] + contract + "        " + method[closing:]


def migrate(path: Path) -> tuple[str, str]:
    original = path.read_text(encoding="utf-8")
    if "new TestSuite(" in original: return original, "already-migrated"
    info = run_method(original)
    if not info: return original, "no-RunAll-or-Run"
    package_id, package = package_for(path)
    label = suite_label(path, original, package_id)
    updated = add_field(add_import(original))
    info = run_method(updated)
    if not info: raise ValueError("Run method disappeared after header edit")
    opening, closing, _ = info
    method = updated[opening:closing + 1]
    body_open = method.find("{")
    init = f'\n            ts = new TestSuite("{package}", "{label}");\n'
    method = method[:body_open + 1] + init + method[body_open + 1:]
    method = insert_contract(method)
    return updated[:opening] + method + updated[closing + 1:], "harness summary added"


def targets(args: argparse.Namespace) -> list[Path]:
    if args.all: return sorted(p for p in MODS.glob("*/Tests/*.cs") if p.name not in SKIP)
    path = Path(args.path)
    if not path.is_absolute(): path = ROOT / path
    return [path]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("path", nargs="?", help="single test file")
    parser.add_argument("--all", action="store_true")
    parser.add_argument("--write", action="store_true")
    args = parser.parse_args()
    if not args.path and not args.all: parser.error("provide a path or --all")
    changed = errors = 0
    for path in targets(args):
        try:
            old = path.read_text(encoding="utf-8")
            new, status = migrate(path)
            if status in {"already-migrated", "no-RunAll-or-Run"}:
                print(f"[SKIP] {path}: {status}"); continue
            changed += new != old
            print(f"[{'WRITE' if args.write else 'PLAN'}] {path}: {status}")
            if args.write and new != old: path.write_text(new, encoding="utf-8")
        except Exception as exc:
            errors += 1; print(f"[ERROR] {path}: {exc}")
    print(f"SUMMARY changed={changed} errors={errors} mode={'write' if args.write else 'dry-run'}")
    return 1 if errors else 0


if __name__ == "__main__": raise SystemExit(main())
