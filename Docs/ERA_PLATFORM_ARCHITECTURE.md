# ERA Platform Architecture

> Current architecture: 2026-09-09

uEmuera is evolving into one Unity launcher with isolated runtime backends.

```text
uEmuera Launcher
  |
  +-- GameDetector
  |     +-- EmueraGameDetector
  |     +-- EraElectronGameDetector
  |
  +-- GameRuntimeManager
        |
        +-- EmueraRuntimeAdapter
        |     +-- existing EmueraMain / EmueraThread / Process
        |
        +-- EraElectronRuntime
              +-- EreDataModel
              +-- EreApiDispatcher
              +-- EreLocalFileServer
              +-- IEraElectronHost
                    +-- WebView2Host (Windows standalone)
                    +-- OfficialSidecarHost (desktop fallback)
                    +-- Android host (pending)
                    +-- Linux host (pending)
```

## Runtime boundaries

### Emuera

Owns ERB parsing, variables, console state, HTML/CBG/G* graphics, Emuera saves and
Fast/Safe boot semantics. EraElectron code must not mutate these structures.

### EraElectron

Owns JavaScript/web execution, ERE data state, bridge calls, browser presentation and
ERE saves. It must not instantiate Emuera parser/runtime state to emulate web games.

## Shared services

Runtime-neutral services include game detection, session IDs, logging, startup
profiling, permissions, storage interfaces and launcher lifecycle.

## Current launcher integration

The game list uses the multi-runtime GameDetector. EraElectron rows launch through
GameRuntimeManager. Emuera rows intentionally still use the established launch path
until EmueraRuntimeAdapter is proven equivalent for cleanup/startup behavior.

Single-game autostart must also use GameDetector; keeping the old Emuera-only
GameDiscovery there causes EraElectron packages to be invisible to packaged autostart.

## Platform host strategy

- Windows standalone: WebView2 embedded host exists.
- Windows Editor: embedded WebView2 is intentionally disabled; use sidecar/test stubs.
- Android: platform WebView host pending.
- Linux: embedded host pending.
- Desktop source-form ERE packages may use the official sidecar when configured.

See [ADR/WEB_RUNTIME_HOST.md](ADR/WEB_RUNTIME_HOST.md).

## Invariants

1. Only one active game runtime at a time.
2. Runtime state must be isolated between game sessions.
3. Old async callbacks must not mutate a new session.
4. Emuera compatibility cannot regress for EraElectron progress.
5. EraElectron compatibility is measured against real ERE packages and reference output.
6. No game-name-specific compatibility hacks.
