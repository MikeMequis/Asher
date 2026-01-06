# ⚙️ IMPLEMENTATION NOTES — Asher Modding Platform

This document describes the **technical rationale** and **low-level design decisions** behind Asher’s launcher-based architecture.

---

## ❌ Why Early Injection Was Abandoned

Initial attempts used early DLL injection directly into the game process.

This approach failed due to:

- Unreliable CLR initialization timing
- SteamWrapper interference
- Silent failures during CLR hosting
- Managed DLLs not executing static constructors when loaded via `LoadLibrary`

Result: difficult debugging and inconsistent behavior.

---

## ✅ Launcher-Based Design Rationale

Asher adopts a **launcher-first model**, similar to SMAPI, to gain:

- Full control over process startup
- Predictable runtime state before injection
- Reliable logging and diagnostics
- Clear separation of responsibilities

The launcher is the **single authoritative entry point**.

---

## 🔌 Injection Strategy

- Injection is performed **after** the game process reaches a stable state.
- The launcher decides *when* injection is safe.
- Bootstrap execution is explicit — no reliance on static constructors.

This avoids loader lock issues and CLR deadlocks.

---

## 🧱 Bootstrap Responsibilities

`Asher.Bootstrap` runs **inside the game process** and:

- Initializes logging
- Loads `Asher.Runtime`
- Transfers execution control to managed runtime logic

Bootstrap contains **no patching logic**.

---

## 🧠 Runtime Responsibilities

`Asher.Runtime` is responsible for:

- Discovering enabled patches
- Loading patch assemblies dynamically
- Applying Harmony patches
- Intercepting content loading

This separation keeps bootstrap minimal and robust.

---

## 🧪 Debugging Strategy

- All critical stages log to `/AsherLogs/`
- Each layer logs independently:
  - injection.log
  - bootstrap.log
  - runtime.log

This allows pinpointing failures without guesswork.

---

## 🧭 Future Extensions

- Late injection fallback
- Safe reload support
- Conditional content patching
- Advanced diagnostics mode

---

## 🧩 Design Philosophy

> Prefer explicit control over clever hacks.

Stability and maintainability always outweigh early or implicit injection tricks.
