# ERA_ELECTRON_PLAN.md — SUPERSEDED

> **This document is superseded by the Phase 8 architecture.**
> It contained stale recommendations that are explicitly rejected:
>
> - ❌ Jint as the primary EraUma rendering runtime (cannot provide DOM / CSS / Vue / Element Plus)
> - ❌ Mapping Vue UI output to the Emuera line console
> - ❌ Treating era-electron as a simple API shim over Emuera rendering
>
> These approaches will not produce real EraUma compatibility.

---

## Current plan: see Phase 8 documentation

| Document | Content |
|---|---|
| `Docs/CURRENT_STATE_AUDIT.md` | Baseline audit of all current systems |
| `Docs/ERA_PLATFORM_ARCHITECTURE.md` | Full multi-runtime platform design (create in M2) |
| `Docs/ERAELECTRON_ARCHITECTURE.md` | EraElectron runtime architecture (create in M2) |
| `Docs/ADR/WEB_RUNTIME_HOST.md` | WebView host technology decision (create in M3) |
| `ReferenceParity/EraElectron/` | Upstream SDK extractor output (create in M2) |

---

## Why the old plan was wrong

EraElectron / EraUma is a full Vue + Element Plus + Chart.js web application.
It cannot be reduced to a line-console experience without breaking:

- Layout (CSS flexbox / grid)
- Widgets (Element Plus components)
- Charts (Chart.js)
- Images / layers / resource pack
- Fonts / CJK rendering
- Responsive behavior
- Audio / input semantics

The correct approach is a browser runtime host that runs the actual
EraElectron web application, with a Unity-native bridge for:

- File system access (sandboxed)
- Save / load
- Native external links
- Permissions
- Audio fallback where needed

---

## Quick reference: Phase 8 Milestone order

```
M0  Audit current project (done)
M1  RuntimeKind / GameDescriptor / IGameRuntime / EmueraRuntimeAdapter / GameDetector (done)
M2  EraElectron upstream tooling — SDK extractor, EraUma usage scanner
M3  WebView host technical spike — Windows + Android + Linux
M4  Synthetic ERE game boot
M5  Official EraElectron example
M6  EraUma title screen
M7  EraUma core gameplay
M8  EraUma multimedia (resource pack / layers / audio / charts)
M9  EraUma advanced runtime (workers / kojo / all SDK calls)
M10 Save interoperability
M11 Android parity with ere.app
M12 Other ERE games (ereKanon / kojo test)
M13 Sidecar fallback
M14 Production launcher
M15 Emuera Fast Boot graduation (Safe/Fast differential gate)
```
