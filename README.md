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
│   │   ├── AssemblyLoader.cs           → Dynamic mod assembly loading
│   │   ├── PreInitBootstrap.cs         → PreInit module discovery & execution
│   │   ├── PatchModuleLoader.cs        → Harmony patch application
│   │   ├── LifecycleModuleLoader.cs    → Lifecycle event registration
│   │   ├── GameLifecycleHooks.cs       → Game lifecycle event hooks
│   │   └── HarmonyLifecycleBootstrap.cs → Optional lifecycle hook injection
│   ├── Core/
│   │   ├── RuntimeContext.cs           → Configuration and paths
│   │   ├── RuntimeController.cs        → Initialization and shutdown
│   │   ├── RuntimeResult.cs            → Operation result wrapper
│   │   └── GameContext.cs              → Game instance access (future)
│   ├── Lifecycle/
│   │   ├── GameLifecycle.cs            → Lifecycle state management
│   │   └── GameLifecycleState.cs       → Lifecycle state enum
│   ├── Logging/
│   │   └── RuntimeLoggerAdapter.cs     → SDK logger bridge
│   ├── RuntimeEntry.cs                 → Public runtime API
│   └── RuntimeLogger.cs                → File-based logging system
│
├── Asher.SDK/              → API for mod developers (.NET Framework 4.8)
│   ├── Logging/
│   │   ├── IAsherLogger.cs             → Logger interface
│   │   └── AsherLog.cs                 → Static logger facade
│   └── Patching/
│       ├── IAsherPatchModule.cs        → Harmony patch module interface
│       ├── IAsherPreInitModule.cs      → PreInit module interface
│       └── IAsherLifecycleModule.cs    → Lifecycle event interface
│
├── /Mods/                  → External mod assemblies (runtime-loaded)
│   └── Asher.Patching.DebugEnabler.dll → First working mod (Debug Menu)
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

## 📦 Mod Structure Example

### **Debug Enabler Mod** (First Working Implementation)

```
Asher.Patching.DebugEnabler/
├── DebugState.cs              → Shared state between modules
├── DebugPreInitModule.cs      → PreInit: sets EnableDebug flag
├── DebugPatchModule.cs        → Patch: modifies canDebug field
└── DebugLifecycleModule.cs    → Lifecycle: logs game events
```

**DebugPreInitModule.cs:**
```csharp
public sealed class DebugPreInitModule : IAsherPreInitModule
{
    public string Name => "Debug Enabler (PreInit)";
    
    public void Execute()
    {
        DebugState.EnableDebug = true;
        AsherLog.Info("Debug flag marked for activation (PreInit).");
    }
}
```

**DebugPatchModule.cs:**
```csharp
public sealed class DebugPatchModule : IAsherPatchModule
{
    public string Name => "Debug Enabler";
    
    public void Apply(Harmony harmony)
    {
        if (!DebugState.EnableDebug) return;
        
        var game1Type = /* find Dust.Game1 */;
        var initMethod = game1Type.GetMethod("Initialize", /*...*/);
        
        harmony.Patch(
            initMethod,
            postfix: new HarmonyMethod(typeof(DebugPatchModule), nameof(EnableDebug))
        );
    }
    
    static void EnableDebug(object __instance)
    {
        var canDebugField = __instance.GetType().GetField("canDebug", /*...*/);
        canDebugField.SetValue(null, true);
        AsherLog.Info("Debug enabled: canDebug = true");
    }
}
```

**DebugLifecycleModule.cs:**
```csharp
public sealed class DebugLifecycleModule : AsherLifecycleModuleBase
{
    public override string Name => "Debug Lifecycle Monitor";
    
    public override void OnGameInitialized()
    {
        AsherLog.Info("✓ Game1.Initialize completed!");
    }
    
    public override void OnContentLoaded()
    {
        AsherLog.Info("✓ Game1.LoadContent completed!");
    }
}
```

**Result:** Debug menu accessible via **Tab** key in pause menu ✅

---

## 📁 Runtime Folder Layout

```
/GameFolder/
├── DustAET.exe              (Asher.Launcher.exe wrapper)
├── DustAET.real.exe         (original game executable)
├── Asher.Launcher.exe       (can coexist, optional)
├── Asher.Runtime.dll
├── Asher.SDK.dll
├── 0Harmony.dll
│
├── /AsherLogs/
│   └── runtime_20260122_211759.log
│
└── /Mods/
    ├── Asher.Patching.DebugEnabler.dll
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

**Log evidence (working):**
```
[PreInit] Módulo encontrado: Asher.Patching.DebugEnabler.DebugPreInitModule
[PreInit] Executando módulo: Debug Enabler (PreInit)
Debug flag marcada para ativação (PreInit).
[PatchModuleLoader] Aplicando módulo: Debug Enabler
[DebugPatch] ✓ Patch aplicado com sucesso!
[DebugPatch] EnableDebug chamado!
[DebugPatch] ✓ canDebug = true
[LifecycleModuleLoader] Notificando módulos: GameInitialized
[DebugLifecycle] ✓ Game1.Initialize concluído!
```

**Result:**
- **First gameplay modification confirmed working** 🎉
- Proof of concept for all modding capabilities
- Foundation ready for complex mods

---

### 🟨 DOING — Current Phase

#### 🟨 Task 5 — Reverse Engineering & Mod Development
**Status:** 🔄 In Progress

**Objective:** Map the internal structure of Dust: An Elysian Tail to enable more gameplay patches.

**Current approach:**
- Direct assembly references to `DustAET.real.exe` (similar to SMAPI)
- Enables IntelliSense and type-safe patch development
- dnSpy for deeper inspection when needed

**Focus areas:**
- Player character mechanics
- Combat system
- Item/inventory management
- UI systems
- Save/load system
- Asset loading pipeline (ContentManager)

**Next mods to implement:**
- Speed multiplier
- Invincibility toggle
- Item spawner
- Custom UI elements
- Asset replacement (textures, fonts)

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

### **First Working Mod**
- ✅ **Debug Menu Enabler**
  - Enables hidden debug menu in Dust: An Elysian Tail
  - Accessible via Tab key in pause menu
  - Demonstrates all three module types
  - Serves as reference implementation for mod developers

### **Runtime Stability**
- ✅ Compatible with Steam launches
- ✅ No game modifications required
- ✅ Clean shutdown and error handling
- ✅ Isolated mod failures (one mod crash doesn't kill runtime)

---

## 📘 References

- **Harmony** — https://github.com/pardeike/Harmony
- **SMAPI** — https://github.com/Pathoschild/SMAPI
- **SMAPI Content Patcher** — https://stardewvalleywiki.com/Modding:Content_Patcher
- **DustAetPatchingPlatform** — https://github.com/GMMan/DustAetPatchingPlatform
  - Steam Forum Discussion — https://steamcommunity.com/app/236090/discussions/0/540744936409038540/

---

## 🎯 Key Achievement

**Asher has successfully transitioned from theoretical infrastructure to a working modding platform with proven runtime patching capabilities.**

The Debug Enabler mod serves as:
- ✅ Proof of concept for Harmony integration
- ✅ Reference implementation for mod developers
- ✅ Validation of the three-stage architecture
- ✅ Foundation for more complex gameplay modifications

**Next milestone:** Expand mod library with gameplay-focused patches (player stats, items, combat, etc.)

---

*Last Updated: January 22, 2026*