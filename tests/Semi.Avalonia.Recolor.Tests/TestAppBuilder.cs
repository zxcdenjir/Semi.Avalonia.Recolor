using Avalonia;
using Avalonia.Headless;

using Xunit;

[assembly: AvaloniaTestApplication(typeof(Semi.Avalonia.Recolor.Tests.TestAppBuilder))]

namespace Semi.Avalonia.Recolor.Tests;

/// <summary>Собирает Avalonia-приложение для тестов без открытия системного окна.</summary>
public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>Минимальное приложение, используемое тестами палитры.</summary>
public sealed class TestApplication : Application;
