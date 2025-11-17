# Application Icon Guide

## Current Status

The AdminGUI currently has no application icon. To add one:

### Option 1: Use Existing Icon (Quick)

If you have an `.ico` file:

1. Copy your icon file to: `src/StampService.AdminGUI/Resources/icon.ico`
2. Icon should be multi-resolution (16x16, 32x32, 48x48, 256x256)
3. Update project file (already configured)

### Option 2: Create Icon from PNG

If you have a PNG logo:

1. Use an online converter: https://icoconvert.com/
2. Upload your PNG
3. Generate multi-resolution ICO
4. Save as `src/StampService.AdminGUI/Resources/icon.ico`

### Option 3: Use Windows Default

For now, we'll use the default shield/key icon from Material Design.

### Recommended Icon Design

For a "Stamp Service" application, consider:
- ?? Shield with key
- ??? Security badge
- ?? Stamp/seal icon
- ?? Key with checkmark

### Technical Requirements

- Format: `.ico` file
- Sizes: 16x16, 32x32, 48x48, 256x256
- Transparent background
- Professional appearance
- Matches brand colors (Purple/Lime from Material Design)

## Implementation

Once you have `icon.ico`, the project is already configured:

```xml
<PropertyGroup>
    <ApplicationIcon>Resources\icon.ico</ApplicationIcon>
</PropertyGroup>
```

The icon will appear:
- In the title bar
- In the taskbar
- In Alt+Tab switcher
- In file explorer (for the .exe)
- In the installer

## Create Icon Resources

For Material Design-based icon, we're using the PackIcon system which provides:
- ShieldKey icon in window
- Consistent with Material Design theme
- No additional files needed

## Future Enhancement

For a custom icon:
1. Design icon with brand colors
2. Create multi-resolution ICO
3. Place in Resources folder
4. Rebuild project
5. Test on Windows 10 and 11
