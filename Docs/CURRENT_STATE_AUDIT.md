# uEmuera Current State Audit

> Updated: 2026-09-09  
> Unity: 6000.5.8f1  
> Repository: Noa3/uEmuera

## Executive summary

uEmuera now contains two runtime families:

1. **EmueraRuntime** — the established ERB / Emuera / EM+EE path.
2. **EraElectronRuntime** — an experimental ERE web runtime with a real runtime
   abstraction, game detector, data model, JS bridge, loopback file server, Windows
   WebView2 host and desktop sidecar host.

The largest risk is no longer missing architecture. It is **verification drift**:
several documents and generated parity files still describe the earlier stub state.
This audit intentionally separates implemented code from verified compatibility.

## Emuera status

| Area | Status | Notes |
|---|---|---|
| Core ERB runtime | WORKING | Existing runtime remains the compatibility baseline. |
| EM/EE extensions | ACTIVE | Parity tracked under ReferenceParity. |
| Images / sprites / CBG | ACTIVE | Visual correctness remains a release requirement. |
| Safe boot | WORKING | Default via BootStrategy.Auto. |
| Fast boot | EXPERIMENTAL | FunctionCatalog / resolver / on-demand compiler exist; Auto remains Safe until differential gate passes. |
| Legacy BackgroundErbLoader | LEGACY | Still present and should be retired only after Fast/Safe equivalence tests pass. |

## Multi-runtime layer

| Component | Status |
|---|---|
| RuntimeKind / GameDescriptor / IGameRuntime | IMPLEMENTED |
| GameDetector (Emuera + EraElectron) | IMPLEMENTED |
| GameRuntimeManager | IMPLEMENTED |
| EmueraRuntimeAdapter | IMPLEMENTED, launcher still preserves legacy Emuera launch path |
| EraElectronRuntime | IMPLEMENTED_UNVERIFIED |
| FirstWindow multi-runtime listing | IMPLEMENTED |
| Runtime-neutral single-game autostart | IMPLEMENTED in current cleanup branch |

## EraElectron status

| Component | Status | Notes |
|---|---|---|
| EraElectronGameDetector | IMPLEMENTED | Verified source-layout markers include .ere-min-version and ere/ sources. |
| EreDataModel | IMPLEMENTED_UNVERIFIED | Local tests exist; reference parity still required. |
| EreApiDispatcher | PARTIAL | Data/save helpers exist; several APIs are placeholders or simplified. |
| EraElectronBridgeScript | PARTIAL | DOM-based output/input compatibility layer exists. |
| EreLocalFileServer | IMPLEMENTED_UNVERIFIED | Per-session loopback origin. |
| Windows WebView2Host | IMPLEMENTED_UNVERIFIED | Standalone Windows only; embedded mode intentionally disabled in Windows Editor. |
| OfficialSidecarHost | IMPLEMENTED_UNVERIFIED | Requires configured compatible EraElectron executable. |
| Android embedded host | MISSING | Platform-native WebView bridge still required. |
| Linux embedded host | MISSING | WebKitGTK/Chromium host decision/implementation still required. |
| Full EraUma gameplay | UNVERIFIED | Do not claim compatibility until an unmodified package passes integration milestones. |

## Immediate engineering priorities

1. Run and record a real Windows standalone synthetic ERE test through WebView2.
2. Replace placeholder `era.input`, `printAndWait`, audio/global-save behavior with
   reference-tested semantics.
3. Test an unmodified packaged EraUma build; capture first JS/runtime blocker.
4. Implement Android WebView host, then Linux host.
5. Finish Emuera Fast/Safe differential gate and remove legacy background loader.
6. Unify remaining duplicate resource/filesystem scans.
7. Keep generated parity tied to local implementation evidence and reference tests.

## Documentation / tooling health

This repository previously contained both generated and hand-maintained parity snapshots
with conflicting dates and statuses. Superseded manual snapshots are now archived.
The EraElectron SDK extractor now merges conservative local implementation evidence:

- bridge + dispatcher path → `IMPLEMENTED_UNVERIFIED`
- only one side → `PARTIAL`
- no path → `MISSING`

This is **not** reference verification. Only differential/integration tests may promote a
feature to VERIFIED.

## Release rule

Do not advertise "full", "almost all", or EraUma compatibility based only on parsing,
registration, or source presence. Each claim must be backed by a current test corpus.
