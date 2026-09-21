#!/usr/bin/env bash
#
# Build the managed assemblies (Asher.SDK, Asher.Runtime) and all patch modules
# (Asher.Patching.*) into ./out and ./out/Mods. build.sh calls this automatically
# when the assemblies are missing.
#
#   ./build-managed.sh
#
# Asher.Runtime uses C# 9 features (nullable annotations, target-typed new, unconstrained
# T?), so a Roslyn-based compiler is required. Mono's legacy mcs/mono-csc (C# 7.x) will
# NOT work. Use one of:
#   - csc (Mono 6.12+ / Roslyn), or
#   - set CSC to a compiler command, e.g. CSC="dotnet exec /path/to/csc.dll"
#
# GraphicsDeprofiler references Microsoft.Xna.Framework.Graphics at compile time. On Linux
# that type comes from FNA, so set FNA_DLL to the game's FNA.dll to build it. If FNA_DLL
# is not set, the other patches are still built and GraphicsDeprofiler is skipped.
#
# Environment:
#   CSC          C# compiler command (default: csc)
#   FNA_DLL      Path to the game's FNA.dll (needed only for GraphicsDeprofiler)
#   LANGVERSION  language version (default: 9.0)
#
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$here/.." && pwd)"
out="$here/out"
mods="$out/Mods"

CSC="${CSC:-csc}"
LANGVERSION="${LANGVERSION:-9.0}"
FNA_DLL="${FNA_DLL:-}"

if ! command -v "${CSC%% *}" >/dev/null 2>&1; then
    echo "[build-managed] ERROR: '$CSC' not found." >&2
    echo "[build-managed] A Roslyn C# 9+ compiler is required (Mono 6.12+ ships csc)." >&2
    echo "[build-managed] Alternatively set CSC, e.g. CSC=\"dotnet exec /path/csc.dll\"." >&2
    exit 1
fi

harmony="$repo_root/packages/Lib.Harmony.2.4.2/lib/net472/0Harmony.dll"
if [ ! -f "$harmony" ]; then
    echo "[build-managed] ERROR: 0Harmony.dll not found at $harmony" >&2
    exit 1
fi

mkdir -p "$out" "$mods"

collect() {
    find "$1" -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*' -print
}

# shellcheck disable=SC2206
csc_cmd=($CSC)

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
"${csc_cmd[@]}" "${common_flags[@]}" \
    -out:"$out/Asher.SDK.dll" \
    -r:"$harmony" \
    $(collect "$repo_root/Asher.SDK")

echo "[build-managed] Asher.Runtime.dll"
"${csc_cmd[@]}" "${common_flags[@]}" \
    -out:"$out/Asher.Runtime.dll" \
    -r:"$harmony" \
    -r:"$out/Asher.SDK.dll" \
    $(collect "$repo_root/Asher.Runtime")

build_patch() {
    local name="$1" proj="$2"
    shift 2
    echo "[build-managed] $name.dll"
    "${csc_cmd[@]}" "${common_flags[@]}" \
        -out:"$mods/$name.dll" \
        -r:"$harmony" \
        -r:"$out/Asher.SDK.dll" \
        "$@" \
        $(collect "$repo_root/Patches/$proj")
}

build_patch Asher.Patching.DebugEnabler   Asher.Patching.DebugEnabler
build_patch Asher.Patching.IntroSkipper   Asher.Patching.IntroSkipper
build_patch Asher.Patching.MuteVoiceActing Asher.Patching.MuteVoiceActing
build_patch Asher.Patching.OverheatDisabler Asher.Patching.OverheatDisabler

if [ -n "$FNA_DLL" ] && [ -f "$FNA_DLL" ]; then
    build_patch Asher.Patching.GraphicsDeprofiler Asher.Patching.GraphicsDeprofiler -r:"$FNA_DLL"
else
    echo "[build-managed] NOTE: FNA_DLL not set; skipping GraphicsDeprofiler"
fi

echo "[build-managed] Done. Assemblies in $out, patches in $mods"
