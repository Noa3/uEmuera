# EraElectron Game Format

> Phase 8 · 2026-08-12  
> Based on direct inspection of erauma-master and ere-kanon-master source.

---

## Package layouts

### Source distribution (developer / repository)

```
<game-root>/
├── ere/                     # JS source directory; '#' alias root
│   ├── era-electron.js      # SDK stub (overridden by engine at runtime)
│   ├── main.js              # Game entry point
│   └── **/*.js              # Game modules
├── csv/                     # ERA-format data files
│   ├── GameBase.csv         # Title, author, version
│   ├── Abl.csv              # Ability names
│   ├── Base.csv             # Base stat names
│   ├── CFlag.csv            # Character flag names
│   ├── Cstr.csv             # Character string variable names
│   ├── Equip.csv            # Equipment slot names
│   ├── Ex.csv               # Extended variable names
│   ├── Flag.csv             # Global flag names
│   ├── Global.csv           # Global variable names
│   ├── Item.csv             # Item names
│   ├── Mark.csv             # Mark/achievement names
│   ├── Param.csv            # Parameter names
│   ├── Skill.csv            # Skill names
│   ├── Stain.csv            # Stain table names
│   ├── Status.csv           # Status table names
│   ├── Talent.csv           # Talent names
│   ├── TCVar.csv            # Train variable names
│   ├── TEquip.csv           # Train equipment names
│   ├── TFlag.csv            # Train flag names
│   ├── Chara/
│   │   ├── Chara0000.csv    # Per-character definition
│   │   └── Chara*.csv
│   ├── _config.json         # User-modifiable config
│   ├── _fixed.json          # Game-fixed config (player cannot override)
│   └── _Replace.csv         # Text replacements (if enabled)
├── .ere-min-version         # Minimum engine version integer
├── package.json             # npm/pnpm manifest
├── webpack.config.js        # Build configuration
└── build/
    └── static.json          # Pre-built game data (~900 KB JSON)
```

### Bundled distribution (player download)

```
<game-root>/
├── dist/
│   ├── era.bundle.js        # SDK injection + engine bootstrap
│   └── main.bundle.js       # Bundled game logic
├── csv/                     # Same as source
├── .ere-min-version
└── (res/ resource pack — may be a separate download)
```

---

## CSV format

### Standard tables (Flag.csv, Abl.csv, etc.)

```
; comment line (entire line ignored)
0,ItemName            ; optional inline comment (stripped before parsing)
1,AnotherName
5,SparseEntry         ; indices need not be consecutive
```

- Encoding: **Shift-JIS (CP932)** — same as Emuera
- First field: integer index
- Second field: name string
- Additional fields: ignored (some tables have default values in field 3+)
- Gaps in indices are valid

### GameBase.csv

```
key,value1|value2|value3
タイトル,MyGame|Subtitle
作者,AuthorName
```

- Key → pipe-delimited value list
- First pipe segment is the primary value

### Chara/*.csv

```
; section header comments
名前,0
呼び名,Nickname

; Base.csv section
stat_name,category,1000

; CFlag section
CFlagName,初期値
```

- Mixed format: character metadata followed by per-stat initial values
- Format varies by section

---

## _config.json schema

```json
{
  "system": {
    "_replace":          boolean,   // enable _Replace.csv
    "hideUserInput":     boolean,   // hide typed text
    "saveCompressedData":boolean    // compress save data
  },
  "window": {
    "audio":    number,   // master audio volume
    "autoMax":  boolean,  // maximize window at start
    "height":   number,   // window height px
    "width":    number    // window width px
  }
}
```

---

## _fixed.json schema

```json
{
  "system": {
    "collapseBlankLines":  boolean,   // merge consecutive blanks
    "extendedCharaTables": string[]   // extra per-chara tables (e.g. ["Skill","Status"])
  },
  "window": {
    "orientation": number   // 0=any, 1=portrait, 2=landscape (Android)
  }
}
```

---

## .ere-min-version

Single integer on one line. Engine refuses to start if its version < this value.

```
2200
```

---

## Module system

| Alias | Resolves to |
|---|---|
| `require('#/era-electron')` | `ere/era-electron.js` (SDK stub; replaced by engine) |
| `require('#/path/module')` | `ere/path/module.js` |
| `require('./relative')` | Standard relative path |

The `#` alias is defined in `webpack.config.js` and `jsconfig.json`:

```javascript
resolve: { alias: { '#': resolve(__dirname, 'ere/') } }
```

**In bundled distributions all `require()` calls are already resolved by webpack.**
The embedded host only needs to inject `window._era` and load the bundles.

---

## Build system

| Tool | Role |
|---|---|
| `pnpm` / `npm` | Package manager |
| `webpack 5` | Bundler (outputs `dist/*.bundle.js`) |
| `ere-webpack-plugin` | ERE entry naming, engine version injection |
| `kojo-loader` | Transforms `.kojo` dialogue → CommonJS (build time) |
| `babel-loader` + `@babel/preset-env` | Transpile to Chrome 60 target |

---

## Variable addressing

ERA variables are addressed as colon-delimited strings:

| Format | Example | Meaning |
|---|---|---|
| `table:index` | `flag:5` | Global flag index 5 |
| `table:charIdx:index` | `abl:0:3` | Character 0, Abl index 3 |
| `table:charIdx` | `callname:1` | Character 1's callname |

String tables: `callname`, `name`, `nickname`, `str`, `cstr`  
Per-character tables: `abl`, `base`, `cflag`, `cstr`, `equip`, `exp`, `param`, `skill`, `status`, `talent`, and extensions from `_fixed.json extendedCharaTables`  
Global tables: `flag`, `global`, everything not in the per-character set

---

## Resource format

Image resources are declared in CSV files under the resource pack.  
See `Docs/ERAELECTRON_RESOURCE_PIPELINE.md` for details.
