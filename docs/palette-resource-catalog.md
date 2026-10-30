# Палитровые ресурсы Semi и Ursa

Каталог собирается из прямых ссылок ресурсов Light/Dark на палитровые токены Semi.
Информационные токены Semi являются алиасами основной синей рампы и перекрашиваются вместе с Primary.

| Пакет | Light | Dark |
| --- | ---: | ---: |
| Semi | 120 | 120 |
| Ursa Semi | 67 | 65 |
| **Всего** | **372** | |

В CSV находится 372 определений и 184 уникальных ключей ресурсов. Каждая строка содержит исходный
палитровый токен, файл и номер строки. Список C# создаётся из этого же набора с нормализацией
`SemiColorInformation*` к соответствующей ступени `SemiColorPrimary*`.

Источники: `Semi.Avalonia/src/Semi.Avalonia/Themes/{Light,Dark}` и
`Ursa.Avalonia/src/Ursa.Themes.Semi/Themes/{Light,Dark}`.

Для обновления каталога запустите `python tools/generate_palette_catalog.py --semi-root ../Semi.Avalonia --ursa-root ../Ursa.Avalonia`.
Для проверки без изменения файлов добавьте `--check`.
