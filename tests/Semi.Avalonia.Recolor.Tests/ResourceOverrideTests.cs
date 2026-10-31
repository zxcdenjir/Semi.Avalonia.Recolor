using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Headless.XUnit;

using Semi.Avalonia.Recolor;

namespace Semi.Avalonia.Recolor.Tests;

/// <summary>Проверяет собранный каталог и реальные переопределения ресурсов тем.</summary>
public sealed class ResourceOverrideTests
{
    private const string InformationIconKey = "NotificationCardInformationIconForeground";
    private const string SelectionListIndicatorKey = "SelectionListIndicatorBackground";

    /// <summary>Проверяет, что исходный CSV и сгенерированный каталог содержат одинаковые ключи.</summary>
    [Fact]
    public void GeneratedCatalogMatchesDocumentedCsv()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "palette-resource-catalog.csv");
        var expected = File.ReadLines(csvPath)
            .Skip(1)
            .Select(line => line.Split(','))
            .Where(columns => columns.Length == 6)
            .Select(columns => new PaletteResourceTarget(columns[2], NormalizeInformationAlias(columns[3])))
            .Distinct()
            .OrderBy(target => target.ResourceKey, StringComparer.Ordinal)
            .ThenBy(target => target.SourcePaletteKey, StringComparer.Ordinal)
            .ToArray();
        var actual = PaletteResourceCatalog.Targets
            .Distinct()
            .OrderBy(target => target.ResourceKey, StringComparer.Ordinal)
            .ThenBy(target => target.SourcePaletteKey, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
        Assert.Contains(actual, target => target.ResourceKey == InformationIconKey
                                          && target.SourcePaletteKey == "SemiColorPrimary");
        Assert.Contains(actual, target => target.ResourceKey == SelectionListIndicatorKey
                                          && target.SourcePaletteKey == "SemiBlue1");
    }

    /// <summary>Проверяет, что индикатор SelectionList использует оттенок Primary в обеих темах.</summary>
    [AvaloniaFact]
    public void SelectionListIndicatorFollowsPrimaryAccentInBothThemes()
    {
        var application = Application.Current!;
        var accent = Color.Parse("#FF6A00");
        var style = new SemiThemeRecolor { PrimaryColor = accent };
        application.Styles.Add(style);

        try
        {
            var darkAccentRamp = RampBuilder.Build(SemiPalette.DarkBlue, accent);
            var lightAccent = RampBuilder.Adapt(accent, SemiPalette.DarkBlue[5], SemiPalette.LightBlue[5]);
            var lightAccentRamp = RampBuilder.Build(SemiPalette.LightBlue, lightAccent);

            AssertResourceColor(application, SelectionListIndicatorKey, ThemeVariant.Dark, darkAccentRamp[1]);
            AssertResourceColor(application, SelectionListIndicatorKey, ThemeVariant.Light, lightAccentRamp[1]);
        }
        finally
        {
            application.Styles.Remove(style);
            style.Reset();
        }
    }

    /// <summary>Проверяет цвет Info-иконки в обеих темах и восстановление после сброса.</summary>
    [AvaloniaFact]
    public void InformationToastIconFollowsPrimaryInBothThemesAndResetRemovesOverrides()
    {
        var application = Application.Current!;
        var accent = Color.Parse("#FF6A00");
        var style = new SemiThemeRecolor { PrimaryColor = accent };
        application.Styles.Add(style);

        try
        {
            AssertResourceColor(application, InformationIconKey, ThemeVariant.Dark, accent);
            var lightAccent = RampBuilder.Adapt(accent, SemiPalette.DarkBlue[5], SemiPalette.LightBlue[5]);
            AssertResourceColor(application, InformationIconKey, ThemeVariant.Light, lightAccent);

            // Ursa's :information selector uses this same icon key for both normal and Light cards.
            AssertResourceColor(application, InformationIconKey, ThemeVariant.Dark, accent);
            AssertResourceColor(application, InformationIconKey, ThemeVariant.Light, lightAccent);

            style.PrimaryColor = null;
            style.Reset();
            Assert.False(application.TryGetResource(InformationIconKey, ThemeVariant.Dark, out _));
            Assert.False(application.TryGetResource(InformationIconKey, ThemeVariant.Light, out _));
        }
        finally
        {
            application.Styles.Remove(style);
            style.Reset();
        }
    }

    private static void AssertResourceColor(Application application, string key, ThemeVariant variant, Color expected)
    {
        Assert.True(application.TryGetResource(key, variant, out var value));
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(value);
        Assert.Equal(expected, brush.Color);
    }

    private static string NormalizeInformationAlias(string sourceKey) =>
        sourceKey.StartsWith("SemiColorInformation", StringComparison.Ordinal)
            ? "SemiColorPrimary" + sourceKey["SemiColorInformation".Length..]
            : sourceKey;
}
