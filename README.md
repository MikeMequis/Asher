**English** | [Português (Brasil)](README.pt-BR.md)

# 🧱 Asher Modding Platform

**Asher** is a launcher-based modding platform for [*Dust: An Elysian Tail*](https://store.steampowered.com/app/236090/Dust_An_Elysian_Tail/). It applies **runtime code patches** (Harmony) and is designed to replace content without editing the game's files — safe, modular, and reversible.

Inspired by mod loaders such as **SMAPI**, Asher favors explicit initialization order, runtime lifecycle control, and clean debugging over fragile early-injection.

## What it includes

- **Windows launcher** — replaces `DustAET.exe` and controls startup order (the original is kept as `DustAET.real.exe`)
- **Linux bootstrap** — the native `DustAET` attaches through `libasher_bootstrap.so` (`LD_PRELOAD`); no launcher swap
- **Runtime mod loader** — Harmony-based patching with PreInit → Patch → Lifecycle stages
- **Electron manager** (`Asher.Electron` + `Asher.Host`) — installer, Patch Manager, settings, and game launch
- **Mod SDK** — interfaces for building external mods loaded from `Asher/Mods/`

## Running Start

1. From `Asher.Electron/`, build the host and start the manager:
   ```bash
   npm install
   npm run build:host:debug   # Windows
   # or: npm run build:host:linux   # Linux (publishes build/linux-host)
   npm start
   ```
   On Linux, Electron also needs its system libraries (NSS/NSPR/ALSA, GTK, and FUSE for the AppImage) — see [Linux dependencies](docs/Cross-Platform-Architecture.md#linux-dependencies).
2. Use **Setup** to detect and save your game folder, then **Install**.
3. Launch the game via **Steam** or the manager's **Launch Game** button (on Linux, launch through the manager; external Steam/desktop launch is not implemented yet).

### Addendum — XNA Framework (build dependency)

`Asher.Runtime` and the patching projects reference **Microsoft XNA Framework 4.0** assemblies from the GAC on Windows (the same runtime Dust uses). Without them, `npm run build:host` / patching builds fail or warn about missing `Microsoft.Xna.Framework*`.

1. Install **[Microsoft XNA Framework Redistributable 4.0](https://www.microsoft.com/en-us/download/details.aspx?id=20914)** (or the [4.0 Refresh](https://www.microsoft.com/en-us/download/details.aspx?id=27598)).
2. Prefer the **x86** redistributable — Asher targets `Platform=x86`.
3. Rebuild: `cd Asher.Electron && npm run build:host:debug`.

Steam installs of Dust often already place these assemblies on the machine. Linux uses **Mono/FNA** instead; the GAC requirement is Windows-only.

## Included Patches

Five patch modules ship by default. Each is an external mod loaded at runtime from `Asher/Mods/`:

- **Debug Menu Enabler** — `Tab` in the pause menu opens the debug menu
- **Intro Skipper** — skips the ESRB rating, splash screens, and startup videos
- **Graphics Deprofiler** — bypasses HiDef GPU profile restrictions
- **Mute Voice Acting** — mutes voice acting while keeping other SFX
- **Dust Storm Overheat Disabler** — prevents Dust Storm from overheating

## Build & Distribution

Summary only; the website's **Build & Distribution** page is the full reference.

```bash
cd Asher.Electron
npm run build:host         # Release backend (Host + runtime + patches)
npm run build:host:debug   # Debug backend for local UI work
npm start                  # run the manager in development
npm run dist               # NSIS installer + portable zip + latest.yml + sync Distribution/
npm run publish            # publish a GitHub Release (requires private/GH_TOKEN)
```

Users run the NSIS installer (`Asher-Setup-<version>.exe`) or extract the portable zip (or use `Distribution/`), then install into the game folder. The manager stays in `Distribution`; the game folder gets runtime files plus `Uninstall-Asher.cmd` beside `DustAET.exe` for emergency restore.

Linux is packaged on a Linux host: `npm run dist:linux` / `npm run publish:linux` produce `Asher-<version>-linux-x86_64.AppImage`, `Asher-<version>-linux-x64.tar.gz`, and `latest-linux.yml`. Linux updates are manual GitHub release downloads.

## AI-assisted development
Asher is built with heavy AI assistance, primarily through **OpenCode**. AI helps with implementation, investigation, refactoring, testing, debugging, and documentation. Architecture, technical direction, scope, validation, and the final call on what ships stay human-directed. Check [\[\[❓ FAQ\]\]](https://mikesstash.com.br/asher/faq/) for a more detailed answer.

## Documentation

- **Website** — https://mikesstash.com.br/asher/ (user guide, architecture, status, FAQ)
- [Cross-platform architecture](docs/Cross-Platform-Architecture.md) — platform contracts, Linux layout/build/packaging
- [Linux bootstrap](Asher.Linux/README.md) — native `LD_PRELOAD` entry + patch orchestration
