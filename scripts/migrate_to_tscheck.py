#!/usr/bin/env python3
"""Migrate custom Assert helpers to ts.Check() using parenthesis-aware parsing."""

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODS = ROOT / "mods"
SKIP = {"TestSuite.cs", "ScribeRoundTripHelper.cs"}


def _find_matching_paren(text, start):
    """Find matching ')' for '(' at `start`, respecting string literals."""
    depth = 0
    in_string = False
    in_char = False
    i = start
    while i < len(text):
        ch = text[i]
        if in_string:
            if ch == '\\':
                i += 2  # skip escape sequence
                continue
            if ch == '"':
                in_string = False
            i += 1
            continue
        if in_char:
            if ch == '\\':
                i += 2
                continue
            if ch == "'":
                in_char = False
            i += 1
            continue
        if ch == '"':
            in_string = True
        elif ch == "'":
            in_char = True
        elif ch == '(':
            depth += 1
        elif ch == ')':
            depth -= 1
            if depth == 0:
                return i
        i += 1
    return -1


def _split_args(args_text):
    """Split comma-separated arguments, respecting nested parens and strings."""
    parts = []
    depth = 0
    in_string = False
    in_char = False
    current_start = 0
    i = 0
    while i < len(args_text):
        ch = args_text[i]
        if in_string:
            if ch == '\\':
                i += 2
                continue
            if ch == '"':
                in_string = False
            i += 1
            continue
        if in_char:
            if ch == '\\':
                i += 2
                continue
            if ch == "'":
                in_char = False
            i += 1
            continue
        if ch == '"':
            in_string = True
        elif ch == "'":
            in_char = True
        elif ch == '(':
            depth += 1
        elif ch == ')':
            depth -= 1
        elif ch == ',' and depth == 0:
            parts.append(args_text[current_start:i])
            current_start = i + 1
        i += 1
    parts.append(args_text[current_start:])
    return [p.strip() for p in parts]


def _replace_calls_paren(text):
    """Replace Assert* call sites using parenthesis-aware argument extraction."""
    call_re = re.compile(r'\bAssert(True|False|Equal|Null|NotNull)\s*(?:<[^>]+>)?\s*\(')

    result = []
    pos = 0
    while pos < len(text):
        m = call_re.search(text, pos)
        if not m:
            result.append(text[pos:])
            break

        result.append(text[pos:m.start()])

        kind = m.group(1)
        paren_start = m.end() - 1  # position of '('
        paren_end = _find_matching_paren(text, paren_start)

        if paren_end == -1:
            result.append(text[m.start():m.end()])
            pos = m.end()
            continue

        args_text = text[paren_start + 1:paren_end]
        args = _split_args(args_text)

        repl = None
        if kind == 'True' and len(args) >= 2:
            repl = f'ts.Check({args[0]}, {args[1]});'
        elif kind == 'False' and len(args) >= 2:
            repl = f'ts.Check(!({args[0]}), {args[1]});'
        elif kind == 'Equal' and len(args) >= 3:
            repl = f'ts.Check(Equals({args[0]}, {args[1]}), {args[2]});'
        elif kind == 'Null' and len(args) >= 2:
            repl = f'ts.Check({args[0]} == null, {args[1]});'
        elif kind == 'NotNull' and len(args) >= 2:
            repl = f'ts.Check({args[0]} != null, {args[1]});'

        if repl:
            result.append(repl)
        else:
            result.append(text[m.start():paren_end + 1] + ';')

        pos = paren_end + 1
        while pos < len(text) and text[pos] in ' \t':
            pos += 1
        if pos < len(text) and text[pos] == ';':
            pos += 1

    return ''.join(result)


def _find_matching_brace(lines, start_idx):
    """Find matching '}' for '{' respecting string/char literals."""
    depth = 0
    in_string = False
    in_char = False
    for i in range(start_idx, len(lines)):
        j = 0
        while j < len(lines[i]):
            ch = lines[i][j]
            if in_string:
                if ch == '\\':
                    j += 2
                    continue
                if ch == '"':
                    in_string = False
                j += 1
                continue
            if in_char:
                if ch == '\\':
                    j += 2
                    continue
                if ch == "'":
                    in_char = False
                j += 1
                continue
            if ch == '"':
                in_string = True
            elif ch == "'":
                in_char = True
            elif ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
                if depth == 0:
                    return i
            j += 1
    return start_idx


def _remove_helpers(text):
    lines = text.split('\n')
    helper_sig = re.compile(r'^\s*private static void Assert(True|False|Equal|Null|NotNull)\b')
    removed = set()
    i = 0
    while i < len(lines):
        m = helper_sig.match(lines[i])
        if m and i not in removed:
            brace_line = i
            while brace_line < len(lines) and '{' not in lines[brace_line]:
                brace_line += 1
            if brace_line >= len(lines):
                i += 1
                continue
            end = _find_matching_brace(lines, brace_line)
            for j in range(i, end + 1):
                removed.add(j)
            i = end + 1
        else:
            i += 1
    result = [l for idx, l in enumerate(lines) if idx not in removed]
    cleaned = []
    blank_count = 0
    for l in result:
        if l.strip() == '':
            blank_count += 1
            if blank_count <= 2:
                cleaned.append(l)
        else:
            blank_count = 0
            cleaned.append(l)
    return '\n'.join(cleaned)


def migrate_file(path: Path) -> tuple[str, str]:
    original = path.read_text(encoding="utf-8")
    if "new TestSuite(" not in original:
        return original, "no-TestSuite"

    clean = re.sub(r'^\s*private static void Assert\w+.*$', '', original, flags=re.MULTILINE)
    remaining = len(re.findall(r'\bAssert(True|False|Equal|Null|NotNull)\s*\(', clean))
    if remaining == 0:
        return original, "already-migrated"

    updated = _remove_helpers(original)
    updated = _replace_calls_paren(updated)

    if updated == original:
        return original, "no-changes"
    return updated, "migrated"


def main():
    import argparse
    parser = argparse.ArgumentParser(description="Migrate Assert helpers to ts.Check")
    parser.add_argument("path", nargs="?", help="single test file")
    parser.add_argument("--all", action="store_true")
    parser.add_argument("--write", action="store_true")
    args = parser.parse_args()

    if args.all:
        targets = sorted(p for p in MODS.glob("*/Tests/*.cs") if p.name not in SKIP)
    elif args.path:
        targets = [Path(args.path)]
    else:
        parser.error("provide a path or --all")
        return 1

    changed = errors = 0
    for path in targets:
        try:
            old = path.read_text(encoding="utf-8")
            new, status = migrate_file(path)
            if status in {"no-TestSuite", "already-migrated", "no-changes"}:
                print(f"[SKIP] {path}: {status}")
                continue
            changed += 1
            print(f"[{'WRITE' if args.write else 'PLAN'}] {path}: {status}")
            if args.write and new != old:
                path.write_text(new, encoding="utf-8")
        except Exception as exc:
            errors += 1
            print(f"[ERROR] {path}: {exc}")
    print(f"SUMMARY changed={changed} errors={errors} mode={'write' if args.write else 'dry-run'}")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
