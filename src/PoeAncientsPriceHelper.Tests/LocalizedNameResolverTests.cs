using PoeAncientsPriceHelper;

namespace PoeAncientsPriceHelper.Tests;

public class LocalizedNameResolverTests
{
    [Theory]
    [InlineData("Большая сфера усиления", "greater orb of augmentation")]
    [InlineData("Малая сфера златокузнеца", "lesser jeweller s orb")]
    [InlineData("Сфера царей", "regal orb")]
    [InlineData("Сфера возвышения", "exalted orb")]
    [InlineData("Руна мудрости лесной ведьмы Ассандры", "hedgewitch assandra s rune of wisdom")]
    [InlineData("уна мудрости лесной ведьмы Ассандры", "hedgewitch assandra s rune of wisdom")]
    [InlineData("на мудрости лесной ведьмы Ассандры", "hedgewitch assandra s rune of wisdom")]
    [InlineData("Неогранённый камень духа уровень 19", "uncut spirit gem level 19")]
    [InlineData("камень духа уровень 19", "uncut spirit gem level 19")]
    [InlineData("Неограненный камень умения уровень 15", "uncut skill gem level 15")]
    [InlineData("Неогранённый камень поддержки уровень 2", "uncut support gem level 2")]
    public void Resolve_RussianNames_ToPoeNinjaKeys(string input, string expected)
    {
        Assert.Equal(expected, LocalizedNameResolver.Resolve(OcrScanner.NormalizeName(input)));
    }

    [Fact]
    public void Resolve_UnknownName_ReturnsOriginal()
    {
        Assert.Equal("chilling flux", LocalizedNameResolver.Resolve("chilling flux"));
    }
}
