# EraElectron Platform Matrix

> Phase 8 · 2026-08-12

---

## Target platforms

| Platform | Embedded host | Sidecar | Priority |
|---|---|---|---|
| Windows 10/11 | WebView2 (proposed) | Official EraElectron exe | P0 |
| Android (API 26+) | Android WebView | N/A | P0 |
| Linux (x64) | WebKitGTK or CEF | If available | P1 |
| macOS | WKWebView | If available | Deferred |
| iOS | WKWebView | N/A | Deferred |

---

## Windows

**Embedded host:** Microsoft WebView2 (Chromium-based, ships with Windows 10/11)  
**Sidecar:** User-provided official EraElectron compatible executable  
**Spike status:** Spike required (see WEB_RUNTIME_HOST.md ADR)

Requirements:
- WebView2 Runtime installed (evergreen; auto-updated via Windows Update)
- Custom virtual host for game-file origin security
- SDK injection via `CoreWebView2.ExecuteScriptAsync` before bundle load
- JS-to-C# bridge via `WebMessageReceived` event

---

## Android

**Embedded host:** `android.webkit.WebView` (system Chromium; API 26+ = Chrome 60+)  
**Min API:** 26 (Android 8.0 Oreo)  
**Spike status:** Spike required

Requirements:
- `WebViewAssetLoader` for secure game-file origin (`https://game.eraelectron/`)
- `addJavascriptInterface` for C# bridge injection (annotate with `@JavascriptInterface`)
- Scoped Storage for game import (Storage Access Framework)
- CJK IME tested (Japanese/Chinese input inside WebView)
- Android back button → launcher navigation without killing process
- Safe area insets for notch/navigation bar

**ere.app behavioral reference:**  
Test against official `ere.app` Android behavior on same device.

---

## Linux

**Embedded host:** WebKitGTK (or CEF if WebKitGTK insufficient)  
**Sidecar:** User-provided EraElectron runtime (if available on Linux)  
**Spike status:** Not yet started

Requirements:
- Case-sensitive filesystem (EraUma MUST be tested on Linux)
- GTK integration with Unity (overlay or separate window)
- Chromium-level JS (WebKitGTK JIT may differ)

---

## Unity Editor (Windows)

**Purpose:** Development and automated testing  
**Host:** Same as Windows embedded host  
**Requirement:** Works in Edit Play mode; test runner compatible

---

## IL2CPP compatibility

| Platform | IL2CPP | Notes |
|---|---|---|
| Android | Required | `EreDataModel`, `EreApiDispatcher` must compile with IL2CPP |
| Windows | Optional (Mono) | No AOT restrictions for desktop |
| Linux | Optional (Mono) | |

---

## ere.app reference

Official Android EraElectron client (`ere.app`) is the behavioral oracle for Android.  
Test each milestone against it:

| Behavior | ere.app | uEmuera Android target |
|---|---|---|
| Game package import | SAF document picker | ✅ Required |
| Resource pack import | SAF document picker | ✅ Required |
| WebView rendering | System WebView | Android WebView target |
| CJK IME | System IME | System IME inside WebView |
| Audio | Web audio | Web audio (Unity bridge fallback) |
| Save location | App-private storage | App-private storage |

---

## Browser engine baseline

EraUma webpack target: **Chrome 60** (ES2015)

| Platform | Engine | Chrome equiv. | Compatible? |
|---|---|---|---|
| Windows WebView2 | Chromium (Edge) | Latest | ✅ |
| Android WebView (API 26) | Chrome 55+ | 55+ | ✅ |
| WebKitGTK | WebKit | — | Verify |
| CEF (latest) | Chromium | Latest | ✅ |
