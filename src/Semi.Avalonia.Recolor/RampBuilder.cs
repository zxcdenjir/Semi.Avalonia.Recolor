using System;

using Avalonia.Media;

namespace Semi.Avalonia.Recolor;

/// <summary>
///     Generates 10-step tonal ramps that preserve the lightness/chroma curve of the
///     original Semi scale while recoloring every step to the user's accent hue.
///     The user's color becomes step 5 (the accent step in both theme variants).
/// </summary>
public static class RampBuilder
{
    /// <summary>
    ///     Builds a ramp whose lightness curve matches <paramref name="original" />,
    ///     shifted so that step 5 has the lightness/chroma/hue of <paramref name="targetStep5" />.
    /// </summary>
    public static Color[] Build(Color[] original, Color targetStep5)
    {
        var orig = new Oklch[original.Length];
        for (var i = 0; i < original.Length; i++)
        {
            orig[i] = Oklch.FromColor(original[i]);
        }

        var target = Oklch.FromColor(targetStep5);
        double dL = target.L - orig[5].L;
        double chromaScale = orig[5].C <= 1e-6 ? 1.0 : target.C / orig[5].C;

        var result = new Color[original.Length];
        for (var i = 0; i < original.Length; i++)
        {
            double l = Math.Clamp(orig[i].L + dL, 0.0, 1.0);
            double c = orig[i].C * chromaScale;
            result[i] = Oklch.ToColor(l, c, target.H);
        }

        return result;
    }

    /// <summary>
    ///     Re-expresses <paramref name="color" /> in another lightness/chroma context:
    ///     e.g. adapting a dark-theme accent for use in the light theme, so that
    ///     contrast against the background matches the original theme relationship.
    /// </summary>
    public static Color Adapt(Color color, Color referenceFrom, Color referenceTo)
    {
        var from = Oklch.FromColor(referenceFrom);
        var to = Oklch.FromColor(referenceTo);
        var o = Oklch.FromColor(color);

        double chromaScale = from.C <= 1e-6 ? 1.0 : to.C / from.C;
        return Oklch.ToColor(to.L, o.C * chromaScale, o.H);
    }

    /// <summary>Derives the companion (secondary) color from a primary color, preserving the
    /// original Blue → LightBlue lightness/chroma/hue relationship.</summary>
    public static Color DeriveCompanion(Color primary, Color originalBlue5, Color originalLightBlue5)
    {
        var blue = Oklch.FromColor(originalBlue5);
        var lightBlue = Oklch.FromColor(originalLightBlue5);
        var p = Oklch.FromColor(primary);

        double dL = lightBlue.L - blue.L;
        double chromaScale = blue.C <= 1e-6 ? 1.0 : lightBlue.C / blue.C;
        double dH = lightBlue.H - blue.H;

        return Oklch.ToColor(Math.Clamp(p.L + dL, 0.0, 1.0), p.C * chromaScale, p.H + dH);
    }

    /// <summary>Keeps lightness and chroma of the source color but moves it to the target hue.</summary>
    public static Color RecolorHue(Color source, double targetHue)
    {
        var o = Oklch.FromColor(source);
        return Oklch.ToColor(o.L, o.C, targetHue);
    }

    /// <summary>
    ///     Builds the 5-step background ramp for the dark theme: the user's color becomes
    ///     Background0, higher steps follow the original background lightness curve.
    /// </summary>
    public static Color[] BuildBackgroundRamp(Color userBackground, Color[] originalBackground)
    {
        var o0 = Oklch.FromColor(originalBackground[0]);
        var user = Oklch.FromColor(userBackground);
        double dL = user.L - o0.L;

        var ramp = new Color[originalBackground.Length];
        ramp[0] = userBackground;
        for (var i = 1; i < originalBackground.Length; i++)
        {
            var o = Oklch.FromColor(originalBackground[i]);
            double chromaScale = o0.C <= 1e-6 ? 1.0 : o.C / o0.C;
            ramp[i] = Oklch.ToColor(Math.Clamp(o.L + dL, 0.0, 1.0), user.C * chromaScale, user.H);
        }

        return ramp;
    }
}
