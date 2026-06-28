using System.Drawing;
using PoeAncientsPriceHelper;

namespace PoeAncientsPriceHelper.Tests;

public class RumourScanEngineTests
{
    [Fact]
    public void WorldGateRegion_IsTopCentreBand_OfThePrimaryScreen()
    {
        var screen = new Rectangle(0, 0, 2560, 1440);
        var gate = RumourScanEngine.WorldGateRegion(screen);

        Assert.Equal(2560 / 5, gate.Width);          // a fifth of the width
        Assert.Equal(1440 / 15, gate.Height);        // a fifteenth of the height
        Assert.Equal(screen.Top, gate.Top);          // pinned to the very top
        // Horizontally centred.
        Assert.Equal(screen.Left + screen.Width / 2, gate.Left + gate.Width / 2);
    }

    [Fact]
    public void WorldGateRegion_RespectsAMonitorOrigin()
    {
        // A monitor to the left of the primary (negative origin).
        var screen = new Rectangle(-1920, 0, 1920, 1080);
        var gate = RumourScanEngine.WorldGateRegion(screen);

        Assert.Equal(screen.Top, gate.Top);
        Assert.Equal(screen.Left + screen.Width / 2, gate.Left + gate.Width / 2);
        Assert.True(gate.Left >= screen.Left && gate.Right <= screen.Right);
    }

    private static OcrTextLine Line(string text) => new(text, new Rectangle(0, 0, 100, 20));

    [Fact]
    public void ContainsWorldToken_TrueWhenWorldLabelPresent()
    {
        Assert.True(RumourScanEngine.ContainsWorldToken([Line("WORLD"), Line("Act 3")]));
    }

    [Fact]
    public void ContainsWorldToken_FalseForSubstringsAndNoise()
    {
        Assert.False(RumourScanEngine.ContainsWorldToken([Line("Underworld Map"), Line("Act 1")]));
        Assert.False(RumourScanEngine.ContainsWorldToken([Line("Mistwood"), Line("Hideout")]));
        Assert.False(RumourScanEngine.ContainsWorldToken([]));
    }
}
