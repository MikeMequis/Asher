---
name: asher-electron
description: Use when working on the Asher Electron manager frontend — main process, preload bridge, renderer controllers, UI state, localization, theme, or how the UI talks to the C# Host over JSONL. Covers safe modification of Asher.Electron.
---

# Asher Electron Frontend

The Asher manager UI is Electron (`Asher.Electron`). It is presentation only: install, launch, and
patching logic live in C# behind the headless `Asher.Host` over JSONL. Persistent boundaries live in
`AGENTS.md`; the wire contract lives in the `asher-jsonl-protocol` skill.

## Directory responsibilities

| Directory | Role | Key files |
|-----------|------|-----------|
| `src/main/` | Node.js main process — window, host lifecycle, IPC, updates | `main.js`, `host-manager.js`, `jsonl-client.js`, `resolve-host-path.js`, `auto-updater.js`, `diagnostic-logger.js` |
| `src/preload/` | Context bridge — exposes `window.asher` to the renderer | `preload.cjs` |
| `src/renderer/` | Browser context — all UI, state, controllers | `app.js`, `application-shell.js`, `application-client.js`, controllers, `localization.js`, `theme.js`, `icons.js` |

## Renderer modules

| Module | Purpose |
|--------|---------|
| `application-shell.js` | App lifecycle, navigation, mode transitions |
| `application-client.js` | Thin wrapper over the `window.asher` preload bridge |
| `application-state.js` | Shared state fetched from the backend |
| `game-setup.js` | Setup wizard controller |
| `installation-controller.js` | Install flow state machine |
| `uninstallation-controller.js` | Uninstall flow state machine |
| `mod-manager.js` | Mod list + toggle state |
| `settings-controller.js` | Settings draft, validate, persist |
| `launch-game.js` | Launch game action |
| `action-banner.js` | Toast notification stack |
| `localization.js` | i18n string tables (en/pt/es) |
| `theme.js` | Light/Dark theme application |
| `icons.js` | Material Symbols helpers |
| `errors.js` | Error classification |

`app.js` is the composition root — wires controllers, renders views, attaches event listeners.

## Flow

Electron window → spawn Host → `ready` → `getApplicationMode` / `getSettings` → land on setup,
install, or home.

| Mode | Screens |
|------|---------|
| Install wizard | Welcome → Setup (detect/validate) → Installing → Complete |
| Manager | Home, Patch Manager, Settings; uninstall from Settings (not in sidebar) |

## Rules

- Game-folder detection and the manager log directory come from Host (`getManagerLogDirectory`).
  Main process does not read `settings.json` or decide `{game}/Asher/AsherLogs` itself.
- Renderer code must not use Node.js or Electron APIs directly; use `window.asher.*` from preload.
- All backend calls go through `ApplicationClient.invoke()` → `window.asher.invoke()`.
- Keep UI state in renderer controllers; do not store application state in main beyond host status.
- Reuse existing controllers, localization, theme, and icon infrastructure before creating
  alternatives.
- Do not duplicate backend business logic in JavaScript — delegate to C# via JSONL.
- DOM updates for mod toggles use incremental patching (`updateModRow`), not full list rebuilds.
- Installation capability comes from `getInstallState` (`state`/`canUninstall`/`canRestore`/`marker`),
  not `hasRestorableBackup`. `application-state.js` is the single place deriving `canUninstall` /
  `canRestore`.
- Renderer platform capabilities come from `platform.js` (`supportsRecoveryHelper`,
  `usesLauncherSwap`). Windows-only UI (Settings → Removal → Total exclusion, emergency `.cmd`) is
  gated on `supportsRecoveryHelper`; `main.js` also rejects `app:run-emergency-uninstall` off Windows.
- Main process is Windows-only for updates (`auto-updater.js`) and post-quit replace
  (`post-quit-helper.js`). Off Windows the updater reports `unavailable`; do not add a fake Linux
  updater. `manager-paths.js` resolves the packaged binary name per platform (`Asher.exe` / `Asher`).

## Modifying the frontend safely

1. Confirm the behavior is UI-side; if it is a business rule, implement it in C# and expose a JSONL
   method instead.
2. Extend existing controllers/localization/theme rather than adding parallel systems.
3. Keep OS knowledge out of the renderer — derive capabilities from `getPlatformInfo` /
   `getInstallState`.
4. Do not recreate retired WPF/Prism/MaterialDesign structure.

## Validation

- UI run: `cd Asher.Electron && npm start`
- Headless shell/manager smoke tests: see `asher-testing`.
- No DOM/E2E tests exist; renderer behavior is verified by running the app.
