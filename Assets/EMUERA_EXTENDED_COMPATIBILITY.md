# Emuera Extended Compatibility Features

uEmuera now includes compatibility features found in extended Emuera variants (EM/EE versions) to improve compatibility with games designed for those versions.

## New Configuration Option

### CompatiIgnoreInvalidLine

**Config key:** `???????????????????` (Japanese)  
**Config key (English):** `CompatiIgnoreInvalidLine`  
**Default value:** `false`  
**Type:** Boolean

#### Description

When enabled, uEmuera will continue execution even when it encounters lines it cannot parse, instead of terminating. This is useful for:

1. **Games using extended commands** (like BINPUT) from modded Emuera versions
2. **Scripts with minor syntax errors** that don't affect core gameplay
3. **Debugging** - allows you to see how far a script runs before hitting critical errors

#### Usage

Add this line to your `emuera.config` file:

```
???????????????????:Yes
```

Or in English format:

```
CompatiIgnoreInvalidLine:Yes
```

#### Behavior

**When disabled (default):**
- Any unparseable line causes immediate termination
- Error message displays: "ERB????????????????Emuera??????"
- Safe for production use

**When enabled:**
- Unparseable lines are skipped with a warning
- Execution continues to next line
- Warning printed: "???[error message]????????????"
- Game may function partially even with unsupported features

#### Example

Given a script with an unknown command:

```erb
@TEST
PRINT Hello
BINPUT    ; Unknown command in standard Emuera
PRINT World
```

**Default behavior (CompatiIgnoreInvalidLine:No):**
- Game fails to load
- Shows error at BINPUT line
- Execution stops

**With CompatiIgnoreInvalidLine:Yes:**
- Game loads successfully
- Prints "Hello"
- Prints warning about BINPUT being unparseable
- Prints "World"
- Continues execution

#### Warnings

?? **Use with caution!**

Enabling this option may cause:
- **Unexpected behavior** if skipped lines were critical to game logic
- **Variable initialization issues** if custom variables aren't registered
- **Silent failures** where features simply don't work without obvious errors
- **Save/load corruption** if critical data structures aren't initialized

#### Recommended Usage

1. **Testing unknown games:**
   ```
   CompatiIgnoreInvalidLine:Yes
   ```
   Try running the game to see what features are missing

2. **Known compatible games:**
   ```
   CompatiIgnoreInvalidLine:No
   ```
   Keep default for better error detection

3. **Debugging custom scripts:**
   ```
   CompatiIgnoreInvalidLine:Yes
   ```
   See how far your script runs before hitting errors

## Future Extended Features

The following features from Emuera Extended/EM variants are planned for future implementation:

### Planned Features (Not Yet Implemented)

- [ ] **Dynamic variable registration** - Auto-create undefined sub-variables like MONEY:??AP
- [ ] **BINPUT command** - Batch input processing
- [ ] **Extended SAVE/LOAD** - Support for EE save format extensions
- [ ] **Additional compatibility flags:**
  - [ ] `CompatiAutoRegisterVariables` - Auto-register unknown variables instead of errors
  - [ ] `CompatiExpandedArrays` - Allow runtime array expansion
  - [ ] `CompatiStubUnknownCommands` - Create no-op stubs for unknown commands

### Why These Aren't Implemented Yet

1. **Dynamic variable registration** requires significant refactoring of the variable system
2. **BINPUT** behavior isn't well-documented in standard Emuera specifications
3. **Extended save formats** need reverse-engineering of binary formats
4. **Testing requirements** - need extensive testing with various game variants

## Contributing

If you know the exact behavior of extended Emuera features, please contribute:

1. Document the feature behavior in detail
2. Provide test cases showing expected vs actual behavior
3. Submit a pull request with implementation

## Technical Details

### Implementation Notes

The `CompatiIgnoreInvalidLine` feature works by:

1. **Config loading** (ConfigCode.cs, ConfigData.cs, Config.cs)
   - Adds `CompatiIgnoreInvalidLine` enum value
   - Loads setting from emuera.config
   - Exposes via `Config.CompatiIgnoreInvalidLine` property

2. **Script parsing** (LogicalLineParser.cs)
   - Creates `InvalidLine` objects for unparseable lines
   - Stores error message in InvalidLine.ErrMes

3. **Runtime execution** (Process.ScriptProc.cs)
   - Checks `Config.CompatiIgnoreInvalidLine` when hitting InvalidLine
   - If enabled: prints warning and continues
   - If disabled: throws CodeEE exception

### Code Locations

Files modified for this feature:

- `Assets/Scripts/Emuera/Config/ConfigCode.cs` - Enum definition
- `Assets/Scripts/Emuera/Config/ConfigData.cs` - Config item creation
- `Assets/Scripts/Emuera/Config/Config.cs` - Property exposure
- `Assets/Scripts/Emuera/GameProc/Process.ScriptProc.cs` - Runtime handling

## Troubleshooting

### "Game still won't load"

Even with `CompatiIgnoreInvalidLine:Yes`, games may fail if:

1. **Missing CSV files** - Add required variable definitions (see GAME_COMPATIBILITY_ISSUE_EXPLANATION.md)
2. **Critical function missing** - Game requires specific system functions
3. **Syntax errors in @SYSTEM_TITLE** - Title screen functions must be parseable

### "Features are missing"

Enable the config option to identify what's missing:

1. Enable `CompatiIgnoreInvalidLine:Yes`
2. Run the game and note warnings
3. Check which commands/variables appear in warnings
4. Either:
   - Implement missing features
   - Remove features from game scripts
   - Add CSV definitions for variables

### "Game behaves strangely"

Skipped lines may have been important:

1. Check warning messages during execution
2. Compare behavior with working Emuera variant
3. Consider implementing the missing features properly instead of skipping

## Version History

### v1.0 (Current)
- Initial implementation of CompatiIgnoreInvalidLine
- Basic error-tolerant execution
- Warning system for skipped lines

## See Also

- [GAME_COMPATIBILITY_ISSUE_EXPLANATION.md](GAME_COMPATIBILITY_ISSUE_EXPLANATION.md) - Detailed guide on game compatibility issues
- [README.md](README.md) - General uEmuera documentation
- Original Emuera documentation (Japanese) - https://ja.osdn.net/projects/emuera/
