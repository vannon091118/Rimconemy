#!/usr/bin/env bash
# bump_version.sh — Bumped die VERSION-Datei eines Rimconemy-Pakets um +0.0.1
#
# Usage:
#   ./scripts/bump_version.sh 01   # Bumped mods/01-Rimconemy-Foundation/VERSION
#   ./scripts/bump_version.sh 05   # Bumped mods/05-Rimconemy-Infected-Automation/VERSION
#   ./scripts/bump_version.sh --all # Bumped ALLE 5 Pakete
#
# Konvention:
#   Nach JEDER Code- oder Def-Änderung an einem Paket wird dessen VERSION
#   um +0.0.1 gepumpt. Dies geschieht pro Paket individuell.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

bump_one() {
    local pkg_num="$1"
    # We deliberately accept FOUNDATION_PACKAGE_ID as the only first-class
    # pacakge and read the rest from the loop. Foundation is special because
    # it embeds its own version string in PackageRegistry.cs at static init
    # and the registry is the sole source of truth for cross-package READs.
    local pkg_dir
    pkg_dir=$(ls -d "$PROJECT_ROOT/mods/${pkg_num}"-* 2>/dev/null | head -1)

    if [ -z "$pkg_dir" ]; then
        echo "❌ Kein Paket mit Nummer ${pkg_num} gefunden."
        return 1
    fi

    local version_file="$pkg_dir/VERSION"
    if [ ! -f "$version_file" ]; then
        echo "❌ Keine VERSION-Datei in $(basename "$pkg_dir")"
        return 1
    fi

    local pkg_name
    pkg_name=$(basename "$pkg_dir")

    local old_version
    old_version=$(cat "$version_file")

    # Parse MAJOR.MINOR.PATCH
    local major minor patch
    IFS='.' read -r major minor patch <<< "$old_version"

    local new_patch=$((patch + 1))
    local new_version="${major}.${minor}.${new_patch}"

    echo "$new_version" > "$version_file"
    echo "✅ ${pkg_name}: ${old_version} → ${new_version}"
    sync_registry_version "$pkg_dir" "$new_version"
}

# Synchronise the hardcoded packageVersion literal that PackageRegistry.cs
# registers for feature assemblies at StaticConstructorOnStartup time.
# Without this the registry claims a stale version forever (P0/§5 of
# 2026-08-04 audit-round-3: -3 bump drift across all five packages).
#
# Foundation itself lives in the same file (PackageRegistry.static ctor).
# Feature packages are registered via TryRegisterLoadedAssembly which lives
# one synchronous block lower; both lines we patch in a single regex edit.
sync_registry_version() {
    local pkg_dir="$1"
    local new_version="$2"
    local registry_file="$PROJECT_ROOT/mods/01-Rimconemy-Foundation/Source/Registry/PackageRegistry.cs"
    if [ ! -f "$registry_file" ]; then
        echo "⏭️  PackageRegistry.cs nicht gefunden - Registry-Sync übersprungen"
        return 0
    fi

    local pkg_short
    pkg_short=$(basename "$pkg_dir")
    # Map "01-Rimconemy-Foundation" -> "rimconemy.foundation"
    local package_id
    case "$pkg_short" in
        01-Rimconemy-Foundation)        package_id="rimconemy.foundation" ;;
        02-Rimconemy-Survival-Progression) package_id="rimconemy.survivalprogression" ;;
        03-Rimconemy-Scavenger-Infrastructure) package_id="rimconemy.scavengerinfrastructure" ;;
        04-Rimconemy-Economy-Territory)  package_id="rimconemy.economyterritory" ;;
        05-Rimconemy-Infected-Automation) package_id="rimconemy.infectedautomation" ;;
        *) echo "⏭️  Unbekannte Package-Form: $pkg_short" ; return 0 ;;
    esac

    # Locate the literal packageVersion line right after the matching
    # packageId line, then rewrite it. The format is one of:
    #   packageVersion: "0.1.14",
    # in PackageDescriptor ctor invocation.
    if grep -q "packageId: \"$package_id\"" "$registry_file"; then
        # Find line-numbers of the packageId mark; replace the NEXT
        # packageVersion line below. We use a Python one-liner for
        # controlled in-place rewriting because sed on different hosts
        # behaves differently around groups.
        python3 - <<PY
import re, sys
reg = "$registry_file"
pkg = "$package_id"
new_v = "$new_version"
text = open(reg, 'r', encoding='utf-8').read()
# Match the packageId block, then capture the packageVersion that follows.
pat = re.compile(
    r'(packageId:\s*"' + re.escape(pkg) + r'"\s*,\s*\n\s*packageVersion:\s*")([0-9A-Za-z\.\-]+)(")',
    flags=re.MULTILINE)
m = pat.search(text)
if not m:
    print(f"⏭️  Kein packageVersion-Block fuer {pkg} gefunden")
    sys.exit(0)
new_text = pat.sub(lambda mt: mt.group(1) + new_v + mt.group(3), text, count=1)
open(reg, 'w', encoding='utf-8').write(new_text)
print(f"✅ Registry-Sync: {pkg} v{ new_v }")
PY
    else
        echo "⏭️  packageId \"$package_id\" nicht in PackageRegistry.cs gefunden - Sync übersprungen"
    fi
}

bump_all() {
    for num in 01 02 03 04 05; do
        bump_one "$num"
    done
}

# ── Main ────────────────────────────────────────────────

if [ $# -eq 0 ]; then
    echo "Usage: $0 <NR|--all>"
    echo "  NR     : Paketnummer (01–05)"
    echo "  --all  : Alle 5 Pakete bumpen"
    exit 1
fi

for arg in "$@"; do
    case "$arg" in
        --all)
            bump_all
            ;;
        0[1-5])
            bump_one "$arg"
            ;;
        *)
            echo "❌ Unbekannte Option: $arg"
            echo "   Erwartet: 01, 02, 03, 04, 05 oder --all"
            exit 1
            ;;
    esac
done
