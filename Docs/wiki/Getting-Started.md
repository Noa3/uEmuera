# Getting Started

This guide covers installing and running uEmuera on all supported platforms.

## Requirements

- An ERA script game folder (UTF-8 encoded files)
- A supported platform:
  - **Windows** 10 or later (x64)
  - **Linux** x64 (Ubuntu 20.04+, or equivalent)
  - **Android** 7.1+ (API 25+)

## Installation

### Windows

1. Download the latest `.zip` from [Releases](https://github.com/Noa3/uEmuera/releases)
2. Extract the archive to any location
3. Run `uEmuera.exe`
4. The app starts in a **resizable windowed mode** (1280×900 default)

### Linux

1. Download the latest Linux `.zip` from [Releases](https://github.com/Noa3/uEmuera/releases)
2. Extract the archive
3. Make the binary executable:
   ```bash
   chmod +x uEmuera.x86_64
   ```
4. Run:
   ```bash
   ./uEmuera.x86_64
   ```
5. The app starts in a **resizable windowed mode** (1280×900 default)

### Android

1. Download the latest `.apk` from [Releases](https://github.com/Noa3/uEmuera/releases)
2. Enable "Install from unknown sources" in your device settings
3. Install the APK
4. Grant **file access** permission on first launch

## Setting Up Game Files

### File Encoding

**Critical:** All ERA-related files must be **UTF-8** encoded:
- `*.csv` files
- `*.ERB` files (ERA scripts)
- `*.ERH` files (ERA headers)

If your game files are in Shift-JIS or other encodings, convert them to UTF-8 first.

### Game Folder Placement

#### Windows / Linux
- Place game folders in the same directory as the executable, OR
- Use the in-app file browser to select a folder

#### Android
Place game folders in one of these locations:

| Path | Notes |
|------|-------|
| `storage/emulated/0/emuera/` | Primary external storage |
| `Android/data/noa3.uEmuera/files/` | App-specific storage (Android 10+) |

> **Android 10+ Tip:** If the app can't find files in `sdcard/uEmuera`, use the app-specific storage path instead.

## First Launch

1. The app displays a file selection screen
2. Navigate to your ERA game folder
3. Select the folder containing `.ERB` files
4. The game loads and displays the text interface

## Controls

| Action | Desktop | Android |
|--------|---------|---------|
| Text input | Keyboard | On-screen keyboard |
| Scroll | Mouse wheel / Page Up/Down | Swipe up/down |
| Quick buttons | Click | Tap |
| Zoom | Settings menu | Pinch gesture / Settings |
