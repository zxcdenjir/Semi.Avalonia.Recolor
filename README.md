# SemiThemeRecolor

**Настраиваемые цвета Semi.Design для Avalonia — одним компонентом и без ручного переопределения стилей.**

[English version](README.en.md)

SemiThemeRecolor строит акцентные палитры из выбранного цвета и переопределяет ресурсы элементов Semi.Avalonia. Изменение применяется сразу и учитывает активную светлую или тёмную тему.

## Возможности

- Перекраска Primary, Secondary, информационных ресурсов и связанных состояний контролов Semi.
- Отдельные цвета фона для Light и Dark; тёмная тема получает шкалу `Background0–4`.
- Дополнительный пакет для Ursa: ресурсы Ursa и хост диалогов на страницах Avalonia.
- `ImagePicker` с предпросмотром и выбором PNG, JPEG, WebP или BMP; GIF не предлагается.
- Haptic-отклик на Android с настройками по умолчанию и на отдельных кнопках.
- Класс `SemiSpaced` для `ItemsControl` с расстоянием 12.

Основной пакет не зависит от Semi.Avalonia или Ursa. Для Ursa подключается отдельная интеграция.

## Установка

После публикации пакетов в NuGet добавьте основной пакет:

```sh
dotnet add package Semi.Avalonia.Recolor
```

Для приложений с Ursa установите и дополнительный пакет:

```sh
dotnet add package Semi.Avalonia.Recolor.Ursa
```

## Быстрый старт

Добавьте перекраску после темы Semi в `App.axaml`:

```xml
<Application
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:semi="https://irihi.tech/semi"
    xmlns:ursa="https://irihi.tech/ursa"
    xmlns:recolorUrsa="clr-namespace:Semi.Avalonia.Recolor.Ursa;assembly=Semi.Avalonia.Recolor.Ursa"
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

Если нужно задать только акцент, укажите `PrimaryColor`. Цвет для Light адаптируется автоматически. Можно независимо задать `PrimaryColorLight`, `PrimaryColorDark`, `SecondaryColor`, `BackgroundColorLight` и `BackgroundColorDark`.

### Подключение Ursa

Загрузите тему Ursa и стили интеграции после темы Semi:

```xml
<Application.Styles>
    <semi:SemiTheme />
    <ursa:UrsaSemiTheme />
    <recolorUrsa:UrsaRecolorStyles />
    <recolor:SemiThemeRecolor PrimaryColor="#FF6A00" />
</Application.Styles>
```

`UrsaRecolorStyles` добавляет `OverlayDialogHost` внешней странице Avalonia, когда подходящего хоста ещё нет, и применяет отступ к заголовку `UrsaWindow`, чтобы кнопки навигации и Drawer не попадали в область перетаскивания окна. Остальные параметры страницы задаются через `PageOverlayDialogHost`:

```xml
<ContentPage
    xmlns:recolorUrsa="clr-namespace:Semi.Avalonia.Recolor.Ursa;assembly=Semi.Avalonia.Recolor.Ursa"
    recolorUrsa:PageOverlayDialogHost.IsEnabled="True"
    recolorUrsa:PageOverlayDialogHost.UseTopLevelOverlay="True" />
```

## Дополнительные контролы

### ImagePicker

```xml
<recolor:ImagePicker
    Image="{Binding SelectedImage, Mode=TwoWay}"
    ImagePath="{Binding SelectedImagePath, Mode=TwoWay}"
    CornerRadius="12"
    Stretch="UniformToFill" />
```

Свойство `Placeholder` принимает любой Avalonia `Control`. Выбранный файл сразу показывается в области предпросмотра; кнопка сброса очищает и `Image`, и `ImagePath`. По умолчанию файл ограничен 30 МиБ и изображение уменьшается до 2048 пикселей по большей стороне. Оба ограничения настраиваются.

### Haptic на Android

Включите отклик глобально:

```xml
<recolor:SemiThemeRecolor EnableHaptic="True" HapticType="Click" />
```

Или настройте отдельную кнопку:

```xml
<Button
    Content="Сохранить"
    recolor:HapticFeedback.IsEnabled="True"
    recolor:HapticFeedback.Type="Confirm" />
```

Добавьте разрешение в `Platforms/Android/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.VIBRATE" />
```

Встроенная реализация рассчитана на Android. Для собственной платформенной реализации доступен `HapticFeedback.PlatformFeedback`.

### Интервал в ItemsControl

```xml
<ItemsControl Classes="SemiSpaced">
    <TextBlock Text="Первый элемент" />
    <TextBlock Text="Второй элемент" />
</ItemsControl>
```

Класс задаёт вертикальную `StackPanel` с `Spacing="12"`. Не используйте его там, где нужна виртуализация или другая панель элементов.

## Сохранение цвета в настройках приложения

Библиотека не выбирает хранилище настроек. Сохраните введённый цвет средствами самого приложения и восстановите его до показа первого окна:

```csharp
if (Color.TryParse(savedAccent, out var accent))
{
    recolor.PrimaryColor = accent;
    recolor.Reapply();
}
```

Например, сохраняйте HEX-строку в Preferences на Android или в выбранном хранилище настроек приложения на других платформах.

## Демо и каталог ресурсов

Демо включает ColorPicker, переключение Light/Dark, контролы Semi и Ursa, ImagePicker и настройки Haptic:

```sh
dotnet run --project demo/SemiThemeRecolor.Demo/SemiThemeRecolor.Demo.csproj
```

Список ключей ресурсов находится в [`docs/palette-resource-catalog.csv`](docs/palette-resource-catalog.csv). Он создаётся из исходников Semi.Avalonia и Ursa:

```sh
python tools/generate_palette_catalog.py --semi-root ../Semi.Avalonia --ursa-root ../Ursa.Avalonia
python tools/generate_palette_catalog.py --check
```

Первой командой обновляются CSV, краткая сводка и C#-каталог. `--check` сообщает, если любой из файлов устарел. Для автоматической регрессионной проверки каталог также сверяется с CSV в тестах.

## Совместимость

- Avalonia 12.x; в проекте проверяется Avalonia 12.1.0.
- Semi.Avalonia 12.1.0.1 и Ursa 2.2.0.
- `net8.0` и `net10.0`.
- ImagePicker использует `TopLevel.StorageProvider`.
- Встроенный Haptic-адаптер поддерживает Android.

## Разработка

```sh
dotnet build SemiThemeRecolor.slnx
dotnet test tests/Semi.Avalonia.Recolor.Tests/Semi.Avalonia.Recolor.Tests.csproj
```

Лицензия: MIT. Подробности — в [LICENSE](LICENSE).
