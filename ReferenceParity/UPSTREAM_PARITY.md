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
| `HTML_PRINT_ISLAND` | FULL | FULL | N/A | MISSING | Registered; calls EmueraConsole.PrintHTMLIsland (displays immediately) |
| `HTML_PRINT_ISLAND_CLEAR` | FULL | FULL | N/A | MISSING | Registered; ClearHTMLIsland stub |
| `HTML_STRINGLINES` | FULL | FULL | N/A | MISSING | Registered method; returns line count at given pixel width |
| `HTML_STRINGLEN` | FULL | FULL | N/A | MISSING | Returns rendered display length in half-width chars |

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
| `GSETPEN` | FULL | FULL | N/A | MISSING | GraphicsSetPenMethod; pen color used by GDRAWLINE |
| `GDRAWLINE` | FULL | FULL | N/A | MISSING | Bresenham line on GraphicsSurface; GDrawLine on GraphicsImage uses pen color |
| `GSAVE` | FULL | FULL | N/A | MISSING | GraphicsSaveMethod; GSAVE(ID, fileNo) |
| `GLOAD` | FULL | FULL | N/A | MISSING | GraphicsLoadMethod; GLOAD(ID, fileNo) |

---

## Sprite Commands

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| `SPRITEANIMEANIMESTART/STOP` | MISSING | MISSING | N/A | MISSING | Not registered. SPRITEANIMECREATE + SPRITEANIMEADDFRAME + SETANIMETIMER exist; play/stop absent |
| `SPRITECREATE` | FULL | FULL | N/A | MISSING | SpriteCreateMethod; from G-surface region or whole surface |
| `SPRITEGETINFO` | MISSING | MISSING | N/A | MISSING | Not registered. Individual properties (SPRITEWIDTH/HEIGHT/POSX/POSY) exist separately |
| `SPRITEEXIST` | FULL | FULL | N/A | MISSING | Registered; alias of SPRITECREATED |
| `SPRITEDISPOSE` | FULL | FULL | N/A | MISSING | SpriteDisposeMethod; calls AppContents.SpriteDispose |
| `SPRITEDISPOSEALL` | FULL | FULL | N/A | MISSING | Registered; AppContents.SpriteDisposeAll(bool includeG) |
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
| `FLOWINPUT` | FULL | FULL | N/A | MISSING | Registered; flowinput* fields set on Process |
| `FLOWINPUTS` | FULL | FULL | N/A | MISSING | Registered; flowinputs* fields set on Process |
| `MOUSEB` / `GETKEY` | PARTIAL | PARTIAL | N/A | MISSING | GETKEY/GETKEYTRIGGERED registered; MOUSEB standalone absent; INPUTMOUSEKEY covers combined mouse-key |
| `WAITANYKEY` | FULL | FULL | N/A | MISSING | WAITANYKEY_Instruction |

---

## DataTable (DT_*)

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| Any `DT_*` command | FULL | FULL | N/A | MISSING | 20 DT_ commands; custom EraDataTable (IL2CPP-safe, no System.Data) |
| `DT_CREATE` | FULL | FULL | N/A | MISSING | Creates named EraDataTable in VariableData.DataTables |
| `DT_EXIST` | FULL | FULL | N/A | MISSING | Tests presence in DataTables dict |
| `DT_RELEASE` | FULL | FULL | N/A | MISSING | Removes table from DataTables |
| `DT_CLEAR` | FULL | FULL | N/A | MISSING | Clears rows, keeps column defs |
| `DT_COLUMN_ADD` | FULL | FULL | N/A | MISSING | EraDataTable.AddCol(name, type) |
| `DT_COLUMN_NAMES` | FULL | FULL | N/A | MISSING | EraDataTable.ColNames() |
| `DT_COLUMN_EXIST` | FULL | FULL | N/A | MISSING | EraDataTable.ColExist(name) |
| `DT_COLUMN_REMOVE` | FULL | FULL | N/A | MISSING | EraDataTable.RemoveCol(name) |
| `DT_ROW_COUNT` | FULL | FULL | N/A | MISSING | EraDataTable.RowCount |
| `DT_ROW_ADD` | FULL | FULL | N/A | MISSING | EraDataTable.AddRow() |
| `DT_ROW_REMOVE` | FULL | FULL | N/A | MISSING | EraDataTable.RemoveRow(idx) |
| `DT_GET` | FULL | FULL | N/A | MISSING | GetStr / GetInt / GetFloat dispatch |
| `DT_SET` | FULL | FULL | N/A | MISSING | SetStr / SetInt / SetFloat dispatch |
| `DT_FIND` | FULL | FULL | N/A | MISSING | EraDataTable.Find(col, val) |
| `DT_SORT` | FULL | FULL | N/A | MISSING | EraDataTable.Sort(col, ascending) |
| `DT_TOCSV` | FULL | FULL | N/A | MISSING | EraDataTable.ToCsv() |
| `DT_TOXML` | FULL | FULL | N/A | MISSING | EraDataTable.ToXml() |
| `DT_ROW_LENGTH(name)` | FULL | FULL | N/A | MISSING | Row count; -1 if table missing |
| `DT_CELL_GET(name,row,col)` | FULL | FULL | N/A | MISSING | Int cell value |
| `DT_CELL_GETS(name,row,col)` | FULL | FULL | N/A | MISSING | String cell value |
| `DT_CELL_ISNULL(name,row,col)` | FULL | FULL | N/A | MISSING | 0=has value, 1=empty, -1=no row, -2=no table |
| `DT_SELECT(name,col,val)` | FULL | FULL | N/A | MISSING | Fills RESULT[] with matching row indices |

---

## MAP Commands (MAP_*)

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| Any `MAP_*` command | FULL | FULL | N/A | MISSING | 12 MAP_ commands; VariableData.DataMaps dictionary |
| `MAP_CREATE` | FULL | FULL | N/A | MISSING | Creates named map in DataMaps |
| `MAP_EXIST` | FULL | FULL | N/A | MISSING | Tests presence in DataMaps |
| `MAP_RELEASE` | FULL | FULL | N/A | MISSING | Removes map from DataMaps |
| `MAP_GET` | FULL | FULL | N/A | MISSING | Returns value for key |
| `MAP_HAS` | FULL | FULL | N/A | MISSING | Tests key presence |
| `MAP_SET` | FULL | FULL | N/A | MISSING | Sets key→value |
| `MAP_REMOVE` | FULL | FULL | N/A | MISSING | Removes key |
| `MAP_CLEAR` | FULL | FULL | N/A | MISSING | Clears all entries |
| `MAP_SIZE` | FULL | FULL | N/A | MISSING | Entry count |
| `MAP_GETKEYS` | FULL | FULL | N/A | MISSING | Returns all keys |
| `MAP_TOXML` | FULL | FULL | N/A | MISSING | Serializes to XML string |
| `MAP_FROMXML` | FULL | FULL | N/A | MISSING | Populates map from XML string |

---

## XML Commands (XML_*)

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| Any `XML_*` command | FULL | FULL | N/A | MISSING | 18 XML_ commands; VariableData.DataXmlDocuments (XmlDocument) |
| `XML_DOCUMENT` | FULL | FULL | N/A | MISSING | Creates named XmlDocument in DataXmlDocuments |
| `XML_RELEASE` | FULL | FULL | N/A | MISSING | Removes XmlDocument |
| `XML_EXIST` | FULL | FULL | N/A | MISSING | Tests presence |
| `XML_GET` | FULL | FULL | N/A | MISSING | Gets node/attribute value by XPath |
| `XML_SET` | FULL | FULL | N/A | MISSING | Sets node value |
| `XML_TOSTR` | FULL | FULL | N/A | MISSING | Serializes XmlDocument to string |
| `XML_ADDNODE` | FULL | FULL | N/A | MISSING | Inserts child node |
| `XML_REMOVENODE` | FULL | FULL | N/A | MISSING | Removes node by XPath |
| `XML_ADDATTRIBUTE` | FULL | FULL | N/A | MISSING | Adds attribute to node |
| `XML_REMOVEATTRIBUTE` | FULL | FULL | N/A | MISSING | Removes attribute from node |
| `XML_REPLACE` | FULL | FULL | N/A | MISSING | Replaces node content |

---

## ERD / Named Array Indices

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| ERD named array indices | MISSING | MISSING | N/A | MISSING | No .erd file loader; NAMEDEFINE/NAMEDEF absent from ErbLoader and HeaderFileLoader |
| `ERDNAME` | FULL | FULL | N/A | MISSING | ERDNAME(varname, index) → keyword name; ConstantData.TryIntegerToKeyword added |

---

## File System / Process

| Feature | Parse | Runtime | Render | Test | Notes |
|---|---|---|---|---|---|
| `EXISTFILE(path)` | FULL | FULL | N/A | MISSING | Sandboxed to ExeDir; path traversal blocked |
| `EXISTVAR(varname)` | FULL | FULL | N/A | MISSING | Returns bitmask of variable type flags |
| `ENUMFILES(dir,...)` | FULL | FULL | N/A | MISSING | Fills RESULTS[] with relative file paths |
| `CLEARMEMORY()` | FULL | FULL | N/A | MISSING | GC.Collect(); returns bytes freed |
| `GETDOINGFUNCTION()` | FULL | FULL | N/A | MISSING | Returns current executing function label name |
| `GETVAR(expr)` | FULL | FULL | N/A | MISSING | Parses string as ERA int expression and evals |
| `GETVARS(expr)` | FULL | FULL | N/A | MISSING | Parses string as ERA string expression and evals |

---

## Summary Counts

| Category | Total Features | FULL Parse | PARTIAL Parse | MISSING Parse |
|---|---|---|---|---|
| HTML Tags | 14 | 12 | 2 | 0 |
| HTML Instructions | 8 | 8 | 0 | 0 |
| CBG Commands | 8 | 8 | 0 | 0 |
| Graphics G* | 14 | 14 | 0 | 0 |
| Sprite Commands | 7 | 5 | 0 | 2 |
| Save/Load | 8 | 8 | 0 | 0 |
| Input | 8 | 7 | 1 | 0 |
| DataTable DT_* | 23 | 23 | 0 | 0 |
| MAP Commands | 13 | 13 | 0 | 0 |
| XML Commands | 12 | 12 | 0 | 0 |
| ERD Named Indices | 2 | 1 | 0 | 1 |
| File System / Process | 7 | 7 | 0 | 0 |
| **Total** | **124** | **118** | **3** | **3** |

**Parse coverage: 95.2% FULL, 2.4% PARTIAL, 2.4% MISSING**

### Known gaps (priority order):
1. `SPRITEANIMEANIMESTART` / `SPRITEANIMEANIMESTOP` — animation play control
2. `SPRITEGETINFO` — sprite query API (individual SPRITEWIDTH/HEIGHT/POSX/POSY exist separately)
3. `<img srcb>` render — hover swap wired but needs integration testing
4. `<div>` / `<clearbutton>` — parse stubs only, rendering NO_OP
5. ERD named array indices — `.erd` file format not loaded; NAMEDEFINE/NAMEDEF absent
6. `MOUSEB` — standalone form still PARTIAL; INPUTMOUSEKEY covers combined mouse-key
