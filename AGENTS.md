# Asher

Modding platform for *Dust: An Elysian Tail*: runtime code patching (Harmony) and content
replacement, with an Electron manager UI and a C# backend. Windows and Linux are supported.

## Architecture

| Layer | Project(s) | Responsibility |
|-------|-----------|----------------|
| Presentation | `Asher.Electron` | UI shell, navigation, user input, rendering |
| Host / transport | `Asher.Host` | JSONL protocol on stdin/stdout; spawned by Electron |
| Application contract | `IAsherApplication` (`Asher.Services`) | Facade over all services; DTOs only |
| Services | `Asher.Services/Implementations/` | Install, uninstall, patches, settings, launch |
| Platform abstraction | `IPlatformInfo` (`Asher.Core/Platform`) + behavior contracts (`Asher.Services/Platform`) | OS discovery, executable layout, runtime deployment, process launch |
| Core | `Asher.Core` | Models, `AsherPaths`, constants |
| SDK | `Asher.SDK` | Patching API consumed by mod DLLs |
| Launcher | `Asher.Launcher` | Windows game-executable replacement (launcher swap); loads the runtime |
| Runtime | `Asher.Runtime` | In-game Harmony patch + lifecycle orchestration |
| Patching | `Patches/Asher.Patching.*` | Individual mod assemblies (5 canonical mods) |

Windows uses the launcher-swap model; Linux keeps the native `DustAET` and attaches through
`libasher_bootstrap.so` (`LD_PRELOAD`). Details: `docs/Cross-Platform-Architecture.md`.

## Hard rules

- Business logic lives in C# services, not renderer JavaScript. New features extend
  `IAsherApplication` + JSONL, never bypass the backend.
- Keep contracts DTO-oriented; do not expose internal service types on the wire.
- All `Asher.Host` stdout is JSONL protocol traffic. Diagnostics go to stderr.
- Do not add WPF, Prism, or MaterialDesign dependencies anywhere. `Asher.Core` stays free of
  UI-specific types.
- OS-specific work goes behind `Asher.Services/Platform` contracts. Do not branch on the OS in
  `GameInstallationService`, `GameFolderService`, `GameLaunchService`, or the renderer.
- Keep launcher/runtime independent of Host — they run inside the game process.
- Preserve existing behavior, contracts, and installation semantics. Prefer extending existing
  abstractions over creating competing ones, and keep changes proportional to the requirement.
- Do not change the installation layout (`Asher/` folder structure; Windows `DustAET.exe` swap),
  JSONL message shapes, or the launcher chain without a concrete requirement.
- Read the source before proposing changes; completed migrations are not to-do lists.

## Do not recreate (retired)

- WPF UI: `Asher.App`, `Asher.UserInterface`, `Asher.Localization`.
- WPF-only services: `ManagerDeploy`, `ManagerLaunch`, `Shortcut`, `InstallationState`,
  `NavigationItemsManager`.
- `PrepareDistribution.ps1`; Prism / MaterialDesign / WPF dependencies in `Asher.Core`.
- Portable-as-primary packaging (replaced by zip / `Distribution/`).

## Deferred / out of scope

- Install wizard stepper chrome; content-patcher UI (no backend exists); desktop shortcut creation
  after install.
- Linux: external Steam/desktop launch, in-app updater, `.deb`; `mono_thread_attach` timing fix.

## Canonical locations

| Topic | Location |
|-------|----------|
| Persistent project contract | `AGENTS.md` (this file) |
| Specialized workflows | `.opencode/skills/` |
| User & project documentation | website — https://mikesstash.com.br/asher/ |
| Platform contracts, Linux layout/build/packaging | `docs/Cross-Platform-Architecture.md` |
| Electron manager structure + JSONL contract | `.opencode/skills/asher-electron`, `.opencode/skills/asher-jsonl-protocol` |
| Native Linux bootstrap | `Asher.Linux/README.md` |
| Cursor compatibility layer | `.cursor/rules/` (temporary; not the source of truth) |

## Validation

- Smallest scope first: build the changed C# project, then run the relevant smoke test.
- Pure logic: `dotnet test Asher.Services.Tests/Asher.Services.Tests.csproj -c Debug`.
- Cross-cutting: `npm run smoke` (headless). UI: `npm start`.
- Real-game validation is required for install, uninstall, launch, or runtime-patching changes.
- Full command set and strategy: `asher-testing` skill.
