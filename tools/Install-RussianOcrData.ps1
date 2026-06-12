param(
    [string]$Destination = (Join-Path $PSScriptRoot '..\src\PoeAncientsPriceHelper\tessdata\rus.traineddata')
)

$ErrorActionPreference = 'Stop'

$targetDir = Split-Path -Parent $Destination
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null

$url = 'https://github.com/tesseract-ocr/tessdata_fast/raw/main/rus.traineddata'
Invoke-WebRequest -Uri $url -OutFile $Destination
Get-FileHash -Algorithm SHA256 -LiteralPath $Destination
