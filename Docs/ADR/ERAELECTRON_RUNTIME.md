# ADR: EraElectron Runtime Architecture

**Status:** PROPOSED  
**Date:** 2026-08-12  
**Depends on:** WEB_RUNTIME_HOST.md

---

## Context

Milestone 1 introduced `IGameRuntime` / `EmueraRuntimeAdapter`. This ADR defines the
architecture of `EraElectronRuntime`, the second concrete implementation.

Key constraints from source inspection (M2):
- Deployed game is a webpack bundle; no live CommonJS resolution required at runtime
- ERA SDK is injected into `window._era` before game JS runs
- All `era.*` calls are synchronous C#→JS returns or Promise-based async
- No Node built-ins required in game JS (build tools only)
- 56 SDK API methods (v4.7.0); P0 set is 13 methods

---

## Decisions

### D1 — EraElectronRuntime is separate from EmueraRuntimeAdapter

`EraElectronRuntime` must not instantiate `ErbLoader`, `VariableEvaluator`,
`LabelDictionary`, or `EmueraConsole`. The ERA data model for EraElectron
is owned by `EreDataModel` (separate class), not shared with Emuera.

### D2 — Three host modes

```csharp
public enum EraElectronHostMode
{
    Auto          = 0,  // prefer Embedded; fall back to OfficialSidecar if needed
    Embedded      = 1,  // system WebView (platform-native)
    OfficialSidecar = 2 // launch official EraElectron executable (desktop fallback)
}
```

### D3 — IEraElectronHost interface

`EraElectronRuntime` delegates web rendering to a host-specific implementation:

```
IEraElectronHost
  ├── EmbeddedWebHost      (WebView2 / Android WebView / WebKitGTK)
  └── SidecarHost          (official EraElectron process)
```

The host is responsible for:
- Loading the game bundles
- Providing the JS-to-C# bridge
- Managing the WebView lifecycle

`EraElectronRuntime` is responsible for:
- Implementing all `era.*` API methods in C#
- Routing ERA data operations to `EreDataModel`
- Session management / stale-callback prevention
- Save/load via `IGameStorage`

### D4 — SDK injection pattern

Before `main.bundle.js` loads, the host injects:

```javascript
window._era = <bridge proxy>;
```

The bridge proxy routes each `era.*` call to the C# implementation.
Async methods return `Promise` objects resolved when C# signals completion.

### D5 — Bundle loading sequence

```
1. Host creates isolated JS context (new WebView / new Worker context)
2. Inject window._era bridge
3. Load era.bundle.js  →  sets window._era API stubs (overridden by bridge)
4. Load main.bundle.js →  game starts executing
5. game calls era.printAndWait(...)
6. C# renders content, awaits user input
7. C# resolves Promise → game continues
```

### D6 — ERA data model ownership

`EreDataModel` owns all EraElectron game state:
- Variable tables (addressed as `'callname:1:2'`)
- Character tables (chara*.csv)
- Global / per-save data

`EreDataModel` is created per session and destroyed when `StopAsync` completes.
It never shares memory with `GlobalStatic` / Emuera `VariableData`.

### D7 — Async bridge contract

Async `era.*` methods (input, clear, loadData, etc.) must:
1. Suspend game JS by returning an unresolved Promise
2. Process the operation on the Unity main thread or a dedicated thread
3. Resolve/reject the Promise when complete
4. Never block Unity's main thread while waiting for user input

---

## Component diagram

```
GameRuntimeManager
  │
  └── EraElectronRuntime : IGameRuntime
        │
        ├── EreDataModel          (ERA variable/character tables)
        │     └── EreDataLoader   (loads CSV + static.json)
        │
        ├── EreApiDispatcher      (routes era.* calls → C# implementations)
        │     ├── EreOutputApi    (print, println, drawLine, etc.)
        │     ├── EreInputApi     (input, waitAnyKey, printAndWait)
        │     ├── EreDataApi      (get, set, add, character ops)
        │     ├── EreSaveApi      (saveData, loadData, etc.)
        │     └── EreMediaApi     (playMusic, stopMusic, etc.)
        │
        └── IEraElectronHost
              ├── EmbeddedWebHost
              │     ├── Windows: WebView2Bridge
              │     ├── Android: AndroidWebViewBridge
              │     └── Linux:   LinuxWebViewBridge (deferred)
              └── SidecarHost
                    └── EraElectronProcess
```

---

## Phase 8 implementation order

| Step | Deliverable | Milestone |
|---|---|---|
| 1 | `EraElectronRuntime` stub (State machine, no functionality) | M4 |
| 2 | `IEraElectronHost` interface + `EmbeddedWebHost` stub | M4 |
| 3 | `EreDataModel` (CSV load, `get`/`set`/`add`) | M4/M5 |
| 4 | `EreApiDispatcher` + bridge injection | M4/M5 |
| 5 | Sync output APIs (print, println, drawLine) | M5 |
| 6 | Async input APIs (input, waitAnyKey, printAndWait) | M5 |
| 7 | Full P0 API set | M5/M6 |
| 8 | Save APIs | M5 |
| 9 | Audio APIs | M7 |
| 10 | Full P1-P3 API set | M7-M9 |
