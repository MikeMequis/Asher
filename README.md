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

---

## 🗂️ Solution Structure

```
/Asher.sln
│
├── Asher.Launcher/         → Custom game launcher (.NET Framework 4.8)
│   └── Program.cs          → Entry point and bootstrap orchestration
│
├── Asher.Runtime/          → Runtime mod loader foundation (.NET Framework 4.8)
│   ├── Bootstrap/
│   │   ├── AssemblyLoader.cs            → Dynamic mod assembly loading
│   │   ├── GameLifecycleHooks.cs        → Game lifecycle event hooks
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
|   └── RuntimeLoggerAdapter.cs          → SDK logger bridge
│
├── Asher.SDK/              → API for mod developers (.NET Framework 4.8)
│   ├── Logging/
│   │   ├── AsherLog.cs                  → Static logger facade
│   │   └── IAsherLogger.cs              → Logger interface
│   └── Patching/
│       ├── AsherLifecycleModuleBase.cs  → Base class for lifecycle monitoring
│       ├── IAsherLifecycleModule.cs     → Lifecycle event interface
│       ├── IAsherPatchModule.cs         → Harmony patch module interface
│       └── IAsherPreInitModule.cs       → PreInit module interface
│
├── /Mods/                  → External mod assemblies (runtime-loaded)
│   ├── Asher.Patching.DebugEnabler.dll       → First working mod (Debug Menu)
|   ├── Asher.Patching.IntroSkipper.dll       → Skips ESRB rating, splashes, etc.
│   └── Asher.Patching.GraphicsDeprofiler.dll → Intel graphic adapter fix
│
└── /AsherLogs/             → Runtime logs
    └── runtime_YYYYMMDD_HHMMSS.log
```

---

## 🚀 Runtime Flow Overview

### **Initialization Sequence (Proven and Working)**

```
1. User launches DustAET.exe (Asher.Launcher.exe wrapper)
   ↓
2. Launcher validates game installation
   ↓
3. RuntimeEntry.Init(context)
   ├─> RuntimeLogger initialized
   ├─> Directories prepared (Mods/, AsherLogs/, config/, cache/)
   └─> Configuration loaded
   ↓
4. Assembly.LoadFrom(DustAET.real.exe)
   ↓
5. AssemblyLoader.LoadAssembliesFrom("Mods/")
   └─> All *.dll files in Mods/ loaded dynamically
   ↓
6. PreInitBootstrap.ExecutePreInitModules()
   └─> Scans all loaded assemblies for IAsherPreInitModule
   └─> Executes each module's Execute() method
   └─> Example: DebugPreInitModule sets DebugState.EnableDebug = true
   ↓
7. PatchModuleLoader.Load()
   ├─> Creates Harmony instance ("com.asher.runtime.mods")
   ├─> Scans for IAsherPatchModule implementations
   ├─> Applies each module's patches via Harmony
   ├─> Example: DebugPatchModule patches Game1.Initialize
   ├─> LifecycleModuleLoader.Load()
   │   └─> Registers IAsherLifecycleModule instances
   └─> HarmonyLifecycleBootstrap.InitializeIfNeeded()
       └─> Applies lifecycle hooks only if modules require them
   ↓
8. Dust.Program.Main(args) invoked via Reflection
   ↓
9. Game executes normally with patches applied
   ├─> Game1.Initialize() 
   │   ├─> [Harmony Patch] DebugPatchModule.EnableDebug()
   │   │   └─> Sets canDebug = true
   │   └─> [Lifecycle Hook] GameLifecycleHooks.OnGameInitialized()
   │       └─> Notifies lifecycle modules
   ├─> Game1.LoadContent()
   │   └─> [Lifecycle Hook] GameLifecycleHooks.OnContentLoaded()
   │       └─> Notifies lifecycle modules
   └─> Game1.Update() + Draw() loop continues
```

---

## 📁 Runtime Folder Layout

```
/GameFolder/
├── DustAET.exe              (Asher.Launcher.exe wrapper)
├── DustAET.real.exe         (original game executable)
├── Asher.Runtime.dll
├── Asher.SDK.dll
├── 0Harmony.dll
│
├── /AsherLogs/
│   └── runtime_20260122_211759.log
│
└── /Mods/
    ├── Asher.Patching.DebugEnabler.dll
    ├── Asher.Patching.IntroSkipper.dll
    └── Asher.Patching.GraphicsDeprofiler.dll
    ├── /config/
    │   └── runtime.cfg (future)
    └── /cache/
```

Folders are created automatically on first launch.

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
| **First gameplay patch**  | ✅ Done     | **Debug Menu Enabler working**                 |
| External mod loader       | ✅ Done     | Dynamic .dll loading from Mods/ folder         |
| Mod metadata (json)       | 🔜 Planned  | mod.json for description, dependencies, etc.   |
| Content patcher           | 🔜 Planned  | XNA ContentManager interception                |
| WPF UI integration        | 🔜 Planned  | Mod selection & priority UI                    |
| Public Mod API docs       | 🔜 Planned  | Developer documentation and examples           |

---

## 🧩 Learning & Implementation Kanban

### 🟢 DONE — Consolidated Phases

#### 🟢 Task 0 — Core Architecture & Bootstrap
**Status:** ✔ Completed

**Deliverables achieved:**
- Stable multi-project solution architecture
- Clear separation between:
  - Launcher (entry point)
  - Runtime (core infrastructure)
  - SDK (mod developer API)
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
- Generics are used for Asher infrastructure, not for game analysis
- Reflection is used for:
  - Runtime type inspection
  - Private member access
  - Harmony patch support
- Harmony is the official runtime patch engine
- No permanent modification of game binaries or `.xnb` files

**Practical outcome:**
- Strong conceptual foundation
- Clear understanding of each tool's responsibility
- No architectural uncertainty

#### 🟢 Task 2 — Launcher-Based Runtime Control
**Status:** ✔ Completed

**What was achieved:**
- Custom launcher fully controls game startup
- Runtime initialization occurs before game execution
- Logging, folders, and context prepared deterministically
- Runtime survives Steam launches transparently
- Injection is delayed and explicit, never blind

**Result:**
- Asher fully owns the runtime lifecycle
- Safe environment for Harmony patches

#### 🟢 Task 3 — Module System Architecture
**Status:** ✔ Completed

**What was implemented:**
1. **PreInit System**
   - `IAsherPreInitModule` interface
   - `PreInitBootstrap` for module discovery and execution
   - Enables configuration before patches are applied
   - Example: Setting flags, initializing shared state

2. **Patch System**
   - `IAsherPatchModule` interface
   - `PatchModuleLoader` for Harmony patch application
   - Scans all loaded assemblies for patch modules
   - Applies patches before game initialization

3. **Lifecycle System**
   - `IAsherLifecycleModule` interface
   - `AsherLifecycleModuleBase` abstract class for easier implementation
   - `LifecycleModuleLoader` for event registration
   - `GameLifecycleHooks` for automatic event capture
   - `HarmonyLifecycleBootstrap` for optional hook injection
   - Event bus pattern for decoupled communication

**Result:**
- Complete modding API with three distinct extension points
- Mods can hook into PreInit, Patching, and Lifecycle events
- Clean separation of concerns

#### 🟢 Task 4 — First Runtime Patch (Debug Enabler)
**Status:** ✔ Completed ✨

**What was achieved:**
- ✅ Harmony successfully injected into game process
- ✅ First working gameplay patch: **Debug Menu Enabler**
- ✅ Patch modifies `Dust.Game1.canDebug` field at runtime
- ✅ Debug menu accessible via Tab key in pause menu
- ✅ PreInit → Patch → Lifecycle flow validated
- ✅ All three module types demonstrated in one mod
- ✅ Comprehensive logging confirms execution flow

**Implementation details:**
- **PreInit**: Sets `DebugState.EnableDebug = true`
- **Patch**: Harmony postfix on `Game1.Initialize()` sets `canDebug = true`
- **Lifecycle**: Logs when `Initialize` and `LoadContent` complete

**Result:**
- **First gameplay modification working with Harmony**
- Proof of concept for modding capabilities

---

### 🟨 DOING — Current Phase

#### 🟨 Task 5 — Patch Porting & Reverse Engineering

**Status:** 🔄 In Progress

**Objective:**
Port and modernize existing gameplay patches from **DustAetPatchingPlatform** into the **Asher runtime architecture**, ensuring safety, modularity, and full lifecycle control. This phase focuses on **systematically converting known, proven patches** into Asher-compliant modules.

**Core strategy:**
* Preserve original gameplay intent while:
  * Removing hard-coded execution timing
  * Integrating clean PreInit / Patch / Lifecycle separation
* Ensure every converted patch:
  * Is reversible
  * Is configurable via PreInit
  * Respects Asher’s controlled bootstrap sequence

**Current workflow:**
1. Analyze original patch behavior and assumptions
2. Inspect game internals using **dnSpy** when required
3. Identify the *lifecycle moment* to intervene
4. Reimplement the patch using:
   * `IAsherPreInitModule` for configuration
   * `IAsherPatchModule` for Harmony patches
   * Optional lifecycle hooks for timing safety
5. Validate behavior with runtime logs and execution

**Technical approach:**
* Direct assembly reference to `DustAET.real.exe`
  * Enables IntelliSense and type-safe development
  * Mirrors SMAPI’s approach to Stardew Valley
* Harmony used exclusively for runtime patching

---

### 🟥 BACKLOG — Short Term (Next ~2 Months)

#### 🔴 Task 6 — Mod Metadata System
- Design `mod.json` schema
- Parse metadata on mod load
- Display mod information in logs
- Support for:
  - Name, description, author
  - Version and dependencies
  - Load priority
  - Enabled/disabled state

#### 🔴 Task 7 — Content Patcher (Core)
- Intercept `ContentManager.Load<T>()`
- Resolve replacements via `content.json`
- Support external assets:
  - Textures (`.png`)
  - Fonts (`.spritefont`)
  - Data files (`.json`, `.xml`)
- Hot-reload capability for development

#### 🔴 Task 8 — Mod Configuration System
- `IAsherConfigModule` interface
- Per-mod configuration files
- JSON/XML support
- Runtime reloading
- UI integration for settings

#### 🔴 Task 9 — WPF UI Integration
**Components:**
- Mod list view (installed mods)
- Enable/disable toggles
- Load order management (drag & drop)
- Per-mod configuration panel
- Runtime log viewer
- Mod installation wizard

**Persistence:**
- `selected_mods.json` (enabled mods)
- `load_order.json` (priority)
- `mod_configs/` folder (per-mod settings)

#### 🔴 Task 10 — Mod Dependency System
- Declare dependencies in `mod.json`
- Validate dependency graph on load
- Automatic load order resolution
- Optional dependencies support
- Version compatibility checks

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

### **Modding Capabilities**
- ✅ Dynamic mod loading from `Mods/` folder
- ✅ Three-stage mod lifecycle:
  1. **PreInit** - Configuration before patches
  2. **Patch** - Harmony runtime patching
  3. **Lifecycle** - React to game events
- ✅ Clean SDK for mod developers
- ✅ Comprehensive logging system
- ✅ Event-driven architecture

### **Working Mods**
* ✅ **Debug Menu Enabler**
  * Enables Debug Menu by pressing Tab in Pause Menu        
* ✅ **Intro Skipper**
  * Skips ESRB rating, splash screens, and startup videos
* ✅ **Graphics Deprofiler**
  * Bypasses HiDef GPU profile restrictions

All implemented as external, runtime-loaded mods.

---

## 📘 References

- **Harmony** — https://github.com/pardeike/Harmony
- **SMAPI** — https://github.com/Pathoschild/SMAPI
- **SMAPI Content Patcher** — https://stardewvalleywiki.com/Modding:Content_Patcher
- **DustAetPatchingPlatform** — https://github.com/GMMan/DustAetPatchingPlatform
  - Steam Forum Discussion — https://steamcommunity.com/app/236090/discussions/0/540744936409038540/

*Last Updated: January 23, 2026*
