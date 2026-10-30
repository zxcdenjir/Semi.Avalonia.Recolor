using System;

using Avalonia;
using XamlMcp.Avalonia;

namespace SemiThemeRecolor.Demo;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .AttachXamlMcp()
        .UsePlatformDetect()
        .WithInterFont()
#if DEBUG
        .WithDeveloperTools()
#endif
        .LogToTrace();
}
