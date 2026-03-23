# Supported Platforms

uEmuera runs on three platforms via Unity 6's cross-platform capabilities.

## Platform Comparison

| Feature | Windows | Linux | Android |
|---------|---------|-------|---------|
| Scripting Backend | IL2CPP | IL2CPP | IL2CPP |
| Managed Stripping | High | High | High |
| Graphics Jobs | ✅ | ✅ | ✅ |
| Burst Compiler | ✅ | ✅ | ✅ |
| Incremental GC | ✅ | ✅ | ✅ |
| Default Resolution | 1280×900 | 1280×900 | Device native |
| Resizable Window | ✅ | ✅ | N/A (fullscreen) |
| Audio (WAV) | ✅ | ✅ | ✅ |
| Audio (OGG/MP3) | Async only | Async only | Async only |
| File Access | Direct | Direct | Permission required |

## Windows

- **Minimum**: Windows 10 x64
- **Graphics**: DirectX 11+ (Vulkan also available)
- **Window Mode**: Starts in a resizable 1280×900 window; supports fullscreen toggle with `Alt+Enter`
- **Build**: IL2CPP compiled for optimal performance

## Linux

- **Minimum**: Ubuntu 20.04 x64 or equivalent
- **Graphics**: Vulkan / OpenGL
- **Window Mode**: Starts in a resizable 1280×900 window; supports fullscreen toggle
- **Dependencies**: Standard Linux graphics drivers

## Android

- **Minimum**: Android 7.1 (API 25)
- **Architecture**: ARM64 (ARMv8) + ARM32 (ARMv7)
- **Graphics**: OpenGL ES 3.1 / Vulkan (auto-detected)
- **Permissions**: File/storage access required
- **Install Location**: Prefers external storage
- **Multithreaded Rendering**: Enabled

## Build Optimizations

All platforms benefit from these Unity 6 optimizations:

### IL2CPP Scripting Backend
Converts C# IL code to C++ before compilation, providing:
- **Faster execution** than Mono interpreter
- **Smaller build size** with code stripping
- **Better security** (no IL code in builds)

### High Managed Stripping
Aggressively removes unused code:
- Reduces final binary size significantly
- Removes unused .NET framework classes
- Engine code stripping also enabled

### Burst Compiler
Compiles performance-critical code paths to native SIMD instructions:
- 5-6x faster math operations
- Used in scrolling, rendering, and layout calculations

### Incremental Garbage Collection
Spreads GC work across multiple frames:
- Reduces frame hitches / stuttering
- Smoother gameplay experience
- Especially beneficial on mobile devices

### Graphics Jobs
Offloads rendering work to worker threads:
- Better CPU utilization
- Reduced main thread bottlenecks
- Enabled on Windows, Linux, and Android
