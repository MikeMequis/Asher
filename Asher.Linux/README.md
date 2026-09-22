# Asher Linux Bootstrap (embedded Mono)

Native Linux bootstrap that attaches to the Mono runtime embedded in **DustAET**, initializes
`Asher.Runtime`, and runs the same generic patch orchestration as the Windows launcher. The manager
installs these artifacts into `<game>/Asher` and launches `DustAET` with `LD_PRELOAD`
(`docs/Cross-Platform-Architecture.md`); this folder builds the bootstrap and managed assemblies and
supports standalone development runs.

```text
libasher_bootstrap.so (LD_PRELOAD)
    -> existing Dust Mono runtime
    -> mono_get_root_domain() / mono_thread_attach()
    -> Asher.Runtime.RuntimeBootstrap.Initialize()
    -> RuntimeEntry.Init(default RuntimeContext)
    -> AssemblyLoader  (loads mods from <ModsPath>)
    -> PreInitBootstrap (configures patch modules)
    -> PatchModuleLoader (Harmony: applies IAsherPatchModule implementations)
```

`Asher.Runtime` owns the generic Harmony orchestration; patch implementations stay in `Asher.Patching.*`.
The runtime contains no patch-specific logic.

## Layout

```text
Asher.Linux/
├── Native/bootstrap.c        native LD_PRELOAD library
├── build.sh                  native build + local managed build + staging
├── build-managed.sh          builds the managed assemblies into out/
└── README.md
```

No DLLs are committed. `build.sh` builds everything into `out/`; the managed assemblies are compiled
locally from the repository sources.

## Requirements

- A C compiler (`gcc`).
- A Roslyn C# 9 compiler for the managed assemblies: `csc` (`mono-devel` provides it) or any command via
  the `CSC` environment variable (e.g. `CSC="dotnet exec /path/to/csc.dll"`). Mono's legacy
  `mcs`/`mono-csc` (C# 7.x) will not work.
- `FNA_DLL=/path/to/game/FNA.dll` to build `GraphicsDeprofiler` (it references
  `Microsoft.Xna.Framework.Graphics.GraphicsAdapter` at compile time). Without it the other patches still build.
- A Linux host with Dust installed.
- Install commands for the toolchain and Electron runtime: `docs/Cross-Platform-Architecture.md` → *Linux dependencies*.

> Match the architecture of `DustAET` for the native library (the shipped build is 64-bit; use
> `EXTRA_CFLAGS=-m32` for a 32-bit game). The managed DLLs are AnyCPU IL.

## Build

```bash
cd Asher.Linux
chmod +x build.sh build-managed.sh
./build.sh
```

`build.sh` builds the native library, then calls `build-managed.sh` if the managed assemblies are not
already present in `out/`. Output in `out/`:

```text
libasher_bootstrap.so
Asher.Runtime.dll
Asher.SDK.dll
0Harmony.dll
Mods/Asher.Patching.DebugEnabler.dll
Mods/Asher.Patching.IntroSkipper.dll
Mods/Asher.Patching.MuteVoiceActing.dll
Mods/Asher.Patching.OverheatDisabler.dll
Mods/Asher.Patching.GraphicsDeprofiler.dll   (only if FNA_DLL is set)
```

The managed assemblies compile from `Asher.SDK/`, `Asher.Runtime/` and `Patches/*`; `0Harmony.dll` is
taken from the vendored `packages/Lib.Harmony.2.4.2` package.

## Standalone run (development)

The manager performs the real install and launch; this recipe runs the bootstrap manually against a game
folder:

```bash
rm -rf ~/asher-bootstrap && mkdir -p ~/asher-bootstrap/Mods
cp out/Asher.Runtime.dll out/Asher.SDK.dll out/0Harmony.dll out/libasher_bootstrap.so ~/asher-bootstrap/
cp out/Mods/*.dll ~/asher-bootstrap/Mods/

cd /path/to/dust
ASHER_MODS_PATH=~/asher-bootstrap/Mods \
ASHER_LOG_PATH=~/asher-bootstrap/AsherLogs \
LD_PRELOAD=~/asher-bootstrap/libasher_bootstrap.so \
MONO_PATH=~/asher-bootstrap \
./DustAET
```

## Expected output

The native bootstrap writes `[AsherPoC] ...` diagnostics to stdout/stderr (the manager redirects these off
its JSONL channel). Per-module results go to `<ASHER_LOG_PATH>/runtime_<timestamp>.log`:

```text
[PreInit] Resumo: 6 módulos encontrados, 6 executados com sucesso.
[PatchModuleLoader] Aplicando módulo: Debug Enabler
[DebugEnabler] Patched Microsoft.Xna.Framework.Game.Initialize
[DebugEnabler] canDebug enabled via post-attach Game.Tick
[PatchModuleLoader] 4 módulos de patch aplicados.
```

`canDebug enabled via post-attach Game.Tick` confirms the DebugEnabler callback executed; the game keeps
running normally.

## Configuration

| Variable | Default | Purpose |
| --- | --- | --- |
| `ASHER_HOME` | `<game>/Asher` | Runtime home |
| `ASHER_MODS_PATH` | `<ASHER_HOME>/Mods` | Directory scanned for patch modules |
| `ASHER_LOG_PATH` | `<ASHER_HOME>/AsherLogs` | Runtime log directory |
| `ASHER_PROFILE` | `default` | Profile name in the RuntimeContext |
| `ASHER_BOOTSTRAP_ASSEMBLY` | `Asher.Runtime.dll` next to the `.so` | Managed entry assembly |
| `ASHER_BOOTSTRAP_AUTORUN` | `1` | `0` disables the automatic background bootstrap |
| `ASHER_BOOTSTRAP_TIMEOUT_MS` | `60000` | Max wait for runtime readiness (`0` = forever) |
| `ASHER_BOOTSTRAP_SETTLE_MS` | `1000` | Delay after the root domain appears before `mono_thread_attach` |

## Embedded-Mono notes

- Dust's Linux executable contains an FNA + Mono stack and exports the Mono embedding API
  (`mono_get_root_domain`, `mono_thread_attach`, `mono_assembly_open`, `mono_runtime_invoke`, ...). The
  bootstrap resolves those symbols with `dlsym(RTLD_DEFAULT, ...)` and attaches a background thread — no
  second runtime, no game modification, no `ptrace`/`/proc` access.
- The attach is necessarily **late**: calling `mono_thread_attach` as soon as `mono_get_root_domain()` is
  non-NULL aborts (`object.c:1938 'klass' not met`). FNA has already run its one-shot `Game.Initialize`.
  Patch modules resolve the **declared** method before patching (Harmony rejects inherited `MethodInfo`s),
  and DebugEnabler adds a post-attach hook on `Game.Tick`.
- `Harmony.GetPatchInfo` proves installation, not execution; the runtime log lines are the execution evidence.
- `Dust.Storage.Store::DUST_STORAGE_INTERNAL_SAVE` ("mono runtime and class libraries are out of sync") is
  Dust's own Steam-storage native internal call and is unrelated to Asher/Harmony.

## Known limits

- `mono_thread_attach` is timing-sensitive: attaching too early aborts with the `object.c:1938`
  assertion before any managed code. The bootstrap now waits `ASHER_BOOTSTRAP_SETTLE_MS` (default 1000 ms)
  after the root domain appears; raise it if a rare abort still occurs.
- `GameTitleBootstrap` is not invoked by the Linux entry point (it is only needed by the Windows launcher flow).
