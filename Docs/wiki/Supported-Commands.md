# Supported Commands

uEmuera supports the standard Emuera command set plus EM/EE (MultipleEx/EnhanceEx) extensions.

## Standard Emuera Commands

uEmuera is based on **emuera1824v15** and supports all standard ERA script commands.

### Display Commands
| Command | Description |
|---------|-------------|
| `PRINT` | Print text |
| `PRINTL` | Print text with line break |
| `PRINTW` | Print text and wait for input |
| `PRINTFORM` | Print with format string |
| `PRINTFORML` | Print with format string + line break |
| `PRINTFORMW` | Print with format string + wait |
| `PRINTC` | Print centered text |
| `PRINTS` | Print string value |
| `PRINTV` | Print variable value |
| `DRAWLINE` | Draw a horizontal line |
| `CLEARLINE` | Clear lines from display |

### Input Commands
| Command | Description |
|---------|-------------|
| `INPUT` | Wait for numeric input |
| `INPUTS` | Wait for string input |
| `TINPUT` | Timed numeric input |
| `TINPUTS` | Timed string input |
| `ONEINPUT` | Wait for single key input |
| `ONEINPUTS` | Wait for single key string input |
| `WAIT` | Wait for any key press |

### Flow Control
| Command | Description |
|---------|-------------|
| `CALL` | Call a function |
| `CALLFORM` | Call function with format string name |
| `JUMP` | Jump to a function |
| `RETURN` | Return from function |
| `IF` / `ELSEIF` / `ELSE` / `ENDIF` | Conditional |
| `SELECTCASE` / `CASE` / `CASEELSE` / `ENDSELECT` | Switch |
| `FOR` / `NEXT` | Loop |
| `WHILE` / `WEND` | While loop |
| `REPEAT` / `REND` | Repeat loop |
| `DO` / `LOOP` | Do-loop |
| `BREAK` | Break from loop |
| `CONTINUE` | Continue loop |
| `GOTO` / `GOTOFORM` | Goto label |
| `BEGIN` | Begin execution mode |
| `QUIT` | Exit game |

### Variable Operations
| Command | Description |
|---------|-------------|
| `VARSET` | Set variable value |
| `VARSIZE` | Get variable size |
| `SWAP` | Swap two variables |
| `SORTCHARA` | Sort character array |
| `RESETDATA` | Reset data to defaults |

## EM/EE Extension Commands

These commands are from the Emuera MultipleEx / EnhanceEx community extensions.

### Binary Input (EM/EE)
| Command | Description |
|---------|-------------|
| `BINPUT` | Binary numeric input — accepts input as binary |
| `BINPUTS` | Binary string input |

### Try-Call Functions (EM/EE)
| Command | Description |
|---------|-------------|
| `TRYCALLF` | Try to call a function; returns whether it exists |
| `TRYCALLFORMF` | Try to call with format string name; returns whether it exists |

## Audio Commands

Full audio implementation via Unity's AudioManager system.

| Command | Description |
|---------|-------------|
| `PLAYSOUND name [, volume]` | Play a sound effect (WAV). Optional volume 0-100. |
| `STOPSOUND` | Stop all playing sound effects |
| `PLAYBGM name [, volume]` | Play background music (WAV loop). Optional volume. |
| `STOPBGM` | Stop background music |
| `EXISTSOUND name` | Check if a sound file exists. Returns 1 if found, 0 if not. |

### Audio Notes
- **WAV** format is recommended for synchronous playback
- **OGG** and **MP3** require async loading — may have timing issues
- Sound files should be placed in the game's `sound/` folder
- Volume ranges from 0 (silent) to 100 (full)

## GXX Graphics Commands

Support for GXX drawing instructions for games that use graphical elements.

| Command | Description |
|---------|-------------|
| `GCREATE` | Create a graphics buffer |
| `GDISPOSE` | Dispose a graphics buffer |
| `GSETCOLOR` | Set drawing color |
| `GDRAWG` | Draw graphics |
| `GSETBRUSH` | Set brush style |
| `GSETFONT` | Set font for drawing |
| `GSETPEN` | Set pen for drawing |
| `GDRAWTEXT` | Draw text to buffer |
| `GDRAWRECT` | Draw rectangle |
| `GDRAWLINE` | Draw line |
| `GFILLRECT` | Fill rectangle |
| `GCLEAR` | Clear graphics buffer |

> **Note:** Not all GXX commands are fully implemented yet. Some games with complex graphics may have rendering issues.
