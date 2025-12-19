
# 🧱 Asher Modding Platform - Unified Project Plan

This document unifies the full architecture and roadmap for the *Asher* modding tool, focused on supporting runtime mod loading and content replacement for the game *Dust: An Elysian Tail*. It integrates both the structural foundation for mod management and the dynamic content patcher system.

---

## ✅ Part 1: Solution Structure

```
/Asher.sln
│
├── Asher.App/                          → WPF launcher (UI, .NET 8)
├── Asher.Modules.Main/                → Main Prism module
├── Asher.Modules.PatchManager/        → UI for managing patches
├── Asher.Services.PatchService/       → Applies Harmony patches
├── Asher.Services.LoaderService/      → Launches game executable
├── Asher.Services.ModManagement/      → Handles mod metadata and priorities
├── Asher.Services.BackupService/      → Backup & restore utilities
├── Asher.Runtime.ModLoader/           → Runtime patch injector (.NET Framework 4.7.2)
├── Asher.Shared/                      → Shared models, interfaces, utils
├── /patches/                          → Folder with patch .dll and content.json files
├── /Asher.Config/                     → Stores selected patches and user settings
```

---

## 🧩 Part 2: Responsibility Breakdown

| Project / Namespace                 | Responsibility                                                  |
|------------------------------------|------------------------------------------------------------------|
| `Asher.App`                        | WPF Application Entry (UI Layer)                                |
| `Asher.Modules.*`                  | Prism MVVM modules                                              |
| `Asher.Services.PatchService`      | Applies patches using Harmony                                   |
| `Asher.Services.ModManagement`     | Handles loading mods and patch priority                         |
| `Asher.Services.LoaderService`     | Launches the game executable with patch support                 |
| `Asher.Runtime.ModLoader`          | Runs inside the game process and injects behavior (Harmony)     |

---

## 🧠 Part 3: Runtime Patch Loading Flow

1. User selects patches in Asher Launcher (.NET 8 UI).
2. Selected patches are stored in `selected_patches.json`.
3. Launcher starts the game (`DustAET.exe`) with the ModLoader present in the game folder.
4. `ModLoader.dll` reads the patch list and loads each `.dll` dynamically.
5. Harmony applies all `[HarmonyPatch]` attributes and modifies runtime behavior.

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

### 🔧 Runtime Patching
- [ ] Implement `HarmonyManager` in `Asher.Runtime.ModLoader`
- [ ] Create reflection-based loader for `.dll` and `mod.json`
- [ ] Add patch discovery and error handling
- [ ] Integrate patch selection UI

### 🔧 Content Interception
- [ ] Patch `ContentManager.Load<T>()` with Harmony
- [ ] Parse `/patches/*/content.json`
- [ ] Maintain patch registry mapping
- [ ] Load and validate external assets
- [ ] Add fallback logging and debug modes
- [ ] Implement advanced `When` conditions (optional)

---

## 📁 Part 8: Folder Layout Example

```
/GameFolder/
├── DustAET.exe
├── AsherModLoader.dll
├── Harmony.Asher.dll
├── /patches/
│   ├── DebugPatch/
│   │   ├── DebugPatch.dll
│   │   └── mod.json
│   ├── TranslatedText/
│   │   ├── content.json
│   │   └── assets/dialogue.json
│   ├── CustomFidget/
│   │   ├── content.json
│   │   └── assets/fidget_new.png
├── /Asher.Config/
│   └── selected_patches.json
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
