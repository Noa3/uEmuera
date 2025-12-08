# Unity Editor Instructions: Adding Settings UI

This document provides step-by-step instructions for adding the Settings UI elements to the Options prefab/scene in Unity Editor.

## Overview

The Settings UI consists of:
1. A Settings button in Menu1
2. A SettingsBox dialog (similar to LanguageBox or MsgBox)
3. Two toggle buttons: CRT Monitor Effect and Pixel Perfect Rendering

## Step 1: Add Settings Button to Menu1

1. Open the Main scene in Unity Editor
2. In the Hierarchy, find: `Options > MenuPad > Menu1 > border`
3. Right-click on `border` and select `UI > Button`
4. Rename it to `settings`
5. Position it below the `language` button:
   - Copy the Transform component settings from `language`
   - Adjust Y position: Move it down by about 60 units
6. Configure the Button:
   - Text: "Settings" (or use localization: `Options.MenuPad.Menu1.settings.Text`)
   - Colors: Use dark theme colors
     - Normal Color: RGB(46, 46, 56) #2E2E38
     - Highlighted: RGB(56, 71, 96) #384760
     - Pressed: RGB(38, 51, 77) #26334D
7. In the Options component (on the Options GameObject):
   - Drag the `settings` button to the `menu_1_settings` field

## Step 2: Create SettingsBox Container

1. In Hierarchy, find: `Options`
2. Right-click `Options` and select `Create Empty`
3. Rename it to `SettingsBox`
4. Add component: `Image`
   - Color: Black with alpha 217 (0, 0, 0, 217) - 85% opacity
   - Raycast Target: Yes (to block clicks)
5. Configure RectTransform:
   - Anchors: Stretch (0,0,1,1)
   - Left: 0, Top: 0, Right: 0, Bottom: 0
6. Set active to `false` (it should be hidden by default)

## Step 3: Create Settings Dialog Border

1. Right-click `SettingsBox` and select `UI > Panel`
2. Rename it to `border`
3. Configure RectTransform:
   - Anchors: Center (0.5, 0.5)
   - Width: 600, Height: 450
   - Position: (0, 0, 0)
4. Configure Image:
   - Color: RGB(31, 31, 38) #1F1F26 (Dark theme background)

## Step 4: Add Title Bar

1. Right-click `border` and select `UI > Panel`
2. Rename it to `titlebar`
3. Configure RectTransform:
   - Anchors: Top stretch (0, 1, 1, 1)
   - Height: 60
   - Left: 0, Top: 0, Right: 0
4. Add child: `UI > Text`
   - Name: `title`
   - Text: "Settings"
   - Font Size: 24
   - Alignment: Center
   - Color: RGB(217, 217, 221) #D9D9DD
   - Add Shadow component for depth

## Step 5: Add Close Button

1. Right-click `border` and select `UI > Button`
2. Rename it to `close`
3. Configure RectTransform:
   - Anchors: Top-right (1, 1)
   - Pivot: (1, 1)
   - Position: (-10, -10, 0)
   - Width: 80, Height: 40
4. Configure Button colors (same as settings button)
5. Change button text to "Close"
6. In Options component:
   - Drag this button to `settings_close` field

## Step 6: Create Post-Processing Toggle

1. Right-click `border` and select `UI > Button`
2. Rename it to `postprocessing`
3. Configure RectTransform:
   - Anchors: Center (0.5, 0.5)
   - Position: (0, 50, 0)
   - Width: 500, Height: 60
4. Configure Button colors
5. Add child Text for label:
   - Name: `label`
   - Text: "CRT Monitor Effect"
   - Anchors: Left stretch (0, 0, 0.7, 1)
   - Alignment: Middle Left
   - Padding: 15px left
6. Add child Text for state:
   - Name: `statetext`
   - Text: "ON"
   - Anchors: Right stretch (0.7, 0, 1, 1)
   - Alignment: Middle Right
   - Padding: 15px right
   - Font Style: Bold
   - Color: RGB(64, 85, 128) #405580 (Accent color)
7. Add child Text for icon:
   - Name: `icon`
   - Text: "✓"
   - Font Size: 24
   - Anchors: Center at (0.85, 0.5)
   - Width: 20, Height: 20
   - Color: RGB(77, 107, 148) #4D6B94
8. In Options component:
   - Drag `postprocessing` button to `settings_postprocessing_toggle`
   - Drag `icon` to `settings_postprocessing_icon`
   - Drag `statetext` to `settings_postprocessing_text`

## Step 7: Create Pixel Perfect Toggle

1. Duplicate the `postprocessing` button (Ctrl+D or Cmd+D)
2. Rename it to `pixelperfect`
3. Change Y position to -50 (below the post-processing toggle)
4. Update the label text to "Pixel Perfect Rendering"
5. In Options component:
   - Drag `pixelperfect` button to `settings_pixelperfect_toggle`
   - Drag its `icon` to `settings_pixelperfect_icon`
   - Drag its `statetext` to `settings_pixelperfect_text`

## Step 8: Apply Dark Theme (Optional but Recommended)

For professional look, ensure all elements use the dark theme colors:

### Background Colors
- Very dark: #141418
- Medium dark: #1F1F26
- Light dark: #292933

### Text Colors
- Primary: #D9D9DD
- Secondary: #9999A6

### Button Colors
- Normal: #2E2E38
- Highlight: #384760
- Pressed: #26334D

### Accent Colors
- Primary: #405580
- Secondary: #4D6B94

## Step 9: Test the UI

1. Enter Play mode
2. Click the options/gear icon from the main menu
3. Click "Settings" in Menu1
4. Verify the SettingsBox appears
5. Click the toggles to ensure they work
6. Click Close to dismiss

## Localization

The UI uses these localization keys (already added to language files):
- `Options.MenuPad.Menu1.settings.Text` - "Settings" button label
- `Options.SettingsBox.border.titlebar.title` - "Settings" dialog title
- `Options.SettingsBox.postprocessing.Text` - "CRT Monitor Effect" label
- `Options.SettingsBox.pixelperfect.Text` - "Pixel Perfect Rendering" label
- `[PostProcessingOn]` - "ON"
- `[PostProcessingOff]` - "OFF"
- `[PixelPerfectOn]` - "ON"
- `[PixelPerfectOff]` - "OFF"

## Visual Reference

### Layout Structure
```
Options
└── SettingsBox (hidden by default, full screen overlay)
    └── border (centered panel, 600x450)
        ├── titlebar (top, 60px height)
        │   └── title (centered text)
        ├── close (top-right button)
        ├── postprocessing (center-top toggle)
        │   ├── label (left-aligned text)
        │   ├── statetext (right-aligned "ON"/"OFF")
        │   └── icon (checkmark)
        └── pixelperfect (center-bottom toggle)
            ├── label (left-aligned text)
            ├── statetext (right-aligned "ON"/"OFF")
            └── icon (checkmark)
```

### Visual Mockup
```
┌─────────────────────────────────────────────────────┐
│ Settings                                    [Close] │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌───────────────────────────────────────────────┐ │
│  │ CRT Monitor Effect              ✓        ON   │ │
│  └───────────────────────────────────────────────┘ │
│                                                     │
│  ┌───────────────────────────────────────────────┐ │
│  │ Pixel Perfect Rendering         ✓        ON   │ │
│  └───────────────────────────────────────────────┘ │
│                                                     │
└─────────────────────────────────────────────────────┘
```

## Notes

- The SettingsBox should follow the same pattern as LanguageBox and MsgBox in the scene
- All UI elements should use the dark theme color scheme for consistency
- Text elements should have Shadow components for better readability
- Make sure to set SettingsBox to inactive by default (unchecked in Inspector)
- The toggles' onClick events are handled by code, no need to wire them manually
