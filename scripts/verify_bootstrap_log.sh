#!/usr/bin/env bash
# verify_bootstrap_log.sh — Asserts the ProfileDetector dedup invariants on a
# freshly emitted Player.log.
#
# Invariants enforced (each is a separate pass/fail):
#
#   I1. Zero per-DLC detection log lines. Specifically:
#         - `DLC 'X' detected by Name match`             → must be 0
#         - `DLC 'X' detected by PackageId match`        → must be 0
#         - `DLC 'X' detected by exact PackageId match`  → must be 0
#       (≤ 0.0.37 emitted these; 0.1.37 strips them. See
#       `mods/01-Rimconemy-Foundation/Source/Profile/ProfileDetector.cs`).
#
#   I2. At most one `Profile detected:` log line per
#       (sorted-packages, missing-count, dlc-missing-count) tuple.
#       Two states with the same packages + missing count but different
#       DLC missing count are distinct states and must therefore also
#       have distinct dedup tuples.
#
#   I3. No two `Profile detected:` log lines have IDENTICAL full content.
#       This is what `_lastEmittedSummary = logMessage;` in
#       `ProfileDetector.cs` actually defends against; full-string
#       equality is a strictly stronger assertion than the per-tuple
#       dedup and catches both the Foundation-cctor re-entry AND
#       any bug where the dedup token is reset without a state change.
#
# Usage:
#   scripts/verify_bootstrap_log.sh
#   scripts/verify_bootstrap_log.sh /path/to/Player.log
#   scripts/verify_bootstrap_log.sh --log /path/to/Player.log
#
# Exit codes:
#   0  All invariants pass.
#   1  At least one invariant failed.
#   2  Usage error (missing log, both --log and positional given, etc.).
#
# Companion gate: `scripts/runtime_test.sh` invokes this script from
# `runtime_gates()` after the existing summary checks, so a regression
# in dedup fails CI before merge.

set -uo pipefail

DEFAULT_LOG="$HOME/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log"
SCRIPT_NAME="$(basename "$0")"
LOG_PATH=""
LOG_SET_COUNT=0

usage() {
    cat <<EOF
Usage: $SCRIPT_NAME [--log PATH] [PATH]

Asserts ProfileDetector bootstrap-log invariants on a RimWorld Player.log file.
Default log path: $DEFAULT_LOG.

Options:
  --log PATH     Path to Player.log (overrides default; mutually exclusive
                 with positional PATH but --log wins if both are given).
  -h, --help     Show this help.
EOF
}

# Parse args. Supports both `--log PATH` and a single positional PATH.
while [[ $# -gt 0 ]]; do
    case "$1" in
        --log)
            [[ $# -ge 2 ]] || { echo "$SCRIPT_NAME: --log requires a path" >&2; exit 2; }
            LOG_PATH="$2"; shift 2; LOG_SET_COUNT=$((LOG_SET_COUNT + 1)) ;;
        -h|--help)
            usage; exit 0 ;;
        -*)
            echo "$SCRIPT_NAME: unknown option: $1" >&2; usage >&2; exit 2 ;;
        *)
            LOG_PATH="$1"; shift; LOG_SET_COUNT=$((LOG_SET_COUNT + 1)) ;;
    esac
done

if [[ "$LOG_SET_COUNT" -gt 1 ]]; then
    echo "$SCRIPT_NAME: only one of --log or positional path allowed" >&2
    exit 2
fi
LOG_PATH="${LOG_PATH:-$DEFAULT_LOG}"

# Pass / fail accounting — mirrors runtime_test.sh style so the gate integrates
# in a fixed shape.
FAILURES=0
WARNINGS=0
fail() { echo "FAIL: $*" >&2; FAILURES=$((FAILURES + 1)); }
warn() { echo "WARN: $*" >&2; WARNINGS=$((WARNINGS + 1)); }
pass() { echo "PASS: $*"; }

# rg with grep fallback — mirrors runtime_test.sh so this script is independent.
has_match() {
    local pattern="$1" path="$2"
    if command -v rg >/dev/null 2>&1; then
        rg -q -- "$pattern" "$path"
    else
        grep -Eq -- "$pattern" "$path"
    fi
}

# Count matches with the same fallback chain.
match_count() {
    local pattern="$1" path="$2"
    if command -v rg >/dev/null 2>&1; then
        rg -c -- "$pattern" "$path" 2>/dev/null | awk -F: '{s+=$NF} END{print s+0}'
    else
        grep -Ec -- "$pattern" "$path" 2>/dev/null || true
    fi
}

[[ -e "$LOG_PATH" ]] || { echo "$SCRIPT_NAME: log not found: $LOG_PATH" >&2; exit 2; }

echo "=== verify_bootstrap_log.sh ==="
echo "log=$LOG_PATH"

# I1 — Per-DLC detection log lines must be 0. The pre-fix DLL emitted 5×2=10 of
# these during the Foundation cctor re-entry; the dedup fix strips the inner
# Log.Message calls from IsDlcLoaded so the canonical "Profile detected" line
# is the only diagnostic for DLC state.
PER_DLC_LINES_TOTAL=0
for pattern in \
    "DLC .* detected by Name match" \
    "by PackageId match" \
    "NOT detected among running mods"
do
    cnt=$(match_count "$pattern" "$LOG_PATH")
    PER_DLC_LINES_TOTAL=$((PER_DLC_LINES_TOTAL + cnt))
done
if [[ "$PER_DLC_LINES_TOTAL" -eq 0 ]]; then
    pass "Bootstrap log: zero per-DLC detection log lines (I1)"
else
    fail "Bootstrap log: per-DLC detection log lines found (count=$PER_DLC_LINES_TOTAL)"
    for pattern in \
        "DLC .* detected by Name match" \
        "by PackageId match" \
        "NOT detected among running mods"
    do
        if has_match "$pattern" "$LOG_PATH"; then
            echo "  matches for /${pattern}/:" >&2
            if command -v rg >/dev/null 2>&1; then
                rg -n -- "$pattern" "$LOG_PATH" | head -10 >&2
            else
                grep -En -- "$pattern" "$LOG_PATH" | head -10 >&2
            fi
        fi
    done
fi

# I2 / I3 — Extract every "Profile detected:" line and assert uniqueness.
# Pattern is anchored to the [Rimconemy.Foundation] prefix to scope the gate
# to Foundation-owned emissions; substring matches on non-canonical contexts
# would feed into the parser as unparsed and trigger a noisy FAIL otherwise.
# The `[` and `]` are RE-anchored regex literals inside the bracket class —
# `\[` matches a literal `[`, `\]` matches a literal `]`. The `.` in
# `Rimconemy.Foundation` is escaped as `\.` to match the literal dot
# (PCRE / POSIX BRE both treat `\.` as a literal-period escape).
PROFILE_LINE_RAW=""
if command -v rg >/dev/null 2>&1; then
    PROFILE_LINE_RAW="$(rg -n -- '\[Rimconemy\.Foundation\] Profile detected:' "$LOG_PATH" 2>/dev/null || true)"
else
    PROFILE_LINE_RAW="$(grep -En -- '\[Rimconemy\.Foundation\] Profile detected:' "$LOG_PATH" 2>/dev/null || true)"
fi

# Strip the leading "<line_number>:" prefix rg/grep -n adds. We key everything
# from the canonical content onward.
PROFILE_LINES=""
while IFS= read -r raw; do
    [[ -z "$raw" ]] && continue
    stripped="${raw#*:}"
    PROFILE_LINES+="${stripped}"$'\n'
done <<< "$PROFILE_LINE_RAW"

PROFILE_COUNT=0
while IFS= read -r raw; do
    [[ -z "$raw" ]] && continue
    PROFILE_COUNT=$((PROFILE_COUNT + 1))
done <<< "$PROFILE_LINES"

if [[ "$PROFILE_COUNT" -eq 0 ]]; then
    fail "Bootstrap log: no 'Profile detected:' lines (ProfileDetector did not run?)"
else
    declare -A SEEN_TUPLE
    declare -A SEEN_FULL
    declare -a UNPARSED_LINES
    DUP_TUPLE_OUT=""
    DUP_FULL_OUT=""
    unparsed_count=0

    while IFS= read -r line; do
        [[ -z "$line" ]] && continue
        # Extract (sorted-packages, missing, dlc-missing). Tolerate whitespace
        # variation and the `<>`-bracket escape style sed uses.
        sorted_pkgs="$(printf '%s' "$line" \
            | sed -nE 's/.*packages registered: ([^)]+).*/\1/p' \
            | sed -nE 's/(.*), missing:.*/\1/p')"
        missing_count="$(printf '%s' "$line" \
            | sed -nE 's/.*missing: ([0-9]+),.*/\1/p')"
        dlc_missing="$(printf '%s' "$line" \
            | sed -nE 's/.*DLCs missing: ([0-9]+)[^0-9].*/\1/p')"
        # If any of the three fields failed to extract, treat the line as
        # unparsed. The gate must NOT silently let an UNPARSED-set pass —
        # otherwise a `BuildSummaryMessage` line-format drift in
        # `ProfileDetector.cs` would turn the gate into an oracle. Track the
        # unparsed lines explicitly and fail if any are seen.
        if [[ -z "$sorted_pkgs" || -z "$missing_count" || -z "$dlc_missing" ]]; then
            key="UNPARSED|line=${line}"
            UNPARSED_LINES+=("$line")
            unparsed_count=$((unparsed_count + 1))
        else
            key="${sorted_pkgs}|missing=${missing_count}|dlc_missing=${dlc_missing}"
        fi
        if [[ -n "${SEEN_TUPLE[$key]:-}" ]]; then
            DUP_TUPLE_OUT+="  ${key}"$'\n'
        else
            SEEN_TUPLE[$key]=1
        fi
        if [[ -n "${SEEN_FULL[$line]:-}" ]]; then
            DUP_FULL_OUT+="  ${line}"$'\n'
        else
            SEEN_FULL[$line]=1
        fi
    done <<< "$PROFILE_LINES"

    # Order: parse-cleanliness check FIRST so a regression that breaks every
    # line's format produces a single loud FAIL rather than also emitting
    # misleading "unique tuples" / "no full-content duplicates" PASS lines
    # whose underlying data is malformed. (Variables `cap` and `i` are
    # intentionally NOT declared `local` because this block runs at script
    # top-level, not inside a function; `local` outside a function is a
    # Bash error under `set -u`.)
    if [[ "$unparsed_count" -gt 0 ]]; then
        fail "Bootstrap log: ${unparsed_count} 'Profile detected:' line(s) failed to parse (line format drift in ProfileDetector.BuildSummaryMessage?)"
        # Dynamic stderr cap: print all lines if count ≤ 10 (small regression
        # is fully visible); print first 3 + "... N more" otherwise to bound
        # diagnostic spam on larger regressions.
        cap=3
        if [[ "${#UNPARSED_LINES[@]}" -le 10 ]]; then
            cap="${#UNPARSED_LINES[@]}"
        fi
        i=0
        while [[ "$i" -lt "${#UNPARSED_LINES[@]}" && "$i" -lt "$cap" ]]; do
            echo "    ${UNPARSED_LINES[$i]}" >&2
            i=$((i + 1))
        done
        if [[ "${#UNPARSED_LINES[@]}" -gt "$cap" ]]; then
            remaining=$(( ${#UNPARSED_LINES[@]} - cap ))
            echo "    ... ${remaining} more line(s) suppressed" >&2
        fi
    else
        pass "Bootstrap log: all 'Profile detected:' lines parse cleanly (canonical ProfileDetector.BuildSummaryMessage shape)"
    fi

    # I2 / I3 emit ONLY when every input line parsed cleanly. Skipping them
    # when `unparsed_count > 0` prevents misleading PASS lines whose
    # underlying data is malformed (each `UNPARSED|line=…` key is unique by
    # construction, so I2/I3 would otherwise pass-by-side-effect alongside
    # the parse-cleanliness FAIL). The earlier `if [[ "$unparsed_count" -gt 0 ]]`
    # has already incremented FAILURES so the gate still fails overall.
    if [[ "$unparsed_count" -eq 0 ]]; then
        if [[ -z "$DUP_TUPLE_OUT" ]]; then
            pass "Bootstrap log: unique (packages,missing,dlc_missing) tuple per Profile detected line (I2, ${PROFILE_COUNT} line(s))"
        else
            fail "Bootstrap log: duplicate (packages,missing,dlc_missing) tuple(s):"
            printf '%s' "$DUP_TUPLE_OUT" >&2
        fi

        if [[ -z "$DUP_FULL_OUT" ]]; then
            pass "Bootstrap log: no two 'Profile detected:' lines have identical full content (I3)"
        else
            fail "Bootstrap log: byte-identical 'Profile detected:' duplicates found:"
            printf '%s' "$DUP_FULL_OUT" >&2
        fi
    fi
fi

echo "=== summary ==="
if [[ "$FAILURES" -eq 0 ]]; then
    echo "verify_bootstrap_log: PASS (warnings=$WARNINGS, profile_lines=${PROFILE_COUNT:-0}, per_dlc_lines=$PER_DLC_LINES_TOTAL)"
    exit 0
fi
echo "verify_bootstrap_log: FAIL (failures=$FAILURES, warnings=$WARNINGS)" >&2
exit 1
