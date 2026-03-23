# Building from Source

This guide covers setting up your development environment and building uEmuera from source.

## Prerequisites

- **Unity 6** (6000.3.3f1 or later) — [Download Unity Hub](https://unity.com/download)
- **Git** — for cloning the repository
- Platform-specific build support modules:
  - **Windows**: Windows Build Support (IL2CPP) module
  - **Linux**: Linux Build Support (IL2CPP) module
  - **Android**: Android Build Support module + Android SDK/NDK

## Clone the Repository

```bash
git clone https://github.com/Noa3/uEmuera.git
cd uEmuera
```

## Open in Unity

1. Open **Unity Hub**
2. Click **Add** → **Add project from disk**
3. Select the cloned `uEmuera` folder
4. Ensure Unity 6000.3.3f1 is installed (Hub will prompt to install if needed)
5. Open the project

## Build Settings

### Windows Build

1. **File** → **Build Settings**
2. Select **Windows, Mac, Linux** platform
3. Set **Target Platform**: Windows
4. Set **Architecture**: x86_64
5. Set **Scripting Backend**: IL2CPP
6. Click **Build** or **Build And Run**

### Linux Build

1. **File** → **Build Settings**
2. Select **Windows, Mac, Linux** platform
3. Set **Target Platform**: Linux
4. Set **Architecture**: x86_64
5. Set **Scripting Backend**: IL2CPP
6. Click **Build**

### Android Build

1. **File** → **Build Settings**
2. Select **Android** platform
3. Click **Switch Platform** if needed
4. Ensure settings:
   - **Scripting Backend**: IL2CPP
   - **Target Architectures**: ARMv7 + ARM64
   - **Minimum API Level**: 25 (Android 7.1)
5. Click **Build** to produce an APK

## Project Configuration

Key settings in `ProjectSettings/ProjectSettings.asset`:

| Setting | Value | Purpose |
|---------|-------|---------|
| Scripting Backend | IL2CPP (all platforms) | Faster runtime, smaller builds |
| Managed Stripping | High | Aggressive unused code removal |
| Engine Code Stripping | Enabled | Remove unused engine features |
| Incremental GC | Enabled | Smoother frame times |
| Graphics Jobs | Enabled | Multi-threaded rendering |
| Unsafe Code | Allowed | Required for Burst/NativeArray |
| Mip Stripping | Enabled | Smaller texture memory |

## Running Tests

1. **Window** → **General** → **Test Runner**
2. Select **EditMode** or **PlayMode** tab
3. Click **Run All** to execute tests

### Test Locations

- `Assets/Tests/EditMode/` — Unit tests that run without Play mode
- `Assets/Tests/PlayMode/` — Integration tests that run in Play mode

## Packages

The project uses these Unity packages:

| Package | Purpose |
|---------|---------|
| `com.unity.burst` | High-performance code compilation |
| `com.unity.collections` | Native collection types |
| `com.unity.2d.pixel-perfect` | Crisp pixel rendering |
| `com.unity.postprocessing` | CRT effect and visual post-processing |
| `com.unity.localization` | Multi-language UI support |
| `com.unity.ugui` | Unity UI system |

## Tips

- **IL2CPP builds take longer** than Mono builds — be patient on first build
- **Android builds** require Android SDK and NDK (install via Unity Hub)
- Use **Development Build** checkbox for debugging; uncheck for release
- The project uses `.NET Standard 2.1` API compatibility level
