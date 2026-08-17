# uEmuera Startup Profiling (Phase 6)

> Status: **PLANNED** (design + metric list). No `StartupProfiler` is implemented yet.
> This document specifies what must be measured so that optimization is driven by data,
> not assumption. Several hot-path changes already landed (see STARTUP_REGRESSIONS.md
> R2/R3) and must be **confirmed** with these measurements.

## Principle

> Optimize startup by changing **when** expensive work runs, not by removing information
> the interpreter needs.

Do not hardcode one absolute startup target for every game/device. Optimize these
metrics instead:

* Time to first title output
* Time to interactive title
* Main-thread stall duration
* Time until visible title assets ready
* Total warmup duration

## Stages to record (high-resolution `Stopwatch`)

```
SelectGame, GamePathPrepared
ResourceCatalogStart, ResourceCatalogReady
ConfigLoaded, ReplaceLoaded, RenameLoaded, GameBaseLoaded, CSVLoaded, ERHLoaded
ErbCatalogStart, ErbCatalogReady
SystemTitleCompileStart, SystemTitleCompileReady
FirstConsoleOutput, FirstInteractiveTitle, FirstUnityRenderedTitle
BackgroundCompile50, BackgroundCompile100
VisibleTitleAssetsReady
AllWarmupComplete
```

## TimeToInteractiveTitle

Defined as: the title script reached a valid interactive state
(`INPUT` / `INPUTS` / `WAIT` / `BINPUT` or equivalent) **and** Unity rendered the output.
This is more meaningful than "`Program.Main` returned from Initialize".

## Main-thread stall tracking

Log main-thread operations exceeding configurable thresholds (e.g. 8 ms / 16 ms / 33 ms).
Do not spam logs in release builds. Attribute the stall (e.g. "Texture upload abc.png").

## Report (developer mode) — example shape

```
Game Startup — <game>
  Cold Start
    Game selection → title text:   <measured> ms
    Title → interactive:           <measured> ms
    Visible title images ready:    <measured> ms
    All ERB warmup complete:       <measured> ms
    All background assets warm:    <measured> ms
  Largest main-thread stall:       <op> <measured> ms
```

## Safe vs Fast comparison (PLANNED)

For every benchmark game, record Safe boot and Fast boot and assert they produce the same
title output, function catalog, variables, render tree, and input behavior. Compatibility
outranks the stopwatch: a Fast mode that is faster but breaks games is not acceptable.

## Instrumentation hooks already present

* `BootConfig.FallbackReason` records a Fast→Safe fallback reason.
* `GenericUtils.SetLoadingStatus` stages loading milestones (could feed stage markers).
* `LoadedFileTracker` records the files read during startup.

## Immediate measurements still owed (from this pass)

* Confirm ms saved by removing `GC.Collect()` and the duplicate `UnloadUnusedAssets`
  (R3). If profiling proves a collection is required at a point, reintroduce it there
  deliberately, off the critical path.
* Confirm `UpdateRenderOP` per-frame drain removed the multi-second image latency (R2).
