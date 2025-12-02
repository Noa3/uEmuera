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
│   │   ├── EmueraMain.cs           # Main application controller
│   │   ├── EmueraThread.cs         # Game execution thread
│   │   ├── EmueraContent.cs        # Content rendering
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
- `EmueraThread.cs`: Handles game execution threading
- `EmueraContent.cs`: Manages content display in Unity UI
- `FirstWindow.cs`: Initial file selection screen

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

### Code Style

- Use `var` for local variable declarations when the type is obvious
- Prefer explicit null checks over null-conditional operators in Unity code
- Use coroutines (`IEnumerator`) for async operations in Unity
- Keep Unity-specific code separate from core Emuera logic

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

## Common Tasks

### Adding New Game Instructions

1. Implement in `Assets/Scripts/Emuera/GameProc/Function/`
2. Register in the appropriate instruction handler
3. Follow existing patterns for argument parsing

### Modifying Console Output

1. Work with `EmueraConsole.cs` and related classes in `GameView/`
2. Use `ConsoleDisplayLine` for line-based rendering
3. Respect the existing color and style system

### UI Changes

1. Unity UI components are in the main scene
2. Use `Canvas` and `CanvasScaler` for responsive design
3. Follow the existing pattern in `EmueraContent.cs` for content display
