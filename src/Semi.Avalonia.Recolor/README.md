# Semi.Avalonia.Recolor

**Задайте цвет приложения один раз — библиотека перекрасит связанные ресурсы Semi.Avalonia и доступные ресурсы Ursa.**

## Установка

```sh
dotnet add package Semi.Avalonia.Recolor
```

Для Ursa устанавливается отдельный необязательный пакет `Semi.Avalonia.Recolor.Ursa`.

## Быстрый старт

Добавьте стиль после `SemiTheme` в `App.axaml`:

```xml
<Application.Styles>
    <semi:SemiTheme />
    <recolor:SemiThemeRecolor
        PrimaryColor="#FF6A00"
        BackgroundColorDark="#1B1B22"
        BackgroundColorLight="#F3F5F9" />
</Application.Styles>
```

Если задан только `PrimaryColor`, библиотека сама адаптирует акцент для светлой темы. При необходимости задайте `PrimaryColorLight`, `PrimaryColorDark` и `SecondaryColor` отдельно.

## Как устроена перекраска

Тема Semi связывает стили контролов с семантическими цветами через ключи ресурсов, например `ButtonSolidPrimaryBackground` → `SemiColorPrimary`. Библиотека содержит каталог таких ключей для Semi и Ursa и публикует соответствующие кисти из собственного словаря ресурсов. Она не обходит дерево стилей при запуске и не изменяет исходные кисти Semi.

При изменении акцента пересчитываются только значения каталога. Палитры Light и Dark подготавливаются заранее, а Ursa-ресурсы работают только если тема Ursa установлена. Информационные оттенки Semi являются алиасами синей Primary-палитры и следуют за выбранным акцентом.

## Параметры цветов

| Свойство | Назначение |
| --- | --- |
| `PrimaryColor` | Основной акцент; применяется напрямую в Dark и адаптируется по светлоте в Light. |
| `PrimaryColorLight`, `PrimaryColorDark` | Явные акценты для соответствующих тем. |
| `SecondaryColor` | Дополнительный акцент семейства LightBlue; строится из Primary, если не задан. |
| `BackgroundColor` | Общий фон обеих тем. |
| `BackgroundColorLight`, `BackgroundColorDark` | Фоны Light и Dark по отдельности. |

Для тёмной темы строится шкала `Background0–4`. Для светлой меняется только базовый `Background0`, сохраняя контраст белого текста и поверхностей.

Все параметры — `StyledProperty<Color?>`; в XAML поддерживаются HEX-цвета и именованные цвета. `Reapply()` повторно применяет текущие настройки, а `Reset()` удаляет переопределения.

## Дополнительные контролы

### ImagePicker

`ImagePicker` открывает системный выбор файла, показывает предпросмотр и принимает PNG, JPEG, WebP и BMP. GIF исключён. `Image` (`Bitmap?`) и `ImagePath` (`string?`) — двусторонние свойства. `Placeholder` принимает любой `Control`, кнопка сброса очищает выбранное изображение.

По умолчанию размер файла ограничен 30 МиБ, а изображение декодируется с максимальной стороной 2048 пикселей. Оба ограничения можно изменить или отключить.

```xml
<recolor:ImagePicker
    Image="{Binding SelectedImage, Mode=TwoWay}"
    ImagePath="{Binding SelectedImagePath, Mode=TwoWay}"
    CornerRadius="12"
    Stretch="UniformToFill" />
```

### Тактильный отклик Android

Включите отклик для кнопок и задайте тип по умолчанию:

```xml
<recolor:SemiThemeRecolor EnableHaptic="True" HapticType="Click" />
```

Отдельную кнопку можно настроить через `HapticFeedback.IsEnabled` и `HapticFeedback.Type`. В Android-приложении добавьте разрешение `android.permission.VIBRATE`. На других платформах встроенный обработчик ничего не делает; при необходимости задайте собственную реализацию через `HapticFeedback.PlatformFeedback`.

### Интервал в ItemsControl

Добавьте `Classes="SemiSpaced"`, чтобы использовать вертикальную `StackPanel` с `Spacing="12"`. Этот класс заменяет панель элементов, поэтому не подходит для виртуализируемых списков.

## Сохранение пользовательского цвета

Библиотека не выбирает хранилище настроек. Приложение сохраняет HEX-цвет своим способом и восстанавливает его до показа главного окна:

```csharp
if (Color.TryParse(savedAccent, out var accent))
{
    recolor.PrimaryColor = accent;
    recolor.Reapply();
}
```

## Совместимость

- Avalonia 12.x (`net8.0` и `net10.0`).
- Проверено с Semi.Avalonia 12.1.0.1 и Ursa 2.2.0.
- Основной пакет зависит только от Avalonia; Semi и Ursa не являются обязательными зависимостями.
- Встроенный тактильный отклик предусмотрен для Android.

Лицензия: MIT.
