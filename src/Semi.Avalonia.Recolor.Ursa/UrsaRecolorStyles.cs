using System;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Semi.Avalonia.Recolor.Haptics;
using Ursa.Controls;

namespace Semi.Avalonia.Recolor.Ursa;

/// <summary>Подключает хосты диалогов для страниц и настройку заголовка UrsaWindow.</summary>
public sealed partial class UrsaRecolorStyles : Styles
{
    private static readonly IDisposable SelectionListSelectionRegistration =
        SelectingItemsControl.SelectionChangedEvent.AddClassHandler<SelectionList>(OnSelectionListSelectionChanged,
            RoutingStrategies.Bubble);

    public UrsaRecolorStyles()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private static void OnSelectionListSelectionChanged(SelectionList list, SelectionChangedEventArgs args)
    {
        GC.KeepAlive(SelectionListSelectionRegistration);
        if (ReferenceEquals(args.Source, list))
            HapticFeedback.TriggerFor(list, HapticType.Selection);
    }
}
