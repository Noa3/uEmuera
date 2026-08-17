# ADR: Web Runtime Host for EraElectron Embedded Mode

**Status:** PROPOSED — technical spike required before final decision  
**Date:** 2026-08-12  
**Deciders:** uEmuera maintainers  
**Supersedes:** ERA_ELECTRON_PLAN.md (stale)

---

## Context

EraElectron games (EraUma, ereKanon) are webpack-bundled Vue + Element Plus +
Chart.js web applications. The deployed game ships as two JavaScript bundles:

```
dist/era.bundle.js      ← SDK stub; engine injects era.* at runtime
dist/main.bundle.js     ← game logic; imports are resolved by webpack
```

The embedded host must:

1. Provide a full browser surface (Chromium-level) for Vue / Element Plus / Chart.js
2. Let uEmuera inject the `era.*` API implementations via a native bridge
3. Serve game files over a controlled same-origin URL scheme
4. Run on Windows, Android, and Linux (first-class targets)
5. Support Web Workers (EraUma potential usage)
6. Support IME (Japanese / Simplified Chinese primary)
7. Be IL2CPP compatible for Android builds
8. Allow Unity to keep the launcher UI rendered on top

Game webpack target: `chrome: '60'` (ES2015 baseline).

---

## Decision drivers

| Requirement | Weight |
|---|---|
| Vue 3 + Element Plus + Chart.js render | Critical |
| Windows + Android + Linux | Critical |
| IL2CPP compatibility | Critical |
| SDK injection bridge | Critical |
| Web Workers | High |
| IME (CJK) | High |
| Secure game-file serving (no raw file://) | High |
| Unity overlay / launcher coexistence | High |
| Open source or source-available | Medium |
| Bundle size (especially Android) | Medium |
| Maintenance / longevity | Medium |
| Cost | Low |

---

## Options evaluated

### Option A: Vuplex 3D WebView

- **Technology:** Chromium-based WebView, Unity native plugin
- **Platforms:** Windows, macOS, Android, iOS, WebGL (via browser iframe), UWP
- **Linux:** NOT supported natively (major gap)
- **IL2CPP:** Supported
- **Web Workers:** Supported (Chromium)
- **IME:** Supported
- **Native bridge:** `WebViewPrefab.SendMessage` / `PostMessage` JS↔C# bridge
- **Serving:** Custom URL scheme; virtual asset loader
- **License:** Commercial (asset store, per-seat)
- **Vue/Element Plus/Chart.js:** Yes (Chrome-engine)
- **Gaps:** Linux not supported

### Option B: ZFBrowser (Coherent / Zen Fulcrum)

- **Technology:** Chromium Embedded Framework (CEF)
- **Platforms:** Windows, macOS, Linux (desktop only)
- **Android:** NOT supported
- **IL2CPP:** Desktop only; not relevant for Android
- **Web Workers:** Yes (Chromium)
- **IME:** Partial
- **Native bridge:** JS-to-C# function binding
- **License:** Commercial
- **Gaps:** No Android

### Option C: UniWebView

- **Technology:** System WebKit (iOS) / Android WebView
- **Platforms:** iOS, Android (mobile only)
- **Windows/Linux:** NOT supported (mobile only)
- **IL2CPP:** Supported
- **Web Workers:** Depends on system WebView version
- **IME:** System IME
- **Native bridge:** `UniWebViewMessage` scheme
- **License:** Commercial
- **Gaps:** No Windows/Linux; system WebView age varies by device

### Option D: Android WebView (direct, no Unity plugin)

- **Technology:** Android `android.webkit.WebView`
- **Platforms:** Android only
- **Unity integration:** Requires manual Android plugin development
- **IL2CPP:** Yes (native Android)
- **Web Workers:** Supported in modern Android WebView (Chrome 60+)
- **IME:** System IME
- **Native bridge:** `addJavascriptInterface` / `evaluateJavascript`
- **License:** Free (platform-provided)
- **Gaps:** Android-only; manual Unity integration work

### Option E: Microsoft WebView2

- **Technology:** Evergreen Chromium (Edge WebView2)
- **Platforms:** Windows 10/11 only
- **Android/Linux:** NOT supported
- **IL2CPP:** No Unity plugin exists (requires custom native bridge)
- **Gaps:** Windows-only; no Unity integration out of box

### Option F: Headless Chromium / Puppeteer bridge

- **Technology:** Standalone Chromium process, piped via IPC
- **Platforms:** Windows, Linux (where Chromium is available)
- **Android:** Not practical
- **Unity integration:** Custom IPC layer required
- **Gaps:** High complexity; no Android; high memory

### Option G: System WebView overlay (platform-native)

- **Technology:** `android.webkit.WebView` (Android), `Microsoft.Web.WebView2` (Windows), 
  `WebKitGTK` or `CEF` (Linux)
- **Unity integration:** Platform-specific overlay; Unity renders launcher beneath/above
- **IL2CPP:** Platform-dependent
- **Advantage:** Best CJK/IME, best memory efficiency, no third-party plugin cost
- **Gaps:** Different APIs per platform; requires more integration work
- **Feasibility:** High if accept per-platform implementation

---

## Constraints discovered from EraUma source

| Constraint | Source |
|---|---|
| Chrome 60 minimum target | `webpack.config.js targets: { chrome: '60' }` |
| ES2015 minimum (no legacy transpile needed) | babel preset-env |
| Web Workers potentially used | Per spec Rule 43 (audit pending) |
| No Node built-ins in game JS | Source scan confirmed |
| Webpack-bundled deploy — no live require() | Architecture confirmed |
| ERA SDK injected at runtime | `window._era` pattern in SDK stub |

---

## Comparison matrix

| Criterion | Vuplex | ZFBrowser | UniWebView | Android native | System overlay |
|---|---|---|---|---|---|
| Windows | ✅ | ✅ | ❌ | ❌ | ✅ (WebView2) |
| Linux | ❌ | ✅ | ❌ | ❌ | ✅ (WebKitGTK/CEF) |
| Android | ✅ | ❌ | ✅ | ✅ | ✅ |
| IL2CPP | ✅ | N/A | ✅ | ✅ | Platform-dependent |
| Vue/Element Plus | ✅ | ✅ | ✅ | ✅ | ✅ |
| Web Workers | ✅ | ✅ | Depends | ✅ | Depends |
| IME (CJK) | ✅ | Partial | ✅ | ✅ | ✅ |
| Native bridge | ✅ | ✅ | ✅ | Custom | Platform API |
| SDK injection | ✅ | ✅ | ✅ | ✅ | ✅ |
| Secure origin | ✅ | ✅ | Custom | Custom | Custom |
| Open source | ❌ | ❌ | ❌ | ✅ | ✅ |
| Linux gap | ❌ | — | — | — | ✅ |
| Cost | Paid | Paid | Paid | Free | Free |

---

## Proposed decision

**Primary: Platform-native system WebView overlay (per-platform implementation)**

Rationale:
- Only option that covers Windows + Android + Linux without a single missing platform
- Best IME and CJK text rendering (system-level)
- Best memory efficiency (no duplicate Chromium process outside system WebView)
- Free; no third-party licensing obligations
- Direct access to platform security/permission model
- Android WebView is already Chromium-based and supports Chrome 60+ on modern devices

Implementation path:
- **Windows:** Microsoft WebView2 (ships with Windows 10/11; evergreen Chromium)
- **Android:** `android.webkit.WebView` with `WebViewClient` / `WebChromeClient`
- **Linux:** WebKitGTK or host-installed Chromium via CEF/subprocess

**Secondary / fallback: Vuplex 3D WebView**  
If system WebView integration cost is too high, Vuplex provides a single Unity API
across Windows and Android with proven Vue support. Linux would be deferred.
Cannot be the first choice because Linux is a first-class target.

**Not chosen:**
- ZFBrowser: No Android
- UniWebView: No Windows/Linux
- WebView2 standalone: No Android/Linux
- Headless Chromium: Too complex, no Android

---

## SDK injection mechanism

Regardless of host technology, the injection pattern is:

```javascript
// Injected by uEmuera bridge before game JS loads:
window._era = {
  printAndWait: async (content, config) => { /* C# bridge call */ },
  get:          (varName)              => { /* C# bridge call */ },
  // ... all 56 APIs ...
};
```

The bridge call must be:
- Synchronous for sync APIs (no await on C# side)
- Promise-resolving for async APIs (C# resolves Promise when input arrives)

---

## Security model for game files

Use virtual asset loader or custom URL scheme, not `file://`:

- **Windows WebView2:** `SetVirtualHostNameToFolderMapping` or custom `ICoreWebView2WebResourceRequested`
- **Android WebView:** `WebViewAssetLoader` with custom `PathHandler`
- **CEF/Linux:** Custom resource handler on `app://` or `ere-game://` scheme

Never enable `allowUniversalAccessFromFileURLs` or `allowFileAccess`.

---

## Spike required before finalizing

Before implementing, validate on real hardware:

1. **Windows:** WebView2 + Vue 3 + Element Plus + Chart.js + Web Workers
2. **Android:** Android WebView + same stack + CJK IME input
3. **Linux:** WebKitGTK or CEF + same stack

Success criteria:
- EraUma `main.bundle.js` loads without JS errors
- Vue components render visually
- era.* bridge calls resolve correctly
- `await era.input()` suspends game JS until C# resolves the Promise
- CJK text input works

Until spike completes, `EraElectronRuntime` remains a stub.

---

## Consequences

- Platform-specific WebView integration code required (not a single Unity plugin)
- Bridge API must be specified before implementation (see ERAELECTRON_RUNTIME.md ADR)
- Sidecar mode (official EraElectron executable) remains the fallback for capabilities the embedded host cannot yet provide
- Linux WebKitGTK or CEF selection deferred until spike
