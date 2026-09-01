# 🧱 Asher Modding Platform

**Asher** is a launcher-based modding platform for [*Dust: An Elysian Tail*](https://store.steampowered.com/app/236090/Dust_An_Elysian_Tail/), designed to support **runtime code patching** and **content replacement** in a safe, modular, and reversible way.

Inspired by mature mod loaders such as **SMAPI**, Asher prioritizes explicit initialization order, runtime lifecycle control, and clean debugging — deliberately avoiding fragile early-injection patterns.

## What it includes

- **Custom launcher** — wraps the game executable and controls startup order (`DustAET.exe` → original `DustAET.real.exe`)
- **Runtime mod loader** — Harmony-based patching with PreInit, Patch, and Lifecycle stages
- **Electron manager** (`Asher.Electron` + `Asher.Host`) — installer and mod manager UI with Patch Manager, settings, and game launch
- **Mod SDK** — interfaces for building external mods loaded from `Asher/Mods/`

## Goals

- Runtime code patching using **Harmony**
- Asset replacement without modifying `.xnb` files
- Modular and reversible mod loading
- UI-based patch selection and configuration
- Full compatibility with **Steam**, **XNA**, and **.NET Framework**

## Quick start

1. From `Asher.Electron/`, build the host and start the app:
   ```bash
   npm install
   npm run build:host:debug
   npm start
   ```
2. Use **Setup** to detect and save your game folder, then **Install**
3. Launch the game via **Steam** or the manager's **Launch Game** button

## Included mods

- **Debug Menu Enabler** — Tab in pause menu opens debug menu
- **Intro Skipper** — Skips ESRB rating, splash screens, and startup videos
- **Graphics Deprofiler** — Bypasses HiDef GPU profile restrictions

## Documentation & releases

- Full documentation: [mikesstash.vercel.app/asher/](https://mikesstash.vercel.app/asher/)
