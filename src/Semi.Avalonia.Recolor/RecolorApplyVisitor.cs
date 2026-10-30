using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace Semi.Avalonia.Recolor;

/// <summary>
///     Applies a recolor map to every resource it visits:
///     <list type="bullet">
///         <item><see cref="SolidColorBrush.Color" /> is mutated in place — every StaticResource or
///             DynamicResource consumer keeps referencing the same instance and updates immediately.</item>
///         <item>Raw <see cref="Color" /> dictionary entries are replaced, so a future re-creation of
///             the dictionary produces the recolored value.</item>
///         <item>The palette background scale is replaced by key (protecting <c>SemiColorWhite</c>,
///             which shares the same hex in the light theme).</item>
///     </list>
/// </summary>
internal sealed class RecolorApplyVisitor : ResourceWalker.IRecolorVisitor
{
    private readonly RecolorMaps _maps;
    private readonly List<(IResourceDictionary Dictionary, object Key, object NewValue)> _pending = [];
    private readonly HashSet<object> _mutatedBrushes = new(ReferenceEqualityComparer.Instance);

    public RecolorApplyVisitor(RecolorMaps maps)
    {
        _maps = maps;
    }

    /// <summary>Number of palette dictionaries seen by the last walk.</summary>
    public int PaletteCount { get; private set; }

    public void VisitValue(object source, object key, object? value, ThemeVariant? variant)
    {
        switch (value)
        {
            case Color color when TryMapColor(color, variant, out var newColor):
                switch (source)
                {
                    case IResourceDictionary dictionary:
                        _pending.Add((dictionary, key, newColor));
                        break;
                    case Setter setter:
                        setter.Value = newColor;
                        break;
                }

                break;
            case SolidColorBrush brush when TryMapColor(brush.Color, variant, out var newBrushColor):
                if (_mutatedBrushes.Add(brush))
                {
                    brush.Color = newBrushColor;
                }

                break;
            case ImmutableSolidColorBrush immutable when TryMapColor(immutable.Color, variant, out var newImmutableColor):
                if (source is IResourceDictionary target)
                {
                    _pending.Add((target, key, new ImmutableSolidColorBrush(newImmutableColor)));
                }

                break;
        }
    }

    public void VisitPalette(IResourceDictionary palette, ThemeVariant? variant)
    {
        PaletteCount++;

        if (IsDark(variant))
        {
            if (_maps.DarkBackgroundRamp is not { } ramp)
            {
                return;
            }

            for (var i = 0; i < ramp.Length; i++)
            {
                ReplaceColor(palette, $"SemiBackground{i}Color", ramp[i]);
                MutateBrush(palette, $"SemiColorBackground{i}", ramp[i]);
            }

            MutateBrush(palette, "SemiColorNavBackground", ramp[1]);
            MutateBrush(palette, "SemiColorOverlayBackground", ramp[0]);
        }
        else
        {
            if (_maps.LightBackground0 is not { } background0)
            {
                return;
            }

            // Only Background0: in the light theme Background1..4 are white and shared with
            // SemiColorWhite, which must stay white (text on solid accents).
            ReplaceColor(palette, "SemiBackground0Color", background0);
            MutateBrush(palette, "SemiColorBackground0", background0);
        }
    }

    public void Complete()
    {
        foreach (var (dictionary, key, newValue) in _pending)
        {
            dictionary[key] = newValue;
        }

        _pending.Clear();
    }

    private void ReplaceColor(IResourceDictionary dictionary, string key, Color color)
    {
        if (dictionary.ContainsKey(key))
        {
            _pending.Add((dictionary, key, color));
        }
    }

    private void MutateBrush(IResourceDictionary dictionary, string key, Color color)
    {
        if (dictionary.TryGetValue(key, out var value)
            && value is SolidColorBrush brush
            && _mutatedBrushes.Add(brush))
        {
            brush.Color = color;
        }
    }

    private bool TryMapColor(Color color, ThemeVariant? variant, out Color newColor)
    {
        var first = IsDark(variant) ? _maps.DarkAccent : _maps.LightAccent;
        var second = IsDark(variant) ? _maps.LightAccent : _maps.DarkAccent;

        if (first.TryGetValue(color, out newColor))
        {
            return true;
        }

        if (second.TryGetValue(color, out newColor))
        {
            return true;
        }

        newColor = default;
        return false;
    }

    private static bool IsDark(ThemeVariant? variant) =>
        variant is not null
        && variant != ThemeVariant.Default
        && (variant == ThemeVariant.Dark || variant.InheritVariant == ThemeVariant.Dark);
}
