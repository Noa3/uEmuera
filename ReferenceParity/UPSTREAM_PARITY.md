# uEmuera — EM+EE Feature Parity Status

Generated: 2026-08-09  
Reference: EM+EE (Emuera-Anchor / EmueraEE)  
Source: `Assets/Scripts/Emuera/` codebase search

**Status legend**  
Parse: `FULL` `PARTIAL` `PARSE_ONLY` `MISSING`  
Runtime: `FULL` `PARTIAL` `NO_OP` `MISSING`  
Render: `FULL` `PARTIAL` `NO_OP` `MISSING` `N/A`  
Test: `MISSING` (no automated test suite exists for any of these features)

---

## HTML Tags

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| `<img src>` | FULL | FULL | FULL | MISSING | ConsoleImagePart; src resolved via AppContents |
| `<img srcb>` | FULL | FULL | PARTIAL | MISSING | Stored; Unity hover/PointerEnter swap not yet wired |
| `<img srcm>` | FULL | FULL | FULL | MISSING | Per-pixel mask via SpriteGetColor + GraphicsSurface |
| `<img flip>` (neg w/h) | FULL | FULL | FULL | MISSING | FlipH/FlipV flags; GDI-style flip in blit path |
| `<div>` | PARTIAL | NO_OP | NO_OP | MISSING | Tag consumed silently; NYI per code comment; content inline |
| `<clearbutton>` | PARTIAL | NO_OP | NO_OP | MISSING | Tag consumed silently; clearbutton behavior NYI |
| `<button>` | FULL | FULL | FULL | MISSING | ConsoleButtonString; integer or string value |
| `<font>` | FULL | FULL | FULL | MISSING | face/color/bcolor; nested font stack supported |
| `<p align>` | FULL | FULL | FULL | MISSING | left/center/right; line-start only; end tag optional |
| `<nobr>` | FULL | FULL | FULL | MISSING | PRINTSINGLE equivalent; error if mid-line or duplicated |
| `<b><i><u><s>` | FULL | FULL | FULL | MISSING | FontStyle flags; correctly stacked and closed |
| `<shape>` | FULL | FULL | FULL | MISSING | rect/space/polygon; color/bcolor/param; HTML_GETPRINTEDSTR round-trip |
| `<nonbutton>` | FULL | FULL | FULL | MISSING | title attr; locked X position (PointXisLocked) |
| `<br>` | FULL | FULL | FULL | MISSING | Forces line break; flushes display buffer |

---

## HTML Instructions

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| `HTML_PRINT` | FULL | FULL | N/A | MISSING | Registered instruction; calls Console.PrintHtml |
| `HTML_PRINT` 2nd arg | FULL | FULL | N/A | MISSING | Optional toPrintBuffer int flag; parsed as OptTerm |
| `HTML_TAGSPLIT` | FULL | FULL | N/A | MISSING | Registered instruction; calls HtmlManager.HtmlTagSplit |
| `HTML_POPPRINTINGSTR` | FULL | FULL | N/A | MISSING | Registered method in Creator.cs |
| `HTML_GETPRINTEDSTR` | FULL | FULL | N/A | MISSING | Registered method; ConsoleImagePart + ConsoleShapePart serialize back to HTML |
| `HTML_PRINT_ISLAND` | MISSING | MISSING | N/A | MISSING | Not found anywhere in codebase |
| `HTML_PRINT_ISLAND_CLEAR` | MISSING | MISSING | N/A | MISSING | Not found anywhere in codebase |
| `HTML_STRINGLINES` | FULL | FULL | N/A | MISSING | Registered method; returns line count at given pixel width |

---

## CBG Commands

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| `CBGSETG` | FULL | FULL | N/A | MISSING | CBGSetGraphicsMethod; CBGSETG(ID, x, y, zdepth) |
| `CBGSETSPRITE` | FULL | FULL | N/A | MISSING | CBGSetCIMGMethod; CBGSETCIMG(imgName, x, y, zdepth) |
| `CBGCLEAR` | FULL | FULL | N/A | MISSING | CBGClearMethod |
| `CBGREMOVERANGE` | FULL | FULL | N/A | MISSING | CBGRemoveRangeMethod; CBGREMOVERANGE(zmin, zmax) |
| `CBGSETBMAPG` | FULL | FULL | N/A | MISSING | CBGSetBMapGMethod; CBGSETBMAPG(ID, x, y, zdepth) |
| `CBGSETBUTTONSPRITE` | FULL | FULL | N/A | MISSING | CBGSETButtonSpriteMethod; 7-param signature with tooltip |
| `CBGREMOVEBMAP` | FULL | FULL | N/A | MISSING | CBGRemoveBMapMethod |
| `CBGCLEARBUTTON` | FULL | FULL | N/A | MISSING | CBGClearButtonMethod |

---

## Graphics (G*) Commands

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| `GCREATE` | FULL | FULL | N/A | MISSING | GraphicsCreateMethod |
| `GCREATEFROMFILE` | FULL | FULL | N/A | MISSING | GraphicsCreateFromFileMethod; copies pixel data from file |
| `GDISPOSE` | FULL | FULL | N/A | MISSING | GraphicsDisposeMethod |
| `GCLEAR` | FULL | FULL | N/A | MISSING | GraphicsClearMethod; overwrites all pixels |
| `GFILLRECTANGLE` | FULL | FULL | N/A | MISSING | GraphicsFillRectangleMethod; clips to bounds |
| `GDRAWG` | FULL | FULL | N/A | MISSING | GraphicsDrawGMethod; optional color matrix arg |
| `GDRAWGWITHMASK` | FULL | FULL | N/A | MISSING | GraphicsDrawGWithMaskMethod; per-pixel mask alpha |
| `GSETCOLOR` | FULL | FULL | N/A | MISSING | GraphicsSetColorMethod; single pixel, ignores alpha |
| `GGETCOLOR` | FULL | FULL | N/A | MISSING | GraphicsGetColorMethod; returns ARGB int |
| `GSETBRUSH` | FULL | FULL | N/A | MISSING | GraphicsSetBrushMethod; brush stored on GraphicsImage |
| `GSETPEN` | FULL | FULL | N/A | MISSING | GraphicsSetPenMethod; pen stored but currently unused (GDRAWLINE absent) |
| `GDRAWLINE` | MISSING | MISSING | N/A | MISSING | **Not registered anywhere.** GSETPEN exists; GDRAWLINE has no implementation |
| `GSAVE` | FULL | FULL | N/A | MISSING | GraphicsSaveMethod; GSAVE(ID, fileNo) |
| `GLOAD` | FULL | FULL | N/A | MISSING | GraphicsLoadMethod; GLOAD(ID, fileNo) |

---

## Sprite Commands

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| `SPRITEANIMEANIMESTART/STOP` | MISSING | MISSING | N/A | MISSING | Not registered. SPRITEANIMECREATE + SPRITEANIMEADDFRAME + SETANIMETIMER exist; play/stop absent |
| `SPRITECREATE` | FULL | FULL | N/A | MISSING | SpriteCreateMethod; from G-surface region or whole surface |
| `SPRITEGETINFO` | MISSING | MISSING | N/A | MISSING | Not registered. Individual properties (SPRITEWIDTH/HEIGHT/POSX/POSY) exist separately |
| `SPRITEEXIST` | MISSING | MISSING | N/A | MISSING | Not registered. Use `SPRITECREATED` (registered) as functional equivalent |
| `SPRITEDISPOSE` | FULL | FULL | N/A | MISSING | SpriteDisposeMethod; calls AppContents.SpriteDispose |
| `SPRITEDISPOSEALL` | MISSING | MISSING | N/A | MISSING | Not registered anywhere |
| `SPRITEGETCOLOR` | FULL | FULL | N/A | MISSING | SpriteGetColorMethod; implemented on CroppedImage and SpriteAnime |

---

## Save / Load

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| `SAVEGAME` / `LOADGAME` | FULL | FULL | N/A | MISSING | SAVELOADGAME_Instruction; shop context only |
| `SAVEDATA` / `LOADDATA` | FULL | FULL | N/A | MISSING | SP_SAVEDATA arg builder; slot + description string |
| `SAVEGLOBAL` / `LOADGLOBAL` | FULL | FULL | N/A | MISSING | SAVEGLOBAL_Instruction / LOADGLOBAL_Instruction |
| `SAVEVAR` / `LOADVAR` | FULL | FULL | N/A | MISSING | SP_SAVEVAR; rejects character vars with explicit error |
| `SAVECHARA` / `LOADCHARA` | FULL | FULL | N/A | MISSING | SP_SAVECHARA; variable-length chara index list |
| `DELDATA` / `CHKDATA` | FULL | FULL | N/A | MISSING | DELDATA_Instruction; CheckdataMethod (Creator.cs:41) |
| Binary save format (1808) | FULL | FULL | N/A | MISSING | EraBinaryDataReader1808 / EraBinaryDataWriter |
| Text save format | FULL | FULL | N/A | MISSING | EraDataReader / EraDataWriter; EMU_1808_START marker |

---

## Input

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| `INPUT` / `INPUTS` | FULL | FULL | N/A | MISSING | INPUT_Instruction / INPUTS_Instruction |
| `TINPUT` / `TINPUTS` | FULL | FULL | N/A | MISSING | TINPUT_Instruction(false) / TINPUTS_Instruction(false) |
| `ONEINPUT` / `ONEINPUTS` | FULL | FULL | N/A | MISSING | ONEINPUT_Instruction; SP_ONEINPUT handles nument flag |
| `TONEINPUT` / `TONEINPUTS` | FULL | FULL | N/A | MISSING | TINPUT_Instruction(true) / TINPUTS_Instruction(true) |
| `FLOWINPUT` | MISSING | MISSING | N/A | MISSING | Not found anywhere in codebase |
| `MOUSEB` / `GETKEY` | PARTIAL | PARTIAL | N/A | MISSING | GETKEY/GETKEYTRIGGERED registered; MOUSEB standalone absent; INPUTMOUSEKEY covers combined mouse-key |
| `WAITANYKEY` | FULL | FULL | N/A | MISSING | WAITANYKEY_Instruction |

---

## DataTable (DT_*)

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| Any `DT_*` command | MISSING | MISSING | N/A | MISSING | No DT_ prefixed commands found. Feature entirely absent |

---

## MAP Commands (MAP_*)

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| Any `MAP_*` command | MISSING | MISSING | N/A | MISSING | No MAP_ prefixed commands found. Feature entirely absent |

---

## XML Commands (XML_*)

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| Any `XML_*` command | MISSING | MISSING | N/A | MISSING | No XML_ prefixed commands found. Feature entirely absent |

---

## ERD / Named Array Indices

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| ERD named array indices | MISSING | MISSING | N/A | MISSING | No .erd file loader; NAMEDEFINE/NAMEDEF absent from ErbLoader and HeaderFileLoader |

---

## Summary Counts

| Category | Total Features | FULL Parse | PARTIAL Parse | MISSING Parse |
|---|---|---|---|---|
| HTML Tags | 14 | 12 | 2 | 0 |
| HTML Instructions | 8 | 6 | 0 | 2 |
| CBG Commands | 8 | 8 | 0 | 0 |
| Graphics G* | 14 | 13 | 0 | 1 |
| Sprite Commands | 7 | 3 | 0 | 4 |
| Save/Load | 8 | 8 | 0 | 0 |
| Input | 7 | 5 | 1 | 1 |
| DataTable DT_* | 1 | 0 | 0 | 1 |
| MAP Commands | 1 | 0 | 0 | 1 |
| XML Commands | 1 | 0 | 0 | 1 |
| ERD Named Indices | 1 | 0 | 0 | 1 |
| **Total** | **70** | **55** | **3** | **12** |

**Parse coverage: 78.6% FULL, 4.3% PARTIAL, 17.1% MISSING**

### Known gaps (priority order):
1. `GDRAWLINE` — pen API exists but draw call missing
2. `HTML_PRINT_ISLAND` / `HTML_PRINT_ISLAND_CLEAR` — deferred render regions
3. `SPRITEEXIST` / `SPRITEGETINFO` / `SPRITEDISPOSEALL` — sprite query API holes
4. `SPRITEANIMEANIMESTART` / `SPRITEANIMEANIMESTOP` — animation play control
5. `FLOWINPUT` — non-blocking input
6. `<img srcb>` render — hover swap not wired to Unity input
7. `DT_*` / `MAP_*` / `XML_*` — entire feature families absent
8. ERD named array indices — `.erd` file format not loaded
