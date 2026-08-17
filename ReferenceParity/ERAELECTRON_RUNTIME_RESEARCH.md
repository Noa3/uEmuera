# EraElectron runtime research

## Status

Research-only. This file does **not** claim that an EraElectron runtime is present in uEmuera or that a browser host has been executed successfully.

## Confirmed reference surface

The EM+EE reference documentation exposes the relevant families that a second host must preserve:

- DataTable management, columns, rows, cells, selection and XML serialization;
- XML document manipulation through XPath-like operations;
- MAP management, key enumeration and XML serialization;
- HTML printing and HTML print islands;
- CBG/GDI/sprite operations;
- keyboard, mouse and button input;
- save data and extended persistence.

Primary documentation used for the parity work:

- <https://evilmask.gitlab.io/emuera.em.doc/en/Reference/DT_ROW.html>
- <https://evilmask.gitlab.io/emuera.em.doc/en/Reference/DT_COLUMN.html>
- <https://evilmask.gitlab.io/emuera.em.doc/en/Reference/DT_CELL.html>
- <https://evilmask.gitlab.io/emuera.em.doc/en/Reference/DT_SELECT.html>
- <https://evilmask.gitlab.io/emuera.em.doc/en/Reference/DT_SERIALIZATION.html>

The documentation is the behavioral reference. The local runtime must not infer compatibility from the presence of a registration alone.

## Host decomposition

A browser/Electron host would need four independently testable layers:

1. **Interpreter adapter** — runs the existing runtime and exposes commands/results without browser types.
2. **Output model** — represents text, images, sprites, HTML islands, buttons and tooltips as serializable events.
3. **Input bridge** — converts browser events to runtime input with explicit coordinate scaling and button identity.
4. **Persistence/file bridge** — maps game-relative paths and save payloads without allowing traversal or arbitrary host file access.

The renderer should consume output events. It should not call `FunctionMethodCreator`, mutate `VariableData`, or implement DataTable semantics a second time.

## Risks that must be resolved before implementation

- Unity and browser rendering have different text metrics and image lifecycles.
- CBG/GDI operations are stateful and cannot be represented by only the final bitmap if later commands depend on handles.
- HTML islands need lifecycle IDs, clear/update semantics and ordering relative to console output.
- Browser pointer coordinates need a documented mapping from CSS pixels to game coordinates.
- `MOUSEB`/button-map behavior must distinguish physical button state from configured game buttons.
- CP932/legacy script input must be decoded before tokenization, not after parsing.
- save payloads must preserve DataTable row IDs, MAP/XML state and extended arrays atomically.
- untrusted XML and game-relative paths require bounded parsers and traversal checks.

## Evidence status

| Area | Local runtime | Reference evidence | Classification |
|---|---|---|---|
| DataTable model/API | implemented in part | reference docs and source inspection | testable parity slice |
| MAP/XML | registered, incomplete edge semantics | reference docs | partial |
| HTML island | not complete | reference docs | missing |
| CBG/GDI renderer | existing Unity-side pieces, no neutral event model | reference docs | missing |
| mouse/button bridge | not complete | reference docs | missing |
| persistence bridge | fragmented | reference docs | missing |
| Electron host | absent | no local runtime execution evidence | planned |

## Next implementation gate

Before a browser-specific project is started, add a headless `IRuntimeHost` test fixture with:

- deterministic input queue;
- captured output events;
- one DataTable fixture;
- one HTML-island fixture;
- one save/load roundtrip;
- explicit failure when a renderer-only API leaks through the boundary.

Until that fixture exists, an Electron proof of concept would be an integration demo rather than a parity implementation.
