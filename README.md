
# 🧱 Asher Modding Platform - Unified Project Plan

This document unifies the full architecture and roadmap for the *Asher* modding tool, focused on supporting runtime mod loading and content replacement for the game *Dust: An Elysian Tail*. It integrates both the structural foundation for mod management and the dynamic content patcher system.

---

## ✅ Part 1: Solution Structure

```
/Asher.sln
│
├── Asher.App/                          → WPF launcher (UI, .NET 8) ✅ IMPLEMENTED
├── Asher.Bootstrap/                    → DLL injection bootstrap (.NET Framework 4.7.2) ✅ IMPLEMENTED
├── Asher.Core/                         → Shared models and data structures ✅ IMPLEMENTED
├── Asher.Localization/                 → Localization support ✅ IMPLEMENTED
├── Asher.Runtime/                      → Runtime mod loader (.NET Framework 4.7.2) ✅ FOUNDATION
├── Asher.Services/                     → Business logic services ✅ IMPLEMENTED
│   ├── GameLauncher                    → Launches game with injection support
│   ├── DllInjectorService             → DLL injection into game process
│   ├── GameFolderService              → Game folder detection
│   ├── HarmonyService                 → Harmony patch management
│   └── PatchManagerService            → Patch management
├── Asher.UserInterface/                → Prism MVVM UI modules ✅ IMPLEMENTED
│   ├── ViewModels/                     → MVVM view models
│   ├── Views/                          → WPF views
│   └── ViewsModule                    → Prism module registration
├── /patches/                          → Folder with patch .dll and content.json files (PLANNED)
└── /Asher.Config/                     → Stores selected patches and user settings (PLANNED)
```

---

## 🧩 Part 2: Responsibility Breakdown

| Project / Namespace                 | Status | Responsibility                                                  |
|------------------------------------|--------|------------------------------------------------------------------|
| `Asher.App`                        | ✅     | WPF Application Entry (UI Layer, Prism container setup)         |
| `Asher.Bootstrap`                  | ✅     | DLL injection entry point, loads Runtime into game process      |
| `Asher.Core`                       | ✅     | Shared models (GameFolderInfo, HarmonyPatchInfo, etc.)         |
| `Asher.Localization`               | ✅     | Localization manager and string resources                       |
| `Asher.Runtime`                    | 🟡     | Runtime mod loader foundation (logging implemented)              |
| `Asher.Services`                   | ✅     | Business logic services (GameLauncher, DllInjector, etc.)       |
| `Asher.UserInterface`              | ✅     | Prism MVVM modules, ViewModels, and Views                       |
| `Asher.Runtime` (future)          | 📋     | Will load patches dynamically and apply Harmony patches        |
| `Asher.Runtime` (future)          | 📋     | Will handle content patching (asset replacement)                |

---

## 🧠 Part 3: Runtime Patch Loading Flow

### Current Implementation (✅ Working)
1. User launches Asher App (.NET 8 WPF UI).
2. App detects game folder automatically (Steam, GOG, Humble Bundle, or manual search).
3. `GameLauncher` copies `Asher.Bootstrap.dll` and `Asher.Runtime.dll` to game folder.
4. Game process (`DustAET.exe`) is started via Steam/launcher.
5. `DllInjectorService` injects `Asher.Bootstrap.dll` into the game process.
6. `AsherBootstrap` static constructor initializes and loads `Asher.Runtime.dll`.
7. `AsherRuntime` initializes and logs to `AsherLogs/runtime.log`.

### Future Implementation (📋 Planned)
1. User selects patches in Asher Launcher UI.
2. Selected patches are stored in `selected_patches.json`.
3. `Asher.Runtime` reads the patch list and loads each `.dll` dynamically.
4. Harmony applies all `[HarmonyPatch]` attributes and modifies runtime behavior.
5. Content patching intercepts `ContentManager.Load<T>()` for asset replacement.

---

## 📦 Part 4: Patch Metadata Format

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

## 📦 Part 5: Content Patcher System

Inspired by SMAPI’s `Content Patcher`, this system replaces assets like textures, fonts, or data *at runtime*, without editing `.xnb` files.

### Goals:
- No `.xnb` editing
- External asset loading
- Support for localization, conditional logic, and modularity

### System Components:
- **Mod Metadata**: `content.json`
- **Harmony Interceptor**: Hooks into `ContentManager.Load<T>()`
- **Asset Manager**: Loads external replacements
- **Patch Engine**: Parses `content.json`
- **Registry**: Maps asset names to replacements

---

## 🧾 Part 6: JSON Format Example (`content.json`)

```json
{
  "Format": "1.0.0",
  "Changes": [
    {
      "Action": "Load",
      "Target": "Characters/Fidget",
      "FromFile": "assets/fidget_new.png"
    },
    {
      "Action": "EditData",
      "Target": "Text/IntroDialogue",
      "Entries": {
        "Line1": "Welcome to the translated world."
      }
    },
    {
      "Action": "ReplaceFont",
      "Target": "Fonts/UIFont",
      "FromFile": "assets/pt-br.spritefont"
    }
  ]
}
```

---

## ⚙️ Part 7: Implementation Roadmap

### ✅ Completed Foundation
- [x] WPF launcher application with Prism MVVM
- [x] Game folder detection (Steam, GOG, Humble Bundle, manual)
- [x] DLL injection system (`DllInjectorService`)
- [x] Bootstrap loader that runs inside game process
- [x] Runtime foundation with logging
- [x] Service layer architecture
- [x] UI modules and navigation

### 🔧 Runtime Patching (📋 Planned)
- [ ] Implement `HarmonyManager` in `Asher.Runtime`
- [ ] Create reflection-based loader for `.dll` and `mod.json`
- [ ] Add patch discovery and error handling
- [ ] Integrate patch selection UI
- [ ] Load Harmony library into game process

### 🔧 Content Interception (📋 Planned)
- [ ] Patch `ContentManager.Load<T>()` with Harmony
- [ ] Parse `/patches/*/content.json`
- [ ] Maintain patch registry mapping
- [ ] Load and validate external assets
- [ ] Add fallback logging and debug modes
- [ ] Implement advanced `When` conditions (optional)

---

## 📁 Part 8: Folder Layout

### Current Implementation
```
/GameFolder/
├── DustAET.exe
├── Asher.Bootstrap.dll          ✅ Injected into game process
├── Asher.Runtime.dll             ✅ Loaded by Bootstrap
└── /AsherLogs/                   ✅ Created automatically
    ├── bootstrap.log              ✅ Bootstrap initialization logs
    ├── runtime.log                ✅ Runtime initialization logs
    └── injection.log              ✅ DLL injection attempt logs
```

### Future Implementation (Planned)
```
/GameFolder/
├── DustAET.exe
├── Asher.Bootstrap.dll
├── Asher.Runtime.dll
├── Harmony.dll                    (for future patch support)
├── /patches/
│   ├── DebugPatch/
│   │   ├── DebugPatch.dll
│   │   └── mod.json
│   ├── TranslatedText/
│   │   ├── content.json
│   │   └── assets/dialogue.json
│   └── CustomFidget/
│       ├── content.json
│       └── assets/fidget_new.png
└── /Asher.Config/
    └── selected_patches.json
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

- [Harmony GitHub](https://github.com/pardeike/Harmony)
- [SMAPI Content Patcher Documentation](https://stardewvalleywiki.com/Modding:Content_Patcher)
