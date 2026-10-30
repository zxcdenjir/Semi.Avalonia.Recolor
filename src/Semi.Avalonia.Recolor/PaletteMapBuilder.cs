using System;
using System.Collections.Generic;

using Avalonia.Media;

namespace Semi.Avalonia.Recolor;

/// <summary>Everything the recolor pass needs, already resolved per theme variant.</summary>
/// <param name="DarkAccent">
///     Original hex → recolored hex for the dark palette (Blue + LightBlue + extras).
/// </param>
/// <param name="LightAccent">
///     Original hex → recolored hex for the light palette (Blue + LightBlue + extras).
/// </param>
/// <param name="DarkBackgroundRamp">
///     Dark theme Background0..4 ramp, null when the background is not recolored.
/// </param>
/// <param name="LightBackground0">
///     Light theme Background0 replacement, null when the background is not recolored.
/// </param>
internal sealed record RecolorMaps(
    IReadOnlyDictionary<Color, Color> DarkAccent,
    IReadOnlyDictionary<Color, Color> LightAccent,
    Color[]? DarkBackgroundRamp,
    Color? LightBackground0)
{
    public static RecolorMaps Empty { get; } =
        new(new Dictionary<Color, Color>(), new Dictionary<Color, Color>(), null, null);

    public bool IsEmpty => DarkAccent.Count == 0
                           && LightAccent.Count == 0
                           && DarkBackgroundRamp is null
                           && LightBackground0 is null;
}

internal static class PaletteMapBuilder
{
    /// <summary>
    ///     Builds the accent maps for both variants. <paramref name="darkAccent" /> is applied
    ///     as-is in the dark theme; <paramref name="lightAccent" /> is applied as-is in the light theme.
    /// </summary>
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

    /// <summary>Builds the inverse maps, restoring the original Semi palette.</summary>
    public static RecolorMaps Invert(RecolorMaps maps) => new(
        Invert(maps.DarkAccent),
        Invert(maps.LightAccent),
        maps.DarkBackgroundRamp is null ? null : SemiPalette.DarkBackground,
        maps.LightBackground0 is null ? null : SemiPalette.LightBackground[0]);

    private static IReadOnlyDictionary<Color, Color> Invert(IReadOnlyDictionary<Color, Color> map)
    {
        var inverse = new Dictionary<Color, Color>(map.Count);
        foreach (var (key, value) in map)
        {
            // A ramp can collapse two steps onto the same color; keep the first (accent) one.
            inverse.TryAdd(value, key);
        }

        return inverse;
    }

    private static void AddRamp(Dictionary<Color, Color> map, Color[] original, Color[] replacement)
    {
        for (var i = 0; i < original.Length; i++)
        {
            map[original[i]] = replacement[i];
        }
    }
}
