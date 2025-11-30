# Game Compatibility Issue Explanation

## Problem Summary
The errors you're seeing are **NOT Unity program bugs** - they are **game data configuration issues**. The game's CSV variable definition files are missing or incomplete.

## Error Analysis

### 1. BINPUT Command Errors
```
??Lv2:#TRANSLATIONS/stain_removal_mod.ERB:27??:?????????
BINPUT
```
**`BINPUT` is likely a custom command from a mod** that requires additional Emuera extensions. Standard Emuera/uEmuera does not include this command.

### 2. Variable Definition Errors
```
??Lv2:??????/????_?????.ERB:42??:"??AP"????????????
MONEY:??AP
```
Variables like "??AP", "??AP", etc. are **NOT defined in the game's CSV files**.

## Root Cause

uEmuera already has **proper support** for these variable types:
- `MONEY` is defined as an array variable (VariableCode.cs line 32)
- `DAY` is defined as an array variable (VariableCode.cs line 31)

**The problem:** The game's `_??.csv` (VariableSize.csv) file is **missing the definitions** for the specific array indices these variables use.

### How Emuera Variables Work:
1. `MONEY`, `DAY`, `FLAG`, etc. are **base variable arrays**
2. Games define **specific indices** in CSV files like `_??.csv` or `VariableSize.csv`
3. Example:
   ```csv
   ;_??.csv
   ;??,???,???,???,???
   0,MONEY,???,1,0
   1,MONEY,??AP,1,0
   2,MONEY,??AP,1,0
   3,MONEY,??AP,1,0
   4,DAY,??,1,0
   5,DAY,??,1,0
   6,DAY,??,1,0
   7,DAY,??,1,0
   ```

## Solutions

### Solution 1: Add Missing Variable Definitions (Recommended)
Create or update the variable definition CSV file in your game:

1. Navigate to `/era/CSV/`
2. Look for `_??.csv`, `VariableSize.csv`, or similar files
3. If the file doesn't exist, create it with this content:

```csv
;_??.csv or VariableSize.csv
;Format: ??,?????,???,???,???
0,MONEY,??AP,1,0
1,MONEY,??AP,1,0
2,MONEY,??AP,1,0
3,MONEY,??AP,1,0
4,MONEY,??AP???,1,0
5,MONEY,??AP???,1,0
6,MONEY,??AP???,1,0
7,MONEY,??AP???,1,0
8,MONEY,??AP???,1,0
9,MONEY,??AP???,1,0
10,MONEY,??AP???,1,0
11,MONEY,??AP???,1,0
12,MONEY,????,1,0
13,MONEY,????,1,0
14,MONEY,??PT,1,0
15,MONEY,????,1,0
16,MONEY,???,1,0
17,MONEY,???????????,1,0
18,MONEY,?????????????,1,0
19,MONEY,?????,1,0
20,DAY,??,1,0
21,DAY,??,1,0
22,DAY,??,1,0
23,DAY,??,1,0
```

**Note:** The exact CSV format may vary depending on your game variant. Check existing CSV files in `/era/CSV/` for the correct format.

### Solution 2: Comment Out Problematic Code
If you just want the game to run without these features:

1. Navigate to each file mentioned in the errors
2. Add `;` at the beginning of lines causing errors

Example:
```erb
;BINPUT  <- Add semicolon to comment out
;MONEY:??AP += 1  <- Comment out if you can't define the variable
```

### Solution 3: Use a Compatible Game Version
The game in `/era/` might be:
- Designed for a **modded Emuera variant** (eratohoK, EraMakerKai, etc.)
- Using **extension features** not in standard Emuera
- **Incompatible with uEmuera**

Try to:
1. Find the **original, unmodded version** of the game
2. Check if the game requires a specific Emuera variant
3. Look for game documentation about required Emuera version

### Solution 4: About BINPUT
`BINPUT` appears to be:
- A **custom command** from a mod or extension
- **NOT part of standard Emuera 1824**

If BIN PUT is critical to your game:
1. Find which Emuera variant/mod adds this command
2. Look for documentation on what BINPUT does
3. You may need to use a different Emuera interpreter

## For Developers: Why Not Add These to uEmuera?

**Short answer:** It would break the design.

**Long answer:**
- These are **game-specific variables**, not engine features
- Adding them to the engine would:
  - Make uEmuera incompatible with other games
  - Violate separation of engine vs. game data
  - Require maintenance for every game variant
- The **correct solution** is for games to properly define their variables in CSV files

## Conclusion

**This is a game data configuration issue, not a uEmuera bug.**

uEmuera correctly implements the Emuera 1824 specification. Your game either:
1. **Has missing/incomplete CSV files** ? Add the variable definitions
2. **Is designed for a modded Emuera** ? Find the compatible Emuera variant
3. **Uses unsupported extensions** ? Check game documentation

The Unity program is working correctly - it's properly detecting that the game scripts have configuration errors.

## Quick Diagnostic

Run this check:
1. Go to `/era/CSV/`
2. Look for files like:
   - `_??.csv`
   - `VariableSize.csv`
   - `Variable.csv`
3. **If these files are missing**, the game is improperly configured
4. **If they exist but don't define the needed variables**, add the definitions above

## Additional Resources

- [Emuera Wiki (Japanese)](https://ja.osdn.net/projects/emuera/)
- Standard Emuera supports user-defined variables via CSV
- Check your game's documentation or source for required CSV structure

