using PoeAncientsPriceHelper;

namespace PoeAncientsPriceHelper.Tests;

public class LocalizedNameResolverTests
{
    [Theory]
    [InlineData("Большая сфера усиления", "greater orb of augmentation")]
    [InlineData("Малая сфера златокузнеца", "lesser jeweller s orb")]
    [InlineData("Сфера царей", "regal orb")]
    [InlineData("Сфера возвышения", "exalted orb")]
    [InlineData("Уникальная бижутерия", "unique jewellery")]
    [InlineData("Руна мудрости лесной ведьмы Ассандры", "hedgewitch assandra s rune of wisdom")]
    [InlineData("уна мудрости лесной ведьмы Ассандры", "hedgewitch assandra s rune of wisdom")]
    [InlineData("на мудрости лесной ведьмы Ассандры", "hedgewitch assandra s rune of wisdom")]
    [InlineData("Руна дикости тана Гирта", "thane girt s rune of wildness")]
    [InlineData("Руна мастерства тана Граннеля", "thane grannell s rune of mastery")]
    [InlineData("Руна весны тана Лельда", "thane leld s rune of spring")]
    [InlineData("Руна лета тана Мирка", "thane myrk s rune of summer")]
    [InlineData("Древняя руна правления", "ancient rune of control")]
    [InlineData("Древняя руна вражды", "ancient rune of animosity")]
    [InlineData("Древняя руна тлена", "ancient rune of decay")]
    [InlineData("Древняя руна подрыва", "ancient rune of detonation")]
    [InlineData("Древняя руна находки", "ancient rune of discovery")]
    [InlineData("ревняя руна находки", "ancient rune of discovery")]
    [InlineData("Древняя руна поединка", "ancient rune of dueling")]
    [InlineData("Древняя руна мастерства", "ancient rune of prowess")]
    [InlineData("Древняя руна расплаты", "ancient rune of retaliation")]
    [InlineData("Древняя руна разбивания", "ancient rune of shattering")]
    [InlineData("Древняя руна осколков", "ancient rune of splinters")]
    [InlineData("Древняя руна орды", "ancient rune of the horde")]
    [InlineData("Древняя руна Титана", "ancient rune of the titan")]
    [InlineData("Древняя руна ведьмовства", "ancient rune of witchcraft")]
    [InlineData("Руна накопления", "rune of accumulation")]
    [InlineData("Руна акробатики", "rune of acrobatics")]
    [InlineData("Руна противоборства", "rune of confrontation")]
    [InlineData("Руна постоянства", "rune of consistency")]
    [InlineData("Руна кульминации", "rune of culmination")]
    [InlineData("Руна основ", "rune of foundations")]
    [InlineData("Руна охвата", "rune of reach")]
    [InlineData("Руна славы", "rune of renown")]
    [InlineData("Руна цветения", "rune of the blossom")]
    [InlineData("Руна охоты", "rune of the hunt")]
    [InlineData("Руна призмы", "rune of the prism")]
    [InlineData("Руна живого пламени", "rune of vital flame")]
    [InlineData("Руна живучести", "rune of vitality")]
    [InlineData("Чародейский расплав Уровень 18", "thaumaturgic flux level 18")]
    [InlineData("Адаптивный сплав", "adaptive alloy")]
    [InlineData("Руна агонии Фенумы", "fenumus rune of agony")]
    [InlineData("агонии", "fenumus rune of agony")]
    [InlineData("гуна агонии г енумы", "fenumus rune of agony")]
    [InlineData("Руна высушивания Фенумы", "fenumus rune of draining")]
    [InlineData("Руна плетения Фенумы", "fenumus rune of spinning")]
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
