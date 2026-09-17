# Build solution -> single dist folder with NDDownload.exe + NDDownloadIn.exe
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$OutputFolder = 'NDToolsBox'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solution = Join-Path $root 'NDDownload\NDDownload.sln'
$distRoot = Join-Path $root 'dist'
$outDir = Join-Path $distRoot $OutputFolder

$msbuild = @(
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $msbuild) {
    throw 'MSBuild not found. Install Visual Studio 2019 or 2022.'
}

Write-Host "=== MSBuild $Configuration ===" -ForegroundColor Cyan
& $msbuild $solution /p:Configuration=$Configuration /v:minimal /nologo /m

$ndOut = Join-Path $root "NDDownload\bin\$Configuration"
$inOut = Join-Path $root "NDDownloadIn\bin\$Configuration"

$ndExe = Join-Path $ndOut 'NDDownload.exe'
$inExe = Join-Path $inOut 'NDDownloadIn.exe'

if (-not (Test-Path $ndExe)) { throw "Missing: $ndExe" }
if (-not (Test-Path $inExe)) { throw "Missing: $inExe" }

Write-Host ''
Write-Host "=== Package -> $outDir ===" -ForegroundColor Cyan

if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

Copy-Item (Join-Path $ndOut '*') $outDir -Force
Copy-Item $inExe $outDir -Force
if (Test-Path (Join-Path $inOut 'NDDownloadIn.exe.config')) {
    Copy-Item (Join-Path $inOut 'NDDownloadIn.exe.config') $outDir -Force
}
if (Test-Path (Join-Path $inOut 'NDDownloadIn.pdb')) {
    Copy-Item (Join-Path $inOut 'NDDownloadIn.pdb') $outDir -Force
}

Get-ChildItem $inOut -File | Where-Object {
    $_.Extension -in '.dll', '.config' -and $_.Name -ne 'NDDownload.exe.config'
} | ForEach-Object {
    Copy-Item $_.FullName $outDir -Force
}

# 从 ResourcesUrl.version 生成服务器用版本文件
$resourcesUrlCs = Join-Path $root 'NDDownload.Core\Download\ResourcesUrl.cs'
$versionMatch = Select-String -Path $resourcesUrlCs -Pattern 'public\s+static\s+float\s+version\s*=\s*([0-9]+(?:\.[0-9]+)?)f?\s*;' | Select-Object -First 1
if (-not $versionMatch) {
    throw "Cannot parse ResourcesUrl.version from $resourcesUrlCs"
}
$installerVersion = $versionMatch.Matches[0].Groups[1].Value
$versionFileName = 'NDDownload_version.txt'
$versionOut = Join-Path $outDir $versionFileName
Set-Content -Path $versionOut -Value $installerVersion -Encoding ASCII -NoNewline
Copy-Item $versionOut (Join-Path $distRoot $versionFileName) -Force
Write-Host "  $versionFileName = $installerVersion" -ForegroundColor Green

$readmeLines = @(
    'NDToolsBox installer bundle'
    ''
    'Deploy entire folder to:'
    '  C:\ProgramData\Autodesk\ApplicationPlugins\NDToolsBox\'
    ''
    'Entry 1: NDDownload.exe - Tencent cloud server only'
    ''
    'Entry 2: NDDownloadIn.exe - Company intranet server only'
    ''
    "Installer version: $installerVersion"
    "Configuration: $Configuration"
    "Built: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
)
Set-Content -Path (Join-Path $outDir 'README.txt') -Value $readmeLines -Encoding UTF8
$readmeZh = Join-Path $root 'README.package.zh-CN.txt'
if (Test-Path $readmeZh) {
    Copy-Item $readmeZh (Join-Path $outDir 'README.zh-CN.txt') -Force
}

Write-Host '  NDDownload.exe' -ForegroundColor Green
Write-Host '  NDDownloadIn.exe' -ForegroundColor Green
Write-Host ''
Write-Host "Done: $outDir" -ForegroundColor Cyan
Write-Host "Upload to server: $versionFileName (also at dist\$versionFileName)" -ForegroundColor Cyan
