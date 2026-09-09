# uEmuera Documentation

This directory contains the maintained developer documentation for uEmuera.

## Start here

- [Current State Audit](CURRENT_STATE_AUDIT.md) — current implementation status and known gaps.
- [ERA Platform Architecture](ERA_PLATFORM_ARCHITECTURE.md) — multi-runtime architecture.
- [Runtime Support](RUNTIME_SUPPORT.md) — generated support snapshot.
- [Licensing Architecture](LICENSING_ARCHITECTURE.md) — third-party/runtime licensing boundaries.

## Emuera runtime

- [Fast Boot Architecture](FAST_BOOT_ARCHITECTURE.md)
- [Startup Regressions](STARTUP_REGRESSIONS.md)
- [Startup Profiling](STARTUP_PROFILING.md)
- [Threading Model](THREADING_MODEL.md)
- [Resource Pipeline](RESOURCE_PIPELINE.md)
- [Image Loading Optimization](IMAGE_LOADING_OPTIMIZATION.md)
- [Performance Optimizations](PERFORMANCE_OPTIMIZATIONS.md)

Reference and conformance output lives under `../ReferenceParity/`.

## EraElectron / ERE runtime

- [EraElectron Architecture](ERAELECTRON_ARCHITECTURE.md)
- [Web Runtime Host ADR](ADR/WEB_RUNTIME_HOST.md)
- [EraElectron Runtime ADR](ADR/ERAELECTRON_RUNTIME.md)
- [Game Format](ERAELECTRON_GAME_FORMAT.md)
- [Resource Pipeline](ERAELECTRON_RESOURCE_PIPELINE.md)
- [Save Format](ERAELECTRON_SAVE_FORMAT.md)
- [Security](ERAELECTRON_SECURITY.md)
- [Testing](ERAELECTRON_TESTING.md)
- [Platform Matrix](ERAELECTRON_PLATFORM_MATRIX.md)
- [Upstream Sync](ERAELECTRON_UPSTREAM_SYNC.md)

Generated EraElectron reference information lives under
`../ReferenceParity/EraElectron/`.

## Shared lifecycle

- [Multi-Runtime Game Lifecycle](MULTI_RUNTIME_GAME_LIFECYCLE.md)
- [Visual Guide](VISUAL_GUIDE.md)
- [Unity Editor Instructions](UNITY_EDITOR_INSTRUCTIONS.md)

## Historical material

Completed migration/refactoring summaries and superseded architecture plans are kept
under [Archive](Archive/) so they remain available without competing with current
documentation.

### Documentation rule

A generated file is not a hand-maintained source of truth. If generated output disagrees
with code, fix the generator and regenerate it. Compatibility must not be marked
`VERIFIED` without a reference or integration test.
