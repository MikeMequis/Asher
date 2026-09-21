#!/usr/bin/env bash
#
# Build the Asher Linux bootstrap.
#
#   ./build.sh
#
# Output (in ./out):
#   out/libasher_bootstrap.so                  native LD_PRELOAD bootstrap
#   out/Asher.Runtime.dll                      real Asher runtime (built by build-managed.sh)
#   out/Asher.SDK.dll                          runtime dependency
#   out/0Harmony.dll                           Harmony 2.4.2 (vendored package)
#   out/Mods/Asher.Patching.*.dll              patch modules loaded by the runtime
#
# The managed assemblies and patch modules are built locally by build-managed.sh
# (requires a Roslyn C# 9 compiler). Nothing is committed prebuilt.
#
# Environment:
#   CC              C compiler (default: gcc)
#   EXTRA_CFLAGS    extra compiler flags, e.g. "-m32" if DustAET is 32-bit
#   CSC             C# compiler command passed through to build-managed.sh
#   FNA_DLL         Path to the game's FNA.dll (to build GraphicsDeprofiler)
#
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$here/.." && pwd)"
out="$here/out"
mods_out="$out/Mods"
native_src="$here/Native/bootstrap.c"

CC="${CC:-gcc}"
EXTRA_CFLAGS="${EXTRA_CFLAGS:-}"

mkdir -p "$out" "$mods_out"

echo "[build] Native bootstrap -> out/libasher_bootstrap.so"
# shellcheck disable=SC2086
"$CC" -shared -fPIC -O2 -Wall -Wextra $EXTRA_CFLAGS \
    -o "$out/libasher_bootstrap.so" \
    "$native_src" \
    -ldl -lpthread

if [ ! -f "$out/Asher.Runtime.dll" ] || [ ! -f "$out/Asher.SDK.dll" ] \
    || [ ! -f "$mods_out/Asher.Patching.DebugEnabler.dll" ]; then
    echo "[build] Managed assemblies missing -> running build-managed.sh"
    "$here/build-managed.sh"
fi

echo "[build] Staging 0Harmony.dll"
cp "$repo_root/packages/Lib.Harmony.2.4.2/lib/net472/0Harmony.dll" "$out/0Harmony.dll"

echo "[build] Patch modules:"
ls -1 "$mods_out" 2>/dev/null | sed 's/^/  /'
echo "[build] Done. Artifacts in $out"
