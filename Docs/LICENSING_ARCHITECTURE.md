# Licensing Architecture

> Phase 8 · 2026-08-12  
> **MANDATORY: Read before copying upstream EraElectron or game source into uEmuera.**

---

## Rule

Do not assume code-copy compatibility.  
If direct source integration imposes licensing obligations, stop and document options.

---

## uEmuera core

No explicit license file found at project root as of Phase 8.  
**Action required:** Decide and commit a LICENSE file before any public release.

---

## Emuera upstream

| Component | Known license |
|---|---|
| Emuera 1.824 (original C#) | Custom (non-commercial; see original readme) |
| Emuera.EM extensions | Bundled with games; license per-game |

**uEmuera is a port**, not a fork. Runtime semantics are re-implemented.  
Do not copy Emuera 1.824 source verbatim.

---

## EraElectron SDK (era-electron.js)

- Source: `gitgud.io/umaera/engine/era-electron`  
- License: **Not confirmed** — submodule license not inspected  
- Action: Inspect `engine/` submodule LICENSE file before any source copy

The `ere/era-electron.js` file present in EraUma is a stub/type-definition shim (no logic).  
Its JSDoc type annotations have been used to extract API signatures for compatibility — this is factual structural documentation, not implementation code reproduction.

---

## EraUma

- Source: `gitgud.io/umaera/erauma`  
- License: **GPL-2.0-only** (confirmed in `package.json`)

**Implication:** EraUma game scripts and data are GPL-2.0-only.  
uEmuera must NOT incorporate EraUma source code into its own codebase.  
uEmuera may implement a compatible runtime for EraUma without being GPL-licensed  
(runtime is a separate work; it does not incorporate the game source).

---

## Vue 3

- License: **MIT** ✅
- No restriction on hosting Vue-powered games in uEmuera.

## Element Plus

- License: **MIT** ✅

## Chart.js

- License: **MIT** ✅

## EraElectron (official runtime / ere-app)

- License: **Not confirmed** — inspect before bundling
- Relevant only for OfficialSidecar mode (user-provided runtime)
- uEmuera does not bundle the official EraElectron runtime by default

## ere-webpack-plugin / kojo-loader

- Source: `gitgud.io/umaera/engine`
- License: **Not confirmed** — inspect before any code copy
- Used by game authors during build (not by uEmuera runtime)
- uEmuera does not need to copy these; it loads the compiled webpack output

---

## Architecture consequences

| Scenario | Risk | Decision |
|---|---|---|
| Copy EraUma CSV data into uEmuera test fixtures | HIGH — GPL-2.0 | Don't commit; use local corpus |
| Implement era.* API in C# for compatibility | LOW — API surface is factual | Proceed |
| Bundle official EraElectron runtime with uEmuera | UNKNOWN | Investigate; likely user-installs |
| Use Vue/Element Plus/Chart.js in embedded WebView | OK — MIT | Proceed |
| Copy EraUma JS source for test stubs | HIGH — GPL-2.0 | Use synthetic fixtures only |

---

## Third-party notices (generate at release)

Generate `THIRD_PARTY_NOTICES` for release builds covering:
- All Unity plugins installed
- WebView host technology chosen
- Any JS libraries bundled in uEmuera itself (not game libraries)

---

## Synthetic test fixtures

**Never commit EraUma or ereKanon game content into the repository.**  
Create minimal license-safe `.js` fixtures (uEmuera-owned, MIT) that exercise the  
era.* API surface without using game story, character, or CSV data.
