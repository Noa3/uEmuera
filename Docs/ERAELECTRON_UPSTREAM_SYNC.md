# EraElectron Upstream Sync

> Phase 8 · 2026-08-12

---

## Purpose

EraElectron engine and game updates will add new APIs, change existing ones, and
deprecate others. uEmuera must track these changes automatically rather than
requiring another full manual audit.

---

## Upstream sources

| Source | Location | What to track |
|---|---|---|
| era-electron SDK | `gitgud.io/umaera/engine/era-electron.git` | API additions, removals, signature changes, version bump |
| ere-webpack-plugin | `gitgud.io/umaera/engine/ere-webpack-plugin.git` | Bundle format changes, entry point rules |
| kojo-loader | `gitgud.io/umaera/engine/kojo-loader.git` | .kojo syntax changes |
| EraUma | `gitgud.io/umaera/erauma.git` | New API usage, new npm deps, .ere-min-version bumps |
| ereKanon | `gitgud.io/umaera/ere-kanon.git` | Coverage of additional APIs |

---

## Sync workflow

```
1. Developer runs: python Tools/EraElectronReference/check_upstream.py
2. Script:
   a. git-fetch latest SDK tag / commit
   b. Re-run extract_api.py against new era-electron.js
   c. Diff new API.generated.json against baseline
   d. Re-run scan_game_usage.py against latest EraUma + ereKanon
   e. Emit API_DELTA.generated.md with new/changed/removed entries
3. Developer reviews delta; updates uEmuera status fields in API.generated.json
4. New APIs added as MISSING to RUNTIME_SUPPORT.generated.md
```

---

## Reference version tracking

`ReferenceParity/EraElectron/UPSTREAM_REFERENCE.generated.json` stores:

```json
{
  "sdk_version": "4.7.0",
  "erauma_version": "3.0.00",
  "ere_min_version": "2200",
  "scan_date": "2026-08-12",
  "sdk_commit": "<TBD — obtain from git submodule>",
  "erauma_commit": "<TBD>"
}
```

Always record the exact version inspected.  
A new EraUma release must NOT require another full manual audit — run the scanner.

---

## Tools

| Script | Input | Output |
|---|---|---|
| `Tools/EraElectronReference/extract_api.py` | `ere/era-electron.js` | `API.generated.json` |
| `Tools/EraElectronReference/scan_game_usage.py` | game root | `ERAUMA_USAGE.generated.json` |
| `Tools/EraElectronReference/check_upstream.py` | (git fetch) | `API_DELTA.generated.md` (pending) |

---

## Emuera upstream (parallel)

Existing tooling in `ReferenceParity/tools/generate_parity.py` continues to
track `EvilMask/emuera.em` and `EvilMask/emuera.em.doc` independently.

Two independent reference families:
```
ReferenceParity/Emuera/      — EM/EM+EE parity
ReferenceParity/EraElectron/ — EraElectron SDK parity
```

Never merge them.

---

## Current snapshot

| Item | Captured | Notes |
|---|---|---|
| SDK version | 4.7.0 | From `ere/era-electron.js` in erauma-master |
| EraUma version | 3.0.00 | From `package.json` |
| EraUma min engine | 2200 | From `.ere-min-version` |
| ereKanon version | 116 | |
| ereKanon min engine | 110 | |
| SDK commit | Unknown | Submodule not fetched during inspection |
| Scan date | 2026-08-12 | |
