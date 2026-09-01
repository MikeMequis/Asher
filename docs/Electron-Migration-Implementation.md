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

## Step 10 — Electron Frontend Spike

### Structure

| Path | Role |
|------|------|
| `Asher.Electron/package.json` | Electron app entry (`npm start`) |
| `Asher.Electron/src/main/main.js` | Window lifecycle, IPC handlers, host startup |
| `Asher.Electron/src/main/host-manager.js` | Spawns/monitors `Asher.Host --jsonl` |
| `Asher.Electron/src/main/jsonl-client.js` | JSONL parse, `requestId` correlation, progress |
| `Asher.Electron/src/main/resolve-host-path.js` | Dev executable resolution |
| `Asher.Electron/src/preload/preload.js` | `contextBridge` IPC surface (`window.asher`) |
| `Asher.Electron/src/renderer/` | Minimal spike UI (HTML/CSS/JS) |
| `Asher.Electron/scripts/smoke-test.mjs` | Headless protocol validation (no GUI) |

Renderer cannot access Node APIs or spawn the C# process directly (`contextIsolation`, `sandbox`, no `nodeIntegration`).

### C# host integration

- Electron main spawns `Asher.Host.exe --jsonl` as a child process with piped stdin/stdout/stderr.
- Host path resolved from repo build output (`Asher.Host/bin/x86/Debug/net8.0-windows/`) or `ASHER_HOST_PATH`.
- Readiness: waits for `{ "type": "event", "event": "ready" }` (30s timeout).
- Shutdown: `shutdown` JSONL method on app quit, then stdin close + process exit wait (5s kill fallback).
- Unexpected host exit → `terminated` status surfaced to renderer.

### Renderer ↔ Main communication

Preload exposes:

- `getHostStatus()` / `startHost()`
- `invoke(method, params?, { trackProgress?, allowFailure? })`
- `onHostStatusChanged(callback)` / `onProgress(callback)`

Main process owns all JSONL and process details; renderer only sees application operations and status.

### JSONL integration

`JsonlClient` mirrors `Asher.Host.TestClient` behavior:

- Line-buffered stdout parsing
- Pending map keyed by `requestId`
- `progress` messages forwarded via IPC before matching `response`
- Malformed JSON logged as protocol errors (host continues)
- `allowFailure` option for install probe responses (`success: false` with `application_error`)

### First functional flow

**Primary (UI):** `detectGameFolder` — read-only game detection displayed as JSON.

**Secondary (UI):** `getSettings` — validates multiple sequential requests.

**Progress verification:** `install` with invalid temp `GameFolderDto` (same safe probe as test client); UI shows progress event count + final response without mutating a real game install.

### Error / lifecycle handling

| Scenario | Behavior |
|----------|----------|
| Host exe missing | `error` status + retry button |
| Ready timeout | Host killed; error shown |
| Request while unavailable | IPC throws user-facing message |
| Application errors | Surfaced in UI (no stack traces) |
| Host crash | `terminated` status + retry |
| App exit | Graceful `shutdown` then process cleanup |

### Validation

| Check | Result |
|-------|--------|
| `Asher.Host` build | **Success** |
| `Asher.App` build | **Success** |
| `Asher.Host.TestClient` | **Passed** |
| `npm run smoke` (headless) | **Passed** — ready, settings, detection, install probe response, multi-request, shutdown |
| Progress events on dev machine | **Not observed** (install fails fast before reporting — same as test client WARN) |
| WPF regression | **No WPF source changes** |

### Development

**Prerequisites:** Build the C# host first.

```text
dotnet build Asher.Host/Asher.Host.csproj -c Debug -p:Platform=x86
```

**Install and run Electron:**

```text
cd Asher.Electron
npm install
npm start
```

**Headless smoke (no window):**

```text
cd Asher.Electron
npm run smoke
```

**Host path override:**

```text
set ASHER_HOST_PATH=C:\path\to\Asher.Host.exe
npm start
```

### Limitations

- Dev-only host path heuristics; no production packaging.
- No request cancellation UI; protocol supports `cancel` but not exposed.
- Sequential host request processing (long install blocks other ops).
- Progress may not appear on machines where install fails before first progress report.
- No routing/navigation, theming, or WPF feature parity.
- WPF remains the shipping GUI; Electron is an independent spike frontend.

---

## Step 11 — Electron Game Setup Flow

### WPF flow analyzed

| WPF screen | Behavior extracted |
|------------|-------------------|
| `WelcomeViewModel` | Entry to install wizard |
| `GameDetectionViewModel` | Auto-detect on navigate; browse folder; validate via `IGameFolderService.GetInfo`; show valid/invalid; session state via `IInstallationStateService.SetGameFolder`; continue → create patches folder → install progress |
| `MainWindowViewModel` | `InstallWizard` vs `Manager` mode from installed game path |

Electron setup scope stops before install progress (out of scope). It reproduces folder selection, validation, and configuration persistence.

### Electron flow implemented

```text
Host ready
    ↓
getApplicationMode + getSettings
    ↓
Saved valid path? ──yes──► Ready view
    │
    no
    ↓
Setup view (detect / browse / validate)
    ↓
getGameFolderInfo
    ↓
valid? ──no──► invalid state (remain in setup)
    │
    yes
    ↓
saveSettings (gameFolderPath + gameVersion)
    ↓
Ready view
```

UI states: `idle`, `validating`, `valid`, `invalid`, `saving`, `ready`, `error`.

### Application contract

**No contract changes.** Existing JSONL methods used:

| Operation | JSONL method |
|-----------|--------------|
| Load mode/settings | `getApplicationMode`, `getSettings` |
| Auto-detect | `detectGameFolder` |
| Validate path | `getGameFolderInfo` |
| Persist | `saveSettings` |

Renderer never touches `AsherSettings` directly.

### Settings interaction

On save: `getSettings` → merge `gameFolderPath` and `gameVersion` from validated `GameFolderDto` → `saveSettings`. On startup: reload via `getSettings` + revalidate with `getGameFolderInfo`.

### Validation behavior

All folder checks delegated to C# (`getGameFolderInfo` / `detectGameFolder`). Invalid folder message mirrors WPF: *"Could not find DustAET.exe in this folder."*

### Error handling

| Kind | User message |
|------|--------------|
| Host unavailable | Cannot connect; retry button |
| Invalid folder | Validation message; remain in setup |
| Application error | Message from host `error.message` |
| Communication failure | Generic retry message |

No stack traces in UI.

### New Electron pieces

| File | Role |
|------|------|
| `src/renderer/game-setup.js` | Setup state machine |
| `src/renderer/app.js` | Host status + view routing |
| `src/main/main.js` | `dialog:pick-folder` IPC (OS folder picker only) |
| `scripts/setup-smoke-test.mjs` | Headless setup/persistence validation |

Spike debug panels (raw JSON/progress probe) removed from renderer.

### Build / test results

| Check | Result |
|-------|--------|
| `Asher.Host` build | **Success** |
| `Asher.App` build | **Success** |
| `Asher.Host.TestClient` | **Passed** |
| `npm run smoke` | **Passed** |
| `npm run smoke:setup` | **Passed** — mode, invalid validation, save/restore persistence |
| WPF source changes | **None** |

### Persistence validation

`setup-smoke-test.mjs` saves a probe `gameFolderPath`, verifies reload via `getSettings`, restores original settings.

Manual valid-folder path requires a machine with Dust installed (not available on dev machine).

### Remaining differences from WPF

- WPF stores validated folder in `IInstallationStateService` (session) until install completes; Electron persists `gameFolderPath` to settings immediately on save.
- WPF `CreatePatchesFolder` on continue not exposed on `IAsherApplication` — not called from Electron.
- No Welcome screen or multi-step install wizard navigation.
- No installation/uninstall UI.
- English-only strings (WPF uses localization resources).

---

## Step 12 — Electron Mod Manager

### WPF Manager behavior analyzed

| WPF | Behavior |
|-----|----------|
| `PatchManagerViewModel` | Loads mods on navigate via `GetModsAsync`; refresh command; toggle calls `SetModEnabledAsync` on `IsEnabled` change; tracks active/total counts |
| `PatchManagerView` | Lists mod name, description, enabled toggle; empty state; refresh button; active/total stats |
| `ManagedModItemViewModel` | Maps `FileName`, `Name`, `Description`, `IsEnabled` from service |

### Electron Manager implementation

| File | Role |
|------|------|
| `src/renderer/mod-manager.js` | Load/toggle state machine |
| `src/renderer/app.js` | Setup ↔ Manager navigation |
| `src/renderer/index.html` | Manager list UI |
| `scripts/manager-smoke-test.mjs` | Headless getMods / setModEnabled validation |

### Contract operations used

**No contract changes.**

| Operation | JSONL method |
|-----------|--------------|
| Load mods | `getMods` |
| Toggle mod | `setModEnabled` (`fileName`, `enabled`) |
| Config gate | `getApplicationMode`, `getSettings`, `getGameFolderInfo` (via setup controller) |

`OperationResult.success` checked in renderer after toggle; protocol-level `success` remains `true`.

### Navigation / state

- Nav bar (`Setup` / `Mod Manager`) appears when game folder is configured.
- Manager requires valid configured folder; otherwise redirects to Setup.
- Ready view includes **Open Mod Manager**.
- Manager states: `loading`, `loaded`, `empty`, `error`, plus per-mod `toggling` and `toggleError`.

### Mod loading

On Manager entry: `getMods` → render DTO list or empty state. Refresh button reloads. No filesystem access from JavaScript.

### Toggle behavior

Toggle → `setModEnabled` → check `result.success` → update UI or revert checkbox and show error. All mods disabled while a toggle is in flight.

### Validation results

| Check | Result |
|-------|--------|
| `Asher.Host` / `Asher.App` build | **Success** |
| `Asher.Host.TestClient` | **Passed** |
| `npm run smoke` / `smoke:setup` | **Passed** |
| `npm run smoke:manager` | **Passed** — empty mod list, failed toggle on nonexistent mod |
| Live mod toggle | **Not run** — no Dust/Asher install on dev machine |
| WPF changes | **None** |

### Limitations

- Zero mods returned on dev machine (expected without installed game/mods folder).
- No Harmony patch catalog UI (WPF Patch Manager is mod DLLs only; Harmony manager is separate WPF screen).
- No localization; English strings only.
- Sequential toggle lock (one mod at a time) — matches host sequential processing.

### Remaining differences from WPF

- No auto-load on tab switch beyond explicit `loadMods` on navigate/refresh.
- Toggle is checkbox + label, not MaterialDesign switch.
- No patch info cards styling; minimal list layout.

---

## Step 13 — Electron Application Shell

### Startup lifecycle

```text
booting → connecting (host starting) → loading-app (fetch C# state) → ready
                                                              ↘ host-error
```

1. `ApplicationShell.start()` subscribes to host status.
2. Main process starts `Asher.Host` (unchanged).
3. On `ready`, shell calls `fetchApplicationState()` (`getApplicationMode`, `getSettings`, `getGameFolderInfo`).
4. Renderer content hidden until shell phase is `ready` or `host-error`.
5. Loading overlay shown during `booting` / `connecting` / `loading-app`.

### Application mode handling

`application-state.js` derives `recommendedScreen` from C#:

- `getGameFolderInfo` valid path → `manager`
- otherwise → `setup`

Initial route uses `recommendedScreen`. Setup → Manager after save calls `refreshApplicationState()` then navigates only if `isConfigured` is true from C#.

### Navigation

| Screen | When |
|--------|------|
| Setup | Unconfigured, or user editing configuration |
| Manager | C# reports valid configured folder |

Nav bar (`Setup` / `Mod Manager`) visible when application is ready. Manager nav disabled when not configured.

### Shared state

| Module | Role |
|--------|------|
| `application-shell.js` | Phase, host status, app state, screen, errors |
| `application-client.js` | Preload bridge wrapper |
| `application-state.js` | C# state fetch |
| `errors.js` | Shared error classification |
| `game-setup.js` | Setup operations only |
| `mod-manager.js` | Mod load/toggle only |
| `app.js` | View rendering |

No external state framework.

### Host lifecycle

Host spawn/JSONL remains in main process. Renderer uses `ApplicationClient` only — no direct process or JSONL knowledge.

Runtime termination → `host-error` phase, content hidden, retry button (manual, no auto-restart loop).

### Failure handling

| Case | Behavior |
|------|----------|
| Startup failure | `host-error` + message + Retry |
| Runtime termination | Same |
| App state load failure | `host-error` |
| Manager config lost | `handleConfigurationLost()` → refresh C# state → Setup with message |

### Validation

| Check | Result |
|-------|--------|
| `Asher.Host` / `Asher.App` build | **Success** |
| `Asher.Host.TestClient` | **Passed** |
| `npm run smoke` / `smoke:setup` / `smoke:manager` | **Passed** |
| `npm run smoke:shell` | **Passed** |
| WPF changes | **None** |

### Limitations

- No automatic host restart.
- Window shown before host ready (loading overlay covers gap).
- English-only shell strings.

---

## Step 14 — Electron Asher Installation Flow

### WPF behavior analyzed

| WPF | Behavior |
|-----|----------|
| `InstallationProgressViewModel` | Auto-starts `InstallAsync` on navigate; reports progress; on success calls `MarkAsInstalled`; navigates to result |
| `InstallationProgressView` | Progress %, status message, step details, indeterminate start |
| `InstallationResultViewModel` | Success/failure display; finish triggers shortcuts/relaunch (out of Electron scope) |

### Contract sufficiency

**No contract changes.** Used existing JSONL methods:

| Operation | Method |
|-----------|--------|
| Start install | `install` (`GameFolderDto`) |
| Progress | JSONL `progress` lines (same `requestId`) |
| Cancel | `cancel` (`targetRequestId`) |
| Post-install settings | `getSettings` + `saveSettings` (`isInstalled`, paths) |
| Mode refresh | `getApplicationMode` via `fetchApplicationState()` |

### Electron implementation

| File | Role |
|------|------|
| `installation-controller.js` | Install lifecycle state machine |
| `application-client.js` | `onOperationStarted`, `onProgress`, `cancel()` |
| `main/jsonl-client.js` | `onStarted(requestId)` callback |
| `main/main.js` | Broadcasts `asher:operation-started` + `requestId` on progress |
| `app.js` + `index.html` | Install screen UI |

Shell screen `install` added; entered from Setup ready view or after saving configuration when `needsInstallation` (`installWizard` + valid folder).

### Progress handling

`trackProgress: true` on install invoke; progress filtered by `requestId` in `InstallationController`. Final UI state driven by install response `InstallationResultDto.success`, not progress percentage alone.

### Cancellation

Cancel button → `cancelling` state → `cancel` JSONL → install response with `cancelled` error code → `cancelled` state. No process kill.

### Error handling

| Case | UI state |
|------|----------|
| Invalid game folder | `failed` before install starts |
| Application error | `failed` + message from result |
| Cancelled | `cancelled` |
| Host unavailable | `failed` via shared error classifier |

### Post-installation

On `completed`: `saveSettings` marks installed (mirrors WPF `MarkAsInstalled` fields) → `shell.refreshApplicationState()` → navigate to Manager → load mods.

### Validation

| Check | Result |
|-------|--------|
| C# builds + `TestClient` | **Passed** |
| All existing `npm run smoke*` | **Passed** |
| `npm run smoke:install` | **Passed** — requestId, failure path, cancel not_found |
| Live install with real game | **Not run** — no Dust install on dev machine |
| Live cancel mid-install | **Not run** — install fails fast without game |

### Limitations

- No installation result shortcuts/relaunch/payload deploy (WPF `InstallationResultViewModel` scope).
- Progress may not appear when install fails before first progress report.
- `MarkAsInstalled` replicated via `saveSettings` DTO fields (not a dedicated contract method).
- English-only install strings.

---

## Step 15 — Electron Asher Uninstallation Flow

### WPF behavior analyzed

| WPF | Behavior |
|-----|----------|
| `SettingsViewModel.ExecuteUninstallCommand` | `MessageBox` Yes/No confirmation before navigate |
| `UninstallProgressViewModel.OnNavigatedTo` | Validates `IsGameInstalled` + `HasRestorableBackup`; auto-starts `UninstallAsync` |
| `UninstallProgressView` | Progress %, message, details (same `InstallationProgress` model as install) |
| Post-success | `MarkAsUninstalled()` + `UninstallCompleteEvent` → shell returns to install-wizard mode |

Electron mirrors install flow structure: confirmation in renderer (not C#), operation via JSONL `uninstall`, settings update via `saveSettings`.

### Contract sufficiency

**No contract changes.** Reused existing JSONL methods:

| Operation | Method |
|-----------|--------|
| Eligibility | `isGameInstalled`, `hasRestorableBackup` (via `fetchApplicationState`) |
| Start uninstall | `uninstall` (`GameFolderDto`) |
| Progress | JSONL `progress` lines (same `requestId`) |
| Cancel | `cancel` (`targetRequestId`) |
| Post-uninstall settings | `getSettings` + `saveSettings` (`isInstalled: false`, `installationDate: null`) |
| Mode refresh | `getApplicationMode` via `fetchApplicationState()` |

`canUninstall` is derived in Electron (`manager` mode + installed + restorable backup), matching WPF pre-checks.

### Electron implementation

| File | Role |
|------|------|
| `uninstallation-controller.js` | Uninstall lifecycle state machine |
| `application-state.js` | `canUninstall` flag |
| `application-shell.js` | `uninstall` screen, `handleUninstallComplete()` |
| `app.js` + `index.html` | Uninstall UI; Manager “Uninstall Asher” button |

Flow: Manager → Uninstall screen → confirming → starting → uninstalling → completed/failed/cancelled. Cancel confirmation returns to Manager without starting operation.

### Confirmation

Dedicated confirm panel on uninstall screen; Manager button hidden unless `canUninstall`. Shell blocks navigation to uninstall when not eligible.

### Progress handling

`trackProgress: true` on uninstall invoke; progress filtered by `requestId` in `UninstallationController`. Final state driven by `InstallationResultDto.success`, not progress alone.

### Cancellation

Cancel button → `cancelling` → `cancel` JSONL → response with `cancelled` error code → `cancelled` state. No process kill.

### Error handling

| Case | UI state |
|------|----------|
| No configured folder | `failed` before uninstall starts |
| Not installed / no backup | `failed` before uninstall starts |
| Application error | `failed` + message from result |
| Cancelled | `cancelled` |
| Host unavailable | `failed` via shared error classifier |

### Post-uninstall state

On `completed`: `saveSettings` marks uninstalled (mirrors WPF `MarkAsUninstalled`) → `shell.refreshApplicationState()` → navigate to `recommendedScreen` (Setup when install wizard needed).

### Validation

| Check | Result |
|-------|--------|
| C# builds + `TestClient` | **Passed** |
| All existing `npm run smoke*` | **Passed** |
| `npm run smoke:uninstall` | **Passed** — requestId, failure path, `isGameInstalled` / `hasRestorableBackup` |
| Live uninstall with real game | **Not run** — no Dust install on dev machine |
| Live cancel mid-uninstall | **Not run** — uninstall fails fast without game |
| Confirm cancel (no operation) | **Wired** — cancel on confirm panel resets to idle / back to Manager |

### Limitations

- No live end-to-end uninstall on dev machine; smoke validates protocol and failure paths only.
- Progress may not appear when uninstall fails before first progress report.
- `MarkAsUninstalled` replicated via `saveSettings` DTO fields (not a dedicated contract method).
- English-only uninstall strings.

---

## Step 16 — Remaining WPF Functionality Audit

Investigation only — no code changes.

### WPF inventory

| View / flow | ViewModel | User actions | Status |
|-------------|-----------|--------------|--------|
| `WelcomeView` | `WelcomeViewModel` | Begin install wizard | Partial — Electron uses Ready view + direct install entry |
| `GameDetectionView` | `GameDetectionViewModel` | Auto-detect, browse, validate, continue | Migrated — `game-setup.js` / Setup view |
| `InstallationProgressView` | `InstallationProgressViewModel` | Auto-install, progress, cancel | Migrated — `installation-controller.js` |
| `InstallationResultView` | `InstallationResultViewModel` | Finish, retry, cancel; shortcut; relaunch/payload | Partial — success/failure only; finish extras out of scope |
| `HomeView` | `HomeViewModel` | Hub cards; launch game; navigate | Missing — no Electron home |
| `PatchManagerView` | `PatchManagerViewModel` | Load/refresh mods; enable/disable | Migrated — Mod Manager |
| `SettingsView` | `SettingsViewModel` | Path, prefs, reset, uninstall entry | Partial — path via Setup; uninstall via Manager |
| `UninstallProgressView` | `UninstallProgressViewModel` | Auto-uninstall, progress, cancel | Migrated — `uninstallation-controller.js` |
| `ContentPatcherView` | `ContentPatcherViewModel` | Add replacement (stub) | Out of scope — WPF TODO, no service |
| `MainWindow` shell | `MainWindowViewModel` | Sidebar nav; install vs manager mode; localization | Partial — simpler Setup/Manager nav; English only |

Dialogs: folder picker (Setup/Settings), uninstall confirm (Settings MessageBox / Electron confirm panel), launch/error MessageBoxes.

### Electron comparison

| WPF capability | Electron equivalent | Status | Notes |
|----------------|---------------------|--------|-------|
| Game folder detect/validate/save | Setup + `game-setup.js` | Migrated | Persists on save; WPF used session state until install |
| Install + progress + cancel | Install screen + `installation-controller.js` | Migrated | Auto-starts on screen enter |
| Install result (basic) | Install completed/failed/cancelled panels | Partial | No next-steps, shortcut, relaunch, payload deploy |
| Mod list + toggle + refresh | Manager + `mod-manager.js` | Migrated | Same `getMods` / `setModEnabled` |
| Uninstall + progress + cancel | Uninstall screen + `uninstallation-controller.js` | Migrated | Entry from Manager footer, not Settings |
| Application mode / shell | `application-shell.js` | Migrated | Host lifecycle, C#-driven navigation |
| Home dashboard | — | Missing | No hub or quick-action cards |
| Launch game | — | Supported but not exposed | JSONL `launchGame` exists; no UI |
| Settings (language, theme, toggles, reset) | — | Partial | `saveSettings` DTO supports fields; only path/version edited today |
| Change game folder (Settings browse) | Ready → reconfigure → Setup | Partial | Same picker; not a dedicated Settings screen |
| Install wizard welcome / stepper nav | Ready view; Setup/Manager nav | Partial | Functionally adequate; less onboarding chrome |
| Localization (EN/PT/ES) | English hardcoded | Missing | WPF `LocalizationManager` |
| Theme (Light/Dark) | Static CSS | Missing | WPF `IThemeService` |
| Content patcher | — | Out of scope | WPF stub only |
| Desktop shortcut / manager relaunch / payload | — | Out of scope | WPF-only services; not on `IAsherApplication` |

### Missing capabilities — contract coverage

| Requirement | Classification |
|-------------|----------------|
| Launch game | **Supported but not exposed by Electron** — `launchGame` on contract + JSONL |
| Settings prefs (language, theme, auto-launch, backup, updates) | **Supported but not exposed** — `getSettings` / `saveSettings` DTO fields |
| Game path change | **Supported but not exposed** — same setup contract ops; partial UI |
| Install/uninstall markers | **Supported but not exposed** — Electron merges `saveSettings` (no dedicated API) |
| Create patches folder on continue | **Missing from contract** — WPF calls `IGameFolderService` directly |
| Desktop shortcut | **Missing from contract** — `IShortcutService` |
| Manager relaunch / payload deploy | **Missing from contract** — `IManagerLaunchService`, `IManagerDeployService` |
| Content patcher | **Intentionally out of scope** — no backend |
| Localization apply | **Missing from contract** — WPF `LocalizationManager` is presentation-layer |
| Theme apply | **Missing from contract** — WPF `IThemeService` is presentation-layer |

### Functional dependencies (missing workflows)

| Workflow | Contract ops | Progress/cancel | State deps | WPF-only deps | Independent? |
|----------|--------------|-----------------|------------|---------------|--------------|
| Launch game | `launchGame` | None | Configured + installed game | `Process.Start`, Windows paths | **Yes** |
| Settings screen | `getSettings`, `saveSettings`, `getGameFolderInfo`, `detectGameFolder` | None | Any mode | Theme/language services | **Yes** |
| Home hub | Navigation only (+ launch) | None | Manager mode | Prism regions | **Yes** (mostly UI) |
| Install result finish | `saveSettings` (partial) | None | Post-install | Shortcut, deploy, relaunch, shutdown | **No** — needs contract extension or explicit exclusion |
| Localization | `saveSettings` (persist only) | None | None | `LocalizationManager` | **Partial** — strings + apply in renderer |
| Content patcher | None | TBD | Mod/game folder | None | **No** — no service exists |

### Migration priority

**Priority A — Core**

| Item | Reason |
|------|--------|
| Launch game | Primary post-install action on WPF Home; contract ready; completes the configure→install→play loop |
| Settings screen (prefs + path) | Expected in normal mode; prefs already in DTO; path reconfigure exists but scattered |

**Priority B — Important**

| Item | Reason |
|------|--------|
| Home / navigation hub | Improves discoverability; WPF groups launch, mods, settings |
| Localization | WPF ships 3 languages; Electron English-only |
| Install result enhancements | Better onboarding; shortcut/relaunch likely stay excluded |

**Priority C — Optional / peripheral**

| Item | Reason |
|------|--------|
| Install welcome / wizard stepper | Cosmetic; Ready + Setup cover function |
| Theme switching | Cosmetic; renderer-local unless contract added |
| Content patcher | Stub in WPF; no backend |
| Shortcut / relaunch / payload | Explicitly deferred; Windows/process-lifetime specific |

### Architectural risks

| Risk | Impact |
|------|--------|
| `saveSettings` merge for install/uninstall markers | Drift if DTO shape changes; no atomic mark-installed API |
| `CreatePatchesFolder` not on contract | Electron skips step WPF runs before install |
| Settings flags persisted but unused by services | UI may imply behavior (`AutoLaunchEnabled`) that C# ignores |
| Shortcut/deploy/relaunch outside contract | Install finish cannot be fully replicated without extension or permanent exclusion |
| WPF `IInstallationStateService` session state | Electron persists earlier — different failure/recovery semantics mid-wizard |
| Theme/language in WPF services | Electron needs renderer-side strategy or new contract surface |
| Windows-only launch/shortcut/detection | Future Linux Electron host needs platform abstraction |
| Prism events (`InstallationCompleteEvent`, etc.) | Already replaced by `refreshApplicationState()` — pattern is sound |

### Migration completeness

```text
Core application workflows:
4 / 5 migrated (1 partial: install result finish)

Secondary workflows:
0 / 4 migrated (home hub, localization, theme, install onboarding chrome)

Known missing functionality:
- Launch game UI
- Settings screen (language, theme, toggles, reset)
- Home dashboard / hub navigation
- Install result finish (shortcut, relaunch, payload, next-steps)

Intentionally excluded:
- Content patcher (WPF stub)
- Desktop shortcut creation
- Manager relaunch / payload deploy / app shutdown
- Production packaging
```

### Recommended next target

> **Superseded (2026-09-01):** Launch game was implemented in Step 17. **Step 18 is now the migration gate** — payload bundling, contract additions, and fresh-install validation before further UI (settings, home/hub). See Step 18.

**Launch game** — best next step *(completed Step 17)*.

- Contract and JSONL already expose `launchGame`; no protocol change.
- Reuses existing shell, error classifier, and configured-state checks.
- Tests the primary “use Asher to play” path independent of new screens.
- Safe to implement alone: single invoke, no progress/cancel, no filesystem work in Electron.

**Following targets (deferred until Step 18 gate — see Step 18 “Deferred until gate passes”):**

1. **Settings screen** — unify path + preference editing via existing `getSettings` / `saveSettings`.
2. **Home / hub navigation** — launch + links to Manager and Settings; optional polish.
3. **Localization** — renderer string tables + persist language; apply is presentation concern.

---

## Step 17 — Electron Launch Game

### WPF behavior analyzed

| WPF | Behavior |
|-----|----------|
| `HomeViewModel.LaunchGameCommand` | Calls `IGameLaunchService.TryLaunchGame`; silent return on success; `MessageBox` with error on failure |
| Preconditions | Resolved game folder with Asher installed; executable must exist (`DustAET.exe`) |

No confirmation step; launch is fire-and-forget from the UI.

### Contract usage

**No contract changes.** Used existing JSONL `launchGame` (no params) via `ApplicationClient.invoke`.

Failure returns protocol `application_error` with message from C#; success returns `OperationResult` with `success: true`.

### Electron implementation

| File | Role |
|------|------|
| `launch-game.js` | Invoke `launchGame`; success/error messages |
| `application-state.js` | `canLaunchGame` — manager mode + valid folder + `isGameInstalled` |
| `application-shell.js` | `canLaunchGame` getter |
| `index.html` / `app.js` | **Launch Game** button on Mod Manager header |

Button hidden unless `canLaunchGame`; disabled while launching or host unavailable.

### Validation

| Check | Result |
|-------|--------|
| C# builds + `TestClient` | **Passed** |
| All existing `npm run smoke*` | **Passed** |
| `npm run smoke:launch` | **Passed** — failure path without installed game |
| Live launch with real game | **Not run** — no Dust install on dev machine |

### Windows-specific assumptions (not refactored)

| Assumption | Location |
|------------|----------|
| `DustAET.exe` filename | `AsherPaths.GameExecutableName` |
| `Process.Start` + `UseShellExecute = true` | `GameLaunchService` |
| Game folder resolution via manager install path / settings / detection | `GameLaunchService.ResolveGameFolderPath` |
| `AsherPaths.MigrateLegacyLayout` on resolve | `GameLaunchService` |
| Portuguese error strings from C# | `GameLaunchService` |

Linux port will need platform-specific launch abstraction in C# services; Electron remains a thin invoke.

### Limitations

- No Home/hub screen; launch exposed from Manager only.
- No process monitoring or auto-launch-on-exit.
- `AutoLaunchEnabled` setting not read by launch service.
- Live game start not validated on dev machine.

### Host startup sync fix

Renderer now calls `startHost()` and applies the returned status (not only `getHostStatus` + IPC events). Main no longer starts the host before the window loads; current status is rebroadcast on `did-finish-load`. `HostManager.start()` shares one in-flight promise for concurrent callers.

Preload must be CommonJS (`preload.cjs`) — sandboxed Electron preload cannot use ESM `import` when `package.json` has `"type": "module"`.

Diagnostic log file: `%APPDATA%\\asher-electron\\asher-electron.log` (path also shown in app footer).

---

## Step 18 — Electron-Owned Installation & Distribution

### Gate

**Step 18 is the migration gate.** No further Electron UI work (settings screen, home/hub, localization, install onboarding chrome) should start until Step 18 is complete and validated on a clean game folder.

Rationale:

- Steps 11–17 built install/uninstall UI and flows, but **fresh install cannot succeed** without bundled payload files — UI polish does not unblock the configure→install→play loop for new users.
- End-to-end validation (Setup → Install → Launch → Uninstall on a folder with no prior Asher runtime) is the acceptance test for calling Electron **installable**, not merely a spike against an already-patched game.

**After the gate:** resume Priority B items from Step 16 (settings screen, home/hub). Transport choice (JSONL vs HTTP) for production packaging can be decided during Step 18 packaging work; do not maintain two full transports long-term.

### Decision

**The full Asher installation workflow moves to Electron.** End users should install Asher onto a game folder through the Electron app (Setup → Install), not by running `PrepareDistribution.ps1` and launching `Distribution\Asher.App.exe` first.

`PrepareDistribution.ps1` remains a **WPF legacy / developer packaging** tool until WPF is retired. It is **not** part of the Electron user workflow.

### Current state (validated on game PC)

| Scenario | Works today? | Notes |
|----------|--------------|-------|
| Electron against **already-installed** game | **Yes** | Setup, Manager, mod toggle, launch, uninstall validated when game folder already has Asher runtime |
| Electron **fresh install** from dev checkout | **No** | Install UI exists (`installation-controller.js`) but C# cannot find payload files |
| WPF via `Distribution\` after `PrepareDistribution.ps1` | **Yes** | Original shipping path; unchanged |

### Why fresh Electron install fails today

`GameInstallationService.ResolveInstallSourceFolder()` looks for install payload next to the running application (`AppDomain.CurrentDomain.BaseDirectory`), then game-folder caches. Electron spawns `Asher.Host.exe` from `Asher.Host\bin\x86\Debug\...`, which contains **host dependencies only** — not:

- `Asher.Launcher.exe`
- `Asher.Runtime.dll` / `Asher.SDK.dll` / `0Harmony.dll`
- `DefaultMods\`

Those files are assembled today by `PrepareDistribution.ps1` into `Distribution\` for WPF. Electron does not copy or bundle them yet.

### Target Electron flow (no Distribution script for users)

```text
User runs Electron app
    ↓
Setup (detect / validate / save game folder)
    ↓
Install Asher (in-app progress + cancel)
    ↓
C# InstallAsync (unchanged logic)
    ↓
Manager (mods, launch, uninstall)
```

All install/uninstall behavior stays in C#. Electron only provides UI and invokes `install` / `uninstall` over JSONL.

### Contract additions (planned with Step 18 C# work)

Two small additions to `IAsherApplication` + JSONL — implement alongside payload/host changes, not as a separate phase. WPF continues using `I*Service` directly; only Electron/JSONL consumers required initially.

| Addition | Maps to | Why now |
|----------|---------|---------|
| **`PreparePatchesFolder(gameFolderPath)`** | `IGameFolderService.CreatePatchesFolder` | WPF runs this on Game Detection **Continue** before install; Electron skips it today (Step 11, Step 16 risk). Call from setup continue or install preflight so install matches WPF. |
| **`MarkInstalled(path, version)`** / **`MarkUninstalled()`** | `AsherSettings.MarkAsInstalled` / `MarkAsUninstalled` via `ISettingsService` | Electron today merges install/uninstall flags through `getSettings` + `saveSettings` (Steps 14–15). Dedicated commands reduce DTO merge drift and match §12.5 intent. |

**Out of scope for these additions:** `CompleteInstall` (shortcut, relaunch, payload deploy) — remains deferred per Step 16.

Suggested JSONL methods: `preparePatchesFolder`, `markInstalled`, `markUninstalled`.

### Required work (not yet implemented)

| Item | Purpose |
|------|---------|
| **Contract additions** | `PreparePatchesFolder`, `MarkInstalled`, `MarkUninstalled` on `IAsherApplication` + JSONL (see above) |
| **Install payload bundling** | Ship Launcher, Runtime, SDK, Harmony, and DefaultMods with Electron/Host — not via manual `PrepareDistribution.ps1` step |
| **Build integration** | MSBuild or Electron packaging step copies payload from project `bin` outputs into a known folder (e.g. `Asher.Host/install-payload/` or `Asher.Electron/resources/install-payload/`) |
| **Host payload resolution** | Ensure `ResolveInstallSourceFolder()` finds bundled payload when Host runs from Electron (extend candidate paths or set host working directory — minimal C# change only if bundling beside Host is insufficient) |
| **Production packaging** | `electron-builder` (or equivalent) packages Electron + `Asher.Host` + install payload as one distributable |
| **Dev workflow** | `npm start` works for fresh install after `dotnet build` — no separate Distribution folder step |
| **Electron install flow update** | Call `preparePatchesFolder` before install where WPF did; use `markInstalled` / `markUninstalled` instead of settings merge |

### Explicit non-goals

- Do not require users to run `PrepareDistribution.ps1` before using Electron.
- Do not duplicate `InstallAsync` logic in JavaScript.
- Do not remove `PrepareDistribution.ps1` until WPF is deprecated (keeps WPF release path working).

### Relationship to existing Electron steps

| Step | Status |
|------|--------|
| 11 Setup | Done — folder detect/validate/save; will call `preparePatchesFolder` when contract lands |
| 14 Install UI | Done — progress, cancel, post-install refresh; will switch to `markInstalled` |
| 15 Uninstall UI | Done — will switch to `markUninstalled` |
| 17 Launch | Done |
| **18 Payload + contract + packaging** | **Next (gate)** — closes gap between install UI and installable fresh game |

### Deferred until Step 18 gate passes

| Item | Was planned in | Why wait |
|------|----------------|----------|
| Settings screen (prefs + path) | Step 16 Priority A | No value proving prefs UI until fresh install works |
| Home / hub navigation | Step 16 Priority B | Launch already on Manager; hub is polish |
| Localization | Step 16 Priority B | English-only acceptable until installable build |
| Install welcome / wizard stepper | Step 16 Priority C | Ready + Setup cover function |

### Validation plan (when Step 18 lands)

1. Clean game folder (no `Asher\` runtime).
2. `npm start` only — no `PrepareDistribution.ps1`.
3. Setup → validate → save → **`preparePatchesFolder`** (when implemented) → Install → verify `DustAET.exe` launcher, runtime files, DefaultMods in game folder.
4. Confirm **`markInstalled`** updated settings (not manual `saveSettings` merge).
5. Manager → Launch → play.
6. Uninstall → confirm **`markUninstalled`** → restore backup → return to Setup.

**Gate exit criteria:** all six steps pass on a machine that did not have Asher pre-installed in the target game folder.

---

## Step 18 — Implementation (completed)

### Contract additions

| Method | JSONL | Implementation |
|--------|-------|----------------|
| `PreparePatchesFolder(path)` | `preparePatchesFolder` | `IGameFolderService.CreatePatchesFolder` via `AsherApplication` |
| `MarkInstalled(path, version)` | `markInstalled` | `ISettingsService.MarkAsInstalled` |
| `MarkUninstalled()` | `markUninstalled` | `ISettingsService.MarkAsUninstalled` |

Files: `IAsherApplication.cs`, `AsherApplication.cs`, `ISettingsService.cs`, `SettingsService.cs`, `JsonlProtocol.cs`, `JsonlHostSession.cs`.

### Install payload bundling

| Item | Detail |
|------|--------|
| **MSBuild target** | `Asher.Host/InstallPayload.targets` — copies payload to `bin/.../install-payload/` after each Host build |
| **Payload contents** | `Asher.Launcher.exe`, runtime DLLs, `0Harmony.dll`, `DefaultMods/` (when built) |
| **Sources** | Project `bin` outputs + `packages/Lib.Harmony.2.4.2`; fallback to `Distribution/` when present |
| **Host resolution** | `GameInstallationService.GetInstallSourceCandidates` checks `install-payload` beside `Asher.Host.exe` (`AsherPaths.HostInstallPayloadFolderName`) |
| **Dev build script** | `Asher.Host/build-with-payload.cmd`; `npm run build:host` / `build:host:debug` in `Asher.Electron` |

### Electron flow updates

| Flow | Change |
|------|--------|
| Setup save | Calls `preparePatchesFolder` after `saveSettings` |
| Install | Preflight `preparePatchesFolder`; post-success uses `markInstalled` (not settings merge) |
| Uninstall | Post-success uses `markUninstalled` (not settings merge) |

### Production packaging

| Item | Detail |
|------|--------|
| **Tool** | `electron-builder` (portable win ia32) |
| **Command** | `npm run dist` (builds Release host + packages to `Asher.Electron/dist/`) |
| **Bundled host** | `extraResources` → `resources/asher-host/` (full Host output including `install-payload/`) |
| **Packaged resolution** | `resolve-host-path.js` uses `process.resourcesPath/asher-host/Asher.Host.exe` when `app.isPackaged` |

### Validation

| Check | Result |
|-------|--------|
| `Asher.Host` / `Asher.App` build | **Success** |
| `Asher.Host.TestClient` | **Passed** (incl. new contract methods) |
| `npm run smoke:payload` | **Passed** — Launcher, Runtime, SDK, Harmony, DefaultMods |
| All existing `npm run smoke*` | **Passed** |
| Live fresh install on clean game folder | **Not run** — requires Dust install on dev/game machine |

### Limitations

- Default mod DLLs copy only when patching projects are built (XNA refs may block `dotnet build` on some machines); install works without default mods.
- `npm run dist` requires Release host build; dev workflow uses Debug + `npm start`.
- Live end-to-end fresh install validation remains manual on a game PC.

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
5. **Fresh install via Electron** — ~~Install UI exists, but install payload is not bundled with Host/Electron yet~~ Payload bundled via `install-payload/` MSBuild target; live fresh-install validation pending on game machine (see Step 18).
6. **No automated test project** — validation is manual console output only.

---

## Decisions

| Decision | Rationale |
|----------|-----------|
| Add `AsherServiceHost` in `Asher.Services` | Keeps wiring next to implementations; host project stays thin |
| Do not modify WPF `App.xaml.cs` in this step | Preserve existing app; avoid Prism refactor scope |
| Read-only default smoke | Safe validation without game directory mutation |
| `net8.0-windows` for host | Required to reference `Asher.Services` / `Asher.Core` |
| Exclude `INavigationItemsManager` from host | Presentation-only; not part of application contract |
| Electron owns end-user installation | Full install via in-app flow; `PrepareDistribution.ps1` is WPF legacy only, not an Electron prerequisite |
| Install payload bundled with Electron/Host | Fresh install must work without manual Distribution folder assembly |
| Step 18 is the migration gate | Block further Electron UI until payload bundling + contract additions + fresh-install validation pass |
| Add `PreparePatchesFolder`, `MarkInstalled`, `MarkUninstalled` with Step 18 | Align Electron install flow with WPF; replace fragile `saveSettings` merge for install markers |

---

## Remaining Work

### Completed (Steps 1–17)

- [x] POCO-ify `ManagedModInfo` (remove `BindableBase` from Core)
- [x] Add `ISettingsService` wrapper; remove direct `AsherSettings` from ViewModels
- [x] Unify WPF DI with `AsherServiceHost` or shared registration module
- [x] Introduce in-process `IAsherApplication` contract
- [x] Freeze §12.5 **core** contract as serializable API surface (partial — `CompleteInstall` / `PreparePatchesFolder` deferred until Step 18)
- [x] IPC / transport investigation (separate phase)
- [x] JSONL transport prototype (`Asher.Host --jsonl` + test client)
- [x] Electron frontend spike (`Asher.Electron` + JSONL client)
- [x] Electron game setup flow (detect, validate, save settings)
- [x] Electron Mod Manager (load mods, enable/disable)
- [x] Electron application shell (startup, navigation, shared state)
- [x] Electron installation flow (install, progress, cancel)
- [x] Electron uninstallation flow (uninstall, progress, cancel, post-uninstall refresh)
- [x] Electron launch game flow
- [ ] Live validation on machine with Asher-installed game — **partial**: launch/mods/manager validated on game PC; **fresh install blocked until Step 18**

### Step 18 gate (completed — pending live fresh-install validation)

- [x] Contract: `PreparePatchesFolder`, `MarkInstalled`, `MarkUninstalled` on `IAsherApplication` + JSONL
- [x] Electron install/uninstall flows: use new contract methods instead of `saveSettings` merge
- [x] Install payload bundling (MSBuild `install-payload/` beside Host)
- [x] Host payload resolution for Electron-spawned `Asher.Host`
- [x] Electron production packaging (`electron-builder`, `npm run dist`)
- [ ] Fresh-install validation on clean game folder (gate exit — manual on game PC)

### After Step 18 gate

- [ ] Electron settings screen (prefs + path)
- [ ] Electron home / hub navigation
- [ ] Localization (renderer string tables)

---

## Changelog

| Date | Step | Summary |
|------|------|---------|
| 2026-09-01 | 18 | Payload bundling, contract additions, Electron flow updates, electron-builder packaging |
| 2026-09-01 | 18 (plan) | Step 18 declared migration gate; contract additions; settings/home UI deferred until gate passes |
| 2026-08-28 | 18 | Electron-owned installation; payload bundling plan; Distribution script out of user path |
| 2026-08-28 | 17+ | Host startup sync fix; preload.cjs; diagnostic logging |
| 2026-08-28 | 17 | Launch game; Manager button via existing `launchGame` JSONL |
| 2026-08-28 | 16 | Remaining WPF audit; launch game + settings identified as next priorities |
| 2026-08-28 | 15 | Uninstallation flow; uninstall/progress/cancel, post-uninstall mode refresh |
| 2026-08-28 | 14 | Installation flow; install/progress/cancel, post-install mode refresh |
| 2026-08-28 | 13 | Application shell; startup lifecycle, shared state, mode-driven navigation |
| 2026-08-28 | 12 | Electron Mod Manager; getMods/setModEnabled, Setup↔Manager navigation |
| 2026-08-28 | 11 | Electron game setup flow; detect/validate/save via existing contract |
| 2026-08-28 | 10 | Electron frontend spike (`Asher.Electron`); JSONL client, host lifecycle, detectGameFolder UI |
| 2026-08-28 | 9 | JSONL stdin/stdout transport; `Asher.Host.TestClient` protocol validation |
| 2026-08-28 | 8 | Transport investigation; recommend localhost HTTP + SSE |
| 2026-08-28 | 7 | Contract DTOs; `IAsherApplication` no longer exposes Core types |
| 2026-08-28 | 6 | `IAsherApplication` facade; host smoke uses application contract |
| 2026-08-28 | 5 | `ApplicationServices` shared composition; WPF + host consume same factory |
| 2026-08-28 | 4 | `ManagedModInfo` POCO + `ManagedModItemViewModel`; `ISettingsService`; ViewModel/host migration |
| 2026-08-28 | 1 | `AsherServiceHost` + `Asher.Host` spike; build + read-only smoke validated |
