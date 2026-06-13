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

$dotnetCandidates = @(
    (Join-Path $repo "..\.dotnet-sdk\dotnet.exe"),
    "dotnet"
)
$dotnet = $dotnetCandidates | Where-Object {
    if ([System.IO.Path]::IsPathRooted($_)) { Test-Path -LiteralPath $_ } else { Get-Command $_ -ErrorAction SilentlyContinue }
} | Select-Object -First 1
if (-not $dotnet) {
    throw "dotnet SDK not found"
}

if (-not $Version) {
    [xml]$csproj = Get-Content -LiteralPath $project
    $Version = $csproj.Project.PropertyGroup.Version | Select-Object -First 1
}
if (-not $Version) {
    throw "Version was not provided and was not found in the project file"
}

$releaseRoot = Join-Path $repo "release"
$packageRoot = Join-Path $releaseRoot "PoeAncientsPriceHelper"
$programDir = Join-Path $packageRoot "Program"
$zipPath = Join-Path $releaseRoot "PoeAncientsPriceHelper-v$Version-$Runtime.zip"

function Assert-UnderReleaseRoot([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $root = [System.IO.Path]::GetFullPath($releaseRoot)
    if (-not $full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove path outside release root: $full"
    }
}

New-Item -ItemType Directory -Force -Path $releaseRoot | Out-Null
foreach ($path in @($packageRoot, $zipPath)) {
    if (Test-Path -LiteralPath $path) {
        Assert-UnderReleaseRoot $path
        Get-ChildItem -LiteralPath $path -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
            $_.Attributes = "Normal"
        }
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

New-Item -ItemType Directory -Force -Path $programDir | Out-Null

if (-not $SkipTests) {
    & $dotnet test $tests --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& $dotnet publish $project `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $programDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Set-Content -LiteralPath (Join-Path $packageRoot "Start.cmd") `
    -Value "@echo off`r`nstart `"`" `"%~dp0Program\PoeAncientsPriceHelper.exe`"" `
    -Encoding ASCII

$readmeHtml = @'
<!DOCTYPE html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Poe Ancients Price Helper RU</title>
<style>
  :root { color-scheme: dark; }
  * { box-sizing: border-box; }
  body {
    margin: 0; padding: 40px 20px;
    background: #0F0F0F; color: #E0E0E0;
    font-family: "Segoe UI", system-ui, sans-serif;
    line-height: 1.6;
  }
  .wrap { max-width: 820px; margin: 0 auto; }
  h1 { font-size: 30px; margin: 0 0 4px; color: #fff; }
  h2 {
    font-size: 20px; margin: 34px 0 10px; color: #fff;
    border-bottom: 1px solid #2A2A2A; padding-bottom: 6px;
  }
  .tagline { color: #999; margin: 0 0 8px; font-size: 15px; }
  a { color: #66B3FF; }
  code, kbd {
    background: #1C1C1C; border: 1px solid #2A2A2A; border-radius: 4px;
    padding: 1px 6px; font-family: Consolas, monospace; font-size: 13px;
  }
  kbd { color: #66FF66; }
  ol, ul { padding-left: 22px; }
  li { margin: 6px 0; }
  table { border-collapse: collapse; margin: 12px 0; width: 100%; }
  th, td { text-align: left; padding: 8px 12px; border-bottom: 1px solid #2A2A2A; }
  th { color: #999; font-weight: 600; font-size: 13px; }
  .start { color: #66FF66; font-weight: 600; }
  .note {
    background: #14140C; border: 1px solid #3A3A1A; border-radius: 6px;
    padding: 12px 16px; margin: 14px 0; font-size: 14px;
  }
  .footer {
    margin-top: 42px; padding-top: 16px; border-top: 1px solid #2A2A2A;
    color: #888; font-size: 14px;
  }
</style>
</head>
<body>
<div class="wrap">

  <h1>Poe Ancients Price Helper RU</h1>
  <p class="tagline">Оверлей для Path of Exile 2: читает список наград на русском клиенте и показывает цены poe.ninja рядом со строками.</p>

  <h2>Запуск</h2>
  <ol>
    <li>Распакуйте архив целиком в любую папку.</li>
    <li>Запустите <code>Start.cmd</code>.</li>
    <li>Выберите лигу, нажмите <strong>Calibrate Region</strong> или <kbd>F4</kbd> и выделите область списка наград в игре.</li>
    <li>Нажмите <span class="start">Start</span>.</li>
  </ol>

  <h2>Что поддерживается</h2>
  <ul>
    <li>Русские названия валюты, рун, сплавов, расплавов, неограненных камней и наград Runeshape Combinations.</li>
    <li>Количество предметов в строке, например <code>Сфера царей (3)</code>.</li>
    <li>Награды без прямой цены poe.ninja показываются как <code>?</code>, чтобы строка не пропадала.</li>
    <li>Автообновление цен в фоне примерно каждые 30 минут.</li>
  </ul>

  <h2>Горячие клавиши</h2>
  <table>
    <tr><th>Клавиша</th><th>Действие</th></tr>
    <tr><td><kbd>F4</kbd></td><td>Заново выделить область списка</td></tr>
    <tr><td><kbd>F5</kbd></td><td>Запуск / остановка сканирования по умолчанию</td></tr>
    <tr><td><kbd>F3</kbd></td><td>Показать диагностические рамки</td></tr>
    <tr><td><kbd>ESC</kbd></td><td>Скрыть оверлей</td></tr>
    <tr><td><kbd>Ctrl</kbd> + клик</td><td>Скрыть оверлей при клике в игре</td></tr>
  </table>

  <h2>Диагностика</h2>
  <p>Если строка не оценивается, запустите <code>Program\PoeAncientsPriceHelper.exe --debug</code> и проверьте <code>Program\scan_log.txt</code>.</p>
  <div class="note">Windows SmartScreen может показать предупреждение, потому что сборка не подписана.</div>

  <div class="footer">Сторонний инструмент. Не связан с Grinding Gear Games.</div>

</div>
</body>
</html>
'@
Set-Content -LiteralPath (Join-Path $packageRoot "README.html") -Value $readmeHtml -Encoding UTF8

if ($PSVersionTable.PSVersion.Major -ge 5) {
    Compress-Archive -LiteralPath $packageRoot -DestinationPath $zipPath -CompressionLevel Optimal -Force
} else {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($packageRoot, $zipPath)
}

Write-Host "Release package:"
Write-Host $zipPath
