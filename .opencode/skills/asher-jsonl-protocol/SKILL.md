---
name: asher-jsonl-protocol
description: Use when adding, changing, or debugging the JSONL protocol between Asher.Electron and Asher.Host — request/response shapes, method list, events, error codes, or the stdout/stderr contract. Also covers how to add a new backend method safely.
---

# Asher JSONL Protocol

Electron spawns `Asher.Host --jsonl` as a child process. Communication is line-delimited JSON on
stdin (requests) and stdout (responses/events). Persistent architecture rules live in `AGENTS.md`;
this skill covers the wire contract and how to change it safely.

## Source locations

| Concern | Location |
|---------|----------|
| Host transport / session | `Asher.Host/Jsonl/` (`JsonlHostSession`) |
| Host CLI + smoke runner | `Asher.Host/SmokeRunner.cs` |
| Protocol test client | `Asher.Host.TestClient/` |
| Electron client | `Asher.Electron/src/main/jsonl-client.js`, `host-manager.js` |
| Renderer wrapper | `Asher.Electron/src/renderer/application-client.js` |
| Preload bridge | `Asher.Electron/src/preload/preload.cjs` |

## Message shapes

Request:

```json
{ "requestId": "<uuid>", "method": "<method>", "params": { ... } }
```

Response:

```json
{ "requestId": "<id>", "success": true, "result": { ... } }
{ "requestId": "<id>", "success": false, "error": { "code": "<code>", "message": "<msg>" } }
```

Events (no `requestId`):

```json
{ "event": "ready" }
{ "event": "progress", "requestId": "<id>", "progress": { ... } }
```

## Methods

`getSettings`, `saveSettings`, `getManagerLogDirectory`, `getApplicationMode`, `getPlatformInfo`,
`detectGameFolder`, `getGameFolderInfo`, `resolveGameFolderPath`, `isGameInstalled`,
`hasRestorableBackup`, `getInstallState`, `getMods`, `setModEnabled`, `install`, `uninstall`,
`launchGame`, `markInstalled`, `markUninstalled`, `cancel`, `shutdown`

Contract notes:

- `getPlatformInfo` → `PlatformInfoDto` (`kind`, executable/library names, `usesLauncherSwap`,
  `supportsRecoveryHelper`, `defaultGameFolderName`). Frontends adapt UI from this instead of
  branching on the OS.
- `getInstallState` (optional `params.gameFolderPath`) → `InstallStateDto` (`state`:
  `notInstalled|installed|partial`, `canUninstall`, `canRestore`, `marker`). Authoritative
  install-status API. `hasRestorableBackup` is compatibility-only and must not be used to derive
  uninstall capability.
- `getManagerLogDirectory` → `{ gameFolderPath, logsDirectory }`, or both null. Distinct from
  `resolveGameFolderPath` (which requires an installed Asher layout).

## Error codes

`invalid_request`, `unknown_method`, `application_error`, `cancelled`, `not_found`, `internal_error`

## Rules

- Host stdout is exclusively JSONL protocol traffic. Never write diagnostics to stdout.
- Every request must include `requestId` and `method`.
- Add backend capabilities as new JSONL methods mapped through `IAsherApplication`, not ad-hoc
  channels.
- Preserve existing message shapes; do not silently change field names or semantics.
- Progress events reference the originating `requestId`.
- Cancellation uses the `cancel` method with `{ targetRequestId }`.

## Adding or changing a method safely

1. Define/extend the DTO + `IAsherApplication` contract in `Asher.Services/Application/` (DTOs only).
2. Implement in the relevant service; keep business rules in C#, not in the renderer.
3. Map the method in the Host session (`Asher.Host/Jsonl/`).
4. Expose it through `preload.cjs` → `ApplicationClient.invoke()` in the renderer.
5. Preserve existing shapes; if a shape must change, treat it as a breaking contract change and
   confirm the requirement first.
6. Validate with the protocol test client and the relevant smoke test (see `asher-testing`).

## Validation

- `dotnet run --project Asher.Host.TestClient -p:Platform=x86`
- `cd Asher.Electron && npm run smoke` (or a targeted smoke script)
