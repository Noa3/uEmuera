# Troubleshooting

Common issues and their solutions.

## Android Issues

### App can't find game files

**Problem:** The app launches but doesn't show your game folders.

**Solutions:**
1. **Android 10+**: Use app-specific storage: `Android/data/noa3.uEmuera/files/`
2. **All versions**: Check that you've granted file access permission
3. Verify game files are in `storage/emulated/0/emuera/`
4. Restart the app after placing files

### Permission denied on first launch

**Problem:** The app asks for file access but the dialog doesn't appear or is denied.

**Solution:**
1. Go to **Settings** → **Apps** → **uEmuera** → **Permissions**
2. Enable **Storage** / **Files and media** permission
3. Relaunch the app

### App crashes on launch

**Problem:** uEmuera force-closes immediately.

**Solutions:**
1. Ensure your Android version is **7.1 or later** (API 25+)
2. Check that your device supports **ARMv7** or **ARM64** architecture
3. Try reinstalling the APK
4. Check Android logcat for crash details

## Game Loading Issues

### "File encoding error" or garbled text

**Problem:** Text appears as garbled characters or the game fails to load.

**Solution:** Your game files are not UTF-8 encoded. Convert all `*.csv`, `*.ERB`, and `*.ERH` files to UTF-8:

```bash
# Linux/Mac - batch convert from Shift-JIS
find . -name "*.ERB" -exec iconv -f SHIFT_JIS -t UTF-8 {} -o {}.utf8 \; -exec mv {}.utf8 {} \;
```

### Game hangs on specific commands

**Problem:** The game freezes at certain points.

**Possible causes:**
1. The game uses commands not yet implemented in uEmuera
2. An infinite loop in the game script
3. Audio timing issue with OGG/MP3 files — switch to WAV

### Missing graphics / images not showing

**Problem:** Images referenced by the game don't display.

**Solutions:**
1. Check that image files are in the correct folder (usually `resources/`)
2. Folder names are case-insensitive, but file names should match exactly
3. Supported formats: PNG, JPG, BMP, WebP
4. Check that the `resources` folder is alongside the game's ERB folder

## Audio Issues

### Sound effects don't play

**Problem:** `PLAYSOUND` commands produce no audio.

**Solutions:**
1. Ensure sound files are in the game's `sound/` subfolder
2. Use **WAV** format (recommended)
3. Check device volume is not muted
4. Check that the file name matches exactly (without extension)

### BGM has gaps or stuttering

**Problem:** Background music has audible gaps.

**Solution:** Convert BGM files to **WAV** format. OGG/MP3 require async loading which can cause gaps.

## Desktop Issues (Windows / Linux)

### Window is too small / too large

**Problem:** The window opens at an inconvenient size.

**Solution:** The window is **resizable** — drag the edges to resize. The default size is 1280×900. You can also use `Alt+Enter` to toggle fullscreen.

### Low frame rate / stuttering

**Problem:** The app runs slowly on desktop.

**Solutions:**
1. Ensure you're using the release build (not debug/development)
2. Update your graphics drivers
3. The app uses on-demand rendering — some apparent slowness is intentional power saving

### Linux: Binary won't execute

**Problem:** The downloaded binary doesn't run.

**Solution:**
```bash
chmod +x uEmuera.x86_64
./uEmuera.x86_64
```

## Reporting Bugs

If your issue isn't listed here:

1. Check [existing issues](https://github.com/Noa3/uEmuera/issues) for duplicates
2. [Create a new issue](https://github.com/Noa3/uEmuera/issues/new) with:
   - Platform and version (e.g., "Android 13, Pixel 7")
   - uEmuera version (from releases page)
   - Steps to reproduce the issue
   - The ERA game name (if game-specific)
   - Any error messages or screenshots
