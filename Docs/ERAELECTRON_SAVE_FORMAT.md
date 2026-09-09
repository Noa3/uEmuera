# EraElectron Save Format

> Updated: 2026-09-09

## Current status

uEmuera now persists EraElectron runtime state instead of returning success for an empty
four-byte marker. This is an **internal fallback format**, not a claim of official
EraElectron save-file interoperability.

| Feature | Status |
|---|---|
| Per-game namespace | Implemented |
| Atomic-ish file storage | Implemented (`FileGameStorage`) |
| Slot key format `save_N` | Implemented |
| uEmuera internal variable round-trip | Implemented |
| Character/train lists | Implemented |
| `saveGlobal/loadGlobal/resetGlobal` for GLOBAL/GLOBALS-style tables | Implemented internally |
| Official EraElectron byte format | Unverified / not implemented |
| Official compression compatibility | Unverified / not implemented |

## Storage layout

```text
<persistentDataPath>/
  saves/
    <GameDescriptor.SaveNamespace>/
      save_0.bin
      save_1.bin
      save_global.bin
```

Slot names and namespaces are sanitized so game input cannot escape the save directory.

## Internal fallback format

uEmuera-owned saves begin with:

```text
UEMUERA-ERE-SAVE-1\n
```

followed by UTF-8 JSON containing a versioned snapshot of integer/string variable maps
and character lists. Global saves contain only GLOBAL/GLOBALS-style tables.

Unknown bytes, the old `ERES` marker, and future unsupported format versions are
rejected **without mutating live game state**.

## Important interoperability boundary

The official EraElectron save serialization/compression format still needs to be captured
from the current reference runtime. Until that work is complete:

- uEmuera saves are reliable for uEmuera's own ERE sessions;
- they must not be advertised as loadable by official EraElectron;
- official EraElectron saves must not be silently accepted as if they were parsed.

## Next reference work

1. Create known saves in official EraElectron 4.8.0.
2. Capture uncompressed and `saveCompressedData=true` samples if possible.
3. Identify header/schema/compression.
4. Add official-save fixture tests.
5. Implement import/export behind a format detector while retaining the internal
   versioned fallback for forward safety.
