# EraElectron Reference Parity

This directory contains generated EraElectron SDK/game-usage snapshots.

## Source of truth

- Upstream API signatures come from the SDK extractor in
  `Tools/EraElectronReference/extract_api.py`.
- Local implementation evidence is inferred conservatively from
  `EreApiDispatcher.cs` and `EraElectronBridgeScript.cs`.
- Real compatibility must come from reference/integration tests.

Status meanings:

- `IMPLEMENTED_UNVERIFIED` — both JS bridge and native dispatcher contain a path.
- `PARTIAL` — only one side contains a path.
- `MISSING` — no local path was detected.
- `VERIFIED` — reserved for explicit reference/integration evidence; the source
  inference tool does not assign it.

Run the upstream checker with a current `era-electron.js` snapshot to regenerate API
metadata. Generated reports must not be manually treated as timeless documentation.

Superseded pre-runtime plans and old snapshots are stored under
`../Archive/`.
