# EraElectron Testing Strategy

> Phase 8 · 2026-08-12

---

## Test levels

```
Unit tests (EditMode)
  └── EreDataModelTests.cs       — VarAddress, get/set/add, CSV parser
  └── (future) EreApiDispatcherTests.cs
  └── (future) GameDetectorTests.cs

Integration tests (EditMode)
  └── GameRuntimeManager routing
  └── EmueraRuntimeAdapter lifecycle

Reference tests (PlayMode / external)
  └── Synthetic ERE fixture
  └── Official EraElectron example
  └── EraUma milestone progression

Golden tests
  └── API response snapshots
  └── Image layer output snapshots
```

---

## Synthetic ERE fixture

Location: `CompatibilityTests/EraElectron/Synthetic/main.js`

Exercises the full P0 era.* API set:
```
isEra, version, print, println, drawLine, printButton, input,
printAndWait, set, get, add, waitAnyKey, clear, saveData, loadData,
getLineCount, setAlign, setWidth, setOffset, setColor, setToBottom, notify
```

Run order: headless Node.js → then embedded host.

**Headless test:**
```bash
node -e "const era=require('./era-electron-stub'); require('#/era-electron',era); require('./main')()"
```

---

## Official EraElectron example

Source: official EraElectron example project (from gitgud.io/umaera/engine).  
Use as feature-oriented baseline for:
- save/load
- shop
- dialogue/kojo
- training systems

Status: 🔲 MISSING — obtain example project and add to `CompatibilityTests/EraElectron/Games/`

---

## EraUma milestone tests

### M6 — Title screen

Acceptance criteria:
```
[ ] erauma-master detected as EraElectron (GameDetector)
[ ] GameRuntimeManager routes to EraElectronRuntime
[ ] Config loaded (_config.json, _fixed.json)
[ ] dist/ built OR webpack run produces bundles
[ ] era.bundle.js loads without JS errors
[ ] window._era bridge injected
[ ] main.bundle.js starts executing
[ ] era.print / era.println / era.drawLine visible
[ ] era.printButton visible
[ ] Button click → era.input resolves
[ ] Title screen interactive
```

### M7 — Core gameplay

```
[ ] New game starts
[ ] menus navigate
[ ] era.get/era.set/era.add work correctly
[ ] Characters load from Chara*.csv
[ ] Training system progresses
[ ] era.saveData writes slot
[ ] era.loadData reads slot
[ ] Return to title works
```

### M8 — Multimedia

```
[ ] Resource pack detected and mounted
[ ] era.printImage renders with correct layer order
[ ] Fallback names used when primary missing
[ ] era.printWholeImage renders
[ ] era.playMusic plays audio
[ ] era.printLineChart renders chart (browser Chart.js)
[ ] era.notify shows toast
[ ] era.setBack / era.setOverlay affect background
```

---

## API conformance tests

For each era.* API:

1. Create a minimal JS fixture that calls the API
2. Capture the return value or screen state
3. Compare against official EraElectron reference result

Template location: `CompatibilityTests/EraElectron/Reference/`

---

## Cross-runtime switch test (permanent regression)

```
Launch EraUma → play 5 minutes → launcher → launch Emuera game → play
→ launcher → launch EraUma again → play
```

Check after each switch:
- No stale JS state
- No stale ERB state
- No stale save namespace
- No texture leak
- No audio still playing
- No file handle leak

Run: 10+ times minimum; check memory before/after.

---

## Current test coverage

| Suite | Tests | Status |
|---|---|---|
| `EreDataModelTests` | 26 | ✅ Written; awaiting compile |
| EraElectron.Core | 0 | 🔲 MISSING |
| EraElectron.Reference | 0 | 🔲 MISSING (synthetic fixture exists) |
| EraUma milestone | 0 | 🔲 MISSING (blocked on WebView spike) |
| Cross-runtime switch | 0 | 🔲 MISSING (blocked on EraElectron M6) |
