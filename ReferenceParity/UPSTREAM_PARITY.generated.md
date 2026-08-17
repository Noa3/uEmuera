# Generated EM+EE parity report

> Generated from source registries and regression metadata. This file is not hand-maintained.
> `FULL` requires a verified reference fixture; registration alone never produces `FULL`.

- uEmuera revision: `0268d4bfdf9c846cd37efcc4eea1e26550dceac6`
- Reference: `EMv18 / EEv55`
- Reference Emuera.EM revision: `cde11d69048f87d4a70d1452da79ae6e56462386`
- Reference tag: `Emuera.NET1824+v22+EMv18+EEv55`
- Generated: `2025-11-14T20:59:19+09:00`

| Feature | Parser | Arguments | Runtime | Rendering | Input | Persistence | Platform | Tests | Overall |
|---|---|---|---|---|---|---|---|---|---|
| html.div | PARTIAL | FULL | FULL | FULL | N/A | N/A | FULL | MISSING | IMPLEMENTED_UNVERIFIED |
| html.clearbutton | FULL | MISSING | FULL | FULL | N/A | N/A | FULL | MISSING | PARTIAL |
| html.img.srcb | FULL | MISSING | FULL | FULL | N/A | N/A | FULL | MISSING | PARTIAL |
| html.img.srcm | FULL | MISSING | FULL | FULL | N/A | N/A | FULL | MISSING | PARTIAL |
| html.print_island | FULL | PARTIAL | FULL | FULL | N/A | N/A | FULL | MISSING | PARTIAL |
| cbg.sprite | FULL | FULL | FULL | FULL | N/A | N/A | FULL | MISSING | IMPLEMENTED_UNVERIFIED |
| cbg.buttonmap | FULL | PARTIAL | FULL | FULL | FULL | N/A | FULL | MISSING | PARTIAL |
| cbg.ordering | FULL | FULL | FULL | FULL | N/A | N/A | FULL | MISSING | IMPLEMENTED_UNVERIFIED |
| dt.create | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | IMPLEMENTED_UNVERIFIED | PARTIAL |
| dt.column | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | MISSING | PARTIAL |
| dt.row | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | IMPLEMENTED_UNVERIFIED | PARTIAL |
| dt.cell | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | MISSING | PARTIAL |
| dt.select | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | IMPLEMENTED_UNVERIFIED | PARTIAL |
| dt.serialization | FULL | PARTIAL | FULL | N/A | N/A | FULL | FULL | IMPLEMENTED_UNVERIFIED | PARTIAL |
| map.create | FULL | FULL | FULL | N/A | N/A | N/A | FULL | MISSING | IMPLEMENTED_UNVERIFIED |
| map.operations | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | MISSING | PARTIAL |
| map.serialization | FULL | PARTIAL | FULL | N/A | N/A | FULL | FULL | MISSING | PARTIAL |
| xml.get | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | MISSING | PARTIAL |
| xml.set | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | MISSING | PARTIAL |
| xml.mutation | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | MISSING | PARTIAL |
| erd.named_indices | FULL | PARTIAL | FULL | N/A | N/A | N/A | FULL | MISSING | PARTIAL |
| input.mouseb | FULL | PARTIAL | FULL | N/A | FULL | N/A | FULL | MISSING | PARTIAL |
| input.coordinates | FULL | PARTIAL | FULL | N/A | FULL | N/A | FULL | MISSING | PARTIAL |
| save.extended_data | FULL | PARTIAL | FULL | N/A | N/A | FULL | FULL | MISSING | PARTIAL |
| save.multidimensional_strings | FULL | PARTIAL | FULL | N/A | N/A | FULL | FULL | MISSING | PARTIAL |
| filesystem.virtual | FULL | PARTIAL | FULL | N/A | N/A | FULL | FULL | MISSING | PARTIAL |
| filesystem.encoding | PARTIAL | MISSING | FULL | N/A | N/A | N/A | FULL | MISSING | PARTIAL |

## Evidence policy

- `PARSE_ONLY` means a name is recognized but no runtime implementation was found.
- `IMPLEMENTED_UNVERIFIED` means source evidence exists but reference execution has not been recorded.
- `PARTIAL` means only some required source surfaces were found.
- `N/A` is used only for dimensions that do not apply to a feature.
