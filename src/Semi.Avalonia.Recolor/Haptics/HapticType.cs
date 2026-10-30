namespace Semi.Avalonia.Recolor.Haptics;

/// <summary>Типы тактильного отклика, доступные в библиотеке.</summary>
public enum HapticType
{
    /// <summary>Короткий отклик при нажатии.</summary>
    Click,
    /// <summary>Подтверждение действия.</summary>
    Confirm,
    /// <summary>Отказ или ошибка.</summary>
    Reject,
    /// <summary>Длительное нажатие.</summary>
    LongPress,
    /// <summary>Изменение выбранного значения.</summary>
    Selection,
    /// <summary>Включение переключателя.</summary>
    ToggleOn,
    /// <summary>Выключение переключателя.</summary>
    ToggleOff,
}
