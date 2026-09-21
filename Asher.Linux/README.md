# Asher Linux Bootstrap (embedded Mono)

Native Linux bootstrap that attaches to the Mono runtime embedded in **DustAET**, initializes
`Asher.Runtime`, and runs the same generic patch orchestration as the Windows launcher.

```text
libasher_bootstrap.so (LD_PRELOAD)
    -> existing Dust Mono runtime
    -> mono_get_root_domain() / mono_thread_attach()
    -> Asher.Runtime.RuntimeBootstrap.Initialize()
    -> RuntimeEntry.Init(default RuntimeContext)
    -> AssemblyLoader  (loads mods from <ModsPath>)
    -> PreInitBootstrap (configures patch modules)
    -> PatchModuleLoader (Harmony: applies IAsherPatchModule implementations)
    -> patch modules live in their own Asher.Patching.* assemblies
```

`Asher.Runtime` owns the generic Harmony orchestration; patch implementations stay in
`Asher.Patching.*` (currently `Asher.Patching.DebugEnabler`). The runtime contains no
patch-specific logic.

## Layout

```text
Asher.Linux/
├── Native/bootstrap.c        native LD_PRELOAD library
├── build.sh                  native build + local managed build + staging
├── build-managed.sh          builds the managed assemblies into out/
└── README.md
```

No DLLs are committed. `build.sh` builds everything into `out/`; the managed assemblies are
compiled locally from the repository sources.

## Requirements

- A C compiler (`gcc`).
- A Roslyn C# 9 compiler for the managed assemblies: `csc` (Mono 6.12+) or any compiler command
  provided via the `CSC` environment variable (e.g. `CSC="dotnet exec /path/to/csc.dll"`). Mono's
  legacy `mcs`/`mono-csc` (C# 7.x) will not work.
- The Linux VM with Dust installed.

> Match the architecture of DustAET for the native library (the shipped build is 64-bit; use
> `EXTRA_CFLAGS=-m32` for a 32-bit game). The managed DLLs are AnyCPU IL.

## Build

```bash
cd Asher.Linux
chmod +x build.sh build-managed.sh
./build.sh
```

`build.sh` builds the native library, then calls `build-managed.sh` if the managed assemblies are
not already present in `out/`. Output in `out/`:

```text
libasher_bootstrap.so
Asher.Runtime.dll
Asher.SDK.dll
0Harmony.dll
Mods/Asher.Patching.DebugEnabler.dll
```

The managed assemblies are compiled from `Asher.SDK/`, `Asher.Runtime/` and
`Patches/Asher.Patching.DebugEnabler/`; `0Harmony.dll` is taken from the vendored
`packages/Lib.Harmony.2.4.2` package. If the managed compiler is not on the build machine, build
them elsewhere with the same compiler and drop the three DLLs into `out/` before running the native
part of `build.sh`.

## Run

```bash
rm -rf /tmp/asher-bootstrap && mkdir -p /tmp/asher-bootstrap/Mods
cp out/Asher.Runtime.dll out/Asher.SDK.dll out/0Harmony.dll out/libasher_bootstrap.so /tmp/asher-bootstrap/
cp out/Mods/Asher.Patching.DebugEnabler.dll /tmp/asher-bootstrap/Mods/

cd /path/to/dust
ASHER_MODS_PATH=/tmp/asher-bootstrap/Mods \
ASHER_LOG_PATH=/tmp/asher-bootstrap/Logs \
LD_PRELOAD=/tmp/asher-bootstrap/libasher_bootstrap.so \
MONO_PATH=/tmp/asher-bootstrap \
./DustAET
```

(`/tmp` is tmpfs here and is cleared when WSL shuts down between commands; use a persistent
directory such as `~/asher-bootstrap` if you stage and run from separate shells.)

### Expected output

```text
[AsherPoC] Native bootstrap loaded
[AsherPoC] Mono symbols resolved
[AsherPoC] Root domain acquired
[AsherPoC] Thread attached
[AsherPoC] Asher.Runtime assembly loaded
[AsherPoC] Asher.Runtime entry point resolved
[Asher] Runtime initialized
[Asher] Bootstrap completed
[AsherPoC] Asher.Runtime bootstrap completed successfully
```

Details and per-module results are written to `<ASHER_LOG_PATH>/runtime_<timestamp>.log`:

```text
[PreInit] Módulo encontrado: Asher.Patching.DebugEnabler.DebugEnablerConfig
[DebugEnabler] Debug Enabler será habilitado
[PatchModuleLoader] Aplicando módulo: Debug Enabler
[DebugEnabler] Patched Microsoft.Xna.Framework.Game.Initialize
[DebugEnabler] Post-attach hook: Microsoft.Xna.Framework.Game.Tick
[DebugEnabler] canDebug enabled via post-attach Game.Tick
[PatchModuleLoader] 1 módulos de patch aplicados.
```

`canDebug enabled via post-attach Game.Tick` confirms the real DebugEnabler callback executed and
set `canDebug = true`. The game keeps running normally.

## Configuration

| Variable | Default | Purpose |
| --- | --- | --- |
| `ASHER_HOME` | `<game>/Asher` | Runtime home | 
| `ASHER_MODS_PATH` | `<ASHER_HOME>/Mods` | Directory scanned for patch modules |
| `ASHER_LOG_PATH` | `<ASHER_HOME>/Logs` | Runtime log directory |
| `ASHER_PROFILE` | `default` | Profile name in the RuntimeContext |
| `ASHER_BOOTSTRAP_ASSEMBLY` | `Asher.Runtime.dll` next to the `.so` | Managed entry assembly |
| `ASHER_BOOTSTRAP_AUTORUN` | `1` | `0` disables the automatic background bootstrap |
| `ASHER_BOOTSTRAP_TIMEOUT_MS` | `60000` | Max wait for runtime readiness (`0` = forever) |

## Findings (embedded Mono on Linux)

- Dust's Linux executable contains an FNA + Mono stack and exports the Mono embedding API
  (`mono_get_root_domain`, `mono_thread_attach`, `mono_assembly_open`, `mono_runtime_invoke`, ...).
- The native bootstrap resolves those symbols with `dlsym(RTLD_DEFAULT, ...)` and attaches a
  background thread; no second runtime, no game modification, no `ptrace`/`/proc` memory access.
- The attach is necessarily **late**: calling `mono_thread_attach` as soon as
  `mono_get_root_domain()` is non-NULL aborts (`object.c:1938 'klass' not met`). With the safe poll
  interval, FNA has already run its one-shot `Game.Initialize` (`Game.hasInitialized = True` on the
  first `Game.Tick`).
- Therefore `Dust.Game1.Initialize` is inherited from `Microsoft.Xna.Framework.Game` and has already
  executed. `Asher.Runtime.Bootstrap.HarmonyTargetResolver` and the patch modules resolve the
  **declared** method before patching (Harmony rejects inherited `MethodInfo`s), and
  `DebugEnablerPatch` adds a post-attach hook on `Game.Tick` so the callback still runs.
- `Harmony.GetPatchInfo` alone proves installation, not execution; the runtime log lines above are
  the execution evidence.
- `Dust.Storage.Store::DUST_STORAGE_INTERNAL_SAVE` ("mono runtime and class libraries are out of
  sync") is Dust's own Steam-storage native internal call and is unrelated to Asher/Harmony.

## Remaining experimental / known limits

- The native bootstrap's `mono_thread_attach` timing is racy on some launches; a rare run aborts
  with the `object.c:1938` assertion before any managed code. A small settle delay after root-domain
  detection would make it deterministic.
- `GameTitleBootstrap` is not invoked by the Linux entry point (it is only needed by the Windows
  launcher flow).
- The debug menu itself (Tab) has not been exercised non-interactively; only the state the game
  uses (`canDebug = true`) is verified.
