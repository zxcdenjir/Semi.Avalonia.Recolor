using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Media.Imaging;

using Semi.Avalonia.Recolor.Haptics;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SemiThemeRecolor.Demo.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private bool _suppressPickerChanges;

    [ObservableProperty]
    private string _accentHex = "#FF6A00";

    [ObservableProperty]
    private Color _pickerColor = Color.Parse("#FF6A00");

    [ObservableProperty]
    private string _secondaryHex = "";

    [ObservableProperty]
    private string _backgroundDarkHex = "#1B1B22";

    [ObservableProperty]
    private string _backgroundLightHex = "#F3F5F9";

    [ObservableProperty]
    private bool _recolorBackground = true;

    [ObservableProperty]
    private bool _hapticsEnabled = true;

    [ObservableProperty]
    private HapticType _selectedHapticType = HapticType.Confirm;

    [ObservableProperty]
    private Bitmap? _pickedImage;

    [ObservableProperty]
    private string? _pickedImagePath;

    [ObservableProperty]
    private Stretch _pickerStretch = Stretch.UniformToFill;

    [ObservableProperty]
    private string _status = "Введите HEX и нажмите «Применить».";

    public MainWindowViewModel()
    {
        if (App.Recolor is { } recolor)
        {
            recolor.EnableHaptic = HapticsEnabled;
            recolor.HapticType = SelectedHapticType;
        }

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

    partial void OnPickerColorChanged(Color value)
    {
        if (_suppressPickerChanges)
        {
            return;
        }

        AccentHex = value.ToString();
        if (App.Recolor is { } recolor)
        {
            recolor.PrimaryColor = value;
            Status = $"Акцент {value} применён.";
        }
    }

    public IReadOnlyList<AccentPreset> Presets { get; } =
    [
        new("Оранжевый", "#FF6A00"),
        new("Изумрудный", "#00A870"),
        new("Фиолетовый", "#7C4DFF"),
        new("Малиновый", "#E0356F"),
        new("Бирюзовый", "#00A6B4"),
        new("Золотой", "#C79100"),
    ];

    public IReadOnlyList<HapticType> HapticTypes { get; } = Enum.GetValues<HapticType>();
    public IReadOnlyList<Stretch> StretchModes { get; } = [Stretch.Uniform, Stretch.UniformToFill, Stretch.Fill];

    partial void OnHapticsEnabledChanged(bool value)
    {
        if (App.Recolor is { } recolor)
            recolor.EnableHaptic = value;
    }

    partial void OnSelectedHapticTypeChanged(HapticType value)
    {
        if (App.Recolor is { } recolor)
            recolor.HapticType = value;
    }

    [RelayCommand]
    private void Apply()
    {
        if (!Color.TryParse(AccentHex, out var accent))
        {
            Status = $"Не удалось разобрать цвет «{AccentHex}».";
            return;
        }

        Color? secondary = null;
        if (string.IsNullOrWhiteSpace(SecondaryHex))
        {
            secondary = null;
        }
        else if (Color.TryParse(SecondaryHex, out var parsedSecondary))
        {
            secondary = parsedSecondary;
        }
        else
        {
            Status = $"Не удалось разобрать цвет Secondary «{SecondaryHex}».";
            return;
        }

        Color? darkBackground = null;
        Color? lightBackground = null;
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

            darkBackground = dark;
            lightBackground = light;
        }

        var recolor = App.Recolor;
        if (recolor is null)
        {
            Status = "SemiThemeRecolor не найден в Application.Styles.";
            return;
        }

        SetPickerColor(accent);
        recolor.PrimaryColor = accent;
        recolor.SecondaryColor = secondary;
        recolor.BackgroundColorDark = darkBackground;
        recolor.BackgroundColorLight = lightBackground;

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

        AccentHex = "#0064FA";
        SetPickerColor(Color.Parse("#0064FA"));
        SecondaryHex = "";
        BackgroundDarkHex = "#16161A";
        BackgroundLightHex = "#FFFFFF";

        recolor.PrimaryColor = null;
        recolor.PrimaryColorLight = null;
        recolor.PrimaryColorDark = null;
        recolor.SecondaryColor = null;
        recolor.BackgroundColor = null;
        recolor.BackgroundColorLight = null;
        recolor.BackgroundColorDark = null;
        recolor.Reset();
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

    private void SetPickerColor(Color color)
    {
        _suppressPickerChanges = true;
        PickerColor = color;
        _suppressPickerChanges = false;
    }
}
