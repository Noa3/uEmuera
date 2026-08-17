# Runtime architecture

## Scope

This document describes the runtime boundary that exists today and the boundary required for a future EraElectron host. It is intentionally an architecture contract, not a claim that an Electron runtime already exists.

## Current execution path

The Unity host owns application lifecycle and presentation. The Emuera runtime is reached through the existing Unity entry point and worker/controller types:

```text
Unity scene
  -> MainEntry / EmueraContent
  -> EmueraThread
  -> GameProc.Process
  -> GameData / FunctionMethodCreator
  -> GameView / content services
```

The important consequence is that `Process` is not currently a standalone runtime service. It is coupled to the Unity-facing console/view and to process-global configuration. A future host must not instantiate it by reflection or duplicate its state; it needs an explicit lifecycle adapter.

## Required host boundary

The smallest useful boundary is:

```text
IRuntimeHost
  Start(RuntimeOptions)
  Tick(CancellationToken)
  SubmitInput(RuntimeInput)
  Snapshot()
  Stop()
```

The interface must remain host-neutral. It must not expose Unity `GameObject`, `Texture2D`, `MonoBehaviour`, WPF controls, Electron IPC objects, or renderer-specific classes. Rendering and input are ports:

- `IRuntimeViewPort`: text lines, images, buttons, HTML islands, tooltip requests;
- `IRuntimeInputPort`: keyboard, mouse, button and text input events;
- `IRuntimePersistencePort`: save/load and extended data;
- `IRuntimeFilePort`: normalized game-relative file access.

The existing Unity adapter can translate these ports to the current `EmueraConsole`, Unity textures and scene objects. An EraElectron adapter would translate the same ports to a browser renderer and IPC. Until this adapter exists, the two hosts must not be described as behaviorally equivalent.

## State ownership rules

1. `GameData` and `VariableData` are runtime state and belong to exactly one runtime instance.
2. Renderer caches, sprites and HTML islands belong to the host adapter, not to `Process`.
3. Input queues are owned by the host adapter and drained on the runtime thread.
4. Persistence serializes runtime state, never renderer objects or live UI handles.
5. A stopped runtime must release its event subscriptions and file handles before a new runtime is created.

## Current implementation gaps

- no checked-in `IRuntimeHost` implementation;
- no deterministic tick boundary for a browser host;
- no renderer-neutral representation for CBG/GDI/HTML output;
- no host-neutral mouse coordinate and button-map contract;
- extended save data is not yet routed through one persistence port;
- Unity lifecycle/domain-reload behavior is not covered by integration tests.

## Acceptance criteria for the next host phase

- Unity continues to start and stop through the existing path;
- a headless adapter can execute an ERB fixture without a Unity scene;
- input and output are observable through ports;
- save/load roundtrips do not depend on renderer state;
- a second host can be added without changing `FunctionMethodCreator` semantics.

No Electron implementation should be added before these criteria are testable. A facade with no working adapter would only hide the existing coupling.
