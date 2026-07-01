# Builds a self-contained Windows x64 release (no .NET runtime required on the PC).
# Output: dist/Rule34Gallery-win-x64/ and dist/Rule34Gallery-win-x64.zip

param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [switch]$SingleFile
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "Rule34GalleryApp\Rule34GalleryApp.csproj"
$distRoot = Join-Path $repoRoot "dist"
$outDir = Join-Path $distRoot "Rule34Gallery-$Runtime"
$zipPath = "$outDir.zip"

Write-Host "Publishing Rule34 Gallery ($Runtime)..." -ForegroundColor Cyan

$publishArgs = @(
    "publish", $project,
    "-c", "Release",
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishDir=$outDir",
    "-p:DebugType=None",
    "-p:DebugSymbols=false"
)

if ($SingleFile) {
    $publishArgs += @("-p:PublishSingleFile=true")
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Do not ship real Firebase secrets in the zip; restore config in the folder for local Install.ps1.
$configInOut = Join-Path $outDir "firebase-config.json"
$devConfig = Join-Path $repoRoot "Rule34GalleryApp\firebase-config.json"
$configStash = Join-Path $env:TEMP "Rule34Gallery-firebase-config.publish.json"

if (Test-Path $devConfig) {
    Copy-Item $devConfig $configInOut -Force
    Write-Host "Staged firebase-config.json from Rule34GalleryApp (local install only; omitted from zip)." -ForegroundColor Green
}

if (Test-Path $configInOut) {
    Copy-Item $configInOut $configStash -Force
    Remove-Item $configInOut -Force
}

$installScript = Join-Path $PSScriptRoot "install-rule34-gallery.ps1"
$pathsScript = Join-Path $PSScriptRoot "windows-paths.ps1"
$updateScript = Join-Path $PSScriptRoot "apply-windows-update.ps1"
Copy-Item $installScript (Join-Path $outDir "Install.ps1") -Force
Copy-Item $pathsScript (Join-Path $outDir "windows-paths.ps1") -Force
Copy-Item $updateScript (Join-Path $outDir "apply-windows-update.ps1") -Force

$readme = @"
Rule34 Gallery — Windows install
================================

Quick start (portable)
  1. Unzip anywhere.
  2. Run Rule34Gallery.exe.

Install to your PC (Start Menu shortcut)
  1. Right-click Install.ps1 → Run with PowerShell
     (or: powershell -ExecutionPolicy Bypass -File Install.ps1)
  2. Launch "Rule34 Gallery" from the Start Menu.

Optional: desktop shortcut
  powershell -ExecutionPolicy Bypass -File Install.ps1 -DesktopShortcut

Firebase / Google sign-in
  Copy firebase-config.example.json to firebase-config.json next to the exe
  and fill in your keys (see README in the repo).

No .NET runtime required — this build is self-contained.
"@
Set-Content -Path (Join-Path $outDir "INSTALL.txt") -Value $readme -Encoding UTF8

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $outDir -DestinationPath $zipPath -Force

if (Test-Path $configStash) {
    Copy-Item $configStash $configInOut -Force
    Remove-Item $configStash -Force
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  Folder: $outDir"
Write-Host "  Zip:    $zipPath"
Write-Host ""
Write-Host "Portable: run Rule34Gallery.exe from the folder."
Write-Host "Install:  run Install.ps1 inside the folder (Start Menu shortcut)."
Write-Host "Copy firebase-config.example.json to firebase-config.json for Firebase login."
