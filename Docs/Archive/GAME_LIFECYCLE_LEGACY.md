# uEmuera Game Lifecycle (Phase 6)

## Boot state machine (target — PLANNED)

A single `GameBootCoordinator` should replace scattered booleans:

```
None
 → Preparing
 → LoadingCriticalConfig
 → BuildingResourceCatalog
 → LoadingGameData
 → BuildingFunctionCatalog
 → CompilingTitle
 → RunningTitle
 → BackgroundWarmup
 → Ready
(Cancelling, Failed as needed)
```

`TitleReady` means, precisely:

* all mandatory CSV/ERH semantic state exists;
* a complete function metadata catalog exists;
* `SYSTEM_TITLE` is callable;
* the console produced its first usable title UI;
* input is safe; render tree is valid.

It does **not** mean all ERB bodies compiled, all images decoded, all audio loaded, or
all resource files opened.

## Current lifecycle (audited)

* **Select game:** `FirstWindow` discovers games and calls `Run(gamePath)`.
* **Boot:** `Run` → `SpriteManager.Init` (now idempotent; essential services always on) →
  `Sys.SetGameFolder` → `Utils.ResourcePrepare` → `EmueraMain.Run`.
* **Load:** `Process.InitializeAsync` loads config/replace/rename/gamebase/CSV/ERH, then
  ERB via the strategy-selected loader (Safe = full sync by default), then
  `initSystemProcess`, then `state.Begin(TITLE)`.
* **Teardown:** `EmueraMain` calls `SpriteManager.ForceClear`; `AppContents.UnloadContents`;
  GlobalStatic reset; console disposal.

## Session ownership & cancellation (target — PLANNED)

Every game start must carry a `GameSessionId` + `CancellationToken`. On return-to-launcher,
restart, game switch, or app close, all work belonging to the old session must stop:

* cancel + join/complete every game-session worker **before**
  `AppContents.UnloadContents` / `SpriteManager.ForceClear` / GlobalStatic reset;
* no task from Game A may mutate Game B's static state;
* no async callback from an old session may touch a new session's screen, hover state,
  pooled CBG object, or resources.

This is the single most important correctness gap remaining after the Safe-mode baseline
is restored, and needs explicit automated tests (restart-during-warmup,
return-to-launcher-during-warmup, srcb race, screen-transition race, CBG race).

## EraElectron (future, do not implement now)

The coordinator, `GameSession`, cancellation, `StartupProfiler`, `GameResourceCatalog`,
VFS, asset I/O workers, permissions, and launcher are intended to be runtime-agnostic so a
future `EraElectronRuntime` can share them. The Emuera `FunctionCatalog` remains
Emuera-specific. Do not weaken the Emuera architecture to accommodate Electron early.
