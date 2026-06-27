using System.Text;
using System.Text.RegularExpressions;

namespace PoeAncientsPriceHelper;

// Shared name normalization used by both OcrScanner (OCR text → key) and PriceRepository
// (API name → key). Extracted so both paths use identical logic and a single set of
// pre-compiled regex instances.
internal static class NameNormalizer
{
    private static readonly Regex NonWordSpace = new(@"[^\w\s]", RegexOptions.Compiled);
    private static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);

    public static string Normalize(string text)
    {
        var s = text.ToLowerInvariant();
        s = NonWordSpace.Replace(s, " ");
        s = MultiSpace.Replace(s, " ");
        return s.Trim();
    }

    // Collapse glyphs the stylised PoE panel font / Windows OCR confuse into canonical classes
    // (n/m/u→n, r/v→r, …), so a systematically garbled read still lines up with the true text. Used by
    // the rumour matcher (name → key) and the panel detector (excluding garbled boilerplate). Input
    // should already be Normalize()d.
    public static string Skeleton(string normalized)
    {
        var sb = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
            sb.Append(c switch
            {
                'w' or 'm' or 'n' or 'u' => 'n',
                'r' or 'v' => 'r',
                'i' or 'l' or 'j' or 't' => 'i',
                'o' or '0' or 'e' or 'c' => 'o',
                '4' or 'a' => 'a',
                _ => c,
            });
        return sb.ToString();
    }
}
