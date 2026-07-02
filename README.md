# 🧱 Asher Modding Platform

**Asher** is a launcher-based modding platform for *Dust: An Elysian Tail*, designed to support **runtime code patching** and **content replacement** in a safe, modular, and reversible way.

Inspired by mature mod loaders such as **SMAPI**, Asher prioritizes **explicit initialization order**, **runtime lifecycle control**, and **clean debugging**, deliberately avoiding fragile early-injection patterns.

---

## 🎯 Project Goals

- Runtime code patching using **Harmony**
- Asset replacement without modifying `.xnb` files
- Modular and reversible mod loading
- UI-based patch selection and configuration
- Full compatibility with **Steam**, **XNA**, and **.NET Framework**

---

## 🧠 Core Architecture (Launcher-First)

Asher is built around a **custom launcher**, which guarantees a deterministic initialization order and reliable runtime behavior.

**Key principle:**  
Injection and patching are **controlled and delayed**, never performed blindly at process startup.

This design mirrors proven approaches used by SMAPI and other stable modding platforms.

A companion **WPF manager app** (`Asher.App`) handles installation, mod management, and user settings without touching the game process directly.

---

## 🗂️ Solution Structure

```
/Asher.sln
│
├── Asher.App/                  → WPF installer & mod manager (.NET 8, x86)
├── Asher.UserInterface/        → WPF views, view models, theming
├── Asher.Services/             → Installation, launch, patch manager, shortcuts
├── Asher.Core/                 → Paths, settings, shared models
├── Asher.Localization/         → UI strings (en-US, pt-BR)
│
├── Asher.Launcher/             → Custom game launcher (.NET Framework 4.8)
│   └── Program.cs              → Entry point and bootstrap orchestration
│
├── Asher.Runtime/              → Runtime mod loader foundation (.NET Framework 4.8)
│   ├── Bootstrap/
│   │   ├── AssemblyLoader.cs            → Dynamic mod assembly loading
│   │   ├── GameLifecycleHooks.cs        → Game lifecycle event hooks
│   │   ├── GameTitleBootstrap.cs        → Sets game window title
│   │   ├── HarmonyLifecycleBootstrap.cs → Optional lifecycle hook injection
│   │   ├── PreInitBootstrap.cs          → PreInit module discovery & execution
│   │   ├── PatchModuleLoader.cs         → Harmony patch application
│   │   └── LifecycleModuleLoader.cs     → Lifecycle event registration
│   ├── Core/
│   │   ├── GameContext.cs               → Game instance access
│   │   ├── RuntimeContext.cs            → Configuration and paths
│   │   ├── RuntimeController.cs         → Initialization and shutdown
│   │   └── RuntimeResult.cs             → Operation result wrapper
│   ├── Lifecycle/
│   │   ├── LifecycleEvent.cs            → Lifecycle state enum
│   │   └── GameLifecycleEventBus.cs     → Lifecycle state management
│   ├── RuntimeEntry.cs                  → Public runtime API
│   ├── RuntimeLogger.cs                 → File-based logging system
│   └── RuntimeLoggerAdapter.cs          → SDK logger bridge
│
├── Asher.SDK/                  → API for mod developers (.NET Framework 4.8)
│   ├── Logging/
│   │   ├── AsherLog.cs                  → Static logger facade
│   │   └── IAsherLogger.cs              → Logger interface
│   └── Patching/
│       ├── AsherLifecycleModuleBase.cs  → Base class for lifecycle monitoring
│       ├── IAsherLifecycleModule.cs     → Lifecycle event interface
│       ├── IAsherPatchModule.cs         → Harmony patch module interface
│       └── IAsherPreInitModule.cs       → PreInit module interface
│
├── Asher.Patching.*/           → Built-in patch mods (DebugEnabler, IntroSkipper, etc.)
│
├── PrepareDistribution.ps1     → Bundles Distribution/ folder for install & deploy
└── Distribution/               → Output folder (generated, not committed)
```

---

## 🖥️ Asher Manager App

`Asher.App` is a WPF application built with **Prism**, **Material Design**, and a modular service layer.

### Installation mode

When Asher is not yet installed in a game folder, the app opens in **installation mode** and starts on the **Start** page:

1. **Start** — welcome and begin installation
2. **Detect Game** — auto-detect or browse for the Dust folder
3. **Installing** — deploy runtime, launcher wrapper, and default mods
4. **Complete** — summary with optional **desktop shortcut** creation

On finish, the app switches to mod manager mode and navigates to **Home**. If the installer was run from `Distribution\`, the distribution app closes and the installed copy at `[Game]\Asher\Asher.App\Asher.App.exe` is launched automatically.

### Mod manager mode

When Asher is already installed (detected from settings or from running inside `[Game]\Asher\Asher.App\`), the app opens in **manager mode** and starts on **Home**:

| Page | Description |
|------|-------------|
| **Home** | Quick actions — launch game, open Patch Manager, Settings |
| **Content Patcher** | Asset replacement UI (planned functionality) |
| **Patch Manager** | Enable/disable mods by moving DLLs between `Mods/` and `Mods/disabled/` |
| **Settings** | Game path, language, theme, backups, auto-launch |

### App features

- **Localization** — English and Portuguese (Brazil); language changes refresh labels without resetting the current page
- **Light / Dark theme** — full-window Material Design theming via Settings
- **Settings persistence** — `settings.json` in AppData, game manager folder, and local app directory
- **Game launch** — launches `DustAET.exe` (Asher launcher wrapper) from the detected game folder
- **Desktop shortcut** — optional shortcut to `Asher.App.exe` after installation
- **Legacy layout migration** — automatically moves old root-level `Asher.App`, `Asher.Backup`, and `patches` into `Asher/`

---

## 🚀 Runtime Flow Overview

### Initialization Sequence

```
1. User launches DustAET.exe via Steam or Asher Manager (Asher.Launcher wrapper)
   ↓
2. Launcher validates game installation
   ↓
3. RuntimeEntry.Init(context)
   ├─> RuntimeLogger initialized
   ├─> Directories prepared (Asher/Mods/, Asher/AsherLogs/)
   └─> Configuration loaded
   ↓
4. Assembly.LoadFrom(DustAET.real.exe)
   ↓
5. AssemblyLoader.LoadAssembliesFrom("Asher/Mods/")
   └─> All *.dll files in Mods/ loaded dynamically
   ↓
6. PreInitBootstrap.ExecutePreInitModules()
   └─> Scans all loaded assemblies for IAsherPreInitModule
   └─> Executes each module's Execute() method
   ↓
7. GameTitleBootstrap.Apply(gameAssembly)
   └─> Sets window title to "Dust - An Elysian Tail (Asher)"
   ↓
8. PatchModuleLoader.Load()
   ├─> Creates Harmony instance ("com.asher.runtime.mods")
   ├─> Scans for IAsherPatchModule implementations
   ├─> Applies each module's patches via Harmony
   ├─> LifecycleModuleLoader.Load()
   └─> HarmonyLifecycleBootstrap.InitializeIfNeeded()
   ↓
9. Dust.Program.Main(args) invoked via Reflection
   ↓
10. Game executes normally with patches applied
```

> **Important:** Launch the game through **Steam** or the manager's **Launch Game** button. Do not run `Asher.Launcher.exe` directly from the distribution folder — it must sit in the game root as `DustAET.exe` with `DustAET.exe.config` probing `Asher` and `Asher\Mods`.

---

## 📁 Installed Game Folder Layout

After installation, the game directory looks like this:

```
/GameFolder/
├── DustAET.exe                  (Asher.Launcher copy — Steam entry point)
├── DustAET.exe.config           (assembly probing: Asher; Asher\Mods)
├── DustAET.real.exe             (original game executable, renamed)
│
└── Asher/
    ├── Asher.Runtime.dll
    ├── Asher.SDK.dll
    ├── 0Harmony.dll             (net472 build — required)
    │
    ├── Mods/                    (active runtime mods)
    │   ├── Asher.Patching.DebugEnabler.dll
    │   ├── Asher.Patching.IntroSkipper.dll
    │   ├── Asher.Patching.GraphicsDeprofiler.dll
    │   └── disabled/            (mods disabled via Patch Manager)
    │
    ├── AsherLogs/
    │   └── runtime_YYYYMMDD_HHMMSS.log
    │
    ├── patches/                 (content patcher assets, future)
    ├── Asher.Backup/            (original exe backup)
    │
    └── Asher.App/               (manager app + install payload)
        ├── Asher.App.exe
        ├── settings.json
        ├── Asher.Launcher.exe
        └── DefaultMods/
```

Folders are created automatically during installation.

---

## 🔧 Build & Distribution

### Requirements

- Visual Studio 2022 (or `dotnet` CLI)
- **Platform: x86**
- **Configuration: Release** (for distribution)

### Steps

1. Build the solution as **x86 Release** in Visual Studio
2. Run the distribution script from the repo root:

```powershell
.\PrepareDistribution.ps1
```

3. Run `Distribution\Asher.App.exe` to install Asher into a game folder
4. After install, use `[GameFolder]\Asher\Asher.App\Asher.App.exe` for day-to-day management
5. Launch the game via **Steam** or the manager's **Launch Game** button

### Distribution notes

- `PrepareDistribution.ps1` explicitly bundles the **net472** `0Harmony.dll` — using the wrong Harmony build will cause `System.Runtime` version errors at launch
- The script copies launcher, runtime, SDK, default mods, and the manager app into `Distribution/`
- `Distribution/` is generated output and should not be committed

---

## 📊 Project Status Overview

| Area                      | Status      | Notes                                          |
| ------------------------- | ----------- | ---------------------------------------------- |
| Solution structure        | ✅ Done     | Multi-project architecture stabilized          |
| Launcher-based runtime    | ✅ Done     | Wrapper EXE approach validated                 |
| Steam compatibility       | ✅ Done     | Game launches normally via Steam               |
| Runtime initialization    | ✅ Done     | Logs, lifecycle and folders working            |
| Injection strategy        | ✅ Done     | No blind injection, controlled bootstrap       |
| Harmony bootstrap         | ✅ Done     | Runtime patching confirmed and working         |
| PreInit system            | ✅ Done     | Flag configuration before patches              |
| Lifecycle hooks           | ✅ Done     | Optional event system for game lifecycle       |
| Mod SDK                   | ✅ Done     | Clean interfaces for mod developers            |
| First gameplay patch      | ✅ Done     | Debug Menu Enabler working                     |
| External mod loader       | ✅ Done     | Dynamic .dll loading from Asher/Mods/          |
| **Installer / Manager UI**| ✅ Done     | WPF app with install wizard & mod manager      |
| **Patch Manager UI**      | ✅ Done     | Enable/disable mods via folder move            |
| **Localization**          | ✅ Done     | English and Portuguese (Brazil)                |
| **Light / Dark theme**    | ✅ Done     | Full-window Material Design theming            |
| **Game window title**     | ✅ Done     | Shows "Dust - An Elysian Tail (Asher)"         |
| Content patcher           | 🔜 Planned  | XNA ContentManager interception                |
| Mod metadata (json)       | 🔜 Planned  | mod.json for description, dependencies, etc.   |
| Public Mod API docs       | 🔜 Planned  | Developer documentation and examples           |

---

## 🧩 Learning & Implementation Kanban

### 🟢 DONE — Consolidated Phases

#### 🟢 Task 0 — Core Architecture & Bootstrap
**Status:** ✔ Completed

**Deliverables achieved:**
- Stable multi-project solution architecture
- Clear separation between Launcher, Runtime, SDK, and Manager App
- Wrapper EXE approach validated:
  - `DustAET.exe` → Asher Launcher
  - `DustAET.real.exe` → original game executable
- Full Steam compatibility preserved
- Explicit runtime initialization order guaranteed

#### 🟢 Task 1 — Runtime Modding Foundations
**Status:** ✔ Completed

**Concepts mastered:**
- Generics (used for Asher infrastructure)
- Reflection (runtime inspection, private member access)
- Harmony (Prefix / Postfix / Transpiler patterns)

**Key decisions locked in:**
- Harmony is the official runtime patch engine
- No permanent modification of game binaries or `.xnb` files

#### 🟢 Task 2 — Launcher-Based Runtime Control
**Status:** ✔ Completed

- Custom launcher fully controls game startup
- Runtime initialization occurs before game execution
- Logging, folders, and context prepared deterministically
- Runtime survives Steam launches transparently

#### 🟢 Task 3 — Module System Architecture
**Status:** ✔ Completed

1. **PreInit System** — configuration before patches
2. **Patch System** — Harmony patch application via `IAsherPatchModule`
3. **Lifecycle System** — optional game event hooks

#### 🟢 Task 4 — First Runtime Patch (Debug Enabler)
**Status:** ✔ Completed ✨

- First working gameplay patch: **Debug Menu Enabler**
- PreInit → Patch → Lifecycle flow validated

#### 🟢 Task 5 — Installer & Manager App
**Status:** ✔ Completed

**What was implemented:**
- `Asher.App` WPF installer with guided setup flow
- Game folder auto-detection (Steam, manual browse)
- Deploys runtime, launcher wrapper, and default mods into `Asher/`
- Mod manager with Home, Patch Manager, Settings, and Content Patcher shell
- Patch enable/disable by moving DLLs between `Mods/` and `Mods/disabled/`
- Launch game from manager
- Settings: language, Light/Dark theme, game path, backups
- Desktop shortcut option after installation
- `AsherPaths` helpers and legacy layout migration
- `PrepareDistribution.ps1` for repeatable builds

---

### 🟨 DOING — Current Phase

#### 🟨 Task 6 — Patch Porting & Reverse Engineering

**Status:** 🔄 In Progress

Port and modernize existing gameplay patches from **DustAetPatchingPlatform** into the Asher runtime architecture.

**Current workflow:**
1. Analyze original patch behavior
2. Inspect game internals using **dnSpy** when required
3. Reimplement using `IAsherPreInitModule`, `IAsherPatchModule`, and optional lifecycle hooks
4. Validate with runtime logs

---

### 🟥 BACKLOG — Short Term

#### 🔴 Task 7 — Mod Metadata System
- Design `mod.json` schema
- Parse metadata on mod load
- Load priority and dependency support

#### 🔴 Task 8 — Content Patcher (Core)
- Intercept `ContentManager.Load<T>()`
- Resolve replacements via `content.json`
- Support external assets (textures, fonts, data files)

#### 🔴 Task 9 — Mod Configuration System
- `IAsherConfigModule` interface
- Per-mod configuration files
- UI integration for settings

#### 🔴 Task 10 — Mod Dependency System
- Declare dependencies in `mod.json`
- Validate dependency graph on load
- Automatic load order resolution

---

## 🧠 Project Principles (Non-Negotiable)

- 🚫 No `.xnb` editing
- 🚫 No permanent binary modification
- ✅ 100% runtime patching
- ✅ Fully reversible (remove mod = original behavior)
- ✅ Modular and extensible
- ✅ Inspired by SMAPI, adapted to Dust
- ✅ Clean separation: Launcher → Runtime → SDK → Mods
- ✅ Comprehensive logging for debugging

---

## 🎮 Confirmed Working Features

### Runtime & Modding
- ✅ Dynamic mod loading from `Asher/Mods/`
- ✅ Three-stage mod lifecycle: PreInit → Patch → Lifecycle
- ✅ Clean SDK for mod developers
- ✅ Comprehensive logging in `Asher/AsherLogs/`
- ✅ Game window title: **Dust - An Elysian Tail (Asher)**

### Manager App
- ✅ Guided installation into any valid Dust game folder
- ✅ Patch Manager — toggle mods on/off without deleting files
- ✅ Launch game from manager
- ✅ English and Portuguese (Brazil) UI
- ✅ Light and Dark theme (full window)
- ✅ Desktop shortcut creation after install
- ✅ Settings persisted across sessions

### Working Mods
* ✅ **Debug Menu Enabler** — Tab in pause menu opens debug menu
* ✅ **Intro Skipper** — Skips ESRB rating, splash screens, and startup videos
* ✅ **Graphics Deprofiler** — Bypasses HiDef GPU profile restrictions

All implemented as external, runtime-loaded mods in `Asher/Mods/`.

---

## 📘 References

- **Harmony** — https://github.com/pardeike/Harmony
- **SMAPI** — https://github.com/Pathoschild/SMAPI
- **SMAPI Content Patcher** — https://stardewvalleywiki.com/Modding:Content_Patcher
- **DustAetPatchingPlatform** — https://github.com/GMMan/DustAetPatchingPlatform
  - Steam Forum Discussion — https://steamcommunity.com/app/236090/discussions/0/540744936409038540/

*Last Updated: July 2, 2026*
