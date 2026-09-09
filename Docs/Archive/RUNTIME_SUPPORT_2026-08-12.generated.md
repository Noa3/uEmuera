# Runtime Support Matrix

> Generated: 2026-08-12 · Phase 8 Milestone 0-4  
> Status codes: ✅ VERIFIED · 🔶 PARTIAL · 🔲 MISSING · ❌ NOT APPLICABLE · ⚠️ STUB

---

## Emuera Runtime

| Feature | Status | Notes |
|---|---|---|
| ERB interpreter (1.824) | ✅ VERIFIED | eraTohoK-master smoke-tested |
| EM extension instructions | ✅ VERIFIED | DataTable, MAP, XML, ERD confirmed |
| EE extension instructions | ✅ VERIFIED | |
| HTML_PRINT | ✅ VERIFIED | |
| CBG (character base graphics) | ✅ VERIFIED | |
| Image layers (G_* commands) | ✅ VERIFIED | Order deterministic |
| Sprites / GameResourceCatalog | ✅ VERIFIED | Per-game cache keyed |
| Save / Load | ✅ VERIFIED | |
| Fast Boot (opt-in) | ✅ VERIFIED | 2.1 s warm cache (eraTohoK) |
| Safe Boot (default) | ✅ VERIFIED | ~5.8 s (eraTohoK) |
| BootStrategy.Auto→Fast gate | 🔲 MISSING | Differential test suite needed |
| ProgressiveCompileScheduler | 🔲 MISSING | Idle warmup not implemented |
| FunctionCatalog per-game cache | ✅ VERIFIED | SHA256-keyed |
| FunctionResolver (all paths) | ✅ VERIFIED | Normal/event/method/dynamic |
| BackgroundErbLoader removal | 🔶 PARTIAL | Still reachable; gate pending |

---

## EraElectron Runtime

### Core architecture (Phase 8 M1-M4)

| Component | Status | Notes |
|---|---|---|
| `RuntimeKind` / `GameDescriptor` | ✅ DONE | No EE-specific objects |
| `IGameRuntime` / `RuntimeContext` | ✅ DONE | Interface only |
| `EmueraRuntimeAdapter` | ✅ DONE | Wraps existing Emuera stack |
| `EraElectronRuntime` | ⚠️ STUB | State machine; no WebView |
| `EraElectronHostMode` | ✅ DONE | Auto / Embedded / OfficialSidecar |
| `IEraElectronHost` | ✅ DONE | Interface only |
| `GameRuntimeManager` | ✅ DONE | Lifecycle coordinator |
| Multi-runtime `GameDetector` | ✅ DONE | Provisional EE fingerprints |

### ERA SDK (v4.7.0, 56 methods)

| Priority | APIs | Status |
|---|---|---|
| P0 (13 APIs) | printAndWait, get, println, printButton, input, set, print, drawLine, add, waitAnyKey, version, isEra, clear | 🔲 MISSING |
| P1 (5 APIs) | printMultiColumns, printInColRows, getLineCount, logger.info, setAlign | 🔲 MISSING |
| P2 (14 APIs) | setWidth, setOffset, setHorizontalAlign, setVerticalAlign, setToBottom, getCharactersInTrain, getAddedCharacters, checkImage, delay, saveData, setColor, playMusic, printLineChart, setOverlay | 🔲 MISSING |
| P3 (16 APIs) | replaceInColRows, printWholeImage, stopMusic, saveGlobal, loadData, notify, rmData, getAllCharacters, endTrain, setOverlay, quit, resetData, beginTrain, addCharacterForTrain, addCharacter, replaceText | 🔲 MISSING |
| Deprecated | setMask (→ setOpacity) | 🔲 MISSING |
| Logger sub-APIs (5) | logger.assert, logger.debug, logger.error, logger.warn | 🔲 MISSING |

### Platform support

| Platform | Embedded host | Sidecar | Notes |
|---|---|---|---|
| Windows | 🔲 MISSING | 🔲 MISSING | WebView2 proposed; spike needed |
| Android | 🔲 MISSING | ❌ N/A | Android WebView; scoped storage |
| Linux | 🔲 MISSING | 🔲 MISSING | WebKitGTK or CEF; spike needed |
| macOS | 🔲 DEFERRED | 🔲 DEFERRED | Not Phase 8 target |
| Unity Editor | 🔲 MISSING | N/A | Testing convenience |

### EraUma milestone progress

| Milestone | Requirement | Status |
|---|---|---|
| M6 — Title screen | Detect, config, JS entry, SDK, title render, buttons | 🔲 MISSING |
| M7 — Core gameplay | New game, menus, systems, save/load | 🔲 MISSING |
| M8 — Multimedia | Resource pack, layers, charts, audio, effects | 🔲 MISSING |
| M9 — Advanced | Workers, kojo, all SDK calls | 🔲 MISSING |
| M10 — Save interop | Official ↔ uEmuera save roundtrip | 🔲 MISSING |
| M11 — Android | ere.app parity | 🔲 MISSING |

---

## Reference tooling

| Tool | Status |
|---|---|
| `Tools/EraElectronReference/extract_api.py` | ✅ DONE |
| `Tools/EraElectronReference/scan_game_usage.py` | ✅ DONE |
| `ReferenceParity/EraElectron/API.generated.json` | ✅ DONE (56 APIs) |
| `ReferenceParity/EraElectron/ERAUMA_USAGE.generated.json` | ✅ DONE (real scan) |
| `ReferenceParity/EraElectron/UPSTREAM_REFERENCE.generated.json` | ✅ DONE |
| EraUma upstream change detector | 🔲 MISSING |
| CONFIG_SCHEMA.generated.json | 🔲 MISSING |
| GAME_COMPATIBILITY.generated.md | 🔲 MISSING |

---

## Test coverage

| Suite | Count | Pass | Known fail |
|---|---|---|---|
| FunctionCatalogTests | 12 | 12 | 0 |
| OnDemandErbCompilerTests | 4 | 4 | 0 |
| CatalogCacheStoreTests | 6 | 6 | 0 |
| Full EditMode | 284 | 279 | 5 (pre-existing) |
| EraElectron.Core | 0 | — | — (not yet created) |
| EraElectron.Reference | 0 | — | — (not yet created) |
