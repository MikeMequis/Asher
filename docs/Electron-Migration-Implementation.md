# Electron Migration — Implementation Log

Implementation record for the Asher frontend transition. Architectural background is in `GUI-Architecture-Investigation.md` (not duplicated here).

---

## Initial State

- Application logic lived in `Asher.Services` + `Asher.Core`, consumed only by the WPF app (`Asher.App` + `Asher.UserInterface`) via Prism DI.
- No standalone host existed; services could be constructed manually but wiring was only in `App.xaml.cs`.
- `Asher.Core` targets `net8.0-windows` with WPF/MaterialDesign/Prism packages; services run without starting a WPF `Application` when instantiated directly.
- Investigation (§12) identified `AsherServiceHost` / console spike as the first implementation step.

---

## Changes

### Step 1 — Shared service host factory

| Item | Detail |
|------|--------|
| **What** | Added `Asher.Services/Hosting/AsherServiceHost.cs` |
| **Why** | Single wiring point for application services without WPF/Prism; mirrors dependency order from `App.xaml.cs` (excluding UI-only services) |
| **Files** | `Asher.Services/Hosting/AsherServiceHost.cs` |
| **Compatibility** | WPF app unchanged; still uses Prism registration in `App.xaml.cs` |

`AsherServiceHost.Create()` registers:

- `ManagerDeployService` → `GameInstallationService` → `GameFolderService` → `GameLaunchService` → `PatchManagerService`
- `InstallationStateService`, `ShortcutService`, `ManagerLaunchService`

**Not registered** (UI-only): `INavigationItemsManager`, `IThemeService`.

### Step 2 — Console host project

| Item | Detail |
|------|--------|
| **What** | Added `Asher.Host` console project and `Program.cs` smoke runner |
| **Why** | Prove services initialize and core operations are invocable without WPF |
| **Files** | `Asher.Host/Asher.Host.csproj`, `Asher.Host/Program.cs`, `Asher.sln` |
| **Compatibility** | No changes to existing projects beyond new `Hosting` folder in Services |

Host targets `net8.0-windows`, `x86`, no `UseWPF`.

### Step 3 — No Core/Services decoupling yet

No changes to `ManagedModInfo`, `AsherSettings`, `NavigationItem`, or WPF app. Host references existing assemblies as-is; transitive WPF/MaterialDesign/Prism assemblies load but no WPF application starts.

---

## Step 4 — First Decoupling Pass

### ManagedModInfo

| Item | Detail |
|------|--------|
| **What** | `ManagedModInfo` is now a plain POCO in `Asher.Core` (no `BindableBase`) |
| **Presentation** | Added `ManagedModItemViewModel` in `Asher.UserInterface` for WPF toggle binding and `PropertyChanged` |
| **Consumers** | `PatchManagerViewModel` maps service results to `ManagedModItemViewModel`; `PatchManagerService` unchanged |
| **Behavior** | Mod list, enable/disable toggle, and `SetModEnabledAsync` flow preserved |

### Settings boundary

| Item | Detail |
|------|--------|
| **What** | `ISettingsService` + `SettingsService` (`Load` / `Save`) delegating to `AsherSettings` |
| **Files** | `Asher.Services/Interfaces/ISettingsService.cs`, `Asher.Services/Implementations/SettingsService.cs` |
| **Registration** | Singleton in `App.xaml.cs`; exposed on `AsherServiceHost.Settings` |

### Consumers migrated

| Consumer | Change |
|----------|--------|
| `MainWindowViewModel` | `ISettingsService` for install-state reload |
| `SettingsViewModel` | `ISettingsService` for load/save |
| `InstallationProgressViewModel` | `ISettingsService` for `MarkAsInstalled` |
| `InstallationResultViewModel` | `ISettingsService` for `MarkAsInstalled` |
| `UninstallProgressViewModel` | `ISettingsService` for `MarkAsUninstalled` |
| `App.xaml.cs` | Pre-DI bootstrap uses `new SettingsService()`; DI resolves `ISettingsService` in `OnInitialized` |
| `Asher.Host/Program.cs` | Smoke uses `host.Settings.Load()` |

**Not migrated** (out of scope): `GameFolderService`, `GameLaunchService` still call `AsherSettings.Load()` internally.

### Compatibility

- Settings file path, JSON format, defaults, and `MarkAsInstalled` / `MarkAsUninstalled` semantics unchanged.
- `Asher.Host` initializes via `AsherServiceHost` and reads settings through `ISettingsService`.
- WPF `PatchManagerView` bindings unchanged (same property names on item view model).

### Build / test

| Project | Configuration | Result |
|---------|---------------|--------|
| `Asher.Host` | Debug \| x86 | **Success** |
| `Asher.App` | Debug \| x86 | **Success** |

Host read-only smoke: service init, `ISettingsService.Load()`, mode resolution, mod discovery, installation state — **OK** (no game on dev machine).

No automated test project in solution.

### Manual validation

| Check | Result |
|-------|--------|
| Settings load/save via service | **OK** (host smoke + unchanged `AsherSettings` implementation) |
| `ManagedModInfo` data from `GetModsAsync` | **OK** (host; 0 mods without game folder) |
| Mod toggle UI | **Not manually run** — requires WPF session with installed game/mods |
| Install/uninstall settings updates | **Not manually run** — requires full install flow |

### Remaining presentation coupling

- `NavigationItem : BindableBase` in Core
- `HarmonyPatchInfo : BindableBase` in Core
- ViewModels still hold `AsherSettings` instances returned by `ISettingsService` (type not abstracted)
- `GameFolderService` / `GameLaunchService` direct `AsherSettings.Load()`
- Duplicate DI wiring (`App.xaml.cs` vs `AsherServiceHost`) — resolved in Step 5

---

## Step 5 — Unified Application Service Composition

### Previous duplication

`App.xaml.cs` and `AsherServiceHost.Create()` each registered/constructed the same nine application services (`ISettingsService` through `IManagerLaunchService`) with identical dependency order. WPF additionally registered `INavigationItemsManager`, `IThemeService`, and `MainWindow`.

### Shared composition

| File | Role |
|------|------|
| `Asher.Services/Hosting/ApplicationServices.cs` | Single factory (`Create()`) with explicit construction order |
| `Asher.Services/Hosting/ApplicationServiceRegistration.cs` | Registers pre-built instances via `Action<Type, object>` (no Prism/WPF) |
| `Asher.App/Hosting/PrismApplicationServiceRegistration.cs` | Thin Prism bridge calling shared registration |

Both hosts call `ApplicationServices.Create()`:

- **`Asher.Host`** — `AsherServiceHost` wraps `ApplicationServices` and exposes service properties.
- **`Asher.App`** — `RegisterTypes` calls `PrismApplicationServiceRegistration.RegisterApplicationServices`, which registers the same instances with Prism via `RegisterInstance`.

### WPF-only registrations (remain in `App.xaml.cs`)

- `MainWindow`
- `INavigationItemsManager` / `NavigationItemsManager`
- `IThemeService` / `ThemeService`
- Pre-DI bootstrap in `OnStartup` still uses `new SettingsService()` for language/theme before the container is built.

### Build / smoke

| Project | Result |
|---------|--------|
| `Asher.Host` Debug \| x86 | **Success** |
| `Asher.App` Debug \| x86 | **Success** |

Host read-only smoke: service init, settings, mode resolution, game detection, mod discovery, installation state — **OK**. No WPF application started.

### Remaining DI limitations

- Pre-startup WPF bootstrap bypasses `ApplicationServices` (settings-only `new SettingsService()`).
- Prism bridge lives in `Asher.App`; shared layer uses delegate-based instance registration, not a container abstraction.
- `GameFolderService` / `GameLaunchService` still call `AsherSettings.Load()` internally.

---

## Step 6 — Application Contract

### Contract introduced

| File | Role |
|------|------|
| `Asher.Services/Application/IAsherApplication.cs` | Application boundary interface |
| `Asher.Services/Application/AsherApplication.cs` | In-process facade over `ApplicationServices` |
| `Asher.Services/Application/ApplicationMode.cs` | `InstallWizard` / `Manager` |
| `Asher.Services/Application/OperationResult.cs` | Success/failure for launch and mod toggle |

`AsherServiceHost` exposes `IAsherApplication Application`. Individual service properties remain for internal/WPF use but `Asher.Host` smoke now uses the contract only.

### Operations exposed

| Area | Methods |
|------|---------|
| Settings | `GetSettings`, `SaveSettings` |
| Application context | `GetApplicationMode`, `ResolveGameFolderPath` |
| Game detection/validation | `DetectGameFolder`, `GetGameFolderInfo`, `IsGameInstalled`, `HasRestorableBackup` |
| Mods | `GetModsAsync`, `SetModEnabledAsync` |
| Install/uninstall | `InstallAsync`, `UninstallAsync` |
| Launch | `LaunchGame` |

Not exposed: Harmony patch catalog/install, shortcuts, manager deploy/relaunch, installation wizard session state (`IInstallationStateService`).

### Services hidden

`AsherApplication` delegates to `ISettingsService`, `IGameFolderService`, `IGameInstallationService`, `IGameLaunchService`, and `IPatchManagerService`. Existing service implementations unchanged.

### Result / progress / errors

| Operation | Representation |
|-----------|----------------|
| Install / uninstall | Existing `InstallationResult`; `IProgress<InstallationProgress>` optional |
| Launch / mod toggle | `OperationResult` (`Success`, `ErrorMessage`) |
| Mod list / game info | Existing Core models (`ManagedModInfo`, `GameFolderInfo`) |
| Settings | Existing `AsherSettings` |
| Cancellation | `CancellationToken` on async operations (honored before delegate) |

### Host integration

`Program.cs` exercises `host.Application` for settings, mode, detection, mods, install/uninstall contract presence, and optional `--live` launch/mod toggle.

WPF unchanged — ViewModels still resolve `I*Service` via Prism.

### Build / smoke

| Project | Result |
|---------|--------|
| `Asher.Host` Debug \| x86 | **Success** |
| `Asher.App` Debug \| x86 | **Success** |

Read-only smoke via `IAsherApplication`: settings, mode, detection, mod discovery, install/uninstall contract — **OK**. Install/uninstall/launch/mod toggle not run destructively on dev machine.

### Remaining limitations

- Contract is in-process only; not serializable.
- WPF does not consume `IAsherApplication` yet.
- Settings type is still `AsherSettings` (Core), not a dedicated DTO.
- Wizard session state, shortcuts, and Harmony patch management remain outside the contract.
- `AsherServiceHost` still exposes individual services alongside `Application`.

---

## Step 7 — Stable Application Contract

### Contract audit

| Operation | Was | Now | Boundary notes |
|-----------|-----|-----|----------------|
| `GetSettings` / `SaveSettings` | `AsherSettings` | `ApplicationSettingsDto` | Removed Core persistence type from public surface |
| `DetectGameFolder` / `GetGameFolderInfo` | `GameFolderInfo` | `GameFolderDto` | Plain data; no service references |
| `GetModsAsync` | `ManagedModInfo` | `ManagedModDto` | Plain mod data |
| `InstallAsync` / `UninstallAsync` | `InstallationResult` + `InstallationProgress` | `InstallationResultDto` + `InstallationProgressDto` | `Exception` replaced with `ErrorMessage` string |
| `SetModEnabledAsync` / `LaunchGame` | `OperationResult` | unchanged | Already contract-suitable |
| `GetApplicationMode` | `ApplicationMode` enum | unchanged | Primitive enum |

`IAsherApplication` no longer references `Asher.Core` or `Asher.Core.Models` types.

### DTOs introduced

`Asher.Services/Application/Contracts/`:

- `ApplicationSettingsDto`
- `GameFolderDto`
- `ManagedModDto`
- `InstallationResultDto`
- `InstallationProgressDto`

### Mapping strategy

Internal `ApplicationContractMapper` maps Core ↔ contract DTOs inside `AsherApplication` only. Services continue using Core models unchanged.

### Result / error / progress

| Type | Semantics |
|------|-----------|
| `OperationResult` | `Success` + optional `ErrorMessage` for launch/mod toggle |
| `InstallationResultDto` | `Success`, `Message`, `GameFolderPath`, `ErrorMessage` (from `Exception.Message` when present) |
| `InstallationProgressDto` | `Percentage`, `Message`, `Details` via `IProgress<InstallationProgressDto>` |
| Unexpected exceptions | Still thrown from services; not wrapped into a framework |

### Compatibility

- WPF unchanged — ViewModels still use `I*Service` and Core models.
- `Asher.Host` smoke unchanged at call sites (DTO property names match prior shapes).

### Build / smoke

| Project | Result |
|---------|--------|
| `Asher.Host` Debug \| x86 | **Success** |
| `Asher.App` Debug \| x86 | **Success** |

Read-only host smoke via DTO-based contract: settings, mode, detection, mods — **OK**.

### Remaining limitations

- No serialization attributes or transport layer added.
- Contract assembly still references Core internally via `AsherApplication` mapper.
- WPF does not consume `IAsherApplication` yet.
- `OperationResult` / `ApplicationMode` remain contract types but outside `Contracts` namespace.

---

## Step 8 — Frontend ↔ C# Transport Investigation

Investigation only — no source changes. Builds on frozen `IAsherApplication` + contract DTOs (Steps 6–7) and §12 process-boundary requirements.

### Transport requirements (from contract)

| Need | Source | Transport implication |
|------|--------|---------------------|
| Request/response queries | Settings, detection, mods, mode | JSON RPC or REST mapping 1:1 to contract methods |
| Long-running commands | `InstallAsync`, `UninstallAsync` | Start + progress stream + final `InstallationResultDto` |
| Short commands | Launch, mod toggle, save settings | Single request/response |
| Progress payload | `InstallationProgressDto` | Push stream (or poll) during operation |
| Errors | `OperationResult`, `InstallationResultDto` | Structured JSON; no `Exception` objects |
| Cancellation | `CancellationToken` on contract async methods | **Optional for v1 parity** (§12.4: not implemented in WPF today); transport should allow it later |
| Process lifecycle | Electron owns UI | Electron spawns C# backend; backend exposes readiness |
| Single writer | Settings + game directory | One backend instance per manager session |
| Platform | Windows now; Linux manager later | Mechanism must work on both without separate protocols |

### Approaches considered

| Approach | Summary |
|----------|---------|
| **Local HTTP** (`127.0.0.1`) | C# hosts minimal HTTP server; Electron `fetch` from main/preload |
| **Named pipes** | Windows `\\.\pipe\...`; Node `net.connect` |
| **Unix domain sockets** | Socket path; Windows 10+ AF_UNIX + Linux native |
| **WebSockets** | Persistent socket; often paired with or instead of HTTP |
| **stdin/stdout JSON** | Line-delimited JSON-RPC over stdio |
| **Electron child process + custom TCP** | Localhost TCP with framed messages |
| **gRPC** | HTTP/2 + protobuf; streaming RPCs |
| **Electron.NET** | Embed Chromium inside .NET app |
| **In-process Node native addon** | Load `Asher.Services` from Electron |

### Comparative evaluation

Legend: **Good** / **Fair** / **Poor** / **N/A**

| Concern | Local HTTP | Named pipes | Unix sockets | WebSockets | stdin/stdout | gRPC | Electron.NET | In-process addon |
|---------|------------|-------------|--------------|------------|--------------|------|--------------|------------------|
| Electron integration | **Good** — `fetch`, mature patterns | **Fair** — Node `net`, less documentation | **Fair** — same as pipes | **Good** — `ws` package | **Poor** — fragile piping | **Fair** — grpc-js + codegen | **Poor** — inverts architecture | **Poor** — no direct .NET load |
| C# integration | **Good** — ASP.NET Core minimal APIs | **Good** — `NamedPipeServerStream` | **Good** — `Socket` AF_UNIX | **Good** — ASP.NET Core WS | **Fair** — custom loop | **Good** — Grpc.AspNetCore | **Good** — but wrong host | **Poor** — N-API bridge |
| Request/response | **Good** | **Good** (with framing) | **Good** (with framing) | **Good** | **Fair** | **Good** | N/A | N/A |
| Long-running ops | **Good** — async job + poll/SSE | **Good** — duplex stream | **Good** — duplex stream | **Good** — native push | **Poor** — multiplexing awkward | **Good** — server streaming | N/A | N/A |
| Progress reporting | **Good** — SSE or WS on same server | **Good** — server push frames | **Good** — server push frames | **Good** — primary use case | **Poor** — interleaved lines | **Good** — streaming RPC | N/A | N/A |
| Errors/results | **Good** — HTTP status + JSON body | **Fair** — custom envelope | **Fair** — custom envelope | **Fair** — custom envelope | **Fair** | **Good** | N/A | N/A |
| Cancellation | **Good** — `DELETE /operations/{id}` | **Good** — cancel message | **Good** — cancel message | **Good** | **Poor** | **Good** | N/A | N/A |
| Process lifecycle | **Good** — `/health`, dynamic port | **Good** — connect retry | **Good** — connect retry | **Good** | **Fair** — parse READY line | **Good** | **Poor** — single process | N/A |
| Startup/shutdown | **Good** — spawn, wait for health, `kill` on quit | **Good** | **Good** | **Good** | **Fair** | **Good** | N/A | N/A |
| Serialization | **Good** — JSON ↔ existing DTOs | **Good** — JSON frames | **Good** — JSON frames | **Good** — JSON | **Good** — JSON | **Fair** — protobuf regen | N/A | N/A |
| Windows support | **Good** | **Good** | **Good** (Win10+) | **Good** | **Good** | **Good** | **Good** | **Poor** |
| Linux support | **Good** | **Poor** — different API | **Good** | **Good** | **Good** | **Good** | **Poor** | **Poor** |
| Packaging/distribution | **Good** — ship `Asher.Backend.exe` beside Electron; random localhost port | **Good** — no port conflicts | **Good** — socket path in temp dir | **Good** | **Good** — no port | **Good** | **Poor** — not Electron-first | **Poor** |
| Debugging | **Excellent** — curl, browser, logs | **Fair** — custom tooling | **Fair** | **Fair** | **Poor** | **Fair** | N/A | N/A |
| Security | **Good** — bind loopback only | **Good** — OS ACLs | **Good** — filesystem perms | **Good** — loopback | **Good** | **Good** | N/A | N/A |
| Complexity | **Moderate** | **Moderate–High** (cross-platform) | **Moderate–High** | **Moderate** | **Low** start, **High** edge cases | **High** | **High** (wrong model) | **Very High** |
| Fit for Asher | **High** | **Medium** | **Medium** | **Medium** (as HTTP adjunct) | **Low** | **Medium** | **None** | **None** |

### Long-running operations

Target flow:

```text
Electron                    C# Backend (IAsherApplication)
   │                              │
   │ POST /install                │
   │─────────────────────────────►│ Start InstallAsync (operationId)
   │◄─────────────────────────────│ 202 + { operationId }
   │                              │
   │ GET /operations/{id}/events  │ (SSE) or WS subscribe
   │─────────────────────────────►│ progress → InstallationProgressDto
   │◄── progress, progress, ... ──│
   │◄── complete + result ────────│ InstallationResultDto
```

**Assessment:** Local HTTP with **Server-Sent Events (SSE)** or a single WebSocket for events is the simplest mapping of `IProgress<InstallationProgressDto>`. Named pipes/sockets can do the same with framed messages but require a custom protocol layer. stdin/stdout struggles with concurrent progress + stdin commands.

### Cancellation

| Layer | Status |
|-------|--------|
| WPF v1 | Not implemented |
| `IAsherApplication` | `CancellationToken` parameters exist; honored before delegate |
| Transport | Should support `POST /operations/{id}/cancel` → `CancellationTokenSource.Cancel()` in backend registry |

**Conclusion:** Cancellation is **optional for v1 Electron parity** but the recommended HTTP job model accommodates it without contract changes.

### Process lifecycle

| Phase | Recommended pattern |
|-------|---------------------|
| Start | Electron `main` spawns `Asher.Backend.exe` (hidden console or GUI-less) with dynamic port (`ASPNETCORE_URLS=http://127.0.0.1:0`) |
| Ready | Backend logs or writes `READY <port>`; exposes `GET /health` → 200 when `IAsherApplication` wired |
| Failure | Spawn error, non-200 health, or timeout → show UI error; do not start renderer API calls |
| Alive | Electron `main` holds child PID; restart on unexpected exit (optional) |
| Shutdown | `POST /shutdown` (graceful) then `child.kill()`; backend flushes settings if needed |
| Crash | Electron detects `exit` event; offer retry |

stdin/stdout "READY" handshake works but is weaker for ongoing bidirectional traffic.

### Linux implications

| Mechanism | Linux manager UI |
|-----------|-------------------|
| Local HTTP on loopback | **Works unchanged** |
| Named pipes | Requires **separate** Unix socket implementation |
| Unix domain sockets | **Works**; good Linux story |
| Electron.NET | **Not suitable** for cross-platform Electron UI |
| WebSockets | **Works** on both |

**Conclusion:** Approaches tied to Windows-only pipes are weaker for stated future Linux manager support. Localhost HTTP or UDS with a shared framing protocol are the portable choices.

### Packaging constraints (not a design)

- Electron app bundle must include a **platform-specific** `Asher.Backend` executable (win-x86 today).
- Backend and frontend versions should handshake (`GET /version`) to detect mismatched installs.
- Install/uninstall mutate game directories — backend must run with user privileges (same as today); no elevated transport.
- Single-instance lock should live in the backend (prevent two managers writing settings).
- Dynamic localhost port avoids conflicts with other apps; pass port to Electron via spawn stdout or temp file.

### Contract sufficiency

| Capability | Status | Notes |
|------------|--------|-------|
| Serializable DTOs | **Existing** | Step 7 contract types |
| Semantic operations | **Existing** | `IAsherApplication` covers core v1 ops |
| Operation correlation ID | **Required (transport)** | Not on interface; HTTP adapter assigns `operationId` for install/uninstall |
| Progress streaming | **Required (transport)** | Map `IProgress<T>` to SSE/WS events |
| Health / ready | **Required (transport)** | Host concern, not application contract |
| Wire format version | **Optional** | `GET /version` for compatibility checks |
| `CompleteInstall` aggregate | **Optional** | §12.5 shortcut/relaunch/payload still split in WPF; not on `IAsherApplication` yet |
| Cancellation endpoint | **Optional** | Contract supports token; WPF does not use it today |
| Pub/sub notifications | **Not required** | Command results sufficient for v1 |

**Conclusion:** `IAsherApplication` is **sufficient as the semantic contract**. The transport layer adds **operation handles, progress events, health, and serialization** without redesigning application operations.

### Decision

**Recommended:** **Localhost HTTP** (ASP.NET Core minimal host) with JSON request/response mapping to `IAsherApplication`, **SSE** (or WebSocket) for install/uninstall progress, `GET /health` for readiness, dynamic loopback port negotiated at spawn.

**Alternative:** **Unix domain socket** (Windows AF_UNIX + Linux) with length-prefixed JSON messages and the same logical operation/progress protocol — stronger isolation from port space; more custom plumbing and weaker ad-hoc debugging than HTTP.

**Rejected:**

- **Electron.NET** — hosts Chromium inside .NET; opposite of Electron UI + C# backend split.
- **In-process .NET from Node** — high complexity; fights Electron's Node/Chromium model.
- **stdin/stdout JSON-RPC as primary transport** — poor fit for duplex progress streaming and operational debugging.
- **Raw TCP without HTTP on localhost** — repeats HTTP framing problems with fewer tools.
- **gRPC** for v1 — disproportionate codegen/tooling for a single-client desktop app (reasonable later if schema rigor is needed).

**Next Prototype:** Extend `Asher.Host` (or thin `Asher.Backend`) with a loopback HTTP server exposing `GET /health`, `GET /settings`, `GET /mods`, and `POST /install` returning an `operationId` + SSE progress stream; Electron main process spawns the backend, waits for health, and calls endpoints with `fetch` — no renderer UI required for the spike.

> **Note:** Step 9 implemented JSONL over stdin/stdout as the first transport spike instead of HTTP.

---

## Step 9 — JSONL Transport Prototype

### Protocol

One JSON object per line on stdin (requests) and stdout (responses/events). Diagnostics only on stderr.

| Message | Fields |
|---------|--------|
| **Event** | `type: "event"`, `event: "ready"`, `protocolVersion: 1` |
| **Request** | `requestId`, `method`, optional `params` |
| **Response** | `type: "response"`, `requestId`, `success`, optional `result`, optional `error { code, message }` |
| **Progress** | `type: "progress"`, `requestId`, `progress` (`InstallationProgressDto`) |

Error codes: `invalid_request`, `unknown_method`, `application_error`, `cancelled`, `not_found`, `internal_error`.

### Supported methods

`getSettings`, `saveSettings`, `getApplicationMode`, `detectGameFolder`, `getGameFolderInfo`, `resolveGameFolderPath`, `isGameInstalled`, `hasRestorableBackup`, `getMods`, `setModEnabled`, `install`, `uninstall`, `launchGame`, `cancel`, `shutdown`.

Dispatched to `IAsherApplication`; contract DTOs serialized with `System.Text.Json` (camelCase).

### Lifecycle

| Phase | Behavior |
|-------|----------|
| Start | `Asher.Host --jsonl` wires `AsherServiceHost` + emits `ready` event |
| Requests | Sequential processing per line; malformed input → structured error, host continues |
| Progress | `install` / `uninstall` emit `progress` lines before final `response` |
| Cancel | `cancel` + `params.targetRequestId` cancels in-flight CTS; `not_found` if absent |
| Shutdown | `shutdown` method or stdin EOF; waits for in-flight ops |

Default smoke mode (no `--jsonl`) unchanged in `SmokeRunner`.

### Test client

`Asher.Host.TestClient` spawns `Asher.Host --jsonl` and verifies: ready, settings, detection, mods, invalid JSON, install response (safe invalid game info), cancel-not-found, shutdown.

```text
dotnet run --project Asher.Host.TestClient/Asher.Host.TestClient.csproj -c Debug -p:Platform=x86
```

### Build / validation

| Project | Result |
|---------|--------|
| `Asher.Host` | **Success** |
| `Asher.App` | **Success** |
| `Asher.Host.TestClient` | **Success**; protocol checks **passed** |

### Limitations

- Requests processed sequentially (long install blocks other requests).
- Mid-flight install/uninstall cancellation is protocol-level only; services do not observe `CancellationToken` during `Task.Run` work.
- Progress not exercised end-to-end on dev machine (install fails fast with invalid `gameInfo`; no destructive install test).
- stdout must remain protocol-clean; no mixed human logging in JSONL mode.
- WPF still does not use JSONL transport.

---

## Validation

### Build

| Project | Configuration | Result |
|---------|---------------|--------|
| `Asher.Host` | Debug \| x86 | **Success** (0 errors; pre-existing nullable warnings in Core/Services) |
| `Asher.App` | Debug \| x86 | **Success** |

Commands:

```text
dotnet build Asher.Host/Asher.Host.csproj -c Debug -p:Platform=x86
dotnet build Asher.App/Asher.App.csproj -c Debug -p:Platform=x86
```

### Manual smoke (`Asher.Host`)

Default (read-only):

```text
dotnet run --project Asher.Host/Asher.Host.csproj -c Debug -p:Platform=x86
```

| Operation | Result |
|-----------|--------|
| Service host init (no WPF) | **OK** |
| `AsherSettings.Load()` | **OK** |
| Application mode resolution | **OK** (install-wizard on machine with no game configured) |
| `DetectGameFolder` / `ResolveGameFolderPath` | **OK** (no game found on dev machine — expected) |
| `GetModsAsync` | **OK** (0 mods — no resolved game folder) |
| `IInstallationStateService` set/get | **OK** |
| Install / uninstall / launch | **Skipped** (read-only mode) |

Optional live flags (not run in CI smoke):

| Flag | Operation |
|------|-----------|
| `--live --launch` | `TryLaunchGame` |
| `--live --toggle-mod --mod <FileName.dll>` | Mod disable/enable round-trip |
| `--live --install` / `--uninstall` | Documented skip — destructive, manual only |

### WPF regression

`Asher.App` builds successfully after changes. No WPF source files were modified.

---

## Problems / Limitations

1. **No game installation on dev machine** — detection/mod listing returned empty; full mod/install/launch paths require a machine with Dust + Asher installed.
2. **Transitive UI assemblies** — Host still loads `Asher.Core` (`UseWPF`, MaterialDesign, Prism) even though it does not start WPF. Full decoupling deferred per investigation §11.8.
3. **Duplicate DI wiring** — ~~WPF (`App.xaml.cs`) and `AsherServiceHost` both construct services; drift risk until unified.~~ Resolved in Step 5 via `ApplicationServices`.
4. **Mutating operations** — Install/uninstall not automated in smoke to avoid destructive changes.
5. **No automated test project** — validation is manual console output only.

---

## Decisions

| Decision | Rationale |
|----------|-----------|
| Add `AsherServiceHost` in `Asher.Services` | Keeps wiring next to implementations; host project stays thin |
| Do not modify WPF `App.xaml.cs` in this step | Preserve existing app; avoid Prism refactor scope |
| Read-only default smoke | Safe validation without game directory mutation |
| `net8.0-windows` for host | Required to reference `Asher.Services` / `Asher.Core` |
| Exclude `INavigationItemsManager` from host | Presentation-only; not part of application contract |

---

## Remaining Work

- [x] POCO-ify `ManagedModInfo` (remove `BindableBase` from Core)
- [x] Add `ISettingsService` wrapper; remove direct `AsherSettings` from ViewModels
- [x] Unify WPF DI with `AsherServiceHost` or shared registration module
- [x] Introduce in-process `IAsherApplication` contract
- [ ] Live validation on machine with Asher-installed game (install, launch, mod toggle, uninstall)
- [x] Freeze §12.5 contract as serializable API surface
- [x] IPC / transport investigation (separate phase)
- [x] JSONL transport prototype (`Asher.Host --jsonl` + test client)
- [ ] Electron UI (separate phase)

---

## Changelog

| Date | Step | Summary |
|------|------|---------|
| 2026-08-28 | 9 | JSONL stdin/stdout transport; `Asher.Host.TestClient` protocol validation |
| 2026-08-28 | 8 | Transport investigation; recommend localhost HTTP + SSE |
| 2026-08-28 | 7 | Contract DTOs; `IAsherApplication` no longer exposes Core types |
| 2026-08-28 | 6 | `IAsherApplication` facade; host smoke uses application contract |
| 2026-08-28 | 5 | `ApplicationServices` shared composition; WPF + host consume same factory |
| 2026-08-28 | 4 | `ManagedModInfo` POCO + `ManagedModItemViewModel`; `ISettingsService`; ViewModel/host migration |
| 2026-08-28 | 1 | `AsherServiceHost` + `Asher.Host` spike; build + read-only smoke validated |
