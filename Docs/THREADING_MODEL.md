# uEmuera Threading Model (Phase 6)

## The core rule

> **Only ONE thread may mutate Emuera semantic/runtime state.**

Semantic state includes: `LabelDictionary`, `IdentifierDictionary`, `FunctionLabelLine`,
`LogicalLine`, `Process`, `ExpressionParser` state, `ParserMediator` state,
`VariableEvaluator`, `GlobalStatic` runtime objects, user-function metadata, event
lists, CALL targets, local variable definitions, and syntax/argument state.

## Thread roles (target)

```
Unity Main Thread            Emuera Interpreter Thread        Worker Threads
-----------------            -------------------------        --------------
UI / render / input          owns ALL semantic state          file reads
texture upload               startup catalog                  hashing / indexing
presentation only            executes SYSTEM_TITLE             directory enumeration
                             lazy-compiles functions           image byte reads
                             idle-time warmup                  immutable preprocessing
```

Worker threads may return **immutable** data only. They may **not** mutate the live
Emuera runtime. The Unity main thread renders and receives already-parsed results; it
does not parse.

## Current reality (audited)

* There is **no** enforced single-owner today. The interpreter runs on its own thread
  (`EmueraThread`), which is correct, but:
  * `BackgroundErbLoader` spawns a second thread that mutates `LabelDictionary` under a
    lock — a direct violation of the rule.
  * `GenericUtils.SetLoadingStatus` was called from interpreter/worker threads and wrote
    Unity UI directly.

## What changed this pass

* **BootStrategy default = Safe** (`BootConfig`), so `BackgroundErbLoader` is **not
  activated** by default. All ERB parsing/mutation happens on the single interpreter
  thread during the synchronous load. This makes the "one mutator" rule hold in the
  default configuration.
* **Loading-status channel:** interpreter/worker threads only stage a string; the Unity
  main thread applies it (`GenericUtils.PumpLoadingStatus`, pumped by SpriteManager each
  frame). No Unity UI mutation off the main thread for loading status.
* **Graphics ops:** CPU-only G* operations run on the interpreter thread against a CPU
  `GraphicsSurface`; anything needing a Unity texture read is queued and drained on the
  main thread (`AppContents.ExecutePendingGraphicsOps`).

## What is still required (PLANNED)

* An explicit `EmueraRuntimeOwnerThreadId` plus development-build asserts that throw when
  semantic mutation happens off that thread (Phase 6 #76). Candidate assertion points:
  `LabelDictionary.AddLabel`, `LabelDictionary.SortLabels`, `IdentifierDictionary`
  mutation, function compilation, `ParserMediator` mutation.
* Main-thread ownership asserts for core Unity presentation services (Phase 6 #77).
* A full audit + reclassification of every `UnityEngine.*` / TMP / `PlayerPrefs` /
  `Resources` / `AudioSource` call reachable from the interpreter or worker threads
  (Phase 6 #45), moving player-facing operations to the main thread via a small
  state channel / dispatcher.
* Replacing `BackgroundErbLoader`'s live mutation entirely with an interpreter-owned
  lazy compiler fed by immutable worker output (Phase 6B).
