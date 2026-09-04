# Electron Migration

Compact record of the WPF → Electron manager transition. UI comparison: `Manager-UI-Architecture.md`. Day-to-day constraints: `.cursor/rules/` (especially `asher-current-state.mdc`, `asher-architecture.mdc`, `jsonl-protocol.mdc`).

**Status (2026-09):** Migration complete. Electron + `Asher.Host` is the only manager UI. Steps 1–20 finished; WPF stack removed.

---

## Overview

Asher’s manager UI moved from a WPF application (`Asher.App` + `Asher.UserInterface`) to **Electron** (`Asher.Electron`). Business logic stayed in **C#** (`Asher.Services`, `Asher.Core`). A headless **Host** process (`Asher.Host`) exposes services to Electron over **JSONL on stdin/stdout**.

**Objective:** Same install, uninstall, mod management, and launch behavior as WPF, with a cross-platform-friendly UI shell and no Prism/WPF dependency in the management layer.

**In-game stack unchanged:** `Asher.Launcher` (replaces `DustAET.exe`) → `Asher.Runtime` → `Asher.Patching.*` inside the game process.

---

## Current Architecture

```
┌─────────────────────────────────────────────────────────┐
│  Asher.Electron                                         │
│  main/     window, HostManager, JSONL client, IPC       │
│  preload/  contextBridge → window.asher                 │
│  renderer/ UI, controllers, localization, theme         │
└───────────────────────┬─────────────────────────────────┘
                        │ JSONL (stdin/stdout)
┌───────────────────────▼─────────────────────────────────┐
│  Asher.Host (--jsonl)                                   │
│  JsonlHostSession → IAsherApplication                   │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  Asher.Services                                         │
│  ApplicationServices → Settings, Install, Folder,       │
│  Launch, PatchManager implementations                   │
│  Asher.Core (models, AsherPaths, settings)              │
└─────────────────────────────────────────────────────────┘

Game process (separate from manager):
  DustAET.exe (= Asher.Launcher) → Runtime → mod DLLs
```

| Component | Responsibility |
|-----------|----------------|
| **Electron renderer** | Presentation, navigation, user input; calls backend via `ApplicationClient` |
| **Electron main** | Spawns/monitors `Asher.Host`, routes IPC, folder dialogs |
| **Asher.Host** | JSONL transport only; no UI |
| **IAsherApplication** | Stable facade; DTOs only (no Core types on the wire) |
| **Services** | Install/uninstall, settings, game detection, mods, launch |
| **Launcher / Runtime** | Game executable swap and in-process Harmony patching |

---

## Migration Decisions

### Decision — JSONL transport (not HTTP)

**Context:** Step 8 evaluated localhost HTTP+SSE, Unix sockets, gRPC, and stdin/stdout JSON.

**Decision:** **JSONL over stdin/stdout** between Electron main and `Asher.Host --jsonl`.

**Rationale:** Simple child-process model; no port negotiation; sufficient for single-client desktop app; fast to prototype.

**Consequences:** HTTP was documented as the long-term recommendation in Step 8 but **not adopted**. Do not add a second transport without a concrete requirement. Progress streams on the same stdout channel. Sequential request processing (long install blocks other requests).

### Decision — IAsherApplication + DTOs

**Context:** Host and future frontends needed a UI-agnostic contract.

**Decision:** `IAsherApplication` in `Asher.Services` with `Application/*Dto` types and `ApplicationContractMapper`.

**Rationale:** Keeps `Asher.Core` persistence models off the public boundary; JSONL maps 1:1 to contract methods.

### Decision — Unified service composition

**Context:** WPF `App.xaml.cs` and `AsherServiceHost` duplicated DI wiring.

**Decision:** `ApplicationServices.Create()` as the single composition root; WPF later removed.

### Decision — Electron owns end-user installation

**Context:** WPF relied on `PrepareDistribution.ps1` and `Distribution\` for first-time setup.

**Decision:** Users install through Electron (Setup → Install). Install payload ships beside `Asher.Host.exe` (`install-payload/` via MSBuild).

**Rationale:** Fresh install must work without manual Distribution assembly.

### Decision — Install state via dedicated contract methods

**Context:** Electron initially merged install markers through `saveSettings`, which was fragile.

**Decision:** `markInstalled` / `markUninstalled` JSONL methods (Step 18).

**Also:** `BackupEnabled` in settings gates `CreateBackup` during install.

### Decision — WPF retirement

**Context:** Electron parity reached after Step 18 gate (2026-09-01).

**Decision:** Remove `Asher.App`, `Asher.UserInterface`, `Asher.Localization`, `PrepareDistribution.ps1`, and WPF-only services from `Asher.Services`. Strip WPF/Prism/MaterialDesign from `Asher.Core`.

---

## Migration Milestones

| When | Milestone |
|------|-----------|
| 2026-08-28 | Steps 1–7: `AsherServiceHost`, `Asher.Host`, `ISettingsService`, `IAsherApplication`, DTO contract |
| 2026-08-28 | Step 8: Transport investigation (HTTP recommended on paper) |
| 2026-08-28 | Step 9: JSONL prototype + `Asher.Host.TestClient` |
| 2026-08-28 | Steps 10–15: Electron spike → setup → mod manager → shell → install/uninstall |
| 2026-08-28 | Step 16: WPF audit; priorities identified |
| 2026-08-28 | Step 17: Launch game; host startup sync; diagnostic logging |
| 2026-09-01 | **Step 18 gate passed** (game PC): fresh install, uninstall, reinstall, patched launch; payload bundling; `npm run dist` |
| 2026-09-01 | Step 19: WPF stack removed; Electron-only manager |
| 2026-09-02 | Step 20: Settings, home hub, i18n (en/pt/es), theme, install Finish UX, mod validation, packaging hygiene |
| 2026-09-03 | Zip/Distribution packaging; manager self-deploy + relaunch; GitHub Releases updates |

---

## Current Status

### Completed

- Full manager flows: setup, install, uninstall, mod manager, launch, settings
- JSONL contract stable; smoke tests in `Asher.Electron/scripts/`
- Install payload: `Asher.Launcher`, `Asher.Runtime`, `Asher.SDK`, `0Harmony.dll`, 5 default mod DLLs
- Localization, theme, toasts, Material Symbols shell
- Zip + `Distribution/` packaging (`npm run dist`); GitHub publish (`npm run publish` + `private/GH_TOKEN`)
- Packaged Finish deploys Electron manager into `game/Asher/Asher.App/` and relaunches from there
- GitHub Releases update check / zip apply for the installed manager

### Deferred

- Full install wizard stepper / welcome onboarding chrome
- Content patcher UI (no backend)
- Desktop shortcut after install
- `CompleteInstall` aggregate as a Host contract method — deploy/relaunch lives in Electron main instead

### Next planned steps

1. **Install wizard polish** — fuller welcome / stepper chrome (Finish + optional launch already exist).
2. **Content patcher** — only if/when a backend exists; no UI-only stub.

Items above are optional polish or gated features, not unfinished migration work.

### Retired / excluded

| Removed | Notes |
|---------|--------|
| `Asher.App`, `Asher.UserInterface`, `Asher.Localization` | Replaced by Electron renderer |
| `PrepareDistribution.ps1` | Replaced by `npm run dist` / `sync-distribution.mjs` |
| `ManagerDeploy`, `ManagerLaunch`, `Shortcut`, `InstallationState`, `NavigationItemsManager` | WPF C# services; Electron main now owns deploy/relaunch |
| Prism / MaterialDesign in Core | Decoupled in Step 19 |
| Portable primary packaging | Replaced by zip / Distribution folder (portable cannot auto-update) |

---

### Decision — Zip Distribution + in-game manager deploy

**Context:** Portable single-exe builds cannot apply electron-builder auto-updates. WPF used to copy the manager into `game/Asher/Asher.App/` and relaunch after install.

**Decision:** Ship an unpacked `Distribution/` folder (and GitHub zip). After install Finish in a packaged build, Electron main copies itself into `Asher/Asher.App/` and relaunches from there. Updates download the release zip and replace that installed folder. Publish uses local `private/GH_TOKEN` only (not embedded in the client).

**Rationale:** Folder layout keeps `resources/asher-host` intact; updates target the stable in-game install path; no NSIS wizard.

**Consequences:** Deploy/relaunch and in-place update apply only when packaged. Dev (`npm start`) keeps in-process Finish behavior.

---

## Protocol and Contract

**Transport:** One JSON object per line. Requests: `{ requestId, method, params }`. Responses: `{ requestId, success, result?, error? }`. Events: `ready`, `progress`.

**Methods:** `getSettings`, `saveSettings`, `getApplicationMode`, `detectGameFolder`, `getGameFolderInfo`, `resolveGameFolderPath`, `isGameInstalled`, `hasRestorableBackup`, `getMods`, `setModEnabled`, `install`, `uninstall`, `launchGame`, `markInstalled`, `markUninstalled`, `cancel`, `shutdown`.

**Error codes:** `invalid_request`, `unknown_method`, `application_error`, `cancelled`, `not_found`, `internal_error`.

**Rules:** Host stdout is protocol-only (diagnostics on stderr). New capabilities → new `IAsherApplication` method + JSONL method. Full detail: `.cursor/rules/jsonl-protocol.mdc`.

**DTOs:** `ApplicationSettingsDto`, `GameFolderDto`, `ManagedModDto`, `InstallationProgressDto`, `InstallationResultDto`, `OperationResult`.

---

## Compatibility and Behavioral Requirements

Preserve unless explicitly required to change:

- **Installation layout:** `DustAET.exe` launcher swap, `DustAET.real.exe`, `Asher/` folder structure (`AsherPaths`)
- **Settings file:** Path, JSON shape, `MarkAsInstalled` / `MarkAsUninstalled` semantics
- **Install/uninstall logic:** `GameInstallationService` — shared semantics; Electron is UI only
- **Mod toggle:** `setModEnabled` must reject nonexistent mods; filesystem move between `Mods/` and `DisabledMods/`
- **Launch:** Game started via patched `DustAET.exe` (launcher chain), not `DustAET.real.exe` directly
- **Business logic in C#:** Renderer orchestrates; does not reimplement service rules

---

## Known Limitations

- **Dev machine:** Often no game installed — detection/mod smoke tests use empty or invalid paths.
- **No UI/E2E tests:** Smoke tests exercise JSONL/host only, not Electron renderer DOM.
- **No xUnit-style test project:** Validation is smoke/integration + manual game PC checks.
- **Packaging:** `npm run dist` may fail on Windows without symlink privilege when electron-builder caches `winCodeSign`; Developer Mode or elevated terminal workaround. Ship path is zip + `Distribution/` (not portable).
- **Manager deploy:** Packaged Finish copies the Electron app into `game/Asher/Asher.App/` and relaunches; skipped under `npm start`.
- **Updates:** GitHub zip apply requires running from the installed `Asher.App` folder; Distribution first-run uses manual download when not installed there.
- **Patching builds:** May warn about XNA GAC / game HintPath on machines without the game installed.
- **Cancellation:** Protocol-level cancel exists; services may not fully observe `CancellationToken` inside `Task.Run` install work.
- **Sequential JSONL:** Long install blocks other host requests.

---

## Validation

### Step 18 gate (2026-09-01, game PC with Steam install)

Validated without `PrepareDistribution.ps1`:

1. `smoke:payload` — 3 canonical mods in `install-payload/DefaultMods/`
2. Setup → save settings
3. Install → launcher swap, mods copied, `markInstalled`
4. Manager → launch → runtime log shows patch modules applied (Intro Skipper confirmed)
5. Uninstall → restore backup, `markUninstalled`
6. Reinstall → launch → patches active

### Ongoing validation

```bash
cd Asher.Electron
npm run build:host:debug   # or build:host for Release/dist
npm run smoke              # headless JSONL suite
npm start                  # primary UI dev path
dotnet run --project Asher.Host.TestClient -p:Platform=x86
```

Game-affecting changes require retest on a machine with Dust: An Elysian Tail installed.

---

## Historical Notes

### Initial state (pre-migration)

Logic lived in `Asher.Services` + `Asher.Core`, wired only through WPF Prism DI (`App.xaml.cs`). No standalone host. First spike: `AsherServiceHost` + console `Asher.Host` to prove services run without WPF.

### Why Step 8 recommended HTTP but Step 9 shipped JSONL

HTTP+SSE was judged best for long-term tooling and progress streaming. JSONL was implemented first as a lower-friction spike and proved sufficient; switching was deferred and never done. **Current transport is JSONL.**

### Gate debugging lessons (payload freshness)

Electron install failures during gate validation were traced to **stale `install-payload` mod DLLs**, not different install logic (`GameInstallationService` is shared). Fixes:

- `InstallPayload.targets` purges `DefaultMods/*.dll` before staging; patching project `bin` outputs take priority over `Distribution\` fallback
- `npm run build:host` builds patching projects before Host
- Rebuild Host after Electron updates on game PC — stale `Asher.Host.exe` causes `unknown_method` errors

**Insight:** `getMods` is filesystem-only; patch application is proven via `Asher/AsherLogs/runtime_*.log`, not mod list presence alone.

### WPF vs Electron UI

`docs/Manager-UI-Architecture.md` compares retired WPF shell/screens with the current Electron manager.

---

## Document maintenance

This file is a **migration and architecture record**, not an implementation task list. For step-by-step historical detail from Steps 1–17, see git history of this file before the 2026-09 compaction.

When adding entries, follow `.cursor/rules/asher-documentation.mdc`.
