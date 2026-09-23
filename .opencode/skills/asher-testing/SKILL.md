---
name: asher-testing
description: Use when building, testing, validating, packaging, or verifying a change in Asher — C# builds, xUnit tests, headless smoke tests, JSONL test client, dev run, Windows/Linux packaging, or deciding whether real Dust execution is required.
---

# Asher Testing and Validation

Validation strategy for Asher. Command inventory and per-script purpose; for install/packaging
narrative see `README.md` and `docs/Cross-Platform-Architecture.md`.

## Choose scope by change

1. **Smallest scope first** — build the changed C# project, then run the relevant smoke test.
2. **Broader when needed** — `npm run smoke` for cross-cutting changes; `npm start` for UI.
3. **Real-game validation** — required for install, uninstall, launch, or runtime-patching changes.
   Must run on a machine with Dust: An Elysian Tail installed.
4. **Packaged validation** — run `npm run dist`, launch `Distribution/Asher.exe`, install into a game
   folder, confirm Finish stays in Distribution and `Uninstall-Asher.cmd` exists beside `DustAET.exe`.

Do not claim success without running the validation the change actually requires. Smoke tests use
temp paths, not real game folders — they do not prove install/launch correctness.

## Build

```bash
# C# backend (from repo root)
dotnet build Asher.Host/Asher.Host.csproj -c Debug -p:Platform=x86

# All host deps including patching projects
cd Asher.Electron && npm run build:host:debug
```

Linux builds Host-side projects AnyCPU/x64.

## .NET unit tests (xUnit)

```bash
dotnet test Asher.Services.Tests/Asher.Services.Tests.csproj -c Debug
```

Covers platform descriptors, portable-settings policy, path resolution, `SteamLibraryVdf`, Linux
discovery with fake roots, install states, and launch-environment construction. No game or Host needed.

## JSONL protocol validation

```bash
dotnet run --project Asher.Host.TestClient -p:Platform=x86
```

## Smoke tests (headless, no GUI)

```bash
cd Asher.Electron
npm run smoke            # Full suite
npm run smoke:setup      # Game folder detect/validate/save
npm run smoke:manager    # getMods + setModEnabled failure path
npm run smoke:shell      # Application shell lifecycle
npm run smoke:install    # Install with invalid path (safe)
npm run smoke:uninstall  # Uninstall with invalid path (safe)
npm run smoke:launch     # Launch preconditions + failure path
npm run smoke:payload    # Verify install-payload contents
npm run smoke:platform   # Platform capabilities + install-state contract
```

## Dev run

```bash
cd Asher.Electron && npm start
```

## Windows packaging

```bash
cd Asher.Electron
npm run dist       # clean + build:host Release + electron-builder dir/zip/nsis (ia32) + latest.yml + sync Distribution/
npm run publish    # same as dist, publishes GitHub Release — requires private/GH_TOKEN
```

Outputs: `dist/win-ia32-unpacked/`, `dist/Asher-Setup-<version>.exe` (NSIS), `dist/*.zip`,
`dist/latest.yml`, and repo-root `Distribution/` (synced unpacked app).

## Linux packaging (run on Linux)

Prerequisites: .NET SDK 8, Node.js + npm, `gcc`, a Roslyn C# 9 compiler (`csc`/`CSC`); Electron needs
NSS/NSPR/ALSA system libraries at runtime. Full list: `docs/Cross-Platform-Architecture.md` →
*Linux dependencies*.

```bash
cd Asher.Electron
npm run build:host:linux        # dotnet publish Asher.Host linux-x64 -> build/linux-host
npm run stage:linux-payload -- --source ../Asher.Linux/out --dest build/linux-host/install-payload
npx electron-builder --linux AppImage tar.gz --x64
npm run verify:linux
npm run dist:linux              # one-shot: publish + Asher.Linux + stage + package + verify
npm run publish:linux           # additionally uploads release assets/metadata
```

Outputs: `dist/Asher-<version>-linux-x86_64.AppImage`, `Asher-<version>-linux-x64.tar.gz`,
`latest-linux.yml`, `linux-unpacked/` (manager binary `Asher`). AppImage requires Linux tooling;
`tar.gz` and `--dir` can be produced cross-platform. `build/` and `dist/` are gitignored.

## Known limitations

- Smoke tests use temp paths, not real game folders.
- Manager deploy/relaunch was removed — UI is Distribution-only; the game folder gets an emergency
  uninstall script instead.
- `npm run dist` may fail on Windows without symlink privilege when electron-builder caches
  `winCodeSign` (Developer Mode or elevated terminal).
- Patching/runtime builds need XNA 4.0 GAC assemblies — see `README.md` *Addendum — XNA Framework*.
- No DOM/E2E tests: smoke tests exercise JSONL/Host only, not the renderer.
