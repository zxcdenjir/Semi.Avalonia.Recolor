using System;

using Avalonia.Media;

namespace Semi.Avalonia.Recolor;

/// <summary>
///     Строит тональные шкалы цветов в пространстве OKLCH, сохраняя кривую светлоты
///     и насыщенности исходной палитры Semi.
/// </summary>
public static class RampBuilder
{
    /// <summary>Строит шкалу на основе <paramref name="original"/> и помещает цвет
    /// <paramref name="targetStep5"/> на ступень 5.</summary>
    public static Color[] Build(Color[] original, Color targetStep5)
    {
        ArgumentNullException.ThrowIfNull(original);
        if (original.Length <= 5)
        {
            throw new ArgumentException("The source ramp must contain step 5.", nameof(original));
        }

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

    /// <summary>Адаптирует цвет к светлоте и насыщенности другой палитры, например
    /// переносит акцент тёмной темы в светлую.</summary>
    public static Color Adapt(Color color, Color referenceFrom, Color referenceTo)
    {
        var from = Oklch.FromColor(referenceFrom);
        var to = Oklch.FromColor(referenceTo);
        var o = Oklch.FromColor(color);

        double chromaScale = from.C <= 1e-6 ? 1.0 : to.C / from.C;
        return Oklch.ToColor(to.L, o.C * chromaScale, o.H);
    }

    /// <summary>Вычисляет Secondary из Primary, сохраняя исходное соотношение Blue и LightBlue.</summary>
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

    /// <summary>Меняет оттенок цвета, сохраняя его светлоту и насыщенность.</summary>
    public static Color RecolorHue(Color source, double targetHue)
    {
        var o = Oklch.FromColor(source);
        return Oklch.ToColor(o.L, o.C, targetHue);
    }

    /// <summary>Строит шкалу Background0–4 для тёмной темы на основе заданного цвета фона.</summary>
    public static Color[] BuildBackgroundRamp(Color userBackground, Color[] originalBackground)
    {
        ArgumentNullException.ThrowIfNull(originalBackground);
        if (originalBackground.Length == 0)
        {
            throw new ArgumentException("The source background ramp cannot be empty.", nameof(originalBackground));
        }

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
