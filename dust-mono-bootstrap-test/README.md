# Dust Mono Bootstrap PoC

Minimal proof that native code loaded into the running **DustAET** (Linux) process can reach the
**existing, already-initialized Mono runtime**, load an external managed assembly and invoke a
method on it.

```text
native bootstrap (.so)
    -> existing Dust Mono runtime
    -> mono_get_root_domain()
    -> mono_thread_attach()
    -> mono_assembly_open()
    -> mono_class_from_name() / mono_class_get_method_from_name()
    -> mono_runtime_invoke()
    -> "ASHER BOOTSTRAP OK"
```

Scope: this is a bootstrap PoC only. No Harmony, no Asher runtime, no game modification,
no second Mono runtime, no hardcoded addresses, no `ptrace`/`/proc` memory access.

## Layout

```text
dust-mono-bootstrap-test/
├── native/bootstrap.c        native bootstrap library
├── managed/BootstrapTest.cs  managed test assembly
├── build.sh                  build script
└── README.md
```

## Requirements

- A C compiler (`gcc`) and the Linux loader tooling.
- A Mono C# compiler on `PATH`: `mcs` or `csc` (package `mono-devel` / `mono-complete`).
- Run the test **inside the Linux VM that has Dust installed**.

> Match the architecture of DustAET. If the game is 32-bit, build the native library as 32-bit
> too (install the multilib toolchain and pass `EXTRA_CFLAGS=-m32`). A bitness mismatch makes the
> loader reject the `LD_PRELOAD` library. The managed DLL is IL-only, so it needs no specific
> architecture.

## Build

```bash
cd dust-mono-bootstrap-test
chmod +x build.sh
./build.sh
```

Output:

```text
out/libasher_bootstrap.so
out/BootstrapTest.dll
```

Both files are placed side by side so the default DLL path works without configuration.
For a 32-bit game:

```bash
EXTRA_CFLAGS=-m32 ./build.sh
```

## Run

Launch Dust from a terminal so stdout/stderr are visible, preloading the bootstrap:

```bash
cd dust-mono-bootstrap-test
ASHER_BOOTSTRAP_TIMEOUT_MS=60000 \
LD_PRELOAD="$PWD/out/libasher_bootstrap.so" \
./DustAET
```

`./DustAET` is the game executable from your Dust installation (run it from, or point the command
at, your installation's directory). The bootstrap does not modify the game in any way.

A background thread waits until Mono is initialized (root domain available), then performs the
bootstrap automatically. If the runtime is never reached it reports the stage that stalled.

### Expected output

```text
[AsherPoC] Native bootstrap loaded
[AsherPoC] Mono symbols resolved
[AsherPoC] Root domain acquired
[AsherPoC] Thread attached
[AsherPoC] Assembly loaded
[AsherPoC] Method resolved
ASHER BOOTSTRAP OK
[AsherPoC] Bootstrap completed successfully
```

If `ASHER BOOTSTRAP OK` appears, the PoC succeeded.

## Configuration

The bootstrap resolves the Mono API at runtime with `dlsym(RTLD_DEFAULT, ...)`; it never
hardcodes addresses and never opens a Mono runtime of its own.

| Variable | Default | Purpose |
| --- | --- | --- |
| `ASHER_BOOTSTRAP_ASSEMBLY` | `BootstrapTest.dll` next to the `.so`, then cwd | Managed assembly to load |
| `ASHER_BOOTSTRAP_NAMESPACE` | `AsherBootstrapTest` | Managed namespace |
| `ASHER_BOOTSTRAP_CLASS` | `Bootstrap` | Managed class |
| `ASHER_BOOTSTRAP_METHOD` | `Initialize` | Static, zero-parameter method |
| `ASHER_BOOTSTRAP_AUTORUN` | `1` | `0` disables the automatic background bootstrap |
| `ASHER_BOOTSTRAP_TIMEOUT_MS` | `60000` | Max wait for runtime readiness (`0` = forever) |

## Diagnostics

Each stage is reported explicitly. Failures print `[AsherPoC] FAILED: ...` naming the exact stage:

| Stage | Failure message |
| --- | --- |
| Symbol resolution | `Mono symbol resolution timed out waiting for mono_get_root_domain and friends` |
| Root domain | `Root domain acquisition timed out (mono_get_root_domain returned NULL)` |
| Thread attach | `Thread attachment failed (mono_thread_attach returned NULL)` |
| Assembly load | `Assembly loading failed (mono_assembly_open '<path>', status=N)` |
| Class | `Class resolution failed (AsherBootstrapTest.Bootstrap)` |
| Method | `Method resolution failed (AsherBootstrapTest.Bootstrap.Initialize)` |
| Invoke | `Method invocation threw a managed exception` |

Common causes:

- **Mono symbols never resolve** — the executable does not export the Mono embedding API into the
  global dynamic symbol table, or the library was loaded into the wrong process.
- **Assembly loading failed** — wrong `ASHER_BOOTSTRAP_ASSEMBLY` path; pass an absolute path.
- **No `ASHER BOOTSTRAP OK` but no failure** — stdout is redirected; run the game from a terminal
  or redirect output to a file.

## Notes

- The library attaches the bootstrap thread to Mono before calling into managed code and detaches
  it when finished.
- Only static entry points are supported (`obj = NULL`), which is what the PoC needs.
- An exported `asher_bootstrap_run()` is available if a future host prefers to trigger the
  bootstrap explicitly instead of using autorun.
