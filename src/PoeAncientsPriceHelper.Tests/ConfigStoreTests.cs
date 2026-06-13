using System.Drawing;
using System.IO;
using System.Linq;
using PoeAncientsPriceHelper;

namespace PoeAncientsPriceHelper.Tests;

public class ConfigStoreTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        using var dir = new TempDir();
        var cfg = LoadFrom(dir.Path);
        Assert.Equal("Runes of Aldur", cfg.LeagueName);
        Assert.Equal(8, cfg.OverlayXOffset);
        Assert.Equal("custom_prices.json", cfg.CustomPricesPath);
        Assert.Equal("VcF5", cfg.StartStopHotkey);
        Assert.False(cfg.IsCalibrated);
    }

    [Fact]
    public void StartStopHotkey_RoundTrips()
    {
        using var dir = new TempDir();
        SaveTo(dir.Path, new AppConfig { StartStopHotkey = "VcF7" });
        Assert.Equal("VcF7", LoadFrom(dir.Path).StartStopHotkey);
    }

    [Fact]
    public void RoundTrip_AllFields()
    {
        using var dir = new TempDir();
        var original = new AppConfig
        {
            LeagueName = "Test League",
            RegionX = 10, RegionY = 20, RegionWidth = 300, RegionHeight = 400,
            OverlayXOffset = 16,
            ReferencePixelColor = "#AABBCC",
            CustomPricesPath = "my_prices.json"
        };
        SaveTo(dir.Path, original);
        var loaded = LoadFrom(dir.Path);
        Assert.Equal("Test League", loaded.LeagueName);
        Assert.Equal(new Rectangle(10, 20, 300, 400), loaded.RegionRect);
        Assert.Equal(16, loaded.OverlayXOffset);
        Assert.Equal("#AABBCC", loaded.ReferencePixelColor);
        Assert.Equal("my_prices.json", loaded.CustomPricesPath);
    }

    [Fact]
    public void AvailableLeagues_NotDuplicated_OnRoundTrip()
    {
        // Newtonsoft's ObjectCreationHandling.Auto appends a deserialized list onto a pre-populated
        // default, doubling entries. AvailableLeagues is [JsonIgnore]'d to stay code-only and avoid it.
        using var dir = new TempDir();
        SaveTo(dir.Path, new AppConfig());
        var loaded = LoadFrom(dir.Path);
        Assert.Equal(new AppConfig().AvailableLeagues, loaded.AvailableLeagues);
        Assert.Equal(loaded.AvailableLeagues.Count, loaded.AvailableLeagues.Distinct().Count());
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenJsonMalformed()
    {
        using var dir = new TempDir();
        File.WriteAllText(Path.Combine(dir.Path, "config.json"), "{ invalid json !!!");
        var cfg = LoadFrom(dir.Path);
        Assert.Equal("Runes of Aldur", cfg.LeagueName);
    }

    [Fact]
    public void Load_MigratesLegacyConfig_WhenAppDataConfigMissing()
    {
        using var dir = new TempDir();
        var appDataDir = Path.Combine(dir.Path, "appdata");
        var legacyDir = Path.Combine(dir.Path, "legacy");

        var legacyConfig = new AppConfig
        {
            RegionX = 60,
            RegionY = 194,
            RegionWidth = 670,
            RegionHeight = 714,
            StartStopHotkey = "VcF8"
        };
        SaveTo(legacyDir, legacyConfig);

        var loaded = ConfigStore.LoadFromPaths(ConfigPath(appDataDir), ConfigPath(legacyDir));

        Assert.Equal(new Rectangle(60, 194, 670, 714), loaded.RegionRect);
        Assert.Equal("VcF8", loaded.StartStopHotkey);
        Assert.True(File.Exists(ConfigPath(appDataDir)));
        Assert.Equal(new Rectangle(60, 194, 670, 714), LoadFrom(appDataDir).RegionRect);
    }

    [Fact]
    public void Load_PrefersAppDataConfig_WhenBothConfigsExist()
    {
        using var dir = new TempDir();
        var appDataDir = Path.Combine(dir.Path, "appdata");
        var legacyDir = Path.Combine(dir.Path, "legacy");

        SaveTo(appDataDir, new AppConfig { StartStopHotkey = "VcF6" });
        SaveTo(legacyDir, new AppConfig { StartStopHotkey = "VcF8" });

        var loaded = ConfigStore.LoadFromPaths(ConfigPath(appDataDir), ConfigPath(legacyDir));

        Assert.Equal("VcF6", loaded.StartStopHotkey);
    }

    private static AppConfig LoadFrom(string dir)
    {
        return ConfigStore.LoadFromPaths(ConfigPath(dir), Path.Combine(dir, "missing-legacy.json"));
    }

    private static void SaveTo(string dir, AppConfig cfg)
    {
        ConfigStore.SaveToPath(ConfigPath(dir), cfg);
    }

    private static string ConfigPath(string dir) => Path.Combine(dir, "config.json");
}

