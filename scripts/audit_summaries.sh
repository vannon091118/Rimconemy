#!/usr/bin/env bash
# audit_summaries.sh — Cross-reference C# test-suites against parser_config.json.
#
# Checks:
#   1. Every config entry has a matching C# test file
#   2. Every C# test file emitting a summary has a config entry
#   3. Envelope: all summaries use [Rimconemy.<Pkg>] prefix
#   4. No Log.Warning for test failures (Swallow-Verbot)
#   5. No magic-number summaries (should use min= via TestSuite)
#
# Usage:
#   ./scripts/audit_summaries.sh
#   ./scripts/audit_summaries.sh --strict  (exit 1 on any finding)

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CONFIG="$SCRIPT_DIR/parser_config.json"
STRICT=false
[[ "${1:-}" == "--strict" ]] && STRICT=true

FINDINGS=0
PASSES=0

findings() { echo "  🔍 $*"; FINDINGS=$((FINDINGS + 1)); }
pass_msg() { echo "  ✅ $*"; PASSES=$((PASSES + 1)); }

echo "=== audit_summaries.sh ==="
echo ""

# ── 1. Config → C#: every config entry should have a matching test file ──
echo "── Check 1: Config entries → C# test files ──"
python3 -c "
import json, os, re, sys

with open('$CONFIG') as f:
    config = json.load(f)

suites = config.get('test_suites', {}).get('required', [])
missing = 0
for s in suites:
    pkg = s.get('package', '??')
    pattern = s.get('pattern', '')
    # Extract a likely class name from the pattern
    # e.g. 'CapabilityGate tests: \\d+...' → 'CapabilityGate'
    #      'PopulationLedger regression tests \\(Phase A...' → 'PopulationLedger'
    m = re.match(r'^([A-Za-z0-9_-]+)', pattern)
    if not m:
        print(f'  ⚠️  Cannot parse pattern: {pattern[:60]}')
        continue
    prefix = m.group(1)
    # Search for matching test file
    found = False
    for root, dirs, files in os.walk('mods'):
        for f in files:
            if f.endswith('.cs') and prefix in f:
                found = True
                break
        if found: break
    if not found:
        print(f'  ❌ Config suite \"{prefix}\" (pkg={pkg}) has no matching C# file')
        missing += 1
if missing == 0:
    print('  ✅ All config entries have matching C# files')
" 2>&1

# ── 2. C# → Config: every test emitting a summary should be in config ──
echo ""
echo "── Check 2: C# summary emitters → config entries ──"
python3 -c "
import json, os, re

with open('$CONFIG') as f:
    config = json.load(f)
config_patterns = set()
for s in config.get('test_suites', {}).get('required', []):
    config_patterns.add(s.get('pattern', ''))

# Find all summary-emitting test files
for root, dirs, files in os.walk('mods'):
    for f in files:
        if not f.endswith('.cs') or 'Tests/' not in root:
            continue
        path = os.path.join(root, f)
        with open(path) as fh:
            content = fh.read()
        # Look for summary lines
        summaries = re.findall(r'\[Rimconemy\.([^\]]+)\]\s+(.+?tests?):?\s*[\"']', content)
        for pkg, suite in summaries:
            # Check if any config pattern matches
            matched = False
            for cp in config_patterns:
                # Extract the suite name from the config pattern
                cm = re.match(r'^(.+?):\s*\\\\d', cp)
                if cm:
                    cname = cm.group(1).replace('\\\\(', '(').replace('\\\\)', ')')
                    if suite.strip().startswith(cname) or cname.startswith(suite.strip()):
                        matched = True
                        break
            if not matched and 'RegressionTests' in f:
                print(f'  ⚠️  {f}: summary \"{suite[:40]}...\" not in config')
" 2>&1

# ── 3. Envelope: all [Xxx] prefixes in test files should use [Rimconemy.*] ──
echo ""
echo "── Check 3: Envelope compliance ──"
ENVELOPE_VIOLATIONS=$(grep -rn 'Log\.\(Error\|Warning\|Message\)("\[\(Rimconemy\.\|0-\)' mods --include='*.cs' | grep -v '\[Rimconemy\.' | grep -v '\[0-' | grep 'Tests/' | wc -l)
if [[ "$ENVELOPE_VIOLATIONS" -eq 0 ]]; then
    pass_msg "0 envelope violations — all test log lines use [Rimconemy.*]"
else
    findings "$ENVELOPE_VIOLATIONS log lines with non-[Rimconemy.*] prefix in test files"
    if [[ "$STRICT" == true ]]; then
        grep -rn 'Log\.\(Error\|Warning\|Message\)("\[\(Rimconemy\.\|0-\)' mods --include='*.cs' | grep -v '\[Rimconemy\.' | grep -v '\[0-' | grep 'Tests/' | head -10
    fi
fi

# ── 4. Swallow-Verbot: no Log.Warning for test failures ──
echo ""
echo "── Check 4: Swallow-Verbot (Log.Warning in test failures) ──"
SWALLOW_COUNT=$(grep -rn 'Log\.Warning.*FAIL\|Log\.Warning.*test FAILED' mods --include='*.cs' -l | wc -l)
if [[ "$SWALLOW_COUNT" -eq 0 ]]; then
    pass_msg "0 Swallow-Verbot violations — no Log.Warning for test failures"
else
    findings "$SWALLOW_COUNT files with Log.Warning for test failures (should be Log.Error)"
    if [[ "$STRICT" == true ]]; then
        grep -rn 'Log\.Warning.*FAIL\|Log\.Warning.*test FAILED' mods --include='*.cs' -l
    fi
fi

# ── 5. Magic numbers: summaries should use min= not hardcoded counts ──
echo ""
echo "── Check 5: Magic-number summaries ──"
MAGIC_COUNT=$(grep -rn '_passed + " passed, " + _failed + " failed' mods --include='*.cs' -l | wc -l)
if [[ "$MAGIC_COUNT" -eq 0 ]]; then
    pass_msg "0 magic-number summaries — all use TestSuite or min= format"
else
    findings "$MAGIC_COUNT files with manual _passed/_failed summaries (should use TestSuite)"
    if [[ "$STRICT" == true ]]; then
        grep -rn '_passed + " passed, " + _failed + " failed' mods --include='*.cs' -l | head -10
    fi
fi

# ── Summary ──
echo ""
echo "══════════════════════════════════════════════════"
echo "Audit complete: $FINDINGS findings, $PASSES passes"
if [[ "$FINDINGS" -gt 0 ]]; then
    echo "Run with --strict for details."
    [[ "$STRICT" == true ]] && exit 1
else
    echo "✅ All checks passed."
fi
echo "══════════════════════════════════════════════════"
