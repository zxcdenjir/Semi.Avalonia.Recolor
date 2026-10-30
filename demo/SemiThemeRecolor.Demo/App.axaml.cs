using System;
using System.Diagnostics;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;

using SemiThemeRecolor.Demo.ViewModels;
using SemiThemeRecolor.Demo.Views;

using RecolorStyle = Semi.Avalonia.Recolor.SemiThemeRecolor;

namespace SemiThemeRecolor.Demo;

public partial class App : Application
{
    /// <summary>Стиль перекраски, объявленный в <c>App.axaml</c>.</summary>
    public static RecolorStyle? Recolor { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Recolor = Styles.OfType<RecolorStyle>().FirstOrDefault();

        if (Recolor is { } benchmarkTarget
            && Environment.GetEnvironmentVariable("SEMI_RECOLOR_BENCHMARK") == "1")
        {
            var colors = new[] { "#FF6A00", "#00A870", "#7C4DFF", "#E0356F", "#00A6B4", "#C79100" };
            var refreshStopwatch = Stopwatch.StartNew();
            benchmarkTarget.Reapply();
            refreshStopwatch.Stop();
            Console.WriteLine($"Palette resource refresh: {refreshStopwatch.Elapsed.TotalMilliseconds:F1} ms");
            benchmarkTarget.Reapply(); // Warm up JIT and the resource graph before measuring.
            var stopwatch = Stopwatch.StartNew();
            foreach (var color in colors)
            {
                benchmarkTarget.PrimaryColor = Color.Parse(color);
            }

            stopwatch.Stop();
            Console.WriteLine($"{colors.Length} cached recolors: {stopwatch.Elapsed.TotalMilliseconds:F1} ms "
                              + $"({stopwatch.Elapsed.TotalMilliseconds / colors.Length:F1} ms/apply)");
            benchmarkTarget.PrimaryColor = Color.Parse("#FF6A00");
        }

        if (Recolor is { } resetTarget
            && Environment.GetEnvironmentVariable("SEMI_RECOLOR_RESET_CHECK") == "1")
        {
            resetTarget.PrimaryColor = null;
            resetTarget.PrimaryColorLight = null;
            resetTarget.PrimaryColorDark = null;
            resetTarget.SecondaryColor = null;
            resetTarget.BackgroundColor = null;
            resetTarget.BackgroundColorLight = null;
            resetTarget.BackgroundColorDark = null;
            resetTarget.Reset();
            var variants = new[] { ThemeVariant.Dark, ThemeVariant.Light };
            var original = variants
                .Select(variant => ResourceNodeExtensions.TryFindResource(this, "SemiColorPrimary", variant,
                    out var value) && value is ISolidColorBrush brush ? brush.Color : (Color?)null)
                .ToArray();
            resetTarget.PrimaryColor = Color.Parse("#FFFFFF");
            resetTarget.Reapply();
            resetTarget.PrimaryColor = null;
            resetTarget.Reapply();
            var restored = variants
                .Select(variant => ResourceNodeExtensions.TryFindResource(this, "SemiColorPrimary", variant,
                    out var value) && value is ISolidColorBrush brush ? brush.Color : (Color?)null)
                .ToArray();
            if (!original.SequenceEqual(restored))
            {
                throw new InvalidOperationException(
                    $"Reset mismatch: [{string.Join(", ", original.Select(color => color?.ToString() ?? "<null>"))}] "
                    + $"became [{string.Join(", ", restored.Select(color => color?.ToString() ?? "<null>"))}].");
            }

            Console.WriteLine("Reset restored both original theme palettes.");
        }

        // Dev-only: exercise the "secondary accent without a primary" path.
        if (Recolor is { } recolor
            && Environment.GetEnvironmentVariable("SEMI_RECOLOR_SECONDARY_ONLY") is { Length: > 0 } secondaryHex
            && Color.TryParse(secondaryHex, out var secondaryOnly))
        {
            recolor.PrimaryColor = null;
            recolor.SecondaryColor = secondaryOnly;
            recolor.Reapply();
        }

        var requestedVariant = Environment.GetEnvironmentVariable("SEMI_RECOLOR_VARIANT");
        if (string.Equals(requestedVariant, "Light", StringComparison.OrdinalIgnoreCase))
        {
            RequestedThemeVariant = ThemeVariant.Light;
        }
        else if (string.Equals(requestedVariant, "Dark", StringComparison.OrdinalIgnoreCase))
        {
            RequestedThemeVariant = ThemeVariant.Dark;
        }

        if (Environment.GetEnvironmentVariable("SEMI_RECOLOR_DUMP") == "1")
        {
            Dispatcher.UIThread.Post(DumpAndExit, DispatcherPriority.ApplicationIdle);
            base.OnFrameworkInitializationCompleted();
            return;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            desktop.MainWindow = window;

            var renderPath = Environment.GetEnvironmentVariable("SEMI_RECOLOR_RENDER");
            if (!string.IsNullOrWhiteSpace(renderPath))
            {
                window.Show();
                Dispatcher.UIThread.Post(() => RenderAndExit(window, renderPath, desktop),
                    DispatcherPriority.ApplicationIdle);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>В режиме разработки сохраняет изображение окна в PNG и завершает приложение.</summary>
    private static void RenderAndExit(Window window, string path, IClassicDesktopStyleApplicationLifetime desktop)
    {
        var pixelSize = new PixelSize(
            (int)Math.Max(1, window.Bounds.Width),
            (int)Math.Max(1, window.Bounds.Height));
        using var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96, 96));
        bitmap.Render(window);
#pragma warning disable CS0618 // the new encoder-options overload is not available on Avalonia 12.1.0
        bitmap.Save(path);
#pragma warning restore CS0618
        Console.WriteLine($"saved {path} ({pixelSize.Width}x{pixelSize.Height})");
        desktop.Shutdown();
    }

    /// <summary>Выводит значения используемых ресурсов Semi и завершает приложение.</summary>
    private void DumpAndExit()
    {
        string[] keys =
        [
            "SemiColorPrimary",
            "SemiColorPrimaryPointerover",
            "SemiColorPrimaryActive",
            "SemiColorPrimaryDisabled",
            "SemiColorPrimaryLight",
            "SemiColorSecondary",
            "SemiColorSecondaryPointerover",
            "SemiColorInformation",
            "SemiColorLink",
            "SemiColorFocusBorder",
            "SemiColorBackground0",
            "SemiColorBackground1",
            "SemiBlue5",
            "SemiLightBlue5",
            "ButtonSolidPrimaryBackground",
            "ButtonSolidPrimaryPointeroverBackground",
            "ButtonSolidPrimaryPressedBackground",
            "ButtonOutlineBorderBrush",
            "DataGridRowSelectedBackground",
            "TextBlockSelectionBackground",
            "AvatarBlueBackground",
            "BadgePrimaryBadgeBackground",
        ];

        Console.WriteLine($"variant = {ActualThemeVariant}");

        foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            Console.WriteLine($"--- {variant} ---");
            foreach (var key in keys)
            {
                var text = ResourceNodeExtensions.TryFindResource(this, key, variant, out var value)
                    ? Describe(value)
                    : "<not found>";
                Console.WriteLine($"{key} = {text}");
            }
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private static string Describe(object? value) => value switch
    {
        SolidColorBrush brush => $"{brush.Color} (opacity {brush.Opacity})",
        ISolidColorBrush brush => brush.Color.ToString(),
        Color color => color.ToString(),
        null => "<null>",
        _ => value.GetType().Name,
    };
}
