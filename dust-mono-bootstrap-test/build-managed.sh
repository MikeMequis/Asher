#!/usr/bin/env bash
#
# Regenerate the managed assemblies staged by build.sh into ./prebuilt.
#
#   ./build-managed.sh
#
# Asher.Runtime uses C# 9 features (nullable annotations on unconstrained generic
# type parameters), so the compiler must support -langversion:9.0 or newer.
# Mono 6.12 ships Roslyn 3.9 which supports C# 9. Older Mono (6.8 and below) does
# not; in that case build the assemblies on Windows instead (see README) and copy
# the resulting AnyCPU DLLs into ./prebuilt.
#
# Asher.Runtime is built in full (it owns the generic Harmony orchestration:
# AssemblyLoader, PreInitBootstrap, PatchModuleLoader). Patch implementations stay
# in their own projects (currently Asher.Patching.DebugEnabler).
#
# Environment:
#   CSC          C# compiler command (default: csc)
#   LANGVERSION  language version (default: 9.0)
#
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$here/.." && pwd)"
prebuilt="$here/prebuilt"

CSC="${CSC:-csc}"
LANGVERSION="${LANGVERSION:-9.0}"

if ! command -v "$CSC" >/dev/null 2>&1; then
    echo "[build-managed] ERROR: '$CSC' not found." >&2
    echo "[build-managed] Install mono-devel (Mono 6.12+) or build on Windows; see README." >&2
    exit 1
fi

harmony="$repo_root/packages/Lib.Harmony.2.4.2/lib/net472/0Harmony.dll"
if [ ! -f "$harmony" ]; then
    echo "[build-managed] ERROR: 0Harmony.dll not found at $harmony" >&2
    exit 1
fi

mkdir -p "$prebuilt"

collect() {
    find "$1" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -print
}

common_flags=(
    -nologo
    -target:library
    -platform:anycpu
    -langversion:"$LANGVERSION"
    -codepage:65001
    -r:System.dll
    -r:System.Core.dll
)

echo "[build-managed] Asher.SDK.dll"
"$CSC" "${common_flags[@]}" \
    -out:"$prebuilt/Asher.SDK.dll" \
    -r:"$harmony" \
    $(collect "$repo_root/Asher.SDK")

echo "[build-managed] Asher.Runtime.dll"
"$CSC" "${common_flags[@]}" \
    -out:"$prebuilt/Asher.Runtime.dll" \
    -r:"$harmony" \
    -r:"$prebuilt/Asher.SDK.dll" \
    $(collect "$repo_root/Asher.Runtime")

echo "[build-managed] Asher.Patching.DebugEnabler.dll"
"$CSC" "${common_flags[@]}" \
    -out:"$prebuilt/Asher.Patching.DebugEnabler.dll" \
    -r:"$harmony" \
    -r:"$prebuilt/Asher.SDK.dll" \
    $(collect "$repo_root/Patches/Asher.Patching.DebugEnabler")

echo "[build-managed] Done."
