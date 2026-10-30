using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Semi.Avalonia.Recolor;

/// <summary>
///     Runtime recoloring of the Semi.Avalonia theme.
///     Place it in <c>Application.Styles</c> right after <c>SemiTheme</c>:
///     <code>
///         &lt;Application.Styles&gt;
///             &lt;semi:SemiTheme Locale="ru-RU" /&gt;
///             &lt;recolor:SemiThemeRecolor PrimaryColor="#FF6A00" /&gt;
///         &lt;/Application.Styles&gt;
///     </code>
///     The accent color and its full tonal ramp (Primary, Secondary, Information, Link,
///     FocusBorder, selection highlight, …) are rebuilt from the user's color; the theme
///     background can be replaced as well. Works in both Light and Dark variants and
///     follows variant changes.
/// </summary>
public class SemiThemeRecolor : Styles
{
    /// <summary>Any Semi palette dictionary contains this raw color key.</summary>
    public const string PaletteMarkerKey = "SemiBlue5Color";

    public static readonly StyledProperty<Color?> PrimaryColorProperty =
        AvaloniaProperty.Register<SemiThemeRecolor, Color?>(nameof(PrimaryColor));

    public static readonly StyledProperty<Color?> PrimaryColorLightProperty =
        AvaloniaProperty.Register<SemiThemeRecolor, Color?>(nameof(PrimaryColorLight));

    public static readonly StyledProperty<Color?> PrimaryColorDarkProperty =
        AvaloniaProperty.Register<SemiThemeRecolor, Color?>(nameof(PrimaryColorDark));

    public static readonly StyledProperty<Color?> SecondaryColorProperty =
        AvaloniaProperty.Register<SemiThemeRecolor, Color?>(nameof(SecondaryColor));

    public static readonly StyledProperty<Color?> BackgroundColorProperty =
        AvaloniaProperty.Register<SemiThemeRecolor, Color?>(nameof(BackgroundColor));

    public static readonly StyledProperty<Color?> BackgroundColorLightProperty =
        AvaloniaProperty.Register<SemiThemeRecolor, Color?>(nameof(BackgroundColorLight));

    public static readonly StyledProperty<Color?> BackgroundColorDarkProperty =
        AvaloniaProperty.Register<SemiThemeRecolor, Color?>(nameof(BackgroundColorDark));

    private const int MaxAttempts = 8;

    private RecolorMaps? _lastMaps;
    private int _attempts;
    private Application? _application;

    public SemiThemeRecolor()
    {
        OwnerChanged += OnOwnerChanged;
        _application = Application.Current;
        if (_application is { } application)
        {
            application.ActualThemeVariantChanged += OnApplicationThemeVariantChanged;
        }

        Reapply();
    }

    /// <summary>The accent color used as-is in the dark theme; the light theme gets a
    /// lightness-adapted version. Set to <c>null</c> to keep the default blue.</summary>
    public Color? PrimaryColor
    {
        get => GetValue(PrimaryColorProperty);
        set => SetValue(PrimaryColorProperty, value);
    }

    /// <summary>Explicit accent override for the light theme.</summary>
    public Color? PrimaryColorLight
    {
        get => GetValue(PrimaryColorLightProperty);
        set => SetValue(PrimaryColorLightProperty, value);
    }

    /// <summary>Explicit accent override for the dark theme.</summary>
    public Color? PrimaryColorDark
    {
        get => GetValue(PrimaryColorDarkProperty);
        set => SetValue(PrimaryColorDarkProperty, value);
    }

    /// <summary>Companion accent color (the LightBlue family). When omitted, it is derived
    /// from the primary color the same way Semi derives LightBlue from Blue.</summary>
    public Color? SecondaryColor
    {
        get => GetValue(SecondaryColorProperty);
        set => SetValue(SecondaryColorProperty, value);
    }

    /// <summary>Application background color. In the dark theme a full Background0..4 ramp
    /// is derived from it; in the light theme only Background0 (the window base) is replaced.</summary>
    public Color? BackgroundColor
    {
        get => GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    /// <summary>Background override for the light theme; falls back to <see cref="BackgroundColor" />.</summary>
    public Color? BackgroundColorLight
    {
        get => GetValue(BackgroundColorLightProperty);
        set => SetValue(BackgroundColorLightProperty, value);
    }

    /// <summary>Background override for the dark theme; falls back to <see cref="BackgroundColor" />.</summary>
    public Color? BackgroundColorDark
    {
        get => GetValue(BackgroundColorDarkProperty);
        set => SetValue(BackgroundColorDarkProperty, value);
    }

    /// <summary>Applies the current property values to every Semi palette currently loaded
    /// in the application. Safe to call multiple times (idempotent).</summary>
    public void Reapply()
    {
        var application = ResolveApplication();
        if (application is null)
        {
            return;
        }

        var primary = PrimaryColor;
        var primaryLight = PrimaryColorLight;
        var primaryDark = PrimaryColorDark;
        var secondary = SecondaryColor;
        var backgroundDark = BackgroundColorDark ?? BackgroundColor;
        var backgroundLight = BackgroundColorLight ?? BackgroundColor;

        if (primary is null && primaryLight is null && primaryDark is null
            && secondary is null && backgroundDark is null && backgroundLight is null)
        {
            Reset();
            return;
        }

        // PrimaryColor is applied as-is in the dark theme; the light theme receives the same
        // hue with the lightness/chroma of the original light accent (contrast on white).
        var darkTarget = primaryDark
                         ?? primary
                         ?? (primaryLight is { } pl
                             ? RampBuilder.Adapt(pl, SemiPalette.LightBlue[5], SemiPalette.DarkBlue[5])
                             : (Color?)null);
        var lightTarget = primaryLight
                          ?? (primaryDark is { } pd
                              ? RampBuilder.Adapt(pd, SemiPalette.DarkBlue[5], SemiPalette.LightBlue[5])
                              : primary is { } p
                                  ? RampBuilder.Adapt(p, SemiPalette.DarkBlue[5], SemiPalette.LightBlue[5])
                                  : (Color?)null);

        var secondaryDark = secondary is { } sd
            ? sd
            : (Color?)null;
        var secondaryLight = secondary is { } sl
            ? RampBuilder.Adapt(sl, SemiPalette.DarkBlue[5], SemiPalette.LightBlue[5])
            : (Color?)null;

        var maps = PaletteMapBuilder.Build(darkTarget, lightTarget, secondaryDark, secondaryLight,
            backgroundDark, backgroundLight);

        if (_lastMaps is { } previous && !previous.IsEmpty)
        {
            ApplyMaps(application, PaletteMapBuilder.Invert(previous));
        }

        var visitor = new RecolorApplyVisitor(maps);
        ResourceWalker.WalkApplication(application, visitor);

        if (visitor.PaletteCount == 0)
        {
            // The Semi palette is not loaded yet (e.g. this style is created before
            // SemiTheme has been attached to the application) — try again later.
            if (_attempts < MaxAttempts)
            {
                _attempts++;
                ScheduleReapply();
            }

            return;
        }

        _attempts = 0;
        _lastMaps = maps;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property.OwnerType == typeof(SemiThemeRecolor))
        {
            ScheduleReapply();
        }
    }

    /// <summary>Restores the original Semi colors previously changed by this instance.</summary>
    public void Reset()
    {
        if (_lastMaps is not { } previous || previous.IsEmpty)
        {
            _lastMaps = null;
            return;
        }

        var application = ResolveApplication();
        if (application is not null)
        {
            ApplyMaps(application, PaletteMapBuilder.Invert(previous));
        }

        _lastMaps = null;
    }

    private static void ApplyMaps(Application application, RecolorMaps maps)
    {
        var visitor = new RecolorApplyVisitor(maps);
        ResourceWalker.WalkApplication(application, visitor);
    }

    private Application? ResolveApplication()
    {
        _application ??= Application.Current ?? Owner as Application;
        return _application;
    }

    private void ScheduleReapply()
    {
        Dispatcher.UIThread.Post(Reapply, DispatcherPriority.Background);
    }

    private void OnOwnerChanged(object? sender, EventArgs e) => ScheduleReapply();

    private void OnApplicationThemeVariantChanged(object? sender, EventArgs e) => Reapply();
}
