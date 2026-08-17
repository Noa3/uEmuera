# EraElectron Resource Pipeline

> Phase 8 · 2026-08-12  
> Based on direct inspection of erauma-master.

---

## Overview

EraElectron games use image and audio resources declared in CSV files.
The resource pack is typically a separate download from the game package.

---

## Image resource declaration

Resources are declared in CSV files (located in a `res/` or resource directory).
The format follows the standard ERA CSV pattern with image-specific metadata.

Games reference resources by **name** (not path):

```javascript
era.printImage("FACE_1", "FACE_1_HAPPY|FACE_1_DEFAULT");
// Layer 1: "FACE_1"
// Layer 2: try "FACE_1_HAPPY", fallback to "FACE_1_DEFAULT"
```

---

## Layer model

`era.printImage(...names)` accepts one argument per layer.
Each argument is either:
- A single resource name: `"FACE_1"`
- A pipe-delimited fallback list: `"FACE_1_HAPPY|FACE_1_DEFAULT|FACE_PLACEHOLDER"`

Resolution rules:
1. Try each name in the fallback list in order
2. Use the first one that exists in the resource catalog
3. If no candidate exists, skip the layer entirely (do not show a placeholder)
4. Final composite preserves argument order — layer 0 is bottom

**Layer render order must not depend on async completion order.**

---

## EraUma resource pack

| Item | Detail |
|---|---|
| Git submodule | `gitgud.io/umaera/data/uma-resource.git` |
| Local path | `.games/erauma-master/res/` (may be absent without submodule init) |
| Image count (from AppContents scan) | 2,630 images |
| CSV files | 7 CSV files found in `resources/` directory |

The resource pack is separate from the game package.
Players download both independently.

---

## Resource mounting

A `GameDescriptor` carries a list of `ResourceMount` entries:

```
Mount 0: base game  (priority 0)  — game CSV + images
Mount 1: res pack   (priority 10) — EraUma resource pack
Mount 2: user patch (priority 100)— optional overrides
```

Resolution: higher priority mount wins if the same resource name exists in multiple mounts.
Base game is never mutated.

---

## Whole images

`era.printWholeImage(names, config?)` displays an image at full viewport width.

```javascript
era.printWholeImage("TITLE_BG", { fit: "cover", width: 24 });
```

Config options: `fit` (fill/contain/cover/none/scale-down), `offset` (0-23), `width` (1-24).

---

## Background / overlay

```javascript
era.setBack("BG_MANSION", { opacity: 0.8, position: "center", fit: "cover" });
era.setOverlay("OVERLAY_RAIN");
```

These persist until changed or cleared.

---

## Serving resources to the web runtime

Resources must be served over the same secure origin as game JS.
Do NOT use `file://` or grant universal file access.

Implementation:
- Platform WebView maps a virtual path or custom scheme to the game resource directories
- The bridge resolves resource names to physical paths via `GameResourceCatalog`
- Images are served as HTTP responses on the virtual origin

---

## EraElectron resource catalog (separate from Emuera)

`GameResourceCatalog` (existing) handles Emuera image loading.
For EraElectron, build a separate `EreResourceCatalog`:
- Scans CSV resource declarations from the game package
- Maps logical resource names to physical file paths
- Supports multiple mounts with priority overlay

**Do NOT share `GameResourceCatalog` between Emuera and EraElectron sessions.**

---

## Audio resources

Audio files (music, sfx) are similarly declared in CSV files.
`era.playMusic(names, {loop, fade})` plays the first available name.

Format: standard audio files (MP3, OGG, WAV — actual formats TBD from resource pack inspection).
Playback: prefer web-native audio (browser handles it in embedded WebView).
Unity audio bridge: only if platform restricts web audio.

---

## Current status

| Feature | Status |
|---|---|
| Emuera resource catalog (GameResourceCatalog) | ✅ WORKING |
| EreResourceCatalog (separate) | 🔲 MISSING |
| Layer rendering order guarantee | 🔲 MISSING (no WebView yet) |
| Fallback name resolution | 🔲 MISSING |
| Resource pack mounting | 🔲 MISSING |
| Virtual origin serving | 🔲 MISSING |
| Audio playback | 🔲 MISSING |
