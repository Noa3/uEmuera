# Runtime Support

> Updated: 2026-09-09  
> Maintained status overview. Generated reference evidence lives under `ReferenceParity/`.

## Emuera

| Area | Status |
|---|---|
| Classic ERB interpreter | Active / working baseline |
| EM / EM+EE extensions | Active; reference parity tracked |
| HTML / images / CBG / Graphics | Active; visual regressions remain release-critical |
| Safe Boot | Default / working |
| Fast Boot | Experimental opt-in |
| Auto → Fast | Not enabled; differential gate pending |
| Legacy BackgroundErbLoader | Still present; removal pending compatibility gate |

## EraElectron / ERE

| Area | Status |
|---|---|
| RuntimeKind / GameDescriptor / GameRuntimeManager | Implemented |
| EraElectron game detection | Implemented with source-layout tests |
| EraElectronRuntime | Wired, unverified against full EraUma gameplay |
| EreDataModel | Implemented with local tests; reference parity pending |
| JS bridge / dispatcher | Partial semantics; all currently scanned EraUma API names have wiring, not verification |
| Windows embedded host | WebView2 implemented for standalone; packaged-game verification pending |
| Desktop official sidecar | Implemented, unverified |
| Android embedded host | Missing |
| Linux embedded host | Missing |
| EraUma full gameplay | Unverified |
| ereKanon gameplay | Unverified |

## Evidence

- SDK signatures: `ReferenceParity/EraElectron/API.generated.json` after running the upstream extractor.
- EraUma usage: `ReferenceParity/EraElectron/ERAUMA_USAGE.generated.json`.
- Local source wiring: `ReferenceParity/EraElectron/LOCAL_IMPLEMENTATION.generated.json`.
- Current architecture: `Docs/CURRENT_STATE_AUDIT.md`.

A source path is not proof of semantic parity. Only reference/integration tests may use
the status `VERIFIED`.
