# uEmuera Startup Regressions & Fixes (Phase 6)

This log tracks concrete startup problems found during the Phase 6 audit, their root
cause, the fix (or planned fix), and the regression test that guards them.

Status legend: **FIXED** (landed this pass) · **PARTIAL** (mitigated) · **PLANNED** (designed, not yet implemented).

---

## R1 — Essential SpriteManager services never start on >8 GB machines — **FIXED**

* **Symptom:** On player builds with more than 8 GB RAM, sprites/graphics and
  background texture creation silently stopped working (blank images, G*/CBG never
  updating). Editor was unaffected, hiding the bug.
* **Root cause:** `SpriteManager.Init()` gated *all* maintenance coroutines behind
  memory-size branches. The `else` branch for `> 8192 MB` started **nothing**, and the
  `<= 4096 MB` branch skipped `UpdateRenderOP`. The graphics-op drain coroutine
  (`UpdateGraphicsSurface`) was additionally only started inside `#if UNITY_EDITOR`.
* **Affected:** every game using images / G-commands / CBG on high-RAM desktops and in
  release builds.
* **Fix:** Separated **required services** from **memory policy**. Essential coroutines
  (`UpdateGraphicsSurface`, `UpdateRenderOP`, `Update`) now always start, once, on every
  platform and RAM size. Memory size now only sets `kPastTime` (texture retention time).
  `Init()` is idempotent (`services_started_` guard) because the persistent
  `CoroutineHelper` is never stopped and `Init()` runs on every game start — previously
  this stacked duplicate coroutines on each restart.
* **File:** `Assets/Scripts/SpriteManager.cs` (`Init`).
* **Regression test:** requires PlayMode harness (coroutine host). Manual: boot a game
  with images on a >8 GB desktop build; images must appear. Tracked as PLANNED automated
  test (needs a memory-size seam to inject).

---

## R2 — Render operations serviced up to 15 s late — **FIXED**

* **Symptom:** Textures produced by worker threads (and render textures) could take up
  to 15 seconds to appear.
* **Root cause:** `SpriteManager.UpdateRenderOP()` used
  `yield return new WaitForSeconds(15)` as its idle wait, so freshly-queued work waited
  for the next 15 s tick.
* **Fix:** Per-frame drain — when the queues are empty the coroutine yields a single
  frame (`yield return null`) and re-checks; ready work is serviced on the next frame.
* **File:** `Assets/Scripts/SpriteManager.cs` (`UpdateRenderOP`).
* **Follow-up (PLANNED):** add a per-frame **work budget** and bounded concurrency
  (Phase 6 #31–#33). The queues are still plain `List`s mutated from multiple threads —
  a lock-free/bounded queue is a separate task.

---

## R3 — Forced GC + duplicate `UnloadUnusedAssets` in the boot hot path — **FIXED**

* **Symptom:** Slow, janky time-to-title; a synchronous full GC stalled the frame right
  before the game booted.
* **Root cause:** `FirstWindow.Run()` did
  `UnloadUnusedAssets → GC.Collect → SpriteManager.Init → ResourcePrepare → UnloadUnusedAssets → EmueraMain.Run`,
  and `Process.InitializeAsync` called `GC.Collect()` again just before showing the title.
* **Fix:** Removed the forced `System.GC.Collect()` from `FirstWindow.Run()` and the
  second (pre-boot) `Resources.UnloadUnusedAssets()`; removed the `GC.Collect()` in
  `Process` after ERB load. One `UnloadUnusedAssets` (reclaiming the previous game) is kept.
* **Files:** `Assets/Scripts/FirstWindow.cs`, `Assets/Scripts/Emuera/GameProc/Process.cs`.
* **Caveat / needs measurement:** the spec rightly says *measure, don't assume*. These
  are textbook hot-path stalls and their removal is safe for correctness, but the actual
  ms saved must be confirmed with `StartupProfiler` (PLANNED, see STARTUP_PROFILING.md).

---

## R4 — Background ERB loader mutates live semantic state from a second thread — **PARTIAL (default-disabled)**

* **Symptom:** Intermittent startup errors, "unrecognized identifier", functions
  evaluating to 0/"", wrong return types, races on restart.
* **Root cause:** `BackgroundErbLoader` parses ERB on a background thread and mutates
  `LabelDictionary` (via `AcquireWriteLock`) while the interpreter runs — violating the
  "one thread mutates semantic state" rule. Compounded by:
  * `PendingUserDefinedMethodTerm` hard-codes `typeof(Int64)` return type, so
    `#FUNCTIONS` (string) calls parsed before the body loads are mis-typed (Phase 6 #8).
  * `WaitForFunction(..., 15000)` fails valid functions on slow devices (Phase 6 #15).
  * Priority decided by **filename** (`SYSTEM_*`, `TITLE.ERB`, …), not function metadata
    (Phase 6 #6).
* **Fix (this pass):** Introduced `BootStrategy` (`Safe`/`Fast`/`Auto`) and `BootConfig`.
  **Auto (default) resolves to Safe**, which uses the full synchronous `LoadErbFiles`
  path and **never activates `BackgroundErbLoader`**. Because every consumer
  (`ExpressionParser`, `Instraction.Child`, `LabelDictionary`,
  `PendingUserDefinedMethodTerm`) is gated behind `BackgroundErbLoader.Instance != null`,
  the unsafe path, the numeric-return assumption, and the 15 s timeout are all inert by
  default. This restores the known-good baseline (Phase 6 §1–2).
* **Files:** `Assets/Scripts/Emuera/GameProc/BootStrategy.cs` (new),
  `Assets/Scripts/Emuera/GameProc/Process.cs` (loader selection).
* **Remaining (PLANNED):** the real fix is a `FunctionCatalog` + interpreter-owned lazy
  compiler that makes Fast mode correct (metadata-based priority, correct
  `#FUNCTION`/`#FUNCTIONS` typing, `EXISTFUNCTION` before body compile, complete event
  groups). Only then should Auto attempt Fast. See FAST_BOOT_ARCHITECTURE.md.

---

## R5 — Unity UI mutated from the interpreter/background thread — **PARTIAL**

* **Symptom:** Potential races / undefined behavior updating TMP/UI text off the main
  thread (`GenericUtils.SetLoadingStatus` → `OptionWindow.SetLoadingStatus` directly).
  Called from `ErbLoader`, `Process` (interpreter thread) and `BackgroundErbLoader`
  (worker thread).
* **Fix (this pass):** `SetLoadingStatus` now only **stages** the string into a
  thread-safe channel; the actual UI write happens on the main thread in
  `GenericUtils.PumpLoadingStatus()`, pumped once per frame by
  `SpriteManager.UpdateGraphicsSurface()`. Bursts coalesce to the latest value.
* **Files:** `Assets/Scripts/GenericUtils.cs`, `Assets/Scripts/SpriteManager.cs`.
* **Remaining (PLANNED):** full audit of every `UnityEngine.*` / TMP / `PlayerPrefs` /
  `Resources` call reachable from the interpreter thread (Phase 6 #45). This pass only
  fixed the loading-status channel.

---

## R6 — Resource preparation can decode full images just to read dimensions — **TOOL LANDED, INTEGRATION PLANNED**

* **Symptom:** Startup decodes images (allocating `Texture2D`) merely to discover
  width/height for incomplete resource definitions.
* **Root cause:** `Utils.ResourcePrepare()` path can call
  `SpriteManager.GetTextureInfo(...)` to size sprites.
* **Fix (this pass):** Added `uEmuera.ImageHeaderProbe` — reads PNG/JPEG/BMP/GIF/WebP
  dimensions from header bytes only, no decode, fully bounds-checked, unit-tested.
* **File:** `Assets/Scripts/uEmuera/ImageHeaderProbe.cs`; tests
  `Assets/Tests/EditMode/ImageHeaderProbeTests.cs`.
* **This pass (FIXED integration):** `Utils.ResourcePrepare()` now uses
  `ImageHeaderProbe.TryReadFile` instead of `SpriteManager.GetTextureInfo` to resolve
  missing CSV dimensions. No `Texture2D` is created during startup resource preparation.

---

## R7 — Stale async image callbacks fire on new-game GameObjects — **FIXED**

* **Symptom:** Rapid game restart / return-to-launcher caused old-session sprite-load
  callbacks to fire on new-game UI objects, briefly showing wrong images or causing
  `NullReferenceException` on already-destroyed objects.
* **Root cause:** `SpriteManager.CallbackInfo` had no session guard — every pending
  callback would fire regardless of whether the game was still the same session.
* **Fix:** `GameSession` (new `Assets/Scripts/Emuera/GameProc/GameSession.cs`) provides a
  monotonic integer that is bumped (`GameSession.Bump()`) at the START of every teardown
  in `EmueraMain.ClearCo` and `RestartCo` (before `GlobalStatic.Reset` and
  `ForceClear`). `CallbackInfo` captures the session ID at creation time and discards
  the result if the ID no longer matches. `BackgroundErbLoader.BackgroundWork` checks the
  session at every batch boundary and exits cleanly instead of mutating the new session's
  `LabelDictionary`.
* **Files:** `GameSession.cs` (new), `SpriteManager.cs`, `EmueraMain.cs`,
  `BackgroundErbLoader.cs`.

---

## R8 — Two forced GC.Collect() calls during game restart — **FIXED**

* **Symptom:** Restart was noticeably slower than Clear+launch.
* **Root cause:** `EmueraMain.ClearCo` and `RestartCo` both called `System.GC.Collect()`
  during the teardown coroutine — a synchronous stop-the-world collection.
* **Fix:** Both calls removed. See STARTUP_REGRESSIONS.md R3 for prior Clear-path fix.
* **File:** `Assets/Scripts/EmueraMain.cs`.

---

## R9 — Placeholder Texture2D allocated per image miss (texture leak) — **FIXED**

* **Symptom:** Each missing or not-yet-loaded image spawned its own 64×64 RGBA32
  `Texture2D` + `Color[64×64]` + `Sprite` + `TextureInfo`. On image-heavy games with
  many preloads, this created thousands of tiny leaked textures (never freed before
  `ForceClear`; also never deduplicating).
* **Root cause:** `CreatePlaceholderTexture(64, 64)` was called once per
  `CreateAndStorePlaceholder` and `CreatePlaceholderSpriteInfo` invocation.
* **Fix:** One shared 4×4 `Texture2D` + `TextureInfo` + `Sprite` created lazily on first
  request (`GetOrCreateSharedPlaceholderTex`); its refcount is anchored at 1 so it is
  never evicted. All placeholder requests reuse these shared objects. Placeholder
  dimensions (4×4) no longer accidentally set `baseimage.size` (so layout is not
  corrupted). New `Sprite` objects are NOT created per `SpriteInfo` — the shared one is
  used, which is correct because placeholder sprites are visually identical.
* **File:** `Assets/Scripts/SpriteManager.cs`.
* **Note:** `GivebackSpriteInfo` on a shared-placeholder SpriteInfo will decrement the
  shared TextureInfo's refcount from 1 toward 0 — but the anchor refcount protects it
  from eviction. Monitor if this causes unexpected eviction in edge cases.

---

## R10 — No interpreter-thread ownership enforcement — **PARTIAL (dev-build guards)**

* **Symptom:** Background thread semantic mutations went undetected in all builds.
* **Fix:** `InterpreterThreadGuard` (new `Assets/Scripts/Emuera/GameProc/InterpreterThreadGuard.cs`):
  sets owner when `EmueraThread.Work` starts, clears on exit; asserts in
  `LabelDictionary.SortLabels/AddLabel/AddFilename`. All asserts are
  `[Conditional("UEMUERA_DEBUG")]` — zero cost in release builds. When `UEMUERA_DEBUG`
  is defined, a violation throws `InvalidOperationException` immediately with file/thread info.
* **Files:** `InterpreterThreadGuard.cs` (new), `EmueraThread.cs`, `LabelDictionary.cs`.
* **Remaining:** extend assertions to all other semantic mutation sites (#76).

---

## R11 — StartupProfiler: instrumentation now in place — **PARTIAL (wired, needs data)**

* **What landed:** `StartupProfiler` (`Assets/Scripts/Emuera/GameProc/StartupProfiler.cs`)
  records stages with millisecond resolution. Marks wired at: `SelectGame`,
  `GamePathPrepared`, `ResourceCatalogStart/Ready`, `CSVLoaded`, `ERHLoaded`,
  `ErbCatalogStart/Ready`, `SystemTitleCompileStart/Ready`, `FirstInteractiveTitle`.
  The full report is logged to the Unity console after each boot
  (`UnityEngine.Debug.Log(StartupProfiler.Report())`).
* **Remaining:** run the profiler against representative games to get real numbers.
  Confirm R2 (15 s render ops) and R3 (GC/unload) savings are measurable.
  Add `RecordMainThreadOp` calls around texture uploads.

---

## Known open items (not yet addressed)

Ranked by impact — see the phase spec for full detail:

1. **Fast-mode correct compilation using FunctionCatalog** — the catalog is now built and wired; the next step is making `BootStrategy.Fast` actually use it for correct on-demand lazy compilation instead of the unsafe `BackgroundErbLoader` mutation model. PLANNED.
2. **Persistent resource index** — cache `GameResourceCatalog` + `FunctionCatalog` metadata to disk per game (size/mtime invalidation) to avoid even the cheap scan on warm boots. PLANNED.
3. **Bounded I/O concurrency + priority queue** for image loading (Phase 6 #32–#33). Currently one coroutine per image with unlimited parallelism. PLANNED.
4. **StartupProfiler real numbers** — instrumentation in place; numbers need a real game run. PLANNED.
5. **Dev-build thread-guard at all remaining semantic mutation sites** (beyond the three LabelDictionary methods already guarded). PLANNED.

---

## R12 — FunctionCatalog: EXISTFUNCTION and PendingUserDefinedMethodTerm type — **FIXED**

* **Symptom (Fast mode only):** `EXISTFUNCTION("FOO")` returned 0 for functions whose
  ERB file had not yet been loaded by the background loader; `#FUNCTIONS` functions
  referenced before their body loaded were silently given `typeof(Int64)` return type,
  causing wrong expression type-checking and runtime type errors.
* **Root cause:** `ExistFunctionMethod.GetIntValue` only consulted `LabelDictionary`
  (which is empty for unloaded files); `PendingUserDefinedMethodTerm` always passed
  `typeof(Int64)` to the base constructor regardless of the actual declaration.
* **Fix:** New `FunctionCatalog` (`Assets/Scripts/Emuera/GameProc/FunctionCatalog.cs`):
  * Line-by-line metadata scan of all ERB files BEFORE body loading. Uses `Config.Encode`
    + BOM detection (never hard-codes Shift-JIS). Handles `#FUNCTION`/`#FUNCTIONS`
    distinction correctly (`#FUNCTIONS` must be tested first).
  * `ExistFunctionMethod.GetIntValue` now falls back to `catalog.ExistFunctionValue(name)`
    when `LabelDictionary` returns null (progressive mode or any future lazy path).
  * `PendingUserDefinedMethodTerm` calls `FunctionCatalog.Instance.GetClrReturnType(name)`
    to get the correct CLR type; falls back to `typeof(Int64)` only when catalog is
    unavailable.
  * Catalog built at `Process.InitializeAsync` before `LoadErbFiles*` — profiler mark
    `FunctionCatalogReady` records the cost.
  * Cleared on `GlobalStatic.Reset()` so stale data never leaks between games.
* **Tests:** `Assets/Tests/EditMode/FunctionCatalogTests.cs` (14 cases: normal/method/string,
  event flags, multi-file, comment skip, goto-label exclusion, clear, etc.)
* **Files:** `FunctionCatalog.cs` (new), `Process.cs`, `Creator.Method.cs`,
  `UserDefinedMethodTerm.cs`, `GlobalStatic.cs`.

---

## R13 — Multiple independent directory scans per startup — **FIXED**

* **Symptom:** Startup walked the resources directory 2–3 times per game launch:
  `SpriteManager.InitializeFileIndex`, `ResourcePrepare`'s inner file list (via
  `GetContentFiles`), and `AppContents.AutoDiscoverImagesFromSubdirectories`.
* **Fix:** New `GameResourceCatalog` (`Assets/Scripts/uEmuera/GameResourceCatalog.cs`):
  * One `Scan(dir)` → single `Directory.GetFiles(…, AllDirectories)` walk for all image
    extensions, indexing by basename, relative path, and full path.
  * `SpriteManager.InitializeFileIndex` delegates to `catalog.ExportFileIndex(file_index_)`
    if the catalog has already scanned the same directory — zero additional walk.
  * Provides `TryGetDimensions` via `ImageHeaderProbe` (no texture decode).
  * `AppContents.LoadContents` calls `GameResourceCatalog.Scan` first, then
    `SpriteManager.InitializeFileIndex`.
  * Cleared in `SpriteManager.ForceClear()` on game teardown.
* **Files:** `GameResourceCatalog.cs` (new), `AppContents.cs`, `SpriteManager.cs`.

---

## R14 — File.ReadAllBytes on Unity main thread during image loading — **FIXED**

* **Symptom:** Coroutine-based image loading (`SpriteManager.Loading`) blocked Unity's
  main thread during `File.ReadAllBytes`, causing frame stalls visible as hitches during
  title screens and gameplay — proportional to image file size.
* **Root cause:** Coroutines run on the Unity main thread; `File.ReadAllBytes` inside
  a coroutine is synchronous blocking I/O on that thread.
* **Fix:** Inside `Loading()`, `File.ReadAllBytes` is now dispatched via
  `System.Threading.Tasks.Task.Run` and the coroutine `yield return null`s each frame
  until the task completes. Unity renders freely while the disk read happens on a thread
  pool thread. Texture2D creation (Unity main-thread-only) remains after the read. Also
  adds a session-ID guard: if `GameSession` advances while the read is in flight
  (restart during image load), the result is discarded.
* **File:** `Assets/Scripts/SpriteManager.cs` (`Loading`).

---

## R15 — Unity call audit from interpreter thread — **AUDITED/CLEAR**

* All `GenericUtils.SetLoadingStatus` calls from interpreter/worker threads now route
  through the thread-safe staging channel (fixed in the previous pass as R5).
* `uEmuera.Media.SystemSounds.Hand/Asterisk.Play()` are no-op stubs — safe from any thread.
* `EmueraConsole.ClientWidth/Height` read `Screen.width/height` — Unity thread-safe
  read-only properties, safe from any thread.
* `UnityEngine.Debug.Log*` — Unity thread-safe logging, safe from any thread.
* No remaining high-risk Unity API calls reachable from the interpreter thread were found.

## R16 — Fast-boot lazy ERB compiler (Phase 6 Fast-boot: point 1) — **IMPLEMENTED**

Scope: replace the unsafe background-thread compile model (R6/R8) for the Fast boot
path with an interpreter-owned on-demand compiler; no cross-thread semantic mutation.

* New `OnDemandErbCompiler` (interpreter-owned singleton) + `ErbOnDemand` router
  (`Assets/Scripts/Emuera/GameProc/OnDemandErbCompiler.cs`).
* `ErbLoader.LoadErbFilesLazy` compiles only priority files (SYSTEM_*, GAMEBASE,
  TITLE, START, COMMON) synchronously, then activates the on-demand compiler for the
  remaining files. Priority boot cost ≈ `O(priority files)`; deferred files compile
  at first reference with a small per-file hitch (documented trade-off, no startup
  stall and no semantic race).
* Invariants:
  * ONLY the interpreter thread mutates `LabelDictionary`/function metadata. The
    compiler runs inside `Process.DoInstruction` / `CallEventFunction` /
    `PendingUserDefinedMethodTerm.ResolveMethod`, i.e. the interpreter thread.
  * Compile-before-parse (mark file in-flight before `loadErb`) prevents reentrant
    self-references from recursing into `loadErb` during their own syntax check.
  * Session guard: `GameSession.IsValid(sessionId_)` aborts stale compiles after a
    restart; `GlobalStatic.Reset` + loader full-load hooks call `Clear()`.
* Wiring:
  * `Process.Fast` → `LoadErbFilesLazy` (Process.cs).
  * Expression forward refs → `PendingUserDefinedMethodTerm` via
    `ErbOnDemand.IsKnownFunction` (ExpressionParser.cs).
  * CALL runtime fallback compiles on demand, then dynamic lookup (Instraction.Child.cs).
  * Event dispatch (`CallEventFunction`) compiles all declaring files for the event
    (catches #LATER / non-priority #PRI copies).
  * Syntax-check warning suppression now routes through `ErbOnDemand.IsFunctionPending`
    (works for both lazy compiler and legacy `BackgroundErbLoader`).
* Tests: `OnDemandErbCompilerTests` (pending reporting, priority-metadata marking,
  failed-compile state, clear lifecycle) — all pass.

## R17 — Persistent catalog cache (Phase 6 Fast-boot: point 2) — **IMPLEMENTED**

Scope: cache the two boot-time scans (FunctionCatalog full ERB parse; GameResourceCatalog
directory listing + header probing) across boots.

* New `CatalogCacheStore` (`Assets/Scripts/Emuera/GameProc/CatalogCacheStore.cs`):
  binary cache files under `persistentDataPath/uEmueraCache/`.
* Validation is fingerprint-based (path + last-write ticks + length for every source
  file), so edits/additions/removals/renames invalidate without content reads. The
  FunctionCatalog cache also embeds encoding codepage + `Config.ICFunction` flag
  (both change stored names) — toggling either invalidates.
* `FunctionCatalog.Build` and `GameResourceCatalog.Scan` try the cache first and save
  after a successful scan. All paths are best-effort: any failure → full re-scan;
  caching can never break boot.
* Resource cache preserves probed header dimensions (no re-probing across boots);
  cache load is validated by a cheap directory re-enumeration (no content reads).
* Tests: `CatalogCacheStoreTests` (round-trip, stale-rejection, encoding-change
  rejection, resource round-trip/stale) — all pass.

## R18 — Bounded image I/O concurrency + priority queue (Phase 6 Fast-boot: point 3) — **IMPLEMENTED**

Scope: cap concurrent image reads and give user-facing loads precedence over preload.

* New `AsyncIoGate` (`Assets/Scripts/AsyncIoGate.cs`): fixed concurrency cap
  (`MaxConcurrentReads = 2`), dual FIFO queues (high/low). Workers always drain the
  high queue before low, so preloads never delay an on-screen sprite.
* `SpriteManager.Loading` now reads through the gate instead of one unbounded
  `Task.Run(File.ReadAllBytes)` per image; `GetSprite` loads are high priority,
  `PreloadCoroutine` items are low priority (`lowPriority: true`).
* `SpriteManager.CacheStats` exposes `ActiveIoReads` / `PendingIoReads` /
  `LowPriorityIoReads` for monitoring.
* Tests: `AsyncIoGateTests` (correct bytes, cap never exceeded, low-priority drains,
  mixed-priority, missing-file fault) — all pass.

