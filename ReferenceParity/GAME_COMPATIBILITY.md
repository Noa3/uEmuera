# uEmuera — Game Compatibility Report

Generated: 2026-08-09  
Engine: uEmuera (Unity 6 port of Emuera/Emuera.EM+EE)  
Scan tool: CompatibilityScanner (built-in, ground-truth from engine registries)

## Summary

All tested C# Emuera games parse without errors (0 file errors on ERB scanning).
Remaining "unknown" tokens are user-defined functions, not missing built-ins.
era-electron games (erauma, ere-kanon) require a JS runtime and are not C# Emuera compatible.

| Game | ERB Files | Parse Errors | Status |
|---|---|---|---|
| anon-tw (English mod) | 3,896 | 0 | ✅ Should load |
| eraNAS | 3,321 | 0 | ✅ Should load |
| eratohoTW (English) | 1,533 | 0 | ✅ Should load |
| eraTYPE-MOON | 217 | 0 | ✅ Should load (0 unknown tokens) |
| eraTohoK | 2,666 | 0 | ✅ Should load (game-specific user functions only) |
| eratohoLiG | 568 | 0 | ✅ Should load (game-specific user functions only) |
| eraQueen | 344 | 0 | ✅ Should load (0 unknown tokens) |
| eraAkumaMaid (English) | 892 | 0 | ⚠️ Should load; TR_NAME function missing from English mod (game-side issue) |
|| eraFL | 1,097 | 0 | ✅ Should load |
|| EraJK | 282 | 0 | ✅ Should load |
|| EraRL | 1,156 | 0 | ✅ Should load |
|| EraSekaQ | 606 | 0 | ✅ Should load |
|| EraFGO-K | 2,320 | 0 | ✅ Should load |
|| EraMegaten | 8,876 | 0 | ✅ Should load (large game — Persona/Megaten Crossover) |
|| EraMaouEx | 320 | 0 | ✅ Should load |
|| EraTohoReverse | 862 | 0 | ✅ Should load |
|| eraAS | 407 | 0 | ✅ Should load |
|| eraBlueResort | 2,813 | 0 | ✅ Should load |
|| erauma | 0 ERB | — | ❌ era-electron game — requires JS runtime, not C# Emuera compatible |
| ere-kanon | 0 ERB | — | ❌ era-electron game — requires JS runtime |
| erauma (data only) | 0 ERB | — | 📦 CSV data loadable; game logic requires era-electron runtime |

Note: "unknown tokens" are call-like identifiers `FOO(...)` that don't match any registered
instruction or method. In all C# Emuera games these are user-defined `@FOO` functions,
not missing built-ins.

## Per-Game Detail

### anon-tw (anon-tw_eng-modding)
- 3,896 ERB + ERH files, 253 CSV files
- 3,587,247 logical lines
- Feature areas: Console=664553, Control=1586415, Graphics=14, HTML=89, Input=3679, Save=106, Sprite=79
- Remaining unknowns (user functions): KOJO_MESSAGE_SEND (321 uses — kojo message library),
  STA, DATUI_TOP_OPTIMIZE, GETTEXTBOX, MOUSEB

### eraNAS
- 3,321 ERB files, 274 CSV files
- 3,113,815 logical lines
- Feature areas: Audio=46, Console=608868, Control=1353103, Graphics=41, HTML=79, Input=3416, Save=118, Sprite=87
- Remaining unknowns (user functions): KOJO_MESSAGE_SEND, STA, DATUI_TOP_OPTIMIZE, DEATHMATCH, PPK, SPLIT_DEL

### eratohoTW-game-eng-release
- 1,533 ERB files, 177 CSV files
- 1,142,984 logical lines
- Feature areas: Console=229901, Control=470475, HTML=37, Input=1136, Save=65
- Remaining unknowns (user functions): KOJO_MESSAGE_SEND, STA, DATUI_TOP_OPTIMIZE

### eraTYPE-MOON (eraTYPE-MOON-master)
- 217 ERB files
- 0 parse errors, 0 unknown built-ins
- Clean scan — no user-defined unknown calls detected

### eraTohoK (eraTohoK-master)
- 2,666 ERB files
- 0 parse errors, 6 unknown tokens (all user-defined functions)
- Unknowns: CITY_ECONOMY_S, CITY_GUARD_S, DRAWMAP_END, DRAWMAP_INIT, DRAWMAP_LINE_S, REGISTER_ROUTE_S
  (game-specific map/city system functions)

### eratohoLiG (eratohoLiG-master)
- 568 ERB files
- 0 parse errors, 8 unknown tokens (all user-defined functions)
- Unknowns: CLEAN, DRUNK, ENE, EREC, ID_TO_CHARA, STA, TSP, VIG
  (game-specific status/character system functions)

### eraQueen (eraQueen-master)
- 344 ERB files
- 0 parse errors, 0 unknown built-ins
- Clean scan — no user-defined unknown calls detected

### eraAkumaMaid — English translation (eraAkumaMaid-game-eng-translation)
- 892 ERB files
- 0 parse errors, 2 unknown tokens
- Unknowns:
  - TR_NAME (91 uses) — translation helper function present in original JP but missing from
    English mod; game-side issue, not an engine gap
  - VIG (1 use) — user-defined function
- Status: ⚠️ Should load; TR_NAME calls will silently fail unless the English mod adds the function

### erauma (erauma-master)
- 0 ERB files — era-electron JS game
- Contains 193 CSV files (character/data tables in standard ERA CSV format)
- CSV data is loadable by uEmuera; game logic requires era-electron JS runtime
- See ERA_ELECTRON_PLAN.md for integration roadmap

### ere-kanon (ere-kanon-master)
- 0 ERB files — era-electron JS game
- Kanon theme; all game logic in JavaScript
- Not compatible with C# Emuera engine
- See ERA_ELECTRON_PLAN.md for integration roadmap

## era-electron Games

erauma and ere-kanon are built for the **era-electron** runtime (JS/Electron), not C# Emuera.
They share the ERA CSV data format but use JavaScript for game logic instead of ERB scripts.
See `ERA_ELECTRON_PLAN.md` for the uEmuera integration roadmap.

## .engine/ Directory

The `.engine/` folder now contains all engine/framework sources (not games):

| Entry | Type | Notes |
|---|---|---|
| Emuera-master | C# Emuera 1.824 | Legacy reference implementation |
| emuera.em-master | C# EM+EE | Primary reference (EvilMask) |
| emuera.em.doc-master | Markdown | Feature documentation |
| era-electron-master | JavaScript/Electron | era-electron runtime source |
| ere-app-master | Mobile framework | Moved from .games/ — not a game |

`ere-app-master` was previously stored under `.games/` but is a mobile wrapper framework,
not a playable game. Relocated to `.engine/`.

## Known Rendering Limitations

Even though games parse correctly, visual rendering may have gaps:

| Feature | Status | Impact |
|---|---|---|
| `<div>` layout | Parse only, no rendering | Low — most games don't use div |
| `<clearbutton>` | Parse only | Low |
| CBG rendering | Implemented (EmuleraCbgRenderer) | ⚠️ Needs Unity Scene setup |
| Image flip (neg w/h) | Implemented (localScale) | ✅ Should work |
| srcb hover | Implemented (PointerEnter/Exit) | ✅ Should work |
| HTML_PRINT_ISLAND | Simplified (immediate display) | Low impact |

## Engine Sources Consulted

| Source | Type | Used For |
|---|---|---|
| EvilMask/emuera.em (GitLab) | C# — primary EM+EE | Command signatures, semantics |
| EvilMask/emuera.em.doc | Markdown docs | Feature descriptions |
| era-electron | JavaScript engine | Ecosystem command reference |
| 0x00000FF/Emuera (historical) | C# Emuera 1.824 | Legacy behavior reference |
