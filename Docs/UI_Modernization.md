# UI Modernization and Post-Processing

This document describes the dark theme UI modernization and CRT post-processing features added to uEmuera.

## Dark Theme UI

### Overview
The main menu and all UI dialogs have been modernized with a dark, professional theme that's easier on the eyes during extended gameplay sessions. The theme uses a carefully selected color palette inspired by classic ERA game aesthetics.

### Color Palette
The dark theme uses the following color scheme:
- **Background**: Very dark with slight blue tint (#141418, #1F1F26, #292933)
- **Accents**: Subtle blue-gray (#405580, #4D6B94)
- **Text**: Light gray for readability (#D9D9DD, #9999A6)
- **Buttons**: Dark with highlighted states for feedback

### UIStyleManager
The `UIStyleManager` class provides utilities for applying the dark theme:
```csharp
// Apply dark theme to a UI hierarchy
UIStyleManager.ApplyDarkTheme(gameObject);

// Add text effects
UIStyleManager.AddTextShadow(textComponent);
UIStyleManager.AddTextOutline(textComponent);
```

## CRT/Old Monitor Post-Processing

### Overview
The post-processing system adds an optional retro CRT monitor effect to recreate the visual experience of classic ERA games running on old monitors.

### Effects Included
1. **Vignette**: Darker edges simulating CRT display characteristics
2. **Chromatic Aberration**: Color separation at screen edges
3. **Grain**: Subtle noise for that old monitor texture
4. **Color Grading**: Slight warm tint with desaturated colors
5. **Bloom**: Soft glow effect simulating phosphor glow

### Usage

#### Enabling/Disabling Post-Processing
Users can toggle the CRT effect through the Settings menu:
1. Open the main menu (⚙️ icon)
2. Select "Settings"
3. Click the "CRT Monitor Effect" toggle

The setting persists between sessions via PlayerPrefs.

#### Programmatic Access
```csharp
// Check if post-processing is enabled
bool isEnabled = PostProcessingManager.instance.IsEnabled();

// Toggle post-processing
PostProcessingManager.instance.TogglePostProcessing();

// Set specific state
PostProcessingManager.instance.SetPostProcessingEnabled(true);
```

### Technical Details

#### PostProcessingManager
The `PostProcessingManager` singleton handles all post-processing functionality:
- Automatic initialization on startup
- Profile configuration with CRT-appropriate settings
- Settings persistence
- Camera integration

#### Layer Setup
Post-processing uses the "PostProcessing" layer (Layer 7). This is automatically configured in the TagManager.

#### Performance
The post-processing effects are optimized for mobile devices:
- Low intensity settings to minimize GPU load
- Uses Unity's built-in Post Processing Stack v2
- Minimal performance impact (~1-2ms per frame on modern devices)

## Localization

All new UI strings are fully localized:
- English (EN-US)
- Japanese (JP)
- Chinese Simplified (ZH-CN)
- German (DE)

### New Localization Keys
- `Options.MenuPad.Menu1.settings.Text` - Settings menu button
- `Options.SettingsBox.border.titlebar.title` - Settings dialog title
- `Options.SettingsBox.postprocessing.Text` - Post-processing option label
- `[PostProcessingOn]` - ON state text
- `[PostProcessingOff]` - OFF state text

## Integration

### FirstWindow
The FirstWindow now automatically applies dark theme styling on startup:
```csharp
void Start()
{
    // ... existing code ...
    ApplyDarkTheme();
    // ... existing code ...
}
```

### OptionWindow
All menu dialogs apply dark theme styling:
- Menu 1 (main options)
- Menu 2 (in-game options)
- Resolution settings
- Language selection
- Settings dialog
- Message boxes

### MainEntry
PostProcessingManager is initialized early in the application lifecycle:
```csharp
void Awake()
{
    // ... existing code ...
    InitializePostProcessing();
}
```

## Future Enhancements

Potential future improvements:
1. Additional post-processing presets (e.g., different monitor types)
2. Intensity slider for fine-tuning effects
3. Custom scanline patterns
4. More color grading options
5. Per-game post-processing preferences
