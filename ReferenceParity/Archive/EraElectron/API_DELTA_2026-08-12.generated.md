# EraElectron API Delta Report

> Generated: 2026-08-12 · SDK 4.7.0 · Source: erauma-master  
> All 56 APIs have uEmuera status **MISSING** — EraElectronRuntime not yet implemented.

---

## Summary

| Category | API count | P0 | P1 | P2 | P3 |
|---|---|---|---|---|---|
| Render / layout | 23 | 9 | 3 | 8 | 3 |
| Data (get/set/char) | 12 | 3 | 0 | 5 | 4 |
| Input | 3 | 3 | 0 | 0 | 0 |
| Save | 6 | 0 | 0 | 1 | 5 |
| Audio | 3 | 0 | 0 | 1 | 2 |
| Debug / logger | 6 | 0 | 1 | 0 | 0 |
| Platform / misc | 5 | 0 | 0 | 1 | 1 |
| Timing | 1 | 0 | 0 | 1 | 0 |
| **Total** | **56** | **15** | **4** | **17** | **15** |

---

## P0 — Implement before EraUma title screen is visible

| API | Return | Async | EraUma calls | Blocker |
|---|---|---|---|---|
| `printAndWait(content, config?)` | Promise\<number\> | ✓ | 21,359 | Text output + wait |
| `get(varName)` | any | — | 5,809 | Data model |
| `println()` | number | — | 4,266 | Text output |
| `printButton(content, accel, config?)` | number | — | 4,054 | Button rendering |
| `input(config?)` | Promise\<any\> | ✓ | 3,412 | User input |
| `set(varName, val)` | T | — | 2,244 | Data model |
| `print(content, config?)` | number | — | 890 | Text output |
| `drawLine(config?)` | number | — | 869 | Divider |
| `add(varName, val)` | any | — | 562 | Data model |
| `waitAnyKey()` | Promise\<void\> | ✓ | 388 | Wait |
| `version` | object | — | 8 | Engine version check |
| `isEra` | boolean | — | — | Runtime detection |
| `clear(lineCount?)` | Promise\<number\> | ✓ | 84 | Screen clear |

---

## P1 — Required for standard progression

| API | Return | Async | EraUma calls |
|---|---|---|---|
| `printMultiColumns(cols, config?)` | number | — | 102 |
| `printInColRows(...cols)` | number | — | 50 |
| `getLineCount()` | number | — | 45 |
| `logger.info(msg)` | void | — | 34 |
| `setAlign(align)` | void | — | 22 |

---

## P2 — Full feature set

| API | EraUma calls | Notes |
|---|---|---|
| `getCharactersInTrain()` | 20 | Training system |
| `setWidth(width)` | 19 | Layout |
| `setOffset(offset)` | 19 | Layout |
| `setHorizontalAlign(align)` | 16 | Layout |
| `getAddedCharacters()` | 16 | Character tracking |
| `setVerticalAlign(align)` | 14 | Layout |
| `setToBottom()` | 12 | Scroll |
| `checkImage(..names)` | 8 | Resource validation |
| `delay(ms)` | 7 | Timing |
| `saveData(idx, comment?)` | 6 | Save |
| `setColor(color?)` | 6 | Text style |
| `playMusic(names, config?)` | 5 | Audio |
| `printLineChart(config)` | 5 | Chart.js — must render in browser |
| `setOverlay(name?, config?)` | 2 | Visual overlay |

---

## P3 — Edge cases / training / admin

| API | EraUma calls | Notes |
|---|---|---|
| `replaceInColRows(…cols)` | 4 | Column replace |
| `printWholeImage(names, config?)` | 3 | Full-size image |
| `stopMusic()` | 3 | Audio |
| `saveGlobal()` | 3 | Global save |
| `loadData(idx)` | 2 | Load |
| `notify(content, title?, type?, duration?)` | 2 | Toast |
| `rmData(idx)` | 2 | Delete save |
| `getAllCharacters()` | 2 | Character list |
| `endTrain()` | 2 | Training |
| `quit()` | 1 | Close app |
| `resetData()` | 1 | Data reset |
| `beginTrain(…ids)` | 1 | Training |
| `addCharacterForTrain(…ids)` | 1 | Training |
| `addCharacter(…ids)` | 1 | Character add |
| `replaceText(content, config?)` | 1 | Replace last line |

---

## APIs not called by EraUma (but in SDK)

These are present in SDK 4.7.0 but have zero call sites in EraUma source. Other games may use them.

```
addCharacterForTrain  checkImage (partial)  isDebug  isEra  isLandscape
loadGlobal  logger.assert  logger.debug  logger.error  logger.warn
nextTurnInTrain  printImage  printProgress  removeCharacter
resetCharacter  resetGlobal  resumeMusic  setBack  setMask (deprecated)
setTitle  toggleDebug
```

---

## Critical implementation notes

### Bundle architecture
Deployed EraUma is a webpack bundle. The embedded host does NOT need live CommonJS resolution. It needs:
1. Load `dist/era.bundle.js` → sets `window._era` stub
2. **Inject real era.* implementations** into `window._era`  
3. Load `dist/main.bundle.js`

### Source distribution note
erauma-master does NOT contain a pre-built `dist/` directory. Players get the packaged `.7z` which includes a compiled bundle. For development/testing, build with `pnpm install && webpack`.

### Async contract
- `input`, `printAndWait`, `waitAnyKey`, `clear`, `loadData`, `saveData` etc. return Promises
- Game JS uses `await` extensively (21K+ await sites for printAndWait alone)
- The event loop must process microtasks between era API calls

### Data model
- `era.get('varname:index:index')` addresses ERA variable tables  
- Same CSV format as Emuera — but ownership is separate (EraElectronRuntime owns it)
- Do NOT share VariableData with EmueraRuntime

### Charts
- `printLineChart` uses Chart.js + chartjs-plugin-annotation + vue-chartjs
- Must be rendered by the browser; do not reimplement in Unity

### Deprecated APIs
- `setMask(name, opacity)` → **replaced by `setOpacity` in newer engine versions** (not yet in SDK 4.7.0 stub)
