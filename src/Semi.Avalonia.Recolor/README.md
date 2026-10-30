# Semi.Avalonia.Recolor

Runtime accent (and background) recoloring for the [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
theme. Give it one HEX color — it rebuilds the whole accent palette in OKLCH and applies it live,
without touching any control theme by hand.

```xml
<Application.Styles>
    <semi:SemiTheme Locale="ru-RU" />
    <recolor:SemiThemeRecolor PrimaryColor="#FF6A00" />
</Application.Styles>
```

```xml
xmlns:semi="https://irihi.tech/semi"
xmlns:recolor="https://irihi.tech/semi.recolor"
```

## Why a special package is needed

Semi's control themes (`Themes/Light/*.axaml`, `Themes/Dark/*.axaml`, `Ursa.Themes.Semi`) alias the
palette through `<StaticResource x:Key="ButtonSolidPrimaryBackground" ResourceKey="SemiColorPrimary" />`.
Those aliases are **captured when the theme dictionary is materialized**, so simply adding an override
dictionary after `<semi:SemiTheme/>` does not affect already-created controls.

`SemiThemeRecolor` instead walks the entire resource graph reachable from `Application` (application
resources, every `Styles` tree, merged/theme dictionaries, control themes, setters) and mutates the
**shared `SolidColorBrush` instances** in the Semi palettes in place. Every `StaticResource` capture and
every `DynamicResource` lookup keeps pointing to the same brush, so buttons, check boxes, sliders, tabs,
`DataGrid`, `TreeDataGrid`, `ColorPicker` and `Ursa.Themes.Semi` are all recolored consistently.
Raw `<Color>` entries are replaced as well, so later re-materialization stays recolored.

## Tonal ramps

The original `SemiBlue0..9` / `SemiLightBlue0..9` scales are converted to OKLCH, the lightness curve and
chroma ratio are preserved, and your color becomes step 5 (the accent step in both variants):

* **Dark theme** — `PrimaryColor` is used as-is.
* **Light theme** — the same hue/chroma is moved to the lightness of the original light accent
  (`#0064FA`), so it stays readable on white.
* **Secondary** — derived from the primary exactly like Semi derives `LightBlue` from `Blue`
  (lightness/chroma/hue delta of the original pair), unless `SecondaryColor` is set.
* **Semantic colors** (`Primary`, `Pointerover`, `Active`, `Disabled`, `PrimaryLight*`, `Secondary*`,
  `Information*`, `Link*`, `FocusBorder`, text/`TextBox` selection highlight) are covered by the same maps.
* **AI gradients** (`SemiAIGeneral*`, `Colorful` buttons) are deliberately left untouched.

## Properties

| Property | Type | Behavior |
| --- | --- | --- |
| `PrimaryColor` | `Color?` | Accent as-is in dark; lightness-adapted in light. |
| `PrimaryColorLight` | `Color?` | Explicit light accent (overrides adaptation). |
| `PrimaryColorDark` | `Color?` | Explicit dark accent. |
| `SecondaryColor` | `Color?` | Companion accent (LightBlue family); derived from primary when null. |
| `BackgroundColor` | `Color?` | Background for both variants. |
| `BackgroundColorLight` | `Color?` | Light background, falls back to `BackgroundColor`. |
| `BackgroundColorDark` | `Color?` | Dark background, falls back to `BackgroundColor`. |

All properties are `StyledProperty<Color?>`, so XAML accepts `#RRGGBB`, `#AARRGGBB` and named colors.

Background rules:

* **Dark** — a `Background0..4` ramp plus `SemiColorNavBackground` / `SemiColorOverlayBackground` is
  derived from your color, following the original background lightness curve.
* **Light** — only `Background0` (the window base) is replaced; `Background1..4` stay white because they
  share the same value as `SemiColorWhite` (text on solid accent buttons must stay white).

Runtime API: `Reapply()` applies the current values again, `Reset()` restores the original Semi palette.
Changes to the properties and `Application.ActualThemeVariantChanged` are handled automatically.

## Compatibility

* Avalonia 12.x (tested with 12.1.0).
* Semi.Avalonia 12.1.0.1 (`Semi.Avalonia`, `Semi.Avalonia.DataGrid`, `Semi.Avalonia.TreeDataGrid`,
  `Semi.Avalonia.ColorPicker`) and `Ursa.Themes.Semi`.
* `net8.0` and `net10.0`.
* High-contrast variants (`Aquatic`, `Desert`, `Dusk`, `NightSky`) are system-color based and are not
  recolored.

The package depends only on `Avalonia`; it does not reference Semi.Avalonia and works by resource
keys/hex values.

## How it is wired

1. `SemiThemeRecolor : Styles` is placed after `SemiTheme` in `Application.Styles`.
2. On attach (and on every property/theme change) it builds `RecolorMaps` for both variants.
3. `ResourceWalker` recurses through `IResourceDictionary` / `ResourceInclude` / `StyleInclude` /
   `ControlTheme` / `Style` / setters and materializes deferred resource aliases.
4. `RecolorApplyVisitor` mutates brushes (`SolidColorBrush.Color`), replaces `Color` entries and the
   palette background keys. The previous maps are inverted before a new apply, so repeated calls are
   idempotent.

## License

MIT
