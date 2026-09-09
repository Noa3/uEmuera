# EraElectron Architecture

> Updated: 2026-09-09  
> See also: `Docs/ADR/ERAELECTRON_RUNTIME.md`, `Docs/ADR/WEB_RUNTIME_HOST.md`

---

## Overview

EraElectron games are webpack-bundled Vue + Element Plus applications.  
uEmuera runs them through a native WebView with an injected `era.*` bridge.

```
EraElectronRuntime
  │
  ├── EreDataModel        ← ERA variable/character tables (isolated)
  │
  ├── EreApiDispatcher    ← Routes era.* bridge calls → C# implementations
  │     ├── EreOutputApi  (print, drawLine, printButton, ...)
  │     ├── EreInputApi   (input, waitAnyKey, printAndWait)
  │     ├── EreDataApi    (get, set, add, character ops)
  │     ├── EreSaveApi    (saveData, loadData, ...)
  │     └── EreMediaApi   (playMusic, stopMusic, ...)
  │
  └── IEraElectronHost
        ├── EmbeddedWebHost    ← Platform WebView (WebView2/Android/Linux)
        └── SidecarHost        ← Official EraElectron process (desktop fallback)
```

---

## Bundle loading sequence

```
1. Host creates isolated JS context (new WebView / new Sidecar process)
2. Origin configured: ere-game:// or virtual host (not file://)
3. Inject window._era = <bridge proxy>
4. Load dist/era.bundle.js   → sets window._era stubs (overridden by bridge)
5. Load dist/main.bundle.js  → game JS begins executing
6. Game: await era.printAndWait("Hello")
7. Bridge: C# renders text, suspends game JS (unresolved Promise)
8. User presses Enter
9. Bridge: C# resolves Promise → game continues
```

---

## SDK injection

EraElectron SDK calls are routed through the JS bridge and native dispatcher. Wiring is tracked separately from reference-verified semantics.  
The proxy intercepts every `window._era.*` call and routes to `EreApiDispatcher`.

**Async methods** return a JavaScript `Promise`.  
The bridge allocates a call ID, suspends resolution, and resolves when C# completes.

**Sync methods** return immediately (no Promise).

---

## Source vs bundled packages

| Format | JS require() | Notes |
|---|---|---|
| **Bundled** (`dist/`) | Already resolved by webpack | Host loads bundle directly; no CommonJS needed |
| **Source** (`ere/`) | Live CommonJS; `#/` alias | Needs webpack build or live resolution |

**Current target:** bundled packages (normal player download).  
Source packages require `npm install && webpack` first.

---

## ERA data model (EreDataModel)

- Owns all EraElectron game state
- Never shares memory with `GlobalStatic` / Emuera `VariableData`
- Variable addressing: `'name:charIndex:arrayIndex'`
- Loaded from CSV files in `csv/` directory
- Extended tables from `_fixed.json` `extendedCharaTables`

---

## Save format

EraElectron save data is compressed (when `saveCompressedData: true`).  
See `Docs/ERAELECTRON_SAVE_FORMAT.md` for format details.  
Saves are namespaced per game via `GameDescriptor.SaveNamespace`.

---

## Image resources

EraElectron games use CSV-declared image resources.  
Images are served from the game resource pack via the same origin.  
Multi-layer images use the pipe `|` delimiter for fallback candidates:
`era.printImage("chara_a|chara_default", "overlay_b")`  
= layer 1: try `chara_a`, fall back to `chara_default`; layer 2: `overlay_b`.

---

## Audio

EraElectron games use `era.playMusic(names, {loop, fade})`.  
Audio files are declared in CSV resource metadata.  
Web audio playback is preferred (browser handles it natively in embedded host).  
Unity audio fallback only if platform audio restrictions require it.

---

## .kojo dialogue

`.kojo` files are transformer by `kojo-loader` at webpack build time into CommonJS.  
In bundled distributions, `.kojo` source is absent — the transformed JS is in the bundle.  
uEmuera does not need a runtime kojo parser for bundled games.

---

## Current implementation status

| Component | Status |
|---|---|
| `EraElectronRuntime` | WIRED_UNVERIFIED |
| `EreDataModel` | IMPLEMENTED_UNVERIFIED; local tests exist |
| `EreApiDispatcher` | PARTIAL; several API branches remain simplified/placeholders |
| `EraElectronBridgeScript` | PARTIAL; DOM/input compatibility layer exists |
| `EreLocalFileServer` | IMPLEMENTED_UNVERIFIED |
| Windows `WebView2Host` | IMPLEMENTED_UNVERIFIED (standalone only) |
| `OfficialSidecarHost` | IMPLEMENTED_UNVERIFIED |
| Android embedded host | MISSING |
| Linux embedded host | MISSING |
| Full EraUma gameplay | UNVERIFIED |

See `CURRENT_STATE_AUDIT.md` and
`../ReferenceParity/EraElectron/LOCAL_IMPLEMENTATION.generated.json`.
