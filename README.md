# SemiThemeRecolor

NuGet-пакет для рантайм-перекраски акцентной палитры темы
[Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia) по одному HEX-цвету.

| | |
| --- | --- |
| Пакет | `Semi.Avalonia.Recolor` |
| Версия Semi | 12.1.0.1 (Avalonia 12.x) |
| Таргеты | `net8.0`, `net10.0` |
| Лицензия | MIT |

## Быстрый старт

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

Подробности — в [README пакета](src/Semi.Avalonia.Recolor/README.md).

## Что внутри

```
src/Semi.Avalonia.Recolor/   пакет (Styles + обходчик ресурсов + OKLCH-генератор)
demo/SemiThemeRecolor.Demo/  демо-приложение (контролы, DataGrid, Light/Dark, пресеты)
```

Механика: пакет обходит всё дерево ресурсов приложения, включая `StaticResource`-алиасы тем
(`Themes/Light|Dark/*.axaml`, `Ursa.Themes.Semi`) и мутирует общие экземпляры `SolidColorBrush` прямо
в палитрах Semi — поэтому перекрашиваются все контролы и пакеты-компаньоны, а не только `SemiColorPrimary`.

## Демо

```powershell
dotnet run --project demo/SemiThemeRecolor.Demo/SemiThemeRecolor.Demo.csproj
```

В демо можно ввести HEX акцента, выбрать пресет, задать фон отдельно для светлой и тёмной темы,
переключить Light/Dark. Сохранённые скриншоты: `docs/demo-dark.png`, `docs/demo-light.png`
(генерируются режимом `SEMI_RECOLOR_RENDER=<path>`).

## Сборка пакета

```powershell
dotnet pack src/Semi.Avalonia.Recolor/Semi.Avalonia.Recolor.csproj -c Release
```
