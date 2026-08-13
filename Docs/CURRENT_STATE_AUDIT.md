# uEmuera Current State Audit — Phase 8 Milestone 2

> Updated: 2026-08-13  
> Unity: 6000.5.4f1 · Platform: Android target  
> Tests: 326 total EditMode (5 known pre-existing failures — unchanged)

---

## Summary

uEmuera is a Unity port of Emuera 1.824 with a Phase 7 Fast Boot subsystem and a Phase 8 multi-runtime architecture layer. The Emuera runtime is fully working. A multi-runtime scaffold (RuntimeKind, GameDescriptor, IGameRuntime, GameRuntimeManager, EmueraRuntimeAdapter, EraElectronRuntime) was completed in Milestone 1. Milestone 2 added EraElectron reference tooling: real EraUma layout was inspected, the detector was corrected with verified fingerprints, the SDK was scanned (50 APIs, 0 Node builtins, 0 npm deps), and all 50 APIs are now stubbed in EreApiDispatcher.

---

## 1. Fast Boot Systems

| System | File | Status | Notes |
|---|---|---|---|
| `FunctionCatalog` | `GameProc/FunctionCatalog.cs` | **WORKING** | Per-game SHA256-keyed cache; 14 613 functions for eraTohoK |
| `FunctionResolver` | `GameProc/FunctionResolver.cs` | **WORKING** | All major normal/event/method/dynamic call paths route through it |
| `OnDemandErbCompiler` | `GameProc/OnDemandErbCompiler.cs` | **WORKING** | Lazy compile on interpreter thread; failure rollback; 4/4 unit tests |
| `CatalogCacheStore` | `GameProc/CatalogCacheStore.cs` | **WORKING** | Per-file-set SHA256 key prevents test/game cache collision (fixed Phase 7) |
| `GameSession` | `GameProc/GameSession.cs` | **WORKING** | Monotonically incrementing session ID; stale-callback protection |
| `BootStrategy` | `GameProc/BootStrategy.cs` | **WORKING** | Auto→Safe conservative; Fast opt-in; fallback logging |
| `StartupProfiler` | `GameProc/StartupProfiler.cs` | **WORKING** | Stage-level timing; reports on every boot |
| `GameResourceCatalog` | `uEmuera/GameResourceCatalog.cs` | **WORKING** | Per-directory cache; validated against fingerprints |
| `AsyncIoGate` | `AsyncIoGate.cs` | **EXPERIMENTAL** | Concurrency cap; EditMode unit tests only; not on hot paths yet |
| `InterpreterThreadGuard` | `GameProc/InterpreterThreadGuard.cs` | **WORKING** | Debug-build assertion; semantic mutation on interpreter thread only |
| `LazyCompileFailure` | `GameProc/LazyCompileFailure.cs` | **WORKING** | Preserves parse/exception failure with position for deferred compile |

### Fast Boot timing (eraTohoK-master, editor warm run)

```
ERHLoaded              ~390 ms
FunctionCatalogReady  ~1 060 ms  (+670 ms — catalog build or cache hit)
ErbCatalogReady       ~2 100 ms  (+1 040 ms — SYSTEM_TITLE priority file + deferred activation)
FirstInteractiveTitle ~2 100 ms
```

Safe baseline same game: ~5.8 s. Cache probe (persistent game cache): `ok=True, 14 613 functions`.

---

## 2. Legacy Fast Boot Code

| Code | File | Status | Reachable? | Action |
|---|---|---|---|---|
| `BackgroundErbLoader` | `GameProc/BackgroundErbLoader.cs` | **LEGACY** | Yes — explicit progressive mode only | Remove after Fast gate passes |
| `WaitForFunction` (15 s timeout) | `BackgroundErbLoader.cs:207` | **LEGACY** | Only via BackgroundErbLoader | Remove with BackgroundErbLoader |
| `LoadSingleErbBackground` | `ErbLoader.cs:1880` | **DEPRECATED** | Yes — `[Obsolete]`-tagged; called by BackgroundErbLoader | Remove when BackgroundErbLoader removed |
| `LoadErbFilesProgressive` | `ErbLoader.cs:1569` | **LEGACY** | Not the current Fast path; never called by Auto/Fast | Retire after gate |
| `FlushLabelsBackground` | `ErbLoader.cs:1908` | **LEGACY** | Called from LoadSingleErbBackground | Remove with BackgroundErbLoader |
| BackgroundErbLoader locks in `LabelDictionary` | `LabelDictionary.cs` | **LEGACY** | Yes — `AcquireReadLock`/`AcquireWriteLock` still wired | Remove locks after BackgroundErbLoader gone |

**Gate:** `BackgroundErbLoader` removal is blocked until the Fast/Safe differential test suite passes.  
`BootStrategy.Auto` remains `→ Safe` until that gate is cleared.

---

## 3. Filesystem Scan Classification

| Location | Method | Classification | Notes |
|---|---|---|---|
| `Utils.GetContentFiles` | `GetFiles` ×5 (images) | **BOOT_CRITICAL** | Populates content dict at ResourcePrepare time |
| `AppContents.LoadContents` | `GetFiles` (CSV) | **BOOT_CRITICAL** | Reads resource CSV files at boot |
| `AppContents` subdirectory scan | `GetDirectories`+`GetFiles` | **BOOT_CRITICAL** | Auto-discovers images below resources/ |
| `GameResourceCatalog.Scan` | `GetFiles` ×6 (image exts) | **BOOT_CRITICAL** | Cached; second boot uses persisted catalog |
| `CatalogCacheStore` resource fingerprint | `GetFiles` ×6 | **BOOT_CRITICAL** | Cache validity check only; fast |
| `Config.GetFiles` (ERB/CSV) | `GetDirectories`+`GetFiles` | **BOOT_CRITICAL** | ERB file list; feeds FunctionCatalog |
| `Config.getUpdateKey` | `GetFiles` (ERB+CSV) | **SAVE_ONLY** | Key-change detector for config reload |
| `Config.createSavDirAndMoveFiles` | `GetFiles` (sav) | **SAVE_ONLY** | First-run sav migration |
| `EraEncoding.BuildIndex` | `GetFiles` (all) | **BOOT_CRITICAL** | Builds case-insensitive VFS index for game root |
| `SpriteManager` (line 198) | `GetFiles` ×5 (image exts) | **GAME_RUNTIME** | Fallback scan if GameResourceCatalog misses |
| `VariableEvaluator.GetFiles` | `GetFiles` (DAT) | **GAME_RUNTIME** | Reads `.dat` files at runtime |
| `EmueraConsole` ERB reload | `GetFiles` ×2 (ERB) | **DEBUG_ONLY** | RELOADERB command only |
| `CompatibilityScanner` | `GetDirectories`+`GetFiles` | **DEBUG_ONLY** | Analysis mode only |
| `Utils.GetFiles` (CSV readers) | `GetFiles` ×4 | **BOOT_CRITICAL** | CSV resource loading |
| `Utils.NormalizeExistingDirectoryPath` | `GetDirectories` ×2 | **BOOT_CRITICAL** | Path normalization at game selection |
| `GameDiscovery` | `GetDirectories` ×2+`GetFiles` ×2 | **GAME_RUNTIME** | Launcher: discovers Emuera games only |

**Redundancy:** `Utils.GetContentFiles` and `GameResourceCatalog.Scan` both enumerate the resources/ directory on cold boot. `GameResourceCatalog` caches; `GetContentFiles` does not. Unification tracked as Phase 8 item 157.

---

## 4. Runtime Boundary Audit

### Current architecture (single runtime)

```
FirstWindow (game picker)
  → Sys.SetGameFolder (sets global ExeDir)
  → Utils.ResourcePrepare (global content dict)
  → EmueraMain.Run
      → EmueraThread.Start (background thread)
          → Program.Main (initializes everything)
              → Process.Initialize
                  → ErbLoader (catalog + lazy/safe compile)
                  → GlobalStatic (global interpreter state)
              → EmueraConsole (display layer)
  → EmueraMain.Clear / Restart (return to launcher)
```

### Coupling requiring separation before EraElectron

| Component | Coupling type | Separation needed |
|---|---|---|
| `FirstWindow` | Directly calls `Sys.SetGameFolder`, `ResourcePrepare`, `EmueraMain.Run` | Route through `IGameRuntime.InitializeAsync` / `StartAsync` |
| `EmueraMain` | Directly starts `EmueraThread`; owns `Clear`/`Restart` | Wrap in `EmueraRuntimeAdapter` |
| `EmueraThread` | Singleton; Emuera interpreter thread | Keep Emuera-only; adapter wraps it |
| `Process` | Emuera interpreter state (`LabelDictionary`, `GlobalStatic`) | Keep Emuera-only; never shared |
| `EmueraConsole` | Emuera display layer | Keep Emuera-only; never shared |
| `SpriteManager` | Global static image cache | Must `ForceClear` on session end; needs session guard |
| `OptionWindow` | Hardcoded `EmueraMain.Clear`/`Restart` | Needs runtime-aware callbacks |
| `GameDiscovery` | Emuera-only directory recognition | Replace with multi-runtime `GameDetector` in launcher |
| `GlobalStatic` | Emuera interpreter globals | Keep Emuera-only; EraElectron has its own state |
| `AppContents` | Emuera resource CSV format | Cannot share with EraElectron directly |
| `GameSession` | Session ID counter | Already runtime-neutral; can remain shared |
| `StartupProfiler` | Stage logger | Already runtime-neutral; can remain shared |
| `CatalogCacheStore` | Emuera catalog cache | Emuera-only; EraElectron has separate catalog |
| `AsyncIoGate` | Concurrency cap | Can become shared infrastructure |

### No IRuntimeHost exists

`RuntimeCapabilityRegistry.cs` registers `"runtime.emuera"` and `"runtime.eraelectron"` string constants but there is no actual interface, no adapter, and no launcher routing. Status: **EXPERIMENTAL/STUB**.

---

## 5. EraElectron Status — Milestone 2

### Runtime scaffold (Milestone 1 — COMPLETE)

| Item | File | Status |
|---|---|---|
| `RuntimeKind` enum | `Runtime/RuntimeKind.cs` | **WORKING** |
| `GameDescriptor` | `Runtime/GameDescriptor.cs` | **WORKING** |
| `IGameRuntime` | `Runtime/IGameRuntime.cs` | **WORKING** |
| `RuntimeContext` + service interfaces | `Runtime/RuntimeContext.cs` | **WORKING** |
| `EmueraRuntimeAdapter` | `Runtime/EmueraRuntimeAdapter.cs` | **WORKING** |
| `EraElectronRuntime` | `Runtime/EraElectronRuntime.cs` | **STUB** |
| `GameRuntimeManager` | `Runtime/GameRuntimeManager.cs` | **WORKING** |
| `GameDetector` + `IGameDetector` | `Runtime/Detection/` | **WORKING** |
| `EmueraGameDetector` | `Runtime/Detection/EmueraGameDetector.cs` | **WORKING** |
| `EraElectronGameDetector` | `Runtime/Detection/EraElectronGameDetector.cs` | **WORKING** — verified Aug 2026 |
| `FirstWindow` multi-runtime routing | `FirstWindow.cs` | **PARTIAL** — GetList uses GameDetector; EraElectron launch path stubs |
| `IEraElectronHost` / `EraElectronHostMode` | `Runtime/IEraElectronHost.cs` | **STUB** |
| `EreDataModel` | `Runtime/EraElectron/EreDataModel.cs` | **PARTIAL** — CSV/var/config parsing present; not connected to host |
| `EreApiDispatcher` | `Runtime/EraElectron/EreApiDispatcher.cs` | **STUB** — all 50 EraUma APIs registered |
| `UnityRuntimeLogger` | `Runtime/UnityRuntimeLogger.cs` | **WORKING** |

### EraElectron reference tooling (Milestone 2 — COMPLETE)

| Item | File | Status |
|---|---|---|
| EraUma source layout inspection | — | **DONE** — 2026-08-13, EraUma 3.0.00 |
| EraElectronGameDetector fingerprint correction | `Runtime/Detection/EraElectronGameDetector.cs` | **DONE** |
| API scanner | `Tools/EraElectronReference/scan_game_usage.py` | **WORKING** |
| `ERAUMA_USAGE.generated.json` | `ReferenceParity/EraElectron/` | **GENERATED** — 3003 JS files, 50 APIs, 91 kojo |
| `API.generated.json` | `ReferenceParity/EraElectron/` | **GENERATED** — 50 APIs all STUB |
| `GameDetectorTests.cs` | `Tests/EditMode/` | **WORKING** — 18/18 pass |
| `EreDataModelTests.cs` | `Tests/EditMode/` | **WORKING** — 24/24 pass |

### EraUma package layout (verified Aug 2026, v3.0.00)

```text
<game root>/
  .ere-min-version      ← "2200"  DEFINITIVE ERE marker
  package.json          ← game metadata, devDependencies include ere-webpack-plugin
  webpack.config.js     ← source/dev only
  ere/
    era-electron.js     ← SDK source (aliased as #/era-electron)
    main.js             ← game entry point
    data/, event/, page/, system/, utils/, i18n/
      *.js, *.kojo (91 kojo files)
  csv/                  ← static game data
  res/                  ← resources
  build/
    static.json         ← pre-built config/data
```

**No `ere.config.json` or `era.config.json` in real EraUma.** Previous detector had wrong fingerprints — corrected.

### EraUma API inventory (50 APIs, Aug 2026)

Top 10 by call count:

| API | Calls | Async | uEmuera |
|---|---|---|---|
| `printAndWait` | 21 359 | ✓ | STUB |
| `get` | 5 809 | — | STUB |
| `println` | 4 266 | — | STUB |
| `printButton` | 4 054 | — | STUB |
| `input` | 3 412 | ✓ | STUB |
| `set` | 2 244 | — | STUB |
| `print` | 890 | — | STUB |
| `drawLine` | 869 | — | STUB |
| `add` | 562 | — | STUB |
| `waitAnyKey` | 388 | ✓ | STUB |

Node builtins used: **0**. npm packages used: **0**. All game logic is internal.  
`.kojo` files: **91** — kojo is used extensively.  
See `ReferenceParity/EraElectron/ERAUMA_USAGE.generated.json` for full list.

### Missing for Milestone 3 (WebHost spike)

| Item | Status |
|---|---|
| WebView / browser host technology chosen | **MISSING** — ADR needed |
| `Docs/ADR/WEB_RUNTIME_HOST.md` | **MISSING** |
| JavaScript execution (any engine) | **MISSING** |
| era.* bridge to real JS context | **MISSING** |
| Synthetic ERE game boots | **MISSING** |

---

## 6. Emuera Compatibility Status

| Feature area | Status | Notes |
|---|---|---|
| ERB interpreter core | **WORKING** | Emuera 1.824 compatibility |
| EM extension instructions | **WORKING** | DataTable, MAP, XML, ERD confirmed |
| HTML_PRINT | **WORKING** | |
| CBG (character base graphics) | **WORKING** | |
| Image layers (G_* commands) | **WORKING** | Order deterministic |
| Sprites | **WORKING** | GameResourceCatalog backed |
| Save / Load | **WORKING** | |
| Fast Boot (opt-in) | **WORKING** | Safe/Fast semantic gate not yet passed; Auto→Safe |
| ProgressiveCompileScheduler | **MISSING** | Idle-warmup not implemented (Phase 8 item 156) |
| Resource scan unification | **PARTIAL** | GameResourceCatalog caches resources/; Utils.GetContentFiles does not |
| Safe/Fast differential test gate | **MISSING** | Required before Auto→Fast |
| BackgroundErbLoader removal | **PENDING** | Blocked on differential gate |

---

## 7. Test Coverage Summary

| Suite | Count | Pass | Known fail | Notes |
|---|---|---|---|---|
| `FunctionCatalogTests` | 12 | 12 | 0 | |
| `OnDemandErbCompilerTests` | 4 | 4 | 0 | |
| `CatalogCacheStoreTests` | 6 | 6 | 0 | includes isolation regression |
| `EreDataModelTests` | 24 | 24 | 0 | Phase 8 — EraElectron data model |
| `GameDetectorTests` | 18 | 18 | 0 | Phase 8 — multi-runtime detection |
| Full EditMode | 326 | 321 | 5 | `CompatibilityScannerTests`, `EraDataTableSemanticsTests` ×3, `Phase3ConformanceTests` ×1 — pre-existing |
| PlayMode | 3 classes | pass | 0 | SpriteManager, OnDemandRenderManager, GenericUtils |

---

## 8. Documentation Status

| Document | Status |
|---|---|
| `Docs/FAST_BOOT_ARCHITECTURE.md` | Stale (Phase 6); superseded by actual code |
| `Docs/STARTUP_REGRESSIONS.md` | Accurate |
| `Docs/THREADING_MODEL.md` | Accurate |
| `Docs/GAME_LIFECYCLE.md` | Partially accurate; predates GameDescriptor |
| `ReferenceParity/ERA_ELECTRON_PLAN.md` | **DEPRECATED** — replace with Phase 8 docs |
| `Docs/ADR/` | **MISSING** — create for Phase 8 |
| `Docs/ERA_PLATFORM_ARCHITECTURE.md` | **MISSING** |
| `Docs/ERAELECTRON_ARCHITECTURE.md` | **MISSING** |
| `ReferenceParity/EraElectron/` | **MISSING** |

---

## 9. Phase 8 Milestone 0–2 Verdict

**Baseline preserved.** Existing Emuera runtime compiles cleanly. Fast boot reaches interactive title in ~2.1 s (warm cache). No regressions from any Phase 8 work.

**Milestone 1 complete:** RuntimeKind / GameDescriptor / IGameRuntime / EmueraRuntimeAdapter / multi-runtime GameDetector all working.

**Milestone 2 complete:** EraUma layout inspected, detector corrected, SDK scanned (50 APIs), usage manifest generated, all 50 APIs stubbed in dispatcher.

**Proceed to Milestone 3:** WebHost technical spike — choose browser host technology, prove Vue + Element Plus + WebWorkers on Windows + Android + Linux.
