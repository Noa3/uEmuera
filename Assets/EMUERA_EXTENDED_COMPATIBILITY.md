# Emuera Extended Compatibility

This document describes the compatibility status of uEmuera with Emuera1824+v18+EMv17+EEv40.exe and related extended versions.

## Overview

uEmuera is a Unity3D port of Emuera, currently based on emuera1824v15 source code. This document tracks feature parity with newer Emuera versions.

## Current Base Version

- **uEmuera Base**: emuera1824v15
- **Target Parity**: Emuera1824+v18+EMv17+EEv40

## Feature Status

### Implemented Features

The following features from the base Emuera are implemented:

- Basic script execution and control flow
- Built-in function dispatch (PRINT*, INPUT*, WAIT, etc.)
- Character management (ADDCHARA, DELCHARA, PICKUPCHARA, etc.)
- Save/Load functionality
- Configuration handling via emuera.config
- Multiple language encoding support (Japanese, Korean, Chinese)
- Training system (DOTRAIN, CALLTRAIN)

### Configuration Options

The following configuration switches are available:

| Config Code | Description | Default |
|-------------|-------------|---------|
| IgnoreCase | Ignore case differences | true |
| CompatiRAND | Match RAND behavior to eramaker | false |
| CompatiLinefeedAs1739 | Reproduce pre-v1739 line break behavior | false |
| CompatiCallEvent | Allow CALL on event functions | false |
| CompatiSPChara | Use SP characters | false |
| CompatiFuncArgAutoConvert | Auto-complete TOSTR for user function arguments | false |
| CompatiFuncArgOptional | Allow omission of all user function arguments | false |
| SystemSaveInUTF8 | Save data in UTF-8 | false |
| SystemSaveInBinary | Save data in binary format | false |
| TimesNotRigorousCalculation | Match TIMES calculation to eramaker | false |
| SystemNoTarget | Don't auto-complete character variable arguments | false |
| SystemIgnoreTripleSymbol | Don't expand triple symbols in FORM | false |
| SystemIgnoreStringSet | Force string expression for string variable assignment | false |

### Known Differences vs Emuera1824+v18+EMv17+EEv40

#### Not Implemented (TODO)

The following features may differ from the reference version and require investigation:

1. **Debug functionality** - Limited debugging support in the Unity port
2. **In-app configuration modification** - Cannot modify era game configuration within the app

#### GXX Graphics Instructions Technical Details

The GXX graphics system in Emuera allows games to create and manipulate graphics buffers programmatically. In uEmuera, these functions are now implemented using Unity's Texture2D system for actual pixel manipulation.

**Fully Implemented:**
- `GCREATE(id, width, height)` - Creates graphics buffer with Unity Texture2D
- `GCREATEFROMFILE(id, filename)` - Creates graphics buffer from image file
- `GDISPOSE(id)` - Releases graphics buffer and Unity texture
- `GCREATED(id)` - Returns 1 if buffer exists, 0 otherwise
- `GWIDTH(id)` - Returns buffer width
- `GHEIGHT(id)` - Returns buffer height
- `GCLEAR(id, color)` - Clears buffer with specified color
- `GFILLRECTANGLE(id, x, y, width, height)` - Fills rectangle with brush color
- `GDRAWG(destId, srcId, ...)` - Copies between buffers with scaling
- `GDRAWGWITHMASK(destId, srcId, maskId, x, y)` - Draws with alpha mask blending
- `GDRAWSPRITE(id, spriteName, ...)` - Draws sprite to buffer
- `GSETCOLOR(id, color, x, y)` - Sets pixel color
- `GGETCOLOR(id, x, y)` - Gets pixel color
- `GSETBRUSH(id, color)` - Sets brush for fill operations
- `GSETFONT(id, fontName, size)` - Sets font for text drawing
- `GSETPEN(id, color)` - Sets pen for line drawing

**Implementation Notes:**
- All graphics operations use Unity's Texture2D with ARGB32 format
- Color matrix transformations are supported for advanced effects
- Y-axis is flipped to match Emuera's coordinate system (origin at top-left)

#### Behavior Notes

- uEmuera uses UTF-8 encoding by default instead of Shift-JIS
- Some platform-specific behaviors may differ due to Unity's cross-platform nature

## Configuration Switches for New Behaviors

New features should be guarded behind configuration flags defaulting to "off" to maintain backward compatibility. When adding extended compatibility features:

1. Add a new `ConfigCode` entry
2. Add corresponding `ConfigItem` in `ConfigData.setDefault()`
3. Add property in `Config.cs`
4. Document the feature here

## Testing Recommendations

When verifying compatibility:

1. Test script execution paths with various built-in functions
2. Verify save/load operations
3. Test input request modes and validation
4. Check UI prompts and forms match expected behavior
5. Ensure no regressions in existing gameplay flows

## References

- Original Emuera: [https://osdn.net/projects/emuera/](https://osdn.net/projects/emuera/)
- uEmuera Repository: [https://github.com/noa3/uEmuera](https://github.com/noa3/uEmuera)

## Version History

- Initial documentation created based on emuera1824v15 source code analysis
