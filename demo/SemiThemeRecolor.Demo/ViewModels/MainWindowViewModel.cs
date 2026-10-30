using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SemiThemeRecolor.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _accentHex = "#FF6A00";

    [ObservableProperty]
    private string _secondaryHex = "";

    [ObservableProperty]
    private string _backgroundDarkHex = "#1B1B22";

    [ObservableProperty]
    private string _backgroundLightHex = "#F3F5F9";

    [ObservableProperty]
    private bool _recolorBackground = true;

    [ObservableProperty]
    private string _status = "Введите HEX и нажмите «Применить».";

    public MainWindowViewModel()
    {
        People =
        [
            new Person("Анна Каренина", "Дизайнер", "Москва"),
            new Person("Иван Петров", "Разработчик", "Санкт-Петербург"),
            new Person("Мария Сидорова", "Аналитик", "Казань"),
            new Person("Пётр Смирнов", "QA-инженер", "Новосибирск"),
            new Person("Ольга Кузнецова", "Менеджер", "Екатеринбург"),
        ];
    }

    public ObservableCollection<Person> People { get; }

    public IReadOnlyList<AccentPreset> Presets { get; } =
    [
        new("Оранжевый", "#FF6A00"),
        new("Изумрудный", "#00A870"),
        new("Фиолетовый", "#7C4DFF"),
        new("Малиновый", "#E0356F"),
        new("Бирюзовый", "#00A6B4"),
        new("Золотой", "#C79100"),
    ];

    [RelayCommand]
    private void Apply()
    {
        var recolor = App.Recolor;
        if (recolor is null)
        {
            Status = "SemiThemeRecolor не найден в Application.Styles.";
            return;
        }

        if (!Color.TryParse(AccentHex, out var accent))
        {
            Status = $"Не удалось разобрать цвет «{AccentHex}».";
            return;
        }

        recolor.PrimaryColor = accent;

        if (string.IsNullOrWhiteSpace(SecondaryHex))
        {
            recolor.SecondaryColor = null;
        }
        else if (Color.TryParse(SecondaryHex, out var secondary))
        {
            recolor.SecondaryColor = secondary;
        }
        else
        {
            Status = $"Не удалось разобрать цвет Secondary «{SecondaryHex}».";
            return;
        }

        if (RecolorBackground)
        {
            if (!Color.TryParse(BackgroundDarkHex, out var dark))
            {
                Status = $"Не удалось разобрать цвет фона «{BackgroundDarkHex}».";
                return;
            }

            if (!Color.TryParse(BackgroundLightHex, out var light))
            {
                Status = $"Не удалось разобрать цвет фона «{BackgroundLightHex}».";
                return;
            }

            recolor.BackgroundColorDark = dark;
            recolor.BackgroundColorLight = light;
        }
        else
        {
            recolor.BackgroundColorDark = null;
            recolor.BackgroundColorLight = null;
        }

        Status = $"Акцент {accent} применён.";
    }

    [RelayCommand]
    private void ApplyPreset(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return;
        }

        AccentHex = hex;
        Apply();
    }

    [RelayCommand]
    private void ResetColors()
    {
        var recolor = App.Recolor;
        if (recolor is null)
        {
            return;
        }

        recolor.PrimaryColor = null;
        recolor.PrimaryColorLight = null;
        recolor.PrimaryColorDark = null;
        recolor.SecondaryColor = null;
        recolor.BackgroundColor = null;
        recolor.BackgroundColorLight = null;
        recolor.BackgroundColorDark = null;
        recolor.Reset();

        AccentHex = "#0064FA";
        SecondaryHex = "";
        BackgroundDarkHex = "#16161A";
        BackgroundLightHex = "#FFFFFF";
        Status = "Исходная палитра Semi восстановлена.";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var application = Application.Current;
        if (application is null)
        {
            return;
        }

        var target = application.ActualThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;

        application.RequestedThemeVariant = target;
        Status = target == ThemeVariant.Dark ? "Тёмная тема." : "Светлая тема.";
    }
}
