param(
    [string]$League = "Runes of Aldur"
)

$ErrorActionPreference = "Stop"

if ($PSVersionTable.PSEdition -ne "Core") {
    $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwsh) {
        & $pwsh.Source -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath -League $League
        exit $LASTEXITCODE
    }

    throw "PowerShell 7+ is required to inspect the .NET 8 assembly. Install pwsh or run this script from PowerShell 7."
}

Add-Type -AssemblyName System.Web

$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
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

$project = Join-Path $repo "src\PoeAncientsPriceHelper\PoeAncientsPriceHelper.csproj"
& $dotnet build $project --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$assemblyPath = Join-Path $repo "src\PoeAncientsPriceHelper\bin\Debug\net8.0-windows\PoeAncientsPriceHelper.dll"
$asm = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$resolver = $asm.GetType("PoeAncientsPriceHelper.LocalizedNameResolver")
$ocr = $asm.GetType("PoeAncientsPriceHelper.OcrScanner")
$priceRepo = $asm.GetType("PoeAncientsPriceHelper.PriceRepository")

$binding = [System.Reflection.BindingFlags]"NonPublic,Public,Static"
$resolve = $resolver.GetMethod("Resolve", $binding)
$isKnown = $resolver.GetMethod("IsKnownRewardKey", $binding)
$ocrNorm = $ocr.GetMethod("NormalizeName", $binding)
$priceNorm = $priceRepo.GetMethod("NormalizeName", $binding)
$exchangeTypes = $priceRepo.GetField("ExchangeTypes", $binding).GetValue($null)

$headers = @{ "User-Agent" = "Mozilla/5.0" }
$priceKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$escapedLeague = [uri]::EscapeDataString($League)

foreach ($type in $exchangeTypes) {
    $url = "https://poe.ninja/poe2/api/economy/exchange/current/overview?league=$escapedLeague&type=$type"
    $json = Invoke-RestMethod -Uri $url -Headers $headers -TimeoutSec 30
    foreach ($item in $json.items) {
        [void]$priceKeys.Add($priceNorm.Invoke($null, @($item.name)))
    }
}

$html = (Invoke-WebRequest -Uri "https://poe2db.tw/ru/Runeshape_Combinations" -Headers $headers -TimeoutSec 45).Content
$matches = [regex]::Matches(
    $html,
    '<div class="d-flex justify-content-between"><div>(?<reward>.*?)</div><div>(?<price>.*?)</div></div>',
    [System.Text.RegularExpressions.RegexOptions]::Singleline
)

$rows = foreach ($match in $matches) {
    $raw = $match.Groups["reward"].Value
    $text = [regex]::Replace($raw, "<[^>]+>", " ")
    $text = [System.Web.HttpUtility]::HtmlDecode($text)
    $text = ($text -replace "\s+", " ").Trim()
    if (-not $text) { continue }

    $base = $text -replace "\s+x\d+\s+Lv.*$", ""
    $base = $base -replace "\s+Lv\d.*$", ""
    $base = $base.Trim()
    if (-not $base) { continue }

    $normalized = $ocrNorm.Invoke($null, @($base))
    $resolved = $resolve.Invoke($null, @($normalized))
    $hasPrice = $priceKeys.Contains($resolved)
    $knownReward = [bool]$isKnown.Invoke($null, @($resolved))

    [pscustomobject]@{
        Reward = $base
        Resolved = $resolved
        HasPrice = $hasPrice
        KnownReward = $knownReward
    }
}

$unique = $rows | Sort-Object Reward -Unique
$unresolved = @($unique | Where-Object { -not $_.HasPrice -and -not $_.KnownReward })

Write-Host "priceKeys=$($priceKeys.Count) uniqueRewards=$($unique.Count) unresolved=$($unresolved.Count)"
if ($unresolved.Count -gt 0) {
    $unresolved | Format-Table -Wrap -AutoSize
    exit 1
}
