# EraUma Dependency Graph

> Generated from erauma-master source inspection · 2026-08-12  
> EraUma 3.0.00 · SDK 4.7.0 · .ere-min-version 2200

---

## Runtime entry points

```
era.bundle.js          ← ere/era-electron.js  (SDK stub; engine injects real era.* at runtime)
main.bundle.js         ← ere/main.js          (game entry; webpack bundles all #/ imports)
```

Webpack output: `dist/[name].bundle.js`  
Both target Chrome 60 (ES2015 baseline).

---

## Module system

| Mechanism | Behaviour |
|---|---|
| `#/era-electron` | SDK alias → `ere/era-electron.js` (replaced by engine injection) |
| `#/path/module` | Internal alias → `ere/path/module.js` |
| `./relative` | Standard relative require |
| `.kojo` files | Transformed by `kojo-loader` at webpack build time |
| Node built-ins | **Not used in game JS** (used only by webpack build tools) |
| npm packages | **Not required at runtime** — `@prisma/client` is build-only |

**Critical insight for embedded host implementation:**  
The deployed game is a pre-built webpack bundle. The embedded host needs to:
1. Load `dist/era.bundle.js` (provides `window._era` stub)
2. Inject real `era.*` API implementations into `window._era`
3. Load `dist/main.bundle.js`

No live CommonJS resolution is required for bundled distributions.

---

## ERA SDK dependency (era.*)

| Priority | APIs | Call count |
|---|---|---|
| P0 (required for any gameplay) | printAndWait, get, println, printButton, input, set, print, drawLine, add, waitAnyKey | 39,604 |
| P1 (needed for standard progression) | printMultiColumns, clear, printInColRows, getLineCount, logger.info, setAlign | 333 |
| P2 (needed for full feature set) | getCharactersInTrain, setWidth, setOffset, setHorizontalAlign, getAddedCharacters, setVerticalAlign, setToBottom, checkImage, version, delay, saveData, setColor, playMusic, printLineChart | 151 |
| P3 (edge cases, training, admin) | replaceInColRows, printWholeImage, stopMusic, saveGlobal, loadData, notify, rmData, getAllCharacters, endTrain, setOverlay, quit, resetData, beginTrain, addCharacterForTrain, addCharacter, replaceText | 34 |

---

## Static data files

| Type | Count | Format | Notes |
|---|---|---|---|
| CSV | 193 | ERA CSV format | Character, item, skill, race tables |
| static.json | 1 | JSON (~920 KB) | Pre-built game data; exact schema unknown |

---

## Build system

```
pnpm install
node ci/build.js          ← desktop build
node ci/android.js        ← android bundle
```

Build toolchain (devDependencies, NOT runtime):

| Package | Role |
|---|---|
| `ere-webpack-plugin` | ERE entry/output naming, version injection |
| `kojo-loader` | Transforms `.kojo` dialogue files to JS |
| `webpack` 5 | Bundler |
| `babel-loader` + `@babel/preset-env` | ES2015 transpilation for Chrome 60 |
| `@prisma/client` + `prisma` | Database schema (game data, build-time only) |
| `archiver` + `7zip-bin` | Release packaging |

---

## Distribution packages

| Package | Contents |
|---|---|
| `erauma-with-engine-<ver>-win-x64.7z` | Game + era-electron runtime (desktop all-in-one) |
| `erauma-<ver>.7z` | Game files only (requires separate engine) |
| `erauma-android-<ver>.zip` | Android bundle |

---

## Git submodules

| Path | Source |
|---|---|
| `engine/` | `gitgud.io/umaera/engine/era-electron.git` (SDK) |
| `common/` | `gitgud.io/umaera/data/uma-common.git` |
| `res/` | `gitgud.io/umaera/data/uma-resource.git` (resource pack) |

**Note:** `engine/` submodule contains the actual era-electron SDK (not the stub in `ere/`).  
**Note:** `res/` is the large image resource pack — separate from the game logic.

---

## uEmuera implementation order (derived from call frequency)

1. **WebView host + SDK injection** — must load before any game JS runs
2. **era.printAndWait** (21,359 calls) — async text print + any-key wait
3. **era.get / era.set / era.add** (8,615 calls) — ERA data table access
4. **era.println / era.print** (5,156 calls) — basic text output
5. **era.printButton** (4,054 calls) — clickable buttons with accelerator
6. **era.input** (3,412 calls) — async player input
7. **era.drawLine** (869 calls) — visual divider
8. **era.waitAnyKey** (388 calls) — pause for any key
9. **era.printMultiColumns / era.printInColRows** (152 calls) — layout
10. **era.clear** (84 calls) — clear display lines
11. **Character APIs** (getAddedCharacters, getCharactersInTrain, etc.)
12. **Save/load** (saveData, loadData, rmData, saveGlobal)
13. **Audio** (playMusic, stopMusic)
14. **Charts** (printLineChart — Chart.js, render-in-browser)
15. **Image APIs** (printWholeImage, setBack, setOverlay)
