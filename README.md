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
├── Asher.App/              → WPF configuration UI (.NET 8)
│ └── Asher.App.exe         → Patch selection & user settings
│
├── Asher.Launcher/         → Custom game launcher (.NET Framework 4.7.2)
│ └── Asher.Launcher.exe    → Launches the game and controls runtime
│
├── Asher.Core/             → Shared models and abstractions
├── Asher.Localization/     → Localization support
├── Asher.Runtime/          → Runtime mod loader foundation (.NET Framework 4.7.2)
│
├── Asher.Services/         → Core services
│ ├── GameFolderService
│ ├── HarmonyService
│ └── PatchManagerService
│
├── Asher.UserInterface/    → Prism MVVM UI modules
│
├── /patches/               → Mods and content patches (planned)
└── /Asher.Config/          → User configuration data (planned)
```

---

## 🚀 Runtime Flow Overview

1. User launches `DustAET.exe` (renamed `Asher.Launcher.exe`)
2. Launcher validates the game installation
3. Runtime environment is prepared (logs, folders, config)
4. Launcher loads `DustAET.real.exe` (original game executable)
5. Asher Runtime initializes:
   - Logging system
   - Runtime lifecycle
   - Harmony patch bootstrap
6. The game continues execution normally, now under Asher control

---

## 📦 Patch Metadata Example

```json
{
  "name": "Debug Enabler",
  "description": "Enables the debug console on startup.",
  "author": "Hales",
  "priority": 1,
  "entry": "DebugEnablerPatch.dll",
  "enabled": true
}
```

---

## 📊 Project Summary — Backlog & Progress

The Asher Modding Platform has been developed using an infrastructure-first approach, prioritizing runtime stability, explicit initialization order, and reversibility before any gameplay-level patching.

Instead of rushing into early hooks or fragile injections, the project intentionally focused on launcher control, runtime lifecycle, and clear architectural boundaries, ensuring that future modding features are built on a solid and predictable foundation.

## 📁 Runtime Folder Layout

```
/GameFolder/
├── DustAET.exe          (Asher.Launcher.exe)
├── DustAET.real.exe     (original game executable)
├── Asher.Runtime.dll
├── 0Harmony.dll
│
├── /AsherLogs/
│   └── runtime.log
│
└── /Mods/
```

Folders are created automatically on first launch.

---

## 📊 Project Status Overview

| Area                   | Status      | Notes                                     |
| ---------------------- | ----------- | ----------------------------------------- |
| Solution structure     | ✅ Done     | Multi-project architecture stabilized     |
| Launcher-based runtime | ✅ Done     | Wrapper EXE approach validated             |
| Steam compatibility    | ✅ Done     | Game launches normally via Steam           |
| Runtime initialization | ✅ Done     | Logs, lifecycle and folders working        |
| Injection strategy     | ✅ Finalized| No blind injection                         |
| Harmony bootstrap      | ✅ Done     | Runtime patching confirmed                 |
| Gameplay patches       | 🔜 Planned  | Dust-specific Harmony patches              |
| Mod loader (.dll)      | 🔜 Planned  | External assemblies + metadata             |
| Content patcher        | 🔜 Planned  | XNA ContentManager interception            |
| WPF UI integration     | 🔜 Planned  | Mod selection & priority UI                |
| Public Mod API         | 🔜 Planned  | Stable developer-facing API                |

---

## 🧩 Learning & Implementation Kanban

### 🟢 Completed Phases

#### 🟢 Task 0: Core Architecture & Bootstrap

**Status:** ✔ Completed

**Deliverables:**
- Stable multi-project solution architecture with clear separation of concerns:
  - Launcher
  - Runtime
  - Core abstractions
  - WPF UI (Prism/MVVM)
- Wrapper EXE approach validated:
  - `DustAET.exe` → Asher Launcher
  - `DustAET.real.exe` → Original game executable
- Full Steam compatibility preserved
- Explicit runtime initialization order guaranteed

---

#### 🟢 Task 1: Runtime Modding Foundations

**Status:** ✔ Completed

**Concepts Mastered:**
- Generics
- Reflection
- Harmony (Prefix/Postfix/Transpiler)

**Key Decisions:**
- Generics are used for Asher infrastructure, not game analysis
- Reflection handles runtime inspection, private member access, and Harmony support
- Harmony serves as the official runtime patch engine
- No permanent modification of game binaries or `.xnb` files

**Outcome:**
- Strong conceptual foundation established
- Clear understanding of each tool's responsibility
- Zero architectural uncertainty moving forward

---

#### 🟢 Task 2: Launcher-Based Runtime Control

**Status:** ✔ Completed

**Achievements:**
- Custom launcher controls game startup lifecycle
- Runtime initialization occurs before game execution
- Logging, folders, and context prepared deterministically
- Runtime survives Steam launches transparently
- Injection is delayed and explicit, never blind

**Result:**
- Asher fully owns the runtime lifecycle
- Safe environment established for future Harmony patches

---

### 🟨 Current Phase

#### 🟨 Task 3: Reverse Engineering (dnSpy)

**Status:** 🔄 In Progress

**Objective:**  
Map the internal structure of *Dust: An Elysian Tail* to enable real gameplay patches.

**Focus Areas:**
- Main game loop
- Game initialization sequence
- Game-derived class structure
- ContentManager usage
- Core systems: Player, UI, Assets, Global state

**Expected Deliverables:**
- List of key classes
- Candidate methods for Harmony patches
- Ideal hook points for debug flags, gameplay logic, and content interception

**Note:**  
This phase uses direct references to DustAET assemblies to enable IntelliSense and safer patch development, similar to SMAPI's approach.

---

### 🟥 Short-Term Backlog (Next ~2 Months)

#### 🔴 Task 4: First Runtime Patch (Hello World)

- Inject Harmony successfully into the game process
- Create a minimal patch (log, flag, or debug output)
- Confirm execution inside the game process

---

#### 🔴 Task 5: External Mod Loader (.dll)

- Load external mod assemblies dynamically
- Read and parse `mod.json` files
- Initialize mods with isolated failure handling per mod

---

#### 🔴 Task 6: Content Patcher (Core)

- Intercept `ContentManager.Load<T>()` calls
- Resolve replacements via `content.json`
- Support external assets:
  - Textures
  - Fonts
  - Data files

---

#### 🔴 Task 7: Launcher ↔ UI Integration (WPF)

- Mod selection UI
- Persistence via `selected_patches.json`
- Load order and priority management
- Runtime logs visible in UI

---
## 🧠 Project Principles (Non-Negotiable)

- 🚫 No `.xnb` editing
- 🚫 No permanent binary modification
- ✅ 100% runtime patching
- ✅ Fully reversible
- ✅ Modular and extensible
- ✅ Inspired by SMAPI, adapted to Dust

---

## 📘 References

- Harmony — https://github.com/pardeike/Harmony
- SMAPI — https://github.com/Pathoschild/SMAPI
- SMAPI Content Patcher — https://stardewvalleywiki.com/Modding:Content_Patcher
