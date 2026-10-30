using Avalonia.Media;

namespace Semi.Avalonia.Recolor;

/// <summary>
///     Source color tables of the Semi.Avalonia palette, taken from
///     Tokens/Palette/Light.axaml and Tokens/Palette/Dark.axaml (v12.1.0.1).
///     Only the accent families (Blue / LightBlue) and background scale are used:
///     everything else (Success, Warning, Danger, Greys, AI gradients) stays untouched.
/// </summary>
public static class SemiPalette
{
    /// <summary>SemiBlue0..9Color of the dark palette (step 5 = default dark accent #54A9FF).</summary>
    public static readonly Color[] DarkBlue =
    [
        Color.Parse("#053170"),
        Color.Parse("#0A4694"),
        Color.Parse("#135CB8"),
        Color.Parse("#1D75DB"),
        Color.Parse("#2990FF"),
        Color.Parse("#54A9FF"),
        Color.Parse("#7FC1FF"),
        Color.Parse("#A9D7FF"),
        Color.Parse("#D4ECFF"),
        Color.Parse("#EFF8FF"),
    ];

    /// <summary>SemiLightBlue0..9Color of the dark palette (secondary accent family).</summary>
    public static readonly Color[] DarkLightBlue =
    [
        Color.Parse("#003761"),
        Color.Parse("#004D85"),
        Color.Parse("#0366A9"),
        Color.Parse("#0A81CC"),
        Color.Parse("#139FF0"),
        Color.Parse("#40B4F3"),
        Color.Parse("#6EC8F6"),
        Color.Parse("#9DDCF9"),
        Color.Parse("#CEEEFC"),
        Color.Parse("#EBF8FE"),
    ];

    /// <summary>SemiBlue0..9Color of the light palette (step 5 = default light accent #0064FA).</summary>
    public static readonly Color[] LightBlue =
    [
        Color.Parse("#EAF5FF"),
        Color.Parse("#CBE7FE"),
        Color.Parse("#98CDFD"),
        Color.Parse("#65B2FC"),
        Color.Parse("#3295FB"),
        Color.Parse("#0064FA"),
        Color.Parse("#0062D6"),
        Color.Parse("#004FB3"),
        Color.Parse("#003D8F"),
        Color.Parse("#002C6B"),
    ];

    /// <summary>SemiLightBlue0..9Color of the light palette (secondary accent family).</summary>
    public static readonly Color[] LightLightBlue =
    [
        Color.Parse("#E9F7FD"),
        Color.Parse("#C9ECFC"),
        Color.Parse("#95D8F8"),
        Color.Parse("#62C3F5"),
        Color.Parse("#30ACF1"),
        Color.Parse("#0095EE"),
        Color.Parse("#007BCA"),
        Color.Parse("#0063A7"),
        Color.Parse("#004B83"),
        Color.Parse("#00355F"),
    ];

    /// <summary>SemiBackground0..4Color of the dark palette (elevated surfaces get lighter).</summary>
    public static readonly Color[] DarkBackground =
    [
        Color.Parse("#16161A"),
        Color.Parse("#232429"),
        Color.Parse("#35363C"),
        Color.Parse("#43444A"),
        Color.Parse("#4F5159"),
    ];

    /// <summary>SemiBackground0..4Color of the light palette (all white, elevation via fills/shadows).</summary>
    public static readonly Color[] LightBackground =
    [
        Colors.White,
        Colors.White,
        Colors.White,
        Colors.White,
        Colors.White,
    ];

    /// <summary>Hardcoded text/TextBox selection background used in both variants.</summary>
    public static readonly Color SelectionBlue = Color.Parse("#0041C5");
}
