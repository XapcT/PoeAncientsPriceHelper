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
    [InlineData("Руна дикости тана Гирта", "thane girt s rune of wildness")]
    [InlineData("Руна мастерства тана Граннеля", "thane grannell s rune of mastery")]
    [InlineData("Руна весны тана Лельда", "thane leld s rune of spring")]
    [InlineData("Руна лета тана Мирка", "thane myrk s rune of summer")]
    [InlineData("Древняя руна правления", "ancient rune of control")]
    [InlineData("Руна накопления", "rune of accumulation")]
    [InlineData("Руна охоты", "rune of the hunt")]
    [InlineData("Чародейский расплав Уровень 18", "thaumaturgic flux level 18")]
    [InlineData("Адаптивный сплав", "adaptive alloy")]
    [InlineData("Барьерная руна симбиоза", "warding rune of symbiosis")]
    [InlineData("Барьерная руна обломков", "warding rune of salvaging")]
    [InlineData("Барьерная руна уничтожения", "warding rune of annihilation")]
    [InlineData("Барьерная руна сердца", "warding rune of heart")]
    [InlineData("Барьерная руна скольжения", "warding rune of glancing")]
    [InlineData("Барьерная руна устойчивости", "warding rune of stability")]
    [InlineData("Барьерная руна храбрости", "warding rune of courage")]
    [InlineData("Барьерная руна укрепления", "warding rune of reinforcement")]
    [InlineData("Большая руна заряда", "greater charging rune")]
    [InlineData("Большая руна барьера", "greater ward rune")]
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
