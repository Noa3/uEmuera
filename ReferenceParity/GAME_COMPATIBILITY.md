# uEmuera — Game Compatibility Report

Generated: 2026-08-09  
Engine: uEmuera (Unity 6 port of Emuera/Emuera.EM+EE)  
Scan tool: CompatibilityScanner (built-in, ground-truth from engine registries)

## Summary

All tested games parse without errors (0 file errors on ERB scanning).
Remaining "unknown" tokens are user-defined functions, not missing built-ins.

| Game | ERB Files | Parse Errors | Unknown Tokens | Status |
|---|---|---|---|---|
| anon-tw (English mod) | 3,896 | 0 | 189 total / 0 built-in missing | ✅ Should load |
| eraNAS | 3,321 | 0 | 213 total / 0 built-in missing | ✅ Should load |
| eratohoTW (English) | 1,533 | 0 | 76 total / 0 built-in missing | ✅ Should load |
| erauma | 0 ERB | — | — | ⚠️ No ERB files (CSV only — character data pack) |

Note: "unknown tokens" are call-like identifiers `FOO(...)` that don't match any registered
instruction or method. In all games these are user-defined `@FOO` functions, not missing built-ins.

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
