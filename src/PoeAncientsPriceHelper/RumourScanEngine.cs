using System.Drawing;

namespace PoeAncientsPriceHelper;

// Background auto-detect loop for the rumour helper (#35). Gated cheaply by an OCR check for the
// "WORLD" label at the top-centre of the Atlas screen: off the map the loop only does that tiny check
// (~1 Hz, near-zero cost); on the map it runs the full-screen rumour detection on a throttle and
// shows/hides the overlay as the panel appears and closes. ESC / Left-Ctrl+click force-dismiss via a
// latch that re-arms once the panel is gone.
internal sealed class RumourScanEngine : IDisposable
{
    private readonly RumourScanner _scanner;
    private readonly Func<Rectangle> _screen;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    // Shared with the App hotkey hook: ESC / Ctrl+click set the dismiss latch; the loop keeps the
    // overlay hidden until the panel is gone, then clears it so a later panel shows again.
    private static volatile bool _dismissed;
    private static volatile bool _showing;
    public static bool IsShowing => _showing;
    public static void RequestDismiss() => _dismissed = true;

    private const int GateIntervalMs = 1000;   // ~1 Hz WORLD-gate check (cheap, small region)
    private const int ScanIntervalMs = 1800;   // full-screen detect throttle while on the map
    private const int TickMs = 150;            // loop granularity; the work is timestamp-throttled
    private const int HideAfterMisses = 2;     // signature-gone passes before hiding the overlay

    public RumourScanEngine(RumourScanner scanner, Func<Rectangle> screen)
    {
        _scanner = scanner;
        _screen = screen;
    }

    public bool IsRunning => _loop is { IsCompleted: false };

    public void Start()
    {
        if (IsRunning) return;
        _dismissed = false;
        _showing = false;
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunLoopAsync(_cts.Token));
    }

    public void StopAndWait(TimeSpan timeout)
    {
        _cts?.Cancel();
        try { _loop?.Wait(timeout); } catch { }
        HideOverlay();
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        bool onMap = false;
        int missStreak = 0;
        var lastGate = DateTime.MinValue;
        var lastScan = DateTime.MinValue;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var screen = _screen();

                // Cheap WORLD gate (~1 Hz). Off the map this is the only work the loop does.
                if ((now - lastGate).TotalMilliseconds >= GateIntervalMs)
                {
                    lastGate = now;
                    var gateLines = _scanner.CaptureLines(WorldGateRegion(screen));
                    bool nowOnMap = ContainsWorldToken(gateLines);
                    if (!nowOnMap && onMap)
                    {
                        // Left the map — drop the overlay and clear any dismiss latch.
                        HideOverlay();
                        _dismissed = false;
                        missStreak = 0;
                    }
                    onMap = nowOnMap;
                }

                // Full-screen detect (throttled) only while on the map.
                if (onMap && (now - lastScan).TotalMilliseconds >= ScanIntervalMs)
                {
                    lastScan = now;
                    var result = _scanner.ReadOnce(screen);
                    bool present = result is { Rows.Count: > 0 };

                    if (_dismissed)
                    {
                        // Held dismissed: stay hidden; once the panel is gone for a couple passes,
                        // clear the latch so a reopened / different panel shows again.
                        HideOverlay();
                        if (!present)
                        {
                            if (++missStreak >= HideAfterMisses) { _dismissed = false; missStreak = 0; }
                        }
                        else missStreak = 0;
                    }
                    else if (present)
                    {
                        RumourOverlayManager.Show(result!.Rows, result.PanelBounds);
                        _showing = true;
                        missStreak = 0;
                    }
                    else if (++missStreak >= HideAfterMisses)
                    {
                        HideOverlay();
                        missStreak = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RumourScanEngine] {ex.GetType().Name}: {ex.Message}");
            }

            try { await Task.Delay(TickMs, ct); }
            catch (OperationCanceledException) { break; }
        }

        HideOverlay();
    }

    private static void HideOverlay()
    {
        if (_showing) { RumourOverlayManager.HideNow(); _showing = false; }
    }

    // The small top-centre band where the Atlas shows the "WORLD" label. A fifth of the width and a
    // fifteenth of the height, centred horizontally at the very top — big enough to catch the label
    // across resolutions / UI scales, small enough that OCR'ing it ~1 Hz is negligible.
    public static Rectangle WorldGateRegion(Rectangle screen)
    {
        int w = Math.Max(1, screen.Width / 5);
        int h = Math.Max(1, screen.Height / 15);
        int x = screen.Left + (screen.Width - w) / 2;
        return new Rectangle(x, screen.Top, w, h);
    }

    // True if a gate line contains "world" as a whole word (so "underworld" etc. can't trip it).
    internal static bool ContainsWorldToken(IEnumerable<OcrTextLine> lines) =>
        lines.Any(l => NameNormalizer.Normalize(l.Text).Split(' ').Contains("world"));

    public void Dispose()
    {
        StopAndWait(TimeSpan.FromSeconds(2));
        _cts?.Dispose();
    }
}
