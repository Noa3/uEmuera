# uEmuera ERA Platform Architecture

> Phase 8 · 2026-08-12

---

## Vision

uEmuera is a cross-platform ERA game launcher and runtime platform.

One launcher → multiple isolated runtimes → any ERA game family.

```
uEmuera Launcher
  │
  ├── Game Library (multi-runtime)
  │     ├── Emuera game cards  [EM+EE]
  │     └── EraElectron game cards  [EraUma, ereKanon, ...]
  │
  ├── Shared Services
  │     ├── GameDetector (RuntimeKind dispatch)
  │     ├── GameFileManifest (one FS walk per session)
  │     ├── CatalogCacheStore (per-game keyed cache)
  │     ├── StartupProfiler
  │     ├── GameSession (stale-callback guard)
  │     └── IRuntimeLogger / IPermissionService
  │
  ├── EmueraRuntimeAdapter  ─→  EmueraMain / EmueraThread / Process
  │       (existing EM+EE interpreter, untouched)
  │
  └── EraElectronRuntime    ─→  IEraElectronHost
          (JavaScript / Vue / Element Plus / Chart.js)
```

---

## Core rules (permanent)

### Emuera rules
- Parser/runtime semantic state on one interpreter thread only.
- Function existence never depends on filename.
- Fast boot exposes no half-initialized functions.
- Image render order never depends on async completion order.

### EraElectron rules
- JavaScript/web behavior belongs to EraElectronRuntime.
- Do not translate web UI into EmueraConsole.
- Do not expose unrestricted Node host access.
- Do not claim API compatibility without a reference test.
- Do not hardcode EraUma behavior into generic engine code.

### Shared rules
- One launcher. Multiple isolated runtimes.
- One game session at a time.
- Game files are untrusted code/data.
- Runtime-specific saves are isolated.
- Old async work cannot mutate a new session.
- Correctness before optimization.
- Measured compatibility before documentation claims.

---

## Component inventory

### Milestone 0 baseline (existing)

| Component | Location | Status |
|---|---|---|
| `FunctionCatalog` | `GameProc/FunctionCatalog.cs` | WORKING |
| `FunctionResolver` | `GameProc/FunctionResolver.cs` | WORKING |
| `OnDemandErbCompiler` | `GameProc/OnDemandErbCompiler.cs` | WORKING |
| `CatalogCacheStore` | `GameProc/CatalogCacheStore.cs` | WORKING |
| `GameSession` | `GameProc/GameSession.cs` | WORKING |
| `BootStrategy` / `BootConfig` | `GameProc/BootStrategy.cs` | WORKING |
| `StartupProfiler` | `GameProc/StartupProfiler.cs` | WORKING |
| `GameResourceCatalog` | `uEmuera/GameResourceCatalog.cs` | WORKING |
| `EmueraMain` + `EmueraThread` | `EmueraMain.cs`, `EmueraThread.cs` | WORKING |
| `FirstWindow` (game picker) | `FirstWindow.cs` | WORKING (Emuera-only) |

### Milestone 1 (runtime abstraction)

| Component | Location | Status |
|---|---|---|
| `RuntimeKind` | `Runtime/RuntimeKind.cs` | DONE |
| `RuntimeState` | `Runtime/RuntimeState.cs` | DONE |
| `GameDescriptor` | `Runtime/GameDescriptor.cs` | DONE |
| `IGameRuntime` + `RuntimeDiagnostics` | `Runtime/IGameRuntime.cs` | DONE |
| `RuntimeContext` + service interfaces | `Runtime/RuntimeContext.cs` | DONE |
| `EmueraRuntimeAdapter` | `Runtime/EmueraRuntimeAdapter.cs` | DONE |
| `IGameDetector` | `Runtime/Detection/IGameDetector.cs` | DONE |
| `EmueraGameDetector` | `Runtime/Detection/EmueraGameDetector.cs` | DONE |
| `EraElectronGameDetector` | `Runtime/Detection/EraElectronGameDetector.cs` | DONE (provisional) |
| `GameDetector` | `Runtime/Detection/GameDetector.cs` | DONE |

### Milestone 3-4 (EraElectron architecture)

| Component | Location | Status |
|---|---|---|
| `EraElectronHostMode` | `Runtime/EraElectronHostMode.cs` | DONE |
| `IEraElectronHost` + `HostCapabilities` + `IEraNativeBridge` | `Runtime/IEraElectronHost.cs` | DONE |
| `EraElectronRuntime` | `Runtime/EraElectronRuntime.cs` | STUB |

### Pending

| Component | Milestone | Notes |
|---|---|---|
| `GameRuntimeManager` | M4 | Lifecycle coordinator for both runtimes |
| `EreDataModel` | M4/M5 | ERA data tables for EraElectron |
| `EreApiDispatcher` | M5 | Routes era.* calls to C# implementations |
| Embedded WebView host | M3 spike → M4 | After WEB_RUNTIME_HOST.md spike |
| Sidecar host | M13 | Official EraElectron process |
| `GameFileManifest` | M8 | Shared one-walk FS inventory |
| `GameSessionCoordinator` | M8 | Unified session management |
| Launcher UI upgrade | M14 | Multi-runtime game cards |

---

## Reference material

- `Docs/CURRENT_STATE_AUDIT.md` — baseline audit
- `Docs/ADR/WEB_RUNTIME_HOST.md` — WebView host selection
- `Docs/ADR/ERAELECTRON_RUNTIME.md` — EraElectron component design
- `ReferenceParity/EraElectron/UPSTREAM_REFERENCE.generated.json` — SDK version
- `ReferenceParity/EraElectron/API.generated.json` — SDK API inventory (56 methods)
- `ReferenceParity/EraElectron/ERAUMA_USAGE.generated.json` — call-site counts
- `ReferenceParity/EraElectron/ERAUMA_DEPENDENCIES.generated.md` — dependency graph
