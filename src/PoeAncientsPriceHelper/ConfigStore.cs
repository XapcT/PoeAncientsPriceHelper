using Newtonsoft.Json;

namespace PoeAncientsPriceHelper;

internal static class ConfigStore
{
    private const string ConfigFileName = "config.json";
    private const string AppDataFolderName = "PoeAncientsPriceHelper";

    private static string ConfigPath =>
        Path.Combine(GetConfigDirectory(), ConfigFileName);

    private static string LegacyConfigPath =>
        Path.Combine(AppContext.BaseDirectory, ConfigFileName);

    public static AppConfig Load()
        => LoadFromPaths(ConfigPath, LegacyConfigPath);

    public static void Save(AppConfig config)
        => SaveToPath(ConfigPath, config);

    internal static AppConfig LoadFromPaths(string configPath, string legacyConfigPath)
    {
        if (File.Exists(configPath))
        {
            return TryLoad(configPath, out var config) ? config : new AppConfig();
        }

        if (!File.Exists(legacyConfigPath)) return new AppConfig();
        if (!TryLoad(legacyConfigPath, out var legacyConfig)) return new AppConfig();

        TrySave(configPath, legacyConfig);
        return legacyConfig;
    }

    internal static void SaveToPath(string configPath, AppConfig config)
        => TrySave(configPath, config);

    private static string GetConfigDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrWhiteSpace(appData)
            ? AppContext.BaseDirectory
            : Path.Combine(appData, AppDataFolderName);
    }

    private static bool TryLoad(string configPath, out AppConfig config)
    {
        config = new AppConfig();

        try
        {
            var json = File.ReadAllText(configPath);
            config = JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TrySave(string configPath, AppConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(configPath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
