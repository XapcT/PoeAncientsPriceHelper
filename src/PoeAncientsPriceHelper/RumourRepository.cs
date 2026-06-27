using Newtonsoft.Json;

namespace PoeAncientsPriceHelper;

// One Island/Expedition rumour row from the bundled snapshot (or a later sheet refresh, #37).
// Mods stay as the source's slash-separated string; Rating keeps its tier verbatim, including any
// "(see notes)" / "(is a gamble)" suffix — both are display strings, not parsed further here.
internal sealed record RumourEntry(string Rumor, string MapType, string Mods, string Rating);

// Loads the bundled rumour data and resolves an OCR'd rumour name to its row. Matching mirrors the
// price resolver in ScanEngine (exact → prefix → fuzzy) because the inputs have the same shape:
// the on-screen name is often TRUNCATED with an ellipsis ("Unknown ruins…") and carries casual
// spelling / apostrophes ("Somethin' fishy"). NameNormalizer already lowercases and turns the
// ellipsis, apostrophes and punctuation into spaces, so a normalized OCR name lines up with a
// normalized key without any rumour-specific cleanup.
internal sealed class RumourRepository
{
    public const string BundledFileName = "rumours.json";

    // Minimum edit-similarity (1 - dist/maxLen) for a fuzzy hit — same 0.84 the price matcher uses,
    // which absorbs ~1 wrong character on a short name without matching an unrelated rumour.
    private const double FuzzyThreshold = 0.84;
    // Below this length a name is too short to prefix-match safely (e.g. a 3-char fragment would
    // "start" half the table). Truncations we care about ("Cold", "Wild") are 4+.
    private const int MinPrefixLength = 4;

    private readonly Dictionary<string, RumourEntry> _byKey;        // normalized rumour name → entry
    private readonly Dictionary<int, List<string>> _keysByLength;   // length-bucketed keys for fuzzy

    public int Count => _byKey.Count;

    public RumourRepository(IEnumerable<RumourEntry> entries)
    {
        _byKey = new Dictionary<string, RumourEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var key = NameNormalizer.Normalize(entry.Rumor);
            if (!string.IsNullOrEmpty(key))
                _byKey[key] = entry;   // last definition wins, like the price dictionary
        }
        _keysByLength = _byKey.Keys.GroupBy(k => k.Length).ToDictionary(g => g.Key, g => g.ToList());
    }

    public static RumourRepository FromJson(string json)
    {
        var entries = JsonConvert.DeserializeObject<List<RumourEntry>>(json) ?? [];
        return new RumourRepository(entries);
    }

    // Load the snapshot shipped next to the exe (AppContext.BaseDirectory, copied via the csproj
    // Content item). Best-effort: a missing/corrupt file yields an empty repository rather than
    // throwing at startup — the overlay just shows "?" until data is available.
    public static RumourRepository LoadBundled()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, BundledFileName);
            if (File.Exists(path))
                return FromJson(File.ReadAllText(path));
            Console.Error.WriteLine($"[RumourRepository] bundled {BundledFileName} not found at {path}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[RumourRepository] load failed: {ex.Message}");
        }
        return new RumourRepository([]);
    }

    // Resolve an OCR'd rumour name to its entry, or null when nothing matches (caller shows '?').
    //   exact  → the normalized name is a known key.
    //   prefix → a stored key STARTS WITH the (truncated) OCR name; shortest such key wins.
    //   fuzzy  → closest key by edit distance, to absorb single-character OCR slips.
    public RumourEntry? Resolve(string ocrName)
    {
        var name = NameNormalizer.Normalize(ocrName);   // strips ellipsis/apostrophes, lowercases
        if (name.Length == 0)
            return null;

        if (_byKey.TryGetValue(name, out var exact))
            return exact;

        if (name.Length >= MinPrefixLength)
        {
            string? prefixKey = null;
            foreach (var key in _byKey.Keys)
                if (key.Length > name.Length &&
                    key.StartsWith(name, StringComparison.Ordinal) &&
                    (prefixKey is null || key.Length < prefixKey.Length))
                    prefixKey = key;
            if (prefixKey is not null)
                return _byKey[prefixKey];
        }

        var fuzzy = BestFuzzy(name);
        return fuzzy is not null ? _byKey[fuzzy] : null;
    }

    // Closest key to the OCR name by Levenshtein similarity, or null if nothing clears the
    // threshold. Only keys within ±3 of the name's length are considered (a large gap is never a
    // near-match), walking the length-bucketed index instead of every key.
    private string? BestFuzzy(string name)
    {
        string? best = null;
        double bestScore = FuzzyThreshold;   // must strictly exceed to win
        for (int len = Math.Max(1, name.Length - 3); len <= name.Length + 3; len++)
        {
            if (!_keysByLength.TryGetValue(len, out var keys)) continue;
            foreach (var key in keys)
            {
                int dist = ScanEngine.Levenshtein(name, key);
                double score = 1.0 - (double)dist / Math.Max(name.Length, key.Length);
                if (score > bestScore) { bestScore = score; best = key; }
            }
        }
        return best;
    }
}
