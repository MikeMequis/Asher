#!/usr/bin/env bash
#
# Build the Asher Mono bootstrap.
#
#   ./build.sh
#
# Output (in ./out, kept side by side so the default assembly path resolves):
#   out/libasher_bootstrap.so   native bootstrap loaded via LD_PRELOAD
#   out/Asher.Runtime.dll       real Asher managed runtime, Harmony-free (RuntimeBootstrap.Initialize)
#   out/Asher.SDK.dll           Asher.Runtime dependency
#   out/Asher.HarmonyPoc.dll    isolated Harmony PoC (the only assembly referencing 0Harmony)
#   out/Asher.Patching.DebugEnabler.dll  real DebugEnabler patch reused by the PoC
#   out/0Harmony.dll            Harmony 2.4.2, used only by Asher.HarmonyPoc
#   out/BootstrapTest.dll       phase-1 reference PoC (optional, only if a compiler is available)
#
# Environment:
#   CC              C compiler (default: gcc)
#   EXTRA_CFLAGS    extra compiler flags, e.g. "-m32" if DustAET is 32-bit
#
# The managed assemblies are staged from ./prebuilt. They are AnyCPU IL and were
# produced from the real repository sources by build-managed.sh (see README).
#
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$here/.." && pwd)"
out="$here/out"
native_src="$here/native/bootstrap.c"
prebuilt="$here/prebuilt"

CC="${CC:-gcc}"
EXTRA_CFLAGS="${EXTRA_CFLAGS:-}"

mkdir -p "$out"

echo "[build] Native bootstrap -> out/libasher_bootstrap.so"
# shellcheck disable=SC2086
"$CC" -shared -fPIC -O2 -Wall -Wextra $EXTRA_CFLAGS \
    -o "$out/libasher_bootstrap.so" \
    "$native_src" \
    -ldl -lpthread

if [ ! -f "$prebuilt/Asher.Runtime.dll" ] || [ ! -f "$prebuilt/Asher.SDK.dll" ] \
    || [ ! -f "$prebuilt/Asher.HarmonyPoc.dll" ] \
    || [ ! -f "$prebuilt/Asher.Patching.DebugEnabler.dll" ]; then
    echo "[build] ERROR: missing managed assemblies in $prebuilt" >&2
    echo "[build]        run ./build-managed.sh (or build on Windows) first" >&2
    exit 1
fi

echo "[build] Staging Asher.Runtime.dll + Asher.SDK.dll + Asher.HarmonyPoc.dll + Asher.Patching.DebugEnabler.dll"
cp "$prebuilt/Asher.Runtime.dll" "$out/Asher.Runtime.dll"
cp "$prebuilt/Asher.SDK.dll" "$out/Asher.SDK.dll"
cp "$prebuilt/Asher.HarmonyPoc.dll" "$out/Asher.HarmonyPoc.dll"
cp "$prebuilt/Asher.Patching.DebugEnabler.dll" "$out/Asher.Patching.DebugEnabler.dll"

harmony="$repo_root/packages/Lib.Harmony.2.4.2/lib/net472/0Harmony.dll"
if [ -f "$harmony" ]; then
    echo "[build] Staging 0Harmony.dll"
    cp "$harmony" "$out/0Harmony.dll"
else
    echo "[build] WARN: 0Harmony.dll not found at $harmony (skipped)"
fi

# Phase-1 reference assembly: only build it when a Mono compiler is present.
managed_compiler=""
if command -v mcs >/dev/null 2>&1; then
    managed_compiler="mcs"
elif command -v csc >/dev/null 2>&1; then
    managed_compiler="csc"
fi

if [ -n "$managed_compiler" ]; then
    echo "[build] Phase-1 reference assembly -> out/BootstrapTest.dll (via $managed_compiler)"
    "$managed_compiler" -target:library -out:"$out/BootstrapTest.dll" "$here/managed/BootstrapTest.cs"
else
    echo "[build] NOTE: no mcs/csc found, skipping phase-1 BootstrapTest.dll"
fi

echo "[build] Done. Artifacts in $out"
