using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;

using Semi.Avalonia.Recolor.Haptics;

namespace Semi.Avalonia.Recolor;

/// <summary>
///     Перекрашивает ресурсы темы Semi.Avalonia во время работы приложения.
///     Добавьте стиль после <c>SemiTheme</c> в <c>Application.Styles</c>:
///     <code>
///         &lt;Application.Styles&gt;
///             &lt;semi:SemiTheme Locale="ru-RU" /&gt;
///             &lt;recolor:SemiThemeRecolor PrimaryColor="#FF6A00" /&gt;
///         &lt;/Application.Styles&gt;
///     </code>
///     Библиотека создаёт палитры Primary и Secondary в OKLCH и переопределяет связанные с ними
///     ресурсы Semi и установленных дополнений. Ресурсы светлой и тёмной темы готовятся заранее.
/// </summary>
public class SemiThemeRecolor : Styles
{
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

    public static readonly StyledProperty<bool> EnableHapticProperty =
        AvaloniaProperty.Register<SemiThemeRecolor, bool>(nameof(EnableHaptic));

    public static readonly StyledProperty<HapticType> HapticTypeProperty =
        AvaloniaProperty.Register<SemiThemeRecolor, HapticType>(nameof(HapticType), HapticType.Click);


    private readonly Dictionary<string, SolidColorBrush> _overrideBrushes = new(StringComparer.Ordinal);
    private RecolorInput? _cachedInput;
    private RecolorMaps? _cachedMaps;
    private Application? _application;

    public SemiThemeRecolor()
    {
        InitializeThemeResourceDictionaries();

        Add(new Style(selector => selector.OfType<ItemsControl>().Class("SemiSpaced"))
        {
            Setters =
            {
                new Setter(ItemsControl.ItemsPanelProperty,
                    new FuncTemplate<Panel>(() => new StackPanel { Spacing = 12 })),
            },
        });

        OwnerChanged += OnOwnerChanged;
        _application = Application.Current;

        // Initial XAML values are applied once the style is attached to Application.Styles.
    }

    /// <summary>Акцент для тёмной темы. Для светлой темы цвет адаптируется по светлоте.
    /// Значение <see langword="null"/> сохраняет стандартную синюю палитру.</summary>
    public Color? PrimaryColor
    {
        get => GetValue(PrimaryColorProperty);
        set => SetValue(PrimaryColorProperty, value);
    }

    /// <summary>Явный акцент светлой темы.</summary>
    public Color? PrimaryColorLight
    {
        get => GetValue(PrimaryColorLightProperty);
        set => SetValue(PrimaryColorLightProperty, value);
    }

    /// <summary>Явный акцент тёмной темы.</summary>
    public Color? PrimaryColorDark
    {
        get => GetValue(PrimaryColorDarkProperty);
        set => SetValue(PrimaryColorDarkProperty, value);
    }

    /// <summary>Дополнительный акцент (семейство LightBlue). Если значение не задано,
    /// цвет вычисляется на основе Primary с сохранением пропорций палитры Semi.</summary>
    public Color? SecondaryColor
    {
        get => GetValue(SecondaryColorProperty);
        set => SetValue(SecondaryColorProperty, value);
    }

    /// <summary>Фон приложения для обеих тем. Для тёмной темы строится шкала Background0–4;
    /// в светлой заменяется только Background0.</summary>
    public Color? BackgroundColor
    {
        get => GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    /// <summary>Фон светлой темы. Если он не задан, используется <see cref="BackgroundColor"/>.</summary>
    public Color? BackgroundColorLight
    {
        get => GetValue(BackgroundColorLightProperty);
        set => SetValue(BackgroundColorLightProperty, value);
    }

    /// <summary>Фон тёмной темы. Если он не задан, используется <see cref="BackgroundColor"/>.</summary>
    public Color? BackgroundColorDark
    {
        get => GetValue(BackgroundColorDarkProperty);
        set => SetValue(BackgroundColorDarkProperty, value);
    }

    /// <summary>Включает тактильный отклик для кнопок без локальной настройки.</summary>
    public bool EnableHaptic
    {
        get => GetValue(EnableHapticProperty);
        set => SetValue(EnableHapticProperty, value);
    }

    /// <summary>Тип тактильного отклика по умолчанию.</summary>
    public HapticType HapticType
    {
        get => GetValue(HapticTypeProperty);
        set => SetValue(HapticTypeProperty, value);
    }

    /// <summary>Повторно применяет текущие значения свойств к переопределениям ресурсов.
    /// Метод можно вызывать несколько раз.</summary>
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

        var input = new RecolorInput(darkTarget, lightTarget, secondaryDark, secondaryLight,
            backgroundDark, backgroundLight);
        if (_cachedInput != input || _cachedMaps is null)
        {
            _cachedInput = input;
            _cachedMaps = PaletteMapBuilder.Build(darkTarget, lightTarget, secondaryDark, secondaryLight,
                backgroundDark, backgroundLight);
        }

        // Install both variants up front. Waiting for ActualThemeVariantChanged during app
        // startup lets controls briefly resolve Semi's original palette (especially when the
        // system theme is discovered after the first window is shown).
        ApplyPaletteOverrides(_cachedMaps, isDark: false);
        ApplyPaletteOverrides(_cachedMaps, isDark: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property.OwnerType == typeof(SemiThemeRecolor))
        {
            if (change.Property == EnableHapticProperty)
                HapticFeedback.IsGloballyEnabled = EnableHaptic;
            else if (change.Property == HapticTypeProperty)
                HapticFeedback.DefaultType = HapticType;
            else if (Owner is not null)
                Reapply();
        }
    }

    /// <summary>Удаляет переопределения и восстанавливает исходные цвета Semi.</summary>
    public void Reset()
    {
        if (Resources is { } resources)
        {
            foreach (var variant in new[] { ThemeVariant.Default, ThemeVariant.Light, ThemeVariant.Dark })
            {
                if (resources.ThemeDictionaries.TryGetValue(variant, out var provider)
                    && provider is ResourceDictionary dictionary)
                {
                    foreach (var key in PaletteResourceCatalog.Targets)
                        dictionary.Remove(key.ResourceKey);
                }
            }
        }

        _overrideBrushes.Clear();
    }

    private Application? ResolveApplication()
    {
        _application ??= Application.Current ?? Owner as Application;
        return _application;
    }

    private void OnOwnerChanged(object? sender, EventArgs e)
    {
        Reapply();
    }

    private readonly record struct RecolorInput(Color? Dark, Color? Light, Color? SecondaryDark,
        Color? SecondaryLight, Color? BackgroundDark, Color? BackgroundLight);

    private void ApplyPaletteOverrides(RecolorMaps maps, bool isDark)
    {
        var variants = isDark
            ? new[] { ThemeVariant.Dark }
            : new[] { ThemeVariant.Default, ThemeVariant.Light };
        foreach (var variant in variants)
        {
            var dictionary = GetThemeDictionary(variant);

            foreach (var target in PaletteResourceCatalog.Targets)
            {
                if (!TryMapResourceTarget(target.SourcePaletteKey, maps, isDark, out var color, out var opacity))
                {
                    dictionary.Remove(target.ResourceKey);
                    _overrideBrushes.Remove(GetBrushCacheKey(variant, target.ResourceKey));
                    continue;
                }

                var cacheKey = GetBrushCacheKey(variant, target.ResourceKey);
                if (!_overrideBrushes.TryGetValue(cacheKey, out var brush))
                {
                    brush = new SolidColorBrush(color, opacity);
                    _overrideBrushes.Add(cacheKey, brush);
                }
                else
                {
                    if (brush.Color != color)
                        brush.Color = color;
                    if (brush.Opacity != opacity)
                        brush.Opacity = opacity;
                }

                if (!dictionary.TryGetValue(target.ResourceKey, out var current) || !ReferenceEquals(current, brush))
                    dictionary[target.ResourceKey] = brush;
            }
        }
    }

    private void InitializeThemeResourceDictionaries()
    {
        Resources ??= new ResourceDictionary();
        _ = GetThemeDictionary(ThemeVariant.Default);
        _ = GetThemeDictionary(ThemeVariant.Light);
        _ = GetThemeDictionary(ThemeVariant.Dark);
    }

    private ResourceDictionary GetThemeDictionary(ThemeVariant variant)
    {
        var resources = Resources ??= new ResourceDictionary();
        if (resources.ThemeDictionaries.TryGetValue(variant, out var provider)
            && provider is ResourceDictionary dictionary)
            return dictionary;

        dictionary = new ResourceDictionary();
        resources.ThemeDictionaries[variant] = dictionary;
        return dictionary;
    }

    private static string GetBrushCacheKey(ThemeVariant variant, string resourceKey) =>
        $"{variant.Key}:{resourceKey}";

    private static bool TryMapResourceTarget(string sourceKey, RecolorMaps maps, bool isDark,
        out Color color, out double opacity)
    {
        color = default;
        opacity = 1;

        if (sourceKey.StartsWith("SemiColorBackground", StringComparison.Ordinal)
            && int.TryParse(sourceKey.AsSpan("SemiColorBackground".Length), out var backgroundIndex))
        {
            if (isDark && maps.DarkBackgroundRamp is { } backgroundRamp && backgroundIndex < backgroundRamp.Length)
            {
                color = backgroundRamp[backgroundIndex];
                return true;
            }

            if (!isDark && backgroundIndex == 0 && maps.LightBackground0 is { } lightBackground)
            {
                color = lightBackground;
                return true;
            }

            return false;
        }

        if (sourceKey == "SemiColorNavBackground" && isDark && maps.DarkBackgroundRamp is { Length: > 1 } navRamp)
        {
            color = navRamp[1];
            return true;
        }

        if (sourceKey == "SemiColorOverlayBackground" && isDark && maps.DarkBackgroundRamp is { Length: > 0 } overlayRamp)
        {
            color = overlayRamp[0];
            opacity = 0.6;
            return true;
        }

        var isPrimary = sourceKey.StartsWith("SemiColorPrimary", StringComparison.Ordinal);
        var isInformation = sourceKey.StartsWith("SemiColorInformation", StringComparison.Ordinal);
        var isSecondary = sourceKey.StartsWith("SemiColorSecondary", StringComparison.Ordinal);
        if (!isPrimary && !isInformation && !isSecondary)
            return false;

        var sourcePrefixLength = isPrimary
            ? "SemiColorPrimary".Length
            : isInformation
                ? "SemiColorInformation".Length
                : "SemiColorSecondary".Length;
        var suffix = sourceKey[sourcePrefixLength..];
        var ramp = isPrimary || isInformation
            ? isDark ? SemiPalette.DarkBlue : SemiPalette.LightBlue
            : isDark ? SemiPalette.DarkLightBlue : SemiPalette.LightLightBlue;

        int index;
        switch (suffix)
        {
            case "": index = 5; break;
            case "Pointerover": index = 6; break;
            case "Active": index = 7; break;
            case "Disabled": index = 2; break;
            case "Light": index = isDark ? 5 : 0; opacity = isDark ? 0.2 : 1; break;
            case "LightPointerover": index = isDark ? 5 : 1; opacity = isDark ? 0.3 : 1; break;
            case "LightActive": index = isDark ? 5 : 2; opacity = isDark ? 0.4 : 1; break;
            default: return false;
        }

        var sourceColor = ramp[index];
        var map = isDark ? maps.DarkAccent : maps.LightAccent;
        if (!map.TryGetValue(sourceColor, out color))
            return false;

        return true;
    }
}
