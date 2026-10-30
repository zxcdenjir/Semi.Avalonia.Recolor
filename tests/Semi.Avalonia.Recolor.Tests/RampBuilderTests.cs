using Avalonia.Media;

using Semi.Avalonia.Recolor;

namespace Semi.Avalonia.Recolor.Tests;

public sealed class RampBuilderTests
{
    [Theory]
    [InlineData("#FF6A00")]
    [InlineData("#00A870")]
    [InlineData("#7C4DFF")]
    public void BuildPlacesTargetAtAccentStepFive(string hex)
    {
        var target = Color.Parse(hex);

        var ramp = RampBuilder.Build(SemiPalette.DarkBlue, target);

        Assert.Equal(10, ramp.Length);
        Assert.Equal(target, ramp[5]);
    }

    [Theory]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    public void BuildHandlesExtremeColorsWithoutThrowing(string hex)
    {
        var ramp = RampBuilder.Build(SemiPalette.DarkBlue, Color.Parse(hex));

        Assert.Equal(10, ramp.Length);
        Assert.All(ramp, color => Assert.Equal(255, color.A));
    }

    [Fact]
    public void BuildRejectsRampsWithoutStepFive()
    {
        Assert.Throws<ArgumentException>(() => RampBuilder.Build(new Color[5], Colors.Blue));
    }

    [Fact]
    public void BuildBackgroundRampRejectsEmptySource()
    {
        Assert.Throws<ArgumentException>(() => RampBuilder.BuildBackgroundRamp(Colors.Black, []));
    }
}
