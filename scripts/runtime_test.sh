#!/usr/bin/env bash
# runtime_test.sh — Repeatable Rimconemy runtime verification.
#
# Default: build/deploy all packages, start RimWorld for a bounded interval,
# then evaluate only the freshly generated Player.log.
#
# Usage:
#   ./scripts/runtime_test.sh
#   ./scripts/runtime_test.sh --no-build
#   ./scripts/runtime_test.sh --skip-start --no-deploy
#   ./scripts/runtime_test.sh --require-scenario-tests

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DEPLOY_SCRIPT="$SCRIPT_DIR/deploy.sh"
DEFAULT_GAME="/home/vannon/GOG Games/RimWorld/game"
DEFAULT_LOG="$HOME/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log"
DEFAULT_REPORT_DIR="$PROJECT_ROOT/.runtime-reports"

GAME_BASE="$DEFAULT_GAME"
LOG_PATH="$DEFAULT_LOG"
REPORT_DIR="$DEFAULT_REPORT_DIR"
START_TIMEOUT=90
DO_DEPLOY=true
DO_BUILD=true
DO_START=true
REQUIRE_SCENARIO_TESTS=false

PACKAGES=(
  "01-Rimconemy-Foundation|rimconemy.foundation"
  "02-Rimconemy-Survival-Progression|rimconemy.survivalprogression"
  "03-Rimconemy-Scavenger-Infrastructure|rimconemy.scavengerinfrastructure"
  "04-Rimconemy-Economy-Territory|rimconemy.economyterritory"
  "05-Rimconemy-Infected-Automation|rimconemy.infectedautomation"
)

RUN_ID="$(date +%Y%m%d-%H%M%S)"
REPORT_PATH=""
FAILURES=0
WARNINGS=0
FAIL_DETAILS=()
WARN_DETAILS=()

usage() {
  cat <<'EOF'
Usage: scripts/runtime_test.sh [options]

Runs the canonical Rimconemy deployment, starts RimWorld for a bounded time,
and evaluates the fresh Player.log. RimWorld/Core files are never modified.

Options:
  --no-deploy             Do not build or deploy; inspect installed mods only.
  --no-build              Deploy with scripts/deploy.sh --no-build.
  --skip-start            Do not start RimWorld; run static installed-mod gates.
  --timeout SEC           Maximum RimWorld runtime (default: 90).
  --game PATH             RimWorld game directory (default: local GOG install).
  --log PATH              Player.log path (default: Unity Linux log path).
  --report-dir PATH       Directory for the compact verification report.
  --require-scenario-tests
                          Require the Scenario contract summary in Player.log.
  -h, --help              Show this help.
EOF
}

fail() {
  echo "FAIL: $*" >&2
  FAIL_DETAILS+=("$*")
  FAILURES=$((FAILURES + 1))
}

warn() {
  echo "WARN: $*" >&2
  WARN_DETAILS+=("$*")
  WARNINGS=$((WARNINGS + 1))
}

pass() {
  echo "PASS: $*"
}

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

file_signature() {
  local path="$1"
  if [[ -e "$path" ]]; then
    local metadata
  metadata=$(stat -Lc '%i|%s|%y' "$path" 2>/dev/null || stat -c '%i|%s|%y' "$path")
  if command_exists sha256sum; then
    printf '%s|%s' "$metadata" "$(sha256sum "$path" | awk '{print $1}')"
  else
    echo "$metadata"
  fi
  else
    echo "missing"
  fi
}

has_match() {
  local pattern="$1"
  local path="$2"
  if command_exists rg; then
    rg -qi -- "$pattern" "$path"
  else
    grep -Eiq -- "$pattern" "$path"
  fi
}

log_matches() {
  local pattern="$1"
  local path="$2"
  if command_exists rg; then
    rg -n -i -C 1 -- "$pattern" "$path" 2>/dev/null | tail -80 || true
  else
    grep -Ein -C 1 -- "$pattern" "$path" 2>/dev/null | tail -80 || true
  fi
}

xml_value() {
  local tag="$1"
  local path="$2"
  sed -n "s:.*<$tag>\\([^<]*\\)</$tag>.*:\\1:p" "$path" | head -1
}

parse_args() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --no-deploy) DO_DEPLOY=false; shift ;;
      --no-build) DO_BUILD=false; shift ;;
      --skip-start) DO_START=false; shift ;;
      --require-scenario-tests) REQUIRE_SCENARIO_TESTS=true; shift ;;
      --timeout)
        [[ $# -ge 2 ]] || { echo "--timeout requires seconds" >&2; return 2; }
        START_TIMEOUT="$2"; shift 2 ;;
      --game)
        [[ $# -ge 2 ]] || { echo "--game requires a path" >&2; return 2; }
        GAME_BASE="$2"; shift 2 ;;
      --log)
        [[ $# -ge 2 ]] || { echo "--log requires a path" >&2; return 2; }
        LOG_PATH="$2"; shift 2 ;;
      --report-dir)
        [[ $# -ge 2 ]] || { echo "--report-dir requires a path" >&2; return 2; }
        REPORT_DIR="$2"; shift 2 ;;
      -h|--help) usage; exit 0 ;;
      *) echo "Unknown option: $1" >&2; usage >&2; return 2 ;;
    esac
  done

  [[ "$START_TIMEOUT" =~ ^[1-9][0-9]*$ ]] || {
    echo "--timeout must be a positive integer" >&2; return 2;
  }
}

validate_environment() {
  [[ -f "$DEPLOY_SCRIPT" ]] || { fail "missing deploy script: $DEPLOY_SCRIPT"; return; }
  [[ -d "$GAME_BASE" ]] || { fail "missing RimWorld game directory: $GAME_BASE"; return; }
  if [[ "$DO_START" == true ]]; then
    [[ -x "$GAME_BASE/../start.sh" ]] || {
      fail "missing executable start script: $GAME_BASE/../start.sh";
    }
    command_exists timeout || fail "required command not found: timeout";
  fi
  command_exists stat || fail "required command not found: stat"
  command_exists sha256sum || warn "sha256sum not found; fresh-log check uses metadata only"
  if [[ "$DO_DEPLOY" == true && "$GAME_BASE" != "$DEFAULT_GAME" ]]; then
    fail "custom --game requires --no-deploy because deploy.sh has the canonical fixed target"
  fi
  if [[ "$DO_DEPLOY" == true && ! -x "$DEPLOY_SCRIPT" ]]; then
    fail "deploy script is not executable: $DEPLOY_SCRIPT"
  fi
}

static_installed_gates() {
  local mods_dir="$GAME_BASE/Mods"
  local entry package_dir package_id about dll supported

  for entry in "${PACKAGES[@]}"; do
    package_dir="${entry%%|*}"
    package_id="${entry##*|}"
    about="$mods_dir/$package_dir/About/About.xml"
    dll=$(find "$mods_dir/$package_dir/Assemblies" -maxdepth 1 -type f -name 'Rimconemy.*.dll' -print -quit 2>/dev/null)

    if [[ ! -f "$about" ]]; then
      fail "$package_id: About.xml missing"
      continue
    fi
    if [[ "$(xml_value packageId "$about")" != "$package_id" ]]; then
      fail "$package_id: About.xml packageId mismatch"
    else
      pass "$package_id: packageId"
    fi
    supported=$(grep -c '<li>1\.6</li>' "$about" 2>/dev/null || true)
    if [[ "$supported" -lt 1 ]]; then
      fail "$package_id: supportedVersions does not contain 1.6"
    else
      pass "$package_id: supportedVersions 1.6"
    fi
    if [[ -z "$dll" ]]; then
      fail "$package_id: Rimconemy assembly missing"
    else
      pass "$package_id: assembly present ($(basename "$dll"))"
    fi
  done
}

write_report_header() {
  mkdir -p "$REPORT_DIR" || { fail "cannot create report directory: $REPORT_DIR"; return; }
  REPORT_PATH="$REPORT_DIR/runtime-$RUN_ID.txt"
  {
    echo "Rimconemy runtime test"
    echo "run=$RUN_ID"
    echo "game=$GAME_BASE"
    echo "log=$LOG_PATH"
    echo "timeout=$START_TIMEOUT"
    echo "deploy=$DO_DEPLOY build=$DO_BUILD start=$DO_START"
    echo "before_log_signature=$BEFORE_LOG_SIGNATURE"
    echo
  } > "$REPORT_PATH" || fail "cannot write report: $REPORT_PATH"
}

run_deploy() {
  [[ "$DO_DEPLOY" == true ]] || { pass "deployment skipped"; return; }
  local deploy_args=(--all)
  [[ "$DO_BUILD" == true ]] || deploy_args=(--no-build --all)
  echo "Running canonical deployment: $DEPLOY_SCRIPT ${deploy_args[*]}"
  if "$DEPLOY_SCRIPT" "${deploy_args[@]}"; then
    pass "canonical deployment"
  else
    fail "canonical deployment failed"
  fi
}

run_game() {
  [[ "$DO_START" == true ]] || { pass "RimWorld start skipped"; return; }
  local start_script="$GAME_BASE/../start.sh"
  echo "Starting RimWorld for up to ${START_TIMEOUT}s"
  timeout --signal=TERM --kill-after=10s "${START_TIMEOUT}s" "$start_script" >/tmp/rimconemy-runtime-start-$RUN_ID.log 2>&1
  local rc=$?
  case "$rc" in
    0|124|143) pass "RimWorld bounded start (exit=$rc)" ;;
    *)
      fail "RimWorld exited immediately with exit=$rc"
      tail -80 "/tmp/rimconemy-runtime-start-$RUN_ID.log" >&2 || true
      ;;
  esac
}

verify_bootstrap_log_gate() {
  [[ "$DO_START" == true ]] || { pass "verify_bootstrap_log: skipped (no RimWorld boot)"; return; }
  local verify_script="$SCRIPT_DIR/verify_bootstrap_log.sh"
  if [[ ! -x "$verify_script" ]]; then
    fail "verify_bootstrap_log: script missing or not executable ($verify_script)"
    return
  fi
  if "$verify_script" "$LOG_PATH"; then
    pass "verify_bootstrap_log: ProfileDetector dedup invariants hold"
  else
    fail "verify_bootstrap_log: invariant violation; see diagnostics above"
  fi
}

runtime_gates() {
  [[ "$DO_START" == true ]] || { pass "runtime log gates skipped"; return; }
  if [[ ! -f "$LOG_PATH" ]]; then
    fail "Player.log missing after start: $LOG_PATH"
    return
  fi

  AFTER_LOG_SIGNATURE="$(file_signature "$LOG_PATH")"
  if [[ "$AFTER_LOG_SIGNATURE" == "$BEFORE_LOG_SIGNATURE" ]]; then
    fail "Player.log signature did not change; fresh runtime evidence unavailable"
    return
  else
    pass "fresh Player.log generated"
  fi

  local forbidden_pattern='CA9011A3|Cannot create an instance of RimWorld\.Need'
    forbidden_pattern+='|Error while determining if .* should have Need .*abstract class'
    forbidden_pattern+='|Config error in Rimconemy_SandboxScenario|no playerFaction|no surfaceLayer|scenario has null part|ScenPart_StartInSandbox has null def'
    forbidden_pattern+='|XML error: .*PatchOperationTest|doesn.t correspond to any field in type PatchOperationTest|<case Class='
    forbidden_pattern+='|is not a valid value for Verse\.ThingCategory'
    forbidden_pattern+='|Exception loading def from file '
    forbidden_pattern+='|XML error: .*thingDef.*doesn.t correspond'
    forbidden_pattern+='|XML error: .*count.*doesn.t correspond'
    forbidden_pattern+='|Exception.*marketSnapshot|marketSnapshot.*(Scribe|IExposable|LookDeep)'
  if has_match "$forbidden_pattern" "$LOG_PATH"; then
    fail "forbidden runtime errors found in Player.log"
    log_matches "$forbidden_pattern" "$LOG_PATH"
  else
    pass "Need/Sandbox/Patch/Market forbidden errors absent"
  fi

  if has_match 'RimWorld 1\.6\.' "$LOG_PATH"; then pass "RimWorld 1.6 runtime marker"; else fail "RimWorld 1.6 runtime marker missing"; fi
  if has_match '\[Rimconemy\.Foundation\] Bootstrap complete\.' "$LOG_PATH"; then pass "Foundation bootstrap complete"; else fail "Foundation bootstrap marker missing"; fi
  if has_match 'Profile detected: FullOverhaul' "$LOG_PATH"; then pass "FullOverhaul profile"; else fail "FullOverhaul profile missing"; fi
  if has_match 'Registry: 5 package\(s\) registered' "$LOG_PATH"; then pass "five packages registered"; else fail "registry did not report five packages"; fi
  local required_summaries=(
      'CapabilityGate tests: [0-9]+ passed, 0 failed'
      'ColonialReader tests: [0-9]+ passed, 0 failed'
      'CrossPackageState tests: [0-9]+ passed, 0 failed'
      'EventLog regression tests: [0-9]+ passed, 0 failed'
      'Profile refresh tests: [0-9]+ passed, 0 failed'
      'Profile detector dedup tests: [0-9]+ passed, 0 failed'
      'BioRemap tests: [0-9]+ passed, 0 failed'
      'NeedMappingService tests: [0-9]+ passed, 0 failed'
      'DomainXpState tests: [0-9]+ passed, 0 failed'
      'UnlockService tests: [0-9]+ passed, 0 failed'
      'BuildingCompletionBridge tests: [0-9]+ passed, 0 failed'
      'SchemaBump tests: [0-9]+ passed, 0 failed'
      'ThreatSnapshotBridge regression tests: [0-9]+ passed, 0 failed'
      'StartEnemies regression tests: [0-9]+ passed, 0 failed'
      'Canonical layer tests: [0-9]+ passed, 0 failed'
      'Honest-Banner-Audit tests: [0-9]+/[0-9]+ passed, 0 failed'
      'CreditsLedger regression tests: [0-9]+ passed, 0 failed'
      'Market persistence tests: [0-9]+ passed, 0 failed'
      'StorySelector tests: [0-9]+ passed, 0 failed'
      'StoryState regression tests: [0-9]+ passed, 0 failed'
      'TutorialDirector tests: [0-9]+ passed, 0 failed'
      'PopulationLedger regression tests \(Phase A subset\): [0-9]+ passed, 0 failed'
      'Revenge-quota flow regression tests: [0-9]+ passed, 0 failed'
      'HordeManifest tests: [0-9]+ passed, 0 failed'
      'Building capability tests: [0-9]+ passed, 0 failed'
      'Building progression regression tests: [0-9]+ passed, 0 failed'
      'Building progression persistence tests: [0-9]+ passed, 0 failed'
      'BuildingCore regression tests: [0-9]+ passed, 0 failed'
      'Building input regression tests: [0-9]+ passed, 0 failed'
      'Physical transfer regression tests: [0-9]+ passed, 0 failed'
      'Outpost investment regression tests: [0-9]+ passed, 0 failed'
      'Building threat regression tests: [0-9]+ passed, 0 failed'
      'Mechadroid job regression tests: [0-9]+ passed, 0 failed'
      'CampfireScraps regression tests: [0-9]+ passed, 0 failed'
      'BauschuttRemapApply tests: [0-9]+ passed, 0 failed'
      'ArrowTurretBlock tests: [0-9]+ passed, 0 failed'
      'CoalChain regression tests: [0-9]+ passed, 0 failed'
      'StainlessSteelChain regression tests: [0-9]+ passed, 0 failed'
      'PhaseProgress regression tests: [0-9]+ passed, 0 failed'
    )
    local summary
    for summary in "${required_summaries[@]}"; do
      if has_match "$summary" "$LOG_PATH"; then pass "summary: $summary"; else fail "missing summary: $summary"; fi
    done
    if [[ "$REQUIRE_SCENARIO_TESTS" == true ]]; then
      if has_match 'Scenario contract tests: [0-9]+ passed, 0 failed' "$LOG_PATH"; then
        pass "Scenario contract summary"
      else
        fail "Scenario contract summary missing"
      fi
    else
      warn "Scenario contract summary not required; use --require-scenario-tests"
    fi

    # Flexible summary detection: handle "X passed, 0 failed (expected=N)" and "X/Y passed" formats
    # Collect all summary-like lines and evaluate them
    local summary_lines
    summary_lines=$(grep -E '^\\[Rimconemy[^]]*\\].*tests:' "$LOG_PATH" || true)
    local detected_failed=false
    while IFS= read -r line; do
      [[ -z "$line" ]] && continue
      # Pattern 1: "X passed, 0 failed" (with optional "(expected=N)")
      if echo "$line" | grep -Eq 'tests: [0-9]+ passed, 0 failed'; then
        # Success - explicit 0 failed
        :
      # Pattern 2: "X passed, Y failed" where Y > 0
      elif echo "$line" | grep -Eq 'tests: [0-9]+ passed, [1-9][0-9]* failed'; then
        fail "summary shows failures: $line"
        detected_failed=true
      # Pattern 3: "X/Y passed" - success only if X == Y
      elif echo "$line" | grep -Eq 'tests: [0-9]+/[0-9]+ passed'; then
        local x y
        x=$(echo "$line" | sed -E 's/.*tests: ([0-9]+)\/([0-9]+) passed.*/\1/')
        y=$(echo "$line" | sed -E 's/.*tests: ([0-9]+)\/([0-9]+) passed.*/\2/')
        if [[ "$x" -eq "$y" ]]; then
          : # success
        else
          fail "summary shows partial pass: $line (X=$x, Y=$y)"
          detected_failed=true
        fi
      # Pattern 4: "X passed, 0 failed (expected=N)" - explicit 0 failed is success
      elif echo "$line" | grep -Eq 'tests: [0-9]+ passed, 0 failed \(expected=[0-9]+\)'; then
        : # success
      else
        # Unknown format - warn but don't fail
        warn "unrecognized summary format: $line"
      fi
    done <<< "$summary_lines"

    # Do not match successful summaries such as "0 failed". Actual
    # Rimconemy failures use Log.Error/Log.Warning stack markers with an
    # uppercase diagnostic token (Exception, FAILED, or Error) on the primary
    # Rimconemy line. The anchored package prefix avoids Unity/vanilla noise.
    local runtime_error_pattern='^\\[Rimconemy[^]]*\\].*(Exception|FAILED|Error|error|exception|failed)'
    local runtime_summary_pattern='^\\[Rimconemy[^]]*\\].*tests: [0-9]+ passed, [0-9]+ failed\\.$'
    local runtime_summary_pattern2='^\\[Rimconemy[^]]*\\].*tests: [0-9]+/[0-9]+ passed\\.$'
    # This check is intentionally case-sensitive: successful summaries contain
    # the lowercase word "failed" in "0 failed" and must not be treated as
    # runtime diagnostics.
    # Exclude only canonical successful test summaries; inspect all other
    # Rimconemy primary lines case-insensitively for real diagnostics.
    local runtime_diagnostics
    runtime_diagnostics=$(grep -Ei -- "$runtime_error_pattern" "$LOG_PATH" | grep -Eiv -- "$runtime_summary_pattern|$runtime_summary_pattern2" || true)
    if [[ -n "$runtime_diagnostics" ]]; then
      fail "Rimconemy error/exception/failure marker found"
      printf '%s\n' "$runtime_diagnostics" | tail -80
    else
      pass "no Rimconemy error/exception/failure markers"
    fi

  # Bootstrap-log dedup invariants (ProfileDetector dedup token must hold).
  # See scripts/verify_bootstrap_log.sh and
  # docs/falsification/foundation__BootstrapLogDedup.md for the contract.
  verify_bootstrap_log_gate
}

RESULT="${RESULT:-PASS}"
finish_report() {
  [[ -n "$REPORT_PATH" ]] || return
  {
    echo "after_log_signature=${AFTER_LOG_SIGNATURE:-not-run}"
    echo "failures=$FAILURES"
    echo "warnings=$WARNINGS"
    echo "result=$([[ "$FAILURES" -eq 0 ]] && echo PASS || echo FAIL)"
    echo "failure_details_begin"
    for d in "${FAIL_DETAILS[@]}"; do echo "$d"; done
    echo "failure_details_end"
    echo "warning_details_begin"
    for d in "${WARN_DETAILS[@]}"; do echo "$d"; done
    echo "warning_details_end"
  } >> "$REPORT_PATH"
  echo "Report: $REPORT_PATH"
}

main() {
  parse_args "$@" || return 2
  BEFORE_LOG_SIGNATURE="$(file_signature "$LOG_PATH")"
  validate_environment
  write_report_header
  [[ "$FAILURES" -eq 0 ]] || { finish_report; return 1; }
  run_deploy
  [[ "$FAILURES" -eq 0 ]] || { finish_report; return 1; }
  static_installed_gates
  [[ "$FAILURES" -eq 0 ]] || { finish_report; return 1; }
  run_game
  runtime_gates
  finish_report

  # ── Parser Preflight: validates parser_config.json completeness ──
  local preflight_script="$SCRIPT_DIR/test_parser_preflight.py"
  if [[ -f "$preflight_script" ]] && command_exists python3; then
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "🔬 Parser Preflight (parser_config.json)..."
    if python3 "$preflight_script" --log "$LOG_PATH" 2>&1; then
      pass "parser preflight: all patterns found"
    else
      fail "parser preflight: missing patterns (update scripts/parser_config.json)"
    fi
  fi

  # ── Structured debug summary (parse_runtime_log.py) ──
  local parser_script="$SCRIPT_DIR/parse_runtime_log.py"
  if [[ -f "$parser_script" ]] && command_exists python3; then
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "📊 Generating structured debug summary..."
    python3 "$parser_script" --log "$LOG_PATH" 2>&1 || true
  fi

  if [[ "$FAILURES" -eq 0 ]]; then
    echo "Runtime test result: PASS (warnings=$WARNINGS)"
    return 0
  fi
  echo "Runtime test result: FAIL (failures=$FAILURES warnings=$WARNINGS)" >&2
  return 1
}

main "$@"
