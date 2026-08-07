#!/usr/bin/env bash
# Wrapper around the Python migration library.
# Usage: ./scripts/migrate_to_testsuite.sh <file.cs | --all> [--write]
# See: /tmp/migrate_to_testsuite.py for details.
exec python3 "$(dirname "$0")/migrate_to_testsuite_py.py" "$@"
