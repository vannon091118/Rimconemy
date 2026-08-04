#!/usr/bin/env bash
# deploy.sh — Baut alle Rimconemy-Pakete und deployt sie in die RimWorld Mods.
#
# Usage:
#   ./scripts/deploy.sh           # Alle 5 Pakete bauen + deployen
#   ./scripts/deploy.sh 05        # Nur Paket 05 bauen + deployen
#   ./scripts/deploy.sh --no-build # Nur kopieren, nicht bauen
#
# Voraussetzungen:
#   - dotnet SDK
#   - RimWorld 1.6 Installation unter RIMWORLD_MODS/..
#   - Harmony Mod unter RIMWORLD_MODS/Harmony/

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
MODS_SRC="$PROJECT_ROOT/mods"

# ── Pfade zur RimWorld-Installation ─────────────────────
RIMWORLD_BASE="/home/vannon/GOG Games/RimWorld/game"
RIMWORLD_MODS="$RIMWORLD_BASE/Mods"
RIMWORLD_MANAGED="$RIMWORLD_BASE/RimWorldLinux_Data/Managed"
HARMONY_ASSEMBLIES="$RIMWORLD_MODS/Harmony/Current/Assemblies"

# ── Build-Parameter ─────────────────────────────────────
DOTNET_BUILD_ARGS=(
    -c Release
    -p:RimWorldManagedPath="$RIMWORLD_MANAGED"
    -p:HarmonyAssembliesPath="$HARMONY_ASSEMBLIES"
)

# ── Rsync-Ausschlüsse (weder Source-Code noch Build-Müll deployen) ──
#
# audit-round-3 §8 (2026-08-04): '*.deps.json' is now explicitly excluded.
# Rationale: Mono / RimWorld 1.6 does not consume .NET Core deps.json at
# runtime (assembly resolution happens via the GAC + mod-folder loading).
# Shipping deps.json duplicates 6-10 KB per package and was historically
# diagnosted as a problem in the ii.zip-era HANDOFF ("local mod copy
# contains no deps.json"). Documenting the exclusion here so the next
# agent or reviewer sees the explicit policy rather than an oversight.
RSYNC_EXCLUDES=(
    --exclude='Source/'
    --exclude='obj/'
    --exclude='.build/'
    --exclude='.buildV2/'
    --exclude='.buildV3/'
    --exclude='*.csproj'
    --exclude='Tests/'
    --exclude='.gitkeep'
    --exclude='mods/'
    --exclude='Assemblies/*.deps.json'
)

RSYNC_ARGS=(
    -av
    --delete
    "${RSYNC_EXCLUDES[@]}"
)

# ══════════════════════════════════════════════════════════
# Build eines einzelnen Pakets
# ══════════════════════════════════════════════════════════
build_one() {
    local pkg_num="$1"
    local pkg_dir
    pkg_dir=$(ls -d "$MODS_SRC/${pkg_num}"-* 2>/dev/null | head -1)

    if [ -z "$pkg_dir" ]; then
        echo "❌ Kein Paket mit Nummer ${pkg_num} gefunden."
        return 1
    fi

    local pkg_name
    pkg_name=$(basename "$pkg_dir")

    local csproj
    csproj=$(ls "$pkg_dir"/*.csproj 2>/dev/null | head -1)

    if [ -z "$csproj" ] || [ ! -f "$csproj" ]; then
        echo "⚠️  Kein .csproj in ${pkg_name} — überspringe Build."
        return 0
    fi

    echo ""
    echo "🔨 Baue ${pkg_name} …"
    dotnet build "$csproj" "${DOTNET_BUILD_ARGS[@]}" \
        | grep -E '(error|warning|Build succeeded|Build FAILED)' \
        || true

    if [ ${PIPESTATUS[0]} -ne 0 ]; then
        echo "❌ Build fehlgeschlagen für ${pkg_name}"
        return 1
    fi
    echo "✅ Build erfolgreich: ${pkg_name}"
}

# ══════════════════════════════════════════════════════════
# Deploy eines einzelnen Pakets (rsync in RimWorld Mods)
# ══════════════════════════════════════════════════════════
deploy_one() {
    local pkg_num="$1"
    local pkg_src
    pkg_src=$(ls -d "$MODS_SRC/${pkg_num}"-* 2>/dev/null | head -1)

    if [ -z "$pkg_src" ]; then
        echo "❌ Kein Quell-Paket mit Nummer ${pkg_num} gefunden."
        return 1
    fi

    local pkg_name
    pkg_name=$(basename "$pkg_src")
    local pkg_target="$RIMWORLD_MODS/$pkg_name"

    if [ ! -d "$pkg_target" ]; then
        echo "❌ Zielverzeichnis existiert nicht: $pkg_target"
        echo "   Bitte erstelle es zuerst oder prüfe die RimWorld-Installation."
        return 1
    fi

    local version
    version=$(cat "$pkg_src/VERSION" 2>/dev/null || echo "?")

    echo ""
    echo "📦 Deploye ${pkg_name} (v${version}) → $(basename "$RIMWORLD_MODS")/"

    rsync "${RSYNC_ARGS[@]}" "$pkg_src/" "$pkg_target/"

    echo "✅ Deploy abgeschlossen: ${pkg_name}"
}

# ══════════════════════════════════════════════════════════
# Build + Deploy eines Pakets
# ══════════════════════════════════════════════════════════
build_and_deploy_one() {
    local pkg_num="$1"
    build_one "$pkg_num" || return 1
    deploy_one "$pkg_num"
}

# ══════════════════════════════════════════════════════════
# Main
# ══════════════════════════════════════════════════════════

DO_BUILD=true
PKGS=()

for arg in "$@"; do
    case "$arg" in
        --no-build)
            DO_BUILD=false
            ;;
        0[1-5])
            PKGS+=("$arg")
            ;;
        --all|all)
            PKGS=(01 02 03 04 05)
            ;;
        *)
            echo "❌ Unbekannte Option: $arg"
            echo "Usage: $0 [--no-build] [01|02|03|04|05|--all]"
            exit 1
            ;;
    esac
done

# Default: alle 5
if [ ${#PKGS[@]} -eq 0 ]; then
    PKGS=(01 02 03 04 05)
fi

echo "╔══════════════════════════════════════════════════╗"
echo "║  Rimconemy Deploy                                ║"
echo "║  Ziel: $RIMWORLD_MODS"
echo "║  Build: $DO_BUILD"
echo "║  Pakete: ${PKGS[*]}"
echo "╚══════════════════════════════════════════════════╝"

FAILED=()

for pkg in "${PKGS[@]}"; do
    if $DO_BUILD; then
        build_and_deploy_one "$pkg" || FAILED+=("$pkg")
    else
        deploy_one "$pkg" || FAILED+=("$pkg")
    fi
done

echo ""
echo "══════════════════════════════════════════════════"
if [ ${#FAILED[@]} -eq 0 ]; then
    echo "✅ Alle ${#PKGS[@]} Pakete erfolgreich deployed."
    echo ""
    echo "🚀 RimWorld kann jetzt gestartet werden:"
    echo "   cd \"$RIMWORLD_BASE\" && ./start.sh"
else
    echo "❌ Fehlgeschlagen: ${FAILED[*]}"
    exit 1
fi
