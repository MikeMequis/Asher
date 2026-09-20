#!/usr/bin/env bash
#
# Build the Asher Mono bootstrap PoC.
#
#   ./build.sh
#
# Output (in ./out, kept side by side so the default DLL path resolves):
#   out/libasher_bootstrap.so   native bootstrap loaded via LD_PRELOAD
#   out/BootstrapTest.dll       managed test assembly
#
# Environment:
#   CC              C compiler (default: gcc)
#   EXTRA_CFLAGS    extra compiler flags, e.g. "-m32" if DustAET is 32-bit
#   MCS             managed compiler override (default: mcs, then csc)
#
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
out="$here/out"
native_src="$here/native/bootstrap.c"
managed_src="$here/managed/BootstrapTest.cs"

CC="${CC:-gcc}"
EXTRA_CFLAGS="${EXTRA_CFLAGS:-}"

mkdir -p "$out"

echo "[build] Native bootstrap -> out/libasher_bootstrap.so"
# shellcheck disable=SC2086
"$CC" -shared -fPIC -O2 -Wall -Wextra $EXTRA_CFLAGS \
    -o "$out/libasher_bootstrap.so" \
    "$native_src" \
    -ldl -lpthread

managed_compiler="${MCS:-}"
if [ -z "$managed_compiler" ]; then
    if command -v mcs >/dev/null 2>&1; then
        managed_compiler="mcs"
    elif command -v csc >/dev/null 2>&1; then
        managed_compiler="csc"
    else
        echo "[build] ERROR: no Mono C# compiler found (install 'mono-devel' for mcs/csc)" >&2
        exit 1
    fi
fi

echo "[build] Managed assembly -> out/BootstrapTest.dll (via $managed_compiler)"
"$managed_compiler" -target:library -out:"$out/BootstrapTest.dll" "$managed_src"

echo "[build] Done."
