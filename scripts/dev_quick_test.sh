#!/usr/bin/env bash
# dev_quick_test.sh — Fast Rimconemy dev iteration gate (~30-45s).
# Does NOT start RimWorld. Checks: XML well-formed, invalid 1.6 fields removed,
# cross-references, static installed gates, regression test signatures in log.
#
# Usage:
#   ./scripts/dev_quick_test.sh                    # default log path
#   ./scripts/dev_quick_test.sh /custom/Player.log
#   ./scripts/dev_quick_test.sh --strict           # also run XML verification script

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DEFAULT_LOG="$HOME/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log"

LOG_PATH="${1:-$DEFAULT_LOG}"
STRICT_MODE=false
[[ "${1:-}" == "--strict" ]] && { STRICT_MODE=true; LOG_PATH="$DEFAULT_LOG"; }
[[ "${2:-}" == "--strict" ]] && STRICT_MODE=true

FAILURES=0
WARNINGS=0

fail() { echo "FAIL: $*" >&2; FAILURES=$((FAILURES + 1)); }
warn() { echo "WARN: $*" >&2; WARNINGS=$((WARNINGS + 1)); }
pass() { echo "PASS: $*"; }

has_match() {
    local pattern="$1" path="$2"
    if command -v rg >/dev/null 2>&1; then rg -q -- "$pattern" "$path"; else grep -Eq -- "$pattern" "$path"; fi
}

command_exists() { command -v "$1" >/dev/null 2>&1; }

echo "=== dev_quick_test.sh ==="
echo "log=$LOG_PATH"
echo "strict=$STRICT_MODE"
echo

# 1. Static installed gates (About.xml, packageId, 1.6 support, DLLs)
echo "--- Static Installed Gates ---"
PACKAGES=(
    "01-Rimconemy-Foundation|rimconemy.foundation"
    "02-Rimconemy-Survival-Progression|rimconemy.survivalprogression"
    "03-Rimconemy-Scavenger-Infrastructure|rimconemy.scavengerinfrastructure"
    "04-Rimconemy-Economy-Territory|rimconemy.economyterritory"
    "05-Rimconemy-Infected-Automation|rimconemy.infectedautomation"
)

mods_dir="/home/vannon/GOG Games/RimWorld/game/Mods"
for entry in "${PACKAGES[@]}"; do
    package_dir="${entry%%|*}"
    package_id="${entry##*|}"
    about="$mods_dir/$package_dir/About/About.xml"
    dll=$(find "$mods_dir/$package_dir/Assemblies" -maxdepth 1 -type f -name 'Rimconemy.*.dll' -print -quit 2>/dev/null)

    [[ -f "$about" ]] || { fail "$package_id: About.xml missing"; continue; }
    if command_exists xmllint; then
        xmllint --noout "$about" 2>/dev/null || { fail "$package_id: About.xml malformed"; continue; }
    fi
    pkg_id=$(grep -oP '(?<=<packageId>)[^<]+' "$about" | head -1)
    [[ "$pkg_id" == "$package_id" ]] || fail "$package_id: packageId mismatch ($pkg_id)"
    pass "$package_id: packageId"
    grep -q '<li>1.6</li>' "$about" && pass "$package_id: 1.6 supported" || fail "$package_id: 1.6 NOT supported"
    [[ -n "$dll" ]] && pass "$package_id: assembly ($(basename "$dll"))" || fail "$package_id: assembly missing"
done

# 2. XML well-formedness + invalid 1.6 fields check
echo
echo "--- XML Validation ---"
INVALID_FIELDS=(
    "ThingDef:surfacePosition"
    "RecipeDef:defaultIngredientCount"
    "MainButtonDef:showInInterface"
)

xml_files=()
while IFS= read -r -d '' f; do xml_files+=("$f"); done < <(find "$PROJECT_ROOT/mods" -name '*.xml' -print0 2>/dev/null)

for f in "${xml_files[@]}"; do
    # Check well-formed
    if command_exists python3; then
        python3 -c "import xml.etree.ElementTree as ET; ET.parse('$f')" 2>/dev/null || { fail "XML malformed: $f"; continue; }
    elif command_exists xmllint; then
        xmllint --noout "$f" 2>/dev/null || { fail "XML malformed: $f"; continue; }
    fi

    # Check invalid fields
    for entry in "${INVALID_FIELDS[@]}"; do
        dtype="${entry%%:*}"
        field="${entry##*:}"
        if grep -q "<$field>" "$f" 2>/dev/null; then
            fail "Invalid 1.6 field <$field> in $f (type: $dtype)"
        fi
    done
done

# Count validated files
VALIDATED=0
for f in "${xml_files[@]}"; do
    if command_exists python3; then
        python3 -c "import xml.etree.ElementTree as ET; ET.parse('$f')" 2>/dev/null && VALIDATED=$((VALIDATED + 1))
    fi
done
pass "XML files well-formed: $VALIDATED/${#xml_files[@]}"

# 3. GameComponent (Game)-ctor pattern (RimWorld 1.6)
#    Background: Verse.Game.FillComponents calls
#      Activator.CreateInstance(compType, new object[] { this })
#      with the Game instance as the first arg. Subclasses of GameComponent
#      MUST therefore expose a `public Xxx(Game game) { }` ctor, otherwise
#      they throw MissingMethodException at game-startup. (Verse.GameComponent
#      itself in 1.6 has only a parameterless ctor, so the subclass ctor
#      uses the implicit `base()` parameterless — do NOT forward `: base(game)`,
#      that fails to compile with CS1729.)
#
#    Two false-positive defenses (per code-reviewer-minimax-m3 feedback):
#      * class-regex requires the `class` keyword, ignoring doc comments
#        like `/// GameComponent ensures persistence`;
#      * ctor-regex is anchored at start-of-line and requires indented
#        `public ...`, so `// public ...` comment lines don't satisfy it.
#    Tests/ is excluded because tests instantiate GameComponents manually
#    (`new X()`) and don't go through FillComponents.
echo
echo "--- GameComponent (Game)-ctor pattern ---"
GC_GAPS=()
declare -A GC_BY_PKG
while IFS= read -r f; do
    case "$f" in
        */Tests/*) continue ;;
    esac
    # Real class declaration inheriting GameComponent (not a doc comment).
    if grep -qE '\bclass\s+\w+\b[^{]*GameComponent\b' "$f"; then
        # Extract package directory name (e.g. 05-Rimconemy-Infected-Automation)
        # via parameter expansion — more robust than sed across bash variants.
        _tmp="${f#*/mods/}"
        pkg="${_tmp%%/*}"
        GC_BY_PKG["$pkg"]=$(( ${GC_BY_PKG["$pkg"]:-0} + 1 ))
        # Canonical ctor: indented `public Xxx(Game game)` on its own line.
        # Anchored to start-of-line so `// public ...` comment lines
        # don't satisfy it.
        if ! grep -qE '^[[:space:]]*public\s+\w+\s*\(\s*Game\s+\w+\s*\)' "$f"; then
            fail "GameComponent ctor gap: $f declares : GameComponent but no public Xxx(Game X) ctor"
            GC_GAPS+=("$f")
        fi
    fi
done < <(find "$PROJECT_ROOT/mods" -name '*.cs' -not -path '*/Tests/*' 2>/dev/null)

if [[ ${#GC_GAPS[@]} -eq 0 ]]; then
    if [[ ${#GC_BY_PKG[@]} -gt 0 ]]; then
        summary=""
        for p in $(printf '%s\n' "${!GC_BY_PKG[@]}" | sort); do
            summary+="$p:${GC_BY_PKG[$p]} "
        done
        pass "GameComponent (Game)-ctor consistent (subclasses — ${summary% })"
    else
        warn "no GameComponent subclasses found in production code (gate not exercised)"
    fi
fi

# 4. P0 Coal Chain cross-references (if files exist)
echo
echo "--- P0 Coal Chain Cross-References ---"
P0_DEFS=(
    "Rimconemy_Coal"
    "Rimconemy_MachineParts"
    "Rimconemy_CraftingStations"
    "Rimconemy_MakeCoal"
    "Rimconemy_SalvageMachineParts"
    "Rimconemy_BurnSteelScraps"
    "Rimconemy_Campfire"
    "Rimconemy_WoodCoalGenerator"
)

for def in "${P0_DEFS[@]}"; do
    if grep -r "defName>$def<" "$PROJECT_ROOT/mods/03-Rimconemy-Scavenger-Infrastructure/Defs" >/dev/null 2>&1; then
        pass "P0 def: $def"
    else
        fail "P0 def MISSING: $def"
    fi
done

# 5. Regression test signatures in Player.log (if log exists)
if [[ -f "$LOG_PATH" ]]; then
    echo
    echo "--- Runtime Regression Signatures ---"
    REQUIRED_SUMMARIES=(
        'CapabilityGate tests: [0-9]+ passed, 0 failed'
        'CreditsLedger regression tests: [0-9]+ passed, 0 failed'
        'StorySelector tests: [0-9]+ passed, 0 failed'
        'BuildingCore regression tests: [0-9]+ passed, 0 failed'
        'Profile detector dedup tests: [0-9]+ passed, 0 failed'
        'CampfireScraps regression tests: [0-9]+ passed, 0 failed'
        'BauschuttRemapApply tests: [0-9]+ passed, 0 failed'
        'ArrowTurretBlock tests: [0-9]+ passed, 0 failed'
        'CoalChain regression tests: [0-9]+ passed, 0 failed'
        'StainlessSteelChain regression tests: [0-9]+ passed, 0 failed'
    )
    for summary in "${REQUIRED_SUMMARIES[@]}"; do
        has_match "$summary" "$LOG_PATH" && pass "summary: $summary" || warn "summary missing: $summary"
    done

    # Forbidden patterns
    FORBIDDEN=(
        'Config error in Rimconemy_Campfire'
        'XML error:.*showInInterface'
        'XML error:.*surfacePosition'
        'XML error:.*defaultIngredientCount'
    )
    for pattern in "${FORBIDDEN[@]}"; do
        if has_match "$pattern" "$LOG_PATH"; then
            fail "Forbidden pattern in log: $pattern"
        else
            pass "Forbidden absent: $pattern"
        fi
    done
else
    warn "Player.log not found at $LOG_PATH (skip runtime signatures)"
fi

# 6. Strict mode: run XML verification script if available
if [[ "$STRICT_MODE" == true ]]; then
    echo
    echo "--- Strict Mode: XML Verification Script ---"
    if [[ -f "$SCRIPT_DIR/verify_bootstrap_log.sh" ]]; then
        if "$SCRIPT_DIR/verify_bootstrap_log.sh" "$LOG_PATH"; then
            pass "verify_bootstrap_log.sh"
        else
            fail "verify_bootstrap_log.sh failed"
        fi
    else
        warn "verify_bootstrap_log.sh not found"
    fi
fi

# Summary
echo
echo "=== SUMMARY ==="
echo "Failures: $FAILURES"
echo "Warnings: $WARNINGS"
[[ "$FAILURES" -eq 0 ]] && { echo "RESULT: PASS"; exit 0; } || { echo "RESULT: FAIL"; exit 1; }