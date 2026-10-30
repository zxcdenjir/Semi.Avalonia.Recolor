# SemiThemeRecolor

**Bring your own Semi.Design colors to Avalonia — without hand-editing control styles.**

[Русская версия](README.md)

SemiThemeRecolor builds accent palettes from a chosen color and overrides the resource keys used by Semi.Avalonia controls. Changes apply immediately and follow the active light or dark theme.

## Features

- Recolors Primary, Secondary, information resources, and related Semi control states.
- Accepts separate Light and Dark backgrounds; Dark receives a `Background0–4` ramp.
- Optional Ursa package with Ursa resource overrides and dialog hosts for Avalonia pages.
- `ImagePicker` with preview and PNG, JPEG, WebP, and BMP selection; GIF is excluded.
- Android haptic feedback with global defaults and per-button overrides.
- `SemiSpaced` class for `ItemsControl` with spacing set to 12.

The core package does not depend on Semi.Avalonia or Ursa. Ursa support is available as a separate integration package.

## Installation

After the packages are published to NuGet, install the core package:

```sh
dotnet add package Semi.Avalonia.Recolor
```

For applications that use Ursa, install the optional package as well:

```sh
dotnet add package Semi.Avalonia.Recolor.Ursa
```

## Quick start

Place the recolor style after the Semi theme in `App.axaml`:

```xml
<Application
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:semi="https://irihi.tech/semi"
    xmlns:recolor="https://irihi.tech/semi.recolor">
    <Application.Styles>
        <semi:SemiTheme />
        <recolor:SemiThemeRecolor
            PrimaryColor="#FF6A00"
            BackgroundColorDark="#1B1B22"
            BackgroundColorLight="#F3F5F9" />
    </Application.Styles>
</Application>
```

Set only `PrimaryColor` for an accent that adapts automatically to Light. You can also set `PrimaryColorLight`, `PrimaryColorDark`, `SecondaryColor`, `BackgroundColorLight`, and `BackgroundColorDark` independently.

### Ursa integration

Load the Ursa theme and integration styles after the Semi theme:

```xml
<Application
    xmlns="https://github.com/avaloniaui"
    xmlns:semi="https://irihi.tech/semi"
    xmlns:ursa="https://irihi.tech/ursa"
    xmlns:recolorUrsa="clr-namespace:Semi.Avalonia.Recolor.Ursa;assembly=Semi.Avalonia.Recolor.Ursa"
    xmlns:recolor="https://irihi.tech/semi.recolor">
    <Application.Styles>
        <semi:SemiTheme />
        <ursa:UrsaSemiTheme />
        <recolorUrsa:UrsaRecolorStyles />
        <recolor:SemiThemeRecolor PrimaryColor="#FF6A00" />
    </Application.Styles>
</Application>
```

`UrsaRecolorStyles` adds an `OverlayDialogHost` to the outer Avalonia page when no matching host exists. It also adjusts the `UrsaWindow` title bar margin so navigation and drawer buttons remain outside the window drag area. Configure a page with `PageOverlayDialogHost` attached properties:

```xml
<ContentPage
    xmlns:recolorUrsa="clr-namespace:Semi.Avalonia.Recolor.Ursa;assembly=Semi.Avalonia.Recolor.Ursa"
    recolorUrsa:PageOverlayDialogHost.IsEnabled="True"
    recolorUrsa:PageOverlayDialogHost.UseTopLevelOverlay="True" />
```

## Extra controls

### ImagePicker

```xml
<recolor:ImagePicker
    Image="{Binding SelectedImage, Mode=TwoWay}"
    ImagePath="{Binding SelectedImagePath, Mode=TwoWay}"
    CornerRadius="12"
    Stretch="UniformToFill" />
```

The `Placeholder` property accepts any Avalonia `Control`. A selected image appears immediately, and the reset button clears both `Image` and `ImagePath`. By default, files are limited to 30 MiB and images are decoded to at most 2048 pixels on their longest side. Both limits are configurable.

### Android haptics

Enable feedback globally:

```xml
<recolor:SemiThemeRecolor EnableHaptic="True" HapticType="Click" />
```

Or configure a button directly:

```xml
<Button
    Content="Save"
    recolor:HapticFeedback.IsEnabled="True"
    recolor:HapticFeedback.Type="Confirm" />
```

Add this permission to `Platforms/Android/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.VIBRATE" />
```

The built-in adapter targets Android. `HapticFeedback.PlatformFeedback` is available for a custom platform implementation.

### ItemsControl spacing

```xml
<ItemsControl Classes="SemiSpaced">
    <TextBlock Text="First item" />
    <TextBlock Text="Second item" />
</ItemsControl>
```

The class uses a vertical `StackPanel` with `Spacing="12"`. Avoid it when the control requires virtualization or a different items panel.

## Persisting a user's accent

The library does not select a settings store. Save the chosen color using your app's existing settings API and restore it before showing the first window:

```csharp
if (Color.TryParse(savedAccent, out var accent))
{
    recolor.PrimaryColor = accent;
    recolor.Reapply();
}
```

For example, store a HEX string in Android Preferences or your app's preferred settings store on other platforms.

## Demo and resource catalog

The demo includes a ColorPicker, Light/Dark switching, Semi and Ursa controls, ImagePicker, and Haptic settings:

```sh
dotnet run --project demo/SemiThemeRecolor.Demo/SemiThemeRecolor.Demo.csproj
```

The resource inventory is in [`docs/palette-resource-catalog.csv`](docs/palette-resource-catalog.csv). It is generated from Semi.Avalonia and Ursa source trees:

```sh
python tools/generate_palette_catalog.py --semi-root ../Semi.Avalonia --ursa-root ../Ursa.Avalonia
python tools/generate_palette_catalog.py --check
```

The first command updates the CSV, summary, and C# catalog. `--check` reports stale generated files. Tests also check that the compiled catalog matches the CSV.

## Compatibility

- Avalonia 12.x; the project currently builds against Avalonia 12.1.0.
- Semi.Avalonia 12.1.0.1 and Ursa 2.2.0.
- `net8.0` and `net10.0`.
- ImagePicker uses `TopLevel.StorageProvider`.
- The built-in Haptic adapter supports Android.

## Development

```sh
dotnet build SemiThemeRecolor.slnx
dotnet test tests/Semi.Avalonia.Recolor.Tests/Semi.Avalonia.Recolor.Tests.csproj
```

MIT licensed. See [LICENSE](LICENSE).
