using System.Net.Http;
using Newtonsoft.Json;

namespace PoeAncientsPriceHelper;

// Maps a localized item name (read off a non-English PoE 2 client) to its canonical English name,
// so the price lookup — whose keys come from poe.ninja's English-only API — works for German,
// Portuguese, Russian, Spanish, … clients. Without this, every row on a localized client is a MISS
// (issue #29: "Chaossphäre" never matches "chaos orb").
//
// Locale files are English-keyed JSON (English name → localized name); the translator inverts them
// into normalized localized → English-key. Matching is exact → diacritic-folded → conservative fuzzy:
// folding rescues the common OCR slip of dropping/mangling an accent ("Chaossphäre" read as
// "chaossphare"), while fuzzy absorbs one-off OCR slips in non-Latin locale names. A name with no
// translation is returned unchanged, so an English client (or an already-English row) is a no-op
// pass-through.
//
// Only the ONE language the user selects (Settings -> Game language) is loaded. English loads nothing,
// so an English client does zero work. Deliberately NO glyph-skeleton step here:
// folding (o↔c↔e, n↔m↔u …) is aggressive enough to map one item onto another, and the accent-drop case
// it would catch is already covered by Fold. Glyph-level OCR slips are still absorbed downstream by
// ScanEngine's fuzzy matcher, which runs on the resolved English key.
internal sealed class NameTranslator
{
    // All keys are NameNormalizer.Normalize()d; values are the English price KEY (also normalized,
    // so they match PriceRepository's dictionary directly).
    private readonly Dictionary<string, string> _exact;      // normalized localized → english key
    private readonly Dictionary<string, string> _folded;     // Fold(localized)      → english key
    private const double FuzzyThreshold = 0.84;

    public int EntryCount => _exact.Count;
    public bool HasEntries => _exact.Count > 0;

    // An empty translator: Translate() is an identity pass-through. Used on English clients / when no
    // locale files are present.
    public static NameTranslator Empty { get; } = new(new Dictionary<string, string>());

    private const string LocaleDirName = "locales";
    private const string PersistedBundledDirName = "_bundled";
    private const string RemoteDirName = "_remote";
    private static readonly Uri RemoteRussianLocaleUrl = new(
        "https://raw.githubusercontent.com/XapcT/PoeAncientsPriceHelper/main/src/PoeAncientsPriceHelper/locales/ru.json");

    // Where locale files live:
    // 1) the bundled locales\ folder shipped next to the exe,
    // 2) the app-managed persisted fallback copied into LocalAppData (survives Velopack current\ swaps),
    // 3) the latest valid locale downloaded from the fork's GitHub main branch,
    // 4) the user's LocalAppData\locales\ drop-in overrides, loaded last.
    private static string BundledDirectory => Path.Combine(AppContext.BaseDirectory, LocaleDirName);
    private static string UserDirectory => Path.Combine(AppPaths.DataDir, LocaleDirName);
    private static string PersistedBundledDirectory => Path.Combine(UserDirectory, PersistedBundledDirName);
    private static string RemoteDirectory => Path.Combine(UserDirectory, RemoteDirName);
    private static IEnumerable<string> DefaultDirectories =>
        [BundledDirectory, PersistedBundledDirectory, RemoteDirectory, UserDirectory];

    // A locale offered in the settings "Game language" dropdown.
    public sealed record LocaleInfo(string Code, string DisplayName);

    // Build the translator for ONE selected client language. English ("en"), empty, or an unknown code
    // → the identity Empty translator (no translation), so the default English client does zero work and
    // carries no risk of a foreign entry shadowing an English row. Only that language's file is loaded
    // (bundled + any user override of the same code), which keeps the match set small and predictable —
    // unlike merging every language at once.
    public static NameTranslator ForLanguage(string? code) => ForLanguage(code, DefaultDirectories);

    internal static NameTranslator ForLanguage(string? code, IEnumerable<string> directories)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Equals("en", StringComparison.OrdinalIgnoreCase))
            return Empty;

        var pairs = new List<(string, string)>();
        foreach (var dir in directories)
        {
            var file = Path.Combine(dir, code + ".json");
            if (!File.Exists(file)) continue;
            try
            {
                var locale = JsonConvert.DeserializeObject<LocaleFile>(File.ReadAllText(file));
                if (locale?.Entries is null) continue;
                foreach (var (english, localized) in locale.Entries)
                    if (!string.IsNullOrWhiteSpace(localized))
                        pairs.Add((english, localized));
            }
            catch { /* a broken contribution must never crash scanning — skip it */ }
        }
        return FromPairs(pairs);
    }

    // Keep a known-good Russian locale outside Velopack's install folder. Updates replace current\
    // wholesale, but they leave AppPaths.DataDir alone, so this protects the RU fork from an update
    // that accidentally removes or breaks the bundled ru.json. Existing valid fallbacks are preserved:
    // a newer app can add translations through its bundled file, while the persisted file remains the
    // last-resort safety net for names that used to work.
    public static void EnsurePersistentFallbacks(Action<string>? log = null) =>
        EnsurePersistentFallback("ru", BundledDirectory, UserDirectory, log);

    public static Task RefreshRemoteLocalesAsync(HttpClient http, Action<string>? log = null) =>
        RefreshRemoteLocaleAsync("ru", RemoteRussianLocaleUrl, http, UserDirectory, log);

    internal static bool EnsurePersistentFallback(
        string code, string bundledDirectory, string userDirectory, Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        var source = Path.Combine(bundledDirectory, code + ".json");
        if (!IsUsableLocaleFile(source, code))
        {
            log?.Invoke($"locale fallback {code}: bundled source missing or invalid");
            return false;
        }

        var targetDir = Path.Combine(userDirectory, PersistedBundledDirName);
        var target = Path.Combine(targetDir, code + ".json");
        if (IsUsableLocaleFile(target, code))
            return false;

        try
        {
            Directory.CreateDirectory(targetDir);
            File.Copy(source, target, overwrite: true);
            log?.Invoke($"locale fallback {code}: copied to {target}");
            return true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"locale fallback {code}: copy failed: {ex.Message}");
            return false;
        }
    }

    internal static async Task<bool> RefreshRemoteLocaleAsync(
        string code, Uri url, HttpClient http, string userDirectory, Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("User-Agent", "PoeAncientsPriceHelper-RU");
            using var resp = await http.SendAsync(req, cts.Token);
            if (!resp.IsSuccessStatusCode)
            {
                log?.Invoke($"locale remote {code}: HTTP {(int)resp.StatusCode}");
                return false;
            }

            var json = await resp.Content.ReadAsStringAsync(cts.Token);
            if (!IsUsableLocaleJson(json, code))
            {
                log?.Invoke($"locale remote {code}: downloaded file is invalid");
                return false;
            }

            var targetDir = Path.Combine(userDirectory, RemoteDirName);
            var target = Path.Combine(targetDir, code + ".json");
            Directory.CreateDirectory(targetDir);
            WriteAllTextAtomic(target, json);
            log?.Invoke($"locale remote {code}: updated from {url}");
            return true;
        }
        catch (OperationCanceledException)
        {
            log?.Invoke($"locale remote {code}: refresh timed out");
            return false;
        }
        catch (Exception ex)
        {
            log?.Invoke($"locale remote {code}: refresh failed: {ex.Message}");
            return false;
        }
    }

    // Discover the locale files present (bundled + user) so the settings UI can list the languages the
    // app can actually translate. Deduped by code (a user file shadows the bundled one). English is NOT
    // returned — it's the no-translation default the UI prepends itself.
    public static IReadOnlyList<LocaleInfo> AvailableLocales() => AvailableLocales(DefaultDirectories);

    public static IReadOnlyList<LocaleInfo> AvailableLocales(IEnumerable<string> directories)
    {
        var byCode = new Dictionary<string, LocaleInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var dir in directories)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
            foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
            {
                try
                {
                    var locale = JsonConvert.DeserializeObject<LocaleFile>(File.ReadAllText(file));
                    var code = locale?.Code ?? Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(code) || code.Equals("en", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var name = string.IsNullOrWhiteSpace(locale?.Language) ? code : locale!.Language!;
                    byCode[code] = new LocaleInfo(code, name);
                }
                catch { /* skip malformed files */ }
            }
        }
        return byCode.Values.OrderBy(l => l.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private NameTranslator(Dictionary<string, string> exact)
    {
        _exact = exact;

        // Diacritic-folded index. A folded key that two DIFFERENT English items collapse onto is
        // ambiguous — drop it rather than risk a confident wrong match (the exact map still covers
        // the clean read). Same-target collisions are harmless and kept.
        _folded = BuildCollapsed(exact, NameNormalizer.Fold);
    }

    private static Dictionary<string, string> BuildCollapsed(
        Dictionary<string, string> exact, Func<string, string> collapse)
    {
        var result = new Dictionary<string, string>(exact.Count);
        var ambiguous = new HashSet<string>();
        foreach (var (loc, en) in exact)
        {
            var key = collapse(loc);
            if (ambiguous.Contains(key)) continue;
            if (result.TryGetValue(key, out var existing))
            {
                if (existing != en) { result.Remove(key); ambiguous.Add(key); }
            }
            else result[key] = en;
        }
        return result;
    }

    private static bool IsUsableLocaleFile(string path, string expectedCode)
    {
        if (!File.Exists(path)) return false;
        try
        {
            return IsUsableLocaleJson(File.ReadAllText(path), expectedCode);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUsableLocaleJson(string json, string expectedCode)
    {
        var locale = JsonConvert.DeserializeObject<LocaleFile>(json);
        if (locale?.Entries is not { Count: > 0 }) return false;
        return string.IsNullOrWhiteSpace(locale.Code)
               || locale.Code.Equals(expectedCode, StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteAllTextAtomic(string path, string text)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, text);
        if (File.Exists(path)) File.Replace(tmp, path, null);
        else File.Move(tmp, path);
    }

    // Resolve an OCR'd, already-normalized name to its English price key. Returns the input unchanged
    // when no translation applies (English client, or an item we have no mapping for) so the caller
    // can feed the result straight into the existing English matcher.
    public string Translate(string normalizedName)
    {
        if (_exact.Count == 0 || string.IsNullOrEmpty(normalizedName)) return normalizedName;
        if (_exact.TryGetValue(normalizedName, out var en)) return en;
        if (_folded.TryGetValue(NameNormalizer.Fold(normalizedName), out en)) return en;
        if (BestFuzzy(normalizedName) is { } fuzzy) return fuzzy;
        return normalizedName;
    }

    private string? BestFuzzy(string normalizedName)
    {
        if (normalizedName.Length < 6) return null;

        string? best = null;
        double bestScore = FuzzyThreshold;
        bool ambiguous = false;
        foreach (var (localized, english) in _exact)
        {
            if (Math.Abs(localized.Length - normalizedName.Length) > 3) continue;
            int dist = ScanEngine.Levenshtein(normalizedName, localized);
            double score = 1.0 - (double)dist / Math.Max(normalizedName.Length, localized.Length);
            if (score > bestScore)
            {
                bestScore = score;
                best = english;
                ambiguous = false;
            }
            else if (best is not null && Math.Abs(score - bestScore) < 0.000001 && english != best)
            {
                ambiguous = true;
            }
        }
        return ambiguous ? null : best;
    }

    // Build directly from English→localized pairs (used by tests and as the merge primitive).
    public static NameTranslator FromPairs(IEnumerable<(string English, string Localized)> pairs)
    {
        var exact = new Dictionary<string, string>();
        foreach (var (english, localized) in pairs)
        {
            var enKey = NameNormalizer.Normalize(english);
            var locKey = NameNormalizer.Normalize(localized);
            if (enKey.Length == 0 || locKey.Length == 0) continue;
            // Last writer wins on a localized-key collision (e.g. a user file overriding a bundled
            // one). Distinct items practically never share a localized name.
            exact[locKey] = enKey;
        }
        return exact.Count == 0 ? Empty : new NameTranslator(exact);
    }

    // Load every <code>.json locale file found under the given directories and merge them into one
    // translator. Later directories override earlier ones key-by-key, so a user file in DataDir\locales
    // can correct a bundled entry. Missing directories and malformed files are skipped (best-effort:
    // a broken contribution must never crash scanning).
    public static NameTranslator LoadFromDirectories(IEnumerable<string> directories, Action<string>? log = null)
    {
        var pairs = new List<(string, string)>();
        foreach (var dir in directories)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
            foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
            {
                try
                {
                    var locale = JsonConvert.DeserializeObject<LocaleFile>(File.ReadAllText(file));
                    if (locale?.Entries is null) continue;
                    foreach (var (english, localized) in locale.Entries)
                        if (!string.IsNullOrWhiteSpace(localized))
                            pairs.Add((english, localized));
                    log?.Invoke($"locale {Path.GetFileName(file)}: {locale.Entries.Count} entries");
                }
                catch (Exception ex)
                {
                    log?.Invoke($"locale {Path.GetFileName(file)} skipped: {ex.Message}");
                }
            }
        }
        return FromPairs(pairs);
    }

    // The on-disk locale schema. English-keyed so a contributor can diff a file against the English
    // reference list and see at a glance which items still need a translation.
    private sealed class LocaleFile
    {
        public string? Language { get; set; }
        public string? Code { get; set; }
        public string? Source { get; set; }
        public string? Note { get; set; }
        public Dictionary<string, string>? Entries { get; set; }
    }
}
