# Poe Ancients Price Helper

A lightweight screen overlay for **Path of Exile 2**. It watches a calibrated region of your screen,
reads the currency / reward list with OCR, looks up live prices from [poe.ninja](https://poe.ninja/poe2),
and draws a click-through price overlay next to each item — so you never have to alt-tab to check what a
stack is worth.

## Features

- **Live prices** next to each list row, sourced from poe.ninja (auto-refreshed every 30 minutes).
- **Stack-aware** — shows the total and the per-item price, e.g. `2 (0.5 each)`.
- **Uncut gems** (skill / spirit / support) priced by exact type **and level** — a row shows `?`
  rather than a guessed price if the gem type or level can't be read cleanly (neighbouring levels
  can differ several-fold, so a wrong-level price would be misleading).
- **GPU-accelerated capture** — uses Windows Graphics Capture (WGC) by default for low CPU usage,
  with automatic fallback to legacy GDI if WGC is unavailable.
- **Windows OCR engine** — uses the native `Windows.Media.Ocr` (WinRT) for fast, accurate detection
  of on-screen text. No external OCR dependencies.
- **Update notifications** — checks GitHub on startup and shows a link in the app when a newer
  release is available.
- **Click-through overlay** that never gets in the way of the game.
- **One-time calibration** — just drag a box around the in-game list panel.
- **Hotkeys:** `F5` start/stop · `F4` recalibrate · `F3` debug boxes · `Esc` / `Ctrl+Click` hide.
- **Minimize to tray** — scanning keeps running in the background.
- **🎨 Theme switcher** — 5 dark themes (Toxic, Midnight, Obsidian, Abyss, Ember). Defaults to
  **Toxic** — its dark green gradient complements the green Start button while keeping the same
  low-light feel.
- **🔤 Font selector** — 6 overlay fonts including **Fontin SmallCaps** (Path of Exile's official
  font), Angie SmallCaps, IBM Plex Mono, Consolas, Georgia, and Palatino Linotype. Defaults to
  **Fontin SmallCaps** for an authentic PoE look. Font changes apply live without restarting.

## Download & run

Grab the latest `PoeAncientsPriceHelper-vX.Y.Z-win-x64.zip` from the
[**Releases**](../../releases) page, unzip it anywhere, and double-click **`Start.cmd`**.
No install and no .NET runtime required — it's a self-contained Windows x64 build.

Full usage instructions (with screenshots) are in the `README.html` included in the download.

> Windows SmartScreen may warn that the app is unsigned — click **More info → Run anyway**.

## Build from source

Requires the **.NET 10 SDK** ([download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0))
and **Windows 10 version 2004+** / Windows 11.

```sh
# restore + build
dotnet build src/

# run tests
dotnet test src/PoeAncientsPriceHelper.Tests/

# build a self-contained release
dotnet publish src/PoeAncientsPriceHelper/ -c Release -r win-x64 --self-contained true -o publish
```

## Capture backend

The screen capture method is configurable via `config.json`:

| Value | Description |
|---|---|
| `"Auto"` (default) | Uses WGC (GPU-based) with automatic GDI fallback per frame |
| `"GDI"` | Forces legacy BitBlt capture (higher CPU, universal compatibility) |

WGC requires Windows 10 2004+. If WGC fails at runtime, the app silently falls back to GDI without
crashing.

## Tech

- **.NET 10** (`net10.0-windows10.0.19041.0`) — WPF (settings window) + WinForms (overlay)
- **Windows.Media.Ocr** (WinRT) for OCR — no external dependencies
- **Windows Graphics Capture** via Vortice.Direct3D11 + WinRT interop for screen capture
- **poe.ninja** API for live price data (parallel fetch over HTTP/2, 30-min auto-refresh)
- **SharpHook** for global hotkeys
- **WPF UI** (lepoco) for the settings window UI

## Support

If this tool saves you some alt-tabbing, there's a **☕ Buy me a coffee** button right in the app.
Thanks!

## Disclaimer for those who seem to be troubled by it.. 
Yes it was greatly helped by AI :D never the less it works and its free!
