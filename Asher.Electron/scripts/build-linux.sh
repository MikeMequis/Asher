#!/usr/bin/env bash
#
# Build and package the Asher manager for Linux (x64).
#
# Pipeline:
#   1. publish Asher.Host for linux-x64 into build/linux-host/
#   2. build the native/managed Asher.Linux artifacts (Asher.Linux/out)
#   3. stage Asher.Linux/out into build/linux-host/install-payload
#   4. electron-builder -> dist/*.AppImage and dist/*.tar.gz
#   5. verify the tar.gz payload layout
#
# Run on a Linux host (AppImage cannot be produced from Windows):
#   cd Asher.Electron && npm run dist:linux
#
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
electron_root="$(cd "$here/.." && pwd)"
repo_root="$(cd "$electron_root/.." && pwd)"

host_out="$electron_root/build/linux-host"
payload_out="$host_out/install-payload"

echo "[build-linux] publish Asher.Host (linux-x64)"
rm -rf "$host_out"
dotnet publish "$repo_root/Asher.Host/Asher.Host.csproj" \
    -c Release -r linux-x64 --self-contained true \
    -p:Platform=AnyCPU -p:InvariantGlobalization=true \
    -o "$host_out"

echo "[build-linux] build Asher.Linux artifacts"
chmod +x "$repo_root/Asher.Linux/build.sh" "$repo_root/Asher.Linux/build-managed.sh" 2>/dev/null || true
( cd "$repo_root/Asher.Linux" && bash ./build.sh )

echo "[build-linux] stage install-payload"
node "$electron_root/scripts/stage-linux-payload.mjs" \
    --source "$repo_root/Asher.Linux/out" \
    --dest "$payload_out"

echo "[build-linux] electron-builder (AppImage, tar.gz; x64)"
( cd "$electron_root" && npx electron-builder --linux AppImage tar.gz --x64 )

echo "[build-linux] verify artifacts"
node "$electron_root/scripts/verify-linux-package.mjs"

echo "[build-linux] done; artifacts in $electron_root/dist"
