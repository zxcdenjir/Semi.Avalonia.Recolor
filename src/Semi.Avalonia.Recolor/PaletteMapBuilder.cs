using System;
using System.Collections.Generic;

using Avalonia.Media;

namespace Semi.Avalonia.Recolor;

/// <summary>Цветовые замены для акцентных палитр и фона каждой темы.</summary>
/// <param name="DarkAccent">Замены цветов акцентной шкалы тёмной темы.</param>
/// <param name="LightAccent">Замены цветов акцентной шкалы светлой темы.</param>
/// <param name="DarkBackgroundRamp">Шкала фона тёмной темы либо <see langword="null"/>.</param>
/// <param name="LightBackground0">Цвет Background0 светлой темы либо <see langword="null"/>.</param>
internal sealed record RecolorMaps(
    IReadOnlyDictionary<Color, Color> DarkAccent,
    IReadOnlyDictionary<Color, Color> LightAccent,
    Color[]? DarkBackgroundRamp,
    Color? LightBackground0)
{
}

internal static class PaletteMapBuilder
{
    /// <summary>Строит замены акцентных цветов и фона для светлой и тёмной тем.</summary>
    public static RecolorMaps Build(
        Color? darkAccent,
        Color? lightAccent,
        Color? secondaryDark,
        Color? secondaryLight,
        Color? darkBackground,
        Color? lightBackground)
    {
        var dark = new Dictionary<Color, Color>();
        var light = new Dictionary<Color, Color>();

        // Secondary (LightBlue) can be set independently of the primary accent.
        var darkSecondary = secondaryDark
                            ?? (darkAccent is { } da
                                ? RampBuilder.DeriveCompanion(da, SemiPalette.DarkBlue[5],
                                    SemiPalette.DarkLightBlue[5])
                                : (Color?)null);
        var lightSecondary = secondaryLight
                             ?? (lightAccent is { } la
                                 ? RampBuilder.DeriveCompanion(la, SemiPalette.LightBlue[5],
                                     SemiPalette.LightLightBlue[5])
                                 : (Color?)null);

        if (darkAccent is { } darkAccentValue)
        {
            AddRamp(dark, SemiPalette.DarkBlue, RampBuilder.Build(SemiPalette.DarkBlue, darkAccentValue));
            dark[SemiPalette.SelectionBlue] =
                RampBuilder.RecolorHue(SemiPalette.SelectionBlue, Oklch.FromColor(darkAccentValue).H);
        }

        if (darkSecondary is { } darkSecondaryValue)
        {
            AddRamp(dark, SemiPalette.DarkLightBlue,
                RampBuilder.Build(SemiPalette.DarkLightBlue, darkSecondaryValue));
        }

        if (lightAccent is { } lightAccentValue)
        {
            AddRamp(light, SemiPalette.LightBlue, RampBuilder.Build(SemiPalette.LightBlue, lightAccentValue));
            light[SemiPalette.SelectionBlue] =
                RampBuilder.RecolorHue(SemiPalette.SelectionBlue, Oklch.FromColor(lightAccentValue).H);
        }

        if (lightSecondary is { } lightSecondaryValue)
        {
            AddRamp(light, SemiPalette.LightLightBlue,
                RampBuilder.Build(SemiPalette.LightLightBlue, lightSecondaryValue));
        }

        return new RecolorMaps(
            dark,
            light,
            darkBackground is { } dbg ? RampBuilder.BuildBackgroundRamp(dbg, SemiPalette.DarkBackground) : null,
            lightBackground);
    }

    private static void AddRamp(Dictionary<Color, Color> map, Color[] original, Color[] replacement)
    {
        for (var i = 0; i < original.Length; i++)
        {
            map[original[i]] = replacement[i];
        }
    }
}
