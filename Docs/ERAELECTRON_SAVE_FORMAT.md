# EraElectron Save Format

> Phase 8 · 2026-08-12  
> Based on source inspection of erauma-master. Format must be verified against
> official EraElectron reference before claiming interoperability.

---

## Overview

EraElectron games use per-game, per-slot save files.
The save format is determined by the EraElectron engine version, not the game.
uEmuera must be able to read and write the official format for interoperability.

---

## Key findings from erauma-master

| Property | Value |
|---|---|
| `saveCompressedData` (from `_config.json`) | `true` |
| ERA SDK save API | `era.saveData(slotIndex, comment?)` → `Promise<boolean>` |
| ERA SDK load API | `era.loadData(slotIndex)` → `Promise<boolean>` |
| Slot indexing | Integer, starting at 0 |
| Global save | `era.saveGlobal()` / `era.loadGlobal()` (separate from slot saves) |

---

## Save namespace isolation

```
uEmuera save root:
  <persistentDataPath>/
    saves/
      <GameDescriptor.SaveNamespace>/    ← SHA256-keyed per game+path
        save_0   ← era.saveData(0)
        save_1   ← era.saveData(1)
        ...
        save_global  ← era.saveGlobal()
```

Different games never share save slots.
Emuera saves are in a completely separate namespace.

---

## Compression

When `saveCompressedData: true`, save data is compressed.  
Compression algorithm: **not yet confirmed** — inspect official EraElectron source.  
Candidate: zlib / deflate (common in ERA tools).

---

## Data schema

The serialized content represents:

```
{
  version: <engine-version-integer>,
  gameVersion: <game-version-string>,
  comment: <user-provided string>,
  timestamp: <ISO datetime>,
  globals: { varName: value, ... },       // global ERA variables
  characters: [
    { id: <charaId>, vars: { ... } },     // per-character data
    ...
  ],
  addedCharacters: [id, ...],
  trainCharacters: [id, ...],
}
```

**Exact schema not yet confirmed — must inspect official EraElectron save output.**

---

## Interoperability goal

```
Official EraElectron save → uEmuera load → gameplay continues → uEmuera save
→ Official EraElectron load → same state
```

This requires byte-for-byte compatible serialization.  
Implement only after inspecting the official format from a real save file.

---

## Current status

| Feature | Status |
|---|---|
| Save namespace isolation | ✅ Implemented (`GameDescriptor.SaveNamespace`) |
| Slot key format (`save_N`) | ✅ Implemented in `EreApiDispatcher` |
| Actual serialization | ⚠️ STUB (returns 4-byte marker "ERES") |
| Compression | 🔲 MISSING |
| Official format compatibility | 🔲 MISSING |
| Global save separation | 🔲 MISSING |

---

## Next steps

1. Obtain a real EraElectron save file (play EraUma in official runtime, save)
2. Inspect binary format (hex dump, compression detection)
3. Identify structure (version header, compression, JSON/binary schema)
4. Implement `EreDataModel.Serialize` / `Deserialize` to match
5. Test roundtrip with official runtime
