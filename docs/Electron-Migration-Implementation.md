# Electron Manager Architecture

The Asher manager UI is **Electron** (`Asher.Electron`); business logic stays in C# (`Asher.Services`,
`Asher.Core`) behind the headless **Asher.Host** process over **JSONL on stdin/stdout**. The WPF
manager was retired. Persistent project rules: `AGENTS.md`. Specialized workflows:
`.opencode/skills/` (`asher-jsonl-protocol`, `asher-electron`, `asher-testing`); platform contracts:
`docs/Cross-Platform-Architecture.md`. (`.cursor/rules/` remains as a temporary Cursor compatibility
layer, not the source of truth.)

## Current architecture

```
Asher.Electron
  main/      window, HostManager, JSONL client, IPC, updates
  preload/   contextBridge → window.asher
  renderer/  UI, controllers, localization, theme
        │ JSONL (stdin/stdout)
Asher.Host (--jsonl)  →  IAsherApplication  →  Asher.Services  →  Asher.Core

Game process (separate):
  Windows: DustAET.exe (= Asher.Launcher) → Runtime → mod DLLs
  Linux:   DustAET (native) → libasher_bootstrap.so → Runtime → mod DLLs
```

| Component | Responsibility |
|-----------|----------------|
| **Electron renderer** | Presentation, navigation, input; calls the backend via `ApplicationClient` |
| **Electron main** | Spawns/monitors `Asher.Host`, routes IPC, folder dialogs, updates |
| **Asher.Host** | JSONL transport only; stdout is protocol |
| **IAsherApplication** | Stable facade; DTOs only (no Core types on the wire) |
| **Services** | Install/uninstall, settings, discovery, mods, launch |
| **Launcher / Runtime** | Game executable swap (Windows) or LD_PRELOAD bootstrap (Linux) + in-process Harmony patching |

## Decisions

### JSONL over stdin/stdout (not HTTP)

Chosen for a simple child-process model with no port negotiation and a single desktop client. HTTP+SSE
was evaluated but not adopted. Consequences: progress streams on the same stdout channel; requests are
processed sequentially, so a long install blocks other requests. Do not add a second transport without
a concrete requirement.

### `IAsherApplication` + DTOs

The Host and any future frontend talk to a UI-agnostic contract (`Application/*Dto` +
`ApplicationContractMapper`). This keeps `Asher.Core` persistence models off the wire and maps 1:1 to
JSONL methods.

### Unified composition root

`ApplicationServices.Create()` is the single DI wiring point (`PlatformServices.Create()` selects the
OS implementation). WPF's duplicate wiring is gone.

### Electron owns installation

Users install through Electron (Setup → Install); the payload ships beside the Host (`install-payload/`).
The manager UI stays in `Distribution/` only — it is not deployed into `game/Asher/`.

### Install state via dedicated contract methods

`markInstalled` / `markUninstalled` carry install state (rather than merging markers through
`saveSettings`). `getInstallState` is the authoritative status/capability API; `hasRestorableBackup` is
retained for compatibility only. Install always creates a game backup.

### Windows-only updater and emergency helper

`auto-updater.js` and `post-quit-helper.js` use Windows tooling; off Windows the updater reports
`unavailable`. On Windows the game folder gets `Uninstall-Asher.cmd` (+ `.ps1`) for emergency restore;
Settings Removal offers Safe uninstall (in-app) and Total exclusion (that helper).

## Compatibility requirements

Preserve unless explicitly required to change:

- **Installation layout:** `Asher/` folder structure (`AsherPaths`); Windows `DustAET.exe` swap /
  `DustAET.real.exe`
- **Settings file:** path, JSON shape, `MarkAsInstalled` / `MarkAsUninstalled` semantics
- **Install/uninstall logic:** `GameInstallationService` — shared semantics; Electron is UI only
- **Mod toggle:** `setModEnabled` rejects nonexistent mods; moves files between `Mods/` and `DisabledMods/`
- **Launch:** Windows starts the swapped `DustAET.exe` (not `DustAET.real.exe`); Linux starts native
  `DustAET` with the bootstrap environment
- **Business logic in C#:** the renderer orchestrates; it does not reimplement service rules

## Known limitations

- **No UI/E2E tests:** smoke tests exercise JSONL/host only, not the renderer DOM.
- **Packaging:** `npm run dist` may fail on Windows without symlink privilege when electron-builder caches
  `winCodeSign` (Developer Mode or elevated terminal).
- **Updates:** packaged Distribution builds can download/apply GitHub release zips; unpackaged `npm start`
  cannot. Linux updates are manual downloads.
- **Patching builds:** need XNA 4.0 in the GAC on Windows — see README *Addendum — XNA Framework*.
- **Cancellation:** protocol-level cancel exists; services may not fully observe `CancellationToken`
  inside `Task.Run` install work.
- **Sequential JSONL:** a long install blocks other host requests.

## Retired (WPF)

`Asher.App`, `Asher.UserInterface`, `Asher.Localization`, `PrepareDistribution.ps1`, and WPF-only services
(`ManagerDeploy`, `ManagerLaunch`, `Shortcut`, `InstallationState`, `NavigationItemsManager`) were removed.
Prism/MaterialDesign/WPF dependencies were stripped from `Asher.Core`. See `Manager-UI-Architecture.md`.
