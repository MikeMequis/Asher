# 🧱 Asher Modding Platform

**Asher** is a launcher-based modding platform for *Dust: An Elysian Tail*, designed to support **runtime code patching** and **content replacement** in a safe, modular, and reversible way.

Inspired by mature mod loaders such as **SMAPI**, Asher prioritizes **explicit initialization order**, **runtime control**, and **clean debugging**, deliberately avoiding fragile early-injection patterns.

---

## 🎯 Project Goals

- Runtime code patching using **Harmony**
- Asset replacement without modifying `.xnb` files
- Modular and reversible mod loading
- UI-based patch selection and configuration
- Full compatibility with **Steam**, **XNA**, and **.NET Framework**

---

## 🧠 Core Architecture (Launcher-First)

Asher is built around a **custom launcher**, which guarantees correct initialization order and reliable runtime behavior.

**Key principle:**  
Injection and patching are **controlled and delayed**, never performed blindly at process startup.

This design mirrors proven approaches used by SMAPI and other stable modding platforms.

---

## 🗂️ Solution Structure

```
/Asher.sln
│
├── Asher.App/ 				→ WPF configuration UI (.NET 8)
│ └── Asher.App.exe 		→ Patch selection & user settings
│
├── Asher.Launcher/ 		→ Custom game launcher (.NET Framework 4.7.2)
│ └── Asher.Launcher.exe 	→ Launches the game and controls runtime
│
├── Asher.Core/ 			→ Shared models and abstractions
├── Asher.Localization/ 	→ Localization support
├── Asher.Runtime/ 			→ Runtime mod loader foundation (.NET Framework 4.7.2)
│
├── Asher.Services/ 		→ Core services
│ ├── GameFolderService
│ ├── HarmonyService
│ └── PatchManagerService
│
├── Asher.UserInterface/ 	→ Prism MVVM UI modules
│
├── /patches/ 				→ Mods and content patches (planned)
└── /Asher.Config/ 			→ User configuration data (planned)
```


---

## 🚀 Runtime Flow Overview

1. User launches `DustAET.exe` (`Asher.Launcher.exe`)
2. Launcher detects and validates the game installation
3. Launcher prepares the runtime environment
4. Launcher starts `DustAET.real.exe` (original `DustAET.exe`)
5. Asher Runtime initializes:
   - Logging
   - Mod discovery
   - Patch lifecycle
6. Game continues execution normally, now under Asher control

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

## 📁 Runtime Folder Layout (Build the solution and insert the files)
```
/GameFolder/
├── DustAET.exe (Renamed from Asher.Launcher.exe)
├── DustAET.real.exe (original .exe game file, renamed)
├── Asher.Runtime.dll
│
├── /AsherLogs/ (generated automatically)
│   └── runtime.log
│
└── /Mods/ (generated automatically)
```

---

## 📊 Project Status Overview
| Area                   | Status         | Notes                                 |
| ---------------------- | -------------- | ------------------------------------- |
| Solution structure     | ✅ Done         | Multi-project architecture stabilized |
| Launcher-based runtime | ✅ Done         | Wrapper EXE approach validated        |
| Steam compatibility    | ✅ Done         | Game launches normally via Steam      |
| Runtime initialization | ✅ Done         | Logging and folders created correctly |
| Injection approach     | ✅ Finalized    | Launcher-first (no blind injection)   |
| Harmony integration    | 🔄 In progress | Runtime ready, patching next          |
| Mod loader (.dll)      | 🔜 Planned     | External assemblies + metadata        |
| Content patcher        | 🔜 Planned     | ContentManager interception           |
| WPF UI integration     | 🔜 Planned     | Mod selection & priority              |
| Public Mod API         | 🔜 Planned     | Stable developer-facing API           |

---

## 🧩 Learning & Implementation Kanban (Summary)
### 🟢 Completed
Task 1 — Runtime Modding Foundations
- Generics
- Reflection
- Harmony (Prefix / Postfix / Transpiler)
- XNA Content Pipeline (conceptual level)

### Key decisions
- Generics used for infrastructure, not game analysis
- Reflection used for:
	- Runtime inspection
	- Private member access
	- Harmony support
- Harmony chosen as the patch engine
- No permanent binary or .xnb modification

### 🟨 Current / Next Step
Task 2 — Reverse Engineering (dnSpy)
- Map core Dust classes
- Identify:
	- Game loop
	- Initialization points
	- ContentManager usage
- Define ideal patch hooks

### 🟥 Short-Term Backlog
- Task 3 — First runtime patch (Hello World)
- Task 4 — External mod loader (.dll)
- Task 5 — Content patcher core
- Task 6 — Launcher ↔ UI integration

## 🧠 Project Principles (Non-Negotiable)
- 🚫 No .xnb editing
- 🚫 No permanent binary modification
- ✅ 100% runtime patching
- ✅ Fully reversible
- ✅ Modular and extensible
- ✅ Inspired by SMAPI, adapted to Dust

## 📘 References
- Harmony — https://github.com/pardeike/Harmony
- SMAPI — https://github.com/Pathoschild/SMAPI
- SMAPI Content Patcher — https://stardewvalleywiki.com/Modding:Content_Patcher
