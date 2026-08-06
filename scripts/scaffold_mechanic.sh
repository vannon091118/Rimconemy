#!/usr/bin/env bash
# scaffold_mechanic.sh — Neue Rimconemy-Mechanik in einem Befehl einrichten.
#
# Erzeugt:
#   1. Test-Datei mit TestSuite-Harness + min= + RunAll()
#   2. parser_config.json-Eintrag (via python)
#   3. Falsification-Stub mit A–G-Evidenz + Marker-Platzhaltern
#
# Usage:
#   ./scripts/scaffold_mechanic.sh <pkg> <MechanicName>
#   ./scripts/scaffold_mechanic.sh 05 HordeOverlayV2
#
# Output:
#   - mods/<pkg>-*/Tests/<MechanicName>RegressionTests.cs
#   - parser_config.json updated
#   - docs/falsification/<pkg>__<MechanicName>.md

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

PKG="${1:-}"
NAME="${2:-}"

if [[ -z "$PKG" || -z "$NAME" ]]; then
    echo "Usage: $0 <pkg> <MechanicName>"
    echo "  pkg: 01, 02, 03, 04, or 05"
    echo "  MechanicName: PascalCase, no spaces (e.g. HordeOverlayV2)"
    exit 2
fi

# Map package number to directory and namespace
case "$PKG" in
    01) PKG_DIR="01-Rimconemy-Foundation";       NS="Foundation";;
    02) PKG_DIR="02-Rimconemy-Survival-Progression"; NS="SurvivalProgression";;
    03) PKG_DIR="03-Rimconemy-Scavenger-Infrastructure"; NS="ScavengerInfrastructure";;
    04) PKG_DIR="04-Rimconemy-Economy-Territory"; NS="EconomyTerritory";;
    05) PKG_DIR="05-Rimconemy-Infected-Automation"; NS="InfectedAutomation";;
    *)  echo "Invalid package: $PKG (must be 01-05)"; exit 2;;
esac

TESTS_DIR="$PROJECT_ROOT/mods/$PKG_DIR/Tests"
FALSIFICATION_DIR="$PROJECT_ROOT/docs/falsification"
TEST_FILE="$TESTS_DIR/${NAME}RegressionTests.cs"
FALS_FILE="$FALSIFICATION_DIR/${PKG}__${NAME}.md"
CONFIG_FILE="$SCRIPT_DIR/parser_config.json"
SUITE_LABEL="$(echo "$NAME" | sed 's/\([A-Z]\)/ \1/g' | sed 's/^ //')"  # HordeOverlayV2 → Horde Overlay V2

# ── 1. Create test file ───────────────────────────────────────
echo "📝 Creating test file: $TEST_FILE"
mkdir -p "$TESTS_DIR"

cat > "$TEST_FILE" << CSHERE
// Tests/${NAME}RegressionTests.cs
//
// Owner: $NS (Package $PKG).
// Scaffolded $(date +%Y-%m-%d) by scaffold_mechanic.sh.
//
// TODO: Document what this suite verifies.

using Rimconemy.Foundation.Tests;
using Verse;

namespace Rimconemy.${NS}.Tests
{
    public static class ${NAME}RegressionTests
    {
        // TODO: Set min= to the number of ts.Check() calls below.
        private const int MinPassCount = 1;

        public static void RunAll()
        {
            var ts = new TestSuite("${NS}", "${NAME} regression");

            // TODO: Replace with real checks.
            ts.Check(true, "T1.ScaffoldSanity — remove me");

            ts.RunSummary(MinPassCount);
        }
    }
}
CSHERE

# ── 2. Add parser_config.json entry ──────────────────────────
echo "📋 Adding config entry: $SUITE_LABEL regression tests"
python3 -c "
import json, sys
with open('$CONFIG_FILE') as f:
    config = json.load(f)

pattern = '${NAME} regression: \\\\\\\\d+ passed, 0 failed'
entry = {'package': '$PKG', 'pattern': pattern}

suites = config.get('test_suites', {}).get('required', [])
# Check if already exists
for s in suites:
    if s.get('pattern') == pattern:
        print('  ⚠️  Config entry already exists — skipping')
        sys.exit(0)

suites.append(entry)
config['test_suites']['required'] = suites

with open('$CONFIG_FILE', 'w') as f:
    json.dump(config, f, indent=2, ensure_ascii=False)
    f.write('\n')
print('  ✅ Config entry added')
"

# ── 3. Create falsification stub ──────────────────────────────
echo "📄 Creating falsification stub: $FALS_FILE"
mkdir -p "$FALSIFICATION_DIR"

cat > "$FALS_FILE" << MDHERE
# Falsification: $NAME ($PKG-$NS)

**Created:** $(date +%Y-%m-%d)
**Scaffolded by:** scaffold_mechanic.sh

## Evidence Matrix

| Gate | Evidence | Status |
|------|----------|--------|
| A — Code exists | \`${NAME}RegressionTests.cs\` | ⬜ UNVERIFIED |
| B — Defs defined | — | ⬜ UNVERIFIED |
| C — Compiles | \`dotnet build\` | ⬜ UNVERIFIED |
| D — Boots | \`runtime_test.sh\` summary present | ⬜ UNVERIFIED |
| E — Scribe roundtrip | ScribeRoundTripHelper | ⬜ UNVERIFIED |
| F — Live runtime | Player.log marker | ⬜ UNVERIFIED |
| G — Manual session | User report | ⬜ UNVERIFIED |

## Runtime Markers (for D4 marker-hypotheses)

\`\`\`
# CONFIRMED — marker appears in log
# REFUTED-CLEAN — marker absent, boot clean → hypothesis outdated
# SUSPICIOUS — marker absent, system was active → investigate
\`\`\`

| Marker | Expected | Status |
|--------|----------|--------|
| \`[Rimconemy.$NS] ${NAME} regression: N passed, 0 failed (min=E).\` | Test summary | ⬜ |

## Verification Checklist

- [ ] Test file: \`mods/$PKG_DIR/Tests/${NAME}RegressionTests.cs\`
- [ ] Config entry: \`scripts/parser_config.json\` → \`$SUITE_LABEL regression\`
- [ ] \`./scripts/deploy.sh $PKG\` → compiles
- [ ] \`./scripts/runtime_test.sh\` → PASS with new suite in summary
- [ ] \`./scripts/parse_runtime_log.py --focused $PKG\` → completeness ✅
- [ ] Falsification gates A–G ticked off
MDHERE

# ── Summary ──────────────────────────────────────────────────
echo ""
echo "══════════════════════════════════════════════════"
echo "✅ Mechanic '$NAME' scaffolded in Package $PKG ($NS)"
echo ""
echo "Created:"
echo "  📝 $TEST_FILE"
echo "  📋 parser_config.json (+1 suite → $(python3 -c "import json; c=json.load(open('$CONFIG_FILE')); print(len(c['test_suites']['required']))") total)"
echo "  📄 $FALS_FILE"
echo ""
echo "Next steps:"
echo "  1. Edit $TEST_FILE — replace scaffold checks with real ones"
echo "  2. Update MinPassCount to match number of ts.Check() calls"
echo "  3. Run: ./scripts/deploy.sh $PKG && ./scripts/runtime_test.sh"
echo "  4. Tick off falsification gates A–G in $FALS_FILE"
echo "══════════════════════════════════════════════════"
