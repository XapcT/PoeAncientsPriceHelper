using System.Drawing;

namespace PoeAncientsPriceHelper;

// A rumour panel located in a full-frame OCR pass: the signature header's bounds, the overall panel
// bounds (for adaptive overlay placement), and the rumour-name lines in top-to-bottom order.
internal sealed record DetectedRumourPanel(
    Rectangle HeaderBounds, Rectangle PanelBounds, IReadOnlyList<OcrTextLine> RumourLines);

// Finds the Island Rumour panel in a full-frame OCR result by its text signature, then extracts the
// rumour-name lines sitting between the "Island Rumours" header and the "Requires / Expedition
// Logbook" footer, within the panel's horizontal band. The panel's text signature is unique on screen,
// so a single positive match identifies it — no template matching needed. Pure logic over OCR lines
// so it is unit-testable with synthetic input modelled on the in-game panel.
internal static class RumourPanelDetector
{
    // Signature tokens unique to the panel — any one identifies it (matched fuzzily, since the stylised
    // header OCRs roughly). The rumour names sit directly beneath the lowest HEADER signature (the
    // "Island Rumours" tab) and above the FOOTER signature (the "Requires / Expedition Logbook" block).
    private static readonly string[] HeaderSignatures = ["island rumours", "uncharted waters"];
    private static readonly string[] FooterSignatures = ["expedition logbook", "requires"];
    // Boilerplate inside the panel that is not a rumour name.
    private static readonly string[] Boilerplate = ["use a logbook to chart the area"];

    // Looser than item matching — the panel header art OCRs rough, and a false positive is harmless
    // because the rumour names below it simply won't resolve.
    private const double SignatureThreshold = 0.72;
    // A rumour line must horizontally overlap the panel band by at least this fraction of its width.
    private const double MinHorizontalOverlap = 0.4;
    private const int MinRumourTextLength = 3;

    public static DetectedRumourPanel? Detect(IReadOnlyList<OcrTextLine> lines)
    {
        var headerMatches = lines.Where(l => MatchesAny(l.Text, HeaderSignatures)).ToList();
        if (headerMatches.Count == 0) return null;
        var footerMatches = lines.Where(l => MatchesAny(l.Text, FooterSignatures)).ToList();

        // Top anchor: bottom of the LOWEST header-signature line (the "Island Rumours" tab sits below
        // the "Uncharted Waters" title; rumour names are directly beneath the tab).
        int topAnchorBottom = headerMatches.Max(l => l.Bounds.Bottom);
        // Footer: top of the highest footer-signature line BELOW the top anchor. None → no lower bound.
        int footerTop = footerMatches.Where(l => l.Bounds.Top >= topAnchorBottom)
                                     .Select(l => l.Bounds.Top)
                                     .DefaultIfEmpty(int.MaxValue).Min();

        // Horizontal band = union of the signature lines, padded so slightly-wider rumour rows still
        // count as inside the panel.
        var signatureLines = headerMatches.Concat(footerMatches).ToList();
        int bandLeft = signatureLines.Min(l => l.Bounds.Left);
        int bandRight = signatureLines.Max(l => l.Bounds.Right);
        int pad = (bandRight - bandLeft) / 4;
        bandLeft -= pad;
        bandRight += pad;

        var rumourLines = new List<OcrTextLine>();
        foreach (var line in lines)
        {
            int centerY = line.Bounds.Top + line.Bounds.Height / 2;
            if (centerY < topAnchorBottom || centerY > footerTop) continue;
            if (MatchesAny(line.Text, HeaderSignatures) || MatchesAny(line.Text, FooterSignatures)) continue;
            if (MatchesAny(line.Text, Boilerplate)) continue;
            if (!HorizontallyOverlaps(line.Bounds, bandLeft, bandRight)) continue;
            var cleaned = line.Text.Trim();
            if (cleaned.Length < MinRumourTextLength || !cleaned.Any(char.IsLetter)) continue;
            rumourLines.Add(line);
        }
        if (rumourLines.Count == 0) return null;
        rumourLines.Sort((a, b) => a.Bounds.Top.CompareTo(b.Bounds.Top));

        var headerBounds = Union(headerMatches.Select(l => l.Bounds));
        var panelBounds = Union(signatureLines.Concat(rumourLines).Select(l => l.Bounds));
        return new DetectedRumourPanel(headerBounds, panelBounds, rumourLines);
    }

    // A line matches a signature if its normalized text contains the token, or is close to it by edit
    // distance (to survive OCR slips on the stylised header).
    private static bool MatchesAny(string text, string[] signatures)
    {
        var norm = NameNormalizer.Normalize(text);
        if (norm.Length == 0) return false;
        foreach (var sig in signatures)
        {
            if (norm.Contains(sig, StringComparison.Ordinal)) return true;
            int dist = ScanEngine.Levenshtein(norm, sig);
            double score = 1.0 - (double)dist / Math.Max(norm.Length, sig.Length);
            if (score >= SignatureThreshold) return true;
        }
        return false;
    }

    private static bool HorizontallyOverlaps(Rectangle r, int left, int right)
    {
        int overlap = Math.Min(r.Right, right) - Math.Max(r.Left, left);
        return overlap >= r.Width * MinHorizontalOverlap;
    }

    private static Rectangle Union(IEnumerable<Rectangle> rects)
    {
        Rectangle? acc = null;
        foreach (var r in rects)
            acc = acc is null ? r : Rectangle.Union(acc.Value, r);
        return acc ?? Rectangle.Empty;
    }
}
