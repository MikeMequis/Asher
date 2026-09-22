# Cross-Platform Architecture (Windows / Linux)

How the Asher manager supports Windows and Linux. Windows uses the launcher-swap model; Linux keeps
the native `DustAET` and attaches through `libasher_bootstrap.so`. Business logic is shared; only
OS-specific operations are delegated. Native bootstrap internals: `Asher.Linux/README.md`.

## Layering

```
Electron UI
    │ JSONL (stdin/stdout)
Asher.Host (--jsonl)
    │
Asher.Services / IAsherApplication
    │
platform abstraction  (IPlatformInfo + 4 behavior contracts)
    ├── Windows implementation (launcher-swap model)
    └── Linux implementation   (LD_PRELOAD bootstrap model)
```

Business logic stays in `GameInstallationService`, `GameFolderService`, `GameLaunchService`. Only
OS-specific operations are delegated; `GameInstallationService` is shared — there is no separate
Linux installer.

## Contracts

Descriptors (`Asher.Core/Platform`):

| Type | Responsibility |
|------|----------------|
| `PlatformKind` | `Windows` / `Linux` |
| `IPlatformInfo` | Platform-varying names/locations (data only): game/real/launcher executable names, `BootstrapLibraryName`, `UsesLauncherSwap`, `SupportsRecoveryHelper`, `WritesPortableSettings`, default game folder name, user settings directory |
| `PlatformInfo.Current` | Resolves the descriptor for the current process |

Behavior (`Asher.Services.Interfaces`, one implementation per OS under `Asher.Services.Platform`):

| Contract | Platform-dependent operation | Windows impl | Linux impl |
|----------|------------------------------|--------------|------------|
| `IGameFolderDiscovery` | game path discovery | `WindowsGameFolderDiscovery` (Steam/GOG/Humble/vdf/search) | `LinuxGameFolderDiscovery` (XDG Steam roots + vdf libraries, best-effort Heroic/Lutris, Home) |
| `IGameExecutableLayout` | executable slot: install/uninstall, backup/restore, install marker, recovery helper | `WindowsGameExecutableLayout` (rename `DustAET.exe` → `.real.exe`, copy launcher) | `LinuxGameExecutableLayout` (no swap; marker = `libasher_bootstrap.so`) |
| `IRuntimeDeployment` | runtime/bootstrap + default mods deployment/cleanup | `WindowsRuntimeDeployment` (3 managed files) | `LinuxRuntimeDeployment` (+ `libasher_bootstrap.so`, `install.json`) |
| `IGameProcessLauncher` | launching the game process | `WindowsGameProcessLauncher` (shell start) | `LinuxGameProcessLauncher` (`LD_PRELOAD`, `ASHER_*`, `MONO_PATH`) |

`PlatformServices.Create()` is the single selection point; `ApplicationServices.Create()` does not
branch on the OS.

### Shared (platform-independent)

- `GameInstallationService`: validation, progress, folder structure, payload discovery, verification.
- `RuntimeDeploymentBase`: file copy, runtime checks, cleanup (backup/logs preserved).
- `AsherPaths`: game-relative, Asher-installation-relative and user-data-relative paths.
- `SteamLibraryVdf`: shared `libraryfolders.vdf` reading; each platform filters for its own path shape.

## Path categories (`AsherPaths`)

| Category | Examples |
|----------|----------|
| Game-relative | `Asher/`, `Mods/`, `Mods/disabled/`, `Asher.Backup/`, `AsherLogs/`, `patches/` |
| Asher-installation-relative | `DefaultMods/`, `install-payload/`, `InstallPayload/`, `Asher.Launcher.exe` |
| User/application-data-relative | `settings.json` (under `IPlatformInfo.UserSettingsDirectory`); portable marker via `WritesPortableSettings` / `portable` file |
| Platform-specific | `DustAET.exe` / `DustAET`, `DustAET.real.exe`, `Asher.Launcher.exe`, `libasher_bootstrap.so`, `Uninstall-Asher.cmd/.ps1` |

## Platform contract to the frontend

`getPlatformInfo` (JSONL) → `PlatformInfoDto`. The renderer holds no OS knowledge:
`src/renderer/platform.js` derives capabilities (`usesLauncherSwap`, `supportsRecoveryHelper`) from
the DTO. Windows-only UI (Settings → Removal → Total exclusion) is hidden when
`supportsRecoveryHelper` is false.

## Install-state contract

`getInstallState` (JSONL) → `InstallStateDto` (`state`, `canUninstall`, `canRestore`, `marker`) is the
authoritative install-status API. `hasRestorableBackup` is retained for compatibility only and must
not be used to derive uninstall capability.

- `markerPresent` = platform install marker (Windows `DustAET.real.exe`; Linux `libasher_bootstrap.so`).
- `runtimePresent` = any managed runtime file (`Asher.Runtime.dll`, `Asher.SDK.dll`, `0Harmony.dll`).
- `installed` = marker + complete runtime; `partial` = only one of them; `notInstalled` = neither.
  Intentionally-preserved backup/logs residue alone is not `partial`.

| | Windows | Linux |
|---|---|---|
| `canRestore` | `true` when a restorable backup exists | always `false` (game files untouched) |
| `canUninstall` | installed/partial **and** restorable backup exists | installed/partial (nothing to restore) |
| `marker` field | marker name, else first managed runtime file | bootstrap name, else first managed runtime file |

## Linux discovery behavior

Candidate order: `Steam`, `Heroic`, `Lutris`, `Installed`, `Home`. Manual folder selection is
unchanged (Electron dialog → `getGameFolderInfo`).

- Steam roots (XDG-aware, never a single hardcoded path): `$XDG_DATA_HOME/Steam`,
  `$HOME/.local/share/Steam`, `$HOME/.steam/steam`, `$HOME/.steam/debian-installation`,
  `$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam`, `$HOME/snap/steam/common/.local/share/Steam`.
  Each root's `steamapps/libraryfolders.vdf` is parsed for additional libraries.
- Heroic/Lutris are best-effort: only paths found in their config (`heroic/config.json`,
  `lutris/games/*.yml`) are used, and every candidate is verified — missing paths are never guessed.
- Discovery returns candidates; `GameFolderService` keeps ordering/validation semantics (first existing
  folder wins, `IsValidGameFolder` attached by `GetInfo`).

## Linux runtime layout and deployment

`LinuxRuntimeDeployment` (shared `RuntimeDeploymentBase`) deploys into `<game>/Asher`:

```text
<game>/
  DustAET                      native ELF, never touched
  Asher/
    libasher_bootstrap.so      marker
    Asher.Runtime.dll
    Asher.SDK.dll
    0Harmony.dll
    Mods/Asher.Patching.*.dll  default patch modules
    AsherLogs/
    install.json               manifest
```

- Deployment is idempotent: copies overwrite, the manifest is rewritten, re-running does not duplicate.
- `install.json` is written only after the full copy; fields: `schemaVersion`, `installedAtUtc`,
  `payloadVersion` (best-effort from `Asher.Runtime.dll`), `bootstrapArchitecture`, `gameArchitecture`
  (ELF `e_machine`; mismatch is logged to stderr).
- `IsRuntimeInstalled` requires the manifest **and** the full bootstrap + managed file set.
- Default mods are read from the first install-source subfolder that has any: `DefaultMods/` (staged
  payload) or `Mods/` (raw `Asher.Linux/out` build output).
- `DustAET` is never renamed/replaced; there is no launcher swap and no recovery script.
- `GameInstallationService` writes `Asher/LEIA-ME.txt` (platform-aware: launcher swap vs bootstrap) on
  install; uninstall removes it with the other Asher-owned files.
- Uninstall removes Asher-owned files under `Asher/` without touching `DustAET`. The backup requirement
  is gated on `IPlatformInfo.UsesLauncherSwap`, so Windows semantics are unchanged.

## Linux launch environment

`LinuxGameProcessLauncher` (via `IGameProcessLauncher`) starts `<game>/DustAET` directly with
`UseShellExecute = false` and `WorkingDirectory = <game>`. The parent environment is snapshotted
(`IEnvironmentProvider`) and preserved; `IProcessStarter` performs the actual start (fakeable in tests).

| Variable | Value |
|---|---|
| `LD_PRELOAD` | `<game>/Asher/libasher_bootstrap.so` prepended to any inherited value (deduped) |
| `ASHER_HOME` | `<game>/Asher` |
| `ASHER_MODS_PATH` | `<game>/Asher/Mods` |
| `ASHER_LOG_PATH` | `<game>/Asher/AsherLogs` |
| `ASHER_PROFILE` | inherited value, else `default` |
| `MONO_PATH` | `<game>/Asher` prepended to any inherited value (deduped) |

- Unrelated inherited variables are not modified.
- The game's stdout/stderr are **not inherited** by the Host: `RedirectStandardOutput`/`RedirectStandardError`
  are set and `SystemProcessStarter` pumps them to the Host's stderr as `[game-stdout]`/`[game-stderr]`.
  Electron routes Host stderr into the Asher manager diagnostic log, so the JSONL stdout channel stays
  clean. The runtime's own `AsherLogs/runtime_*.log` output is unaffected.
- Preconditions: bootstrap library exists and `IsRuntimeInstalled` is true; otherwise launch fails with
  a clear message and no process is started. Fire-and-forget; no PID/process tracking.
- External launch (Steam/desktop shortcuts) is not implemented.

## Linux settings and logs

- Settings resolve to `$XDG_CONFIG_HOME/Asher` (fallback `~/.config/Asher`). The portable copy next to
  the binary is written only when a `portable` marker exists; Windows keeps its always-portable behavior.
- Runtime logs go to `<game>/Asher/AsherLogs`. The launcher exports `ASHER_LOG_PATH`, and the managed
  `RuntimeBootstrap` default is `AsherLogs`, so logs stay visible even without Asher environment variables.

## Electron lifecycle

- `application-state.js` fetches `getPlatformInfo` + `getInstallState` and is the only place deriving
  `canUninstall`/`canRestore`; renderer controllers never read `hasRestorableBackup`.
- Install/uninstall/launch flows stay platform-neutral through JSONL; controllers contain no OS logic.
- `main.js` rejects `app:run-emergency-uninstall` off Windows; `manager-paths.js` resolves the packaged
  binary name (`Asher.exe` / `Asher`).
- `auto-updater.js` and `post-quit-helper.js` are Windows-only. Off Windows the updater reports
  `unavailable`; there is no Linux updater.
- `npm run smoke:platform` verifies the capability/state contract and that the renderer no longer
  references `hasRestorableBackup`.

## Tests

`Asher.Services.Tests` (xUnit) covers platform descriptors, portable-settings policy, path resolution,
`SteamLibraryVdf`, Linux discovery with fake roots/injected environment, install states, and launch
environment construction.

## Linux dependencies

Commands below target **Debian/Ubuntu** (the validated platform). On Ubuntu 24.04+ the `t64` package
names apply; on older releases drop the `t64` suffix. For other distributions install the equivalents.

### Build (packaging pipeline)

```bash
sudo apt-get update
sudo apt-get install -y build-essential curl nodejs npm mono-devel

# .NET SDK 8 (distro repos may only ship a newer SDK; this is distro-agnostic)
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
```

- `build-essential` → `gcc` for `libasher_bootstrap.so`.
- `nodejs` + `npm` → electron-builder and the repo scripts.
- `mono-devel` → Roslyn C# 9 `csc` for the managed assemblies (alternative: `CSC="dotnet exec …/csc.dll"`).
- `FNA_DLL` is optional and is a game file, not a package (only needed for `GraphicsDeprofiler`).

### Runtime (manager / Electron)

```bash
sudo apt-get install -y libnss3 libnspr4 libatk1.0-0t64 libatk-bridge2.0-0t64 libcups2t64 \
  libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libgbm1 \
  libpango-1.0-0 libcairo2 libasound2t64 libatspi2.0-0t64 libgtk-3-0t64 \
  libgdk-pixbuf-2.0-0 libxtst6 libxss1

# To run the AppImage (FUSE2); otherwise extract with --appimage-extract
sudo apt-get install -y libfuse2t64
```

The Host is self-contained and published with `InvariantGlobalization`, so no .NET runtime or `libicu`
is required at runtime.

## Linux build and packaging

Target: **Linux x64**, artifacts **AppImage + tar.gz** (no `.deb`). Run on a Linux host (AppImage cannot
be produced from Windows). Windows packaging is unchanged.

Pipeline (`npm run dist:linux` → `Asher.Electron/scripts/build-linux.sh`):

1. `dotnet publish Asher.Host -c Release -r linux-x64 --self-contained true -p:Platform=AnyCPU -p:InvariantGlobalization=true -o build/linux-host`
2. `Asher.Linux/build.sh` → `Asher.Linux/out` (native `.so` + managed assemblies + `Mods/`)
3. `scripts/stage-linux-payload.mjs` → `build/linux-host/install-payload/` (runtime files + `DefaultMods/`);
   deterministic, recreates the destination, fails if any required artifact is missing
4. `electron-builder --linux AppImage tar.gz --x64`
5. `scripts/verify-linux-package.mjs` — asserts the artifacts contain the Host and payload

Artifacts (`Asher.Electron/dist/`): `Asher-<version>-linux-x86_64.AppImage`,
`Asher-<version>-linux-x64.tar.gz`, `latest-linux.yml`, and `linux-unpacked/` (manager binary `Asher`).
electron-builder names the AppImage arch `x86_64` and the tar.gz arch `x64`. `npm run publish:linux`
uploads the artifacts plus update metadata (the Windows release uses the NSIS installer + `latest.yml`).

Packaged layout:

```text
Asher
resources/app.asar
resources/asher-host/
  Asher.Host
  install-payload/
    libasher_bootstrap.so
    Asher.Runtime.dll
    Asher.SDK.dll
    0Harmony.dll
    DefaultMods/Asher.Patching.*.dll
```

Platform targets: `x86` only when `OS == Windows_NT` **and** `Platform == x86`; otherwise AnyCPU/x64.
The Linux Host is published with `-p:InvariantGlobalization=true`, so it does not require a system ICU.
`InstallPayload.targets` is imported only for Windows non-RID builds, so a Linux publish never stages the
Windows launcher payload. `build/` and `dist/` are gitignored; no generated binaries are committed.

## Deferred / known limits

- External Steam/desktop launch (manager launch is implemented).
- Linux in-app updater (updater is Windows-only).
- Linux `.deb`/other package formats.
- Linux `mono_thread_attach` timing: mitigated by `ASHER_BOOTSTRAP_SETTLE_MS` (default 1000 ms) after the
  root domain appears; see `Asher.Linux/README.md`.
- Shell-script recovery helper on Linux (normal installation stays app-driven).
