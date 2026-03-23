# FAQ — Frequently Asked Questions

## General

### What is uEmuera?

uEmuera is a Unity 6 port of [Emuera](https://osdn.net/projects/emuera/), a text-based game engine that runs ERA script games. It brings Emuera to Windows, Linux, and Android.

### What are ERA script games?

ERA games are text-based games written in the ERA scripting language, originally run by the Eramaker engine. Emuera (and by extension uEmuera) is a modern replacement that runs these games.

### Is uEmuera free?

Yes! uEmuera is open-source under the [Apache License 2.0](https://github.com/Noa3/uEmuera/blob/main/LICENSE).

### Where do I get ERA games?

ERA games are created by their respective communities. uEmuera is only the engine — you need to obtain game files separately.

## Installation

### Where do I put game files on Android?

Two locations work:
1. `storage/emulated/0/emuera/` — standard location
2. `Android/data/noa3.uEmuera/files/` — app-specific storage (required on Android 10+)

### The app can't find my files on Android 10+

Android 10 introduced scoped storage restrictions. Use the app-specific path: `Android/data/noa3.uEmuera/files/`

### My game files are in Shift-JIS encoding. Will they work?

No. uEmuera requires **UTF-8** encoded files. Convert your `*.csv`, `*.ERB`, and `*.ERH` files to UTF-8 before use.

### How do I convert files to UTF-8?

Use a text editor that supports encoding conversion:
- **Notepad++**: Encoding → Convert to UTF-8
- **VS Code**: Click encoding in status bar → Reopen with → Save with UTF-8
- **Command line**: `iconv -f SHIFT_JIS -t UTF-8 input.erb -o output.erb`

## Audio

### Which audio format should I use?

**WAV** is strongly recommended. It loads synchronously, avoiding timing issues.

### Why don't OGG/MP3 files play correctly?

OGG and MP3 require asynchronous loading in Unity. This can cause timing mismatches where the game expects a sound to be ready immediately. Use WAV format to avoid this.

## Performance

### The game is running slowly. What can I do?

- Some ERA games have inefficient scripts — this is a game issue, not uEmuera
- Ensure you're using the latest release build (IL2CPP, not Mono)
- On Android, close background apps to free resources
- Try reducing the zoom level in settings

### uEmuera uses a lot of battery on Android

This is a common characteristic of Unity applications. The app uses on-demand rendering to minimize power consumption, but some battery usage is unavoidable.

## Features

### Can I change game settings in the app?

Not currently. Game configuration must be edited in the game's config files before launching.

### Is there a debug mode?

Not yet. Debug functionality is planned for a future release.

### Does uEmuera support all ERA games?

Almost all. Some games with very specific or uncommon commands may have issues. Please [file an issue](https://github.com/Noa3/uEmuera/issues) if you encounter incompatibilities.

## Development

### What Unity version do I need to build from source?

Unity 6 (6000.3.3f1 or later). See [Building from Source](Building-from-Source) for details.

### Can I contribute?

Yes! Pull requests are welcome. See the [Building from Source](Building-from-Source) guide for development setup.
