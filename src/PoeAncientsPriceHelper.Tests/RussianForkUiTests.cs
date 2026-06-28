namespace PoeAncientsPriceHelper.Tests;

public class RussianForkUiTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PoeAncientsPriceHelper.sln")))
            dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("Repository root was not found.");
        return dir.FullName;
    }

    public static TheoryData<string, string[]> EnglishUiPhrases => new()
    {
        { "src/PoeAncientsPriceHelper/MainWindow.xaml", [
            "League:", "Region:", "Not calibrated", "Calibrate Region", "Idle — prices not loaded",
            "Text=\"Credits\"", "Text=\"Diagnostics\"", "⚙ Settings"
        ] },
        { "src/PoeAncientsPriceHelper/SettingsWindow.xaml", [
            "Start/Stop key:", "Debug overlay key:", "Calibrate key:", "Capture mode:", "Game language:",
            "Auto-start scanning and minimize to tray on launch", "Enable Island Rumour helper"
        ] },
        { "src/PoeAncientsPriceHelper/MainWindow.xaml.cs", [
            "Update now:", "Still running", "\"Show\"", "\"Exit\"", "\"Start\"", "\"Stop\""
        ] },
        { "src/PoeAncientsPriceHelper/SettingsWindow.xaml.cs", [
            "English (default)", "Fast (1.2s)", "Press a key", "Content = \"Rebind\""
        ] },
        { "src/PoeAncientsPriceHelper/CalibrationOverlay.cs", [
            "Drag a box around the item list panel", "Press ENTER to confirm, drag to redo"
        ] },
        { "src/PoeAncientsPriceHelper/RumourOverlay.cs", [
            "\"Rumour\"", "\"Map\"", "\"Mods\"", "\"Rating\"", "unknown rumour"
        ] },
        { "src/PoeAncientsPriceHelper/Program.cs", [
            "couldn't start", "startup error", "Please attach that file", "a crash log could not be written"
        ] },
    };

    [Theory]
    [MemberData(nameof(EnglishUiPhrases))]
    public void ForkUi_DoesNotRegressToLegacyEnglishLabels(string relativePath, string[] phrases)
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

        foreach (var phrase in phrases)
            Assert.DoesNotContain(phrase, text, StringComparison.Ordinal);
    }
}
