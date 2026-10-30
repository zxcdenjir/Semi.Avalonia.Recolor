using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Semi.Avalonia.Recolor.Ursa;

/// <summary>Подключает хосты диалогов для страниц и настройку заголовка UrsaWindow.</summary>
public sealed partial class UrsaRecolorStyles : Styles
{
    public UrsaRecolorStyles()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
