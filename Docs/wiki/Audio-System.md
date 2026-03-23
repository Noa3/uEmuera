# Audio System

uEmuera includes a full audio system built on Unity's AudioSource API, managed by the `AudioManager` class.

## Overview

The audio system supports:
- **Sound effects** (`PLAYSOUND` / `STOPSOUND`)
- **Background music** (`PLAYBGM` / `STOPBGM`)
- **File existence checks** (`EXISTSOUND`)
- **Volume control** (0–100 range)

## Supported Formats

| Format | Loading | Recommended |
|--------|---------|-------------|
| **WAV** | Synchronous | ✅ Yes |
| **OGG** | Asynchronous | ⚠️ May have timing issues |
| **MP3** | Asynchronous | ⚠️ May have timing issues |

> **Recommendation:** Use **WAV** format for all sound files to ensure synchronous loading and avoid timing issues.

## File Placement

Sound files should be placed in the game's `sound/` subfolder:

```
YourGame/
├── ERB/
│   └── (script files)
├── CSV/
│   └── (data files)
└── sound/
    ├── bgm01.wav
    ├── se_click.wav
    └── ...
```

Folder name matching is **case-insensitive** — `sound/`, `Sound/`, and `SOUND/` all work.

## ERA Script Usage

### Playing Sound Effects

```
PLAYSOUND se_click
PLAYSOUND se_attack, 80
```

The optional second parameter is volume (0–100, default: 100).

### Stopping Sound Effects

```
STOPSOUND
```

Stops all currently playing sound effects.

### Playing Background Music

```
PLAYBGM bgm_title
PLAYBGM bgm_battle, 60
```

BGM loops automatically. Only one BGM track plays at a time — calling `PLAYBGM` again replaces the current track.

### Stopping Background Music

```
STOPBGM
```

### Checking File Existence

```
EXISTSOUND bgm_title
PRINTFORML Sound exists: {RESULT}
```

Returns `1` if the sound file exists, `0` otherwise.

## Technical Details

- Audio is managed by the `AudioManager` MonoBehaviour
- WAV files are loaded synchronously via `AudioClip.Create`
- OGG/MP3 use Unity's `UnityWebRequestMultimedia` for async loading
- Volume is normalized from 0–100 (script) to 0.0–1.0 (Unity)
- BGM playback uses loop mode on the AudioSource
