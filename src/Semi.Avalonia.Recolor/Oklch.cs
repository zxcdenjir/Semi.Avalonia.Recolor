using System;

using Avalonia.Media;

namespace Semi.Avalonia.Recolor;

/// <summary>
///     Color in the OKLCH color space (perceptually uniform lightness/chroma/hue).
///     Conversions follow Björn Ottosson's OKLab reference implementation.
/// </summary>
/// <param name="L">Lightness, 0..1.</param>
/// <param name="C">Chroma, 0..~0.37 (clamped to sRGB gamut on conversion).</param>
/// <param name="H">Hue in radians.</param>
public readonly record struct Oklch(double L, double C, double H)
{
    public double HueDegrees => H * 180.0 / Math.PI;

    public static Oklch FromColor(Color color)
    {
        double lr = SrgbToLinear(color.R);
        double lg = SrgbToLinear(color.G);
        double lb = SrgbToLinear(color.B);

        double l = 0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb;
        double m = 0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb;
        double s = 0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb;

        double l_ = Math.Cbrt(l);
        double m_ = Math.Cbrt(m);
        double s_ = Math.Cbrt(s);

        double L = 0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_;
        double a = 1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_;
        double b = 0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_;

        return new Oklch(L, Math.Sqrt(a * a + b * b), Math.Atan2(b, a));
    }

    public Color ToColor() => ToColor(L, C, H);

    /// <summary>Converts OKLCH to sRGB, clamping chroma to the sRGB gamut.</summary>
    public static Color ToColor(double l, double c, double h)
    {
        c = ClampChromaToGamut(l, c, h);
        var (a, b) = (c * Math.Cos(h), c * Math.Sin(h));
        var (r, g, bb) = OklabToLinearRgb(l, a, b);
        return Color.FromRgb(LinearToSrgb(r), LinearToSrgb(g), LinearToSrgb(bb));
    }

    private static (double R, double G, double B) OklabToLinearRgb(double l, double a, double b)
    {
        double lRoot = l + 0.3963377774 * a + 0.2158037573 * b;
        double mRoot = l - 0.1055613458 * a - 0.0638541728 * b;
        double sRoot = l - 0.0894841775 * a - 1.2914855480 * b;

        double lCube = lRoot * lRoot * lRoot;
        double mCube = mRoot * mRoot * mRoot;
        double sCube = sRoot * sRoot * sRoot;

        return (
            +4.0767416621 * lCube - 3.3077115913 * mCube + 0.2309699292 * sCube,
            -1.2684380046 * lCube + 2.6097574011 * mCube - 0.3413193965 * sCube,
            -0.0041960863 * lCube - 0.7034186147 * mCube + 1.7076147010 * sCube);
    }

    /// <summary>Binary search for the largest chroma that still fits inside the sRGB gamut.</summary>
    private static double ClampChromaToGamut(double l, double c, double h)
    {
        if (InGamut(l, c, h)) return c;

        double lo = 0.0;
        double hi = c;
        for (var i = 0; i < 24; i++)
        {
            double mid = (lo + hi) / 2.0;
            if (InGamut(l, mid, h))
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return lo;
    }

    private static bool InGamut(double l, double c, double h)
    {
        const double epsilon = 1e-4;
        var (a, b) = (c * Math.Cos(h), c * Math.Sin(h));
        var (r, g, bb) = OklabToLinearRgb(l, a, b);
        return r >= -epsilon && r <= 1.0 + epsilon
               && g >= -epsilon && g <= 1.0 + epsilon
               && bb >= -epsilon && bb <= 1.0 + epsilon;
    }

    private static double SrgbToLinear(double channel)
    {
        double v = channel / 255.0;
        return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    private static byte LinearToSrgb(double v)
    {
        double s = v <= 0.0031308 ? v * 12.92 : 1.055 * Math.Pow(v, 1.0 / 2.4) - 0.055;
        return (byte)Math.Round(Math.Clamp(s, 0.0, 1.0) * 255.0);
    }
}
