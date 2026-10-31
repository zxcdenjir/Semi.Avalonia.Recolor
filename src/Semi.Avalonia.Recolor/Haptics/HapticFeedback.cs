using System;
using System.Reflection;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Semi.Avalonia.Recolor.Haptics;

/// <summary>Прикреплённые настройки тактильного отклика для кнопок. Значение <see langword="true"/>
/// принудительно включает отклик, <see langword="false"/> отключает его, а <see langword="null"/>
/// использует настройку приложения.</summary>
public static class HapticFeedback
{
    /// <summary>Прикреплённое свойство, которое включает или отключает отклик для контрола.</summary>
    public static readonly AttachedProperty<bool?> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool?>("IsEnabled", typeof(HapticFeedback));

    /// <summary>Прикреплённое свойство с типом отклика для контрола.</summary>
    public static readonly AttachedProperty<HapticType?> TypeProperty =
        AvaloniaProperty.RegisterAttached<Control, HapticType?>("Type", typeof(HapticFeedback));

    private static readonly IDisposable ButtonClickRegistration;

    /// <summary>Общее состояние отклика, задаваемое свойством <see cref="SemiThemeRecolor.EnableHaptic"/>.</summary>
    public static bool IsGloballyEnabled { get; set; }

    /// <summary>Тип отклика по умолчанию, если для контрола не задан отдельный тип.</summary>
    public static HapticType DefaultType { get; set; } = HapticType.Click;

    /// <summary>Необязательный обработчик для собственной реализации отклика на другой платформе.</summary>
    public static Action<HapticType>? PlatformFeedback { get; set; }

    static HapticFeedback()
    {
        ButtonClickRegistration = Button.ClickEvent.AddClassHandler(typeof(Button), OnButtonClick,
            RoutingStrategies.Bubble, handledEventsToo: true);
    }

    /// <summary>Возвращает локальную настройку включения отклика.</summary>
    public static bool? GetIsEnabled(Control element) => element.GetValue(IsEnabledProperty);
    /// <summary>Задаёт локальную настройку включения отклика.</summary>
    public static void SetIsEnabled(Control element, bool? value) => element.SetValue(IsEnabledProperty, value);
    /// <summary>Возвращает локальный тип отклика.</summary>
    public static HapticType? GetType(Control element) => element.GetValue(TypeProperty);
    /// <summary>Задаёт локальный тип отклика.</summary>
    public static void SetType(Control element, HapticType? value) => element.SetValue(TypeProperty, value);

    /// <summary>Запрашивает тактильный отклик, если он включён и доступен на устройстве.</summary>
    public static void Trigger(HapticType? type = null)
    {
        GC.KeepAlive(ButtonClickRegistration);
        if (!IsGloballyEnabled)
            return;

        Perform(type ?? DefaultType);
    }

    /// <summary>Запрашивает отклик для контрола с учётом его прикреплённых настроек.</summary>
    public static void TriggerFor(Control element, HapticType? type = null)
    {
        ArgumentNullException.ThrowIfNull(element);
        GC.KeepAlive(ButtonClickRegistration);
        var localEnabled = GetIsEnabled(element);
        if (localEnabled == false || localEnabled != true && !IsGloballyEnabled)
            return;

        Perform(GetType(element) ?? type ?? DefaultType);
    }

    private static void Perform(HapticType type)
    {
        if (PlatformFeedback is { } feedback)
            feedback(type);
        else
            TryTriggerAndroid(type);
    }

    private static void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        TriggerFor(button);
    }

    // Keep the core assembly platform-neutral. On Android this discovers the platform runtime
    // APIs without adding an Android target framework or a mandatory platform package dependency.
    private static void TryTriggerAndroid(HapticType type)
    {
        try
        {
            var applicationType = Type.GetType("Android.App.Application, Mono.Android");
            var context = applicationType?.GetProperty("Context", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);
            if (context is null)
                return;

            var contextType = context.GetType();
            var vibrator = contextType.GetMethod("GetSystemService", [typeof(string)])
                ?.Invoke(context, ["vibrator"]);
            if (vibrator is null)
                return;

            var effectType = Type.GetType("Android.OS.VibrationEffect, Mono.Android");
            var vibratorType = vibrator.GetType();
            var effectValue = type switch
            {
                HapticType.Confirm => "EffectDoubleClick",
                HapticType.Reject => "EffectHeavyClick",
                HapticType.LongPress => "EffectHeavyClick",
                HapticType.Selection => "EffectTick",
                HapticType.ToggleOn or HapticType.ToggleOff => "EffectTick",
                _ => "EffectClick",
            };

            if (effectType is not null)
            {
                var constant = effectType.GetField(effectValue, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var create = Array.Find(effectType.GetMethods(BindingFlags.Public | BindingFlags.Static), method =>
                    method.Name == "CreatePredefined" && method.GetParameters().Length == 1);
                object? effectArgument = null;
                if (create is not null && constant is not null)
                {
                    var argumentType = create.GetParameters()[0].ParameterType;
                    effectArgument = argumentType.IsInstanceOfType(constant)
                        ? constant
                        : argumentType.IsEnum
                            ? Enum.ToObject(argumentType, constant)
                            : Convert.ChangeType(constant, argumentType);
                }
                var effect = effectArgument is null ? null : create?.Invoke(null, [effectArgument]);
                var vibrateWithEffect = effect is null ? null : vibratorType.GetMethod("Vibrate", [effectType]);
                if (effect is not null && vibrateWithEffect is not null)
                {
                    vibrateWithEffect.Invoke(vibrator, [effect]);
                    return;
                }
            }

            vibratorType.GetMethod("Vibrate", [typeof(long)])?.Invoke(vibrator, [25L]);
        }
        catch
        {
            // Unsupported platform/runtime or missing Android VIBRATE manifest permission.
        }
    }
}
