# EraElectron Game Compatibility

> Generated: 2026-08-12  
> Based on direct source inspection of locally available games.

---

## Summary

| Game | Runtime | Version | uEmuera Detection | uEmuera Playability |
|---|---|---|---|---|
| erauma-master | EraElectron | 3.0.00 | DETECTED (provisional) | MISSING (WebView spike pending) |
| ere-kanon-master | EraElectron | 116 | DETECTED (provisional) | MISSING |
| eraTohoK-master | Emuera | — | DETECTED (Certain) | VERIFIED (Fast boot 2.1 s) |
| Other .games/* | Emuera | — | DETECTED | WORKING |

---

## EraUma (erauma-master v3.0.00)

| Check | Result | Notes |
|---|---|---|
| Package detected | ✅ | `EraElectronGameDetector` finds `ere.config.json` + `main.js` heuristics |
| Detection confidence | PROVISIONAL | Fingerprints not yet validated per Rule 9 |
| ERE min version | 2200 | `.ere-min-version` file |
| CSV loaded | 193 files | Standard ERA CSV format |
| JS entry point | `ere/main.js` (source) | `dist/main.bundle.js` absent (must build) |
| SDK version | 4.7.0 | `era.version.sdk` in `ere/era-electron.js` |
| Node requirement | >= 18 | `package.json engines.node` |
| Game launched | ❌ | EraElectronRuntime is a stub; WebView spike not done |
| Title screen | ❌ | Blocked on WebView spike |
| API coverage | 37/56 APIs used | All 37 are MISSING; P0 set not implemented |

### EraUma P0 API requirements

| API | Call sites | uEmuera status |
|---|---|---|
| `era.printAndWait` | 21,359 | MISSING |
| `era.get` | 5,809 | MISSING |
| `era.println` | 4,266 | MISSING |
| `era.printButton` | 4,054 | MISSING |
| `era.input` | 3,412 | MISSING |
| `era.set` | 2,244 | MISSING |
| `era.print` | 890 | MISSING |
| `era.drawLine` | 869 | MISSING |
| `era.add` | 562 | MISSING |
| `era.waitAnyKey` | 388 | MISSING |

---

## ereKanon (ere-kanon-master v116)

| Check | Result |
|---|---|
| Package detected | ✅ |
| ERE min version | 110 |
| APIs used | 30 distinct era.* APIs |
| Game launched | ❌ |

---

## Emuera games

All Emuera games in `.games/` are detected by `EmueraGameDetector` with `Certain` confidence.

| Game | Status |
|---|---|
| eraTohoK-master | VERIFIED — Fast boot 2.1 s (warm cache) |
| Other eraToho* | WORKING |
| eraAS, eraFL, etc. | WORKING |

---

## Compatibility timeline

| Phase | Target | Status |
|---|---|---|
| M6 | EraUma title screen | 🔲 MISSING — WebView spike first |
| M7 | EraUma core gameplay | 🔲 MISSING |
| M8 | EraUma multimedia | 🔲 MISSING |
| M12 | ereKanon substantially playable | 🔲 MISSING |
| M15 | Emuera Auto→Fast enabled | 🔲 MISSING — differential gate |

---

## Detection fingerprint status

EraElectron fingerprints in `EraElectronGameDetector.cs` are PROVISIONAL.  
Must be validated against real game packages per Rule 9 / MILESTONE 2.  
Current known indicators:
- `ere.config.json` → Medium confidence
- `main.js` or `dist/main.js` → Low confidence (could be non-ERE project)
- Both present → High confidence (PROVISIONAL)

Validation required: inspect real bundled EraUma distribution (`.7z`) to confirm fingerprints.
