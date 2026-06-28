using PoeAncientsPriceHelper;

namespace PoeAncientsPriceHelper.Tests;

// End-to-end checks against the REAL shipped locale files (mirrored into the test output by the
// csproj). Guards against a malformed/renamed file or a regression in the loader — the unit tests in
// NameTranslatorTests cover the matching logic with synthetic data.
public class LocaleFilesTests
{
    private static string LocalesDir => Path.Combine(AppContext.BaseDirectory, "locales");

    private static NameTranslator Load() => NameTranslator.LoadFromDirectories([LocalesDir]);

    [Fact]
    public void BundledLocales_LoadWithoutError_AndHaveEntries()
    {
        var t = Load();
        Assert.True(t.HasEntries);
        Assert.True(t.EntryCount > 400, $"expected a few hundred merged entries, got {t.EntryCount}");
    }

    // The exact items from issue #29's German debug log must now resolve to their English price keys.
    [Theory]
    [InlineData("chaossphäre", "chaos orb")]
    [InlineData("große sphäre des goldschmieds", "greater jeweller s orb")]
    public void BundledLocales_ResolveIssue29GermanItems(string localized, string expectedKey)
    {
        Assert.Equal(expectedKey, Load().Translate(localized));
    }

    // One verified name per shipped language resolves (de/pt/ru/sp all loaded and merged).
    [Theory]
    [InlineData("chaossphäre")]        // de
    [InlineData("orbe du chaos")]      // fr
    [InlineData("orbe do caos")]       // pt
    [InlineData("сфера хаоса")]        // ru (Cyrillic)
    [InlineData("orbe de caos")]       // sp
    public void BundledLocales_EachLanguageResolvesChaosOrb(string localizedChaosOrb)
    {
        Assert.Equal("chaos orb", Load().Translate(localizedChaosOrb));
    }

    [Theory]
    [InlineData("наследие альдура", "aldur s legacy")]
    [InlineData("экспансивный сплав", "expansive alloy")]
    [InlineData("руна перерождения", "rebirth rune")]
    [InlineData("руна барьера", "ward rune")]
    public void BundledRussianLocale_ResolvesKnownForkNames(string localized, string expectedKey)
    {
        Assert.Equal(expectedKey, NameTranslator.ForLanguage("ru").Translate(localized));
    }

    // The settings dropdown is populated from the files actually present — de/fr/pt/ru/sp, never "en".
    [Fact]
    public void AvailableLocales_ListsTheSeededLanguages()
    {
        var locales = NameTranslator.AvailableLocales([LocalesDir]);
        var codes = locales.Select(l => l.Code).ToHashSet();
        Assert.Contains("de", codes);
        Assert.Contains("fr", codes);
        Assert.Contains("pt", codes);
        Assert.Contains("ru", codes);
        Assert.Contains("sp", codes);
        Assert.DoesNotContain("en", codes);
        Assert.All(locales, l => Assert.False(string.IsNullOrWhiteSpace(l.DisplayName)));
    }

    // ForLanguage loads ONLY the selected language; "en" / unknown codes are a no-op identity.
    [Fact]
    public void ForLanguage_LoadsSelectedLanguageOnly()
    {
        Assert.Equal("chaos orb", NameTranslator.ForLanguage("de").Translate("chaossphäre"));
        Assert.False(NameTranslator.ForLanguage("en").HasEntries);
        Assert.False(NameTranslator.ForLanguage("zz").HasEntries);   // no such file
        Assert.Equal("chaossphäre", NameTranslator.ForLanguage("en").Translate("chaossphäre")); // identity
    }
}
