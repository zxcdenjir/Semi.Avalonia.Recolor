# Semi.Avalonia.Recolor.Ursa

Необязательная интеграция с Ursa для SemiThemeRecolor. Основная библиотека не зависит от Ursa; устанавливайте этот пакет, если приложение использует контролы Ursa.

## Подключение

Установите пакет:

```sh
dotnet add package Semi.Avalonia.Recolor.Ursa
```

Загрузите тему Ursa и стили интеграции в `App.axaml`:

```xml
<Application.Styles>
    <u:UrsaSemiTheme />
    <recolorUrsa:UrsaRecolorStyles />
</Application.Styles>
```

Пространство имён `recolorUrsa` задаётся как `clr-namespace:Semi.Avalonia.Recolor.Ursa;assembly=Semi.Avalonia.Recolor.Ursa`.

Стили добавляют `OverlayDialogHost` внешней странице Avalonia, если в дереве ещё нет хоста от `UrsaWindow` или `UrsaView`. Это работает с `ContentPage`, `NavigationPage`, `TabbedPage`, `CarouselPage`, `DrawerPage` и другими наследниками `Page`. Вложенные страницы по умолчанию используют хост внешней страницы.

Интеграция также задаёт отступ заголовка `UrsaWindow`, чтобы кнопки навигации и Drawer располагались вне области перетаскивания окна.

## Параметры хоста страницы

Настройте хост прикреплёнными свойствами страницы:

```xml
<Page
    recolorUrsa:PageOverlayDialogHost.IsEnabled="True"
    recolorUrsa:PageOverlayDialogHost.UseTopLevelOverlay="True"
    recolorUrsa:PageOverlayDialogHost.DialogHostZIndex="1000"
    recolorUrsa:PageOverlayDialogHost.HostId="main" />
```

`UseTopLevelOverlay="True"` размещает хост в overlay-слое всего `TopLevel`; значение `False` ограничивает его шаблоном страницы. `DialogHostZIndex` можно менять, пока страница активна. Свойство `HostId` позволяет адресу страницы использовать отдельный хост.

Если приложение уже содержит `UrsaWindow` или `UrsaView`, интеграция использует их хост, когда `HostId` не задан. В этом случае расположение и Z-порядок задаются шаблоном Ursa.

## Лицензия

MIT.
