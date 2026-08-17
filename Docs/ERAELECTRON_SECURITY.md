# EraElectron Security Model

> Phase 8 · 2026-08-12

---

## Threat model

EraElectron games are JavaScript packages distributed by third parties.  
Treat every game package as **untrusted code** that must not escape the sandbox.

---

## Default permissions (local gameplay)

These are granted automatically:

| Permission | Granted | Scope |
|---|---|---|
| `game.files.read` | ✅ | Game root and resource pack only |
| `game.resources` | ✅ | Declared CSV resources |
| `game.save.read` | ✅ | Game-specific save namespace |
| `game.save.write` | ✅ | Game-specific save namespace |

---

## Permissions requiring explicit grant

| Permission | Default | Prompt |
|---|---|---|
| `game.network` | Block | Ask per game |
| `game.clipboard.read` | Block | Ask |
| `game.clipboard.write` | Block | Ask |
| `game.externalLinks` | Block | Bridge via system browser |
| `game.notifications` | Allow | Silent |
| `game.files.write` (outside save) | Block | Never auto-grant |

---

## Hard denials (never grant)

The embedded runtime must not expose:

```
child_process / shell execution
native DLL loading
environment variables
host filesystem outside game root + save
registry
process enumeration
arbitrary executable launch
```

Classify attempts as:
- `EmbeddedHost_Unsupported` — capability not in embedded sandbox
- `SidecarRequired` — available in official EraElectron sidecar mode
- `PermissionDenied` — blocked by policy

---

## WebView origin isolation

Game JS runs in an isolated origin — never `file://`.

Platform implementations:
- **Windows WebView2:** `SetVirtualHostNameToFolderMapping` on `ere-game://`
- **Android WebView:** `WebViewAssetLoader` with custom `PathHandler`
- **Linux/CEF:** Custom resource handler

Rules:
- Game JS may not navigate the WebView to an external URL
- External URL clicks must open the system browser via `IExternalLinkService`
- CSP must deny `script-src 'unsafe-eval'` unless the game explicitly requires it

---

## Save namespace isolation

Save keys are prefixed by `GameDescriptor.SaveNamespace` (SHA256 of game path).  
Emuera saves and EraElectron saves are in separate namespaces.  
Different EraElectron games cannot read each other's saves.

---

## Sidecar trust level

The official EraElectron sidecar operates outside uEmuera's embedded sandbox.  
It has broader Node.js capabilities.  
Users launching in sidecar mode must be notified that:

> This game is running in an external process with broader system access.

Sidecar is a user-opt-in fallback, not the default.

---

## Network policy

Per-game setting:

```
Allow  — game may make network requests (no external in default profile)
Ask    — prompt user on first network request
Block  — silently block all outgoing requests (default)
```

Remote image resources require `game.network = Allow`.

---

## Android

- No `allowUniversalAccessFromFileURLs`
- No `allowFileAccess` globally
- `setAllowContentAccess(false)` unless needed for specific bridge
- File access via controlled `WebViewAssetLoader` only
