# uEmuera Fast Boot Architecture (Phase 6)

## Goal

```
Select Game → tiny critical boot → title becomes interactive
            → player uses title menu → remaining work continues incrementally
            → full runtime warm/ready
```

Hard constraint: **fast startup must never run the interpreter against half-initialized
semantic state.** Correctness first, speed second.

## Current status

This pass **stabilizes** startup rather than shipping the full staged compiler.

* `BootStrategy` = `Safe` | `Fast` | `Auto` (`BootConfig.Strategy`, default `Auto`).
  * **Safe** → `ErbLoader.LoadErbFiles` (full synchronous parse + syntax check on the
    interpreter thread). Known-good compatibility baseline.
  * **Fast** → `ErbLoader.LoadErbFilesProgressive` + `BackgroundErbLoader` (the existing
    progressive path). **Opt-in only**, because it currently mutates semantic state from
    a background thread and mis-types pending `#FUNCTIONS` calls.
  * **Auto** (default) → resolves to **Safe** for now. Once the interpreter-owned lazy
    compiler below lands, Auto will attempt Fast and fall back to Safe on any
    startup-invariant violation (`BootConfig.RecordFastFallback`).
* `FunctionCatalog` is **now built** on every boot (Safe and Fast) before body loading:
  * O(n) line-by-line scan of all ERB files; uses game encoding + BOM detection.
  * `#FUNCTION`/`#FUNCTIONS` distinction captured per function — fixes `PendingUserDefinedMethodTerm` type.
  * `EXISTFUNCTION` falls back to catalog when `LabelDictionary` hasn't loaded the file yet.
  * This is the foundation the full Fast-mode lazy compiler will build on.
* `GameResourceCatalog` — one directory walk per game replaces three independent scans.

Selection happens at `Process` init:
`Program.AnalysisMode ? loadErbs : BootConfig.UseProgressiveLoading ? LoadErbFilesProgressive : LoadErbFiles`.

## Why the old progressive model is unsafe (to be replaced)

`BackgroundErbLoader` (see `Assets/Scripts/Emuera/GameProc/BackgroundErbLoader.cs`):

* second thread parses ERB and mutates `LabelDictionary` under a lock while the game runs;
* priority is decided by **filename** (`SYSTEM_*`, `TITLE.ERB`, `START.ERB`, …), not by
  function metadata — ERA functions can live in any file;
* `PendingUserDefinedMethodTerm` defaults to `typeof(Int64)`, so a `#FUNCTIONS` (string)
  function referenced before its body loads is mis-typed;
* `WaitForFunction(..., 15000)` turns "slow device" into "function missing";
* `QuickScanFunctionNames` is a hand-rolled `@`-line / fixed Shift-JIS scan that does not
  know `#FUNCTION` vs `#FUNCTIONS`, event modifiers, or preprocessor semantics.

## Target architecture (PLANNED — not yet implemented)

```
GameBootCoordinator (state machine)
   None → Preparing → LoadingCriticalConfig → BuildingResourceCatalog
        → LoadingGameData → BuildingFunctionCatalog → CompilingTitle
        → RunningTitle → BackgroundWarmup → Ready  (+ Cancelling / Failed)

FunctionCatalog / FunctionMetadata
   name, source file, source range, normal/event, #FUNCTION|#FUNCTIONS,
   arg signature, #PRI/#LATER/#ONLY, source order, file order, compile state
   (Catalogued → Queued → Compiling → Compiled → Failed)

ProgressiveCompileScheduler
   idle-time Pump() with a 2–5 ms Stopwatch budget; on-demand compile beats warmup;
   deterministic, no timeouts.
```

### Required properties (definition of done for Fast mode)

1. `SYSTEM_TITLE` located by **function name**, not filename; may live in any ERB file.
2. **All** function declarations catalogued (with correct type) before `SYSTEM_TITLE`
   executes — bodies may stay uncompiled.
3. `#FUNCTION` (int) vs `#FUNCTIONS` (string) typed correctly before body compilation.
4. `EXISTFUNCTION` / `ENUMFUNC` / `TRYCALL` / `TRYJUMP` / `CALLFORM` correct before warmup
   finishes.
5. Complete **event groups** (`#PRI`/normal/`#LATER`/`#ONLY`) known before first invoke;
   bodies lazy.
6. On-demand synchronous compile on the interpreter thread when an uncompiled function is
   called — recursive, no 15 s timeout, transactional publish (never expose a partially
   linked `FunctionLabelLine`).
7. Metadata parsing reuses the **real** encoding/preprocessor/lexer/declaration parser in
   a metadata-only mode (not regex).

### Failure recovery

If Fast detects a catalog failure, invalid cache, unexpected parser condition, thread
ownership violation, or a Fast-path-caused compile failure: abort the Fast session
cleanly and restart in Safe, logging a developer entry via `BootConfig.RecordFastFallback`.
Never silently swallow a parser error that would make a valid function disappear.

## Caching (PLANNED, after correctness)

Persist FunctionCatalog metadata (function boundaries, signatures, encoding) keyed by
ERB size/mtime/encoding/parser-config/ERH state. Do **not** serialize live `LogicalLine`
graphs yet.
