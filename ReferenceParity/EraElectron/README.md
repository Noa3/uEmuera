# EraElectron Reference Parity

This directory contains reproducible EraElectron reference and usage data.

## Files

- `UPSTREAM_REFERENCE.generated.json` — captured upstream/game baseline.
- `ERAUMA_USAGE.generated.json` — EraUma call/dependency usage scan.
- `CONFIG_SCHEMA.generated.json` — captured configuration schema.
- `LOCAL_IMPLEMENTATION.generated.json` — conservative local source-wiring evidence.
- `API.generated.json` — created by `extract_api.py` from an actual EraElectron SDK
  snapshot. It may be absent until the extractor establishes/refreshes a baseline.

## Status model

The source scanners deliberately do **not** assign VERIFIED.

- `WIRED_UNVERIFIED` — both JS bridge and native dispatcher contain a path.
- `PARTIAL` — only one side contains a path.
- `MISSING` — no local path was detected.
- `VERIFIED` — reserved for explicit reference/integration evidence.

Run:

```bash
python Tools/EraElectronReference/generate_local_status.py
python Tools/EraElectronReference/check_upstream.py --sdk-path /path/to/era-electron.js
```

When the upstream signature set changes, inspect the generated delta and use
`--accept` only after review.

Superseded plans and misleading old generated snapshots are stored under
`../Archive/`.
