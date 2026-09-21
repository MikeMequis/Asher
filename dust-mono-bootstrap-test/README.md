# Dust Mono Bootstrap - Phase 9 (real DebugEnabler callback executes on Dust/Linux)

This directory contains the native Linux bootstrap that attaches to the Mono runtime embedded in
**DustAET**, initializes the **real Asher managed runtime**, introspects the managed assemblies Dust
already loaded, and optionally loads an isolated Harmony layer that applies the **real, unmodified
Asher DebugEnabler patch** to the live game.

Phase 1 proved the mechanism against an artificial `BootstrapTest.dll`. Phase 2 replaced the
artificial assembly with `Asher.Runtime.dll`. Phase 3 added reflection-only visibility into Dust's
managed code. Phase 4 applied a DebugEnabler-shaped Harmony patch from `Asher.Runtime`. Phase 5
moved Harmony out of `Asher.Runtime` into a separate `Asher.HarmonyPoc.dll` loaded on demand and
proved that 0Harmony 2.4.2 loads in the real Dust Linux process. Phase 6 fixed target resolution
(`Dust.Game1.Initialize` is inherited from `Microsoft.Xna.Framework.Game`; Harmony rejects inherited
`MethodInfo`s). Phase 7 reused the actual `Asher.Patching.DebugEnabler` patch class. Phase 8 removes
the modifications that had been made to that file, moves the Linux-specific adaptation entirely
into the PoC adapter, and adds diagnostics that distinguish patch installation from patch execution.

```text
libasher_bootstrap.so
    -> existing Dust Mono runtime
    -> mono_get_root_domain()
    -> mono_thread_attach()
    -> Asher.Runtime.dll                         (NO 0Harmony dependency)
    -> RuntimeBootstrap.Initialize()
    -> RuntimeEntry.Init(default RuntimeContext) -> "[Asher] Runtime initialized"
    -> DustAssemblyProbe                          -> Dust assembly + type identified
    -> (ASHER_HARMONY_POC=1) Assembly.LoadFrom(Asher.HarmonyPoc.dll)
    -> Asher.HarmonyPoc.HarmonyPocBootstrap.Initialize()
    -> new Harmony("Asher.Linux.DebugEnabler")    -> declared target patched
    -> real DebugEnabler callback runs on Tick    -> "[Asher] DebugEnabler real postfix executed; canDebug = True"
```

Scope: a single-patch, dependency-isolation experiment. No mod loader, no plugin discovery, no Dust
method invoked or replaced, no gameplay change, no second Mono runtime, no hardcoded addresses, no
`ptrace`/`/proc` memory access.

## Milestone status

```text
[x] Embedded Mono identified
[x] Mono embedding API exported by DustAET
[x] Native bootstrap loaded into Dust
[x] Existing Mono root domain accessed
[x] Native thread attached to Mono
[x] External managed assembly loaded
[x] External managed method invoked
[x] Asher.Runtime loaded
[x] Asher.Runtime initialized
[x] Dust assemblies accessible from Asher.Runtime
[x] Harmony compatibility verified (real DebugEnabler callback executes on Dust/Linux)
[x] DebugEnabler state applied (canDebug = True; post-attach hook, Game.Initialize was already past)
[ ] Tab opens the debug menu (not exercised non-interactively)
```

## Layout

```text
dust-mono-bootstrap-test/
├── native/bootstrap.c          native bootstrap library (unchanged mechanism)
├── managed/BootstrapTest.cs    phase-1 reference assembly
├── prebuilt/                   AnyCPU managed assemblies staged by build.sh
│   ├── Asher.Runtime.dll       real runtime, Harmony-free
│   ├── Asher.SDK.dll
│   ├── Asher.HarmonyPoc.dll    isolated Harmony layer (0Harmony reference)
│   └── Asher.Patching.DebugEnabler.dll  real DebugEnabler patch
├── build.sh                    native build + managed staging
├── build-managed.sh            regenerate prebuilt/ from the repo sources
└── README.md
```

Source locations:

- `Asher.Runtime/Diagnostics/DustAssemblyProbe.cs` - Dust assembly/type introspection
- `Asher.Runtime/RuntimeBootstrap.cs` - runtime entry point + reflective PoC loader
- `Asher.HarmonyPoc/` - isolated Harmony layer (`HarmonyPocBootstrap`, `DebugEnablerHarmonyPatch`)
- `Patches/Asher.Patching.DebugEnabler/` - the real DebugEnabler patch that is reused

## Asher.Runtime entry point

`Asher.Runtime/RuntimeBootstrap.cs` was added to the runtime project:

```csharp
namespace Asher.Runtime
{
    public static class RuntimeBootstrap
    {
        public static void Initialize()
        {
            // builds a default RuntimeContext from the game base directory / ASHER_* env vars
            // and calls the existing RuntimeEntry.Init(context)
            Console.WriteLine("[Asher] Runtime initialized");
        }
    }
}
```

It is the smallest boundary between native code and the runtime: it reuses the existing
`RuntimeEntry.Init` / `RuntimeController.Init` initialization path and adds no new architecture.

After initialization it calls `DustAssemblyProbe.Run()` (unless `ASHER_INTROSPECT=0`). The probe:

1. enumerates `AppDomain.CurrentDomain.GetAssemblies()` and prints each full name;
2. identifies the Dust game assembly at runtime (non-framework assembly whose types are in a
   namespace under `Dust` and/or derive from a type named `Game`) - no assembly or type name is
   hardcoded;
3. gets its types (reporting each `ReflectionTypeLoadException` loader exception individually);
4. resolves one real type through `Assembly.GetType` and lists a few of its methods.

It never invokes game methods and never mutates state.

## Harmony isolation (Asher.HarmonyPoc)

### Why

The Phase-4 `Asher.Runtime.dll` had a direct `AssemblyRef` to `0Harmony` because the Harmony patch
lived in the runtime assembly. To determine whether that direct reference (or the Harmony load
itself) causes the Linux crash, Harmony was moved into a separate assembly.

### Reused implementation

The real patch lives at `Patches/Asher.Patching.DebugEnabler/` and is used **unchanged**:

- `DebugEnablerPatch` targets `Dust.Game1.Initialize` and installs a **postfix** `EnableDebugMenu`.
- The postfix sets the game's static `canDebug` field to `true`. The game itself opens the debug
  menu with Tab (in the pause menu) once `canDebug` is set; Asher does not handle input.
- `DebugEnablerConfig` (a PreInit module) normally sets `DebugEnablerPatch.Enabled = true`.

### Linux adaptation (PoC boundary only)

The real `DebugEnablerPatch.Apply(Harmony)` cannot run unchanged on this runtime: it resolves the
target with `game1Type.GetMethod("Initialize", ...)`, which returns the inherited
`Microsoft.Xna.Framework.Game.Initialize`; Harmony refuses to patch inherited `MethodInfo`s. The
real patch file is therefore **not modified**.

The Linux-specific adaptation lives entirely in `Asher.HarmonyPoc/DebugEnablerHarmonyPatch.cs`:

1. resolve the declared target (`ReflectedType != DeclaringType` -> re-resolve with
   `BindingFlags.DeclaredOnly`);
2. install the real patch's own postfix callback
   (`typeof(DebugEnablerPatch).GetMethod("EnableDebugMenu", NonPublic|Static)`) through Harmony; and
3. also install that callback on `Game.Tick`, because the late LD_PRELOAD attach happens after
   FNA's one-shot `Game.Initialize` (see "Root cause" below).

The adapter does not reimplement DebugEnabler; it wires the existing callback. It also adds
diagnostics around it (see "Execution vs installation" below).

### Dependency boundary

```text
Asher.Runtime.dll                -> mscorlib, Asher.SDK, System.Core   (NO 0Harmony)
Asher.HarmonyPoc.dll             -> mscorlib, 0Harmony, Asher.Runtime, Asher.SDK, Asher.Patching.DebugEnabler
Asher.Patching.DebugEnabler.dll  -> mscorlib, Asher.SDK, 0Harmony, System.Core
```

`Asher.Runtime.RuntimeBootstrap` loads the PoC assembly by reflection (`Assembly.LoadFrom`) only
when `ASHER_HARMONY_POC=1`, using the path in `ASHER_HARMONY_POC_ASSEMBLY`. It then invokes
`Asher.HarmonyPoc.HarmonyPocBootstrap.Initialize()`. `Asher.Runtime` has no compile-time reference
to `Asher.HarmonyPoc`, `Asher.Patching.DebugEnabler` or `0Harmony`.

`Asher.Runtime` is built in its Harmony-free variant for Linux: `build-managed.sh` excludes the
Windows-only Harmony bootstrap subsystem (`GameTitleBootstrap`, `HarmonyLifecycleBootstrap`,
`PatchModuleLoader`) from the source list, so the resulting assembly has no `0Harmony` AssemblyRef.
The Windows `Asher.Runtime.csproj` is unchanged and still builds the full Harmony-based runtime.

### Verify the boundary

```bash
monodis --assemblyref out/Asher.Runtime.dll     # must NOT list 0Harmony
monodis --assemblyref out/Asher.HarmonyPoc.dll  # must list 0Harmony
```

### Execution vs installation

`Harmony.GetPatchInfo(...)` reporting a postfix only proves installation. The adapter adds
diagnostics that distinguish every stage:

| Evidence | Meaning |
| --- | --- |
| `[Asher] DebugEnabler patch applied successfully (prefixes=.., postfixes=..)` | patch installed |
| `[Asher] DebugEnabler target method reached` | patched method actually executed (adapter prefix) |
| `[Asher] DebugEnabler postfix reached; canDebug = True` | real postfix executed and set the field |
| `[Asher] DebugEnabler postfix reached; 'canDebug' field not found` | postfix ran, field lookup failed |
| no `target method reached` line | patched method never ran after install (timing/dispatch) |

The real postfix `EnableDebugMenu` is registered directly with Harmony; the adapter's prefix and
postfix only observe. Installation alone is never reported as success.

### Root cause (confirmed on the real Dust process)

The native bootstrap is an LD_PRELOAD late attach. It cannot safely attach before the embedded Mono
runtime finishes initialising: polling `mono_get_root_domain()` at 1 ms and attaching immediately
aborts inside `mono_thread_attach` (`Assertion at object.c:1938, condition 'klass' not met`). With
the safe (50 ms) poll, attach happens after FNA has already run its one-shot `Game.Initialize`.

The real-game diagnostics show exactly that:

```text
[Asher] DebugEnabler patch applied successfully (prefixes=1, postfixes=2)   <- Initialize patched
(no "[Asher] DebugEnabler target method reached")                          <- but never executes
[Asher] DebugEnabler post-attach reached (Game.Tick); Game.hasInitialized = True
```

`Game.hasInitialized = True` at the first `Game.Tick` after attach proves `Initialize` already ran.
A control patch on the non-virtual `Game.Tick` fired 1200+ times, proving Harmony callbacks do
execute on this Mono/FNA runtime - the target method was simply already past. Conclusion: the
target `Game.Initialize` is correct but unusable for a late attach (timing).

### Fix

The real DebugEnabler callback is kept, and additionally installed on `Game.Tick` (FNA's
non-virtual per-frame method). The callback only sets the static `canDebug` flag, which is
idempotent, so running it from Tick is equivalent to running it during Initialize and happens long
before the player can open the pause menu. `Patches/Asher.Patching.DebugEnabler/DebugEnablerPatch.cs`
is not modified. The late hook lives entirely in `Asher.HarmonyPoc/DebugEnablerHarmonyPatch.cs`.

### Confirmed result (real Dust, Linux/FNA)

```text
[Asher] DebugEnabler patch applied successfully (prefixes=1, postfixes=2)
[Asher] Post-attach target: Microsoft.Xna.Framework.Game.Tick
[Asher] DebugEnabler post-attach reached (Game.Tick); Game.hasInitialized = True
[Asher] Post-attach DebugEnabler patch applied successfully (prefixes=1, postfixes=2)
[Asher] DebugEnabler real postfix executed; canDebug = True
```

The game kept running for the full test window. `Game.Initialize` is never reached (it already ran),
which is expected.

### Verification status

- Dependency boundary verified (equivalent to `monodis --assemblyref`):
  `Asher.Runtime.dll` -> `mscorlib, Asher.SDK, System.Core`;
  `Asher.HarmonyPoc.dll` -> `mscorlib, 0Harmony, Asher.Runtime, Asher.SDK, Asher.Patching.DebugEnabler`.
- Real Dust run (above): patch installed, post-attach callback executed, `canDebug = True`.
- The `Dust.Storage.Store::DUST_STORAGE_INTERNAL_SAVE` ("mono runtime and class libraries are out of
  sync") message appears in baseline and patched runs alike and is unrelated to Harmony; it is
  Dust's own Steam-storage native internal call, not part of the patch path.

## Requirements

- A C compiler (`gcc`) and the Linux loader tooling.
- Run inside the Linux VM that has Dust installed.
- The managed assemblies are shipped prebuilt, so **no C# compiler is required on the VM**.

> Match the architecture of DustAET for the native library. If the game is 32-bit, build the native
> library with `EXTRA_CFLAGS=-m32`. The managed DLLs are AnyCPU IL with the `Requires32Bit` flag
> cleared, so they load in either a 32-bit or 64-bit Mono process.

## Build

```bash
cd dust-mono-bootstrap-test
chmod +x build.sh
./build.sh
```

Output in `out/`:

```text
libasher_bootstrap.so
Asher.Runtime.dll                (Harmony-free)
Asher.SDK.dll
Asher.HarmonyPoc.dll             (isolated Harmony layer)
Asher.Patching.DebugEnabler.dll  (real DebugEnabler patch)
0Harmony.dll
BootstrapTest.dll                (phase-1 reference, only if mcs/csc is present)
```

### Regenerating the managed assemblies

`prebuilt/` is generated from the real repository sources (`Asher.SDK/`, the Harmony-free
`Asher.Runtime/`, `Patches/Asher.Patching.DebugEnabler/` and `Asher.HarmonyPoc/`) with:

```bash
./build-managed.sh              # needs Mono csc with C# 9+ support
```

`Asher.Runtime` uses C# 9 features (nullable annotations on unconstrained generic type parameters),
so Mono 6.12+ (Roslyn 3.9) is required. On Windows the assemblies can be produced with the .NET SDK
Roslyn compiler against .NET Framework 4.7.2 reference assemblies and the repository's
`packages/Lib.Harmony.2.4.2/lib/net472/0Harmony.dll`; copy the resulting AnyCPU DLLs into
`prebuilt/`.

## Test

Launch Dust from a terminal with the bootstrap preloaded. The known test location is:

```
/home/marselo/.local/share/Steam/steamapps/common/Dust An Elysian Tail/DustAET
```

Stage the artifacts under a path without spaces:

```bash
rm -rf /tmp/asher-bootstrap && mkdir -p /tmp/asher-bootstrap
cp out/*.dll out/libasher_bootstrap.so /tmp/asher-bootstrap/
```

The bootstrap finds `Asher.Runtime.dll` next to the `.so` automatically (via `dladdr`), and
`MONO_PATH` guarantees `Asher.SDK.dll` / `Asher.HarmonyPoc.dll` /
`Asher.Patching.DebugEnabler.dll` / `0Harmony.dll` resolution.

### Test 1 - Baseline (Harmony disabled)

```bash
ASHER_HARMONY_POC=0 \
LD_PRELOAD=/tmp/asher-bootstrap/libasher_bootstrap.so \
MONO_PATH=/tmp/asher-bootstrap \
"/home/marselo/.local/share/Steam/steamapps/common/Dust An Elysian Tail/DustAET"
```

Dust must run normally and the output must end at the introspection stage, with no Harmony lines.

### Test 2 - real DebugEnabler enabled

```bash
ASHER_HARMONY_POC=1 \
ASHER_HARMONY_POC_ASSEMBLY=/tmp/asher-bootstrap/Asher.HarmonyPoc.dll \
LD_PRELOAD=/tmp/asher-bootstrap/libasher_bootstrap.so \
MONO_PATH=/tmp/asher-bootstrap \
"/home/marselo/.local/share/Steam/steamapps/common/Dust An Elysian Tail/DustAET"
```

### Expected output

```text
[AsherPoC] Native bootstrap loaded
[AsherPoC] Mono symbols resolved
[AsherPoC] Root domain acquired
[AsherPoC] Thread attached
[AsherPoC] Asher.Runtime assembly loaded
[AsherPoC] Asher.Runtime entry point resolved
[Asher] Runtime initialized
[Asher] Loaded assemblies: N
[Asher] Assembly: <assembly full name>      (one line per loaded assembly)
[Asher] Dust assembly found: <name>
[Asher] Dust assembly types: N
[Asher] Dust type resolved: <actual namespace.type>
[Asher] Method found: <method name>         (up to 5 declared methods)
[Asher] Dust assembly introspection completed successfully
[Asher] Harmony PoC assembly loaded
[Asher] Initializing Harmony
[Asher] Harmony instance created
[Asher] DebugEnabler target resolution started
[Asher] Target type: Dust.Game1
[Asher] Game1 declares Initialize: False
[Asher] Target method: Initialize
[Asher] Target declaring type: Microsoft.Xna.Framework.Game
[Asher] Target base definition: Microsoft.Xna.Framework.Game.Initialize
[Asher] Target method virtual: True, body present: True
[Asher] Applying existing DebugEnabler postfix
[Asher] DebugEnabler patch applied successfully (prefixes=1, postfixes=2)
[Asher] Post-attach target: Microsoft.Xna.Framework.Game.Tick
[Asher] Post-attach DebugEnabler patch applied successfully (prefixes=1, postfixes=2)
[Asher] Harmony PoC completed successfully
[AsherPoC] Asher.Runtime bootstrap completed successfully
```

This sequence is Test 2. Test 1 (`ASHER_HARMONY_POC=0`) stops after
`[Asher] Dust assembly introspection completed successfully` and loads nothing Harmony-related.

Then, when Dust runs the next frame, the real DebugEnabler callback executes from the post-attach
hook:

```text
[Asher] DebugEnabler post-attach reached (Game.Tick); Game.hasInitialized = True
[Asher] DebugEnabler real postfix executed; canDebug = True
```

`patch applied successfully` alone is not the proof; the `real postfix executed; canDebug = True`
line confirms live execution and the state change the game uses to expose the debug menu. The
`Game.hasInitialized = True` line documents why the original `Game.Initialize` target cannot fire.

### If Tab does not open the debug menu

The patch can install successfully yet not produce the debug menu. Determine the exact stage:

```text
Patch not installed                  -> no "DebugEnabler patch applied successfully"
Post-attach never runs               -> no "DebugEnabler post-attach reached"
Postfix runs, canDebug not found     -> "real postfix executed; 'canDebug' field not found"
canDebug set, Tab still does nothing -> the game's own input/menu path differs on Linux
```

The original `Game.Initialize` prefix is expected to stay silent (it already ran before attach); that
is not a failure. Do not rewrite the real patch and do not change the native bootstrap; report the
stage.

The actual assembly and type names are whatever Dust exposes at runtime; the test records them
rather than assuming them.

The runtime also creates `<game>/Asher/Logs/runtime_<timestamp>.log`. The game must still launch and
run normally.

## Configuration

The bootstrap resolves the Mono API at runtime with `dlsym(RTLD_DEFAULT, ...)`; it never hardcodes
addresses and never opens a Mono runtime of its own.

| Variable | Default | Purpose |
| --- | --- | --- |
| `ASHER_BOOTSTRAP_ASSEMBLY` | `Asher.Runtime.dll` next to the `.so`, then cwd | Managed assembly to load |
| `ASHER_BOOTSTRAP_NAMESPACE` | `Asher.Runtime` | Entry point namespace |
| `ASHER_BOOTSTRAP_CLASS` | `RuntimeBootstrap` | Entry point class |
| `ASHER_BOOTSTRAP_METHOD` | `Initialize` | Static, zero-parameter method |
| `ASHER_BOOTSTRAP_AUTORUN` | `1` | `0` disables the automatic background bootstrap |
| `ASHER_BOOTSTRAP_TIMEOUT_MS` | `60000` | Max wait for runtime readiness (`0` = forever) |
| `ASHER_HOME` | `<game>/Asher` | Runtime home (Mods/Logs/config) |
| `ASHER_MODS_PATH` | `<ASHER_HOME>/Mods` | Mods directory |
| `ASHER_LOG_PATH` | `<ASHER_HOME>/Logs` | Log directory |
| `ASHER_PROFILE` | `default` | Profile name stored in the RuntimeContext |
| `ASHER_INTROSPECT` | `1` | `0` skips the Dust assembly introspection step |
| `ASHER_HARMONY_POC` | `0` | `1` loads and runs the isolated `Asher.HarmonyPoc` assembly |
| `ASHER_HARMONY_POC_ASSEMBLY` | (none) | Path to `Asher.HarmonyPoc.dll`; required when the PoC is enabled |


Running the phase-1 reference assembly instead:

```bash
LD_PRELOAD=/tmp/asher-bootstrap/libasher_bootstrap.so \
ASHER_BOOTSTRAP_ASSEMBLY=/tmp/asher-bootstrap/BootstrapTest.dll \
"/home/marselo/.local/share/Steam/steamapps/common/Dust An Elysian Tail/DustAET"
```

## Diagnostics

Each stage is reported explicitly. Failures print `[AsherPoC] FAILED: ...` naming the exact stage:

| Stage | Failure message |
| --- | --- |
| Symbol resolution | `Mono symbol resolution timed out waiting for mono_get_root_domain and friends` |
| Root domain | `Root domain acquisition timed out (mono_get_root_domain returned NULL)` |
| Thread attach | `Thread attachment failed (mono_thread_attach returned NULL)` |
| Assembly load | `Asher.Runtime assembly loading failed (mono_assembly_open '<path>', status=N)` |
| Class | `Entry point class resolution failed (Asher.Runtime.RuntimeBootstrap)` |
| Method | `Asher.Runtime entry point method resolution failed (Asher.Runtime.RuntimeBootstrap.Initialize)` |
| Invoke | `Method invocation threw a managed exception:` followed by `Exception.ToString()` (type, message, stack trace) |
| Assembly scan | `[Asher] Dust assembly not identified` + `Dust assembly could not be identified among the loaded assemblies` |
| Type load | `[Asher] GetTypes threw ReflectionTypeLoadException in <name>` + one `Loader exception[i]` line each, then `[Asher] GetTypes failed in <name>` |
| Type resolve | `[Asher] Dust assembly '<name>' exposes no loadable types` or `Type '<full name>' could not be resolved via Assembly.GetType` |
| PoC path | `[Asher] Harmony PoC assembly path not configured` (no crash, PoC skipped) |
| PoC load | `[Asher] Harmony PoC assembly load failed` + exception detail |
| PoC entry | `[Asher] Harmony PoC entry point type not found` / `... method not found` / `... resolution failed` |
| PoC invoke | `[Asher] Harmony PoC initialization failed` + exception detail |
| Harmony init | `[Asher] Harmony initialization failed` + exception detail |
| Target resolve | `[Asher] DebugEnabler target type not found: Dust.Game1` / `[Asher] DebugEnabler target method not found: Dust.Game1.Initialize` |
| Target diagnostics | `[Asher] DebugEnabler target resolution started`, `[Asher] Target type`, `[Asher] Game1 declares Initialize`, `[Asher] Target method`, `[Asher] Target declaring type`, `[Asher] Target base definition`, `[Asher] Target method virtual` |
| Patch apply | `[Asher] DebugEnabler patch application failed` + exception detail; or `... patch application failed: postfix not installed` |
| Method run | `[Asher] DebugEnabler target method reached` (original Game.Initialize prefix; normally silent because it already ran) |
| Post-attach | `[Asher] DebugEnabler post-attach reached (Game.Tick); Game.hasInitialized = <bool>` |
| Postfix run | `[Asher] DebugEnabler real postfix executed; canDebug = <value>` (real callback executed) |
| Postfix field | `[Asher] DebugEnabler real postfix executed; 'canDebug' field not found` |

Common causes:

- **Symbols never resolve** - the executable does not export the Mono embedding API, or the library
  was loaded into the wrong process.
- **Assembly loading failed** - wrong `ASHER_BOOTSTRAP_ASSEMBLY`; pass an absolute path.
- **Entry point / class not found** - `prebuilt/Asher.Runtime.dll` is stale; run
  `./build-managed.sh`.
- **Managed exception** - the `Exception.ToString()` payload (including stack trace) is printed;
  `RuntimeLogger` also writes a detailed `[FATAL]` line to stderr and the runtime log.
- **Dust assembly not identified** - the full assembly list is printed; the game assembly may be
  named or shaped unexpectedly, or it may be loaded outside the root AppDomain. Report the printed
  names instead of guessing.

## Compatibility notes

- The managed assemblies were produced from the real `Asher.Runtime`/`Asher.SDK` sources, compiled
  for .NET Framework 4.7.2, `AnyCPU` (`CorFlags = ILOnly`).
- `Asher.Runtime` no longer references `0Harmony`; only `Asher.HarmonyPoc` does. `0Harmony.dll`
  (2.4.2, net472) is staged by `build.sh` and resolved via `MONO_PATH`. No other Harmony version is
  introduced.
- New source: `Asher.Runtime/RuntimeBootstrap.cs`, `Asher.Runtime/Diagnostics/DustAssemblyProbe.cs`
  and the `Asher.HarmonyPoc` project (`HarmonyPocBootstrap`, `DebugEnablerHarmonyPatch`). The
  existing `RuntimeEntry.Init(RuntimeContext)` API is reused unchanged.
- The probe uses only reflection (`AppDomain`, `Assembly`, `Type`, `MethodInfo`). The Harmony PoC
  patches but does not invoke Dust methods; the original `Game1.Initialize` body is preserved.
- `Asher.SDK` still references `0Harmony` (unchanged); the initialization path
  (`RuntimeBootstrap` -> `RuntimeEntry` -> `RuntimeController` -> `RuntimeLogger` -> `DustAssemblyProbe`)
  never touches Harmony types, so `0Harmony` is not loaded in the baseline.
