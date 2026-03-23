# Configuration

uEmuera app settings and configuration options.

## In-App Settings

uEmuera provides several visual and rendering settings accessible from the Settings menu.

### Display Settings

| Setting | Description | Default |
|---------|-------------|---------|
| **Pixel Perfect** | Enable pixel-perfect rendering for crisp text | Off |
| **CRT Effect** | Retro CRT monitor visual effect | Off |
| **Zoom Level** | Text and UI zoom | 100% |

### Pixel Perfect Rendering

When enabled, the Pixel Perfect Camera ensures every pixel maps cleanly to screen pixels, eliminating blur. Best for:
- High-DPI / Retina displays
- Users who prefer sharp, crisp text
- Combining with the CRT effect

### CRT Post-Processing

An optional retro effect that simulates a CRT monitor:
- **Vignette** — darker corners
- **Chromatic aberration** — subtle color fringing
- **Film grain** — slight noise texture

Optimized for mobile devices with minimal performance impact.

## Window Settings (Desktop)

| Setting | Value |
|---------|-------|
| Default size | 1280 × 900 |
| Resizable | Yes |
| Fullscreen mode | Windowed (Alt+Enter to toggle) |
| Run in background | Yes |

The window remembers fullscreen state but resets to default size on launch.

## Game Configuration

uEmuera currently **does not support** editing ERA game configuration from within the app. To change game settings:

1. Locate the game's configuration file (usually `emuera.config` or similar)
2. Edit with a text editor (UTF-8 encoding)
3. Save and relaunch uEmuera

> **Note:** In-app configuration editing is planned for a future release.

## Android-Specific Settings

| Setting | Value |
|---------|-------|
| Start mode | Fullscreen |
| Orientation | Auto-rotate |
| Resizable activity | Yes (for split-screen / freeform) |
| Minimum window | 400 × 300 |
| Sustained performance | Enabled |
| Install location | External storage preferred |
