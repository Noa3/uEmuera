<div align="center">

# uEmuera — Noa Version

**A Unity 6 runtime platform for ERA games**

Traditional **Emuera / EM+EE** games are the primary supported runtime.  
**EraElectron / ERE** support is under active development with EraUma as the flagship compatibility target.

[Download](https://github.com/Noa3/uEmuera/releases) · [Wiki](https://github.com/Noa3/uEmuera/wiki/) · [Documentation](Docs/README.md)

</div>

## Project status

uEmuera is based on [xerysherry/uEmuera](https://github.com/xerysherry/uEmuera) and is being extended from a single Emuera port into a multi-runtime ERA launcher.

| Runtime | Status | Platforms |
|---|---|---|
| Emuera / classic ERA | Active | Windows, Linux, Android |
| EM / EM+EE extensions | Active, parity tracked | Windows, Linux, Android |
| Fast Boot | Experimental opt-in; Auto remains Safe | Emuera runtime |
| EraElectron embedded | Experimental / unverified | Windows standalone via WebView2 |
| EraElectron sidecar | Experimental / unverified | Desktop when an official runtime is configured |
| EraElectron embedded | Planned | Android, Linux |

**Compatibility claims are evidence-based.** Generated parity information lives in
[ReferenceParity](ReferenceParity/) and runtime architecture/status in [Docs](Docs/README.md).
EraElectron support does **not** yet imply full EraUma playability.

## Current goals

1. Preserve and increase Emuera / EM+EE game compatibility.
2. Make Fast Boot reach the real title screen quickly without semantic races.
3. Complete EraElectron compatibility for unmodified EraUma and other ERE games.
4. Keep image crops, offsets, alpha and layer ordering deterministic.
5. Support Windows, Linux and Android without game-specific hacks.

## Quick start

### Emuera games

Select a directory containing an Emuera game (typically an `ERB/` directory and/or
`emuera.config`). The launcher detects supported game roots automatically.

The runtime supports modern UTF-8 data and legacy CP932 / Shift-JIS paths where the
current loader can detect them. Do not convert a game solely because older README
versions required UTF-8.

### EraElectron games

The launcher can detect verified source-layout markers such as `.ere-min-version`
and the `ere/` tree. Windows standalone builds contain an experimental WebView2 host.
Source-form games can use a configured official EraElectron sidecar when available.

Full EraUma compatibility is still a development target; see
[EraElectron architecture](Docs/ERAELECTRON_ARCHITECTURE.md).

## Development

- **Unity:** 6000.5.8f1
- **Language:** C#
- **Primary runtime:** Emuera / EM+EE
- **Second runtime:** EraElectron / ERE
- **Tests:** Unity EditMode and PlayMode suites
- **Reference tooling:** Emuera and EraElectron upstream/API scanners

Start with [Docs/README.md](Docs/README.md).

## Repository hygiene

Unity-generated IDE project files are intentionally ignored. Open the project in Unity
and let Unity regenerate `*.csproj` / solution files locally.

Large game packages and local compatibility corpora belong outside versioned source
(e.g. `.games/`, already ignored).

## Screenshots

<details>
<summary>Show screenshots</summary>

| Start Screen | Game Running | Quick Buttons |
|:---:|:---:|:---:|
| ![Start](Screenshot/screenshot1.png) | ![Running](Screenshot/screenshot2.png) | ![Buttons](Screenshot/screenshot3.png) |

| Command Input | Zoom Control |
|:---:|:---:|
| ![Input](Screenshot/screenshot4.png) | ![Zoom](Screenshot/screenshot5.png) |

</details>

## License

uEmuera is licensed under the [Apache License 2.0](LICENSE).

Third-party runtime dependencies may use different licenses. See
[Docs/LICENSING_ARCHITECTURE.md](Docs/LICENSING_ARCHITECTURE.md) before embedding or
redistributing additional EraElectron components.
