# 🧱 Asher Modding Platform

**Asher** is a launcher-based modding platform for *Dust: An Elysian Tail*, designed to support **runtime code patching** and **content replacement** in a safe, modular, and reversible way.

Inspired by mature mod loaders such as **SMAPI**, Asher prioritizes **stability, explicit initialization control, and clean debugging**, avoiding fragile early-injection patterns.

---

## 🎯 Project Goals

- Runtime code patching using **Harmony**
- Asset replacement without modifying `.xnb` files
- Modular and reversible mod loading
- UI-based patch selection and configuration
- Full compatibility with Steam / XNA / .NET Framework

---

## 🧠 Core Architecture (Launcher-First)

Asher is built around a **custom launcher**, which guarantees correct initialization order and reliable runtime behavior.

**Key principle:**  
Injection is controlled and delayed — never performed blindly at process start.

---

## 🗂️ Solution Structure

```
/Asher.sln
│
├── Asher.App/                          → WPF configuration UI (.NET 8)
│   └── Asher.App.exe                   → Patch selection & user settings
│
├── Asher.Launcher/                     → Custom game launcher (.NET 8)
│   └── Asher.Launcher.exe              → Launches the game and controls injection
│
├── Asher.Bootstrap/                    → Runtime bootstrap (.NET Framework 4.7.2)
│                                         → Executes inside the game process
│
├── Asher.Core/                         → Shared models and abstractions
├── Asher.Localization/                 → Localization support
├── Asher.Runtime/                      → Runtime mod loader foundation
│
├── Asher.Services/                     → Core services
│   ├── DllInjectorService
│   ├── GameFolderService
│   ├── HarmonyService
│   └── PatchManagerService
│
├── Asher.UserInterface/                → Prism MVVM UI modules
│
├── /patches/                           → Mods and content patches (planned)
└── /Asher.Config/                      → User configuration data (planned)
```

---

## 🚀 Runtime Flow Overview

1. User launches `Asher.Launcher.exe`.
2. Launcher detects the game installation folder.
3. Launcher validates required runtime files.
4. Launcher starts `DustAET.exe` in a controlled state.
5. After process stabilization, the launcher injects `Asher.Bootstrap`.
6. Bootstrap loads `Asher.Runtime` inside the game process.
7. Runtime initializes logging, mod discovery, and patch application.

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

## 📁 Runtime Folder Layout

```
/GameFolder/
├── DustAET.exe
├── Asher.Bootstrap.dll
├── Asher.Runtime.dll
└── /AsherLogs/
    ├── bootstrap.log
    ├── runtime.log
    └── injection.log
```

---

## ✅ Final Result

A powerful and modular modding architecture for Dust: An Elysian Tail that supports:
- Runtime patching via Harmony
- Asset replacement via JSON and external files
- Clean, reversible mod deployment
- UI-based mod selection and management

---

## 📘 References

- Harmony: https://github.com/pardeike/Harmony
- SMAPI Content Patcher: https://stardewvalleywiki.com/Modding:Content_Patcher
