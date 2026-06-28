param(
    [string]$Version,
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $repo "src\PoeAncientsPriceHelper\PoeAncientsPriceHelper.csproj"
$tests = Join-Path $repo "src\PoeAncientsPriceHelper.Tests\PoeAncientsPriceHelper.Tests.csproj"
$publishDir = Join-Path $repo "publish\$Runtime"
$releaseRoot = Join-Path $repo "release"
$releaseNotes = Join-Path $releaseRoot "release-notes.md"

function Find-Dotnet {
    $candidates = @(
        $(if ($env:DOTNET_ROOT) { Join-Path $env:DOTNET_ROOT "dotnet.exe" }),
        (Join-Path $repo "..\.dotnet-sdk10\dotnet.exe"),
        (Join-Path $repo "..\.dotnet-sdk\dotnet.exe"),
        "D:\Soft\PoE2_Build\.dotnet-sdk\dotnet.exe",
        "dotnet"
    ) | Where-Object { $_ }

    foreach ($candidate in $candidates) {
        if ([System.IO.Path]::IsPathRooted($candidate)) {
            if (Test-Path -LiteralPath $candidate) { return $candidate }
        }
        elseif (Get-Command $candidate -ErrorAction SilentlyContinue) {
            return $candidate
        }
    }
    throw "dotnet SDK not found"
}

function Find-Vpk {
    $local = Join-Path $repo ".dotnet-tools\vpk.exe"
    if (Test-Path -LiteralPath $local) { return $local }

    $cmd = Get-Command "vpk" -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    throw "Velopack CLI not found. Install it with: dotnet tool install vpk --version 1.2.0 --tool-path .\.dotnet-tools"
}

function Assert-UnderRepoOutput([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $publishRoot = [System.IO.Path]::GetFullPath((Join-Path $repo "publish"))
    $releaseFull = [System.IO.Path]::GetFullPath($releaseRoot)
    if ($full.StartsWith($publishRoot, [System.StringComparison]::OrdinalIgnoreCase)) { return }
    if ($full.StartsWith($releaseFull, [System.StringComparison]::OrdinalIgnoreCase)) { return }
    throw "Refusing to remove path outside publish/release roots: $full"
}

$dotnet = Find-Dotnet
$vpk = Find-Vpk

if (-not $Version) {
    [xml]$csproj = Get-Content -LiteralPath $project
    $Version = $csproj.Project.PropertyGroup.Version | Select-Object -First 1
}
if (-not $Version) {
    throw "Version was not provided and was not found in the project file"
}

New-Item -ItemType Directory -Force -Path (Join-Path $repo "publish") | Out-Null
New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null

foreach ($path in @($publishDir, $releaseRoot)) {
    if (Test-Path -LiteralPath $path) {
        Assert-UnderRepoOutput $path
        Get-ChildItem -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
            $_.Attributes = "Normal"
        }
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null

if (-not $SkipTests) {
    & $dotnet test $tests --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& $dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

@"
## What's new

- Added a persisted Russian locale fallback outside Velopack's current folder.
- Russian name translation now survives future installer updates even if bundled locale files are removed or broken.
- Auto-updates continue to use XapcT/PoeAncientsPriceHelper GitHub Releases.

## Install

Download PoeAncientsPriceHelper-win-Setup.exe and run it. Existing Velopack installs update automatically.
"@ | Set-Content -LiteralPath $releaseNotes -Encoding UTF8

& $vpk pack `
    --packId "PoeAncientsPriceHelper" `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe "PoeAncientsPriceHelper.exe" `
    --outputDir $releaseRoot `
    --runtime $Runtime `
    --channel "win" `
    --packTitle "Poe Ancients Price Helper RU" `
    --packAuthors "XapcT" `
    --releaseNotes $releaseNotes `
    --icon (Join-Path $repo "src\PoeAncientsPriceHelper\app.ico") `
    --delta None
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Release artifacts:"
Get-ChildItem -LiteralPath $releaseRoot -File | Sort-Object Name | ForEach-Object {
    Write-Host $_.FullName
}
