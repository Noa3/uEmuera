# Game Compatibility Issue Explanation

This document explains compatibility considerations when running era games with uEmuera and differences from the original Windows Emuera.

## Overview

uEmuera is a Unity3D port of Emuera designed to enable cross-platform support, particularly for mobile devices. While it aims for high compatibility with era script games, there are some differences to be aware of.

## Platform Differences

### File Encoding

**Requirement**: All era-related files must be encoded in UTF-8, including:
- `*.csv` files
- `*.ERB` files  
- `*.ERH` files

The original Windows Emuera typically uses Shift-JIS (Code Page 932) encoding. Games must be converted to UTF-8 before use with uEmuera.

### File Location

On Android, game files should be placed in one of these locations:
- `storage/emulated/0/emuera`
- `storage/emulated/1/emuera`
- `storage/emulated/2/emuera`

**Note for Android 10+**: If files in `sdcard/uEmuera` are not found, try placing them in:
`sdcard/Android/data/xerysherry.uEmuera/files/`

## Known Compatibility Issues

### 1. Graphics Instructions

Some GXX-related drawing instructions are not fully implemented. Games heavily relying on advanced graphics features may have display issues.

#### Partially Implemented GXX Instructions

The following graphics instructions exist but have limited or no actual implementation:

| Instruction | Status | Description |
|-------------|--------|-------------|
| `GCREATE` | ✅ Works | Creates a graphics buffer with specified dimensions |
| `GCREATEFROMFILE` | ✅ Works | Creates a graphics buffer from an image file |
| `GDISPOSE` | ✅ Works | Disposes of a graphics buffer |
| `GCREATED` | ✅ Works | Checks if a graphics buffer exists |
| `GWIDTH` | ✅ Works | Returns graphics buffer width |
| `GHEIGHT` | ✅ Works | Returns graphics buffer height |
| `GCLEAR` | ⚠️ Stub | Clears graphics buffer with color (no visual effect) |
| `GFILLRECTANGLE` | ⚠️ Stub | Fills rectangle with color (no visual effect) |
| `GDRAWG` | ⚠️ Stub | Draws one graphics buffer to another (no visual effect) |
| `GDRAWGWITHMASK` | ⚠️ Stub | Draws with mask (no visual effect) |
| `GDRAWSPRITE` | ⚠️ Stub | Draws sprite to graphics buffer (no visual effect) |
| `GSETCOLOR` | ⚠️ Stub | Sets pixel color (no visual effect) |
| `GGETCOLOR` | ⚠️ Partial | Gets pixel color (returns default values instead of actual pixel colors) |
| `GSETBRUSH` | ⚠️ Stub | Sets brush for drawing (no effect) |
| `GSETFONT` | ⚠️ Stub | Sets font for text drawing (no effect) |
| `GSETPEN` | ⚠️ Stub | Sets pen for line drawing (no effect) |

**Legend:**
- ✅ Works: Instruction functions as expected
- ⚠️ Stub: Instruction is recognized but does not perform actual drawing operations
- ⚠️ Partial: Instruction has limited functionality

#### Impact on Games

Games that use these graphics instructions for:
- Dynamic image generation
- Custom UI rendering
- Image manipulation and compositing
- Text-to-image conversion

May experience missing graphics, blank areas, or incorrect visual output.

### 2. Configuration Modification

In-app modification of era game configuration is not currently supported. Configuration changes must be made by editing the `emuera.config` file directly.

### 3. Debugging

Debug functionality is limited in the Unity port. The full debugging experience of the Windows Emuera is not available.

### 4. Performance

- Some game instructions have lower efficiency than the original, which may cause lag on less powerful devices
- Unity3D applications typically consume more battery than native applications

## Compatibility Configuration Options

Several configuration options can help resolve compatibility issues:

| Option | Purpose |
|--------|---------|
| `CompatiRAND` | Match RAND pseudo-variable behavior to eramaker |
| `CompatiLinefeedAs1739` | Reproduce pre-v1739 non-button line break behavior |
| `CompatiErrorLine` | Continue execution even with unparseable lines |
| `CompatiCALLNAME` | Assign NAME when CALLNAME is empty string |
| `CompatiFunctionNoignoreCase` | Don't ignore case for functions/attributes |

## Troubleshooting

### Game Not Loading

1. Verify all files are UTF-8 encoded
2. Check file permissions on Android
3. Ensure the game folder contains `emuera.config` or `ERB` directory

### Display Issues

1. Try adjusting resolution settings in the app menu
2. Check if the game uses unsupported GXX instructions (see Graphics Instructions section above)
3. Adjust font settings if text appears incorrectly
4. If graphics appear missing or blank, the game may rely on unimplemented GXX drawing functions

### Performance Problems

1. Reduce log history line count in configuration
2. Close other applications to free memory
3. Try lower resolution settings

## Reporting Issues

When reporting compatibility issues, please include:

1. The specific era game name and version
2. Steps to reproduce the issue
3. Any error messages displayed
4. Device and OS information
5. uEmuera version number

## Additional Resources

- [uEmuera GitHub Repository](https://github.com/noa3/uEmuera)
- [EMUERA_EXTENDED_COMPATIBILITY.md](./EMUERA_EXTENDED_COMPATIBILITY.md) - Technical compatibility details

## Version Notes

This document reflects uEmuera based on emuera1824v15. Compatibility may change with future updates.
