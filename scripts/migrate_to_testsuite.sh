#!/usr/bin/env bash
# Restore and run the TestSuite migration tool.
# Default is dry-run; pass --write to modify test files.
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec python3 "$SCRIPT_DIR/migrate_to_testsuite.py" "$@"
