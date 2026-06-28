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

    [Fact]
    public void PersistentFallback_IsCopiedFromBundledRu_WhenMissing()
    {
        using var dir = new TempDir();
        var bundled = Path.Combine(dir.Path, "bundled");
        var user = Path.Combine(dir.Path, "user", "locales");
        Directory.CreateDirectory(bundled);

        File.Copy(Path.Combine(LocalesDir, "ru.json"), Path.Combine(bundled, "ru.json"));

        Assert.True(NameTranslator.EnsurePersistentFallback("ru", bundled, user));

        var fallback = Path.Combine(user, "_bundled", "ru.json");
        Assert.True(File.Exists(fallback));
        Assert.Equal("chaos orb", NameTranslator.ForLanguage("ru", [Path.GetDirectoryName(fallback)!])
            .Translate("сфера хаоса"));
    }

    [Fact]
    public void PersistentFallback_IsUsed_WhenBundledLocaleIsUnavailable()
    {
        using var dir = new TempDir();
        var fallbackDir = Path.Combine(dir.Path, "locales", "_bundled");
        Directory.CreateDirectory(fallbackDir);
        File.Copy(Path.Combine(LocalesDir, "ru.json"), Path.Combine(fallbackDir, "ru.json"));

        var translator = NameTranslator.ForLanguage("ru", [Path.Combine(dir.Path, "missing"), fallbackDir]);

        Assert.Equal("chaos orb", translator.Translate("сфера хаоса"));
        Assert.Equal("ward rune", translator.Translate("руна барьера"));
    }

    [Fact]
    public void PersistentFallback_DoesNotOverwriteExistingValidFallback()
    {
        using var dir = new TempDir();
        var bundled = Path.Combine(dir.Path, "bundled");
        var user = Path.Combine(dir.Path, "user", "locales");
        var fallbackDir = Path.Combine(user, "_bundled");
        Directory.CreateDirectory(bundled);
        Directory.CreateDirectory(fallbackDir);

        File.Copy(Path.Combine(LocalesDir, "ru.json"), Path.Combine(bundled, "ru.json"));
        var fallback = Path.Combine(fallbackDir, "ru.json");
        File.WriteAllText(fallback, """
        {
          "language": "Русский (Russian)",
          "code": "ru",
          "entries": {
            "Chaos Orb": "Старое рабочее имя"
          }
        }
        """);

        Assert.False(NameTranslator.EnsurePersistentFallback("ru", bundled, user));
        Assert.Contains("Старое рабочее имя", File.ReadAllText(fallback));
        Assert.DoesNotContain("Сфера хаоса", File.ReadAllText(fallback));
    }

    [Fact]
    public void ForLanguage_UsesProvidedDirectories()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "ru.json"), """
        {
          "language": "Русский (Russian)",
          "code": "ru",
          "entries": {
            "Chaos Orb": "Тестовая сфера"
          }
        }
        """);

        Assert.Equal("chaos orb", NameTranslator.ForLanguage("ru", [dir.Path]).Translate("тестовая сфера"));
    }

    [Fact]
    public async Task RemoteLocale_IsDownloadedAndLoaded()
    {
        using var dir = new TempDir();
        using var http = new HttpClient(new FakeHttpMessageHandler("""
        {
          "language": "Русский (Russian)",
          "code": "ru",
          "entries": {
            "Chaos Orb": "Удаленная сфера"
          }
        }
        """));

        Assert.True(await NameTranslator.RefreshRemoteLocaleAsync(
            "ru", new Uri("https://example.invalid/ru.json"), http, dir.Path));

        var remoteDir = Path.Combine(dir.Path, "_remote");
        Assert.True(File.Exists(Path.Combine(remoteDir, "ru.json")));
        Assert.Equal("chaos orb", NameTranslator.ForLanguage("ru", [remoteDir]).Translate("удаленная сфера"));
    }

    [Fact]
    public async Task RemoteLocale_InvalidDownload_DoesNotOverwriteExistingFile()
    {
        using var dir = new TempDir();
        var remoteDir = Path.Combine(dir.Path, "_remote");
        Directory.CreateDirectory(remoteDir);
        var remoteFile = Path.Combine(remoteDir, "ru.json");
        File.WriteAllText(remoteFile, """
        {
          "language": "Русский (Russian)",
          "code": "ru",
          "entries": {
            "Chaos Orb": "Рабочая сфера"
          }
        }
        """);
        using var http = new HttpClient(new FakeHttpMessageHandler("""{ "code": "ru", "entries": {} }"""));

        Assert.False(await NameTranslator.RefreshRemoteLocaleAsync(
            "ru", new Uri("https://example.invalid/ru.json"), http, dir.Path));

        Assert.Contains("Рабочая сфера", File.ReadAllText(remoteFile));
        Assert.Equal("chaos orb", NameTranslator.ForLanguage("ru", [remoteDir]).Translate("рабочая сфера"));
    }

    [Fact]
    public void UserLocale_OverridesRemoteLocale()
    {
        using var dir = new TempDir();
        var userDir = Path.Combine(dir.Path, "locales");
        var remoteDir = Path.Combine(userDir, "_remote");
        Directory.CreateDirectory(userDir);
        Directory.CreateDirectory(remoteDir);
        File.WriteAllText(Path.Combine(remoteDir, "ru.json"), """
        {
          "language": "Русский (Russian)",
          "code": "ru",
          "entries": {
            "Wrong Item": "Общее имя"
          }
        }
        """);
        File.WriteAllText(Path.Combine(userDir, "ru.json"), """
        {
          "language": "Русский (Russian)",
          "code": "ru",
          "entries": {
            "Chaos Orb": "Общее имя"
          }
        }
        """);

        Assert.Equal("chaos orb", NameTranslator.ForLanguage("ru", [remoteDir, userDir]).Translate("общее имя"));
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
