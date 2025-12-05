# Copilot Instructions for uEmuera

## Project Overview

uEmuera is a Unity3D port of Emuera (Emulator of Eramaker), a text-based game platform originally developed for Windows. This project leverages Unity3D's cross-platform capabilities to enable running era script games on non-Windows platforms, particularly Android devices.

The project is based on emuera1824v15 source code and is licensed under the Apache License 2.0.

## Technology Stack

- **Engine**: Unity 6000.2.14f1 (Unity 6)
- **Language**: C# 9.0
- **Target Framework**: .NET Standard 2.1
- **Primary Platforms**: Android, Windows Standalone

### Allowed Packages

The following Unity packages are approved for use in this project when it makes sense:

- **com.unity.mathematics**: High-performance math library for SIMD-friendly operations
- **com.unity.collections**: Native collection types for performance-critical code
- **com.unity.burst**: Burst compiler for optimizing performance-critical code paths
- **com.unity.textmeshpro**: Advanced text rendering (TextMeshPro)

## Architecture Overview

### Threading Model

The game uses a **framerate-independent architecture**:

- **Game Thread (`EmueraThread`)**: Runs game logic on a dedicated background thread, independent of Unity's frame rate. Uses `ManualResetEventSlim` for efficient input waiting with timeout for timer updates.
- **Main Thread (Unity)**: Handles rendering, UI updates, and user input. Communicates with the game thread via thread-safe mechanisms.
- **On-Demand Rendering (`OnDemandRenderManager`)**: Reduces power consumption by only rendering when there's activity (input or content changes).

Key thread-safety patterns:
- Use `volatile` for simple flags accessed across threads
- Use `lock` for complex state that requires atomic updates
- Use `ManualResetEventSlim` for efficient thread signaling

### Data Flow

```
User Input (Main Thread) -> EmueraThread.Input() -> Game Logic (Background Thread)
                                                          |
                                                          v
Game Output (Background Thread) -> GenericUtils.AddText() -> EmueraContent (Main Thread)
                                                                     |
                                                                     v
                                                        OnDemandRenderManager.SetContentDirty()
```

## Project Structure

```
uEmuera/
├── Assets/
│   ├── Scripts/                    # Main source code
│   │   ├── Emuera/                 # Core Emuera engine code
│   │   │   ├── Config/             # Configuration management
│   │   │   ├── Content/            # Content and asset management
│   │   │   ├── GameData/           # Game data structures and expressions
│   │   │   ├── GameProc/           # Game processing and execution
│   │   │   ├── GameView/           # Console and display rendering
│   │   │   ├── Sub/                # Utility classes and parsers
│   │   │   └── _Library/           # Library utilities (SFMT, GDI, etc.)
│   │   ├── uEmuera/                # Unity-specific adaptations
│   │   │   ├── Drawing.cs          # Drawing primitives (Color, Point, etc.)
│   │   │   ├── Forms.cs            # Thread-safe Timer and UI stubs
│   │   │   └── ...                 # Other platform abstraction
│   │   ├── EmueraMain.cs           # Main application controller
│   │   ├── EmueraThread.cs         # Game execution thread (framerate-independent)
│   │   ├── EmueraContent.cs        # Content rendering
│   │   ├── OnDemandRenderManager.cs # Power-efficient rendering
│   │   └── ...                     # UI and utility scripts
│   ├── Tests/                      # Unit tests (EditMode and PlayMode)
│   ├── Localization/               # Localization data
│   └── Resources/                  # Unity resources
├── ProjectSettings/                # Unity project settings
└── Packages/                       # Unity package manager
```

## Key Components

### Core Engine (Assets/Scripts/Emuera/)

- **GameProc/**: Contains the main game processing logic including:
  - `Process.cs`: Main game process controller
  - `ErbLoader.cs`: ERA script file loader
  - `LogicalLineParser.cs`: Script parsing logic
  - `Function/`: Built-in function implementations

- **GameData/**: Data structures including:
  - `Variable/`: Variable management and evaluation
  - `Expression/`: Expression parsing and evaluation
  - `Function/`: Function definitions

- **GameView/**: Display and rendering:
  - `EmueraConsole.cs`: Main console implementation
  - `ConsoleDisplayLine.cs`: Line rendering
  - `PrintStringBuffer.cs`: Output buffering

### Unity Integration (Assets/Scripts/)

- `EmueraMain.cs`: Main Unity MonoBehaviour, manages game lifecycle
- `EmueraThread.cs`: Handles game execution on background thread (framerate-independent)
- `EmueraContent.cs`: Manages content display in Unity UI
- `OnDemandRenderManager.cs`: Controls rendering frequency based on activity
- `FirstWindow.cs`: Initial file selection screen

### Platform Abstraction (Assets/Scripts/uEmuera/)

- `Drawing.cs`: Platform-independent drawing primitives (`Color` as readonly struct, `Point`, `Rectangle`, etc.)
- `Forms.cs`: Thread-safe `Timer` class and UI stubs
- `Window.cs`: Window abstraction

## Coding Conventions

### Naming Conventions

- **Classes**: PascalCase (e.g., `EmueraConsole`, `GameProcess`)
- **Public Methods**: PascalCase (e.g., `Run()`, `Clear()`)
- **Private Fields**: snake_case with trailing underscore (e.g., `working_`, `canvas_scaler_`)
- **Local Variables**: snake_case (e.g., `console_lines`, `emuera_width`)
- **Constants**: UPPER_SNAKE_CASE or PascalCase depending on context

### Documentation Style

- Use XML documentation comments (`///`) for public members
- Include `<summary>`, `<param>`, and `<returns>` tags where applicable
- Add `[Tooltip("...")]` attributes to serialized Unity fields
- Document thread-safety requirements for public methods

### Code Style

- Use `var` for local variable declarations when the type is obvious
- Prefer explicit null checks over null-conditional operators in Unity code
- Use `readonly struct` for immutable value types (e.g., `Color`)
- Implement `IEquatable<T>` for value types used in collections
- Use `HashCode.Combine()` for proper hash code generation
- Use pattern matching with `switch` expressions where applicable
- Keep Unity-specific code separate from core Emuera logic

### Thread Safety Guidelines

- Mark shared state accessed from multiple threads as `volatile` or use locks
- Use `ManualResetEventSlim` for efficient thread signaling
- Avoid `Thread.Abort()` - use cooperative cancellation instead
- Document thread-safety requirements in XML comments

## File Encoding

**Important**: All era-related files must use UTF-8 encoding:
- `*.csv` files
- `*.ERB` files (ERA script files)
- `*.ERH` files (ERA header files)

## Building and Testing

### Unity Editor

1. Open the project in Unity 6000.2.x
2. Build settings are configured for Android and Windows Standalone
3. Tests are located in `Assets/Tests/` with EditMode and PlayMode folders

### Running Tests

- Use Unity Test Framework via Window > General > Test Runner
- EditMode tests run without Play mode
- PlayMode tests require entering Play mode

## Contributing Guidelines

1. Follow existing code style and naming conventions
2. Add XML documentation for new public APIs
3. Ensure UTF-8 encoding for all text files
4. Test on both Android and Windows platforms when possible
5. Keep Unity-specific code separate from core engine logic
6. Ensure thread safety for code that may be called from the game thread
7. Use `readonly struct` for new value types when immutability is appropriate

## Common Tasks

### Adding New Game Instructions

1. Implement in `Assets/Scripts/Emuera/GameProc/Function/`
2. Register in the appropriate instruction handler
3. Follow existing patterns for argument parsing

### Modifying Console Output

1. Work with `EmueraConsole.cs` and related classes in `GameView/`
2. Use `ConsoleDisplayLine` for line-based rendering
3. Respect the existing color and style system
4. Remember: console updates happen on the game thread, rendering on main thread

### UI Changes

1. Unity UI components are in the main scene
2. Use `Canvas` and `CanvasScaler` for responsive design
3. Follow the existing pattern in `EmueraContent.cs` for content display
4. Call `OnDemandRenderManager.SetContentDirty()` when visual content changes

### Performance Optimization

1. Use `SpinWait` for short waits instead of `Thread.Sleep`
2. Use object pooling for frequently created objects (see `EmueraContent` line/image pools)
3. Minimize allocations in the game loop
4. Consider using `com.unity.collections` for native containers in performance-critical code
