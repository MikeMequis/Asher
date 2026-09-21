# Cross-Platform Architecture (Windows / Linux)

Foundation for running the Asher manager installer on Linux. This is a contract/architecture
record, not a Linux installer implementation. The Windows installer behavior is unchanged.

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

Business logic stays in `GameInstallationService`, `GameFolderService`, `GameLaunchService`.
Only OS-specific operations are delegated; `GameInstallationService` is shared — there is **no
separate Linux installer**.

## Contracts

Descriptors (Asher.Core):

| Type | Responsibility |
|------|----------------|
| `PlatformKind` | `Windows` / `Linux` |
| `IPlatformInfo` | Platform-varying names/locations (data only): game/real/launcher executable names, `BootstrapLibraryName`, `UsesLauncherSwap`, `SupportsRecoveryHelper`, `WritesPortableSettings`, default game folder name, user settings directory |
| `PlatformInfo.Current` | Resolves the descriptor for the current process |

Behavior (Asher.Services.Interfaces, one impl per OS under `Asher.Services.Platform`):

| Contract | Platform-dependent operation | Windows impl | Linux impl (initial stub) |
|----------|------------------------------|--------------|---------------------------|
| `IGameFolderDiscovery` | game path discovery | `WindowsGameFolderDiscovery` (Steam/GOG/Humble/vdf/search) | `LinuxGameFolderDiscovery` (XDG Steam roots + vdf libraries, best-effort Heroic/Lutris, Home) |
| `IGameExecutableLayout` | install/uninstall of the executable slot, backup/restore, install marker, recovery helper | `WindowsGameExecutableLayout` (rename `DustAET.exe` → `.real.exe`, copy launcher) | `LinuxGameExecutableLayout` (no swap; marker = `libasher_bootstrap.so`) |
| `IRuntimeDeployment` | runtime/bootstrap + default mods deployment/cleanup | `WindowsRuntimeDeployment` (3 managed files) | `LinuxRuntimeDeployment` (+ `libasher_bootstrap.so`) |
| `IGameProcessLauncher` | launching the game process | `WindowsGameProcessLauncher` (shell start) | `LinuxGameProcessLauncher` (`LD_PRELOAD`, `ASHER_*`, `MONO_PATH`) |

`PlatformServices.Create()` is the single selection point; `ApplicationServices.Create()` does not
branch on the OS.

### Shared (platform-independent)

- `GameInstallationService`: validation, progress, folder structure, payload discovery, verification.
- `RuntimeDeploymentBase`: file copy, active-runtime check, cleanup (backup/logs preserved).
- `AsherPaths`: game-relative, Asher-installation-relative and user-data-relative paths.
- `SteamLibraryVdf`: shared `libraryfolders.vdf` path reading; each platform filters for its own path shape (drive path vs rooted POSIX path).

## Windows → contract mapping

| Previous location | Now |
|-------------------|-----|
| `GameFolderService.GetSteamPath/GetGogPath/…` | `WindowsGameFolderDiscovery` |
| `GameLaunchService.Process.Start(UseShellExecute)` | `WindowsGameProcessLauncher` |
| `GameInstallationService` backup/rename/launcher/restore/markers/emergency script | `WindowsGameExecutableLayout` |
| `GameInstallationService` runtime file copy / mod copy / clean | `WindowsRuntimeDeployment` + `RuntimeDeploymentBase` |

## Path categories (`AsherPaths`)

| Category | Examples |
|----------|----------|
| Game-relative | `Asher/`, `Mods/`, `Mods/disabled/`, `Asher.Backup/`, `AsherLogs/`, `patches/` |
| Asher-installation-relative | `DefaultMods/`, `install-payload/`, `InstallPayload/`, `Asher.Launcher.exe` |
| User/application-data-relative | `settings.json` (under `IPlatformInfo.UserSettingsDirectory`); portable marker `IPlatformInfo.WritesPortableSettings` / `portable` file |
| Platform-specific | `DustAET.exe` / `DustAET`, `DustAET.real.exe`, `Asher.Launcher.exe`, `libasher_bootstrap.so`, `Uninstall-Asher.cmd/.ps1` |

## Platform contract to the frontend

`getPlatformInfo` (JSONL) → `PlatformInfoDto`. The renderer holds no OS knowledge:
`src/renderer/platform.js` derives capabilities (`usesLauncherSwap`, `supportsRecoveryHelper`) from
the returned DTO. Windows-only UI (Settings → Removal → Total exclusion) is hidden when
`supportsRecoveryHelper` is false.

## Install-state contract

`getInstallState` (JSONL) → `InstallStateDto` (`state`, `canUninstall`, `canRestore`, `marker`).
This replaces inferring install capability from `hasRestorableBackup`; that method remains for
compatibility only.

Semantics (shared orchestration, platform-specific meaning):

- `markerPresent` = platform install marker (Windows `DustAET.real.exe`; Linux `libasher_bootstrap.so`).
- `runtimePresent` = any managed runtime file (`Asher.Runtime.dll`, `Asher.SDK.dll`, `0Harmony.dll`).
- `installed` = marker + managed runtime present; `partial` = only one of them; `notInstalled` = neither.
  Intentionally-preserved backup/logs residue alone is not `partial`.

| | Windows | Linux |
|---|---|---|
| marker | `DustAET.real.exe` | `libasher_bootstrap.so` |
| `canRestore` | `true` when a restorable backup exists | always `false` (game files untouched) |
| `canUninstall` | installed/partial **and** restorable backup exists | installed/partial (nothing to restore) |
| `marker` field | marker name, else first managed runtime file | bootstrap name, else first managed runtime file |

## Linux discovery behavior

Candidate order (mirrors the Windows source labels): `Steam`, `Heroic`, `Lutris`, `Installed`, `Home`.
Manual folder selection is unchanged (Electron dialog → `getGameFolderInfo`).

- Steam roots (XDG-aware, never a single hardcoded path): `$XDG_DATA_HOME/Steam`,
  `$HOME/.local/share/Steam`, `$HOME/.steam/steam`, `$HOME/.steam/debian-installation`,
  `$HOME/.var/app/com.valvesoftware.Steam/.local/share/Steam`, `$HOME/snap/steam/common/.local/share/Steam`.
  Each root's `steamapps/libraryfolders.vdf` is parsed for additional libraries (multiple libraries supported).
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

- Deployment is idempotent: copies overwrite, the manifest is rewritten, and re-running does not
  duplicate files.
- `install.json` is written only after the full copy; fields: `schemaVersion`, `installedAtUtc`,
  `payloadVersion` (best-effort from `Asher.Runtime.dll`), `bootstrapArchitecture`, `gameArchitecture`
  (ELF `e_machine`; mismatch is logged to stderr).
- `IsRuntimeInstalled` requires the manifest **and** the full bootstrap + managed file set, so an
  `installed` state only appears after a complete deployment.
- Default mods are read from the first install-source subfolder that has any: `DefaultMods/` (staged
  payload) or `Mods/` (raw `Asher.Linux/out` build output), reconciling the build/payload mismatch.
- `DustAET` is never renamed/replaced; there is no launcher swap and no recovery script.
- Uninstall removes Asher-owned files under `Asher/` without touching `DustAET`. It no longer requires
  a restorable backup on platforms that do not swap the executable (`UninstallAsync` gates the backup
  check on `IPlatformInfo.UsesLauncherSwap`; Windows behavior is unchanged).
- `LinuxGameExecutableLayout.HasRestorableBackup` is `false` (nothing to restore). `hasRestorableBackup`
  therefore reports `false` on Linux; the renderer already relies on `getInstallState`.

## Electron lifecycle

- `application-state.js` fetches `getPlatformInfo` + `getInstallState` and is the only place deriving
  `canUninstall`/`canRestore`; renderer controllers never read `hasRestorableBackup`.
- `platform.js` derives capabilities (`usesLauncherSwap`, `supportsRecoveryHelper`). Windows-only UI
  (Settings → Removal → Total exclusion) is hidden unless `supportsRecoveryHelper`.
- Install/uninstall/launch flows stay platform-neutral through JSONL. Controllers
  (`installation-controller.js`, `uninstallation-controller.js`, `launch-game.js`) contain no OS logic.
- `main.js` rejects `app:run-emergency-uninstall` off Windows; `manager-paths.js` resolves the packaged
  binary name (`Asher.exe` / `Asher`).
- `auto-updater.js` and `post-quit-helper.js` are Windows-only (PowerShell extraction, post-quit
  robocopy). Off Windows the updater reports `unavailable`; there is no fake Linux updater.
- `npm run smoke:platform` verifies the capability/state contract and that the renderer no longer
  references `hasRestorableBackup`.

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
  Electron already routes Host stderr into the Asher manager diagnostic log, so the JSONL stdout channel
  stays clean. The runtime's own `AsherLogs/runtime_*.log` output is unaffected.
- Preconditions: bootstrap library exists and `IsRuntimeInstalled` is true; otherwise launch fails
  with a clear message and no process is started.
- Fire-and-forget; no PID/process tracking.
- External launch (Steam/desktop shortcuts) still needs a decision; see Deferred.

## Linux bootstrap model (already implemented elsewhere)

- DustAET is a native ELF with an embedded Mono runtime.
- `libasher_bootstrap.so` (`LD_PRELOAD`) attaches and loads `Asher.Runtime` (`Asher.Linux/README.md`).
- Launching therefore exports environment instead of swapping executables; `LinuxGameProcessLauncher`
  sets `LD_PRELOAD`, `ASHER_HOME`, `ASHER_MODS_PATH`, `ASHER_LOG_PATH`, `MONO_PATH`.
- The runtime log folder is `<game>/Asher/AsherLogs`, matching `AsherPaths.LogsFolderName`. The
  launcher exports `ASHER_LOG_PATH`, and the managed `RuntimeBootstrap` default is now `AsherLogs`
  too, so logs stay visible even when the game is started without Asher environment variables.
- Linux user settings resolve to `$XDG_CONFIG_HOME/Asher` (fallback `~/.config/Asher`). On Linux the
  portable copy next to the binary is written only when a `portable` marker file exists
  (`IPlatformInfo.WritesPortableSettings`); Windows keeps its always-portable behavior.

## Tests

`Asher.Services.Tests` (xUnit) covers platform descriptors, portable-settings policy, pure path
resolution, `SteamLibraryVdf`, and Linux discovery with fake roots / injected environment. The Linux
discovery tests run on Windows via temporary directories and an injected `getEnvironmentVariable`.

## Linux build and packaging

Target: **Linux x64**, artifacts **AppImage + tar.gz** (no `.deb`). Run on a Linux host
(AppImage cannot be produced from Windows). Windows packaging is unchanged.

Pipeline (`npm run dist:linux` → `Asher.Electron/scripts/build-linux.sh`):

1. `dotnet publish Asher.Host -c Release -r linux-x64 --self-contained true -p:Platform=AnyCPU -o build/linux-host`
2. `Asher.Linux/build.sh` → `Asher.Linux/out` (native `.so` + managed assemblies + `Mods/`)
3. `scripts/stage-linux-payload.mjs` → `build/linux-host/install-payload/` (runtime files + `DefaultMods/`);
   deterministic, recreates the destination, fails if any required artifact is missing
4. `electron-builder --linux AppImage tar.gz --x64`
5. `scripts/verify-linux-package.mjs` — asserts the tar.gz contains the Host and payload

Artifacts (`Asher.Electron/dist/`): `Asher-2.0.0-linux-x86_64.AppImage`, `Asher-2.0.0-linux-x64.tar.gz`,
and `linux-unpacked/` (manager binary `Asher`). Note electron-builder names the AppImage arch `x86_64`
while the tar.gz uses `x64`.

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
Keying on `Platform` (not `RuntimeIdentifier`) is required: referenced library projects did not observe
the RID during cross-publish, so `Asher.Services` was emitted x86 and the x64 Linux Host failed to load
it. The Linux Host is published with `-p:InvariantGlobalization=true`, so it does not require a system
ICU (`libicu`) — WSL/minimal distros otherwise abort at startup.

`InstallPayload.targets` is imported only for Windows non-RID builds, so a Linux publish never stages
the Windows launcher payload. `build/` and `dist/` are gitignored; no generated binaries are committed.

## Deferred (not implemented here)

- External Steam/desktop launch (environment propagation) — manager launch is implemented.
- Linux manager auto-update (updater is Windows-only; reports unavailable off Windows).
- Linux `.deb`/other package formats.
- Linux runtime-log bootstrap hang fix (racy `mono_thread_attach`; see Asher.Linux README).
- Shell-script recovery helper on Linux (normal installation stays app-driven).
