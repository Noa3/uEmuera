# Synthetic EraElectron fixture

This directory is a **uEmuera-owned packaged ERE smoke test**. It is not derived from
EraUma or any commercial game.

## What it tests

The packaged path contains:

- `.ere-min-version` — engine compatibility requirement (`2200`)
- `package.json` — launcher title/version metadata
- `era.bundle.js` — minimal SDK-side object
- `main.bundle.js` — browser-ready game entry used by WebView2
- `main.js` + `era-electron-stub.js` — source/headless development fixture

The Windows standalone test exercises:

1. GameDetector packaged ERE recognition.
2. EreLocalFileServer loader order.
3. `bridge.js` injection before the game entry.
4. WebView2 `uemuera-ready` startup handshake.
5. `era.input()` button accelerator handling.
6. sync `set/get/add` data calls.
7. async save/delay calls.
8. native-window close → runtime cleanup → launcher return.

## Manual Windows smoke test

Copy this folder into a uEmuera game library, start a **Windows standalone** build,
and select **uemuera-synthetic-ere**.

Expected visible sequence:

- title: `uEmuera EraElectron smoke test`
- click **Continue**
- `Input bridge: OK`
- `Data bridge: OK`
- save result
- final `PASS` line

Then close the EraElectron window with **X**. uEmuera should stop the ERE runtime and
return to the game library without leaving the WebView2 host or loopback server alive.

## Startup contract

The loader reports `uemuera-ready` when the exported game entry function has been
**successfully invoked**. It must not wait for the returned Promise to resolve because a
normal EraElectron `main()` remains active through the game's interaction loops.

## Headless source fixture

The older `main.js` and `era-electron-stub.js` remain useful for Node-side API-shape
experiments. They are not the browser-ready package used by the embedded host.
