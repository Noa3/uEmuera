# Multi-Runtime Game Lifecycle

> Phase 8 · 2026-08-12

---

## Session model

One session = one active runtime.  
Sessions are identified by a monotonically-incrementing integer (`GameSession`).  
All async work (image loads, bridge callbacks, timer ticks) carries the session ID it
was created in; work from a dead session is silently discarded.

---

## Launch sequence

```
User taps game card
      ↓
GameRuntimeManager.LaunchAsync(descriptor, context)
      ↓
[if current session running]
    StopCurrentAsync()
        → runtime.StopAsync()
        → GameSession.Bump()           ← invalidates old async work
        → runtime.Dispose()
      ↓
CreateRuntime(descriptor.RuntimeKind)
      ↓
runtime.InitializeAsync(descriptor, context)
      ↓
runtime.StartAsync()
      ↓
[game running]
```

---

## Stop sequence (return to launcher)

```
User taps "Return to Launcher" / game calls quit
      ↓
GameRuntimeManager.StopCurrentAsync()
      ↓
runtime.StopAsync()
    [Emuera]
        EmueraMain.Clear()
        → GameSession.Bump()
        → EmueraThread.End()
        → AppContents.UnloadContents()
        → SpriteManager.ForceClear()
        → Resources.UnloadUnusedAssets()
        → FirstWindow.Show()  (temporary; will be replaced by launcher callback)
    [EraElectron]
        host.StopAsync()
        → close WebView / terminate sidecar
        → EreDataModel.Clear()
        → session invalidated
      ↓
runtime.Dispose()
      ↓
[launcher visible, ready for next game]
```

---

## Game switch (A → B)

```
Game A running
      ↓
User selects Game B
      ↓
GameRuntimeManager.LaunchAsync(B)
      ↓
StopCurrentAsync()  [Game A teardown, see Stop sequence]
      ↓
CreateRuntime(B.RuntimeKind)  [different runtime from A]
      ↓
B.InitializeAsync()
      ↓
B.StartAsync()
      ↓
[Game B running; no state from A survives]
```

### Cross-runtime switch guarantees

After switching from Emuera → EraElectron (or vice versa):

| Asset type | Guarantee |
|---|---|
| ERB label dictionary | Cleared by `GlobalStatic.Reset()` |
| Emuera variable data | Cleared by `ConfigData.Instance.Clear()` |
| Sprites / textures | Cleared by `SpriteManager.ForceClear()` |
| Background images | Cleared by `AppContents.UnloadContents()` |
| EraElectron JS heap | Destroyed with WebView context |
| EraElectron ERA data | `EreDataModel` disposed |
| Save namespace | Isolated per `GameDescriptor.SaveNamespace` |
| Audio | Stopped before new runtime starts |
| Pending async callbacks | Invalidated by `GameSession.Bump()` |
| File handles | Closed by runtime `StopAsync()` |

---

## Suspend / resume (app backgrounded)

```
App → background
      ↓
GameRuntimeManager.SuspendCurrentAsync()  [TODO M8]
      ↓
[Emuera]    EmueraThread pauses input processing
[EraElectron] host.Hide(); audio paused

App → foreground
      ↓
GameRuntimeManager.ResumeCurrentAsync()
      ↓
[Emuera]    EmueraThread resumes
[EraElectron] host.Show(); audio resumed
```

---

## Session ID usage

`GameSession.Current` is an integer that increments on every `Bump()`.

Each long-lived async operation (SpriteManager image load, EraElectron bridge call,
BackgroundErbLoader batch) captures the session ID at creation time and calls
`GameSession.IsValid(capturedId)` before touching live state.

If the game was restarted or switched while the operation was in flight,
`IsValid` returns false and the result is silently dropped.

---

## Pending work items

| Item | Milestone |
|---|---|
| `GameSessionCoordinator` unified class | M8 |
| SuspendAsync / ResumeAsync full implementation | M8 |
| EraElectron session guard in bridge callbacks | M5 |
| Android back-button → launcher navigation | M11 |
| Cross-runtime regression test | CI gate |
