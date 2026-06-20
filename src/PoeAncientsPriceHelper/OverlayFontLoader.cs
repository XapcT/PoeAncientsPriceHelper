using System.Drawing.Text;

namespace PoeAncientsPriceHelper;

// Loads bundled custom fonts (FontinSmallCaps, AngieSmallCaps, IBM Plex Mono) from the fonts/
// directory and provides Font creation by display name. System fonts (Consolas, Georgia,
// Palatino Linotype) are resolved directly by name. Only used by the overlay — the settings
// window always uses the WPF UI theme font.
internal static class OverlayFontLoader
{
    private static readonly PrivateFontCollection _pfc = new();
    private static bool _loaded;

    public static readonly string[] FontNames =
    [
        "Consolas", "Fontin SmallCaps", "Angie SmallCaps",
        "IBM Plex Mono", "Georgia", "Palatino Linotype"
    ];

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        var dir = Path.Combine(AppContext.BaseDirectory, "fonts");
        if (!Directory.Exists(dir)) return;
        foreach (var ttf in Directory.GetFiles(dir, "*.ttf"))
        {
            try { _pfc.AddFontFile(ttf); }
            catch { /* corrupt or unreadable — skip */ }
        }
    }

    // Returns a System.Drawing.Font for the overlay (GDI+). Fully qualified types because the
    // project uses both WinForms (System.Drawing) and WPF (System.Windows), and FontStyle /
    // FontFamily exist in both.
    public static System.Drawing.Font CreateFont(string name, float size, System.Drawing.FontStyle style)
    {
        EnsureLoaded();

        // System fonts — create directly by family name.
        if (name is "Consolas" or "Georgia" or "Palatino Linotype")
        {
            try { return new System.Drawing.Font(name, size, style); }
            catch { /* fall through to fallback */ }
        }

        // Custom fonts — match against loaded PrivateFontCollection families.
        var nameKey = name.Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
        foreach (var fam in _pfc.Families)
        {
            var famKey = fam.Name.Replace(" ", "", StringComparison.Ordinal).ToLowerInvariant();
            if (famKey == nameKey || famKey.Contains(nameKey, StringComparison.Ordinal) ||
                nameKey.Contains(famKey, StringComparison.Ordinal))
                return new System.Drawing.Font(fam, size, style);
        }

        // Last resort: try as a system font name, then Consolas.
        try { return new System.Drawing.Font(name, size, style); }
        catch { return new System.Drawing.Font("Consolas", size, style); }
    }
}
