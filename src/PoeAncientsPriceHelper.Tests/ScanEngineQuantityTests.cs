using PoeAncientsPriceHelper;

namespace PoeAncientsPriceHelper.Tests;

public class ScanEngineQuantityTests
{
    [Theory]
    // readMultiplier, readExplicit, priorLocked, remembered → expected
    [InlineData(3, true, 1, 1, 3)]   // explicit Nx this pass always wins
    [InlineData(1, false, 3, 1, 3)]  // OCR dropped the marker → keep the locked stack
    [InlineData(1, false, 1, 3, 3)]  // not locked yet, but memory remembers the stack
    [InlineData(1, true, 1, 3, 1)]   // explicit 1x read overrides stale memory
    [InlineData(1, false, 1, 1, 1)]  // genuine single
    public void ResolveMultiplierForDisplay_PrefersReliableStackSignal(
        int readMultiplier,
        bool readExplicit,
        int priorLocked,
        int remembered,
        int expected)
    {
        int actual = ScanEngine.ResolveMultiplierForDisplay(readMultiplier, readExplicit, priorLocked, remembered);
        Assert.Equal(expected, actual);
    }
}
